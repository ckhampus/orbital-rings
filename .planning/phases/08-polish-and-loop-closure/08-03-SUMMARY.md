---
phase: 08-polish-and-loop-closure
plan: 03
subsystem: ui
tags: [title-screen, scene-integration, ambient-drone, wish-celebration, mute-toggle, game-loop]

# Dependency graph
requires:
  - phase: 08-01
    provides: "SaveManager Autoload with HasSave, Load, ApplyState, ClearSave, ScheduleSceneRestore"
  - phase: 08-02
    provides: "AmbientDrone, WishCelebration, MuteToggle audio/visual scripts"
provides:
  - "TitleScreen scene as main scene with Continue/New Station flow"
  - "Game scene integration with AmbientDrone, WishCelebration, MuteToggle nodes"
  - "Complete game loop: title screen -> gameplay -> autosave -> resume"
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Programmatic title screen UI with dark space aesthetic (ColorRect + styled buttons)"
    - "Confirmation dialog pattern for destructive actions (New Station with existing save)"
    - "Lazy node discovery in _Process for autoload-scene ordering resilience"

key-files:
  created:
    - Scripts/UI/TitleScreen.cs
    - Scenes/TitleScreen/TitleScreen.tscn
  modified:
    - Scenes/QuickTest/QuickTestScene.tscn
    - Scripts/Autoloads/GameEvents.cs
    - Scripts/Build/BuildManager.cs
    - Scripts/Citizens/CitizenManager.cs
    - project.godot

key-decisions:
  - "Programmatic UI in TitleScreen._Ready() -- matches established project pattern of code-built UI"
  - "GameEvents.Instance moved from _Ready to _EnterTree -- ensures singleton available before other autoloads initialize"
  - "BuildManager and CitizenManager use lazy RingVisual discovery in _Process -- handles title screen main scene where RingVisual does not exist"
  - "Confirmation dialog for New Station only when save exists -- prevents accidental data loss"

patterns-established:
  - "Title screen as entry point with conditional Continue button based on save state"
  - "Lazy node discovery pattern: null-check in _Process instead of hard _Ready requirement"
  - "_EnterTree singleton initialization for earliest-possible availability"

requirements-completed: []

# Metrics
duration: 5min
completed: 2026-03-04
---

# Phase 8 Plan 03: Title Screen and Game Scene Integration Summary

**Title screen with Continue/New Station flow, game scene audio integration (drone + celebration + mute), and human-verified complete game loop**

## Performance

- **Duration:** ~5 min (execution) + human verification
- **Started:** 2026-03-04
- **Completed:** 2026-03-04
- **Tasks:** 2 (1 auto + 1 human-verify)
- **Files modified:** 7

## Accomplishments
- Dark-themed title screen with "Orbital Rings" title, conditional Continue button, New Station button, and confirmation dialog for save overwrite
- Game scene updated with AmbientDrone, WishCelebration, and MuteToggle nodes integrated into the scene tree
- Main scene changed from QuickTestScene to TitleScreen -- game now launches to title screen
- Human verified complete game loop: title screen -> build rooms -> citizens walk and wish -> fulfill wishes -> save/load round-trip -> New Station reset

## Task Commits

Each task was committed atomically:

1. **Task 1: Title screen scene and game scene integration** - `78bbb18` (feat) + `1b620b5` (fix)
2. **Task 2: Human verification of complete game loop** - Approved (no code changes)

## Files Created/Modified
- `Scripts/UI/TitleScreen.cs` - Full-screen title screen with programmatic UI: dark background, title label, Continue/New Station buttons, confirmation dialog
- `Scenes/TitleScreen/TitleScreen.tscn` - Minimal Godot scene wrapping TitleScreen.cs as root Control node
- `Scenes/QuickTest/QuickTestScene.tscn` - Added AmbientDrone, WishCelebration, and MuteToggle node entries
- `Scripts/Autoloads/GameEvents.cs` - Moved Instance assignment from _Ready to _EnterTree for earlier availability
- `Scripts/Build/BuildManager.cs` - Lazy RingVisual discovery in _Process instead of hard _Ready requirement
- `Scripts/Citizens/CitizenManager.cs` - Lazy RingVisual discovery in _Process for title screen compatibility
- `project.godot` - Main scene changed to res://Scenes/TitleScreen/TitleScreen.tscn

## Decisions Made
- Programmatic UI for title screen (matching project-wide pattern of code-built UI, no scene-based layout)
- GameEvents.Instance singleton set in _EnterTree instead of _Ready -- ensures availability before other autoloads that depend on it
- BuildManager and CitizenManager use lazy RingVisual lookup in _Process -- title screen has no RingVisual, so _Ready hard lookups would fail
- Confirmation dialog only shown when save exists -- prevents unnecessary friction for first-time players

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] GameEvents.Instance not available during title screen initialization**
- **Found during:** Task 1 build verification
- **Issue:** With TitleScreen as main scene, autoload initialization order caused GameEvents.Instance to be null when other autoloads tried to subscribe in _Ready
- **Fix:** Moved GameEvents.Instance assignment from _Ready to _EnterTree (earlier lifecycle hook)
- **Files modified:** Scripts/Autoloads/GameEvents.cs
- **Verification:** Build succeeds, title screen loads without null reference errors
- **Committed in:** 1b620b5

**2. [Rule 1 - Bug] BuildManager and CitizenManager crash on title screen (no RingVisual)**
- **Found during:** Task 1 runtime testing
- **Issue:** Both managers assumed RingVisual exists in the scene tree during _Ready/_Process, but title screen has no ring
- **Fix:** Added lazy RingVisual discovery with null guards in _Process instead of hard _Ready dependency
- **Files modified:** Scripts/Build/BuildManager.cs, Scripts/Citizens/CitizenManager.cs
- **Verification:** Title screen loads cleanly, game scene still functions after transition
- **Committed in:** 1b620b5

---

**Total deviations:** 2 auto-fixed (2 bugs)
**Impact on plan:** Both fixes necessary for title screen to function as main scene. Autoload initialization order was not anticipated in the plan. No scope creep.

## Issues Encountered
- Autoload initialization order differs when main scene changes from game scene to title screen. Autoloads that depend on scene tree nodes (RingVisual) needed guards since the title screen does not contain those nodes. Resolved with lazy discovery pattern.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- This is the final plan of Phase 8 (the last phase). The game is feature-complete for v1.
- All 5 Phase 8 success criteria confirmed by human verification:
  1. Save/load preserves full game state across sessions
  2. Ambient sound plays continuously, placement snap triggers correctly
  3. HUD displays credits, happiness, and population in real time
  4. Wish fulfillment produces chime + gold sparkles + badge pop + "+X%" text
  5. Game feels cozy after extended play

---
## Self-Check: PASSED

All 2 created files verified on disk. All 5 modified files verified on disk. Both commit hashes (78bbb18, 1b620b5) found in git log.

---
*Phase: 08-polish-and-loop-closure*
*Completed: 2026-03-04*
