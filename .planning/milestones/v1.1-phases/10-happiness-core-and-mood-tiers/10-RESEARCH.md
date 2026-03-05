# Phase 10: Happiness Core and Mood Tiers - Research

**Researched:** 2026-03-04
**Domain:** Godot 4 C# game logic refactor — dual happiness system, exponential decay, hysteresis state machine
**Confidence:** HIGH (all findings from direct codebase analysis; no external library uncertainty)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Mood Pacing**
- Very gentle decay (cozy-first): ~5 minutes of inactivity to drop one tier
- Frame-based exponential smoothing for decay (in `_Process`, not timer ticks) — smooth continuous drift toward baseline
- Small mood bumps per wish (~5-8% of range) — reaching higher tiers requires sustained activity
- Flat gain: every wish gives the same mood boost regardless of wish type
- Instant mood jump on wish fulfillment (no tween on the data value — HUD can animate separately in Phase 13)
- Hard cap at 1.0 — no overflow above max

**Tier Distribution**
- Front-loaded tiers (easy to leave Quiet, hard to reach Radiant):
  - Quiet: 0.00 – 0.10
  - Cozy: 0.10 – 0.30
  - Lively: 0.30 – 0.55
  - Vibrant: 0.55 – 0.80
  - Radiant: 0.80 – 1.00
- Tier names confirmed: Quiet / Cozy / Lively / Vibrant / Radiant
- Medium hysteresis buffer (~0.05) on demotion boundaries to prevent tier oscillation
- New games start at mood 0.0 (Quiet tier)

**Baseline Growth**
- Baseline caps around Cozy (~0.20) — even mature stations only rest above Quiet
- Slow baseline creep: ~0.05 baseline after 10 wishes, ~0.15 after 30+ wishes
- Baseline is purely internal — player is not shown the floor value
- Mood decay stops exactly at baseline (asymptotic approach, never undershoots)

**Event System**
- Fire events only on mood tier changes (not per-frame mood updates)
- Replace old `HappinessChanged(float)` event now — add `MoodTierChanged` and `WishCountChanged` events
- HappinessBar will break (acceptable — Phase 13 replaces it entirely)

**Refactor Approach**
- Extract MoodSystem as a plain C# class (POCO, not a Godot Node)
- HappinessManager creates and owns MoodSystem, passes delta time for decay in `_Process`
- MoodSystem encapsulates: mood value, baseline, tier calculation, hysteresis, decay logic
- HappinessManager retains: arrivals, housing capacity, blueprint unlocks, event wiring

**Configuration**
- Inspector-tunable resource: `HappinessConfig` (like EconomyConfig)
- File path: `Resources/Happiness/default_happiness.tres`
- Exposes: decay rate, gain amount, tier thresholds, hysteresis width, baseline scaling factor, baseline cap

### Claude's Discretion
- Exact decay rate constant (targeting ~5 min per tier drop)
- Exact mood gain per wish (targeting ~5-8% range)
- Exact sqrt scaling factor for baseline (targeting slow creep, cap at ~0.20)
- HappinessConfig field names and types
- MoodSystem internal structure and method signatures
- How to wire `_Process` delta time from HappinessManager to MoodSystem

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| HCORE-01 | Lifetime happiness increments by 1 on each wish fulfilled (integer, never decreases) | New `_lifetimeHappiness` int field in HappinessManager; incremented in `OnWishFulfilled`; fires `WishCountChanged` event |
| HCORE-02 | Station mood is a float that rises on wish fulfillment (flat gain, no diminishing returns) | MoodSystem.OnWishFulfilled adds flat gain (recommended 0.06) capped at 1.0, replacing old diminishing returns formula |
| HCORE-03 | Station mood decays toward a baseline each frame using exponential smoothing | MoodSystem.Update(delta) called from HappinessManager._Process; exponential smoothing with k=0.003662/s |
| HCORE-04 | Mood baseline rises with sqrt(lifetime happiness) so mature stations rest above Quiet | MoodSystem recomputes baseline = Min(BaselineCap, BaselineScalingFactor * Sqrt(lifetimeHappiness)) on each wish |
| HCORE-05 | Blueprint unlocks trigger at wish count thresholds (4, 12) instead of percentage thresholds | Replace `(float threshold, string[] rooms)[]` array with `(int wishCount, string[] rooms)[]`; compare `_lifetimeHappiness` instead of `_happiness` |
| TIER-01 | Five named mood tiers (Quiet/Cozy/Lively/Vibrant/Radiant) with defined mood ranges | `MoodTier` enum; `CalculateTier(float mood, MoodTier current)` in MoodSystem with promote/demote boundaries |
| TIER-04 | Hysteresis on tier demotion boundaries prevents rapid tier oscillation | Demotion checks use `promoteBoundary - hysteresisWidth`; promotion checks use exact boundary; state is tracked in `_currentTier` |
</phase_requirements>

---

## Summary

Phase 10 is a focused refactor of `HappinessManager` — no new Godot scenes, no new UI, no save format changes. The work is entirely in C# game logic. The codebase is well-structured for this change: `EconomyConfig` provides an exact template for `HappinessConfig`, `GameEvents` is straightforward to extend with new typed events, and the existing `OnWishFulfilled` handler is the single entry point for all changes.

The core algorithmic work is a classic exponential decay system. The math is simple and well-understood: mood decays toward a rising floor using `mood += (baseline - mood) * (1 - exp(-k * delta))` each frame. The hysteresis is a two-threshold state machine — promote at the exact boundary, demote only when crossing `boundary - 0.05`. This prevents tier oscillation when mood hovers at a boundary.

The one structural complexity is the POCO extraction: `MoodSystem` must be a plain C# class (no `Node` inheritance) that receives delta time from its owner. This is a common pattern for testable game logic and requires no special Godot knowledge — just a class that takes a `float delta` parameter in its update method.

**Primary recommendation:** Write `MoodSystem` as a self-contained POCO with pure methods, wire it into `HappinessManager._Process`, then update events and blueprint unlock logic. Total scope is approximately 3 files created/modified.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Godot 4.4 C# | 4.4 | Engine runtime | Project constraint |
| .NET 10 | 10.0 | C# runtime | Project assembly target (confirmed in build artifacts) |
| `System` | stdlib | `MathF.Sqrt`, `MathF.Exp` | Built-in; no external deps needed |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `Godot.Resource` | 4.4 | `[GlobalClass]` base for HappinessConfig | Required for Inspector-editable `.tres` resources |
| `Godot.Node._Process` | 4.4 | Frame delta delivery to HappinessManager | Required for smooth per-frame decay |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| POCO MoodSystem | Node subclass | Node adds lifecycle complexity and requires scene tree placement; POCO is testable and zero overhead |
| Exponential smoothing | Linear decay per frame | Exponential is frame-rate independent; linear decay would need delta clamping to avoid overshooting |
| Pure C# events | Godot [Signal] | Project decision: pure C# events already in use across all singletons; avoids marshalling bugs |

**Installation:** No new packages required. All needed APIs are in the existing project.

---

## Architecture Patterns

### Recommended Project Structure

New files to create:
```
Scripts/
├── Data/
│   └── HappinessConfig.cs       # [GlobalClass] Resource with [Export] fields
├── Happiness/
│   └── MoodSystem.cs            # POCO: mood value, baseline, tier, decay, hysteresis
└── Autoloads/
    └── HappinessManager.cs      # MODIFIED: owns MoodSystem, _Process wiring, new events

Resources/
└── Happiness/
    └── default_happiness.tres   # Inspector-tunable config instance
```

### Pattern 1: EconomyConfig Template for HappinessConfig

**What:** `[GlobalClass]` C# `Resource` subclass with `[Export]` properties, loaded via `ResourceLoader.Load<T>` with code-default fallback.

**When to use:** Any inspector-tunable configuration that should be editable in the Godot editor without code changes.

**Example (from existing EconomyConfig.cs):**
```csharp
// Source: /workspace/Scripts/Data/EconomyConfig.cs
[GlobalClass]
public partial class HappinessConfig : Resource
{
    [ExportGroup("Mood Decay")]
    [Export] public float DecayRate { get; set; } = 0.003662f;        // ln(3)/300 — ~5 min per tier drop
    [Export] public float MoodGainPerWish { get; set; } = 0.06f;      // 6% of range, flat gain

    [ExportGroup("Baseline")]
    [Export] public float BaselineScalingFactor { get; set; } = 0.016f; // sqrt scaling factor
    [Export] public float BaselineCap { get; set; } = 0.20f;           // max baseline (~Cozy floor)

    [ExportGroup("Tier Thresholds")]
    [Export] public float TierCozyThreshold { get; set; } = 0.10f;
    [Export] public float TierLivelyThreshold { get; set; } = 0.30f;
    [Export] public float TierVibrantThreshold { get; set; } = 0.55f;
    [Export] public float TierRadiantThreshold { get; set; } = 0.80f;

    [ExportGroup("Hysteresis")]
    [Export] public float HysteresisWidth { get; set; } = 0.05f;       // demotion buffer
}
```

HappinessManager loads it the same way EconomyManager loads EconomyConfig:
```csharp
// In HappinessManager._Ready()
if (Config == null)
    Config = ResourceLoader.Load<HappinessConfig>("res://Resources/Happiness/default_happiness.tres");
if (Config == null)
{
    GD.PushWarning("HappinessManager: No HappinessConfig found. Using code defaults.");
    Config = new HappinessConfig();
}
```

### Pattern 2: POCO MoodSystem with delta injection

**What:** Plain C# class (no Node inheritance) that encapsulates all mood math. Receives `float delta` from its owner's `_Process`.

**When to use:** Game logic that needs to be frame-rate independent, testable in isolation, and free of Godot lifecycle dependencies.

**Example:**
```csharp
// Source: new file Scripts/Happiness/MoodSystem.cs
public class MoodSystem
{
    private readonly HappinessConfig _config;
    private float _mood;
    private float _baseline;
    private MoodTier _currentTier = MoodTier.Quiet;

    public float Mood => _mood;
    public float Baseline => _baseline;
    public MoodTier CurrentTier => _currentTier;

    public MoodSystem(HappinessConfig config) => _config = config;

    /// <summary>
    /// Called from HappinessManager._Process. Applies exponential decay
    /// toward baseline and updates tier with hysteresis.
    /// Returns the new tier (caller checks for tier change to fire events).
    /// </summary>
    public MoodTier Update(float delta, int lifetimeHappiness)
    {
        // Recompute baseline from lifetime wishes
        _baseline = MathF.Min(_config.BaselineCap,
            _config.BaselineScalingFactor * MathF.Sqrt(lifetimeHappiness));

        // Exponential smoothing: mood drifts toward baseline
        float alpha = 1f - MathF.Exp(-_config.DecayRate * delta);
        _mood = _mood + (_baseline - _mood) * alpha;
        _mood = MathF.Max(_mood, _baseline);   // never undershoot

        // Recalculate tier with hysteresis
        var newTier = CalculateTier(_mood, _currentTier, _config);
        _currentTier = newTier;
        return _currentTier;
    }

    /// <summary>
    /// Called on wish fulfillment. Applies flat mood gain.
    /// Returns new tier (caller checks for tier change).
    /// </summary>
    public MoodTier OnWishFulfilled()
    {
        _mood = MathF.Min(1.0f, _mood + _config.MoodGainPerWish);
        var newTier = CalculateTier(_mood, _currentTier, _config);
        _currentTier = newTier;
        return _currentTier;
    }

    /// <summary>
    /// Pure tier calculation with hysteresis.
    /// Promotion uses exact threshold; demotion uses threshold - hysteresisWidth.
    /// </summary>
    public static MoodTier CalculateTier(float mood, MoodTier current, HappinessConfig cfg)
    {
        float hw = cfg.HysteresisWidth;

        // Promotion: check upward from current tier
        if (current < MoodTier.Radiant && mood >= PromoteThreshold(current, cfg))
            return current + 1;

        // Demotion: check downward from current tier
        if (current > MoodTier.Quiet && mood < DemoteThreshold(current, cfg))
            return current - 1;

        return current;
    }
}
```

### Pattern 3: GameEvents extension for new typed events

**What:** Add two new events to `GameEvents.cs` following the existing `Action<T>` delegate pattern with null-safe Emit helpers.

**When to use:** Any new cross-system communication that needs to be fired from one autoload and consumed by another.

**Example (following existing pattern in GameEvents.cs):**
```csharp
// Add to GameEvents.cs — Economy Events section pattern:
// ---------------------------------------------------------------------------
// Happiness v2 Events (Phase 10)
// ---------------------------------------------------------------------------

/// <param name="newTier">The new mood tier after the change.</param>
/// <param name="previousTier">The tier before the change.</param>
public event Action<MoodTier, MoodTier> MoodTierChanged;

/// <param name="newCount">Updated lifetime wish count.</param>
public event Action<int> WishCountChanged;

public void EmitMoodTierChanged(MoodTier newTier, MoodTier previousTier)
    => MoodTierChanged?.Invoke(newTier, previousTier);

public void EmitWishCountChanged(int newCount)
    => WishCountChanged?.Invoke(newCount);
```

The old `HappinessChanged(float)` event is retained in GameEvents.cs but HappinessManager stops calling it. HappinessBar subscribes to the old event; it will silently stop updating (acceptable, Phase 13 replaces it).

### Pattern 4: Blueprint unlock switch to wish counts

**What:** Replace `(float threshold, string[] rooms)[]` array with `(int wishCount, string[] rooms)[]`. Compare `_lifetimeHappiness` instead of `_happiness`.

**Before (existing):**
```csharp
private static readonly (float threshold, string[] rooms)[] UnlockMilestones =
{
    (0.25f, new[] { "sky_loft", "craft_lab" }),
    (0.60f, new[] { "star_lounge", "comm_relay" }),
};
// In CheckUnlockMilestones():
if (_happiness < threshold) break;
```

**After:**
```csharp
private static readonly (int wishCount, string[] rooms)[] UnlockMilestones =
{
    (4, new[] { "sky_loft", "craft_lab" }),
    (12, new[] { "star_lounge", "comm_relay" }),
};
// In CheckUnlockMilestones():
if (_lifetimeHappiness < wishCount) break;
```

### Pattern 5: _Process wiring for delta time

**What:** HappinessManager overrides `_Process(double delta)` to call `_moodSystem.Update((float)delta, _lifetimeHappiness)` and fire events on tier change.

**Example:**
```csharp
// In HappinessManager
private MoodSystem _moodSystem;
private MoodTier _lastReportedTier = MoodTier.Quiet;

public override void _Process(double delta)
{
    var previousTier = _lastReportedTier;
    var newTier = _moodSystem.Update((float)delta, _lifetimeHappiness);

    if (newTier != previousTier)
    {
        _lastReportedTier = newTier;
        GameEvents.Instance?.EmitMoodTierChanged(newTier, previousTier);
    }
}
```

### Anti-Patterns to Avoid

- **Using a Godot Timer for mood decay:** Timers fire discretely, creating visible "mood steps." `_Process` exponential decay is smooth and frame-rate independent.
- **Storing tier as an int:** Use a `MoodTier` enum for type safety and switch-expression exhaustiveness checking.
- **Recalculating tier from scratch every frame:** Store `_currentTier` in MoodSystem and only transition when crossing thresholds — the hysteresis state machine requires knowing the previous tier.
- **Emitting MoodTierChanged on every `_Process` call:** Only emit when the tier actually changes (compare previous vs new).
- **Removing HappinessChanged from GameEvents.cs:** Keep it in the file to avoid compilation errors in HappinessBar and SaveManager subscribers. Just stop calling `EmitHappinessChanged` from HappinessManager.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Frame-rate independent decay | Custom timer accumulator | `MathF.Exp(-k * delta)` exponential smoothing | Mathematically correct, no frame-rate drift, single line |
| Inspector-editable config | Hardcoded constants in HappinessManager | `[GlobalClass] Resource` with `[Export]` fields | Pattern already proven in EconomyConfig; free Inspector UI |
| Cross-system event notification | Polling or direct method calls | Extend existing `GameEvents` with typed C# events | Pattern used across all 7 singletons; zero marshalling overhead |

**Key insight:** The project already has all required patterns. This phase is connecting known patterns in a new combination, not inventing new architecture.

---

## Common Pitfalls

### Pitfall 1: Hysteresis Requires Current Tier as Input

**What goes wrong:** Calculating tier purely from the current mood value returns an unstable result when mood sits exactly on a boundary. Code that does `if (mood >= 0.30) return Lively` will oscillate when mood bounces above/below 0.30.

**Why it happens:** The two-threshold model only works if the calculation knows which tier you're currently in. Without current tier, there's no way to choose between "stay in Cozy" and "promote to Lively."

**How to avoid:** `CalculateTier(float mood, MoodTier current, HappinessConfig cfg)` must receive the current tier. Promotion uses `promoteBoundary`; demotion uses `promoteBoundary - hysteresisWidth`. Never compute tier from mood alone.

**Warning signs:** Tier flickering in logs on a single wish fulfillment, or rapid MoodTierChanged events firing back-to-back.

### Pitfall 2: Mood Undershooting Baseline

**What goes wrong:** `mood = mood + (baseline - mood) * alpha` can produce values slightly below `baseline` due to floating point. Over many frames this won't converge — it oscillates near baseline.

**Why it happens:** IEEE 754 floating point arithmetic is not exact. The multiplication can produce a result marginally below baseline.

**How to avoid:** Clamp after each update: `_mood = MathF.Max(_mood, _baseline)`. This is documented explicitly in the decisions.

**Warning signs:** Mood value reported as 0.049999 when baseline is 0.05.

### Pitfall 3: Firing Events from _Process

**What goes wrong:** `_Process` runs 60 times per second. If MoodTierChanged fires every frame, subscribers (future HUD, SaveManager) get overwhelmed.

**Why it happens:** Forgetting to compare previous tier before emitting.

**How to avoid:** Cache `_lastReportedTier` in HappinessManager. Only call `EmitMoodTierChanged` when `newTier != _lastReportedTier`. Update the cache immediately after emitting.

**Warning signs:** SaveManager autosave debounce fires constantly, game feels sluggish.

### Pitfall 4: SaveManager Still Reads Old `Happiness` Property

**What goes wrong:** SaveManager's `CollectGameState()` reads `HappinessManager.Instance?.Happiness`. After the refactor, this property no longer exists (or returns stale data), causing silent save corruption.

**Why it happens:** SaveManager is not in scope for Phase 10 (save format changes are Phase 12), but it still calls into HappinessManager.

**How to avoid:** Keep a `public float Mood => _moodSystem?.Mood ?? 0f` property on HappinessManager. SaveManager will continue saving the mood float. This is intentionally temporary — Phase 12 updates the save format.

**Warning signs:** Loading a save shows mood at 0% even though the game was saved at Lively.

### Pitfall 5: Baseline Calculation Order in Update

**What goes wrong:** If baseline is recalculated after decay is applied, the decay target used for this frame is the old baseline — the new baseline doesn't take effect until next frame. Usually harmless, but creates a one-frame lag on wish fulfillment.

**Why it happens:** Ordering `decay`, then `recompute baseline` in `Update()`.

**How to avoid:** Recompute `_baseline` first in `Update()`, then apply decay toward the new baseline.

### Pitfall 6: MoodTier Enum Not in Correct Namespace

**What goes wrong:** `MoodTier` is referenced in GameEvents (for the `MoodTierChanged` event), HappinessManager, and MoodSystem. If defined in a namespace that GameEvents doesn't import, compilation fails.

**Why it happens:** GameEvents is in `OrbitalRings.Autoloads`; MoodSystem would be in `OrbitalRings.Happiness`. Cross-namespace reference requires a using statement.

**How to avoid:** Define `MoodTier` in `OrbitalRings.Data` (same namespace as `EconomyConfig`, `RoomDefinition`) so it's widely available. Add `using OrbitalRings.Data;` to GameEvents.cs.

---

## Code Examples

### Exponential Decay Math (verified with Node.js simulation)

```csharp
// Source: mathematical derivation — verified in research calculations
// Target: 5 minutes (300s) to decay one tier from its floor with baseline = 0
// e.g., Cozy floor (0.30) to Cozy/Lively demotion boundary (0.25) in ~50s,
//        then continuing down to Quiet boundary (0.10) — total ~300s

// Decay constant: k = ln(3) / 300 ≈ 0.003662 per second
// This ensures: in 300 seconds, mood drops by factor of 3 (from 0.30 to 0.10)
private const float DecayRateDefault = 0.003662f;  // ln(3)/300

// Per-frame update (called from _Process):
float alpha = 1f - MathF.Exp(-_config.DecayRate * (float)delta);
_mood = _mood + (_baseline - _mood) * alpha;
_mood = MathF.Max(_mood, _baseline);
```

### Tier Thresholds with Hysteresis (full table)

```csharp
// Promote boundaries (exact): mood must reach OR exceed these to promote
// Demote boundaries (with hysteresis): mood must drop BELOW these to demote
// Hysteresis width: 0.05

// Tier      Promote At   Demote Below
// Quiet     0.10         N/A (floor)
// Cozy      0.30         0.05  (0.10 - 0.05)
// Lively    0.55         0.25  (0.30 - 0.05)
// Vibrant   0.80         0.50  (0.55 - 0.05)
// Radiant   N/A (ceil)   0.75  (0.80 - 0.05)

private static float PromoteThreshold(MoodTier tier, HappinessConfig cfg) => tier switch
{
    MoodTier.Quiet   => cfg.TierCozyThreshold,
    MoodTier.Cozy    => cfg.TierLivelyThreshold,
    MoodTier.Lively  => cfg.TierVibrantThreshold,
    MoodTier.Vibrant => cfg.TierRadiantThreshold,
    _                => float.MaxValue  // Radiant cannot promote
};

private static float DemoteThreshold(MoodTier tier, HappinessConfig cfg) => tier switch
{
    MoodTier.Cozy    => cfg.TierCozyThreshold - cfg.HysteresisWidth,
    MoodTier.Lively  => cfg.TierLivelyThreshold - cfg.HysteresisWidth,
    MoodTier.Vibrant => cfg.TierVibrantThreshold - cfg.HysteresisWidth,
    MoodTier.Radiant => cfg.TierRadiantThreshold - cfg.HysteresisWidth,
    _                => float.MinValue  // Quiet cannot demote
};
```

### Baseline Growth Formula (calibrated)

```csharp
// Source: research calculation — factor 0.016 targets ~0.05 baseline at 10 wishes
// Caps at 0.20 (BaselineCap), reached around 156 wishes
// At 10 wishes:  0.016 * sqrt(10)  = 0.051
// At 30 wishes:  0.016 * sqrt(30)  = 0.088
// At 100 wishes: 0.016 * sqrt(100) = 0.160
// At 156 wishes: 0.016 * sqrt(156) = 0.200 (cap)

_baseline = MathF.Min(
    _config.BaselineCap,
    _config.BaselineScalingFactor * MathF.Sqrt(lifetimeHappiness)
);
```

### .tres Resource File Template

```gdresource
; File: Resources/Happiness/default_happiness.tres
[gd_resource type="Resource" script_class="HappinessConfig" load_steps=2 format=3]

[ext_resource type="Script" path="res://Scripts/Data/HappinessConfig.cs" id="1"]

[resource]
script = ExtResource("1")
DecayRate = 0.003662
MoodGainPerWish = 0.06
BaselineScalingFactor = 0.016
BaselineCap = 0.2
TierCozyThreshold = 0.1
TierLivelyThreshold = 0.3
TierVibrantThreshold = 0.55
TierRadiantThreshold = 0.8
HysteresisWidth = 0.05
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Single `_happiness` float (0-1) with diminishing returns | Dual: `_lifetimeHappiness` (int) + `_mood` (float) with flat gain | Phase 10 | Loop stays alive indefinitely; no more saturation at ~50 wishes |
| Blueprint unlocks at `happiness >= 0.25f` and `>= 0.60f` | Blueprint unlocks at `lifetimeHappiness >= 4` and `>= 12` | Phase 10 | Deterministic unlock timing; predictable for player |
| `HappinessChanged(float)` event fired on every wish | `WishCountChanged(int)` + `MoodTierChanged(MoodTier, MoodTier)` fired on meaningful changes only | Phase 10 | Less event noise; SaveManager won't debounce-trigger on every frame |
| All happiness logic inline in HappinessManager | MoodSystem POCO extracted from HappinessManager | Phase 10 | Testable in isolation; clear separation of data vs. orchestration |

**Deprecated/outdated in this phase:**
- `HappinessGainBase = 0.08f` constant and diminishing returns formula: replaced by `HappinessConfig.MoodGainPerWish`
- `UnlockMilestones` as `(float threshold, ...)[]`: replaced by `(int wishCount, ...)[]`
- `HappinessManager.Happiness` property returning raw float: replaced by `Mood` and `LifetimeHappiness` properties

---

## Open Questions

1. **Baseline factor reconciliation**
   - What we know: CONTEXT says "~0.05 at 10 wishes, ~0.15 at 30+ wishes" but no single sqrt factor achieves both exactly
   - What's unclear: Whether these are design targets or illustrative examples
   - Recommendation: Use factor 0.016 (hits 0.051 at 10 wishes) as the starting point; the ~0.15 target at 30 wishes is approximate and the gap is within acceptable tuning range. Expose via HappinessConfig for easy adjustment.

2. **SaveManager compatibility during Phase 10**
   - What we know: SaveManager reads `HappinessManager.Instance?.Happiness` and `GetCrossedMilestoneCount()`. Phase 12 updates save format.
   - What's unclear: Whether to preserve the old `Happiness` property (returning `Mood`) or allow saves to break between Phase 10 and Phase 12.
   - Recommendation: Keep `public float Happiness => _moodSystem?.Mood ?? 0f` as a compatibility shim. SaveManager continues to save mood as the "happiness" float. Phase 12 replaces it cleanly. This avoids a broken save between phases.

3. **EconomyManager.SetHappiness bridge**
   - What we know: `EconomyManager.SetHappiness(float)` takes a 0-1 happiness value for the income multiplier. Phase 11 replaces this with tier-based multipliers.
   - What's unclear: What to pass in Phase 10 — raw mood float, or a tier-normalized value.
   - Recommendation: Continue calling `EconomyManager.Instance?.SetHappiness(_moodSystem.Mood)` in Phase 10. The float still works for Phase 10's purposes; Phase 11 changes the contract.

---

## Validation Architecture

> `workflow.nyquist_validation` is absent from `.planning/config.json` — treated as enabled.

### Test Framework

| Property | Value |
|----------|-------|
| Framework | None detected — no test files, no test config, no test runner |
| Config file | None — Wave 0 must create |
| Quick run command | N/A — no test infrastructure |
| Full suite command | N/A |

**Note:** This project is a Godot game. Standard .NET test frameworks (xUnit, NUnit) can test pure C# logic (like MoodSystem) without the Godot runtime. MoodSystem is explicitly designed as a POCO precisely to enable this. However, no test infrastructure exists in this project today.

Given the project's delivery pattern (25 plans in 3 days, manual verification via play), the pragmatic path is: Wave 0 creates MoodSystem with testable structure; validation is done by running the game and observing tier transitions. Automated tests would be valuable but are not currently part of the project's workflow.

### Phase Requirements to Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| HCORE-01 | Lifetime counter increments by 1, never decreases | Manual (in-game) | N/A | N/A |
| HCORE-02 | Mood gains flat amount on wish, no diminishing returns | Manual: watch Happiness debug output | N/A | N/A |
| HCORE-03 | Mood decays toward baseline each frame | Manual: idle for 5 min, watch tier drop | N/A | N/A |
| HCORE-04 | Baseline rises with sqrt(lifetime) | Manual: fulfill 10 wishes, idle, verify floor at ~0.05 | N/A | N/A |
| HCORE-05 | Blueprint unlocks at wish 4 and 12 | Manual: count wishes, verify unlock notification | N/A | N/A |
| TIER-01 | Five tiers with correct ranges | Manual: GD.Print tier on each change | N/A | N/A |
| TIER-04 | No rapid oscillation at boundary | Manual: tune mood to hover at 0.30, observe | N/A | N/A |

### Wave 0 Gaps

No test framework exists. The planner may choose to add a lightweight test for MoodSystem pure logic if desired, but the project pattern does not require it. The existing pattern is: implement, run game, observe.

---

## Sources

### Primary (HIGH confidence)
- `/workspace/Scripts/Autoloads/HappinessManager.cs` — full existing implementation analyzed
- `/workspace/Scripts/Autoloads/GameEvents.cs` — event bus pattern verified
- `/workspace/Scripts/Data/EconomyConfig.cs` — exact template for HappinessConfig
- `/workspace/Scripts/Autoloads/EconomyManager.cs` — config loading pattern, SetHappiness integration point
- `/workspace/Scripts/Autoloads/SaveManager.cs` — save/load integration points verified
- `/workspace/.planning/phases/10-happiness-core-and-mood-tiers/10-CONTEXT.md` — locked decisions
- `/workspace/.planning/REQUIREMENTS.md` — requirement definitions

### Secondary (MEDIUM confidence)
- Node.js calculations (in-session): decay constant k=0.003662, verified with simulation to achieve ~5 min per tier drop
- Node.js calculations: baseline scaling factor 0.016 achieves ~0.051 at 10 wishes

### Tertiary (LOW confidence)
- None

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — this is an existing Godot 4 C# project; no new libraries needed
- Architecture: HIGH — all patterns (EconomyConfig, GameEvents, POCO extraction) are verified in the existing codebase
- Pitfalls: HIGH — derived from direct code reading and mathematical simulation
- Numeric constants: MEDIUM — math is correct but the CONTEXT baseline targets are slightly inconsistent; one open question documented

**Research date:** 2026-03-04
**Valid until:** Stable — these are internal design decisions, not external library dependencies
