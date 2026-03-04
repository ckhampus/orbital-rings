# Architecture Research

**Domain:** Godot 4 C# cozy builder game — Happiness v2 integration into existing singleton architecture
**Researched:** 2026-03-04
**Confidence:** HIGH (analysis is based on direct reading of the existing codebase; no speculative API research required)

---

## Standard Architecture

### System Overview — Current (v1.0)

```
                          GameEvents (signal bus)
                               |
        ┌──────────────────────┼──────────────────────┐
        |                      |                      |
  WishFulfilled          HappinessChanged        BlueprintUnlocked
        |                      |                      |
        v                      v                      v
 HappinessManager  ──SetHappiness──>  EconomyManager   BuildPanel/HUD
  float _happiness              float _currentHappiness
  % thresholds                  multiplier formula
        |
  OnArrivalCheck
        |
  CitizenManager.SpawnCitizen
        |
  HappinessBar (UI)
   reads HappinessManager.Happiness on init
   subscribes to HappinessChanged (float)
```

### System Overview — Target (v2)

```
                          GameEvents (signal bus)
                               |
        ┌──────────────────────┼──────────────────────────────────────┐
        |                      |                          |           |
  WishFulfilled    LifetimeHappinessChanged    MoodChanged    MoodTierChanged
        |                      |                  |              |
        v                      v                  v              v
 HappinessManager     HappinessCounter (HUD)   EconomyManager  MoodDisplay (HUD)
  int _lifetime                                  float _currentMood  FloatingText
  float _mood                 (replaces HappinessChanged event)
  float _baseline
  MoodTier _tier
  Timer _moodDecayTimer
        |
  OnArrivalCheck (tier-based)
        |
  CitizenManager.SpawnCitizen
```

### Component Responsibilities

| Component | Responsibility v1 | Responsibility v2 | Change |
|-----------|-------------------|-------------------|--------|
| `HappinessManager` | Owns `float _happiness` (0–1), wish-gain with diminishing returns, % unlock milestones, arrival probability | Owns `int _lifetimeHappiness` + `float _mood` + `float _baseline`, mood gain/decay, tier computation, wish-count unlock milestones, tier-based arrival probability | **Replace — full refactor** |
| `GameEvents` | `event Action<float> HappinessChanged` | Add `event Action<int> LifetimeHappinessChanged`, `event Action<float, MoodTier> MoodChanged`, `event Action<MoodTier, MoodTier> MoodTierChanged`; keep or remove old event | **Extend** |
| `EconomyManager` | `float _currentHappiness` drives `1 + (happiness × 0.3)` multiplier | `MoodTier _currentTier` drives lookup table multiplier | **Swap input type** |
| `CitizenManager` | Called by `HappinessManager.OnArrivalCheck` — no direct change | No direct change — arrival is still driven by `HappinessManager.OnArrivalCheck` | **No change** |
| `HappinessBar` (UI) | Subscribes to `HappinessChanged(float)`, displays bar + percentage | **Replace** with two widgets: counter `♥ 47` + tier label | **Replace** |
| `SaveManager` / `SaveData` | `float Happiness`, `int CrossedMilestoneCount` | `int LifetimeHappiness`, `float Mood`, `int CrossedMilestoneCount`; version bump + migration | **Extend + migrate** |
| `WishBoard` / `WishFulfilled` | No change — still emits `WishFulfilled(citizenName, wishType)` | No change | **No change** |
| `BuildManager` | No change | No change | **No change** |

---

## Recommended Project Structure

No new files are required. All changes are to existing files:

```
Scripts/
├── Autoloads/
│   ├── GameEvents.cs          # EXTEND: add 3 new events, keep or deprecate HappinessChanged
│   ├── HappinessManager.cs    # REPLACE: refactor to dual-value system
│   ├── EconomyManager.cs      # MODIFY: swap happiness float input for MoodTier
│   └── SaveManager.cs         # MODIFY: SaveData version bump, new fields, migration logic
├── UI/
│   └── HappinessBar.cs        # REPLACE: rebuild as HappinessCounter + MoodDisplay
└── Data/
    └── (optional) MoodTier.cs # NEW enum if not inlined in HappinessManager
```

The `MoodTier` enum is small enough to live in `HappinessManager.cs`. Extract to `Data/MoodTier.cs` only if `EconomyManager.cs` needs to reference it directly (to avoid circular dependency — both are in the `OrbitalRings.Autoloads` namespace so the enum can be `public` in the same namespace).

---

## Architectural Patterns

### Pattern 1: Enum-keyed lookup table for tier effects

**What:** Define `MoodTier` as an enum (Quiet, Cozy, Lively, Vibrant, Radiant). Store tier thresholds, arrival scales, and economy multipliers in a static readonly array indexed by tier cast to int. Computing the active tier is a linear scan of the threshold array.

**When to use:** Any time a float maps to one of N named states with associated data. Lookup tables are easier to tune than nested conditionals and make the spec values visible in code.

**Trade-offs:** Tightly couples the data to the enum ordinal. Acceptable here because the tier ordering is fixed by design.

**Example:**
```csharp
public enum MoodTier { Quiet, Cozy, Lively, Vibrant, Radiant }

private static readonly (float minMood, float arrivalScale, float economyMult)[] TierData =
{
    (0f,    0.0f, 1.0f),   // Quiet
    (2.0f,  0.2f, 1.1f),   // Cozy
    (5.0f,  0.4f, 1.2f),   // Lively
    (10.0f, 0.6f, 1.3f),   // Vibrant
    (18.0f, 0.8f, 1.4f),   // Radiant
};

private static MoodTier ComputeTier(float mood)
{
    // Walk backwards so we return the highest tier whose threshold mood meets
    for (int i = TierData.Length - 1; i >= 0; i--)
        if (mood >= TierData[i].minMood) return (MoodTier)i;
    return MoodTier.Quiet;
}
```

### Pattern 2: _Process-based mood decay (not a Timer)

**What:** Run mood decay each frame inside `_Process(double delta)` in `HappinessManager`. The formula `mood += (baseline - mood) * DecayRate * delta` is framerate-independent and requires no Timer node.

**When to use:** Smooth, continuous decay that must feel organic. The 60s arrival timer is a discrete check and fits a Timer; mood decay is continuous and fits `_Process`.

**Trade-offs:** `HappinessManager._Process` fires every frame (at render rate, not physics rate). The calculation is O(1), so the cost is negligible. ProcessMode is already set to `Pausable` which is correct — mood should not decay while the game is paused.

**Example:**
```csharp
public override void _Process(double delta)
{
    float f = (float)delta;
    float baseline = BaselineFactor * Mathf.Sqrt(_lifetimeHappiness);
    _mood += (baseline - _mood) * DecayRate * f;
    _mood = Mathf.Max(_mood, 0f);

    MoodTier newTier = ComputeTier(_mood);
    if (newTier != _currentTier)
    {
        var oldTier = _currentTier;
        _currentTier = newTier;
        GameEvents.Instance?.EmitMoodTierChanged(oldTier, newTier);
    }

    // Emit mood change every frame for EconomyManager to read
    // (or only on change -- see integration note below)
    GameEvents.Instance?.EmitMoodChanged(_mood, _currentTier);
}
```

**Integration note:** Emitting `MoodChanged` every frame is wasteful. Emit only when `_mood` changes by more than a small epsilon, or only when tier changes. `EconomyManager` needs the multiplier — it only cares about `MoodTier`, not the raw float. Emit `MoodTierChanged` on tier change and let EconomyManager subscribe to that instead. Store `_currentTier` on `HappinessManager` so consumers can poll it without events.

### Pattern 3: Additive events — preserve HappinessChanged for SaveManager compatibility

**What:** Keep `HappinessChanged(float)` in `GameEvents` for the `SaveManager` debounce subscription. `SaveManager` subscribes to state-change events to trigger autosave — it does not care about the payload, only that something changed. Replacing `HappinessChanged` with a rename would require removing and re-adding SaveManager's subscription.

**When to use:** When changing an existing event would require updating all subscribers simultaneously (risky mid-milestone). Instead, add new events for new consumers and keep the old event for existing subscribers that just need a "something changed" signal.

**Trade-offs:** Two events for related data can cause confusion. The cleaner approach is to remove `HappinessChanged(float)` entirely and have SaveManager subscribe to `LifetimeHappinessChanged` and `MoodTierChanged` instead. This is a one-line change in SaveManager and is preferred — do the clean version.

---

## Data Flow

### Wish Fulfillment Flow (v2)

```
WishBoard.OnRoomPlaced detects wish fulfilled
    ↓
GameEvents.EmitWishFulfilled(citizenName, wishType)
    ↓
HappinessManager.OnWishFulfilled(citizenName, wishType)
    ├── _lifetimeHappiness += 1
    ├── _mood += MoodGainPerWish (3.0)
    ├── _mood clamped (no explicit cap — naturally bounded by decay rate)
    ├── GameEvents.EmitLifetimeHappinessChanged(_lifetimeHappiness)
    │       ↓
    │   HUD counter "♥ N" ticks up with pulse animation
    └── CheckUnlockMilestones()  [now keyed to _lifetimeHappiness int thresholds]
            ↓ (if milestone crossed)
        GameEvents.EmitBlueprintUnlocked(roomId)
```

### Mood Decay Flow (v2 — per frame)

```
HappinessManager._Process(delta)
    ├── baseline = BaselineFactor * sqrt(_lifetimeHappiness)
    ├── _mood += (baseline - _mood) * DecayRate * delta
    ├── _mood = Max(_mood, 0)
    └── newTier = ComputeTier(_mood)
            ↓ (if tier changed)
        GameEvents.EmitMoodTierChanged(oldTier, newTier)
            ↓
            ├── EconomyManager.OnMoodTierChanged(newTier) → stores _currentTier
            └── HUD MoodDisplay.OnMoodTierChanged(oldTier, newTier)
                    └── update label text + color + spawn floating text
```

### Arrival Check Flow (v2 — unchanged structure, changed probability)

```
HappinessManager._arrivalTimer.Timeout (every 60s)
    ↓
int currentPop = CitizenManager.Instance.CitizenCount
if currentPop >= _housingCapacity: return
    ↓
float arrivalScale = TierData[(int)_currentTier].arrivalScale
if _currentTier == MoodTier.Quiet: return  // 0.0 arrival scale = no arrivals
    ↓
float chance = arrivalScale * ArrivalProbabilityBase
if GD.Randf() < chance:
    CitizenManager.Instance.SpawnCitizen()
    SpawnArrivalText(...)
```

Note: `ArrivalProbabilityBase` replaces `ArrivalProbabilityScale`. At Quiet tier, arrival scale is 0.0, so the early return replaces the old `if (_happiness <= 0f) return` guard.

### Economy Multiplier Flow (v2)

```
v1: EconomyManager._currentHappiness = happiness (float)
    multiplier = 1 + (happiness * (HappinessMultiplierCap - 1.0f))

v2: EconomyManager._currentTier = tier (MoodTier enum)
    multiplier = TierData[(int)_currentTier].economyMult (lookup, not formula)

SetHappiness(float) → replace with SetMoodTier(MoodTier)
```

The `SetHappiness` method on EconomyManager is called only from HappinessManager (in `OnWishFulfilled` and `RestoreState`). Replacing it with `SetMoodTier(MoodTier tier)` is a contained change. `GetIncomeBreakdown()` currently returns `happinessMult` as a float — it can continue to do so (just read from the tier lookup table).

---

## Events: New, Modified, Removed

### Remove

| Event | Reason |
|-------|--------|
| `HappinessChanged(float)` | Replaced by `LifetimeHappinessChanged` + `MoodChanged`. SaveManager subscription moves to new events. HappinessBar is replaced entirely. |

### Add

| Event | Signature | Who Emits | Who Subscribes |
|-------|-----------|-----------|----------------|
| `LifetimeHappinessChanged` | `Action<int> newCount` | `HappinessManager.OnWishFulfilled` | `HUD counter widget`, `SaveManager` |
| `MoodChanged` | `Action<float> newMood` | `HappinessManager._Process` (on significant change only) | `SaveManager` (debounce trigger only) |
| `MoodTierChanged` | `Action<MoodTier, MoodTier> oldTier, newTier` | `HappinessManager._Process` | `EconomyManager`, `HUD mood display widget` |

### Keep Unchanged

| Event | Reason |
|-------|--------|
| `WishFulfilled(string, string)` | Still the trigger; HappinessManager subscribes as before |
| `BlueprintUnlocked(string)` | Still emitted by HappinessManager when milestones pass; BuildPanel subscribes as before |
| All other events | Unaffected by this milestone |

---

## SaveManager: Serialization Changes

### SaveData POCO — changes required

```csharp
// v1 fields to remove/replace:
public float Happiness { get; set; }           // REMOVE

// v2 fields to add:
public int LifetimeHappiness { get; set; }     // ADD
public float Mood { get; set; }                // ADD

// Unchanged:
public int Version { get; set; } = 1;          // BUMP to 2
public int CrossedMilestoneCount { get; set; } // KEEP (semantics unchanged — still milestone index)
public int HousingCapacity { get; set; }       // KEEP
public List<string> UnlockedRooms { get; set; } // KEEP
// ... all other fields unchanged
```

### CollectGameState — changes required

```csharp
// Replace:
Happiness = HappinessManager.Instance?.Happiness ?? 0f,

// With:
LifetimeHappiness = HappinessManager.Instance?.LifetimeHappiness ?? 0,
Mood = HappinessManager.Instance?.Mood ?? 0f,
```

### ApplyState — changes required

```csharp
// Replace call to RestoreState:
HappinessManager.Instance?.RestoreState(
    data.Happiness,
    new HashSet<string>(data.UnlockedRooms),
    data.CrossedMilestoneCount,
    data.HousingCapacity);

// With:
HappinessManager.Instance?.RestoreState(
    data.LifetimeHappiness,
    data.Mood,
    new HashSet<string>(data.UnlockedRooms),
    data.CrossedMilestoneCount,
    data.HousingCapacity);
```

### HappinessManager.RestoreState — signature change

```csharp
// v1:
public void RestoreState(float happiness, HashSet<string> unlockedRooms,
    int milestoneCount, int housingCapacity)

// v2:
public void RestoreState(int lifetimeHappiness, float mood,
    HashSet<string> unlockedRooms, int milestoneCount, int housingCapacity)
```

Inside `RestoreState`, after setting state, call `EconomyManager.Instance?.SetMoodTier(ComputeTier(mood))` (replaces `SetHappiness`) and emit `LifetimeHappinessChanged` + `MoodTierChanged` to sync UI.

### SaveManager event subscriptions — changes required

`SaveManager._onHappinessChanged` subscribes to the old `HappinessChanged` event. Replace with subscriptions to `LifetimeHappinessChanged` and `MoodChanged` (both just call `OnAnyStateChanged()` — the payload is irrelevant to SaveManager).

```csharp
// Remove:
private Action<float> _onHappinessChanged;
GameEvents.Instance.HappinessChanged += _onHappinessChanged;
GameEvents.Instance.HappinessChanged -= _onHappinessChanged;

// Add:
private Action<int> _onLifetimeHappinessChanged;
private Action<float> _onMoodChanged;
_onLifetimeHappinessChanged = _ => OnAnyStateChanged();
_onMoodChanged = _ => OnAnyStateChanged();
GameEvents.Instance.LifetimeHappinessChanged += _onLifetimeHappinessChanged;
GameEvents.Instance.MoodChanged += _onMoodChanged;
// (and unsubscribe in _ExitTree / UnsubscribeEvents)
```

### v1 Save Migration

Migration runs inside `SaveManager.Load()` (or a dedicated `MigrateV1ToV2(SaveData)` helper called from `Load()`). The `Version` field gates which path is taken.

```csharp
public SaveData Load()
{
    // ... read and deserialize as before ...
    if (data.Version == 1)
        data = MigrateV1ToV2(data);
    return data;
}

private static SaveData MigrateV1ToV2(SaveData v1)
{
    // Invert the diminishing-returns formula to estimate wish count
    // v1 formula: gain = 0.08 / (1 + happiness); sum of gains ≈ happiness
    // Approximation from design spec: wishes ≈ (happiness / HappinessGainBase) * (1 + happiness)
    const float HappinessGainBase = 0.08f;
    float h = v1.Happiness;
    int estimatedWishes = Mathf.RoundToInt((h / HappinessGainBase) * (1f + h));

    float baseline = Mathf.Sqrt(estimatedWishes); // BaselineFactor = 1.0
    float initialMood = baseline;                  // Start at baseline, not above it

    v1.LifetimeHappiness = estimatedWishes;
    v1.Mood = initialMood;
    v1.Version = 2;
    return v1;  // v1 fields (Happiness) remain in the object but are ignored by v2 logic
}
```

The old `Happiness` field is left in `SaveData` as a nullable or simply ignored — `System.Text.Json` will silently skip unrecognized fields on the v2 class if `Happiness` is removed from the POCO (or include it as an ignored migration-only field).

**Cleaner approach:** Keep `Happiness` in `SaveData` as a migration-only property with `[JsonIgnore]` after migration is complete, or simply remove it and rely on `JsonSerializer`'s default behavior of ignoring unknown fields during deserialization of a v1 save file into the v2 class (where `LifetimeHappiness` and `Mood` default to 0, then migration fills them in).

Recommended: add `[System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]` to nothing — just remove `Happiness` from the POCO. On v1 save load, `LifetimeHappiness` and `Mood` will be 0; `Version` will be 1; the migration branch runs. The old `Happiness` key in the JSON is ignored by the deserializer.

---

## EconomyManager: Integration Change

### What changes

`_currentHappiness` (float) becomes `_currentTier` (MoodTier). The `SetHappiness(float)` method is replaced by `SetMoodTier(MoodTier)`. Income calculation reads from the tier lookup table instead of a formula.

```csharp
// Remove:
private float _currentHappiness;
public void SetHappiness(float happiness) { _currentHappiness = Mathf.Clamp(happiness, 0f, 1f); }

// Add:
private MoodTier _currentTier = MoodTier.Quiet;
public void SetMoodTier(MoodTier tier) { _currentTier = tier; }

// Replace income formula:
// v1: float happinessMult = 1.0f + (_currentHappiness * (Config.HappinessMultiplierCap - 1.0f));
// v2: float happinessMult = HappinessManager.TierData[(int)_currentTier].economyMult;
//     (or expose a static helper on HappinessManager: HappinessManager.GetEconomyMultiplier(_currentTier))
```

EconomyManager does not need to subscribe to any event. HappinessManager calls `EconomyManager.Instance?.SetMoodTier(newTier)` directly when the tier changes (same pattern as the existing `SetHappiness` direct call from `RestoreState` and `OnWishFulfilled`). Alternatively, EconomyManager subscribes to `MoodTierChanged` — either works; the direct call is simpler and matches the existing pattern.

### GetIncomeBreakdown — update for HUD

`GetIncomeBreakdown()` returns `happinessMult` as a float. After the change it returns the tier multiplier value. No signature change required — the return type is still `float`.

---

## HUD: Component Replacement

### HappinessBar.cs — full replacement

`HappinessBar.cs` is a `MarginContainer` that builds a fill bar and subscribes to `HappinessChanged(float)`. The entire widget is obsolete. Replace it with two separate widgets, or one combined widget:

**Option A — Two widgets (recommended for clean separation):**
- `HappinessCounter.cs` — subscribes to `LifetimeHappinessChanged(int)`, displays `♥ 47` with pulse on increment
- `MoodDisplay.cs` — subscribes to `MoodTierChanged`, displays tier name with tier color, spawns floating text on tier change

**Option B — One widget:**
- `HappinessHUD.cs` — handles both events in one class

Option A matches the existing pattern (CreditHUD, PopulationDisplay are separate widgets). Use Option A.

Both widgets initialize from `HappinessManager.Instance` in `_Ready()` the same way `HappinessBar` reads `HappinessManager.Instance.Happiness` today.

```csharp
// HappinessCounter._Ready() initialization:
int initial = HappinessManager.Instance?.LifetimeHappiness ?? 0;
UpdateCounter(initial);

// MoodDisplay._Ready() initialization:
MoodTier initial = HappinessManager.Instance?.CurrentTier ?? MoodTier.Quiet;
UpdateDisplay(initial);
```

---

## Suggested Build Order

Dependencies determine order. The HappinessManager refactor is the source of truth — all consumers update after it.

```
Step 1 — HappinessManager refactor (no consumers yet, but defines the contract)
  - Add MoodTier enum
  - Replace float _happiness with int _lifetimeHappiness + float _mood
  - Add _Process decay loop
  - Replace % milestone thresholds with wish-count thresholds
  - Replace arrival probability formula with tier lookup
  - Update RestoreState signature
  - Compile guard: keep SetHappiness stub returning no-op until EconomyManager is updated
    (prevents compile errors while Step 2 is incomplete)

Step 2 — GameEvents event changes (depends on: MoodTier enum from Step 1)
  - Add LifetimeHappinessChanged(int)
  - Add MoodChanged(float)
  - Add MoodTierChanged(MoodTier, MoodTier)
  - Remove HappinessChanged(float) — or keep as no-op until all subscribers are updated
    Recommended: remove immediately and let the compiler surface every subscriber.
    There are exactly 3 subscribers: HappinessBar, SaveManager, and HappinessManager emits it.
    All are updated in this milestone.

Step 3 — EconomyManager consumer update (depends on: Step 1 MoodTier, Step 2 events)
  - Replace _currentHappiness with _currentTier
  - Replace SetHappiness with SetMoodTier
  - Update CalculateTickIncome to use tier lookup
  - Update GetIncomeBreakdown to return tier multiplier

Step 4 — SaveManager / SaveData migration (depends on: Step 1 new API)
  - Bump SaveData.Version to 2
  - Add LifetimeHappiness and Mood fields
  - Remove (or ignore) Happiness field
  - Update CollectGameState to read new fields
  - Update ApplyState to pass new fields
  - Add MigrateV1ToV2 logic
  - Update event subscriptions (HappinessChanged → LifetimeHappinessChanged + MoodChanged)

Step 5 — HUD replacement (depends on: Step 2 events, Step 1 public API)
  - Delete or gut HappinessBar.cs
  - Create HappinessCounter.cs (subscribes to LifetimeHappinessChanged)
  - Create MoodDisplay.cs (subscribes to MoodTierChanged, spawns tier-change floating text)
  - Wire both into the existing HUD scene (QuickTestScene.tscn) in place of HappinessBar

Step 6 — Integration smoke test
  - Fresh game: mood starts Quiet, rises to Cozy after first wish, decays back toward baseline
  - Blueprint unlock fires at wish 4 and wish 12
  - Economy multiplier changes at each tier boundary
  - HUD counter increments on each wish, tier label updates
  - Save/load: both values persist and restore correctly
  - v1 migration: load an old save.json — LifetimeHappiness and Mood are estimated,
    session continues without errors
```

**Why this order:**

Steps 1 and 2 are done together or sequentially because Step 2 depends on the `MoodTier` enum that lives in Step 1. Steps 3, 4, and 5 have no dependencies on each other after Step 2 is complete — they can be done in any order, but Step 3 (EconomyManager) is the smallest change and validates the new API first. Step 4 (SaveManager) is the riskiest change (data migration) and benefits from doing last among consumers so the API it calls is stable.

The HUD (Step 5) is last because it is purely a consumer with no downstream impact — a broken HUD does not break the game loop, making it the safest place for any iteration.

---

## Anti-Patterns

### Anti-Pattern 1: Emitting MoodChanged every frame from _Process

**What people do:** Call `GameEvents.Instance?.EmitMoodChanged(_mood, _currentTier)` every frame inside `_Process`.
**Why it's wrong:** SaveManager subscribes to state-change events to trigger its debounce timer. If `MoodChanged` fires every frame, the autosave debounce restarts every frame and the game never saves at all.
**Do this instead:** `MoodChanged` is emitted only on tier change (`MoodTierChanged`), or when mood changes by more than an epsilon (e.g., 0.1 units). SaveManager subscribes to `MoodTierChanged` and `LifetimeHappinessChanged` only — these fire infrequently and are the correct autosave triggers.

### Anti-Pattern 2: Storing mood tier on EconomyManager directly

**What people do:** EconomyManager subscribes to `MoodTierChanged` and caches the tier internally.
**Why it's wrong:** Not wrong per se, but creates a second copy of truth. If HappinessManager is the single owner of tier state, calling `EconomyManager.SetMoodTier()` directly from HappinessManager (same as the existing `SetHappiness` pattern) is simpler and avoids the EconomyManager subscription and unsubscription boilerplate.
**Do this instead:** Follow the existing pattern. HappinessManager calls `EconomyManager.Instance?.SetMoodTier(newTier)` directly when the tier changes inside `_Process`. EconomyManager does not subscribe to any events.

### Anti-Pattern 3: v1 save migration inside SaveData POCO constructor

**What people do:** Add migration logic to `SaveData`'s constructor or a property getter to auto-convert old fields.
**Why it's wrong:** SaveData is a plain C# serialization POCO. Logic in POCOs makes them hard to test and violates the single-responsibility principle.
**Do this instead:** Migration is a static method on SaveManager: `private static SaveData MigrateV1ToV2(SaveData v1)`. It is pure (no side effects), takes old data, returns new data. Called from `Load()` before the data is consumed.

### Anti-Pattern 4: Removing the CrossedMilestoneCount semantics

**What people do:** Since milestones are now keyed to wish counts rather than percentages, assume `CrossedMilestoneCount` needs to change and redesign it.
**Why it's wrong:** `CrossedMilestoneCount` is an index into the `UnlockMilestones` array, not a percentage. The semantics are "how many milestone entries have been processed," which is equally valid for wish-count thresholds. The field name, type, and SaveData serialization are all unchanged.
**Do this instead:** Keep `CrossedMilestoneCount` as-is. Only change the `UnlockMilestones` array content: replace `(float threshold, string[] rooms)` tuples with `(int wishCount, string[] rooms)` tuples. The milestone-checking loop (`while (_crossedMilestoneCount < UnlockMilestones.Length)`) is structurally identical.

---

## Integration Points Summary

| Singleton | Reads From HappinessManager | Writes To HappinessManager | Change Required |
|-----------|----------------------------|---------------------------|-----------------|
| `GameEvents` | — | — | Add 3 events, remove 1 |
| `EconomyManager` | `_currentTier` (via `SetMoodTier`) | — | Swap `SetHappiness` → `SetMoodTier` |
| `CitizenManager` | Called by `HappinessManager.OnArrivalCheck` | — | None |
| `WishBoard` | — | Emits `WishFulfilled` (trigger) | None |
| `BuildManager` | `IsRoomUnlocked` (unchanged API) | — | None |
| `SaveManager` | `LifetimeHappiness`, `Mood` (via `CollectGameState`) | `RestoreState(int, float, ...)` | Signature + fields + migration |
| `HappinessBar` (UI) | Subscribes `HappinessChanged` | — | Delete; replace with 2 new widgets |

---

## Sources

- `/workspace/Scripts/Autoloads/HappinessManager.cs` — direct code analysis, HIGH confidence
- `/workspace/Scripts/Autoloads/GameEvents.cs` — direct code analysis, HIGH confidence
- `/workspace/Scripts/Autoloads/SaveManager.cs` — direct code analysis, HIGH confidence
- `/workspace/Scripts/Autoloads/EconomyManager.cs` — direct code analysis, HIGH confidence
- `/workspace/Scripts/Citizens/CitizenManager.cs` — direct code analysis, HIGH confidence
- `/workspace/Scripts/UI/HappinessBar.cs` — direct code analysis, HIGH confidence
- `/workspace/.planning/design/happiness-v2.md` — design spec, HIGH confidence
- `/workspace/.planning/PROJECT.md` — project context, HIGH confidence

---
*Architecture research for: Orbital Rings v1.1 — Happiness v2 integration*
*Researched: 2026-03-04*
