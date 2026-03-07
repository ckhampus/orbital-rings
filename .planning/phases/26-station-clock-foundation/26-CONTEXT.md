# Phase 26: Station Clock Foundation - Context

**Gathered:** 2026-03-07
**Status:** Ready for planning

<domain>
## Phase Boundary

Station clock singleton with four time periods (Morning, Day, Evening, Night) in a configurable 8-minute cycle. Provides a time authority API (CurrentPeriod, PeriodProgress, PeriodChanged event) for downstream phases, and a HUD indicator showing the current period. No citizen behavior changes — purely clock infrastructure + visual indicator.

</domain>

<decisions>
## Implementation Decisions

### HUD Period Indicator
- Sun/moon Unicode emoji pair: ☀ for Morning/Day, ☽ for Evening/Night
- Text label alongside icon showing period name (e.g., "☀ Morning") — matches mood tier label pattern
- Positioned rightmost in top-right HUD cluster: Credits | Population | Mood | Clock
- Scale pop animation (1.15x bounce) on period change — consistent with MoodHUD tier-change pattern
- Warm period-specific color palette for icon + label:
  - Morning = soft gold
  - Day = warm white
  - Evening = amber/coral
  - Night = soft blue
- Same font size as other HUD elements (20)

### Period Proportions
- All four periods equal length (2 minutes each in default 8-minute cycle)
- 8-minute total cycle as default, fully tunable via ClockConfig Inspector resource
- Each period's share adjustable in Inspector without code changes
- New games always start at Morning
- Clock pauses when game is paused (follows Godot process mode)

### Period Transition
- Instant snap at period boundaries — no blend/transition state from the clock itself
- HUD animation only on period change (scale pop) — no floating text notification
- Phase 27 lighting can use PeriodProgress to lerp smoothly on its own

### Clock API
- Exposes StationClock.PeriodProgress (0.0–1.0 normalized within each period)
- Exposes StationClock.CurrentPeriod enum
- PeriodChanged event via GameEvents signal bus
- Downstream phases (27, 29, 30) consume these for lighting and behavior

### Clock Prominence
- Ambient background feel — time is felt, not watched
- No player time controls (no pause/speed up) — clock runs like weather
- No citizen behavior changes in this phase — purely infrastructure for Phase 29/30

### Claude's Discretion
- Exact emoji/Unicode character choice for sun/moon icons
- Exact color values for the four period colors (warm palette direction is locked)
- ClockConfig resource field naming and organization
- PeriodChanged event signature details
- Whether PeriodProgress uses _Process or _PhysicsProcess

</decisions>

<specifics>
## Specific Ideas

- Icon + label pattern should match existing HUD widgets (heart ♥ + count, house ⌂ + count, tier label)
- Period colors follow the same warm pastel philosophy as mood tier colors (soft grey-blue, warm tan, amber, coral, gold)
- Clock is ambient — player shouldn't feel time pressure or optimization anxiety from knowing the period

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- MoodHUD (Scripts/UI/MoodHUD.cs): Programmatic HUD widget pattern with scale pop and color cross-fade — ClockHUD follows same structure
- FloatingText (Scripts/UI/FloatingText.cs): Reusable notification spawner — not used for clock but available
- GameEvents (Scripts/Autoloads/GameEvents.cs): Signal bus for PeriodChanged event
- HousingConfig/EconomyConfig pattern: Inspector-tunable .tres resources — ClockConfig follows same approach

### Established Patterns
- Autoload singleton: `public static StationClock Instance { get; private set; }` set in _Ready()
- HUD widgets: MarginContainer subclass, programmatic child creation in _Ready(), event subscription to GameEvents
- Config resources: Godot Resource subclass with [Export] fields, loaded via Inspector
- Kill-before-create tween pattern for animations

### Integration Points
- project.godot: Add StationClock as 9th autoload (after SaveManager)
- QuickTestScene.tscn: Add ClockHUD to HUDLayer (CanvasLayer 5), rightmost position
- GameEvents.cs: Add PeriodChanged delegate and event
- SaveManager: Clock elapsed time saved/restored (Phase 31 handles full persistence, but basic position tracking needed)

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 26-station-clock-foundation*
*Context gathered: 2026-03-07*
