using Godot;

namespace OrbitalRings.Data;

/// <summary>
/// Inspector-tunable configuration for the station day/night cycle.
/// Period proportions use weights: each period's duration is
/// (its weight / total weight) * TotalCycleDuration.
/// Equal weights (all 1.0) produce four equal periods.
///
/// Default values: 480s (8 minutes) total cycle, equal period weights.
/// </summary>
[GlobalClass]
public partial class ClockConfig : Resource
{
    [ExportGroup("Cycle Timing")]

    /// <summary>Total cycle duration in seconds. Default 480 (8 minutes).</summary>
    [Export] public float TotalCycleDuration { get; set; } = 480.0f;

    [ExportGroup("Period Proportions")]

    /// <summary>Morning share weight. Default 1.0 (equal share).</summary>
    [Export] public float MorningWeight { get; set; } = 1.0f;

    /// <summary>Day share weight. Default 1.0 (equal share).</summary>
    [Export] public float DayWeight { get; set; } = 1.0f;

    /// <summary>Evening share weight. Default 1.0 (equal share).</summary>
    [Export] public float EveningWeight { get; set; } = 1.0f;

    /// <summary>Night share weight. Default 1.0 (equal share).</summary>
    [Export] public float NightWeight { get; set; } = 1.0f;
}
