# Phase 23: Economy and Housing Unit Tests - Context

**Gathered:** 2026-03-07
**Status:** Ready for planning

<domain>
## Phase Boundary

Economy formulas and housing capacity math are regression-proof — all pure calculations have passing tests. Covers requirements ECON-01 through ECON-03 and HOUS-01, plus targeted edge cases (zero-citizen income, BaseCostOverride branch, demolish rounding). No integration tests (Phase 25), no save/load tests (Phase 24).

</domain>

<decisions>
## Implementation Decisions

### Economy test approach
- Extend `GameTestClass` — economy methods are instance methods on the EconomyManager singleton
- Use `EconomyManager.Instance` via private shorthand property `Econ => EconomyManager.Instance`
- Singleton reset between tests handled by GameTestClass base class
- State set up via public API: `SetCitizenCount()`, `SetWorkingCitizenCount()`, `SetMoodTier()`
- Tests exercise the full income formula including work bonus component: (base + citizen*sqrt + workers*bonus) * tierMult

### Config strategy
- Use production `new EconomyConfig()` defaults — tests break when config changes, intentional (consistent with Phase 22)
- Expected values computed from known production defaults (BaseRoomCost=100, multipliers, etc.)
- No custom test configs

### Room cost coverage
- All 30 combos: 5 categories × 3 segment sizes × 2 row positions (inner/outer)
- Individual `[Test]` methods per combo, grouped by category with comment headers
- Behavior-focused method names: `RoomCostHousing1SegInner`, `RoomCostWork3SegOuter`, etc.
- Plus 1-2 tests for `BaseCostOverride > 0` branch on RoomDefinition

### Tick income tests
- One test per mood tier (5 total) with citizens and workers set via public setters
- Verify full formula: (BaseStationIncome + PassiveIncomePerCitizen * sqrt(citizenCount) + workers * WorkBonusMultiplier) * tierMultiplier
- Zero-citizen edge case: verifies BaseStationIncome trickle in isolation

### Demolish refund tests
- Test per-requirement: CalculateDemolishRefund returns correct partial refund
- Edge case: odd cost that forces rounding (e.g., cost=77, refund=round(77*0.5)=39)

### Housing test placement
- Expand existing `Tests/Housing/HousingTests.cs` — add 2-seg and 3-seg ComputeCapacity tests alongside existing 1-seg smoke test
- Remove commented-out Shouldly demo from smoke test era
- Keep extending `TestClass` (ComputeCapacity is static, no singleton needed)
- Vary BaseCapacity across tests (not always 2) to confirm formula works with different bases

### Test file organization
- Economy: `Tests/Economy/EconomyTests.cs` — new file, replaces `.gitkeep`
- Housing: `Tests/Housing/HousingTests.cs` — expand existing file
- Namespace: `OrbitalRings.Tests.Economy` and `OrbitalRings.Tests.Housing`

### Helpers and conventions
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

</decisions>

<specifics>
## Specific Ideas

- Extends `GameTestClass` for economy (singleton-dependent), `TestClass` for housing (static method)
- Shouldly assertions used directly, no wrappers
- Production config defaults with pre-computed expected values in comments alongside each assertion

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `EconomyManager.CalculateRoomCost(RoomDefinition, int, bool)`: instance method, depends on Config + private GetCategoryMultiplier()
- `EconomyManager.CalculateTickIncome()`: instance method, depends on Config + _citizenCount + _workingCitizenCount + _currentTierMultiplier
- `EconomyManager.CalculateDemolishRefund(int)`: instance method, depends on Config.DemolishRefundRatio
- `EconomyManager.SetCitizenCount(int)`, `SetWorkingCitizenCount(int)`, `SetMoodTier(MoodTier)`: public state setters
- `HousingManager.ComputeCapacity(RoomDefinition, int)`: static method, pure formula
- `RoomDefinition`: Godot Resource with Category enum, BaseCapacity, BaseCostOverride fields
- `EconomyConfig`: all production defaults documented in code (BaseRoomCost=100, multipliers 0.7-1.3, etc.)

### Established Patterns
- `GameTestClass` extends `TestClass`, auto-calls `TestHelper.ResetAllSingletons()` before each test (Phase 21)
- Tests/Mood/MoodSystemTests.cs: established pattern for comment-header grouping, behavior-focused naming
- Tests/Housing/HousingTests.cs: existing smoke test for ComputeCapacity with BaseCapacity=2, 1-seg

### Integration Points
- `Tests/Economy/EconomyTests.cs`: new file, replaces `.gitkeep`
- `Tests/Housing/HousingTests.cs`: expand with additional ComputeCapacity tests
- No production code changes needed — all APIs are already public

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 23-economy-and-housing-unit-tests*
*Context gathered: 2026-03-07*
