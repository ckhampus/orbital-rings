# Architecture Research

**Domain:** Testing infrastructure for Godot 4 C# game with autoload singleton architecture
**Researched:** 2026-03-07
**Confidence:** MEDIUM-HIGH (GoDotTest/GodotTestDriver APIs verified against NuGet and GitHub docs; singleton testing patterns derived from direct codebase analysis; some integration patterns inferred from framework design rather than verified production usage)

---

## Executive Summary

Integrating GoDotTest + GodotTestDriver into Orbital Rings requires navigating a fundamental tension: the codebase relies on 8 autoload singletons accessed via static `Instance` properties, while testing best practices demand isolation between test runs. The architecture recommended here works with this reality rather than fighting it. The game's autoloads already initialize before scene nodes enter the tree, which means GoDotTest's test runner scene inherits a fully initialized singleton graph automatically. The key insight is to classify tests into three tiers -- pure C# tests (no engine), singleton-aware tests (engine + autoloads), and scene integration tests (engine + autoloads + scene tree) -- and structure the test project so each tier is distinct in both organization and setup complexity.

The existing codebase has one major testability advantage: MoodSystem is already a POCO (plain C# object) with no Godot Node inheritance, proving the project already separates pure logic from engine lifecycle. The economy formulas in EconomyManager are also pure functions (CalculateTickIncome, CalculateRoomCost, CalculateDemolishRefund). The save data POCOs (SaveData, SavedRoom, SavedCitizen) are plain C# types serializable without engine context. These are the easiest wins for test coverage.

The harder challenge is testing systems that depend on singleton state: HousingManager's fewest-occupants-first algorithm reads from BuildManager.Instance, and HappinessManager's arrival gating reads from HousingManager.Instance. For these, the test approach is "bootstrap and reset" -- let autoloads initialize naturally via the test runner scene, then reset their internal state at the start of each test suite using existing public APIs (ClearAllRooms, ClearCitizens, ClearAssignments, RestoreCredits).

---

## System Overview

### Testing Architecture Layers

```
+---------------------------------------------------------------+
|                     Test Runner Scene                          |
|  (TestRunner.tscn + TestRunner.cs -- main scene for tests)    |
+----------------------------+----------------------------------+
|                            |                                  |
|  Autoloads (project.godot) |  Test Scene (Node2D root)       |
|  GameEvents                |  passed to TestClass constructor  |
|  EconomyManager            |  tests add/remove child nodes    |
|  BuildManager              |                                  |
|  CitizenManager            |                                  |
|  WishBoard                 |                                  |
|  HappinessManager          |                                  |
|  HousingManager            |                                  |
|  SaveManager               |                                  |
+----------------------------+----------------------------------+
                    |
    +---------------+---------------+
    |               |               |
  Tier 1          Tier 2          Tier 3
  Pure C#         Singleton       Scene
  Tests           Tests           Tests
  (MoodSystem,    (Economy calc,  (Save round-
  SaveData,       Housing assign, trip, wish
  SegmentGrid)    Mood tiers)     fulfillment)
```

### Test Execution Flow

```
Godot starts with --run-tests flag
    |
    v
project.godot loads autoloads (1-8) -- all singletons initialize
    |
    v
TestRunner.tscn is the main scene (or test entry detects --run-tests)
    |
    v
TestRunner._Ready() calls GoTest.RunTests(Assembly, this)
    |
    v
GoDotTest discovers all TestClass subclasses via reflection
    |
    v
For each test class (sequential, NOT parallel):
    [SetupAll] -> [Setup] -> [Test] -> [Cleanup] -> ... -> [CleanupAll]
    |
    v
After all suites: exit with result code (--quit-on-finish)
```

---

## Recommended Project Structure

```
Orbital Rings/
+-- Scripts/                    # Production code (unchanged)
|   +-- Autoloads/
|   +-- Build/
|   +-- Citizens/
|   +-- Core/
|   +-- Data/
|   +-- Happiness/
|   +-- Ring/
|   +-- UI/
|   +-- Audio/
|   +-- Camera/
|
+-- test/                       # All test code lives here
|   +-- TestRunner.cs           # GoDotTest entry point script
|   +-- TestRunner.tscn         # Minimal Node2D scene for test execution
|   +-- Helpers/
|   |   +-- TestHelper.cs       # Singleton reset utilities
|   |   +-- SaveDataBuilder.cs  # Builder for constructing test save data
|   +-- Unit/                   # Tier 1: Pure C# tests (no engine deps)
|   |   +-- MoodSystemTest.cs
|   |   +-- SegmentGridTest.cs
|   |   +-- SaveDataSerializationTest.cs
|   |   +-- EconomyFormulaTest.cs
|   |   +-- HousingCapacityTest.cs
|   +-- Integration/            # Tier 2: Singleton-aware tests
|   |   +-- EconomyManagerTest.cs
|   |   +-- HousingManagerTest.cs
|   |   +-- HappinessManagerTest.cs
|   |   +-- MoodTierTransitionTest.cs
|   +-- System/                 # Tier 3: Full scene/save tests
|       +-- SaveLoadRoundTripTest.cs
|       +-- WishFulfillmentTest.cs
|
+-- Scenes/                     # Production scenes (unchanged)
+-- Resources/                  # Production resources (unchanged)
+-- Orbital Rings.csproj        # Add test package references
+-- project.godot               # No changes needed (autoloads stay)
```

### Structure Rationale

- **test/ at project root:** Matches GoDotTest's recommended pattern and the project's existing folder convention. Not inside Scripts/ because test code is not game logic. The `.csproj` conditional exclusion (`<DefaultItemExcludes>` for ExportRelease) keeps test code out of release builds.
- **Three subdirectories by tier, not by feature:** Tests grouped by execution characteristics (pure C#, needs singletons, needs scene tree) because setup/teardown complexity differs dramatically between tiers. A MoodSystem unit test needs zero setup; a save/load round-trip test needs rooms placed, citizens spawned, and housing assigned.
- **Helpers/ directory:** Shared test utilities that are neither tests nor production code. TestHelper provides singleton reset methods. SaveDataBuilder provides a fluent API for constructing test save data with different format versions.

---

## Architectural Patterns

### Pattern 1: Test Runner Scene with Conditional Entry

**What:** A single entry point that detects the `--run-tests` command line flag and either runs tests or loads the normal game. This avoids needing a separate Godot project or changing the main scene.

**When to use:** Always. GoDotTest requires a scene node as the test root.

**Trade-offs:** The main scene script gains a conditional check. This is a few lines of code and keeps the project as a single Godot project rather than requiring a separate test project.

**Implementation:**

The TestRunner scene is a minimal Node2D with a script that calls GoDotTest:

```csharp
// test/TestRunner.cs
using System.Reflection;
using Godot;
using GoDotTest;

public partial class TestRunner : Node2D
{
    public override async void _Ready()
        => await GoTest.RunTests(Assembly.GetExecutingAssembly(), this);
}
```

The game's existing main scene (TitleScreen) gains a test detection guard:

```csharp
// In Scripts/UI/TitleScreen.cs, at the top of _Ready():
#if DEBUG
var env = TestEnvironment.From(OS.GetCmdlineArgs());
if (env.ShouldRunTests)
{
    GetTree().ChangeSceneToFile("res://test/TestRunner.tscn");
    return;
}
#endif
// ... normal title screen logic ...
```

This approach means the main scene in project.godot stays as TitleScreen.tscn. When `--run-tests` is passed, TitleScreen immediately redirects to the test runner scene. The `#if DEBUG` guard ensures zero overhead in release builds.

**Alternative considered: Changing main scene to test runner.** Rejected because it would require changing project.godot for every test run and changing it back for playtesting. The conditional approach is simpler.

### Pattern 2: Singleton Reset Between Test Suites

**What:** A TestHelper class that resets all 8 singleton instances to a clean state before each test suite, using the public APIs the singletons already expose.

**When to use:** For Tier 2 (singleton-aware) and Tier 3 (scene) tests. Not needed for Tier 1 (pure C#) tests.

**Trade-offs:** Relies on singletons having adequate reset APIs. The codebase already has RestoreCredits, ClearAllRooms, ClearCitizens, RestoreFromSave, RestoreActiveWishes -- this is sufficient. Does not provide true isolation (singletons share process memory), but is practical for the scope of this project.

**Implementation:**

```csharp
// test/Helpers/TestHelper.cs
using OrbitalRings.Autoloads;
using OrbitalRings.Data;

namespace OrbitalRings.Tests.Helpers;

public static class TestHelper
{
    /// <summary>
    /// Resets all singletons to a clean initial state.
    /// Call in [SetupAll] or [Setup] for tests that depend on singleton state.
    /// </summary>
    public static void ResetAllSingletons()
    {
        // Reset economy to starting credits
        EconomyManager.Instance?.RestoreCredits(
            EconomyManager.Instance.Config?.StartingCredits ?? 750);
        EconomyManager.Instance?.SetCitizenCount(0);
        EconomyManager.Instance?.SetWorkingCitizenCount(0);
        EconomyManager.Instance?.SetMoodTier(MoodTier.Quiet);

        // Clear build state -- note: requires RingVisual which may not exist
        // in test runner scene. BuildManager guards against null _ringVisual.
        // For tests needing room placement, load the game scene instead.

        // Reset happiness to zero
        HappinessManager.Instance?.RestoreState(
            0, 0f, 0f, new System.Collections.Generic.HashSet<string>
            { "bunk_pod", "air_recycler", "workshop",
              "reading_nook", "storage_bay", "garden_nook" }, 0);

        // Clear citizens (safe even if none exist)
        CitizenManager.Instance?.ClearCitizens();

        // Clear housing
        // HousingManager needs BuildManager rooms cleared first
        // but HousingManager.RestoreFromSave with empty list effectively clears

        // Clear wish board
        WishBoard.Instance?.RestoreActiveWishes(
            new System.Collections.Generic.Dictionary<string, string>());
        WishBoard.Instance?.RestorePlacedRoomTypes(
            new System.Collections.Generic.Dictionary<string, int>());

        // Reset StateLoaded flags
        CitizenManager.StateLoaded = false;
        EconomyManager.StateLoaded = false;
        HousingManager.StateLoaded = false;
    }
}
```

### Pattern 3: Three-Tier Test Classification

**What:** Every test class is categorized by its dependency level, determining its setup requirements.

**Tier 1 -- Pure C# (test/Unit/):**
- Tests classes that have no Godot Node inheritance and no singleton dependencies
- Examples: MoodSystem, SegmentGrid, SaveData serialization, economy formulas, HousingManager.ComputeCapacity
- Setup: Construct the class directly. No scene tree needed.
- These tests run fastest and are the most reliable.

```csharp
public class MoodSystemTest : TestClass
{
    public MoodSystemTest(Node testScene) : base(testScene) { }

    [Test]
    public void WishFulfilledIncreasesMood()
    {
        var config = new HappinessConfig();
        var mood = new MoodSystem(config);

        // Initial state: mood is 0, tier is Quiet
        Assert.Equal(MoodTier.Quiet, mood.CurrentTier);

        // Fulfill a wish
        mood.OnWishFulfilled();

        // Mood should increase by MoodGainPerWish (0.06)
        Assert.True(mood.Mood > 0f);
    }
}
```

**Tier 2 -- Singleton-Aware (test/Integration/):**
- Tests that read or mutate singleton state but do not require scene tree nodes beyond the test root
- Examples: EconomyManager income calculation with citizen counts, HousingManager assignment with mock room data, HappinessManager tier transitions
- Setup: Call TestHelper.ResetAllSingletons() in [SetupAll] or [Setup]. Manipulate singleton state via public APIs.

```csharp
public class EconomyManagerTest : TestClass
{
    public EconomyManagerTest(Node testScene) : base(testScene) { }

    [SetupAll]
    public void SetupAll()
    {
        TestHelper.ResetAllSingletons();
    }

    [Test]
    public void IncomeIncludesBaseStationIncome()
    {
        EconomyManager.Instance.SetCitizenCount(0);
        EconomyManager.Instance.SetMoodTier(MoodTier.Quiet);

        int income = EconomyManager.Instance.CalculateTickIncome();

        // Base station income is 1.0, Quiet multiplier is 1.0
        // Round(1.0 * 1.0) = 1
        Assert.Equal(1, income);
    }
}
```

**Tier 3 -- Scene Integration (test/System/):**
- Tests that require scene nodes, frame advancement, or complex multi-singleton coordination
- Examples: Save/load round-trip, wish fulfillment loop (citizen visits room, wish fulfilled, happiness increases)
- Setup: May need to load game scene or construct scene fragments. Uses GodotTestDriver's Fixture for managed node lifecycle.

```csharp
public class SaveLoadRoundTripTest : TestClass
{
    public SaveLoadRoundTripTest(Node testScene) : base(testScene) { }

    [SetupAll]
    public void SetupAll()
    {
        TestHelper.ResetAllSingletons();
    }

    [Test]
    public void SaveDataSerializesAndDeserializesCorrectly()
    {
        var original = SaveDataBuilder.CreateV3()
            .WithCredits(500)
            .WithLifetimeHappiness(10)
            .WithMood(0.45f)
            .WithRoom("bunk_pod", row: 0, startPos: 0, segments: 2, cost: 140)
            .WithCitizen("Nova", bodyType: 0, homeSegment: 0)
            .WithCitizen("Pixel", bodyType: 1, homeSegment: null)
            .Build();

        string json = System.Text.Json.JsonSerializer.Serialize(original);
        var loaded = System.Text.Json.JsonSerializer.Deserialize<SaveData>(json);

        Assert.Equal(3, loaded.Version);
        Assert.Equal(500, loaded.Credits);
        Assert.Equal(10, loaded.LifetimeHappiness);
        Assert.Equal(2, loaded.Citizens.Count);
        Assert.Equal(0, loaded.Citizens[0].HomeSegmentIndex);
        Assert.Null(loaded.Citizens[1].HomeSegmentIndex);
    }
}
```

### Pattern 4: SaveDataBuilder for Test Fixtures

**What:** A fluent builder for constructing SaveData objects with different format versions and configurations. Eliminates repetitive POCO construction across test classes.

**When to use:** Any test involving save/load data, format version compatibility, or game state restoration.

```csharp
// test/Helpers/SaveDataBuilder.cs
using System.Collections.Generic;
using OrbitalRings.Autoloads;

namespace OrbitalRings.Tests.Helpers;

public class SaveDataBuilder
{
    private readonly SaveData _data;

    private SaveDataBuilder(int version)
    {
        _data = new SaveData { Version = version };
    }

    public static SaveDataBuilder CreateV1() => new(1);
    public static SaveDataBuilder CreateV2() => new(2);
    public static SaveDataBuilder CreateV3() => new(3);

    public SaveDataBuilder WithCredits(int credits)
    {
        _data.Credits = credits;
        return this;
    }

    public SaveDataBuilder WithLifetimeHappiness(int lh)
    {
        _data.LifetimeHappiness = lh;
        return this;
    }

    public SaveDataBuilder WithMood(float mood)
    {
        _data.Mood = mood;
        return this;
    }

    public SaveDataBuilder WithMoodBaseline(float baseline)
    {
        _data.MoodBaseline = baseline;
        return this;
    }

    public SaveDataBuilder WithRoom(string roomId, int row, int startPos,
        int segments, int cost)
    {
        _data.PlacedRooms.Add(new SavedRoom
        {
            RoomId = roomId,
            Row = row,
            StartPos = startPos,
            SegmentCount = segments,
            Cost = cost
        });
        return this;
    }

    public SaveDataBuilder WithCitizen(string name, int bodyType = 0,
        int? homeSegment = null, string wishId = null)
    {
        _data.Citizens.Add(new SavedCitizen
        {
            Name = name,
            BodyType = bodyType,
            PrimaryR = 0.9f, PrimaryG = 0.6f, PrimaryB = 0.5f,
            SecondaryR = 0.6f, SecondaryG = 0.85f, SecondaryB = 0.7f,
            WalkwayAngle = 0f,
            Direction = 1f,
            CurrentWishId = wishId,
            HomeSegmentIndex = homeSegment
        });
        return this;
    }

    public SaveDataBuilder WithUnlockedRoom(string roomId)
    {
        _data.UnlockedRooms.Add(roomId);
        return this;
    }

    public SaveDataBuilder WithActiveWish(string citizenName, string wishId)
    {
        _data.ActiveWishes[citizenName] = wishId;
        return this;
    }

    public SaveData Build() => _data;
}
```

### Pattern 5: Assertions Without External Framework

**What:** GoDotTest does not include an assertion library. Use a lightweight custom Assert class or adopt Shouldly/FluentAssertions via NuGet.

**Trade-offs:**
- Custom Assert: Zero dependencies, minimal API, easy to understand. Sufficient for the scope of this project.
- Shouldly: Richer assertion messages, widely used in .NET. Adds a NuGet dependency.

**Recommendation:** Start with a minimal custom Assert class. Add Shouldly later if assertion messages become a pain point.

```csharp
// test/Helpers/Assert.cs
using System;

namespace OrbitalRings.Tests.Helpers;

public static class Assert
{
    public static void Equal<T>(T expected, T actual, string message = null)
    {
        if (!Equals(expected, actual))
            throw new Exception(
                message ?? $"Expected {expected} but got {actual}");
    }

    public static void True(bool condition, string message = null)
    {
        if (!condition)
            throw new Exception(message ?? "Expected true but got false");
    }

    public static void False(bool condition, string message = null)
    {
        if (condition)
            throw new Exception(message ?? "Expected false but got true");
    }

    public static void Null(object value, string message = null)
    {
        if (value != null)
            throw new Exception(message ?? $"Expected null but got {value}");
    }

    public static void NotNull(object value, string message = null)
    {
        if (value == null)
            throw new Exception(message ?? "Expected non-null but got null");
    }

    public static void ApproxEqual(float expected, float actual,
        float tolerance = 0.001f, string message = null)
    {
        if (MathF.Abs(expected - actual) > tolerance)
            throw new Exception(
                message ?? $"Expected ~{expected} but got {actual} " +
                           $"(tolerance {tolerance})");
    }

    public static void Throws<TException>(Action action, string message = null)
        where TException : Exception
    {
        try
        {
            action();
            throw new Exception(
                message ?? $"Expected {typeof(TException).Name} but no exception was thrown");
        }
        catch (TException) { /* expected */ }
    }
}
```

---

## Component Responsibilities

| Component | Responsibility | Typical Implementation |
|-----------|----------------|------------------------|
| **TestRunner.cs** | Entry point for GoDotTest. Detects `--run-tests` flag and kicks off test discovery and execution. | Node2D script calling `GoTest.RunTests()` in `_Ready()` |
| **TestRunner.tscn** | Minimal scene providing a root node for GoDotTest. No visual nodes needed. | Single Node2D with TestRunner.cs attached |
| **TestHelper.cs** | Resets all 8 singletons to clean state between test suites. Provides shorthand for common test setup operations. | Static methods calling existing singleton public APIs (RestoreCredits, ClearCitizens, etc.) |
| **SaveDataBuilder.cs** | Fluent builder for constructing SaveData with specific versions and game state configurations. | Builder pattern with version-specific factory methods (CreateV1/V2/V3) |
| **Assert.cs** | Lightweight assertion library throwing descriptive exceptions on failure. | Static methods: Equal, True, False, Null, NotNull, ApproxEqual, Throws |
| **Unit tests (test/Unit/)** | Test pure C# classes with no engine dependencies. Fast, reliable, no setup. | Directly construct POCOs and call methods |
| **Integration tests (test/Integration/)** | Test singleton behavior with controlled state. Reset before each suite. | Call TestHelper.ResetAllSingletons() in [SetupAll], manipulate via public APIs |
| **System tests (test/System/)** | Test multi-system flows including serialization round-trips. | Construct SaveData, serialize/deserialize, verify field preservation |

---

## Integration Points with Existing Autoloads

### How Autoloads Behave During Test Runs

When Godot starts with `--run-tests`, the engine loads autoloads from project.godot BEFORE any scene enters the tree. This means:

1. **GameEvents.Instance** is set in `_EnterTree()` -- available immediately
2. **EconomyManager.Instance** is set in `_Ready()` -- creates Timer, loads EconomyConfig, subscribes to events
3. **BuildManager.Instance** is set in `_Ready()` -- tries to find RingVisual (will be null in test scene), loads RoomDefinitions
4. **CitizenManager.Instance** is set in `_Ready()` -- tries to find RingVisual (null), spawns 5 starter citizens (attaching to self as parent)
5. **WishBoard.Instance** is set in `_Ready()` -- loads WishTemplates from Resources/Wishes/
6. **HappinessManager.Instance** is set in `_Ready()` -- creates Timer, loads HappinessConfig, creates MoodSystem
7. **HousingManager.Instance** is set in `_EnterTree()` -- subscribes to events
8. **SaveManager.Instance** is set in `_Ready()` -- creates debounce Timer, subscribes to all state-change events

**Critical implication:** By the time TestRunner._Ready() fires, all 8 singletons are initialized with their default state. CitizenManager has already spawned 5 starter citizens (because StateLoaded is false). EconomyManager has 750 starting credits. HappinessManager is at Quiet tier. This is the "warm start" state that TestHelper.ResetAllSingletons() must clean up.

### Singleton Testability Matrix

| Singleton | Reset API Available | Pure Functions Available | Testing Strategy |
|-----------|--------------------|-----------------------|-----------------|
| **GameEvents** | Events auto-reset (no persistent state) | N/A (event bus only) | No reset needed. Subscribe in test, verify event fires. |
| **EconomyManager** | `RestoreCredits()`, `SetCitizenCount()`, `SetWorkingCitizenCount()`, `SetMoodTier()` | `CalculateTickIncome()`, `CalculateRoomCost()`, `CalculateDemolishRefund()`, `GetIncomeBreakdown()` | Tier 1 for formulas (extract to static helpers or test via Instance). Tier 2 for state-dependent income. |
| **BuildManager** | `ClearAllRooms()` (needs RingVisual) | `GetPlacedRoom()` (read-only query) | Tier 2/3 only -- room placement requires RingVisual scene node. Use RestorePlacedRoom for controlled setup, but also requires RingVisual. |
| **CitizenManager** | `ClearCitizens()` | None (all methods mutate state or need scene tree) | Tier 2 for citizen count tracking. Tier 3 for spawn/despawn behavior. |
| **WishBoard** | `RestoreActiveWishes()`, `RestorePlacedRoomTypes()` | `GetTemplateById()`, `IsRoomTypeAvailable()` | Tier 2 for wish tracking state. |
| **HappinessManager** | `RestoreState()` | `LifetimeWishes` (getter), `Mood` (getter), `CurrentTier` (getter) | Tier 1 for MoodSystem POCO. Tier 2 for tier transitions via RestoreState + manual updates. |
| **HousingManager** | `RestoreFromSave()` (clears and rebuilds) | `ComputeCapacity()` (static pure function) | Tier 1 for ComputeCapacity. Tier 2 for assignment logic (needs BuildManager rooms). |
| **SaveManager** | `PendingLoad` (settable), `ClearSave()` | `Load()` (reads file) | Tier 3 for round-trip testing. File I/O via Godot's `user://` path. |

### BuildManager Limitation

BuildManager is the hardest singleton to test because `ClearAllRooms()` and `RestorePlacedRoom()` both require a non-null `_ringVisual` reference (the RingVisual scene node). In the test runner scene, `_ringVisual` will be null because the Ring scene is not loaded.

**Workaround options:**

1. **Test without BuildManager room placement:** For tests that need room data (housing assignment, economy costs), construct SaveData with pre-configured rooms and test the serialization/deserialization path rather than the placement path. This covers the critical path (save/load) without requiring a full game scene.

2. **Load the game scene in Tier 3 tests:** For full integration tests that need room placement to work, use GodotTestDriver's Fixture to load QuickTestScene.tscn. This provides RingVisual, enabling BuildManager to function. These tests are slower but test the real code paths.

3. **Add a test-only room registration method to BuildManager:** This is the cleanest solution but requires modifying production code for testability. Not recommended for the initial testing milestone.

**Recommendation:** Use approach 1 (SaveData-based testing) for most tests, and approach 2 (full scene loading) only for end-to-end save/load round-trip tests.

---

## Data Flow: Test Execution

### Test Discovery and Execution

```
Godot --run-tests --quit-on-finish
    |
    v
Engine initializes, loads autoloads from project.godot
    |
    v
TitleScreen._Ready() detects --run-tests, calls ChangeSceneToFile("res://test/TestRunner.tscn")
    |
    v
TestRunner._Ready() calls GoTest.RunTests(Assembly.GetExecutingAssembly(), this)
    |
    v
GoDotTest TestProvider scans assembly for classes extending TestClass
    |
    v
TestExecutor runs each suite:
    |
    +-- MoodSystemTest (Tier 1)
    |     [SetupAll] -- nothing needed
    |     [Test] WishFulfilledIncreasesMood -- pure POCO test
    |     [Test] DecayConvergesToBaseline -- pure POCO test
    |     [Test] HysteresisPreventsOscillation -- pure POCO test
    |     [CleanupAll] -- nothing needed
    |
    +-- EconomyFormulaTest (Tier 1)
    |     [Test] BaseIncomeSqrtScaling -- pure formula test
    |     [Test] RoomCostCategoryMultipliers -- pure formula test
    |     [Test] DemolishRefundRatio -- pure formula test
    |
    +-- SegmentGridTest (Tier 1)
    |     [Test] ToIndexFromIndexRoundTrip -- pure POCO test
    |     [Test] WrapPositionHandlesNegative -- pure POCO test
    |     [Test] AreSegmentsFreeChecksRange -- pure POCO test
    |
    +-- SaveDataSerializationTest (Tier 1)
    |     [Test] V3RoundTrip -- serialize/deserialize
    |     [Test] V2BackwardCompat -- missing fields default correctly
    |     [Test] V1BackwardCompat -- old format loads
    |     [Test] NullableHomeSegmentPreserved -- null vs 0 distinction
    |
    +-- EconomyManagerTest (Tier 2)
    |     [SetupAll] -- TestHelper.ResetAllSingletons()
    |     [Setup] -- reset credits to known value
    |     [Test] TrySpendInsufficientFundsFails
    |     [Test] TrySpendDeductsCorrectly
    |     [Test] EarnAddsToBalance
    |     [Test] RefundAddsAndEmitsEvent
    |     [Test] MoodTierMultiplierAffectsIncome
    |
    +-- HousingCapacityTest (Tier 1)
    |     [Test] SingleSegmentBaseCapacity
    |     [Test] MultiSegmentScalesCorrectly
    |
    +-- MoodTierTransitionTest (Tier 2)
    |     [SetupAll] -- TestHelper.ResetAllSingletons()
    |     [Test] RestoreStateSetsTierCorrectly
    |     [Test] WishFulfilledAdvancesTier
    |
    +-- SaveLoadRoundTripTest (Tier 3)
    |     [SetupAll] -- TestHelper.ResetAllSingletons()
    |     [Test] V3SaveDataPreservesAllFields
    |     [Test] V2SaveLoadsWithNullHousing
    |     [Test] V1SaveLoadsWithDefaultMood
    |
    v
All suites complete -- GoDotTest exits with success/failure code
```

### Key Data Flows for Tested Systems

**1. Save/Load Round-Trip (Critical Path)**

```
Test constructs SaveData via SaveDataBuilder
    |
    v  System.Text.Json.JsonSerializer.Serialize(saveData)
    |
    v  string json
    |
    v  System.Text.Json.JsonSerializer.Deserialize<SaveData>(json)
    |
    v  Verify all fields match original:
       - Version (3)
       - Credits, LifetimeHappiness, Mood, MoodBaseline
       - UnlockedRooms (list equality)
       - PlacedRooms (count, each room's fields)
       - Citizens (count, each citizen's fields including nullable HomeSegmentIndex)
       - ActiveWishes (dictionary equality)
       - PlacedRoomTypes (dictionary equality)
```

**2. Mood Tier Transitions (Core Game Logic)**

```
Test constructs HappinessConfig with known thresholds
    |
    v  new MoodSystem(config)
    |
    v  mood.Update(delta, lifetimeHappiness) -- simulate frames
    |   or
    v  mood.OnWishFulfilled() -- simulate wish events
    |
    v  Verify:
       - mood.CurrentTier matches expected tier
       - mood.Mood within expected range
       - Hysteresis: tier does not demote until below (threshold - width)
       - Baseline: mood decays toward correct floor
```

**3. Economy Formulas (Deterministic)**

```
Test constructs EconomyConfig with known values
    |
    v  Set singleton state: citizenCount, workingCount, tierMultiplier
    |
    v  EconomyManager.Instance.CalculateTickIncome()
    |
    v  Verify:
       - Base + sqrt(citizens) * rate + workers * bonus) * tierMult
       - Rounding matches RoundToInt
```

**4. Housing Capacity (Static Pure Function)**

```
Test calls HousingManager.ComputeCapacity(definition, segmentCount)
    |
    v  Verify:
       - 1 segment: BaseCapacity
       - 2 segments: BaseCapacity + 1
       - 3 segments: BaseCapacity + 2
```

---

## Anti-Patterns

### Anti-Pattern 1: Testing Through the Full Game Scene

**What people do:** Load QuickTestScene.tscn for every test to get BuildManager working with RingVisual.
**Why it's wrong:** Slow (scene loading per test suite), fragile (depends on full game scene stability), tests too much at once (a UI change breaks economy tests).
**Do this instead:** Use SaveData-based testing for most tests. Only load the game scene for explicit scene integration tests. Test pure logic and singleton state directly.

### Anti-Pattern 2: Mocking Singletons

**What people do:** Create mock implementations of EconomyManager, BuildManager, etc. to isolate tests.
**Why it's wrong:** The existing singletons use static `Instance` properties, not interfaces. Replacing them requires either reflection hacks, interface extraction (architectural refactor), or DI framework integration. This is massive scope for a v1.3 testing milestone.
**Do this instead:** Use the singletons as they are. Reset their state between test suites via existing public APIs. The singletons ARE the system under test. Test pure functions directly where available (MoodSystem, SegmentGrid, economy formulas).

### Anti-Pattern 3: Testing Implementation Details via Private Field Access

**What people do:** Use reflection to read or set private fields on singletons for test assertions.
**Why it's wrong:** Tests become coupled to implementation details. Refactoring internal fields breaks tests even when behavior is unchanged.
**Do this instead:** Test through public APIs. Verify EconomyManager income by calling `CalculateTickIncome()`. Verify HousingManager assignment by calling `GetHomeForCitizen()`. Verify mood by checking `HappinessManager.Instance.Mood` and `CurrentTier`.

### Anti-Pattern 4: Test Ordering Dependencies

**What people do:** Assume tests run in a specific order across test classes, relying on state left by a previous test suite.
**Why it's wrong:** GoDotTest runs test methods within a class in declaration order, but the order of test classes is determined by reflection and may change. Cross-suite state leakage makes failures non-reproducible.
**Do this instead:** Each test suite calls `TestHelper.ResetAllSingletons()` in [SetupAll]. Each test suite is independently runnable via `--run-tests=SuiteName`.

### Anti-Pattern 5: Mixing Tier 1 and Tier 2 Tests

**What people do:** Put a MoodSystem POCO test and an EconomyManager singleton test in the same file because they are both "happiness-related."
**Why it's wrong:** The POCO test needs zero setup and runs instantly. The singleton test needs a full singleton reset. Mixing tiers means the fast tests carry the overhead of the slow tests' setup.
**Do this instead:** Separate by tier. `test/Unit/MoodSystemTest.cs` for pure POCO tests. `test/Integration/MoodTierTransitionTest.cs` for singleton-dependent tests.

---

## New vs Modified Components

### New Files

| File | Type | Purpose | Estimated LOC |
|------|------|---------|---------------|
| `test/TestRunner.cs` | Script | GoDotTest entry point, calls GoTest.RunTests | ~10 |
| `test/TestRunner.tscn` | Scene | Minimal Node2D scene for test execution root | ~5 lines .tscn |
| `test/Helpers/TestHelper.cs` | Helper | Singleton reset utilities | ~40 |
| `test/Helpers/SaveDataBuilder.cs` | Helper | Fluent builder for SaveData test fixtures | ~80 |
| `test/Helpers/Assert.cs` | Helper | Lightweight assertion library | ~50 |
| `test/Unit/MoodSystemTest.cs` | Test | MoodSystem POCO: decay, gain, hysteresis, tier calc | ~120 |
| `test/Unit/SegmentGridTest.cs` | Test | SegmentGrid: index mapping, wrap, adjacency, occupancy | ~80 |
| `test/Unit/SaveDataSerializationTest.cs` | Test | SaveData JSON round-trip for v1/v2/v3 formats | ~100 |
| `test/Unit/EconomyFormulaTest.cs` | Test | Economy pure functions: income, cost, refund | ~80 |
| `test/Unit/HousingCapacityTest.cs` | Test | ComputeCapacity static function | ~30 |
| `test/Integration/EconomyManagerTest.cs` | Test | EconomyManager singleton: spend, earn, income with state | ~80 |
| `test/Integration/HousingManagerTest.cs` | Test | HousingManager assignment logic with pre-set rooms | ~100 |
| `test/Integration/HappinessManagerTest.cs` | Test | HappinessManager tier transitions and arrival gating | ~80 |
| `test/System/SaveLoadRoundTripTest.cs` | Test | Full save/load with version compatibility | ~120 |

### Modified Files

| File | Change Scope | What Changes |
|------|-------------|-------------|
| `Orbital Rings.csproj` | Small | Add PackageReference for Chickensoft.GoDotTest and Chickensoft.GodotTestDriver. Add conditional exclude for test code in ExportRelease builds. |
| `Scripts/UI/TitleScreen.cs` | Small | Add `#if DEBUG` guard at top of _Ready() to detect `--run-tests` and redirect to test scene (~10 LOC). |

### Untouched Production Files

All 40+ existing production C# files remain untouched. The testing infrastructure is purely additive. No production code needs modification for testability beyond the TitleScreen entry point guard.

---

## Build Order Considering Dependencies

### Phase 1: Framework Wiring

**Files:** `Orbital Rings.csproj` (package refs), `test/TestRunner.cs`, `test/TestRunner.tscn`, `test/Helpers/Assert.cs`, `Scripts/UI/TitleScreen.cs` (entry guard)
**Delivers:** Running `godot --run-tests --quit-on-finish` produces "0 tests found" output and exits cleanly.
**Dependencies:** None.
**Rationale:** Prove the framework works before writing any tests. The #1 pitfall with GoDotTest is misconfigured entry points or missing package versions.

### Phase 2: Test Helpers

**Files:** `test/Helpers/TestHelper.cs`, `test/Helpers/SaveDataBuilder.cs`
**Delivers:** Shared utilities available for all subsequent test phases.
**Dependencies:** Phase 1 (project compiles with GoDotTest).
**Rationale:** Helpers are used by every test suite. Building them first avoids duplication.

### Phase 3: Tier 1 Pure C# Tests

**Files:** `test/Unit/MoodSystemTest.cs`, `test/Unit/SegmentGridTest.cs`, `test/Unit/EconomyFormulaTest.cs`, `test/Unit/HousingCapacityTest.cs`
**Delivers:** ~15 passing tests covering core game logic with zero engine dependency risk.
**Dependencies:** Phase 2 (Assert helper).
**Rationale:** Highest value-to-effort ratio. These tests are fast, reliable, and cover the most critical game math. MoodSystem tests verify decay/hysteresis/tier logic that affects every play session. Economy formula tests catch rounding bugs that would silently alter game balance.

### Phase 4: Save/Load Tests

**Files:** `test/Unit/SaveDataSerializationTest.cs`, `test/System/SaveLoadRoundTripTest.cs`
**Delivers:** Confidence that v1, v2, and v3 save formats load correctly. Null vs 0 HomeSegmentIndex distinction verified. Backward compatibility regression-proof.
**Dependencies:** Phase 2 (SaveDataBuilder).
**Rationale:** Save/load is the highest-severity failure mode. A serialization bug silently corrupts player progress. These tests are the project's most valuable safety net for future changes.

### Phase 5: Tier 2 Integration Tests

**Files:** `test/Integration/EconomyManagerTest.cs`, `test/Integration/HousingManagerTest.cs`, `test/Integration/HappinessManagerTest.cs`
**Delivers:** Singleton behavior verified under controlled state. Economy spend/earn/income, housing assignment/capacity, mood tier transitions.
**Dependencies:** Phase 2 (TestHelper), Phase 1 (autoloads must initialize).
**Rationale:** These tests exercise the singleton coordination that is hardest to verify manually. The `[Setup]` reset pattern ensures each test starts clean.

### Phase Ordering Rationale

```
Phase 1 (Framework Wiring) --> no deps, proves infrastructure works
    |
    v
Phase 2 (Test Helpers) --> depends on Phase 1
    |
    +---> Phase 3 (Pure C# Tests) --> depends on Phase 2
    |
    +---> Phase 4 (Save/Load Tests) --> depends on Phase 2
    |
    +---> Phase 5 (Integration Tests) --> depends on Phase 2
```

Phases 3, 4, and 5 are independent of each other after Phase 2. The recommended sequence (3 then 4 then 5) front-loads the easiest and most valuable tests, then adds save/load safety, then tackles the more complex singleton integration tests.

---

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| 10-20 tests (v1.3) | Current architecture is sufficient. Single test runner, sequential execution, manual singleton reset. Test run completes in under 5 seconds. |
| 50-100 tests (v1.4+) | Consider splitting the `--run-tests` entry to support `--run-tests=Unit` and `--run-tests=Integration` for faster feedback during development. GoDotTest already supports `--run-tests=SuiteName`. |
| 200+ tests (v2+) | If test execution time exceeds 30 seconds, consider a second test runner scene that skips autoload-heavy tests for rapid TDD feedback. Or adopt GdUnit4Net for out-of-process pure C# test execution. |

### First Bottleneck

The first bottleneck will be **autoload initialization time** -- all 8 singletons initialize even for pure C# tests that do not need them. This is inherent to GoDotTest running inside Godot. For a project of this size (~50-100 tests), this adds ~1-2 seconds of startup overhead and is not worth optimizing in v1.3.

### Second Bottleneck

The second bottleneck will be **test scene loading** for Tier 3 tests that need the game scene. Loading QuickTestScene.tscn involves mesh generation, resource loading, and autoload state population. Limit the number of tests that require scene loading. Use SaveData-based testing instead wherever possible.

---

## CI Integration

GoDotTest supports CI execution via command line:

```bash
# Run all tests, exit on completion
godot --run-tests --quit-on-finish

# Run specific suite
godot --run-tests=MoodSystemTest --quit-on-finish

# Collect code coverage with coverlet
coverlet "./.godot/mono/temp/bin/Debug" \
  --target godot \
  --targetargs "--run-tests --coverage --quit-on-finish" \
  --format "opencover" \
  --output "./coverage/coverage.xml" \
  --exclude-by-file "**/test/**/*.cs"
```

The `--coverage` flag tells GoDotTest to force-exit the process (bypassing Godot's normal shutdown) so coverlet can collect coverage data correctly.

---

## Sources

- [Chickensoft GoDotTest GitHub](https://github.com/chickensoft-games/GoDotTest) -- Test runner framework, TestClass API, test environment setup (MEDIUM confidence -- GitHub README, not yet verified in this project)
- [Chickensoft GodotTestDriver GitHub](https://github.com/chickensoft-games/GodotTestDriver) -- Fixture management, input simulation, node drivers (MEDIUM confidence -- GitHub README)
- [GoDotTest NuGet](https://www.nuget.org/packages/Chickensoft.GoDotTest/) -- Version 2.0.30, targets .NET 8+, GodotSharp >= 4.6.1 (HIGH confidence -- verified on NuGet)
- [GodotTestDriver NuGet](https://www.nuget.org/packages/Chickensoft.GodotTestDriver/) -- Version 3.1.62, targets .NET 8+, GodotSharp >= 4.6.1 (HIGH confidence -- verified on NuGet)
- Direct codebase analysis of all 40+ source files (HIGH confidence -- primary source for singleton testability matrix, reset APIs, and pure function inventory)
- `project.godot` -- Autoload initialization order, confirmed 8 singletons with specific ordering (HIGH confidence)
- `Scripts/Autoloads/*.cs` -- Singleton patterns, static Instance properties, StateLoaded guards, public reset APIs (HIGH confidence)
- `Scripts/Happiness/MoodSystem.cs` -- POCO testability proof, pure C# class with no Node inheritance (HIGH confidence)
- `Scripts/Ring/SegmentGrid.cs` -- POCO with pure static methods, ideal for unit testing (HIGH confidence)
- `Scripts/Autoloads/SaveManager.cs` -- SaveData/SavedRoom/SavedCitizen POCOs, System.Text.Json serialization, version-gated restore (HIGH confidence)

---
*Architecture research for: Orbital Rings v1.3 -- Testing Infrastructure*
*Researched: 2026-03-07*
