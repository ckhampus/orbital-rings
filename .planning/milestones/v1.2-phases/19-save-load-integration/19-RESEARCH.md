# Phase 19: Save/Load Integration - Research

**Researched:** 2026-03-06
**Domain:** Save/load persistence audit for Godot 4.x C# game (System.Text.Json serialization)
**Confidence:** HIGH

## Summary

Phase 19 is a **code audit and gap-closure phase**, not a greenfield implementation phase. The save/load plumbing for housing assignments was already implemented incrementally across Phases 14 (schema), 15 (RestoreFromSave + stale reference handling), and 17 (EnsureHomeTimer on HomeSegmentIndex setter). The CONTEXT.md discussion confirmed this is verification work.

All three success criteria paths have been traced through the existing codebase and the code appears complete. The save path serializes `HomeSegmentIndex` via `HousingManager.GetHomeForCitizen()` (SaveManager.cs line 335). The load path restores assignments via `HousingManager.RestoreFromSave()` (SaveManager.cs lines 462-469). The stale reference path checks `_housingRoomCapacities.ContainsKey()` and logs a warning (HousingManager.cs lines 192-196). v2 backward compatibility is handled by `int?` defaulting to `null` during System.Text.Json deserialization.

**Primary recommendation:** Trace all three code paths with detailed commentary, fix any gaps found (likely zero), add clarifying comments or improved logging where beneficial, and commit the verification result regardless of whether code changes are needed.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- This is a **code audit + fix** phase, not greenfield implementation
- Pure code audit approach -- trace all 3 save/load paths explicitly (no runtime testing or Godot launch)
- v2 backward compatibility verified by tracing deserialization path, not by loading actual v2 save file
- Mid-activity save: citizens resume from walkway on reload (no mid-visit or mid-home-rest state saved)
- No new save fields needed (no isAtHome, no radialOffset, no homeRestProgress)
- Minimal fixes for broken code + light cleanup if it improves clarity
- No architectural changes, no refactoring beyond what's needed
- REQUIREMENTS.md update (marking INFR-04 complete) deferred to milestone closure
- Always produce a commit, even if audit finds zero code issues

### Claude's Discretion
- Exact audit ordering (which path to trace first)
- Whether to add/improve GD.Print logging in the restore path
- Whether to add comments clarifying the save/load flow for future readers
- Light cleanup scope (better error messages, removing dead code paths)

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| INFR-04 | Save/load housing assignments with backward compatibility (v2 saves load as unhoused) | All three code paths traced: normal save/load, v2 backward compat, stale reference handling. Code exists in SaveManager.cs + HousingManager.cs + CitizenNode.cs |
| INFR-05 | Save format bumped to v3 with nullable HomeSegmentIndex | Already complete (Phase 14). SavedCitizen.HomeSegmentIndex is `int?`, SaveData.Version is 3. Verification confirms schema is correct |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| System.Text.Json | .NET 8 built-in | JSON serialization/deserialization | Already used by SaveManager for all save/load; handles nullable `int?` correctly |
| Godot 4.x | 4.x (C# binding) | Game engine runtime | Project engine |

### Supporting
No additional libraries needed for this audit phase.

## Architecture Patterns

### Existing Save/Load Architecture (Already Implemented)

```
Save Flow:
  SaveManager.PerformSave()
    -> CollectGameState()
       -> HousingManager.GetHomeForCitizen(name) -> int? HomeSegmentIndex
       -> SavedCitizen { HomeSegmentIndex = int? }
    -> JsonSerializer.Serialize(saveData)
    -> FileAccess.Open("user://save.json").StoreString(json)

Load Flow (2-stage):
  Stage 1 - Pre-scene (ApplyState):
    -> HousingManager.StateLoaded = true  (prevents _Ready InitializeExistingRooms)
    -> PendingLoad = data

  Stage 2 - Post-scene (ApplySceneState, after 2-frame delay):
    -> BuildManager.RestorePlacedRoom()    (rooms first)
    -> CitizenManager.SpawnCitizenFromSave()  (citizens second)
    -> HousingManager.RestoreFromSave(assignments)  (housing third)
       -> InitializeExistingRooms(assignCitizens: false)  (populate capacity dict)
       -> For each (name, homeIndex):
          - null/negative -> skip (citizen stays unhoused)
          - room missing -> log warning, skip (stale reference)
          - room exists -> AssignCitizen(name, anchorIndex)
       -> AssignAllUnhoused() (catch remaining)
       -> _isRestoring = false
       -> EmitHousingStateChanged()
```

### Pattern 1: Nullable int? for Backward Compatibility
**What:** `SavedCitizen.HomeSegmentIndex` is `int?` not `int`. When deserializing v2 saves that lack this field, System.Text.Json defaults it to `null`.
**When to use:** Any new save field that must be backward-compatible with older save formats.
**Verification:** System.Text.Json ignores unknown/missing properties by default (no `JsonSerializerOptions.PropertyNameCaseInsensitive` needed for missing fields). A missing JSON property on a nullable type deserializes to `null`. This is confirmed by the existing v1->v2 migration pattern where `LifetimeHappiness`, `Mood`, and `MoodBaseline` default to 0/0f.

### Pattern 2: StateLoaded Guard
**What:** `HousingManager.StateLoaded` static bool prevents `_Ready()` from calling `InitializeExistingRooms()` during load, since the SaveManager will handle initialization separately via `RestoreFromSave()`.
**When to use:** Any autoload that has default initialization in `_Ready()` that would conflict with save restoration.
**Verification:** Set in `ApplyState()` (line 396). Checked in `HousingManager._Ready()` (line 110). Pattern matches `CitizenManager.StateLoaded` and `EconomyManager.StateLoaded`.

### Pattern 3: _isRestoring Event Suppression
**What:** `HousingManager._isRestoring` bool suppresses `CitizenAssignedHome` and `CitizenUnhoused` events during `RestoreFromSave()` to prevent autosave loops.
**When to use:** Any batch operation that would trigger many events, each of which would cause a save.
**Verification:** Set to `true` at line 185, `false` at line 204. Checked in `AssignCitizen()` (line 363) and `OnRoomDemolished()` (line 274).

### Anti-Patterns to Avoid
- **Running Godot to test save/load:** Locked decision -- this is a code audit only.
- **Adding new save fields:** Locked decision -- no new fields needed.
- **Refactoring beyond gap fixes:** Locked decision -- minimal changes only.

## Don't Hand-Roll

Not applicable to this audit phase. All save/load infrastructure already exists.

## Common Pitfalls

### Pitfall 1: Order Dependency in ApplySceneState
**What goes wrong:** Housing restoration crashes if rooms or citizens aren't restored first.
**Why it happens:** `RestoreFromSave()` calls `InitializeExistingRooms()` which scans `BuildManager` for placed rooms, and `AssignCitizen()` calls `FindCitizenNode()` which iterates `CitizenManager.Citizens`.
**How to avoid:** Rooms MUST be restored before citizens, citizens MUST be restored before housing. This order is already correct in `ApplySceneState()` (lines 437-469).
**Warning signs:** NullReferenceException in RestoreFromSave, empty `_housingRoomCapacities` after load.

### Pitfall 2: InitializeExistingRooms Called Twice
**What goes wrong:** If `StateLoaded` is not set, `_Ready()` calls `InitializeExistingRooms()` AND `RestoreFromSave()` calls it again, leading to duplicate capacity entries.
**Why it happens:** `_Ready()` runs before `ApplySceneState()` (that's the whole point of the 2-frame delay).
**How to avoid:** `StateLoaded` guard in `_Ready()` (line 110). Already implemented.
**Warning signs:** Doubled capacity values.

### Pitfall 3: Event Storm During Restore
**What goes wrong:** Each `AssignCitizen()` call during restore emits `CitizenAssignedHome`, triggering autosave, which re-serializes mid-restore.
**Why it happens:** SaveManager subscribes to `CitizenAssignedHome` for autosave.
**How to avoid:** `_isRestoring` flag suppresses events. Already implemented.
**Warning signs:** SaveManager.PerformSave called during RestoreFromSave stack trace.

### Pitfall 4: Home Timer Not Starting After Load
**What goes wrong:** Citizens have HomeSegmentIndex set but never return home because the timer wasn't started.
**Why it happens:** During restore, events are suppressed so `OnCitizenAssignedHome` (which starts the timer) never fires.
**How to avoid:** `HomeSegmentIndex` setter calls `EnsureHomeTimer()` directly (CitizenNode.cs lines 164-166). This runs even when events are suppressed because it's triggered by the property setter, not the event.
**Warning signs:** Citizens never walk home after loading.

### Pitfall 5: Stale Reference After Room Demolish + Save
**What goes wrong:** Player demolishes a room, citizen gets reassigned, saves, loads -- but if the save captured the old assignment before reassignment, the loaded state has a stale reference.
**Why it happens:** Race condition between demolish event processing and save debounce.
**How to avoid:** The 0.5s debounce timer ensures the save happens after all event handlers complete. The demolish handler synchronously updates `_citizenHomes` and `CitizenNode.HomeSegmentIndex` before the timer fires. Additionally, `RestoreFromSave` has explicit stale reference detection (ContainsKey check).
**Warning signs:** "Stale home reference" log message during load.

## Code Examples

### Verified: Save Path (SaveManager.cs lines 315-337)
```csharp
// Each citizen's home is serialized as nullable int
HomeSegmentIndex = HousingManager.Instance?.GetHomeForCitizen(citizenData.CitizenName)
```
Source: `/workspace/Scripts/Autoloads/SaveManager.cs` line 335

### Verified: Load Path (SaveManager.cs lines 462-469)
```csharp
// Restore housing assignments (must come after rooms AND citizens are restored)
if (HousingManager.Instance != null)
{
    var assignments = data.Citizens
        .Select(c => (c.Name, c.HomeSegmentIndex))
        .ToList();
    HousingManager.Instance.RestoreFromSave(assignments);
}
```
Source: `/workspace/Scripts/Autoloads/SaveManager.cs` lines 462-469

### Verified: Stale Reference Detection (HousingManager.cs lines 188-196)
```csharp
// Only assign if the room still exists (stale references become unhoused)
if (!_housingRoomCapacities.ContainsKey(homeIndex.Value))
{
    GD.Print($"Housing: Stale home reference for {citizenName} at segment {homeIndex.Value} -- citizen is unhoused");
    continue;
}
```
Source: `/workspace/Scripts/Autoloads/HousingManager.cs` lines 192-196

### Verified: v2 Backward Compatibility (SavedCitizen.cs line 74)
```csharp
/// Added in save format v3. Deserializes as null from v2 saves (backward-compatible).
public int? HomeSegmentIndex { get; set; }
```
Source: `/workspace/Scripts/Autoloads/SaveManager.cs` line 74

### Verified: Home Timer Lazy Creation (CitizenNode.cs lines 772-795)
```csharp
private void EnsureHomeTimer()
{
    if (_homeTimer != null)
    {
        if (_homeTimer.IsStopped())
            RearmHomeTimer();
        return;
    }
    // Try to get housing config now (may have been null during Initialize)
    _housingConfig ??= HousingManager.Instance?.Config;
    if (_housingConfig == null) return;

    _homeTimer = new Timer { Name = "HomeTimer", OneShot = true, ... };
    _homeTimer.Timeout += OnHomeTimerTimeout;
    AddChild(_homeTimer);
    _homeTimer.Start();
}
```
Source: `/workspace/Scripts/Citizens/CitizenNode.cs` lines 772-795

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| HappinessManager owned capacity | HousingManager owns capacity | Phase 16 | Single source of truth |
| int HomeSegmentIndex (0 = unhoused) | int? HomeSegmentIndex (null = unhoused) | Phase 14 | Clean v2 backward compat |
| No _isRestoring guard | _isRestoring suppresses events during restore | Phase 15 | Prevents autosave loops |
| Manual timer start after assignment | EnsureHomeTimer on setter | Phase 17 | Timer works even during suppressed-event restore |

## Open Questions

1. **Are there any edge cases with the HousingStateChanged event after restore?**
   - What we know: `EmitHousingStateChanged()` fires once at end of `RestoreFromSave()`. PopulationDisplay and CitizenInfoPanel subscribe to this.
   - What's unclear: Nothing significant -- the event fires after all internal state is consistent.
   - Recommendation: Verify PopulationDisplay updates correctly on this event during audit.

2. **Does SpawnCitizenFromSave skip CitizenArrived intentionally?**
   - What we know: Yes, intentionally. `SpawnCitizenFromSave` does NOT emit `CitizenArrived` because these citizens already arrived in a prior session. `CitizenArrived` would trigger `OnCitizenArrived` in HousingManager, causing double-assignment.
   - What's unclear: Nothing -- this is correct behavior.
   - Recommendation: Confirm this in audit commentary.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None (Godot 4.x C# -- no test framework configured) |
| Config file | none |
| Quick run command | N/A |
| Full suite command | N/A |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| INFR-04 | Housing assignments persist across save/load | manual-only | Code audit traces all paths | N/A |
| INFR-04 | v2 saves load citizens as unhoused | manual-only | Code audit traces deserialization | N/A |
| INFR-04 | Stale references detected and reassigned | manual-only | Code audit traces ContainsKey check | N/A |
| INFR-05 | Save format v3 with nullable HomeSegmentIndex | manual-only | Already verified in Phase 14 | N/A |

**Justification for manual-only:** This is a code audit phase per locked decision. No runtime testing or Godot launch required. Verification is through code path tracing, not automated tests.

### Sampling Rate
N/A -- audit phase with no automated tests.

### Wave 0 Gaps
None -- this is a pure code audit phase. No test infrastructure needed.

## Audit Tracing Guide

The planner should structure tasks around the three code paths from CONTEXT.md:

### Path 1: Normal Save/Load (Housed Citizens)
**Save:** `CollectGameState()` -> for each citizen -> `HousingManager.GetHomeForCitizen(name)` -> `SavedCitizen.HomeSegmentIndex`
**Load:** `ApplyState()` -> `HousingManager.StateLoaded = true` -> `ApplySceneState()` -> rooms restored -> citizens spawned (with `HousingManager.Instance?.Config` passed to Initialize) -> `RestoreFromSave(assignments)` -> `InitializeExistingRooms(assignCitizens: false)` -> for each assignment -> `AssignCitizen(name, anchor)` -> `CitizenNode.HomeSegmentIndex = anchor` -> `EnsureHomeTimer()` -> timer starts -> citizen will walk home
**Key files:** SaveManager.cs (lines 284-352, 391-482), HousingManager.cs (lines 177-207), CitizenNode.cs (lines 159-168, 772-795)

### Path 2: v2 Save Deserialization
**Flow:** `Load()` -> `JsonSerializer.Deserialize<SaveData>(json)` -> `SavedCitizen.HomeSegmentIndex` is missing in JSON -> defaults to `null` -> `RestoreFromSave()` -> `homeIndex == null` -> `continue` (line 189) -> citizen remains unhoused -> `AssignAllUnhoused()` assigns from scratch
**Key insight:** No version check needed for this path. The nullable `int?` default handles it implicitly.

### Path 3: Stale Reference Detection
**Flow:** `RestoreFromSave()` -> `InitializeExistingRooms(assignCitizens: false)` populates `_housingRoomCapacities` from current BuildManager rooms -> for each assignment -> `_housingRoomCapacities.ContainsKey(homeIndex.Value)` -> if false: log warning, skip (citizen stays unhoused) -> `AssignAllUnhoused()` catches them
**Key insight:** The capacity dictionary is populated from the actual restored rooms, not from save data. This means demolished rooms (not in save) correctly result in missing keys.

## Sources

### Primary (HIGH confidence)
- `/workspace/Scripts/Autoloads/SaveManager.cs` - Full save/load orchestrator code read and traced
- `/workspace/Scripts/Autoloads/HousingManager.cs` - Full assignment engine with RestoreFromSave read and traced
- `/workspace/Scripts/Citizens/CitizenNode.cs` - HomeSegmentIndex setter and EnsureHomeTimer read and traced
- `/workspace/Scripts/Citizens/CitizenManager.cs` - SpawnCitizenFromSave flow read and traced
- `/workspace/Scripts/Autoloads/GameEvents.cs` - All housing events verified

### Secondary (MEDIUM confidence)
- System.Text.Json nullable handling - well-documented .NET behavior: missing JSON properties on nullable types default to null

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - all code read directly from source files
- Architecture: HIGH - all three code paths traced line-by-line through actual source
- Pitfalls: HIGH - identified from actual code patterns, not hypothetical

**Research date:** 2026-03-06
**Valid until:** 2026-04-06 (stable -- code audit of existing implementation)
