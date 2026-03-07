---
phase: 25-singleton-integration-tests
plan: 01
subsystem: testing
tags: [godot, integration-tests, event-subscription, singleton, test-infrastructure]

# Dependency graph
requires:
  - phase: 21-integration-test-infrastructure
    provides: GameTestClass, TestHelper.ResetAllSingletons, singleton Reset() methods
provides:
  - SubscribeToEvents() on HousingManager, HappinessManager, EconomyManager
  - SeedRoomForTest() on HousingManager for headless room seeding
  - TestHelper.ResubscribeAllSingletons() orchestrator
  - GameTestClass [Setup] auto re-subscribes events after reset
affects: [25-02-PLAN, singleton-integration-tests]

# Tech tracking
tech-stack:
  added: []
  patterns: [event-resubscription-after-reset, headless-room-seeding]

key-files:
  modified:
    - Scripts/Autoloads/HousingManager.cs
    - Scripts/Autoloads/HappinessManager.cs
    - Scripts/Autoloads/EconomyManager.cs
    - Tests/Infrastructure/TestHelper.cs
    - Tests/Infrastructure/GameTestClass.cs

key-decisions:
  - "SubscribeToEvents() mirrors _Ready() event wiring for idempotent re-subscription"
  - "SeedRoomForTest() bypasses BuildManager dependency for headless room state"
  - "ResubscribeAllSingletons() called in GameTestClass [Setup] for automatic integration test support"

patterns-established:
  - "SubscribeToEvents pattern: public method mirrors _Ready() event subscriptions for test re-wiring"
  - "SeedRoomForTest pattern: test-only direct dictionary population bypassing scene dependencies"

requirements-completed: []

# Metrics
duration: 2min
completed: 2026-03-07
---

# Phase 25 Plan 01: Event Re-subscription Infrastructure Summary

**SubscribeToEvents() on 3 singletons, SeedRoomForTest() on HousingManager, and auto re-subscription in GameTestClass [Setup] via TestHelper.ResubscribeAllSingletons()**

## Performance

- **Duration:** 2 min
- **Started:** 2026-03-07T16:25:14Z
- **Completed:** 2026-03-07T16:27:28Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments
- Added SubscribeToEvents() to HousingManager, HappinessManager, and EconomyManager to restore event wiring after ClearAllSubscribers()
- Added SeedRoomForTest(anchorIndex, capacity) to HousingManager for headless room state seeding without BuildManager
- Added TestHelper.ResubscribeAllSingletons() orchestrator and updated GameTestClass [Setup] to call it automatically
- All 77 existing tests pass with zero regressions

## Task Commits

Each task was committed atomically:

1. **Task 1: Add SubscribeToEvents() to singletons and SeedRoomForTest() to HousingManager** - `513e367` (feat)
2. **Task 2: Add ResubscribeAllSingletons() to TestHelper and update GameTestClass [Setup]** - `2d96285` (feat)

## Files Created/Modified
- `Scripts/Autoloads/HousingManager.cs` - Added SubscribeToEvents() and SeedRoomForTest() methods
- `Scripts/Autoloads/HappinessManager.cs` - Added SubscribeToEvents() method
- `Scripts/Autoloads/EconomyManager.cs` - Added SubscribeToEvents() method
- `Tests/Infrastructure/TestHelper.cs` - Added ResubscribeAllSingletons() static method
- `Tests/Infrastructure/GameTestClass.cs` - Updated [Setup] to call ResubscribeAllSingletons() after ResetAllSingletons()

## Decisions Made
- SubscribeToEvents() mirrors _Ready() event wiring exactly (same delegate assignments and subscriptions) for idempotent re-subscription
- SeedRoomForTest() directly populates _housingRoomCapacities and _roomOccupants dictionaries, bypassing BuildManager's visual scene dependency
- ResubscribeAllSingletons() placed in GameTestClass [Setup] (not opt-in per test) since re-subscribing with no active events is harmless

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Event re-subscription infrastructure is ready for Plan 02 (integration tests)
- SeedRoomForTest() enables headless housing tests without BuildManager
- GameTestClass auto-reset + auto-resubscribe means integration tests get clean state with working event chains

## Self-Check: PASSED

All 5 modified files verified on disk. Both task commits (513e367, 2d96285) verified in git log.

---
*Phase: 25-singleton-integration-tests*
*Completed: 2026-03-07*
