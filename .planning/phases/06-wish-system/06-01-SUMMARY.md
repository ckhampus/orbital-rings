---
phase: 06-wish-system
plan: 01
subsystem: gameplay
tags: [wishes, resources, autoload, godot-tres, event-driven]

# Dependency graph
requires:
  - phase: 05-citizen-system
    provides: "CitizenManager singleton with Citizens list for nudge iteration"
  - phase: 04-build-system
    provides: "BuildManager with RoomPlaced/RoomDemolished events and GetPlacedRoom query"
  - phase: 01-foundation
    provides: "GameEvents signal bus with WishGenerated/WishFulfilled events"
provides:
  - "WishBoard Autoload singleton with event-driven wish tracking"
  - "12 WishTemplate .tres resources across 4 categories (Social, Comfort, Curiosity, Variety)"
  - "4 category icon PNGs for wish badge display"
  - "Query API: GetWishForCitizen, GetActiveWishCount, IsRoomTypeAvailable, GetRandomTemplate"
  - "WishNudgeRequested event for citizen visit timer integration"
affects: [06-02-PLAN, 06-03-PLAN]

# Tech tracking
tech-stack:
  added: [ImageMagick-for-placeholder-icons]
  patterns: [event-driven-dictionary-tracking, SafeNode-autoload-singleton, tres-resource-templates]

key-files:
  created:
    - Scripts/Autoloads/WishBoard.cs
    - Resources/Wishes/wish_social_hangout.tres
    - Resources/Wishes/wish_social_stargaze.tres
    - Resources/Wishes/wish_social_comm.tres
    - Resources/Wishes/wish_comfort_reading.tres
    - Resources/Wishes/wish_comfort_rest.tres
    - Resources/Wishes/wish_comfort_loft.tres
    - Resources/Wishes/wish_curiosity_observe.tres
    - Resources/Wishes/wish_curiosity_tinker.tres
    - Resources/Wishes/wish_curiosity_craft.tres
    - Resources/Wishes/wish_variety_garden.tres
    - Resources/Wishes/wish_variety_explore.tres
    - Resources/Wishes/wish_variety_relay.tres
    - Resources/Icons/wish_social.png
    - Resources/Icons/wish_comfort.png
    - Resources/Icons/wish_curiosity.png
    - Resources/Icons/wish_variety.png
  modified:
    - project.godot

key-decisions:
  - "WishNudgeRequested event on WishBoard instead of direct CitizenNode.NudgeVisit() call -- decouples Plan 01 from Plan 02 method that does not exist yet"
  - "DirAccess.Open + loop for template loading instead of hardcoded paths -- auto-discovers new templates without code changes"
  - "BuildManager.GetPlacedRoom scan for initialization -- handles pre-placed rooms at game start without new public API on BuildManager"

patterns-established:
  - "Event-driven dictionary tracking: active state maintained via GameEvents subscriptions, no polling or citizen iteration"
  - "WishTemplate .tres resource format: script_class=WishTemplate with WishId, Category enum int, PackedStringArray for TextVariants and FulfillingRoomIds"

requirements-completed: [WISH-03, WISH-04]

# Metrics
duration: 2min
completed: 2026-03-03
---

# Phase 6 Plan 1: Wish Data Layer Summary

**12 WishTemplate .tres resources across 4 categories with WishBoard Autoload singleton for event-driven wish tracking and room-type availability queries**

## Performance

- **Duration:** 2 min
- **Started:** 2026-03-03T15:44:53Z
- **Completed:** 2026-03-03T15:47:46Z
- **Tasks:** 2
- **Files modified:** 18

## Accomplishments
- Created 12 WishTemplate .tres resource files (3 per category: Social, Comfort, Curiosity, Variety) with text variants and room fulfillment mappings
- Created 4 category icon placeholder PNGs (64x64 colored circles) via ImageMagick
- Built WishBoard.cs Autoload with dictionary-based wish tracking, room type counting, template loading, and query API
- Registered WishBoard in project.godot autoload chain after CitizenManager

## Task Commits

Each task was committed atomically:

1. **Task 1: Create WishTemplate .tres resources and placeholder icon PNGs** - `1fb0555` (feat)
2. **Task 2: Create WishBoard Autoload and register in project.godot** - `9175ace` (feat)

## Files Created/Modified
- `Scripts/Autoloads/WishBoard.cs` - SafeNode Autoload singleton: wish tracking, room type counting, template loading, query API, WishNudgeRequested event
- `Resources/Wishes/wish_social_*.tres` (3 files) - Social category wish templates (hangout, stargaze, comm)
- `Resources/Wishes/wish_comfort_*.tres` (3 files) - Comfort category wish templates (reading, rest, loft)
- `Resources/Wishes/wish_curiosity_*.tres` (3 files) - Curiosity category wish templates (observe, tinker, craft)
- `Resources/Wishes/wish_variety_*.tres` (3 files) - Variety category wish templates (garden, explore, relay)
- `Resources/Icons/wish_social.png` - Coral circle placeholder (64x64)
- `Resources/Icons/wish_comfort.png` - Blue circle placeholder (64x64)
- `Resources/Icons/wish_curiosity.png` - Amber circle placeholder (64x64)
- `Resources/Icons/wish_variety.png` - Green circle placeholder (64x64)
- `project.godot` - Added WishBoard autoload registration

## Decisions Made
- Used WishNudgeRequested event on WishBoard instead of directly calling CitizenNode.NudgeVisit() -- Plan 02's NudgeVisit method does not exist yet, so an event decouples the dependency cleanly
- Used DirAccess.Open + loop for template loading -- auto-discovers new .tres templates without requiring code changes
- Scanned BuildManager.GetPlacedRoom across all segment indices for initialization -- handles pre-placed rooms at game start without requiring new public API on BuildManager

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- WishBoard Autoload ready for Plan 02 (citizen wish generation/fulfillment integration)
- WishNudgeRequested event ready for CitizenNode to subscribe to
- All 12 templates loaded and queryable via GetRandomTemplate / GetRandomTemplateForCategory
- Room type availability tracking active via RoomPlaced/RoomDemolished events

## Self-Check: PASSED

All created files verified present. All commit hashes verified in git log.

---
*Phase: 06-wish-system*
*Completed: 2026-03-03*
