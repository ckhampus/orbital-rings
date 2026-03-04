using System.Collections.Generic;
using Godot;
using OrbitalRings.Citizens;
using OrbitalRings.Data;
using OrbitalRings.Happiness;
using OrbitalRings.UI;

namespace OrbitalRings.Autoloads;

/// <summary>
/// Core progression engine: tracks station lifetime wishes, owns MoodSystem for
/// fluctuating mood state, gates citizen arrivals behind housing capacity, and
/// unlocks blueprints at wish-count milestones.
///
/// Registered as an Autoload in project.godot (6th, after all other singletons).
/// Access via HappinessManager.Instance.
///
/// Dual-value system (Phase 10):
///   _lifetimeHappiness — monotonically increasing integer, one per fulfilled wish
///   MoodSystem._mood   — fluctuating float (0–1), gains +0.06 per wish, decays each frame
/// </summary>
public partial class HappinessManager : Node
{
    /// <summary>
    /// Singleton instance. Set in _Ready(). Guaranteed non-null after Autoloads initialize.
    /// </summary>
    public static HappinessManager Instance { get; private set; }

    /// <summary>
    /// When true, _Ready() skips InitializeHousingCapacity. Set by SaveManager before
    /// scene transition so loaded state is not overwritten by default initialization.
    /// </summary>
    public static bool StateLoaded { get; set; }

    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    /// <summary>Interval in seconds between citizen arrival probability checks.</summary>
    private const float ArrivalCheckInterval = 60.0f;

    /// <summary>
    /// At 100% mood, this is the probability of a new citizen per check.
    /// P(arrival) = mood * ArrivalProbabilityScale.
    /// </summary>
    private const float ArrivalProbabilityScale = 0.6f;

    /// <summary>
    /// Minimum housing capacity accounting for the 5 starter citizens
    /// who bypass the housing check at game start.
    /// </summary>
    private const int StarterCitizenCapacity = 5;

    // -------------------------------------------------------------------------
    // Unlock milestones
    // -------------------------------------------------------------------------

    /// <summary>
    /// Blueprint unlock thresholds: (lifetime wish count, room IDs to unlock).
    /// Fired at exactly 4 and 12 lifetime wishes. Locked decision from CONTEXT.md.
    /// </summary>
    private static readonly (int wishCount, string[] rooms)[] UnlockMilestones =
    {
        (4,  new[] { "sky_loft", "craft_lab" }),
        (12, new[] { "star_lounge", "comm_relay" }),
    };

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private int _lifetimeHappiness;
    private MoodSystem _moodSystem;
    private MoodTier _lastReportedTier = MoodTier.Quiet;

    /// <summary>HappinessConfig resource — set via Inspector or loaded from default path in _Ready.</summary>
    [Export] public HappinessConfig Config { get; set; }

    private Timer _arrivalTimer;
    private readonly HashSet<string> _unlockedRooms = new()
    {
        "bunk_pod", "air_recycler", "workshop",
        "reading_nook", "storage_bay", "garden_nook"
    };

    /// <summary>
    /// Tracks how many milestones have been crossed (0, 1, or 2).
    /// Avoids re-checking already-triggered milestones.
    /// </summary>
    private int _crossedMilestoneCount;

    /// <summary>Running total of housing capacity from placed Housing-category rooms.</summary>
    private int _housingCapacity = StarterCitizenCapacity;

    /// <summary>
    /// Maps segment anchor index to BaseCapacity for placed Housing rooms.
    /// Used to subtract capacity on demolish (room is gone by the time
    /// RoomDemolished fires, so we track it ourselves).
    /// </summary>
    private readonly Dictionary<int, int> _housingRoomCapacities = new();

    /// <summary>
    /// CanvasLayer for displaying arrival floating text from this Autoload.
    /// Created in _Ready() since Autoloads aren't in the scene tree's UI layer.
    /// </summary>
    private CanvasLayer _arrivalCanvasLayer;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Total wishes fulfilled across the station's lifetime. Monotonically increasing.</summary>
    public int LifetimeWishes => _lifetimeHappiness;

    /// <summary>Current station mood (0.0–1.0). Fluctuates with activity and decay.</summary>
    public float Mood => _moodSystem?.Mood ?? 0f;

    /// <summary>
    /// Compatibility shim for SaveManager and EconomyManager until Phase 12/11 update them.
    /// Returns current mood float (same 0-1 range as old happiness).
    /// </summary>
    public float Happiness => _moodSystem?.Mood ?? 0f;

    /// <summary>Current mood tier with hysteresis applied.</summary>
    public MoodTier CurrentTier => _moodSystem?.CurrentTier ?? MoodTier.Quiet;

    /// <summary>
    /// Returns whether the given room ID is unlocked for building.
    /// Starter rooms are always unlocked; milestone rooms unlock at thresholds.
    /// </summary>
    public bool IsRoomUnlocked(string roomId) => _unlockedRooms.Contains(roomId);

    /// <summary>
    /// Returns the current housing capacity (starter + placed Housing rooms).
    /// Useful for HUD population display.
    /// </summary>
    public int CalculateHousingCapacity() => _housingCapacity;

    // -------------------------------------------------------------------------
    // Save/Load API
    // -------------------------------------------------------------------------

    /// <summary>Returns a copy of the currently unlocked room IDs (for save).</summary>
    public HashSet<string> GetUnlockedRoomIds() => new(_unlockedRooms);

    /// <summary>Returns the number of milestones crossed (for save).</summary>
    public int GetCrossedMilestoneCount() => _crossedMilestoneCount;

    /// <summary>Returns the current housing capacity (for save).</summary>
    public int GetHousingCapacity() => _housingCapacity;

    /// <summary>
    /// Restores all happiness/progression state from save data.
    /// Signature preserved for SaveManager backward compatibility (Phase 12 will expand it).
    /// happiness float from old save treated as mood — _lifetimeHappiness starts at 0 for old saves.
    /// </summary>
    public void RestoreState(float happiness, HashSet<string> unlockedRooms, int milestoneCount, int housingCapacity)
    {
        // happiness float from old save → treat as mood for backward compat
        _lifetimeHappiness = 0;  // old saves have no lifetime counter; start at 0
        _moodSystem?.RestoreState(happiness, 0f);
        _lastReportedTier = _moodSystem?.CurrentTier ?? MoodTier.Quiet;

        _unlockedRooms.Clear();
        foreach (var roomId in unlockedRooms)
            _unlockedRooms.Add(roomId);

        _crossedMilestoneCount = milestoneCount;
        _housingCapacity = housingCapacity;

        EconomyManager.Instance?.SetHappiness(happiness);
        // Do NOT call EmitHappinessChanged — HappinessBar will be replaced in Phase 13
    }

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        Instance = this;

        // Subscribe to events
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.WishFulfilled += OnWishFulfilled;
            GameEvents.Instance.RoomPlaced += OnRoomPlaced;
            GameEvents.Instance.RoomDemolished += OnRoomDemolished;
        }

        // Create arrival timer (same pattern as EconomyManager income timer)
        _arrivalTimer = new Timer();
        _arrivalTimer.WaitTime = ArrivalCheckInterval;
        _arrivalTimer.OneShot = false;
        AddChild(_arrivalTimer);
        _arrivalTimer.Timeout += OnArrivalCheck;
        _arrivalTimer.Start();

        // Timer pauses with the scene tree
        ProcessMode = ProcessModeEnum.Pausable;

        // Scan for any pre-placed housing rooms at startup
        // (skipped when loading from save -- SaveManager restores state directly)
        if (!StateLoaded)
            InitializeHousingCapacity();

        // Create CanvasLayer for arrival floating text (same layer as HUDLayer)
        _arrivalCanvasLayer = new CanvasLayer { Layer = 5 };
        AddChild(_arrivalCanvasLayer);

        // Load config (same pattern as EconomyManager loading EconomyConfig)
        if (Config == null)
            Config = ResourceLoader.Load<HappinessConfig>("res://Resources/Happiness/default_happiness.tres");
        if (Config == null)
        {
            GD.PushWarning("HappinessManager: No HappinessConfig found. Using code defaults.");
            Config = new HappinessConfig();
        }
        _moodSystem = new MoodSystem(Config);
    }

    public override void _ExitTree()
    {
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.WishFulfilled -= OnWishFulfilled;
            GameEvents.Instance.RoomPlaced -= OnRoomPlaced;
            GameEvents.Instance.RoomDemolished -= OnRoomDemolished;
        }
    }

    public override void _Process(double delta)
    {
        if (_moodSystem == null) return;

        var previousTier = _lastReportedTier;
        var newTier = _moodSystem.Update((float)delta, _lifetimeHappiness);

        if (newTier != previousTier)
        {
            _lastReportedTier = newTier;
            GameEvents.Instance?.EmitMoodTierChanged(newTier, previousTier);
        }
    }

    // -------------------------------------------------------------------------
    // Wish fulfillment handler
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called when any citizen's wish is fulfilled. Increments lifetime wish count,
    /// applies flat mood gain via MoodSystem, fires tier-change event if tier changed,
    /// updates economy multiplier, and checks unlock milestones.
    /// </summary>
    private void OnWishFulfilled(string citizenName, string wishType)
    {
        _lifetimeHappiness++;
        GameEvents.Instance?.EmitWishCountChanged(_lifetimeHappiness);

        var previousTier = _lastReportedTier;
        var newTier = _moodSystem.OnWishFulfilled();

        if (newTier != previousTier)
        {
            _lastReportedTier = newTier;
            GameEvents.Instance?.EmitMoodTierChanged(newTier, previousTier);
        }

        // Compatibility: continue calling SetHappiness with mood float until Phase 11 updates
        EconomyManager.Instance?.SetHappiness(_moodSystem.Mood);

        CheckUnlockMilestones();
    }

    // -------------------------------------------------------------------------
    // Unlock milestones
    // -------------------------------------------------------------------------

    /// <summary>
    /// Checks if lifetime wish count has crossed any new unlock thresholds.
    /// Iterates from the last crossed milestone to avoid re-triggering.
    /// </summary>
    private void CheckUnlockMilestones()
    {
        while (_crossedMilestoneCount < UnlockMilestones.Length)
        {
            var (wishCount, rooms) = UnlockMilestones[_crossedMilestoneCount];
            if (_lifetimeHappiness < wishCount) break;

            // Milestone crossed -- unlock rooms
            foreach (var roomId in rooms)
            {
                _unlockedRooms.Add(roomId);
                GameEvents.Instance?.EmitBlueprintUnlocked(roomId);
            }

            _crossedMilestoneCount++;
        }
    }

    // -------------------------------------------------------------------------
    // Citizen arrival check
    // -------------------------------------------------------------------------

    /// <summary>
    /// Periodic check (~60s): roll a probability based on current mood.
    /// If successful and population is below housing capacity, spawn a citizen
    /// with fade-in animation and floating arrival text.
    /// </summary>
    private void OnArrivalCheck()
    {
        if (Mood <= 0f) return;

        int currentPop = CitizenManager.Instance?.CitizenCount ?? 0;
        if (currentPop >= _housingCapacity) return;

        // P(arrival) = mood * scale (at 100% mood, 60% chance)
        float chance = Mood * ArrivalProbabilityScale;
        if (GD.Randf() < chance)
        {
            var citizen = CitizenManager.Instance?.SpawnCitizen();
            if (citizen != null)
            {
                // Fade-in animation (locked decision: "new citizen capsule fades in on walkway")
                citizen.SetMeshTransparencyMode(true);
                citizen.SetMeshAlpha(0f);
                var fadeTween = citizen.CreateTween();
                fadeTween.TweenMethod(
                    Callable.From((float alpha) => citizen.SetMeshAlpha(alpha)),
                    0.0f, 1.0f, 0.5f
                ).SetEase(Tween.EaseType.Out);
                fadeTween.TweenCallback(Callable.From(() => citizen.SetMeshTransparencyMode(false)));

                // Floating arrival text
                string name = citizen.Data?.CitizenName ?? "A citizen";
                SpawnArrivalText($"{name} has arrived!");
            }
        }
    }

    /// <summary>
    /// Spawns a floating "Name has arrived!" text at screen center using
    /// the reusable FloatingText class. Warm mint color for arrival fanfare.
    /// </summary>
    private void SpawnArrivalText(string message)
    {
        var floater = new FloatingText();
        _arrivalCanvasLayer.AddChild(floater);

        var viewport = GetViewport().GetVisibleRect().Size;
        Vector2 center = new Vector2(viewport.X / 2 - 80, viewport.Y / 2 - 60);
        floater.Setup(message, new Color(0.6f, 0.9f, 0.7f), center);
    }

    // -------------------------------------------------------------------------
    // Housing capacity tracking
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called when a room is placed. If it's a Housing-category room,
    /// adds its BaseCapacity to the running total.
    /// </summary>
    private void OnRoomPlaced(string roomType, int segmentIndex)
    {
        var roomInfo = Build.BuildManager.Instance?.GetPlacedRoom(segmentIndex);
        if (roomInfo == null) return;

        var def = roomInfo.Value.Definition;
        if (def.Category == RoomDefinition.RoomCategory.Housing)
        {
            int capacity = def.BaseCapacity;
            int anchorIndex = roomInfo.Value.AnchorIndex;
            _housingRoomCapacities[anchorIndex] = capacity;
            _housingCapacity += capacity;
        }
    }

    /// <summary>
    /// Called when a room is demolished. If it was a Housing-category room,
    /// subtracts its BaseCapacity from the running total.
    /// Uses _housingRoomCapacities dictionary since the room is already gone
    /// by the time this event fires.
    /// </summary>
    private void OnRoomDemolished(int segmentIndex)
    {
        if (_housingRoomCapacities.TryGetValue(segmentIndex, out int capacity))
        {
            _housingCapacity -= capacity;
            _housingRoomCapacities.Remove(segmentIndex);

            // Never go below starter capacity
            if (_housingCapacity < StarterCitizenCapacity)
                _housingCapacity = StarterCitizenCapacity;
        }
    }

    /// <summary>
    /// Scans all 24 segment indices at startup for any pre-placed housing rooms.
    /// Populates _housingRoomCapacities and updates _housingCapacity.
    /// </summary>
    private void InitializeHousingCapacity()
    {
        if (Build.BuildManager.Instance == null) return;

        // SegmentGrid has 24 total segments (12 outer + 12 inner)
        for (int i = 0; i < Ring.SegmentGrid.TotalSegments; i++)
        {
            var roomInfo = Build.BuildManager.Instance.GetPlacedRoom(i);
            if (roomInfo == null) continue;

            var def = roomInfo.Value.Definition;
            int anchorIndex = roomInfo.Value.AnchorIndex;

            // Only count each room once (by anchor index) and only Housing category
            if (def.Category == RoomDefinition.RoomCategory.Housing
                && !_housingRoomCapacities.ContainsKey(anchorIndex))
            {
                _housingRoomCapacities[anchorIndex] = def.BaseCapacity;
                _housingCapacity += def.BaseCapacity;
            }
        }
    }
}
