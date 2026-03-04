using Godot;

namespace OrbitalRings.Data;

/// <summary>
/// Inspector-tunable configuration for the dual happiness system (Phase 10).
/// All decay rates, mood gain values, tier thresholds, and hysteresis parameters
/// live here for easy iteration without touching simulation code.
///
/// Default values are calibrated from the happiness balance research
/// (.planning/phases/10-happiness-core-and-mood-tiers/10-RESEARCH.md).
/// </summary>
[GlobalClass]
public partial class HappinessConfig : Resource
{
    [ExportGroup("Mood Decay")]

    /// <summary>
    /// Fractional happiness lost per second from decay. Default 0.003662 gives
    /// approximately 50% happiness loss over 2.5 in-game minutes with no wishes
    /// fulfilled, preventing stagnation without punishing brief lulls.
    /// </summary>
    [Export] public float DecayRate { get; set; } = 0.003662f;

    /// <summary>
    /// Flat happiness added each time a citizen's wish is fulfilled.
    /// Default 0.06 means roughly 17 fulfilled wishes carry happiness from
    /// floor to ceiling, keeping the wish loop feeling rewarding.
    /// </summary>
    [Export] public float MoodGainPerWish { get; set; } = 0.06f;

    [ExportGroup("Baseline")]

    /// <summary>
    /// Coefficient multiplied by sqrt(citizenCount) to derive the passive
    /// happiness baseline. Default 0.016 yields a baseline of ~0.16 at
    /// 100 citizens, preventing happiness collapse in established stations.
    /// </summary>
    [Export] public float BaselineScalingFactor { get; set; } = 0.016f;

    /// <summary>
    /// Upper bound on the passive baseline, regardless of citizen count.
    /// Default 0.20 keeps baseline contribution modest so fulfilled wishes
    /// remain the primary driver of high-tier moods.
    /// </summary>
    [Export] public float BaselineCap { get; set; } = 0.20f;

    [ExportGroup("Tier Thresholds")]

    /// <summary>
    /// Happiness value at or above which the station enters Cozy tier.
    /// Default 0.10 -- the first tier transition is intentionally easy to reach
    /// so new players experience progression quickly.
    /// </summary>
    [Export] public float TierCozyThreshold { get; set; } = 0.10f;

    /// <summary>
    /// Happiness value at or above which the station enters Lively tier.
    /// Default 0.30 -- requires consistent wish fulfillment to sustain.
    /// </summary>
    [Export] public float TierLivelyThreshold { get; set; } = 0.30f;

    /// <summary>
    /// Happiness value at or above which the station enters Vibrant tier.
    /// Default 0.55 -- mid-game milestone, signals a thriving station.
    /// </summary>
    [Export] public float TierVibrantThreshold { get; set; } = 0.55f;

    /// <summary>
    /// Happiness value at or above which the station enters Radiant tier.
    /// Default 0.80 -- top tier, reserved for optimised late-game stations.
    /// </summary>
    [Export] public float TierRadiantThreshold { get; set; } = 0.80f;

    [ExportGroup("Hysteresis")]

    /// <summary>
    /// Width of the dead-band around each tier boundary. When happiness
    /// is falling, the actual demotion threshold is (threshold - HysteresisWidth).
    /// Default 0.05 prevents rapid tier flickering near boundary values.
    /// </summary>
    [Export] public float HysteresisWidth { get; set; } = 0.05f;

    [ExportGroup("Arrival Probability")]

    /// <summary>
    /// Probability of a citizen arriving per 60s check at Quiet tier.
    /// Default 0.15 — approximately 7 minute average wait between arrivals.
    /// Timer interval stays fixed at 60s; only this probability changes with tier.
    /// </summary>
    [Export] public float ArrivalProbabilityQuiet   { get; set; } = 0.15f;

    /// <summary>Arrival probability at Cozy tier. Default 0.25 — ~4 min avg wait.</summary>
    [Export] public float ArrivalProbabilityCozy    { get; set; } = 0.25f;

    /// <summary>Arrival probability at Lively tier. Default 0.40 — ~2.5 min avg wait.</summary>
    [Export] public float ArrivalProbabilityLively  { get; set; } = 0.40f;

    /// <summary>Arrival probability at Vibrant tier. Default 0.60 — ~1.7 min avg wait.</summary>
    [Export] public float ArrivalProbabilityVibrant { get; set; } = 0.60f;

    /// <summary>Arrival probability at Radiant tier. Default 0.75 — ~1.3 min avg wait.</summary>
    [Export] public float ArrivalProbabilityRadiant { get; set; } = 0.75f;
}
