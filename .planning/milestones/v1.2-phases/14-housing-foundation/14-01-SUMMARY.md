---
phase: 14-housing-foundation
plan: 01
subsystem: infra
tags: [godot, housing, resource, autoload, save-schema, events]

# Dependency graph
requires:
  - phase: 10-happiness-v2
    provides: "GameEvents pattern, HappinessManager autoload, MoodTier type"
provides:
  - "HousingConfig [GlobalClass] Resource with 4 timing fields"
  - "HousingManager skeleton autoload with singleton pattern"
  - "CitizenAssignedHome and CitizenUnhoused event signatures in GameEvents"
  - "SavedCitizen.HomeSegmentIndex nullable field for housing persistence"
  - "SaveData version 3 format"
  - "default_housing.tres with PRD-calibrated timing values"
affects: [15-assignment-logic, 16-capacity-transfer, 17-zzz-visual, 18-demolish-eviction, 19-save-load]

# Tech tracking
tech-stack:
  added: []
  patterns: [housing-config-resource, housing-events, save-v3-schema]

key-files:
  created:
    - Scripts/Data/HousingConfig.cs
    - Resources/Housing/default_housing.tres
    - Scripts/Autoloads/HousingManager.cs
  modified:
    - Scripts/Autoloads/GameEvents.cs
    - Scripts/Autoloads/SaveManager.cs
    - project.godot

key-decisions:
  - "Timing fields only in HousingConfig (no capacity constants -- capacity stays on RoomDefinition.BaseCapacity)"
  - "SaveData.Version bumped to 3 for HomeSegmentIndex schema change"
  - "CitizenAssignedHome carries (string, int) matching CitizenEnteredRoom pattern"

patterns-established:
  - "HousingConfig resource pattern: [GlobalClass] Resource in Scripts/Data/ with .tres in Resources/Housing/"
  - "HousingManager autoload pattern: singleton with _EnterTree, registered between HappinessManager and SaveManager"

requirements-completed: [INFR-01, INFR-02, INFR-05]

# Metrics
duration: 2min
completed: 2026-03-06
---

# Phase 14 Plan 01: Housing Foundation Summary

**HousingConfig resource with PRD-calibrated timing, HousingManager skeleton autoload, housing events in GameEvents, and save schema v3 with nullable HomeSegmentIndex**

## Performance

- **Duration:** 2 min
- **Started:** 2026-03-06T08:49:49Z
- **Completed:** 2026-03-06T08:51:36Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments
- Created HousingConfig [GlobalClass] Resource with 4 inspector-tunable timing fields (HomeTimerMin/Max, RestDurationMin/Max) at PRD defaults
- Created HousingManager skeleton autoload with singleton pattern and Config export, registered in project.godot
- Added CitizenAssignedHome(string, int) and CitizenUnhoused(string) events to GameEvents with Emit helpers
- Updated SavedCitizen with nullable int? HomeSegmentIndex and bumped SaveData version to 3

## Task Commits

Each task was committed atomically:

1. **Task 1: Create HousingConfig resource and default .tres file** - `2f52932` (feat)
2. **Task 2: Create HousingManager skeleton, add housing events, update save schema** - `9b1a938` (feat)

## Files Created/Modified
- `Scripts/Data/HousingConfig.cs` - [GlobalClass] Resource with 4 exported timing fields for home-return system
- `Resources/Housing/default_housing.tres` - Default HousingConfig instance with PRD-calibrated values (90-150s / 8-15s)
- `Scripts/Autoloads/HousingManager.cs` - Empty singleton skeleton with Config export, ready for Phase 15 assignment logic
- `Scripts/Autoloads/GameEvents.cs` - Added Housing Events section with CitizenAssignedHome and CitizenUnhoused
- `Scripts/Autoloads/SaveManager.cs` - Added HomeSegmentIndex to SavedCitizen, bumped Version to 3
- `project.godot` - Registered HousingManager autoload between HappinessManager and SaveManager

## Decisions Made
- Timing fields only in HousingConfig (no capacity constants) -- capacity lives on RoomDefinition.BaseCapacity per user decision
- SaveData.Version bumped from 1 (default) to 3, CollectGameState from 2 to 3 -- schema change requires version bump
- CitizenAssignedHome uses (string citizenName, int segmentIndex) matching existing CitizenEnteredRoom pattern for consistency

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- All shared types compiled and ready for Phases 15-19 to import
- HousingManager skeleton ready for Phase 15 assignment logic
- GameEvents housing signals ready for subscription
- SavedCitizen schema ready for Phase 19 save/load wiring
- No blockers or concerns

## Self-Check: PASSED

All 7 created/modified files verified present. Both task commits (2f52932, 9b1a938) verified in git log.

---
*Phase: 14-housing-foundation*
*Completed: 2026-03-06*
