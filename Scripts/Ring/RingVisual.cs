using System.Collections.Generic;
using Godot;
using OrbitalRings.Autoloads;

namespace OrbitalRings.Ring;

/// <summary>
/// Visual state for segment highlight feedback.
/// </summary>
public enum SegmentVisualState { Normal, Hovered, Selected, Dimmed }

/// <summary>
/// Node3D that owns 24 MeshInstance3D segment children (12 outer, 12 inner)
/// plus a single walkway annulus mesh. Built programmatically in _Ready().
///
/// Each segment has three pre-created StandardMaterial3D instances (base, hover, selected)
/// enabling instant material swaps without allocation during gameplay.
///
/// Also manages "Zzz" sleep indicators above room segments when citizens are
/// resting at home. Listens to CitizenEnteredHome/CitizenExitedHome events
/// and creates/removes Label3D indicators per segment. This is owned by
/// RingVisual (not CitizenNode) because the ring is always visible -- no
/// visibility propagation or tween binding issues.
/// </summary>
public partial class RingVisual : Node3D
{
    private readonly MeshInstance3D[] _segmentMeshes = new MeshInstance3D[SegmentGrid.TotalSegments];
    private readonly StandardMaterial3D[] _baseMaterials = new StandardMaterial3D[SegmentGrid.TotalSegments];
    private readonly StandardMaterial3D[] _hoverMaterials = new StandardMaterial3D[SegmentGrid.TotalSegments];
    private readonly StandardMaterial3D[] _selectedMaterials = new StandardMaterial3D[SegmentGrid.TotalSegments];
    private readonly StandardMaterial3D[] _dimmedMaterials = new StandardMaterial3D[SegmentGrid.TotalSegments];
    private readonly SegmentGrid _grid = new();

    /// <summary>Public read access for interaction system.</summary>
    public SegmentGrid Grid => _grid;

    // -------------------------------------------------------------------------
    // Zzz sleep indicator state
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tracks the number of citizens currently sleeping in each segment.
    /// When count goes from 0 to 1, create label. When count goes from 1 to 0, remove label.
    /// Multiple citizens can share a home room.
    /// </summary>
    private readonly Dictionary<int, int> _zzzSleepCounts = new();

    /// <summary>Active Zzz Label3D per segment index.</summary>
    private readonly Dictionary<int, Label3D> _zzzLabels = new();

    /// <summary>Active bob tweens per segment index (for cleanup).</summary>
    private readonly Dictionary<int, Tween> _zzzBobTweens = new();

    /// <summary>Active fade-in tweens per segment index (for cleanup on early exit).</summary>
    private readonly Dictionary<int, Tween> _zzzFadeInTweens = new();

    /// <summary>Vertical offset above segment surface for Zzz label.</summary>
    private const float ZzzVerticalOffset = 0.4f;

    // -------------------------------------------------------------------------
    // Stored delegate references for event unsubscription
    // -------------------------------------------------------------------------

    private System.Action<string, int> _onCitizenEnteredHome;
    private System.Action<string, int> _onCitizenExitedHome;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        BuildSegments();
        BuildWalkway();
    }

    public override void _EnterTree()
    {
        if (GameEvents.Instance != null)
        {
            _onCitizenEnteredHome = OnCitizenEnteredHome;
            _onCitizenExitedHome = OnCitizenExitedHome;

            GameEvents.Instance.CitizenEnteredHome += _onCitizenEnteredHome;
            GameEvents.Instance.CitizenExitedHome += _onCitizenExitedHome;
        }
    }

    public override void _ExitTree()
    {
        if (GameEvents.Instance != null)
        {
            if (_onCitizenEnteredHome != null)
                GameEvents.Instance.CitizenEnteredHome -= _onCitizenEnteredHome;
            if (_onCitizenExitedHome != null)
                GameEvents.Instance.CitizenExitedHome -= _onCitizenExitedHome;
        }

        // Clean up all active Zzz labels and tweens
        foreach (var (_, tween) in _zzzBobTweens)
            tween?.Kill();
        _zzzBobTweens.Clear();

        foreach (var (_, tween) in _zzzFadeInTweens)
            tween?.Kill();
        _zzzFadeInTweens.Clear();

        foreach (var (_, label) in _zzzLabels)
            label?.QueueFree();
        _zzzLabels.Clear();

        _zzzSleepCounts.Clear();
    }

    // -------------------------------------------------------------------------
    // Segment building
    // -------------------------------------------------------------------------

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

                // Create four material instances for each segment (base, hover, selected, dimmed)
                var baseMat = new StandardMaterial3D { AlbedoColor = baseColor };
                var hoverMat = new StandardMaterial3D { AlbedoColor = RingColors.Brighten(baseColor) };
                var selectedMat = new StandardMaterial3D { AlbedoColor = RingColors.SelectionHighlight(baseColor) };
                var dimmedMat = new StandardMaterial3D
                {
                    AlbedoColor = new Color(
                        baseColor.R * 0.5f,
                        baseColor.G * 0.5f,
                        baseColor.B * 0.5f,
                        0.6f
                    ),
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha
                };

                _baseMaterials[flatIndex] = baseMat;
                _hoverMaterials[flatIndex] = hoverMat;
                _selectedMaterials[flatIndex] = selectedMat;
                _dimmedMaterials[flatIndex] = dimmedMat;

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

    // -------------------------------------------------------------------------
    // Segment visual state
    // -------------------------------------------------------------------------

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
            SegmentVisualState.Dimmed => _dimmedMaterials[flatIndex],
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

    // -------------------------------------------------------------------------
    // Zzz sleep indicator system
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called when a citizen enters their home room to rest. Creates a Zzz label
    /// above the segment if this is the first sleeper in this room.
    /// </summary>
    private void OnCitizenEnteredHome(string citizenName, int segmentIndex)
    {
        // Increment sleep count for this segment
        if (!_zzzSleepCounts.ContainsKey(segmentIndex))
            _zzzSleepCounts[segmentIndex] = 0;

        _zzzSleepCounts[segmentIndex]++;

        // Only create label when first citizen starts sleeping in this segment
        if (_zzzSleepCounts[segmentIndex] == 1)
        {
            CreateZzzLabel(segmentIndex);
        }
    }

    /// <summary>
    /// Called when a citizen exits their home room after resting. Removes the Zzz
    /// label when no more citizens are sleeping in this segment.
    /// </summary>
    private void OnCitizenExitedHome(string citizenName, int segmentIndex)
    {
        if (!_zzzSleepCounts.ContainsKey(segmentIndex))
            return;

        _zzzSleepCounts[segmentIndex]--;

        // Remove label when last citizen wakes up in this segment
        if (_zzzSleepCounts[segmentIndex] <= 0)
        {
            _zzzSleepCounts.Remove(segmentIndex);
            RemoveZzzLabel(segmentIndex);
        }
    }

    /// <summary>
    /// Creates and animates a "Zzz" Label3D above the given segment.
    /// Positioned at the segment's radial midpoint with a vertical offset.
    /// Fade in over 0.5s, then start a gentle infinite bob animation.
    /// </summary>
    private void CreateZzzLabel(int segmentIndex)
    {
        // Remove any existing label for this segment (safety)
        RemoveZzzLabel(segmentIndex);

        // Compute world position at segment center
        var (row, pos) = SegmentGrid.FromIndex(segmentIndex);
        var (innerR, outerR) = SegmentGrid.GetRowRadii(row);
        float midRadius = (innerR + outerR) * 0.5f;
        float midAngle = SegmentGrid.GetStartAngle(pos) + SegmentGrid.SegmentArc * 0.5f;

        float x = Mathf.Cos(midAngle) * midRadius;
        float z = Mathf.Sin(midAngle) * midRadius;
        float y = SegmentGrid.RingHeight / 2f + ZzzVerticalOffset;

        var label = new Label3D
        {
            Text = "Zzz",
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FontSize = 32,
            OutlineSize = 4,
            Modulate = new Color(0.6f, 0.55f, 0.9f, 0.0f),
            OutlineModulate = new Color(0.3f, 0.25f, 0.5f, 0.0f),
            PixelSize = 0.005f,
            NoDepthTest = true,
            Shaded = false,
            DoubleSided = true,
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector3(x, y, z),
        };

        AddChild(label);
        _zzzLabels[segmentIndex] = label;

        // Fade in over 0.5s, then start bob animation
        var fadeIn = CreateTween();
        _zzzFadeInTweens[segmentIndex] = fadeIn;

        fadeIn.SetParallel(true);
        fadeIn.TweenProperty(label, "modulate:a", 1.0f, 0.5f);
        fadeIn.TweenProperty(label, "outline_modulate:a", 1.0f, 0.5f);
        fadeIn.SetParallel(false);

        // Capture segmentIndex for the callback closure
        int capturedIndex = segmentIndex;
        fadeIn.TweenCallback(Callable.From(() =>
        {
            _zzzFadeInTweens.Remove(capturedIndex);
            StartZzzBob(capturedIndex);
        }));
    }

    /// <summary>
    /// Removes the Zzz label for a segment with a 0.5s fade-out animation.
    /// </summary>
    private void RemoveZzzLabel(int segmentIndex)
    {
        // Kill any active fade-in tween
        if (_zzzFadeInTweens.TryGetValue(segmentIndex, out var fadeInTween))
        {
            fadeInTween?.Kill();
            _zzzFadeInTweens.Remove(segmentIndex);
        }

        // Kill any active bob tween
        if (_zzzBobTweens.TryGetValue(segmentIndex, out var bobTween))
        {
            bobTween?.Kill();
            _zzzBobTweens.Remove(segmentIndex);
        }

        // Fade out and remove the label
        if (_zzzLabels.TryGetValue(segmentIndex, out var label))
        {
            if (IsInstanceValid(label))
            {
                var fadeOut = CreateTween();
                fadeOut.SetParallel(true);
                fadeOut.TweenProperty(label, "modulate:a", 0.0f, 0.5f);
                fadeOut.TweenProperty(label, "outline_modulate:a", 0.0f, 0.5f);
                fadeOut.SetParallel(false);
                fadeOut.TweenCallback(Callable.From(() =>
                {
                    if (IsInstanceValid(label))
                        label.QueueFree();
                }));
            }

            _zzzLabels.Remove(segmentIndex);
        }
    }

    /// <summary>
    /// Starts a gentle infinite vertical bob animation on the Zzz label
    /// for the given segment.
    /// </summary>
    private void StartZzzBob(int segmentIndex)
    {
        if (!_zzzLabels.TryGetValue(segmentIndex, out var label))
            return;
        if (!IsInstanceValid(label))
            return;

        // Kill any existing bob tween for this segment
        if (_zzzBobTweens.TryGetValue(segmentIndex, out var existingBob))
            existingBob?.Kill();

        float baseY = label.Position.Y;
        var bobTween = CreateTween();
        bobTween.SetLoops();
        bobTween.TweenProperty(label, "position:y", baseY + 0.03f, 1.5f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        bobTween.TweenProperty(label, "position:y", baseY, 1.5f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);

        _zzzBobTweens[segmentIndex] = bobTween;
    }
}
