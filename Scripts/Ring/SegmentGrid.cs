using Godot;

namespace OrbitalRings.Ring;

/// <summary>
/// Enum identifying which row of the ring a segment belongs to.
/// </summary>
public enum SegmentRow { Outer, Inner }

/// <summary>
/// Pure C# data model for the 24-slot ring segment grid.
/// Tracks occupancy state and provides polar math constants
/// matching the locked ring proportions (outer=6, inner=3, equal thirds).
///
/// Layout (radial, from center outward):
///   Inner row:  3.0 - 4.0  (12 segments)
///   Walkway:    4.0 - 5.0  (continuous strip)
///   Outer row:  5.0 - 6.0  (12 segments)
///
/// Flat index mapping:
///   0-11  = Outer row positions 0-11
///   12-23 = Inner row positions 0-11
/// </summary>
public class SegmentGrid
{
    public const int SegmentsPerRow = 12;
    public const int TotalSegments = SegmentsPerRow * 2;

    public const float OuterRadius = 6.0f;
    public const float InnerRadius = 3.0f;
    public const float OuterRowInner = 5.0f;
    public const float InnerRowOuter = 4.0f;
    public const float RingHeight = 0.3f;
    public const float WalkwayRecess = 0.025f;

    /// <summary>Segment arc in radians (30 degrees).</summary>
    public const float SegmentArc = Mathf.Tau / SegmentsPerRow;

    private readonly bool[] _occupied = new bool[TotalSegments];

    /// <summary>Check if a segment is occupied by a room.</summary>
    public bool IsOccupied(SegmentRow row, int position)
    {
        return _occupied[ToIndex(row, position)];
    }

    /// <summary>Set the occupancy state of a segment.</summary>
    public void SetOccupied(SegmentRow row, int position, bool occupied)
    {
        _occupied[ToIndex(row, position)] = occupied;
    }

    /// <summary>
    /// Converts row + clock position (0-11) to flat array index.
    /// Outer segments: 0-11, Inner segments: 12-23.
    /// </summary>
    public static int ToIndex(SegmentRow row, int position)
    {
        return row == SegmentRow.Outer ? position : SegmentsPerRow + position;
    }

    /// <summary>
    /// Converts flat array index back to row + position.
    /// </summary>
    public static (SegmentRow row, int position) FromIndex(int index)
    {
        return index < SegmentsPerRow
            ? (SegmentRow.Outer, index)
            : (SegmentRow.Inner, index - SegmentsPerRow);
    }

    /// <summary>
    /// Returns the start angle in radians for a given clock position (0-11).
    /// Position 0 starts at angle 0 (positive X axis direction).
    /// </summary>
    public static float GetStartAngle(int position)
    {
        return position * SegmentArc;
    }

    /// <summary>
    /// Returns the display label for a segment: "Outer 1 -- Empty" through "Inner 12 -- Occupied".
    /// Clock positions are 1-based for display (1-12), 0-based internally (0-11).
    /// </summary>
    public string GetLabel(SegmentRow row, int position)
    {
        string rowName = row == SegmentRow.Outer ? "Outer" : "Inner";
        int clockPos = position + 1;
        string status = IsOccupied(row, position) ? "Occupied" : "Empty";
        return $"{rowName} {clockPos} -- {status}";
    }

    /// <summary>
    /// Returns inner and outer radius for a given row.
    /// Outer row: 5.0 - 6.0, Inner row: 3.0 - 4.0.
    /// </summary>
    public static (float inner, float outer) GetRowRadii(SegmentRow row)
    {
        return row == SegmentRow.Outer
            ? (OuterRowInner, OuterRadius)
            : (InnerRadius, InnerRowOuter);
    }

    // -------------------------------------------------------------------------
    // Circular Adjacency Helpers (Phase 4)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Normalizes a position to the valid 0-11 range, handling negative wrap-around.
    /// E.g., -1 becomes 11, 12 becomes 0.
    /// </summary>
    public static int WrapPosition(int position)
    {
        return ((position % SegmentsPerRow) + SegmentsPerRow) % SegmentsPerRow;
    }

    /// <summary>
    /// Returns true if two clock positions are adjacent, accounting for circular wrap.
    /// Position 0 is adjacent to position 11.
    /// </summary>
    public static bool AreAdjacent(int posA, int posB)
    {
        int delta = System.Math.Abs(posA - posB);
        return delta == 1 || delta == SegmentsPerRow - 1;
    }

    /// <summary>
    /// Checks if <paramref name="count"/> consecutive segments starting from
    /// <paramref name="startPos"/> (wrapping around) are all unoccupied.
    /// </summary>
    public bool AreSegmentsFree(SegmentRow row, int startPos, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int pos = WrapPosition(startPos + i);
            if (IsOccupied(row, pos))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Sets occupancy for <paramref name="count"/> consecutive segments starting from
    /// <paramref name="startPos"/> (wrapping around).
    /// </summary>
    public void SetSegmentsOccupied(SegmentRow row, int startPos, int count, bool occupied)
    {
        for (int i = 0; i < count; i++)
        {
            int pos = WrapPosition(startPos + i);
            SetOccupied(row, pos, occupied);
        }
    }
}
