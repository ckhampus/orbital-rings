using Godot;
using OrbitalRings.Data;

namespace OrbitalRings.Autoloads;

/// <summary>
/// Station day/night cycle clock. Accumulates real time via _Process,
/// divides the cycle into four weighted periods (Morning, Day, Evening, Night),
/// and emits PeriodChanged via GameEvents on period boundaries.
///
/// Registered as an Autoload in project.godot -- access via StationClock.Instance.
/// Loads ClockConfig from the default .tres resource or falls back to code defaults.
///
/// Downstream consumers: Phase 27 (lighting), Phase 29 (scheduling), Phase 30 (state machine).
/// </summary>
public partial class StationClock : Node
{
    /// <summary>
    /// Singleton instance, set in _Ready(). Guaranteed non-null after Autoloads initialize.
    /// </summary>
    public static StationClock Instance { get; private set; }

    /// <summary>
    /// Clock configuration loaded from .tres resource. Cycle duration and period
    /// proportions are defined here for Inspector tuning.
    /// </summary>
    [Export] public ClockConfig Config { get; set; }

    /// <summary>Current station period based on elapsed time and config weights.</summary>
    public StationPeriod CurrentPeriod { get; private set; } = StationPeriod.Morning;

    /// <summary>Normalized progress (0.0-1.0) within the current period.</summary>
    public float PeriodProgress { get; private set; }

    /// <summary>
    /// Elapsed time within the current cycle. Read-only for external consumers.
    /// Phase 31 (save/load) will use this to serialize/restore clock position.
    /// </summary>
    public float ElapsedTime => _elapsedTime;

    private float _elapsedTime;

    /// <summary>
    /// Tracks the last period for which PeriodChanged was emitted.
    /// Prevents double-fire on period boundaries (RESEARCH.md pitfall #2).
    /// </summary>
    private StationPeriod _lastEmittedPeriod = StationPeriod.Morning;

    public override void _Ready()
    {
        Instance = this;

        // Load config: prefer Inspector-assigned, then .tres file, then code defaults
        if (Config == null)
            Config = ResourceLoader.Load<ClockConfig>("res://Resources/Clock/default_clock.tres");
        if (Config == null)
        {
            GD.PushWarning("StationClock: No ClockConfig assigned or found at default path. Using code defaults.");
            Config = new ClockConfig();
        }

        // Pause with the game (not during menus/loading)
        ProcessMode = ProcessModeEnum.Pausable;
    }

    public override void _Process(double delta)
    {
        _elapsedTime += (float)delta;

        // Modulo wrap to prevent float drift over long sessions (RESEARCH.md pitfall #1)
        _elapsedTime %= Config.TotalCycleDuration;

        // Compute current period and progress from elapsed time
        CurrentPeriod = ComputePeriod(_elapsedTime, out float progress);
        PeriodProgress = progress;

        // Emit PeriodChanged exactly once per transition (RESEARCH.md pitfall #2)
        if (CurrentPeriod != _lastEmittedPeriod)
        {
            var previous = _lastEmittedPeriod;
            _lastEmittedPeriod = CurrentPeriod;
            GameEvents.Instance?.EmitPeriodChanged(CurrentPeriod, previous);
        }
    }

    /// <summary>
    /// Computes the station period and normalized progress for a given elapsed time.
    /// Uses weighted proportional algorithm: each period's duration is
    /// (its weight / total weight) * TotalCycleDuration.
    /// </summary>
    private StationPeriod ComputePeriod(float elapsedTime, out float periodProgress)
    {
        float totalWeight = Config.MorningWeight + Config.DayWeight
                          + Config.EveningWeight + Config.NightWeight;
        float cycleDuration = Config.TotalCycleDuration;

        // Normalize elapsed time within one cycle
        float t = elapsedTime % cycleDuration;

        // Compute period boundaries as cumulative fractions of cycle
        float[] weights = { Config.MorningWeight, Config.DayWeight,
                            Config.EveningWeight, Config.NightWeight };
        float cumulative = 0f;
        for (int i = 0; i < 4; i++)
        {
            float periodDuration = (weights[i] / totalWeight) * cycleDuration;
            if (t < cumulative + periodDuration)
            {
                periodProgress = (t - cumulative) / periodDuration;
                return (StationPeriod)i;
            }
            cumulative += periodDuration;
        }

        // Edge case: floating point at exact cycle end wraps to Morning
        periodProgress = 0f;
        return StationPeriod.Morning;
    }

    /// <summary>
    /// Resets clock to initial state. Called by TestHelper.ResetAllSingletons()
    /// for test isolation.
    /// </summary>
    public void Reset()
    {
        _elapsedTime = 0f;
        CurrentPeriod = StationPeriod.Morning;
        PeriodProgress = 0f;
        _lastEmittedPeriod = StationPeriod.Morning;
    }

    /// <summary>
    /// Sets elapsed time directly and recomputes period state.
    /// Used by Phase 31 (save/load) to restore clock position,
    /// and by tests for deterministic time positioning.
    /// </summary>
    public void SetElapsedTime(float time)
    {
        _elapsedTime = time % Config.TotalCycleDuration;
        CurrentPeriod = ComputePeriod(_elapsedTime, out float progress);
        PeriodProgress = progress;

        // Update last emitted period to match, preventing spurious event on next _Process
        _lastEmittedPeriod = CurrentPeriod;
    }
}
