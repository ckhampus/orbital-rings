# Roadmap: Orbital Rings

## Milestones

- ✅ **v1.0 MVP** — Phases 1-9 (shipped 2026-03-04)
- ✅ **v1.1 Happiness v2** — Phases 10-13 (shipped 2026-03-05)
- ✅ **v1.2 Housing** — Phases 14-19 (shipped 2026-03-06)
- 🚧 **v1.3 Testing** — Phases 20-25 (in progress)

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

### v1.3 Testing (In Progress)

- [x] **Phase 20: Test Framework Wiring** - GoDotTest + GodotTestDriver + Shouldly packages, test runner scene, CLI execution, export exclusion (completed 2026-03-07)
- [x] **Phase 21: Integration Test Infrastructure** - Singleton reset, event cleanup, timer suppression for reliable test isolation (completed 2026-03-07)
- [x] **Phase 22: Mood System Unit Tests** - Pure POCO tests for decay, tiers, hysteresis, wish gain, and state restore (completed 2026-03-07)
- [x] **Phase 23: Economy and Housing Unit Tests** - Pure formula tests for room costs, tick income, demolish refunds, and capacity scaling (completed 2026-03-07)
- [x] **Phase 24: Save/Load Serialization Tests** - JSON round-trip and backward-compatible deserialization across v1/v2/v3 formats (completed 2026-03-07)
- [x] **Phase 25: Singleton Integration Tests** - Housing assignment, demolition reassignment, and mood-economy propagation through live singletons (completed 2026-03-07)

## Phase Details

### Phase 20: Test Framework Wiring
**Goal**: Test infrastructure exists and proves it works — a test runner discovers and executes test classes, runs headless from CLI, and excludes test code from release builds
**Depends on**: Phase 19 (v1.2 complete)
**Requirements**: FRMW-01, FRMW-02, FRMW-03, FRMW-04, FRMW-05
**Success Criteria** (what must be TRUE):
  1. Running `godot --run-tests --quit-on-finish` discovers test classes and exits with code 0
  2. Shouldly assertions compile and execute within test methods (a deliberate failure produces a readable message)
  3. Export build succeeds without including any test files or test dependencies
  4. NuGet restore pulls GoDotTest, GodotTestDriver, and Shouldly without manual intervention
**Plans**: 2 plans

Plans:
- [x] 20-01-PLAN.md — NuGet config, .csproj conditional test compilation, export exclusion
- [x] 20-02-PLAN.md — Test runner scene/script, ComputeCapacity smoke test, CLI verification

### Phase 21: Integration Test Infrastructure
**Goal**: Singleton state isolation is reliable — tests that touch game singletons cannot corrupt each other
**Depends on**: Phase 20
**Requirements**: INTG-01, INTG-02, INTG-03
**Success Criteria** (what must be TRUE):
  1. Calling ResetAllSingletons restores every singleton to a clean initial state (verified by checking fields after reset)
  2. GameEvents has zero subscribers after ClearAllSubscribers is called (no stale delegate leaks between test suites)
  3. Singleton timers (autosave, housing cycle) do not fire during test execution
**Plans**: 2 plans

Plans:
- [x] 21-01-PLAN.md — Reset() methods on all 7 singletons + ClearAllSubscribers() on GameEvents
- [x] 21-02-PLAN.md — TestHelper, GameTestClass, and verification tests proving infrastructure works

### Phase 22: Mood System Unit Tests
**Goal**: MoodSystem POCO logic is regression-proof — decay math, tier transitions, hysteresis, wish gain, and save restore all have passing tests
**Depends on**: Phase 20
**Requirements**: MOOD-01, MOOD-02, MOOD-03, MOOD-04, MOOD-05
**Success Criteria** (what must be TRUE):
  1. Decay test confirms mood value decreases toward baseline at the expected exponential rate over simulated time
  2. Tier transition tests confirm correct tier at each threshold boundary (all 5 tiers covered)
  3. Hysteresis test confirms a mood value just below a tier boundary does not demote until crossing the hysteresis gap
  4. Wish gain test confirms mood increments by the correct amount per wish fulfilled
  5. Restore test confirms MoodSystem reconstructs correct mood value and tier from saved data
**Plans**: 1 plan

Plans:
- [x] 22-01-PLAN.md — MoodSystemTests.cs with ~15 test methods covering decay, tiers, hysteresis, wish gain, restore, and edge cases

### Phase 23: Economy and Housing Unit Tests
**Goal**: Economy formulas and housing capacity math are regression-proof — all pure calculations have passing tests
**Depends on**: Phase 20
**Requirements**: ECON-01, ECON-02, ECON-03, HOUS-01
**Success Criteria** (what must be TRUE):
  1. CalculateRoomCost returns expected values for every room type at each valid segment size (1/2/3)
  2. CalculateTickIncome applies the correct multiplier for each of the 5 mood tiers
  3. CalculateDemolishRefund returns the correct partial refund for each room type and size
  4. ComputeCapacity returns 2 for 1-segment, 3 for 2-segment, and 4 for 3-segment rooms
**Plans**: 1 plan

Plans:
- [x] 23-01-PLAN.md — EconomyTests.cs with ~44 test methods (room costs, tick income, demolish refund) + HousingTests.cs expansion (capacity for all segment sizes)

### Phase 24: Save/Load Serialization Tests
**Goal**: Save data integrity is regression-proof — JSON round-trips preserve all fields and legacy formats deserialize with correct defaults
**Depends on**: Phase 20
**Requirements**: SAVE-01, SAVE-02, SAVE-03
**Success Criteria** (what must be TRUE):
  1. A v3 SaveData round-trips through JSON serialization and deserialization with every field value preserved exactly
  2. Hand-crafted v1 JSON (missing MoodValue, MoodTier, HomeSegmentIndex fields) deserializes with correct v2/v3 defaults
  3. Hand-crafted v2 JSON (missing HomeSegmentIndex field) deserializes with correct v3 default (null)
**Plans**: 1 plan

Plans:
- [x] 24-01-PLAN.md — SaveDataTests.cs with 4 test methods (v3 round-trip, v1 backward compat, v2 backward compat, empty collections edge case)

### Phase 25: Singleton Integration Tests
**Goal**: Cross-singleton coordination is verified — housing assignment, demolition reassignment, and mood-economy propagation work through live autoload singletons
**Depends on**: Phase 21, Phase 22, Phase 23
**Requirements**: INTG-04, INTG-05, INTG-06
**Success Criteria** (what must be TRUE):
  1. After adding housing rooms and triggering citizen assignment, citizens distribute with fewest-occupants-first (no room exceeds others by more than 1 occupant)
  2. After demolishing a room with assigned citizens, those citizens are reassigned to remaining rooms (no orphaned assignments)
  3. After changing mood tier via wish fulfillment, the economy manager applies the new tier's income multiplier on the next tick
**Plans**: 2 plans

Plans:
- [ ] 25-01-PLAN.md — Event re-subscription infrastructure (SubscribeToEvents, SeedRoomForTest, ResubscribeAllSingletons)
- [ ] 25-02-PLAN.md — SingletonIntegrationTests.cs with ~8 tests for housing assignment, demolition, and mood-economy propagation

## Progress

**Execution Order:**
Phases execute in numeric order: 20 → 21 → 22 → 23 → 24 → 25
Note: Phases 22, 23, and 24 depend only on Phase 20 and could execute in parallel, but are numbered sequentially for simplicity. Phase 25 depends on 21, 22, and 23.

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
| 25. Singleton Integration Tests | 2/2 | Complete    | 2026-03-07 | - |

---
*Roadmap created: 2026-03-02*
*Last updated: 2026-03-07 after Phase 25 planning*
