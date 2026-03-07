namespace OrbitalRings.Data;

/// <summary>
/// Station day cycle periods. Values are ordered chronologically
/// so arithmetic wrapping (current + 1) % 4 gives next period.
/// </summary>
public enum StationPeriod
{
    Morning = 0,
    Day = 1,
    Evening = 2,
    Night = 3,
}
