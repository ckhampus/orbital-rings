# Phase 16: Capacity Transfer - Research

**Researched:** 2026-03-06
**Domain:** C# refactoring -- Godot Autoload singleton state migration
**Confidence:** HIGH

## Summary

Phase 16 is a mechanical refactoring that removes duplicate housing capacity tracking from HappinessManager, making HousingManager the single source of truth. The changes touch four files: HappinessManager.cs (bulk removal), SaveManager.cs (remove HousingCapacity field and stop saving/restoring it), SaveData (remove property), and TitleScreen.cs (remove HappinessManager.StateLoaded reset).

HousingManager already has a `TotalCapacity` property that computes capacity from its `_housingRoomCapacities` dictionary. It already subscribes to RoomPlaced/RoomDemolished events and maintains its own capacity cache. The only new work is redirecting HappinessManager's arrival gating check to query HousingManager, and removing the now-dead code from HappinessManager and SaveManager.

**Primary recommendation:** Execute this as a single plan with surgical removals from HappinessManager, followed by the one-line arrival check update and SaveManager cleanup. Compile after each file to catch cascading breaks early.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- StarterCitizenCapacity = 5 stays as a private const in HappinessManager (arrival gating owner)
- Arrival check formula becomes: `StarterCitizenCapacity + HousingManager.Instance.TotalCapacity`
- No floor clamp needed -- TotalCapacity >= 0, so the sum is always >= 5
- This is a game-start bootstrap value, not a housing tunable -- doesn't belong in HousingConfig
- Remove `_housingCapacity` field and `_housingRoomCapacities` dictionary entirely
- Remove `CalculateHousingCapacity()` and `GetHousingCapacity()` public methods (no external callers after SaveManager update)
- Remove `InitializeHousingCapacity()` private method
- Remove `StateLoaded` flag -- it only guarded InitializeHousingCapacity; nothing else uses it
- Remove `OnRoomPlaced()` and `OnRoomDemolished()` handlers -- they only did capacity tracking
- Remove RoomPlaced and RoomDemolished event subscriptions from `_Ready()` and `_ExitTree()`
- Update class-level doc comment to reflect that arrival gating now queries HousingManager
- Remove `HousingCapacity` field from SaveData -- capacity is derivable from placed rooms
- Drop `int housingCapacity` parameter from `HappinessManager.RestoreState()`
- Update SaveManager call sites to stop saving/passing housing capacity
- No save version bump -- removing a field is backward-compatible (System.Text.Json ignores unknown properties)

### Claude's Discretion
- Exact wording of updated HappinessManager doc comment
- Order of cleanup within files (top-down vs grouped by concern)
- Whether to add a comment on the arrival check explaining the StarterCitizenCapacity + TotalCapacity formula

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| INFR-03 | Housing capacity tracking transferred from HappinessManager to HousingManager | HousingManager already has TotalCapacity property and full capacity tracking. Phase removes duplicate tracking from HappinessManager and redirects arrival gating to query HousingManager. SaveData.HousingCapacity removed as capacity is derivable from placed rooms. |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Godot 4.x | 4.x (C#) | Game engine runtime | Project engine |
| System.Text.Json | .NET builtin | Save serialization | Already used by SaveManager; backward-compatible with field removal |

### Supporting
No additional libraries needed. This phase is pure refactoring of existing code.

## Architecture Patterns

### Current Capacity Flow (BEFORE)
```
RoomPlaced/RoomDemolished events
    |
    +--> HappinessManager (tracks _housingCapacity, _housingRoomCapacities)  <-- DUPLICATE
    +--> HousingManager   (tracks _housingRoomCapacities, computes TotalCapacity)

Arrival Check: HappinessManager reads _housingCapacity (its own field)
Save:          SaveManager calls HappinessManager.GetHousingCapacity()
Load:          SaveManager passes housingCapacity to HappinessManager.RestoreState()
```

### Target Capacity Flow (AFTER)
```
RoomPlaced/RoomDemolished events
    |
    +--> HousingManager only (tracks _housingRoomCapacities, computes TotalCapacity)

Arrival Check: HappinessManager queries HousingManager.Instance.TotalCapacity
Save:          Capacity NOT saved (derivable from placed rooms, which ARE saved)
Load:          HousingManager.InitializeExistingRooms() rebuilds capacity from rooms
```

### Pattern: Arrival Check Update
**What:** Single-line change from local field read to cross-singleton query
**Current (line 303):**
```csharp
if (currentPop >= _housingCapacity) return;
```
**Target:**
```csharp
if (currentPop >= StarterCitizenCapacity + (HousingManager.Instance?.TotalCapacity ?? 0)) return;
```

### Pattern: Parameter Removal from RestoreState
**What:** Drop the `housingCapacity` parameter and its assignment
**Current:**
```csharp
public void RestoreState(int lifetimeHappiness, float mood, float moodBaseline,
    HashSet<string> unlockedRooms, int milestoneCount, int housingCapacity)
{
    // ...
    _housingCapacity = housingCapacity;
}
```
**Target:**
```csharp
public void RestoreState(int lifetimeHappiness, float mood, float moodBaseline,
    HashSet<string> unlockedRooms, int milestoneCount)
{
    // ... (_housingCapacity line removed)
}
```

### Anti-Patterns to Avoid
- **Leaving orphaned references:** After removing `HappinessManager.StateLoaded`, all external references (SaveManager.cs line 397, TitleScreen.cs line 223) must also be removed. Missing one causes a compile error.
- **Removing too much from `_Ready()`:** Only remove the two capacity-related event subscriptions (RoomPlaced, RoomDemolished) and the `StateLoaded` guard block. The WishFulfilled subscription, arrival timer, CanvasLayer, and config loading must stay.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Capacity computation | Custom calculation in HappinessManager | `HousingManager.Instance.TotalCapacity` | Already exists, already correct, already tested in Phase 15 |
| Save backward-compat | Version bump or migration | System.Text.Json ignores unknown fields | Removing `HousingCapacity` from SaveData is backward-compatible by default |

## Common Pitfalls

### Pitfall 1: Missing TitleScreen.cs Update
**What goes wrong:** `HappinessManager.StateLoaded` is removed from HappinessManager, but TitleScreen.cs line 223 still references it, causing a compile error.
**Why it happens:** CONTEXT.md only lists HappinessManager, SaveManager, and HousingManager as integration points. TitleScreen.cs is an easy miss.
**How to avoid:** After removing `StateLoaded` from HappinessManager, grep for `HappinessManager.StateLoaded` across the entire codebase. Remove the line from TitleScreen.cs (line 223) and SaveManager.cs (line 397).
**Warning signs:** Compile error referencing `HappinessManager.StateLoaded`.

### Pitfall 2: Missing SaveManager.ApplyState Call Site Updates
**What goes wrong:** RestoreState signature changes but SaveManager still passes the old argument count.
**Why it happens:** There are TWO call sites in SaveManager.ApplyState (v2 path at line 407 and v1 path at line 418). Both must be updated.
**How to avoid:** Both `HappinessManager.Instance?.RestoreState(...)` calls on lines 407-413 and 418-424 must drop the `data.HousingCapacity` argument.
**Warning signs:** Compile error about argument count mismatch.

### Pitfall 3: Removing StarterCitizenCapacity Constant
**What goes wrong:** Developer removes the constant along with other capacity-related code.
**Why it happens:** It looks like capacity code, but it's actually used by the arrival check.
**How to avoid:** CONTEXT.md explicitly states it stays. The constant changes usage but is NOT removed.
**Warning signs:** Arrival check would use a magic number instead of a named constant.

### Pitfall 4: Pre-existing Bug -- HousingManager.StateLoaded Not Reset in StartNewStation
**What goes wrong:** Not a phase 16 concern, but worth noting: `HousingManager.StateLoaded` is set to true in SaveManager.ApplyState but never reset to false in TitleScreen.StartNewStation. If a player loads a game, returns to title, and starts a new game, HousingManager would skip InitializeExistingRooms.
**How to avoid:** Out of scope for this phase, but the planner should flag it as a known issue. The fix would be adding `HousingManager.StateLoaded = false;` to StartNewStation, but that belongs in a separate fix.

## Code Examples

### Complete list of removals from HappinessManager.cs

```csharp
// REMOVE: StateLoaded property (line 33)
public static bool StateLoaded { get; set; }

// REMOVE: _housingCapacity field (line 87)
private int _housingCapacity = StarterCitizenCapacity;

// REMOVE: _housingRoomCapacities dictionary (line 94)
private readonly Dictionary<int, int> _housingRoomCapacities = new();

// REMOVE: CalculateHousingCapacity method (line 128)
public int CalculateHousingCapacity() => _housingCapacity;

// REMOVE: GetHousingCapacity method (line 141)
public int GetHousingCapacity() => _housingCapacity;

// REMOVE: housingCapacity parameter from RestoreState (line 149)
// and assignment on line 160

// REMOVE from _Ready(): RoomPlaced/RoomDemolished subscriptions (lines 177-178)
// REMOVE from _Ready(): StateLoaded guard block (lines 194-195)

// REMOVE from _ExitTree(): RoomPlaced/RoomDemolished unsubscriptions (lines 217-218)

// REMOVE: OnRoomPlaced handler (lines 365-378)
// REMOVE: OnRoomDemolished handler (lines 386-397)
// REMOVE: InitializeHousingCapacity method (lines 403-424)
```

### Arrival check update in HappinessManager.cs

```csharp
// Line 303 changes from:
if (currentPop >= _housingCapacity) return;
// To:
if (currentPop >= StarterCitizenCapacity + (HousingManager.Instance?.TotalCapacity ?? 0)) return;
```

### SaveData.cs changes (inside SaveManager.cs)

```csharp
// REMOVE from SaveData class (line 26):
public int HousingCapacity { get; set; }
```

### SaveManager.CollectGameState changes

```csharp
// REMOVE from CollectGameState (line 293):
HousingCapacity = HappinessManager.Instance?.GetHousingCapacity() ?? 5,
```

### SaveManager.ApplyState changes

```csharp
// Both call sites drop the last argument:
// v2 path (line 407-413):
HappinessManager.Instance?.RestoreState(
    data.LifetimeHappiness,
    data.Mood,
    data.MoodBaseline,
    new HashSet<string>(data.UnlockedRooms),
    data.CrossedMilestoneCount);
    // removed: data.HousingCapacity

// v1 path (line 418-424):
HappinessManager.Instance?.RestoreState(
    0,
    data.Happiness,
    0f,
    new HashSet<string>(data.UnlockedRooms),
    data.CrossedMilestoneCount);
    // removed: data.HousingCapacity
```

### SaveManager.ApplyState StateLoaded removal

```csharp
// REMOVE line 397:
HappinessManager.StateLoaded = true;
```

### TitleScreen.StartNewStation removal

```csharp
// REMOVE line 223:
HappinessManager.StateLoaded = false;
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| HappinessManager owns capacity | HousingManager owns capacity | Phase 14-15 (2026-03-06) | HousingManager already tracks capacity; HappinessManager's copy is now dead weight |
| SaveData stores HousingCapacity | Capacity derived from placed rooms | Phase 16 (this phase) | Eliminates desync risk, simplifies save format |

**Deprecated/outdated:**
- `HappinessManager._housingCapacity`: Replaced by `HousingManager.TotalCapacity`
- `SaveData.HousingCapacity`: Derivable state, no need to persist

## Affected Files Summary

| File | Changes | Lines Affected |
|------|---------|----------------|
| HappinessManager.cs | Bulk removal (~80 lines removed), 1 line changed, doc comment update | 33, 87, 94, 128, 141, 149-160, 177-178, 194-195, 217-218, 303, 365-424 |
| SaveManager.cs | Remove HousingCapacity from SaveData, CollectGameState, ApplyState (3 sites) | 26, 293, 397, 407-413, 418-424 |
| TitleScreen.cs | Remove HappinessManager.StateLoaded reset | 223 |
| HousingManager.cs | No changes needed (already has TotalCapacity) | None |

## Open Questions

1. **Pre-existing bug: HousingManager.StateLoaded not reset in StartNewStation**
   - What we know: SaveManager sets it to true, but StartNewStation never resets it to false
   - What's unclear: Whether this causes actual issues (if a player never loads then starts new, it's always false)
   - Recommendation: Out of scope for Phase 16. Document as known issue. Could be addressed in Phase 19 (save/load) or as a quick fix.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None -- no automated test infrastructure exists |
| Config file | None |
| Quick run command | N/A |
| Full suite command | N/A |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| INFR-03a | HappinessManager no longer tracks housing capacity | manual-only | Build project: `dotnet build` (compile check) | N/A |
| INFR-03b | Arrival gating queries HousingManager.TotalCapacity | manual-only | In-game: verify citizens stop arriving when pop >= StarterCitizenCapacity + TotalCapacity | N/A |
| INFR-03c | Building/demolishing rooms updates capacity in one place only | manual-only | Code review: grep for `_housingCapacity` confirms zero results outside HousingManager | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build` (compile verification -- this is a refactoring phase)
- **Per wave merge:** Manual smoke test: start new game, build housing, verify arrival cap works
- **Phase gate:** Compile clean + manual verification that arrival gating respects housing capacity

### Wave 0 Gaps
None -- no test infrastructure to create. This phase is mechanical refactoring verified by compilation and manual testing.

**Justification for manual-only:** No unit test framework exists in the project. The primary validation for this refactoring is compilation (removing dead code that compiles = correct removal) and a brief manual smoke test.

## Sources

### Primary (HIGH confidence)
- Direct source code inspection of HappinessManager.cs (425 lines)
- Direct source code inspection of HousingManager.cs (459 lines)
- Direct source code inspection of SaveManager.cs (514 lines)
- Direct source code inspection of TitleScreen.cs (line 223 -- StateLoaded reset)
- Grep verification: all callers of GetHousingCapacity, CalculateHousingCapacity, HappinessManager.StateLoaded confirmed

### Secondary (MEDIUM confidence)
- System.Text.Json backward compatibility (removing fields from deserialization target is safe -- unknown JSON properties are silently ignored by default)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - pure refactoring of code we can read directly
- Architecture: HIGH - both source and target patterns are visible in existing code
- Pitfalls: HIGH - all integration points verified by grep, line numbers confirmed
- Affected files: HIGH - comprehensive grep for all symbols being removed

**Research date:** 2026-03-06
**Valid until:** 2026-04-06 (stable -- mechanical refactoring of known code)
