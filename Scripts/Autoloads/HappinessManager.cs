using System.Collections.Generic;
using Godot;
using OrbitalRings.Citizens;
using OrbitalRings.Data;
using OrbitalRings.Happiness;
using OrbitalRings.UI;

namespace OrbitalRings.Autoloads;

/// <summary>
/// Core progression engine: tracks station lifetime wishes, owns MoodSystem for
/// fluctuating mood state, gates citizen arrivals (querying HousingManager for
/// housing capacity), and unlocks blueprints at wish-count milestones.
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

    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    /// <summary>Interval in seconds between citizen arrival probability checks.</summary>
    private const float ArrivalCheckInterval = 60.0f;

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

    /// <summary>Current mood baseline (0.0–BaselineCap). Rises with lifetime wish count.</summary>
    public float MoodBaseline => _moodSystem?.Baseline ?? 0f;

    /// <summary>Current mood tier with hysteresis applied.</summary>
    public MoodTier CurrentTier => _moodSystem?.CurrentTier ?? MoodTier.Quiet;

    /// <summary>
    /// Returns whether the given room ID is unlocked for building.
    /// Starter rooms are always unlocked; milestone rooms unlock at thresholds.
    /// </summary>
    public bool IsRoomUnlocked(string roomId) => _unlockedRooms.Contains(roomId);

    // -------------------------------------------------------------------------
    // Test Infrastructure
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns this singleton to a clean "just loaded, no game data" state.
    /// Recreates MoodSystem fresh, resets unlocked rooms to starter set, stops
    /// the arrival timer. Does NOT touch Instance, Config, or _arrivalCanvasLayer.
    /// Called by TestHelper.ResetAllSingletons() between tests.
    /// </summary>
    public void Reset()
    {
        _lifetimeHappiness = 0;
        _moodSystem = new MoodSystem(Config ?? new HappinessConfig());
        _lastReportedTier = MoodTier.Quiet;
        _unlockedRooms.Clear();
        _unlockedRooms.UnionWith(new[]
        {
            "bunk_pod", "air_recycler", "workshop",
            "reading_nook", "storage_bay", "garden_nook"
        });
        _crossedMilestoneCount = 0;
        _arrivalTimer?.Stop();
    }

    /// <summary>
    /// Re-subscribes to GameEvents after ClearAllSubscribers(). Called by
    /// TestHelper for integration tests.
    /// </summary>
    public void SubscribeToEvents()
    {
        if (GameEvents.Instance == null) return;

        GameEvents.Instance.WishFulfilled += OnWishFulfilled;
    }

    // -------------------------------------------------------------------------
    // Save/Load API
    // -------------------------------------------------------------------------

    /// <summary>Returns a copy of the currently unlocked room IDs (for save).</summary>
    public HashSet<string> GetUnlockedRoomIds() => new(_unlockedRooms);

    /// <summary>Returns the number of milestones crossed (for save).</summary>
    public int GetCrossedMilestoneCount() => _crossedMilestoneCount;

    /// <summary>
    /// Restores all happiness/progression state from save data.
    /// v2 saves pass all three happiness values; v1 backward-compat passes
    /// (0, oldHappiness, 0f) so mood starts from the old float and lifetime/baseline reset.
    /// </summary>
    public void RestoreState(int lifetimeHappiness, float mood, float moodBaseline,
        HashSet<string> unlockedRooms, int milestoneCount)
    {
        _lifetimeHappiness = lifetimeHappiness;
        _moodSystem?.RestoreState(mood, moodBaseline);
        _lastReportedTier = _moodSystem?.CurrentTier ?? MoodTier.Quiet;

        _unlockedRooms.Clear();
        foreach (var roomId in unlockedRooms)
            _unlockedRooms.Add(roomId);

        _crossedMilestoneCount = milestoneCount;

        EconomyManager.Instance?.SetMoodTier(_lastReportedTier);
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
            EconomyManager.Instance?.SetMoodTier(newTier);
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
            EconomyManager.Instance?.SetMoodTier(newTier);
        }

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
    /// Periodic check (~60s): roll a probability based on the current mood tier.
    /// If successful and there is room for a new citizen, spawn one with fade-in
    /// animation and floating arrival text.
    /// If population is below StarterCitizenCapacity (5), arrival is allowed
    /// regardless of housing. Otherwise, arrival requires at least one unoccupied
    /// housing bed (TotalHoused &lt; TotalCapacity).
    /// Quiet tier always gives 0.15 probability — no early-return guard needed.
    /// </summary>
    private void OnArrivalCheck()
    {
        int currentPop = CitizenManager.Instance?.CitizenCount ?? 0;
        int housingCapacity = HousingManager.Instance?.TotalCapacity ?? 0;
        int housed = HousingManager.Instance?.TotalHoused ?? 0;
        bool belowStarterCap = currentPop < StarterCitizenCapacity;
        bool hasFreeBed = housingCapacity > 0 && housed < housingCapacity;

        if (!belowStarterCap && !hasFreeBed) return;

        float chance = ArrivalProbabilityForTier(_lastReportedTier);
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
    /// Maps the current mood tier to the configured arrival probability.
    /// Probability is used once per 60s timer tick — only the value changes with tier,
    /// not the timer interval (locked decision from CONTEXT.md).
    /// </summary>
    private float ArrivalProbabilityForTier(MoodTier tier) => tier switch
    {
        MoodTier.Quiet => Config.ArrivalProbabilityQuiet,
        MoodTier.Cozy => Config.ArrivalProbabilityCozy,
        MoodTier.Lively => Config.ArrivalProbabilityLively,
        MoodTier.Vibrant => Config.ArrivalProbabilityVibrant,
        MoodTier.Radiant => Config.ArrivalProbabilityRadiant,
        _ => Config.ArrivalProbabilityQuiet,
    };

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

}
