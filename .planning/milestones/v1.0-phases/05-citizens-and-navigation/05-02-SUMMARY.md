---
phase: 05-citizens-and-navigation
plan: 02
subsystem: citizens
tags: [room-visit, drift-fade-tween, click-detection, emission-glow, citizen-info-panel, ray-plane-intersection]

# Dependency graph
requires:
  - phase: 05-citizens-and-navigation
    plan: 01
    provides: CitizenNode walking behavior, CitizenManager Autoload, CitizenAppearance mesh, GameEvents citizen events
  - phase: 04-room-placement-and-build-interaction
    provides: BuildManager with BuildMode enum, SegmentGrid occupancy, RingVisual
provides:
  - Room visit behavior with proximity-based target selection and drift-fade tween animation
  - Click-to-inspect interaction with ray-plane hit detection and emission glow highlight
  - CitizenInfoPanel floating popup showing citizen name and wish placeholder
  - Auto-deselect on visiting citizen, build mode suppression of citizen clicks
affects: [05-03-citizen-navigation, 06-wishes, 07-happiness]

# Tech tracking
tech-stack:
  added: [Tween chained sequences, emission glow via StandardMaterial3D]
  patterns: [8-phase tween chain for drift-fade animation, ray-plane citizen click picking, mesh transparency mode toggle for Z-fight prevention]

key-files:
  created:
    - Scripts/UI/CitizenInfoPanel.cs
  modified:
    - Scripts/Citizens/CitizenNode.cs
    - Scripts/Citizens/CitizenManager.cs

key-decisions:
  - "SegmentGrid passed via Initialize() from CitizenManager rather than tree lookup -- cleaner dependency injection"
  - "Emission glow (EmissionEnergyMultiplier=2.5) for selected citizen leveraging existing environment glow bloom"
  - "Transparency mode toggled ON before fading, OFF after -- prevents Z-fighting artifacts (pitfall #4)"
  - "Auto-deselect in _Process when selected citizen starts visiting -- prevents glow on invisible citizen"

patterns-established:
  - "8-phase tween chain: drift -> fade-out -> hide -> wait -> show -> fade-in -> drift-back -> restore"
  - "Mesh transparency mode toggle: set Alpha+DepthDrawAlways before fading, restore Disabled+OpaqueOnly after"
  - "CitizenInfoPanel: camera.UnprojectPosition for 3D-to-2D popup positioning with viewport clamping"

requirements-completed: [CTZN-03, CTZN-04]

# Metrics
duration: 3min
completed: 2026-03-03
---

# Phase 5 Plan 2: Room Visits and Click-to-Inspect Summary

**Proximity-based room visit with 8-phase drift-fade tween animation, plus ray-plane citizen click detection with emission glow and floating info panel**

## Performance

- **Duration:** 3 min
- **Started:** 2026-03-03T13:50:29Z
- **Completed:** 2026-03-03T13:54:00Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- Citizens periodically visit nearby occupied rooms with smooth radial drift + fade-out/in animation sequence
- Visit timer (20-40s randomized) checks all 24 segments for nearest occupied one within angular proximity threshold
- Click detection via ray-plane intersection finds closest citizen in Normal mode, shows CitizenInfoPanel with name and "No wish" placeholder
- Selected citizen glows with emission highlight (bloom via environment glow), deselects on click-away or Escape
- Build mode (Placing/Demolish) suppresses citizen clicks; visiting citizens excluded from click targets

## Task Commits

Each task was committed atomically:

1. **Task 1: Add room visit behavior to CitizenNode** - `c624538` (feat)
2. **Task 2: Add click detection and CitizenInfoPanel with emission glow** - `773cd12` (feat)

## Files Created/Modified
- `Scripts/Citizens/CitizenNode.cs` - Added visit constants, Timer child node, OnVisitTimerTimeout with proximity detection, StartVisit 8-phase tween chain, SetRadialPosition/SetMeshAlpha/SetMeshTransparencyMode helpers, AngleDistance static helper, updated Initialize() to accept SegmentGrid
- `Scripts/Citizens/CitizenManager.cs` - Added RingVisual.Grid lookup and pass-through, camera cache, ray-plane click detection, FindCitizenAtScreenPos, SelectCitizen/DeselectCitizen with emission glow, CitizenInfoPanel creation on CanvasLayer, auto-deselect visiting citizens in _Process, Escape key handling
- `Scripts/UI/CitizenInfoPanel.cs` - New floating popup with name label, wish label ("No wish" placeholder), dark semi-transparent StyleBoxFlat, viewport-clamped positioning via camera.UnprojectPosition

## Decisions Made
- SegmentGrid passed via Initialize() from CitizenManager rather than tree lookup in CitizenNode -- cleaner dependency injection, CitizenManager already finds RingVisual
- Emission glow with EmissionEnergyMultiplier=2.5f and Emission color as PrimaryColor.Lightened(0.3f) -- leverages existing environment glow_enabled for bloom without any additional post-processing setup
- Transparency mode toggled on/off around fade animations to prevent Z-fighting artifacts (pitfall #4 from research)
- Auto-deselect in _Process when selected citizen starts a visit -- prevents emission glow persisting on invisible citizen

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- CitizenInfoPanel is ready for Phase 6: wish text label just needs updating from "No wish" to actual wish content
- Room visit events (CitizenEnteredRoom/ExitedRoom) are emitting for future happiness/wish systems to consume
- Click detection pattern is extensible for future citizen interaction features
- All four CTZN requirements (01-04) now complete across Plans 01 and 02

## Self-Check: PASSED

All 3 files verified on disk. Both task commits (c624538, 773cd12) found in git history.

---
*Phase: 05-citizens-and-navigation*
*Completed: 2026-03-03*
