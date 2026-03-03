---
phase: quick
plan: 1
subsystem: citizens
tags: [animation, tween, polar-coordinates, walkway]

# Dependency graph
requires:
  - phase: 05-citizens
    provides: "CitizenNode with room visit tween sequence"
provides:
  - "Correct angular walk-before-drift visit animation"
affects: [citizens, ring-visualization]

# Tech tracking
tech-stack:
  added: []
  patterns: ["angular-walk-then-radial-drift visit sequence"]

key-files:
  created: []
  modified:
    - Scripts/Citizens/CitizenNode.cs

key-decisions:
  - "Shortest-path angular interpolation with Tau wraparound normalization to [-Pi, Pi]"
  - "Walk duration derived from angular distance / citizen walking speed for natural pacing"
  - "Minimum 0.1s walk duration to prevent zero-length tweens when citizen is already at target segment"

patterns-established:
  - "Phase 0 angular walk: always walk along walkway to target angle before radial drift"

requirements-completed: [BUGFIX-VISIT-ANGLE]

# Metrics
duration: 1min
completed: 2026-03-03
---

# Quick Task 1: Fix Visit Animation Angular Walk Summary

**Citizens now walk angularly along the walkway to the target segment's mid-angle before drifting radially into rooms, preventing drift into empty segments**

## Performance

- **Duration:** 1 min
- **Started:** 2026-03-03T19:43:35Z
- **Completed:** 2026-03-03T19:44:55Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments
- Fixed visit animation so citizens walk to the correct segment before entering a room
- Added Phase 0 angular walk tween with shortest-path Tau wraparound handling
- Updated _currentAngle after walk phase so citizen resumes walking from room position
- Changed all radial drift phases to use targetAngle instead of captured _currentAngle

## Task Commits

Each task was committed atomically:

1. **Task 1: Fix visit animation to walk to target segment before radial drift** - `c549972` (fix)

## Files Created/Modified
- `Scripts/Citizens/CitizenNode.cs` - Added angular walk phase (Phase 0) before radial drift, changed StartVisit to accept targetPosition, compute targetAngle, and use it in all radial phases

## Decisions Made
- Shortest-path angular interpolation normalizes delta to [-Pi, Pi] range to handle Tau boundary crossing
- Walk duration = angular distance / citizen speed, matching natural walking pace (no artificial speed)
- Minimum 0.1s walk duration clamp prevents zero-length tweens when citizen is already at the target segment

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Bug fix is self-contained; no downstream impacts
- Citizens will now visually walk to the correct segment before visiting rooms
- Build compiles cleanly with 0 warnings and 0 errors

## Self-Check: PASSED

- FOUND: Scripts/Citizens/CitizenNode.cs
- FOUND: 1-SUMMARY.md
- FOUND: c549972

---
*Phase: quick*
*Completed: 2026-03-03*
