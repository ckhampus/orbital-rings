# Phase 10: Happiness Core and Mood Tiers - Context

**Gathered:** 2026-03-04
**Status:** Ready for planning

<domain>
## Phase Boundary

Refactor HappinessManager from a single happiness float (0-1, diminishing returns) to a dual-value system: lifetime happiness (integer wish counter, monotonic) + station mood (float, fluctuating with decay/baseline). Implement five mood tiers with hysteresis. Switch blueprint unlocks from percentage thresholds to wish-count thresholds (4, 12). Arrivals/economy integration is Phase 11; HUD replacement is Phase 13.

</domain>

<decisions>
## Implementation Decisions

### Mood Pacing
- Very gentle decay (cozy-first): ~5 minutes of inactivity to drop one tier
- Frame-based exponential smoothing for decay (in `_Process`, not timer ticks) — smooth continuous drift toward baseline
- Small mood bumps per wish (~5-8% of range) — reaching higher tiers requires sustained activity
- Flat gain: every wish gives the same mood boost regardless of wish type
- Instant mood jump on wish fulfillment (no tween on the data value — HUD can animate separately in Phase 13)
- Hard cap at 1.0 — no overflow above max

### Tier Distribution
- Front-loaded tiers (easy to leave Quiet, hard to reach Radiant):
  - Quiet: 0.00 – 0.10
  - Cozy: 0.10 – 0.30
  - Lively: 0.30 – 0.55
  - Vibrant: 0.55 – 0.80
  - Radiant: 0.80 – 1.00
- Tier names confirmed: Quiet / Cozy / Lively / Vibrant / Radiant
- Medium hysteresis buffer (~0.05) on demotion boundaries to prevent tier oscillation
- New games start at mood 0.0 (Quiet tier)

### Baseline Growth
- Baseline caps around Cozy (~0.20) — even mature stations only rest above Quiet; every tier above Cozy requires active play
- Slow baseline creep: ~0.05 baseline after 10 wishes, ~0.15 after 30+ wishes
- Baseline is purely internal — player is not shown the floor value; they just notice the station doesn't drop as far
- Mood decay stops exactly at baseline (asymptotic approach, never undershoots)

### Event System
- Fire events only on mood tier changes (not per-frame mood updates)
- Replace old `HappinessChanged(float)` event now — add `MoodTierChanged` and `WishCountChanged` events
- HappinessBar will break (acceptable — Phase 13 replaces it entirely)

### Refactor Approach
- Extract MoodSystem as a plain C# class (POCO, not a Godot Node)
- HappinessManager creates and owns MoodSystem, passes delta time for decay in `_Process`
- MoodSystem encapsulates: mood value, baseline, tier calculation, hysteresis, decay logic
- HappinessManager retains: arrivals, housing capacity, blueprint unlocks, event wiring

### Configuration
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

</decisions>

<specifics>
## Specific Ideas

- Cozy-first philosophy: the station should feel forgiving. Mood decay is background ambience, not pressure. Player should never feel punished for stepping away.
- Baseline as invisible reward: player discovers organically that their station "rests higher" after many wishes. No explicit UI for this — it's a felt progression.
- v1.0 used diminishing returns (`gain = 0.08 / (1 + happiness)`) which saturated at ~50 wishes. v2 replaces this with flat gain + decay, keeping the loop alive indefinitely.

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `EconomyConfig` pattern (`Resources/Economy/default_economy.tres`): exact template for HappinessConfig — C# Resource class with `[Export]` fields
- `FloatingText` class: reusable for any future mood-related floating text
- `GameEvents` signal bus: add new events here following existing `Action<T>` delegate pattern

### Established Patterns
- Autoload singleton with `Instance` property — HappinessManager follows this
- `StateLoaded` flag pattern: prevents `_Ready()` from overwriting loaded state
- Kill-before-create tween pattern (from HappinessBar) — not needed in Phase 10 but relevant for Phase 13
- Timer-based periodic checks (arrival timer) — keep this pattern for arrivals, use `_Process` for mood decay

### Integration Points
- `HappinessManager.OnWishFulfilled(citizenName, wishType)` — entry point for mood gain + lifetime counter increment
- `EconomyManager.SetHappiness(float)` — currently takes 0-1 float; Phase 11 will update this to use tier-based multiplier
- `GameEvents.HappinessChanged` — being replaced with `MoodTierChanged` + `WishCountChanged`
- `HappinessManager.RestoreState()` — needs updated signature for new dual values (Phase 12)
- `SaveManager` reads `Happiness` property — needs to read lifetime + mood + baseline (Phase 12)
- Blueprint unlock check in `CheckUnlockMilestones()` — switching from float threshold to wish count comparison

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 10-happiness-core-and-mood-tiers*
*Context gathered: 2026-03-04*
