# Pitfalls Research

**Domain:** Adding GoDotTest + GodotTestDriver testing infrastructure to an existing Godot 4 C# game with 8 autoload singletons, event-driven architecture, and System.Text.Json save/load
**Researched:** 2026-03-07
**Confidence:** HIGH (derived from direct codebase analysis of 20+ source files, GoDotTest/GodotTestDriver documentation, and Godot C# testing community patterns)

## Critical Pitfalls

### Pitfall 1: Autoload Singleton State Leaking Between Test Suites

**What goes wrong:**
All 8 autoloads (GameEvents, EconomyManager, BuildManager, CitizenManager, WishBoard, HappinessManager, HousingManager, SaveManager) use the `static Instance` singleton pattern with state initialized in `_Ready()` or `_EnterTree()`. GoDotTest runs all test suites sequentially in a single Godot process. The autoloads are initialized ONCE when the test runner scene loads and persist for the entire test run. Test A modifies `EconomyManager.Instance.Credits` via `TrySpend()`, Test B expects the starting balance -- it gets Test A's leftover balance. Every test that touches a singleton is implicitly coupled to every previous test.

This is the single most dangerous pitfall because it produces test failures that are ordering-dependent. Tests pass individually, fail when run together. Tests pass locally, fail in CI with a different suite ordering.

**Why it happens:**
The C# singleton pattern with static `Instance` properties means the object lives for the entire process lifetime. GoDotTest does not restart the Godot process between test suites -- it runs everything in one `_Ready()` call via `GoTest.RunTests()`. Unlike xUnit/NUnit which create fresh class instances per test, GoDotTest's `TestClass` instances are new but the global singletons they interact with are not.

The codebase's singletons store mutable state in private fields:
- `EconomyManager._credits`, `_citizenCount`, `_workingCitizens`, `_currentTierMultiplier`
- `HappinessManager._lifetimeHappiness`, `_crossedMilestoneCount`, `_unlockedRooms`
- `HousingManager._housingRoomCapacities`, `_roomOccupants`, `_citizenHomes`
- `WishBoard._activeWishes`, `_placedRoomTypes`, `_segmentRoomIds`
- `BuildManager._placedRooms`, `_mode`
- `CitizenManager._citizens`
- `SaveManager._pendingLoadFrames`, `PendingLoad`

None of these have reset/clear methods designed for testing (some have partial ones like `ClearCitizens()` and `ClearAllRooms()` used by save/load, but they emit events with side effects).

**How to avoid:**
Create a `TestHelper` utility class with a `ResetAllSingletons()` method that restores every autoload to its initial state. Call this in `[Setup]` or `[Cleanup]` of every test suite that touches game state. The reset method must:

1. Reset EconomyManager: restore credits to starting value, clear working citizens set, reset tier multiplier to 1.0.
2. Reset HappinessManager: zero lifetime wishes, reset mood system, restore default unlocked rooms, zero milestone count.
3. Reset HousingManager: clear all room capacities, occupants, citizen homes. Reset `StateLoaded` to false.
4. Reset BuildManager: call `ClearAllRooms()` to remove all placed rooms, reset mode to Normal.
5. Reset CitizenManager: call `ClearCitizens()` to free all citizen nodes, reset `StateLoaded` to false.
6. Reset WishBoard: clear active wishes, placed room types, segment room IDs.
7. Reset SaveManager: null out `PendingLoad`, stop debounce timer.
8. Reset GameEvents: this one is subtle -- see Pitfall 2.

Some singletons lack public reset APIs. You will need to add `internal` or `public` reset methods specifically for testing. Do NOT use reflection to set private fields -- it couples tests to implementation details and breaks on refactor. Instead, add explicit `ResetForTesting()` methods on each singleton, guarded by `#if DEBUG` or a `[Conditional]` attribute.

**Warning signs:**
- Tests pass when run individually via `--run-tests=MySuite.MyTest` but fail when run as a full suite.
- Flaky tests that pass/fail depending on which test ran before them.
- Tests that work on first run but fail on second run without restarting Godot.
- Economy tests showing unexpected credit balances.

**Phase to address:**
Phase 1 (Framework Setup) -- the `ResetForTesting()` infrastructure must exist before ANY test is written. This is foundational. Every subsequent phase depends on reliable test isolation.

---

### Pitfall 2: C# Event Delegate Subscriber Accumulation Across Tests

**What goes wrong:**
GameEvents uses pure C# `event Action<...>` delegates (not Godot signals). When a test creates a node (e.g., a mock listener or a real CitizenNode) and subscribes it to `GameEvents.Instance.WishFulfilled += handler`, that subscription persists on the GameEvents instance even after the test's `[Cleanup]` frees the node. C# delegates hold strong references to subscriber objects. If the subscriber node is freed via `QueueFree()`, the delegate still holds a reference to the now-disposed C# wrapper. The next time the event fires, it invokes the handler on a disposed object, causing `ObjectDisposedException: Cannot access a disposed object`.

This is especially dangerous because the codebase intentionally uses C# events instead of Godot signals (GameEvents.cs:17-20 documents this decision), meaning Godot's automatic signal disconnection on node free does NOT apply.

**Why it happens:**
The existing codebase handles this correctly in production through the `SafeNode` pattern (SafeNode.cs) which pairs `_EnterTree` subscription with `_ExitTree` unsubscription. But test code creates nodes dynamically and may not follow this pattern. A test might:
1. Create a node and manually subscribe to GameEvents.
2. Run assertions.
3. Call `node.QueueFree()` in cleanup.
4. `QueueFree` is deferred -- the node isn't actually freed until the next frame.
5. Between cleanup and the next test, an event fires and hits the pending-free node.

Even worse: if the test doesn't clean up at all (common first mistake), subscribers accumulate. By test 50, an event fires and invokes 49 stale handlers.

**How to avoid:**
1. Never manually subscribe to GameEvents in tests. If you need to observe events, create a purpose-built `EventSpy` helper class that subscribes in its constructor and unsubscribes in an explicit `Dispose()` method.
2. The `ResetAllSingletons()` method from Pitfall 1 should also clear all event delegate subscriber lists on GameEvents. This requires adding a `ClearAllSubscribers()` method to GameEvents that sets every event field to null: `WishFulfilled = null; RoomPlaced = null;` etc. This is safe because `ResetAllSingletons()` also reinitializes all the singletons that were subscribed.
3. After calling `ClearAllSubscribers()`, re-subscribe all singletons that need events (SaveManager, HousingManager, etc.) by calling their subscription methods. Alternatively, design the reset so each singleton re-subscribes itself during its own reset.
4. For nodes created in tests, always use `GodotTestDriver.Fixture` to manage lifecycle -- it handles cleanup automatically.

**Warning signs:**
- `ObjectDisposedException` during test runs, especially in later test suites.
- Event handler invocation count increasing with each test (e.g., `WishFulfilled` fires once but 5 handlers run).
- Autosave triggers during tests (SaveManager's debounce timer responding to stale event subscriptions).
- Tests that "work" but leave GD.PrintErr warnings about accessing freed objects.

**Phase to address:**
Phase 1 (Framework Setup) -- the `EventSpy` pattern and `ClearAllSubscribers()` must be designed alongside `ResetForTesting()`. These are inseparable concerns.

---

### Pitfall 3: Scene Tree Lifecycle Mismatch in Test Environment

**What goes wrong:**
GoDotTest provides the test runner scene as a `Node` to each `TestClass` constructor. Tests can add child nodes to this scene to get them into the tree. But the test scene is NOT the game scene. It does not contain:
- A `RingVisual` node (CitizenManager._Ready() searches for `Ring` via `GetTree().Root.FindChild("Ring")`)
- A `Camera3D` (CitizenManager caches `GetViewport().GetCamera3D()`)
- Any `CanvasLayer` for UI (HappinessManager creates one, CitizenManager creates one)
- The segment grid, room visuals, or walkway mesh

Singletons that search the scene tree in `_Ready()` will get null references. Some fail silently (null-conditional `?.` throughout the codebase), others crash. The autoloads were designed for the game scene, not a blank test scene.

**Why it happens:**
The autoloads initialize before the game scene loads, but they expect the game scene to exist by the time `_Process` runs. In tests, the "game scene" is the test runner scene, which has none of the expected structure. The codebase uses lazy discovery in some places (CitizenManager._Process() retries finding RingVisual every frame), but this pattern still requires the actual node to eventually exist.

**How to avoid:**
Categorize tests into three tiers with different approaches:

**Tier 1 -- Pure Logic Tests (no scene tree needed):**
Test POCOs and pure calculation methods directly without any Godot nodes:
- `MoodSystem` (already a POCO, takes `HappinessConfig` in constructor)
- `EconomyManager.CalculateRoomCost()`, `CalculateTickIncome()`, `CalculateDemolishRefund()`
- `HousingManager.ComputeCapacity()`
- `SaveData` serialization/deserialization round-trips
- Tier promotion/demotion logic

These tests should NOT touch singletons at all. Instantiate the POCO directly with test data.

**Tier 2 -- Singleton Logic Tests (singletons needed, scene tree minimal):**
For testing singleton behavior (e.g., "EconomyManager.TrySpend deducts credits"), ensure singletons are initialized by the autoload system but don't rely on scene tree nodes. Use `ResetForTesting()` before each test.

**Tier 3 -- Integration Tests (scene tree needed):**
For testing cross-system flows (e.g., "place room -> citizen visits -> wish fulfilled"), use GodotTestDriver fixtures to load minimal test scenes. Create stripped-down `.tscn` files that contain only the nodes needed for the specific test, not the full game scene.

**Warning signs:**
- NullReferenceException on `GetTree().Root.FindChild(...)` calls.
- Tests hang waiting for a scene node that will never appear.
- `_Process` methods running in tests and performing unexpected scene tree searches each frame.
- Autoload Timers firing during tests (income timer, arrival timer, debounce timer).

**Phase to address:**
Phase 1 (Framework Setup) -- the test tier structure must be established upfront to prevent confusion about which approach to use for each test type.

---

### Pitfall 4: Autoload Timers Firing During Tests

**What goes wrong:**
Three autoloads create child `Timer` nodes that tick continuously:
- `EconomyManager._incomeTimer` (5.5s interval, triggers `OnIncomeTick` which modifies credits)
- `HappinessManager._arrivalTimer` (60s interval, triggers citizen spawn attempts)
- `SaveManager._debounceTimer` (0.5s one-shot, triggers `PerformSave` which writes to disk)

GoDotTest runs tests in the Godot main loop, meaning `_Process` runs between test methods and Timers continue ticking. A test that sets credits to 100 and then waits a few seconds (e.g., for an async operation or frame delays) may find credits at 104 because the income timer fired. Worse, `SaveManager._debounceTimer` will write save files to disk during tests, corrupting any real save data.

**Why it happens:**
Timers are scene tree nodes that tick automatically when the tree is processing. The test runner scene processes normally. Autoloads are part of the scene tree root, so their child Timers process alongside test nodes.

**How to avoid:**
1. In `ResetForTesting()`, stop all autoload timers: `_incomeTimer.Stop()`, `_arrivalTimer.Stop()`, `_debounceTimer.Stop()`.
2. For integration tests that need timers to fire, provide explicit `AdvanceTimer()` helper methods that manually trigger the timer callback without waiting for real time to pass.
3. Consider setting `ProcessMode = ProcessModeEnum.Disabled` on autoloads during tests, then re-enabling only the ones under test. But this breaks `_Process`-based logic (HappinessManager mood decay), so use it selectively.
4. For SaveManager specifically, either:
   a. Override the save path to a temp directory during tests, OR
   b. Stop the debounce timer and only call `PerformSave()` explicitly when testing save functionality.

**Warning signs:**
- Credits changing between test setup and assertion.
- Random citizens spawning during test runs.
- `user://save.json` being overwritten during tests, destroying real save data.
- Tests that pass quickly but fail when CI is slow (more time for timers to fire).
- Mood values drifting during test execution due to `_Process` decay.

**Phase to address:**
Phase 1 (Framework Setup) -- timer management must be part of `ResetForTesting()`. This is critical for test determinism.

---

### Pitfall 5: Save/Load Tests Writing to Real User Directory

**What goes wrong:**
`SaveManager` writes to `user://save.json` via Godot's `FileAccess.Open()`. The `user://` prefix resolves to the OS-specific user data directory (e.g., `~/.local/share/godot/app_userdata/Orbital Rings/` on Linux). When tests exercise save/load paths, they read and write the player's real save file. A test could:
1. Overwrite the player's save with test data.
2. Read the player's save and get unexpected values that cause test failures.
3. Delete the save file via `ClearSave()`.
4. In CI, the `user://` path may not exist or may lack write permissions.

**Why it happens:**
`SavePath` is a `const string` (`"user://save.json"`) in SaveManager with no configuration point. There's no dependency injection or path override mechanism. The save system was designed for production use, not testability.

**How to avoid:**
Two approaches (use both):

**Approach A -- Test-specific save path:**
Add a `public static string SavePathOverride` property to SaveManager. In `ResetForTesting()`, set it to a temp path like `"user://test_save.json"`. Modify all `FileAccess.Open(SavePath, ...)` calls to use `SavePathOverride ?? SavePath`. In test cleanup, delete the test save file.

**Approach B -- Bypass FileAccess entirely for unit tests:**
For save/load round-trip tests, don't test through SaveManager at all. Test the serialization layer directly:
```csharp
var saveData = new SaveData { Credits = 500, Version = 3, ... };
string json = JsonSerializer.Serialize(saveData);
var loaded = JsonSerializer.Deserialize<SaveData>(json);
Assert.Equal(500, loaded.Credits);
```
This tests the data contract without touching the filesystem. Reserve full SaveManager tests for integration tests that use an isolated temp path.

**Warning signs:**
- Real save file disappears after running tests.
- Tests fail in CI with `FileAccess.Open returned null` errors.
- Test data showing up in the real game after running tests.
- Tests passing locally (save file exists) but failing in CI (no save file).

**Phase to address:**
Phase 1 (Framework Setup) -- the save path isolation must be configured before any save/load tests are written. Phase 2 or 3 (save/load test suite) uses this infrastructure.

---

### Pitfall 6: SaveData Serialization Tests Missing Nullable/Default Edge Cases

**What goes wrong:**
System.Text.Json has specific behaviors with nullable types and default values that bite in round-trip testing:

1. `SavedCitizen.HomeSegmentIndex` is `int?` (nullable). `JsonSerializer` serializes `null` as the JSON literal `null`, which deserializes back correctly. But if a test constructs a `SavedCitizen` without setting `HomeSegmentIndex`, it defaults to `null` implicitly -- the test appears to work but didn't actually test the null path explicitly.

2. `SaveData.PlacedRoomTypes` is `Dictionary<string, int>` but unlike the `List<T>` fields, it is NOT initialized with `= new()` in the class definition. It IS initialized (`= new()`) in the current code, but a future developer might add a new dictionary field without the initializer. Old saves that lack the field would deserialize it as `null`, not empty.

3. The `Version` field defaults to `3` in the class definition (`public int Version { get; set; } = 3`). If a test serializes a v1 save scenario but forgets to explicitly set `Version = 1`, it will silently use version 3, and the version-gated restore paths won't be exercised.

**Why it happens:**
System.Text.Json's default value handling is different from Newtonsoft.Json. Missing JSON properties get C# default values: 0 for int, null for nullable/reference types, false for bool. The codebase compensates with field initializers (`= new()`, `= 3`), but tests must verify these edge cases, not rely on them.

**How to avoid:**
1. Create explicit test fixtures for each save format version:
   - `v1_save.json` -- real v1 format with `Happiness` field, no `LifetimeHappiness`/`Mood`/`MoodBaseline`, no `HomeSegmentIndex`.
   - `v2_save.json` -- v2 format with mood fields, no `HomeSegmentIndex`.
   - `v3_save.json` -- current format with all fields.
2. Include a test that deserializes raw JSON strings (not round-tripped C# objects) to verify that missing fields produce correct defaults.
3. Test the boundary explicitly: serialize with `Version = 1`, deserialize, verify `LifetimeHappiness == 0` and `Mood == 0` and `HomeSegmentIndex == null`.
4. Add a "canary" test that verifies all `SaveData` properties have field initializers where expected -- this catches future fields added without initializers.

**Warning signs:**
- Save/load tests pass but actual old saves crash on load.
- A NullReferenceException in `ApplySceneState` when loading a v2 save (missing `HomeSegmentIndex`).
- Tests that construct `SaveData` with all fields set, which never exercises the default/missing path.

**Phase to address:**
Phase 2 or 3 (save/load test suite) -- the test fixtures for each version must be created as test data files, not generated programmatically.

---

### Pitfall 7: Testing Event-Driven Chains Without Frame Advancement

**What goes wrong:**
The game's architecture uses event cascades: `RoomPlaced` -> `WishBoard.OnRoomPlaced` -> `NudgeCitizensForRoom` -> `WishNudgeRequested`. Some of these chains are synchronous (C# events invoke immediately), but the side effects may depend on frame processing. For example:
- `SaveManager._Process` checks `_pendingLoadFrames` and waits 2 frames before calling `ApplySceneState`.
- `CitizenManager._Process` retries finding `RingVisual` each frame.
- `HappinessManager._Process` runs mood decay via `MoodSystem.Update()`.
- Tweens advance in `_Process`.

A test that calls `GameEvents.Instance.EmitRoomPlaced("bunk_pod", 5)` and immediately asserts that WishBoard tracked the room type will likely pass (synchronous chain). But a test that loads a save via `SaveManager.ApplyState()` and immediately checks if rooms are restored will fail -- the frame-delay pattern means `ApplySceneState` hasn't run yet.

**Why it happens:**
GoDotTest test methods are `async Task` and can `await` things, but there's no built-in "wait N frames" utility. GodotTestDriver provides `Fixture.WaitForFrames()` but only if you're using fixtures. Developers writing quick unit tests forget that some operations are deferred.

**How to avoid:**
1. Create a `TestHelper.WaitFrames(SceneTree tree, int count)` async utility that awaits `tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame)` the specified number of times.
2. Document which operations are synchronous (safe to assert immediately) and which are deferred (require frame advancement):
   - **Synchronous:** Event emission and handler invocation, dictionary mutations, property changes.
   - **Deferred (1+ frames):** `QueueFree` node removal, `SaveManager.ScheduleSceneRestore()` (2 frames), Tween progress, Timer timeouts, `_Process` updates.
3. For save/load integration tests, always await at least 3 frames after `ScheduleSceneRestore()` before asserting scene state.
4. Use GodotTestDriver's `Fixture` class for integration tests -- it handles frame management correctly.

**Warning signs:**
- Save/load tests pass sometimes and fail other times (race with frame processing).
- Assertions on node state after `QueueFree` succeed because the node isn't freed yet (false positive).
- Tests pass locally (fast machine, frames process quickly) but fail in CI (slower, timing differs).

**Phase to address:**
Phase 1 (Framework Setup) -- the `WaitFrames` helper is foundational. Phase 2+ uses it extensively.

---

### Pitfall 8: Testing MoodSystem Decay Without Controlling Delta Time

**What goes wrong:**
`MoodSystem.Update(float delta, int lifetimeHappiness)` uses exponential smoothing with `alpha = 1 - exp(-decayRate * delta)`. In production, `delta` comes from `HappinessManager._Process(double delta)`, which is frame-time-dependent. In tests, if you call `MoodSystem.Update()` directly with a fabricated delta, the results depend on the exact delta value. If you let `_Process` run during tests (because the autoload is active), the delta is real wall-clock time, making decay amounts non-deterministic.

A test like "mood should decay from 0.5 to below 0.45 after 10 seconds" will be flaky because the actual decay depends on frame rate, test execution speed, and whether the CI runner is under load.

**Why it happens:**
`MoodSystem` is a POCO that receives delta from its owner. This is actually good design for testability -- but only if tests bypass the `HappinessManager._Process` path and call `MoodSystem.Update()` directly with controlled deltas. The danger is that a developer writes an integration test that relies on real time passing.

**How to avoid:**
1. Test MoodSystem directly as a POCO (Tier 1 test). Construct it with a known `HappinessConfig`, call `Update()` with exact delta values, assert exact results:
   ```csharp
   var config = new HappinessConfig();
   var mood = new MoodSystem(config);
   mood.OnWishFulfilled(); // mood jumps to 0.06
   mood.Update(1.0f, 0);  // 1 second of decay
   // Assert mood is between expected bounds
   ```
2. Never test mood decay via wall-clock time. If you need to test the full HappinessManager pipeline, stop the autoload's processing and manually invoke `_Process` with controlled deltas.
3. For the tier threshold tests (Quiet->Cozy at 0.2, Cozy->Lively at 0.4, etc.), set mood directly via `RestoreState()` to just above/below thresholds rather than trying to reach them through decay simulation.

**Warning signs:**
- Mood tests with exact equality assertions (`Assert.Equal(0.437f, mood.Mood)`) that fail due to floating-point drift.
- Tests with `Thread.Sleep()` or `await Task.Delay()` to "wait for decay" -- these are always wrong.
- Flaky CI failures in mood tests that pass locally.

**Phase to address:**
Phase 2 or 3 (mood system test suite) -- but the principle of "test POCOs directly with controlled inputs" should be established in Phase 1.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Testing singletons via static Instance instead of injected dependencies | No refactoring needed, tests work immediately | Tests are coupled to global state, can't run in parallel, require explicit reset between tests | Acceptable for this project -- 8 singletons, single-player game, sequential test runner. Dependency injection would be over-engineering |
| Putting ResetForTesting() methods directly on production singletons | Clean API, easy to call | Test-only code in production classes, risk of accidentally calling in production | Acceptable with `#if DEBUG` guard or `[Conditional("DEBUG")]` attribute. The alternative (test-only subclasses) adds complexity for no real benefit |
| Hardcoding JSON test fixtures as string literals in test files | Fast to write, no external files to manage | Long strings in test code are hard to read and maintain, duplicate the save format definition | Only in initial implementation. Move to embedded resource files once you have 3+ fixture strings |
| Skipping integration tests for UI components (tooltip, info panel, HUD) | Faster test suite, fewer brittle tests | UI bugs not caught until manual testing | Acceptable for v1.3 -- focus on logic tests. UI integration tests are a v1.4+ concern |
| Not mocking Godot API calls (ResourceLoader, FileAccess, GD.Randi) | No mocking framework complexity | Tests depend on res:// filesystem existing, random values are non-deterministic | Acceptable if: (a) pure logic tests avoid Godot APIs entirely, (b) integration tests accept non-determinism in GD.Randi by testing ranges not exact values, (c) ResourceLoader tests use real .tres files |

## Integration Gotchas

| Integration Point | Common Mistake | Correct Approach |
|-------------------|----------------|------------------|
| GoDotTest + project.godot | Changing `run/main_scene` to the test scene permanently | Keep the game's main scene as default. Use command-line `--run-tests` flag detection in the main scene script to redirect to test scene. Or use a separate Godot project profile |
| GoDotTest + Godot 4.4 exit codes | Godot prints ObjectDB leak warnings on exit that look like errors | These are cosmetic. Godot's shutdown prints "Leaked instance" for autoloads that outlive the test runner. Suppress in CI by checking test output, not exit code alone. Use `--coverage` flag which force-exits cleanly |
| GodotTestDriver Fixture + autoload singletons | Using Fixture to load the full game scene for every test | Fixtures should load minimal test scenes. Full game scene pulls in all UI, audio, ring mesh -- 90% irrelevant to the test and slow to load |
| GoDotTest TestClass + async tests | Forgetting that test methods must return `Task` (not `void`) to be properly awaited | All test methods with `[Test]` attribute must be `public async Task MethodName()`. If they return void, exceptions are swallowed silently |
| SaveManager + test file paths | Tests calling `SaveManager.Instance.Load()` which reads the real save file | Either override save path for tests, or bypass SaveManager entirely by testing serialization directly with `JsonSerializer.Serialize/Deserialize` |
| GameEvents + test assertions | Subscribing to events for assertions but forgetting to unsubscribe | Use a disposable `EventSpy<T>` wrapper that tracks invocations and unsubscribes on Dispose. Pattern: `using var spy = new EventSpy(() => GameEvents.Instance.CreditsChanged += spy.Handler, () => GameEvents.Instance.CreditsChanged -= spy.Handler);` |

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Loading full game scene in every integration test | Test suite takes 30+ seconds, CI timeout | Create minimal `.tscn` files for testing (just the nodes needed). Most tests need zero scene -- test POCOs directly | Immediately -- first test suite will be painfully slow |
| Not stopping autoload Timers during test runs | Income timer fires dozens of times during a long test suite, economy tests become flaky | Stop all Timers in `ResetForTesting()`. Only start timers explicitly when testing timer-dependent behavior | After 10+ tests that each take 1+ second (cumulative timer firings) |
| WishBoard loading all .tres templates from disk on every test reset | Each `_Ready()` call triggers filesystem scan of `res://Resources/Wishes/` | Don't re-initialize WishBoard between tests unless testing wish-related features. For wish tests, cache templates on first load | At 20+ test suites with full reset (disk I/O adds up) |
| Creating and freeing dozens of CitizenNode instances per test without cleanup | Orphan nodes accumulate in scene tree, Godot logs "Leaked instance" warnings on exit | Use `QueueFree()` in `[Cleanup]` and await one frame for deferred deletion. Or use GodotTestDriver Fixture which handles this | After 100+ citizen node creations across all tests |

## Testing Anti-Patterns Specific to This Architecture

### Anti-Pattern 1: Testing Cross-Singleton Chains as Unit Tests

**What it looks like:**
A test titled "test_room_placement_triggers_housing_assignment" that calls `BuildManager.Instance.PlaceRoom(...)`, then asserts `HousingManager.Instance.GetOccupantCount(segment) > 0`. This is testing the entire event chain: BuildManager -> GameEvents.RoomPlaced -> HousingManager.OnRoomPlaced -> AssignUnhousedCitizens.

**Why it's bad:**
If this test fails, you don't know which link in the chain broke. Is the event not firing? Is HousingManager not subscribed? Is the assignment algorithm wrong? Is the capacity not tracked? The test is an integration test pretending to be a unit test.

**Instead:**
- Unit test HousingManager.FindBestRoom logic with controlled room capacity data.
- Unit test that calling AssignCitizen updates all three data structures.
- Integration test that the event chain works end-to-end (clearly labeled as integration).

### Anti-Pattern 2: Asserting Exact Floating-Point Values from Mood System

**What it looks like:**
`Assert.Equal(0.06f, moodSystem.Mood)` after `OnWishFulfilled()`.

**Why it's bad:**
`MoodSystem.OnWishFulfilled()` does `_mood = MathF.Min(1.0f, _mood + _config.MoodGainPerWish)`. If `_mood` was 0f and `MoodGainPerWish` is 0.06f, the result IS exactly 0.06f. But after any `Update()` call with decay, floating-point arithmetic makes exact equality unreliable.

**Instead:**
Use tolerance-based assertions: `Assert.InRange(moodSystem.Mood, 0.055f, 0.065f)`. For tier threshold tests, test the tier enum directly: `Assert.Equal(MoodTier.Quiet, moodSystem.CurrentTier)`.

### Anti-Pattern 3: Testing Save Round-Trip Through SaveManager Instead of Serialization

**What it looks like:**
```csharp
SaveManager.Instance.PerformSave(); // writes to disk
var loaded = SaveManager.Instance.Load(); // reads from disk
Assert.Equal(expectedCredits, loaded.Credits);
```

**Why it's bad:**
This tests the filesystem, JSON serialization, AND SaveManager orchestration all at once. If it fails, is it a serialization bug, a file permission issue, or a data collection bug? It also writes to the real save path (Pitfall 5).

**Instead:**
- Test serialization directly: `JsonSerializer.Serialize(saveData)` -> `JsonSerializer.Deserialize<SaveData>(json)`.
- Test `CollectGameState()` separately (set singleton state, verify the returned SaveData has correct values).
- Test `ApplyState()`/`ApplySceneState()` separately (provide known SaveData, verify singletons have correct state after).
- Reserve full round-trip for one integration test with isolated file path.

## "Looks Done But Isn't" Checklist

- [ ] **Test runner scene:** The `.tscn` file exists and `--run-tests` flag detection works -- verify it runs from BOTH command line AND editor play button
- [ ] **Singleton reset:** ResetForTesting() clears ALL mutable state on ALL 8 singletons -- verify by printing state after reset, comparing to fresh-launch state
- [ ] **Timer suppression:** All autoload Timers stopped during tests -- verify by checking `Timer.IsStopped()` in a test assertion
- [ ] **Event cleanup:** GameEvents subscriber lists cleared between suites -- verify by checking that event invocations in test N+1 don't trigger handlers from test N
- [ ] **Save path isolation:** Tests never touch `user://save.json` -- verify by checking file modification timestamp before and after test run
- [ ] **v1 save compat:** Test loads actual v1-format JSON (not a v3 object serialized with Version=1) -- verify by hand-crafting JSON without v2/v3 fields
- [ ] **v2 save compat:** Test loads actual v2-format JSON without HomeSegmentIndex -- verify citizens become unhoused (auto-assigned) on load
- [ ] **CI readiness:** Tests run and pass in headless Godot (`--headless` flag) -- verify by running `godot --headless --run-tests` before pushing CI config
- [ ] **Frame advancement:** All deferred operations have corresponding frame waits -- verify by removing waits and confirming tests fail (test the tests)
- [ ] **Coverage collection:** `--coverage` flag works with coverlet and produces coverage report -- verify report includes game code namespaces, not just test code

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Singleton state leaking between tests | LOW | Add ResetForTesting() methods, call in [Cleanup]. Can be done incrementally per singleton as tests are written |
| Event subscriber accumulation | LOW | Add ClearAllSubscribers() to GameEvents, call in ResetForTesting(). All singletons re-subscribe in their own reset |
| Scene tree mismatch crashes | LOW | Categorize failing tests into tiers, move scene-dependent tests to integration tier with minimal test scenes |
| Timer interference making tests flaky | LOW | Add Timer.Stop() calls in ResetForTesting(). Immediate fix, no architectural change |
| Save file corruption during tests | MEDIUM | Implement SavePathOverride. Requires modifying SaveManager (production code change), but small and safe |
| Save format version tests incomplete | LOW | Add JSON fixture files for v1/v2/v3. Pure test-data work, no production code changes |
| Frame advancement missing | MEDIUM | Audit all async test methods for operations that need frame waits. Add WaitFrames helper. May require rewriting some tests |
| Mood tests flaky due to timing | LOW | Refactor to test MoodSystem POCO directly with controlled deltas. No production code change needed |

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Singleton state leaking (Pitfall 1) | Phase 1: Framework Setup | Run full test suite 3 times consecutively -- all must pass |
| Event subscriber accumulation (Pitfall 2) | Phase 1: Framework Setup | Add an assertion counting GameEvents subscribers at start of each suite -- must be baseline count |
| Scene tree lifecycle mismatch (Pitfall 3) | Phase 1: Framework Setup | Document test tiers in test project README. Pure logic tests must not import Godot namespaces |
| Timer interference (Pitfall 4) | Phase 1: Framework Setup | Assert all Timers are stopped at test suite start |
| Save path isolation (Pitfall 5) | Phase 1: Framework Setup | CI run must not create/modify user://save.json |
| Save format edge cases (Pitfall 6) | Phase 2-3: Save/Load Tests | v1, v2, v3 JSON fixtures all load without exceptions |
| Frame advancement (Pitfall 7) | Phase 1: Framework Setup | WaitFrames helper exists and is documented |
| MoodSystem timing (Pitfall 8) | Phase 2-3: Mood Tests | Zero use of Thread.Sleep or Task.Delay in mood tests |

## Sources

- [GoDotTest GitHub repository](https://github.com/chickensoft-games/GoDotTest) -- test runner architecture, TestClass lifecycle, command-line flags
- [GodotTestDriver GitHub repository](https://github.com/chickensoft-games/GodotTestDriver) -- Fixture class, input simulation, test driver pattern
- [GoDotTest README](https://github.com/chickensoft-games/GoDotTest/blob/main/README.md) -- [Setup]/[Cleanup] attributes, sequential execution, --run-tests flag
- [Godot Forum: Using GUT to test autoload singletons](https://forum.godotengine.org/t/using-gut-to-test-instantiating-scenes-via-autoload-singleton-event-bus-rootscene/86974) -- nodes not in tree during tests, explicit add_child required
- [Godot Forum: C# Event Handlers ObjectDisposedException](https://forum.godotengine.org/t/c-event-handlers-triggering-unhandled-exception-system-objectdisposedexception-cannot-access-a-disposed-object/17794) -- disposed node event handler invocation
- [Godot GitHub Issue #66319](https://github.com/godotengine/godot/issues/66319) -- signal delegates not disconnecting on node free
- [Godot GitHub Issue #74984](https://github.com/godotengine/godot/issues/74984) -- signal trying to access freed listeners
- [DEV Community: Don't use Singleton Pattern in your unit tests](https://dev.to/bacarpereira/don-t-use-singleton-pattern-in-your-unit-tests-8p7) -- state leaking between tests
- [Understanding tree order (Godot 4 Recipes)](https://kidscancode.org/godot_recipes/4.x/basics/tree_ready_order/index.html) -- _EnterTree vs _Ready lifecycle
- [Godot Documentation: Node Lifecycle](https://deepwiki.com/godotengine/godot-docs/5.4-node-lifecycle-and-processing) -- _EnterTree, _Ready, _Process order
- Direct codebase analysis of all 8 autoload singletons: GameEvents.cs, EconomyManager.cs, BuildManager.cs, CitizenManager.cs, WishBoard.cs, HappinessManager.cs, HousingManager.cs, SaveManager.cs
- Direct codebase analysis of MoodSystem.cs (POCO testability pattern), SafeNode.cs (event lifecycle pattern), SaveData/SavedCitizen/SavedRoom (serialization contracts)

---
*Pitfalls research for: Orbital Rings v1.3 Testing Infrastructure*
*Researched: 2026-03-07*
