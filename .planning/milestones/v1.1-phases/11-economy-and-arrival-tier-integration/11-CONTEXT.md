# Phase 11: Economy and Arrival Tier Integration - Context

**Gathered:** 2026-03-04
**Status:** Ready for planning

<domain>
## Phase Boundary

Wire the existing `HappinessManager.CurrentTier` (MoodTier enum, built in Phase 10) into two gameplay systems: citizen arrival probability and room income multiplier. Both systems currently use a continuous mood float; this phase replaces those with discrete per-tier values. HUD display is Phase 13; save format is Phase 12.

</domain>

<decisions>
## Implementation Decisions

### Arrival Probability Per Tier
- Back-loaded distribution — higher tiers deliver meaningfully more arrivals:
  - Quiet: 15% per 60s check
  - Cozy: 25% per 60s check
  - Lively: 40% per 60s check
  - Vibrant: 60% per 60s check
  - Radiant: 75% per 60s check
- Five Inspector-tunable float fields in HappinessConfig (same pattern as tier thresholds)
- Expected time between arrivals: ~7 min at Quiet, ~1.3 min at Radiant

### Arrival Check Interval
- Fixed 60s timer interval — do NOT change interval by tier
- Tier change updates the in-memory probability only; no timer reset
- Next natural 60s tick picks up the new probability automatically

### Income Multiplier Per Tier
- Even steps across all 5 tiers:
  - Quiet: 1.0x
  - Cozy: 1.1x
  - Lively: 1.2x
  - Vibrant: 1.3x
  - Radiant: 1.4x
- Five Inspector-tunable float fields in EconomyConfig (alongside existing HappinessMultiplierCap)
- EconomyConfig.HappinessMultiplierCap bumps from 1.3 to 1.4 (or can be removed in favor of the per-tier fields)

### API Integration
- Add `SetMoodTier(MoodTier tier)` to EconomyManager — replaces `SetHappiness(float)` for multiplier purposes
- HappinessManager subscribes to its own `MoodTierChanged` event (or calls directly on tier change) and calls `EconomyManager.Instance?.SetMoodTier(newTier)`
- `SetHappiness(float)` shim in EconomyManager can be removed once Phase 11 wires the new method
- Income multiplier lookup lives in EconomyManager (not HappinessManager) — economy logic stays in economy domain

### Claude's Discretion
- Exact field names in HappinessConfig and EconomyConfig for the per-tier values
- Whether to keep `SetHappiness()` as a deprecated stub or remove it entirely
- How HappinessManager calls EconomyManager on tier change (event subscription vs. direct call in OnWishFulfilled / _Process)

</decisions>

<specifics>
## Specific Ideas

No specific references — open to standard approaches.

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `HappinessConfig` (`Resources/Happiness/default_happiness.tres`): add five `ArrivalProbability{Tier}` float exports following existing `[ExportGroup]` pattern
- `EconomyConfig` (`Resources/Economy/default_economy.tres`): add five `IncomeMult{Tier}` float exports alongside existing `HappinessMultiplierCap`
- `GameEvents.EmitMoodTierChanged(newTier, previousTier)`: already fires on every tier transition — the natural hook for updating EconomyManager

### Established Patterns
- `HappinessManager.OnArrivalCheck()`: current arrival logic (`float chance = Mood * ArrivalProbabilityScale`) — replace with per-tier lookup from HappinessConfig
- `EconomyManager.CalculateTickIncome()`: uses `_currentHappiness` float → replace with `_currentTierMultiplier` float looked up from EconomyConfig
- `EconomyManager.SetHappiness(float)`: compatibility shim explicitly noted for Phase 11 replacement
- Timer-based periodic checks (ArrivalCheckInterval = 60f): keep interval fixed, update probability field only

### Integration Points
- `HappinessManager._lastReportedTier`: already tracks current tier — read this when arrival check fires
- `EconomyManager.SetMoodTier(MoodTier)`: new method to add, called on tier change
- `HappinessManager.OnWishFulfilled()`: currently calls `EconomyManager.Instance?.SetHappiness(_moodSystem.Mood)` — Phase 11 updates this call site to use `SetMoodTier`

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 11-economy-and-arrival-tier-integration*
*Context gathered: 2026-03-04*
