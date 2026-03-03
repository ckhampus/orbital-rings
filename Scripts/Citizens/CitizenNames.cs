namespace OrbitalRings.Citizens;

/// <summary>
/// Static name pool providing sequential unique names for citizens.
/// Contains 26 diverse real-world names (one per letter A-Z).
/// Names are dispensed sequentially and wrap around when exhausted.
/// </summary>
public static class CitizenNames
{
    private static readonly string[] Names =
    {
        "Aria", "Bodhi", "Celeste", "Davi", "Elara",
        "Fern", "Gael", "Hana", "Idris", "Juno",
        "Kaia", "Lev", "Mira", "Nico", "Orla",
        "Paz", "Quinn", "Remi", "Suki", "Theo",
        "Uma", "Vesper", "Wren", "Xia", "Yael", "Zara"
    };

    private static int _nextIndex = 0;

    /// <summary>
    /// Returns the next name from the pool, advancing the index.
    /// Wraps around to the beginning when all 26 names have been used.
    /// </summary>
    public static string GetNextName()
    {
        string name = Names[_nextIndex];
        _nextIndex = (_nextIndex + 1) % Names.Length;
        return name;
    }
}
