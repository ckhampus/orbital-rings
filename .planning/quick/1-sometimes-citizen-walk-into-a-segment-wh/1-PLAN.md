---
phase: quick
plan: 1
type: execute
wave: 1
depends_on: []
files_modified:
  - Scripts/Citizens/CitizenNode.cs
autonomous: true
requirements: [BUGFIX-VISIT-ANGLE]

must_haves:
  truths:
    - "Citizen walks angularly to the target room's segment before drifting radially"
    - "Citizen never drifts into a segment that has no room"
    - "Visit animation looks natural: walk along walkway, then drift inward/outward"
  artifacts:
    - path: "Scripts/Citizens/CitizenNode.cs"
      provides: "Fixed visit animation with angular walk phase"
      contains: "segMidAngle"
  key_links:
    - from: "OnVisitTimerTimeout"
      to: "StartVisit"
      via: "passes target segment index and row"
      pattern: "StartVisit.*bestSegment"
---

<objective>
Fix citizen room visit animation so citizens walk to the target segment's angular position before drifting radially into the room, instead of drifting at their current angle into a potentially empty segment.

Purpose: Citizens currently drift radially at their current angle when visiting a room, but the room may be up to 1.5 segments away angularly. This causes them to visually enter an empty segment.

Output: Updated CitizenNode.cs with corrected visit animation sequence.
</objective>

<execution_context>
@/home/node/.claude/get-shit-done/workflows/execute-plan.md
@/home/node/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@Scripts/Citizens/CitizenNode.cs
@Scripts/Ring/SegmentGrid.cs
</context>

<tasks>

<task type="auto">
  <name>Task 1: Fix visit animation to walk to target segment before radial drift</name>
  <files>Scripts/Citizens/CitizenNode.cs</files>
  <action>
The bug is in StartVisit() and OnVisitTimerTimeout(). When a citizen decides to visit a room:

1. OnVisitTimerTimeout finds the best segment (bestSegment) and calls StartVisit(bestRow)
2. StartVisit captures `float angle = _currentAngle` and uses that fixed angle for the entire radial drift
3. But the target room may be up to 1.5 segments away, so the citizen drifts into the wrong segment

Fix by modifying the visit system:

A. Change StartVisit signature to accept the target segment position:
   `private void StartVisit(SegmentRow targetRow, int targetPosition)`

B. In StartVisit, compute the target segment's mid-angle:
   `float targetAngle = SegmentGrid.GetStartAngle(targetPosition) + SegmentGrid.SegmentArc * 0.5f;`

C. Add an angular walk phase BEFORE the radial drift in the tween sequence. Insert a new Phase 0 before the existing Phase 1 (drift):
   - Compute the shortest angular distance and direction from _currentAngle to targetAngle (handle Tau wraparound)
   - Calculate walk duration based on angular distance / _speed (use existing walking speed so it looks natural)
   - Use TweenMethod to animate from currentAngle to targetAngle, calling SetRadialPosition(interpolatedAngle, WalkwayRadius, includeBob: true) at each step
   - IMPORTANT: For the angular interpolation, handle the wraparound case. If the shortest path crosses the 0/Tau boundary, interpolate through the wrapped direction. Use a float parameter t from 0 to 1 and compute: `float a = currentAngle + shortestDelta * t; if (a < 0) a += Mathf.Tau; if (a >= Mathf.Tau) a -= Mathf.Tau;`
   - Clamp minimum walk duration to 0.1f to avoid zero-length tweens when citizen is already at the segment

D. Update _currentAngle to targetAngle after the walk phase completes (so the citizen resumes walking from the room's position after the visit). Add a TweenCallback after the walk phase:
   `_currentAngle = targetAngle;`

E. Change all subsequent phases to use targetAngle instead of the captured `angle` variable. Specifically:
   - Phase 1 (drift to room): SetRadialPosition(targetAngle, ...)
   - Phase 5 (show at target radius): SetRadialPosition(targetAngle, targetRadius)
   - Phase 7 (drift back): SetRadialPosition(targetAngle, ...)

F. Update the call site in OnVisitTimerTimeout to pass the position:
   Currently: `StartVisit(bestRow);`
   Change to extract position from bestSegment: `var (_, bestPos) = SegmentGrid.FromIndex(bestSegment);` then `StartVisit(bestRow, bestPos);`

Do NOT change the visit timer logic, proximity threshold, wish-aware distance weighting, or any other behavior. Only the animation targeting is being fixed.
  </action>
  <verify>
    <automated>cd /workspace && dotnet build 2>&1 | tail -5</automated>
  </verify>
  <done>Citizens walk angularly along the walkway to the target segment's mid-angle before drifting radially into the room. The _currentAngle is updated so they resume walking from the room position. Build succeeds with no errors.</done>
</task>

</tasks>

<verification>
- `dotnet build` compiles without errors
- StartVisit now accepts targetPosition parameter
- Tween sequence includes angular walk phase before radial drift
- _currentAngle is updated to targetAngle after walk phase
- All radial drift phases use targetAngle, not the old _currentAngle
</verification>

<success_criteria>
Citizens always visually walk to the correct segment before entering a room. No citizen drifts radially into an empty segment. Build compiles cleanly.
</success_criteria>

<output>
After completion, create `.planning/quick/1-sometimes-citizen-walk-into-a-segment-wh/1-SUMMARY.md`
</output>
