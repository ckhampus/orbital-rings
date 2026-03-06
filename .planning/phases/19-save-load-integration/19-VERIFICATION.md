---
phase: 19-save-load-integration
verified: 2026-03-06T22:47:51Z
status: passed
score: 5/5 must-haves verified
re_verification: false
human_verification:
  - test: "Save, quit, reload — citizen walks to home room"
    expected: "After loading, each citizen's HomeSegmentIndex is restored. Within the home timer interval, the citizen initiates a home return tween to the correct segment."
    why_human: "Timer-driven animation sequence; cannot verify firing behavior programmatically."
  - test: "Load a v2 save file (no HomeSegmentIndex field in JSON) — citizen gets assigned fresh"
    expected: "All citizens start unhoused (HomeSegmentIndex == null), then AssignAllUnhoused runs and assigns them to available rooms as if they just arrived."
    why_human: "Requires a real v2 save file and a running game session to observe the behavior end-to-end."
  - test: "Demolish a room after saving, then reload — displaced citizen is reassigned"
    expected: "On load, the stale homeIndex fails ContainsKey check, citizen is logged as unhoused, and AssignAllUnhoused assigns them to a different room."
    why_human: "Requires manual save-demolish-quit-reload sequence and log inspection."
---

# Phase 19: Save/Load Integration Verification Report

**Phase Goal:** Housing assignments survive save-quit-load cycles, old saves load cleanly, and stale assignments are corrected automatically
**Verified:** 2026-03-06T22:47:51Z
**Status:** PASSED
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|---------|
| 1 | Every citizen retains their home room assignment after save-quit-load | VERIFIED | `SaveManager.CollectGameState` line 335 writes `HomeSegmentIndex = HousingManager.Instance?.GetHomeForCitizen(citizenData.CitizenName)` per citizen. `ApplySceneState` lines 464-468 passes `(c.Name, c.HomeSegmentIndex)` tuples to `HousingManager.Instance.RestoreFromSave`. `RestoreFromSave` calls `AssignCitizen` which sets `CitizenNode.HomeSegmentIndex`, triggering `EnsureHomeTimer`. |
| 2 | Loading a v2 save results in all citizens starting unhoused then being assigned from scratch | VERIFIED | `SavedCitizen.HomeSegmentIndex` is `int?` (line 74 of SaveManager.cs). System.Text.Json deserializes absent JSON fields to `null` on nullable types. `RestoreFromSave` line 194: `if (homeIndex == null || homeIndex.Value < 0) continue` — all citizens skipped. `AssignAllUnhoused()` line 207 then assigns them from scratch. |
| 3 | A stale assignment (demolished room) causes citizen to be unhoused and reassigned on load | VERIFIED | `RestoreFromSave` lines 197-200: `if (!_housingRoomCapacities.ContainsKey(homeIndex.Value))` — logs stale reference and skips. `InitializeExistingRooms(assignCitizens: false)` at line 188 populates capacity dict from actual BuildManager rooms (not save data), so demolished rooms are absent. `AssignAllUnhoused()` line 207 catches these citizens. |
| 4 | Home return timer restarts after load (citizens resume home-return cycle) | VERIFIED | `CitizenNode.HomeSegmentIndex` setter (lines 159-168): `if (value != null && IsInsideTree()) EnsureHomeTimer()`. `EnsureHomeTimer` (lines 772-795) creates the timer if absent or restarts it if stopped. Called from `AssignCitizen` → `citizenNode.HomeSegmentIndex = anchorIndex` even during `_isRestoring = true` (property setter is unconditional, not event-gated). |
| 5 | No autosave loop fires during restore (events suppressed) | VERIFIED | `_isRestoring = true` set at line 190, cleared at line 209. `AssignCitizen` line 368: `if (!_isRestoring)` guards `EmitCitizenAssignedHome`. `OnRoomDemolished` line 279: `if (!_isRestoring)` guards `EmitCitizenUnhoused`. `SpawnCitizenFromSave` (CitizenManager.cs lines 372-401) does NOT emit `CitizenArrived` — confirmed by absence of `EmitCitizenArrived` call. `EmitHousingStateChanged()` fires once at end of `RestoreFromSave` (line 211). |

**Score:** 5/5 truths verified

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Scripts/Autoloads/SaveManager.cs` | Save/load orchestration with housing assignment round-trip | VERIFIED | `CollectGameState` reads `GetHomeForCitizen` per citizen (line 335). `ApplyState` sets `HousingManager.StateLoaded = true` (line 396). `ApplySceneState` calls `RestoreFromSave` after rooms and citizens restored (lines 462-468). `SavedCitizen.HomeSegmentIndex` is `int?` (line 74). |
| `Scripts/Autoloads/HousingManager.cs` | RestoreFromSave with stale reference detection and AssignAllUnhoused fallback | VERIFIED | `RestoreFromSave` present at line 182. XML doc comments added in Phase 19 document all three verified paths (lines 177-181). Stale reference detection: ContainsKey check lines 197-200. `AssignAllUnhoused()` fallback at line 207. |
| `Scripts/Citizens/CitizenNode.cs` | HomeSegmentIndex setter triggers EnsureHomeTimer on load | VERIFIED | Setter at lines 159-168. `EnsureHomeTimer()` called when `value != null && IsInsideTree()`. `EnsureHomeTimer` at lines 772-795 creates timer lazily (fetches `HousingManager.Instance?.Config` if `_housingConfig` was null) and starts it. |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `SaveManager.cs` | `HousingManager.GetHomeForCitizen` | `CollectGameState` reads home for each citizen | WIRED | Line 335: `HomeSegmentIndex = HousingManager.Instance?.GetHomeForCitizen(citizenData.CitizenName)` — confirmed present |
| `SaveManager.cs` | `HousingManager.RestoreFromSave` | `ApplySceneState` passes assignment tuples | WIRED | Lines 464-468: assignments list built from `data.Citizens`, `RestoreFromSave` called. Ordering correct: rooms restored before citizens before housing. |
| `HousingManager.cs` | `CitizenNode.HomeSegmentIndex` | `AssignCitizen` sets property, triggers EnsureHomeTimer | WIRED | `AssignCitizen` line 366: `citizenNode.HomeSegmentIndex = anchorIndex`. Property setter unconditionally calls `EnsureHomeTimer()` when non-null and in tree. |

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|---------|
| INFR-04 | 19-01-PLAN.md | Save/load housing assignments with backward compatibility (v2 saves load as unhoused) | SATISFIED | All three code paths verified: normal round-trip, v2 null-deserialization, stale reference detection. Marked complete in REQUIREMENTS.md. |
| INFR-05 | 19-01-PLAN.md | Save format bumped to v3 with nullable HomeSegmentIndex | SATISFIED | `SaveData.Version = 3` (line 22). `SavedCitizen.HomeSegmentIndex` is `int?` with v3 doc comment (line 74). Marked complete in REQUIREMENTS.md spanning Phase 14 (schema) and Phase 19 (wiring audit). |

No orphaned requirements found. Both IDs declared in the plan are confirmed in REQUIREMENTS.md as Phase 19 deliverables.

---

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `Scripts/UI/TitleScreen.cs` | 221-223 | `StartNewStation()` resets `CitizenManager.StateLoaded` and `EconomyManager.StateLoaded` but NOT `HousingManager.StateLoaded` | Info | No current runtime impact — Autoloads do not re-run `_Ready()` on `ChangeSceneToFile`. The missing reset is a latent inconsistency that would matter if HousingManager's `_Ready()` were ever re-triggered. Noted for awareness, not blocking. |

No stub implementations, placeholder returns, or TODO/FIXME comments found in the three audited files.

---

### Human Verification Required

#### 1. Normal Save/Load Round-trip

**Test:** Play game with citizens housed. Save (autosave fires). Quit to title. Continue. Observe citizens.
**Expected:** Each citizen resumes on the walkway. Within the home timer window (HousingConfig.HomeTimerMin to HomeTimerMax seconds), each citizen walks toward and enters their assigned home segment.
**Why human:** Timer-driven tween animation cannot be verified programmatically.

#### 2. v2 Backward Compatibility

**Test:** Create a save JSON file with no `HomeSegmentIndex` fields on citizens (v2 format). Load it via Continue.
**Expected:** All citizens start unhoused. `AssignAllUnhoused()` assigns them to available housing rooms. No crash. Log shows no stale reference warnings.
**Why human:** Requires a crafted test save file and live game session.

#### 3. Stale Reference Detection

**Test:** House citizens, save. Open save.json and manually change a citizen's `HomeSegmentIndex` to a value not matching any placed room (or demolish that room before saving). Reload.
**Expected:** Log shows "Housing: Stale home reference for [name] at segment [N] -- citizen is unhoused". Citizen is reassigned to a valid room.
**Why human:** Requires manual save file manipulation and log inspection.

---

### Gaps Summary

No gaps found. All five observable truths are verified against actual code. All three artifacts exist, are substantive, and are wired. Both requirements (INFR-04, INFR-05) are satisfied by the implementation. The only anti-pattern noted (missing `HousingManager.StateLoaded = false` in `StartNewStation`) has no current runtime impact and is informational only.

The phase was an audit of pre-existing implementation from Phases 14, 15, and 17. The audit correctly found no broken code and added XML documentation to `RestoreFromSave` as the primary deliverable. Commit `3482acc` is present and contains the expected 5-line addition to `HousingManager.cs`.

---

_Verified: 2026-03-06T22:47:51Z_
_Verifier: Claude (gsd-verifier)_
