using Godot;

namespace OrbitalRings.Data;

/// <summary>
/// Inspector-tunable configuration for the housing return-home system.
/// All timing parameters live here for easy iteration without touching behavior code.
///
/// Default values calibrated from the PRD (docs/prd-housing.md):
/// home return cycle 90-150s, rest duration 8-15s.
/// </summary>
[GlobalClass]
public partial class HousingConfig : Resource
{
    [ExportGroup("Home Return Timing")]

    /// <summary>Minimum seconds between home-return trips. Default 90s (PRD lower bound).</summary>
    [Export] public float HomeTimerMin { get; set; } = 90.0f;

    /// <summary>Maximum seconds between home-return trips. Default 150s (PRD upper bound).</summary>
    [Export] public float HomeTimerMax { get; set; } = 150.0f;

    /// <summary>Minimum seconds a citizen rests at home. Default 8s (PRD lower bound).</summary>
    [Export] public float RestDurationMin { get; set; } = 8.0f;

    /// <summary>Maximum seconds a citizen rests at home. Default 15s (PRD upper bound).</summary>
    [Export] public float RestDurationMax { get; set; } = 15.0f;
}
