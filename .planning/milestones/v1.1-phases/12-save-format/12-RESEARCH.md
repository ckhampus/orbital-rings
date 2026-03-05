# Phase 12: Save Format - Research

**Researched:** 2026-03-05
**Domain:** Save/load serialization, Godot C# Autoload state persistence
**Confidence:** HIGH

## Summary

Phase 12 updates the save system to persist three new happiness values introduced in Phase 10 (lifetime happiness, mood, mood baseline). The existing SaveManager uses System.Text.Json to serialize a `SaveData` POCO to `user://save.json`. The changes are straightforward: add fields to SaveData, bump the version number, update CollectGameState to read from HappinessManager's new properties, expand RestoreState to accept the new values, and replace the dead `HappinessChanged` autosave trigger with `MoodTierChanged` and `WishCountChanged`.

All code touched by this phase lives in three files: `SaveManager.cs`, `HappinessManager.cs`, and `MoodSystem.cs` (which already has a public `Baseline` property). The backward compatibility path for v1 saves is already handled -- the existing `RestoreState` treats the old `Happiness` float as mood with `_lifetimeHappiness=0`. The phase adds v2 awareness so new saves capture the full state. A dead-shim cleanup removes the `Happiness` property from HappinessManager and the `HappinessChanged` subscription from SaveManager.

**Primary recommendation:** This is a small, surgical phase -- three files changed, no new classes, no new UI. Keep it as a single plan with clearly ordered tasks: SaveData fields first, then CollectGameState/ApplyState, then autosave event rewiring, then dead shim cleanup.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Add `LifetimeHappiness` (int), `Mood` (float), `MoodBaseline` (float) to SaveData
- Bump `SaveData.Version` from 1 to 2
- Keep old `Happiness` field for v1 backward compatibility (defaults to 0 in v2 saves)
- Read `HappinessManager.Instance.LifetimeWishes` for lifetime count
- Read `HappinessManager.Instance.Mood` for current mood float
- Read MoodSystem baseline (needs public accessor or method on HappinessManager)
- Continue reading `CrossedMilestoneCount`, `HousingCapacity`, `UnlockedRooms` as before
- Expand `HappinessManager.RestoreState()` to accept lifetime happiness, mood, and baseline
- Pass all three values through to `MoodSystem.RestoreState(mood, baseline)`
- Set `_lifetimeHappiness` from saved value (not hardcoded 0)
- SaveManager detects version: v2 passes new fields, v1 uses existing backward-compat path
- Replace `HappinessChanged` subscription with `MoodTierChanged` and `WishCountChanged`
- Remove `Happiness` property shim from HappinessManager
- Remove `HappinessChanged` event subscription from SaveManager
- Keep `HappinessChanged` event definition in GameEvents (HappinessBar may still reference it until Phase 13)

### Claude's Discretion
- Exact version detection logic in ApplyState (simple version check vs. field presence check)
- Whether to expose MoodSystem.Baseline via HappinessManager property or a GetMoodBaseline() method
- Whether to remove or keep the old `Happiness` field in SaveData for v2 writes
- Exact ordering of event subscription cleanup

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| SAVE-01 | Save format stores lifetime happiness and mood values (fresh saves only, no v1 migration) | Three new SaveData fields (LifetimeHappiness, Mood, MoodBaseline), Version bump to 2, CollectGameState reads from HappinessManager new properties, RestoreState expanded with new signature, v1 backward-compat path preserved by version check in ApplyState |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| System.Text.Json | .NET 10.0 | JSON serialization | Already used by SaveManager; auto-serializes new POCO properties |
| Godot.NET.Sdk | 4.6.1 | Game engine C# bindings | Project SDK |

### Supporting
No additional libraries needed. This phase only modifies existing code.

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| System.Text.Json | Newtonsoft.Json | No reason to switch -- existing pattern works, no custom converters needed |
| Version int field | Semantic versioning string | Overkill for a simple save format; int comparison is simpler and already established |

## Architecture Patterns

### Relevant File Structure
```
Scripts/
├── Autoloads/
│   ├── SaveManager.cs       # SaveData POCO + save/load orchestration
│   ├── HappinessManager.cs  # Owns MoodSystem, public API for save/load
│   └── GameEvents.cs        # Event bus (MoodTierChanged, WishCountChanged)
└── Happiness/
    └── MoodSystem.cs         # Mood math POCO, RestoreState(mood, baseline)
```

### Pattern 1: SaveData POCO with Version Field
**What:** SaveData is a plain C# class with auto-properties. System.Text.Json serializes/deserializes it automatically. The `Version` field controls which fields are meaningful.
**When to use:** Always -- this is the established save format.
**Current state (v1):**
```csharp
// Scripts/Autoloads/SaveManager.cs:19-31
public class SaveData
{
    public int Version { get; set; } = 1;
    public int Credits { get; set; }
    public float Happiness { get; set; }
    public int CrossedMilestoneCount { get; set; }
    public int HousingCapacity { get; set; }
    public List<string> UnlockedRooms { get; set; } = new();
    // ... rooms, citizens, wishes
}
```
**v2 extension:** Add three new properties with defaults that are safe for v1 deserialization. When System.Text.Json deserializes a v1 JSON file that lacks these fields, they get their C# default values (0 for int, 0f for float), which is exactly correct: v1 saves have no lifetime happiness and zero baseline.

### Pattern 2: Version-Gated Restore
**What:** ApplyState checks `SaveData.Version` to decide which RestoreState overload/path to use.
**When to use:** When save format has breaking changes in field semantics.
**Recommendation:** Use a simple `if (data.Version >= 2)` check. For v2 saves, pass all three new fields to the expanded RestoreState. For v1 saves, use the existing backward-compat path (happiness float as mood, lifetime=0, baseline=0).

### Pattern 3: Debounced Autosave via Event Subscription
**What:** SaveManager subscribes to state-change events. Any event resets a 0.5s debounce timer. Timer expiry triggers PerformSave.
**When to use:** Anytime a game state change should eventually be saved.
**Current issue:** SaveManager subscribes to `HappinessChanged` which is never emitted after Phase 10. It needs to subscribe to `MoodTierChanged` and `WishCountChanged` instead.

### Pattern 4: StateLoaded Guard
**What:** Static bool flags (e.g., `HappinessManager.StateLoaded`) set by SaveManager before scene transition. Each manager's `_Ready()` checks this flag to skip default initialization when loading from save.
**Relevance:** No change needed -- the pattern is already in place and the new RestoreState changes happen before scene load, same as today.

### Anti-Patterns to Avoid
- **Do NOT add migration logic:** SAVE-01 explicitly says "fresh saves only, no v1 migration." Future MIGR-01/MIGR-02 requirements handle that. V1 saves load via existing backward-compat path (mood = old happiness float, lifetime = 0).
- **Do NOT remove HappinessChanged from GameEvents:** The event definition stays until Phase 13 removes HappinessBar.
- **Do NOT change MoodSystem internals:** MoodSystem.RestoreState(float, float) and MoodSystem.Baseline are already correct. Only HappinessManager and SaveManager change.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| JSON serialization of new fields | Custom serialization logic | System.Text.Json auto-property serialization | Adding properties to a POCO is all that's needed; the serializer handles them automatically |
| Version detection | Complex field-presence sniffing | `data.Version >= 2` integer comparison | SaveData already has a Version field with default 1; v1 files deserialize with Version=1, new saves write Version=2 |
| Default values for missing fields | Explicit deserialization fallback code | C# default values on POCO properties | `int` defaults to 0, `float` defaults to 0f -- exactly what v1 backward-compat needs |

## Common Pitfalls

### Pitfall 1: Forgetting to Bump Version on Write
**What goes wrong:** New saves still write Version=1, so on next load they take the v1 path and lose lifetime/mood data.
**Why it happens:** The Version default is `= 1` in the POCO. CollectGameState must explicitly set Version=2.
**How to avoid:** Set `data.Version = 2` in CollectGameState, or change the POCO default to 2 (but this changes deserialization of v1 files -- not safe). Best: set explicitly in CollectGameState.
**Warning signs:** Loaded game always starts at Quiet mood with 0 lifetime wishes.

### Pitfall 2: Breaking RestoreState Signature Without Updating All Call Sites
**What goes wrong:** Expanding RestoreState to accept new parameters breaks the existing call in ApplyState if not updated simultaneously.
**Why it happens:** C# won't compile if signature changes and call sites don't match.
**How to avoid:** The v1 and v2 paths in ApplyState must both call the updated RestoreState with appropriate arguments. For v1: pass (happiness, 0f, 0, unlockedRooms, milestoneCount, housingCapacity). For v2: pass (mood, baseline, lifetimeHappiness, unlockedRooms, milestoneCount, housingCapacity).
**Warning signs:** Compilation errors.

### Pitfall 3: Removing HappinessChanged Subscription Without Adding Replacements
**What goes wrong:** If the old subscription is removed but MoodTierChanged/WishCountChanged aren't added, happiness changes no longer trigger autosave. Players lose progress.
**Why it happens:** Doing cleanup and wiring in separate steps with a gap between.
**How to avoid:** Add new subscriptions in the same commit as removing the old one. Or better: add new subscriptions first, remove old one after.
**Warning signs:** Game doesn't autosave after wish fulfillment.

### Pitfall 4: MoodSystem.Baseline Accessibility
**What goes wrong:** CollectGameState needs to read the mood baseline for save, but `MoodSystem._baseline` is private.
**Why it happens:** MoodSystem was designed before save requirements.
**How to avoid:** MoodSystem already has `public float Baseline => _baseline;` (line 21 of MoodSystem.cs). HappinessManager needs to expose this via a property like `public float MoodBaseline => _moodSystem?.Baseline ?? 0f;`.
**Warning signs:** Compilation error when CollectGameState tries to access baseline.

### Pitfall 5: Null-Safety in CollectGameState
**What goes wrong:** If HappinessManager.Instance is null when CollectGameState runs, new fields get default 0 values, which is actually safe. But accessing a property chain without null-conditional can crash.
**Why it happens:** Autoload initialization order edge cases.
**How to avoid:** Use `?.` operator consistently, same as the existing pattern: `HappinessManager.Instance?.LifetimeWishes ?? 0`.
**Warning signs:** NullReferenceException during save.

## Code Examples

### Example 1: SaveData v2 Fields
```csharp
// Add to SaveData class after existing fields
public class SaveData
{
    public int Version { get; set; } = 1;  // Keep default 1 for v1 deserialization safety
    public int Credits { get; set; }
    public float Happiness { get; set; }       // v1 field, kept for backward compat
    public int CrossedMilestoneCount { get; set; }
    public int HousingCapacity { get; set; }

    // v2 fields (default to 0/0f when deserializing v1 saves)
    public int LifetimeHappiness { get; set; }
    public float Mood { get; set; }
    public float MoodBaseline { get; set; }

    // ... existing collections unchanged
}
```

### Example 2: CollectGameState v2 Writes
```csharp
private SaveData CollectGameState()
{
    var data = new SaveData
    {
        Version = 2,  // CRITICAL: bump version for new saves
        Credits = EconomyManager.Instance?.Credits ?? 0,
        Happiness = 0f,  // v1 field: write 0 in v2 saves (or omit -- discretion)
        LifetimeHappiness = HappinessManager.Instance?.LifetimeWishes ?? 0,
        Mood = HappinessManager.Instance?.Mood ?? 0f,
        MoodBaseline = HappinessManager.Instance?.MoodBaseline ?? 0f,
        CrossedMilestoneCount = HappinessManager.Instance?.GetCrossedMilestoneCount() ?? 0,
        HousingCapacity = HappinessManager.Instance?.GetHousingCapacity() ?? 5,
        UnlockedRooms = HappinessManager.Instance?.GetUnlockedRoomIds().ToList() ?? new List<string>()
    };
    // ... rooms, citizens, wishes unchanged
    return data;
}
```

### Example 3: Version-Gated ApplyState
```csharp
public void ApplyState(SaveData data)
{
    CitizenManager.StateLoaded = true;
    HappinessManager.StateLoaded = true;
    EconomyManager.StateLoaded = true;

    EconomyManager.Instance?.RestoreCredits(data.Credits);

    if (data.Version >= 2)
    {
        // v2 path: full happiness state
        HappinessManager.Instance?.RestoreState(
            data.LifetimeHappiness,
            data.Mood,
            data.MoodBaseline,
            new HashSet<string>(data.UnlockedRooms),
            data.CrossedMilestoneCount,
            data.HousingCapacity);
    }
    else
    {
        // v1 backward-compat: happiness float as mood, no lifetime/baseline
        HappinessManager.Instance?.RestoreState(
            0,          // lifetimeHappiness
            data.Happiness,  // mood (old happiness float)
            0f,         // moodBaseline
            new HashSet<string>(data.UnlockedRooms),
            data.CrossedMilestoneCount,
            data.HousingCapacity);
    }

    PendingLoad = data;
}
```

### Example 4: Expanded HappinessManager.RestoreState
```csharp
public void RestoreState(int lifetimeHappiness, float mood, float moodBaseline,
    HashSet<string> unlockedRooms, int milestoneCount, int housingCapacity)
{
    _lifetimeHappiness = lifetimeHappiness;
    _moodSystem?.RestoreState(mood, moodBaseline);
    _lastReportedTier = _moodSystem?.CurrentTier ?? MoodTier.Quiet;

    _unlockedRooms.Clear();
    foreach (var roomId in unlockedRooms)
        _unlockedRooms.Add(roomId);

    _crossedMilestoneCount = milestoneCount;
    _housingCapacity = housingCapacity;

    EconomyManager.Instance?.SetMoodTier(_lastReportedTier);
}
```

### Example 5: Event Subscription Rewiring
```csharp
// In SaveManager fields - replace:
private Action<float> _onHappinessChanged;
// With:
private Action<MoodTier, MoodTier> _onMoodTierChanged;
private Action<int> _onWishCountChanged;

// In _Ready() delegate initialization - replace:
_onHappinessChanged = _ => OnAnyStateChanged();
// With:
_onMoodTierChanged = (_, _) => OnAnyStateChanged();
_onWishCountChanged = _ => OnAnyStateChanged();

// In SubscribeEvents - replace:
GameEvents.Instance.HappinessChanged += _onHappinessChanged;
// With:
GameEvents.Instance.MoodTierChanged += _onMoodTierChanged;
GameEvents.Instance.WishCountChanged += _onWishCountChanged;

// Mirror changes in UnsubscribeEvents
```

## Discretion Recommendations

### Version Detection Logic
**Recommendation:** Use `data.Version >= 2` (simple integer comparison).
**Rationale:** The Version field already exists and defaults to 1. It is the established pattern. Field-presence checking would require checking if `LifetimeHappiness != 0` which is ambiguous (a new game with zero wishes would have LifetimeHappiness=0 legitimately).

### Exposing MoodSystem.Baseline
**Recommendation:** Use a property on HappinessManager: `public float MoodBaseline => _moodSystem?.Baseline ?? 0f;`
**Rationale:** Consistent with existing `public float Mood => _moodSystem?.Mood ?? 0f;` one line above. Properties are idiomatic for simple read-only accessors in C#.

### Old Happiness Field in v2 Writes
**Recommendation:** Keep the field in SaveData, write 0f in v2 saves.
**Rationale:** Removing the field from the class would break v1 deserialization (System.Text.Json would ignore the JSON property). Keeping it and writing 0f is harmless and maintains structural backward compatibility. If someone somehow downgrades, the v1 reader gets 0f happiness which is safe (better than a crash).

### Event Subscription Cleanup Ordering
**Recommendation:** Wire new events first, then remove old events, in the same task. This ensures no window where happiness changes don't trigger autosave.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Single `Happiness` float in SaveData | Three fields: LifetimeHappiness, Mood, MoodBaseline | Phase 12 (this phase) | Save captures full mood system state |
| `HappinessChanged` event triggers autosave | `MoodTierChanged` + `WishCountChanged` trigger autosave | Phase 12 (this phase) | Autosave fires on actual happiness events (HappinessChanged was dead after Phase 10) |
| `RestoreState(float, HashSet, int, int)` | `RestoreState(int, float, float, HashSet, int, int)` | Phase 12 (this phase) | Full mood state restoration from save |

**Deprecated/outdated:**
- `HappinessManager.Happiness` property shim: Removed in this phase (was compatibility bridge for SaveManager)
- `HappinessChanged` subscription in SaveManager: Replaced by MoodTierChanged + WishCountChanged

## Open Questions

1. **Does HappinessBar still reference HappinessChanged?**
   - What we know: CONTEXT.md says "Keep HappinessChanged event definition in GameEvents (HappinessBar may still reference it until Phase 13)"
   - What's unclear: Whether HappinessBar actually subscribes. Phase 13 handles HUD replacement regardless.
   - Recommendation: Keep the event definition in GameEvents. Only remove the SaveManager subscription. Phase 13 handles the rest.

## Validation Architecture

> Note: `workflow.nyquist_validation` not present in config.json -- treating as enabled.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None detected (no test project, no test files) |
| Config file | None |
| Quick run command | N/A |
| Full suite command | N/A |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SAVE-01 | New v2 save contains LifetimeHappiness, Mood, MoodBaseline fields | manual | Load save.json, verify JSON fields present | N/A |
| SAVE-01 | Loading v2 save restores lifetime happiness count exactly | manual | Save with N wishes, load, verify LifetimeWishes == N | N/A |
| SAVE-01 | Loading v2 save restores mood and baseline so tier resumes correctly | manual | Save at Cozy tier, load, verify tier is Cozy | N/A |
| SAVE-01 | Fresh new game starts with zero lifetime happiness and Quiet tier | manual | Start new game, verify LifetimeWishes=0 and CurrentTier=Quiet | N/A |
| SAVE-01 | v1 save loads via backward-compat path (mood = old happiness, lifetime = 0) | manual | Load a v1 save file, verify mood=Happiness value, lifetime=0 | N/A |

### Sampling Rate
- **Per task commit:** Manual verification: open save.json, check fields; launch game, save/load cycle
- **Per wave merge:** Full save/load round-trip: new game -> fulfill wishes -> save -> quit -> load -> verify state
- **Phase gate:** All five behaviors verified manually before `/gsd:verify-work`

### Wave 0 Gaps
No automated test infrastructure exists. All verification is manual (game launch + file inspection). Creating a test framework is outside Phase 12 scope.

## Sources

### Primary (HIGH confidence)
- Source code: `/workspace/Scripts/Autoloads/SaveManager.cs` -- complete save/load implementation, SaveData POCO, event subscriptions
- Source code: `/workspace/Scripts/Autoloads/HappinessManager.cs` -- RestoreState signature, public API, MoodSystem ownership
- Source code: `/workspace/Scripts/Happiness/MoodSystem.cs` -- RestoreState(float, float), public Baseline property (line 21)
- Source code: `/workspace/Scripts/Autoloads/GameEvents.cs` -- MoodTierChanged, WishCountChanged, HappinessChanged event definitions

### Secondary (MEDIUM confidence)
- Phase 10/11 decisions in STATE.md -- established patterns for mood system, tier events, economy integration
- CONTEXT.md decisions -- user-locked implementation approach

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- examining actual source code, no external dependencies involved
- Architecture: HIGH -- all patterns directly observed in existing codebase
- Pitfalls: HIGH -- derived from reading actual code paths and identifying gaps
- Code examples: HIGH -- adapted from existing code patterns in the repository

**Research date:** 2026-03-05
**Valid until:** Indefinite (phase-specific research, all based on current source code)
