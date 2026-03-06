---
phase: quick-6
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Scripts/Camera/OrbitalCamera.cs
autonomous: true
requirements: [QUICK-6]
must_haves:
  truths:
    - "Camera orbits along the walkway circle, not around a fixed center"
    - "Look-at point slides along walkway centerline as user orbits"
    - "Zoom still works, always focused on walkway point"
    - "Tilt still works with walkway-centered pivot"
    - "Idle auto-orbit continues working with new pivot"
  artifacts:
    - path: "Scripts/Camera/OrbitalCamera.cs"
      provides: "Walkway-centered orbital camera"
      contains: "WalkwayCenterRadius"
  key_links:
    - from: "Scripts/Camera/OrbitalCamera.cs"
      to: "Scripts/Ring/SegmentGrid.cs"
      via: "InnerRowOuter and OuterRowInner constants for walkway center"
      pattern: "SegmentGrid\\.(InnerRowOuter|OuterRowInner)"
---

<objective>
Modify the orbital camera so it focuses on the walkway centerline instead of the world origin.

Purpose: The camera should feel like it is "walking the ring" — orbiting moves the viewpoint along the circular walkway path (radius 4.5), and the camera always looks at the walkway point beneath it rather than the center of the ring.

Output: Updated OrbitalCamera.cs with walkway-centered orbit behavior.
</objective>

<execution_context>
@/home/node/.claude/get-shit-done/workflows/execute-plan.md
@/home/node/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/quick/6-camera-should-focus-on-walk-path-rotatin/6-CONTEXT.md
@Scripts/Camera/OrbitalCamera.cs
@Scripts/Ring/SegmentGrid.cs
</context>

<interfaces>
<!-- Key constants from SegmentGrid needed for walkway centerline calculation -->

From Scripts/Ring/SegmentGrid.cs:
```csharp
public const float OuterRowInner = 5.0f;
public const float InnerRowOuter = 4.0f;
```

Walkway centerline = (InnerRowOuter + OuterRowInner) / 2f = 4.5f
</interfaces>

<tasks>

<task type="auto">
  <name>Task 1: Shift camera orbit pivot to walkway centerline</name>
  <files>Scripts/Camera/OrbitalCamera.cs</files>
  <action>
Modify OrbitalCamera.cs to orbit along the walkway circle instead of around world origin. The changes are:

1. Add a using directive for `OrbitalRings.Ring` (to access SegmentGrid constants).

2. Add a private constant for the walkway center radius:
   ```csharp
   private static readonly float WalkwayCenterRadius =
       (SegmentGrid.InnerRowOuter + SegmentGrid.OuterRowInner) / 2f;
   ```

3. Update `UpdateCameraTransform()` to:
   a. Compute the walkway point at the current orbit angle using `Rotation.Y`:
      ```csharp
      float orbitAngle = Rotation.Y;
      float walkX = WalkwayCenterRadius * Mathf.Sin(orbitAngle);
      float walkZ = WalkwayCenterRadius * Mathf.Cos(orbitAngle);
      Position = new Vector3(walkX, 0, walkZ);
      ```
   b. Keep the existing tilt/zoom spherical offset for the Camera3D child (local position unchanged):
      ```csharp
      _camera.Position = new Vector3(0, height, distance);
      ```
   c. Change `_camera.LookAt(Vector3.Zero, Vector3.Up)` to `_camera.LookAt(GlobalPosition, Vector3.Up)` so the camera looks at the walkway point (the rig's position) instead of world origin.

4. Update the class summary doc-comment to reflect the new behavior: the rig translates along the walkway circle at radius 4.5, and the camera looks at the walkway point rather than world origin.

All input handling (RotateY for orbit, keyboard orbit, zoom, tilt, idle orbit, momentum) remains unchanged. The rotation still drives orbit angle; the new code simply also translates the rig position based on that angle.
  </action>
  <verify>
    <automated>cd /workspace && dotnet build 2>&1 | tail -5</automated>
  </verify>
  <done>
    - OrbitalCamera.cs compiles without errors
    - Camera rig position is set to walkway circle point each frame based on Rotation.Y
    - Camera3D child looks at GlobalPosition (walkway point) instead of Vector3.Zero
    - All existing controls (orbit, zoom, tilt, idle, momentum) still function
    - Code passes dotnet format
  </done>
</task>

</tasks>

<verification>
- `dotnet build` succeeds with no errors or warnings related to OrbitalCamera
- `dotnet format --verify-no-changes` passes
- Visual verification: camera orbits along walkway path, not around center
</verification>

<success_criteria>
Camera pivot is on the walkway centerline (radius 4.5). Orbiting slides the viewpoint along the circular walkway path. The camera always looks at the walkway point beneath it. Zoom, tilt, and idle auto-orbit all work correctly with the new pivot.
</success_criteria>

<output>
After completion, create `.planning/quick/6-camera-should-focus-on-walk-path-rotatin/6-SUMMARY.md`
</output>
