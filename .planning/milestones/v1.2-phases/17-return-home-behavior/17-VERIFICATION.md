---
phase: 17-return-home-behavior
verified: 2026-03-06T15:31:31Z
status: passed
score: 8/8 must-haves verified
re_verification: false
---

# Phase 17: Return Home Behavior Verification Report

**Phase Goal:** Implement return-home behavior for housed citizens — periodic timer, walk-to-home tween, rest indicator, wish timer pausing, priority handling, and demolish-eject safety.
**Verified:** 2026-03-06T15:31:31Z
**Status:** passed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Housed citizens periodically walk to their home room segment every 90-150 seconds | VERIFIED | `_homeTimer` created in `Initialize()` with `HousingConfig.HomeTimerMin/Max` (90-150s defaults); started in `_Ready()` when `HomeSegmentIndex != null`; `OnHomeTimerTimeout` calls `StartHomeReturn()` and re-arms via `RearmHomeTimer()` |
| 2 | A "Zzz" Label3D appears above the home segment during 8-15 second rest with fade in/out and gentle bob | VERIFIED | `CreateZzzLabel()` creates `Label3D` with `Billboard.Enabled`, purple color (`0.6f, 0.55f, 0.9f`), `TopLevel=true`; `StartZzzBob()` creates looping sine tween (+/- 0.03f at 1.5s); fade in/out tweens both `modulate:a` and `outline_modulate:a` in parallel; rest duration drawn from `RestDurationMin/Max` (8-15s) |
| 3 | Citizens do not generate new wishes while resting at home (wish timer paused) | VERIFIED | Phase 3 callback in `StartHomeReturn()`: `_wishTimer?.Stop()`; Phase 5 callback resumes: `_wishTimer?.Start()` after rest ends |
| 4 | Citizens with an active wish skip home return when the home timer fires | VERIFIED | `OnHomeTimerTimeout()`: `if (_currentWish != null) { RearmHomeTimer(); return; }` — rearmed not discarded |
| 5 | Citizens walking home abort and resume normal behavior if a wish generates mid-walk | VERIFIED | `OnWishTimerTimeout()`: `if (_walkingToHome) AbortHomeReturn();`; `_walkingToHome` is true from `StartHomeReturn()` start until Phase 0 completion callback sets it false |
| 6 | Citizens resting at home are ejected immediately if their home is demolished | VERIFIED | `OnRoomDemolished()` checks `HomeSegmentIndex.Value == segmentIndex` and `_isAtHome || _walkingToHome`, then calls `EjectFromHome()`; subscribed via stored delegate `_onRoomDemolished` |
| 7 | Unhoused citizens have no home timer and never attempt home returns | VERIFIED | `_homeTimer` only created in `Initialize()` when `housingConfig != null`; timer only started in `_Ready()` when `HomeSegmentIndex != null`; `OnHomeTimerTimeout()` guards `if (HomeSegmentIndex == null) return` |
| 8 | Wish badge is hidden during the entire home return cycle | VERIFIED | `StartHomeReturn()` hides badge before tween begins: `if (_wishBadge != null) _wishBadge.Visible = false;`; restored in Phase 8 callback and in `AbortHomeReturn()` |

**Score:** 8/8 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Scripts/Autoloads/GameEvents.cs` | `CitizenEnteredHome` and `CitizenExitedHome` events with emit helpers | VERIFIED | Lines 253-263: both events, both emit helpers present with XML doc comments matching Phase 14 pattern |
| `Scripts/Citizens/CitizenNode.cs` | Home return behavior: timer, tween, Zzz label, guards, abort, eject | VERIFIED | 1159 lines; contains `StartHomeReturn`, `AbortHomeReturn`, `EjectFromHome`, `CreateZzzLabel`, `RemoveZzzLabel`, `StartZzzBob`, `OnHomeTimerTimeout`, `RearmHomeTimer`, `OnCitizenAssignedHome`, `OnCitizenUnhoused`, `OnRoomDemolished` |
| `Scripts/Citizens/CitizenManager.cs` | HousingConfig pass-through and IsAtHome auto-deselect | VERIFIED | `SpawnCitizen()` line 325 and `SpawnCitizenFromSave()` line 387 both pass `HousingManager.Instance?.Config`; `_Process()` line 163 guards `IsVisiting || IsAtHome`; `FindCitizenAtScreenPos()` line 225 skips `IsVisiting || IsAtHome` |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `CitizenNode.cs` | `HousingConfig.cs` | `Initialize` optional parameter | WIRED | Signature `Initialize(CitizenData, float, SegmentGrid, HousingConfig housingConfig = null)`; used to create `_homeTimer` with `housingConfig.HomeTimerMin/Max` |
| `CitizenNode.cs` | `GameEvents.cs` | `EmitCitizenEnteredHome`/`EmitCitizenExitedHome` in tween callbacks | WIRED | Phase 3 callback: `GameEvents.Instance?.EmitCitizenEnteredHome(citizenName, homeSegment)`; Phase 5 callback: `EmitCitizenExitedHome` |
| `CitizenNode.cs` | `HousingManager.cs` (via `GameEvents.RoomDemolished`) | `RoomDemolished` event subscription for eject | WIRED | `SubscribeEvents()` stores `_onRoomDemolished = OnRoomDemolished` and subscribes; `UnsubscribeEvents()` unsubscribes cleanly |
| `CitizenManager.cs` | `CitizenNode.cs` | `IsAtHome` check in `_Process` auto-deselect | WIRED | `_Process()`: `if (_selectedCitizen != null && (_selectedCitizen.IsVisiting || _selectedCitizen.IsAtHome))` |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| BEHV-01 | 17-01-PLAN.md | Housed citizens periodically return to their home room (90-150s cycle) | SATISFIED | `_homeTimer` with `HousingConfig.HomeTimerMin=90` / `HomeTimerMax=150`; `StartHomeReturn()` initiates angular walk |
| BEHV-02 | 17-01-PLAN.md | Home rest lasts 8-15s with a "Zzz" FloatingText indicator | SATISFIED | `restDuration` drawn from `RestDurationMin=8` / `RestDurationMax=15`; `Label3D` Zzz indicator with billboard, purple color, fade in/out, bob |
| BEHV-03 | 17-01-PLAN.md | Citizen wish timer pauses during home rest | SATISFIED | `_wishTimer?.Stop()` in Phase 3 callback (citizen hidden); `_wishTimer?.Start()` in Phase 5 callback (citizen reappearing) |
| BEHV-04 | 17-01-PLAN.md | Home return is lower priority than active wish fulfillment | SATISFIED | `OnHomeTimerTimeout()` skips and rearms if `_currentWish != null`; `OnWishTimerTimeout()` aborts walk-to-home if `_walkingToHome` |

All four BEHV requirements fully satisfied. No orphaned phase-17 requirements in REQUIREMENTS.md.

### Anti-Patterns Found

None. No TODO/FIXME/HACK markers, no empty implementations, no stub returns found in any of the three modified files.

### Human Verification Required

The following behaviors require runtime observation in the Godot editor and cannot be verified statically:

#### 1. Zzz Label3D Visual Appearance

**Test:** Place a housing room, assign a citizen, reduce `HomeTimerMin`/`HomeTimerMax` to 5-10s in the HousingConfig Inspector, wait for a home return.
**Expected:** "Zzz" label appears above the home room in soft purple, fades in over ~0.5s, gently bobs up and down, then fades out before citizen reappears.
**Why human:** Billboard rendering, color fidelity, and animation smoothness cannot be verified without running the renderer.

#### 2. End-to-End Home Return Sequence

**Test:** Same setup. Observe citizen behavior over a full home-return cycle.
**Expected:** Citizen walks along walkway arc toward home segment, drifts inward/outward to room edge, fades out, rests with Zzz, reappears, drifts back to walkway, resumes walking. No visual glitches or stuck states.
**Why human:** Tween sequence correctness (direction, radius, timing) requires visual inspection.

#### 3. Mid-Walk Wish Abort

**Test:** Reduce wish timer min to ~2s. Watch a citizen begin walking home, then observe if a wish generates mid-walk.
**Expected:** Citizen stops walking home, restores immediately to walkway position, wish badge appears, citizen resumes normal behavior.
**Why human:** Race condition timing between wish generation and home walk phase cannot be reliably tested statically.

#### 4. Demolish-Eject During Rest

**Test:** While a citizen is resting at home (invisible, Zzz showing), demolish the housing room.
**Expected:** Zzz disappears immediately, citizen reappears at the home segment position on the walkway, resumes normal behavior without errors.
**Why human:** Requires coordinated demolish action during a specific tween phase.

### Build Verification

- `dotnet build`: **0 errors, 0 warnings** — project compiles cleanly
- `dotnet format --verify-no-changes` (via solution file): **passed** — no formatting drift
- Commits verified in git history: `0bb3c9e`, `481d2db`, `b33cd12`

### Gaps Summary

No gaps. All 8 observable truths verified against the codebase. All 3 required artifacts exist and are substantive and wired. All 4 key links confirmed. All 4 requirements (BEHV-01 through BEHV-04) satisfied with direct code evidence. No anti-patterns detected. Build is clean.

---

_Verified: 2026-03-06T15:31:31Z_
_Verifier: Claude (gsd-verifier)_
