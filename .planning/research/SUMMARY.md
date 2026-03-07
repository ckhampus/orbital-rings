# Project Research Summary

**Project:** Orbital Rings v1.3 — Testing Infrastructure Milestone
**Domain:** Testing infrastructure for Godot 4 C# game (autoload singleton architecture)
**Researched:** 2026-03-07
**Confidence:** HIGH

## Executive Summary

Orbital Rings is a 9,500+ LOC Godot 4.6 / C# / .NET 10 cozy space station builder with a mature event-driven singleton architecture. The v1.3 testing milestone adds a GoDotTest + GodotTestDriver foundation to catch regressions as the codebase grows. The recommended approach is the Chickensoft ecosystem — GoDotTest 2.0.30 as the in-process test runner, GodotTestDriver 3.1.62 for scene lifecycle management, and Shouldly 4.3.0 for readable assertions. This stack is the de facto standard for Godot C# testing and is verified compatible with the project's Godot 4.6.1 / .NET 10 target. No mocking framework is needed: the existing code has well-separated pure logic (MoodSystem POCO, economy formulas, housing capacity) that can be tested directly without interface extraction or DI refactoring.

The fundamental challenge is that all 8 autoload singletons use static `Instance` properties and run continuously inside the Godot process that GoDotTest shares. Tests that mutate singleton state will corrupt each other unless defensive infrastructure is built first. Every pitfall identified traces back to this single root cause: GoDotTest runs all tests in one long-lived process, meaning singletons, their child Timers, and their C# event subscriptions all persist across test suites. The solution is a `TestHelper.ResetAllSingletons()` utility and a `GameEvents.ClearAllSubscribers()` method that must be called in `[SetupAll]` for any test suite that touches game state.

The codebase already exhibits the key testability property needed to make the milestone tractable: MoodSystem is a POCO, SaveData/SavedCitizen/SavedRoom are plain C# types, and EconomyManager's income/cost formulas are pure functions. These targets yield the highest-value tests at the lowest implementation cost. The save/load system (3 format versions, backward compatibility, nullable HomeSegmentIndex) is the highest-severity risk area and should be the focus of dedicated serialization tests that bypass the filesystem entirely. Integration tests with the actual scene tree should be scoped narrowly to housing assignment flow and mood tier transitions.

## Key Findings

### Recommended Stack

See `.planning/research/STACK.md` for full details and version verification.

The project needs three NuGet packages added to `Orbital Rings.csproj`, plus `nuget.org` added to `NuGet.Config` (currently only has the local GodotSharp source). All packages target `netstandard2.1` or `net8.0`, which is forward-compatible with the project's `net10.0` target. The test runner uses Pattern A — a dedicated `test/TestRunner.tscn` scene run with F6 or `godot --run-tests --quit-on-finish` — rather than modifying the TitleScreen entry point.

**Core technologies:**
- `Chickensoft.GoDotTest 2.0.30`: In-process test runner — the only C#-native runner designed to run inside Godot's scene tree; required for any test touching Godot APIs
- `Chickensoft.GodotTestDriver 3.1.62`: Scene lifecycle management — provides `Fixture.AutoFree()`, `Fixture.Cleanup()`, and frame-waiting helpers for integration tests
- `Shouldly 4.3.0`: Assertion library — MIT licensed, readable `.ShouldBe()` syntax, zero Godot coupling; preferred over FluentAssertions (license concerns) and raw exceptions (unreadable failures)

**Critical NuGet.Config change required:** Add `https://api.nuget.org/v3/index.json` as a package source. Without this, `dotnet restore` cannot find any of the testing packages.

### Expected Features

See `.planning/research/FEATURES.md` for full testability assessment of all 40+ production classes.

**Must have (table stakes):**
- Test runner scene with CLI execution — unblocks all test work; gates the entire milestone
- Unit tests for MoodSystem POCO — decay, tier transitions, hysteresis, wish gain/restore; most valuable single suite
- Economy formula tests — CalculateTickIncome, CalculateRoomCost, CalculateDemolishRefund across all 5 mood tiers
- Save/load round-trip tests — v1, v2, v3 format serialization; null vs 0 HomeSegmentIndex distinction
- Save format version migration tests — v1/v2 JSON missing fields must deserialize with correct defaults
- Test isolation via singleton reset — foundational requirement; without it all subsequent tests are unreliable
- Release build exclusion — test code must not ship in exported game builds

**Should have (competitive):**
- Housing assignment integration tests — fewest-occupants-first algorithm, demolish displacement, reassignment
- Mood tier transition integration tests — verify SetMoodTier propagation through event chain
- Event bus verification tests — confirm GameEvents emits correct events with correct args
- Stale save reference handling — three code paths in RestoreFromSave that cover unhoused citizens on load
- Debug launch configurations — VSCode entries for "Debug Tests" and "Debug Current Test"

**Defer (v2+):**
- Wish fulfillment end-to-end test — requires full scene with citizen spawning and wish template loading
- Code coverage collection — useful but not blocking; the `--coverage` flag is available when ready
- CI pipeline — can be added independently after local tests pass
- Custom test drivers for game-specific nodes — only needed if integration tests become brittle

### Architecture Approach

See `.planning/research/ARCHITECTURE.md` for full component specs, data flow diagrams, and implementation code.

The architecture classifies tests into three tiers by dependency level. Tier 1 (pure C# tests in `test/Unit/`) covers POCOs and pure functions with zero engine deps — instant, reliable, highest ROI. Tier 2 (singleton-aware tests in `test/Integration/`) covers singleton behavior with controlled state using `TestHelper.ResetAllSingletons()` in `[SetupAll]`. Tier 3 (scene tests in `test/System/`) covers full save/load round-trips and multi-singleton flows using GodotTestDriver `Fixture` for managed node lifecycle. The three-tier classification must be established upfront; mixing tiers creates cascading setup overhead.

**Major components:**
1. `test/TestRunner.cs + TestRunner.tscn` — GoDotTest entry point; minimal Node2D scene that calls `GoTest.RunTests(Assembly.GetExecutingAssembly(), this)` in `_Ready()`
2. `test/Helpers/TestHelper.cs` — `ResetAllSingletons()` and `WaitFrames()` utilities; must be built before any Tier 2/3 tests; calls existing public APIs (RestoreCredits, ClearCitizens, RestoreActiveWishes, etc.)
3. `test/Helpers/SaveDataBuilder.cs` — Fluent builder for SaveData test fixtures with version-specific factory methods (CreateV1/V2/V3); eliminates repetitive POCO construction
4. `test/Helpers/Assert.cs` (or Shouldly) — Assertion library; Shouldly is recommended for richer failure messages
5. `test/Unit/` — Five test classes covering MoodSystem, SegmentGrid, save serialization, economy formulas, housing capacity
6. `test/Integration/` — Three test classes covering EconomyManager, HousingManager, HappinessManager with singleton reset pattern
7. `test/System/` — SaveLoadRoundTripTest covering v1/v2/v3 format compatibility with SaveDataBuilder fixtures

**BuildManager limitation:** `ClearAllRooms()` and `RestorePlacedRoom()` require a non-null `_ringVisual` reference that does not exist in the test runner scene. Workaround: test room-related behavior via SaveData construction rather than placement API, and reserve full scene loading for explicit Tier 3 integration tests only.

### Critical Pitfalls

See `.planning/research/PITFALLS.md` for full prevention strategies and recovery costs.

1. **Singleton state leaking between test suites** — All 8 autoloads persist for the entire test process; state from Test A contaminates Test B. Prevention: build `TestHelper.ResetAllSingletons()` before writing any test; some singletons need `#if DEBUG`-guarded `ResetForTesting()` methods added since not all reset APIs currently exist.

2. **C# event delegate subscriber accumulation** — GameEvents uses `event Action<T>` delegates (not Godot signals), so Godot's automatic signal disconnection on node free does NOT apply. Freed nodes remain as stale subscribers and throw `ObjectDisposedException` when events fire. Prevention: add `GameEvents.ClearAllSubscribers()` called in `ResetAllSingletons()`; use a disposable `EventSpy<T>` helper for event observation in tests.

3. **Autoload Timers firing during tests** — Three autoloads create child Timers (income: 5.5s, arrival: 60s, save debounce: 0.5s). These fire continuously during the test run, causing credit amounts to drift and real save files to be overwritten. Prevention: stop all Timers in `ResetAllSingletons()`; add `SaveManager.SavePathOverride` to redirect writes to a temp path during tests.

4. **Scene tree lifecycle mismatch** — The test runner scene lacks RingVisual, Camera3D, segment grid, and walkway mesh. Singletons that search the tree in `_Ready()` get null references. Prevention: enforce the three-tier classification strictly; Tier 1 tests must never import Godot namespaces; BuildManager-dependent tests require the game scene via GodotTestDriver Fixture.

5. **Save format edge cases missing from tests** — System.Text.Json silently defaults missing fields to zero/null, meaning a test that constructs a `SaveData` object with all fields set never exercises the actual v1/v2 backward-compatibility paths. Prevention: test deserialization from hand-crafted raw JSON strings that genuinely omit v2/v3 fields, not from programmatically constructed objects.

## Implications for Roadmap

Based on all four research files, the build order is tightly constrained. Infrastructure work must precede all test writing because of the singleton-isolation requirement — writing tests on a shaky foundation produces unreliable results that undermine confidence in the entire milestone.

### Phase 1: Framework Wiring

**Rationale:** All subsequent phases depend on a working test runner and reliable singleton isolation. The #1 pitfall (state leakage) and #3 pitfall (timer interference) must be resolved before the first test is written. "Prove infrastructure works" is the gate criterion.
**Delivers:** `godot --run-tests --quit-on-finish` runs, discovers 0 tests, exits with code 0. Timer suppression confirmed. Event cleanup in place.
**Addresses:** Test runner scene, CLI execution, release build exclusion, debug launch configs
**Avoids:** Pitfalls 1 (singleton state), 2 (event accumulation), 3 (scene tree mismatch), 4 (timer interference), 5 (save file corruption), 7 (frame advancement)
**Changes required:** Add `nuget.org` to NuGet.Config; add 3 PackageReferences to .csproj; create `test/TestRunner.cs` + `test/TestRunner.tscn`; create `test/Helpers/TestHelper.cs` (with `ResetAllSingletons()`, `WaitFrames()`); create `test/Helpers/Assert.cs` or confirm Shouldly works; add `#if DEBUG`-guarded `ResetForTesting()` methods to singletons that lack full reset APIs; add `GameEvents.ClearAllSubscribers()`; add `SaveManager.SavePathOverride`

### Phase 2: Test Helpers and Pure C# Test Suites

**Rationale:** Tier 1 tests have zero engine dependency risk — they run instantly and can only fail if the logic is wrong. These tests deliver maximum coverage per line of test code. SaveDataBuilder must be built alongside these tests because save serialization tests depend on it.
**Delivers:** ~15 passing tests covering MoodSystem, economy formulas, housing capacity, and save serialization. Confidence that core game math is regression-proof.
**Uses:** Shouldly assertions, HappinessConfig/EconomyConfig/HousingConfig constructors, MoodSystem POCO, SaveData POCOs
**Implements:** `test/Helpers/SaveDataBuilder.cs`; `test/Unit/MoodSystemTest.cs` (decay, tier transitions, hysteresis, boundary conditions); `test/Unit/EconomyFormulaTest.cs` (CalculateRoomCost, CalculateTickIncome, CalculateDemolishRefund, all 5 tier multipliers); `test/Unit/HousingCapacityTest.cs` (ComputeCapacity for all segment sizes); `test/Unit/SaveDataSerializationTest.cs` (raw JSON fixtures for v1/v2/v3, null HomeSegmentIndex, missing-field defaults)
**Avoids:** Pitfall 6 (save format edge cases) by deserializing from raw JSON strings, not round-tripped objects
**Research flag:** Standard patterns — no phase research needed

### Phase 3: Save/Load Round-Trip Tests

**Rationale:** Save corruption is the highest-severity failure mode in the game. A serialization bug silently destroys player progress. These tests are the milestone's most valuable safety net for future changes. Separated from Phase 2 because save round-trip tests are System-tier (may involve ApplyState/ApplySceneState with frame advancement), requiring TestHelper infrastructure proven in Phase 1.
**Delivers:** Confidence that v1, v2, and v3 saves load with correct field values and defaults. Null HomeSegmentIndex reference handling verified. Backward-compat regression-proof.
**Uses:** SaveDataBuilder, TestHelper.WaitFrames(), GodotTestDriver Fixture (if testing full ApplySceneState path)
**Implements:** `test/System/SaveLoadRoundTripTest.cs` covering V3 full round-trip, V2 missing HomeSegmentIndex, V1 missing mood fields, stale housing reference handling
**Avoids:** Pitfall 5 (real save file) via SavePathOverride; Pitfall 7 (frame advancement) via WaitFrames helper
**Research flag:** Standard patterns — no phase research needed

### Phase 4: Singleton Integration Tests

**Rationale:** These tests exercise the singleton coordination that is hardest to verify manually. Highest complexity tier because they depend on singletons being properly reset and autoloads being available. Deferred until after Tier 1 tests confirm that pure logic is correct — no point debugging integration failures caused by wrong formulas.
**Delivers:** EconomyManager spend/earn/income with state verified. HousingManager assignment/capacity flow verified. HappinessManager tier transitions via event chain verified.
**Uses:** TestHelper.ResetAllSingletons(), public singleton APIs, GameEvents event subscription
**Implements:** `test/Integration/EconomyManagerTest.cs`; `test/Integration/HousingManagerTest.cs`; `test/Integration/HappinessManagerTest.cs`
**Avoids:** Pitfall 1 (singleton leakage) via ResetAllSingletons in [SetupAll]; Pitfall 2 (event accumulation) via ClearAllSubscribers; Anti-Pattern 1 (cross-singleton chains labeled as unit tests)
**Research flag:** Standard patterns — no phase research needed; singleton reset pattern is documented

### Phase Ordering Rationale

- Phase 1 before all others: The singleton isolation infrastructure (ResetAllSingletons, ClearAllSubscribers, SavePathOverride, Timer suppression) is a prerequisite for every subsequent phase. Tests written without it produce unreliable results that erode confidence rather than building it.
- Phase 2 before Phase 3: Pure C# tests have zero engine risk; if they fail, the logic is wrong. Running them first builds baseline confidence before moving to filesystem-touching tests.
- Phase 3 before Phase 4: Save/load correctness is foundational to the game's data integrity. Singleton integration tests depend on knowing the data layer is reliable.
- Phase 4 last: Highest complexity, highest setup requirements. Depends on both the isolation infrastructure (Phase 1) and confirmed-correct pure logic (Phase 2).

### Research Flags

Phases with standard patterns (skip research-phase):
- **All phases:** GoDotTest and GodotTestDriver documentation is thorough and verified. The Chickensoft ecosystem has established patterns for all three test tiers. No domain-specific research needed during planning — implementation details are documented in ARCHITECTURE.md with working code examples.

Phases that may need investigation before execution:
- **Phase 1 (Framework Wiring):** The Godot 4.6 / .NET 10 combination has an open issue (godotengine/godot#112701) about shared framework assembly probing on Windows. Flag for validation during initial NuGet restore. Workaround: testing packages do not depend on `Microsoft.Extensions.*`, so this issue likely does not affect them.
- **Phase 4 (Singleton Integration):** BuildManager's `_ringVisual` dependency makes housing assignment integration tests non-trivial. May need additional investigation on the cleanest workaround (SaveData-based testing vs. loading a minimal game scene with RingVisual).

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All three package versions verified against NuGet flatcontainer API on 2026-03-07. Compatibility with net10.0 confirmed by TFM forward-compatibility rules. One open Godot issue flagged for validation but unlikely to block. |
| Features | HIGH | Based on direct codebase inspection of all 8 autoload singletons, 7 data classes, and MoodSystem POCO. Feature priorities derived from actual code structure, not assumptions. |
| Architecture | MEDIUM-HIGH | GoDotTest/GodotTestDriver APIs verified against NuGet and GitHub READMEs. Singleton testing patterns derived from direct codebase analysis. Some integration patterns (particularly BuildManager workaround) inferred from framework design rather than verified in production. |
| Pitfalls | HIGH | 8 pitfalls identified through direct analysis of 20+ source files, GoDotTest documentation, and documented Godot C# testing community failures. Recovery costs assessed as LOW to MEDIUM for all pitfalls. |

**Overall confidence:** HIGH

### Gaps to Address

- **BuildManager RingVisual dependency:** The cleanest approach for housing assignment integration tests is unresolved. Three options documented in ARCHITECTURE.md (SaveData-based testing, load game scene, add test-only API). Decision should be made in Phase 4 planning with a preference for option 1 (least production code impact).
- **Singleton reset APIs:** Not all 8 singletons have complete public reset APIs. The TestHelper implementation in ARCHITECTURE.md uses the available public APIs, but some singletons may need `#if DEBUG`-guarded `ResetForTesting()` methods added. Exact scope of production code changes needed for complete isolation is TBD until Phase 1 implementation.
- **GoDotTest v2.0.x stability:** NuGet tracking shows 101 versions. Verify no breaking API changes between the version documented in Chickensoft's README and 2.0.30 during initial setup.

## Sources

### Primary (HIGH confidence)
- NuGet flatcontainer API for GoDotTest, GodotTestDriver, Shouldly — version numbers verified 2026-03-07
- Direct codebase analysis: all 8 autoload singletons, MoodSystem.cs, SegmentGrid.cs, SaveData/SavedCitizen/SavedRoom, SafeNode.cs, project.godot
- `Orbital Rings.csproj` — confirmed Godot.NET.Sdk 4.6.1, net10.0 target, EnableDynamicLoading=true
- `project.godot` — confirmed autoload initialization order and count

### Secondary (MEDIUM confidence)
- [Chickensoft GoDotTest GitHub README](https://github.com/chickensoft-games/GoDotTest) — TestClass API, lifecycle hooks, CLI flags, coverage workflow
- [Chickensoft GodotTestDriver GitHub README](https://github.com/chickensoft-games/GodotTestDriver) — Fixture API, input simulation, custom drivers
- [DeepWiki GoDotTest Running Tests](https://deepwiki.com/chickensoft-games/GoDotTest/4-running-tests) — complete test runner configuration
- [Chickensoft GodotGame template](https://github.com/chickensoft-games/GodotGame) — reference project with testing, coverage, CI/CD

### Tertiary (supporting context)
- [Godot Forum: Using GUT with autoload singletons](https://forum.godotengine.org/t/using-gut-to-test-instantiating-scenes-via-autoload-singleton-event-bus-rootscene/86974) — nodes not in tree during tests
- [Godot Forum: C# Event Handlers ObjectDisposedException](https://forum.godotengine.org/t/c-event-handlers-triggering-unhandled-exception-system-objectdisposedexception-cannot-access-a-disposed-object/17794) — disposed node event handler invocation
- [Godot GitHub Issue #112701](https://github.com/godotengine/godot/issues/112701) — .NET 10 shared framework assembly probing (Windows-specific, low risk for testing packages)
- [DEV Community: Don't use Singleton Pattern in your unit tests](https://dev.to/bacarpereira/don-t-use-singleton-pattern-in-your-unit-tests-8p7) — state leaking between tests

---
*Research completed: 2026-03-07*
*Ready for roadmap: yes*

---

---

# V1.2 Housing System Research Summary

*Preserved from 2026-03-05. Covers the v1.2 Housing System milestone research.*

---

**Project:** Orbital Rings v1.2 -- Housing System
**Domain:** Godot 4.4 C# cozy space station builder -- citizen housing assignment and return-home behavior
**Researched:** 2026-03-05
**Confidence:** HIGH

## Executive Summary

The v1.2 Housing System transforms housing rooms from a faceless capacity gate into a personal system where each citizen has a visible home they periodically return to. Research across all four areas confirms this is a well-scoped, low-risk milestone: every required pattern already exists in the production codebase, no new dependencies are needed, and the estimated scope is ~420 LOC across 6-7 files. The recommended approach is to add a new HousingManager autoload as the 8th singleton (between HappinessManager and SaveManager), transfer capacity tracking out of HappinessManager, and layer the return-home behavior onto CitizenNode's existing visit animation pipeline.

The highest-value design decision is keeping housing assignment (HousingManager) strictly separate from capacity gating (currently in HappinessManager). Research reveals HappinessManager holds stale dual-ownership of housing capacity that must be cleaned up during this milestone -- leaving it in place would create a second source of truth for capacity and cause desynchronization bugs as both systems react to the same RoomDemolished events. The capacity transfer must be centralized in HousingManager from Phase 1, not patched on later.

The key risks are all manageable and well-understood: event ordering between co-subscribers on RoomDemolished, tween interruption during rapid demolish-rebuild cycles, home timer priority contention with the wish system, and save format backward compatibility with v2 saves. Each risk has a clear prevention strategy mapped to a specific build phase. The overall risk profile is LOW -- this is a feature addition on a stable, well-tested architecture, not an architectural change.

---

## Key Findings

### Recommended Stack

The housing system requires zero new dependencies. All required technology is already present and validated in the production codebase. See `.planning/research/STACK.md` for full detail.

**Core technologies:**

- **HousingManager (Godot Node, C#):** New autoload singleton following the exact pattern of the 7 existing autoloads. Owns citizen-to-room assignment mapping and bidirectional lookups. Registered in project.godot between HappinessManager and SaveManager.
- **HousingConfig (Godot Resource, [GlobalClass]):** Inspector-tunable timing constants for the return-home cycle. Follows HappinessConfig/EconomyConfig pattern exactly. File at `res://Resources/Housing/default_housing.tres`.
- **Godot Timer (one-shot, re-arming):** Per-citizen home timer (90-150s) following the identical pattern as the existing `_visitTimer` and `_wishTimer` in CitizenNode.
- **Godot Tween:** Return-home animation sequence (9 phases) structurally identical to the existing StartVisit() tween chain -- same kill-before-create pattern, same `_activeTween` field.
- **FloatingText (OrbitalRings.UI.FloatingText):** Reused for the "Zzz" indicator on home entry. Smaller font (14 vs 18), muted lavender color. Screen-space positioning via Camera3D.UnprojectPosition.
- **Dictionary<string, int> / Dictionary<int, List<string>>:** Bidirectional assignment mapping in HousingManager. Citizen names as keys (unique by construction, stable across save/load).
- **System.Text.Json + int? (nullable):** Save format extended with `HomeSegmentIndex` as nullable int on SavedCitizen. Null correctly represents unhoused when deserializing v2 saves that lack the field. Save version bumped to 3.

**Critical API change:** BuildManager.GetPlacedRoom() return tuple must gain a SegmentCount field. Only 2 external call sites; change is additive.

### Expected Features

Research against comparable cozy/builder games (Banished, Oxygen Not Included, Stardew Valley, Spiritfarer, RimWorld, Before We Leave) confirms clear feature tiers. See `.planning/research/FEATURES.md` for full competitive analysis.

**Must have (v1.2 core -- all required for milestone completion):**
- Automatic home assignment on citizen arrival -- even-spread, fewest-occupants-first algorithm
- Reassignment on room demolish -- displaced citizens attempt reassignment or become gracefully unhoused
- Assign unhoused citizens when new housing is built -- oldest-first, fills gaps automatically
- Return-home behavior with tween animation -- periodic 90-150s cycle, 8-15s rest duration
- "Zzz" visual indicator during home rest -- distinguishes home visits from regular room visits
- Home location line in CitizenInfoPanel -- "Home: Bunk Pod (Outer 3)" or "Home: --"
- Unhoused citizens handled gracefully -- no penalty, no mood debuff, no departure
- Save/load housing assignments -- backward-compatible with v2 saves

**Should have (v1.2 enhanced -- implement if time allows):**
- Size-scaled capacity (`BaseCapacity + segmentCount - 1`) -- one line of math, large fairness gain
- HousingConfig resource -- tunable timing constants following established codebase pattern
- Wish timer pause during home rest -- prevents wish bubbles appearing at home room
- Home return lower priority than active wishes -- protects the core wish-fulfillment loop

**Defer to v1.3+:**
- Room tooltip showing current residents (requires SegmentTooltip/SegmentInteraction extension -- moderate scope)
- Home-return path indicator / visual trail
- Citizen roommate display in info panel ("Lives with: Nova, Pixel")

**Anti-features (explicitly excluded by PRD and cozy philosophy):**
- Player-managed room assignments (drag-to-assign) -- adds management burden, violates cozy genre
- Housing quality tiers / comfort levels -- creates optimization anxiety
- Citizen housing preferences or roommate compatibility -- combinatorial optimization problem
- Negative consequences for unhoused citizens -- violates no-fail-state philosophy
- Day/night cycle driving home-return timing -- deferred to future milestone

### Architecture Approach

The system integrates as the 8th autoload in a strict event-driven architecture where all cross-system communication routes through GameEvents (typed C# delegate bus). HousingManager inserts cleanly between HappinessManager (6th) and SaveManager (7th, becoming 8th). The most significant architectural change is transferring housing capacity ownership from HappinessManager to HousingManager -- this removes ~50 LOC of stale HappinessManager code and establishes HousingManager as the single source of truth. See `.planning/research/ARCHITECTURE.md` for full data flows, component boundaries, and build order.

**Major components:**

1. **HousingManager (new autoload, ~180 LOC):** Owns `_assignments` (citizenName -> segmentIndex), `_roomResidents` (segmentIndex -> List<string>), `_roomCapacities` (segmentIndex -> effectiveCapacity), and `_unhousedCitizens` queue. Subscribes to RoomPlaced, RoomDemolished, CitizenArrived. Emits CitizenAssignedHome and CitizenUnhoused.
2. **HousingConfig (new resource, ~30 LOC):** HomeTimerMin/Max (90-150s), RestDurationMin/Max (8-15s). Loaded with ResourceLoader fallback pattern identical to HappinessConfig.
3. **CitizenNode (modified, +80 LOC):** Gains `_homeSegmentIndex`, `_homeTimer`, `_isReturningHome`, `StartReturnHome()` method. Subscribes to CitizenAssignedHome/CitizenUnhoused events filtered by citizen name. Shares `_activeTween` with regular visits (mutually exclusive via guards).
4. **HappinessManager (simplified, -50 LOC net):** Housing capacity tracking fields removed entirely. Queries `HousingManager.Instance.CalculateHousingCapacity()` for arrival gating. RoomPlaced/RoomDemolished subscriptions fully removed if no other logic remains.
5. **SaveManager (modified, +30 LOC):** SavedCitizen gains `int? HomeSegmentIndex`. CollectGameState serializes assignments. ApplySceneState restores with validation. Save version bumped to 3.
6. **UI consumers (minor modifications):** CitizenInfoPanel adds home label (~15 LOC), SegmentInteraction appends residents to housing tooltips (~10 LOC), PopulationDisplay switches from count to count/capacity format (~15 LOC).

**Key patterns (all existing, all reused):**
- Static `Instance` singleton with StateLoaded guard in `_Ready()`
- Config Resource with [GlobalClass], [Export], and fallback ResourceLoader load
- Event-driven communication via GameEvents -- no direct singleton-to-singleton method calls for notifications
- Kill-before-create Tween with shared `_activeTween` field
- Null-safe singleton access (`?.` everywhere)

### Critical Pitfalls

Research identified 8 pitfalls; the top 5 requiring architectural prevention are below. See `.planning/research/PITFALLS.md` for full detail, recovery strategies, and the "Looks Done But Isn't" checklist.

1. **Dual ownership of housing capacity (Pitfall 5)** -- HappinessManager and HousingManager must never both track capacity. Centralize the size-scaled formula (`BaseCapacity + segmentCount - 1`) in one static helper from Phase 1 and fully remove HappinessManager's capacity fields in Phase 3. Warning sign: PopulationDisplay count mismatches arrival gate.

2. **Event ordering race on RoomDemolished (Pitfall 1)** -- Both HappinessManager and HousingManager subscribe to RoomDemolished. Each handler must be self-contained, mutating only its own state. HousingManager must track its own room capacity map, never reading HappinessManager's during the same event frame. Prevention: design HousingManager's data structures to be fully autonomous from day one.

3. **Stale home assignment after save/load (Pitfall 8)** -- Segment indices reference position, not room identity. On load, every saved assignment must be validated: does BuildManager.GetPlacedRoom(index) return a Housing-category room? If not (room demolished between save and load), mark citizen unhoused and attempt reassignment. Validation is mandatory in the restore path.

4. **Save format v3 nullable vs int bug (Pitfall 4)** -- Use `int?` (nullable) for HomeSegmentIndex in SavedCitizen, not `int`. Missing JSON fields deserialize to null (correctly representing unhoused) vs 0 (a valid segment index -- a subtle data corruption bug for v2 saves).

5. **Home timer vs wish/visit priority (Pitfall 3)** -- Three independent timers (visit 20-40s, wish 30-60s, home 90-150s) can fire in rapid succession. Guard the home timer handler with: `if (_currentWish != null) { ResetHomeTimer(); return; }` and `if (_isVisiting) { ResetHomeTimer(); return; }`. The wish-fulfillment loop must never be blocked by home behavior.

---

## Implications for Roadmap

The build order is well-determined by dependency chains. Architecture research mapped these phases explicitly and they are validated by pitfall-to-phase mappings. No architectural ambiguity exists; implementation can begin immediately.

### Phase 1: Foundation -- GameEvents, HousingConfig, SaveData Schema

**Rationale:** Pure data definitions with zero behavior. All subsequent phases depend on these types existing. Getting the schema right here (especially nullable HomeSegmentIndex) prevents save-format bugs that are costly to fix later.
**Delivers:** Compiling project with new event signatures, HousingConfig resource creatable in Inspector, SavedCitizen with correct nullable field.
**Addresses:** Table-stakes feature prerequisites (assignment events, config resource)
**Avoids:** Save format int-vs-nullable bug (Pitfall 4), missing Emit helper methods gotcha
**Files:** GameEvents.cs (+2 events + 2 emit methods), HousingConfig.cs (new), SaveManager.cs (SavedCitizen field only)
**Research flag:** None -- all patterns are established and proven.

### Phase 2: HousingManager Core

**Rationale:** The assignment map, capacity tracking, and event subscriptions must exist before any consumer can query them. This phase also centralizes the size-scaled capacity formula, permanently preventing dual-ownership divergence.
**Delivers:** Functional HousingManager that assigns citizens to housing rooms on arrival, handles demolish/rebuild cycles, maintains the bidirectional lookup map, and tracks size-scaled room capacities.
**Addresses:** Auto-assign on arrival, reassign on demolish, assign unhoused on new build, even-spread algorithm, size-scaled capacity
**Avoids:** Dual capacity ownership (Pitfall 5), autoload initialization order (Pitfall 6), event ordering race (Pitfall 1)
**Files:** HousingManager.cs (new, ~180 LOC), project.godot (autoload entry)
**Research flag:** None -- follows established autoload pattern 7 times over.

### Phase 3: Capacity Transfer from HappinessManager

**Rationale:** Must happen after HousingManager exists and is functional. Removes the stale dual-ownership situation. Placing this before CitizenNode work ensures the visit animation has correct capacity semantics when tested.
**Delivers:** HappinessManager queries HousingManager for capacity; its own capacity fields and housing event subscriptions are fully removed (~-50 LOC net). Single source of truth established for housing capacity.
**Addresses:** Arrival gating correctness, architecture cleanliness, StarterCitizenCapacity removal
**Avoids:** Dual capacity tracking divergence (Pitfall 5, Anti-Pattern 1 in ARCHITECTURE.md)
**Files:** HappinessManager.cs (remove capacity fields, update arrival check), SaveManager.cs (remove housingCapacity from save/restore path)
**Research flag:** None -- well-mapped refactor with fully identified call sites.

### Phase 4: Return-Home Behavior on CitizenNode

**Rationale:** The most visible and highest-value feature. Depends on HousingManager providing home segments via events. Placed after capacity transfer so the system is coherent before adding citizen behavior on top of it.
**Delivers:** Citizens periodically walk to their home room, fade in, display Zzz, rest 8-15s, and return. Wish and visit priority guards prevent home behavior from disrupting the core loop.
**Addresses:** Return-home animation, Zzz floater, wish timer pause, home return vs wish priority
**Avoids:** Home timer vs wish priority race (Pitfall 3), demolish-rebuild cascade mid-tween (Pitfall 2), FloatingText screen-space issue (Pitfall 7)
**Files:** CitizenNode.cs (home timer, StartReturnHome(), Zzz floater, event subscriptions, ~80 LOC)
**Research flag:** Conduct a brief in-engine spike on the Zzz visual at the start of this phase. Pitfall 7 flags that FloatingText screen-space positioning may not track camera orbit during the ~1s float. If it looks wrong, switch to Label3D (localized change, no architectural impact). The PRD says FloatingText is acceptable, but validate empirically before committing.

### Phase 5: UI Updates

**Rationale:** Pure consumers of HousingManager state with minimal scope and no downstream impact. Satisfying to implement after the core system is testable. Each change is independently deployable.
**Delivers:** CitizenInfoPanel shows home room name and location, housing room tooltip shows resident names, PopulationDisplay shows count/capacity format.
**Addresses:** Home location read-back mechanism (info panel), player legibility (population display), cozy social awareness (tooltip residents -- if in scope)
**Avoids:** SegmentGrid.GetLabel() purity violation (tooltip text belongs in SegmentInteraction, not the grid helper)
**Files:** CitizenInfoPanel.cs (~15 LOC), SegmentInteraction.cs (~10 LOC), PopulationDisplay.cs (~15 LOC)
**Research flag:** None -- all patterns well-documented and scoped small.

### Phase 6: Save/Load Integration

**Rationale:** Must be last -- serializes all housing state, requiring all features to be complete. Version bump to v3 with full backward-compatibility validation against a real v2 save file before shipping.
**Delivers:** Housing assignments persist across save-quit-load cycles. v2 saves load cleanly with citizens auto-assigned from scratch. Stale assignments (demolished rooms) validated and corrected on load. New housing events (CitizenAssignedHome, CitizenUnhoused) wired to SaveManager debounce trigger.
**Addresses:** Save/load housing assignments, backward compatibility, autosave trigger coverage
**Avoids:** Stale assignment on load (Pitfall 8), v3 breaking v2 load path (Pitfall 4)
**Files:** SaveManager.cs (serialize/deserialize assignments, version bump, autosave triggers, ~30 LOC)
**Research flag:** None -- version-gated restore pattern proven in v1-to-v2 migration.

### Phase Ordering Rationale

The dependency chain is strictly linear with three parallel branches after Phase 2:

```
Phase 1 (Foundation) -- no deps, establishes shared types
    |
Phase 2 (HousingManager) -- deps: Phase 1
    |
    +-- Phase 3 (Capacity Transfer) -- deps: Phase 2
    |
    +-- Phase 4 (Return-Home) -- deps: Phase 2
    |
    +-- Phase 5 (UI) -- deps: Phase 2
    |
Phase 6 (Save/Load) -- deps: all above
```

Phases 3, 4, and 5 are mutually independent after Phase 2. The recommended sequence (3 then 4 then 5) puts the riskiest change (capacity transfer, which modifies a live production path) first to surface integration issues early, then the largest feature addition, then the simplest UI changes.

### Research Flags

**Phases needing an in-engine validation spike during implementation:**
- **Phase 4 (Return-Home):** Zzz visual implementation (FloatingText vs Label3D). Run a 5-minute spike at the start of this phase to verify FloatingText positions correctly during camera orbit. If it does not track, switch to Label3D -- the change is localized to the tween sequence.

**Phases with fully established patterns (no research needed):**
- **Phase 1 (Foundation):** Event bus and config resource patterns are proven in production.
- **Phase 2 (HousingManager):** Autoload singleton pattern exists 7 times. Dictionary data structures follow proven WishBoard pattern.
- **Phase 3 (Capacity Transfer):** Refactor with fully mapped call sites and identified removal targets.
- **Phase 5 (UI):** Three small, isolated changes each under 20 LOC.
- **Phase 6 (Save/Load):** Version-gated restore pattern already proven in v1-to-v2 migration.

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All recommendations based on direct analysis of production code. Zero external dependencies. Every pattern appears 6-9 times in existing codebase. |
| Features | MEDIUM | Genre analysis across 7 comparable games. No direct competitor in cozy-space-station niche. PRD decisions independently corroborate genre research conclusions. |
| Architecture | HIGH | Direct codebase analysis of all 40+ source files. All component boundaries, data flows, and integration points mapped with actual code examples from specific file locations. |
| Pitfalls | HIGH | All 8 pitfalls derived from direct codebase reading at specific file/line combinations. Not speculative. Includes recovery strategies and verification checklists. |

**Overall confidence:** HIGH

### Gaps to Address

- **Zzz visual implementation (minor):** FloatingText vs Label3D is an open question requiring an in-engine test. Plan for a brief exploratory spike at the start of Phase 4. If FloatingText looks wrong during orbit, switching to Label3D is a localized change with no architectural impact.

- **ARCHITECTURE.md vs STACK.md discrepancy on capacity ownership:** ARCHITECTURE.md states HousingManager becomes the single owner of ALL capacity tracking. STACK.md (written first) suggests a compromise that leaves some capacity tracking in HappinessManager. The ARCHITECTURE.md position is correct and validated by Pitfall 5. The roadmap must follow the ARCHITECTURE.md recommendation: full capacity transfer in Phase 3, nothing left in HappinessManager.

- **BuildManager API change coordination:** Adding SegmentCount to GetPlacedRoom() return tuple is a breaking change within the codebase. The two external callers (HappinessManager.OnRoomPlaced and HappinessManager.OnRoomDemolished) must be updated atomically with the API change. Plan this as a single commit touching BuildManager.cs and HappinessManager.cs together.

- **PopulationDisplay scope clarification:** FEATURES.md classifies count/capacity display as a "Should Ship" enhancement. ARCHITECTURE.md treats it as part of Phase 5. Confirm scope during planning kickoff -- either classification is reasonable, but it should be decided before Phase 5 begins.

---

## Sources

### Primary (HIGH confidence)

All findings based on direct codebase analysis:
- `Scripts/Autoloads/GameEvents.cs` -- event bus pattern, C# delegate conventions (15+ events)
- `Scripts/Citizens/CitizenNode.cs` -- Timer lifecycle, Tween chain pattern, visit animation pipeline
- `Scripts/Citizens/CitizenManager.cs` -- citizen spawning, singleton pattern, save/load API
- `Scripts/Autoloads/HappinessManager.cs` -- housing capacity tracking, arrival gating, config resource loading
- `Scripts/Autoloads/SaveManager.cs` -- version-gated save format, System.Text.Json serialization, debounced autosave
- `Scripts/Data/HappinessConfig.cs` -- [GlobalClass] Resource pattern, [Export] fields with defaults
- `Scripts/Data/EconomyConfig.cs` -- same Resource pattern, establishes config file convention
- `Scripts/Data/RoomDefinition.cs` -- BaseCapacity field, RoomCategory enum
- `Scripts/UI/FloatingText.cs` -- self-destructing floating text, Setup() API, screen-space positioning
- `Scripts/UI/CitizenInfoPanel.cs` -- programmatic UI panel, VBoxContainer label structure
- `Scripts/Ring/SegmentInteraction.cs` -- tooltip text composition pattern
- `Scripts/Build/BuildManager.cs` -- GetPlacedRoom() API, PlacedRoom record, room tracking
- `Scripts/Ring/SegmentGrid.cs` -- flat index mapping, 24-segment layout
- `docs/prd-housing.md` -- feature requirements, design decisions, timing constants, edge cases
- `.planning/PROJECT.md` -- project decisions log, architectural constraints, deferred features

### Secondary (MEDIUM confidence)

Genre research informing feature prioritization:
- Banished -- auto-assignment, demolish handling, graceful unhoused behavior
- Oxygen Not Included -- housing as capacity gate separate from wellbeing
- Stardew Valley -- home locations visible in social UI, Zzz sleeping indicator
- Spiritfarer -- cabin-per-spirit, communal home identity
- RimWorld -- bedroom assignment, info panel home display, mood-free housing
- Before We Leave -- tile-count scaling for building effectiveness (basis for size-scaled capacity)
- Dwarf Fortress -- sleep indicators, housing assignment, eviction handling

---

*Research completed: 2026-03-05*
*Ready for roadmap: yes*

---

---

# V1.1 Happiness v2 Research Summary

*Preserved from 2026-03-04. Covers the v1.1 Happiness v2 mood and tier system research.*

---

**Project:** Orbital Rings -- v1.1 Happiness v2
**Domain:** Cozy 3D space station builder (Godot 4.4 C# / existing singleton architecture)
**Researched:** 2026-03-04
**Confidence:** HIGH

---

> **Milestone scope:** This summary covers the v1.1 Happiness v2 research only.
> The v1.0 foundation research (ring geometry, citizen navigation, economy, wish system)
> remains documented in the sections below the v1.1 content.

---

## Executive Summary (v1.1)

Orbital Rings v1.1 replaces a single monotonically increasing happiness float (v1: 0.0-1.0) with a dual-value system: a permanent `lifetimeHappiness` integer counter and a fluctuating `mood` float that decays toward a rising baseline derived from that counter. This is a well-understood pattern in incremental and cozy game design: the permanent counter satisfies the "always-progressing" instinct, while the fluctuating mood gives each active session its emotional texture. The design is correctly non-punishing because the decay floor rises with accumulated play -- a player who returns after an absence finds their station at its earned resting state, not at zero.

All new technical surface area maps to APIs already present in Godot 4.4 and the existing codebase. No new packages or addons are required. The implementation is a targeted refactor of `HappinessManager`, a replacement of `HappinessBar` with two smaller HUD widgets, and a one-way save migration. Every API decision (frame-based decay via `_Process`, `AddThemeColorOverride` for tier colors, `FloatingText` reuse for tier notifications, kill-before-create `Tween` for counter pulse, version-gated `SaveData` migration) has a direct precedent already proven in the live codebase.

The primary risks are not technical complexity but correctness of migration and event wiring. The most dangerous failure mode is v1 save data being silently zeroed if the migration gate is omitted. The second most dangerous is a stale `HappinessChanged(float)` subscription surviving in `EconomyManager` after the refactor, causing the income multiplier to permanently clamp to 1.0. Both risks are mechanical to prevent: delete the old event early so the compiler surfaces every missed subscriber, and test migration with an actual v1 `save.json` before shipping.

---

## Key Findings (v1.1)

### Recommended Stack

The existing Godot 4.4 C# stack requires zero additions. All implementation uses built-in engine APIs with direct precedents in this codebase. See `.planning/research/STACK.md` for API-level detail.

**Core technologies:**
- `_Process(double delta)` in `HappinessManager` -- smooth per-frame mood decay; the only correct approach for continuous exponential smoothing (Timer nodes produce stair-step artifacts)
- `Tween` via `CreateTween()` -- all UI animations; kill-before-create pattern already established in `HappinessBar.cs`, `FloatingText.cs`, `CreditHUD.cs`
- `AddThemeColorOverride("font_color", color)` -- tier label color; absolute color override immune to theme conflicts; already used in `HappinessBar.cs` and `FloatingText.cs`
- `FloatingText : Label` (existing class) -- tier change notification; zero new code; proven in production for credit and happiness gain notifications
- `System.Text.Json` (.NET 8 built-in) -- save migration; missing fields default to zero, enabling clean v1 to v2 migration without a separate deserializer

**What NOT to add:** No third-party tween library, no Timer for mood decay, no `Label3D` for HUD notifications, no `AnimationPlayer` scene for floating text, no animated number roll-up for the lifetime counter.

### Expected Features (v1.1)

**Must have for v1.1:**
- Named mood tiers (Quiet / Cozy / Lively / Vibrant / Radiant) -- players expect human-readable state names, not raw floats
- Always-visible mood tier in HUD -- ambient state must be on-screen without menu interaction
- Feedback on tier change -- floating text auto-fading in ~2 seconds; non-blocking
- Permanent progress counter (heart N) -- monotonically increasing; never regresses
- Decay toward rising baseline (not toward zero) -- the design's key genre-correctness decision

**Should have:**
- Rising baseline tied to lifetime wish count
- Tier label color as ambient mood indicator -- five distinct colors (gray to coral to amber to gold to soft white)
- Wish counter as the always-increasing "score"

**Defer to v1.x:**
- "About to tier up" visual pulse
- Persistent wish board supplement
- Additional blueprint milestones beyond v1.1 set

**Anti-features to firmly avoid:**
- Raw mood float exposed anywhere in player-facing UI
- Mood decay toward zero (punishment loop)
- Blocking or semi-blocking tier change notifications
- Multiple simultaneous mood dimensions

### Architecture Approach (v1.1)

**Major components and their changes:**
1. `HappinessManager` -- Replace: owns `int _lifetimeHappiness` + `float _mood` + `float _baseline` + `MoodTier _currentTier`; drives per-frame decay via `_Process`
2. `GameEvents` -- Extend: add `LifetimeHappinessChanged(int)`, `MoodTierChanged(MoodTier, MoodTier)`, `MoodChanged(float)`; remove `HappinessChanged(float)` atomically
3. `EconomyManager` -- Modify: replace `_currentHappiness float` with `_currentTier MoodTier`; income multiplier becomes a tier lookup table (1.0 / 1.1 / 1.2 / 1.3 / 1.4)
4. `SaveManager / SaveData` -- Extend + migrate: bump Version 1 to 2; add `LifetimeHappiness` and `Mood` fields; static `MigrateV1ToV2` method
5. `HappinessBar` (UI) -- Replace entirely: create `HappinessCounter.cs` and `MoodDisplay.cs`
6. `CitizenManager`, `WishBoard`, `BuildManager` -- No change

### Critical Pitfalls (v1.1)

1. **Save migration data loss** -- v1 saves silently zeroing all lifetime happiness if no version gate exists. Prevention: add `if (data.Version < 2) data = MigrateV1ToV2(data)` immediately after deserialization; test with an actual v1 `save.json`.
2. **HappinessChanged event contract break** -- stale subscribers on the old `Action<float>` event produce maximum income multiplier forever. Prevention: delete `HappinessChanged` early; migrate all consumers in the same phase.
3. **Autosave storm from continuous mood decay** -- `MoodChanged` emitted every frame causes SaveManager debounce to reset every frame. Prevention: emit `MoodChanged` only on tier change or with a 1-second minimum interval.
4. **Tier boundary oscillation (chatter)** -- rapid tier toggling when mood rests near a threshold. Prevention: implement hysteresis (demotion threshold = boundary - 0.5) and a minimum tier hold time of 5 seconds.
5. **Decay feels punishing at low lifetime happiness** -- tier-aware decay rates (lower tiers decay slower); enforce "resting tier floor."

---

## Implications for Roadmap (v1.1)

### Phase 1: HappinessManager Refactor + Event Bus Migration
**Rationale:** Dependency root. All other phases consume HappinessManager's new API and GameEvents's new events. MoodTier enum must exist before new GameEvents signatures can compile.
**Delivers:** New HappinessManager with dual-value state, MoodTier enum, three new GameEvents events, _Process decay loop with hysteresis and tier-aware decay rates.

### Phase 2: Save Migration (v1 to v2)
**Rationale:** Highest-severity risk; must be verified against a real v1 save.json before any v2 save is written.
**Delivers:** SaveData.Version bumped to 2; migration path; CollectGameState and ApplyState updated.

### Phase 3: EconomyManager Consumer Update
**Rationale:** Smallest consumer change; validates the new MoodTier API against a real downstream system before the HUD is built.
**Delivers:** Tier lookup table for income multiplier (1.0 / 1.1 / 1.2 / 1.3 / 1.4 per tier).

### Phase 4: HUD Replacement
**Rationale:** Purely a consumer with no downstream impact. HappinessBar.cs is deleted entirely, not patched.
**Delivers:** HappinessCounter.cs + MoodDisplay.cs with tier colors and floating text notifications.

### Phase 5: Integration Smoke Test + Tuning
**Rationale:** End-to-end verification before any v1.x additions. Tuning requires playtesting, not further implementation.
**Delivers:** Verified smoke test suite; decay rates and tier thresholds moved to HappinessConfig resource for Inspector tuning.

---

## Confidence Assessment (v1.1)

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All APIs verified against Godot 4.4 official docs and live codebase usage; no new packages required |
| Features | MEDIUM | Cozy game genre research from developer/community sources; no direct competitor in the cozy-space-station-wish-loop niche |
| Architecture | HIGH | Based on direct reading of every affected source file; build order confirmed against actual dependency graph |
| Pitfalls | HIGH | Six specific pitfalls identified with warning signs, recovery strategies, and phase assignments |

**Overall confidence:** HIGH

---

---

# V1.0 Foundation Research Summary

*Preserved from 2026-03-02. Covers the original Orbital Rings v1.0 implementation research.*

---

**Project:** Orbital Rings
**Domain:** Cozy 3D space station builder / management game with citizen wish loop
**Researched:** 2026-03-02
**Confidence:** MEDIUM-HIGH

## Executive Summary (v1.0)

Orbital Rings is a cozy 3D builder in a largely unoccupied market position: no competitor combines named individual citizens with personal wishes, a genuinely no-punishment loop, and a soft-3D space setting. The engine foundation is already locked -- Godot 4.6 C# with Forward Plus rendering and Jolt Physics -- and is the right choice for this scope. The architecture is well-understood: a layered Node + service pattern with a small set of Autoload singletons, custom Resource data definitions, and an EventBus for cross-system communication. The ring geometry is the only genuinely novel engineering problem, and it has a clear solution: a custom polar-coordinate SegmentGrid (not Godot's GridMap), mathematical segment selection (not trimesh collision), and a hand-authored walkway navmesh (not auto-baked).

The core feature loop is build, wish, grow -- driven by a single credit currency and a happiness score that gates both citizen arrivals and blueprint unlocks. This is a simpler and more coherent economy than most management games in this space -- intentionally so, to preserve the cozy genre promise of no punishment and no stress. The risk is not complexity but permissiveness: without a fail state or resource scarcity, the positive feedback loop (citizens, credits, rooms, more citizens) can go runaway within 15-20 minutes of play. Economy balance must be treated as a first-class design concern, modeled in a spreadsheet before any credit numbers are baked into code.

The most critical pitfalls are all addressable at project setup time: C# signal lifecycle hygiene, data/logic separation (keeping parameters in Resource files, not C# constants), mathematical segment selection, and hand-authored ring navmesh. These decisions must be made in the first phase; retrofitting them is expensive. The citizen simulation at ~20 citizens is well within Godot's comfortable performance range, so ECS and other architectural complexity are unnecessary -- the project's biggest risk is early architectural shortcuts, not scale.

---

*Research completed: 2026-03-07 (v1.3 update) | 2026-03-05 (v1.2 update) | 2026-03-04 (v1.1 update) | 2026-03-02 (v1.0 foundation)*
*Ready for roadmap: yes*
