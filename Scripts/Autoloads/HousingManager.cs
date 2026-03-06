using System.Collections.Generic;
using Godot;
using OrbitalRings.Build;
using OrbitalRings.Citizens;
using OrbitalRings.Data;
using OrbitalRings.Ring;

namespace OrbitalRings.Autoloads;

/// <summary>
/// Manages citizen-to-home-room assignment. Owns the mapping of citizens to
/// housing rooms and emits CitizenAssignedHome/CitizenUnhoused events.
///
/// Registered as an Autoload in project.godot (8th singleton, before SaveManager).
/// Access via HousingManager.Instance.
///
/// Phase 14: skeleton only (type + singleton pattern).
/// Phase 15: full assignment logic.
/// Phase 16: capacity transfer from HappinessManager.
/// </summary>
public partial class HousingManager : Node
{
    /// <summary>Singleton instance, set in _EnterTree().</summary>
    public static HousingManager Instance { get; private set; }

    /// <summary>HousingConfig resource -- set via Inspector or loaded from default path.</summary>
    [Export] public HousingConfig Config { get; set; }

    /// <summary>
    /// When true, _Ready() skips InitializeExistingRooms. Set by SaveManager before
    /// scene transition so loaded state is not overwritten by default initialization.
    /// </summary>
    public static bool StateLoaded { get; set; }

    // -------------------------------------------------------------------------
    // Data structures
    // -------------------------------------------------------------------------

    /// <summary>
    /// Maps anchor index to housing room capacity. Pre-cached so capacity is
    /// available even after room is demolished (same pattern as HappinessManager).
    /// </summary>
    private readonly Dictionary<int, int> _housingRoomCapacities = new();

    /// <summary>Maps anchor index to list of citizen names assigned to that room.</summary>
    private readonly Dictionary<int, List<string>> _roomOccupants = new();

    /// <summary>Reverse lookup: maps citizen name to home anchor index.</summary>
    private readonly Dictionary<string, int> _citizenHomes = new();

    /// <summary>Suppresses event emission during save restoration to prevent autosave loops.</summary>
    private bool _isRestoring;

    // -------------------------------------------------------------------------
    // Delegate references (for clean unsubscription)
    // -------------------------------------------------------------------------

    private System.Action<string, int> _onRoomPlaced;
    private System.Action<int> _onRoomDemolished;
    private System.Action<string> _onCitizenArrived;

    // -------------------------------------------------------------------------
    // Public properties
    // -------------------------------------------------------------------------

    /// <summary>Total housing capacity across all placed housing rooms.</summary>
    public int TotalCapacity
    {
        get
        {
            int total = 0;
            foreach (var cap in _housingRoomCapacities.Values)
                total += cap;
            return total;
        }
    }

    /// <summary>Total number of currently housed citizens.</summary>
    public int TotalHoused => _citizenHomes.Count;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        // Script autoloads don't populate [Export] properties from the inspector,
        // so initialize Config with defaults if not already set.
        Config ??= new HousingConfig();

        // Subscribe to events using stored delegate references for clean unsubscription
        if (GameEvents.Instance != null)
        {
            _onRoomPlaced = OnRoomPlaced;
            _onRoomDemolished = OnRoomDemolished;
            _onCitizenArrived = OnCitizenArrived;

            GameEvents.Instance.RoomPlaced += _onRoomPlaced;
            GameEvents.Instance.RoomDemolished += _onRoomDemolished;
            GameEvents.Instance.CitizenArrived += _onCitizenArrived;
        }

        // Scan for any pre-placed housing rooms at startup
        // (skipped when loading from save -- SaveManager restores state directly)
        if (!StateLoaded)
            InitializeExistingRooms();
    }

    public override void _ExitTree()
    {
        if (GameEvents.Instance != null)
        {
            if (_onRoomPlaced != null)
                GameEvents.Instance.RoomPlaced -= _onRoomPlaced;
            if (_onRoomDemolished != null)
                GameEvents.Instance.RoomDemolished -= _onRoomDemolished;
            if (_onCitizenArrived != null)
                GameEvents.Instance.CitizenArrived -= _onCitizenArrived;
        }

        Instance = null;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Computes housing capacity for a room: BaseCapacity + (segmentCount - 1).
    /// This is the SOLE authority for capacity calculation.
    /// </summary>
    public static int ComputeCapacity(RoomDefinition definition, int segmentCount)
    {
        return definition.BaseCapacity + (segmentCount - 1);
    }

    /// <summary>
    /// Returns the anchor index of the citizen's home, or null if unhoused.
    /// Used by SaveManager and future UI.
    /// </summary>
    public int? GetHomeForCitizen(string citizenName)
    {
        if (_citizenHomes.TryGetValue(citizenName, out int anchorIndex))
            return anchorIndex;
        return null;
    }

    /// <summary>
    /// Returns the number of citizens assigned to the room at anchorIndex.
    /// </summary>
    public int GetOccupantCount(int anchorIndex)
    {
        if (_roomOccupants.TryGetValue(anchorIndex, out var list))
            return list.Count;
        return 0;
    }

    /// <summary>
    /// Returns the list of citizen names assigned to a room. Used by future tooltip UI.
    /// </summary>
    public IReadOnlyList<string> GetOccupants(int anchorIndex)
    {
        if (_roomOccupants.TryGetValue(anchorIndex, out var list))
            return list;
        return System.Array.Empty<string>();
    }

    /// <summary>
    /// Rebuilds internal state from save data without emitting events.
    /// Called by SaveManager after rooms and citizens are restored.
    ///
    /// Three verified code paths converge here (audited Phase 19):
    /// 1. Normal save/load: each citizen's homeIndex maps to a restored room -> AssignCitizen.
    /// 2. v2 backward compat: homeIndex is null (field absent in JSON) -> skip -> AssignAllUnhoused.
    /// 3. Stale reference: homeIndex points to a demolished room -> ContainsKey fails -> skip + log -> AssignAllUnhoused.
    /// </summary>
    public void RestoreFromSave(IReadOnlyList<(string citizenName, int? homeIndex)> assignments)
    {
        // Populate capacity dictionary from already-restored BuildManager rooms.
        // During save/load, neither _Ready().InitializeExistingRooms (skipped: StateLoaded=true)
        // nor OnRoomPlaced events (not emitted by RestorePlacedRoom) populate this dictionary.
        // Pass assignCitizens: false — the save data loop below handles assignments.
        InitializeExistingRooms(assignCitizens: false);

        _isRestoring = true;

        foreach (var (citizenName, homeIndex) in assignments)
        {
            if (homeIndex == null || homeIndex.Value < 0) continue;

            // Only assign if the room still exists (stale references become unhoused)
            if (!_housingRoomCapacities.ContainsKey(homeIndex.Value))
            {
                GD.Print($"Housing: Stale home reference for {citizenName} at segment {homeIndex.Value} -- citizen is unhoused");
                continue;
            }

            AssignCitizen(citizenName, homeIndex.Value);
        }

        // After restore, attempt to assign any remaining unhoused citizens
        AssignAllUnhoused();

        _isRestoring = false;

        GameEvents.Instance?.EmitHousingStateChanged();
    }

    // -------------------------------------------------------------------------
    // Event handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called when a room is placed. If it's a Housing-category room,
    /// caches capacity and assigns unhoused citizens oldest-first.
    /// </summary>
    private void OnRoomPlaced(string roomType, int segmentIndex)
    {
        var roomInfo = BuildManager.Instance?.GetPlacedRoom(segmentIndex);
        if (roomInfo == null) return;

        var def = roomInfo.Value.Definition;
        if (def.Category != RoomDefinition.RoomCategory.Housing) return;

        int segmentCount = roomInfo.Value.SegmentCount;
        int capacity = ComputeCapacity(def, segmentCount);
        int anchorIndex = roomInfo.Value.AnchorIndex;

        _housingRoomCapacities[anchorIndex] = capacity;
        _roomOccupants[anchorIndex] = new List<string>();

        GD.Print($"Housing: Room placed at segment {anchorIndex} with capacity {capacity} (base={def.BaseCapacity}, segments={segmentCount})");

        // Assign unhoused citizens oldest-first up to capacity
        AssignUnhousedCitizens(anchorIndex);
    }

    /// <summary>
    /// Called when a room is demolished. Uses pre-cached dictionary since the room
    /// is already gone by the time this event fires (same pitfall as HappinessManager).
    /// Displaced citizens are reassigned oldest-first or remain unhoused.
    /// </summary>
    private void OnRoomDemolished(int segmentIndex)
    {
        // Look up by anchor index from our capacity dictionary
        // The segmentIndex from the event IS the anchor index (BuildManager emits anchor)
        if (!_housingRoomCapacities.ContainsKey(segmentIndex)) return;

        int anchorIndex = segmentIndex;

        // Collect displaced citizens before removing room data
        var displaced = new List<string>();
        if (_roomOccupants.TryGetValue(anchorIndex, out var occupants))
        {
            displaced.AddRange(occupants);
        }

        // Remove room from tracking
        _housingRoomCapacities.Remove(anchorIndex);
        _roomOccupants.Remove(anchorIndex);

        GD.Print($"Housing: Room demolished at segment {anchorIndex}, displacing {displaced.Count} citizens");

        // Displace each citizen (oldest-first ordering from original list order)
        foreach (var citizenName in displaced)
        {
            _citizenHomes.Remove(citizenName);

            // Set CitizenNode.HomeSegmentIndex to null
            var citizenNode = FindCitizenNode(citizenName);
            if (citizenNode != null)
                citizenNode.HomeSegmentIndex = null;

            if (!_isRestoring)
            {
                GameEvents.Instance?.EmitCitizenUnhoused(citizenName);
                GD.Print($"Housing: {citizenName} displaced from demolished room");
            }

            // Attempt reassignment to another room
            int bestRoom = FindBestRoom();
            if (bestRoom >= 0)
            {
                AssignCitizen(citizenName, bestRoom);
            }
        }
    }

    /// <summary>
    /// Called when a new citizen arrives. Attempts to assign them to the room
    /// with fewest occupants.
    /// </summary>
    private void OnCitizenArrived(string citizenName)
    {
        int bestRoom = FindBestRoom();
        if (bestRoom >= 0)
        {
            AssignCitizen(citizenName, bestRoom);
        }
        else
        {
            GD.Print($"Housing: {citizenName} arrived but no housing available -- citizen is unhoused");
        }
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Finds the housing room with fewest occupants that has vacancy.
    /// Ties broken randomly using reservoir sampling.
    /// Returns anchor index, or -1 if all rooms are full or no rooms exist.
    /// </summary>
    private int FindBestRoom()
    {
        int bestAnchor = -1;
        int minOccupants = int.MaxValue;
        int tieCount = 0;

        foreach (var (anchor, capacity) in _housingRoomCapacities)
        {
            int occupants = _roomOccupants.TryGetValue(anchor, out var list) ? list.Count : 0;
            if (occupants >= capacity) continue; // full

            if (occupants < minOccupants)
            {
                minOccupants = occupants;
                bestAnchor = anchor;
                tieCount = 1;
            }
            else if (occupants == minOccupants)
            {
                tieCount++;
                // Reservoir sampling for random tiebreak
                if (GD.Randi() % tieCount == 0)
                    bestAnchor = anchor;
            }
        }

        return bestAnchor;
    }

    /// <summary>
    /// Assigns a citizen to a housing room. Updates all tracking structures,
    /// sets CitizenNode.HomeSegmentIndex, and emits events (unless restoring).
    /// </summary>
    private void AssignCitizen(string citizenName, int anchorIndex)
    {
        // Add to room occupants
        if (!_roomOccupants.ContainsKey(anchorIndex))
            _roomOccupants[anchorIndex] = new List<string>();
        _roomOccupants[anchorIndex].Add(citizenName);

        // Set reverse lookup
        _citizenHomes[citizenName] = anchorIndex;

        // Set CitizenNode.HomeSegmentIndex
        var citizenNode = FindCitizenNode(citizenName);
        if (citizenNode != null)
            citizenNode.HomeSegmentIndex = anchorIndex;

        if (!_isRestoring)
        {
            GameEvents.Instance?.EmitCitizenAssignedHome(citizenName, anchorIndex);
            GD.Print($"Housing: {citizenName} assigned to room at segment {anchorIndex}");
        }
    }

    /// <summary>
    /// Assigns unhoused citizens to a specific room, oldest-first (list order).
    /// Stops when room is full.
    /// </summary>
    private void AssignUnhousedCitizens(int anchorIndex)
    {
        if (CitizenManager.Instance == null) return;
        if (!_housingRoomCapacities.TryGetValue(anchorIndex, out int capacity)) return;

        foreach (var citizen in CitizenManager.Instance.Citizens)
        {
            string citizenName = citizen.Data.CitizenName;

            // Skip already-housed citizens
            if (_citizenHomes.ContainsKey(citizenName)) continue;

            // Check room capacity
            if (GetOccupantCount(anchorIndex) >= capacity) break;

            AssignCitizen(citizenName, anchorIndex);
        }
    }

    /// <summary>
    /// Attempts to assign all unhoused citizens to any available rooms, oldest-first.
    /// Used after save restoration.
    /// </summary>
    private void AssignAllUnhoused()
    {
        if (CitizenManager.Instance == null) return;

        foreach (var citizen in CitizenManager.Instance.Citizens)
        {
            string citizenName = citizen.Data.CitizenName;

            // Skip already-housed citizens
            if (_citizenHomes.ContainsKey(citizenName)) continue;

            int bestRoom = FindBestRoom();
            if (bestRoom < 0) break; // no more rooms available

            AssignCitizen(citizenName, bestRoom);
        }
    }

    /// <summary>
    /// Scans all 24 segment indices for pre-placed housing rooms.
    /// Populates capacity and occupant tracking, then optionally assigns existing unhoused citizens.
    /// </summary>
    /// <param name="assignCitizens">
    /// When false, only populates room capacities without assigning citizens.
    /// Used by RestoreFromSave which handles assignments from save data separately.
    /// </param>
    private void InitializeExistingRooms(bool assignCitizens = true)
    {
        if (BuildManager.Instance == null) return;

        for (int i = 0; i < SegmentGrid.TotalSegments; i++)
        {
            var roomInfo = BuildManager.Instance.GetPlacedRoom(i);
            if (roomInfo == null) continue;

            var def = roomInfo.Value.Definition;
            int anchorIndex = roomInfo.Value.AnchorIndex;

            // Only count each room once (by anchor index) and only Housing category
            if (def.Category == RoomDefinition.RoomCategory.Housing
                && !_housingRoomCapacities.ContainsKey(anchorIndex))
            {
                int segmentCount = roomInfo.Value.SegmentCount;
                int capacity = ComputeCapacity(def, segmentCount);
                _housingRoomCapacities[anchorIndex] = capacity;
                _roomOccupants[anchorIndex] = new List<string>();
            }
        }

        // Attempt to assign any existing unhoused citizens to discovered rooms
        if (assignCitizens && _housingRoomCapacities.Count > 0)
        {
            AssignAllUnhoused();
        }
    }

    /// <summary>
    /// Finds a CitizenNode by name from CitizenManager's list.
    /// Returns null if not found.
    /// </summary>
    private static CitizenNode FindCitizenNode(string citizenName)
    {
        if (CitizenManager.Instance == null) return null;

        foreach (var citizen in CitizenManager.Instance.Citizens)
        {
            if (citizen.Data.CitizenName == citizenName)
                return citizen;
        }

        return null;
    }
}
