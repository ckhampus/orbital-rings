using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using OrbitalRings.Build;
using OrbitalRings.Citizens;

namespace OrbitalRings.Autoloads;

// -------------------------------------------------------------------------
// Save data POCOs (plain C# types only -- no Godot types)
// -------------------------------------------------------------------------

/// <summary>
/// Complete game state snapshot. All fields are plain C# types for
/// System.Text.Json serialization without custom converters.
/// </summary>
public class SaveData
{
    public int Version { get; set; } = 1;
    public int Credits { get; set; }
    public float Happiness { get; set; }
    public int CrossedMilestoneCount { get; set; }
    public int HousingCapacity { get; set; }

    // v2 fields (default to 0/0f when deserializing v1 saves)
    public int LifetimeHappiness { get; set; }
    public float Mood { get; set; }
    public float MoodBaseline { get; set; }

    public List<string> UnlockedRooms { get; set; } = new();
    public List<SavedRoom> PlacedRooms { get; set; } = new();
    public List<SavedCitizen> Citizens { get; set; } = new();
    public Dictionary<string, string> ActiveWishes { get; set; } = new();
    public Dictionary<string, int> PlacedRoomTypes { get; set; } = new();
}

/// <summary>
/// Serializable representation of a placed room on the ring.
/// Row encoded as int: 0=Outer, 1=Inner.
/// </summary>
public class SavedRoom
{
    public string RoomId { get; set; }
    public int Row { get; set; }
    public int StartPos { get; set; }
    public int SegmentCount { get; set; }
    public int Cost { get; set; }
}

/// <summary>
/// Serializable representation of a citizen. Colors stored as individual
/// float components to avoid Godot.Color serialization issues.
/// </summary>
public class SavedCitizen
{
    public string Name { get; set; }
    public int BodyType { get; set; }
    public float PrimaryR { get; set; }
    public float PrimaryG { get; set; }
    public float PrimaryB { get; set; }
    public float SecondaryR { get; set; }
    public float SecondaryG { get; set; }
    public float SecondaryB { get; set; }
    public float WalkwayAngle { get; set; }
    public float Direction { get; set; }
    public string CurrentWishId { get; set; }
}

// -------------------------------------------------------------------------
// SaveManager Autoload
// -------------------------------------------------------------------------

/// <summary>
/// Save/load orchestrator. Registered as Autoload in project.godot (last,
/// after all other singletons). Subscribes to all state-change events for
/// debounced autosave. Provides Load/ApplyState/ApplySceneState/HasSave/ClearSave.
///
/// Save format: JSON via System.Text.Json at user://save.json.
/// Corrupted saves return null (no crash).
///
/// Scene restoration uses a frame-delay pattern: after ChangeSceneToFile,
/// the caller invokes ScheduleSceneRestore(). SaveManager waits 2 frames
/// for all _Ready methods to complete, then applies scene-dependent state
/// (rooms, citizens, wishes).
///
/// Access via SaveManager.Instance (set in _Ready).
/// </summary>
public partial class SaveManager : Node
{
    /// <summary>Singleton instance, set in _Ready().</summary>
    public static SaveManager Instance { get; private set; }

    /// <summary>
    /// Pending save data waiting to be applied after scene loads.
    /// Set by ApplyState, consumed by ApplySceneState after frame delay.
    /// </summary>
    public SaveData PendingLoad { get; set; }

    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private const string SavePath = "user://save.json";

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private Timer _debounceTimer;
    private bool _saving;
    private int _pendingLoadFrames = -1;

    // Stored delegate references for unsubscription
    private Action<string, int> _onRoomPlaced;
    private Action<int> _onRoomDemolished;
    private Action<string, string> _onWishFulfilled;
    private Action<string> _onCitizenArrived;
    private Action<float> _onHappinessChanged;
    private Action<int> _onCreditsChanged;
    private Action<string> _onBlueprintUnlocked;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        Instance = this;

        // Create debounce timer (0.5s, one-shot)
        _debounceTimer = new Timer
        {
            Name = "AutosaveDebounce",
            WaitTime = 0.5,
            OneShot = true
        };
        AddChild(_debounceTimer);
        _debounceTimer.Timeout += PerformSave;

        // Initialize delegate fields (varying signatures need individual lambdas)
        _onRoomPlaced = (_, _) => OnAnyStateChanged();
        _onRoomDemolished = _ => OnAnyStateChanged();
        _onWishFulfilled = (_, _) => OnAnyStateChanged();
        _onCitizenArrived = _ => OnAnyStateChanged();
        _onHappinessChanged = _ => OnAnyStateChanged();
        _onCreditsChanged = _ => OnAnyStateChanged();
        _onBlueprintUnlocked = _ => OnAnyStateChanged();

        // Subscribe to all state-change events
        SubscribeEvents();
    }

    public override void _ExitTree()
    {
        UnsubscribeEvents();
    }

    public override void _Process(double delta)
    {
        // Frame-delay pattern for scene state restoration
        if (_pendingLoadFrames >= 0)
        {
            _pendingLoadFrames++;
            if (_pendingLoadFrames >= 2)
            {
                if (PendingLoad != null)
                {
                    ApplySceneState(PendingLoad);
                    PendingLoad = null;
                }
                _pendingLoadFrames = -1;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Event subscription
    // -------------------------------------------------------------------------

    private void SubscribeEvents()
    {
        if (GameEvents.Instance == null) return;

        GameEvents.Instance.RoomPlaced += _onRoomPlaced;
        GameEvents.Instance.RoomDemolished += _onRoomDemolished;
        GameEvents.Instance.WishFulfilled += _onWishFulfilled;
        GameEvents.Instance.CitizenArrived += _onCitizenArrived;
        GameEvents.Instance.HappinessChanged += _onHappinessChanged;
        GameEvents.Instance.CreditsChanged += _onCreditsChanged;
        GameEvents.Instance.BlueprintUnlocked += _onBlueprintUnlocked;
    }

    private void UnsubscribeEvents()
    {
        if (GameEvents.Instance == null) return;

        GameEvents.Instance.RoomPlaced -= _onRoomPlaced;
        GameEvents.Instance.RoomDemolished -= _onRoomDemolished;
        GameEvents.Instance.WishFulfilled -= _onWishFulfilled;
        GameEvents.Instance.CitizenArrived -= _onCitizenArrived;
        GameEvents.Instance.HappinessChanged -= _onHappinessChanged;
        GameEvents.Instance.CreditsChanged -= _onCreditsChanged;
        GameEvents.Instance.BlueprintUnlocked -= _onBlueprintUnlocked;
    }

    // -------------------------------------------------------------------------
    // Autosave trigger
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by any state-change event. Resets the debounce timer so saves
    /// only fire after 0.5s of quiet (prevents rapid-fire saves during batch
    /// operations like multiple citizens arriving at once).
    /// </summary>
    private void OnAnyStateChanged()
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    // -------------------------------------------------------------------------
    // Save
    // -------------------------------------------------------------------------

    /// <summary>
    /// Collects full game state from all singletons and writes to user://save.json.
    /// Guarded against re-entrancy.
    /// </summary>
    private void PerformSave()
    {
        if (_saving) return;
        _saving = true;

        try
        {
            var saveData = CollectGameState();
            string json = JsonSerializer.Serialize(saveData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PushWarning($"SaveManager: Could not open {SavePath} for writing.");
                return;
            }
            file.StoreString(json);
        }
        catch (Exception ex)
        {
            GD.PushWarning($"SaveManager: Save failed: {ex.Message}");
        }
        finally
        {
            _saving = false;
        }
    }

    /// <summary>
    /// Reads state from all singletons into a SaveData POCO.
    /// Only reads from Autoload singletons (never scene nodes directly).
    /// </summary>
    private SaveData CollectGameState()
    {
        var data = new SaveData
        {
            Version = 2,
            Credits = EconomyManager.Instance?.Credits ?? 0,
            Happiness = 0f, // v1 field: write 0 in v2 saves
            CrossedMilestoneCount = HappinessManager.Instance?.GetCrossedMilestoneCount() ?? 0,
            HousingCapacity = HappinessManager.Instance?.GetHousingCapacity() ?? 5,
            LifetimeHappiness = HappinessManager.Instance?.LifetimeWishes ?? 0,
            Mood = HappinessManager.Instance?.Mood ?? 0f,
            MoodBaseline = HappinessManager.Instance?.MoodBaseline ?? 0f,
            UnlockedRooms = HappinessManager.Instance?.GetUnlockedRoomIds().ToList() ?? new List<string>()
        };

        // Placed rooms
        if (BuildManager.Instance != null)
        {
            foreach (var (roomId, row, startPos, segmentCount, cost) in BuildManager.Instance.GetAllPlacedRooms())
            {
                data.PlacedRooms.Add(new SavedRoom
                {
                    RoomId = roomId,
                    Row = row,
                    StartPos = startPos,
                    SegmentCount = segmentCount,
                    Cost = cost
                });
            }
        }

        // Citizens
        if (CitizenManager.Instance != null)
        {
            foreach (var citizen in CitizenManager.Instance.Citizens)
            {
                var citizenData = citizen.Data;
                if (citizenData == null) continue;

                data.Citizens.Add(new SavedCitizen
                {
                    Name = citizenData.CitizenName,
                    BodyType = (int)citizenData.Body,
                    PrimaryR = citizenData.PrimaryColor.R,
                    PrimaryG = citizenData.PrimaryColor.G,
                    PrimaryB = citizenData.PrimaryColor.B,
                    SecondaryR = citizenData.SecondaryColor.R,
                    SecondaryG = citizenData.SecondaryColor.G,
                    SecondaryB = citizenData.SecondaryColor.B,
                    WalkwayAngle = citizen.CurrentAngle,
                    Direction = citizen.Direction,
                    CurrentWishId = citizen.CurrentWish?.WishId
                });
            }
        }

        // Active wishes (citizenName -> wishId)
        if (WishBoard.Instance != null)
        {
            foreach (var (citizenName, wish) in WishBoard.Instance.GetActiveWishes())
            {
                data.ActiveWishes[citizenName] = wish.WishId;
            }

            data.PlacedRoomTypes = WishBoard.Instance.GetPlacedRoomTypeCounts();
        }

        return data;
    }

    // -------------------------------------------------------------------------
    // Load
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reads and deserializes the save file. Returns null if file is missing,
    /// unreadable, or corrupted. Never throws.
    /// </summary>
    public SaveData Load()
    {
        if (!FileAccess.FileExists(SavePath))
            return null;

        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (file == null) return null;

        try
        {
            string json = file.GetAsText();
            return JsonSerializer.Deserialize<SaveData>(json);
        }
        catch (JsonException ex)
        {
            GD.PushWarning($"SaveManager: Corrupted save file: {ex.Message}");
            return null;
        }
    }

    // -------------------------------------------------------------------------
    // State application (pre-scene)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Restores Autoload singleton state that must be set BEFORE ChangeSceneToFile.
    /// Sets StateLoaded flags so scene _Ready() methods skip default initialization.
    /// Scene-dependent state (rooms, citizens) is deferred to ApplySceneState.
    /// </summary>
    public void ApplyState(SaveData data)
    {
        // Set StateLoaded flags so _Ready() guards skip default initialization
        CitizenManager.StateLoaded = true;
        HappinessManager.StateLoaded = true;
        EconomyManager.StateLoaded = true;

        // Restore economy
        EconomyManager.Instance?.RestoreCredits(data.Credits);

        // Restore happiness/progression (version-gated)
        if (data.Version >= 2)
        {
            HappinessManager.Instance?.RestoreState(
                data.LifetimeHappiness,
                data.Mood,
                data.MoodBaseline,
                new HashSet<string>(data.UnlockedRooms),
                data.CrossedMilestoneCount,
                data.HousingCapacity);
        }
        else
        {
            // v1 backward-compat: happiness float as mood, no lifetime/baseline
            HappinessManager.Instance?.RestoreState(
                0,
                data.Happiness,
                0f,
                new HashSet<string>(data.UnlockedRooms),
                data.CrossedMilestoneCount,
                data.HousingCapacity);
        }

        // Store for scene-dependent restoration after scene loads
        PendingLoad = data;
    }

    // -------------------------------------------------------------------------
    // State application (post-scene)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Restores scene-dependent state: rooms, citizens, and wishes.
    /// Called after the game scene has fully loaded (all _Ready methods complete).
    /// </summary>
    private void ApplySceneState(SaveData data)
    {
        // Restore rooms
        if (BuildManager.Instance != null)
        {
            BuildManager.Instance.ClearAllRooms();
            foreach (var room in data.PlacedRooms)
            {
                BuildManager.Instance.RestorePlacedRoom(
                    room.RoomId, room.Row, room.StartPos, room.SegmentCount, room.Cost);
            }
        }

        // Restore citizens
        if (CitizenManager.Instance != null)
        {
            CitizenManager.Instance.ClearCitizens();
            foreach (var citizen in data.Citizens)
            {
                CitizenManager.Instance.SpawnCitizenFromSave(
                    citizen.Name, citizen.BodyType,
                    citizen.PrimaryR, citizen.PrimaryG, citizen.PrimaryB,
                    citizen.SecondaryR, citizen.SecondaryG, citizen.SecondaryB,
                    citizen.WalkwayAngle, citizen.Direction,
                    citizen.CurrentWishId);
            }
        }

        // Restore wish board tracking
        if (WishBoard.Instance != null)
        {
            WishBoard.Instance.RestoreActiveWishes(data.ActiveWishes);
            if (data.PlacedRoomTypes != null)
            {
                WishBoard.Instance.RestorePlacedRoomTypes(data.PlacedRoomTypes);
            }
        }

        GD.Print("SaveManager: Game state restored from save.");
    }

    /// <summary>
    /// Starts the frame-delay counter for scene state restoration.
    /// Call this immediately after ChangeSceneToFile to ensure all scene
    /// _Ready methods complete before state is applied.
    /// </summary>
    public void ScheduleSceneRestore()
    {
        _pendingLoadFrames = 0;
    }

    // -------------------------------------------------------------------------
    // Utility
    // -------------------------------------------------------------------------

    /// <summary>Returns true if a save file exists.</summary>
    public bool HasSave() => FileAccess.FileExists(SavePath);

    /// <summary>Deletes the save file if it exists.</summary>
    public void ClearSave()
    {
        if (FileAccess.FileExists(SavePath))
        {
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
        }
    }
}
