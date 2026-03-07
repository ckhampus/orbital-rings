# Phase 22: Mood System Unit Tests - Context

**Gathered:** 2026-03-07
**Status:** Ready for planning

<domain>
## Phase Boundary

MoodSystem POCO logic is regression-proof — decay math, tier transitions, hysteresis, wish gain, and save restore all have passing tests. Pure unit tests only (no singletons, no scene tree). Covers requirements MOOD-01 through MOOD-05 plus targeted edge cases.

</domain>

<decisions>
## Implementation Decisions

### Test file organization
- Single file: `Tests/Mood/MoodSystemTests.cs`
- Remove `.gitkeep` from `Tests/Mood/` when real test file is added
- Group test methods by topic with comment headers (e.g., `// --- Decay Tests ---`), no `#region` directives
- Behavior-focused method names: `DecayMovesTowardBaseline`, `TierPromotesAtThreshold`, `HysteresisPreventsRapidDemotion`

### Config strategy
- Use production `new HappinessConfig()` defaults (DecayRate=0.003662, MoodGainPerWish=0.06, thresholds 0.10/0.30/0.55/0.80)
- Tests break when config changes — intentional: config drift should be caught
- Shared private `CreateMoodSystem()` helper method in the test class — single place to change if constructor evolves
- Trust config values, test behavior — no assertions on HappinessConfig field values themselves

### State pre-seeding
- Use `RestoreState(targetMood, baseline)` to jump to any desired state for tier/hysteresis tests
- Do not simulate wishes/decay to reach a target value — RestoreState is the public API for this

### Assertion precision
- Exact-with-tolerance for decay math: `value.ShouldBe(expected, 0.001f)` — three decimal places
- Tier assertions are exact: `CurrentTier.ShouldBe(MoodTier.Cozy)`
- Decay tests verify both single large delta AND multiple small steps (frame-rate independence)

### Edge cases (beyond MOOD-01 through MOOD-05)
- Wish at max mood: OnWishFulfilled when mood ~0.98, verify cap at 1.0 and Radiant tier
- Decay with zero baseline: Update() with 0 lifetime happiness, mood decays toward zero, never negative
- Restore clamps out-of-range: RestoreState with mood=5.0 and mood=-1.0, verify clamping to [0,1]
- Frame-rate independence: 60x Update(1s) vs 1x Update(60s) produce same result within tolerance
- Undershoot prevention: explicit test that mood never drops below baseline after large delta
- Full promotion sequence: one test calling OnWishFulfilled() repeatedly, asserting tier at each threshold crossing

### Claude's Discretion
- Exact expected values for decay assertions (compute from the formula)
- Number of Update() iterations for frame-rate independence test
- Which specific threshold values to use in tier boundary tests (just at boundary, or also just-above/just-below)
- Test method ordering within each topic group

</decisions>

<specifics>
## Specific Ideas

- Extends `TestClass` directly (not `GameTestClass`) — pure POCO, no singleton reset needed
- Namespace: `OrbitalRings.Tests.Mood`
- Shouldly assertions used directly, no wrappers

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `MoodSystem` (Scripts/Happiness/MoodSystem.cs): Pure POCO, constructor takes HappinessConfig, all methods return MoodTier or void
- `HappinessConfig` (Scripts/Data/HappinessConfig.cs): Godot Resource with all mood constants, `new HappinessConfig()` gives production defaults
- `MoodTier` enum (Scripts/Data/MoodTier.cs): Quiet=0, Cozy=1, Lively=2, Vibrant=3, Radiant=4

### Established Patterns
- Tests/Domain/ComputeCapacityTests.cs: existing smoke test pattern — extends TestClass, uses Shouldly assertions
- Tests/Infrastructure/TestHelper.cs and GameTestClass.cs: available but not needed for pure POCO tests
- Single .csproj compilation: test code has internal member access to MoodSystem

### Integration Points
- Tests/Mood/MoodSystemTests.cs: new file, replaces .gitkeep
- No production code changes needed — MoodSystem API is fully public

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 22-mood-system-unit-tests*
*Context gathered: 2026-03-07*
