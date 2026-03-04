using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using OrbitalRings.Citizens;
using OrbitalRings.Core;
using OrbitalRings.Data;

namespace OrbitalRings.Autoloads;

/// <summary>
/// Central wish tracking system. Manages active citizen wishes via event-driven
/// dictionary state -- no citizen iteration for wish tracking.
///
/// Loads all WishTemplate .tres resources at startup, tracks which citizens have
/// active wishes, and monitors placed room types for wish fulfillment queries.
///
/// Exposes <see cref="WishNudgeRequested"/> event for Plan 02 integration:
/// when a room is placed that could fulfill an active wish, this event fires
/// so CitizenNode can reset its visit timer.
///
/// Extends SafeNode for consistent signal lifecycle management.
/// Access via WishBoard.Instance (set in _Ready, before scene nodes enter tree).
/// </summary>
public partial class WishBoard : SafeNode
{
    /// <summary>
    /// Singleton instance. Set in _Ready(). Guaranteed non-null after Autoloads initialize
    /// because Autoloads initialize before any scene nodes enter the tree.
    /// </summary>
    public static WishBoard Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Wish tracking (event-driven dictionary -- no citizen iteration)
    // -------------------------------------------------------------------------

    /// <summary>Active wishes keyed by citizen name. One wish per citizen.</summary>
    private readonly Dictionary<string, WishTemplate> _activeWishes = new();

    // -------------------------------------------------------------------------
    // Room type tracking (for wish fulfillment queries)
    // -------------------------------------------------------------------------

    /// <summary>Count of placed instances per room type (RoomId).</summary>
    private readonly Dictionary<string, int> _placedRoomTypes = new();

    /// <summary>Maps segment flat index to room type for demolish lookups.</summary>
    private readonly Dictionary<int, string> _segmentRoomIds = new();

    // -------------------------------------------------------------------------
    // Template storage
    // -------------------------------------------------------------------------

    /// <summary>All loaded wish templates.</summary>
    private readonly List<WishTemplate> _allTemplates = new();

    /// <summary>Templates grouped by category for balanced wish generation.</summary>
    private readonly Dictionary<WishTemplate.WishCategory, List<WishTemplate>> _templatesByCategory = new();

    // -------------------------------------------------------------------------
    // Events (for Plan 02 integration)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fired when a newly placed room could fulfill an active wish.
    /// Plan 02 will have CitizenNode subscribe to this event to reset visit timers.
    /// </summary>
    public event Action<string> WishNudgeRequested;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        Instance = this;
        base._Ready();

        LoadTemplates();
        InitializePlacedRooms();
    }

    /// <summary>
    /// Loads all WishTemplate .tres resources from res://Resources/Wishes/.
    /// Groups them by category for balanced random selection.
    /// </summary>
    private void LoadTemplates()
    {
        using var dir = DirAccess.Open("res://Resources/Wishes/");
        if (dir == null)
        {
            GD.PushWarning("WishBoard: Could not open Resources/Wishes/ directory");
            return;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (!string.IsNullOrEmpty(fileName))
        {
            if (fileName.EndsWith(".tres"))
            {
                string path = $"res://Resources/Wishes/{fileName}";
                var template = ResourceLoader.Load<WishTemplate>(path);
                if (template != null)
                {
                    _allTemplates.Add(template);
                }
                else
                {
                    GD.PushWarning($"WishBoard: Failed to load template at {path}");
                }
            }
            fileName = dir.GetNext();
        }
        dir.ListDirEnd();

        // Group by category
        foreach (WishTemplate.WishCategory category in Enum.GetValues<WishTemplate.WishCategory>())
        {
            _templatesByCategory[category] = new List<WishTemplate>();
        }

        foreach (var template in _allTemplates)
        {
            _templatesByCategory[template.Category].Add(template);
        }

        GD.Print($"WishBoard: Loaded {_allTemplates.Count} wish templates");

        if (_allTemplates.Count == 0)
        {
            GD.PushWarning("WishBoard: No wish templates found in Resources/Wishes/");
        }
    }

    /// <summary>
    /// Scans BuildManager for any rooms already placed at game start.
    /// Populates _placedRoomTypes and _segmentRoomIds for rooms placed
    /// before WishBoard was ready (e.g., pre-built stations).
    /// </summary>
    private void InitializePlacedRooms()
    {
        var buildManager = Build.BuildManager.Instance;
        if (buildManager == null) return;

        // Scan all segment indices for placed rooms
        for (int i = 0; i < Ring.SegmentGrid.TotalSegments; i++)
        {
            var roomInfo = buildManager.GetPlacedRoom(i);
            if (roomInfo != null)
            {
                string roomId = roomInfo.Value.Definition.RoomId;
                int anchorIndex = roomInfo.Value.AnchorIndex;

                // Only track each room once (use anchor index as canonical key)
                if (!_segmentRoomIds.ContainsKey(anchorIndex))
                {
                    _segmentRoomIds[anchorIndex] = roomId;

                    if (_placedRoomTypes.ContainsKey(roomId))
                        _placedRoomTypes[roomId]++;
                    else
                        _placedRoomTypes[roomId] = 1;
                }
            }
        }
    }

    // -------------------------------------------------------------------------
    // Event subscriptions (SafeNode lifecycle)
    // -------------------------------------------------------------------------

    protected override void SubscribeEvents()
    {
        if (GameEvents.Instance == null) return;
        GameEvents.Instance.WishGenerated += OnWishGenerated;
        GameEvents.Instance.WishFulfilled += OnWishFulfilled;
        GameEvents.Instance.RoomPlaced += OnRoomPlaced;
        GameEvents.Instance.RoomDemolished += OnRoomDemolished;
    }

    protected override void UnsubscribeEvents()
    {
        if (GameEvents.Instance == null) return;
        GameEvents.Instance.WishGenerated -= OnWishGenerated;
        GameEvents.Instance.WishFulfilled -= OnWishFulfilled;
        GameEvents.Instance.RoomPlaced -= OnRoomPlaced;
        GameEvents.Instance.RoomDemolished -= OnRoomDemolished;
    }

    // -------------------------------------------------------------------------
    // Event handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called when a citizen generates a new wish. Stores the active wish in the dictionary.
    /// </summary>
    private void OnWishGenerated(string citizenName, string wishId)
    {
        var template = _allTemplates.FirstOrDefault(t => t.WishId == wishId);
        if (template != null)
        {
            _activeWishes[citizenName] = template;
        }
        else
        {
            GD.PushWarning($"WishBoard: Unknown wish ID '{wishId}' for citizen '{citizenName}'");
        }
    }

    /// <summary>
    /// Called when a citizen's wish is fulfilled. Removes from active tracking.
    /// </summary>
    private void OnWishFulfilled(string citizenName, string wishId)
    {
        _activeWishes.Remove(citizenName);
    }

    /// <summary>
    /// Called when a room is placed. Tracks the room type and segment mapping,
    /// then nudges citizens whose active wishes match the placed room.
    /// </summary>
    private void OnRoomPlaced(string roomType, int segmentIndex)
    {
        // Track room type count
        if (_placedRoomTypes.ContainsKey(roomType))
            _placedRoomTypes[roomType]++;
        else
            _placedRoomTypes[roomType] = 1;

        // Track segment-to-room mapping (for demolish lookup)
        _segmentRoomIds[segmentIndex] = roomType;

        // Nudge citizens whose active wish can be fulfilled by this room type
        NudgeCitizensForRoom(roomType);
    }

    /// <summary>
    /// Called when a room is demolished. Decrements room type count and cleans up
    /// segment mapping. Uses _segmentRoomIds to look up which room type was removed
    /// (since RoomDemolished only provides segmentIndex, not room type).
    /// </summary>
    private void OnRoomDemolished(int segmentIndex)
    {
        if (_segmentRoomIds.TryGetValue(segmentIndex, out string roomType))
        {
            _segmentRoomIds.Remove(segmentIndex);

            if (_placedRoomTypes.ContainsKey(roomType))
            {
                _placedRoomTypes[roomType]--;
                if (_placedRoomTypes[roomType] <= 0)
                    _placedRoomTypes.Remove(roomType);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Nudge system
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fires <see cref="WishNudgeRequested"/> for each citizen whose active wish
    /// has a FulfillingRoomIds entry matching the given room type.
    /// Plan 02 will have CitizenNode subscribe to this event to reset visit timers.
    /// </summary>
    public void NudgeCitizensForRoom(string roomType)
    {
        foreach (var (citizenName, wish) in _activeWishes)
        {
            if (wish.FulfillingRoomIds.Contains(roomType))
            {
                WishNudgeRequested?.Invoke(citizenName);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Query API (public methods for external consumers)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the active wish for a citizen, or null if they have no active wish.
    /// </summary>
    public WishTemplate GetWishForCitizen(string citizenName)
    {
        return _activeWishes.TryGetValue(citizenName, out var wish) ? wish : null;
    }

    /// <summary>
    /// Returns the number of currently active wishes across all citizens.
    /// </summary>
    public int GetActiveWishCount()
    {
        return _activeWishes.Count;
    }

    /// <summary>
    /// Returns a read-only view of all active wishes (citizenName -> WishTemplate).
    /// </summary>
    public IReadOnlyDictionary<string, WishTemplate> GetActiveWishes()
    {
        return _activeWishes;
    }

    /// <summary>
    /// Returns true if at least one instance of the given room type is placed on the ring.
    /// </summary>
    public bool IsRoomTypeAvailable(string roomId)
    {
        return _placedRoomTypes.ContainsKey(roomId) && _placedRoomTypes[roomId] > 0;
    }

    /// <summary>
    /// Returns the wish template with the given ID, or null if not found.
    /// Used by CitizenNode.SetWishFromSave() to restore active wishes from save data.
    /// </summary>
    public WishTemplate GetTemplateById(string wishId)
    {
        return _allTemplates.FirstOrDefault(t => t.WishId == wishId);
    }

    /// <summary>
    /// Returns a random wish template from all loaded templates.
    /// </summary>
    public WishTemplate GetRandomTemplate()
    {
        if (_allTemplates.Count == 0) return null;
        int index = (int)(GD.Randi() % (uint)_allTemplates.Count);
        return _allTemplates[index];
    }

    /// <summary>
    /// Returns a random wish template from the specified category.
    /// </summary>
    public WishTemplate GetRandomTemplateForCategory(WishTemplate.WishCategory category)
    {
        if (!_templatesByCategory.TryGetValue(category, out var templates) || templates.Count == 0)
            return null;
        int index = (int)(GD.Randi() % (uint)templates.Count);
        return templates[index];
    }

    // -------------------------------------------------------------------------
    // Save/Load API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns a copy of the placed room type counts (for save).
    /// </summary>
    public Dictionary<string, int> GetPlacedRoomTypeCounts()
    {
        return new Dictionary<string, int>(_placedRoomTypes);
    }

    /// <summary>
    /// Restores active wishes from save data. Clears current tracking,
    /// then re-populates from the citizenName -> wishId mapping.
    /// </summary>
    public void RestoreActiveWishes(Dictionary<string, string> citizenWishes)
    {
        _activeWishes.Clear();
        foreach (var (citizenName, wishId) in citizenWishes)
        {
            var template = GetTemplateById(wishId);
            if (template != null)
            {
                _activeWishes[citizenName] = template;
            }
            else
            {
                GD.PushWarning($"WishBoard: Unknown wish ID '{wishId}' for citizen '{citizenName}' during restore.");
            }
        }
    }

    /// <summary>
    /// Restores placed room type counts from save data. Replaces current tracking.
    /// </summary>
    public void RestorePlacedRoomTypes(Dictionary<string, int> roomTypes)
    {
        _placedRoomTypes.Clear();
        foreach (var (roomId, count) in roomTypes)
        {
            _placedRoomTypes[roomId] = count;
        }
    }
}
