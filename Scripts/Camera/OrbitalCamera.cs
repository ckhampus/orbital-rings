using Godot;
using OrbitalRings.Autoloads;

namespace OrbitalRings.Camera;

/// <summary>
/// Orbital camera system providing smooth momentum orbit, bounded zoom,
/// idle auto-orbit, and WASD/right-click-drag input around a central point.
///
/// Attach to a Node3D ("CameraRig") at world origin with a child Camera3D.
/// The rig rotates on Y (orbit), the child Camera3D is offset on Z (zoom)
/// and tilted on X (fixed overhead angle).
///
/// Does NOT extend SafeNode — the camera manages its own lifecycle as a
/// scene-level node, not a signal consumer.
/// </summary>
public partial class OrbitalCamera : Node3D
{
  // -------------------------------------------------------------------------
  // Exported Properties (tunable in Inspector)
  // -------------------------------------------------------------------------

  [ExportGroup("Orbit")]
  [Export] public float OrbitSpeed { get; set; } = 0.005f;
  [Export] public float OrbitMomentumDecay { get; set; } = 0.92f;
  [Export] public float KeyboardOrbitSpeed { get; set; } = 2.0f;

  [ExportGroup("Zoom")]
  [Export] public float ZoomMin { get; set; } = 5.0f;
  [Export] public float ZoomMax { get; set; } = 25.0f;
  [Export] public float ZoomSpeed { get; set; } = 1.5f;
  [Export] public float ZoomSmoothing { get; set; } = 8.0f;

  [ExportGroup("Idle")]
  [Export] public float IdleOrbitSpeed { get; set; } = 0.02f;
  [Export] public float IdleTimeout { get; set; } = 5.0f;

  [ExportGroup("Tilt")]
  [Export] public float TiltAngleDeg { get; set; } = 60.0f;

  // -------------------------------------------------------------------------
  // Private State
  // -------------------------------------------------------------------------

  private Camera3D _camera;
  private float _orbitVelocity;
  private float _targetZoom;
  private float _currentZoom;
  private float _idleTimer;
  private bool _isDragging;
  private bool _wasOrbiting;

  // -------------------------------------------------------------------------
  // Lifecycle
  // -------------------------------------------------------------------------

  public override void _Ready()
  {
    EnsureInputActions();

    _camera = GetNode<Camera3D>("Camera3D");

    // Default view: zoomed out to show whole ring
    _targetZoom = ZoomMax;
    _currentZoom = ZoomMax;

    // Fixed tilt angle (strategy-game overhead view)
    _camera.RotationDegrees = new Vector3(-TiltAngleDeg, 0, 0);
    _camera.Position = new Vector3(0, 0, _currentZoom);
  }

  public override void _UnhandledInput(InputEvent @event)
  {
    // Right-click press/release: toggle drag mode
    if (@event is InputEventMouseButton mb)
    {
      if (mb.ButtonIndex == MouseButton.Right)
      {
        _isDragging = mb.Pressed;
      }
      // Scroll wheel zoom
      else if (mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed)
      {
        _targetZoom = Mathf.Max(_targetZoom - ZoomSpeed, ZoomMin);
        ResetIdleTimer();
      }
      else if (mb.ButtonIndex == MouseButton.WheelDown && mb.Pressed)
      {
        _targetZoom = Mathf.Min(_targetZoom + ZoomSpeed, ZoomMax);
        ResetIdleTimer();
      }
    }
    // Right-click drag: orbit
    else if (@event is InputEventMouseMotion mm && _isDragging)
    {
      _orbitVelocity = -mm.Relative.X * OrbitSpeed;
      ResetIdleTimer();
    }
  }

  public override void _Process(double delta)
  {
    float dt = (float)delta;

    // Keyboard orbit input (WASD / arrow keys)
    float keyInput = Input.GetAxis("orbit_left", "orbit_right");
    if (Mathf.Abs(keyInput) > 0.01f)
    {
      _orbitVelocity += keyInput * KeyboardOrbitSpeed * dt;
      ResetIdleTimer();
    }

    // Apply orbit rotation (rotate the pivot Node3D on Y axis)
    RotateY(_orbitVelocity);

    // Decay momentum (cinematic glide after input stops)
    _orbitVelocity *= OrbitMomentumDecay;

    // Clamp tiny velocities to zero
    if (Mathf.Abs(_orbitVelocity) < 0.0001f)
    {
      _orbitVelocity = 0f;
    }

    // Smooth zoom (lerp toward target)
    _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, ZoomSmoothing * dt);
    _camera.Position = new Vector3(0, 0, _currentZoom);

    // Idle orbit: gentle auto-orbit after timeout with no input
    _idleTimer += dt;
    if (_idleTimer > IdleTimeout && _orbitVelocity == 0f)
    {
      RotateY(IdleOrbitSpeed * dt);
    }

    // Emit GameEvents orbit start/stop on transitions
    bool isOrbiting = Mathf.Abs(_orbitVelocity) > 0.0001f;
    if (isOrbiting && !_wasOrbiting)
    {
      GameEvents.Instance?.EmitCameraOrbitStarted();
    }
    else if (!isOrbiting && _wasOrbiting)
    {
      GameEvents.Instance?.EmitCameraOrbitStopped();
    }
    _wasOrbiting = isOrbiting;
  }

  // -------------------------------------------------------------------------
  // Helpers
  // -------------------------------------------------------------------------

  private void ResetIdleTimer()
  {
    _idleTimer = 0f;
  }

  /// <summary>
  /// Programmatically register orbit_left and orbit_right input actions
  /// as a fallback in case project.godot entries are missing.
  /// This is more reliable than editing project.godot's complex input
  /// serialization format.
  /// </summary>
  private static void EnsureInputActions()
  {
    if (!InputMap.HasAction("orbit_left"))
    {
      InputMap.AddAction("orbit_left");

      var keyA = new InputEventKey();
      keyA.PhysicalKeycode = Key.A;
      InputMap.ActionAddEvent("orbit_left", keyA);

      var keyLeft = new InputEventKey();
      keyLeft.PhysicalKeycode = Key.Left;
      InputMap.ActionAddEvent("orbit_left", keyLeft);
    }

    if (!InputMap.HasAction("orbit_right"))
    {
      InputMap.AddAction("orbit_right");

      var keyD = new InputEventKey();
      keyD.PhysicalKeycode = Key.D;
      InputMap.ActionAddEvent("orbit_right", keyD);

      var keyRight = new InputEventKey();
      keyRight.PhysicalKeycode = Key.Right;
      InputMap.ActionAddEvent("orbit_right", keyRight);
    }
  }
}
