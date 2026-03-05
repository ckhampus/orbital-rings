# Phase 13: HUD Replacement - Research

**Researched:** 2026-03-05
**Domain:** Godot 4 C# UI widgets, HUD lifecycle patterns, tween animation
**Confidence:** HIGH

## Summary

This phase replaces the defunct HappinessBar (a percentage fill bar that no longer receives events since Phase 10 removed HappinessChanged emissions) with two new HUD elements: a lifetime wish counter ("heart N" with pulse animation) and a mood tier label (colored text). Both widgets subscribe to Phase 10 events (WishCountChanged, MoodTierChanged) that already exist and are actively emitted. A lightweight tier change floating notification (MCOM-01) is included.

The codebase has a well-established HUD widget pattern: MarginContainer subclass, programmatic child creation in `_Ready()`, event subscription in `_Ready()` / unsubscription in `_ExitTree()`, kill-before-create tween pattern, and `MouseFilter.Ignore` on decorative labels. CreditHUD (300 lines) and PopulationDisplay (113 lines) are the reference implementations. The new MoodHUD widget follows this identical pattern with minimal risk.

**Primary recommendation:** Build a single `MoodHUD : MarginContainer` class containing an HBoxContainer with heart icon label, wish count label, and tier name label. Subscribe to `WishCountChanged` and `MoodTierChanged`. Delete `HappinessBar.cs` and its scene node entirely. Remove the deprecated `Happiness` property shim and `HappinessChanged` event from autoloads.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **Tier Colors:** Warm spectrum progression (cool-to-warm as tiers rise). Quiet=soft grey-blue, up to Radiant=bright gold. Color cross-fade (~0.3s tween) on tier transition, not instant snap
- **Animation Feel:** Wish counter pulse uses scale bounce (brief scale-up + elastic settle), same pattern as PopulationDisplay citizen arrival animation. No floating "+1" text on wish fulfillment. Tier change: color cross-fade + brief scale pop on the tier name label
- **Tier Change Notification:** Include floating "Station mood: Lively" text on tier change (MCOM-01) using existing FloatingText class
- **HUD Layout:** Replace HappinessBar in same rightmost position. Layout order: Credits | Population | heart N | TierName. One combined widget (single MarginContainer class, e.g., MoodHUD) containing both heart counter and tier label side-by-side
- **Font Size:** 20 for both wish counter number and tier label (matches CreditHUD and PopulationDisplay)
- **Heart Icon:** Reuses existing coral color (0.95, 0.55, 0.55) from HappinessBar
- **Wish Counter Number:** Uses warm white (0.95, 0.93, 0.90) matching other HUD text
- **Tier Label:** Shows name only (e.g., "Cozy" not "star Cozy"), no tooltip on hover
- **Cleanup:** Delete HappinessBar.cs entirely + remove from QuickTestScene.tscn. Remove deprecated Happiness property shim from HappinessManager. Remove HappinessChanged event and EmitHappinessChanged from GameEvents. Keep Happiness field in SaveData for v1 backward compatibility

### Claude's Discretion
- Exact RGB values for each tier in the warm spectrum (harmonize with existing palette)
- Exact scale bounce magnitude and timing (match PopulationDisplay feel)
- Exact cross-fade duration for tier color transition
- Floating text position and style for tier change notification
- Whether MoodHUD builds UI in _Ready() programmatically (following CreditHUD pattern) or uses a .tscn scene

### Deferred Ideas (OUT OF SCOPE)
- MCOM-02: Tier label pulse/glow when mood is near the top of its range (about to promote)
- Mood tooltip showing tier progression or mood details
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| HUD-01 | HUD displays lifetime wish counter as "heart N" with pulse animation on increment | MoodHUD subscribes to `GameEvents.WishCountChanged(int)`, reads `HappinessManager.LifetimeWishes` for initial value, uses PopulationDisplay scale bounce pattern |
| HUD-02 | HUD displays current mood tier name with tier-colored text | MoodHUD subscribes to `GameEvents.MoodTierChanged(MoodTier, MoodTier)`, reads `HappinessManager.CurrentTier` for initial value, uses tier-to-color lookup dictionary |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Godot 4 C# | 4.x | Game engine + UI framework | Project engine (all existing UI uses Godot Control nodes) |
| MarginContainer | Godot built-in | Base class for HUD widgets | All HUD widgets (CreditHUD, PopulationDisplay, HappinessBar) extend MarginContainer |
| Tween | Godot built-in | Animation for pulse, color cross-fade | Kill-before-create tween pattern established in all HUD widgets |
| FloatingText | Custom (Scripts/UI/) | Drift-and-fade text labels | Existing reusable class, already used by CreditHUD and HappinessManager |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| HBoxContainer | Godot built-in | Horizontal layout of icon + number + tier label | Standard for side-by-side HUD elements (used in all existing widgets) |
| Label | Godot built-in | Text display with theme overrides | Font size, color, and mouse filter set via theme overrides |

No external libraries needed. Everything is built with Godot's built-in Control nodes.

## Architecture Patterns

### Recommended Project Structure
```
Scripts/UI/
    MoodHUD.cs           # NEW - Combined wish counter + tier label widget
    HappinessBar.cs      # DELETED - Replaced by MoodHUD
    FloatingText.cs      # EXISTING - Used for tier change notification
    CreditHUD.cs         # EXISTING - Reference pattern
    PopulationDisplay.cs # EXISTING - Reference animation pattern
```

### Pattern 1: Programmatic HUD Widget (CreditHUD Pattern)
**What:** MarginContainer subclass builds all child nodes in `_Ready()` using `new Label()`, `AddChild()`, theme overrides
**When to use:** All HUD widgets in this project
**Example:**
```csharp
// Source: Scripts/UI/CreditHUD.cs (lines 59-77)
public override void _Ready()
{
    _hbox = new HBoxContainer();
    _hbox.MouseFilter = MouseFilterEnum.Ignore;
    AddChild(_hbox);

    _iconLabel = new Label();
    _iconLabel.Text = "\u2665"; // Unicode heart
    _iconLabel.AddThemeColorOverride("font_color", HeartColor);
    _iconLabel.AddThemeFontSizeOverride("font_size", 20);
    _iconLabel.MouseFilter = MouseFilterEnum.Ignore;
    _hbox.AddChild(_iconLabel);
    // ... more children
}
```

### Pattern 2: Event Subscription Lifecycle
**What:** Subscribe in `_Ready()` (not `_EnterTree`), unsubscribe in `_ExitTree()`
**When to use:** Any node consuming GameEvents signals
**Why:** Autoload singletons may not be initialized in `_EnterTree` in Godot 4 C#
**Example:**
```csharp
// Source: Scripts/UI/PopulationDisplay.cs (lines 70-87)
public override void _Ready()
{
    if (GameEvents.Instance != null)
    {
        GameEvents.Instance.CitizenArrived += OnCitizenArrived;
    }
}

public override void _ExitTree()
{
    if (GameEvents.Instance != null)
    {
        GameEvents.Instance.CitizenArrived -= OnCitizenArrived;
    }
}
```

### Pattern 3: Kill-Before-Create Tween
**What:** Kill any existing active tween before creating a new one to prevent stacking
**When to use:** Any animated response to events that can fire rapidly
**Example:**
```csharp
// Source: Scripts/UI/PopulationDisplay.cs (lines 103-111)
_activeTween?.Kill();

_activeTween = _countLabel.CreateTween();
_countLabel.Scale = new Vector2(1.2f, 1.2f);
_activeTween.TweenProperty(_countLabel, "scale", new Vector2(1.0f, 1.0f), 0.3f)
    .SetEase(Tween.EaseType.Out)
    .SetTrans(Tween.TransitionType.Elastic);
```

### Pattern 4: Scale Bounce Animation (PopulationDisplay Reference)
**What:** Set scale to 1.2x immediately, tween back to 1.0x with elastic ease-out
**When to use:** Wish counter pulse on increment, tier label pop on change
**Exact values from PopulationDisplay:**
- Scale up: `new Vector2(1.2f, 1.2f)` (set immediately, not tweened)
- Scale down: `new Vector2(1.0f, 1.0f)` over `0.3f` seconds
- Ease: `Tween.EaseType.Out`, Trans: `Tween.TransitionType.Elastic`

### Anti-Patterns to Avoid
- **Using _EnterTree for event subscription:** Autoload singletons may be null. Always use `_Ready()`.
- **Forgetting MouseFilter.Ignore:** Decorative labels will block 3D click events below HUDLayer if not set to Ignore.
- **Creating tweens without killing existing ones:** Rapid events (multiple wishes in quick succession) will stack tweens and cause erratic animation.
- **Subscribing without unsubscribing:** Memory leaks and null reference errors when scene changes. Always mirror subscribe/unsubscribe.
- **Using a .tscn scene for this widget:** All other HUD widgets are fully programmatic in `_Ready()`. A .tscn would break the established pattern. **Decision: Build programmatically.**

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Floating text | Custom drift/fade logic | `FloatingText.cs` | Already handles drift direction, fade timing, and self-destruction via `QueueFree()` |
| Scale bounce animation | Custom timing code | Copy PopulationDisplay pattern exactly | Proven elastic settle feel, exact values already tuned |
| Tier-to-name mapping | String switch statement | `MoodTier.ToString()` | C# enum has built-in `.ToString()` that returns "Quiet", "Cozy", etc. exactly matching the required display text |

**Key insight:** This phase is primarily wiring and cleanup. All animation patterns, event infrastructure, and UI patterns already exist. The implementation is composition of existing pieces, not invention.

## Common Pitfalls

### Pitfall 1: Forgetting Initial State on _Ready
**What goes wrong:** HUD shows "0" wishes and "Quiet" on every scene load until the first event fires
**Why it happens:** Events only fire on *change*. Initial state must be read from autoload singletons.
**How to avoid:** In `_Ready()`, read `HappinessManager.Instance.LifetimeWishes` for initial wish count and `HappinessManager.Instance.CurrentTier` for initial tier. Apply without animation.
**Warning signs:** HUD flickers or shows stale data after save/load.

### Pitfall 2: Tween on Wrong Node for Scale
**What goes wrong:** Scale bounce affects the entire MoodHUD container instead of just the count label
**Why it happens:** `CreateTween()` vs `_label.CreateTween()` creates the tween on different nodes
**How to avoid:** Use `_countLabel.CreateTween()` for wish pulse (scale on count label only), use `_tierLabel.CreateTween()` for tier pop (scale on tier label only). The tween target node determines what gets animated.
**Warning signs:** Entire HUD row jumps when a wish is fulfilled.

### Pitfall 3: Color Cross-Fade Interrupted by Rapid Tier Changes
**What goes wrong:** If mood oscillates near a tier boundary, color tweens stack and produce garbled colors
**Why it happens:** MoodTierChanged can fire multiple times quickly (despite hysteresis, decay can trigger rapid changes during high activity)
**How to avoid:** Use kill-before-create pattern for the color tween. Kill the existing color tween before starting a new cross-fade.
**Warning signs:** Tier label flickers between colors.

### Pitfall 4: Stale Scene Reference After HappinessBar Deletion
**What goes wrong:** Project won't build or scene errors on load
**Why it happens:** QuickTestScene.tscn references HappinessBar.cs script and node
**How to avoid:** Remove all three things: (1) the HappinessBar node from QuickTestScene.tscn, (2) the ext_resource line referencing HappinessBar.cs script, (3) the HappinessBar.cs file itself. Add MoodHUD node in HappinessBar's position.
**Warning signs:** Godot editor shows "Missing script" error on scene load.

### Pitfall 5: SaveData.Happiness Field Removal
**What goes wrong:** v1 saves fail to deserialize
**Why it happens:** System.Text.Json throws on unknown/missing properties depending on configuration
**How to avoid:** Keep `SaveData.Happiness` field. It's still read in the v1 backward-compat path in SaveManager.cs (line 404). Only the `HappinessManager.Happiness` *property shim* is removed.
**Warning signs:** Old save files crash on load.

## Code Examples

### MoodHUD Widget Structure
```csharp
// Recommended structure following CreditHUD + PopulationDisplay patterns
public partial class MoodHUD : MarginContainer
{
    // Colors
    private static readonly Color HeartColor = new(0.95f, 0.55f, 0.55f);   // coral, from HappinessBar
    private static readonly Color CountColor = new(0.95f, 0.93f, 0.90f);   // warm white, project standard

    // Tier color lookup (warm spectrum: cool→warm)
    private static readonly Dictionary<MoodTier, Color> TierColors = new()
    {
        { MoodTier.Quiet,   new Color(0.65f, 0.72f, 0.78f) },  // soft grey-blue (cool, muted)
        { MoodTier.Cozy,    new Color(0.85f, 0.75f, 0.55f) },  // warm tan/sand
        { MoodTier.Lively,  new Color(0.95f, 0.72f, 0.42f) },  // warm amber/orange
        { MoodTier.Vibrant, new Color(0.95f, 0.60f, 0.40f) },  // warm coral-orange
        { MoodTier.Radiant, new Color(1.00f, 0.85f, 0.35f) },  // bright gold
    };

    // UI nodes
    private HBoxContainer _hbox;
    private Label _heartLabel;
    private Label _countLabel;
    private Label _tierLabel;

    // Tween state (separate tweens for independent kill-before-create)
    private Tween _pulseTween;
    private Tween _tierColorTween;
    private Tween _tierPopTween;
}
```

### Wish Counter Pulse (from PopulationDisplay pattern)
```csharp
// Source pattern: Scripts/UI/PopulationDisplay.cs (lines 103-111)
private void OnWishCountChanged(int newCount)
{
    _countLabel.Text = newCount.ToString();

    // Kill-before-create
    _pulseTween?.Kill();

    // Scale bounce: set scale up immediately, elastic settle back
    _pulseTween = _countLabel.CreateTween();
    _countLabel.Scale = new Vector2(1.2f, 1.2f);
    _pulseTween.TweenProperty(_countLabel, "scale", new Vector2(1.0f, 1.0f), 0.3f)
        .SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Elastic);
}
```

### Tier Color Cross-Fade
```csharp
private void OnMoodTierChanged(MoodTier newTier, MoodTier previousTier)
{
    // Update tier name text
    _tierLabel.Text = newTier.ToString();

    // Kill existing color tween
    _tierColorTween?.Kill();

    // Cross-fade to new tier color (~0.3s)
    Color targetColor = TierColors[newTier];
    _tierColorTween = CreateTween();
    _tierColorTween.TweenMethod(
        Callable.From<Color>(c => _tierLabel.AddThemeColorOverride("font_color", c)),
        TierColors.GetValueOrDefault(previousTier, TierColors[MoodTier.Quiet]),
        targetColor,
        0.3f
    ).SetEase(Tween.EaseType.Out);

    // Scale pop on tier label
    _tierPopTween?.Kill();
    _tierPopTween = _tierLabel.CreateTween();
    _tierLabel.Scale = new Vector2(1.15f, 1.15f);
    _tierPopTween.TweenProperty(_tierLabel, "scale", new Vector2(1.0f, 1.0f), 0.3f)
        .SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Elastic);

    // Spawn floating notification (MCOM-01)
    SpawnTierNotification(newTier);
}
```

### Tier Change Floating Text (MCOM-01)
```csharp
// Source pattern: Scripts/Autoloads/HappinessManager.cs (lines 354-362)
private void SpawnTierNotification(MoodTier tier)
{
    var floater = new FloatingText();
    AddChild(floater);

    // Position near the tier label, slightly above
    Vector2 startPos = new Vector2(_tierLabel.Position.X, -20);
    Color tierColor = TierColors[tier];
    floater.Setup($"Station mood: {tier}", tierColor, startPos);
}
```

### QuickTestScene.tscn Node Replacement
```
# REMOVE these lines:
[ext_resource type="Script" path="res://Scripts/UI/HappinessBar.cs" id="7_happinessbar"]

[node name="HappinessBar" type="MarginContainer" parent="HUDLayer"]
anchors_preset = 1
anchor_left = 1.0
anchor_right = 1.0
offset_left = -200.0
offset_bottom = 50.0
grow_horizontal = 0
script = ExtResource("7_happinessbar")

# ADD these lines (same position, same anchoring):
[ext_resource type="Script" path="res://Scripts/UI/MoodHUD.cs" id="7_moodhud"]

[node name="MoodHUD" type="MarginContainer" parent="HUDLayer"]
anchors_preset = 1
anchor_left = 1.0
anchor_right = 1.0
offset_left = -200.0
offset_bottom = 50.0
grow_horizontal = 0
script = ExtResource("7_moodhud")
```

### GameEvents Cleanup
```csharp
// REMOVE from Scripts/Autoloads/GameEvents.cs:
public event Action<float> HappinessChanged;
public void EmitHappinessChanged(float newHappiness)
    => HappinessChanged?.Invoke(newHappiness);
```

### HappinessManager Cleanup
```csharp
// REMOVE from Scripts/Autoloads/HappinessManager.cs:
// Line 119 — the deprecated Happiness property shim:
public float Happiness => _moodSystem?.Mood ?? 0f;
// Line 169 comment — references HappinessBar:
// Do NOT call EmitHappinessChanged — HappinessBar will be replaced in Phase 13

// Also remove the `using OrbitalRings.UI;` import if FloatingText is no longer
// referenced after cleanup (check: it IS still used by SpawnArrivalText, so KEEP it)
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Single float happiness (0-1) displayed as fill bar | Dual-value system: lifetime wishes (int) + mood (float) displayed as count + tier name | Phase 10 | HappinessBar is broken since Phase 10; HappinessChanged never emitted |
| HappinessChanged event | WishCountChanged + MoodTierChanged events | Phase 10 | Old event has zero subscribers (except HappinessBar itself); new events are actively emitted |
| Economy uses happiness float | Economy uses MoodTier enum | Phase 11 | EconomyManager.SetMoodTier replaces old SetHappiness |

**Deprecated/outdated (to remove in this phase):**
- `HappinessBar.cs`: Dead code, subscribes to event that is never emitted
- `GameEvents.HappinessChanged`: Zero emitters, only subscriber is HappinessBar
- `GameEvents.EmitHappinessChanged()`: Never called anywhere
- `HappinessManager.Happiness` property: Only consumer is HappinessBar (SaveManager reads SaveData.Happiness, not this property)

## Existing Color Palette Reference

For tier color selection within Claude's discretion, here is the existing HUD palette:

| Element | Color | RGB |
|---------|-------|-----|
| Star icon (credits) | Gold | (1.0, 0.85, 0.3) |
| Smiley icon (population) | Mint/teal | (0.5, 0.85, 0.75) |
| Heart icon (happiness) | Coral | (0.95, 0.55, 0.55) |
| HUD text (all widgets) | Warm white | (0.95, 0.93, 0.90) |
| Flash gold (credit tick) | Gold | (1.0, 0.92, 0.6) |
| Tooltip background | Dark purple | (0.15, 0.12, 0.18, 0.85-0.9) |

Tier colors must harmonize with this palette. The warm spectrum should avoid colliding with the mint smiley (population) or the gold star (credits). The progression from cool grey-blue to bright gold is distinct from both.

## Open Questions

1. **HBoxContainer separation constant for MoodHUD**
   - What we know: CreditHUD uses default separation, PopulationDisplay uses 4, HappinessBar uses 6
   - What's unclear: Optimal separation between heart+count and tier label within one combined widget
   - Recommendation: Use 6 (matching HappinessBar's spacing which occupied the same slot), fine-tune visually

2. **Offset positioning for MoodHUD in QuickTestScene.tscn**
   - What we know: HappinessBar uses `offset_left = -200.0`. With the bar gone (it was 120px wide + heart + percentage), the new widget is narrower (heart + count + tier name text)
   - What's unclear: Whether -200.0 is still the right offset or needs adjustment
   - Recommendation: Keep -200.0 initially. The HBoxContainer will auto-size to content. Fine-tune if needed.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None detected (Godot 4 C# project, no unit test framework) |
| Config file | None |
| Quick run command | Manual visual verification in Godot editor |
| Full suite command | Manual visual verification in Godot editor |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| HUD-01 | Wish counter displays and pulses on wish fulfillment | manual-only | Run scene, fulfill a wish, observe heart+count pulse | N/A |
| HUD-02 | Tier label shows current tier name in tier color | manual-only | Run scene, observe tier label; fulfill wishes to trigger tier change, observe color cross-fade | N/A |
| MCOM-01 | Floating text on tier change | manual-only | Trigger tier change, observe "Station mood: X" floating text | N/A |
| CLEANUP | HappinessBar removed, no build errors | build | `dotnet build` | N/A |

**Manual-only justification:** All requirements are visual (animation timing, color appearance, UI layout). Godot 4 C# does not have a standard headless UI test runner. The project has no existing test infrastructure. Build verification (`dotnet build`) confirms cleanup completeness.

### Sampling Rate
- **Per task commit:** `dotnet build` to verify no compilation errors after cleanup
- **Per wave merge:** Manual visual check in Godot editor
- **Phase gate:** `dotnet build` succeeds + visual verification of both HUD elements

### Wave 0 Gaps
- None that can be addressed with automated tests. This is a UI-only phase with no existing test infrastructure.

## Sources

### Primary (HIGH confidence)
- `Scripts/UI/CreditHUD.cs` - Reference HUD widget pattern (programmatic build, event lifecycle, tween animations)
- `Scripts/UI/PopulationDisplay.cs` - Reference scale bounce animation pattern (exact values: 1.2x scale, 0.3s elastic settle)
- `Scripts/UI/HappinessBar.cs` - Widget being replaced (HeartColor constant, layout position, event subscription pattern)
- `Scripts/UI/FloatingText.cs` - Reusable floating text class for tier change notification
- `Scripts/Autoloads/GameEvents.cs` - Event signatures: `WishCountChanged(int)`, `MoodTierChanged(MoodTier, MoodTier)`, `HappinessChanged(float)` to remove
- `Scripts/Autoloads/HappinessManager.cs` - Public API: `LifetimeWishes`, `CurrentTier`, deprecated `Happiness` shim
- `Scripts/Data/MoodTier.cs` - Enum definition: Quiet=0, Cozy=1, Lively=2, Vibrant=3, Radiant=4
- `Scenes/QuickTest/QuickTestScene.tscn` - Scene layout: HUDLayer node hierarchy, HappinessBar anchoring at offset_left=-200

### Verification
- Grep confirmed `HappinessChanged` has zero emitters and one subscriber (HappinessBar only)
- Grep confirmed `HappinessManager.Happiness` property is consumed only by HappinessBar (SaveManager reads `SaveData.Happiness`, a different field)
- Grep confirmed no other `.cs` files reference HappinessBar class

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All patterns directly observed in existing codebase; no external libraries needed
- Architecture: HIGH - Exact replication of CreditHUD/PopulationDisplay patterns with different data
- Pitfalls: HIGH - All pitfalls identified from actual codebase patterns (tween stacking, _EnterTree timing, scene references)
- Cleanup safety: HIGH - Grep-verified that all deprecated code has zero external consumers

**Research date:** 2026-03-05
**Valid until:** Indefinite (patterns are internal to this codebase, not dependent on external library versions)
