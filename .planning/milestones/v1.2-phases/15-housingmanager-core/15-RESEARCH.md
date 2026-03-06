# Phase 15: HousingManager Core - Research

**Researched:** 2026-03-06
**Domain:** Citizen-to-housing-room assignment engine (Godot 4 / C#)
**Confidence:** HIGH

## Summary

Phase 15 implements the core assignment engine inside the existing `HousingManager` skeleton (created in Phase 14). The system must subscribe to three events (RoomPlaced, RoomDemolished, CitizenArrived), maintain a citizen-to-room mapping with size-scaled capacity, and emit CitizenAssignedHome/CitizenUnhoused events for downstream consumers.

The codebase is well-prepared: GameEvents already defines both housing events, SavedCitizen already has a `HomeSegmentIndex` nullable int, and HousingConfig exists with timing constants. The primary implementation challenge is that `BuildManager.GetPlacedRoom()` currently returns `(Definition, AnchorIndex, Cost)` but does NOT include `SegmentCount` -- the HousingManager needs segment count to compute capacity via `BaseCapacity + (segmentCount - 1)`. This requires either extending the GetPlacedRoom return type or adding a new query method.

The secondary concern is save/load integration: SaveManager.CollectGameState currently does not write `HomeSegmentIndex` to SavedCitizen, and ApplySceneState does not restore it. Both must be wired during this phase, restoring from saved data rather than re-running the assignment algorithm.

**Primary recommendation:** Implement HousingManager as a pure data/logic manager with Dictionary-based tracking, following HappinessManager's capacity-tracking pattern exactly. Extend BuildManager.GetPlacedRoom to include SegmentCount, as this is the cleanest way to obtain the data needed for capacity computation.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Silent engine -- no visual feedback (no floating text, no sounds) when citizens get or lose homes
- Events DO fire on every assignment/displacement (CitizenAssignedHome, CitizenUnhoused) for future subscribers
- SaveManager wired to housing events for autosave (same pattern as RoomPlaced/CitizenArrived)
- Visual feedback deferred to Phase 17 (return-home behavior) and Phase 18 (UI panels)
- Instant reassignment: displaced citizens are reassigned in the same frame as demolition
- Partial reassignment: reassign as many displaced citizens as possible to available rooms
- Priority: oldest-arrived citizens get reassigned first (same rule as new-room assignment)
- Citizens that can't be reassigned remain unhoused (no penalty -- HOME-05)
- HousingManager tracks its own housing capacity (Dictionary<anchorIndex, capacity>)
- HousingManager maintains its own registry of housing rooms (subscribes to RoomPlaced/RoomDemolished)
- HappinessManager keeps its own arrival gating via _housingCapacity until Phase 16 transfers it
- Both systems track capacity in parallel during Phase 15 -- Phase 16 removes HappinessManager's copy
- `BaseCapacity + (segmentCount - 1)` -- +1 citizen per extra segment
- Bunk Pod: 1-seg=2, 2-seg=3. Sky Loft: 1-seg=4, 2-seg=5, 3-seg=6
- HousingManager.ComputeCapacity(definition, segmentCount) is the authority -- formula lives in one place
- Trust BuildManager for segment count validation -- no double-checking in HousingManager
- Fewest-occupants-first (even spread), ties broken randomly (PRD section 1)
- "Oldest" means spawn/arrival order (position in CitizenManager's list)
- Batch assignment: all eligible citizens assigned in same frame when a new room is built
- No staggered delays -- since there's no visual feedback in Phase 15, staggering is invisible
- Restore from saved HomeSegmentIndex -- rebuild HousingManager's internal mapping from save data
- Do NOT re-run assignment algorithm on load (would shuffle homes)
- Stale assignments (saved home references demolished room) handled as unhoused, then reassigned

### Claude's Discretion
- Internal data structure for citizen-to-room mapping (Dictionary, List, etc.)
- Exact method signatures and API surface on HousingManager
- How to obtain placed room segment count from BuildManager
- Whether to use a public method or property for unhoused citizen queries

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| HOME-01 | Citizens are automatically assigned to a housing room on arrival (fewest-occupants-first) | Subscribe to CitizenArrived, find room with min occupants, emit CitizenAssignedHome |
| HOME-02 | Citizens are reassigned when their home room is demolished (or become unhoused) | Subscribe to RoomDemolished, use pre-cached capacity dict (room gone before event fires), reassign displaced citizens oldest-first |
| HOME-03 | Unhoused citizens are assigned when new housing rooms are built (oldest-first) | Subscribe to RoomPlaced, iterate CitizenManager.Citizens in list order (oldest-first), batch assign same frame |
| HOME-04 | Housing capacity scales with room size (BaseCapacity + segments - 1) | ComputeCapacity static/instance method, needs segment count from BuildManager (API extension required) |
| HOME-05 | Unhoused citizens function identically to housed ones (no penalty, no debuff) | Verify no behavioral branching on housing status exists -- currently none (CitizenNode has no home concept) |
| INFR-01 | New HousingManager autoload singleton owns citizen-to-room mapping | Skeleton exists from Phase 14, extend with full assignment logic, capacity tracking, event subscriptions |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Godot 4.x | 4.x (C#) | Engine runtime | Project engine |
| System.Collections.Generic | .NET | Dictionary/List for mappings | Already used in all managers |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| GameEvents (project) | N/A | Event bus for CitizenAssignedHome/CitizenUnhoused | All housing state changes |
| BuildManager (project) | N/A | Query placed rooms for definition + segment count | Capacity computation |
| CitizenManager (project) | N/A | Access citizen list for oldest-first ordering | Assignment + reassignment |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Dictionary<int, List<string>> (room->citizens) | Flat List<(string,int)> | Dictionary gives O(1) room lookup, List requires scan. Dictionary wins for demolish reassignment. |
| Extending GetPlacedRoom return type | New GetPlacedRoomWithSegments method | Extending existing method is simpler, avoids API duplication |

## Architecture Patterns

### Recommended Project Structure
```
Scripts/
  Autoloads/
    HousingManager.cs     # Full implementation (was skeleton)
    SaveManager.cs        # +HomeSegmentIndex in save/load
    GameEvents.cs         # No changes (events already defined)
  Build/
    BuildManager.cs       # +SegmentCount in GetPlacedRoom return type
  Citizens/
    CitizenNode.cs        # +HomeSegmentIndex property (runtime state)
```

### Pattern 1: Event-Driven Capacity Tracking (from HappinessManager)
**What:** Manager subscribes to RoomPlaced/RoomDemolished, caches capacity in a Dictionary keyed by anchor index, so capacity is available even after room is demolished.
**When to use:** Any manager that needs to react to room lifecycle events.
**Example:**
```csharp
// Source: HappinessManager.cs lines 86-94 (existing pattern)
// HousingManager mirrors this exactly
private readonly Dictionary<int, int> _housingRoomCapacities = new();
private readonly Dictionary<int, List<string>> _roomOccupants = new();

private void OnRoomPlaced(string roomType, int segmentIndex)
{
    var roomInfo = BuildManager.Instance?.GetPlacedRoom(segmentIndex);
    if (roomInfo == null) return;
    var def = roomInfo.Value.Definition;
    if (def.Category != RoomDefinition.RoomCategory.Housing) return;

    int segmentCount = roomInfo.Value.SegmentCount; // needs API extension
    int capacity = ComputeCapacity(def, segmentCount);
    int anchorIndex = roomInfo.Value.AnchorIndex;
    _housingRoomCapacities[anchorIndex] = capacity;
    _roomOccupants[anchorIndex] = new List<string>();

    // Assign unhoused citizens oldest-first
    AssignUnhousedCitizens(anchorIndex, capacity);
}
```

### Pattern 2: Oldest-First Assignment via List Order
**What:** CitizenManager._citizens is a List<CitizenNode> where citizens are appended in arrival order. Iterating this list in order naturally gives oldest-first priority.
**When to use:** When assigning homes to unhoused citizens on room build, or reassigning displaced citizens on demolish.
**Example:**
```csharp
// CitizenManager.Citizens is IReadOnlyList<CitizenNode>
// List order = spawn/arrival order = oldest first
foreach (var citizen in CitizenManager.Instance.Citizens)
{
    if (GetHomeForCitizen(citizen.Data.CitizenName) < 0) // unhoused
    {
        if (GetOccupantCount(anchorIndex) < capacity)
        {
            AssignCitizen(citizen.Data.CitizenName, anchorIndex);
        }
    }
}
```

### Pattern 3: Fewest-Occupants-First with Random Tiebreak
**What:** When a citizen arrives, find all housing rooms with vacancy, pick the one with fewest occupants. Break ties randomly.
**When to use:** HOME-01 assignment on citizen arrival.
**Example:**
```csharp
private int FindBestRoom()
{
    int bestAnchor = -1;
    int minOccupants = int.MaxValue;
    int tieCount = 0;

    foreach (var (anchor, capacity) in _housingRoomCapacities)
    {
        int occupants = _roomOccupants.TryGetValue(anchor, out var list) ? list.Count : 0;
        if (occupants >= capacity) continue; // full

        if (occupants < minOccupants)
        {
            minOccupants = occupants;
            bestAnchor = anchor;
            tieCount = 1;
        }
        else if (occupants == minOccupants)
        {
            tieCount++;
            // Reservoir sampling for random tiebreak
            if (GD.Randi() % tieCount == 0)
                bestAnchor = anchor;
        }
    }
    return bestAnchor;
}
```

### Pattern 4: Save/Load Restoration Without Re-Running Algorithm
**What:** On load, rebuild HousingManager state from saved HomeSegmentIndex on each citizen, not by re-running assignment. Stale references (demolished rooms) become unhoused.
**When to use:** SaveManager.ApplySceneState, after rooms and citizens are restored.
**Example:**
```csharp
// In HousingManager - called after rooms and citizens are restored
public void RestoreFromSave(IReadOnlyList<(string citizenName, int? homeIndex)> assignments)
{
    foreach (var (name, homeIndex) in assignments)
    {
        if (homeIndex == null || homeIndex < 0) continue;
        if (!_housingRoomCapacities.ContainsKey(homeIndex.Value))
        {
            // Stale reference: room no longer exists, citizen is unhoused
            continue;
        }
        AssignCitizenDirect(name, homeIndex.Value); // no event emission on restore
    }
    // After restore, attempt to assign any unhoused citizens
    AssignAllUnhoused();
}
```

### Anti-Patterns to Avoid
- **Re-running assignment on load:** Would shuffle citizen homes, breaking player expectations. Restore from saved data.
- **Querying BuildManager during RoomDemolished:** The room is already gone by the time the event fires. Pre-cache capacity in a dictionary (same pattern as HappinessManager).
- **Coupling HousingManager to HappinessManager capacity:** Both track capacity in parallel during Phase 15. Phase 16 will transfer ownership. Don't create cross-references now.
- **Adding visual feedback:** Phase 15 is engine-only. Events fire, but no floating text, sounds, or animations.
- **Emitting events during save restoration:** Would trigger autosave loops and false state changes.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Random tiebreaking | Custom random selection | `GD.Randi() % tieCount` reservoir sampling | Statistically uniform, single-pass |
| Citizen arrival order | Sorting by timestamp | `CitizenManager.Citizens` list order | Already insertion-ordered, no metadata needed |
| Capacity computation | Separate formula per room type | `ComputeCapacity(def, segCount)` static method | Single source of truth, PRD formula |
| Event debouncing for save | Custom debounce logic | SaveManager's existing `_debounceTimer` pattern | Already handles rapid-fire events |

## Common Pitfalls

### Pitfall 1: Room Gone Before RoomDemolished Fires
**What goes wrong:** Trying to query BuildManager.GetPlacedRoom inside OnRoomDemolished returns null -- the room was already removed.
**Why it happens:** BuildManager removes the room from _placedRooms, THEN emits RoomDemolished (see BuildManager.cs lines 564-567).
**How to avoid:** Pre-cache room data in HousingManager's own Dictionary when RoomPlaced fires. On RoomDemolished, read from the cache.
**Warning signs:** `GetPlacedRoom(segmentIndex)` returns null in demolish handler.

### Pitfall 2: GetPlacedRoom Doesn't Return SegmentCount
**What goes wrong:** HousingManager cannot compute capacity because GetPlacedRoom returns `(Definition, AnchorIndex, Cost)` -- no segment count.
**Why it happens:** The PlacedRoom record has SegmentCount but GetPlacedRoom only exposes three fields.
**How to avoid:** Extend GetPlacedRoom's return type to include SegmentCount: `(RoomDefinition Definition, int AnchorIndex, int SegmentCount, int Cost)?`
**Warning signs:** Using `def.BaseCapacity` directly instead of `ComputeCapacity(def, segCount)` -- would break HOME-04.

### Pitfall 3: Event Emission During Save Restoration
**What goes wrong:** Restoring housing assignments from save emits CitizenAssignedHome events, which triggers SaveManager autosave, creating a save-loop.
**Why it happens:** Assignment helper methods emit events by default.
**How to avoid:** Use a separate code path for save restoration that skips event emission. Or use a `_isRestoring` flag guard.
**Warning signs:** Autosave fires immediately after load completes.

### Pitfall 4: Citizen Order Assumption
**What goes wrong:** Assuming CitizenManager.Citizens is sorted by some field. It is insertion-ordered -- oldest citizens are first in the list.
**Why it happens:** No explicit sorting exists; the List is append-only (SpawnCitizen/SpawnCitizenFromSave both call `_citizens.Add(citizen)`).
**How to avoid:** Rely on list order for oldest-first semantics. The CONTEXT.md explicitly defines "oldest" as position in CitizenManager's list.
**Warning signs:** Trying to sort by timestamp or adding a timestamp field.

### Pitfall 5: Parallel Capacity Tracking Confusion
**What goes wrong:** HousingManager and HappinessManager both track capacity. One uses BaseCapacity only, the other uses size-scaled capacity. Numbers disagree.
**Why it happens:** HappinessManager.OnRoomPlaced uses `def.BaseCapacity` (flat, not size-scaled). HousingManager uses `BaseCapacity + (segCount - 1)`.
**How to avoid:** Accept the discrepancy during Phase 15. HappinessManager's capacity is only used for arrival gating (`currentPop >= _housingCapacity`). Phase 16 will transfer to HousingManager's (correct) values.
**Warning signs:** Tests expecting both managers to report identical capacity.

### Pitfall 6: HousingManager _Ready Ordering
**What goes wrong:** HousingManager._Ready tries to access BuildManager.Instance but it's null.
**Why it happens:** Autoload initialization order in project.godot matters. HousingManager (7th) loads AFTER BuildManager (3rd), so BuildManager.Instance is available.
**How to avoid:** Current autoload order is correct: GameEvents, EconomyManager, BuildManager, CitizenManager, WishBoard, HappinessManager, HousingManager, SaveManager.
**Warning signs:** NullReferenceException on Instance access in _Ready.

## Code Examples

### ComputeCapacity (Single Source of Truth)
```csharp
// Source: CONTEXT.md locked decision
/// <summary>
/// Computes housing capacity for a room: BaseCapacity + (segmentCount - 1).
/// This is the SOLE authority for capacity calculation.
/// </summary>
public static int ComputeCapacity(RoomDefinition definition, int segmentCount)
{
    return definition.BaseCapacity + (segmentCount - 1);
}
```

### BuildManager.GetPlacedRoom Extension
```csharp
// Extend return type to include SegmentCount
public (RoomDefinition Definition, int AnchorIndex, int SegmentCount, int Cost)? GetPlacedRoom(int flatIndex)
{
    // Direct anchor lookup
    if (_placedRooms.TryGetValue(flatIndex, out var directRoom))
        return (directRoom.Definition, flatIndex, directRoom.SegmentCount, directRoom.Cost);

    // Check if flatIndex falls within any multi-segment room's range
    var (queryRow, queryPos) = SegmentGrid.FromIndex(flatIndex);
    foreach (var (anchorIndex, room) in _placedRooms)
    {
        if (room.Row != queryRow) continue;
        for (int i = 0; i < room.SegmentCount; i++)
        {
            int pos = SegmentGrid.WrapPosition(room.StartPos + i);
            if (pos == queryPos)
                return (room.Definition, anchorIndex, room.SegmentCount, room.Cost);
        }
    }
    return null;
}
```

### SaveManager Integration (CollectGameState)
```csharp
// In CollectGameState, add HomeSegmentIndex to each SavedCitizen
data.Citizens.Add(new SavedCitizen
{
    // ... existing fields ...
    HomeSegmentIndex = HousingManager.Instance?.GetHomeForCitizen(citizenData.CitizenName)
});
```

### SaveManager Integration (ApplySceneState)
```csharp
// After rooms and citizens are restored, restore housing assignments
if (HousingManager.Instance != null)
{
    var assignments = data.Citizens
        .Select(c => (c.Name, c.HomeSegmentIndex))
        .ToList();
    HousingManager.Instance.RestoreFromSave(assignments);
}
```

### SaveManager Event Subscription
```csharp
// Add to SaveManager.SubscribeEvents / UnsubscribeEvents
private Action<string, int> _onCitizenAssignedHome;
private Action<string> _onCitizenUnhoused;

// In _Ready or SubscribeEvents:
_onCitizenAssignedHome = (_, _) => OnAnyStateChanged();
_onCitizenUnhoused = _ => OnAnyStateChanged();
GameEvents.Instance.CitizenAssignedHome += _onCitizenAssignedHome;
GameEvents.Instance.CitizenUnhoused += _onCitizenUnhoused;
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Global capacity number (HappinessManager) | Per-room capacity tracking (HousingManager) | Phase 15 | Citizens have actual home rooms |
| BaseCapacity as fixed value | BaseCapacity + (segments - 1) | Phase 15 | Larger rooms hold more citizens |
| No citizen-room mapping | Dictionary-based citizen-room mapping | Phase 15 | Foundation for return-home behavior |

**Deprecated/outdated:**
- HappinessManager's `_housingCapacity` / `_housingRoomCapacities` will become redundant after Phase 16 (capacity transfer). During Phase 15, both systems track in parallel.

## Open Questions

1. **GetPlacedRoom API change scope**
   - What we know: The return type must be extended to include SegmentCount. This is a tuple change that affects all callers.
   - What's unclear: How many callers exist and whether they destructure the tuple positionally.
   - Recommendation: Grep all callers, update destructuring. Most callers only use `.Definition` or `.AnchorIndex` via `.Value.Field`, so adding a field to the tuple is backward-compatible for property-style access but breaks positional destructuring.

2. **CitizenNode HomeSegmentIndex storage**
   - What we know: SavedCitizen already has `int? HomeSegmentIndex`. CitizenNode needs a runtime field too.
   - What's unclear: Should this be on CitizenData (Resource) or CitizenNode (Node3D)?
   - Recommendation: Add to CitizenNode as a simple property (not CitizenData, which is identity/appearance only). HousingManager is the source of truth for the mapping; CitizenNode just caches it for quick access.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Godot 4 (no external test framework -- manual in-game validation) |
| Config file | None -- Godot project uses runtime testing |
| Quick run command | Launch game scene, build housing rooms, observe citizen assignments |
| Full suite command | Full playthrough: build, demolish, save/load, verify assignments persist |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| HOME-01 | Citizen arrival assigns to fewest-occupants room | manual | Build 2 housing rooms, wait for arrival, check assignment | N/A |
| HOME-02 | Demolish reassigns or unhomes citizens | manual | Demolish a housing room, verify reassignment | N/A |
| HOME-03 | New room assigns unhoused citizens oldest-first | manual | Start game (5 unhoused), build room, check oldest assigned first | N/A |
| HOME-04 | Capacity = BaseCapacity + segments - 1 | manual | Build 1-seg and 2-seg Bunk Pod, verify 2 vs 3 capacity | N/A |
| HOME-05 | Unhoused citizens function identically | manual | Verify unhoused citizens walk, visit, wish normally | N/A |
| INFR-01 | HousingManager singleton owns mapping | manual | Verify Instance non-null, assignments tracked correctly | N/A |

### Sampling Rate
- **Per task commit:** Run game scene, verify no crashes, check GD.Print output for assignments
- **Per wave merge:** Full playthrough with save/load cycle
- **Phase gate:** All 5 HOME requirements verified manually in game

### Wave 0 Gaps
None -- this is a Godot game project without automated test infrastructure. All validation is manual in-game testing. The implementation should include strategic `GD.Print` statements for assignment/displacement events to aid manual verification.

## Sources

### Primary (HIGH confidence)
- `/workspace/Scripts/Autoloads/HousingManager.cs` - Phase 14 skeleton, singleton pattern
- `/workspace/Scripts/Autoloads/GameEvents.cs` - CitizenAssignedHome/CitizenUnhoused events (lines 232-245)
- `/workspace/Scripts/Autoloads/HappinessManager.cs` - Capacity tracking pattern (lines 86-94, 365-424)
- `/workspace/Scripts/Autoloads/SaveManager.cs` - Save/load patterns, event subscriptions (lines 191-217, 276-345, 429-465)
- `/workspace/Scripts/Build/BuildManager.cs` - GetPlacedRoom API (lines 246-267), PlacedRoom record (lines 52-58)
- `/workspace/Scripts/Citizens/CitizenManager.cs` - Citizens list, SpawnCitizen/SpawnCitizenFromSave APIs
- `/workspace/Scripts/Citizens/CitizenNode.cs` - Citizen runtime structure, no home fields yet
- `/workspace/Scripts/Data/RoomDefinition.cs` - BaseCapacity field, RoomCategory enum
- `/workspace/Scripts/Data/HousingConfig.cs` - Timing constants (Phase 17 usage, not Phase 15)
- `/workspace/Resources/Rooms/bunk_pod.tres` - BaseCapacity=2, MaxSegments=2
- `/workspace/Resources/Rooms/sky_loft.tres` - BaseCapacity=4, MaxSegments=3
- `/workspace/docs/prd-housing.md` - Housing PRD with assignment algorithm and edge cases
- `/workspace/project.godot` - Autoload order confirmation (lines 18-28)

### Secondary (MEDIUM confidence)
- CONTEXT.md decisions on parallel capacity tracking, instant reassignment, batch assignment

### Tertiary (LOW confidence)
None -- all findings verified against source code.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All libraries already in use, patterns established
- Architecture: HIGH - Direct extension of existing patterns (HappinessManager capacity tracking)
- Pitfalls: HIGH - All pitfalls identified from source code analysis (BuildManager event ordering, GetPlacedRoom API gap)
- Save/Load: HIGH - SavedCitizen.HomeSegmentIndex already exists, integration points clearly identified

**Research date:** 2026-03-06
**Valid until:** 2026-04-06 (stable project, no external dependency changes expected)
