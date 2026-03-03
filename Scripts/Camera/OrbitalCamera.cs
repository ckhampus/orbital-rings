using Godot;
using OrbitalRings.Autoloads;

namespace OrbitalRings.Camera;

/// <summary>
/// Orbital camera system providing smooth momentum orbit, bounded zoom,
/// idle auto-orbit, and WASD/right-click-drag input around a central point.
///
/// Attach to a Node3D ("CameraRig") at world origin with a child Camera3D.
/// The rig rotates on Y (orbit). The child Camera3D is positioned using
/// spherical coordinates (tilt angle + distance) so it always looks at the
/// origin from an elevated vantage point.
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
  [Export] public float TouchpadZoomSpeed { get; set; } = 0.5f;

  [ExportGroup("Idle")]
  [Export] public float IdleOrbitSpeed { get; set; } = 0.02f;
  [Export] public float IdleTimeout { get; set; } = 5.0f;

  [ExportGroup("Tilt")]
  [Export] public float TiltAngleDeg { get; set; } = 45.0f;
  [Export] public float TiltMin { get; set; } = 20.0f;
  [Export] public float TiltMax { get; set; } = 60.0f;
  [Export] public float TiltSpeed { get; set; } = 40.0f;
  [Export] public float TiltSmoothing { get; set; } = 8.0f;

  // -------------------------------------------------------------------------
  // Private State
  // -------------------------------------------------------------------------

  private Camera3D _camera;
  private float _orbitVelocity;
  private float _targetZoom;
  private float _currentZoom;
  private float _targetTiltDeg;
  private float _currentTiltDeg;
  private float _idleTimer;
  private bool _isDragging;
  private bool _isTilting;
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

	// Initialize tilt from the exported starting angle
	_targetTiltDeg = TiltAngleDeg;
	_currentTiltDeg = TiltAngleDeg;

	// Position camera using spherical coordinates and look at origin.
	// The CameraRig sits at world origin; Camera3D is offset so it
	// looks down at the pivot from the correct tilt angle and distance.
	UpdateCameraTransform();
  }

  public override void _Input(InputEvent @event)
  {
	// Right-click press/release: toggle drag mode
	if (@event is InputEventMouseButton mb)
	{
	  if (mb.ButtonIndex == MouseButton.Right)
	  {
		_isDragging = mb.Pressed;
	  }
	  else if (mb.ButtonIndex == MouseButton.Middle)
	  {
		_isTilting = mb.Pressed;
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
	// Right-click drag: orbit / Middle-click drag: tilt
	else if (@event is InputEventMouseMotion mm)
	{
	  if (_isDragging)
	  {
		_orbitVelocity = -mm.Relative.X * OrbitSpeed;
		ResetIdleTimer();
	  }
	  if (_isTilting)
	  {
		_targetTiltDeg = Mathf.Clamp(
		  _targetTiltDeg + -mm.Relative.Y * 0.3f,
		  TiltMin,
		  TiltMax);
		ResetIdleTimer();
	  }
	}
	// Touchpad two-finger scroll: zoom
	else if (@event is InputEventPanGesture pan)
	{
	  // Pan gesture Delta.Y: positive = scroll down (zoom out), negative = scroll up (zoom in)
	  _targetZoom = Mathf.Clamp(_targetZoom + pan.Delta.Y * TouchpadZoomSpeed, ZoomMin, ZoomMax);
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

	// Keyboard zoom input (+/- keys)
	if (Input.IsActionPressed("zoom_in"))
	{
	  _targetZoom = Mathf.Max(_targetZoom - ZoomSpeed * dt * 3.0f, ZoomMin);
	  ResetIdleTimer();
	}
	if (Input.IsActionPressed("zoom_out"))
	{
	  _targetZoom = Mathf.Min(_targetZoom + ZoomSpeed * dt * 3.0f, ZoomMax);
	  ResetIdleTimer();
	}

	// Keyboard tilt input (W/S / Up/Down arrow keys)
	if (Input.IsActionPressed("tilt_up"))
	{
	  _targetTiltDeg = Mathf.Min(_targetTiltDeg + TiltSpeed * dt, TiltMax);
	  ResetIdleTimer();
	}
	if (Input.IsActionPressed("tilt_down"))
	{
	  _targetTiltDeg = Mathf.Max(_targetTiltDeg - TiltSpeed * dt, TiltMin);
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

	// Smooth tilt (lerp toward target)
	_currentTiltDeg = Mathf.Lerp(_currentTiltDeg, _targetTiltDeg, TiltSmoothing * dt);

	// Update camera position and orientation to maintain tilt + distance
	UpdateCameraTransform();

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
  /// Position the Camera3D using spherical coordinates so it looks down
  /// at the CameraRig pivot (world origin) from the configured tilt angle
  /// and current zoom distance.
  ///
  /// Y = zoom * sin(tilt) = height above ground
  /// Z = zoom * cos(tilt) = horizontal distance from pivot
  /// Then LookAt(origin) to point the camera correctly.
  /// </summary>
  private void UpdateCameraTransform()
  {
	float tiltRad = Mathf.DegToRad(_currentTiltDeg);
	float height = _currentZoom * Mathf.Sin(tiltRad);
	float distance = _currentZoom * Mathf.Cos(tiltRad);

	_camera.Position = new Vector3(0, height, distance);
	_camera.LookAt(Vector3.Zero, Vector3.Up);
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

	if (!InputMap.HasAction("zoom_in"))
	{
	  InputMap.AddAction("zoom_in");
	  var keyPlus = new InputEventKey();
	  keyPlus.PhysicalKeycode = Key.Equal;  // +/= key
	  InputMap.ActionAddEvent("zoom_in", keyPlus);
	  var keyKpPlus = new InputEventKey();
	  keyKpPlus.PhysicalKeycode = Key.KpAdd;  // Numpad +
	  InputMap.ActionAddEvent("zoom_in", keyKpPlus);
	}

	if (!InputMap.HasAction("zoom_out"))
	{
	  InputMap.AddAction("zoom_out");
	  var keyMinus = new InputEventKey();
	  keyMinus.PhysicalKeycode = Key.Minus;
	  InputMap.ActionAddEvent("zoom_out", keyMinus);
	  var keyKpMinus = new InputEventKey();
	  keyKpMinus.PhysicalKeycode = Key.KpSubtract;  // Numpad -
	  InputMap.ActionAddEvent("zoom_out", keyKpMinus);
	}

	if (!InputMap.HasAction("tilt_up"))
	{
	  InputMap.AddAction("tilt_up");
	  var keyW = new InputEventKey();
	  keyW.PhysicalKeycode = Key.W;
	  InputMap.ActionAddEvent("tilt_up", keyW);
	  var keyUp = new InputEventKey();
	  keyUp.PhysicalKeycode = Key.Up;
	  InputMap.ActionAddEvent("tilt_up", keyUp);
	}

	if (!InputMap.HasAction("tilt_down"))
	{
	  InputMap.AddAction("tilt_down");
	  var keyS = new InputEventKey();
	  keyS.PhysicalKeycode = Key.S;
	  InputMap.ActionAddEvent("tilt_down", keyS);
	  var keyDown = new InputEventKey();
	  keyDown.PhysicalKeycode = Key.Down;
	  InputMap.ActionAddEvent("tilt_down", keyDown);
	}
  }
}
