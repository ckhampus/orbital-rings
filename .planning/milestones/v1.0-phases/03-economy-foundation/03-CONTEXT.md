# Phase 3: Economy Foundation - Context

**Gathered:** 2026-03-03
**Status:** Ready for planning

<domain>
## Phase Boundary

Implement the credit economy: EconomyManager Autoload, passive income ticking, cost formulas with size discounts and category tiers, a balance spreadsheet modeling citizen milestones, and a minimal credit HUD. All economy parameters live in Inspector-editable EconomyConfig Resource files, not C# constants. Citizens don't exist yet — the economy runs on a starter income rate and is ready to plug into citizen income when Phase 5 lands.

</domain>

<decisions>
## Implementation Decisions

### Income Rhythm
- Periodic chunk ticks every 5-6 seconds (not smooth rolling)
- Each tick deposits a visible +N to the credit balance
- Small starter income with zero citizens (station itself generates a trickle, e.g., +1 per tick)
- Income scales with citizen count but with diminishing returns per additional citizen (prevents runaway loop)
- Subtle counter flash (brief color pulse/glow) on each income tick — no sound

### Economic Pacing
- Gentle constraint feel — player thinks about what to build next but never feels stuck or punished
- Happiness multiplier is subtle (~1.3x cap, not the current 2.0x default) — fulfilling wishes matters but income stays stable
- Work room bonus is small (1.2-1.3x, down from current 1.5x default) — less pressure to optimize assignments
- No session length target — spreadsheet ensures the curve feels right at each milestone
- Flat room costs — no inflation based on how many rooms exist
- Build once, no upkeep — paying for a room is a one-time cost, no ongoing drain
- 50% demolish refund (current default confirmed)

### Credit Display
- Top-right corner of screen — icon (coin/crystal) + number (e.g., ⭐ 1,250)
- Count up/down animation when credits change (rolling counter like a slot machine)
- Subtle flash on income ticks
- Floating numbers on spend/refund: "-100" drifts up on room placement, "+50" on demolish
- Income rate NOT shown next to balance — balance only; hover/click the counter for a breakdown tooltip showing base income, citizen income, work bonus components
- This is a minimal economy HUD for Phase 3; full HUD wiring (happiness, population) is Phase 8

### Cost Curve
- Slight size discount for multi-segment rooms (~5-10% per additional segment, down from current 0.85 factor)
- Room costs vary by type within each category (not flat per category) — some rooms cost more and are more interesting
- Category cost tiers (cheapest to most expensive): Housing < Life Support < Work < Comfort < Utility
- Outer row segments cost slightly more than inner row (outer segments are physically larger due to larger radius)
- Starting credits enough to place 5-6 cheap rooms (Housing/Life Support) on an empty ring — may need to increase from current 500 default
- Unlocked rooms (from Phase 7 happiness milestones) are NOT more expensive — progression gives variety, not cost escalation

### Balance Spreadsheet
- Must be produced before any numbers are hardcoded in code
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

</decisions>

<specifics>
## Specific Ideas

- Floating "-100" on spend / "+50" on demolish — playful, visible feedback without being noisy
- Income breakdown on hover, not shown by default — keeps HUD minimal but information accessible
- Housing is the cheapest category — player naturally starts by giving citizens a place to live
- No upkeep, no inflation, no punishment — the economy only ever grows, matching the cozy promise
- Outer row costing more adds a light spatial strategy element tied to the ring's physical geometry

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- EconomyConfig (Scripts/Data/EconomyConfig.cs) — Already exists with PassiveIncomePerCitizen, WorkBonusMultiplier, HappinessMultiplierCap, BaseRoomCost, SizeDiscountFactor, DemolishRefundRatio, StartingCredits. Default values need adjustment per decisions (HappinessMultiplierCap: 2.0→~1.3, WorkBonusMultiplier: 1.5→~1.25, SizeDiscountFactor: 0.85→~0.92)
- GameEvents (Scripts/Autoloads/GameEvents.cs) — Already has CreditsChanged(int) and HappinessChanged(float) events ready for economy use
- RoomDefinition (Scripts/Data/RoomDefinition.cs) — Has Category enum (Housing, LifeSupport, Work, Comfort, Utility), MinSegments/MaxSegments, BaseCapacity, Effectiveness
- SafeNode (Scripts/Core/SafeNode.cs) — Base class for signal lifecycle; EconomyManager should extend this
- SegmentGrid (Scripts/Ring/SegmentGrid.cs) — Has outer/inner row radii (OuterRowInner=5.0, OuterRadius=6.0 vs InnerRadius=3.0, InnerRowOuter=4.0) that can inform outer vs inner cost differential

### Established Patterns
- Pure C# event delegates for cross-system communication (not Godot [Signal])
- Autoload singleton pattern (GameEvents.Instance) — EconomyManager should follow this
- Inspector-editable Resource subclasses for data configuration
- SafeNode subscribe/unsubscribe for event lifecycle

### Integration Points
- EconomyManager registers as Autoload in project.godot (alongside GameEvents)
- EconomyManager fires GameEvents.CreditsChanged on every balance update
- EconomyManager reads EconomyConfig Resource for all balance parameters
- Future Phase 4 (room placement) will call EconomyManager to charge/refund credits
- Future Phase 5 (citizens) will register citizens with EconomyManager for income calculation
- Credit HUD Control node subscribes to GameEvents.CreditsChanged

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 03-economy-foundation*
*Context gathered: 2026-03-03*
