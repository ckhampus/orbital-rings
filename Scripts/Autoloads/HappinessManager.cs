using System.Collections.Generic;
using Godot;
using OrbitalRings.Citizens;
using OrbitalRings.Data;
using OrbitalRings.UI;

namespace OrbitalRings.Autoloads;

/// <summary>
/// Core progression engine: tracks station happiness, gates citizen arrivals
/// behind housing capacity, and unlocks blueprints at milestone thresholds.
///
/// Registered as an Autoload in project.godot (6th, after all other singletons).
/// Access via HappinessManager.Instance.
///
/// Happiness only goes up (cozy promise). Diminishing returns formula:
///   gain = HappinessGainBase / (1 + currentHappiness)
///
/// Calibration (base=0.08): 25% unlock ~wish 4, 60% unlock ~wish 12,
/// 100% reached asymptotically around ~50 wishes.
/// </summary>
public partial class HappinessManager : Node
{
    /// <summary>
    /// Singleton instance. Set in _Ready(). Guaranteed non-null after Autoloads initialize.
    /// </summary>
    public static HappinessManager Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    /// <summary>
    /// Base happiness gain per wish fulfillment. Divided by (1 + currentHappiness)
    /// for diminishing returns. Calibrated: 25% at ~wish 4, 60% at ~wish 12.
    /// </summary>
    private const float HappinessGainBase = 0.08f;

    /// <summary>Interval in seconds between citizen arrival probability checks.</summary>
    private const float ArrivalCheckInterval = 60.0f;

    /// <summary>
    /// At 100% happiness, this is the probability of a new citizen per check.
    /// P(arrival) = happiness * ArrivalProbabilityScale.
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
    /// Blueprint unlock thresholds: (happiness threshold, room IDs to unlock).
    /// Locked decisions from CONTEXT.md.
    /// </summary>
    private static readonly (float threshold, string[] rooms)[] UnlockMilestones =
    {
        (0.25f, new[] { "sky_loft", "craft_lab" }),
        (0.60f, new[] { "star_lounge", "comm_relay" }),
    };

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private float _happiness;
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

    /// <summary>Current station happiness (0.0 to 1.0).</summary>
    public float Happiness => _happiness;

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
        InitializeHousingCapacity();

        // Create CanvasLayer for arrival floating text (same layer as HUDLayer)
        _arrivalCanvasLayer = new CanvasLayer { Layer = 5 };
        AddChild(_arrivalCanvasLayer);
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

    // -------------------------------------------------------------------------
    // Wish fulfillment handler
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called when any citizen's wish is fulfilled. Increments happiness with
    /// diminishing returns, updates economy multiplier, and checks unlock milestones.
    /// </summary>
    private void OnWishFulfilled(string citizenName, string wishType)
    {
        // Already at max -- wishes still fulfill but happiness stays at 100%
        if (_happiness >= 1.0f) return;

        // Diminishing returns: early wishes grant more, later wishes grant less
        float gain = HappinessGainBase / (1.0f + _happiness);
        _happiness = Mathf.Min(_happiness + gain, 1.0f);

        // Update economy multiplier (capped at 1.3x internally by EconomyManager)
        EconomyManager.Instance?.SetHappiness(_happiness);

        // Fire event for UI (happiness bar, floating text)
        GameEvents.Instance?.EmitHappinessChanged(_happiness);

        // Check if any unlock thresholds were crossed
        CheckUnlockMilestones();
    }

    // -------------------------------------------------------------------------
    // Unlock milestones
    // -------------------------------------------------------------------------

    /// <summary>
    /// Checks if happiness has crossed any new unlock thresholds.
    /// Iterates from the last crossed milestone to avoid re-triggering.
    /// </summary>
    private void CheckUnlockMilestones()
    {
        while (_crossedMilestoneCount < UnlockMilestones.Length)
        {
            var (threshold, rooms) = UnlockMilestones[_crossedMilestoneCount];
            if (_happiness < threshold) break;

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
    /// Periodic check (~60s): roll a probability based on happiness.
    /// If successful and population is below housing capacity, spawn a citizen
    /// with fade-in animation and floating arrival text.
    /// </summary>
    private void OnArrivalCheck()
    {
        if (_happiness <= 0f) return;

        int currentPop = CitizenManager.Instance?.CitizenCount ?? 0;
        if (currentPop >= _housingCapacity) return;

        // P(arrival) = happiness * scale (at 100% happiness, 60% chance)
        float chance = _happiness * ArrivalProbabilityScale;
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
