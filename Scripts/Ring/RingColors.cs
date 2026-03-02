using Godot;

namespace OrbitalRings.Ring;

/// <summary>
/// Static color palette for the ring's warm pastel aesthetic.
/// Outer row = soft rose, inner row = soft lavender, walkway = warm beige.
/// Adjacent segments alternate between base and slightly deeper shade.
/// </summary>
public static class RingColors
{
    // Outer row: soft rose alternating shades
    public static readonly Color OuterBase = new(0.91f, 0.78f, 0.80f);
    public static readonly Color OuterAlt = new(0.88f, 0.74f, 0.77f);

    // Inner row: soft lavender alternating shades
    public static readonly Color InnerBase = new(0.82f, 0.78f, 0.91f);
    public static readonly Color InnerAlt = new(0.78f, 0.74f, 0.88f);

    // Walkway: warm beige (reads as "path")
    public static readonly Color Walkway = new(0.88f, 0.85f, 0.78f);

    /// <summary>
    /// Brightens a color by adding a fixed amount to each channel, clamped to 1.0.
    /// </summary>
    public static Color Brighten(Color c, float factor = 0.15f)
    {
        return new Color(
            Mathf.Min(c.R + factor, 1.0f),
            Mathf.Min(c.G + factor, 1.0f),
            Mathf.Min(c.B + factor, 1.0f)
        );
    }

    /// <summary>
    /// Creates a selection highlight: brighten by 0.25 plus a warm gold accent shift.
    /// </summary>
    public static Color SelectionHighlight(Color c)
    {
        Color brightened = Brighten(c, 0.25f);
        return new Color(
            Mathf.Min(brightened.R + 0.05f, 1.0f),
            Mathf.Min(brightened.G + 0.03f, 1.0f),
            brightened.B
        );
    }

    /// <summary>
    /// Returns the base color for a segment, alternating base/alt by even/odd position.
    /// </summary>
    public static Color GetBaseColor(SegmentRow row, int position)
    {
        bool isEven = position % 2 == 0;
        return row == SegmentRow.Outer
            ? (isEven ? OuterBase : OuterAlt)
            : (isEven ? InnerBase : InnerAlt);
    }
}
