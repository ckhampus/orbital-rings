# Phase 21: Integration Test Infrastructure - Research

**Researched:** 2026-03-07
**Domain:** C# singleton state isolation for Godot game testing (GoDotTest framework)
**Confidence:** HIGH

## Summary

Phase 21 delivers the infrastructure that makes integration tests (Phase 25) reliable: Reset() methods on all 8 autoload singletons, ClearAllSubscribers() on GameEvents, timer suppression, and a test base class that orchestrates cleanup before each test. The codebase is well-structured for this -- all singletons follow consistent patterns (static Instance, mutable private dictionaries/lists, child Timer nodes), and GameEvents uses pure C# event delegates that become null when all subscribers are removed.

The core technical challenge is cataloging every mutable field in each singleton and ensuring Reset() returns it to the "just loaded, no game data" state. This research documents every field that needs clearing, every timer that needs stopping, and the exact GoDotTest lifecycle hooks to use. The verification tests prove the infrastructure works by dirtying state, resetting, and asserting clean state.

**Primary recommendation:** Add a public `Reset()` method to each singleton's production .cs file that clears all mutable state and stops owned timers. Create `TestHelper.ResetAllSingletons()` and `GameTestClass` in `Tests/Infrastructure/`. Write one verification test per singleton plus one for GameEvents.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Each of the 8 autoload singletons gets a public `Reset()` method in production code
- `Reset()` clears mutable state (dictionaries, lists, counters, flags) to initial defaults
- `Reset()` stops owned timers (EconomyManager._incomeTimer, HappinessManager._arrivalTimer, SaveManager._debounceTimer)
- `Reset()` resets static `StateLoaded` flags to false (EconomyManager, HousingManager, CitizenManager)
- `Reset()` does NOT touch the `Instance` reference or `[Export] Config` resources (scene tree owns those)
- Each singleton returns to a "just loaded, no game data" state after Reset()
- `GameEvents` gets a public `ClearAllSubscribers()` method in production code that nulls all C# event delegates
- `TestHelper` static class lives in `Tests/Infrastructure/`
- `TestHelper.ResetAllSingletons()` calls each singleton's `Reset()` plus `GameEvents.Instance.ClearAllSubscribers()`
- `GameTestClass` extends GoDotTest's `TestClass`, lives in `Tests/Infrastructure/`
- `GameTestClass` auto-calls `TestHelper.ResetAllSingletons()` in setup before each test
- Integration tests (Phase 25) extend `GameTestClass`; pure POCO tests (Phases 22-24) can extend `TestClass` directly
- No convenience accessors for singletons -- tests use `EconomyManager.Instance` etc. directly
- Timers stopped inside each singleton's `Reset()` -- no scene tree pausing or ProcessMode manipulation
- CitizenNode per-instance timers are out of scope (singleton timers only per INTG-03)
- One verification test per singleton: dirties, resets, verifies clean
- One verification test for GameEvents: subscribes, clears, verifies zero subscribers

### Claude's Discretion
- Exact fields and collections cleared in each singleton's Reset()
- Order of operations in ResetAllSingletons()
- GoDotTest setup/teardown lifecycle hooks used by GameTestClass
- How to verify "zero subscribers" on GameEvents (reflection vs. explicit null checks)
- Namespace for test infrastructure classes

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| INTG-01 | Singleton reset infrastructure clears state between test suites | Complete mutable field catalog for all 8 singletons; Reset() method patterns; TestHelper.ResetAllSingletons() orchestration; verification test pattern |
| INTG-02 | GameEvents subscribers cleared between test suites (no stale delegates) | All 32 event delegates cataloged; ClearAllSubscribers() nulling pattern; verification via subscribe-clear-emit-assert pattern |
| INTG-03 | Singleton timers suppressed during test execution | Three Timer nodes identified (EconomyManager._incomeTimer, HappinessManager._arrivalTimer, SaveManager._debounceTimer); Timer.Stop() in each Reset(); no timer-related side effects during tests |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Chickensoft.GoDotTest | 2.0.30 | Test runner and TestClass base | Already installed (Phase 20); provides [Setup]/[Cleanup] lifecycle hooks |
| Shouldly | 4.3.0 | Assertion library | Already installed (Phase 20); readable assertion messages |
| Godot.NET.Sdk | 4.6.1 | Engine SDK | Project target; .NET 10 |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Reflection | (runtime) | Verify event delegate null state | Only in GameEvents verification test if reflection approach chosen |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Reflection for zero-subscriber check | Subscribe + clear + emit + assert-no-handler-ran | Reflection is more thorough (checks all 32 fields); emit approach is simpler but requires wiring test handlers to each event |

**Installation:** No new packages needed. All dependencies installed in Phase 20.

## Architecture Patterns

### Recommended Project Structure
```
Tests/
  Infrastructure/
    TestHelper.cs          # Static class with ResetAllSingletons()
    GameTestClass.cs       # Base class extending TestClass with [Setup] auto-reset
  Housing/
    HousingTests.cs        # Existing smoke test (Phase 20)
  Integration/
    SingletonResetTests.cs # Verification tests for Reset() methods
    GameEventsTests.cs     # Verification test for ClearAllSubscribers()
```

### Pattern 1: Singleton Reset() Method
**What:** Each singleton gets a public `Reset()` method that clears all mutable state and stops timers, returning it to "just loaded, no game data" state.
**When to use:** Called by TestHelper.ResetAllSingletons() between tests.
**Example:**
```csharp
// In EconomyManager.cs
public void Reset()
{
    _credits = 0;               // Not Config.StartingCredits -- "no game data" state
    _citizenCount = 0;
    _workingCitizenCount = 0;
    _workingCitizens.Clear();
    _currentTierMultiplier = 1.0f;
    _incomeTimer?.Stop();
    StateLoaded = false;
}
```

### Pattern 2: GameEvents ClearAllSubscribers()
**What:** Nulls all 32 C# event delegate fields. Since C# events are null when no subscribers exist, setting to null is equivalent to removing all subscribers.
**When to use:** Called by TestHelper.ResetAllSingletons() to prevent stale delegate leaks.
**Example:**
```csharp
// In GameEvents.cs
public void ClearAllSubscribers()
{
    CameraOrbitStarted = null;
    CameraOrbitStopped = null;
    SegmentHovered = null;
    // ... all 32 events
    CitizenExitedHome = null;
}
```

### Pattern 3: TestHelper Static Orchestrator
**What:** Static class that calls Reset() on all singletons in dependency order plus ClearAllSubscribers().
**When to use:** Called in GameTestClass [Setup] method before each test.
**Example:**
```csharp
// In Tests/Infrastructure/TestHelper.cs
namespace OrbitalRings.Tests.Infrastructure;

public static class TestHelper
{
    public static void ResetAllSingletons()
    {
        // Clear events FIRST to prevent Reset() side effects from triggering stale handlers
        GameEvents.Instance?.ClearAllSubscribers();

        // Reset singletons (order: leaf dependencies first, then dependents)
        EconomyManager.Instance?.Reset();
        BuildManager.Instance?.Reset();
        CitizenManager.Instance?.Reset();
        WishBoard.Instance?.Reset();
        HappinessManager.Instance?.Reset();
        HousingManager.Instance?.Reset();
        SaveManager.Instance?.Reset();
    }
}
```

### Pattern 4: GameTestClass Base Class
**What:** Extends GoDotTest TestClass, auto-resets all singletons before each test via [Setup].
**When to use:** Integration tests (Phase 25) extend this. Unit tests can use plain TestClass.
**Example:**
```csharp
// In Tests/Infrastructure/GameTestClass.cs
namespace OrbitalRings.Tests.Infrastructure;

using Chickensoft.GoDotTest;
using Godot;

public class GameTestClass : TestClass
{
    public GameTestClass(Node testScene) : base(testScene) { }

    [Setup]
    public void ResetGameState()
    {
        TestHelper.ResetAllSingletons();
    }
}
```

### Anti-Patterns to Avoid
- **Resetting Instance references:** Reset() must NOT null out `Instance` or reassign `Config` -- the scene tree owns those. Tests run within the Godot process where singletons are already initialized.
- **Calling _Ready() for reset:** Never call `_Ready()` from Reset(). `_Ready()` creates child Timer nodes, subscribes to events, etc. Calling it again would create duplicate timers and double-subscribe.
- **Resetting in [SetupAll] instead of [Setup]:** [SetupAll] runs once per test class, not per test. If Test A dirties state, Test B would see it. Always reset in [Setup] (per-test).
- **Stopping timers by removing them from scene tree:** Just call `Timer.Stop()`. The Timer node remains as a child -- it just stops ticking. Removing and re-adding would break the Timeout signal connection.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Test lifecycle hooks | Custom test runner with setup/teardown | GoDotTest [Setup]/[Cleanup] attributes | Already works, battle-tested, correct ordering |
| Event subscriber counting | Manual counter tracking subscriptions | C# reflection on event backing fields, or subscribe-clear-emit pattern | Events are standard C# delegates; null == zero subscribers |
| Timer suppression framework | ProcessMode manipulation, custom timer wrapper | Timer.Stop() in each Reset() | Simple, direct, no side effects on other nodes |

**Key insight:** The problem domain is straightforward state management. The complexity is in cataloging fields correctly, not in clever architecture.

## Common Pitfalls

### Pitfall 1: Missing Mutable Fields in Reset()
**What goes wrong:** A field is missed in Reset(), so state leaks between tests. Test B sees Test A's data and passes/fails incorrectly.
**Why it happens:** Singletons accumulate fields across phases. Easy to miss one.
**How to avoid:** Systematic audit of every private field in each singleton. This research catalogs them all (see Mutable Field Catalog below).
**Warning signs:** Tests pass individually but fail when run together; flaky test results.

### Pitfall 2: Reset() Triggers Side Effects via GameEvents
**What goes wrong:** A singleton's Reset() method indirectly triggers event handlers on other singletons (e.g., clearing credits emits CreditsChanged, which triggers SaveManager autosave).
**Why it happens:** Singletons subscribe to GameEvents in _Ready() and those subscriptions persist.
**How to avoid:** Call `GameEvents.Instance.ClearAllSubscribers()` FIRST, before any singleton Reset(). This ensures no event handlers fire during reset.
**Warning signs:** Autosave timer fires during test setup; unexpected state changes during reset.

### Pitfall 3: Static StateLoaded Flags Not Reset
**What goes wrong:** `StateLoaded = true` persists from a test that simulates save/load. Next test's singleton _Ready() (if called) skips initialization.
**Why it happens:** StateLoaded is a static field on the class, not an instance field. Reset() must explicitly set it to false.
**How to avoid:** Include `StateLoaded = false` in Reset() for EconomyManager, HousingManager, and CitizenManager.
**Warning signs:** Tests that depend on fresh _Ready() initialization get stale state.

### Pitfall 4: HousingManager Delegate References Leak
**What goes wrong:** HousingManager stores `_onRoomPlaced`, `_onRoomDemolished`, `_onCitizenArrived` as instance fields for clean unsubscription. If ClearAllSubscribers() nulls the event but these delegate references remain, they become stale but harmless. However, if Reset() doesn't clear them, _ExitTree() will try to unsubscribe stale delegates.
**Why it happens:** HousingManager caches delegates for -= symmetry.
**How to avoid:** Reset() should null out these delegate fields. ClearAllSubscribers() handles the event side; Reset() handles the singleton side.
**Warning signs:** NullReferenceException in _ExitTree() during test teardown.

### Pitfall 5: WishBoard Extends SafeNode, Not Node
**What goes wrong:** WishBoard uses SafeNode's _EnterTree/_ExitTree for event subscription/unsubscription. ClearAllSubscribers() removes the event handlers, but WishBoard's cached state thinks it's still subscribed.
**Why it happens:** SafeNode auto-subscribes in _EnterTree. After ClearAllSubscribers(), the SafeNode is in an inconsistent state.
**How to avoid:** This is actually fine for testing -- ClearAllSubscribers() removes handlers, and WishBoard's Reset() clears its data. The WishBoard won't re-subscribe until _ExitTree/_EnterTree cycle, which doesn't happen during tests. Just ensure Reset() clears all WishBoard dictionaries.
**Warning signs:** None expected -- ClearAllSubscribers is one-directional (removes handlers, doesn't corrupt the singleton).

### Pitfall 6: BuildManager._placedRooms Contains Record with MeshInstance3D
**What goes wrong:** BuildManager tracks PlacedRoom records that contain `MeshInstance3D Mesh` references. Reset() must handle these scene tree nodes carefully.
**Why it happens:** PlacedRoom is a record with a Mesh field that references a scene tree node.
**How to avoid:** Reset() should NOT QueueFree() meshes (tests may not have placed any). Just clear the dictionary. In test context, there won't be real meshes.
**Warning signs:** ObjectDisposedException if Reset() tries to free nonexistent meshes.

## Code Examples

### Complete Mutable Field Catalog

Every mutable field that each singleton's Reset() must clear:

#### EconomyManager
```csharp
public void Reset()
{
    _credits = 0;
    _citizenCount = 0;
    _workingCitizenCount = 0;
    _workingCitizens.Clear();
    _currentTierMultiplier = 1.0f;
    _incomeTimer?.Stop();
    StateLoaded = false;
}
```
Fields: `_credits` (int), `_citizenCount` (int), `_workingCitizenCount` (int), `_workingCitizens` (HashSet<string>), `_currentTierMultiplier` (float), `_incomeTimer` (Timer -- stop, don't remove).
Static: `StateLoaded` (bool).
Preserve: `Instance`, `Config`, `_incomeTimer` (node reference).

#### HousingManager
```csharp
public void Reset()
{
    _housingRoomCapacities.Clear();
    _roomOccupants.Clear();
    _citizenHomes.Clear();
    _isRestoring = false;
    _onRoomPlaced = null;
    _onRoomDemolished = null;
    _onCitizenArrived = null;
    StateLoaded = false;
}
```
Fields: `_housingRoomCapacities` (Dictionary<int,int>), `_roomOccupants` (Dictionary<int,List<string>>), `_citizenHomes` (Dictionary<string,int>), `_isRestoring` (bool), `_onRoomPlaced`/`_onRoomDemolished`/`_onCitizenArrived` (delegate refs).
Static: `StateLoaded` (bool).
Preserve: `Instance`, `Config`.

#### HappinessManager
```csharp
public void Reset()
{
    _lifetimeHappiness = 0;
    _moodSystem = new MoodSystem(Config ?? new HappinessConfig());
    _lastReportedTier = MoodTier.Quiet;
    _unlockedRooms.Clear();
    _unlockedRooms.UnionWith(new[] {
        "bunk_pod", "air_recycler", "workshop",
        "reading_nook", "storage_bay", "garden_nook"
    });
    _crossedMilestoneCount = 0;
    _arrivalTimer?.Stop();
    // Note: _arrivalCanvasLayer left as-is (UI node, no game state)
}
```
Fields: `_lifetimeHappiness` (int), `_moodSystem` (MoodSystem -- recreate fresh), `_lastReportedTier` (MoodTier), `_unlockedRooms` (HashSet<string> -- reset to starter rooms), `_crossedMilestoneCount` (int), `_arrivalTimer` (Timer -- stop).
Preserve: `Instance`, `Config`, `_arrivalCanvasLayer`.

#### SaveManager
```csharp
public void Reset()
{
    PendingLoad = null;
    _saving = false;
    _pendingLoadFrames = -1;
    _debounceTimer?.Stop();
    // Null out delegate fields to prevent stale handler references
    _onRoomPlaced = null;
    _onRoomDemolished = null;
    _onWishFulfilled = null;
    _onCitizenArrived = null;
    _onMoodTierChanged = null;
    _onWishCountChanged = null;
    _onCreditsChanged = null;
    _onBlueprintUnlocked = null;
    _onCitizenAssignedHome = null;
    _onCitizenUnhoused = null;
}
```
Fields: `PendingLoad` (SaveData), `_saving` (bool), `_pendingLoadFrames` (int), `_debounceTimer` (Timer -- stop), 10 delegate reference fields.
Preserve: `Instance`, `_debounceTimer` (node reference).

#### BuildManager
```csharp
public void Reset()
{
    _mode = BuildMode.Normal;
    _selectedRoom = null;
    _anchorFlatIndex = -1;
    _currentSize = 1;
    _startPos = 0;
    _pendingDemolishIndex = -1;
    _placedRooms.Clear();
    // _ghostMesh: only exists during active placement; should be null between operations
    if (_ghostMesh != null && Node.IsInstanceValid(_ghostMesh))
    {
        _ghostMesh.QueueFree();
        _ghostMesh = null;
    }
    // _roomDefinitions: loaded once in _Ready(), treat as read-only cache -- don't clear
    // _ringVisual: scene tree reference -- don't touch
}
```
Fields: `_mode` (BuildMode), `_selectedRoom` (RoomDefinition), `_anchorFlatIndex` (int), `_currentSize` (int), `_startPos` (int), `_anchorRow` (SegmentRow), `_pendingDemolishIndex` (int), `_placedRooms` (Dictionary<int,PlacedRoom>), `_ghostMesh` (MeshInstance3D -- free if valid).
Preserve: `Instance`, `_ringVisual`, `_roomDefinitions` (read-only cache).

#### CitizenManager
```csharp
public void Reset()
{
    // Don't QueueFree citizen nodes in test context -- they may not exist
    _citizens.Clear();
    _selectedCitizen = null;
    _grid = null;  // Will be re-discovered in _Process if needed
    StateLoaded = false;
    // _camera, _ringPlane, _infoPanel: UI/scene references -- don't touch
}
```
Fields: `_citizens` (List<CitizenNode>), `_selectedCitizen` (CitizenNode), `_grid` (SegmentGrid).
Static: `StateLoaded` (bool).
Preserve: `Instance`, `_camera`, `_ringPlane`, `_infoPanel`.

#### WishBoard
```csharp
public void Reset()
{
    _activeWishes.Clear();
    _placedRoomTypes.Clear();
    _segmentRoomIds.Clear();
    // _allTemplates and _templatesByCategory: loaded once in _Ready(), read-only -- don't clear
    // WishNudgeRequested event: cleared by GameEvents.ClearAllSubscribers() pattern
    //   but WishNudgeRequested is on WishBoard, not GameEvents -- null it here
    // Note: WishNudgeRequested is a WishBoard-owned event, not a GameEvents event
}
```
Fields: `_activeWishes` (Dictionary<string,WishTemplate>), `_placedRoomTypes` (Dictionary<string,int>), `_segmentRoomIds` (Dictionary<int,string>).
Also: `WishNudgeRequested` event delegate on WishBoard itself -- null it in Reset().
Preserve: `Instance`, `_allTemplates`, `_templatesByCategory`.

#### GameEvents
```csharp
public void ClearAllSubscribers()
{
    // Camera Events
    CameraOrbitStarted = null;
    CameraOrbitStopped = null;

    // Segment Events
    SegmentHovered = null;
    SegmentUnhovered = null;
    SegmentSelected = null;
    SegmentDeselected = null;

    // Room Events
    RoomPlaced = null;
    RoomDemolished = null;

    // Build Mode Events
    BuildModeChanged = null;
    PlacementPreviewUpdated = null;
    PlacementPreviewCleared = null;

    // Placement Feedback Events
    RoomPlacementConfirmed = null;
    RoomDemolishConfirmed = null;
    PlacementInvalid = null;

    // Demolish Hover Events
    DemolishHoverUpdated = null;
    DemolishHoverCleared = null;

    // Citizen Events
    CitizenArrived = null;
    CitizenClicked = null;
    CitizenEnteredRoom = null;
    CitizenExitedRoom = null;

    // Wish Events
    WishGenerated = null;
    WishFulfilled = null;

    // Economy Events
    CreditsChanged = null;
    IncomeTicked = null;
    CreditsSpent = null;
    CreditsRefunded = null;

    // Progression Events
    BlueprintUnlocked = null;

    // Happiness v2 Events
    MoodTierChanged = null;
    WishCountChanged = null;

    // Housing Events
    CitizenAssignedHome = null;
    CitizenUnhoused = null;
    HousingStateChanged = null;

    // Home Visit Events
    CitizenEnteredHome = null;
    CitizenExitedHome = null;
}
```
Total: 32 event delegates to null.

### GameEvents Does Not Need Reset() -- Only ClearAllSubscribers()
GameEvents has no mutable state fields beyond the event delegates themselves. The `Instance` reference is set in `_EnterTree()` and should persist. ClearAllSubscribers() is the complete reset.

### Recommended ResetAllSingletons() Order

```csharp
public static void ResetAllSingletons()
{
    // 1. Clear ALL event subscribers first.
    //    This prevents Reset() operations from triggering stale handlers
    //    (e.g., EconomyManager.Reset() clearing credits would emit CreditsChanged
    //    to SaveManager, which would trigger autosave).
    GameEvents.Instance?.ClearAllSubscribers();

    // 2. Reset each singleton. Order doesn't matter after events are cleared,
    //    since no cross-singleton communication can happen.
    EconomyManager.Instance?.Reset();
    BuildManager.Instance?.Reset();
    CitizenManager.Instance?.Reset();
    WishBoard.Instance?.Reset();
    HappinessManager.Instance?.Reset();
    HousingManager.Instance?.Reset();
    SaveManager.Instance?.Reset();
}
```

### Verification Test Pattern: Singleton Reset
```csharp
[Test]
public void EconomyManagerResetClearsAllState()
{
    // Arrange: dirty the singleton
    var economy = EconomyManager.Instance;
    economy.Earn(500);               // sets _credits > 0
    economy.SetCitizenCount(10);     // sets _citizenCount
    economy.SetMoodTier(MoodTier.Radiant); // sets _currentTierMultiplier

    // Act
    economy.Reset();

    // Assert: all fields back to initial state
    economy.Credits.ShouldBe(0);
    economy.CalculateTickIncome().ShouldBeGreaterThan(0); // BaseStationIncome only, no citizen income
    // Verify timer is stopped (timer exists but not ticking)
    // StateLoaded is false
    EconomyManager.StateLoaded.ShouldBeFalse();
}
```

### Verification Test Pattern: GameEvents ClearAllSubscribers
```csharp
[Test]
public void ClearAllSubscribersRemovesAllHandlers()
{
    var events = GameEvents.Instance;
    bool handlerCalled = false;

    // Subscribe to several events
    events.CreditsChanged += _ => handlerCalled = true;
    events.RoomPlaced += (_, _) => handlerCalled = true;
    events.CitizenArrived += _ => handlerCalled = true;

    // Act
    events.ClearAllSubscribers();

    // Assert: emitting events does not call handlers
    events.EmitCreditsChanged(100);
    events.EmitRoomPlaced("test", 0);
    events.EmitCitizenArrived("test");

    handlerCalled.ShouldBeFalse();
}
```

### Alternative: Reflection-Based Zero-Subscriber Verification
```csharp
[Test]
public void ClearAllSubscribersNullsAllEventFields()
{
    var events = GameEvents.Instance;

    // Subscribe to an event to ensure it's non-null
    events.CreditsChanged += _ => { };

    // Act
    events.ClearAllSubscribers();

    // Assert: use reflection to verify ALL event backing fields are null
    var eventFields = typeof(GameEvents)
        .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
        .Where(f => f.FieldType.IsSubclassOf(typeof(System.Delegate))
                  || f.FieldType == typeof(System.Action)
                  || f.FieldType.IsGenericType
                     && f.FieldType.GetGenericTypeDefinition() == typeof(System.Action<>));

    foreach (var field in eventFields)
    {
        field.GetValue(events).ShouldBeNull(
            $"Event field '{field.Name}' should be null after ClearAllSubscribers()");
    }
}
```

**Recommendation:** Use the emit-and-assert approach as the primary test (simpler, tests behavior). Optionally add the reflection test as a safety net to catch any new events added in future phases that aren't included in ClearAllSubscribers().

### GoDotTest Lifecycle Hook Decision
**Use `[Setup]` (per-test), not `[SetupAll]` (per-class).**

The [Setup] attribute marks a method that runs before EACH test. This is exactly what we need -- every test starts with clean singleton state. [SetupAll] only runs once before the first test in a class, which would allow state leakage between tests within the same class.

```csharp
public class GameTestClass : TestClass
{
    public GameTestClass(Node testScene) : base(testScene) { }

    [Setup]
    public void ResetGameState()
    {
        TestHelper.ResetAllSingletons();
    }
}
```

### Namespace Decision
**Use `OrbitalRings.Tests.Infrastructure`** for TestHelper and GameTestClass. This follows the existing pattern where test classes use `OrbitalRings.Tests.{SubFolder}` (e.g., `OrbitalRings.Tests.Housing`).

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Mock singletons with interfaces | Reset real singletons in-process | Project decision | No production code interfaces needed; tests run against real code |
| Separate test .csproj | Single .csproj with conditional compilation | Phase 20 | `#if RUN_TESTS` and `<Compile Remove>` keeps tests out of release builds |
| xUnit/NUnit test runner | GoDotTest in-process runner | Phase 20 | Tests have full access to Godot APIs, scene tree, and singletons |

**Deprecated/outdated:**
- GoDotTest namespace `GoDotTest` (old): Use `Chickensoft.GoDotTest` (current, v2.0.30)

## Open Questions

1. **Should WishBoard.WishNudgeRequested be cleared by ClearAllSubscribers()?**
   - What we know: WishNudgeRequested is defined on WishBoard, not GameEvents. ClearAllSubscribers() only handles GameEvents.
   - What's unclear: Whether to add it to ClearAllSubscribers scope or handle in WishBoard.Reset().
   - Recommendation: Handle in WishBoard.Reset() since it's a WishBoard-owned event. Set `WishNudgeRequested = null` in WishBoard.Reset().

2. **Should HappinessManager.Reset() recreate MoodSystem or just reset its fields?**
   - What we know: MoodSystem is a POCO owned by HappinessManager. It has private fields (_mood, _baseline, _currentTier) with no public Reset().
   - What's unclear: Whether to add Reset() to MoodSystem or recreate the object.
   - Recommendation: Recreate with `_moodSystem = new MoodSystem(Config ?? new HappinessConfig())`. Simpler, guaranteed clean, and MoodSystem's constructor is lightweight.

3. **Timer state verification in tests**
   - What we know: Timer.Stop() stops the timer. We need to verify timers don't fire during test execution.
   - What's unclear: Whether to explicitly assert timer is stopped, or just rely on no side effects.
   - Recommendation: Verify indirectly -- after Reset(), ensure that waiting a few frames doesn't change singleton state (e.g., credits don't increase from income tick). Direct Timer.IsStopped() assertion is also valid if accessible.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Chickensoft.GoDotTest 2.0.30 |
| Config file | `Orbital Rings.csproj` (conditional PackageReference) |
| Quick run command | `godot --headless --run-tests --quit-on-finish` |
| Full suite command | `godot --headless --run-tests --quit-on-finish` |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| INTG-01 | Singleton reset clears state | unit | `godot --headless --run-tests --quit-on-finish` | Wave 0 |
| INTG-02 | GameEvents zero subscribers after clear | unit | `godot --headless --run-tests --quit-on-finish` | Wave 0 |
| INTG-03 | Singleton timers suppressed | unit | `godot --headless --run-tests --quit-on-finish` | Wave 0 |

### Sampling Rate
- **Per task commit:** `godot --headless --run-tests --quit-on-finish`
- **Per wave merge:** Same (single test suite)
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `Tests/Infrastructure/TestHelper.cs` -- core reset orchestrator
- [ ] `Tests/Infrastructure/GameTestClass.cs` -- base class with [Setup] auto-reset
- [ ] `Tests/Integration/SingletonResetTests.cs` -- verification tests for INTG-01, INTG-03
- [ ] `Tests/Integration/GameEventsTests.cs` -- verification test for INTG-02

*(These are the primary deliverables of this phase, not pre-existing gaps)*

## Sources

### Primary (HIGH confidence)
- Direct code inspection of all 8 singleton source files in `/workspace/Scripts/`
- Direct code inspection of `GameEvents.cs` -- all 32 event delegates enumerated
- Direct code inspection of `Tests/TestRunner.cs` and `Tests/Housing/HousingTests.cs` -- existing test patterns
- `Orbital Rings.csproj` -- confirmed GoDotTest 2.0.30, Shouldly 4.3.0, Godot.NET.Sdk 4.6.1
- `project.godot` -- confirmed all 8 autoload singletons and their load order

### Secondary (MEDIUM confidence)
- [GoDotTest README](https://github.com/chickensoft-games/GoDotTest/blob/main/README.md) -- [Setup], [Cleanup], [SetupAll], [CleanupAll], [Failure] lifecycle attributes; TestClass constructor signature
- [GoDotTest GitHub](https://github.com/chickensoft-games/GoDotTest) -- v2.0.30 uses Chickensoft.GoDotTest namespace

### Tertiary (LOW confidence)
- C# event delegate null behavior -- well-established language feature, verified via [Microsoft Learn Delegate.GetInvocationList](https://learn.microsoft.com/en-us/dotnet/api/system.delegate.getinvocationlist)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - all packages already installed and verified in Phase 20
- Architecture: HIGH - based on direct code inspection of all 8 singletons; every mutable field cataloged
- Pitfalls: HIGH - derived from actual code patterns (event subscription chains, static flags, delegate caching)
- Mutable field catalog: HIGH - direct source code audit of every singleton

**Research date:** 2026-03-07
**Valid until:** 2026-04-07 (stable -- no external dependency changes expected)
