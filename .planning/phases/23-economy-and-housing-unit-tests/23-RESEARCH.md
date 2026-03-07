# Phase 23: Economy and Housing Unit Tests - Research

**Researched:** 2026-03-07
**Domain:** C# unit testing for Godot game economy formulas and housing capacity math
**Confidence:** HIGH

## Summary

This phase covers unit tests for four pure calculation methods: `CalculateRoomCost`, `CalculateTickIncome`, `CalculateDemolishRefund` (on `EconomyManager`), and `ComputeCapacity` (on `HousingManager`). All APIs are already public and well-documented. The production code uses `new EconomyConfig()` defaults exclusively, so expected values can be pre-computed with certainty.

The test infrastructure from Phases 20-22 is fully operational. Economy tests extend `GameTestClass` (singleton-dependent methods need reset between tests). Housing tests extend `TestClass` (static method, no singleton needed). The established pattern from `MoodSystemTests.cs` provides the template for comment-header grouping, behavior-focused naming, and Shouldly assertions.

**Primary recommendation:** Follow the MoodSystemTests pattern exactly. Pre-compute all 30 room cost expected values (verified in this research), 5 tier income values, demolish refund values, and 3 housing capacities. Write individual `[Test]` methods per combination for clear failure reporting.

**Critical finding:** `Mathf.RoundToInt` uses banker's rounding (`MidpointRounding.ToEven`), not away-from-zero. This means `RoundToInt(38.5f) = 38`, not 39. The CONTEXT.md demolish edge case claim of `round(77*0.5)=39` is incorrect -- the actual result is 38. Tests must use banker's rounding expectations.

<user_constraints>

## User Constraints (from CONTEXT.md)

### Locked Decisions
- Extend `GameTestClass` for economy tests (singleton-dependent instance methods)
- Use `EconomyManager.Instance` via private shorthand property `Econ => EconomyManager.Instance`
- Singleton reset between tests handled by GameTestClass base class
- State set up via public API: `SetCitizenCount()`, `SetWorkingCitizenCount()`, `SetMoodTier()`
- Tests exercise the full income formula including work bonus component
- Use production `new EconomyConfig()` defaults -- tests break when config changes, intentional
- Expected values computed from known production defaults (BaseRoomCost=100, multipliers, etc.)
- No custom test configs
- All 30 combos: 5 categories x 3 segment sizes x 2 row positions (inner/outer)
- Individual `[Test]` methods per combo, grouped by category with comment headers
- Behavior-focused method names: `RoomCostHousing1SegInner`, `RoomCostWork3SegOuter`, etc.
- Plus 1-2 tests for `BaseCostOverride > 0` branch on RoomDefinition
- One test per mood tier (5 total) with citizens and workers set via public setters
- Verify full formula: (BaseStationIncome + PassiveIncomePerCitizen * sqrt(citizenCount) + workers * WorkBonusMultiplier) * tierMultiplier
- Zero-citizen edge case: verifies BaseStationIncome trickle in isolation
- Test per-requirement: CalculateDemolishRefund returns correct partial refund
- Edge case: odd cost that forces rounding
- Expand existing `Tests/Housing/HousingTests.cs` -- add 2-seg and 3-seg ComputeCapacity tests
- Remove commented-out Shouldly demo from smoke test era
- Keep extending `TestClass` (ComputeCapacity is static, no singleton needed)
- Vary BaseCapacity across tests (not always 2) to confirm formula works with different bases
- Economy: `Tests/Economy/EconomyTests.cs` -- new file, replaces `.gitkeep`
- Housing: `Tests/Housing/HousingTests.cs` -- expand existing file
- Namespace: `OrbitalRings.Tests.Economy` and `OrbitalRings.Tests.Housing`
- Private static category helpers: `HousingRoom()`, `WorkRoom()`, `LifeSupportRoom()`, `ComfortRoom()`, `UtilityRoom()` returning `RoomDefinition` with the right category
- Private static `Econ => EconomyManager.Instance` shorthand property
- Comment headers for grouping: `// --- Room Cost: Housing ---`, `// --- Tick Income ---`, etc.
- No `#region` directives (consistent with Phase 22)

### Claude's Discretion
- Exact expected values for all 30 room cost assertions (compute from formula + config defaults)
- Exact citizen/worker counts used in tick income tests
- Whether to test all 5 tiers at a single citizen count or vary citizen counts across tiers
- Test method ordering within each topic group
- Whether the BaseCostOverride test uses a real category or a generic one

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope

</user_constraints>

<phase_requirements>

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| ECON-01 | CalculateRoomCost returns correct costs for all room types and sizes | All 30 expected values pre-computed from production config defaults; float32 precision verified; formula: baseCost * catMult * sizeFactor * rowMult |
| ECON-02 | CalculateTickIncome applies tier multiplier correctly across all 5 tiers | Income formula documented with exact config values; tier multiplier mapping verified in source; edge case (0 citizens) analyzed |
| ECON-03 | CalculateDemolishRefund returns correct partial refund | Formula: RoundToInt(originalCost * 0.5f); banker's rounding behavior verified for .5 edge cases |
| HOUS-01 | ComputeCapacity returns correct values for all segment sizes (1/2/3) | Formula: BaseCapacity + (segmentCount - 1); static method, no singleton dependency; existing smoke test to expand |

</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Chickensoft.GoDotTest | 2.0.30 | Test runner/discovery | Already in project, Phase 20 established |
| Shouldly | (project ref) | Assertion library | Already in project, established assertion pattern |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| GameTestClass | N/A (project) | Auto-resets singletons before each test | Economy tests (singleton-dependent) |
| TestClass | N/A (GoDotTest) | Base test class without reset | Housing tests (static method) |

### No Additional Libraries Needed
This phase uses exclusively existing infrastructure. No new packages.

## Architecture Patterns

### File Structure
```
Tests/
  Economy/
    EconomyTests.cs          # NEW -- replaces .gitkeep
  Housing/
    HousingTests.cs           # EXPAND -- add 2-seg, 3-seg capacity tests
```

### Pattern 1: GameTestClass for Singleton-Dependent Tests
**What:** Economy tests extend `GameTestClass`, which auto-calls `TestHelper.ResetAllSingletons()` via `[Setup]` before each test.
**When to use:** Any test that reads from `EconomyManager.Instance` (since `CalculateRoomCost`, `CalculateTickIncome`, `CalculateDemolishRefund` are instance methods).
**Example:**
```csharp
// Source: Tests/Infrastructure/GameTestClass.cs
public class EconomyTests : GameTestClass
{
    public EconomyTests(Node testScene) : base(testScene) { }

    private static EconomyManager Econ => EconomyManager.Instance;

    // Category helper factories
    private static RoomDefinition HousingRoom() =>
        new() { Category = RoomDefinition.RoomCategory.Housing };

    [Test]
    public void RoomCostHousing1SegInner()
    {
        // 100 * 0.7 * 1.0 * 1.0 = 70
        Econ.CalculateRoomCost(HousingRoom(), 1, false).ShouldBe(70);
    }
}
```

### Pattern 2: TestClass for Static Method Tests
**What:** Housing tests extend `TestClass` (no singleton needed since `ComputeCapacity` is static).
**When to use:** `HousingManager.ComputeCapacity(RoomDefinition, int)` is a static pure function.
**Example:**
```csharp
// Source: Tests/Housing/HousingTests.cs (existing pattern)
public class HousingTests : TestClass
{
    public HousingTests(Node testScene) : base(testScene) { }

    [Test]
    public void ComputeCapacityReturnsBaseForSingleSegment()
    {
        var definition = new RoomDefinition { BaseCapacity = 2 };
        HousingManager.ComputeCapacity(definition, 1).ShouldBe(2);
    }
}
```

### Pattern 3: Comment-Header Grouping (from MoodSystemTests)
**What:** Group related tests under comment headers, no `#region` directives.
**Example:**
```csharp
// --- Room Cost: Housing ---

[Test]
public void RoomCostHousing1SegInner() { ... }

[Test]
public void RoomCostHousing1SegOuter() { ... }

// --- Tick Income ---

[Test]
public void TickIncomeQuietTier() { ... }
```

### Anti-Patterns to Avoid
- **Using `#region` directives:** Project convention (Phase 22) forbids them. Use comment headers.
- **Creating custom EconomyConfig:** Tests must use production defaults. Break on config change is intentional.
- **Using `GameTestClass` for housing tests:** `ComputeCapacity` is static, no singleton needed.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Singleton reset | Manual cleanup in each test | `GameTestClass` base class | Already handles reset via `[Setup]` |
| Expected value computation | Runtime formula evaluation in tests | Pre-computed literal constants | Tests verify specific values, not that formulas match |
| Category room creation | Inline `new RoomDefinition { Category = ... }` everywhere | Static helper methods (`HousingRoom()`, etc.) | Reduces repetition across 30+ tests |

## Common Pitfalls

### Pitfall 1: Banker's Rounding (MidpointRounding.ToEven)
**What goes wrong:** Assuming `Mathf.RoundToInt(38.5f) = 39`. It actually returns 38.
**Why it happens:** `Mathf.RoundToInt` calls `(int)MathF.Round(s)`, which uses .NET's default `MidpointRounding.ToEven` (banker's rounding). Values at exactly .5 round to the nearest even integer.
**How to avoid:** Compute expected values using banker's rounding. For `77 * 0.5f = 38.5f`, the result is 38 (even), not 39.
**Warning signs:** Tests fail on exactly-half values. The CONTEXT.md stated `round(77*0.5)=39` but the correct result is 38.
**Source:** [Godot GodotSharp MathfEx.cs](https://github.com/godotengine/godot/blob/master/modules/mono/glue/GodotSharp/GodotSharp/Core/MathfEx.cs) -- `return (int)MathF.Round(s);`

### Pitfall 2: Float32 Precision in Multi-Step Computation
**What goes wrong:** Expected values computed with float64 (Python/calculator) differ from float32 (C#) results.
**Why it happens:** C# `float` is 32-bit. `baseCost * catMult * sizeFactor * rowMult` evaluates left-to-right with float32 intermediate values.
**How to avoid:** All 30 room cost values were verified with step-by-step float32 simulation in this research. No divergence found for current config values.
**Warning signs:** Tests fail by +/- 1 on values near .5 rounding boundaries.

### Pitfall 3: EconomyManager Default Tier Multiplier
**What goes wrong:** After `Reset()`, `_currentTierMultiplier` is 1.0f (Quiet tier). If tests don't call `SetMoodTier()`, they silently test Quiet tier only.
**Why it happens:** Reset sets `_currentTierMultiplier = 1.0f` which matches Quiet. Tests must explicitly set the tier.
**How to avoid:** Each tier income test must call `Econ.SetMoodTier(MoodTier.X)` before `CalculateTickIncome()`.

### Pitfall 4: BaseCostOverride Test Needs Non-Zero Value
**What goes wrong:** `BaseCostOverride` defaults to 0, which means the formula uses `Config.BaseRoomCost`. To test the override branch, explicitly set `BaseCostOverride > 0`.
**Why it happens:** `float baseCost = room.BaseCostOverride > 0 ? room.BaseCostOverride : Config.BaseRoomCost;`
**How to avoid:** Create a `RoomDefinition` with `BaseCostOverride = 150` (or any positive value) and verify the cost uses 150 instead of 100.

## Code Examples

### EconomyManager API Surface (for tests)
```csharp
// Source: Scripts/Autoloads/EconomyManager.cs

// Room cost: baseCost * catMult * sizeFactor * rowMult
public int CalculateRoomCost(RoomDefinition room, int segmentCount, bool isOuterRow)

// Income: RoundToInt((BaseStationIncome + PassiveIncomePerCitizen * sqrt(citizens) + workers * WorkBonusMultiplier) * tierMult)
public int CalculateTickIncome()

// Refund: RoundToInt(originalCost * DemolishRefundRatio)
public int CalculateDemolishRefund(int originalCost)

// State setters (for test setup)
public void SetCitizenCount(int count)
public void SetWorkingCitizenCount(int count)
public void SetMoodTier(MoodTier tier)
```

### HousingManager API Surface (for tests)
```csharp
// Source: Scripts/Autoloads/HousingManager.cs

// Capacity: BaseCapacity + (segmentCount - 1)
public static int ComputeCapacity(RoomDefinition definition, int segmentCount)
```

### EconomyConfig Production Defaults
```csharp
// Source: Scripts/Data/EconomyConfig.cs
BaseStationIncome = 1.0f
PassiveIncomePerCitizen = 2.0f
WorkBonusMultiplier = 1.25f
BaseRoomCost = 100
SizeDiscountFactor = 0.92f
DemolishRefundRatio = 0.5f
OuterRowCostMultiplier = 1.1f
HousingCostMultiplier = 0.7f
LifeSupportCostMultiplier = 0.85f
WorkCostMultiplier = 1.0f
ComfortCostMultiplier = 1.15f
UtilityCostMultiplier = 1.3f
IncomeMultQuiet = 1.0f
IncomeMultCozy = 1.1f
IncomeMultLively = 1.2f
IncomeMultVibrant = 1.3f
IncomeMultRadiant = 1.4f
```

### Pre-Computed Room Cost Expected Values (All 30 Combos)

Formula: `RoundToInt(BaseRoomCost * CategoryMult * (segments * SizeDiscountFactor^(segments-1)) * RowMult)`

Verified with float32 step-by-step simulation -- no divergence from float64 for these config values.

| Category | Segments | Row | Expected Cost |
|----------|----------|-----|---------------|
| Housing | 1 | Inner | 70 |
| Housing | 1 | Outer | 77 |
| Housing | 2 | Inner | 129 |
| Housing | 2 | Outer | 142 |
| Housing | 3 | Inner | 178 |
| Housing | 3 | Outer | 196 |
| LifeSupport | 1 | Inner | 85 |
| LifeSupport | 1 | Outer | 94 |
| LifeSupport | 2 | Inner | 156 |
| LifeSupport | 2 | Outer | 172 |
| LifeSupport | 3 | Inner | 216 |
| LifeSupport | 3 | Outer | 237 |
| Work | 1 | Inner | 100 |
| Work | 1 | Outer | 110 |
| Work | 2 | Inner | 184 |
| Work | 2 | Outer | 202 |
| Work | 3 | Inner | 254 |
| Work | 3 | Outer | 279 |
| Comfort | 1 | Inner | 115 |
| Comfort | 1 | Outer | 126 |
| Comfort | 2 | Inner | 212 |
| Comfort | 2 | Outer | 233 |
| Comfort | 3 | Inner | 292 |
| Comfort | 3 | Outer | 321 |
| Utility | 1 | Inner | 130 |
| Utility | 1 | Outer | 143 |
| Utility | 2 | Inner | 239 |
| Utility | 2 | Outer | 263 |
| Utility | 3 | Inner | 330 |
| Utility | 3 | Outer | 363 |

### Pre-Computed Tick Income Expected Values

Using citizens=10, workers=3 (recommended test values -- produce distinguishable results across tiers):

Formula: `RoundToInt((1.0 + 2.0 * sqrt(10) + 3 * 1.25) * tierMult)`
Subtotal: `1.0 + 6.3246 + 3.75 = 11.0746`

| Tier | Multiplier | Raw | Expected |
|------|-----------|-----|----------|
| Quiet | 1.0 | 11.0746 | 11 |
| Cozy | 1.1 | 12.1820 | 12 |
| Lively | 1.2 | 13.2895 | 13 |
| Vibrant | 1.3 | 14.3969 | 14 |
| Radiant | 1.4 | 15.5044 | 16 |

Zero-citizen edge case (0 citizens, 0 workers):

| Tier | Raw | Expected |
|------|-----|----------|
| Quiet | 1.0 | 1 |
| Cozy | 1.1 | 1 |
| Lively | 1.2 | 1 |
| Vibrant | 1.3 | 1 |
| Radiant | 1.4 | 1 |

Note: All 5 zero-citizen tiers yield 1 (BaseStationIncome trickle). Only Quiet tier is truly distinct for zero-citizen testing -- the others all round to 1.

### Pre-Computed Demolish Refund Values

Formula: `RoundToInt(originalCost * 0.5f)`

| Original Cost | Raw | Expected (banker's rounding) |
|---------------|-----|-----|
| 70 | 35.0 | 35 |
| 100 | 50.0 | 50 |
| 184 | 92.0 | 92 |
| 77 | 38.5 | **38** (not 39 -- banker's rounds to even) |

### Housing Capacity Expected Values

Formula: `BaseCapacity + (segmentCount - 1)`

| BaseCapacity | Segments | Expected |
|-------------|----------|----------|
| 2 | 1 | 2 |
| 2 | 2 | 3 |
| 2 | 3 | 4 |
| 3 | 1 | 3 |
| 3 | 2 | 4 |
| 3 | 3 | 5 |

### BaseCostOverride Test Pattern
```csharp
// When BaseCostOverride > 0, formula uses override instead of Config.BaseRoomCost
var room = new RoomDefinition
{
    Category = RoomDefinition.RoomCategory.Housing,
    BaseCostOverride = 150
};
// 150 * 0.7 * 1.0 * 1.0 = 105
Econ.CalculateRoomCost(room, 1, false).ShouldBe(105);
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `SetHappiness(float)` | `SetMoodTier(MoodTier)` | Phase 11 | Economy operates in tier-space, not float-space |
| `_happiness` float field | `_currentTierMultiplier` via `IncomeMultiplierForTier()` | Phase 11 | Tests set tier directly, verify multiplier effect |

**Deprecated/outdated:**
- `SetHappiness()`: Removed. Use `SetMoodTier(MoodTier)` instead.
- `HappinessMultiplierCap`: Removed in quick task #5 (orphaned field).

## Open Questions

1. **Radiant tier income at 10 citizens/3 workers = 16 (from 15.5044)**
   - What we know: `RoundToInt(15.5044f)` should be 16 (rounds up since 15.5044 > 15.5)
   - What's unclear: Whether float32 intermediate precision produces exactly 15.5044 or slightly different
   - Recommendation: If test fails by +/-1, verify with a debugger. The value is not near a .5 boundary so HIGH confidence it rounds to 16.

2. **Zero-citizen Quiet-only or all-tiers test?**
   - What we know: All 5 tiers at 0 citizens yield income=1 (1.0*tierMult rounds to 1 for all tiers 1.0-1.4)
   - What's unclear: Whether testing all 5 tiers at zero citizens adds value vs just Quiet
   - Recommendation: Test just Quiet at zero citizens (the meaningful edge case). The other tiers don't differentiate at zero citizens.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Chickensoft.GoDotTest 2.0.30 + Shouldly |
| Config file | Tests/TestRunner.tscn + Tests/TestRunner.cs |
| Quick run command | `godot --headless --run-tests --quit-on-finish` |
| Full suite command | `godot --headless --run-tests --quit-on-finish` |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ECON-01 | CalculateRoomCost for all types/sizes | unit | `godot --headless --run-tests --quit-on-finish` | No -- Wave 0 |
| ECON-02 | CalculateTickIncome across 5 tiers | unit | `godot --headless --run-tests --quit-on-finish` | No -- Wave 0 |
| ECON-03 | CalculateDemolishRefund correct partial refund | unit | `godot --headless --run-tests --quit-on-finish` | No -- Wave 0 |
| HOUS-01 | ComputeCapacity for segment sizes 1/2/3 | unit | `godot --headless --run-tests --quit-on-finish` | Partial -- 1-seg exists |

### Sampling Rate
- **Per task commit:** `godot --headless --run-tests --quit-on-finish`
- **Per wave merge:** `godot --headless --run-tests --quit-on-finish`
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `Tests/Economy/EconomyTests.cs` -- covers ECON-01, ECON-02, ECON-03 (replaces .gitkeep)
- [ ] `Tests/Housing/HousingTests.cs` -- expand for HOUS-01 (2-seg, 3-seg, varied BaseCapacity)

## Sources

### Primary (HIGH confidence)
- `/workspace/Scripts/Autoloads/EconomyManager.cs` -- full source of all three methods under test
- `/workspace/Scripts/Data/EconomyConfig.cs` -- all production default values
- `/workspace/Scripts/Autoloads/HousingManager.cs` -- ComputeCapacity source
- `/workspace/Scripts/Data/RoomDefinition.cs` -- RoomCategory enum, BaseCostOverride field
- `/workspace/Tests/Mood/MoodSystemTests.cs` -- established test pattern to follow
- `/workspace/Tests/Housing/HousingTests.cs` -- existing smoke test to expand
- `/workspace/Tests/Infrastructure/GameTestClass.cs` -- singleton auto-reset base class
- [Godot MathfEx.cs](https://github.com/godotengine/godot/blob/master/modules/mono/glue/GodotSharp/GodotSharp/Core/MathfEx.cs) -- RoundToInt implementation confirms banker's rounding

### Secondary (MEDIUM confidence)
- Float32 precision simulation (Python) -- verified all 30 room cost values match under float32 step-by-step evaluation

### Tertiary (LOW confidence)
- None

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- using only existing infrastructure from Phases 20-22
- Architecture: HIGH -- follows established MoodSystemTests pattern exactly
- Pitfalls: HIGH -- banker's rounding verified against Godot source; float32 precision simulated
- Expected values: HIGH -- computed from production source code defaults, verified with float32 simulation

**Research date:** 2026-03-07
**Valid until:** 2026-04-06 (stable -- production config defaults are fixed values)
