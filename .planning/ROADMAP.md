# Roadmap: Orbital Rings

## Milestones

- ✅ **v1.0 MVP** — Phases 1-9 (shipped 2026-03-04)
- ✅ **v1.1 Happiness v2** — Phases 10-13 (shipped 2026-03-05)
- 🚧 **v1.2 Housing** — Phases 14-19 (in progress)

## Phases

<details>
<summary>✅ v1.0 MVP (Phases 1-9) — SHIPPED 2026-03-04</summary>

- [x] Phase 1: Foundation and Project Architecture (3/3 plans) — completed 2026-03-02
- [x] Phase 2: Ring Geometry and Segment Grid (2/2 plans) — completed 2026-03-02
- [x] Phase 3: Economy Foundation (3/3 plans) — completed 2026-03-03
- [x] Phase 4: Room Placement and Build Interaction (4/4 plans) — completed 2026-03-03
- [x] Phase 5: Citizens and Navigation (3/3 plans) — completed 2026-03-03
- [x] Phase 6: Wish System (3/3 plans) — completed 2026-03-03
- [x] Phase 7: Happiness and Progression (2/2 plans) — completed 2026-03-03
- [x] Phase 8: Polish and Loop Closure (3/3 plans) — completed 2026-03-04
- [x] Phase 9: Wire Work Bonus and Tech Debt Cleanup (2/2 plans) — completed 2026-03-04

</details>

<details>
<summary>✅ v1.1 Happiness v2 (Phases 10-13) — SHIPPED 2026-03-05</summary>

- [x] Phase 10: Happiness Core and Mood Tiers (2/2 plans) — completed 2026-03-04
- [x] Phase 11: Economy and Arrival Tier Integration (3/3 plans) — completed 2026-03-04
- [x] Phase 12: Save Format (1/1 plan) — completed 2026-03-05
- [x] Phase 13: HUD Replacement (1/1 plan) — completed 2026-03-05

</details>

### v1.2 Housing (In Progress)

**Milestone Goal:** Give each citizen a home room they visibly return to, making housing feel personal and alive.

- [x] **Phase 14: Housing Foundation** - Event signatures, config resource, and save schema for the housing system (completed 2026-03-06)
- [x] **Phase 15: HousingManager Core** - Assignment engine with even-spread algorithm, capacity tracking, and lifecycle events (UAT gap closure in progress) (completed 2026-03-06)
- [x] **Phase 16: Capacity Transfer** - Single source of truth for housing capacity in HousingManager, removing stale ownership from HappinessManager (completed 2026-03-06)
- [x] **Phase 17: Return-Home Behavior** - Citizens periodically walk home, rest with Zzz indicator, and resume their routines (completed 2026-03-06)
- [x] **Phase 18: Housing UI** - Info panel, room tooltip, and population display show housing state to the player (completed 2026-03-06)
- [ ] **Phase 19: Save/Load Integration** - Housing assignments persist across sessions with backward-compatible v3 save format

## Phase Details

### Phase 14: Housing Foundation
**Goal**: All shared types, event signatures, and data schemas exist so subsequent phases can compile against them
**Depends on**: Phase 13 (v1.1 complete)
**Requirements**: INFR-01, INFR-02, INFR-05
**Success Criteria** (what must be TRUE):
  1. HousingConfig resource can be created in the Godot Inspector with tunable timing fields (HomeTimerMin, HomeTimerMax, RestDurationMin, RestDurationMax)
  2. GameEvents has CitizenAssignedHome and CitizenUnhoused event signatures that compile and can be subscribed to
  3. SavedCitizen has a nullable HomeSegmentIndex field that serializes to null (not 0) when unset
**Plans:** 1/1 plans complete
Plans:
- [x] 14-01-PLAN.md — HousingConfig resource, HousingManager skeleton, housing events, save schema v3

### Phase 15: HousingManager Core
**Goal**: Citizens are automatically assigned to housing rooms with even distribution, and reassigned or gracefully unhoused when rooms change
**Depends on**: Phase 14
**Requirements**: HOME-01, HOME-02, HOME-03, HOME-04, HOME-05, INFR-01
**Success Criteria** (what must be TRUE):
  1. When a citizen arrives and housing rooms exist, they are assigned to the room with the fewest occupants
  2. When a housing room is demolished, its residents are reassigned to other housing rooms (or become unhoused if none available)
  3. When a new housing room is built and unhoused citizens exist, the oldest-unhoused citizens are assigned first
  4. A 3-segment housing room holds more citizens than a 1-segment housing room (capacity = BaseCapacity + segments - 1)
  5. Unhoused citizens walk, visit rooms, and fulfill wishes identically to housed citizens
**Plans:** 3/3 plans complete
Plans:
- [x] 15-01-PLAN.md — BuildManager API extension, CitizenNode home property, full HousingManager assignment engine
- [x] 15-02-PLAN.md — SaveManager housing persistence and autosave event wiring
- [x] 15-03-PLAN.md — Fix stale home references on save/load (UAT gap closure)

### Phase 16: Capacity Transfer
**Goal**: HousingManager is the single source of truth for housing capacity, with HappinessManager's stale capacity fields fully removed
**Depends on**: Phase 15
**Requirements**: INFR-03
**Success Criteria** (what must be TRUE):
  1. HappinessManager no longer tracks housing capacity or subscribes to room placement/demolish events for capacity purposes
  2. Citizen arrival gating queries HousingManager for current capacity (not HappinessManager)
  3. Building or demolishing housing rooms updates capacity in one place only, with no desynchronization between systems
**Plans:** 1/1 plans complete
Plans:
- [x] 16-01-PLAN.md — Remove capacity tracking from HappinessManager, redirect arrival gating to HousingManager, clean up SaveManager/TitleScreen

### Phase 17: Return-Home Behavior
**Goal**: Citizens visibly return to their home room on a periodic cycle, rest with a sleeping indicator, and resume normal behavior without disrupting the wish loop
**Depends on**: Phase 15
**Requirements**: BEHV-01, BEHV-02, BEHV-03, BEHV-04
**Success Criteria** (what must be TRUE):
  1. Housed citizens periodically walk to their home room (approximately every 90-150 seconds) and rest there for 8-15 seconds
  2. A "Zzz" text floater appears above citizens while they rest at home, distinguishing home visits from regular room visits
  3. Citizens do not generate new wishes while resting at home (wish timer paused)
  4. If a citizen has an active wish to fulfill, they skip or defer their home return until the wish is handled
**Plans:** 1/1 plans complete
Plans:
- [x] 17-01-PLAN.md — Home timer, StartHomeReturn tween, Zzz Label3D indicator, priority/interruption handling, CitizenManager wiring

### Phase 18: Housing UI
**Goal**: Players can see which citizen lives where, who occupies each housing room, and overall population vs capacity at a glance
**Depends on**: Phase 15
**Requirements**: UI-01, UI-02, UI-03
**Success Criteria** (what must be TRUE):
  1. Clicking a citizen shows their home room name and location in the info panel (or "No home" if unhoused)
  2. Hovering over a housing room tooltip shows the names of citizens currently assigned to that room
  3. The population display shows a "count/capacity" format (e.g., "5/7") so the player knows how much housing room remains
**Plans:** 1/1 plans complete
Plans:
- [x] 18-01-PLAN.md — Home label in CitizenInfoPanel, room-aware tooltip in SegmentInteraction, housed/capacity PopulationDisplay

### Phase 19: Save/Load Integration
**Goal**: Housing assignments survive save-quit-load cycles, old saves load cleanly, and stale assignments are corrected automatically
**Depends on**: Phase 16, Phase 17, Phase 18
**Requirements**: INFR-04, INFR-05
**Success Criteria** (what must be TRUE):
  1. After saving and reloading, every citizen retains their home room assignment and resumes the home-return cycle
  2. Loading a v2 save (pre-housing) results in all citizens starting as unhoused, then being assigned homes from scratch as if they just arrived
  3. If a saved assignment references a demolished room (stale data), the citizen is marked unhoused and reassigned automatically on load
**Plans**: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 14 -> 15 -> 16 -> 17 -> 18 -> 19
Note: Phases 16, 17, and 18 depend only on Phase 15 (not each other) but are sequenced 16 first to surface capacity-transfer integration risks early, then 17 for the largest feature, then 18 for UI polish. Phase 19 depends on all prior phases.

| Phase | Milestone | Plans | Status | Completed |
|-------|-----------|-------|--------|-----------|
| 1. Foundation and Project Architecture | v1.0 | 3/3 | Complete | 2026-03-02 |
| 2. Ring Geometry and Segment Grid | v1.0 | 2/2 | Complete | 2026-03-02 |
| 3. Economy Foundation | v1.0 | 3/3 | Complete | 2026-03-03 |
| 4. Room Placement and Build Interaction | v1.0 | 4/4 | Complete | 2026-03-03 |
| 5. Citizens and Navigation | v1.0 | 3/3 | Complete | 2026-03-03 |
| 6. Wish System | v1.0 | 3/3 | Complete | 2026-03-03 |
| 7. Happiness and Progression | v1.0 | 2/2 | Complete | 2026-03-03 |
| 8. Polish and Loop Closure | v1.0 | 3/3 | Complete | 2026-03-04 |
| 9. Wire Work Bonus and Tech Debt Cleanup | v1.0 | 2/2 | Complete | 2026-03-04 |
| 10. Happiness Core and Mood Tiers | v1.1 | 2/2 | Complete | 2026-03-04 |
| 11. Economy and Arrival Tier Integration | v1.1 | 3/3 | Complete | 2026-03-04 |
| 12. Save Format | v1.1 | 1/1 | Complete | 2026-03-05 |
| 13. HUD Replacement | v1.1 | 1/1 | Complete | 2026-03-05 |
| 14. Housing Foundation | v1.2 | 1/1 | Complete | 2026-03-06 |
| 15. HousingManager Core | v1.2 | 3/3 | Complete | 2026-03-06 |
| 16. Capacity Transfer | v1.2 | 1/1 | Complete | 2026-03-06 |
| 17. Return-Home Behavior | v1.2 | 1/1 | Complete | 2026-03-06 |
| 18. Housing UI | v1.2 | Complete    | 2026-03-06 | 2026-03-06 |
| 19. Save/Load Integration | v1.2 | 0/TBD | Not started | - |

---
*Roadmap created: 2026-03-02*
*Last updated: 2026-03-06 after Phase 18-01 complete*
