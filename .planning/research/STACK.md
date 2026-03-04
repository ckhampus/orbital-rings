# Stack Research

**Domain:** Cozy 3D builder/management game (Orbital Rings) — v1.1 Happiness v2 additions
**Researched:** 2026-03-04
**Confidence:** HIGH — all APIs verified against existing live codebase and Godot 4.4 official documentation

---

## Scope

This document covers only the **new technical surface area** introduced by the v1.1 Happiness v2 milestone. The existing stack (Godot 4.4, C#/.NET 8, 7 Autoload singletons, System.Text.Json save) is validated and unchanged. No new packages or addons are needed.

---

## Core API Map: Four Technical Problems

### 1. Frame-Based Float Decay (`_Process` vs `Timer`)

**Decision: use `_Process(double delta)` in `HappinessManager`.**

The mood decay formula from the design spec is:

```
mood += (baseline - mood) * DecayRate * delta
```

This is exponential smoothing — it must run every frame because its output depends on its previous output. A `Timer` cannot implement this correctly (it runs at discrete intervals, producing visible stair-step behavior at low decay rates). The existing `EconomyManager` correctly uses `Timer` for discrete income ticks; mood decay is fundamentally different and belongs in `_Process`.

**Frame-rate independence:** The naive `Mathf.Lerp(mood, baseline, t)` called each frame is frame-rate dependent. The correct frame-rate-independent form is:

```csharp
// Frame-rate-independent exponential decay
// Equivalent to: mood moves toward baseline with a half-life of ln(2)/DecayRate seconds
mood += (baseline - mood) * (1f - Mathf.Exp(-DecayRate * (float)delta));
```

At `DecayRate = 0.02` this gives a half-life of ~34.7 seconds, matching the design spec's "~35 seconds." At `DecayRate = 0.02` and 60fps (delta = 0.0167s), the per-frame factor is `1 - exp(-0.02 * 0.0167) ≈ 0.000333` — imperceptible per frame but smooth over time.

**Why not the simpler `mood += (baseline - mood) * DecayRate * delta`?**

The simpler linear form from the design spec is acceptable at these small decay rates (the difference from the exponential form is less than 0.01% per frame at 60fps). Use it as written in the spec — the frame-rate independence concern only becomes material at `DecayRate > 0.1`. Document this as a known approximation.

**Integration with existing singleton:**

`HappinessManager` does not currently use `_Process`. Add it:

```csharp
public override void _Process(double delta)
{
    float baseline = BaselineFactor * Mathf.Sqrt(_lifetimeHappiness);
    _mood += (baseline - _mood) * DecayRate * (float)delta;
    _mood = Mathf.Max(_mood, 0f); // never below zero

    MoodTier newTier = CalculateTier(_mood);
    if (newTier != _currentTier)
    {
        _currentTier = newTier;
        GameEvents.Instance?.EmitMoodTierChanged(_currentTier);
    }
}
```

`ProcessMode = ProcessModeEnum.Pausable` is already set on `HappinessManager` (the Timer uses it), so mood decay will correctly pause with the scene tree.

**Performance note:** A single `_Process` on an Autoload singleton is negligible. The existing Godot C# `_Process` performance concern (reflection overhead when many instances each define `_Process`) does not apply to a singleton called once per frame.

**Confidence:** HIGH — `_Process` API verified against Godot 4.4 docs; exponential decay math verified against [Frame Rate Independent Damping using Lerp](https://www.rorydriscoll.com/2016/03/07/frame-rate-independent-damping-using-lerp/).

---

### 2. Color Lerping for Tier Label

**Decision: `AddThemeColorOverride("font_color", color)` for instant tier color switch, with a brief Tween-driven modulate pulse for transition feel.**

The five tier colors are discrete states, not a continuous gradient. When tier changes:

1. Set the label color immediately (no lerp needed — the floating text notification signals the change)
2. Apply a brief brightness pulse via `modulate` to draw attention

```csharp
private static readonly Color[] TierColors = new Color[]
{
    new Color(0.72f, 0.72f, 0.72f), // Quiet   — soft gray
    new Color(0.95f, 0.60f, 0.50f), // Cozy    — warm coral
    new Color(0.95f, 0.78f, 0.35f), // Lively  — sunny amber
    new Color(1.00f, 0.85f, 0.30f), // Vibrant — bright gold
    new Color(0.98f, 0.96f, 0.92f), // Radiant — soft white glow
};

private void OnMoodTierChanged(MoodTier tier)
{
    // Instant color switch — no lerp, the floating notification handles signaling
    _tierLabel.AddThemeColorOverride("font_color", TierColors[(int)tier]);

    // Brief pulse: overbrighten modulate then return to normal
    _activePulseTween?.Kill();
    _activePulseTween = _tierLabel.CreateTween();
    _activePulseTween.TweenProperty(_tierLabel, "modulate",
        new Color(1.4f, 1.4f, 1.4f, 1.0f), 0.12f)
        .SetEase(Tween.EaseType.Out);
    _activePulseTween.TweenProperty(_tierLabel, "modulate",
        new Color(1.0f, 1.0f, 1.0f, 1.0f), 0.30f)
        .SetEase(Tween.EaseType.In);
}
```

**Why `AddThemeColorOverride` not `modulate`:**

`modulate` multiplies the existing render color. If the label's base color is white (`modulate = Color(1,1,1,1)`), then `modulate` and `font_color` give the same result. But if the base theme sets a non-white font color, `modulate` will multiply incorrectly. `AddThemeColorOverride("font_color", ...)` sets the absolute text color, which is the correct approach when changing tier color. This pattern is already used in the codebase: `HappinessBar._heartLabel` and `FloatingText.Setup()` both use `AddThemeColorOverride("font_color", ...)`.

**"Near promotion" pulse (subtle glow when approaching tier boundary):**

The design spec calls for a subtle visual hint when mood is near the top of its range. Implement as a periodic scale pulse using `SetAutoRestart(true)` or by re-triggering in `_Process` when `moodFractionInTier > 0.85f`:

```csharp
// In _Process, after tier calculation:
float tierTop = TierUpperBound(_currentTier);
float tierBottom = TierLowerBound(_currentTier);
float fraction = (tierTop > tierBottom)
    ? (_mood - tierBottom) / (tierTop - tierBottom)
    : 0f;

bool nearPromotion = fraction > 0.85f;
if (nearPromotion != _wasNearPromotion)
{
    _wasNearPromotion = nearPromotion;
    GameEvents.Instance?.EmitMoodNearPromotion(nearPromotion);
}
```

The HUD responds to `MoodNearPromotion` by starting or stopping a looping gentle scale tween on the tier label (`Scale` from `Vector2.One` to `Vector2(1.05f, 1.05f)` and back, `SetLoops(0)` for infinite).

**Confidence:** HIGH — `AddThemeColorOverride` API confirmed from Godot forum examples and matches existing usage in `HappinessBar.cs` and `FloatingText.cs` in this codebase.

---

### 3. Floating Text for Tier Change Notification

**Decision: reuse the existing `FloatingText` class with no modifications.**

`FloatingText.cs` already implements the complete pattern:

```csharp
// FloatingText.Setup() already does:
// - AddThemeColorOverride("font_color", color)
// - Drift upward 55px over 0.9s with ease-out
// - Fade out (modulate:a → 0) with 0.2s delay
// - QueueFree() on tween completion
```

For the tier change notification ("Station mood: Lively"), spawn via `HappinessManager`'s existing `_arrivalCanvasLayer`:

```csharp
private void SpawnTierChangeText(MoodTier newTier)
{
    var floater = new FloatingText();
    _arrivalCanvasLayer.AddChild(floater);

    var viewport = GetViewport().GetVisibleRect().Size;
    // Position below center (HUD is top-right, tier notification sits center-lower)
    Vector2 pos = new Vector2(viewport.X / 2 - 100f, viewport.Y * 0.6f);
    Color tierColor = TierColors[(int)newTier];
    floater.Setup($"Station mood: {newTier}", tierColor, pos);
}
```

**Why not `Label3D`:**

`Label3D` renders into 3D world space and requires billboard setup to face the camera. For a 2D HUD notification that is anchored to screen coordinates, `Label` (via `FloatingText : Label`) on a `CanvasLayer` is correct. `Label3D` is appropriate for wish bubbles above citizens (3D-positioned, which the project already uses for wish speech bubbles). This is a screen-space notification, not a world-space label.

**Why not an `AnimationPlayer` scene:**

Using a packed scene with `AnimationPlayer` for floating text adds an asset that must be kept in sync with the code. The existing `FloatingText` class is a pure-code approach that is already proven in production (used for `+N credit` and `+X% happiness` in v1.0). Reuse it.

**Confidence:** HIGH — pattern verified against existing `FloatingText.cs`, `HappinessManager.SpawnArrivalText()`, and `HappinessBar.SpawnFloatingText()` in this codebase.

---

### 4. HUD Counter Animation (Lifetime Happiness)

**Decision: kill-before-create Tween with `TweenProperty` on `scale` for pulse, `AddThemeColorOverride` for warm flash. Update text immediately (do not animate the number rolling up).**

The design spec says the counter "ticks up with a brief warm pulse (same pattern as the credit counter flash)." Look at `CreditHUD.cs` for the exact pattern to match:
<br>

```csharp
// Pattern (from HappinessBar.cs -- same idiom as CreditHUD):
_activeTween?.Kill(); // kill-before-create is critical -- prevents fighting tweens

_activeTween = CreateTween();
_activeTween.SetParallel(true);

// Pulse the heart icon scale
_activeTween.TweenProperty(_heartLabel, "scale",
    new Vector2(1.3f, 1.3f), 0.12f)
    .SetEase(Tween.EaseType.Out);
_activeTween.TweenProperty(_counterLabel, "scale",
    new Vector2(1.2f, 1.2f), 0.12f)
    .SetEase(Tween.EaseType.Out);

_activeTween.SetParallel(false); // switch to sequential

// Return to normal size
_activeTween.TweenProperty(_heartLabel, "scale",
    Vector2.One, 0.25f)
    .SetEase(Tween.EaseType.In);
_activeTween.TweenProperty(_counterLabel, "scale",
    Vector2.One, 0.25f)
    .SetEase(Tween.EaseType.In);
```

Update the counter label text immediately before starting the tween (not after) so the number shows the new value during the pulse:

```csharp
_counterLabel.Text = $"\u2665 {_lifetimeHappiness}";
// Then start tween
```

**Why immediate text update instead of animated roll-up:**

The design spec says "same pattern as the credit counter flash." `CreditHUD` updates the number immediately and pulses the widget. Rolling the number up (counting from 46 to 47 over 300ms) adds complexity, creates timing issues when multiple wishes fire in quick succession, and makes the display momentarily show an incorrect value. Immediate update with a visual pulse is the established pattern in this codebase and is visually satisfying.

**Note on `SetParallel` with sequential follow-up:** The `TweenProperty` calls after `SetParallel(false)` run sequentially but only affect the heart and counter labels — so both labels return to `Vector2.One` at the same time because they each get their own sequential call. If you want them to animate back simultaneously, add a parallel block for the return:

```csharp
_activeTween.SetParallel(true);
_activeTween.TweenProperty(_heartLabel, "scale", Vector2.One, 0.25f).SetEase(Tween.EaseType.In);
_activeTween.TweenProperty(_counterLabel, "scale", Vector2.One, 0.25f).SetEase(Tween.EaseType.In);
```

**Confidence:** HIGH — `CreateTween`, `TweenProperty`, `SetParallel`, `Kill`, `TweenCallback` all verified against [Godot 4.4 Tween docs](https://docs.godotengine.org/en/4.4/classes/class_tween.html) and confirmed by usage in existing `HappinessBar.cs`, `HappinessManager.cs` (fade-in tween), and `FloatingText.cs` in this codebase.

---

## Save Migration

**Decision: version field in `SaveData` with explicit migration block in `SaveManager.Load()` / `SaveManager.ApplyState()`.**

The existing `SaveData` already has `public int Version { get; set; } = 1;`. The migration path is:

**v1 format** (existing): has `float Happiness` (0.0–1.0)
**v2 format** (new): has `int LifetimeHappiness` and `float Mood` instead

Migration in `SaveManager.Load()`:

```csharp
public SaveData Load()
{
    // ... existing file read + JsonDeserialize ...

    if (data.Version < 2)
        data = MigrateV1ToV2(data);

    return data;
}

private static SaveData MigrateV1ToV2(SaveData v1)
{
    // Invert diminishing returns formula to estimate wish count:
    // gain = HappinessGainBase / (1 + currentHappiness) accumulated to happiness
    // Approximate inverse: wishes ≈ happiness / HappinessGainBase * (1 + happiness)
    const float HappinessGainBase = 0.08f;
    int estimatedWishes = Mathf.RoundToInt(
        v1.Happiness / HappinessGainBase * (1f + v1.Happiness));

    float baseline = Mathf.Sqrt(estimatedWishes); // BaselineFactor = 1.0
    float initialMood = baseline;                  // Start at resting baseline

    v1.LifetimeHappiness = estimatedWishes;
    v1.Mood = initialMood;
    v1.Happiness = 0f;    // Clear old field (will be ignored in v2)
    v1.Version = 2;

    return v1; // now a valid v2 save
}
```

**Why not a separate migration class:** The migration is a one-step, one-direction transform. A static method in `SaveManager` is the lowest-friction approach that keeps migration logic co-located with the save/load code. If the save format grows across many versions, extract to a `SaveMigration` class at that point.

**`SaveData` changes needed:**

```csharp
public class SaveData
{
    public int Version { get; set; } = 2;       // bump from 1
    public int Credits { get; set; }
    public float Happiness { get; set; }         // keep for v1 migration reads — do not remove
    public int LifetimeHappiness { get; set; }   // NEW
    public float Mood { get; set; }              // NEW
    public int CrossedMilestoneCount { get; set; } // remove after v2 (keyed to wish count now)
    // ... rest unchanged
}
```

`System.Text.Json` will silently ignore unknown fields and default-initialize missing fields when deserializing a v1 save, so a v1 file with no `LifetimeHappiness` field will deserialize with `LifetimeHappiness = 0`, `Mood = 0.0f` — the migration block then overwrites those defaults with the computed values.

**Confidence:** HIGH — `System.Text.Json` missing-field behavior verified as default-initialization (not exception); `SaveData.Version` pattern confirmed from existing `SaveManager.cs` in this codebase.

---

## GameEvents Bus Additions

Two new events must be added to `GameEvents.cs` to keep the singleton architecture consistent (UI reacts to events, never polls HappinessManager directly):

```csharp
// In GameEvents.cs — add alongside existing HappinessChanged

/// <param name="newCount">Updated lifetime happiness wish count.</param>
public event Action<int> LifetimeHappinessChanged;
public void EmitLifetimeHappinessChanged(int newCount)
    => LifetimeHappinessChanged?.Invoke(newCount);

/// <param name="newTier">The new mood tier after transition.</param>
public event Action<MoodTier> MoodTierChanged;
public void EmitMoodTierChanged(MoodTier newTier)
    => MoodTierChanged?.Invoke(newTier);

/// <param name="isNear">True when mood fraction within current tier exceeds 0.85.</param>
public event Action<bool> MoodNearPromotion;
public void EmitMoodNearPromotion(bool isNear)
    => MoodNearPromotion?.Invoke(isNear);
```

`MoodTier` is an enum defined in `HappinessManager.cs` (or a shared Data namespace), with values `Quiet, Cozy, Lively, Vibrant, Radiant` matching the design spec tiers.

**Why `MoodNearPromotion` as a bool event and not a float:** The HUD only needs to know "start pulsing" or "stop pulsing." Exposing the raw fraction would couple the HUD to the tier threshold math. The bool event is the minimal interface.

**Confidence:** HIGH — matches the existing `GameEvents` pattern (pure C# event delegates, null-safe emit, typed signatures) confirmed from `GameEvents.cs` in this codebase.

---

## EconomyManager Integration Point

`EconomyManager.SetHappiness(float happiness)` currently receives a raw float and computes `1 + (happiness * 0.3x)`. For v2, this must be replaced with a tier-based lookup. The simplest approach is a new method:

```csharp
// In EconomyManager.cs — add alongside SetHappiness
public void SetMoodTier(MoodTier tier)
{
    _currentEconomyMultiplier = tier switch
    {
        MoodTier.Quiet   => 1.0f,
        MoodTier.Cozy    => 1.1f,
        MoodTier.Lively  => 1.2f,
        MoodTier.Vibrant => 1.3f,
        MoodTier.Radiant => 1.4f,
        _                => 1.0f,
    };
}
```

Replace `_currentHappiness`-based multiplier in `CalculateTickIncome()` with `_currentEconomyMultiplier`. Keep `SetHappiness` temporarily during migration (it is used in `HappinessManager.RestoreState`; remove once `RestoreState` is updated to v2).

**Confidence:** HIGH — confirmed from `EconomyManager.CalculateTickIncome()` in this codebase.

---

## Recommended Stack (Summary)

### Core Technologies

No changes to core engine stack. All APIs used are built into Godot 4.4.

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| Godot 4.4 C# | already set | Engine + all APIs below | No additions needed |
| `_Process(double delta)` | Godot built-in | Mood decay per frame | Only correct approach for continuous exponential smoothing |
| `Tween` (via `CreateTween()`) | Godot built-in | All UI animations | Kill-before-create pattern already proven in this codebase |
| `AddThemeColorOverride("font_color", color)` | Godot built-in | Tier label color change | Absolute color override, not affected by base theme conflicts |
| `FloatingText : Label` (existing) | This codebase | Tier change notification | Zero new code — existing implementation handles all needed behavior |
| `System.Text.Json` | .NET 8 built-in | Save migration | Already in use; missing fields default to zero, enabling v1→v2 migration |

### Supporting Libraries

None required. No new NuGet packages needed for any v1.1 feature.

### Development Tools

No changes — existing Rider + Godot 4.4 toolchain is sufficient.

---

## What NOT to Add

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| Third-party tween library (GTweensGodot, GoTween) | Project already has `CreateTween()` patterns working correctly; adding a library creates a dependency for zero benefit | Godot `CreateTween()` — already used in `FloatingText`, `HappinessBar`, `HappinessManager` |
| `Timer` node for mood decay | Discrete ticks produce stair-step decay instead of smooth continuous drift | `_Process(double delta)` in `HappinessManager` |
| `Label3D` for tier change notification | 3D world-space label requires billboard setup and camera-facing math; wrong for a screen-space HUD notification | `FloatingText : Label` on `_arrivalCanvasLayer` (existing) |
| Separate `AnimationPlayer` scene for floating text | Adds asset that must be kept in sync with code; existing code-driven `FloatingText` is already proven | `FloatingText.Setup()` reuse |
| Rolling number counter (counting 46→47 over 300ms) | Creates timing issues on rapid wish fulfillment; shows incorrect intermediate values | Immediate text update + scale pulse (CreditHUD pattern) |

---

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| `_Process` for mood decay | `SceneTreeTimer` / `Timer` | Only if decay is meant to be discrete steps (it is not — it is continuous drift) |
| `AddThemeColorOverride` for tier color | `modulate` tinting | Only if the label's base `font_color` is always white; `AddThemeColorOverride` is safer |
| Immediate text + scale pulse for counter | Animated number roll-up | Only if the UX specifically requires showing intermediate values (e.g., a score screen); not for a live HUD counter |
| In-place `SaveData` version migration | Separate `SaveData_v2` class | Appropriate when formats diverge significantly across many fields; v1→v2 changes only 2 fields |

---

## Sources

- [Godot 4.4 Tween class docs](https://docs.godotengine.org/en/4.4/classes/class_tween.html) — `TweenProperty`, `TweenCallback`, `SetParallel`, `Kill` — HIGH confidence
- [Godot 4.4 CanvasLayer docs](https://docs.godotengine.org/en/4.4/classes/class_canvaslayer.html) — screen-space UI layer — HIGH confidence
- [Godot 4.4 Label3D docs](https://docs.godotengine.org/en/4.4/classes/class_label3d.html) — confirmed world-space, not suitable for HUD notifications — HIGH confidence
- [Frame Rate Independent Damping using Lerp](https://www.rorydriscoll.com/2016/03/07/frame-rate-independent-damping-using-lerp/) — exponential decay math — HIGH confidence
- Existing `/workspace/Scripts/UI/FloatingText.cs` — confirmed `CreateTween` + `TweenProperty` + `QueueFree` pattern — HIGH confidence (live code)
- Existing `/workspace/Scripts/UI/HappinessBar.cs` — confirmed `AddThemeColorOverride`, `Color.Lerp`, kill-before-create tween, modulate pulse — HIGH confidence (live code)
- Existing `/workspace/Scripts/Autoloads/HappinessManager.cs` — confirmed `_arrivalCanvasLayer`, `Timer` usage, event bus pattern — HIGH confidence (live code)
- Existing `/workspace/Scripts/Autoloads/SaveManager.cs` — confirmed `SaveData.Version`, `System.Text.Json` deserialization, missing-field behavior — HIGH confidence (live code)
- Existing `/workspace/Scripts/Autoloads/GameEvents.cs` — confirmed pure C# event delegate pattern — HIGH confidence (live code)
- [Godot forum: Tweening a Label's Text Color](https://forum.godotengine.org/t/tweening-a-labels-text-color/72218) — `add_theme_color_override("font_color", ...)` confirmed as standard approach — MEDIUM confidence (forum, consistent with official docs)

---

*Stack research for: Orbital Rings v1.1 — Happiness v2 dual-value mood system*
*Researched: 2026-03-04*
