---
status: diagnosed
phase: 01-foundation-and-project-architecture
source: [01-01-SUMMARY.md, 01-02-SUMMARY.md]
started: 2026-03-02T19:10:00Z
updated: 2026-03-02T19:45:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Scene Loads on Play
expected: Press F5 (or Play). The QuickTestScene launches showing a warm-lit 3D environment with a procedural sky (warm horizon tones), directional light casting shadows, and visible ambient lighting. No errors in the Output panel.
result: issue
reported: "Looks good, but ambient light seems to be disabled?"
severity: minor

### 2. Placeholder Ring and Reference Objects
expected: A lavender-colored torus ring lies flat on the XZ plane (like a donut on a table) in the center of the scene. Two small pastel-colored reference boxes are also visible nearby.
result: issue
reported: "I can see the torus lying flat on the XZ plane. But shouldn't it be a flat vinyl like disc? Also cant see the reference objects because they're inside the torus"
severity: major

### 3. Right-Click Drag Orbit
expected: Hold right mouse button and drag to orbit the camera around the center of the scene. On release, the camera continues gliding briefly with momentum before stopping.
result: pass

### 4. Keyboard Orbit
expected: Press WASD or arrow keys to orbit the camera around the scene. Movement is smooth and continuous while keys are held.
result: pass

### 5. Scroll Wheel Zoom
expected: Scroll wheel zooms the camera in and out. Zooming stops at a minimum distance (close to ring) and maximum distance (far overview). The zoom feels smooth, not jerky.
result: issue
reported: "Doesn't work (I'm using a touchpad)"
severity: major

### 6. Idle Auto-Orbit
expected: Stop all input (don't touch mouse or keyboard) and wait about 5 seconds. The camera begins a gentle automatic orbit around the scene. Moving the mouse or pressing a key stops the auto-orbit immediately.
result: pass

### 7. GameEvents Autoload Registered
expected: In the Godot editor, go to Project > Project Settings > Autoload tab. GameEvents should be listed as an autoload pointing to Scripts/Autoloads/GameEvents.cs.
result: pass

### 8. Resource Subclasses in Editor
expected: In the Godot editor FileSystem dock, right-click any folder > New Resource (or use the Resource picker). Search for "Room", "Citizen", "Wish", or "Economy". The custom types RoomDefinition, CitizenData, WishTemplate, and EconomyConfig should appear in the list.
result: issue
reported: "Resources not visible in the editor"
severity: major

## Summary

total: 8
passed: 4
issues: 4
pending: 0
skipped: 0

## Gaps

- truth: "Scene shows warm-lit environment with visible ambient lighting from sky"
  status: failed
  reason: "User reported: Looks good, but ambient light seems to be disabled?"
  severity: minor
  test: 1
  root_cause: "In DefaultEnvironment.tres, ambient_light_source = 1 which maps to AMBIENT_SOURCE_DISABLED in Godot 4. Should be 2 (COLOR) or 3 (SKY). The ambient_light_color, ambient_light_energy, and ambient_light_sky_contribution properties are all configured but completely inert because the source is disabled."
  artifacts:
    - path: "Resources/Environment/DefaultEnvironment.tres"
      issue: "ambient_light_source = 1 (DISABLED) instead of 2 (COLOR) or 3 (SKY)"
  missing:
    - "Change ambient_light_source from 1 to 2 (COLOR) to enable ambient lighting with configured warm color and sky contribution"
  debug_session: ""
- truth: "Placeholder ring is a flat vinyl-like disc shape with reference objects visible nearby"
  status: failed
  reason: "User reported: Torus is donut-shaped, not a flat disc. Reference boxes are inside the torus and not visible."
  severity: major
  test: 2
  root_cause: "TorusMesh produces a round-tube donut shape, not a flat disc/annulus. The torus has inner_radius=3.0, outer_radius=6.0 creating a tube center at 4.5 with tube radius 1.5. Reference boxes at positions (4.5,0.7,0) and (-3.0,0.7,3.5) are both at distance ~4.5 from Y-axis — inside the torus tube volume."
  artifacts:
    - path: "Scenes/QuickTest/QuickTestScene.tscn"
      issue: "TorusMesh creates donut, not flat disc. Box positions inside torus volume."
  missing:
    - "Replace TorusMesh with flat annular disc geometry (CSG subtraction or CylinderMesh approach)"
    - "Move reference boxes to sit on top of the disc surface, outside the ring volume"
  debug_session: ""
- truth: "Scroll zoom works with touchpad two-finger scroll"
  status: failed
  reason: "User reported: Doesn't work (I'm using a touchpad)"
  severity: major
  test: 5
  root_cause: "OrbitalCamera._Input() only handles InputEventMouseButton with WheelUp/WheelDown for zoom. Touchpad two-finger scroll generates InputEventPanGesture in Godot 4, which is never checked. No InputEventPanGesture or InputEventMagnifyGesture handling exists anywhere in the codebase. No keyboard zoom fallback (+/- keys) either."
  artifacts:
    - path: "Scripts/Camera/OrbitalCamera.cs"
      issue: "_Input only handles InputEventMouseButton for zoom, not InputEventPanGesture (touchpad)"
  missing:
    - "Add InputEventPanGesture handler that reads Delta.Y and applies to _targetZoom"
    - "Optionally add InputEventMagnifyGesture for pinch-to-zoom"
    - "Add keyboard zoom fallback (+/- keys)"
  debug_session: ""
- truth: "Custom Resource subclasses (RoomDefinition, CitizenData, WishTemplate, EconomyConfig) appear in editor New Resource dialog"
  status: failed
  reason: "User reported: Resources not visible in the editor"
  severity: major
  test: 8
  root_cause: "Code is correct — all 4 classes have [GlobalClass] and extend Resource. The issue is that .godot/global_script_class_cache.cfg is empty (list=[]). In Godot 4 C#, the editor must be restarted after building the C# solution for [GlobalClass] types to be registered. This is a known Godot C# workflow requirement, not a code bug."
  artifacts:
    - path: ".godot/global_script_class_cache.cfg"
      issue: "Empty cache — editor needs restart after C# build to populate"
  missing:
    - "User action: Build > Build Solution in editor, then close and reopen Godot editor"
    - "If still missing after restart: delete .godot/global_script_class_cache.cfg and reopen editor"
  debug_session: ""
