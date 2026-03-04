# Phase 11: Economy and Arrival Tier Integration - Research

**Researched:** 2026-03-04
**Domain:** Godot 4.6 C# — wiring MoodTier enum into EconomyManager income and HappinessManager arrival probability
**Confidence:** HIGH

## Summary

Phase 11 is a targeted integration: replace two continuous-float inputs (`_currentHappiness` in EconomyManager and `Mood * ArrivalProbabilityScale` in HappinessManager) with discrete per-tier lookup tables configured through existing resource files. All scaffolding already exists — MoodTier enum, HappinessConfig/EconomyConfig resource classes, GameEvents.MoodTierChanged, and both manager singletons. No new architecture is needed; this phase is purely a wiring and configuration change.

The only design question left to Claude's discretion is field naming conventions in the config resources, whether to keep `SetHappiness()` as a deprecated stub or remove it, and how HappinessManager notifies EconomyManager on tier change (event subscription vs. direct call). All of these are low-risk choices with clear precedents in the existing codebase.

The income multiplier formula in `CalculateTickIncome()` and `GetIncomeBreakdown()` currently uses `_currentHappiness * (HappinessMultiplierCap - 1.0f)`. Phase 11 replaces this with a direct tier-indexed float (`_currentTierMultiplier`) stored in EconomyManager and set by `SetMoodTier()`. The `HappinessMultiplierCap` field in EconomyConfig becomes superseded by the five per-tier fields; it can remain for backward compatibility with `GetIncomeBreakdown()` until Phase 13 removes the old HUD.

**Primary recommendation:** Add five `[Export]` fields to each config resource following the existing `[ExportGroup]` pattern, replace the two formula calls with per-tier lookups, and wire HappinessManager to call `EconomyManager.Instance?.SetMoodTier(newTier)` wherever it currently calls `SetHappiness()`.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Arrival probability per tier:**
- Quiet: 15% per 60s check
- Cozy: 25% per 60s check
- Lively: 40% per 60s check
- Vibrant: 60% per 60s check
- Radiant: 75% per 60s check
- Five Inspector-tunable float fields in HappinessConfig (same pattern as tier thresholds)
- Expected time between arrivals: ~7 min at Quiet, ~1.3 min at Radiant

**Arrival check interval:**
- Fixed 60s timer interval — do NOT change interval by tier
- Tier change updates the in-memory probability only; no timer reset
- Next natural 60s tick picks up the new probability automatically

**Income multiplier per tier:**
- Quiet: 1.0x, Cozy: 1.1x, Lively: 1.2x, Vibrant: 1.3x, Radiant: 1.4x
- Five Inspector-tunable float fields in EconomyConfig (alongside existing HappinessMultiplierCap)
- EconomyConfig.HappinessMultiplierCap bumps from 1.3 to 1.4 (or can be removed in favor of per-tier fields)

**API integration:**
- Add `SetMoodTier(MoodTier tier)` to EconomyManager — replaces `SetHappiness(float)` for multiplier purposes
- HappinessManager subscribes to its own MoodTierChanged event (or calls directly on tier change) and calls `EconomyManager.Instance?.SetMoodTier(newTier)`
- `SetHappiness(float)` shim in EconomyManager can be removed once Phase 11 wires the new method
- Income multiplier lookup lives in EconomyManager (not HappinessManager)

### Claude's Discretion
- Exact field names in HappinessConfig and EconomyConfig for the per-tier values
- Whether to keep `SetHappiness()` as a deprecated stub or remove it entirely
- How HappinessManager calls EconomyManager on tier change (event subscription vs. direct call in OnWishFulfilled / _Process)

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| TIER-02 | Citizen arrival probability scales by current mood tier | Arrival check is in `HappinessManager.OnArrivalCheck()` — replace `Mood * ArrivalProbabilityScale` with per-tier lookup from HappinessConfig; five new float fields drive the lookup; `_lastReportedTier` is the read-source |
| TIER-03 | Economy income multiplier scales by current mood tier (1.0x to 1.4x) | `EconomyManager.CalculateTickIncome()` uses `_currentHappiness` float — replace with `_currentTierMultiplier` float set by new `SetMoodTier(MoodTier)` method; five new float fields in EconomyConfig drive the lookup |
</phase_requirements>

---

## Standard Stack

### Core
| Component | Version | Purpose | Why Standard |
|-----------|---------|---------|--------------|
| Godot 4.6 C# autoloads | 4.6 | Singleton managers accessed via `.Instance` | Established project pattern for all managers |
| `[GlobalClass]` Resource | 4.6 | Inspector-tunable config via `.tres` file | All config already uses this pattern (HappinessConfig, EconomyConfig) |
| `[ExportGroup]` + `[Export]` | 4.6 | Grouping Inspector fields | Existing pattern in HappinessConfig (Mood Decay, Baseline, Tier Thresholds, Hysteresis groups) |
| `MoodTier` enum (int 0-4) | Phase 10 | Discrete tier values for switch/lookup | Already defined; integer values enable array indexing if needed |
| `GameEvents.MoodTierChanged` | Phase 10 | Event bus for tier transitions | Already fires on every tier change; natural hook for EconomyManager update |

### Supporting
| Component | Version | Purpose | When to Use |
|-----------|---------|---------|-------------|
| `Math.Clamp` (not `MathF.Clamp`) | .NET 8 | Float clamping in Godot 4 C# | Established workaround — `MathF.Clamp` unavailable in this build env (Phase 10 decision) |
| `switch` expression on `MoodTier` | C# 8+ | Map tier to probability/multiplier | Clean, exhaustive, same pattern as `PromoteThreshold`/`DemoteThreshold` in MoodSystem |

### Alternatives Considered
| Standard Choice | Alternative | Why Standard Wins |
|-----------------|-------------|------------------|
| Five explicit `[Export]` float fields | Array `[Export]` field | Explicit fields appear individually in the Inspector and match the existing tier-threshold pattern in HappinessConfig |
| Direct call `SetMoodTier()` in `OnWishFulfilled` and `_Process` tier-change block | Subscribe EconomyManager to `GameEvents.MoodTierChanged` | Direct call is simpler, avoids a second subscriber with identical logic, and keeps economy update co-located with the tier-change detection code |

---

## Architecture Patterns

### How Tier Change Propagates Today

```
HappinessManager._Process()
  └── _moodSystem.Update(delta, lifetimeHappiness) → newTier
      └── if newTier != _lastReportedTier
          ├── _lastReportedTier = newTier
          └── GameEvents.Instance?.EmitMoodTierChanged(newTier, previousTier)

HappinessManager.OnWishFulfilled()
  └── _moodSystem.OnWishFulfilled() → newTier
      └── if newTier != _lastReportedTier
          ├── _lastReportedTier = newTier
          └── GameEvents.Instance?.EmitMoodTierChanged(newTier, previousTier)
      └── EconomyManager.Instance?.SetHappiness(_moodSystem.Mood)  // ← Phase 11 replaces this
```

Both tier-change paths are in HappinessManager. Phase 11 adds `EconomyManager.Instance?.SetMoodTier(newTier)` to the two tier-change blocks AND removes the `SetHappiness()` call from `OnWishFulfilled`.

### Pattern 1: Per-Tier Probability Lookup in OnArrivalCheck

Replace:
```csharp
// Current (Phase 10 compatible but not tier-driven)
float chance = Mood * ArrivalProbabilityScale;
```

With:
```csharp
// Phase 11: tier-based probability from config
float chance = ArrivalProbabilityForTier(_lastReportedTier);
```

Where the lookup helper reads from `Config`:
```csharp
private float ArrivalProbabilityForTier(MoodTier tier) => tier switch
{
    MoodTier.Quiet   => Config.ArrivalProbabilityQuiet,
    MoodTier.Cozy    => Config.ArrivalProbabilityCozy,
    MoodTier.Lively  => Config.ArrivalProbabilityLively,
    MoodTier.Vibrant => Config.ArrivalProbabilityVibrant,
    MoodTier.Radiant => Config.ArrivalProbabilityRadiant,
    _                => Config.ArrivalProbabilityQuiet,
};
```

The early return guard `if (Mood <= 0f)` can be replaced with a guard against Quiet-tier probability being zero, or simply removed since Quiet has 0.15 (never zero).

### Pattern 2: Per-Tier Multiplier in EconomyManager

Add field to EconomyManager:
```csharp
private float _currentTierMultiplier = 1.0f;  // default Quiet
```

New method (replaces SetHappiness for multiplier purposes):
```csharp
public void SetMoodTier(MoodTier tier)
{
    _currentTierMultiplier = IncomeMultiplierForTier(tier);
}

private float IncomeMultiplierForTier(MoodTier tier) => tier switch
{
    MoodTier.Quiet   => Config.IncomeMultQuiet,
    MoodTier.Cozy    => Config.IncomeMultCozy,
    MoodTier.Lively  => Config.IncomeMultLively,
    MoodTier.Vibrant => Config.IncomeMultVibrant,
    MoodTier.Radiant => Config.IncomeMultRadiant,
    _                => Config.IncomeMultQuiet,
};
```

Replace income formula in `CalculateTickIncome()`:
```csharp
// Old
float happinessMult = 1.0f + (_currentHappiness * (Config.HappinessMultiplierCap - 1.0f));
// New
float happinessMult = _currentTierMultiplier;
```

Same replacement in `GetIncomeBreakdown()` — both methods share the formula and both must be updated together.

### Pattern 3: HappinessConfig New Fields (ExportGroup)

Following the existing pattern:
```csharp
[ExportGroup("Arrival Probability")]

/// <summary>
/// Probability of a citizen arriving per 60s check at Quiet tier.
/// Default 0.15 — ~7 minute average wait between arrivals.
/// </summary>
[Export] public float ArrivalProbabilityQuiet  { get; set; } = 0.15f;
[Export] public float ArrivalProbabilityCozy   { get; set; } = 0.25f;
[Export] public float ArrivalProbabilityLively { get; set; } = 0.40f;
[Export] public float ArrivalProbabilityVibrant{ get; set; } = 0.60f;
[Export] public float ArrivalProbabilityRadiant{ get; set; } = 0.75f;
```

### Pattern 4: EconomyConfig New Fields (ExportGroup)

Following the existing pattern:
```csharp
[ExportGroup("Tier Income Multipliers")]

/// <summary>
/// Income multiplier at Quiet tier. Default 1.0 (no bonus).
/// </summary>
[Export] public float IncomeMultQuiet   { get; set; } = 1.0f;
[Export] public float IncomeMultCozy    { get; set; } = 1.1f;
[Export] public float IncomeMultLively  { get; set; } = 1.2f;
[Export] public float IncomeMultVibrant { get; set; } = 1.3f;
[Export] public float IncomeMultRadiant { get; set; } = 1.4f;
```

### Pattern 5: Wiring in HappinessManager

The tier-change notification to EconomyManager must happen in BOTH places where tier changes are detected:

```csharp
// In _Process():
if (newTier != previousTier)
{
    _lastReportedTier = newTier;
    GameEvents.Instance?.EmitMoodTierChanged(newTier, previousTier);
    EconomyManager.Instance?.SetMoodTier(newTier);   // ← ADD
}

// In OnWishFulfilled():
if (newTier != previousTier)
{
    _lastReportedTier = newTier;
    GameEvents.Instance?.EmitMoodTierChanged(newTier, previousTier);
    EconomyManager.Instance?.SetMoodTier(newTier);   // ← ADD
}
// Remove: EconomyManager.Instance?.SetHappiness(_moodSystem.Mood);  // ← REMOVE
```

Also update `RestoreState()` — it currently calls `EconomyManager.Instance?.SetHappiness(happiness)`. Replace with:
```csharp
EconomyManager.Instance?.SetMoodTier(_lastReportedTier);
```

### Pattern 6: .tres File Updates

Both `.tres` resource files need the new fields added. Godot will serialize new `[Export]` properties automatically when the resource is saved through the editor, but since defaults are set in code, the `.tres` files should be updated to persist the values explicitly. The planner should include tasks to update both resource files.

### Anti-Patterns to Avoid

- **Changing the timer interval by tier:** Locked decision — keep 60s fixed, change probability only.
- **Storing `_currentHappiness` for multiplier AND `_currentTierMultiplier`:** Remove `_currentHappiness` usage from income calculation once `SetMoodTier()` is wired. Keeping both creates divergence.
- **Calling `SetMoodTier()` only on wish fulfillment:** Tier also changes from `_moodSystem.Update()` in `_Process()`. Both paths must call `SetMoodTier()`.
- **Forgetting `GetIncomeBreakdown()`:** It duplicates the income formula from `CalculateTickIncome()`. Both must be updated.
- **Forgetting `RestoreState()`:** It calls `SetHappiness()`. After Phase 11, it must call `SetMoodTier()` instead.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Per-tier data storage | Dictionary or array | Five explicit `[Export]` float properties | Already the pattern used for tier thresholds; Inspector-visible, no boilerplate |
| Event routing for tier change | Custom event system | Existing `GameEvents.MoodTierChanged` | Already fires; would be redundant to add another channel |
| Tier-to-multiplier mapping | Runtime calculation | Config resource fields | Keeps balance data in `.tres` files, not in code |

---

## Common Pitfalls

### Pitfall 1: Dual Formula Sites
**What goes wrong:** `CalculateTickIncome()` and `GetIncomeBreakdown()` both contain `1.0f + (_currentHappiness * (Config.HappinessMultiplierCap - 1.0f))`. Updating only one leaves `GetIncomeBreakdown()` returning stale data — the tooltip shows different math than actual income.
**Why it happens:** The two methods duplicate the formula; it's easy to update the hot path and miss the display method.
**How to avoid:** Update both methods in the same task. Search for `_currentHappiness` before closing the task.
**Warning signs:** Income ticks correctly but HUD income breakdown shows wrong multiplier.

### Pitfall 2: RestoreState Calls Old SetHappiness
**What goes wrong:** After Phase 11, `HappinessManager.RestoreState()` still calls `EconomyManager.Instance?.SetHappiness(happiness)`. On load, the multiplier stays at whatever `_currentHappiness` was before (defaults to 0.0 → 1.0x multiplier), even if the restored tier is Vibrant.
**Why it happens:** `RestoreState()` is a save/load code path not exercised during normal gameplay testing.
**How to avoid:** Replace the `SetHappiness(happiness)` call in `RestoreState()` with `SetMoodTier(_lastReportedTier)` — `_lastReportedTier` is set from the restored mood before this call.
**Warning signs:** Freshly loaded save has wrong income rate until a wish is fulfilled.

### Pitfall 3: Arrival Guard Using Mood Float
**What goes wrong:** `OnArrivalCheck()` has an early return `if (Mood <= 0f)`. After Phase 11, Quiet tier still has 0.15 probability regardless of mood float. This guard is now meaningless but harmless — however if mood is very low (near 0 at game start), it would block arrivals even at Quiet tier.
**Why it happens:** The guard was designed for the old float-based system.
**How to avoid:** Remove the `if (Mood <= 0f) return;` guard entirely, or replace it with a check that the probability is nonzero (which it always is given the locked values).

### Pitfall 4: New Config Fields Not in .tres Files
**What goes wrong:** New `[Export]` fields with defaults in code work fine in-game, but the `.tres` files don't store them. If someone opens the resource in the Godot editor and saves it without changing anything, Godot may not persist the code-default values, resulting in zeros if the script defaults are ever changed.
**Why it happens:** `.tres` files only store values that differ from the resource's defaults, or that were explicitly saved.
**How to avoid:** After adding the fields, update both `.tres` files to include the new fields explicitly. This is a one-time serialization step.

### Pitfall 5: Missing MoodTier Namespace Import in EconomyManager
**What goes wrong:** `EconomyManager.cs` doesn't currently import `OrbitalRings.Data`. Adding `SetMoodTier(MoodTier tier)` requires the `MoodTier` type to be visible.
**Why it happens:** EconomyManager currently only uses `OrbitalRings.Data` for `RoomDefinition` — it may or may not have the using directive already.
**How to avoid:** Verify the using directives when adding `SetMoodTier()`. `using OrbitalRings.Data;` is already present (see line 4 of EconomyManager.cs — confirmed present).

---

## Code Examples

### Current Arrival Check (to be replaced)
```csharp
// Source: HappinessManager.cs line 318
// Current: probability is mood float * scale constant
float chance = Mood * ArrivalProbabilityScale;
if (GD.Randf() < chance)
```

### Current Income Formula (to be replaced in two places)
```csharp
// Source: EconomyManager.cs lines 130-131, 240-241
float happinessMult = 1.0f + (_currentHappiness * (Config.HappinessMultiplierCap - 1.0f));
return Mathf.RoundToInt((baseIncome + citizenIncome + workBonus) * happinessMult);
```

### Existing ExportGroup Pattern (to follow)
```csharp
// Source: HappinessConfig.cs lines 16-23
[ExportGroup("Mood Decay")]
[Export] public float DecayRate { get; set; } = 0.003662f;

[ExportGroup("Tier Thresholds")]
[Export] public float TierCozyThreshold { get; set; } = 0.10f;
```

### Existing Tier Switch Pattern (to follow)
```csharp
// Source: MoodSystem.cs lines 109-116
private float PromoteThreshold(MoodTier tier) => tier switch
{
    MoodTier.Quiet   => _config.TierCozyThreshold,
    MoodTier.Cozy    => _config.TierLivelyThreshold,
    MoodTier.Lively  => _config.TierVibrantThreshold,
    MoodTier.Vibrant => _config.TierRadiantThreshold,
    _                => float.MaxValue
};
```

### .tres File Field Format (to follow for new fields)
```
# Source: Resources/Happiness/default_happiness.tres (current format)
TierCozyThreshold = 0.1
TierLivelyThreshold = 0.3
TierVibrantThreshold = 0.55
TierRadiantThreshold = 0.8
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `happiness * ArrivalProbabilityScale` | Per-tier probability float from config | Phase 11 | Arrivals now step discretely with tier; 15% → 75% range gives meaningful progression |
| `_currentHappiness * (cap - 1.0f)` formula | Direct `_currentTierMultiplier` lookup | Phase 11 | Simpler formula, no cap arithmetic; multiplier is exactly what config says |
| `SetHappiness(float)` called from HappinessManager | `SetMoodTier(MoodTier)` called from HappinessManager | Phase 11 | Economy domain works in tier-space, not mood-float-space |

**Deprecated after Phase 11:**
- `ArrivalProbabilityScale` constant in HappinessManager: no longer used (replaced by per-tier config fields)
- `_currentHappiness` field in EconomyManager: no longer used for income (replaced by `_currentTierMultiplier`). Note: `SetHappiness()` still sets it; the field can be removed once `SetHappiness()` is removed.
- `EconomyConfig.HappinessMultiplierCap`: superseded by five per-tier multiplier fields; can remain in the `.tres` but is no longer read by income calculation.

---

## Open Questions

1. **Whether to remove `SetHappiness()` entirely or keep as deprecated stub**
   - What we know: CONTEXT.md says it "can be removed" — discretion left to Claude
   - What's unclear: SaveManager may call it during restore (checking `RestoreState()` confirms HappinessManager calls it, not SaveManager directly)
   - Recommendation: Remove it entirely. The only call site is `HappinessManager.RestoreState()` and `HappinessManager.OnWishFulfilled()` — both will be updated in Phase 11. Removing it enforces the new API and prevents accidental future use. Add an XML doc comment on `SetMoodTier()` noting it replaces `SetHappiness()`.

2. **`_currentHappiness` field fate in EconomyManager**
   - What we know: Once `SetHappiness()` is removed, nothing sets `_currentHappiness`; it can be deleted
   - What's unclear: Nothing — it's safe to remove with `SetHappiness()`
   - Recommendation: Remove the field and method together in a single task.

---

## Validation Architecture

`nyquist_validation` key is absent from `.planning/config.json` — treated as enabled.

### Test Framework

This is a Godot 4.6 C# project. There is no existing test framework or test directory. The logic being tested (tier-to-multiplier/probability lookups and the wiring calls) lives in POCO classes and Autoload methods that depend on Godot singletons, making pure unit tests in isolation impractical without Godot's runtime.

| Property | Value |
|----------|-------|
| Framework | None currently (Godot 4 C# — GdUnit4 is the standard but not installed) |
| Config file | None — see Wave 0 |
| Quick run command | Manual: launch game, verify income rate and arrival frequency change with tier |
| Full suite command | Manual gameplay verification (see below) |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| TIER-02 | Arrival probability uses tier value (0.15 at Quiet, 0.75 at Radiant) | Manual smoke | Launch game, trigger tier change, observe arrival frequency change | No test file — manual only |
| TIER-03 | Income multiplier uses tier value (1.0x at Quiet, 1.4x at Radiant) | Manual smoke | Launch game, check income tick amount changes immediately on tier transition | No test file — manual only |

Both requirements verify runtime behavior through Godot's node graph — automated verification requires either GdUnit4 (not installed) or a standalone C# test project that mocks the Godot types. Manual verification is sufficient for this phase given the codebase has no existing test infrastructure.

### Sampling Rate
- **Per task commit:** Verify the specific changed behavior manually (e.g., after adding `SetMoodTier`, confirm income formula reads `_currentTierMultiplier`)
- **Per wave merge:** Launch game, trigger a tier change, confirm both income and arrival probability visibly change
- **Phase gate:** All three manual verification steps in success criteria pass before `/gsd:verify-work`

### Wave 0 Gaps
- No automated test infrastructure exists for this phase. Manual verification via gameplay is the validation path.
- If automated tests are desired: install GdUnit4 (`dotnet add package GdUnit4`) and create `Tests/EconomyTierTests.cs` and `Tests/ArrivalTierTests.cs`. This is out of scope for Phase 11.

*(No automated test framework — existing infrastructure covers zero phase requirements automatically)*

---

## Sources

### Primary (HIGH confidence)
- Direct code inspection of `/workspace/Scripts/Autoloads/HappinessManager.cs` — arrival check logic, tier change detection, call sites for SetHappiness
- Direct code inspection of `/workspace/Scripts/Autoloads/EconomyManager.cs` — income formula, SetHappiness signature, CalculateTickIncome and GetIncomeBreakdown
- Direct code inspection of `/workspace/Scripts/Data/HappinessConfig.cs` — ExportGroup pattern, existing field names
- Direct code inspection of `/workspace/Scripts/Data/EconomyConfig.cs` — existing Income group, HappinessMultiplierCap field
- Direct code inspection of `/workspace/Scripts/Data/MoodTier.cs` — enum values 0-4
- Direct code inspection of `/workspace/Scripts/Autoloads/GameEvents.cs` — MoodTierChanged event signature
- Direct code inspection of `/workspace/Scripts/Happiness/MoodSystem.cs` — tier switch expression pattern
- `/workspace/Resources/Happiness/default_happiness.tres` — current field values and format
- `/workspace/Resources/Economy/default_economy.tres` — current field values and format
- `/workspace/.planning/phases/11-economy-and-arrival-tier-integration/11-CONTEXT.md` — locked decisions

### Secondary (MEDIUM confidence)
- Phase 10 STATE.md entry: `Math.Clamp` (not `MathF.Clamp`) required in Godot C# build environment

### Tertiary (LOW confidence)
- None

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all components verified by direct code inspection; no external dependencies introduced
- Architecture: HIGH — patterns derived directly from existing codebase conventions; no novel patterns required
- Pitfalls: HIGH — identified from direct code reading of the two formula sites that must both be updated, and from the RestoreState call path

**Research date:** 2026-03-04
**Valid until:** 2026-06-04 (stable — no external dependencies; only changes if project code changes)
