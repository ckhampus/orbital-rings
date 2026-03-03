---
phase: quick-3
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Scripts/Camera/OrbitalCamera.cs
autonomous: true
requirements: [QUICK-3]
must_haves:
  truths:
    - "Player can adjust camera tilt angle in-game using keyboard keys"
    - "Tilt is clamped between 20 and 60 degrees"
    - "Tilt transitions smoothly (not snapping)"
  artifacts:
    - path: "Scripts/Camera/OrbitalCamera.cs"
      provides: "In-game tilt adjustment with input handling and smooth interpolation"
      contains: "TiltMin"
  key_links:
    - from: "tilt input (W/S or Q/E keys)"
      to: "_targetTiltDeg field"
      via: "keyboard action in _Process"
      pattern: "_targetTiltDeg"
---

<objective>
Add in-game camera tilt adjustment so the player can change the viewing angle between 20 and 60 degrees using keyboard input, with smooth interpolation.

Purpose: Let the player control how top-down vs side-on they view the ring, improving spatial awareness and aesthetics.
Output: Modified OrbitalCamera.cs with tilt input, clamping, and smooth transitions.
</objective>

<execution_context>
@/home/node/.claude/get-shit-done/workflows/execute-plan.md
@/home/node/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@Scripts/Camera/OrbitalCamera.cs
</context>

<interfaces>
<!-- From Scripts/Camera/OrbitalCamera.cs -->
<!-- Key existing patterns to follow: -->

Existing zoom pattern (use as template for tilt):
- _targetZoom / _currentZoom pair with Mathf.Lerp smoothing
- ZoomSmoothing export controls lerp speed
- Keyboard zoom uses Input.IsActionPressed with dt * 3.0f multiplier
- Clamped with Mathf.Clamp between ZoomMin and ZoomMax

Existing input action registration pattern in EnsureInputActions():
- InputMap.HasAction check -> AddAction -> create InputEventKey -> ActionAddEvent

Existing spherical coordinate system in UpdateCameraTransform():
- TiltAngleDeg is already used to compute camera height and distance
- Just needs to read from _currentTiltDeg instead of TiltAngleDeg directly
</interfaces>

<tasks>

<task type="auto">
  <name>Task 1: Add tilt input, smoothing, and clamping to OrbitalCamera</name>
  <files>Scripts/Camera/OrbitalCamera.cs</files>
  <action>
Modify OrbitalCamera.cs to add in-game tilt adjustment. Follow the existing zoom pattern exactly:

1. **Add Tilt exports** in the existing [ExportGroup("Tilt")] section:
   - Change existing TiltAngleDeg default from 60.0f to 45.0f (good middle starting point)
   - Add: `[Export] public float TiltMin { get; set; } = 20.0f;`
   - Add: `[Export] public float TiltMax { get; set; } = 60.0f;`
   - Add: `[Export] public float TiltSpeed { get; set; } = 40.0f;` (degrees per second, feels responsive)
   - Add: `[Export] public float TiltSmoothing { get; set; } = 8.0f;` (same as ZoomSmoothing)

2. **Add private state fields** alongside existing _targetZoom/_currentZoom:
   - `private float _targetTiltDeg;`
   - `private float _currentTiltDeg;`

3. **Initialize in _Ready()** after existing zoom init:
   - `_targetTiltDeg = TiltAngleDeg;`
   - `_currentTiltDeg = TiltAngleDeg;`

4. **Register tilt input actions** in EnsureInputActions():
   - Register "tilt_up" action: Key.W and Key.Up (shared with no conflict since orbit uses A/D/Left/Right)
   - Register "tilt_down" action: Key.S and Key.Down
   - IMPORTANT: W/S are NOT used by any existing action (orbit uses A/D, zoom uses +/-). Arrow Up/Down are also free.

5. **Add tilt input handling in _Process()** after the keyboard zoom block (around line 133):
   ```csharp
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
   ```
   Note: "tilt_up" INCREASES the angle (more top-down), "tilt_down" DECREASES (more side-on). This matches intuition: W = look from above.

6. **Add smooth tilt interpolation** right after the existing zoom lerp (`_currentZoom = Mathf.Lerp(...)` line):
   ```csharp
   _currentTiltDeg = Mathf.Lerp(_currentTiltDeg, _targetTiltDeg, TiltSmoothing * dt);
   ```

7. **Update UpdateCameraTransform()** to use `_currentTiltDeg` instead of `TiltAngleDeg`:
   - Change: `float tiltRad = Mathf.DegToRad(TiltAngleDeg);`
   - To: `float tiltRad = Mathf.DegToRad(_currentTiltDeg);`

8. **Also add mouse middle-button drag for tilt** in _Input() for mouse-centric users:
   - Track middle mouse button press/release (similar to right-click drag pattern for orbit)
   - Add `private bool _isTilting;` field
   - In InputEventMouseButton handler: if ButtonIndex == MouseButton.Middle, set `_isTilting = mb.Pressed;`
   - In InputEventMouseMotion handler: if `_isTilting`, adjust `_targetTiltDeg += -mm.Relative.Y * 0.3f` (negative so drag-up = more top-down), clamp to [TiltMin, TiltMax], and ResetIdleTimer()
  </action>
  <verify>
    <automated>cd /workspace && dotnet build 2>&1 | tail -5</automated>
  </verify>
  <done>Camera tilt can be adjusted in-game via W/S keys and middle-mouse-drag. Tilt smoothly interpolates between 20 and 60 degrees. Build succeeds with no errors.</done>
</task>

</tasks>

<verification>
- `dotnet build` completes with no errors
- OrbitalCamera.cs contains TiltMin (20), TiltMax (60), TiltSpeed, TiltSmoothing exports
- _targetTiltDeg and _currentTiltDeg fields exist with Lerp smoothing
- UpdateCameraTransform uses _currentTiltDeg not TiltAngleDeg
- tilt_up and tilt_down input actions registered in EnsureInputActions
- Middle mouse button tilt drag implemented
</verification>

<success_criteria>
Player can press W/S (or Up/Down arrows) to tilt the camera between 20 and 60 degrees with smooth transitions. Middle-mouse-drag also adjusts tilt. Build compiles successfully.
</success_criteria>

<output>
After completion, create `.planning/quick/3-i-want-to-be-able-adjust-the-tilt-of-the/3-SUMMARY.md`
</output>
