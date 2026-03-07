# Phase 22: Mood System Unit Tests - Research

**Researched:** 2026-03-07
**Domain:** C# unit testing of MoodSystem POCO (exponential decay, tier transitions, hysteresis, wish gain, save/restore)
**Confidence:** HIGH

## Summary

Phase 22 tests the MoodSystem class -- a pure POCO with no Godot Node inheritance. All five MOOD requirements map directly to public methods on MoodSystem: `Update()` for decay math, `OnWishFulfilled()` for wish gain, `RestoreState()` for save restoration, and `CurrentTier` for tier transitions with hysteresis. The class takes `HappinessConfig` (a Godot Resource whose `new HappinessConfig()` provides production defaults), making it fully testable without the scene tree.

The existing test infrastructure (GoDotTest 2.0.30 + Shouldly 4.3.0) is proven across HousingTests and SingletonResetTests. MoodSystem tests extend `TestClass` directly (not `GameTestClass`) since no singleton reset is needed. The CONTEXT.md locks down all major design decisions -- single file, comment-grouped methods, `RestoreState()` for state pre-seeding, exact-with-tolerance assertions for floats.

**Primary recommendation:** Implement a single `Tests/Mood/MoodSystemTests.cs` with ~15 test methods grouped by topic (Decay, Tier, Hysteresis, Wish, Restore, Edge Cases). Use a shared `CreateMoodSystem()` helper to instantiate MoodSystem with production defaults. Pre-compute expected decay values from the exponential smoothing formula. Use `RestoreState()` to set up initial state for all non-trivial tests.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Single file: `Tests/Mood/MoodSystemTests.cs`
- Remove `.gitkeep` from `Tests/Mood/` when real test file is added
- Group test methods by topic with comment headers (e.g., `// --- Decay Tests ---`), no `#region` directives
- Behavior-focused method names: `DecayMovesTowardBaseline`, `TierPromotesAtThreshold`, `HysteresisPreventsRapidDemotion`
- Use production `new HappinessConfig()` defaults (DecayRate=0.003662, MoodGainPerWish=0.06, thresholds 0.10/0.30/0.55/0.80)
- Tests break when config changes -- intentional: config drift should be caught
- Shared private `CreateMoodSystem()` helper method in the test class -- single place to change if constructor evolves
- Trust config values, test behavior -- no assertions on HappinessConfig field values themselves
- Use `RestoreState(targetMood, baseline)` to jump to any desired state for tier/hysteresis tests
- Do not simulate wishes/decay to reach a target value -- RestoreState is the public API for this
- Exact-with-tolerance for decay math: `value.ShouldBe(expected, 0.001f)` -- three decimal places
- Tier assertions are exact: `CurrentTier.ShouldBe(MoodTier.Cozy)`
- Decay tests verify both single large delta AND multiple small steps (frame-rate independence)
- Extends `TestClass` directly (not `GameTestClass`) -- pure POCO, no singleton reset needed
- Namespace: `OrbitalRings.Tests.Mood`
- Shouldly assertions used directly, no wrappers

### Edge Cases (locked)
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

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| MOOD-01 | MoodSystem decay math produces correct values over time | Exponential smoothing formula fully analyzed; expected values computed for multiple scenarios; frame-rate independence proven mathematically |
| MOOD-02 | MoodSystem tier transitions fire at correct thresholds | All 5 tier thresholds mapped; CalculateTier single-step promotion/demotion behavior documented; boundary values computed |
| MOOD-03 | MoodSystem hysteresis prevents rapid tier oscillation near boundaries | Hysteresis dead-band documented (threshold - 0.05); demotion boundary values computed for all tiers |
| MOOD-04 | MoodSystem wish gain increments mood correctly | OnWishFulfilled adds 0.06 flat; cap at 1.0; full 17-wish promotion sequence computed |
| MOOD-05 | MoodSystem restore reconstructs state from saved values | RestoreState API documented; CalculateTierFromScratch (no hysteresis) behavior verified; clamping behavior confirmed |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Chickensoft.GoDotTest | 2.0.30 | Test framework for Godot C# | Already configured in .csproj; provides [Test], [Setup], TestClass |
| Shouldly | 4.3.0 | Assertion library | Already configured; readable assertions with tolerance overloads for floats |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| MoodSystem | N/A (project code) | System under test | Pure POCO, testable without scene tree |
| HappinessConfig | N/A (project code) | Configuration resource | `new HappinessConfig()` provides production defaults |
| MoodTier | N/A (project code) | Enum (Quiet=0..Radiant=4) | Tier assertion targets |

### No Additional Dependencies Needed
This phase adds only test code. All required packages are already in the .csproj.

## Architecture Patterns

### File Structure
```
Tests/
├── Mood/
│   └── MoodSystemTests.cs    # NEW (replaces .gitkeep)
├── Housing/
│   └── HousingTests.cs       # Existing pattern to follow
├── Infrastructure/
│   ├── TestHelper.cs          # NOT needed for this phase
│   └── GameTestClass.cs       # NOT needed for this phase
└── Integration/
    ├── SingletonResetTests.cs # Reference for TestClass extension pattern
    └── GameEventsTests.cs
```

### Pattern 1: Test Class Structure
**What:** Single test class extending `TestClass`, constructor takes `Node testScene`
**When to use:** All POCO/unit tests that do not require singleton state
**Example:**
```csharp
// Source: Tests/Housing/HousingTests.cs (existing project pattern)
namespace OrbitalRings.Tests.Mood;

using Chickensoft.GoDotTest;
using Godot;
using OrbitalRings.Data;
using OrbitalRings.Happiness;
using Shouldly;

public class MoodSystemTests : TestClass
{
    public MoodSystemTests(Node testScene) : base(testScene) { }

    private static MoodSystem CreateMoodSystem()
    {
        return new MoodSystem(new HappinessConfig());
    }

    // --- Decay Tests ---

    [Test]
    public void DecayMovesTowardBaseline()
    {
        var mood = CreateMoodSystem();
        mood.RestoreState(0.5f, 0.0f);
        mood.Update(60f, 0); // lifetimeHappiness=0 -> baseline=0
        mood.Mood.ShouldBe(0.4014f, 0.001f);
    }
}
```

### Pattern 2: State Pre-seeding via RestoreState
**What:** Use `RestoreState(mood, baseline)` to place MoodSystem in a known state before testing
**When to use:** Any test that needs mood at a specific value (tier boundary, hysteresis, decay from a starting point)
**Key detail:** `RestoreState` uses `CalculateTierFromScratch` (no hysteresis), setting the tier based purely on the mood value. The `_baseline` set by RestoreState gets overwritten on the next `Update()` call (Update recomputes baseline from `lifetimeHappiness`).

### Pattern 3: Tolerance Assertions for Floats
**What:** Use `value.ShouldBe(expected, tolerance)` for floating-point comparisons
**When to use:** All decay math assertions
**Tolerance:** 0.001f (three decimal places) as specified in CONTEXT.md

### Anti-Patterns to Avoid
- **Simulating to reach state:** Do NOT call OnWishFulfilled/Update repeatedly just to get mood to a target value. Use RestoreState instead.
- **Testing config values:** Do NOT assert that HappinessConfig.DecayRate == 0.003662. Trust config, test behavior.
- **Using GameTestClass:** This phase tests a POCO. No singleton reset is needed. Extend TestClass directly.
- **Using #region directives:** Use comment headers (`// --- Topic ---`) instead.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Reaching a specific mood value | Manual wish/decay simulation loops | `RestoreState(targetMood, baseline)` | Simpler, deterministic, tests the public API |
| Tier from mood value | Custom tier lookup logic in tests | `RestoreState` sets tier via `CalculateTierFromScratch` | Already correct in production code |
| Float comparison | Manual `Math.Abs(a - b) < epsilon` | `value.ShouldBe(expected, 0.001f)` | Shouldly handles this with readable error messages |

## Common Pitfalls

### Pitfall 1: Update() Recomputes Baseline from lifetimeHappiness
**What goes wrong:** Test sets baseline via RestoreState, then calls Update, and baseline changes unexpectedly.
**Why it happens:** `Update()` always recomputes `_baseline = min(BaselineCap, BaselineScalingFactor * sqrt(lifetimeHappiness))` before applying decay. The baseline from RestoreState is overwritten.
**How to avoid:** For decay tests, choose the `lifetimeHappiness` argument to `Update()` carefully. If you want baseline=0, pass `lifetimeHappiness=0`. If you need a specific baseline, compute the required lifetimeHappiness: `lh = (desired_baseline / 0.016)^2`.
**Warning signs:** Test assertions on Mood fail with unexpected values because decay targeted the wrong baseline.

### Pitfall 2: CalculateTier Only Steps One Tier Per Call
**What goes wrong:** Test expects mood=0.80 to immediately yield Radiant from Quiet, but CalculateTier only promotes one step.
**Why it happens:** `CalculateTier` checks one promote/demote step. This is intentional design to prevent multi-tier skipping in a single frame.
**How to avoid:** RestoreState bypasses this via `CalculateTierFromScratch`, which sets the correct tier directly. Only the full promotion sequence test (calling OnWishFulfilled repeatedly) needs to account for single-step behavior.
**Warning signs:** After `RestoreState(0.80, 0.0)`, tier is Radiant (correct -- RestoreState uses CalculateTierFromScratch). After `Update()`, tier stays Radiant (correct -- no promotion/demotion needed).

### Pitfall 3: Hysteresis Boundary Is "Less Than", Not "Less Than or Equal"
**What goes wrong:** Test expects demotion at exactly (threshold - hysteresisWidth) but it doesn't demote.
**Why it happens:** The code uses `mood < DemoteThreshold(current)`, which is strict less-than. At exactly the demote threshold, the mood stays in the current tier.
**How to avoid:** Test demotion with a value just BELOW the demote threshold (e.g., 0.049f for Cozy demote threshold of 0.05).
**Warning signs:** Test for "mood at demote boundary stays" fails because you used `<` instead of `<=` mental model.

### Pitfall 4: Promotion Boundary Is "Greater Than or Equal"
**What goes wrong:** Test expects no promotion at exactly the threshold, but it promotes.
**Why it happens:** The code uses `mood >= PromoteThreshold(current)`. At exactly the threshold, promotion occurs.
**How to avoid:** Test promotion with mood exactly AT threshold (should promote) and just BELOW (should not).
**Warning signs:** Off-by-one in threshold tests.

### Pitfall 5: MoodSystem Clamps Above Baseline, Not Above Zero
**What goes wrong:** Test expects mood clamped to 0, but it clamps to baseline.
**Why it happens:** Line 48: `_mood = MathF.Max(_mood, _baseline)`. If baseline is 0.1, mood can never go below 0.1 via decay.
**How to avoid:** For "never negative" tests, use baseline=0 (lifetimeHappiness=0) so the clamp floor is 0. For "never below baseline" tests, use a non-zero baseline.
**Warning signs:** Undershoot test passes trivially because baseline provides the floor anyway.

## Code Examples

### MoodSystem API Surface (verified from source)

```csharp
// Source: Scripts/Happiness/MoodSystem.cs

// Constructor
public MoodSystem(HappinessConfig config)

// Properties
public float Mood { get; }           // Current mood value [0,1]
public float Baseline { get; }       // Current baseline (floor)
public MoodTier CurrentTier { get; } // Current tier with hysteresis

// Core methods
public MoodTier Update(float delta, int lifetimeHappiness)  // Decay each frame
public MoodTier OnWishFulfilled()                           // +0.06 mood, cap at 1.0
public void RestoreState(float mood, float baseline)        // Restore from save
```

### HappinessConfig Production Defaults (verified from source)

```csharp
// Source: Scripts/Data/HappinessConfig.cs
DecayRate           = 0.003662f    // Exponential decay rate per second
MoodGainPerWish     = 0.06f        // Flat gain per wish fulfilled
BaselineScalingFactor = 0.016f     // Coefficient for sqrt(lifetimeHappiness)
BaselineCap         = 0.20f        // Maximum baseline
TierCozyThreshold   = 0.10f        // Quiet -> Cozy
TierLivelyThreshold = 0.30f        // Cozy -> Lively
TierVibrantThreshold = 0.55f       // Lively -> Vibrant
TierRadiantThreshold = 0.80f       // Vibrant -> Radiant
HysteresisWidth     = 0.05f        // Dead-band below each threshold for demotion
```

### Decay Formula (verified from source + computed)

```
alpha = 1 - exp(-DecayRate * delta)
mood  = mood + (baseline - mood) * alpha
mood  = max(mood, baseline)   // Undershoot prevention
```

### Pre-Computed Expected Values for Tests

| Scenario | Initial Mood | Baseline | Delta | lifetimeHappiness | Expected Mood |
|----------|-------------|----------|-------|-------------------|---------------|
| Basic decay toward zero | 0.5 | 0.0 | 60s | 0 | 0.4014 |
| Decay toward non-zero baseline | 0.5 | ~0.10 | 60s | 39 | 0.4211 |
| Large delta (5 min) | 0.8 | 0.0 | 300s | 0 | 0.2667 |
| Small delta (1s) | 1.0 | 0.0 | 1s | 0 | 0.9963 |
| Frame-rate: 1x60s | 0.5 | 0.0 | 60s | 0 | 0.4014 |
| Frame-rate: 60x1s | 0.5 | 0.0 | 1s x60 | 0 | 0.4014 |
| Huge delta (undershoot) | 0.15 | 0.10 | 10000s | ~39 | 0.10 (clamped) |

Note: Frame-rate independence difference is exactly 0.0 for this exponential smoothing formula (mathematically proven).

### Tier Promotion/Demotion Thresholds

| Tier | Promote At (>=) | Demote At (<) | Hysteresis Dead Zone |
|------|-----------------|---------------|----------------------|
| Quiet -> Cozy | 0.10 | N/A | N/A |
| Cozy -> Lively | 0.30 | 0.05 (Cozy -> Quiet) | [0.05, 0.10) |
| Lively -> Vibrant | 0.55 | 0.25 (Lively -> Cozy) | [0.25, 0.30) |
| Vibrant -> Radiant | 0.80 | 0.50 (Vibrant -> Lively) | [0.50, 0.55) |
| Radiant (max) | N/A | 0.75 (Radiant -> Vibrant) | [0.75, 0.80) |

### Wish Fulfillment Promotion Sequence

| Wish # | Mood After | Tier After | Transition |
|--------|-----------|------------|------------|
| 1 | 0.06 | Quiet | -- |
| 2 | 0.12 | Cozy | Quiet -> Cozy |
| 5 | 0.30 | Lively | Cozy -> Lively |
| 10 | 0.60 | Vibrant | Lively -> Vibrant |
| 14 | 0.84 | Radiant | Vibrant -> Radiant |
| 17 | 1.00 | Radiant | Cap reached |

### RestoreState Behavior

```csharp
// RestoreState clamps mood to [0, 1] and baseline to [0, BaselineCap]
// Uses CalculateTierFromScratch (NO hysteresis)
// Tier is set purely based on mood value:
//   >= 0.80 -> Radiant
//   >= 0.55 -> Vibrant
//   >= 0.30 -> Lively
//   >= 0.10 -> Cozy
//   else    -> Quiet

// RestoreState(5.0f, 0.0f) -> mood = 1.0 (clamped), tier = Radiant
// RestoreState(-1.0f, 0.0f) -> mood = 0.0 (clamped), tier = Quiet
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Linear decay | Exponential smoothing | Phase 10 design | Frame-rate independent, smooth approach to baseline |
| Single happiness value | Dual system (lifetime + mood) | Phase 10 design | Lifetime never decreases; mood fluctuates with activity |
| Hard tier boundaries | Hysteresis dead-band | Phase 10 design | Prevents tier flickering near boundaries |

## Open Questions

1. **Shouldly float tolerance exact signature**
   - What we know: `value.ShouldBe(expected, tolerance)` works for float/double/decimal per Shouldly docs
   - What's unclear: Whether tolerance param must match the value type (float vs double) -- CONTEXT.md specifies `0.001f` (float literal) which should work
   - Recommendation: Use `0.001f` as CONTEXT.md specifies; if compiler complains, try `0.001d` (double)

2. **lifetimeHappiness for non-zero baseline in decay tests**
   - What we know: `baseline = min(0.20, 0.016 * sqrt(lh))`. For baseline ~0.10, need `lh=39` (gives 0.0999)
   - What's unclear: Whether to test decay with non-zero baseline at all (MOOD-01 may be satisfied with baseline=0)
   - Recommendation: Include at least one decay test with non-zero baseline to verify decay converges to baseline (not to zero)

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Chickensoft.GoDotTest 2.0.30 + Shouldly 4.3.0 |
| Config file | `Orbital Rings.csproj` (conditional PackageReferences) |
| Quick run command | `godot --headless --scene-path res://Tests/TestRunner.tscn -- --run-tests=MoodSystemTests --quit-on-finish` |
| Full suite command | `godot --headless --scene-path res://Tests/TestRunner.tscn -- --run-tests --quit-on-finish` |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| MOOD-01 | Decay math produces correct values | unit | Quick run: `--run-tests=MoodSystemTests` | No -- Wave 0 |
| MOOD-02 | Tier transitions at correct thresholds | unit | Quick run: `--run-tests=MoodSystemTests` | No -- Wave 0 |
| MOOD-03 | Hysteresis prevents rapid oscillation | unit | Quick run: `--run-tests=MoodSystemTests` | No -- Wave 0 |
| MOOD-04 | Wish gain increments mood correctly | unit | Quick run: `--run-tests=MoodSystemTests` | No -- Wave 0 |
| MOOD-05 | Restore reconstructs state from save | unit | Quick run: `--run-tests=MoodSystemTests` | No -- Wave 0 |

### Sampling Rate
- **Per task commit:** Quick run command (MoodSystemTests only)
- **Per wave merge:** Full suite command (all test classes)
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `Tests/Mood/MoodSystemTests.cs` -- covers MOOD-01 through MOOD-05 (this IS the phase deliverable)
- [ ] Remove `Tests/Mood/.gitkeep` when MoodSystemTests.cs is added

*(Wave 0 gap IS the phase itself -- this phase creates the test file)*

## Sources

### Primary (HIGH confidence)
- `/workspace/Scripts/Happiness/MoodSystem.cs` -- complete source of system under test (126 lines)
- `/workspace/Scripts/Data/HappinessConfig.cs` -- all config defaults verified from source
- `/workspace/Scripts/Data/MoodTier.cs` -- enum definition verified
- `/workspace/Tests/Housing/HousingTests.cs` -- established test class pattern
- `/workspace/Tests/Integration/SingletonResetTests.cs` -- TestClass extension pattern
- `/workspace/Tests/Infrastructure/GameTestClass.cs` -- confirms POCO tests should NOT use this

### Secondary (MEDIUM confidence)
- [Shouldly docs - ShouldBe](https://docs.shouldly.org/documentation/equality/shouldbe) -- float tolerance overload confirmed
- Shouldly 4.3.0 NuGet package XML docs -- confirms ShouldBePositive for float/double types exist

### Tertiary (LOW confidence)
- None

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- all packages already in .csproj, existing tests prove the pattern
- Architecture: HIGH -- MoodSystem source code fully analyzed, all methods documented
- Pitfalls: HIGH -- decay formula computed, boundary conditions verified mathematically
- Pre-computed values: HIGH -- verified with both float64 and float32 precision; frame-rate independence confirmed

**Research date:** 2026-03-07
**Valid until:** 2026-04-07 (stable domain -- production code and test framework are fixed)
