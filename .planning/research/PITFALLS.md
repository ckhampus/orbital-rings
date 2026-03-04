# Pitfalls Research

**Domain:** Replacing a monotonically increasing happiness float with a dual-value decay system (Lifetime Happiness + Station Mood) in an existing Godot 4 C# cozy game
**Researched:** 2026-03-04
**Confidence:** HIGH (based on direct codebase inspection of all affected files + verified general principles from official Godot docs and EDA community sources)

> **Scope note:** This file covers pitfalls specific to the v1.1 Happiness v2 migration.
> General Godot 4 / Orbital Rings v1.0 pitfalls (navmesh, signal leaks, collision shapes, etc.)
> remain documented in the original PITFALLS.md above this milestone boundary.

---

## Critical Pitfalls

### Pitfall 1: Save Migration Data Loss — Silently Treating v1 Saves as v2

**What goes wrong:**
`SaveData.Version` is currently hard-coded to `1` in the POCO. When v2 ships, the `Load()` method in `SaveManager` deserializes the JSON without inspecting `Version`. A v1 save file has only the `Happiness` float field (range 0.0–1.0). The new v2 schema adds `LifetimeHappiness` (int) and `Mood` (float) and removes `Happiness`. If the deserializer silently ignores unknown fields (which `System.Text.Json` does by default) and uses default values for missing fields, a v1 save will load with `LifetimeHappiness = 0` and `Mood = 0` — effectively erasing all progression. The player sees their station as if they'd never played.

**Why it happens:**
`System.Text.Json` uses `JsonSerializerDefaults.General` by default, which silently drops fields not present in the target type and zero-initializes missing ones. There is no migration gate in the current `Load()` path — it deserializes directly to the current `SaveData` type without branching on `Version`. Developers adding new fields assume forward compatibility without verifying the reverse: loading old files into the new type.

**How to avoid:**
Add a migration gate immediately after deserialization in `SaveManager.Load()`:

```csharp
var raw = JsonSerializer.Deserialize<JsonElement>(json);
int version = raw.TryGetProperty("Version", out var v) ? v.GetInt32() : 1;

if (version == 1)
    return MigrateV1ToV2(raw);

return raw.Deserialize<SaveData>();
```

The migration function reconstructs `LifetimeHappiness` from the v1 `Happiness` float using the inversion formula specified in the design doc (`wishes ≈ (happiness / HappinessGainBase) × (1 + happiness)`), sets `Mood` to the computed baseline, carries over all other fields unchanged, and writes `Version = 2`. Test with an actual v1 save file before shipping.

**Warning signs:**
- Loading an old save file sets `LifetimeHappiness` to 0 in the debugger
- Blueprint unlocks disappear on load (they're keyed to `_crossedMilestoneCount` which is preserved, but recheck)
- No `Version` check anywhere in `Load()` or `ApplyState()`

**Phase to address:**
Save migration phase (dedicated task). Must be implemented and tested before any other v2 feature ships, because once a v2 save is written, v1 migration is a narrow window.

---

### Pitfall 2: HappinessChanged Event Contract Break — All Consumers Expect a Float

**What goes wrong:**
`GameEvents.HappinessChanged` currently carries `Action<float>` — a single happiness value in [0, 1]. Five subscribers depend on this contract:
- `HappinessBar` (UI) — reads float to render bar fill and percentage label
- `SaveManager` — triggers autosave on any happiness change
- `EconomyManager` — uses the float via `SetHappiness(float)` to compute income multiplier
- Any future subscribers added before this migration

In v2, happiness is split into two values with different types and semantics. If the event is renamed/replaced without updating all subscribers, the compiler will catch missing handler registrations — but the subtler failure is that consumers that survive the rename but receive the wrong semantic (e.g., receiving `Mood` where they expected the 0–1 normalized `Happiness`) will produce wrong values silently. `EconomyManager.SetHappiness` clamps to [0, 1] — passing raw `Mood` (which goes to ~20+) will always clamp to 1.0, yielding the maximum economy multiplier forever.

**Why it happens:**
When adding new fields to an event bus, developers often create a new event for the new data (`MoodChanged`) while leaving the old event in place "for now." Consumers that should be migrated stay on the old event, reading stale or semantically wrong data. The compiler does not flag this — the code compiles and runs, but produces wrong gameplay.

**How to avoid:**
Treat the event migration as a single atomic change:
1. Add `MoodChanged` (`Action<float>`) and `LifetimeHappinessChanged` (`Action<int>`) to `GameEvents`.
2. Remove `HappinessChanged` (or keep it as a deprecated alias that fires during a transition period with the normalized mood value for backward compatibility, then delete it).
3. Migrate `HappinessBar` → subscribe to `MoodChanged` and `LifetimeHappinessChanged` (UI is being replaced anyway).
4. Migrate `EconomyManager.SetHappiness` → replace with `SetMoodTier(MoodTier)` or `SetEconomyMultiplier(float)` so the economy multiplier is resolved in `HappinessManager` before the event fires, not reconstructed by the consumer.
5. Migrate `SaveManager` → subscribe to `MoodChanged` as the autosave trigger (it only needs to know "something changed", not the value).

Compile-time enforcement: delete `HappinessChanged` early so any missed subscriber becomes a build error.

**Warning signs:**
- `EconomyManager._currentHappiness` always reads 1.0 after migration
- Income multiplier is always at maximum regardless of activity
- Old `HappinessBar` still showing a percentage bar after the new HUD is built
- `_onHappinessChanged` delegate still wired in `SaveManager.SubscribeEvents()`

**Phase to address:**
HappinessManager v2 refactor phase — implement all event consumer migrations in the same phase, not across multiple phases.

---

### Pitfall 3: Mood Decay Running Every Frame — Autosave Storm

**What goes wrong:**
The mood decay formula (`mood += (baseline - mood) * DecayRate * delta`) runs every frame via `_Process`. When mood does not exactly equal baseline, this fires a non-zero delta every frame. If `HappinessManager` emits `MoodChanged` every frame, and `SaveManager` is subscribed to `MoodChanged` as an autosave trigger, the debounce timer resets every frame — the save never fires (debounce 0.5s, frame interval ~16ms). This sounds safe but it means the save timestamp keeps being pushed forward indefinitely while the player is idle. If the game closes unexpectedly during the decay phase, the last save was before the most recent wish fulfillment.

The converse problem: if the debounce is removed and the save fires every frame during decay, the game writes `save.json` at 60 FPS — massive I/O, perceptible stutter.

**Why it happens:**
Continuous decay creates a continuous stream of micro-changes. The debounce pattern was designed for discrete events (wish fulfilled, room placed) — not continuous floating-point drift. Mixing a continuous simulation update with a discrete event bus causes the event bus to become a fire-hose.

**How to avoid:**
Do not emit `MoodChanged` every frame. Instead, implement one of two patterns:

**Option A (preferred):** Emit `MoodChanged` only on tier change. Tier is the discrete, meaningful unit. Mood value itself is an internal float that drives the tier. Only tier transitions (Cozy → Lively, etc.) are externally significant. The UI reads `HappinessManager.Instance.Mood` directly on a polling interval or subscribes to a `MoodTierChanged` event.

**Option B:** Emit `MoodChanged` at most once per second using a minimum emission interval guard:

```csharp
private float _moodEmitAccumulator;
private const float MoodEmitInterval = 1.0f;

public override void _Process(double delta)
{
    UpdateDecay((float)delta);
    _moodEmitAccumulator += (float)delta;
    if (_moodEmitAccumulator >= MoodEmitInterval)
    {
        _moodEmitAccumulator = 0f;
        GameEvents.Instance?.EmitMoodChanged(_mood);
    }
}
```

`SaveManager` should be wired to tier changes and explicit state events (wish fulfilled, room placed), NOT to mood value drift.

**Warning signs:**
- Profiler shows `PerformSave` firing continuously or `OnAnyStateChanged` being called every frame
- `save.json` modification timestamp updates every second while player is idle
- Mood value in save file has 15 decimal places of floating-point noise
- Autosave never completes because the debounce timer never expires

**Phase to address:**
HappinessManager v2 implementation — establish the decay emission pattern before wiring any consumers.

---

### Pitfall 4: Tier Boundary Oscillation — Rapid Tier Toggling Near Thresholds

**What goes wrong:**
With the current tier table, `Cozy` ends at 4.9 and `Lively` starts at 5.0. If mood is resting near 5.0 — which happens naturally during moderate activity — a single wish fulfillment pushes mood to 5.0 + 3.0 = 8.0 (Lively), then decay drags it back toward baseline. If baseline is ~4.5 (about 20 lifetime wishes), mood settles around 4.5, then crosses 5.0 again on the next wish. The floating tier notification fires: "Station mood: Lively" ... pause ... "Station mood: Cozy" ... one wish later ... "Station mood: Lively". This looks broken and feels punishing even in a game with no fail state.

The effect is most pronounced when:
- Baseline sits just below a tier boundary (e.g., baseline = 4.8, tier boundary = 5.0)
- MoodGainPerWish (3.0) is large enough to push across the boundary but decay brings it back
- The wish arrival rate is moderate (not constant, not absent)

**Why it happens:**
Discrete tier labels applied to a continuous, fluctuating float with no hysteresis. The system responds to the instantaneous value crossing a threshold. In control systems this is called "chatter" — the output oscillates around the switching point. The thermostat analogy: a thermostat that turns on at exactly 70°F and off at exactly 70°F would cycle continuously when room temperature is 70°F.

**How to avoid:**
Apply hysteresis to tier transitions. The tier promotes at the standard threshold, but requires the mood to drop a defined amount below the threshold before demoting:

```csharp
private MoodTier _currentTier = MoodTier.Quiet;
private const float TierHysteresis = 0.5f; // must drop 0.5 below boundary to demote

private MoodTier ComputeTier(float mood)
{
    // Promotion uses standard thresholds
    if (mood >= 18.0f) return MoodTier.Radiant;
    if (mood >= 10.0f) return MoodTier.Vibrant;
    if (mood >= 5.0f)  return MoodTier.Lively;
    if (mood >= 2.0f)  return MoodTier.Cozy;
    return MoodTier.Quiet;
}

private void UpdateTier(float mood)
{
    var rawTier = ComputeTier(mood);

    // Demotion requires crossing below (boundary - hysteresis)
    if (rawTier < _currentTier)
    {
        float demotionThreshold = GetLowerBoundaryForTier(_currentTier) - TierHysteresis;
        if (mood >= demotionThreshold) return; // suppress demotion
    }

    if (rawTier != _currentTier)
    {
        _currentTier = rawTier;
        GameEvents.Instance?.EmitMoodTierChanged(_currentTier);
    }
}
```

Additionally, consider a minimum tier hold time (e.g., 5 seconds) before a demotion can fire after a promotion. This prevents the "promoted then immediately demoted in one wish cycle" scenario.

**Warning signs:**
- Tier change notification fires twice within a few seconds without any player action
- "Station mood: Cozy / Lively / Cozy / Lively" in rapid succession in the test console
- The tier label in the HUD flickers between two states
- Playtest feedback: "The mood thing seems glitchy"

**Phase to address:**
HappinessManager v2 implementation — implement hysteresis before the tier notification UI is connected, so the notification fires correctly from the start.

---

### Pitfall 5: Decay Feels Punishing — Mood Drops Visibly While Player Is Building

**What goes wrong:**
DecayRate = 0.02 with delta gives a half-life of ~35 seconds. A wish fulfillment gives +3.0 mood. After 35 seconds of no further wishes, mood has dropped 1.5 — possibly crossing a tier boundary. If the player spends 45–60 seconds browsing the build panel, placing a room, and placing another, the station tier may quietly demote before the player notices. They placed two rooms and the station feels worse. The cozy promise ("mood breathes with your activity") becomes "mood punishes you for thinking".

This is especially acute in early game: with a low baseline (e.g., 0–2), a single wish raises mood from 0 to 3 (Cozy tier), then decay brings it back to 0 (Quiet) within about 2 minutes. The player who started a fresh station and fulfilled one wish sees their "Cozy" notification disappear while they're still celebrating.

**Why it happens:**
Linear exponential decay feels faster near the upper tier boundary than near the baseline. A 35-second half-life feels fine at high mood (the player is active, fulfilling wishes rapidly) but brutal at low mood (each wish matters more, and the decay window is tight). The design doc's note that "decay is gentle, not punitive" is a goal, not a guarantee — the numbers need validation against real session behavior.

**How to avoid:**
Three strategies, apply all three:

1. **Slow the decay at low tiers.** Use a tier-aware decay rate: lower tiers decay slower than higher tiers. Example: Quiet/Cozy decay at 0.01 (half-life ~70s), Lively at 0.02, Vibrant/Radiant at 0.03. This gives the player more time to enjoy their first tier promotion.

2. **Use the baseline as a floor properly.** The formula `mood += (baseline - mood) * rate * delta` already provides a floor — mood decays toward baseline, not toward zero. Verify in code that baseline is recomputed correctly from `lifetimeHappiness` before each decay step, not cached from session start.

3. **Never demote below the resting tier.** If the current baseline implies the player's resting tier is Cozy, prevent mood from displaying the Quiet label even if a momentary dip sends the raw float below 2.0. Clamp the displayed tier to `max(ComputeTier(mood), ComputeTier(baseline))`. The resting tier is the player's "floor identity" for the station — they earned it permanently through Lifetime Happiness.

**Warning signs:**
- Playtest feedback: "I built a room and the mood got worse"
- Tier demotes within 60 seconds of a tier promotion without any inactivity
- New game: first wish gives Cozy, but Cozy disappears before the second wish can spawn
- The decay half-life at low mood feels shorter than at high mood (not true mathematically, but perceived as so because each tier step represents more emotional weight early in the game)

**Phase to address:**
Playtesting / tuning phase after initial implementation. Do not tune decay rates in code — put them in the `EconomyConfig` resource or a dedicated `HappinessConfig` resource so they can be adjusted without recompilation.

---

### Pitfall 6: Blueprint Unlock Milestone Carry-Over — Double-Firing Unlocks on Migration

**What goes wrong:**
In v1, `_crossedMilestoneCount` tracks how many percentage-based thresholds were crossed (0, 1, or 2). In v2, milestones are keyed to wish counts (4, 12). The migration sets `LifetimeHappiness` by inverting the v1 formula. If a v1 player had ~60% happiness (≈12 wishes crossed), the migration correctly estimates 12 wishes. On load, `CheckUnlockMilestones()` runs from `_crossedMilestoneCount`. If `_crossedMilestoneCount` is carried from v1 (value = 2), the milestone check starts at index 2 and fires nothing (already past both milestones). Correct.

BUT: if the migration estimates `LifetimeHappiness = 14` and the code resets `_crossedMilestoneCount = 0` (naively starting fresh), the milestone loop will fire `BlueprintUnlocked` for rooms the player already built — causing duplicate blueprint notifications and potentially confusing unlock state in `BuildPanel`.

**Why it happens:**
The migration logic has two independent concerns: computing `LifetimeHappiness` from the old `Happiness` float, and determining which milestones should be considered already crossed. These are easy to conflate. If one path resets `_crossedMilestoneCount` while another leaves it, behavior is inconsistent.

**How to avoid:**
In the v1→v2 migration function, carry `CrossedMilestoneCount` forward unchanged from the v1 save. The v1 value (0, 1, or 2) maps directly to the v2 milestone count since v2 has the same number of unlock milestones (2). Do not recompute it from the estimated `LifetimeHappiness`. Explicitly document in the migration code why `CrossedMilestoneCount` is not recomputed.

If the unlock milestone count changes in v3 (e.g., adding a third milestone), that migration will need a proper mapping — but for v1→v2, the count is stable.

**Warning signs:**
- `BlueprintUnlocked` event fires on session start for rooms the player already unlocked
- `BuildPanel` shows "New!" badges on already-known rooms after loading a save
- `_crossedMilestoneCount` is 0 in the debugger immediately after loading a save where `LifetimeHappiness > 12`

**Phase to address:**
Save migration phase — verify with a saved game that has at least one crossed milestone before considering migration complete.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Emit `MoodChanged` every frame from `_Process` | Simple code, no accumulator needed | Autosave debounce never expires; I/O storm if debounce removed | Never — use tier change events or interval throttle |
| Keep `HappinessChanged (float)` event alongside new events | No consumer migration needed immediately | Consumers on old event receive semantically wrong data; economy multiplier maxes out | Never — migrate all consumers atomically |
| Hard-code decay rate and tier thresholds as C# constants | Faster to write | Balance tuning requires recompile + restart each iteration | MVP only — move to `HappinessConfig` resource before first playtest |
| Skip hysteresis on tier transitions | Simpler tier evaluation code | Tier oscillation near boundaries; player-visible "glitchy" notification spam | Never — hysteresis is a one-time addition, not ongoing complexity |
| Set `Mood = 0` on migration instead of computing from baseline | No migration math needed | New session after loading v1 save shows Quiet tier despite accomplished station | Never — baseline math is 3 lines, worth it |
| Reuse `EconomyManager.SetHappiness(float)` by passing normalized mood | No API change to EconomyManager | Mood is not in [0, 1], clamp produces wrong multiplier; economy always at maximum | Never — create `SetEconomyMultiplier(float)` that takes the tier multiplier directly |

---

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| `EconomyManager` ↔ `HappinessManager` | Pass raw `_mood` float to `SetHappiness(float)` | Resolve the tier in `HappinessManager`, pass the tier's economy multiplier value directly: `EconomyManager.Instance?.SetEconomyMultiplier(tier.EconomyMultiplier)` |
| `SaveManager` ↔ `MoodChanged` event | Subscribe autosave trigger to `MoodChanged` (fires every frame during decay) | Subscribe autosave trigger to `MoodTierChanged` + explicit discrete events (wish fulfilled, room placed) — not continuous mood drift |
| `HappinessBar` → v2 UI | Rename/repurpose the existing node | The entire widget is being replaced (bar → counter + tier label); delete `HappinessBar.cs` and its scene node rather than patching it to avoid stale code paths |
| `RestoreState(float, ...)` on `HappinessManager` | Call old `RestoreState` signature from `SaveManager.ApplyState()` | Add new `RestoreStateV2(int lifetimeHappiness, float mood, ...)` overload; update `SaveManager.ApplyState()` to call the new signature; remove the old one |
| `SaveData.Happiness` field | Leave the old field in `SaveData` for backward compatibility | Leave it present for JSON deserialization of v1 files (deserialization reads it), but mark it `[Obsolete]` and only access it in the migration function; never write it in v2 saves |

---

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| `sqrt(lifetimeHappiness)` computed every frame in baseline recalculation | Negligible on its own, but compounds if decay + baseline + tier check all run every `_Process` frame | Cache baseline; recompute only when `lifetimeHappiness` changes (on wish fulfillment, never during decay) | Not a performance problem at expected scale, but wastes CPU for no gain |
| Tier change notification spawning `FloatingText` while autosave debounce is pending | Multiple `FloatingText` nodes live simultaneously if tier oscillates | Hysteresis prevents oscillation; also enforce a minimum 5s cooldown before the same tier change notification can re-fire | Visible with tier boundary oscillation (see Pitfall 4) |
| Per-frame `_Process` in `HappinessManager` that emits events consumed by UI | UI receives 60 updates/second for data that changes slowly | Rate-limit UI updates; UI should poll current tier on a 250ms timer rather than subscribing to continuous mood events | Becomes noticeable if mood animation is added to tier labels |

---

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Showing raw mood float anywhere in the UI | Players optimize around the number instead of playing naturally | Never expose the raw float; only show the tier name and lifetime counter |
| Tier change notification on every minor fluctuation | Notification spam feels broken; player tunes it out | Hysteresis + minimum hold time; notification fires at most once per ~10 seconds |
| Demoting tier while player is actively building | Player perceives building as causing mood to drop | Enforce the "resting tier floor" — displayed tier cannot go below `ComputeTier(baseline)` |
| Lifetime counter ticking up without visible celebration | The permanent achievement is invisible | Pulse the counter on increment (same pattern as credit counter flash already in `CreditHUD`); ensure this animation is wired in the wish fulfillment handler |
| "Station mood: Quiet" on a mature station | Station with 50 wishes feels dead | Baseline formula ensures mature stations rest above Quiet; verify the formula at 25, 50, 100 wishes in a spreadsheet before shipping |
| No feedback when Radiant tier is reached | Radiant is the peak state; player doesn't notice | Give Radiant a distinct visual treatment — ambient glow or ring color shift — not just a text label |

---

## "Looks Done But Isn't" Checklist

- [ ] **Save migration:** v1 save file loads correctly with non-zero `LifetimeHappiness` and correct starting `Mood` — verify by loading an actual `save.json` from a v1 session, not a newly created test save
- [ ] **Blueprint milestone carry-over:** Loading a v1 save with both milestones crossed does NOT re-fire `BlueprintUnlocked` events on session start
- [ ] **EconomyManager multiplier:** After v2 migration, `EconomyManager._currentHappiness` is no longer used; verify income calculation uses tier multiplier, not the old clamped float
- [ ] **HappinessChanged event removed:** Confirm no subscriber is still wired to the old `HappinessChanged` event; grep for `HappinessChanged +=` and `OnHappinessChanged`
- [ ] **Decay runs correctly:** Verify mood decays toward baseline (not toward zero) — confirm with a save that has `LifetimeHappiness = 36` (baseline = 6.0); idle the game; mood should settle at ~6.0, not 0.0
- [ ] **Tier oscillation absent:** With mood at 4.9 and baseline at 4.5, fulfill one wish — verify the "Lively" notification fires once, not twice, even after decay returns mood to ~4.5
- [ ] **Autosave frequency:** With no player input and mood decaying, verify `save.json` is NOT written every second — check the file's modification timestamp is stable during idle decay

---

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Migration silently zeroes lifetime happiness | HIGH — affects all existing players | Ship a hotfix that reads the old `Happiness` float from the JSON before migration runs; re-derive `LifetimeHappiness`; re-save |
| EconomyManager always at maximum multiplier | MEDIUM — silent wrong behavior | Trace `_currentHappiness` in EconomyManager; replace `SetHappiness` call site with tier multiplier; 1–2 hours fix |
| Autosave storm from mood decay events | MEDIUM — I/O issue, not data loss | Remove `MoodChanged` from autosave trigger subscriptions; add back only `MoodTierChanged`; 30 minutes fix |
| Tier oscillation visible in playtest | LOW — UI/feel issue | Add hysteresis constant and hold timer to `UpdateTier()`; 1–2 hours fix |
| Decay feels punishing in first session | MEDIUM — requires tuning, not rewrites | Expose decay rates and tier thresholds in a `HappinessConfig` Resource; adjust via Inspector without recompile; 1–3 hours tuning |
| Double-fired `BlueprintUnlocked` on migration | LOW — notification annoyance | Carry `CrossedMilestoneCount` forward from v1 save unchanged; 15 minutes fix |

---

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| v1 save migration data loss | Save migration phase (first task) | Load actual v1 save.json; confirm `LifetimeHappiness > 0` and `Mood = baseline` in debugger |
| HappinessChanged event contract break | HappinessManager refactor phase | Delete old event; confirm build succeeds; confirm economy multiplier is not always at maximum |
| Autosave storm from decay events | HappinessManager refactor phase | Idle for 60s with mood decaying; confirm save.json modification timestamp is stable |
| Tier boundary oscillation | HappinessManager refactor phase | Set mood to 4.9, baseline to 4.5; fulfill one wish; verify one notification fires, not two |
| Decay feels punishing at low mood | Playtesting / tuning phase | Fresh game: fulfill one wish; verify Cozy tier holds for at least 90 seconds before any demotion |
| Blueprint unlock double-fire | Save migration phase | Load v1 save with both milestones crossed; confirm no `BlueprintUnlocked` event fires at session start |

---

## Sources

- Direct codebase inspection: `HappinessManager.cs`, `SaveManager.cs`, `GameEvents.cs`, `EconomyManager.cs`, `HappinessBar.cs` (all current v1.0 state)
- Design spec: `.planning/design/happiness-v2.md`
- Hysteresis for tier boundary control: https://shawnhargreaves.com/blog/hysteresis.html (Shawn Hargreaves, XNA/DirectX veteran)
- Hysteresis in control systems: https://en.wikipedia.org/wiki/Hysteresis
- Event-driven architecture breaking changes: https://medium.com/insiderengineering/common-pitfalls-in-event-driven-architectures-de84ad8f7f25
- Game save migration versioning: https://www.gamedev.net/forums/topic/702903-how-to-transfer-save-data-through-versions/
- Godot `_Process` delta and frame-rate independence: https://kidscancode.org/godot_recipes/4.x/basics/understanding_delta/index.html
- `System.Text.Json` default deserialization behavior (missing fields → default values, extra fields → silently ignored): https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members

---
*Pitfalls research for: Orbital Rings — v1.1 Happiness v2 (dual-value decay system migration)*
*Researched: 2026-03-04*
