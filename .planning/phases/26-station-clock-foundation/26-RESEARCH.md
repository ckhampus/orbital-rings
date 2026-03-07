# Phase 26: Station Clock Foundation - Research

**Researched:** 2026-03-07
**Domain:** Godot 4.6 C# autoload singleton, time accumulation, HUD widget, Inspector-tunable Resource
**Confidence:** HIGH

## Summary

This phase creates a station clock singleton (StationClock) that cycles through four time periods (Morning, Day, Evening, Night) in a configurable 8-minute loop, a HUD indicator showing the current period, and a GameEvents signal for period changes. The implementation follows well-established patterns already in the codebase: autoload singletons (EconomyManager, HappinessManager), HUD widgets (CreditHUD, PopulationDisplay, MoodHUD), Inspector-tunable Resource configs (EconomyConfig, HousingConfig, HappinessConfig), and the GameEvents signal bus pattern.

The codebase has 8 existing autoloads, consistent programmatic HUD widget construction, kill-before-create tween animation, and `[GlobalClass]` Resource subclasses with `[Export]` fields. Every pattern StationClock needs already exists in the project. The clock is pure time accumulation via `_Process(double delta)` with period boundary detection -- no physics, no complex state machines.

**Primary recommendation:** Follow existing codebase patterns exactly. StationClock is a new autoload with `_Process`-based delta accumulation, ClockConfig is a `[GlobalClass] Resource` with `[Export]` fields for period proportions and cycle length, ClockHUD is a `MarginContainer` with programmatic child creation following MoodHUD's exact structure, and PeriodChanged fires through the existing GameEvents signal bus.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- HUD shows sun/moon Unicode emoji pair: sun for Morning/Day, moon for Evening/Night
- Text label alongside icon showing period name (e.g., "sun Morning")
- Positioned rightmost in top-right HUD cluster: Credits | Population | Mood | Clock
- Scale pop animation (1.15x bounce) on period change, matching MoodHUD tier-change pattern
- Warm period-specific color palette: Morning = soft gold, Day = warm white, Evening = amber/coral, Night = soft blue
- Same font size as other HUD elements (20)
- All four periods equal length (2 minutes each in default 8-minute cycle)
- 8-minute total cycle as default, fully tunable via ClockConfig Inspector resource
- Each period's share adjustable in Inspector without code changes
- New games always start at Morning
- Clock pauses when game is paused (follows Godot process mode)
- Instant snap at period boundaries, no blend/transition state from clock itself
- HUD animation only on period change (scale pop), no floating text notification
- Phase 27 lighting can use PeriodProgress to lerp smoothly on its own
- Exposes StationClock.PeriodProgress (0.0-1.0 normalized within each period)
- Exposes StationClock.CurrentPeriod enum
- PeriodChanged event via GameEvents signal bus
- Downstream phases (27, 29, 30) consume these for lighting and behavior
- Ambient background feel, time is felt not watched
- No player time controls (no pause/speed up), clock runs like weather
- No citizen behavior changes in this phase, purely infrastructure for Phase 29/30

### Claude's Discretion
- Exact emoji/Unicode character choice for sun/moon icons
- Exact color values for the four period colors (warm palette direction is locked)
- ClockConfig resource field naming and organization
- PeriodChanged event signature details
- Whether PeriodProgress uses _Process or _PhysicsProcess

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| CLOCK-01 | Station cycles through Morning, Day, Evening, and Night periods in an 8-minute real-time loop | StationClock autoload with `_Process` delta accumulation, StationPeriod enum, period boundary detection, GameEvents.PeriodChanged signal |
| CLOCK-02 | Clock cycle length and period proportions are configurable via Inspector resource | ClockConfig `[GlobalClass] Resource` with `[Export]` fields for TotalCycleDuration and per-period proportion weights, loaded from `res://Resources/Clock/default_clock.tres` |
| CLOCK-03 | Player can see the current station period via an ambient icon in the HUD | ClockHUD `MarginContainer` widget in HUDLayer with period icon + label, scale pop animation on period change, warm period-specific colors |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Godot.NET.Sdk | 4.6.1 | Game engine + C# runtime | Project SDK, already in .csproj |
| System.Text.Json | (built-in) | Save data serialization | Already used by SaveManager for game state persistence |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Chickensoft.GoDotTest | 2.0.30 | Test framework | Already in .csproj, for clock unit tests |
| Shouldly | 4.3.0 | Assertion library | Already in .csproj, for readable test assertions |

No new packages needed. Everything required is already in the project.

**Installation:**
```bash
# No installation needed -- all dependencies already present
```

## Architecture Patterns

### Recommended Project Structure
```
Scripts/
├── Autoloads/
│   └── StationClock.cs        # Autoload singleton (9th, after SaveManager)
├── Data/
│   ├── StationPeriod.cs       # Enum: Morning, Day, Evening, Night
│   └── ClockConfig.cs         # [GlobalClass] Resource with [Export] fields
├── UI/
│   └── ClockHUD.cs            # MarginContainer HUD widget
└── Autoloads/
    └── GameEvents.cs          # Add PeriodChanged event (existing file)

Resources/
└── Clock/
    └── default_clock.tres     # Default ClockConfig instance

Scenes/
└── QuickTest/
    └── QuickTestScene.tscn    # Add ClockHUD to HUDLayer (existing file)

Tests/
└── Clock/
    └── ClockTests.cs          # Unit tests for clock logic
```

### Pattern 1: Autoload Singleton (StationClock)
**What:** Node-based singleton registered in project.godot, owns time accumulation logic
**When to use:** Cross-system state that must persist across scene changes and be globally queryable
**Example:**
```csharp
// Source: Existing pattern from EconomyManager.cs, HappinessManager.cs
namespace OrbitalRings.Autoloads;

public partial class StationClock : Node
{
    public static StationClock Instance { get; private set; }

    [Export] public ClockConfig Config { get; set; }

    public StationPeriod CurrentPeriod { get; private set; } = StationPeriod.Morning;
    public float PeriodProgress { get; private set; }  // 0.0-1.0 within current period

    private float _elapsedTime;

    public override void _Ready()
    {
        Instance = this;
        // Config loading follows EconomyManager pattern:
        // Inspector-assigned > ResourceLoader.Load > code defaults
        ProcessMode = ProcessModeEnum.Pausable;  // Pauses with game
    }

    public override void _Process(double delta)
    {
        _elapsedTime += (float)delta;
        // Wrap elapsed time within total cycle duration
        // Determine period from elapsed time and config proportions
        // Emit PeriodChanged via GameEvents when period boundary crossed
    }
}
```

### Pattern 2: Inspector-Tunable Resource (ClockConfig)
**What:** `[GlobalClass]` Resource subclass with `[Export]` fields, stored as .tres
**When to use:** Designer-tunable constants that should not be hard-coded
**Example:**
```csharp
// Source: Existing pattern from EconomyConfig.cs, HousingConfig.cs, HappinessConfig.cs
namespace OrbitalRings.Data;

[GlobalClass]
public partial class ClockConfig : Resource
{
    [ExportGroup("Cycle Timing")]

    /// <summary>Total cycle duration in seconds. Default 480 (8 minutes).</summary>
    [Export] public float TotalCycleDuration { get; set; } = 480.0f;

    [ExportGroup("Period Proportions")]

    /// <summary>Morning share weight. Default 1.0 (equal share).</summary>
    [Export] public float MorningWeight { get; set; } = 1.0f;

    /// <summary>Day share weight. Default 1.0 (equal share).</summary>
    [Export] public float DayWeight { get; set; } = 1.0f;

    /// <summary>Evening share weight. Default 1.0 (equal share).</summary>
    [Export] public float EveningWeight { get; set; } = 1.0f;

    /// <summary>Night share weight. Default 1.0 (equal share).</summary>
    [Export] public float NightWeight { get; set; } = 1.0f;
}
```

### Pattern 3: HUD Widget (ClockHUD)
**What:** MarginContainer with programmatic child creation in _Ready(), event subscription to GameEvents
**When to use:** Top-right HUD cluster elements
**Example:**
```csharp
// Source: Existing pattern from MoodHUD.cs, CreditHUD.cs, PopulationDisplay.cs
namespace OrbitalRings.UI;

public partial class ClockHUD : MarginContainer
{
    private Label _iconLabel;
    private Label _periodLabel;
    private Tween _popTween;

    public override void _Ready()
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 4);
        hbox.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(hbox);

        _iconLabel = new Label();
        _iconLabel.AddThemeFontSizeOverride("font_size", 20);
        _iconLabel.MouseFilter = MouseFilterEnum.Ignore;
        hbox.AddChild(_iconLabel);

        _periodLabel = new Label();
        _periodLabel.AddThemeFontSizeOverride("font_size", 20);
        _periodLabel.MouseFilter = MouseFilterEnum.Ignore;
        hbox.AddChild(_periodLabel);

        // Subscribe to PeriodChanged via GameEvents
        // Initialize from StationClock.Instance.CurrentPeriod
    }
}
```

### Pattern 4: GameEvents Signal Bus Extension
**What:** Pure C# event delegates with null-safe Emit helpers, organized in comment-delimited sections
**When to use:** Any cross-system communication
**Example:**
```csharp
// Source: Existing pattern from GameEvents.cs
// Add to GameEvents.cs in a new section:

// ---------------------------------------------------------------------------
// Clock Events (Phase 26)
// ---------------------------------------------------------------------------

/// <param name="newPeriod">The new station period.</param>
/// <param name="previousPeriod">The period before the change.</param>
public event Action<StationPeriod, StationPeriod> PeriodChanged;

public void EmitPeriodChanged(StationPeriod newPeriod, StationPeriod previousPeriod)
    => PeriodChanged?.Invoke(newPeriod, previousPeriod);
```

### Pattern 5: Config Loading (Three-Tier Fallback)
**What:** Inspector-assigned > ResourceLoader.Load from default path > code defaults
**When to use:** All config resources
**Example:**
```csharp
// Source: EconomyManager.cs lines 52-61
if (Config == null)
    Config = ResourceLoader.Load<ClockConfig>("res://Resources/Clock/default_clock.tres");
if (Config == null)
{
    GD.PushWarning("StationClock: No ClockConfig assigned or found at default path. Using code defaults.");
    Config = new ClockConfig();
}
```

### Anti-Patterns to Avoid
- **Timer node for clock ticks:** Do not use a Godot Timer for period transitions. Timer fires at fixed intervals but period durations are configurable and non-uniform. Use `_Process` delta accumulation with modular arithmetic instead.
- **Godot [Signal] for PeriodChanged:** The project uses pure C# events exclusively (see GameEvents.cs header comment). Never use `[Signal]` attributes -- they cause marshalling overhead and C#/engine boundary bugs.
- **Hard-coded period durations:** Never hard-code 120-second periods. All timing must flow from ClockConfig weights to support Inspector tuning (CLOCK-02).
- **Separate scene for ClockHUD:** HUD widgets are programmatic MarginContainers added to QuickTestScene.tscn, not separate .tscn scenes.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Time accumulation | Custom timer system | `_Process(double delta)` with float accumulation and modulo wrapping | Godot's `_Process` already respects `ProcessMode.Pausable` and handles frame-rate independence |
| Period duration calculation | Fixed duration per period | Weighted proportion system (weight / totalWeight * cycleDuration) | Supports non-uniform periods without code changes (CLOCK-02) |
| HUD layout management | Custom positioning system | Anchor presets + offset values in .tscn | Existing HUD elements use anchor_left=1.0 with negative offsets, consistent pattern |
| Scale pop animation | Custom animation player | Kill-before-create Tween pattern | MoodHUD already implements the exact 1.15x elastic bounce pattern to replicate |
| Cross-system events | Custom observer pattern | GameEvents signal bus | All 25+ events in the project use this pattern |

**Key insight:** Every subsystem this phase needs -- singleton lifecycle, config resources, HUD widgets, event bus, tween animation -- has an existing, working implementation in the codebase. The implementation is pattern replication, not invention.

## Common Pitfalls

### Pitfall 1: Floating Point Drift in Time Accumulation
**What goes wrong:** Accumulating `_elapsedTime += (float)delta` over many cycles causes precision loss, eventually drifting period boundaries.
**Why it happens:** 32-bit float loses precision after thousands of seconds. An 8-minute cycle running for 1 hour = 7.5 cycles = 3,600 seconds of accumulation.
**How to avoid:** Use modulo wrapping: `_elapsedTime %= totalCycleDuration` every frame (or at least every cycle boundary). This keeps the float value small and precise.
**Warning signs:** Periods getting longer or shorter after extended play sessions.

### Pitfall 2: Period Boundary Double-Fire
**What goes wrong:** PeriodChanged event fires twice for the same transition if period detection runs before and after a frame where the boundary is exactly crossed.
**Why it happens:** Comparing `previousPeriod != newPeriod` without tracking the last-emitted period separately from the computed period.
**How to avoid:** Track `_lastEmittedPeriod` as separate state (same pattern as HappinessManager's `_lastReportedTier`). Only emit when computed period differs from last emitted.
**Warning signs:** HUD flickers or animations play twice in succession.

### Pitfall 3: Autoload Ordering
**What goes wrong:** StationClock accesses GameEvents.Instance in `_Ready()` but GameEvents hasn't initialized yet.
**Why it happens:** Autoloads initialize in project.godot declaration order. If StationClock is listed before GameEvents, its `_Ready()` fires first.
**How to avoid:** Register StationClock AFTER SaveManager (last current autoload). GameEvents is first, so it will always be ready. The CONTEXT.md explicitly states: "Add StationClock as 9th autoload (after SaveManager)."
**Warning signs:** NullReferenceException on first frame.

### Pitfall 4: Kill-Before-Create Tween Leak
**What goes wrong:** Calling `_popTween = _periodLabel.CreateTween()` without first killing the previous tween causes multiple tweens to stack, producing chaotic animation.
**Why it happens:** Period changes could theoretically fire in rapid succession (e.g., during testing or if cycle duration is set very short in Inspector).
**How to avoid:** Always `_popTween?.Kill()` before creating a new tween. This is the standard pattern in MoodHUD, CreditHUD, and PopulationDisplay.
**Warning signs:** Animation jitters, scale gets stuck at non-1.0 values.

### Pitfall 5: ClearAllSubscribers Missing PeriodChanged
**What goes wrong:** Test isolation fails because PeriodChanged event retains stale subscribers between tests.
**Why it happens:** GameEvents.ClearAllSubscribers() must explicitly null the new PeriodChanged delegate field. Forgetting to add it to the clear method is easy.
**How to avoid:** Add `PeriodChanged = null;` to ClearAllSubscribers() at the same time as adding the event field.
**Warning signs:** Test failures that only occur when tests run in specific order.

### Pitfall 6: HUD Position Collision
**What goes wrong:** ClockHUD overlaps MoodHUD because offset_left values collide.
**Why it happens:** MoodHUD is at offset_left=-200. ClockHUD needs to be further right (closer to 0 or a small negative).
**How to avoid:** Current HUD positions: CreditHUD=-520, PopulationDisplay=-340, MoodHUD=-200. ClockHUD should be at approximately -80 to -100 to maintain consistent spacing.
**Warning signs:** Overlapping text in top-right corner.

## Code Examples

### StationPeriod Enum
```csharp
// Source: Follows MoodTier.cs pattern exactly
namespace OrbitalRings.Data;

/// <summary>
/// Station day cycle periods. Values are ordered chronologically
/// so arithmetic wrapping (current + 1) % 4 gives next period.
/// </summary>
public enum StationPeriod
{
    Morning = 0,
    Day = 1,
    Evening = 2,
    Night = 3,
}
```

### Period Calculation from Elapsed Time
```csharp
// Core algorithm: weighted proportional period determination
// Source: Novel for this project, but follows standard game dev day/night cycle pattern
private StationPeriod ComputePeriod(float elapsedTime, out float periodProgress)
{
    float totalWeight = Config.MorningWeight + Config.DayWeight
                      + Config.EveningWeight + Config.NightWeight;
    float cycleDuration = Config.TotalCycleDuration;

    // Normalize elapsed time within one cycle
    float t = elapsedTime % cycleDuration;

    // Compute period boundaries as cumulative fractions of cycle
    float[] weights = { Config.MorningWeight, Config.DayWeight,
                        Config.EveningWeight, Config.NightWeight };
    float cumulative = 0f;
    for (int i = 0; i < 4; i++)
    {
        float periodDuration = (weights[i] / totalWeight) * cycleDuration;
        if (t < cumulative + periodDuration)
        {
            periodProgress = (t - cumulative) / periodDuration;
            return (StationPeriod)i;
        }
        cumulative += periodDuration;
    }

    // Edge case: floating point at exact cycle end wraps to Morning
    periodProgress = 0f;
    return StationPeriod.Morning;
}
```

### ClockHUD Period Color Dictionary
```csharp
// Source: Follows MoodHUD.TierColors dictionary pattern
// Colors follow user-locked warm palette direction
private static readonly Dictionary<StationPeriod, Color> PeriodColors = new()
{
    { StationPeriod.Morning, new Color(0.95f, 0.85f, 0.45f) },  // soft gold
    { StationPeriod.Day,     new Color(0.95f, 0.93f, 0.90f) },  // warm white
    { StationPeriod.Evening, new Color(0.95f, 0.65f, 0.40f) },  // amber/coral
    { StationPeriod.Night,   new Color(0.55f, 0.70f, 0.90f) },  // soft blue
};
```

### ClockHUD Period Icons
```csharp
// Unicode sun/moon characters for period icons
// User locked: sun for Morning/Day, moon for Evening/Night
private static string GetPeriodIcon(StationPeriod period) => period switch
{
    StationPeriod.Morning => "\u2600",  // Black sun with rays (sun)
    StationPeriod.Day     => "\u2600",  // Black sun with rays (sun)
    StationPeriod.Evening => "\u263D",  // First quarter moon
    StationPeriod.Night   => "\u263D",  // First quarter moon
    _ => "\u2600",
};
```

### QuickTestScene.tscn ClockHUD Node
```
# Add to HUDLayer in QuickTestScene.tscn, after MoodHUD
# Follows exact same node structure as CreditHUD/PopulationDisplay/MoodHUD

[node name="ClockHUD" type="MarginContainer" parent="HUDLayer"]
anchors_preset = 1
anchor_left = 1.0
anchor_right = 1.0
offset_left = -80.0
offset_bottom = 50.0
grow_horizontal = 0
script = ExtResource("XX_clockhud")
```

### project.godot Autoload Entry
```ini
# Add after SaveManager line in [autoload] section
StationClock="*res://Scripts/Autoloads/StationClock.cs"
```

### TestHelper Integration
```csharp
// Add to GameEvents.ClearAllSubscribers():
PeriodChanged = null;

// Add to TestHelper.ResetAllSingletons():
StationClock.Instance?.Reset();
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Godot [Signal] attributes | Pure C# event delegates | Project convention since Phase 1 | Avoids marshalling bugs (GitHub #76690, #72994) |
| Separate .tscn per HUD widget | Programmatic child creation in _Ready() | Project convention since Phase 3 | Simpler, no external scene dependencies |
| Global configs as constants | [GlobalClass] Resource .tres files | Project convention since Phase 3 | Inspector-tunable without code changes |
| _EnterTree singleton init | _Ready singleton init | Phase 3 discovery | _EnterTree can fire before other autoloads in C# |

**Deprecated/outdated:**
- None specific to this phase. All patterns are current project conventions.

## Open Questions

1. **Unicode rendering of sun/moon characters**
   - What we know: The project uses Unicode characters for all HUD icons (star, heart, smiley). Godot's default font supports basic Unicode.
   - What's unclear: Whether U+2600 (sun) and U+263D (moon) render well at font size 20 in Godot's default font. Some Unicode astronomical symbols render as empty boxes in certain fonts.
   - Recommendation: Test U+2600 and U+263D first. Fallback options: U+25CB/U+25CF (circle outlines), or simple text like "[SUN]"/"[MOON]" if Unicode fails. The existing project uses U+2665 (heart), U+2605 (star), and U+263A (smiley) which all render correctly, so the default font has reasonable Unicode support.

2. **SaveManager integration scope for this phase**
   - What we know: CONTEXT.md mentions "basic position tracking needed" for SaveManager. Phase 31 handles full clock persistence (SAVE-01).
   - What's unclear: Whether StationClock should subscribe SaveManager to PeriodChanged for autosave triggers in this phase, or defer entirely to Phase 31.
   - Recommendation: Do NOT integrate with SaveManager in this phase. Phase 31 explicitly owns SAVE-01 ("Station clock position is saved and restored on load"). Adding SaveManager wiring now would be premature. StationClock should expose `ElapsedTime` as a readable property so Phase 31 can serialize it later.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Chickensoft.GoDotTest 2.0.30 + Shouldly 4.3.0 |
| Config file | `Orbital Rings.csproj` (conditional RUN_TESTS compilation) |
| Quick run command | `godot --headless --run-tests --quit-on-finish` |
| Full suite command | `godot --headless --run-tests --quit-on-finish` |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| CLOCK-01 | Clock cycles through Morning, Day, Evening, Night in sequence | unit | `godot --headless --run-tests --quit-on-finish` (ClockTests) | Wave 0 |
| CLOCK-01 | Clock wraps from Night back to Morning | unit | Same runner, specific test method | Wave 0 |
| CLOCK-01 | Total cycle is 8 minutes (480s) by default | unit | Same runner, specific test method | Wave 0 |
| CLOCK-02 | Custom cycle duration changes period lengths | unit | Same runner, specific test method | Wave 0 |
| CLOCK-02 | Non-uniform period weights produce correct durations | unit | Same runner, specific test method | Wave 0 |
| CLOCK-02 | ClockConfig fields are Inspector-visible | manual-only | Visual check in Godot editor | N/A |
| CLOCK-03 | ClockHUD displays current period name and icon | integration | Requires scene tree, visual verification | manual-only |
| CLOCK-03 | ClockHUD animates on period change | integration | Requires scene tree, visual verification | manual-only |

### Sampling Rate
- **Per task commit:** `dotnet format && dotnet build`
- **Per wave merge:** `godot --headless --run-tests --quit-on-finish` (full test suite)
- **Phase gate:** Full suite green + visual HUD verification in editor

### Wave 0 Gaps
- [ ] `Tests/Clock/ClockTests.cs` -- covers CLOCK-01, CLOCK-02 (period computation logic, cycle wrapping, weight proportions)
- [ ] StationClock.Reset() method -- needed for test isolation via TestHelper

## Sources

### Primary (HIGH confidence)
- **Codebase inspection** -- GameEvents.cs, MoodHUD.cs, CreditHUD.cs, PopulationDisplay.cs, EconomyConfig.cs, HousingConfig.cs, HappinessConfig.cs, EconomyManager.cs, HappinessManager.cs, SaveManager.cs, project.godot, QuickTestScene.tscn
- **MoodTier.cs** -- Enum pattern to replicate for StationPeriod
- **TestHelper.cs, GameTestClass.cs** -- Test infrastructure patterns

### Secondary (MEDIUM confidence)
- [Godot C# exported properties docs](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_exports.html) -- Confirms [Export] enum creates Inspector dropdown
- [Godot _Process delta time](https://kidscancode.org/godot_recipes/4.x/basics/understanding_delta/index.html) -- Confirms delta accumulation pattern for frame-rate independence

### Tertiary (LOW confidence)
- Unicode character rendering (U+2600, U+263D) in Godot's default font -- needs runtime verification

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- no new dependencies, all patterns exist in codebase
- Architecture: HIGH -- direct replication of EconomyManager/MoodHUD/EconomyConfig patterns
- Pitfalls: HIGH -- identified from actual codebase patterns (kill-before-create, ClearAllSubscribers, autoload ordering)
- HUD layout: MEDIUM -- offset_left=-80 is an estimate, may need adjustment based on text width
- Unicode icons: MEDIUM -- existing project uses similar Unicode chars successfully, but specific sun/moon glyphs need runtime verification

**Research date:** 2026-03-07
**Valid until:** 2026-04-07 (stable -- all patterns are project conventions, not external library APIs)
