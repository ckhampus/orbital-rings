# Roadmap: Orbital Rings

## Milestones

- ✅ **v1.0 MVP** — Phases 1-9 (shipped 2026-03-04)
- 🚧 **v1.1 Happiness v2** — Phases 10-13 (in progress)

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

### 🚧 v1.1 Happiness v2 (In Progress)

**Milestone Goal:** Replace the single happiness percentage with a dual-value system (Lifetime Happiness + Station Mood) so the core loop stays alive indefinitely.

- [x] **Phase 10: Happiness Core and Mood Tiers** — Refactor HappinessManager with dual-value state, mood decay, rising baseline, tier system with hysteresis, and wish-count blueprint unlocks (completed 2026-03-04)
- [x] **Phase 11: Economy and Arrival Tier Integration** — Wire citizen arrivals and income multiplier to the new mood tier API (completed 2026-03-04)
- [x] **Phase 12: Save Format** — Store lifetime happiness and mood values in save data (completed 2026-03-05)
- [ ] **Phase 13: HUD Replacement** — Replace HappinessBar with lifetime wish counter and mood tier display

## Phase Details

### Phase 10: Happiness Core and Mood Tiers
**Goal**: Players experience a dual happiness system where wishes permanently count toward lifetime happiness while station mood fluctuates with activity and settles at an earned baseline
**Depends on**: Phase 9 (v1.0 complete)
**Requirements**: HCORE-01, HCORE-02, HCORE-03, HCORE-04, HCORE-05, TIER-01, TIER-04
**Success Criteria** (what must be TRUE):
  1. Fulfilling a wish increments a lifetime happiness counter that never decreases
  2. Station mood rises on wish fulfillment and gently decays toward a baseline that rises with accumulated wishes
  3. The game recognizes five distinct mood tiers (Quiet/Cozy/Lively/Vibrant/Radiant) based on current mood value
  4. Mood tier does not oscillate rapidly when mood hovers near a tier boundary (hysteresis prevents chatter)
  5. Blueprint unlocks trigger at wish counts 4 and 12 instead of the old percentage thresholds
**Plans**: 2 plans

Plans:
- [ ] 10-01-PLAN.md — Contracts layer: MoodTier enum, HappinessConfig resource, default .tres, GameEvents v2 events
- [ ] 10-02-PLAN.md — Core logic: MoodSystem POCO, HappinessManager refactor with dual-value state and _Process wiring

### Phase 11: Economy and Arrival Tier Integration
**Goal**: The station's mood tier visibly affects gameplay by governing how quickly new citizens arrive and how much income rooms generate
**Depends on**: Phase 10
**Requirements**: TIER-02, TIER-03
**Success Criteria** (what must be TRUE):
  1. Citizens arrive more frequently at higher mood tiers than at lower ones
  2. Room income scales by a tier-based multiplier (1.0x at Quiet up to 1.4x at Radiant)
  3. Changing mood tier immediately changes arrival rate and income without requiring save/load
**Plans**: 3 plans

Plans:
- [ ] 11-01-PLAN.md — Economy income tier: EconomyConfig multiplier fields, EconomyManager.SetMoodTier(), replace income formula
- [ ] 11-02-PLAN.md — Arrival probability tier: HappinessConfig arrival fields, replace arrival formula with per-tier lookup
- [ ] 11-03-PLAN.md — Wiring: HappinessManager calls SetMoodTier() on tier change in all three paths (_Process, OnWishFulfilled, RestoreState)

### Phase 12: Save Format
**Goal**: Game state persists correctly across sessions with the new happiness values
**Depends on**: Phase 10
**Requirements**: SAVE-01
**Success Criteria** (what must be TRUE):
  1. Saving and loading preserves the lifetime happiness count exactly
  2. Saving and loading preserves mood and baseline values so the station resumes at the correct tier
  3. A fresh new game starts with zero lifetime happiness and Quiet mood tier
**Plans**: 1 plan

Plans:
- [ ] 12-01-PLAN.md — SaveData v2 fields, version-gated restore, autosave event rewiring, dead shim cleanup

### Phase 13: HUD Replacement
**Goal**: Players can see their permanent progress and current station mood at a glance without opening any menu
**Depends on**: Phase 10, Phase 11
**Requirements**: HUD-01, HUD-02
**Success Criteria** (what must be TRUE):
  1. The HUD displays a lifetime wish counter as a heart-number that pulses on each wish fulfilled
  2. The HUD displays the current mood tier name in a color that matches the tier
  3. The old single-bar happiness display is gone — replaced entirely by the new widgets
**Plans**: TBD

Plans:
- [ ] 13-01: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 10 -> 11 -> 12 -> 13
Note: Phases 11 and 12 both depend only on Phase 10 and could execute in either order.

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 1. Foundation and Project Architecture | v1.0 | 3/3 | Complete | 2026-03-02 |
| 2. Ring Geometry and Segment Grid | v1.0 | 2/2 | Complete | 2026-03-02 |
| 3. Economy Foundation | v1.0 | 3/3 | Complete | 2026-03-03 |
| 4. Room Placement and Build Interaction | v1.0 | 4/4 | Complete | 2026-03-03 |
| 5. Citizens and Navigation | v1.0 | 3/3 | Complete | 2026-03-03 |
| 6. Wish System | v1.0 | 3/3 | Complete | 2026-03-03 |
| 7. Happiness and Progression | v1.0 | 2/2 | Complete | 2026-03-03 |
| 8. Polish and Loop Closure | v1.0 | 3/3 | Complete | 2026-03-04 |
| 9. Wire Work Bonus and Tech Debt Cleanup | v1.0 | 2/2 | Complete | 2026-03-04 |
| 10. Happiness Core and Mood Tiers | 2/2 | Complete    | 2026-03-04 | - |
| 11. Economy and Arrival Tier Integration | 3/3 | Complete    | 2026-03-04 | - |
| 12. Save Format | 1/1 | Complete   | 2026-03-05 | - |
| 13. HUD Replacement | v1.1 | 0/? | Not started | - |

---
*Roadmap created: 2026-03-02*
*Last updated: 2026-03-05 after Phase 12 planned (1 plan)*
