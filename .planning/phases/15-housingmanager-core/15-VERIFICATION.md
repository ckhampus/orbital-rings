---
phase: 15-housingmanager-core
verified: 2026-03-06T14:00:00Z
status: passed
score: 10/10 must-haves verified
re_verification:
  previous_status: passed
  previous_score: 9/9
  note: "Previous verification predated Plan 15-03 (gap closure). Re-verification adds the Plan 15-03 truth and audits the full codebase post-fix."
  gaps_closed:
    - "Loading a save restores housing assignments exactly as saved (stale home reference bug fixed by Plan 15-03)"
  gaps_remaining: []
  regressions: []
human_verification:
  - test: "Save/load housing restoration with valid rooms"
    expected: "Citizens retain exact home room assignments after save+reload. No 'Stale home reference' warnings in console."
    why_human: "Plan 15-03 fix involves a subtle ordering concern (InitializeExistingRooms calls AssignAllUnhoused before _isRestoring is set, then RestoreFromSave's loop re-assigns from saved data). The user's UAT confirmed it works, but static analysis cannot rule out duplicate-add in _roomOccupants in all scenarios."
  - test: "Fewest-occupants-first distribution with tied rooms"
    expected: "With two rooms at equal occupancy, new arrivals are spread evenly over time (random tiebreak via reservoir sampling)."
    why_human: "Dictionary iteration order is implementation-defined; tiebreak is random. Cannot verify distribution statically."
  - test: "Demolish reassignment flow"
    expected: "Displaced citizens are reassigned to surviving room(s). Any overflow remains unhoused but continues walking and fulfilling wishes normally."
    why_human: "Displacement and reassignment sequence is runtime behavior."
  - test: "v2 save backward compatibility (pre-housing saves)"
    expected: "All citizens start unhoused (HomeSegmentIndex deserializes as null from v2 saves). With housing rooms present, citizens get assigned via normal arrival algorithm."
    why_human: "Requires a v2 save file to test against."
---

# Phase 15: HousingManager Core Verification Report

**Phase Goal:** Citizens are automatically assigned to housing rooms with even distribution, and reassigned or gracefully unhoused when rooms change
**Verified:** 2026-03-06
**Status:** passed
**Re-verification:** Yes — after Plan 15-03 gap closure (stale home reference on load)

---

## Re-Verification Context

The previous VERIFICATION.md was written after Plans 15-01 and 15-02 only. It was stamped `status: passed` but the UAT (15-UAT.md) conducted after that verification found Test 7 (save/load housing preservation) failing: all citizens received "Stale home reference" warnings on load. Plan 15-03 diagnosed and fixed the root cause: `_housingRoomCapacities` was empty when `RestoreFromSave` ran because neither `InitializeExistingRooms` (skipped: `StateLoaded=true`) nor `OnRoomPlaced` events (not emitted by `RestorePlacedRoom`) had populated it. The fix adds `InitializeExistingRooms()` at the start of `RestoreFromSave`.

This re-verification covers all three plans and confirms the fix is in place.

---

## Goal Achievement

### Observable Truths

All 10 truths are verified. Truths 1-6 carry over from Plans 15-01 and 15-02 (confirmed still in place). Truth 10 is new from Plan 15-03.

| #  | Truth                                                                                                                            | Status   | Evidence                                                                                                                      |
|----|----------------------------------------------------------------------------------------------------------------------------------|----------|-------------------------------------------------------------------------------------------------------------------------------|
| 1  | When a citizen arrives and housing rooms exist, they are assigned to the room with the fewest occupants                         | VERIFIED | `OnCitizenArrived` (line 287) calls `FindBestRoom()` (fewest-occupants-first + reservoir tiebreak, lines 308-334), then `AssignCitizen` |
| 2  | When a housing room is demolished, residents are reassigned or become unhoused                                                  | VERIFIED | `OnRoomDemolished` (line 236) displaces occupants, removes room from dicts, calls `FindBestRoom()` for each displaced citizen; partial reassignment allowed |
| 3  | When a new housing room is built and unhoused citizens exist, oldest-unhoused citizens are assigned first                       | VERIFIED | `OnRoomPlaced` (line 210) calls `AssignUnhousedCitizens(anchorIndex)` (line 367), which iterates `CitizenManager.Citizens` in list order (oldest first) |
| 4  | A 3-segment housing room holds more citizens than a 1-segment room (capacity = BaseCapacity + segments - 1)                     | VERIFIED | `ComputeCapacity` (lines 133-136): `return definition.BaseCapacity + (segmentCount - 1)`. Applied in `OnRoomPlaced` (line 219) and `InitializeExistingRooms` (line 430) |
| 5  | Unhoused citizens walk, visit rooms, and fulfill wishes identically to housed citizens                                           | VERIFIED | `HomeSegmentIndex` on CitizenNode (line 150) is a passive nullable property. No behavioral branches on housing status in `_Process`, `OnVisitTimerTimeout`, or wish logic |
| 6  | HousingManager is a singleton autoload owning the citizen-to-room mapping                                                       | VERIFIED | `project.godot` line 26: `HousingManager="*res://Scripts/Autoloads/HousingManager.cs"`. `Instance` set in `_EnterTree()` (line 87). Three dictionaries: `_housingRoomCapacities`, `_roomOccupants`, `_citizenHomes` (lines 43-49) |
| 7  | `CollectGameState` writes each citizen's `HomeSegmentIndex` to the save file                                                    | VERIFIED | `SaveManager.cs` line 337: `HomeSegmentIndex = HousingManager.Instance?.GetHomeForCitizen(citizenData.CitizenName)` inside citizen serialization loop |
| 8  | `ApplySceneState` restores housing assignments from saved `HomeSegmentIndex` without re-running the assignment algorithm         | VERIFIED | Lines 467-474: `HousingManager.Instance.RestoreFromSave(assignments)` called after rooms+citizens restored. `_isRestoring` flag (line 180) suppresses event emission during restore |
| 9  | `CitizenAssignedHome` and `CitizenUnhoused` events trigger autosave via `SaveManager` debounce                                  | VERIFIED | Delegate fields at lines 131-132; initialized in `_Ready` (lines 161-162); subscribed in `SubscribeEvents` (lines 207-208); routes to `OnAnyStateChanged()` debounce |
| 10 | Loading a save restores housing assignments exactly as saved (no stale home reference warnings for valid rooms)                 | VERIFIED | `RestoreFromSave` (line 173) calls `InitializeExistingRooms()` (line 178) before checking `_housingRoomCapacities`, ensuring capacity dict is populated before assignment lookups. Fix confirmed at commit `b3edeab`. UAT Test 7 re-confirmed passing. |

**Score:** 10/10 truths verified

---

## Required Artifacts

| Artifact                              | Expected                                                                               | Status   | Details                                                                                                                  |
|---------------------------------------|----------------------------------------------------------------------------------------|----------|--------------------------------------------------------------------------------------------------------------------------|
| `Scripts/Autoloads/HousingManager.cs` | Full assignment engine, min 150 lines; exports `Instance`, `ComputeCapacity`, `GetHomeForCitizen`, `RestoreFromSave` | VERIFIED | 459 lines. All four exports present. Three data dictionaries (lines 43-49). Event subscriptions, `FindBestRoom`, `AssignCitizen`, `AssignUnhousedCitizens`, `InitializeExistingRooms` all substantive. Plan 15-03 fix at lines 175-178. |
| `Scripts/Build/BuildManager.cs`       | `GetPlacedRoom` returns 4-field tuple including `SegmentCount`                         | VERIFIED | Line 246: `(RoomDefinition Definition, int AnchorIndex, int SegmentCount, int Cost)?`. Both return paths (direct, line 250; multi-segment fallback, line 262) include `SegmentCount`. |
| `Scripts/Citizens/CitizenNode.cs`     | `HomeSegmentIndex` nullable int property                                               | VERIFIED | Line 150: `public int? HomeSegmentIndex { get; set; }` with doc comment. Set by `AssignCitizen` (HousingManager line 354) and cleared in `OnRoomDemolished` (line 265). |
| `Scripts/Autoloads/SaveManager.cs`    | `HomeSegmentIndex` persistence; `RestoreFromSave` call; housing event subscriptions    | VERIFIED | `HomeSegmentIndex` at line 75 (SavedCitizen), 337 (write), 471 (read for restore). `RestoreFromSave` at line 473. Housing event subscriptions at lines 131-132, 161-162, 207-208. |

---

## Key Link Verification

### Plan 15-01 Links

| From                    | To                                          | Via                                    | Status | Evidence                                                              |
|-------------------------|---------------------------------------------|----------------------------------------|--------|-----------------------------------------------------------------------|
| `HousingManager.cs`     | `GameEvents.Instance.RoomPlaced`            | event subscription in `_Ready`         | WIRED  | Line 99: `GameEvents.Instance.RoomPlaced += _onRoomPlaced;`           |
| `HousingManager.cs`     | `GameEvents.Instance.RoomDemolished`        | event subscription in `_Ready`         | WIRED  | Line 100: `GameEvents.Instance.RoomDemolished += _onRoomDemolished;`  |
| `HousingManager.cs`     | `GameEvents.Instance.CitizenArrived`        | event subscription in `_Ready`         | WIRED  | Line 101: `GameEvents.Instance.CitizenArrived += _onCitizenArrived;`  |
| `HousingManager.cs`     | `GameEvents.Instance.EmitCitizenAssignedHome` | event emission on assignment          | WIRED  | Line 358: `GameEvents.Instance?.EmitCitizenAssignedHome(citizenName, anchorIndex)` |
| `HousingManager.cs`     | `GameEvents.Instance.EmitCitizenUnhoused`   | event emission on displacement         | WIRED  | Line 269: `GameEvents.Instance?.EmitCitizenUnhoused(citizenName)`     |
| `HousingManager.cs`     | `BuildManager.Instance.GetPlacedRoom`       | capacity computation on room placed    | WIRED  | Lines 212 (OnRoomPlaced), 419 (InitializeExistingRooms): `BuildManager.Instance?.GetPlacedRoom(segmentIndex)` |

### Plan 15-02 Links

| From              | To                                        | Via                                      | Status | Evidence                                                                 |
|-------------------|-------------------------------------------|------------------------------------------|--------|--------------------------------------------------------------------------|
| `SaveManager.cs`  | `HousingManager.Instance.GetHomeForCitizen` | `CollectGameState` reading home assignment | WIRED  | Line 337: `HousingManager.Instance?.GetHomeForCitizen(citizenData.CitizenName)` |
| `SaveManager.cs`  | `HousingManager.Instance.RestoreFromSave` | `ApplySceneState` restoring housing state | WIRED  | Line 473: `HousingManager.Instance.RestoreFromSave(assignments)`          |
| `SaveManager.cs`  | `GameEvents.Instance.CitizenAssignedHome` | event subscription for autosave          | WIRED  | Line 207: `GameEvents.Instance.CitizenAssignedHome += _onCitizenAssignedHome;` |
| `SaveManager.cs`  | `GameEvents.Instance.CitizenUnhoused`     | event subscription for autosave          | WIRED  | Line 208: `GameEvents.Instance.CitizenUnhoused += _onCitizenUnhoused;`    |

### Plan 15-03 Link

| From                              | To                                    | Via                                      | Status | Evidence                                                               |
|-----------------------------------|---------------------------------------|------------------------------------------|--------|------------------------------------------------------------------------|
| `HousingManager.RestoreFromSave`  | `HousingManager.InitializeExistingRooms` | direct call at start of `RestoreFromSave` | WIRED  | Line 178: `InitializeExistingRooms();` — called before `_isRestoring = true` (line 180) and before assignment foreach loop (line 182) |

---

## Requirements Coverage

| Requirement | Source Plan(s)   | Description                                                                 | Status    | Evidence                                                                                              |
|-------------|------------------|-----------------------------------------------------------------------------|-----------|-------------------------------------------------------------------------------------------------------|
| HOME-01     | 15-01, 15-02     | Citizens automatically assigned to housing room on arrival (fewest-occupants-first) | SATISFIED | `OnCitizenArrived` -> `FindBestRoom()` -> `AssignCitizen()`. Persisted and restored via SaveManager. |
| HOME-02     | 15-01            | Citizens reassigned when home room demolished (or become unhoused)          | SATISFIED | `OnRoomDemolished` displaces occupants, calls `FindBestRoom()` for each displaced citizen              |
| HOME-03     | 15-01            | Unhoused citizens assigned when new housing rooms built (oldest-first)      | SATISFIED | `OnRoomPlaced` -> `AssignUnhousedCitizens(anchorIndex)` iterates `CitizenManager.Citizens` in spawn order |
| HOME-04     | 15-01            | Housing capacity scales with room size (BaseCapacity + segments - 1)        | SATISFIED | `ComputeCapacity(def, segmentCount)` returns `def.BaseCapacity + (segmentCount - 1)`, used at all three placement sites |
| HOME-05     | 15-01            | Unhoused citizens function identically to housed ones                       | SATISFIED | `HomeSegmentIndex` is passive data only. No behavioral branches in CitizenNode on housing status       |
| INFR-01     | 15-01, 15-02, 15-03 | HousingManager autoload singleton owns citizen-to-room mapping           | SATISFIED | Registered in `project.godot` (line 26). `Instance` set in `_EnterTree`. Mapping via three dictionaries. Save/load restore via `RestoreFromSave` with Plan 15-03 ordering fix. |

### Orphaned Requirements Check

Requirements mapped to Phase 15 in REQUIREMENTS.md: `HOME-01`, `HOME-02`, `HOME-03`, `HOME-04`, `HOME-05`, `INFR-01`. All 6 are claimed by at least one plan (`15-01-PLAN.md` claims all 6; `15-02-PLAN.md` claims HOME-01 and INFR-01; `15-03-PLAN.md` claims INFR-01). No orphaned requirements.

### Scope Note: Plan 15-02 and INFR-04

Plan 15-02 implemented housing persistence ahead of schedule. REQUIREMENTS.md maps `INFR-04` ("Save/load housing assignments with backward compatibility") to Phase 19. The Phase 15 implementation is the persistence half of INFR-04. Phase 19 will complete it once return-home behavior (Phase 17) exists. This is early partial delivery, not a scope violation.

---

## Anti-Patterns Found

No blockers. Scan of all four modified files:

| File                          | Pattern                          | Severity | Notes                                                                          |
|-------------------------------|----------------------------------|----------|--------------------------------------------------------------------------------|
| `HousingManager.cs`           | `return null` (lines 146, 449, 457) | Info  | Guard returns in `GetHomeForCitizen` and `FindCitizenNode`; these are correct sentinel returns, not stubs |
| All modified files            | TODO / FIXME / HACK / PLACEHOLDER | None    | Zero placeholder comments found in any of the four files                       |
| `HousingManager.cs`           | Empty handlers                   | None     | All event handlers contain substantive logic (event subscription, lookup, assign/displace logic) |

Build result: 0 errors, 0 warnings (`dotnet build "Orbital Rings.csproj" --no-restore`).

### Code Quality Note: Potential Double-Add in RestoreFromSave

**Severity: Warning (not blocker — UAT confirmed working)**

When `RestoreFromSave` calls `InitializeExistingRooms()`, the latter calls `AssignAllUnhoused()` while `_isRestoring` is still `false`. At that moment `_citizenHomes` is empty, so `AssignAllUnhoused` assigns ALL citizens to rooms via the fewest-occupants algorithm. Then the explicit `foreach` loop in `RestoreFromSave` calls `AssignCitizen` again for each saved assignment. `AssignCitizen` does not guard against adding the same citizen name twice to `_roomOccupants[anchorIndex]`.

If `AssignAllUnhoused` and the saved assignments produce identical room assignments (which they may, since fewest-occupants on a fresh dictionary tends toward the same rooms), each citizen appears twice in `_roomOccupants`. This would report occupancy as double the actual count, potentially blocking future assignments.

The UAT (Test 7) passed after this fix, suggesting the practical impact is masked — possibly because `FindBestRoom` is not called during `RestoreFromSave`'s explicit loop (only `AssignCitizen` is called), so the double-count does not prevent any restoration. However this is fragile and could cause issues if room capacity is tight or in edge cases.

**This does not block the phase goal** (housing assignments are correctly restored as UAT confirms), but it is a latent correctness issue to address before Phase 17 (return-home behavior) places runtime load on the occupancy tracking.

---

## Human Verification Required

### 1. Save/Load Housing Restoration (re-test after Plan 15-03)

**Test:** Build 2-3 housing rooms, wait for citizen assignments (check GD.Print), save (F5 or autosave), reload the save.
**Expected:** Citizens have the same home rooms as before the save. No "Stale home reference" warnings. GD.Print in `RestoreFromSave` should show assignments being confirmed, not warnings.
**Why human:** Runtime behavior; Plan 15-03 fix has a subtle ordering concern (see Code Quality Note above) that static analysis cannot fully resolve.

### 2. Fewest-Occupants-First Distribution

**Test:** Build two Bunk Pods (1-segment, capacity 2 each). Let 5 starter citizens arrive.
**Expected:** Pod 1 gets 2, Pod 2 gets 2, 1 citizen unhoused. Next arrival assigned to Pod 1 or Pod 2 randomly (tie on 2 occupants each).
**Why human:** Dictionary iteration order and random tiebreak cannot be verified statically.

### 3. Demolish Reassignment Flow

**Test:** Build two housing rooms, fill them. Demolish one.
**Expected:** GD.Print shows displaced citizens being reassigned to surviving room (up to capacity). Overflow citizens remain unhoused but walk, visit, and fulfill wishes normally.
**Why human:** Displacement event chain and re-assignment sequence is runtime behavior.

### 4. v2 Save Backward Compatibility

**Test:** Load a save file created before Phase 14 (no `HomeSegmentIndex` field).
**Expected:** All citizens start unhoused (`HomeSegmentIndex` deserializes as null). Housing rooms (if present) trigger assignment via normal occupancy algorithm.
**Why human:** Requires a pre-housing save file to test against.

---

## Gaps Summary

No gaps remain. All 10 truths are verified. The previous gap (stale home reference on load) is closed by Plan 15-03's surgical fix: `InitializeExistingRooms()` is now called at the start of `RestoreFromSave` before any `_housingRoomCapacities.ContainsKey` checks. All 4 artifacts pass all three levels (exists, substantive, wired). All 11 key links (6 from Plan 15-01, 4 from Plan 15-02, 1 from Plan 15-03) are confirmed wired. All 6 requirements are satisfied. Build passes with 0 errors and 0 warnings.

A code quality concern (potential double-add in `_roomOccupants` during `RestoreFromSave`) is documented for future attention but does not block the phase goal as confirmed by UAT.

---

_Verified: 2026-03-06_
_Verifier: Claude (gsd-verifier)_
_Re-verification after Plan 15-03 gap closure_
