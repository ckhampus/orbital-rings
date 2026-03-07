---
phase: 21-integration-test-infrastructure
plan: 01
subsystem: testing
tags: [godot, csharp, singleton, reset, test-infrastructure, state-isolation]

# Dependency graph
requires:
  - phase: 20-test-framework-wiring
    provides: GoDotTest framework, Shouldly assertions, test runner scene
provides:
  - Reset() methods on all 7 game singletons for state isolation between tests
  - ClearAllSubscribers() on GameEvents for zero stale delegate leaks
  - Timer suppression (EconomyManager._incomeTimer, HappinessManager._arrivalTimer, SaveManager._debounceTimer) via Stop() in Reset()
  - Static StateLoaded flag reset on EconomyManager, HousingManager, CitizenManager
affects: [21-02-PLAN, phase-25-integration-tests]

# Tech tracking
tech-stack:
  added: []
  patterns: [singleton-reset-pattern, clear-all-subscribers-pattern]

key-files:
  created: []
  modified:
    - Scripts/Autoloads/EconomyManager.cs
    - Scripts/Autoloads/HousingManager.cs
    - Scripts/Autoloads/HappinessManager.cs
    - Scripts/Autoloads/SaveManager.cs
    - Scripts/Build/BuildManager.cs
    - Scripts/Citizens/CitizenManager.cs
    - Scripts/Autoloads/WishBoard.cs
    - Scripts/Autoloads/GameEvents.cs

key-decisions:
  - "Included _anchorRow reset in BuildManager.Reset() (not in original field catalog) for complete state isolation"
  - "GameEvents has 34 event delegates (not 32) -- RoomPlacementConfirmed and RoomDemolishConfirmed were missed in research count"
  - "BuildManager uses IsInstanceValid() (not Node.IsInstanceValid) since BuildManager extends Node"

patterns-established:
  - "Singleton Reset() pattern: public void Reset() clears mutable state, stops timers, preserves Instance/Config/read-only caches"
  - "ClearAllSubscribers() pattern: null all event delegate fields before singleton resets to prevent cross-singleton side effects"

requirements-completed: [INTG-01, INTG-02, INTG-03]

# Metrics
duration: 3min
completed: 2026-03-07
---

# Phase 21 Plan 01: Singleton Reset Infrastructure Summary

**Reset() methods on 7 singletons and ClearAllSubscribers() on GameEvents (34 event delegates) for test state isolation**

## Performance

- **Duration:** 3 min
- **Started:** 2026-03-07T12:44:42Z
- **Completed:** 2026-03-07T12:47:41Z
- **Tasks:** 2
- **Files modified:** 8

## Accomplishments
- All 7 game singletons (EconomyManager, HousingManager, HappinessManager, SaveManager, BuildManager, CitizenManager, WishBoard) have public Reset() methods that return them to clean "just loaded, no game data" state
- GameEvents has ClearAllSubscribers() that nulls all 34 event delegate fields, organized by category matching the existing code structure
- Three singleton timers (income, arrival, debounce) are stopped by their respective Reset() methods
- Static StateLoaded flags reset to false on EconomyManager, HousingManager, CitizenManager
- Existing smoke test still passes (1 passed, 0 failed) -- no regressions from production code changes

## Task Commits

Each task was committed atomically:

1. **Task 1: Add Reset() to all 7 singletons** - `a68d814` (feat)
2. **Task 2: Add ClearAllSubscribers() to GameEvents** - `d6b58e1` (feat)

## Files Created/Modified
- `Scripts/Autoloads/EconomyManager.cs` - Added Reset() clearing credits, citizen counts, working citizens, tier multiplier, stopping income timer
- `Scripts/Autoloads/HousingManager.cs` - Added Reset() clearing housing dictionaries, delegate refs, StateLoaded
- `Scripts/Autoloads/HappinessManager.cs` - Added Reset() recreating MoodSystem, resetting unlocked rooms to starters, stopping arrival timer
- `Scripts/Autoloads/SaveManager.cs` - Added Reset() clearing pending load, stopping debounce timer, nulling 10 delegate fields
- `Scripts/Build/BuildManager.cs` - Added Reset() clearing build state, placed rooms, freeing ghost mesh if valid
- `Scripts/Citizens/CitizenManager.cs` - Added Reset() clearing citizen list, selection, grid reference, StateLoaded
- `Scripts/Autoloads/WishBoard.cs` - Added Reset() clearing wish dictionaries and WishNudgeRequested event
- `Scripts/Autoloads/GameEvents.cs` - Added ClearAllSubscribers() nulling all 34 event delegate fields

## Decisions Made
- Included `_anchorRow = default` in BuildManager.Reset() even though the plan's field catalog omitted it -- it is mutable state that should be reset for correctness
- GameEvents actually has 34 event delegates, not the 32 cited in the plan and research. RoomPlacementConfirmed and RoomDemolishConfirmed (Placement Feedback Events) were missed in the original count. All 34 are included in ClearAllSubscribers()
- Used `IsInstanceValid()` (inherited from Node) rather than `Node.IsInstanceValid()` in BuildManager.Reset() since BuildManager extends Node

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Added _anchorRow reset to BuildManager.Reset()**
- **Found during:** Task 1 (cross-referencing field names with source code)
- **Issue:** The plan's field catalog for BuildManager listed `_anchorRow` in the research but did not include it in the exact Reset() specification
- **Fix:** Added `_anchorRow = default;` to BuildManager.Reset()
- **Files modified:** Scripts/Build/BuildManager.cs
- **Verification:** dotnet build succeeds, field is properly reset
- **Committed in:** a68d814 (Task 1 commit)

**2. [Rule 1 - Bug] Corrected event delegate count from 32 to 34**
- **Found during:** Task 2 (cross-referencing GameEvents.cs source with plan)
- **Issue:** Plan specified 32 event delegates but actual GameEvents.cs has 34 (RoomPlacementConfirmed and RoomDemolishConfirmed were missed)
- **Fix:** Included all 34 event delegates in ClearAllSubscribers()
- **Files modified:** Scripts/Autoloads/GameEvents.cs
- **Verification:** dotnet build succeeds, all events accounted for
- **Committed in:** d6b58e1 (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (1 missing critical, 1 bug)
**Impact on plan:** Both auto-fixes ensure complete state isolation. No scope creep.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- All singleton Reset() methods and GameEvents.ClearAllSubscribers() are ready for Plan 02 to build TestHelper.ResetAllSingletons() and GameTestClass on top of
- Plan 02 will create Tests/Infrastructure/TestHelper.cs, Tests/Infrastructure/GameTestClass.cs, and verification tests
- No blockers or concerns

## Self-Check: PASSED

All 8 modified files exist. Both task commits (a68d814, d6b58e1) verified in git log. SUMMARY.md created.

---
*Phase: 21-integration-test-infrastructure*
*Completed: 2026-03-07*
