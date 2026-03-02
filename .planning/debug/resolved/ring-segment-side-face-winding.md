---
status: resolved
trigger: "ring-segment-side-face-winding: Side faces of ring segments wound in wrong direction, invisible due to backface culling"
created: 2026-03-02T00:00:00Z
updated: 2026-03-02T00:03:00Z
---

## Current Focus

hypothesis: CONFIRMED AND VERIFIED - All four side face types had CCW winding but Godot requires CW for front faces
test: Mathematical cross-product analysis + human visual verification in Godot
expecting: N/A - resolved
next_action: N/A - resolved

## Symptoms

expected: Side faces of ring segments should be visible from the outside (camera perspective) -- outer/inner side walls should face outward.
actual: Side faces are completely invisible because they are backface-culled. The winding order makes them face inward instead of outward.
errors: No error messages -- purely a visual/rendering issue.
reproduction: Run the Godot project and look at the ring segments. All side faces (inner walls, outer walls, and radial walls between segments) are invisible.
started: Present since the ring geometry was built in Phase 2.

## Eliminated

## Evidence

- timestamp: 2026-03-02T00:00:30Z
  checked: Top face winding via cross-product analysis
  found: Top face Triangle 1 cross product = (0, -9, 0) = downward, opposite to Up normal. In Godot CW-front convention, cross product points away from viewer above, so top face IS correctly front-facing from above. Matches report that top faces render fine.
  implication: Godot CW-front convention confirmed by working top/bottom faces.

- timestamp: 2026-03-02T00:00:35Z
  checked: Outer curved face winding via cross-product at a0=0, a1=pi/6
  found: Triangle 1 (tOA, tOB, bOB) cross product = (6h, 0, 1.608h) = outward radially. In Godot CW-front convention, cross product should point AWAY from viewer for front face, but this points TOWARD viewer (outward). Therefore BACK-facing from outside.
  implication: Outer face confirmed wrong winding.

- timestamp: 2026-03-02T00:00:40Z
  checked: Inner curved face winding via cross-product
  found: Triangle 1 (tIB, tIA, bIA) cross product = (-0.45, 0, -0.1206) = inward toward center. Inner face normal also points inward, visible from center side. Cross product points TOWARD center-side viewer. BACK-facing in Godot CW convention.
  implication: Inner face confirmed wrong winding.

- timestamp: 2026-03-02T00:00:45Z
  checked: Start radial face winding via cross-product at startAngle=0
  found: Triangle 1 (sTI, sTO, sBO) cross product = (0, 0, -0.9) matches intended normal (0,0,-1). Cross product toward viewer on -Z side. BACK-facing in Godot CW convention.
  implication: Start radial face confirmed wrong winding.

- timestamp: 2026-03-02T00:00:50Z
  checked: End radial face winding via cross-product at endAngle=pi/6
  found: Triangle 1 (eTO, eTI, eBI) cross product = (-0.45, 0, 0.7794) matches sideNormal (-0.5, 0, 0.866). Cross product toward viewer. BACK-facing.
  implication: End radial face confirmed wrong winding.

- timestamp: 2026-03-02T00:00:55Z
  checked: Verified proposed fix - reversed outer face AddQuad(bOA, bOB, tOB, tOA)
  found: Triangle 1 (bOA, bOB, tOB) cross product = (-0.9, 0, -0.2412) = inward. In Godot CW convention, cross product away from outside viewer = front-facing. CORRECT.
  implication: Reversing vertex order fixes the winding for Godot's convention.

- timestamp: 2026-03-02T00:01:30Z
  checked: Verified inner face fix - reversed inner face AddQuad(bIB, bIA, tIA, tIB)
  found: Triangle 1 (bIB, bIA, tIA) cross product = (0.45, 0, 0.1206) = outward. Inner face visible from center; cross product points away from center-side viewer. Front-facing in Godot CW convention. CORRECT.
  implication: All four reversed side face quads verified mathematically correct.

- timestamp: 2026-03-02T00:02:00Z
  checked: Fix applied to RingMeshBuilder.cs, also updated AddQuad docstring from CCW to CW and top face comment
  found: All four side face AddQuad calls reversed (a,b,c,d -> d,c,b,a). Code compiles structurally (dotnet build fails only due to missing Godot SDK in this environment, not code errors).
  implication: Fix is ready for visual verification in Godot editor.

- timestamp: 2026-03-02T00:03:00Z
  checked: Human visual verification in Godot editor
  found: User confirmed "It's fixed now" -- all side faces rendering correctly.
  implication: Fix verified end-to-end.

## Resolution

root_cause: Godot uses clockwise (CW) front-face winding convention, but all four side face types (outer curved, inner curved, start radial, end radial) in RingMeshBuilder were built with counter-clockwise (CCW) winding when viewed from the intended visible direction. This causes all side faces to be back-face culled. Top/bottom faces happened to have the correct CW winding from their visible direction already.
fix: Reversed vertex order (a,b,c,d -> d,c,b,a) in all four side-face AddQuad calls in RingMeshBuilder.cs (lines 64, 71, 86, 101). Also corrected AddQuad docstring and top face comment to say CW instead of CCW.
verification: Mathematical cross-product verification of all 4 faces + human visual confirmation in Godot editor. All side faces now render correctly.
files_changed: [Scripts/Ring/RingMeshBuilder.cs]
