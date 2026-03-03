# Roadmap: Orbital Rings

## Overview

Orbital Rings is built from the ground up in eight phases, each delivering a coherent capability. The project starts with architectural decisions that are expensive to retrofit (signals, data schema, camera), then builds the ring geometry that is the game's structural foundation, then layers in economy, building interaction, citizens, wishes, happiness, and finally the polish pass that makes the whole loop feel cozy rather than mechanical. No phase introduces a dependency its predecessor has not satisfied. The game is complete when a player can build rooms, watch named citizens arrive and express wishes, fulfill those wishes, and feel the station grow — all without a single punishing moment.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Foundation and Project Architecture** - Establish signal bus, data schema, camera, and architectural patterns before any gameplay code is written
- [ ] **Phase 2: Ring Geometry and Segment Grid** - Build the polar segment grid, ring mesh, and visual segment differentiation that everything else sits on
- [ ] **Phase 3: Economy Foundation** - Implement the credit economy, passive income, balance spreadsheet, and EconomyManager Autoload
- [ ] **Phase 4: Room Placement and Build Interaction** - Wire room placement into the ring, all 5 categories with placeholder art, demolish, and satisfying placement feedback
- [ ] **Phase 5: Citizens and Navigation** - Spawn named walking citizens on the ring walkway with a navigation approach proven by prototype
- [ ] **Phase 6: Wish System** - Citizens express wishes via speech bubbles; wishes linger harmlessly and are visible on click
- [ ] **Phase 7: Happiness and Progression** - Wish fulfillment raises happiness, happiness drives citizen arrival and blueprint unlocks
- [ ] **Phase 8: Polish and Loop Closure** - Save/load, ambient audio, full HUD wiring, and integration pass that makes the loop feel cozy and complete

## Phase Details

### Phase 1: Foundation and Project Architecture
**Goal**: The project has a working Godot 4 C# structure with established architectural patterns that every subsequent phase builds on safely
**Depends on**: Nothing (first phase)
**Requirements**: RING-02
**Success Criteria** (what must be TRUE):
  1. Player can orbit the camera horizontally around a placeholder ring object and zoom in/out at a fixed tilt angle
  2. GameEvents Autoload exists with typed C# delegate signals and all future signal declarations stubbed in one place
  3. Resource subclasses for RoomDefinition, WishTemplate, and CitizenData exist and are editable in the Godot Inspector
  4. A quick-test scene with 2-3 placeholder objects loads in under 3 seconds and serves as the iteration sandbox
  5. Signal disconnect pattern is established in a base class or documented convention before any gameplay signal is connected
**Plans**: 3 plans

Plans:
- [x] 01-01-PLAN.md -- Core architecture: GameEvents Autoload, SafeNode base class, Resource subclasses
- [x] 01-02-PLAN.md -- Camera system: OrbitalCamera, environment setup, QuickTest scene
- [ ] 01-03-PLAN.md -- Gap closure: ambient light fix, flat disc geometry, touchpad/keyboard zoom

### Phase 2: Ring Geometry and Segment Grid
**Goal**: The ring is visible, its 24 segments are selectable by mouse click using polar math (not trimesh collision), and inner vs. outer positions are visually readable
**Depends on**: Phase 1
**Requirements**: RING-01, RING-03
**Success Criteria** (what must be TRUE):
  1. Player sees a flat donut ring with 12 outer and 12 inner segment positions and a walkway corridor between them
  2. Hovering the mouse over any segment highlights it; clicking selects the correct segment index (verified by log output)
  3. Outer and inner rows are visually distinct — readable at a glance which row a segment belongs to
  4. Segment numbers or position indicators are visible to the player on the ring surface
  5. The walkway corridor is present as a navigable strip between the two rows, authored ready for citizen pathfinding
**Plans**: 2 plans

Plans:
- [ ] 02-01-PLAN.md -- Ring mesh and segment grid: SegmentGrid data model, RingMeshBuilder, RingVisual with 24 colored segments + walkway, replace CSG placeholder
- [ ] 02-02-PLAN.md -- Segment interaction and tooltip: polar-math hover/click detection, hover/selection visual feedback, screen-space tooltip

### Phase 3: Economy Foundation
**Goal**: The credit economy is live, balanced via a spreadsheet before any numbers are hardcoded, and all rates are editable in Resource files without recompile
**Depends on**: Phase 1
**Requirements**: ECON-01, ECON-02, ECON-03, ECON-04, ECON-05
**Success Criteria** (what must be TRUE):
  1. Player starts a new game with enough credits to place 3-5 rooms on an empty ring
  2. Credit balance visibly increases over time as passive income ticks (even with no citizens, a starter rate applies)
  3. An economy balance spreadsheet exists modeling citizen count, income rate, room costs, and happiness multiplier across 5/15/30 citizen milestones
  4. Room costs scale by segment size with diminishing returns (a 3-segment room costs less than 3x a 1-segment room)
  5. All economy parameters (income rate, happiness multiplier cap, cost formulas) live in Inspector-editable Resource files, not C# constants
**Plans**: 3 plans

Plans:
- [ ] 03-01-PLAN.md -- Balance spreadsheet and data layer: economy-balance.md, EconomyConfig updates, RoomDefinition BaseCostOverride, GameEvents economy display events
- [ ] 03-02-PLAN.md -- EconomyManager Autoload: credit balance, Timer income tick, cost calculation, spend/earn/refund, default_economy.tres
- [ ] 03-03-PLAN.md -- Credit HUD: rolling counter, income flash, floating spend/refund numbers, hover tooltip

### Phase 4: Room Placement and Build Interaction
**Goal**: The player can place and demolish rooms anywhere on the ring, choose from 5 visually distinct categories, and feel a satisfying snap response every time
**Depends on**: Phase 2, Phase 3
**Requirements**: BLDG-01, BLDG-02, BLDG-03, BLDG-04, BLDG-05, BLDG-06
**Success Criteria** (what must be TRUE):
  1. Player can select 1, 2, or 3 adjacent empty segments and place a room that occupies exactly those segments
  2. The build panel shows all 5 room categories (Housing, Life Support, Work, Comfort, Utility), each with at least 2 room types that look visually distinct
  3. Placing a room produces an audible snap sound and a visible animation or flash response within the same frame
  4. Player can demolish any placed room and see credits returned as a partial refund (less than full cost)
  5. Larger rooms (2-3 segments) are visibly more effective in their stat display and cost more, but cost per segment is lower than a 1-segment room
  6. The ring correctly rejects placement attempts on occupied segments or segments that would exceed the ring boundary
**Plans**: 4 plans

Plans:
- [ ] 04-01-PLAN.md -- Room data layer, BuildManager Autoload, and RoomVisual rendering system
- [ ] 04-02-PLAN.md -- BuildPanel bottom toolbar UI and SegmentInteraction build mode delegation
- [ ] 04-03-PLAN.md -- PlacementFeedback with tween animations, particles, and procedural audio
- [ ] 04-04-PLAN.md -- Human verification checkpoint for complete build loop

### Phase 5: Citizens and Navigation
**Goal**: Named citizens with distinct appearances walk the ring walkway continuously and enter rooms based on spatial proximity, with navigation proven by prototype before full implementation
**Depends on**: Phase 2, Phase 4
**Requirements**: CTZN-01, CTZN-02, CTZN-03, CTZN-04
**Success Criteria** (what must be TRUE):
  1. At least 3 named citizens with visually distinct appearances walk the walkway continuously without stopping or clipping through walls
  2. Citizens navigate the circular walkway without cutting across the ring interior or taking the wrong arc around the ring
  3. Citizens visibly drift toward and enter rooms that are relevant to their current state (proximity-based, not teleport)
  4. Clicking a citizen opens a panel showing their name and current wish (or "no wish" if none active)
  5. When a citizen is removed (for testing), its signal connections are cleaned up with zero orphan nodes reported in the Godot debugger
**Plans**: TBD

### Phase 6: Wish System
**Goal**: Citizens generate wishes expressed as speech bubbles, those wishes remain visible and harmless until fulfilled, and wishes span all four defined categories
**Depends on**: Phase 5
**Requirements**: WISH-01, WISH-02, WISH-03, WISH-04
**Success Criteria** (what must be TRUE):
  1. Citizens periodically display speech bubbles with wish text describing a room type they want (e.g., "I'd love a place to stargaze")
  2. Building the room type matching a wish causes the corresponding citizen's wish to resolve and their speech bubble to clear
  3. An unfulfilled wish stays visible on the citizen indefinitely with no negative consequence to credits, happiness, or anything else
  4. Active wishes span at least 3 of the 4 categories (social, comfort, curiosity, variety) across a normal play session of 10+ minutes
  5. WishBoard Autoload tracks all active wishes and can report the current wish list without iterating citizens directly
**Plans**: TBD

### Phase 7: Happiness and Progression
**Goal**: Station happiness is tracked visibly, fulfilling wishes raises it, happiness gates citizen arrivals and unlocks new room blueprints at observable milestones
**Depends on**: Phase 6
**Requirements**: PROG-01, PROG-02, PROG-03
**Success Criteria** (what must be TRUE):
  1. A happiness value is displayed to the player and visibly increases when a wish is fulfilled
  2. New citizens arrive passively as happiness grows — the player can observe the population count rising without taking any action beyond building
  3. At least 2 distinct blueprint unlock moments occur during a 20-minute play session at expected build pace, each adding at least 1 new room type to the build panel
  4. Higher station happiness produces a visible (but not dominant) multiplier effect on credit income compared to low happiness
**Plans**: TBD

### Phase 8: Polish and Loop Closure
**Goal**: The full build-wish-grow loop feels cozy and complete — save/load works, ambient audio sets the tone, the HUD is fully wired, and the loop closure moment (wish fulfilled) is emotionally satisfying
**Depends on**: Phase 7
**Requirements**: (none — cross-cutting integration of all prior phases)
**Success Criteria** (what must be TRUE):
  1. The player can quit the game and resume from exactly the same state (ring layout, citizens, happiness, credits) via autosave/load
  2. Ambient sound plays continuously and the placement snap sound triggers on every room placement without audio gaps or double-triggers
  3. The HUD displays credits, happiness, and population count, and all three update in real time as the game state changes
  4. The moment a wish is fulfilled produces a noticeable positive feedback response (sound, particle, or UI flash) that feels like a small reward
  5. After 30 minutes of play, the game feels cozy — no punishment, no stress, visible growth, and a genuine desire to keep building
**Plans**: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation and Project Architecture | 3/3 | Complete | 2026-03-02 |
| 2. Ring Geometry and Segment Grid | 2/2 | Complete | 2026-03-02 |
| 3. Economy Foundation | 3/3 | Complete | 2026-03-03 |
| 4. Room Placement and Build Interaction | 0/4 | Not started | - |
| 5. Citizens and Navigation | 0/TBD | Not started | - |
| 6. Wish System | 0/TBD | Not started | - |
| 7. Happiness and Progression | 0/TBD | Not started | - |
| 8. Polish and Loop Closure | 0/TBD | Not started | - |

---
*Roadmap created: 2026-03-02*
*Last updated: 2026-03-03 after Phase 3 completion*
