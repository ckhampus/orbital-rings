---
phase: 04-room-placement-and-build-interaction
plan: 02
subsystem: build-ui
tags: [build-panel, toolbar, room-cards, segment-interaction, hotkeys, cost-preview]

# Dependency graph
requires:
  - phase: 04-room-placement-and-build-interaction
    provides: "BuildManager Autoload, RoomDefinition .tres files, RoomColors, GameEvents Phase 4 events"
provides:
  - "BuildPanel bottom toolbar with 5 category tabs, room cards, and demolish button"
  - "SegmentInteraction build mode delegation (clicks and hovers to BuildManager)"
  - "Live cost preview label tracking ghost mesh during placement"
  - "Demolish refund preview label during hover"
  - "BuildUILayer in QuickTestScene (CanvasLayer layer 8)"
affects: [04-03-placement-feedback, 04-04-integration]

# Tech tracking
tech-stack:
  added: []
  patterns: ["Programmatic bottom toolbar UI with category tabs and hotkeys", "Build mode delegation pattern: SegmentInteraction routes to BuildManager based on CurrentMode", "3D-to-2D label projection for live cost/refund preview near ghost mesh"]

key-files:
  created:
    - Scripts/Build/BuildPanel.cs
  modified:
    - Scripts/Ring/SegmentInteraction.cs
    - Scenes/QuickTest/QuickTestScene.tscn

key-decisions:
  - "BuildPanel as PanelContainer with programmatic UI (following CreditHUD pattern) rather than scene-based layout"
  - "Live cost label as child of BuildPanel using camera.UnprojectPosition for 3D-to-2D tracking"
  - "SegmentInteraction delegates to BuildManager via singleton Instance rather than node path lookup"
  - "Hover highlighting suppressed during Placing mode (ghost preview provides feedback), preserved during Demolish mode"
  - "Dimmed segments not restored to Normal during hover transitions in Placing mode"

patterns-established:
  - "Build mode delegation: SegmentInteraction checks BuildManager.CurrentMode before processing clicks/hovers"
  - "Category tab pattern: Button array with active/inactive StyleBoxFlat swap and colored bottom border accent"
  - "Room card pattern: PanelContainer with metadata storage via SetMeta for click handler room identification"

requirements-completed: [BLDG-01, BLDG-02, BLDG-03, BLDG-05]

# Metrics
duration: 3min
completed: 2026-03-03
---

# Phase 4 Plan 02: Build Panel UI and Segment Interaction Wiring Summary

**Bottom toolbar with 5 category tabs, room cards, demolish button, hotkeys 1-5/B/Escape, live cost preview near ghost, and SegmentInteraction delegation to BuildManager in build modes**

## Performance

- **Duration:** 3 min
- **Started:** 2026-03-03T10:56:40Z
- **Completed:** 2026-03-03T11:00:15Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- Created BuildPanel bottom toolbar with 5 category tabs showing room cards (name, cost, segment range) and a visually distinct demolish button
- Wired SegmentInteraction to delegate clicks and hovers to BuildManager when in Placing or Demolish mode, preserving normal selection in Normal mode
- Implemented live cost preview label that tracks ghost mesh position via 3D-to-2D projection, turning red when insufficient credits
- Implemented refund preview label showing "+N" in green near hovered rooms during demolish mode
- Added hotkey support: B toggles toolbar, 1-5 selects category tabs, Escape closes toolbar and exits build mode

## Task Commits

Each task was committed atomically:

1. **Task 1: Create BuildPanel bottom toolbar with category tabs, room cards, and demolish button** - `1e84b29` (feat)
2. **Task 2: Wire SegmentInteraction to delegate clicks and hovers to BuildManager in build mode** - `4e0f89e` (feat)

## Files Created/Modified
- `Scripts/Build/BuildPanel.cs` - Bottom toolbar UI with category tabs, room cards, demolish button, live cost/refund preview labels
- `Scripts/Ring/SegmentInteraction.cs` - Extended with build mode delegation: clicks/hovers route to BuildManager, hover highlighting suppressed in Placing mode
- `Scenes/QuickTest/QuickTestScene.tscn` - Added BuildUILayer (CanvasLayer layer 8) with BuildPanel anchored bottom-wide

## Decisions Made
- BuildPanel builds all UI programmatically in _Ready following the established CreditHUD pattern, ensuring consistency
- Live cost label positioned as child of BuildPanel using camera.UnprojectPosition for smooth 3D-to-2D tracking each frame
- SegmentInteraction uses BuildManager.Instance singleton directly rather than node path lookup for simplicity and reliability
- Hover highlighting suppressed during Placing mode because ghost preview already provides visual placement feedback; preserved during Demolish mode to help identify target rooms
- Dimmed segments not restored to Normal during hover transitions in Placing mode to maintain occupancy feedback

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- BuildPanel is ready to receive placement/demolish feedback events from Plan 03 (PlacementFeedback)
- SegmentInteraction delegation is fully connected -- clicks and hovers flow to BuildManager for all build modes
- Live cost and refund labels are operational and will work with the existing BuildManager ghost preview system
- Plan 04 (integration) can verify the full end-to-end placement and demolish flow

## Self-Check: PASSED

- All 3 files verified present on disk
- Both task commits verified in git log (1e84b29, 4e0f89e)
- dotnet build succeeds with 0 warnings, 0 errors

---
*Phase: 04-room-placement-and-build-interaction*
*Completed: 2026-03-03*
