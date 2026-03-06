---
phase: 17-return-home-behavior
plan: 01
subsystem: citizens
tags: [godot, tween, label3d, timer, housing, citizen-behavior]

# Dependency graph
requires:
  - phase: 14-housing-foundation
    provides: HousingConfig resource, HousingManager singleton, CitizenAssignedHome/CitizenUnhoused events
  - phase: 15-housing-assignment
    provides: citizen-to-home assignment logic, HomeSegmentIndex property
  - phase: 16-capacity-transfer
    provides: HousingManager as single source of truth for capacity
provides:
  - Periodic home return behavior for housed citizens (90-150s timer)
  - Zzz Label3D indicator with billboard mode, purple color, fade in/out, vertical bob
  - Wish timer pausing during home rest (no wish generation while resting)
  - Wish priority over home return (active wish skips, mid-walk wish aborts)
  - Demolish-eject safety (citizen restored to walkway immediately)
  - CitizenEnteredHome/CitizenExitedHome events on GameEvents
affects: [save-load, citizen-info-panel, housing-tooltip]

# Tech tracking
tech-stack:
  added: []
  patterns: [Label3D with TopLevel for parent-independent visibility, nested tween for concurrent fade+bob, stored delegate event subscriptions]

key-files:
  created: []
  modified:
    - Scripts/Autoloads/GameEvents.cs
    - Scripts/Citizens/CitizenNode.cs
    - Scripts/Citizens/CitizenManager.cs

key-decisions:
  - "Label3D with TopLevel=true for Zzz indicator (stays visible when citizen Visible=false)"
  - "Separate nested tweens for Zzz fade in/out (concurrent with main sequence)"
  - "_walkingToHome flag scopes abort window to angular walk phase only"

patterns-established:
  - "Home return tween follows same 8-phase structure as room visit (StartVisit)"
  - "Zzz Label3D lifecycle: CreateZzzLabel/RemoveZzzLabel/StartZzzBob triplet"
  - "Stored delegate references for CitizenNode event subscriptions (mirrors HousingManager pattern)"

requirements-completed: [BEHV-01, BEHV-02, BEHV-03, BEHV-04]

# Metrics
duration: 5min
completed: 2026-03-06
---

# Phase 17 Plan 01: Return Home Behavior Summary

**Periodic home return with 8-phase tween, Zzz Label3D rest indicator, wish priority guards, and demolish-eject safety**

## Performance

- **Duration:** 5 min
- **Started:** 2026-03-06T15:21:02Z
- **Completed:** 2026-03-06T15:26:55Z
- **Tasks:** 3
- **Files modified:** 3

## Accomplishments
- Housed citizens periodically walk to their home room (90-150s configurable timer), rest 8-15s with a floating "Zzz" indicator, then resume walking
- Zzz Label3D with billboard mode, soft purple color (#9A8CE6), 0.5s fade in/out, gentle vertical bob animation
- Wish timer pauses during home rest; active wishes skip home return; mid-walk wish generation aborts the return
- Demolishing a home during rest ejects the citizen immediately (Zzz disappears, citizen reappears on walkway)
- CitizenEnteredHome/CitizenExitedHome events added to GameEvents for future UI/analytics

## Task Commits

Each task was committed atomically:

1. **Task 1: GameEvents housing events and CitizenNode home return infrastructure** - `0bb3c9e` (feat)
2. **Task 2: StartHomeReturn tween sequence and wish-walk abort wiring** - `481d2db` (feat)
3. **Task 3: CitizenManager wiring and dotnet format** - `b33cd12` (feat)

## Files Created/Modified
- `Scripts/Autoloads/GameEvents.cs` - Added CitizenEnteredHome/CitizenExitedHome events with emit helpers
- `Scripts/Citizens/CitizenNode.cs` - Full home return behavior: timer, 8-phase tween, Zzz Label3D lifecycle, abort/eject handlers, event subscriptions
- `Scripts/Citizens/CitizenManager.cs` - Pass HousingConfig to Initialize, extend auto-deselect and click detection for IsAtHome

## Decisions Made
- Used Label3D with TopLevel=true for the Zzz indicator so it renders independently from the citizen node's Visible state (citizen is hidden during rest, but Zzz must remain visible above the room)
- Created separate nested tweens for Zzz fade in/out animations (cannot be part of the main sequential tween since they need to run concurrently with the rest interval start)
- The _walkingToHome flag is true only during Phase 0 (angular walk), scoping the abort window precisely -- once the citizen reaches the home segment and begins drifting/fading, the home return commits

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Home return behavior is fully functional and ready for visual testing in Godot editor
- For quick iteration, temporarily reduce HomeTimerMin/Max in HousingConfig to 10-15s
- Save/load already handles HomeSegmentIndex (Phase 15) -- home timer will restart on load via _Ready
- Future phases can subscribe to CitizenEnteredHome/CitizenExitedHome for tooltip/UI updates

---
*Phase: 17-return-home-behavior*
*Completed: 2026-03-06*
