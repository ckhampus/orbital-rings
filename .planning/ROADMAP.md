# Roadmap: Orbital Rings

## Milestones

- ✅ **v1.0 MVP** — Phases 1-9 (shipped 2026-03-04)
- ✅ **v1.1 Happiness v2** — Phases 10-13 (shipped 2026-03-05)
- ✅ **v1.2 Housing** — Phases 14-19 (shipped 2026-03-06)
- ✅ **v1.3 Testing** — Phases 20-25 (shipped 2026-03-07)
- 🚧 **v1.4 Citizen AI** — Phases 26-32 (in progress)

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

<details>
<summary>✅ v1.2 Housing (Phases 14-19) — SHIPPED 2026-03-06</summary>

- [x] Phase 14: Housing Foundation (1/1 plan) — completed 2026-03-06
- [x] Phase 15: HousingManager Core (3/3 plans) — completed 2026-03-06
- [x] Phase 16: Capacity Transfer (1/1 plan) — completed 2026-03-06
- [x] Phase 17: Return-Home Behavior (1/1 plan) — completed 2026-03-06
- [x] Phase 18: Housing UI (1/1 plan) — completed 2026-03-06
- [x] Phase 19: Save/Load Integration (1/1 plan) — completed 2026-03-06

</details>

<details>
<summary>✅ v1.3 Testing (Phases 20-25) — SHIPPED 2026-03-07</summary>

- [x] Phase 20: Test Framework Wiring (2/2 plans) — completed 2026-03-07
- [x] Phase 21: Integration Test Infrastructure (2/2 plans) — completed 2026-03-07
- [x] Phase 22: Mood System Unit Tests (1/1 plan) — completed 2026-03-07
- [x] Phase 23: Economy and Housing Unit Tests (1/1 plan) — completed 2026-03-07
- [x] Phase 24: Save/Load Serialization Tests (1/1 plan) — completed 2026-03-07
- [x] Phase 25: Singleton Integration Tests (2/2 plans) — completed 2026-03-07

</details>

### v1.4 Citizen AI (In Progress)

**Milestone Goal:** Make citizens feel alive with observable daily routines shaped by personal traits, layered on a visible day/night cycle that gives the station rhythm.

- [ ] **Phase 26: Station Clock Foundation** — Time authority singleton with four periods, configurable timing, and HUD indicator
- [ ] **Phase 27: Day/Night Visuals** — Lighting and atmosphere transitions that make the station's time of day visible to the player
- [ ] **Phase 28: Citizen Traits** — Interest and Rhythm trait assignment at creation with info panel display
- [ ] **Phase 29: Schedule and Scoring** — Period-weighted activity templates and utility-based room selection
- [ ] **Phase 30: Citizen State Machine** — Explicit state machine replacing boolean-flag behavior with schedule-aware decisions
- [ ] **Phase 31: Save/Load v4** — Persist clock position and citizen traits with backward-compatible v3 migration
- [ ] **Phase 32: Room Tooltip Visitors** — Room hover tooltip showing names of citizens currently visiting

## Phase Details

### Phase 26: Station Clock Foundation
**Goal**: Players can observe time passing on their station through a visible four-period day cycle
**Depends on**: Nothing (foundation for v1.4)
**Requirements**: CLOCK-01, CLOCK-02, CLOCK-03
**Success Criteria** (what must be TRUE):
  1. Station visibly cycles through Morning, Day, Evening, and Night periods in an 8-minute loop
  2. Player can see the current period via an icon/label in the HUD at all times
  3. Period durations and cycle length are tunable in the Inspector without code changes
  4. Other systems can query StationClock.Instance.CurrentPeriod and subscribe to PeriodChanged events
**Plans**: 2 plans

Plans:
- [ ] 26-01-PLAN.md — Clock core: StationPeriod enum, ClockConfig resource, StationClock autoload, GameEvents PeriodChanged, unit tests
- [ ] 26-02-PLAN.md — HUD + wiring: ClockHUD widget, QuickTestScene integration, autoload registration, visual verification

### Phase 27: Day/Night Visuals
**Goal**: The station's appearance changes with the time of day, making the clock feel real and the station feel alive
**Depends on**: Phase 26 (requires StationClock and PeriodChanged event)
**Requirements**: LIT-01, LIT-02
**Success Criteria** (what must be TRUE):
  1. Station lighting smoothly transitions between distinct color and intensity presets at each period boundary
  2. Station backdrop shifts appearance between periods (deeper blue at night, lighter during day)
  3. Transitions feel smooth (no jarring pops) and the station remains warm and visible even at Night
**Plans**: TBD

Plans:
- [ ] 27-01: TBD

### Phase 28: Citizen Traits
**Goal**: Each citizen has a visible personality expressed through two traits that players can discover in the info panel
**Depends on**: Nothing (independent data addition; can proceed in parallel with Phase 27)
**Requirements**: TRAIT-01, TRAIT-02, TRAIT-03
**Success Criteria** (what must be TRUE):
  1. Every citizen is assigned one Interest trait (Curious, Social, Green Thumb, or Industrious) at creation
  2. Every citizen is assigned one Rhythm trait (Night Owl, Early Bird, Homebody, or Wanderer) at creation
  3. Clicking a citizen shows both traits on a single line in the info panel between body type and home
  4. Trait assignment is deterministic from citizen name (name-hash seeding for v3 save migration compatibility)
**Plans**: TBD

Plans:
- [ ] 28-01: TBD

### Phase 29: Schedule and Scoring
**Goal**: Citizens make intelligent room choices influenced by their traits, the time of day, and active wishes
**Depends on**: Phase 26 (CurrentPeriod), Phase 28 (Interest/Rhythm traits)
**Requirements**: BHV-03, BHV-04, BHV-05, BHV-06
**Success Criteria** (what must be TRUE):
  1. Citizens select rooms via utility scoring that considers trait affinity, proximity, recency, wish bonus, and jitter
  2. Schedule templates bias citizen activity by time of day (more visits during Day, more rest at Night)
  3. Rhythm traits visibly modify behavior (Night Owls are more active in evening/night, Early Birds prefer morning)
  4. Wish bonus always outweighs trait affinity so wishes remain the primary driver of citizen behavior
  5. Scoring handles edge cases gracefully (no rooms built, all rooms score equally, housing rooms excluded from visits)
**Plans**: TBD

Plans:
- [ ] 29-01: TBD
- [ ] 29-02: TBD

### Phase 30: Citizen State Machine
**Goal**: Citizens operate via an explicit state machine with observable states, replacing the implicit boolean-flag behavior
**Depends on**: Phase 29 (UtilityScorer and CitizenSchedule must be ready to wire in)
**Requirements**: BHV-01, BHV-02, BHV-07
**Success Criteria** (what must be TRUE):
  1. Citizens cycle through Walking, Evaluating, Visiting, and Resting states with observable transitions
  2. A single unified decision timer (15-30s) drives all state transitions, replacing the old separate visit and home timers
  3. Citizens handle room demolition gracefully in any state (visiting a room that gets demolished returns them to walkway)
  4. Old boolean flags (_isVisiting, _isAtHome, _walkingToHome) and separate timers are fully removed
**Plans**: TBD

Plans:
- [ ] 30-01: TBD
- [ ] 30-02: TBD

### Phase 31: Save/Load v4
**Goal**: All new v1.4 state persists across save/load and v3 saves migrate cleanly without data loss
**Depends on**: Phase 30 (all new state must be finalized before persisting)
**Requirements**: SAVE-01, SAVE-02, SAVE-03
**Success Criteria** (what must be TRUE):
  1. Station clock position is saved and restored on load (game resumes at correct period)
  2. Citizen traits are saved and restored on load (traits survive save/load round-trip)
  3. Loading a v3 save assigns traits deterministically from citizen name hash and defaults clock to Morning
  4. Autosave does not overwrite a valid v3 save with corrupted v4 state during migration
**Plans**: TBD

Plans:
- [ ] 31-01: TBD

### Phase 32: Room Tooltip Visitors
**Goal**: Players can discover which citizens are currently visiting any room by hovering over it
**Depends on**: Phase 30 (state machine emits room entry/exit events correctly)
**Requirements**: UI-01
**Success Criteria** (what must be TRUE):
  1. Hovering over a room shows the names of all citizens currently visiting that room
  2. Tooltip updates in real-time as citizens enter and leave rooms
**Plans**: TBD

Plans:
- [ ] 32-01: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 26 → 27 → 28 → 29 → 30 → 31 → 32
Note: Phases 27 and 28 are independent and may execute in parallel.

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
| 18. Housing UI | v1.2 | 1/1 | Complete | 2026-03-06 |
| 19. Save/Load Integration | v1.2 | 1/1 | Complete | 2026-03-06 |
| 20. Test Framework Wiring | v1.3 | 2/2 | Complete | 2026-03-07 |
| 21. Integration Test Infrastructure | v1.3 | 2/2 | Complete | 2026-03-07 |
| 22. Mood System Unit Tests | v1.3 | 1/1 | Complete | 2026-03-07 |
| 23. Economy and Housing Unit Tests | v1.3 | 1/1 | Complete | 2026-03-07 |
| 24. Save/Load Serialization Tests | v1.3 | 1/1 | Complete | 2026-03-07 |
| 25. Singleton Integration Tests | v1.3 | 2/2 | Complete | 2026-03-07 |
| 26. Station Clock Foundation | 1/2 | In Progress|  | - |
| 27. Day/Night Visuals | v1.4 | 0/TBD | Not started | - |
| 28. Citizen Traits | v1.4 | 0/TBD | Not started | - |
| 29. Schedule and Scoring | v1.4 | 0/TBD | Not started | - |
| 30. Citizen State Machine | v1.4 | 0/TBD | Not started | - |
| 31. Save/Load v4 | v1.4 | 0/TBD | Not started | - |
| 32. Room Tooltip Visitors | v1.4 | 0/TBD | Not started | - |

---
*Roadmap created: 2026-03-02*
*Last updated: 2026-03-07 after Phase 26 planning*
