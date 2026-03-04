# Phase 3: Economy Foundation - Research

**Researched:** 2026-03-03
**Domain:** Game economy system (credit income, cost formulas, balance tuning) in Godot 4.6 C#
**Confidence:** HIGH

## Summary

Phase 3 builds the credit economy on top of existing infrastructure. The project already has `EconomyConfig` (Resource with Export fields), `GameEvents` (Autoload signal bus with `CreditsChanged`/`HappinessChanged` events), and the `SafeNode` base class for event lifecycle. The new work is: an `EconomyManager` Autoload that owns the credit balance and ticks passive income on a Timer, cost calculation methods using category tiers and size discounts, a minimal credit HUD with rolling counter and floating spend/refund numbers, and a balance spreadsheet produced *before* any numbers are hardcoded.

The existing codebase follows a clean pattern: pure C# event delegates (not Godot `[Signal]`), Inspector-editable `Resource` subclasses for data, `SafeNode` subscribe/unsubscribe for event lifecycle, and Autoload singletons with `Instance` static properties. The economy system follows all of these patterns exactly.

**Primary recommendation:** Build the balance spreadsheet first (as a CSV or markdown table), then implement `EconomyManager` as a second Autoload, wire the credit HUD as a `SafeNode` Control, and validate the numbers match the spreadsheet at each milestone (0, 5, 15, 30 citizens).

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Periodic chunk ticks every 5-6 seconds (not smooth rolling)
- Each tick deposits a visible +N to the credit balance
- Small starter income with zero citizens (station itself generates a trickle, e.g., +1 per tick)
- Income scales with citizen count but with diminishing returns per additional citizen (prevents runaway loop)
- Subtle counter flash (brief color pulse/glow) on each income tick — no sound
- Gentle constraint feel — player thinks about what to build next but never feels stuck or punished
- Happiness multiplier is subtle (~1.3x cap, not the current 2.0x default) — fulfilling wishes matters but income stays stable
- Work room bonus is small (1.2-1.3x, down from current 1.5x default) — less pressure to optimize assignments
- No session length target — spreadsheet ensures the curve feels right at each milestone
- Flat room costs — no inflation based on how many rooms exist
- Build once, no upkeep — paying for a room is a one-time cost, no ongoing drain
- 50% demolish refund (current default confirmed)
- Top-right corner of screen — icon (coin/crystal) + number (e.g., ⭐ 1,250)
- Count up/down animation when credits change (rolling counter like a slot machine)
- Subtle flash on income ticks
- Floating numbers on spend/refund: "-100" drifts up on room placement, "+50" on demolish
- Income rate NOT shown next to balance — balance only; hover/click the counter for a breakdown tooltip showing base income, citizen income, work bonus components
- This is a minimal economy HUD for Phase 3; full HUD wiring (happiness, population) is Phase 8
- Slight size discount for multi-segment rooms (~5-10% per additional segment, down from current 0.85 factor)
- Room costs vary by type within each category (not flat per category) — some rooms cost more and are more interesting
- Category cost tiers (cheapest to most expensive): Housing < Life Support < Work < Comfort < Utility
- Outer row segments cost slightly more than inner row (outer segments are physically larger due to larger radius)
- Starting credits enough to place 5-6 cheap rooms (Housing/Life Support) on an empty ring — may need to increase from current 500 default
- Unlocked rooms (from Phase 7 happiness milestones) are NOT more expensive — progression gives variety, not cost escalation
- Balance spreadsheet must be produced before any numbers are hardcoded in code
- Models citizen count milestones (0, 5, 15, 30 citizens)
- Covers: income per tick, room costs by category/size, happiness multiplier effect, credit accumulation curves
- Open-ended pacing — no target session length, just ensure each milestone feels right
- Must validate that diminishing returns + happiness cap prevent runaway positive feedback loop

### Claude's Discretion
- Exact income tick interval within 5-6 second range
- Specific diminishing returns formula for citizen income scaling
- Exact category tier cost ratios (Claude calibrates in spreadsheet)
- Outer vs inner row cost multiplier amount
- Spreadsheet format (CSV, markdown table, etc.)
- EconomyManager Autoload architecture and tick implementation
- Floating number animation specifics (drift speed, fade, font)
- Counter flash color and duration
- Hover tooltip layout and positioning

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| ECON-01 | Player starts with enough credits to place a few rooms on an empty ring | EconomyConfig.StartingCredits already exists; spreadsheet will calibrate the value so 5-6 cheap rooms are affordable; EconomyManager initializes balance from config on game start |
| ECON-02 | Each citizen generates a small passive credit income over time | EconomyManager Timer tick with diminishing-returns formula over citizen count; starter trickle at zero citizens via a BaseStationIncome config field |
| ECON-03 | Citizens assigned to work rooms generate bonus credits | EconomyConfig.WorkBonusMultiplier (exists, needs value adjustment to ~1.25); EconomyManager exposes a method for Phase 5 to register working citizens; calculated during each tick |
| ECON-04 | Higher station happiness slightly multiplies credit generation | EconomyConfig.HappinessMultiplierCap (exists, needs value adjustment to ~1.3); happiness multiplier = 1 + (happiness * (cap - 1)); applied as final multiplier on tick income |
| ECON-05 | Room costs scale by segment size with diminishing returns for larger rooms | Cost formula: baseCost * categoryMultiplier * segments * (sizeDiscount ^ (segments - 1)) * rowMultiplier; SizeDiscountFactor adjusted from 0.85 to ~0.92; per-room BaseCostOverride in RoomDefinition |
</phase_requirements>

## Standard Stack

### Core
| Library/Component | Version | Purpose | Why Standard |
|-------------------|---------|---------|--------------|
| Godot Engine | 4.6.1 | Game engine, scene tree, rendering, UI | Project engine (confirmed in .csproj: Godot.NET.Sdk/4.6.1) |
| .NET | 10.0 | C# runtime | Project target framework (confirmed in .csproj) |
| Godot Timer node | Built-in | Periodic income tick every ~5.5s | Simpler and more reliable than SceneTreeTimer for repeating ticks; auto-pauses with scene tree |
| Godot Tween (CreateTween) | Built-in | Rolling counter animation, floating numbers, flash effects | Built-in tweening is sufficient; no external library needed for these simple animations |
| Godot Resource + [Export] | Built-in | EconomyConfig inspector-editable data | Already established pattern in project (EconomyConfig.cs, RoomDefinition.cs) |

### Supporting
| Component | Purpose | When to Use |
|-----------|---------|-------------|
| SafeNode base class | Event subscribe/unsubscribe lifecycle | CreditHUD extends SafeNode to connect to GameEvents.CreditsChanged |
| GameEvents Autoload | Cross-system event bus | EconomyManager emits CreditsChanged; HUD subscribes |
| SegmentGrid constants | Outer/inner row radii for cost differential | OuterRadius=6.0 vs InnerRowOuter=4.0 informs outer row cost multiplier |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Timer node for tick | SceneTreeTimer + await loop | SceneTreeTimer is one-shot and needs re-creation each tick; Timer node is cleaner for repeating events and auto-pauses correctly |
| Built-in Tween | GTweensGodot library | External dependency for simple number interpolation; built-in CreateTween is sufficient and matches zero-dependency project style |
| Markdown spreadsheet | Google Sheets / LibreOffice | Markdown table stays in-repo, version-controlled, reviewable in PRs; no external tool dependency |

## Architecture Patterns

### Recommended Project Structure
```
Scripts/
├── Autoloads/
│   ├── GameEvents.cs          # Existing signal bus
│   └── EconomyManager.cs      # NEW: credit balance, tick income, cost calculation
├── Core/
│   └── SafeNode.cs            # Existing base class
├── Data/
│   ├── EconomyConfig.cs       # Existing Resource (needs new fields)
│   └── RoomDefinition.cs      # Existing Resource (needs BaseCostOverride field)
└── UI/
    ├── CreditHUD.cs           # NEW: top-right credit display + rolling counter
    ├── FloatingText.cs        # NEW: drifting +N/-N text on spend/refund
    └── SegmentTooltip.cs      # Existing

Resources/
└── Economy/
    └── default_economy.tres   # NEW: EconomyConfig .tres instance

Scenes/
└── QuickTest/
    └── QuickTestScene.tscn    # Add HUD CanvasLayer with CreditHUD

.planning/phases/03-economy-foundation/
└── economy-balance.md         # NEW: balance spreadsheet (markdown)
```

### Pattern 1: EconomyManager Autoload with Timer Tick
**What:** Singleton Node registered in project.godot, owns the credit balance integer, creates a child Timer node for periodic income ticks, exposes public methods for Spend/Earn/Refund.
**When to use:** Central economy state that multiple systems read/write.
**Confidence:** HIGH — follows the exact GameEvents.Instance pattern already established.

```csharp
// EconomyManager.cs — follows GameEvents singleton pattern
public partial class EconomyManager : Node
{
    public static EconomyManager Instance { get; private set; }

    [Export] public EconomyConfig Config { get; set; }

    private int _credits;
    private Timer _incomeTimer;

    public int Credits => _credits;

    public override void _Ready()
    {
        Instance = this;
        _credits = Config.StartingCredits;

        // Create periodic income timer as child node
        _incomeTimer = new Timer();
        _incomeTimer.WaitTime = Config.IncomeTickInterval; // ~5.5s
        _incomeTimer.Autostart = true;
        _incomeTimer.OneShot = false;
        AddChild(_incomeTimer);
        _incomeTimer.Timeout += OnIncomeTick;

        GameEvents.Instance?.EmitCreditsChanged(_credits);
    }

    private void OnIncomeTick()
    {
        int income = CalculateTickIncome();
        _credits += income;
        GameEvents.Instance?.EmitCreditsChanged(_credits);
        // Also emit a separate event for the HUD to show the +N flash
    }

    public bool TrySpend(int amount) { /* deduct if affordable */ }
    public void Earn(int amount) { /* add credits */ }
    public void Refund(int amount) { /* add refund credits */ }
}
```

### Pattern 2: CreditHUD as SafeNode Control
**What:** A Control node that extends SafeNode, subscribes to `GameEvents.CreditsChanged`, and uses `CreateTween()` to animate the displayed number toward the new balance.
**When to use:** Any UI that reacts to economy events.
**Confidence:** HIGH — follows SafeNode pattern + existing SegmentTooltip UI approach.

```csharp
// CreditHUD.cs — subscribes to CreditsChanged via SafeNode lifecycle
public partial class CreditHUD : MarginContainer  // or PanelContainer
{
    // Note: Cannot extend SafeNode (Node) and MarginContainer (Control) simultaneously.
    // Instead, manually implement Subscribe/Unsubscribe in _EnterTree/_ExitTree.

    private Label _balanceLabel;
    private float _displayedCredits;

    protected void SubscribeEvents()
    {
        GameEvents.Instance.CreditsChanged += OnCreditsChanged;
    }

    protected void UnsubscribeEvents()
    {
        GameEvents.Instance.CreditsChanged -= OnCreditsChanged;
    }

    private void OnCreditsChanged(int newBalance)
    {
        // Animate from current displayed value to new balance
        var tween = CreateTween();
        tween.TweenMethod(
            Callable.From<float>(SetDisplayedCredits),
            _displayedCredits,
            (float)newBalance,
            0.4f  // rolling counter duration
        );
        _displayedCredits = newBalance;
    }

    private void SetDisplayedCredits(float value)
    {
        _balanceLabel.Text = ((int)value).ToString("N0");
    }
}
```

### Pattern 3: Floating Text via Tween + QueueFree
**What:** A Label node spawned at a screen position, tweened upward with fading opacity, then freed.
**When to use:** Visual feedback for credit spend/refund events.
**Confidence:** HIGH — well-established Godot pattern for damage/credit numbers.

```csharp
// FloatingText.cs — spawned by CreditHUD on spend/refund
public partial class FloatingText : Label
{
    public void Setup(string text, Color color, Vector2 startPos)
    {
        Text = text;
        Position = startPos;
        AddThemeColorOverride("font_color", color);

        var tween = CreateTween();
        tween.SetParallel(true);
        // Drift upward
        tween.TweenProperty(this, "position:y", startPos.Y - 60f, 1.0f)
             .SetEase(Tween.EaseType.Out);
        // Fade out
        tween.TweenProperty(this, "modulate:a", 0.0f, 1.0f)
             .SetEase(Tween.EaseType.In)
             .SetDelay(0.3f);
        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
```

### Pattern 4: Cost Calculation as Pure Function
**What:** Static or instance method on EconomyManager that computes room placement cost from RoomDefinition + segment count + row. No side effects — just returns the integer cost.
**When to use:** Called by Phase 4 room placement UI to display cost preview, and by TrySpend to deduct.
**Confidence:** HIGH — pure functions are testable and predictable.

```csharp
public int CalculateRoomCost(RoomDefinition room, int segmentCount, bool isOuterRow)
{
    float baseCost = room.BaseCostOverride > 0
        ? room.BaseCostOverride
        : Config.BaseRoomCost;

    float categoryMult = GetCategoryMultiplier(room.Category);
    float sizeFactor = segmentCount * MathF.Pow(Config.SizeDiscountFactor, segmentCount - 1);
    float rowMult = isOuterRow ? Config.OuterRowCostMultiplier : 1.0f;

    return Mathf.RoundToInt(baseCost * categoryMult * sizeFactor * rowMult);
}
```

### Anti-Patterns to Avoid
- **Hardcoded economy numbers in C# code:** All values MUST live in EconomyConfig Resource. The decisions explicitly require Inspector-editable parameters.
- **Smooth per-frame income accumulation:** User locked periodic chunk ticks (5-6 seconds). Do NOT use `_Process(delta)` to accumulate fractional credits.
- **SafeNode inheritance for Control nodes:** SafeNode extends Node, but HUD controls extend Control/Container. Cannot use multiple inheritance. Manually implement the subscribe/unsubscribe pattern in _EnterTree/_ExitTree instead.
- **Using GameEvents for internal EconomyManager state:** The Timer → income tick is internal to EconomyManager. Only the resulting CreditsChanged event goes through GameEvents.
- **Calculating costs at runtime from scratch each frame:** Cost calculation is stateless and cheap but should still be called on-demand (when placing or previewing), not every frame.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Periodic tick timing | Custom delta accumulator in _Process | Godot Timer node as child of EconomyManager | Timer handles pause, drift-free intervals, and cleanup automatically |
| Number animation (rolling counter) | Manual lerp in _Process with state flags | CreateTween().TweenMethod() | Tween handles easing, duration, cancellation; avoids stale state bugs |
| Floating text lifecycle | Manual tracking array + timer | Label + CreateTween() + QueueFree callback | Tween chains handle the full spawn-animate-destroy lifecycle cleanly |
| Number formatting (1,250) | String concatenation with manual commas | int.ToString("N0") | .NET built-in formatting handles locale-correct thousand separators |

**Key insight:** Godot's built-in Timer and Tween systems handle the tricky parts (pause awareness, delta accumulation, cleanup) that custom solutions always get wrong in edge cases.

## Common Pitfalls

### Pitfall 1: Runaway Positive Feedback Loop
**What goes wrong:** More citizens → more income → more rooms → more citizens → exponential growth, game becomes trivially easy at ~15-20 minutes.
**Why it happens:** Linear income scaling per citizen with no cap or diminishing returns. The STATE.md explicitly flags this concern.
**How to avoid:** Use square-root diminishing returns on citizen income: `income = baseIncome + perCitizen * sqrt(citizenCount)`. The spreadsheet must validate that income at 30 citizens is meaningful but not overwhelming. Combined with the ~1.3x happiness cap, growth stays gentle.
**Warning signs:** If 30-citizen income is more than 10x the 5-citizen income, the curve is too steep.

### Pitfall 2: Starting Credits Too Low
**What goes wrong:** Player places 2-3 rooms, runs out of credits, must wait passively for income with nothing to do. Breaks the "never stuck" cozy promise.
**Why it happens:** Current default is 500 credits with BaseRoomCost of 100. If category multipliers make the cheapest room (Housing) cost 60-80, the player can only place 6-8 rooms — but if costs include outer row premium and multi-segment sizes, the budget shrinks fast.
**How to avoid:** Spreadsheet must verify that starting credits afford exactly 5-6 cheap (1-segment Housing/Life Support) rooms on inner row. Adjust StartingCredits accordingly (likely 600-800 range).
**Warning signs:** If cheapest 1-segment inner-row room costs more than StartingCredits / 5, starting credits need to increase.

### Pitfall 3: Timer Continues After Scene Change / Pause
**What goes wrong:** Income ticks accumulate during pause menu or scene transitions, giving player unexpected credit jumps.
**Why it happens:** SceneTreeTimer can be configured to process always; Timer node defaults to pausing with tree but needs explicit consideration.
**How to avoid:** Use Timer node (not SceneTreeTimer) as a child of EconomyManager. Timer nodes automatically pause when the scene tree is paused. Set `ProcessMode = ProcessModeEnum.Pausable` explicitly on EconomyManager.
**Warning signs:** Credits change while the game is paused.

### Pitfall 4: Tween Stacking on Rapid Credit Changes
**What goes wrong:** If credits change rapidly (e.g., placing multiple rooms quickly), multiple rolling-counter tweens stack and the displayed number oscillates or overshoots.
**Why it happens:** Each CreditsChanged event creates a new tween without killing the previous one.
**How to avoid:** Store the active tween reference and call `tween.Kill()` before creating a new one. Always tween FROM the current displayed value TO the new target.
**Warning signs:** The credit counter shows numbers jumping back and forth, or settling at wrong values.

### Pitfall 5: EconomyConfig Resource Not Assigned
**What goes wrong:** EconomyManager.Config is null at runtime because no .tres file was assigned in the Inspector, causing NullReferenceException on first tick.
**Why it happens:** [Export] Resource fields default to null until a .tres instance is assigned in the scene/autoload properties.
**How to avoid:** Create a `default_economy.tres` Resource instance during implementation. In EconomyManager._Ready(), add a null check with a fallback: `Config ??= new EconomyConfig();` with sensible defaults. Log a warning if the fallback is used.
**Warning signs:** Economy works in editor but crashes in exported builds where the Resource wasn't saved.

### Pitfall 6: Integer Overflow / Rounding Errors in Cost Formula
**What goes wrong:** Cost calculations using float math can produce unexpected rounding (e.g., 99 instead of 100) or, in extreme edge cases, overflow.
**Why it happens:** Chaining float multipliers (category * size * row * discount) accumulates floating-point error before casting to int.
**How to avoid:** Use `Mathf.RoundToInt()` (not truncation cast `(int)`) for the final cost. Keep intermediate calculations in float, round only at the end. Credit balance stays as int.
**Warning signs:** Room costs display as 99 or 101 when they should be clean round numbers.

## Code Examples

### Timer-Based Income Tick (Godot 4 C#)
```csharp
// Source: Godot Timer docs + established project Autoload pattern
// Create a repeating Timer as a child node of the Autoload
var timer = new Timer();
timer.WaitTime = 5.5;      // seconds between income ticks
timer.OneShot = false;      // repeating
timer.Autostart = true;     // starts immediately
AddChild(timer);            // makes it part of scene tree (pause-aware)
timer.Timeout += OnIncomeTick;
```

### Rolling Counter Animation (Godot 4 C#)
```csharp
// Source: Godot Tween docs - TweenMethod for numeric interpolation
// Kill any existing tween to prevent stacking
_activeTween?.Kill();
_activeTween = CreateTween();
_activeTween.TweenMethod(
    Callable.From<float>(val => {
        _balanceLabel.Text = ((int)val).ToString("N0");
    }),
    _displayedValue,
    (float)newBalance,
    0.4f  // animation duration in seconds
).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
_displayedValue = newBalance;
```

### Floating Spend/Refund Number (Godot 4 C#)
```csharp
// Source: Godot floating combat text pattern adapted for economy
var floater = new Label();
floater.Text = amount > 0 ? $"+{amount}" : $"{amount}";
floater.AddThemeColorOverride("font_color",
    amount > 0 ? new Color(0.3f, 0.85f, 0.3f) : new Color(0.95f, 0.3f, 0.3f));
floater.Position = spawnPosition;

// Add to the CanvasLayer so it renders above 3D
_hudCanvasLayer.AddChild(floater);

var tween = floater.CreateTween();
tween.SetParallel(true);
tween.TweenProperty(floater, "position:y",
    spawnPosition.Y - 50f, 0.8f).SetEase(Tween.EaseType.Out);
tween.TweenProperty(floater, "modulate:a",
    0.0f, 0.8f).SetEase(Tween.EaseType.In).SetDelay(0.2f);
tween.SetParallel(false);
tween.TweenCallback(Callable.From(floater.QueueFree));
```

### Counter Flash Effect (Godot 4 C#)
```csharp
// Brief color pulse on income tick — subtle warm glow
var flash = CreateTween();
_balanceLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.92f, 0.6f)); // warm gold
flash.TweenProperty(_balanceLabel, "theme_override_colors/font_color",
    _defaultFontColor, 0.5f).SetEase(Tween.EaseType.Out);
```

### Diminishing Returns Formula for Citizen Income
```csharp
// Square root scaling: income grows but flattens as citizen count rises
// At 0 citizens: baseStationIncome (trickle)
// At 5 citizens: base + perCitizen * sqrt(5) ≈ base + perCitizen * 2.24
// At 15 citizens: base + perCitizen * sqrt(15) ≈ base + perCitizen * 3.87
// At 30 citizens: base + perCitizen * sqrt(30) ≈ base + perCitizen * 5.48
float citizenIncome = Config.PassiveIncomePerCitizen * MathF.Sqrt(citizenCount);
float baseIncome = Config.BaseStationIncome; // trickle even at 0 citizens
float happinessMultiplier = 1.0f + (currentHappiness * (Config.HappinessMultiplierCap - 1.0f));
float workBonus = workingCitizenCount * Config.WorkBonusMultiplier; // flat bonus per worker

int tickIncome = Mathf.RoundToInt((baseIncome + citizenIncome + workBonus) * happinessMultiplier);
```

### Room Cost Calculation
```csharp
// Category tier multipliers (Housing cheapest, Utility most expensive)
private float GetCategoryMultiplier(RoomDefinition.RoomCategory category) => category switch
{
    RoomDefinition.RoomCategory.Housing     => 0.7f,
    RoomDefinition.RoomCategory.LifeSupport => 0.85f,
    RoomDefinition.RoomCategory.Work        => 1.0f,
    RoomDefinition.RoomCategory.Comfort     => 1.15f,
    RoomDefinition.RoomCategory.Utility     => 1.3f,
    _ => 1.0f
};

// Cost formula with size discount and row premium
// 1-seg Housing inner: 100 * 0.7 * 1 * 1.0 * 1.0 = 70
// 2-seg Housing inner: 100 * 0.7 * 2 * 0.92 * 1.0 = 129 (vs 140 without discount = 8% savings)
// 3-seg Housing inner: 100 * 0.7 * 3 * 0.92^2 * 1.0 = 178 (vs 210 without discount = 15% savings)
// 1-seg Utility outer: 100 * 1.3 * 1 * 1.0 * 1.1 = 143
public int CalculateRoomCost(RoomDefinition room, int segments, bool isOuterRow)
{
    float baseCost = room.BaseCostOverride > 0 ? room.BaseCostOverride : Config.BaseRoomCost;
    float catMult = GetCategoryMultiplier(room.Category);
    float sizeFactor = segments * MathF.Pow(Config.SizeDiscountFactor, segments - 1);
    float rowMult = isOuterRow ? Config.OuterRowCostMultiplier : 1.0f;
    return Mathf.RoundToInt(baseCost * catMult * sizeFactor * rowMult);
}
```

### EconomyConfig Resource Fields (Extended)
```csharp
// New fields needed beyond what currently exists in EconomyConfig.cs
[ExportGroup("Income")]
[Export] public float BaseStationIncome { get; set; } = 1.0f;        // NEW: trickle at 0 citizens
[Export] public float PassiveIncomePerCitizen { get; set; } = 2.0f;  // ADJUSTED: per-sqrt-citizen
[Export] public float WorkBonusMultiplier { get; set; } = 1.25f;     // ADJUSTED: from 1.5
[Export] public float HappinessMultiplierCap { get; set; } = 1.3f;   // ADJUSTED: from 2.0
[Export] public float IncomeTickInterval { get; set; } = 5.5f;       // NEW: seconds between ticks

[ExportGroup("Costs")]
[Export] public int BaseRoomCost { get; set; } = 100;
[Export] public float SizeDiscountFactor { get; set; } = 0.92f;       // ADJUSTED: from 0.85
[Export] public float DemolishRefundRatio { get; set; } = 0.5f;
[Export] public float OuterRowCostMultiplier { get; set; } = 1.1f;    // NEW: outer row premium

[ExportGroup("Category Cost Multipliers")]
[Export] public float HousingCostMultiplier { get; set; } = 0.7f;     // NEW
[Export] public float LifeSupportCostMultiplier { get; set; } = 0.85f; // NEW
[Export] public float WorkCostMultiplier { get; set; } = 1.0f;        // NEW
[Export] public float ComfortCostMultiplier { get; set; } = 1.15f;    // NEW
[Export] public float UtilityCostMultiplier { get; set; } = 1.3f;     // NEW

[ExportGroup("Starting Values")]
[Export] public int StartingCredits { get; set; } = 750;              // ADJUSTED: from 500
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Godot 3 Tween node (add to tree) | Godot 4 CreateTween() (no node needed) | Godot 4.0 | Tweens are now method-based, no node allocation; lighter weight |
| [Signal] attribute for C# events | Pure C# event delegates | Project decision (Phase 1) | Avoids marshalling overhead and IsConnected bugs; established pattern |
| Hardcoded game constants | [Export] Resource subclasses | Project decision (Phase 1) | Inspector-editable, hot-reloadable, version-controlled .tres files |

**Deprecated/outdated:**
- Godot 3 `Tween` node: Replaced by `CreateTween()` in Godot 4. Do not instantiate Tween as a node.
- `SceneTreeTimer` for repeating tasks: SceneTreeTimer is one-shot; use Timer node for repeating periodic events.

## Open Questions

1. **RoomDefinition.BaseCostOverride field**
   - What we know: Context says "room costs vary by type within each category" — some rooms cost more. RoomDefinition currently has no cost field.
   - What's unclear: Should this be a BaseCostOverride on RoomDefinition, or should each room's cost be entirely derived from its category + a per-room multiplier?
   - Recommendation: Add an optional `BaseCostOverride` int field (default 0 = use EconomyConfig.BaseRoomCost) to RoomDefinition. This gives per-room cost tuning without duplicating the formula. Spreadsheet calibrates these values.

2. **GameEvents expansion for income tick display**
   - What we know: CreditsChanged(int newBalance) exists. The HUD needs to know *how much* was earned per tick to show the "+N" floating number.
   - What's unclear: Should there be a separate `IncomeTicked(int amount)` event, or should CreditsChanged carry both old and new balance?
   - Recommendation: Add a new `IncomeTicked(int amount)` event to GameEvents. The HUD subscribes to this for floating "+N" display. CreditsChanged stays as-is for balance updates. Similarly, add `CreditsSpent(int amount)` and `CreditsRefunded(int amount)` for floating "-N" and "+N" on spend/demolish.

3. **Hover tooltip for income breakdown**
   - What we know: User wants click/hover on the credit counter to show a breakdown (base income, citizen income, work bonus components).
   - What's unclear: Whether this tooltip should be built in Phase 3 or deferred to Phase 8 (full HUD wiring), since citizens don't exist yet in Phase 3.
   - Recommendation: Build a minimal tooltip in Phase 3 that shows "Base income: +1/tick" and "Total: +1/tick". When citizens arrive (Phase 5), the tooltip will naturally expand to show citizen and work components. The tooltip infrastructure should be in place now.

4. **Credit HUD scene structure**
   - What we know: SegmentTooltip is already in a CanvasLayer (layer 10) in QuickTestScene.tscn. The credit HUD needs to be in a similar CanvasLayer.
   - What's unclear: Whether to reuse the existing TooltipLayer or create a new HUD CanvasLayer.
   - Recommendation: Create a separate "HUDLayer" CanvasLayer (layer 5, below tooltip layer 10) for the credit display. This keeps HUD and tooltips at different z-orders and allows independent management.

## Sources

### Primary (HIGH confidence)
- Existing codebase: `Scripts/Data/EconomyConfig.cs`, `Scripts/Autoloads/GameEvents.cs`, `Scripts/Core/SafeNode.cs` — established patterns for Resource configs, event bus, and lifecycle
- `project.godot` — confirms Godot 4.6, GameEvents autoload registration pattern, .NET 10.0
- `.planning/phases/03-economy-foundation/03-CONTEXT.md` — locked user decisions on income rhythm, cost curve, HUD design
- `.planning/STATE.md` — Phase 3 flag about runaway feedback loop concern

### Secondary (MEDIUM confidence)
- [Godot Timer docs](https://docs.godotengine.org/en/stable/classes/class_timer.html) — Timer node API for repeating ticks
- [Godot Tween docs](https://docs.godotengine.org/en/stable/classes/class_tween.html) — CreateTween, TweenMethod, TweenProperty C# API
- [Godot C# exported properties](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_exports.html) — [Export] attribute and Resource pattern
- [Godot 4.6 Autoload docs](https://docs.godotengine.org/en/4.6/tutorials/scripting/singletons_autoload.html) — Singleton registration pattern
- [Floating combat text pattern](https://kidscancode.org/godot_recipes/4.x/ui/floating_text/index.html) — Label + Tween + QueueFree lifecycle

### Tertiary (LOW confidence)
- [Game economy diminishing returns](https://blog.nerdbucket.com/diminishing-returns-in-game-design/article) — Square root vs logarithmic scaling tradeoffs for idle/builder game income
- [Idle game mathematics (Kongregate)](https://gameanalytics.com/blog/idle-game-mathematics/) — Prestige system math; square root scaling requires 3-4x earning to double

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all components are built-in Godot 4 or already exist in the project
- Architecture: HIGH — patterns directly extend established codebase conventions (Autoload, SafeNode, Resource, GameEvents)
- Pitfalls: HIGH — runaway feedback loop explicitly flagged in STATE.md; Timer/Tween pitfalls well-documented in Godot docs
- Economy formulas: MEDIUM — square root diminishing returns is well-established in game design literature but specific coefficient values need spreadsheet validation
- HUD/UI specifics: MEDIUM — C# Tween API for number interpolation is documented but specific syntax (Callable.From) may need minor adjustments during implementation

**Research date:** 2026-03-03
**Valid until:** 2026-04-03 (stable domain — Godot 4.6 APIs, game economy math)
