---
phase: quick-2
plan: 01
subsystem: citizens
tags: [godot, click-detection, hitbox, ux]

requires:
  - phase: 05-citizens
    provides: "CitizenManager with click detection and ClickProximityThreshold"
provides:
  - "Doubled citizen click proximity threshold for easier selection"
affects: []

tech-stack:
  added: []
  patterns: []

key-files:
  created: []
  modified:
    - Scripts/Citizens/CitizenManager.cs

key-decisions:
  - "0.8f threshold: ~10-13x capsule radius, generous but no overlap risk at 5.65 unit spacing"

patterns-established: []

requirements-completed: [QUICK-2]

duration: 1min
completed: 2026-03-03
---

# Quick Task 2: Bigger Citizen Hitbox Summary

**Doubled ClickProximityThreshold from 0.4 to 0.8 world units for reliable citizen click selection**

## Performance

- **Duration:** <1 min
- **Started:** 2026-03-03T19:52:34Z
- **Completed:** 2026-03-03T19:53:03Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments
- Increased ClickProximityThreshold from 0.4f to 0.8f in CitizenManager.cs
- Citizens are now significantly easier to click while moving on the walkway ring
- No risk of false positives: 1.6 unit diameter vs 5.65 unit citizen spacing

## Task Commits

Each task was committed atomically:

1. **Task 1: Increase citizen click proximity threshold** - `09f0948` (fix)

**Plan metadata:** [pending] (docs: complete quick task 2)

## Files Created/Modified
- `Scripts/Citizens/CitizenManager.cs` - Changed ClickProximityThreshold constant from 0.4f to 0.8f

## Decisions Made
None - followed plan as specified. The 0.8f value was pre-calculated in the plan with clear rationale.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Citizen click detection improved, no blockers for future work

---
*Quick Task: 2-i-want-a-bigger-hitbox-for-citizens-it-i*
*Completed: 2026-03-03*
