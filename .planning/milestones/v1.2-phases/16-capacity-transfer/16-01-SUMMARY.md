---
phase: 16-capacity-transfer
plan: 01
subsystem: autoloads
tags: [housing, capacity, refactor, single-source-of-truth]

# Dependency graph
requires:
  - phase: 15-housing-manager-core
    provides: HousingManager with TotalCapacity property and capacity tracking
provides:
  - HousingManager as single source of truth for housing capacity
  - Clean HappinessManager with no duplicate capacity state
  - Arrival gating via HousingManager.Instance.TotalCapacity query
affects: [17-sleep-cycle, 18-zzz-indicator, 19-polish]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Cross-singleton query for derived state (HappinessManager queries HousingManager.TotalCapacity)"

key-files:
  created: []
  modified:
    - Scripts/Autoloads/HappinessManager.cs
    - Scripts/Autoloads/SaveManager.cs
    - Scripts/UI/TitleScreen.cs
    - Scripts/Autoloads/HousingManager.cs

key-decisions:
  - "Arrival gating formula: StarterCitizenCapacity + HousingManager.Instance.TotalCapacity (null-safe with ?? 0)"
  - "HousingCapacity removed from SaveData entirely (no save version bump needed -- System.Text.Json ignores unknown properties)"
  - "StateLoaded property removed from HappinessManager (only guarded removed InitializeHousingCapacity)"

patterns-established:
  - "Single source of truth: capacity lives only in HousingManager, queried by others"

requirements-completed: [INFR-03]

# Metrics
duration: 4min
completed: 2026-03-06
---

# Phase 16 Plan 01: Capacity Transfer Summary

**Surgical removal of duplicate housing capacity tracking from HappinessManager, redirecting arrival gating to query HousingManager.Instance.TotalCapacity as single source of truth**

## Performance

- **Duration:** 4 min
- **Started:** 2026-03-06T13:46:58Z
- **Completed:** 2026-03-06T13:51:21Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments
- Removed 103 lines of duplicate capacity tracking from HappinessManager (fields, methods, event subscriptions, startup scan)
- Redirected arrival gating to HousingManager.Instance.TotalCapacity with null-safe fallback
- Cleaned SaveManager (SaveData field, save collection, restore calls) and TitleScreen (StateLoaded reset)
- Net reduction of 110 lines across 4 files with zero compile warnings

## Task Commits

Each task was committed atomically:

1. **Task 1: Remove capacity tracking from HappinessManager and redirect arrival gating** - `9b71c0d` (refactor)
2. **Task 2: Clean up SaveManager and TitleScreen references** - `a8cd91d` (refactor)
3. **Deviation fix: Remove stale cross-reference comment** - `2c309e1` (refactor)

## Files Created/Modified
- `Scripts/Autoloads/HappinessManager.cs` - Removed all capacity-tracking fields, methods, event subscriptions; redirected arrival check to HousingManager
- `Scripts/Autoloads/SaveManager.cs` - Removed HousingCapacity from SaveData, save collection, and restore calls; removed HappinessManager.StateLoaded
- `Scripts/UI/TitleScreen.cs` - Removed HappinessManager.StateLoaded reset from StartNewStation
- `Scripts/Autoloads/HousingManager.cs` - Removed stale doc comment referencing deleted HappinessManager.InitializeHousingCapacity

## Decisions Made
- Arrival gating uses `StarterCitizenCapacity + (HousingManager.Instance?.TotalCapacity ?? 0)` -- null-safe pattern matches existing codebase conventions
- No save format version bump needed -- removing a property from SaveData is backward-compatible (System.Text.Json silently ignores unknown keys when deserializing old saves)
- `using System.Collections.Generic` retained in HappinessManager because `HashSet<string>` is still used for unlocked rooms

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Removed stale cross-reference in HousingManager doc comment**
- **Found during:** Final verification (grep for InitializeHousingCapacity)
- **Issue:** HousingManager.InitializeExistingRooms doc comment said "Same pattern as HappinessManager.InitializeHousingCapacity" but that method no longer exists
- **Fix:** Removed the stale cross-reference line from the XML doc comment
- **Files modified:** Scripts/Autoloads/HousingManager.cs
- **Verification:** Dead code grep now returns zero results
- **Committed in:** 2c309e1

---

**Total deviations:** 1 auto-fixed (1 stale reference)
**Impact on plan:** Trivial doc comment cleanup. No scope creep.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- HousingManager is the single source of truth for housing capacity
- Arrival gating works correctly via cross-singleton query
- Ready for Phase 17 (Sleep Cycle) which builds on HousingManager's home assignment

---
*Phase: 16-capacity-transfer*
*Completed: 2026-03-06*
