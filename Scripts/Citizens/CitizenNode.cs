using Godot;
using OrbitalRings.Autoloads;
using OrbitalRings.Data;
using OrbitalRings.Ring;

namespace OrbitalRings.Citizens;

/// <summary>
/// A walking citizen on the circular walkway. Uses angle-based polar coordinate
/// movement along the walkway centerline (radius 4.5, midpoint of inner/outer row edges).
///
/// Room visit behavior: periodically drifts toward a nearby occupied segment,
/// fades out (entering room), waits inside, fades back in, and resumes walking.
///
/// Extends Node3D (not SafeNode) because SafeNode extends Node, not Node3D.
/// CitizenNode needs Node3D for Position/Rotation. Implements the same
/// subscribe/unsubscribe lifecycle pattern manually via _EnterTree/_ExitTree.
///
/// Navigation is deliberately simple: the walkway is a 1D circular path,
/// so navigation is literally angle += speed * delta. No NavigationAgent3D needed.
/// </summary>
public partial class CitizenNode : Node3D
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    /// <summary>Walkway centerline radius (midpoint of InnerRowOuter=4.0 and OuterRowInner=5.0).</summary>
    public const float WalkwayRadius = 4.5f;

    /// <summary>Base walking speed in radians/sec (~42s for a full loop).</summary>
    private const float BaseSpeed = 0.15f;

    /// <summary>Speed variation range (+/-15% of base speed).</summary>
    private const float SpeedVariation = 0.15f;

    /// <summary>Vertical bob amplitude (subtle walking rhythm).</summary>
    private const float BobAmplitude = 0.015f;

    /// <summary>Vertical bob frequency in rad/s (~1.3 Hz natural walking rhythm).</summary>
    private const float BobFrequency = 8.0f;

    // ---- Visit constants ----

    /// <summary>Minimum seconds between visit attempts.</summary>
    private const float VisitTimerMin = 20.0f;

    /// <summary>Maximum seconds between visit attempts.</summary>
    private const float VisitTimerMax = 40.0f;

    /// <summary>Minimum seconds spent inside a room.</summary>
    private const float VisitDurationMin = 4.0f;

    /// <summary>Maximum seconds spent inside a room.</summary>
    private const float VisitDurationMax = 8.0f;

    /// <summary>Seconds for radial drift from walkway to room edge.</summary>
    private const float DriftDuration = 0.5f;

    /// <summary>Seconds for fade out/in animation.</summary>
    private const float FadeDuration = 0.3f;

    /// <summary>Drift target radius for inner row rooms.</summary>
    private const float InnerDriftRadius = 3.5f;

    /// <summary>Drift target radius for outer row rooms.</summary>
    private const float OuterDriftRadius = 5.5f;

    /// <summary>
    /// Citizen visits room within this many segment arcs of angular distance.
    /// ~1.5 segment widths ensures citizens only visit nearby rooms.
    /// </summary>
    private const float VisitProximityThreshold = 1.5f;

    // -------------------------------------------------------------------------
    // Fields
    // -------------------------------------------------------------------------

    private CitizenData _data;
    private float _currentAngle;    // radians, 0 to Tau
    private float _direction;       // +1.0 CCW or -1.0 CW
    private float _speed;           // radians/sec
    private float _bobPhase;        // randomized per citizen to avoid sync (pitfall #5)
    private bool _isVisiting;       // true during room visit sequence
    private Node3D _meshContainer;  // holds capsule meshes from CitizenAppearance
    private Tween _activeTween;     // for visit animation, kill-before-create pattern (pitfall #7)
    private Timer _visitTimer;      // periodic check for nearby occupied rooms
    private SegmentGrid _grid;      // reference to ring occupancy data

    // -------------------------------------------------------------------------
    // Properties
    // -------------------------------------------------------------------------

    /// <summary>Citizen identity and appearance data.</summary>
    public CitizenData Data => _data;

    /// <summary>Current angular position on the walkway (radians, 0 to Tau).</summary>
    public float CurrentAngle => _currentAngle;

    /// <summary>Whether this citizen is currently visiting a room.</summary>
    public bool IsVisiting => _isVisiting;

    // -------------------------------------------------------------------------
    // Lifecycle (SafeNode pattern for Node3D)
    // -------------------------------------------------------------------------

    public override void _EnterTree()
    {
        SubscribeEvents();
    }

    public override void _ExitTree()
    {
        UnsubscribeEvents();
        _activeTween?.Kill();
    }

    /// <summary>
    /// Override to connect to GameEvents.Instance and other signal sources.
    /// Called automatically in _EnterTree().
    /// </summary>
    protected virtual void SubscribeEvents() { }

    /// <summary>
    /// Override to disconnect from all signal sources. Called automatically
    /// in _ExitTree(). MUST mirror every connection made in SubscribeEvents().
    /// </summary>
    protected virtual void UnsubscribeEvents() { }

    // -------------------------------------------------------------------------
    // Initialization
    // -------------------------------------------------------------------------

    /// <summary>
    /// Initialize the citizen with data, starting angle, and grid reference.
    /// Must be called before adding to the scene tree.
    /// </summary>
    /// <param name="data">Citizen identity and appearance data.</param>
    /// <param name="startAngle">Starting angular position in radians (0 to Tau).</param>
    /// <param name="grid">Ring segment grid for occupancy checks during room visits.</param>
    public void Initialize(CitizenData data, float startAngle, SegmentGrid grid)
    {
        _data = data;
        _currentAngle = startAngle;
        _grid = grid;
        Name = data.CitizenName;

        // Random direction: CW or CCW
        _direction = GD.Randf() > 0.5f ? 1.0f : -1.0f;

        // Random speed variation (+/-15% of base speed)
        _speed = BaseSpeed * (1.0f + (GD.Randf() * 2f - 1f) * SpeedVariation);

        // Random bob phase to prevent sync across citizens (pitfall #5)
        _bobPhase = GD.Randf() * Mathf.Tau;

        // Create visual mesh from CitizenData
        _meshContainer = CitizenAppearance.CreateCitizenMesh(data);
        AddChild(_meshContainer);

        // Create visit timer as child Timer node
        // Explicit Timer.Start() over Autostart=true (Phase 3 decision: Autostart before AddChild unreliable)
        _visitTimer = new Timer
        {
            Name = "VisitTimer",
            OneShot = false,
            WaitTime = VisitTimerMin + GD.Randf() * (VisitTimerMax - VisitTimerMin)
        };
        _visitTimer.Timeout += OnVisitTimerTimeout;
        AddChild(_visitTimer);
        _visitTimer.Start();

        // Set initial position from angle
        UpdatePositionFromAngle();
    }

    // -------------------------------------------------------------------------
    // Process (walking movement)
    // -------------------------------------------------------------------------

    public override void _Process(double delta)
    {
        // Don't walk during room visits
        if (_isVisiting) return;

        float dt = (float)delta;

        // Advance angle
        _currentAngle += _direction * _speed * dt;

        // Wrap to [0, Tau)
        if (_currentAngle < 0) _currentAngle += Mathf.Tau;
        if (_currentAngle >= Mathf.Tau) _currentAngle -= Mathf.Tau;

        // Advance bob phase
        _bobPhase += BobFrequency * dt;

        UpdatePositionFromAngle();
    }

    // -------------------------------------------------------------------------
    // Position computation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Computes the citizen's 3D position from the current angle using polar coordinates.
    /// Includes vertical bob and proper surface Y offset.
    /// </summary>
    private void UpdatePositionFromAngle()
    {
        SetRadialPosition(_currentAngle, WalkwayRadius, includeBob: true);
    }

    /// <summary>
    /// Sets the citizen's 3D position from polar coordinates (angle + radius).
    /// Used for both normal walking and visit drift animation.
    /// </summary>
    /// <param name="angle">Angular position in radians.</param>
    /// <param name="radius">Radial distance from center.</param>
    /// <param name="includeBob">Whether to include vertical bob offset.</param>
    private void SetRadialPosition(float angle, float radius, bool includeBob = false)
    {
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;

        float bob = includeBob ? Mathf.Sin(_bobPhase) * BobAmplitude : 0f;

        float surfaceY = SegmentGrid.RingHeight / 2f - SegmentGrid.WalkwayRecess
                       + CitizenAppearance.GetCapsuleHeight(_data.Body) / 2f;

        Position = new Vector3(x, surfaceY + bob, z);

        // Face direction of travel (tangent to circle)
        float facingAngle = angle + (_direction > 0 ? Mathf.Pi * 0.5f : -Mathf.Pi * 0.5f);
        Rotation = new Vector3(0, -facingAngle, 0);
    }

    // -------------------------------------------------------------------------
    // Room visit system
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fired periodically by _visitTimer. Checks for nearby occupied segments
    /// and initiates a visit if one is close enough.
    /// </summary>
    private void OnVisitTimerTimeout()
    {
        // Don't start a new visit while already visiting
        if (_isVisiting) return;

        // No grid reference means no occupancy data (shouldn't happen, but guard)
        if (_grid == null) return;

        // Find nearest occupied segment by angular distance
        float myAngle = _currentAngle;
        int bestSegment = -1;
        float bestAngleDist = float.MaxValue;
        SegmentRow bestRow = SegmentRow.Outer;

        for (int i = 0; i < SegmentGrid.TotalSegments; i++)
        {
            var (row, pos) = SegmentGrid.FromIndex(i);
            if (!_grid.IsOccupied(row, pos)) continue;

            float segMidAngle = SegmentGrid.GetStartAngle(pos) + SegmentGrid.SegmentArc * 0.5f;
            float angleDist = AngleDistance(myAngle, segMidAngle);
            if (angleDist < bestAngleDist)
            {
                bestAngleDist = angleDist;
                bestSegment = i;
                bestRow = row;
            }
        }

        // Only visit if nearest occupied segment is within proximity threshold
        if (bestSegment >= 0 && bestAngleDist < SegmentGrid.SegmentArc * VisitProximityThreshold)
        {
            StartVisit(bestRow);
        }

        // Reset timer with new random interval regardless of whether visit started
        _visitTimer.WaitTime = VisitTimerMin + GD.Randf() * (VisitTimerMax - VisitTimerMin);
        _visitTimer.Start();
    }

    /// <summary>
    /// Begins the room visit animation sequence: drift to room edge, fade out,
    /// wait inside, fade in, drift back to walkway.
    /// </summary>
    /// <param name="targetRow">Which row the target room is in (determines drift direction).</param>
    private void StartVisit(SegmentRow targetRow)
    {
        _isVisiting = true;

        // Kill any active tween before creating new one (pitfall #7)
        _activeTween?.Kill();

        float targetRadius = targetRow == SegmentRow.Outer ? OuterDriftRadius : InnerDriftRadius;
        float visitDuration = VisitDurationMin + GD.Randf() * (VisitDurationMax - VisitDurationMin);
        float currentRadius = WalkwayRadius;
        float angle = _currentAngle;
        string citizenName = _data.CitizenName;

        // Prepare materials for transparency (pitfall #4: set BEFORE fading)
        SetMeshTransparencyMode(true);

        // Create chained tween sequence
        var tween = CreateTween();
        _activeTween = tween;

        // Phase 1: Drift radially from walkway to room edge
        tween.TweenMethod(
            Callable.From((float t) => SetRadialPosition(angle, Mathf.Lerp(currentRadius, targetRadius, t))),
            0.0f, 1.0f, DriftDuration
        );

        // Phase 2: Fade out
        tween.TweenMethod(
            Callable.From((float alpha) => SetMeshAlpha(alpha)),
            1.0f, 0.0f, FadeDuration
        );

        // Phase 3: Hide and emit entered event
        tween.TweenCallback(Callable.From(() =>
        {
            Visible = false;
            GameEvents.Instance?.EmitCitizenEnteredRoom(citizenName);
        }));

        // Phase 4: Wait inside room
        tween.TweenInterval(visitDuration);

        // Phase 5: Show at target radius and emit exited event
        tween.TweenCallback(Callable.From(() =>
        {
            Visible = true;
            SetRadialPosition(angle, targetRadius);
            GameEvents.Instance?.EmitCitizenExitedRoom(citizenName);
        }));

        // Phase 6: Fade in
        tween.TweenMethod(
            Callable.From((float alpha) => SetMeshAlpha(alpha)),
            0.0f, 1.0f, FadeDuration
        );

        // Phase 7: Drift back from room edge to walkway
        tween.TweenMethod(
            Callable.From((float t) => SetRadialPosition(angle, Mathf.Lerp(targetRadius, currentRadius, t))),
            0.0f, 1.0f, DriftDuration
        );

        // Phase 8: Restore opaque materials and resume walking
        tween.TweenCallback(Callable.From(() =>
        {
            SetMeshAlpha(1.0f);
            SetMeshTransparencyMode(false);
            _isVisiting = false;
            _activeTween = null;
        }));
    }

    // -------------------------------------------------------------------------
    // Mesh transparency helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Enables or disables alpha transparency mode on all citizen mesh materials.
    /// Must be called BEFORE fading to avoid Z-fighting artifacts (pitfall #4).
    /// </summary>
    private void SetMeshTransparencyMode(bool enabled)
    {
        if (_meshContainer == null) return;

        foreach (var child in _meshContainer.GetChildren())
        {
            if (child is MeshInstance3D mesh && mesh.MaterialOverride is StandardMaterial3D mat)
            {
                if (enabled)
                {
                    mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                    mat.DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Always;
                }
                else
                {
                    mat.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
                    mat.DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.OpaqueOnly;
                }
            }
        }
    }

    /// <summary>
    /// Sets the alpha channel on all citizen mesh materials (body + band).
    /// </summary>
    /// <param name="alpha">Alpha value from 0 (invisible) to 1 (opaque).</param>
    private void SetMeshAlpha(float alpha)
    {
        if (_meshContainer == null) return;

        foreach (var child in _meshContainer.GetChildren())
        {
            if (child is MeshInstance3D mesh && mesh.MaterialOverride is StandardMaterial3D mat)
            {
                var color = mat.AlbedoColor;
                mat.AlbedoColor = new Color(color.R, color.G, color.B, alpha);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Static helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the shortest angular distance between two angles (always positive).
    /// Handles wraparound at Tau boundary.
    /// </summary>
    private static float AngleDistance(float a, float b)
    {
        float diff = Mathf.Abs(a - b);
        return Mathf.Min(diff, Mathf.Tau - diff);
    }

    // -------------------------------------------------------------------------
    // Public helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the primary MeshInstance3D (first child of mesh container).
    /// Used for emission glow on selected citizen.
    /// </summary>
    public MeshInstance3D GetPrimaryMesh()
    {
        if (_meshContainer == null) return null;
        return _meshContainer.GetChildOrNull<MeshInstance3D>(0);
    }
}
