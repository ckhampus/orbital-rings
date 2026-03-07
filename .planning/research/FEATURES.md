# Feature Landscape

**Domain:** Testing infrastructure for a Godot 4 C# game (cozy space station builder)
**Researched:** 2026-03-07
**Confidence:** HIGH -- GoDotTest and GodotTestDriver documentation verified via NuGet and official GitHub READMEs. Architecture analysis based on direct codebase inspection of all 8 autoload singletons, config resources, and POCO classes.

---

## Context: What We Are Testing

Orbital Rings is a 9,500+ LOC Godot 4.4 / C# / .NET 8 game with:
- 8 Autoload singletons (GameEvents, EconomyManager, BuildManager, CitizenManager, WishBoard, HappinessManager, SaveManager, HousingManager)
- Pure C# event delegates (not Godot signals) for all cross-system communication
- 1 POCO class (MoodSystem) explicitly designed for testability in isolation
- 3 Godot Resource config classes (HappinessConfig, EconomyConfig, HousingConfig) with `new()` constructors
- Save/load across 3 format versions (v1, v2, v3) using System.Text.Json
- Pure calculation methods on singletons (CalculateTickIncome, CalculateRoomCost, ComputeCapacity)

The testing milestone does NOT introduce new gameplay. It adds the framework and critical path coverage to catch regressions as the codebase grows.

---

## Table Stakes

Features that any testing infrastructure for this codebase must have. Missing any of these means the testing milestone fails to provide confidence.

| Feature | Why Expected | Complexity | Dependencies | Notes |
|---------|--------------|------------|--------------|-------|
| Test runner scene | GoDotTest requires a dedicated scene to discover and run test classes via reflection | Low | GoDotTest NuGet package | Single .tscn with a C# script calling `GoTest.RunTests()` |
| Command-line test execution | CI/CD and headless runs need `--run-tests --quit-on-finish` | Low | Test runner scene, Godot CLI | GoDotTest provides this out of the box via command-line args |
| Unit tests for pure calculations | EconomyManager.CalculateRoomCost, CalculateTickIncome, HousingManager.ComputeCapacity are pure functions -- testing them is free | Low | Config resources with `new()` constructors | These methods accept config + inputs, return outputs. No Godot tree needed. |
| MoodSystem POCO unit tests | MoodSystem was explicitly designed as a POCO for isolated testing (documented in code comments). Decay, tier transitions, hysteresis, wish gain, and restore all testable without any Godot node | Low | HappinessConfig with `new()` | Most valuable unit test target. Covers exponential decay math, 5-tier state machine, hysteresis dead-band, and boundary conditions |
| Save/load round-trip tests | 3 format versions with backward compatibility is the highest-risk code path. Serialize SaveData to JSON, deserialize, verify fields survive | Low-Med | System.Text.Json, SaveData/SavedRoom/SavedCitizen POCOs | POCOs are plain C# -- no Godot types. Can test JSON round-trip without engine |
| Save format version migration tests | v1 saves missing LifetimeHappiness/Mood/MoodBaseline must deserialize with defaults. v2 saves missing HomeSegmentIndex must deserialize with null | Med | SaveData POCO, version-gated logic in ApplyState | Test that v1 JSON missing v2 fields deserializes correctly, v2 JSON missing v3 fields deserializes correctly |
| Test isolation (no cross-test state leakage) | GoDotTest runs tests sequentially in-process. Static Instance singletons leak state between test suites if not cleaned up | Med | [Setup]/[Cleanup] lifecycle hooks | Must reset or mock singleton state between tests. Critical for correctness. |
| Assertion library | GoDotTest provides no built-in assertions. Tests need a way to assert expected vs actual | Low | Shouldly NuGet package | Shouldly gives readable `.ShouldBe()` syntax. Lightweight, well-maintained, no Godot conflicts |
| Exclude test code from release builds | Test files must not ship in the exported game | Low | .csproj `DefaultItemExcludes` with `ExportRelease` condition | Standard GoDotTest pattern: conditional exclude of test/ directory |
| Housing assignment logic tests | Fewest-occupants-first with capacity scaling is algorithmic -- verify assignment, displacement on demolish, reassignment, stale reference handling | Med | HousingManager internal logic, BuildManager room data | Needs either integration with scene or extraction of assignment logic into testable methods |
| Economy formula verification | Income formula (base + sqrt scaling + work bonus) * tier multiplier has specific calibrated values. Tests lock the spreadsheet math | Low | EconomyConfig `new()`, pure CalculateTickIncome | Parameterized tests across all 5 mood tiers confirm multiplier values match config |

---

## Differentiators

Features that go beyond minimum viability. Not required for the milestone to succeed, but significantly increase testing value. Build these if time permits.

| Feature | Value Proposition | Complexity | Dependencies | Notes |
|---------|-------------------|------------|--------------|-------|
| Integration tests with GodotTestDriver | Scene-based tests that load actual game scenes, simulate input, and verify system interactions (wish fulfillment loop, room placement triggering housing assignment) | High | GodotTestDriver NuGet, Fixture API, test scene setup | Highest-value differentiator. Validates the event-driven singleton coordination that unit tests cannot reach |
| Code coverage collection | Coverlet integration to measure which code paths are exercised by tests. Identifies untested risk areas | Med | coverlet CLI tool, `--coverage` flag, coverage XML output | GoDotTest documents this workflow. Useful for identifying blind spots but not blocking |
| CI pipeline integration | GitHub Actions or similar running tests on every push/PR | Med | Godot headless mode, test runner, coverage | Requires Godot installed in CI. Chickensoft provides reference GH Actions workflows |
| Custom test drivers for game-specific nodes | GodotTestDriver custom drivers for CitizenNode, RingSegment, BuildPanel that abstract away implementation details | High | GodotTestDriver driver pattern, game scene structure | Makes integration tests robust to UI refactors. Investment pays off over time |
| Event bus verification tests | Tests that verify GameEvents.Instance emits the correct events in the correct order when system operations occur | Med | GameEvents singleton, event subscription in tests | Subscribe to events in [Setup], assert event was fired with correct args. Validates the wiring |
| Snapshot testing for save data | Golden-file comparisons: serialize a known game state, compare against a stored JSON snapshot. Detect unintended save format drift | Med | Known-state SaveData, JSON file comparison | Catches accidental field additions/removals/renames before they corrupt user saves |
| Parameterized tests for tier boundaries | Test all 5 tier thresholds, hysteresis boundaries, and edge cases using parameterized/data-driven patterns | Low-Med | MoodSystem POCO, HappinessConfig | GoDotTest does not have built-in parameterized test support. Use loops or manual repetition |
| Wish fulfillment end-to-end test | Load game scene, place a room that fulfills a citizen's wish, verify happiness increments and mood updates | High | Full scene load, GodotTestDriver, multiple singleton coordination | The core game loop. Most complex test but validates the most critical path |
| Debug launch configurations | VSCode launch.json entries for "Debug Tests", "Debug Current Test", "Play Game" | Low | .vscode/launch.json, GODOT env variable | GoDotTest documents exact configs. Developer QoL feature |
| Stale save reference handling tests | Verify that loading a save where HomeSegmentIndex points to a no-longer-existing room gracefully marks citizen as unhoused | Med | SaveData with stale references, HousingManager.RestoreFromSave | Documented in Phase 19 audit. Three code paths in RestoreFromSave need coverage |

---

## Anti-Features

Features to explicitly NOT build for this milestone. These add complexity without proportional testing value for a game of this scope.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| Parallel test execution | GoDotTest explicitly does not support parallel tests and has no plans to. Parallel tests cause race conditions with Godot's scene tree and singleton state | Run tests sequentially. Execution time is not a concern at 9.5K LOC |
| Full mocking framework (Moq-style) | The codebase uses static singleton instances (GameEvents.Instance, etc.) which are not interface-abstracted. Retrofitting interfaces for mocking would be a major refactor that changes production code | Test pure functions directly. For integration tests, use real singletons within GodotTestDriver fixtures that properly set up and tear down the scene tree |
| Performance/benchmark testing | The game runs at 60fps with procedural mesh generation. There are no performance concerns to benchmark | Focus on correctness testing. Performance testing is premature |
| Visual regression testing (screenshot comparison) | The game uses procedural geometry with no static sprites. Screenshot comparison is brittle and low-value | Test behavior and state, not visuals |
| Mutation testing | Adds significant tooling complexity for a solo-developer game project | Use code coverage to identify gaps instead |
| Test generation from code | Auto-generated tests from reflection or code analysis produce low-quality tests that verify implementation, not behavior | Write intentional tests for critical paths |
| xUnit/NUnit test adapter | These require running tests outside of Godot's process, which means no access to the scene tree, Resource loading, or Godot APIs. GoDotTest runs inside Godot which is essential for integration tests | Use GoDotTest as the sole test runner. It runs inside Godot where all APIs are available |
| Separate test project (.csproj) | Splitting tests into a separate assembly breaks access to `internal` members and complicates Godot's build pipeline | Keep tests in the same project under a `test/` directory, excluded from release builds via .csproj condition |
| Testing procedural mesh/audio generation | Procedural geometry is a rendering concern, not a logic concern. There is no meaningful assertion to make about generated vertices | Test the math inputs to procedural generation (segment angles, room placement indices) not the mesh output |

---

## Feature Dependencies

```
Test Runner Scene
  |
  +-> Unit Test Suites (no scene tree needed)
  |     |
  |     +-> MoodSystem POCO tests
  |     +-> Economy calculation tests
  |     +-> Housing capacity computation tests
  |     +-> Save/load JSON round-trip tests
  |     +-> Save format migration tests (v1->v2->v3)
  |
  +-> Integration Test Suites (scene tree needed)
        |
        +-> GodotTestDriver Fixture setup
        |     |
        |     +-> Singleton lifecycle tests (GameEvents wiring)
        |     +-> Housing assignment flow tests
        |     +-> Mood tier change -> economy multiplier tests
        |     +-> Wish fulfillment loop tests
        |
        +-> Custom test drivers (optional, for robustness)
```

Key dependency chains:
- All test suites depend on the test runner scene existing
- Unit tests for POCOs and pure functions have ZERO dependencies on the scene tree
- Integration tests require GodotTestDriver's Fixture to manage node lifecycle
- Housing assignment integration tests depend on BuildManager and CitizenManager state being set up in the fixture
- Save/load round-trip tests can be pure unit tests (SaveData is a plain C# class serialized with System.Text.Json)

---

## Testability Assessment of Existing Code

### Highly Testable (unit tests, no Godot tree)

| Component | Why | Test Strategy |
|-----------|-----|---------------|
| MoodSystem | POCO class, no Node inheritance, constructor-injected config | Instantiate with `new HappinessConfig()`, call methods, assert results |
| EconomyManager.CalculateRoomCost | Pure function, takes config + inputs, returns int | Call directly on instance with known config values |
| EconomyManager.CalculateTickIncome | Pure function, reads only internal state set via SetCitizenCount/SetMoodTier | Set state, call method, assert result |
| EconomyManager.CalculateDemolishRefund | Pure function, one multiplication | Trivial test |
| HousingManager.ComputeCapacity | Static pure function | `ComputeCapacity(def, segmentCount).ShouldBe(expected)` |
| SaveData / SavedRoom / SavedCitizen | Plain C# POCOs with no Godot types | JSON serialize/deserialize round-trip |
| HappinessConfig / EconomyConfig / HousingConfig | Godot Resources but have parameterless constructors with coded defaults | `new HappinessConfig()` gives predictable test values |

### Moderately Testable (needs scene tree or careful setup)

| Component | Challenge | Test Strategy |
|-----------|-----------|---------------|
| EconomyManager (full lifecycle) | Node subclass, creates Timer child, subscribes to GameEvents | Use GodotTestDriver Fixture to add to tree. Set state via public API, verify income ticks |
| HousingManager (assignment flow) | Reads BuildManager.Instance and CitizenManager.Instance | Integration test with fixture that sets up all three singletons |
| SaveManager (ApplyState/ApplySceneState) | Orchestrates all singletons, frame-delay pattern | Integration test that sets up full scene, calls ApplyState, waits frames, verifies state |
| WishBoard (wish tracking) | Loads .tres resources from filesystem, subscribes to events | Integration test with fixture, or mock template loading |
| HappinessManager (progression) | Owns MoodSystem, subscribes to events, spawns Timer | Test MoodSystem in isolation for math. Integration test for event wiring |

### Hard to Test (not worth testing directly)

| Component | Why | Alternative |
|-----------|-----|-------------|
| CitizenNode (visual behavior) | Walking, Zzz animations, mesh rendering | Test the data flow (assignment, wish state) not the visuals |
| OrbitalCamera | Input-driven 3D camera | Skip -- visual/input concern |
| BuildPanel / HUD | UI rendering | Test underlying state, not UI display |
| Procedural mesh/audio | Rendering pipeline | Not testable, not worth testing |

---

## MVP Recommendation

Prioritize these features for the testing milestone, in this order:

### Phase 1: Framework Setup (must ship first)
1. **Test runner scene** -- unblocks all other test work
2. **Assertion library (Shouldly)** -- unblocks writing assertions
3. **Debug launch configurations** -- developer workflow for running/debugging tests
4. **Release build exclusion** -- tests must not ship in exports

### Phase 2: Unit Test Suites (highest ROI, lowest complexity)
1. **MoodSystem POCO tests** -- decay, tier transitions, hysteresis, wish gain, restore. Most valuable single test suite
2. **Economy calculation tests** -- room cost formula, income formula, demolish refund, tier multipliers
3. **Housing capacity computation tests** -- ComputeCapacity for all segment sizes
4. **Save/load JSON round-trip tests** -- SaveData survives serialize/deserialize across all 3 format versions

### Phase 3: Integration Test Suites (highest value, highest complexity)
1. **Housing assignment flow** -- fewest-occupants-first, demolish displacement, reassignment
2. **Mood tier -> economy multiplier** -- verify SetMoodTier propagation through event chain
3. **Save format version migration** -- v1/v2 saves load correctly with default values for missing fields

### Defer
- **Wish fulfillment end-to-end** -- requires full scene setup with citizen spawning, wish template loading, and room placement. Defer to after the foundation is solid
- **Code coverage collection** -- useful but not blocking. Add after tests exist
- **CI pipeline** -- valuable but can be added independently after tests pass locally
- **Custom test drivers** -- only needed if integration tests become brittle due to implementation coupling

---

## Sources

- [Chickensoft GoDotTest GitHub](https://github.com/chickensoft-games/GoDotTest) -- test runner, lifecycle hooks, CLI args, coverage workflow
- [Chickensoft GodotTestDriver GitHub](https://github.com/chickensoft-games/GodotTestDriver) -- fixture API, input simulation, custom drivers, event waiting
- [GoDotTest NuGet v2.0.30](https://www.nuget.org/packages/Chickensoft.GoDotTest/) -- latest version, .NET 8 compatible, published Feb 2026
- [GodotTestDriver NuGet v3.1.62](https://www.nuget.org/packages/Chickensoft.GodotTestDriver/) -- latest version, targets Godot 4.6.1+, published Feb 2026
- [Chickensoft GodotGame template](https://github.com/chickensoft-games/GodotGame) -- reference project with testing, coverage, CI/CD
- Direct codebase inspection of all 8 autoload singletons, 7 data classes, and MoodSystem POCO
