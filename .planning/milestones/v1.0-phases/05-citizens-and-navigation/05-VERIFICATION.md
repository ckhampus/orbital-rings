---
phase: 05-citizens-and-navigation
verified: 2026-03-03T14:30:00Z
status: human_needed
score: 5/5 must-haves verified (automated)
human_verification:
  - test: "Watch 5 citizens on the walkway for 30+ seconds"
    expected: "Distinct body types (Tall/Short/Round) and two-color bands visible; some walk CW, some CCW; speeds differ; no uniform marching; gentle vertical bob; no clipping through room blocks"
    why_human: "Visual appearance, animation quality, and clipping cannot be verified by static code analysis"
  - test: "Place 2-3 rooms on the ring and wait 20-60 seconds"
    expected: "A citizen near a room drifts radially toward the room edge (smooth, not teleport), fades out, reappears after a few seconds, fades back in, drifts back to walkway, and resumes walking. No Z-fighting or flickering during fade. Both inner and outer row rooms visited."
    why_human: "Tween animation sequence, Z-fight absence, and proximity detection correctness require runtime verification"
  - test: "Click a citizen in Normal build mode"
    expected: "A floating panel appears near the citizen showing their name and 'No wish'. The clicked citizen's capsule glows (emission bloom visible via environment post-processing). Clicking elsewhere or pressing Escape dismisses the popup and removes the glow."
    why_human: "Emission bloom visibility depends on environment glow settings and cannot be confirmed from code alone"
  - test: "Enter build mode (select a room type from build panel), then click a citizen"
    expected: "Nothing happens — popup does not appear, no glow is applied"
    why_human: "Build mode suppression requires runtime interaction to confirm"
  - test: "Run the game for 2+ minutes, observe Godot Output panel"
    expected: "No orphan node warnings, no null reference errors, no crashes"
    why_human: "Runtime stability and signal cleanup (SafeNode pattern) require live session observation"
---

# Phase 5: Citizens and Navigation Verification Report

**Phase Goal:** Named citizens with distinct appearances walk the ring walkway continuously and enter rooms based on spatial proximity, with navigation proven by prototype before full implementation
**Verified:** 2026-03-03T14:30:00Z
**Status:** human_needed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | At least 3 named citizens with visually distinct appearances walk the walkway continuously | ? NEEDS HUMAN | 5 citizens spawned (CitizenManager.SpawnStarterCitizens(5)), 3 BodyTypes in CitizenData, CitizenAppearance.CreateCitizenMesh verified substantive — visual confirmation required |
| 2 | Citizens navigate the circular walkway without cutting across the ring interior | ? NEEDS HUMAN | `_currentAngle += _direction * _speed * dt` polar movement confirmed in code — no NavigationAgent3D — but walkway-only constraint verified visually |
| 3 | Citizens visibly drift toward and enter rooms that are proximity-based | ? NEEDS HUMAN | 8-phase tween chain in StartVisit() verified in code with IsOccupied check against SegmentGrid — drift/fade quality requires runtime |
| 4 | Clicking a citizen opens a panel showing their name and wish status | ? NEEDS HUMAN | FindCitizenAtScreenPos, SelectCitizen, CitizenInfoPanel.ShowForCitizen all verified in code — popup appearance and positioning requires runtime |
| 5 | Signal connections cleaned up with zero orphan nodes | ? NEEDS HUMAN | _EnterTree/_ExitTree with SubscribeEvents/UnsubscribeEvents pattern verified in CitizenNode.cs, _activeTween?.Kill() on exit verified — runtime debugger check required |

**Score (automated):** 5/5 truths have verified code backing; all 5 require human confirmation for visual/behavioral criteria

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Scripts/Autoloads/GameEvents.cs` | CitizenClicked, CitizenEnteredRoom, CitizenExitedRoom events | VERIFIED | All 3 events declared (lines 143, 146, 149) with Emit helpers (lines 157-164). Substantive, wired via CitizenNode and CitizenManager. |
| `Scripts/Citizens/CitizenNames.cs` | Static name pool with 26 diverse real-world names and GetNextName() | VERIFIED | 26 names A-Z present (lines 11-17), GetNextName() modulo wrap at line 28. Wired via CitizenManager.SpawnStarterCitizens. |
| `Scripts/Citizens/CitizenAppearance.cs` | Static helper creating CapsuleMesh with body type proportions and two-color band | VERIFIED | CreateCitizenMesh() creates primary + band capsule with per-instance StandardMaterial3D. GetCapsuleHeight() exported for Y positioning. Wired via CitizenNode.Initialize(). |
| `Scripts/Citizens/CitizenNode.cs` | SafeNode-derived walking citizen with angle-based polar movement and vertical bob | VERIFIED | Extends Node3D with manual _EnterTree/_ExitTree lifecycle. _currentAngle field present. Polar movement in _Process, bob via _bobPhase. Contains _visitTimer, StartVisit, 8-phase tween chain. |
| `Scripts/Citizens/CitizenManager.cs` | Autoload singleton managing citizen list, spawning 5 starters, setting EconomyManager citizen count | VERIFIED | CitizenManager.Instance set in _Ready(). SpawnStarterCitizens(5) called. EconomyManager.Instance?.SetCitizenCount(_citizens.Count) called. Wired as Autoload in project.godot. |
| `Scripts/UI/CitizenInfoPanel.cs` | Screen-space popup showing citizen name and wish placeholder | VERIFIED | ShowForCitizen() sets _nameLabel.Text and _wishLabel.Text="No wish". Viewport-clamped positioning via camera.UnprojectPosition. Wired via CitizenManager.SelectCitizen. |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `CitizenManager.cs` | `CitizenNode.cs` | Spawns CitizenNode instances as children | WIRED | `var citizen = new CitizenNode()` at line 302, `AddChild(citizen)` at line 306 |
| `CitizenManager.cs` | `EconomyManager.cs` | SetCitizenCount call on spawn | WIRED | `EconomyManager.Instance?.SetCitizenCount(_citizens.Count)` at line 103 |
| `CitizenNode.cs` | `CitizenAppearance.cs` | Creates visual mesh from CitizenData | WIRED | `_meshContainer = CitizenAppearance.CreateCitizenMesh(data)` at line 165 |
| `CitizenManager.cs` | `GameEvents.cs` | Emits CitizenArrived on spawn | WIRED | `GameEvents.Instance?.EmitCitizenArrived(data.CitizenName)` at line 310 |
| `CitizenNode.cs` | `SegmentGrid.cs` | Proximity check via IsOccupied | WIRED | `_grid.IsOccupied(row, pos)` at line 270 in OnVisitTimerTimeout. SegmentGrid passed via Initialize(). |
| `CitizenManager.cs` | `CitizenInfoPanel.cs` | Shows panel on citizen click detection | WIRED | `_infoPanel = new CitizenInfoPanel()` at line 111, `_infoPanel.ShowForCitizen(citizen, mousePos)` at line 245 |
| `CitizenManager.cs` | `BuildManager.cs` | Checks CurrentMode to suppress clicks during build modes | WIRED | `BuildManager.Instance?.CurrentMode != BuildMode.Normal` at line 161 |
| `CitizenNode.cs` | `GameEvents.cs` | Emits CitizenEnteredRoom/CitizenExitedRoom during visits | WIRED | `GameEvents.Instance?.EmitCitizenEnteredRoom(citizenName)` at line 334, `EmitCitizenExitedRoom` at line 345 |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| CTZN-01 | 05-01, 05-03 | Named citizens with distinct appearances arrive at the station as happiness grows | SATISFIED | 5 named citizens spawned with 3 body types (Tall/Short/Round), 8-color curated palette, two-color CapsuleMesh. CitizenNames.GetNextName() assigns sequential unique names. |
| CTZN-02 | 05-01, 05-03 | Citizens walk along the walkway visibly | SATISFIED | Angle-based polar movement: `_currentAngle += _direction * _speed * dt` with random CW/CCW direction, +/-15% speed variation, desynchronized bob. WalkwayRadius=4.5 (centerline). |
| CTZN-03 | 05-02, 05-03 | Citizens visit rooms based on spatial attraction (gravitating toward relevant rooms) | SATISFIED | OnVisitTimerTimeout iterates 24 segments via SegmentGrid.IsOccupied, finds nearest within VisitProximityThreshold (1.5 segment arcs). StartVisit 8-phase tween: drift + fade-out + wait + fade-in + drift-back. |
| CTZN-04 | 05-02, 05-03 | Player can click a citizen to see their name and current wish | SATISFIED | FindCitizenAtScreenPos via ray-plane intersection, SelectCitizen enables EmissionEnabled+glow, CitizenInfoPanel.ShowForCitizen displays name + "No wish". Build mode suppressed. Click-away and Escape dismiss. |

No orphaned requirements found. All CTZN-01 through CTZN-04 are claimed by plans 05-01 and 05-02, confirmed completed in 05-03 summary.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `Scripts/UI/CitizenInfoPanel.cs` | 76 | `_wishLabel.Text = "No wish"` placeholder | INFO | Intentional — locked decision per plan, designed to be replaced in Phase 6. Not a blocker. |

No blocker anti-patterns found. The `return null` instances in CitizenNode.cs (line 442) and CitizenManager.cs (lines 189, 195) are proper null-guard returns, not stubs.

### Human Verification Required

#### 1. Citizen Appearance and Walking Quality

**Test:** Open QuickTestScene in Godot and run. Watch all 5 citizens on the walkway for 30+ seconds.
**Expected:** At least 3 visually distinct body types (tall/thin, short/stubby, round/wide) with two-color bands. Some citizens walk clockwise, others counter-clockwise. Speeds are noticeably different. A gentle vertical bob is visible. Citizens do not clip through room blocks placed on the ring.
**Why human:** Visual appearance, animation smoothness, and physical clipping require runtime observation.

#### 2. Room Visit Animation

**Test:** Place 2-3 rooms on the ring (any segments). Wait 20-60 seconds watching a citizen near a room.
**Expected:** The citizen drifts radially outward (outer room) or inward (inner room) smoothly over ~0.5s, then fades out over ~0.3s, stays invisible for 4-8 seconds, fades back in, and drifts back to the walkway centerline before resuming normal walking. No Z-fighting or flickering during the fade. Both inner-row and outer-row rooms should be visitable.
**Why human:** Tween animation quality, fade artifact absence, and 8-phase sequence correctness require live execution.

#### 3. Click-to-Inspect Panel and Emission Glow

**Test:** Click on a walking citizen in Normal mode (no room type selected in build panel).
**Expected:** A dark semi-transparent floating panel appears near the citizen showing their name (e.g., "Aria") and "No wish". The clicked citizen's capsule glows visibly with bloom emission. Click elsewhere — panel disappears and glow is removed. Press Escape — same dismissal behavior.
**Why human:** Screen-space panel positioning, emission glow bloom strength, and interaction flow require runtime testing.

#### 4. Build Mode Suppression

**Test:** Select a room type in the build panel (entering Placing mode), then click on a citizen.
**Expected:** Nothing happens — no panel, no glow. Exit build mode — clicking citizens works again.
**Why human:** Build mode priority behavior requires runtime interaction to confirm.

#### 5. Runtime Stability and Signal Cleanup

**Test:** Run QuickTestScene for 2+ minutes. Observe the Godot Output panel throughout.
**Expected:** No orphan node warnings, no null reference errors, no crashes, no error spam.
**Why human:** Signal lifecycle correctness and runtime stability require live session observation in the Godot debugger.

### Gaps Summary

No automated gaps found. The build passes cleanly (0 warnings, 0 errors). All 5 artifacts are substantive and wired. All 8 key links are confirmed. All 4 requirements are covered.

The phase status is `human_needed` because Phase 5's success criteria (ROADMAP lines 100-104) include visual and behavioral outcomes that cannot be verified by static code analysis: citizen appearance distinctiveness, walking smoothness, animation quality, and runtime stability. The human verification checkpoint (Plan 05-03) documents that these were approved, but as a code verifier this cannot be confirmed from file content alone.

---

_Verified: 2026-03-03T14:30:00Z_
_Verifier: Claude (gsd-verifier)_
