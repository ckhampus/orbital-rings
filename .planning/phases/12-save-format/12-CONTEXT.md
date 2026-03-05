# Phase 12: Save Format - Context

**Gathered:** 2026-03-05
**Status:** Ready for planning

<domain>
## Phase Boundary

Update SaveData and SaveManager to persist the three new happiness values (lifetime happiness, mood, mood baseline) introduced in Phase 10. Fresh saves only — no v1 migration logic. V1 saves continue to load via existing backward-compat path (mood = old happiness float, lifetime = 0). HUD replacement is Phase 13.

</domain>

<decisions>
## Implementation Decisions

### Save data fields
- Add `LifetimeHappiness` (int), `Mood` (float), `MoodBaseline` (float) to SaveData
- Bump `SaveData.Version` from 1 to 2
- Keep old `Happiness` field for v1 backward compatibility (defaults to 0 in v2 saves)

### CollectGameState updates
- Read `HappinessManager.Instance.LifetimeWishes` for lifetime count
- Read `HappinessManager.Instance.Mood` for current mood float
- Read MoodSystem baseline (needs public accessor or method on HappinessManager)
- Continue reading `CrossedMilestoneCount`, `HousingCapacity`, `UnlockedRooms` as before

### RestoreState expansion
- Expand `HappinessManager.RestoreState()` to accept lifetime happiness, mood, and baseline
- Pass all three values through to `MoodSystem.RestoreState(mood, baseline)`
- Set `_lifetimeHappiness` from saved value (not hardcoded 0)
- SaveManager detects version: v2 passes new fields, v1 uses existing backward-compat path

### Autosave event wiring
- SaveManager currently subscribes to `HappinessChanged` which is never emitted post-Phase 10
- Replace with subscriptions to `MoodTierChanged` and `WishCountChanged` to trigger autosave on happiness changes

### Dead shim cleanup
- Remove `Happiness` property shim from HappinessManager (SaveManager will use new fields directly)
- Remove `HappinessChanged` event subscription from SaveManager (replaced by new events)
- Keep `HappinessChanged` event definition in GameEvents (HappinessBar may still reference it until Phase 13)

### Claude's Discretion
- Exact version detection logic in ApplyState (simple version check vs. field presence check)
- Whether to expose MoodSystem.Baseline via HappinessManager property or a GetMoodBaseline() method
- Whether to remove or keep the old `Happiness` field in SaveData for v2 writes
- Exact ordering of event subscription cleanup

</decisions>

<specifics>
## Specific Ideas

No specific requirements — prior phases defined the values to persist and the backward compatibility approach (Phase 10 CONTEXT: "Old saves: happiness float maps to initial mood with _lifetimeHappiness=0; milestone guards preserve unlock state").

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `SaveData` class (Scripts/Autoloads/SaveManager.cs:19): Plain POCO with Version field — add new fields alongside existing ones
- `MoodSystem.RestoreState(float mood, float baseline)` (Scripts/Happiness/MoodSystem.cs:70): Already accepts both values
- `HappinessManager.RestoreState()` (Scripts/Autoloads/HappinessManager.cs:151): Current signature takes (float happiness, HashSet<string>, int, int) — expand with lifetime + baseline

### Established Patterns
- System.Text.Json serialization with `WriteIndented = true` — new fields auto-serialize
- `StateLoaded` flag pattern prevents `_Ready()` from overwriting loaded state
- Debounced autosave via 0.5s one-shot timer on any state-change event
- Event subscription stored as delegate fields for clean unsubscription

### Integration Points
- `SaveManager.CollectGameState()` reads from all Autoload singletons — add HappinessManager reads
- `SaveManager.ApplyState()` calls `HappinessManager.RestoreState()` before scene load
- `GameEvents.MoodTierChanged` and `WishCountChanged` — new autosave triggers
- `MoodSystem._baseline` is private — needs public accessor for save

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 12-save-format*
*Context gathered: 2026-03-05*
