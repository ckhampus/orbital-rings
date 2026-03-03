---
phase: 06-wish-system
plan: 02
subsystem: gameplay
tags: [wishes, citizens, badge, sprite3d, tween-animation, visit-targeting, event-driven]

# Dependency graph
requires:
  - phase: 06-wish-system
    provides: "WishBoard singleton with GetRandomTemplate, GetWishForCitizen, WishNudgeRequested event"
  - phase: 05-citizen-system
    provides: "CitizenNode with visit system, CitizenInfoPanel with ShowForCitizen"
  - phase: 04-build-system
    provides: "BuildManager.GetPlacedRoom for wish-aware room matching"
provides:
  - "Wish generation on CitizenNode (30-60s timer, WishBoard.GetRandomTemplate)"
  - "Sprite3D billboard badge above citizen head per wish category icon"
  - "Wish-aware visit targeting with 0.3x distance multiplier for matching rooms"
  - "Wish fulfillment detection on room visit completion with pop animation"
  - "WishNudgeRequested subscription for responsive room-build feedback (7s delay)"
  - "CitizenInfoPanel wish text display with category-colored label"
affects: [06-03-PLAN]

# Tech tracking
tech-stack:
  added: []
  patterns: [sprite3d-billboard-badge, wish-aware-weighted-distance, tween-pop-animation, deterministic-hash-variant-selection]

key-files:
  created: []
  modified:
    - Scripts/Citizens/CitizenNode.cs
    - Scripts/UI/CitizenInfoPanel.cs

key-decisions:
  - "Wish fulfillment only on visit completion (Phase 8 callback), not on room existence -- ensures citizens must physically visit matching rooms"
  - "effectiveDist multiplier (0.3x) for wish matching instead of exclusive targeting -- citizens prefer but don't lock to matching rooms"
  - "Badge as child of CitizenNode inherits Visible=false during visits automatically (Godot parent-child visibility propagation)"
  - "Deterministic text variant via citizen name hash -- same citizen always shows same text for same wish type"

patterns-established:
  - "Sprite3D billboard badge: child of Node3D, Billboard=Enabled, PixelSize=0.005, OpaquePrePass alpha cut"
  - "Tween pop animation: parallel scale-up + fade-out, then QueueFree cleanup via TweenCallback"
  - "Weighted distance visit targeting: effectiveDist = angleDist * multiplier for matching rooms"

requirements-completed: [WISH-01, WISH-02, WISH-03]

# Metrics
duration: 3min
completed: 2026-03-03
---

# Phase 6 Plan 2: Citizen Wish Integration Summary

**Wish lifecycle in CitizenNode with badge display, weighted visit targeting, fulfillment detection, and CitizenInfoPanel wish text display**

## Performance

- **Duration:** 3 min
- **Started:** 2026-03-03T15:50:38Z
- **Completed:** 2026-03-03T15:53:46Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- Extended CitizenNode with full wish lifecycle: generation timer, Sprite3D badge, wish-aware visits, fulfillment on room visit, nudge response
- Updated CitizenInfoPanel to show active wish text from WishBoard with category-colored label, replacing "No wish" placeholder
- Wish-aware distance weighting creates preferential (not exclusive) visit targeting for matching rooms

## Task Commits

Each task was committed atomically:

1. **Task 1: Extend CitizenNode with wish generation, badge display, wish-aware visits, and fulfillment** - `a4e63d0` (feat)
2. **Task 2: Update CitizenInfoPanel to display wish text and category** - `47f051e` (feat)

## Files Created/Modified
- `Scripts/Citizens/CitizenNode.cs` - Added wish generation timer (30-60s), Sprite3D billboard badge per category icon, wish-aware visit targeting (0.3x multiplier), fulfillment detection in Phase 8 callback, pop animation, WishNudgeRequested subscription
- `Scripts/UI/CitizenInfoPanel.cs` - WishBoard lookup replacing "No wish" placeholder, category label with accent colors (coral/blue/amber/green), deterministic text variant via name hash

## Decisions Made
- Wish fulfillment only triggers in Phase 8 callback (after visit completes), not when a matching room exists -- citizens must physically visit
- effectiveDist multiplier approach (0.3x for matching rooms) creates preference without exclusion -- citizens can still visit non-matching rooms
- Badge as direct child of CitizenNode inherits parent visibility changes during visit sequence (no additional hide/show code needed)
- Text variant index computed via Mathf.Abs(name.GetHashCode()) % variants.Length for deterministic per-citizen consistency

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Full wish lifecycle operational: generation -> badge display -> weighted visit targeting -> fulfillment detection -> pop animation -> cooldown -> repeat
- Ready for Plan 03 (happiness integration and wish fulfillment effects)
- WishBoard tracks active wishes via GameEvents subscriptions, CitizenInfoPanel queries via GetWishForCitizen

## Self-Check: PASSED

All created/modified files verified present. All commit hashes verified in git log.

---
*Phase: 06-wish-system*
*Completed: 2026-03-03*
