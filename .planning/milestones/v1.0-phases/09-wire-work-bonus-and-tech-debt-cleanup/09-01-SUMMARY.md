---
phase: 09-wire-work-bonus-and-tech-debt-cleanup
plan: 01
subsystem: economy
tags: [events, economy, work-bonus, citizen-tracking, hud]

# Dependency graph
requires:
  - phase: 05-citizen-system
    provides: "CitizenNode room visit events, CitizenManager.CitizenCount"
  - phase: 03-economy-engine
    provides: "EconomyManager income formula, CreditHUD tooltip"
  - phase: 04-build-system
    provides: "BuildManager.GetPlacedRoom, RoomDefinition.RoomCategory"
provides:
  - "End-to-end work bonus economy flow: citizen room visits -> working count -> income bonus"
  - "CitizenEnteredRoom/ExitedRoom events carry flatSegmentIndex"
  - "EconomyManager tracks working citizens via HashSet for race-condition safety"
  - "CreditHUD tooltip displays actual citizen count"
affects: [economy, citizens, hud]

# Tech tracking
tech-stack:
  added: []
  patterns: ["HashSet tracking for event-driven count with safe removal on exit"]

key-files:
  created: []
  modified:
    - "Scripts/Autoloads/GameEvents.cs"
    - "Scripts/Citizens/CitizenNode.cs"
    - "Scripts/Autoloads/EconomyManager.cs"
    - "Scripts/UI/CreditHUD.cs"

key-decisions:
  - "HashSet<string> for working citizen tracking solves demolished-room race condition"
  - "Exit handler checks set membership, not room existence, for safe decrement"

patterns-established:
  - "Event-driven count tracking via HashSet with add-on-enter/remove-on-exit pattern"

requirements-completed: [ECON-03]

# Metrics
duration: 2min
completed: 2026-03-04
---

# Phase 9 Plan 1: Wire Work Bonus and Tech Debt Cleanup Summary

**Work bonus wired end-to-end: citizen room visits carry segment index, EconomyManager tracks working citizens via HashSet, income formula applies bonus, CreditHUD tooltip reads live citizen count**

## Performance

- **Duration:** 2 min
- **Started:** 2026-03-04T13:06:22Z
- **Completed:** 2026-03-04T13:08:24Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments
- Citizen room visit events now carry flatSegmentIndex, enabling room category lookup
- EconomyManager subscribes to room events and maintains working citizen count via HashSet (race-condition safe)
- Income formula work bonus term activates when citizens visit Work category rooms
- CreditHUD tooltip displays actual citizen count instead of hardcoded 0
- Dead CitizenDeparted event removed (zero subscribers, zero emitters)

## Task Commits

Each task was committed atomically:

1. **Task 1: Enhance event signatures and wire EconomyManager subscriber** - `3d67503` (feat)
2. **Task 2: Fix CreditHUD tooltip citizen count** - `c39cd4d` (fix)

## Files Created/Modified
- `Scripts/Autoloads/GameEvents.cs` - Enhanced CitizenEnteredRoom/ExitedRoom with flatSegmentIndex, removed dead CitizenDeparted
- `Scripts/Citizens/CitizenNode.cs` - Updated emit calls to pass _visitTargetSegment
- `Scripts/Autoloads/EconomyManager.cs` - Added event subscriptions, HashSet tracking, OnCitizenEnteredRoom/ExitedRoom handlers, IsWorkRoom helper, RestoreCredits cleanup
- `Scripts/UI/CreditHUD.cs` - Fixed tooltip to read CitizenManager.Instance.CitizenCount

## Decisions Made
- HashSet<string> tracking for working citizens solves the demolished-room race condition identified in research: on exit, we check set membership (was citizen counted?) rather than room existence (does room still exist?)
- Exit handler uses `_workingCitizens.Remove()` return value to conditionally decrement, avoiding double-decrement bugs

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- ECON-03 requirement satisfied: work bonus economy flow is complete
- Ready for 09-02 plan (remaining tech debt cleanup)

## Self-Check: PASSED

All files verified present. All commits verified in git log.

---
*Phase: 09-wire-work-bonus-and-tech-debt-cleanup*
*Completed: 2026-03-04*
