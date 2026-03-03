---
phase: 05-citizens-and-navigation
plan: 01
subsystem: citizens
tags: [capsule-mesh, polar-movement, autoload, citizen-system, walking-animation]

# Dependency graph
requires:
  - phase: 04-room-placement-and-build-interaction
    provides: BuildMode enum, GameEvents signal bus, EconomyManager stubs
provides:
  - CitizenNode with angle-based polar walking on circular walkway
  - CitizenManager Autoload with 5 starter citizens
  - CitizenAppearance mesh builder with body-type-varied two-color capsules
  - CitizenNames static pool with 26 diverse names
  - GameEvents citizen interaction events (CitizenClicked, CitizenEnteredRoom, CitizenExitedRoom)
affects: [05-02-room-visits, 05-03-click-info-panel, 06-wishes, 07-happiness]

# Tech tracking
tech-stack:
  added: [CapsuleMesh, Node3D-based citizen entities]
  patterns: [angle-based polar movement, per-instance material capsule meshes, SafeNode lifecycle for Node3D]

key-files:
  created:
    - Scripts/Citizens/CitizenNode.cs
    - Scripts/Citizens/CitizenManager.cs
    - Scripts/Citizens/CitizenNames.cs
    - Scripts/Citizens/CitizenAppearance.cs
  modified:
    - Scripts/Autoloads/GameEvents.cs
    - project.godot

key-decisions:
  - "CitizenNode extends Node3D (not SafeNode) because SafeNode extends Node; implements same subscribe/unsubscribe lifecycle manually"
  - "Angle-based polar movement (angle += speed * delta) instead of NavigationAgent3D -- walkway is a 1D circular path"
  - "Two overlapping CapsuleMesh instances for two-color band effect (primary body + secondary band at midsection)"
  - "Per-instance StandardMaterial3D for every citizen to prevent shared-material contamination"
  - "Curated warm/pastel color palette (8 colors) for cozy aesthetic"

patterns-established:
  - "Node3D SafeNode pattern: _EnterTree/ExitTree with SubscribeEvents/UnsubscribeEvents for Node3D subclasses"
  - "Angle-based polar coordinate movement: _currentAngle += _direction * _speed * delta, position from cos/sin"
  - "Capsule citizen rendering: body-type proportions (Tall/Short/Round) with two-color CapsuleMesh band overlay"

requirements-completed: [CTZN-01, CTZN-02]

# Metrics
duration: 2min
completed: 2026-03-03
---

# Phase 5 Plan 1: Core Citizen System Summary

**Angle-based polar walking citizens with body-type CapsuleMesh visuals, CitizenManager Autoload spawning 5 starters on the ring walkway**

## Performance

- **Duration:** 2 min
- **Started:** 2026-03-03T13:44:38Z
- **Completed:** 2026-03-03T13:47:03Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments
- CitizenNode implements angle-based polar movement on walkway centerline (radius 4.5) with vertical bob and tangent facing
- CitizenManager Autoload spawns 5 starter citizens evenly spaced with random body types, directions, speeds, and color pairs
- CitizenAppearance creates two-color capsule meshes with body-type proportions (Tall/Short/Round)
- GameEvents extended with CitizenClicked, CitizenEnteredRoom, CitizenExitedRoom events for Plan 02/03
- EconomyManager.SetCitizenCount(5) called on spawn, enabling citizen income contribution

## Task Commits

Each task was committed atomically:

1. **Task 1: Create citizen data layer** - `2f421f6` (feat)
2. **Task 2: Create CitizenNode walking behavior and CitizenManager Autoload** - `ab2a0ed` (feat)

## Files Created/Modified
- `Scripts/Citizens/CitizenNames.cs` - Static name pool with 26 diverse real-world names (A-Z) and sequential GetNextName()
- `Scripts/Citizens/CitizenAppearance.cs` - Static helper creating CapsuleMesh with body type proportions and two-color band
- `Scripts/Citizens/CitizenNode.cs` - Node3D-based walking citizen with angle-based polar movement and vertical bob
- `Scripts/Citizens/CitizenManager.cs` - Autoload singleton spawning 5 starters with random appearance and evenly spaced positions
- `Scripts/Autoloads/GameEvents.cs` - Added CitizenClicked, CitizenEnteredRoom, CitizenExitedRoom events with emit helpers
- `project.godot` - Registered CitizenManager Autoload after EconomyManager

## Decisions Made
- CitizenNode extends Node3D (not SafeNode) because SafeNode extends Node, but CitizenNode needs Node3D for Position/Rotation. Implements identical subscribe/unsubscribe lifecycle manually.
- Angle-based polar movement chosen over NavigationAgent3D -- the walkway is a 1D circular path, navigation is literally `angle += speed * delta`. Aligns with existing polar math patterns throughout codebase.
- Two overlapping CapsuleMesh instances for two-color band effect: primary body capsule + shorter, slightly wider secondary capsule at midsection. Simple approach avoiding shader complexity.
- Curated palette of 8 warm/pastel colors for cozy aesthetic. Secondary color guaranteed different from primary via while-loop retry.
- RadialSegments=16 and Rings=4/2 for performance -- tiny capsules don't need the default 64 segments.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- CitizenNode is prepared for Plan 02 room visits: _isVisiting field, _activeTween field (kill-before-create pattern), GetPrimaryMesh() for emission glow
- CitizenManager.Citizens provides read-only list for Plan 02/03 click detection iteration
- GameEvents has all citizen interaction events ready for Plan 02 (CitizenEnteredRoom/ExitedRoom) and Plan 03 (CitizenClicked)
- No blockers -- ready for Plan 02 (room visit drift/fade) and Plan 03 (click-to-inspect panel)

## Self-Check: PASSED

All 4 created files verified on disk. Both task commits (2f421f6, ab2a0ed) found in git history.

---
*Phase: 05-citizens-and-navigation*
*Completed: 2026-03-03*
