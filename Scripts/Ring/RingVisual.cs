using Godot;

namespace OrbitalRings.Ring;

/// <summary>
/// Visual state for segment highlight feedback.
/// </summary>
public enum SegmentVisualState { Normal, Hovered, Selected }

/// <summary>
/// Node3D that owns 24 MeshInstance3D segment children (12 outer, 12 inner)
/// plus a single walkway annulus mesh. Built programmatically in _Ready().
///
/// Each segment has three pre-created StandardMaterial3D instances (base, hover, selected)
/// enabling instant material swaps without allocation during gameplay.
///
/// This is NOT a SafeNode -- it is a visual builder, not a signal consumer.
/// </summary>
public partial class RingVisual : Node3D
{
    private readonly MeshInstance3D[] _segmentMeshes = new MeshInstance3D[SegmentGrid.TotalSegments];
    private readonly StandardMaterial3D[] _baseMaterials = new StandardMaterial3D[SegmentGrid.TotalSegments];
    private readonly StandardMaterial3D[] _hoverMaterials = new StandardMaterial3D[SegmentGrid.TotalSegments];
    private readonly StandardMaterial3D[] _selectedMaterials = new StandardMaterial3D[SegmentGrid.TotalSegments];
    private readonly SegmentGrid _grid = new();

    /// <summary>Public read access for interaction system.</summary>
    public SegmentGrid Grid => _grid;

    public override void _Ready()
    {
        BuildSegments();
        BuildWalkway();
    }

    /// <summary>
    /// Builds 24 individual segment MeshInstance3D children with pre-allocated materials.
    /// Outer segments (0-11) and inner segments (12-23).
    /// </summary>
    private void BuildSegments()
    {
        for (int row = 0; row < 2; row++)
        {
            SegmentRow segRow = row == 0 ? SegmentRow.Outer : SegmentRow.Inner;
            (float innerR, float outerR) = SegmentGrid.GetRowRadii(segRow);

            for (int pos = 0; pos < SegmentGrid.SegmentsPerRow; pos++)
            {
                float startAngle = SegmentGrid.GetStartAngle(pos);
                float endAngle = startAngle + SegmentGrid.SegmentArc;

                ArrayMesh mesh = RingMeshBuilder.CreateAnnularSector(
                    innerR, outerR, startAngle, endAngle, SegmentGrid.RingHeight);

                int flatIndex = SegmentGrid.ToIndex(segRow, pos);

                Color baseColor = RingColors.GetBaseColor(segRow, pos);

                // Create three material instances for each segment
                var baseMat = new StandardMaterial3D { AlbedoColor = baseColor };
                var hoverMat = new StandardMaterial3D { AlbedoColor = RingColors.Brighten(baseColor) };
                var selectedMat = new StandardMaterial3D { AlbedoColor = RingColors.SelectionHighlight(baseColor) };

                _baseMaterials[flatIndex] = baseMat;
                _hoverMaterials[flatIndex] = hoverMat;
                _selectedMaterials[flatIndex] = selectedMat;

                var meshInstance = new MeshInstance3D
                {
                    Name = $"Segment_{segRow}_{pos}",
                    Mesh = mesh,
                    MaterialOverride = baseMat
                };

                AddChild(meshInstance);
                _segmentMeshes[flatIndex] = meshInstance;
            }
        }
    }

    /// <summary>
    /// Builds a single continuous walkway annulus between the inner and outer rows.
    /// Slightly recessed below the row surfaces to create visual separation.
    /// </summary>
    private void BuildWalkway()
    {
        float walkwayHeight = SegmentGrid.RingHeight - SegmentGrid.WalkwayRecess * 2;

        ArrayMesh walkwayMesh = RingMeshBuilder.CreateAnnularSector(
            SegmentGrid.InnerRowOuter, SegmentGrid.OuterRowInner,
            0, Mathf.Tau,
            walkwayHeight,
            subdivisions: 48);

        var walkwayMat = new StandardMaterial3D { AlbedoColor = RingColors.Walkway };

        var walkwayInstance = new MeshInstance3D
        {
            Name = "Walkway",
            Mesh = walkwayMesh,
            MaterialOverride = walkwayMat,
            Transform = new Transform3D(Basis.Identity, new Vector3(0, -SegmentGrid.WalkwayRecess, 0))
        };

        AddChild(walkwayInstance);
    }

    /// <summary>
    /// Swaps the MaterialOverride on a segment to reflect its visual state.
    /// </summary>
    public void SetSegmentState(int flatIndex, SegmentVisualState state)
    {
        if (flatIndex < 0 || flatIndex >= SegmentGrid.TotalSegments)
            return;

        _segmentMeshes[flatIndex].MaterialOverride = state switch
        {
            SegmentVisualState.Normal => _baseMaterials[flatIndex],
            SegmentVisualState.Hovered => _hoverMaterials[flatIndex],
            SegmentVisualState.Selected => _selectedMaterials[flatIndex],
            _ => _baseMaterials[flatIndex]
        };
    }

    /// <summary>
    /// Returns the MeshInstance3D for a given flat index (for future use).
    /// </summary>
    public MeshInstance3D GetSegmentMesh(int flatIndex)
    {
        return _segmentMeshes[flatIndex];
    }
}
