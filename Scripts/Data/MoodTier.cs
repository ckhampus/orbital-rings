namespace OrbitalRings.Data;

/// <summary>
/// Named mood tiers for station atmosphere. Values are ordered from lowest to highest
/// so arithmetic promotion/demotion (current + 1, current - 1) is valid.
/// </summary>
public enum MoodTier
{
    Quiet   = 0,
    Cozy    = 1,
    Lively  = 2,
    Vibrant = 3,
    Radiant = 4,
}
