# Phase 25: Singleton Integration Tests - Research

**Researched:** 2026-03-07
**Domain:** Cross-singleton integration testing in Godot/C# with GoDotTest framework
**Confidence:** HIGH

## Summary

Phase 25 writes integration tests that verify three cross-singleton event chains: housing assignment (INTG-04), demolition reassignment (INTG-05), and mood-economy propagation (INTG-06). All tests live in a single file (`Tests/Integration/SingletonIntegrationTests.cs`), extend `GameTestClass`, and follow established patterns from Phases 22-24.

The most critical technical finding is that **the existing test infrastructure breaks event chains** between singletons. `GameTestClass.[Setup]` calls `TestHelper.ResetAllSingletons()`, which calls `GameEvents.Instance.ClearAllSubscribers()`. This nulls all event delegate fields on GameEvents, severing the subscriptions that singletons established in `_Ready()` during autoload initialization. After setup, emitting `CitizenArrived` does not reach `HousingManager.OnCitizenArrived`, and emitting `WishFulfilled` does not reach `HappinessManager.OnWishFulfilled`. This means **a small production code change is required** -- contrary to the CONTEXT.md claim of "no production code changes needed" -- to re-subscribe singletons to events after reset.

The second significant finding is that `HousingManager.RestoreFromSave()` calls `InitializeExistingRooms()` which reads `BuildManager.Instance.GetPlacedRoom()`. After `BuildManager.Reset()`, the placed rooms dictionary is empty, so `InitializeExistingRooms()` finds nothing. The `_housingRoomCapacities` dictionary stays empty, making `FindBestRoom()` return -1 and all citizen assignments fail. The tests need a way to seed HousingManager's internal room data without going through BuildManager's visual scene dependency. A lightweight test-only `SeedRoomForTest(int anchorIndex, int capacity)` method on HousingManager is the cleanest approach.

**Primary recommendation:** Add `SubscribeToEvents()` methods to HousingManager and HappinessManager (plus a `TestHelper.ResubscribeAllSingletons()` orchestrator) and add `SeedRoomForTest()` to HousingManager for headless room state seeding. Then write the integration tests per CONTEXT.md scenarios.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Use HousingManager.RestoreFromSave() to pre-seed housing state, bypassing BuildManager entirely
- No minimal test scene, no test-only seeding APIs on BuildManager
- For INTG-04: Hybrid approach -- RestoreFromSave() sets up rooms with known capacities, then EmitCitizenArrived events trigger the live fewest-occupants-first algorithm
- For INTG-05: RestoreFromSave() populates state, then EmitRoomDemolished(anchorIndex) triggers the real demolition handler (HousingManager uses its cached _housingRoomCapacities, doesn't need BuildManager)
- For INTG-06: EmitWishFulfilled() repeatedly through the full chain (GameEvents -> HappinessManager.OnWishFulfilled -> MoodSystem -> EconomyManager.SetMoodTier)
- INTG-04 scenarios: even distribution across 2-3 rooms, single room gets all citizens, rooms with different capacities (1-seg vs 3-seg), all rooms full -- additional citizen remains unhoused
- INTG-05 scenarios: demolish room with 2+ citizens (verify redistribution), demolish the ONLY housing room (citizens become unhoused, CitizenUnhoused fires, GetHomeForCitizen returns null)
- INTG-06 scenarios: pump wishes until mood crosses Quiet to Cozy, verify MoodTierChanged event fires with correct old/new tiers, verify CalculateTickIncome() returns higher value after tier change
- Verify key intermediate events with inline lambda subscribers (CitizenAssignedHome, CitizenUnhoused, MoodTierChanged)
- No reusable event recorder helper -- keep it explicit and inline
- Single file: Tests/Integration/SingletonIntegrationTests.cs with comment-header sections per requirement
- Namespace: OrbitalRings.Tests.Integration
- Extends GameTestClass (singleton reset between tests)
- Behavior-focused method names, Shouldly assertions, no #region directives

### Claude's Discretion
- Exact RestoreFromSave() input data (citizen names, home segment indices, room configurations)
- Number of wishes needed to cross Quiet->Cozy threshold (depends on MoodGainPerWish=0.06 and threshold=0.10)
- Whether to set up EconomyManager citizen/worker counts for INTG-06 income comparison
- Helper method design for constructing RestoreFromSave() input tuples
- Test method ordering within each requirement section

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| INTG-04 | Housing assignment distributes citizens with fewest-occupants-first | HousingManager.FindBestRoom() algorithm documented; event chain: EmitCitizenArrived -> OnCitizenArrived -> FindBestRoom -> AssignCitizen -> EmitCitizenAssignedHome; requires event re-subscription and room state seeding |
| INTG-05 | Housing reassignment handles room demolition gracefully | HousingManager.OnRoomDemolished() works from cached _housingRoomCapacities (no BuildManager dependency); emits CitizenUnhoused then attempts FindBestRoom reassignment; requires seeded room state |
| INTG-06 | Mood tier change propagates correct economy multiplier | Full chain: EmitWishFulfilled -> HappinessManager.OnWishFulfilled -> MoodSystem.OnWishFulfilled -> tier change -> EmitMoodTierChanged + EconomyManager.SetMoodTier; requires event re-subscription |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Chickensoft.GoDotTest | 2.0.30 | Test runner and TestClass/GameTestClass base | Already installed (Phase 20); [Setup]/[Test] lifecycle |
| Shouldly | 4.3.0 | Assertion library | Already installed; .ShouldBe(), .ShouldBeNull() etc. |
| Godot.NET.Sdk | 4.6.1 | Engine SDK | Project target; .NET 10 |

### Supporting
No additional libraries needed. All infrastructure is in place from Phases 20-21.

**Installation:** No new packages needed.

## Architecture Patterns

### Recommended Project Structure
```
Tests/
  Infrastructure/
    TestHelper.cs          # Add ResubscribeAllSingletons()
    GameTestClass.cs       # Existing -- unchanged
  Integration/
    SingletonResetTests.cs          # Existing (Phase 21)
    GameEventsTests.cs              # Existing (Phase 21)
    SingletonIntegrationTests.cs    # NEW (Phase 25)
Scripts/
  Autoloads/
    HousingManager.cs      # Add SubscribeToEvents() + SeedRoomForTest()
    HappinessManager.cs    # Add SubscribeToEvents()
    EconomyManager.cs      # Add SubscribeToEvents()
```

### Pattern 1: Event Re-subscription for Integration Tests

**What:** After `GameTestClass.[Setup]` calls `ResetAllSingletons()` (which clears all event subscribers), singletons need their event subscriptions restored so cross-singleton event chains work.

**Why this is needed:** `ClearAllSubscribers()` nulls all event delegate fields on `GameEvents.Instance`. The subscriptions from `_Ready()` during autoload init are permanently severed. Singletons cannot receive events until re-subscribed.

**Affected singletons and their event subscriptions:**
- `HousingManager`: `CitizenArrived`, `RoomPlaced`, `RoomDemolished` (all handlers are private)
- `HappinessManager`: `WishFulfilled` (handler is private)
- `EconomyManager`: `CitizenEnteredRoom`, `CitizenExitedRoom` (not needed for Phase 25 tests but should be included for completeness)

**Recommended approach -- SubscribeToEvents() method:**
```csharp
// In HousingManager.cs
public void SubscribeToEvents()
{
    if (GameEvents.Instance == null) return;
    _onRoomPlaced = OnRoomPlaced;
    _onRoomDemolished = OnRoomDemolished;
    _onCitizenArrived = OnCitizenArrived;
    GameEvents.Instance.RoomPlaced += _onRoomPlaced;
    GameEvents.Instance.RoomDemolished += _onRoomDemolished;
    GameEvents.Instance.CitizenArrived += _onCitizenArrived;
}

// In HappinessManager.cs
public void SubscribeToEvents()
{
    if (GameEvents.Instance == null) return;
    GameEvents.Instance.WishFulfilled += OnWishFulfilled;
}
```

**Orchestrator in TestHelper:**
```csharp
// In TestHelper.cs
public static void ResubscribeAllSingletons()
{
    HousingManager.Instance?.SubscribeToEvents();
    HappinessManager.Instance?.SubscribeToEvents();
    EconomyManager.Instance?.SubscribeToEvents();
}
```

**Test usage:** Call `TestHelper.ResubscribeAllSingletons()` at the start of each integration test that needs event chains, OR modify `GameTestClass.[Setup]` to call it after `ResetAllSingletons()`.

**Key decision:** Whether to put re-subscription in `GameTestClass.[Setup]` (automatic for all integration tests) or leave it opt-in per test. Recommendation: Put it in `GameTestClass.[Setup]` since any test extending `GameTestClass` presumably needs singleton integration, and re-subscription with no event handlers is harmless.

### Pattern 2: Headless Room State Seeding

**What:** `HousingManager` needs room data in `_housingRoomCapacities` and `_roomOccupants` for `FindBestRoom()` to work. The existing `RestoreFromSave()` calls `InitializeExistingRooms()` which reads `BuildManager`, making it a no-op in headless tests.

**Why this is needed:** After `BuildManager.Reset()`, `_placedRooms` is empty. `InitializeExistingRooms()` iterates 24 segments, calls `BuildManager.Instance.GetPlacedRoom(i)` for each, gets null for all, and populates nothing.

**Recommended approach -- SeedRoomForTest():**
```csharp
// In HousingManager.cs -- Test Infrastructure section
/// <summary>
/// Seeds a housing room directly into internal tracking, bypassing BuildManager.
/// For integration tests that need room state without a visual scene.
/// </summary>
public void SeedRoomForTest(int anchorIndex, int capacity)
{
    _housingRoomCapacities[anchorIndex] = capacity;
    _roomOccupants[anchorIndex] = new List<string>();
}
```

**Why not RestoreFromSave():** The CONTEXT.md locks "Use HousingManager.RestoreFromSave() to pre-seed housing state," but this method fundamentally depends on BuildManager having rooms. The intent (bypass BuildManager) can be honored by seeding rooms through `SeedRoomForTest()` to populate what `InitializeExistingRooms()` would have populated, then using the live event-driven paths (EmitCitizenArrived, EmitRoomDemolished) for the actual behavior under test. This preserves the hybrid spirit of the CONTEXT.md approach while fixing the BuildManager dependency.

**Alternative:** Modify `RestoreFromSave()` to accept room capacity data directly (but this changes production API semantics). `SeedRoomForTest()` is cleaner because it's explicitly test infrastructure.

### Pattern 3: Inline Event Assertion (Established Pattern)
**What:** Subscribe to events with inline lambdas that capture local variables for assertions.
**When to use:** Verifying intermediate events fire with correct parameters.
**Example:**
```csharp
var assigned = new List<(string name, int segment)>();
GameEvents.Instance.CitizenAssignedHome += (name, seg) => assigned.Add((name, seg));

// ... trigger behavior ...

assigned.Count.ShouldBe(3);
assigned[0].name.ShouldBe("Alice");
```

### Anti-Patterns to Avoid
- **Reusable event recorder class:** CONTEXT.md explicitly says "No reusable event recorder helper -- keep it explicit and inline"
- **Testing private state via reflection:** Use only public APIs (GetHomeForCitizen, GetOccupantCount, GetOccupants, CalculateTickIncome)
- **Relying on RestoreFromSave without BuildManager:** As documented above, this is a no-op in headless tests

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Singleton state reset | Manual field clearing in each test | GameTestClass [Setup] auto-reset | Already built in Phase 21; consistent cleanup |
| Event chain wiring | Manual event subscription in each test | TestHelper.ResubscribeAllSingletons() | Centralizes re-subscription; mirrors ClearAllSubscribers() symmetry |
| Room capacity seeding | Direct dictionary manipulation via reflection | SeedRoomForTest() public method | Clean public API; doesn't break encapsulation |
| Mood tier computation | Manual threshold math in tests | Real MoodSystem via OnWishFulfilled chain | Tests should exercise production code, not reimplement it |

**Key insight:** These integration tests verify that real singleton code works together through real events. Any test-side reimplementation of production logic defeats the purpose. The only "fake" input should be the initial state seeding.

## Common Pitfalls

### Pitfall 1: Event Subscriptions Cleared by GameTestClass
**What goes wrong:** Tests emit events (EmitCitizenArrived, EmitWishFulfilled) but nothing happens -- singletons don't respond.
**Why it happens:** `ClearAllSubscribers()` in `[Setup]` removes all event wiring from `_Ready()`.
**How to avoid:** Call `TestHelper.ResubscribeAllSingletons()` after `ResetAllSingletons()` in `GameTestClass.[Setup]`.
**Warning signs:** Tests pass but assertions on state changes fail (no events were processed).

### Pitfall 2: RestoreFromSave Depends on BuildManager
**What goes wrong:** Calling `RestoreFromSave()` results in empty `_housingRoomCapacities`, making all assignment operations silently fail.
**Why it happens:** `InitializeExistingRooms()` reads from `BuildManager.Instance.GetPlacedRoom()` which returns null for all segments after `Reset()`.
**How to avoid:** Use `SeedRoomForTest()` to directly populate room tracking dictionaries.
**Warning signs:** `TotalCapacity` is 0 after calling `RestoreFromSave()`.

### Pitfall 3: FindBestRoom Tiebreaking is Random
**What goes wrong:** Tests that assert exact room assignments for ties fail intermittently.
**Why it happens:** `FindBestRoom()` uses `GD.Randi() % tieCount` for reservoir sampling tiebreaks.
**How to avoid:** Design test scenarios where ties don't occur (rooms with different occupancy levels), OR assert distribution properties (max-min occupancy <= 1) rather than exact room assignments.
**Warning signs:** Flaky test failures on CI, passing locally.

### Pitfall 4: Float Precision in Mood Math
**What goes wrong:** Assertions on exact mood values fail due to float32 arithmetic.
**Why it happens:** Repeated `_mood + 0.06f` accumulates rounding error. Phase 22 documented that `5*0.06f < 0.30f` in float32.
**How to avoid:** Use `.ShouldBe(expected, tolerance)` for mood value assertions, or rely on tier transitions (integer comparisons) as the primary assertion. For INTG-06, assert tier change and income change, not exact mood float.
**Warning signs:** `0.29999998` != `0.30000000` assertion failures.

### Pitfall 5: HappinessManager._Process Not Called in Tests
**What goes wrong:** Tier changes from decay don't happen because `_Process` never runs.
**Why it happens:** Tests don't advance the Godot frame loop. `_Process` is the decay path; `OnWishFulfilled` is the gain path.
**How to avoid:** For INTG-06, use the `OnWishFulfilled` path (via EmitWishFulfilled) which checks tier immediately after mood gain. Don't rely on decay-based tier changes in integration tests.
**Warning signs:** Mood only goes up (wishes), never down (decay), which is fine for these tests.

### Pitfall 6: OnRoomDemolished Emits CitizenUnhoused THEN Attempts Reassignment
**What goes wrong:** Tests that subscribe to CitizenUnhoused and assert "citizen stays unhoused" may see the citizen get reassigned to another room immediately after.
**Why it happens:** `OnRoomDemolished` fires `EmitCitizenUnhoused(citizenName)` then calls `FindBestRoom()` for reassignment. If other rooms exist, the citizen is immediately re-housed.
**How to avoid:** For "demolish the ONLY room" test, ensure no other rooms exist. For "demolish with remaining rooms" test, assert reassignment via CitizenAssignedHome events.
**Warning signs:** CitizenUnhoused fires but GetHomeForCitizen still returns a value.

## Code Examples

Verified patterns from production code analysis:

### INTG-04: Housing Assignment Test Structure
```csharp
// After GameTestClass [Setup] runs ResetAllSingletons + ResubscribeAllSingletons:
var housing = HousingManager.Instance;

// Seed two rooms (anchor indices 0 and 5, capacity 2 each)
housing.SeedRoomForTest(0, 2);
housing.SeedRoomForTest(5, 2);

// Track assignment events
var assigned = new List<(string name, int segment)>();
GameEvents.Instance.CitizenAssignedHome += (name, seg) => assigned.Add((name, seg));

// Trigger citizen arrivals
GameEvents.Instance.EmitCitizenArrived("Alice");
GameEvents.Instance.EmitCitizenArrived("Bob");
GameEvents.Instance.EmitCitizenArrived("Carol");

// Verify even distribution (max-min occupancy <= 1)
int room0 = housing.GetOccupantCount(0);
int room5 = housing.GetOccupantCount(5);
Math.Abs(room0 - room5).ShouldBeLessThanOrEqualTo(1);

// Verify all citizens housed
housing.GetHomeForCitizen("Alice").ShouldNotBeNull();
housing.GetHomeForCitizen("Bob").ShouldNotBeNull();
housing.GetHomeForCitizen("Carol").ShouldNotBeNull();

// Verify assignment events fired
assigned.Count.ShouldBe(3);
```

### INTG-05: Demolition Reassignment Test Structure
```csharp
var housing = HousingManager.Instance;

// Seed two rooms and populate with citizens
housing.SeedRoomForTest(0, 3);
housing.SeedRoomForTest(5, 3);
GameEvents.Instance.EmitCitizenArrived("Alice");
GameEvents.Instance.EmitCitizenArrived("Bob");
GameEvents.Instance.EmitCitizenArrived("Carol");

// Ensure at least one citizen is in room 0
// (Note: assignment depends on FindBestRoom tiebreaking; use occupancy assertions)

// Track events
var unhoused = new List<string>();
var reassigned = new List<(string name, int segment)>();
GameEvents.Instance.CitizenUnhoused += name => unhoused.Add(name);
GameEvents.Instance.CitizenAssignedHome += (name, seg) => reassigned.Add((name, seg));

// Demolish room 0
GameEvents.Instance.EmitRoomDemolished(0);

// All displaced citizens should have been reassigned to room 5
foreach (var citizen in unhoused)
{
    housing.GetHomeForCitizen(citizen).ShouldBe(5);
}
housing.TotalHoused.ShouldBe(3); // No one lost
```

### INTG-06: Mood-Economy Propagation Test Structure
```csharp
var economy = EconomyManager.Instance;

// Set up citizen/worker counts for income calculation
economy.SetCitizenCount(5);
economy.SetWorkingCitizenCount(0);

// Record income at Quiet tier (default after reset)
int quietIncome = economy.CalculateTickIncome();

// Track tier change events
var tierChanges = new List<(MoodTier newTier, MoodTier oldTier)>();
GameEvents.Instance.MoodTierChanged += (newTier, oldTier) =>
    tierChanges.Add((newTier, oldTier));

// Pump wishes to cross Quiet -> Cozy (2 wishes: 0.06, 0.12 >= 0.10)
GameEvents.Instance.EmitWishFulfilled("Alice", "comfort");
GameEvents.Instance.EmitWishFulfilled("Alice", "comfort");

// Verify tier changed
tierChanges.Count.ShouldBe(1);
tierChanges[0].oldTier.ShouldBe(MoodTier.Quiet);
tierChanges[0].newTier.ShouldBe(MoodTier.Cozy);

// Verify economy updated
int cozyIncome = economy.CalculateTickIncome();
cozyIncome.ShouldBeGreaterThan(quietIncome);
```

### Pre-computed Test Values

**Wish count to cross Quiet -> Cozy:**
- MoodGainPerWish = 0.06f, TierCozyThreshold = 0.10f
- Wish 1: mood = 0.06 (Quiet)
- Wish 2: mood = 0.12 (>= 0.10, promotes to Cozy)
- Answer: **2 wishes**

**Income at Quiet vs Cozy (5 citizens, 0 workers):**
- Base = 1.0, CitizenIncome = 2.0 * sqrt(5) = 4.4721, WorkBonus = 0
- Quiet: (1.0 + 4.4721) * 1.0 = 5.4721 -> RoundToInt = 5
- Cozy: (1.0 + 4.4721) * 1.1 = 6.0194 -> RoundToInt = 6
- Delta: 6 - 5 = 1 credit difference

**Capacity formula:** `BaseCapacity + (segmentCount - 1)`
- 1-seg room, BaseCapacity=2: capacity 2
- 3-seg room, BaseCapacity=2: capacity 4
- For test simplicity, use BaseCapacity directly as capacity for 1-seg rooms

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Mocking singletons | Real singletons with Reset() | Phase 21 (this milestone) | Tests exercise actual production code |
| Float-based happiness | MoodTier enum with hysteresis | Phase 10 | Tier transitions are discrete, testable |

**Deprecated/outdated:**
- Nothing deprecated. All APIs are current.

## Critical Research Finding: Production Code Changes Required

The CONTEXT.md states "No production code changes needed -- all APIs are already public." This is **incorrect** for three reasons:

### 1. Event Re-subscription (BLOCKING)

After `ClearAllSubscribers()` in `[Setup]`, singletons cannot receive events. The handlers (`OnCitizenArrived`, `OnWishFulfilled`, `OnRoomDemolished`) are private methods. There is no public API to re-subscribe singletons to events.

**Required changes:**
- `HousingManager.cs`: Add public `SubscribeToEvents()` method
- `HappinessManager.cs`: Add public `SubscribeToEvents()` method
- `EconomyManager.cs`: Add public `SubscribeToEvents()` method (for completeness)
- `TestHelper.cs`: Add `ResubscribeAllSingletons()` method
- `GameTestClass.cs`: Call `ResubscribeAllSingletons()` after `ResetAllSingletons()` in `[Setup]`

### 2. Room State Seeding (BLOCKING)

`HousingManager.RestoreFromSave()` depends on `BuildManager` having rooms. After reset, BuildManager is empty.

**Required change:**
- `HousingManager.cs`: Add public `SeedRoomForTest(int anchorIndex, int capacity)` method in the Test Infrastructure section

### 3. Impact Assessment

Both changes are small, well-scoped additions to the Test Infrastructure sections that already exist in each singleton. They follow the established pattern of adding public methods specifically for test support (like `Reset()` itself). Total: ~30 lines of production code across 4 files.

## Open Questions

1. **Should ResubscribeAllSingletons() live in GameTestClass.[Setup] or be called manually per test?**
   - What we know: All Phase 25 tests need event chains. Re-subscribing with no active events is harmless.
   - Recommendation: Put it in `GameTestClass.[Setup]` for consistency. Any future test extending GameTestClass that needs event chains will automatically work.

2. **Should SeedRoomForTest be called RestoreRoomForTest for naming consistency?**
   - What we know: The pattern is test infrastructure methods (`Reset()`, `SubscribeToEvents()`). "Seed" implies test-only data injection.
   - Recommendation: `SeedRoomForTest` clearly communicates intent. Using "Restore" could confuse with `RestoreFromSave`.

3. **Random tiebreaking in FindBestRoom -- how to write deterministic tests?**
   - What we know: `GD.Randi()` is Godot's PRNG. No seed control from tests.
   - Recommendation: Design scenarios to avoid ties. Assert distribution properties (occupancy spread <= 1) rather than exact assignments. For 3 citizens in 2 rooms, expect 1-2 or 2-1 distribution -- don't assert which room gets which citizen.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Chickensoft.GoDotTest 2.0.30 |
| Config file | Tests/TestRunner.tscn + Tests/TestRunner.cs |
| Quick run command | `godot --headless --run-tests --quit-on-finish` |
| Full suite command | `godot --headless --run-tests --quit-on-finish` |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| INTG-04 | Housing assignment distributes citizens evenly | integration | `godot --headless --run-tests --quit-on-finish` | No - Wave 0 |
| INTG-04 | Single room gets all citizens | integration | same | No - Wave 0 |
| INTG-04 | Different capacity rooms distribute correctly | integration | same | No - Wave 0 |
| INTG-04 | Full rooms leave citizen unhoused | integration | same | No - Wave 0 |
| INTG-05 | Demolish room redistributes citizens | integration | same | No - Wave 0 |
| INTG-05 | Demolish only room leaves citizens unhoused | integration | same | No - Wave 0 |
| INTG-06 | Wish fulfillment crosses mood tier | integration | same | No - Wave 0 |
| INTG-06 | Tier change updates economy multiplier | integration | same | No - Wave 0 |

### Sampling Rate
- **Per task commit:** `godot --headless --run-tests --quit-on-finish`
- **Per wave merge:** Same (single test binary)
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `Tests/Integration/SingletonIntegrationTests.cs` -- covers INTG-04, INTG-05, INTG-06
- [ ] Production code: `HousingManager.SubscribeToEvents()` + `SeedRoomForTest()`
- [ ] Production code: `HappinessManager.SubscribeToEvents()`
- [ ] Production code: `EconomyManager.SubscribeToEvents()`
- [ ] Infrastructure: `TestHelper.ResubscribeAllSingletons()`
- [ ] Infrastructure: `GameTestClass.[Setup]` updated to call re-subscribe

## Sources

### Primary (HIGH confidence)
- `/workspace/Scripts/Autoloads/HousingManager.cs` -- Full source analysis of all internal data structures, event handlers, RestoreFromSave flow, FindBestRoom algorithm
- `/workspace/Scripts/Autoloads/HappinessManager.cs` -- Full source analysis of OnWishFulfilled chain, MoodSystem interaction, EconomyManager.SetMoodTier call
- `/workspace/Scripts/Autoloads/EconomyManager.cs` -- Full source analysis of SetMoodTier, CalculateTickIncome, IncomeMultiplierForTier
- `/workspace/Scripts/Autoloads/GameEvents.cs` -- All 34 event delegates, ClearAllSubscribers() implementation
- `/workspace/Scripts/Happiness/MoodSystem.cs` -- OnWishFulfilled mood gain, CalculateTier with hysteresis
- `/workspace/Scripts/Data/HappinessConfig.cs` -- Default thresholds: Cozy=0.10, MoodGainPerWish=0.06
- `/workspace/Scripts/Data/EconomyConfig.cs` -- Default multipliers: Quiet=1.0, Cozy=1.1, income formula
- `/workspace/Tests/Infrastructure/TestHelper.cs` -- ResetAllSingletons ordering (ClearAllSubscribers first)
- `/workspace/Tests/Infrastructure/GameTestClass.cs` -- [Setup] auto-reset pattern
- `/workspace/Tests/Integration/SingletonResetTests.cs` -- Established test patterns for singleton verification
- `/workspace/Tests/Integration/GameEventsTests.cs` -- Inline lambda event testing pattern
- `/workspace/Tests/Economy/EconomyTests.cs` -- GameTestClass usage, pre-computed expected values
- `/workspace/Tests/Mood/MoodSystemTests.cs` -- MoodSystem unit test patterns, tolerance assertions

### Secondary (MEDIUM confidence)
- Phase 21 Research (`.planning/phases/21-integration-test-infrastructure/21-RESEARCH.md`) -- Original singleton reset design rationale

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- all libraries are already installed and verified across 5 previous phases
- Architecture: HIGH -- patterns are established by Phase 21-24 test code; production code reviewed line-by-line
- Pitfalls: HIGH -- all pitfalls verified by direct source code analysis (especially event subscription clearing)
- Production code changes: HIGH -- verified by tracing ClearAllSubscribers -> null delegate fields -> Emit?.Invoke() no-op chain

**Research date:** 2026-03-07
**Valid until:** Indefinite (testing infrastructure is stable; no external dependencies)
