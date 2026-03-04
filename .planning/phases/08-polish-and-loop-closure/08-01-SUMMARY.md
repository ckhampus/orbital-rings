---
phase: 08-polish-and-loop-closure
plan: 01
subsystem: save-load
tags: [system-text-json, serialization, autosave, debounce, autoload, singleton]

# Dependency graph
requires:
  - phase: 03-economy-credits
    provides: EconomyManager singleton with credits balance
  - phase: 04-room-placement
    provides: BuildManager singleton with placed rooms tracking
  - phase: 05-citizens-on-the-ring
    provides: CitizenManager/CitizenNode with citizen state
  - phase: 06-wish-system
    provides: WishBoard singleton with active wishes and templates
  - phase: 07-happiness-and-progression
    provides: HappinessManager with happiness, unlocks, housing capacity
provides:
  - SaveManager Autoload with autosave, load, clear, and scene restoration
  - Public save/load API on BuildManager, CitizenManager, HappinessManager, EconomyManager, WishBoard
  - SaveData/SavedRoom/SavedCitizen POCOs for JSON serialization
  - StateLoaded guards on CitizenManager, HappinessManager, EconomyManager
  - Frame-delay scene restoration pattern (ScheduleSceneRestore)
affects: [08-02 title screen, 08-03 polish]

# Tech tracking
tech-stack:
  added: [System.Text.Json]
  patterns: [debounced-autosave, state-loaded-guard, frame-delay-scene-restore, poco-serialization]

key-files:
  created:
    - scripts/Autoloads/SaveManager.cs
  modified:
    - scripts/Build/BuildManager.cs
    - scripts/Citizens/CitizenManager.cs
    - scripts/Citizens/CitizenNode.cs
    - scripts/Autoloads/HappinessManager.cs
    - scripts/Autoloads/EconomyManager.cs
    - scripts/Autoloads/WishBoard.cs
    - project.godot

key-decisions:
  - "Plain C# POCOs for SaveData (no Godot types) -- System.Text.Json cannot serialize Godot structs"
  - "Debounced autosave (0.5s Timer) on all 7 state-change events -- prevents rapid-fire saves during batch operations"
  - "Frame-delay restoration: wait 2 frames after ChangeSceneToFile for all _Ready to complete before applying scene state"
  - "Static StateLoaded flags on managers -- checked in _Ready to skip default initialization when loading from save"
  - "Room definition cache in BuildManager via DirAccess scan -- enables RestorePlacedRoom by roomId without hardcoded paths"

patterns-established:
  - "StateLoaded guard pattern: static bool flag checked in _Ready, set by SaveManager before scene transition"
  - "Frame-delay scene restoration: _pendingLoadFrames counter in _Process, waits 2 frames then applies state"
  - "Debounced event-driven save: lambda delegates wrapping varying event signatures, stored as fields for unsubscription"
  - "POCO serialization: all Godot types decomposed to primitives (Color -> R/G/B floats, enum -> int)"

requirements-completed: []

# Metrics
duration: 5min
completed: 2026-03-04
---

# Phase 8 Plan 1: Save/Load System Summary

**SaveManager Autoload with debounced autosave on 7 state-change events, JSON serialization via System.Text.Json, and public save/load APIs on 5 existing managers**

## Performance

- **Duration:** 5 min
- **Started:** 2026-03-04T09:16:21Z
- **Completed:** 2026-03-04T09:21:26Z
- **Tasks:** 2
- **Files modified:** 8

## Accomplishments
- Public save/load API added to all 5 existing manager singletons (BuildManager, CitizenManager, HappinessManager, EconomyManager, WishBoard)
- SaveManager Autoload with complete save/load lifecycle: autosave, load, apply pre-scene state, apply post-scene state, has-save check, clear
- StateLoaded guards on 3 managers prevent double-initialization when loading from save
- SaveData uses exclusively plain C# types for zero-friction System.Text.Json serialization

## Task Commits

Each task was committed atomically:

1. **Task 1: Add save/load public API to all existing managers** - `fc8f315` (feat)
2. **Task 2: Create SaveManager Autoload with autosave, load, and clear** - `0007c87` (feat)

## Files Created/Modified
- `scripts/Autoloads/SaveManager.cs` - New Autoload: SaveData POCOs, autosave, load, apply state, scene restoration
- `scripts/Build/BuildManager.cs` - GetAllPlacedRooms, RestorePlacedRoom, ClearAllRooms, room definition cache
- `scripts/Citizens/CitizenManager.cs` - StateLoaded guard, ClearCitizens, SpawnCitizenFromSave
- `scripts/Citizens/CitizenNode.cs` - Direction property, SetDirection, SetWishFromSave internal methods
- `scripts/Autoloads/HappinessManager.cs` - StateLoaded guard, GetUnlockedRoomIds, GetCrossedMilestoneCount, GetHousingCapacity, RestoreState
- `scripts/Autoloads/EconomyManager.cs` - StateLoaded guard, RestoreCredits
- `scripts/Autoloads/WishBoard.cs` - GetTemplateById, GetPlacedRoomTypeCounts, RestoreActiveWishes, RestorePlacedRoomTypes
- `project.godot` - SaveManager registered as last Autoload

## Decisions Made
- Plain C# POCOs for SaveData (no Godot types) -- System.Text.Json cannot serialize Godot structs (Color, Vector3)
- Debounced autosave via Timer (0.5s one-shot) on all 7 state-change events -- prevents rapid-fire saves during batch operations
- Frame-delay scene restoration (wait 2 frames after ChangeSceneToFile) -- ensures all _Ready methods complete before applying scene state
- Static StateLoaded flags on 3 managers -- simple and effective guard against double-initialization
- Room definition cache loaded in BuildManager._Ready via DirAccess -- same pattern as WishBoard.LoadTemplates

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- SaveManager.HasSave(), Load(), ApplyState(), ScheduleSceneRestore() ready for title screen integration (Plan 02)
- All manager APIs in place for complete state round-trip
- ClearSave() available for "New Station" flow

---
*Phase: 08-polish-and-loop-closure*
*Completed: 2026-03-04*
