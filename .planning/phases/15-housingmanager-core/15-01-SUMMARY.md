---
phase: 15-housingmanager-core
plan: 01
subsystem: infra
tags: [godot, housing, assignment-engine, autoload, capacity-tracking, events]

# Dependency graph
requires:
  - phase: 14-housing-foundation
    provides: "HousingManager skeleton, GameEvents housing events, SavedCitizen.HomeSegmentIndex, HousingConfig resource"
provides:
  - "Full HousingManager assignment engine with citizen-to-room mapping"
  - "ComputeCapacity static method: BaseCapacity + (segmentCount - 1)"
  - "GetHomeForCitizen, GetOccupantCount, GetOccupants, TotalCapacity, TotalHoused API"
  - "RestoreFromSave for save/load integration without event emission"
  - "Extended BuildManager.GetPlacedRoom with SegmentCount in return tuple"
  - "CitizenNode.HomeSegmentIndex nullable property for convenience cache"
affects: [15-02-save-integration, 16-capacity-transfer, 17-zzz-visual, 18-ui-panels]

# Tech tracking
tech-stack:
  added: []
  patterns: [housing-assignment-engine, fewest-occupants-first, reservoir-sampling-tiebreak, parallel-capacity-tracking]

key-files:
  created: []
  modified:
    - Scripts/Autoloads/HousingManager.cs
    - Scripts/Build/BuildManager.cs
    - Scripts/Citizens/CitizenNode.cs

key-decisions:
  - "HousingManager.StateLoaded flag added for save/load guard (mirrors HappinessManager pattern)"
  - "Delegate references stored for clean event unsubscription (not inline lambdas)"
  - "FindCitizenNode helper iterates CitizenManager.Citizens list (O(n) acceptable for small citizen counts)"

patterns-established:
  - "Housing assignment engine: Dictionary-based capacity tracking with fewest-occupants-first assignment"
  - "Reservoir sampling for random tiebreak in FindBestRoom (GD.Randi() % tieCount)"
  - "RestoreFromSave with _isRestoring flag to suppress event emission during save restoration"

requirements-completed: [HOME-01, HOME-02, HOME-03, HOME-04, HOME-05, INFR-01]

# Metrics
duration: 3min
completed: 2026-03-06
---

# Phase 15 Plan 01: HousingManager Core Summary

**Full citizen-to-room assignment engine with fewest-occupants-first logic, size-scaled capacity (BaseCapacity + segments - 1), demolish reassignment, and event-driven integration**

## Performance

- **Duration:** 3 min
- **Started:** 2026-03-06T09:56:40Z
- **Completed:** 2026-03-06T09:59:41Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- Extended BuildManager.GetPlacedRoom to expose SegmentCount in the return tuple (backward-compatible, all callers use named property access)
- Added CitizenNode.HomeSegmentIndex nullable property as convenience cache for housing assignment
- Implemented full HousingManager assignment engine (454 lines) with Dictionary-based capacity tracking, event subscriptions, and oldest-first assignment/reassignment logic

## Task Commits

Each task was committed atomically:

1. **Task 1: Extend BuildManager.GetPlacedRoom to include SegmentCount and add CitizenNode.HomeSegmentIndex** - `304759b` (feat)
2. **Task 2: Implement full HousingManager assignment engine** - `33e23e2` (feat)

## Files Created/Modified
- `Scripts/Build/BuildManager.cs` - Extended GetPlacedRoom return type from 3-field to 4-field tuple (added SegmentCount)
- `Scripts/Citizens/CitizenNode.cs` - Added nullable HomeSegmentIndex property for housing assignment cache
- `Scripts/Autoloads/HousingManager.cs` - Full assignment engine replacing Phase 14 skeleton with 454 lines of logic

## Decisions Made
- Added StateLoaded flag to HousingManager (mirrors HappinessManager pattern) for save/load guard on InitializeExistingRooms
- Used stored delegate references for event subscription/unsubscription instead of inline lambdas (cleaner cleanup)
- FindCitizenNode iterates CitizenManager.Citizens list (O(n) acceptable for small citizen counts, avoids maintaining a separate dictionary)

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- HousingManager assignment engine complete and compiling
- RestoreFromSave API ready for Plan 02 (SaveManager integration)
- GetHomeForCitizen API ready for Plan 02 (CollectGameState)
- StateLoaded flag ready for Plan 02 (SaveManager sets it before scene transition)
- No blockers or concerns

## Self-Check: PASSED

All 3 modified files verified present. Both task commits (304759b, 33e23e2) verified in git log.

---
*Phase: 15-housingmanager-core*
*Completed: 2026-03-06*
