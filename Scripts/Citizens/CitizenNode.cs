using Godot;
using OrbitalRings.Data;
using OrbitalRings.Ring;

namespace OrbitalRings.Citizens;

/// <summary>
/// A walking citizen on the circular walkway. Uses angle-based polar coordinate
/// movement along the walkway centerline (radius 4.5, midpoint of inner/outer row edges).
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

    // -------------------------------------------------------------------------
    // Fields
    // -------------------------------------------------------------------------

    private CitizenData _data;
    private float _currentAngle;    // radians, 0 to Tau
    private float _direction;       // +1.0 CCW or -1.0 CW
    private float _speed;           // radians/sec
    private float _bobPhase;        // randomized per citizen to avoid sync (pitfall #5)
    private bool _isVisiting = false;  // set by Plan 02 room visit system
    private Node3D _meshContainer;     // holds capsule meshes from CitizenAppearance
    private Tween _activeTween;        // for visit animation in Plan 02, kill-before-create pattern

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
    /// Initialize the citizen with data and a starting angle on the walkway.
    /// Must be called before adding to the scene tree.
    /// </summary>
    /// <param name="data">Citizen identity and appearance data.</param>
    /// <param name="startAngle">Starting angular position in radians (0 to Tau).</param>
    public void Initialize(CitizenData data, float startAngle)
    {
        _data = data;
        _currentAngle = startAngle;
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
        // Compute XZ position from polar coordinates
        float x = Mathf.Cos(_currentAngle) * WalkwayRadius;
        float z = Mathf.Sin(_currentAngle) * WalkwayRadius;

        // Compute vertical bob (desynchronized per citizen)
        float bob = Mathf.Sin(_bobPhase) * BobAmplitude;

        // Stand ON the walkway surface:
        // Ring top face is at Y = RingHeight/2 = 0.15
        // Walkway is recessed by WalkwayRecess = 0.025
        // So walkway surface is at Y = 0.15 - 0.025 = 0.125
        // Capsule origin is at its center, so offset up by half the capsule height
        float surfaceY = SegmentGrid.RingHeight / 2f - SegmentGrid.WalkwayRecess
                       + CitizenAppearance.GetCapsuleHeight(_data.Body) / 2f;

        Position = new Vector3(x, surfaceY + bob, z);

        // Face direction of travel (tangent to circle)
        // For CCW (+direction), tangent is at angle + 90 degrees
        // For CW (-direction), tangent is at angle - 90 degrees
        float facingAngle = _currentAngle + (_direction > 0 ? Mathf.Pi * 0.5f : -Mathf.Pi * 0.5f);
        Rotation = new Vector3(0, -facingAngle, 0);
    }

    // -------------------------------------------------------------------------
    // Public helpers for Plan 02
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the primary MeshInstance3D (first child of mesh container).
    /// Used by Plan 02 for emission glow on selected citizen.
    /// </summary>
    public MeshInstance3D GetPrimaryMesh()
    {
        if (_meshContainer == null) return null;
        return _meshContainer.GetChildOrNull<MeshInstance3D>(0);
    }
}
