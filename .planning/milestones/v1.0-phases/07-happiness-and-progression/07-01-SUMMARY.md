---
phase: 07-happiness-and-progression
plan: 01
subsystem: gameplay
tags: [happiness, progression, autoload, singleton, timer, unlock, housing-capacity]

# Dependency graph
requires:
  - phase: 06-wish-system
    provides: "WishFulfilled event, wish generation/fulfillment loop"
  - phase: 04-build-system
    provides: "BuildPanel, BuildManager, RoomPlaced/RoomDemolished events, GetPlacedRoom API"
  - phase: 05-citizens
    provides: "CitizenManager, CitizenNode, CitizenNames, walkway spawning"
  - phase: 03-economy
    provides: "EconomyManager.SetHappiness, income multiplier, HappinessChanged event stub"
provides:
  - "HappinessManager Autoload with happiness tracking (diminishing returns)"
  - "Citizen arrival timer gated by housing capacity"
  - "Blueprint unlock milestones at 0.25 and 0.60 thresholds"
  - "Public CitizenManager.SpawnCitizen() method for dynamic spawning"
  - "BuildPanel unlock filtering via HappinessManager.IsRoomUnlocked"
affects: [07-02-plan, 08-hud-polish]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Event-driven housing capacity tracking (subscribe to RoomPlaced/RoomDemolished, maintain running total)"
    - "Milestone-based unlock system with crossed-count tracking"

key-files:
  created:
    - Scripts/Autoloads/HappinessManager.cs
  modified:
    - Scripts/Citizens/CitizenManager.cs
    - Scripts/Build/BuildPanel.cs
    - project.godot

key-decisions:
  - "HappinessGainBase = 0.08 for pacing: 25% unlock at ~wish 4, 60% at ~wish 12"
  - "Housing capacity tracked via event subscription + dictionary, not polling BuildManager"
  - "Starter capacity of 5 ensures initial citizens always have housing"
  - "Removed redundant SetCitizenCount call from CitizenManager._Ready since SpawnCitizen handles it"

patterns-established:
  - "Progression milestone tracking with _crossedMilestoneCount to avoid re-triggering"
  - "Housing capacity dictionary for demolish lookups (room gone before event fires)"

requirements-completed: [PROG-01, PROG-02, PROG-03]

# Metrics
duration: 3min
completed: 2026-03-03
---

# Phase 7 Plan 01: Core Progression Engine Summary

**HappinessManager Autoload with diminishing-returns happiness, housing-gated citizen arrivals, and milestone-based blueprint unlocks wired into wish/build/citizen systems**

## Performance

- **Duration:** 3 min
- **Started:** 2026-03-03T20:57:15Z
- **Completed:** 2026-03-03T21:00:54Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments
- HappinessManager Autoload with complete progression logic: happiness tracking (gain = 0.08 / (1 + h)), arrival timer (60s, P = h * 0.6), housing capacity tracking, and unlock milestones
- CitizenManager.SpawnCitizen() extracted as public API for dynamic citizen spawning from HappinessManager
- BuildPanel filters rooms by HappinessManager.IsRoomUnlocked() and refreshes dynamically on BlueprintUnlocked events

## Task Commits

Each task was committed atomically:

1. **Task 1: Create HappinessManager Autoload and register in project.godot** - `d7aab4b` (feat)
2. **Task 2: Refactor CitizenManager.SpawnCitizen and filter BuildPanel by unlock state** - `a55c647` (feat)

## Files Created/Modified
- `Scripts/Autoloads/HappinessManager.cs` - Core progression engine: happiness tracking, arrival timer, housing capacity, blueprint unlocks
- `Scripts/Citizens/CitizenManager.cs` - Refactored SpawnCitizen as public method with optional startAngle, SpawnStarterCitizens simplified to loop
- `Scripts/Build/BuildPanel.cs` - LoadRoomDefinitions filters by IsRoomUnlocked, subscribes to BlueprintUnlocked for dynamic refresh
- `project.godot` - HappinessManager registered as 6th Autoload after WishBoard

## Decisions Made
- Used HappinessGainBase = 0.08 (plan specified this value): 25% unlock at ~wish 4, 60% at ~wish 12, 100% asymptote at ~50 wishes
- Housing capacity tracked via RoomPlaced/RoomDemolished event subscriptions with a Dictionary<int, int> mapping anchor index to BaseCapacity, avoiding the need to poll BuildManager every 60 seconds
- Starter capacity constant of 5 ensures initial citizens always have housing even with no housing rooms placed
- Removed redundant EconomyManager.SetCitizenCount() call from CitizenManager._Ready() since SpawnCitizen() now handles it per-call (Rule 1 - cleanup)
- Added _roomsByCategory.Clear() at the start of LoadRoomDefinitions to support re-calling on BlueprintUnlocked

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Removed redundant SetCitizenCount call in CitizenManager._Ready()**
- **Found during:** Task 2 (SpawnCitizen refactor)
- **Issue:** After refactoring, SpawnCitizen() calls SetCitizenCount on each spawn. The explicit call in _Ready() after SpawnStarterCitizens was redundant.
- **Fix:** Removed the duplicate call to keep a single source of truth.
- **Files modified:** Scripts/Citizens/CitizenManager.cs
- **Verification:** SpawnCitizen calls SetCitizenCount internally; count is correct after 5 starter spawns.
- **Committed in:** a55c647 (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 bug cleanup)
**Impact on plan:** Minimal -- removed dead code after refactor. No scope creep.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- HappinessManager is live and wired: wishes increment happiness, housing gates arrivals, milestones unlock blueprints
- Plan 02 can build on this: happiness bar HUD, arrival fanfare (fade-in tween on SpawnCitizen return value), unlock notification floating text, tab glow effects
- All event subscriptions have matching unsubscriptions in _ExitTree for clean lifecycle

---
*Phase: 07-happiness-and-progression*
*Completed: 2026-03-03*
