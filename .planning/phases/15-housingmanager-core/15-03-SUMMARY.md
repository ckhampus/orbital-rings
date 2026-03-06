---
phase: 15-housingmanager-core
plan: 03
subsystem: housing
tags: [godot, save-load, housing-manager, bug-fix]

# Dependency graph
requires:
  - phase: 15-01
    provides: "HousingManager with InitializeExistingRooms and RestoreFromSave methods"
  - phase: 15-02
    provides: "SaveManager integration calling HousingManager.RestoreFromSave"
provides:
  - "Fixed save/load housing restoration -- citizens retain home assignments across saves"
affects: [phase-16, phase-17]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Re-initialize cached dictionaries before restore (populate-before-check pattern)"

key-files:
  created: []
  modified:
    - Scripts/Autoloads/HousingManager.cs

key-decisions:
  - "InitializeExistingRooms called before _isRestoring (not after) to populate capacities for ContainsKey check"

patterns-established:
  - "Populate-before-check: when RestoreFromSave depends on cached data, call the initializer explicitly since normal population paths are bypassed during load"

requirements-completed: [INFR-01]

# Metrics
duration: 1min
completed: 2026-03-06
---

# Phase 15 Plan 03: Gap Closure -- Stale Home Reference on Load Summary

**Fixed save/load housing by calling InitializeExistingRooms before RestoreFromSave checks _housingRoomCapacities**

## Performance

- **Duration:** 1 min
- **Started:** 2026-03-06T10:37:52Z
- **Completed:** 2026-03-06T10:39:03Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments
- Fixed the root cause of UAT test 7 failure: _housingRoomCapacities was empty when RestoreFromSave ran
- Added InitializeExistingRooms() call at top of RestoreFromSave, before _isRestoring flag
- Surgical 5-line fix (3 comment lines + 1 call + 1 blank line), no other files modified

## Task Commits

Each task was committed atomically:

1. **Task 1: Populate housing room capacities at start of RestoreFromSave** - `b3edeab` (fix)

## Files Created/Modified
- `Scripts/Autoloads/HousingManager.cs` - Added InitializeExistingRooms() call at start of RestoreFromSave to populate capacity dictionary before assignment lookups

## Decisions Made
- Called InitializeExistingRooms before _isRestoring=true (not after), because the AssignAllUnhoused call inside InitializeExistingRooms is harmless at that point (_citizenHomes is still empty, so no citizens are "unhoused" yet)
- Did NOT modify BuildManager.RestorePlacedRoom to emit events -- targeted fix avoids ripple effects on other systems

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- NuGet restore required before build (Godot.SourceGenerators not cached) -- resolved with `dotnet restore`

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Phase 15 fully complete (all 3 plans done)
- Housing assignment, save/load persistence, and gap closure all verified
- Ready for Phase 16: Capacity Transfer from HappinessManager to HousingManager

## Self-Check: PASSED

- FOUND: Scripts/Autoloads/HousingManager.cs
- FOUND: 15-03-SUMMARY.md
- FOUND: commit b3edeab

---
*Phase: 15-housingmanager-core*
*Completed: 2026-03-06*
