# Phase 9: Wire Work Bonus and Tech Debt Cleanup - Research

**Researched:** 2026-03-04
**Domain:** Godot 4 C# event wiring, economy integration, code cleanup
**Confidence:** HIGH

## Summary

Phase 9 closes the last unsatisfied v1 requirement (ECON-03) and resolves three info-level tech debt items identified in the v1.0 milestone audit. The core gap is a missing event subscriber: `CitizenNode` emits `CitizenEnteredRoom` / `CitizenExitedRoom` events when citizens visit rooms, and `EconomyManager` has `SetWorkingCitizenCount()` ready to receive the count, but no code bridges them. All the formula infrastructure (`_workingCitizenCount * Config.WorkBonusMultiplier` in `CalculateTickIncome()`) and HUD display infrastructure (`GetIncomeBreakdown()` returning workBonus, tooltip rendering the line) already exist.

The tech debt items are all low-complexity: SafeNode missing `base._EnterTree()` / `base._ExitTree()` calls, WishBoard missing a null guard consistent with codebase pattern, and GameEvents.CitizenDeparted being dead code. Each is a 1-5 line change.

**Primary recommendation:** Add a subscriber (most naturally in EconomyManager itself) that listens to CitizenEnteredRoom/CitizenExitedRoom, resolves the citizen's visit target to a room category via BuildManager, and maintains the working citizen count. The event signatures need enhancement to carry room category information since the current events only pass citizenName.

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| ECON-03 | Citizens assigned to work rooms generate bonus credits | EconomyManager formula exists (`_workingCitizenCount * Config.WorkBonusMultiplier`), SetWorkingCitizenCount() exists, CitizenEnteredRoom/ExitedRoom events exist. Missing: subscriber to bridge events to setter. Event signature must be enhanced to carry room category info. |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Godot 4 | 4.x | Game engine | Project engine |
| .NET / C# | 8.0 | Language runtime | Project language |

### Supporting
No new libraries needed. This phase uses only existing project infrastructure:
- `GameEvents` (signal bus)
- `EconomyManager` (economy singleton)
- `BuildManager` (room lookup API)
- `CitizenNode` (event emitter)
- `CreditHUD` (tooltip display)

**Installation:** No new packages required.

## Architecture Patterns

### Existing Project Patterns (MUST follow)

**1. Pure C# event delegates (not Godot [Signal])**
All cross-system events use `event Action<T>` with `?.Invoke()` emit helpers. Defined in `GameEvents.cs`. Project decision from Phase 1.

**2. SafeNode lifecycle pattern**
Nodes that subscribe to events extend `SafeNode` or manually implement the `_EnterTree` subscribe / `_ExitTree` unsubscribe pattern. Every `+=` has a matching `-=`.

**3. Singleton access via `.Instance`**
All Autoloads expose a static `Instance` property set in `_Ready()` or `_EnterTree()`.

**4. Null-guarded event subscription**
The established codebase pattern guards event subscriptions with `if (GameEvents.Instance != null)`. See `HappinessManager._Ready()` lines 169-174, `CreditHUD._Ready()` lines 100-111. WishBoard deviates from this pattern (tech debt item).

**5. Per-instance materials**
Every mesh gets its own `StandardMaterial3D` instance to prevent shared-material contamination (Phase 2 lesson, applied consistently since).

### Recommended Architecture for Work Bonus Wiring

The wiring must connect these existing pieces:

```
CitizenNode._visitTargetSegment → [event data] → subscriber → BuildManager.GetPlacedRoom() → category check → EconomyManager.SetWorkingCitizenCount()
```

**Key design decision: Event signature enhancement**

The current events are:
```csharp
// GameEvents.cs lines 146-164
public event Action<string> CitizenEnteredRoom;   // citizenName only
public event Action<string> CitizenExitedRoom;    // citizenName only
```

These do NOT carry segment index or room category. The subscriber needs to know which room the citizen entered to determine if it's a Work category room.

**Option A (recommended): Enhance event signatures to include segment index**
```csharp
public event Action<string, int> CitizenEnteredRoom;  // citizenName, flatSegmentIndex
public event Action<string, int> CitizenExitedRoom;   // citizenName, flatSegmentIndex
```
Then the subscriber calls `BuildManager.Instance.GetPlacedRoom(flatSegmentIndex)` to check `Definition.Category == Work`.

This is the cleanest approach because:
- `_visitTargetSegment` is already tracked in CitizenNode (line 124, set at line 397)
- The segment index is available at the exact point where emit happens (line 480, 491)
- It follows the existing pattern of RoomPlaced(string roomType, int segmentIndex)
- The subscriber does not need to maintain any citizen-to-room mapping

**Impact of signature change:** No existing code subscribes to these events (confirmed by grep), so changing the signature is fully backward-compatible. Zero breakage.

**Option B: Maintain a Dictionary<string, int> in EconomyManager**
Map citizenName -> segmentIndex on enter, look up on exit. This avoids changing the event signature but introduces coupling and state management complexity for no benefit.

**Recommendation:** Option A. Cleaner, follows existing event patterns (RoomPlaced already includes segmentIndex), and no subscribers to break.

### Where to put the subscriber

**EconomyManager is the correct location** (not CitizenManager or HappinessManager):
- EconomyManager owns `_workingCitizenCount` and `SetWorkingCitizenCount()`
- The count is an economy concern, not a citizen lifecycle concern
- HappinessManager already handles Housing room tracking via RoomPlaced/Demolished events as a separate concern
- EconomyManager already subscribes to no external events (it owns income timer internally), so adding subscriptions follows a natural growth pattern

EconomyManager currently extends `Node` directly. Since it's an Autoload (earliest initialization), it should subscribe in `_Ready()` with null guard and unsubscribe in `_ExitTree()`, matching the `HappinessManager` pattern.

### Working citizen count tracking

The subscriber needs to maintain a count that:
- Increments when a citizen enters a Work-category room
- Decrements when a citizen exits a Work-category room
- Passes the total to `SetWorkingCitizenCount()`

This is simpler than Housing capacity tracking (which needs to survive demolish events) because:
- Visits are transient (enter, wait, exit)
- The count naturally returns to 0 when all citizens exit
- No save/load needed for this count (visits don't persist across save)

```csharp
// In EconomyManager
private void OnCitizenEnteredRoom(string citizenName, int flatSegmentIndex)
{
    if (IsWorkRoom(flatSegmentIndex))
    {
        _workingCitizenCount++;
    }
}

private void OnCitizenExitedRoom(string citizenName, int flatSegmentIndex)
{
    if (IsWorkRoom(flatSegmentIndex))
    {
        _workingCitizenCount = Mathf.Max(0, _workingCitizenCount - 1);
    }
}

private bool IsWorkRoom(int flatSegmentIndex)
{
    var room = Build.BuildManager.Instance?.GetPlacedRoom(flatSegmentIndex);
    return room?.Definition.Category == Data.RoomDefinition.RoomCategory.Work;
}
```

### Anti-Patterns to Avoid
- **Polling for work count:** Do NOT scan all citizens every tick to count who is visiting. Use event-driven increment/decrement.
- **Storing category in the event:** Do NOT add room category to the event. The event should carry the segment index (data); the subscriber determines what it means (logic). This follows separation of concerns.
- **Using CitizenDeparted for cleanup:** CitizenDeparted is dead code being removed. Do not introduce new subscribers to it.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Room category lookup | Custom room-tracking dictionary | `BuildManager.Instance.GetPlacedRoom(index).Definition.Category` | BuildManager already tracks all placed rooms with their definitions. No duplication needed. |
| Citizen-to-room mapping | Dictionary<string, int> citizenRoomMap | Enhanced event signature carrying segmentIndex | Event carries the data directly; no state mapping needed. |

**Key insight:** All the infrastructure already exists. This phase is pure wiring -- connecting two working halves that were built in different phases.

## Common Pitfalls

### Pitfall 1: Event signature change breaking existing subscribers
**What goes wrong:** Changing `Action<string>` to `Action<string, int>` breaks existing subscribers.
**Why it happens:** Forgetting to check all subscribers.
**How to avoid:** Verified via grep: zero code subscribes to CitizenEnteredRoom or CitizenExitedRoom. The signature change is safe.
**Warning signs:** Compiler errors on build. Easy to catch.

### Pitfall 2: Race condition on room demolished during visit
**What goes wrong:** A citizen enters a Work room, the room is demolished while they're inside, then the citizen exits. The exit handler calls `GetPlacedRoom()` which returns null because the room no longer exists. The count never decrements.
**Why it happens:** The visit tween runs over seconds; room demolish can happen mid-visit.
**How to avoid:** The exit handler should guard against null `GetPlacedRoom()` result. If the room was demolished, skip the decrement (count was already correct because the enter/exit are symmetric -- the citizen must have entered it when it existed, so the decrement is needed). Use the segment index passed in the event, and maintain a set of "currently working" citizens to track who needs decrementing.
**Better approach:** Track which citizens are currently counted as working. On exit, check if this citizen was counted (not whether the room still exists). This is more robust.

### Pitfall 3: Forgetting to unsubscribe in _ExitTree
**What goes wrong:** EconomyManager subscribes to events in _Ready but forgets to unsubscribe in _ExitTree. On scene change (title screen to game), the old subscription persists as a dangling reference.
**Why it happens:** EconomyManager currently has no _ExitTree override.
**How to avoid:** Add `_ExitTree()` override that mirrors the _Ready subscriptions. Follow the HappinessManager pattern exactly.

### Pitfall 4: CreditHUD tooltip citizenCount bug
**What goes wrong:** The income tooltip shows "Citizens: +X (0 citizens)" because line 274 in CreditHUD.cs hardcodes `int citizenCount = 0;` instead of reading from EconomyManager or CitizenManager.
**Why it happens:** The tooltip was built in Phase 3 before CitizenManager existed (Phase 5).
**How to avoid:** Fix this while we're updating the tooltip display. Read from `CitizenManager.Instance?.CitizenCount ?? 0`.

### Pitfall 5: Save/load interaction with _workingCitizenCount
**What goes wrong:** After loading a save, `_workingCitizenCount` is 0. Citizens resume from saved positions on the walkway (not inside rooms), so no enter events fire until the next natural visit. This is actually correct behavior -- no fix needed.
**Why it happens:** Visit state is not persisted (by design -- visits are transient 4-8 second animations).
**How to avoid:** No action needed. Document this as expected behavior.

## Code Examples

### Enhanced CitizenEnteredRoom/ExitedRoom event signatures
```csharp
// GameEvents.cs -- change from Action<string> to Action<string, int>
// Source: current codebase GameEvents.cs lines 146-164

/// <param name="citizenName">Display name of the citizen entering a room.</param>
/// <param name="flatSegmentIndex">Flat segment index of the room being entered.</param>
public event Action<string, int> CitizenEnteredRoom;

/// <param name="citizenName">Display name of the citizen exiting a room.</param>
/// <param name="flatSegmentIndex">Flat segment index of the room being exited.</param>
public event Action<string, int> CitizenExitedRoom;

public void EmitCitizenEnteredRoom(string citizenName, int flatSegmentIndex)
  => CitizenEnteredRoom?.Invoke(citizenName, flatSegmentIndex);

public void EmitCitizenExitedRoom(string citizenName, int flatSegmentIndex)
  => CitizenExitedRoom?.Invoke(citizenName, flatSegmentIndex);
```

### CitizenNode emit site updates
```csharp
// CitizenNode.cs -- update emit calls to include _visitTargetSegment
// Source: current CitizenNode.cs lines 477-491

// Phase 3: Hide and emit entered event (line ~480)
GameEvents.Instance?.EmitCitizenEnteredRoom(citizenName, _visitTargetSegment);

// Phase 5: Show and emit exited event (line ~491)
GameEvents.Instance?.EmitCitizenExitedRoom(citizenName, _visitTargetSegment);
```

### EconomyManager subscription and handler
```csharp
// EconomyManager.cs -- add to _Ready() after existing initialization
// Source: follows HappinessManager._Ready() pattern (lines 168-174)

// In _Ready():
if (GameEvents.Instance != null)
{
    GameEvents.Instance.CitizenEnteredRoom += OnCitizenEnteredRoom;
    GameEvents.Instance.CitizenExitedRoom += OnCitizenExitedRoom;
}

// New _ExitTree() override:
public override void _ExitTree()
{
    if (GameEvents.Instance != null)
    {
        GameEvents.Instance.CitizenEnteredRoom -= OnCitizenEnteredRoom;
        GameEvents.Instance.CitizenExitedRoom -= OnCitizenExitedRoom;
    }
}

// Track which citizens are currently counted as working
private readonly HashSet<string> _workingCitizens = new();

private void OnCitizenEnteredRoom(string citizenName, int flatSegmentIndex)
{
    if (IsWorkRoom(flatSegmentIndex))
    {
        _workingCitizens.Add(citizenName);
        _workingCitizenCount = _workingCitizens.Count;
    }
}

private void OnCitizenExitedRoom(string citizenName, int flatSegmentIndex)
{
    if (_workingCitizens.Remove(citizenName))
    {
        _workingCitizenCount = _workingCitizens.Count;
    }
}

private bool IsWorkRoom(int flatSegmentIndex)
{
    var room = Build.BuildManager.Instance?.GetPlacedRoom(flatSegmentIndex);
    return room?.Definition.Category == Data.RoomDefinition.RoomCategory.Work;
}
```

### SafeNode base call fix
```csharp
// SafeNode.cs -- add base calls
// Source: current SafeNode.cs lines 29-37

public override void _EnterTree()
{
    base._EnterTree();
    SubscribeEvents();
}

public override void _ExitTree()
{
    UnsubscribeEvents();
    base._ExitTree();
}
```

### WishBoard null guard fix
```csharp
// WishBoard.cs -- add null guard consistent with codebase pattern
// Source: current WishBoard.cs lines 173-187

protected override void SubscribeEvents()
{
    if (GameEvents.Instance == null) return;
    GameEvents.Instance.WishGenerated += OnWishGenerated;
    GameEvents.Instance.WishFulfilled += OnWishFulfilled;
    GameEvents.Instance.RoomPlaced += OnRoomPlaced;
    GameEvents.Instance.RoomDemolished += OnRoomDemolished;
}

protected override void UnsubscribeEvents()
{
    if (GameEvents.Instance == null) return;
    GameEvents.Instance.WishGenerated -= OnWishGenerated;
    GameEvents.Instance.WishFulfilled -= OnWishFulfilled;
    GameEvents.Instance.RoomPlaced -= OnRoomPlaced;
    GameEvents.Instance.RoomDemolished -= OnRoomDemolished;
}
```

### GameEvents CitizenDeparted removal
```csharp
// GameEvents.cs -- remove these lines (140, 154-155):
// public event Action<string> CitizenDeparted;
// public void EmitCitizenDeparted(string citizenName)
//   => CitizenDeparted?.Invoke(citizenName);
```

### CreditHUD tooltip citizen count fix
```csharp
// CreditHUD.cs -- fix line 274 in OnMouseEntered()
// Source: current CreditHUD.cs line 274

// BEFORE:
int citizenCount = 0;

// AFTER:
int citizenCount = Citizens.CitizenManager.Instance?.CitizenCount ?? 0;
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Events carry only citizenName | Events carry citizenName + flatSegmentIndex | This phase | Enables room-category-aware subscribers without state mapping |

**Not deprecated -- infrastructure enhancement:**
The existing event infrastructure is sound. This phase extends it minimally to carry one additional piece of data that was available at the emit site but not transmitted.

## Open Questions

1. **Working citizen count and tooltip display precision**
   - What we know: `GetIncomeBreakdown()` returns `workBonus` as float. The tooltip shows `+{workBonus:F0}`. With `WorkBonusMultiplier = 1.25`, one working citizen produces `+1` in the display (1.25 rounds to 1).
   - What's unclear: Whether the tooltip should show the integer count of working citizens alongside the bonus amount.
   - Recommendation: Show the bonus amount only (consistent with current tooltip structure). The working citizen count is an implementation detail, not player-facing.

2. **Should _workingCitizenCount reset on scene change?**
   - What we know: EconomyManager Autoload persists across scene changes (title screen to game). The HashSet of working citizens would accumulate stale entries if citizens are cleared.
   - What's unclear: Whether ClearCitizens() in CitizenManager needs to signal a working count reset.
   - Recommendation: Clear the HashSet in EconomyManager when RestoreCredits() is called (save/load path) or add a ClearWorkingCitizens() called from the same place ClearCitizens() is called. This is a minor detail the planner should account for.

## Sources

### Primary (HIGH confidence)
- `/workspace/Scripts/Autoloads/EconomyManager.cs` - Full source reviewed, SetWorkingCitizenCount() at line 239, CalculateTickIncome() at line 107-113
- `/workspace/Scripts/Autoloads/GameEvents.cs` - Full source reviewed, CitizenEnteredRoom/ExitedRoom at lines 146-164, CitizenDeparted at lines 140/154-155
- `/workspace/Scripts/Citizens/CitizenNode.cs` - Full source reviewed, emit sites at lines 480/491, _visitTargetSegment tracking
- `/workspace/Scripts/Autoloads/WishBoard.cs` - Full source reviewed, SubscribeEvents at lines 173-187
- `/workspace/Scripts/Core/SafeNode.cs` - Full source reviewed, _EnterTree/_ExitTree at lines 29-37
- `/workspace/Scripts/UI/CreditHUD.cs` - Full source reviewed, tooltip at lines 266-293, citizenCount bug at line 274
- `/workspace/Scripts/Build/BuildManager.cs` - GetPlacedRoom() API at lines 246-267
- `/workspace/.planning/v1.0-MILESTONE-AUDIT.md` - Gap analysis, tech debt items, fix recommendations

### Secondary (MEDIUM confidence)
- Grep verification: zero subscribers to CitizenEnteredRoom/CitizenExitedRoom confirmed
- Grep verification: zero callers of SetWorkingCitizenCount confirmed
- Grep verification: zero subscribers/emitters of CitizenDeparted confirmed

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - no new libraries, all existing project code
- Architecture: HIGH - all integration points verified in source code, event flow traced end-to-end
- Pitfalls: HIGH - race conditions identified through code flow analysis, mitigations verified against existing patterns
- Tech debt items: HIGH - exact line numbers and fixes identified from source

**Research date:** 2026-03-04
**Valid until:** Indefinite (project-specific code analysis, not external dependency)
