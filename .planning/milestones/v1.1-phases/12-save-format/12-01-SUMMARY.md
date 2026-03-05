---
phase: 12-save-format
plan: 01
subsystem: save-system
tags: [save-format, versioning, backward-compat, mood-system, autosave]

# Dependency graph
requires:
  - phase: 10-happiness-core-and-mood-tiers
    provides: "MoodSystem with Mood/Baseline/MoodTier, LifetimeWishes counter, MoodTierChanged + WishCountChanged events"
  - phase: 11-economy-and-arrival-tier-integration
    provides: "EconomyManager.SetMoodTier, tier-space economy, arrival probability by tier"
provides:
  - "SaveData v2 with LifetimeHappiness, Mood, MoodBaseline fields"
  - "Version-gated ApplyState (v2 full restore, v1 backward-compat)"
  - "HappinessManager.RestoreState 6-parameter signature"
  - "HappinessManager.MoodBaseline public property"
  - "Autosave triggers on MoodTierChanged + WishCountChanged"
affects: [13-hud-replacement]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Version-gated save restore: data.Version >= 2 selects new code path, else backward-compat defaults"
    - "POCO default fields for safe v1 deserialization: int/float default to 0/0f"

key-files:
  created: []
  modified:
    - "Scripts/Autoloads/SaveManager.cs"
    - "Scripts/Autoloads/HappinessManager.cs"

key-decisions:
  - "Happiness shim retained (deprecated) for HappinessBar until Phase 13 removes it"
  - "SaveData.Version default stays 1 for safe v1 deserialization; CollectGameState sets Version=2 explicitly"
  - "v2 fields use C# default values (0/0f) so v1 saves deserialize safely without migration"

patterns-established:
  - "Version-gated restore: if (data.Version >= N) { newPath } else { backwardCompatPath }"

requirements-completed: [SAVE-01]

# Metrics
duration: 3min
completed: 2026-03-05
---

# Phase 12 Plan 01: Save Format Summary

**SaveData v2 persists lifetime happiness, mood, and mood baseline with version-gated restore and rewired autosave triggers**

## Performance

- **Duration:** 3 min
- **Started:** 2026-03-05T19:49:26Z
- **Completed:** 2026-03-05T19:52:18Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- SaveData POCO extended with v2 fields (LifetimeHappiness, Mood, MoodBaseline) that default safely for v1 deserialization
- CollectGameState writes Version=2 with all three happiness values from HappinessManager
- ApplyState uses version-gated branching: v2 passes all fields, v1 backward-compat passes (0, oldHappiness, 0f)
- Autosave rewired from dead HappinessChanged to MoodTierChanged + WishCountChanged events

## Task Commits

Each task was committed atomically:

1. **Task 1: SaveData v2 fields, CollectGameState, version-gated ApplyState, and expanded RestoreState** - `2f60202` (feat)
2. **Task 2: Autosave event rewiring** - `d13d3d4` (feat)

## Files Created/Modified
- `Scripts/Autoloads/SaveManager.cs` - SaveData v2 fields, CollectGameState writes Version=2, version-gated ApplyState, autosave events rewired to MoodTierChanged + WishCountChanged
- `Scripts/Autoloads/HappinessManager.cs` - MoodBaseline property added, RestoreState expanded to 6-parameter signature, Happiness shim marked deprecated

## Decisions Made
- **Happiness shim retained:** Plan called for removing the Happiness shim, but HappinessBar.cs (Phase 13 target) references it. Kept as deprecated with XMLDoc pointing to Phase 13 removal. SaveManager no longer uses it.
- **Version default stays 1:** SaveData.Version defaults to 1 in the class so v1 JSON (which has no Version field) deserializes correctly. CollectGameState explicitly sets Version=2.
- **v1 backward-compat path:** v1 saves pass (0, data.Happiness, 0f) to the new RestoreState, preserving the old behavior of mapping the happiness float to mood with zero lifetime and baseline.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Retained Happiness shim for HappinessBar.cs**
- **Found during:** Task 1 (removing Happiness shim per plan)
- **Issue:** HappinessBar.cs line 116 references `HappinessManager.Instance.Happiness`, causing build failure when shim was removed
- **Fix:** Kept Happiness property with updated XMLDoc marking it deprecated and pointing to Phase 13 removal. SaveManager no longer references it (uses Mood/MoodBaseline/LifetimeWishes directly).
- **Files modified:** Scripts/Autoloads/HappinessManager.cs
- **Verification:** Build succeeds with 0 errors and 0 warnings
- **Committed in:** 2f60202 (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Necessary to avoid breaking HappinessBar. No scope creep -- the shim is a no-op pass-through that Phase 13 will remove along with HappinessBar itself.

## Issues Encountered
None beyond the deviation documented above.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Save system fully updated for v2 format with backward compatibility
- Phase 13 (HUD replacement) can proceed to remove HappinessBar and the deprecated Happiness shim
- HappinessChanged event in GameEvents.cs remains for HappinessBar until Phase 13

## Self-Check: PASSED

All files verified present. All commit hashes verified in git log.

---
*Phase: 12-save-format*
*Completed: 2026-03-05*
