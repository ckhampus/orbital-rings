# Architecture Patterns

**Domain:** Housing system integration into Godot 4 C# space station builder
**Researched:** 2026-03-05
**Confidence:** HIGH (based on direct codebase analysis of all 40+ source files, no external sources needed)

---

## Executive Summary

The housing system integrates into a well-established autoload singleton architecture with event-driven communication via typed C# delegates on a GameEvents signal bus. The existing codebase uses 7 autoloads in a strict initialization order; housing adds an 8th (HousingManager) that slots between HappinessManager and SaveManager. The core challenge is cleanly separating housing assignment (new concern) from housing capacity (existing concern in HappinessManager) while preserving the event-driven, decoupled communication pattern the codebase relies on.

This document maps every integration point, identifies what is new versus modified, traces the data flows, and recommends a build order that respects dependency chains.

---

## System Overview -- Current (v1.1)

```
                          GameEvents (signal bus)
                              |
        +----------+----------+----------+-----------+----------+
        |          |          |          |           |          |
  BuildManager  HappinessM   CitizenM   WishBoard   EconomyM  SaveM
  (placement)  (capacity)   (spawning)  (wishes)    (credits)  (persist)
  (demolish)   (arrivals)   (behavior)
               (mood/tier)
```

Housing capacity lives in HappinessManager as `_housingCapacity` and `_housingRoomCapacities`. Citizens have no concept of home -- they walk and visit rooms but never "live" anywhere.

## System Overview -- Target (v1.2)

```
                          GameEvents (signal bus)
                              |
        +------+------+------+------+------+------+------+
        |      |      |      |      |      |      |      |
  Build  Happi  HOUS   Citiz  Wish   Econ   Save   UI
  Mgr    Mgr    MGR    Mgr    Board  Mgr    Mgr    (info,
                (NEW)                               tooltip,
  (room  (arri- (assign (home  (no    (no    (v3    pop
  events) vals)  map)   timer) chg)   chg)   save)  display)
               (capac)
               (reassn)
```

HousingManager becomes the single owner of both capacity tracking AND citizen-to-room assignments. HappinessManager delegates capacity queries to HousingManager.

---

## New Autoload: HousingManager

### Position in Autoload Order

```
project.godot autoload order:
1. GameEvents
2. EconomyManager
3. BuildManager
4. CitizenManager
5. WishBoard
6. HappinessManager
7. HousingManager    <-- NEW
8. SaveManager       <-- was 7th, now 8th
```

**Rationale:** HousingManager needs BuildManager (to query room definitions via GetPlacedRoom), CitizenManager (to read citizen list for reassignment), and HappinessManager does not need HousingManager at init time -- it queries lazily during OnArrivalCheck. SaveManager must remain last because it reads from all other singletons during CollectGameState.

### Component Boundaries

| Component | Responsibility | Communicates With |
|-----------|---------------|-------------------|
| **HousingManager** (NEW) | Owns citizen-to-room assignment map. Handles assign/unassign/reassign logic. Tracks room capacities with size-scaled formula. | GameEvents (subscribe: RoomPlaced, RoomDemolished, CitizenArrived; emit: CitizenAssignedHome, CitizenUnhoused), BuildManager (read: GetPlacedRoom for room definitions) |
| **HousingConfig** (NEW) | Resource holding tunable timing constants for return-home behavior | CitizenNode (reads config for home timer and rest duration intervals) |
| **CitizenNode** (MODIFIED) | Gains home timer, return-home tween sequence, Zzz floater. New fields: _homeSegmentIndex, _homeTimer | GameEvents (subscribes to CitizenAssignedHome/CitizenUnhoused for self) |
| **CitizenInfoPanel** (MODIFIED) | Gains "Home: Room Name (Location)" line | HousingManager (reads assignment for display), BuildManager (reads room definition for name) |
| **SegmentInteraction** (MODIFIED) | Appends resident names to tooltip for housing rooms | HousingManager (reads residents for segment) |
| **PopulationDisplay** (MODIFIED) | Shows count/capacity format (e.g., "5/7") | HousingManager (reads capacity), GameEvents (subscribes to RoomPlaced/RoomDemolished for updates) |
| **SaveManager** (MODIFIED) | Serializes/deserializes housing assignments, save version v3 | HousingManager (reads/writes assignment state) |
| **HappinessManager** (MODIFIED) | Removes housing capacity tracking. Queries HousingManager for arrival gating. | HousingManager (reads capacity via CalculateHousingCapacity) |
| **GameEvents** (MODIFIED) | Gains CitizenAssignedHome and CitizenUnhoused events | All subscribers |

---

## Integration Points (Detailed)

### 1. GameEvents -- New Events

Two new events for housing assignment state changes:

```csharp
// ---------------------------------------------------------------------------
// Housing Events
// ---------------------------------------------------------------------------

/// <param name="citizenName">Name of the citizen assigned to a home.</param>
/// <param name="flatSegmentIndex">Flat segment index of the home room's anchor.</param>
public event Action<string, int> CitizenAssignedHome;

/// <param name="citizenName">Name of the citizen who lost their home.</param>
public event Action<string> CitizenUnhoused;

public void EmitCitizenAssignedHome(string citizenName, int flatSegmentIndex)
    => CitizenAssignedHome?.Invoke(citizenName, flatSegmentIndex);

public void EmitCitizenUnhoused(string citizenName)
    => CitizenUnhoused?.Invoke(citizenName);
```

**Why events, not direct calls:** The existing architecture is strictly event-driven. CitizenNode, CitizenInfoPanel, PopulationDisplay, and SaveManager all need to react to assignment changes without HousingManager knowing about them. This preserves the decoupled pattern.

**Why not reuse CitizenEnteredRoom/CitizenExitedRoom:** Those are transient visit events (citizen walks into a room for a few seconds). Home assignment is a persistent state change -- semantically different.

### 2. HousingManager -- Core Data Structures

```csharp
public partial class HousingManager : Node
{
    public static HousingManager Instance { get; private set; }
    public static bool StateLoaded { get; set; }

    // citizen name -> flat segment index of home room anchor
    private readonly Dictionary<string, int> _assignments = new();

    // flat segment index (anchor) -> list of citizen names assigned
    private readonly Dictionary<int, List<string>> _roomResidents = new();

    // flat segment index (anchor) -> effective capacity for that room
    private readonly Dictionary<int, int> _roomCapacities = new();

    // citizens waiting for housing, ordered oldest-first
    private readonly List<string> _unhousedCitizens = new();
}
```

**Subscribed events:**
- `RoomPlaced` -- When a Housing-category room is built, register capacity, attempt to assign unhoused citizens
- `RoomDemolished` -- When a Housing-category room is demolished, displace all residents, attempt reassignment to other rooms
- `CitizenArrived` -- When a new citizen arrives, attempt to assign them to housing

**Key public API:**

| Method | Signature | Purpose |
|--------|-----------|---------|
| `GetHomeSegment` | `int GetHomeSegment(string citizenName)` | Returns flat segment index or -1 (unhoused) |
| `GetResidents` | `IReadOnlyList<string> GetResidents(int anchorIndex)` | Returns citizens assigned to a room |
| `CalculateHousingCapacity` | `int CalculateHousingCapacity()` | Sum of all housing rooms' effective capacity |
| `RestoreAssignment` | `void RestoreAssignment(string citizenName, int anchorIndex)` | Restores from save data |
| `ClearAssignments` | `void ClearAssignments()` | Clears all assignments (before load) |
| `AssignAllUnhoused` | `void AssignAllUnhoused()` | Attempts to assign all unhoused citizens |

**Assignment algorithm (even-spread, lowest occupancy first):**

```csharp
private int FindBestRoom()
{
    int bestAnchor = -1;
    int bestOccupancy = int.MaxValue;

    foreach (var (anchor, capacity) in _roomCapacities)
    {
        int occupancy = _roomResidents.TryGetValue(anchor, out var list) ? list.Count : 0;
        if (occupancy < capacity && occupancy < bestOccupancy)
        {
            bestOccupancy = occupancy;
            bestAnchor = anchor;
        }
    }

    return bestAnchor; // -1 if no room has available capacity
}
```

Ties are broken by iteration order (Dictionary order in C# is insertion order for recent entries, effectively random). This matches the PRD's "ties broken randomly" requirement without extra logic.

**Size-scaled capacity (per PRD recommendation B):**

```csharp
// When a Housing room is placed:
int effectiveCapacity = definition.BaseCapacity + (segmentCount - 1);
```

This must be computed from BuildManager.GetPlacedRoom() which returns the RoomDefinition and segment count. The RoomDefinition.BaseCapacity is the base for 1-segment rooms. Adding (segments - 1) gives larger rooms proportionally more capacity.

### 3. HappinessManager -- Capacity Responsibility Transfer

**Fields to REMOVE from HappinessManager:**
- `_housingCapacity` (int field)
- `_housingRoomCapacities` (Dictionary<int, int>)
- `InitializeHousingCapacity()` method
- `CalculateHousingCapacity()` public property
- `GetHousingCapacity()` save API method
- Housing-specific logic in `OnRoomPlaced()` and `OnRoomDemolished()`

**What REPLACES them:**

```csharp
// BEFORE (in HappinessManager.OnArrivalCheck):
int currentPop = CitizenManager.Instance?.CitizenCount ?? 0;
if (currentPop >= _housingCapacity) return;

// AFTER:
int currentPop = CitizenManager.Instance?.CitizenCount ?? 0;
int capacity = HousingManager.Instance?.CalculateHousingCapacity() ?? 0;
if (currentPop >= capacity) return;
```

**StarterCitizenCapacity removal:** Currently `_housingCapacity` starts at 5 to account for the 5 starter citizens who spawn before any rooms exist. With HousingManager, capacity starts at 0. The 5 starters still spawn unconditionally in `CitizenManager.SpawnStarterCitizens()` (which ignores capacity). The arrival gate purely checks actual room-derived capacity, which is correct -- no new citizens arrive until the player builds housing.

**Event subscription cleanup:** HappinessManager currently subscribes to `RoomPlaced` and `RoomDemolished` purely for housing capacity tracking. If no other logic in those handlers remains after the transfer, the subscriptions can be removed entirely. Check: `OnRoomPlaced` only does housing capacity logic (confirmed by code reading). `OnRoomDemolished` only does housing capacity logic (confirmed). Both subscriptions can be fully removed.

**Save/Load impact:** `HappinessManager.RestoreState()` currently receives `housingCapacity` as a parameter. Remove this parameter. `HappinessManager.GetHousingCapacity()` was used by SaveManager -- replace with `HousingManager.Instance.CalculateHousingCapacity()` in SaveManager. `SaveData.HousingCapacity` field can be removed (HousingManager computes capacity from placed rooms, which are already saved).

### 4. CitizenNode -- Return-Home Behavior

**New fields:**

```csharp
private int _homeSegmentIndex = -1;      // flat index of home room (-1 = unhoused)
private Timer _homeTimer;                 // periodic return-home cycle
private bool _isReturningHome;            // distinguishes home visits from regular visits
```

**Behavior cycle:**

```
Home Timer fires (90-150s, configurable via HousingConfig)
    |
    v
Guards (any YES -> skip, reset timer):
  - _isVisiting? (mid-visit, don't interrupt)
  - _isReturningHome? (shouldn't happen, but guard)
  - _homeSegmentIndex == -1? (unhoused, no home to return to)
  - Has active wish with nearby matching room? (wish priority > home)
    |
    v  All guards pass
_isReturningHome = true
    |
    v  StartReturnHome():
  1. Walk angularly to home segment mid-angle
  2. Drift radially to room edge
  3. Spawn "Zzz" FloatingText at citizen position
  4. Fade out
  5. Emit CitizenEnteredRoom (for work bonus tracking consistency)
  6. Pause wish timer (_wishTimer.Stop())
  7. Wait 8-15 seconds (configurable via HousingConfig)
  8. Emit CitizenExitedRoom
  9. Fade in
  10. Drift back to walkway
  11. Resume wish timer (_wishTimer.Start())
  12. _isReturningHome = false
```

**Key design decisions:**

1. **Reuse _activeTween:** Return-home and regular visits are mutually exclusive (guards prevent overlap). Using the same `_activeTween` field is correct and matches the kill-before-create pattern.

2. **Wish priority rule:** The simplest implementation checks if the citizen has an active wish AND a matching room exists nearby. If yes, skip the home return. This avoids complex priority queuing.

3. **Zzz floater:** Use FloatingText (same as arrival text and credit text). Smaller font size (14 vs 18), lighter color (soft lavender/gray), positioned at the citizen's world position projected to screen space. Alternative: Sprite3D badge like wish badges. Recommendation: FloatingText is simpler, already proven, and matches the "Zzz is subtle" requirement.

4. **Wish timer pause during rest:** `_wishTimer.Stop()` before the rest interval, `_wishTimer.Start()` with remaining time after. Actually, since the wish timer is one-shot and re-armed on timeout, simply stopping it during rest and restarting with a fresh interval after is simpler and avoids tracking remaining time.

**Event subscriptions (new on CitizenNode):**

```csharp
protected override void SubscribeEvents()
{
    base.SubscribeEvents();  // existing WishNudgeRequested subscription
    if (GameEvents.Instance != null)
    {
        GameEvents.Instance.CitizenAssignedHome += OnCitizenAssignedHome;
        GameEvents.Instance.CitizenUnhoused += OnCitizenUnhoused;
    }
}

protected override void UnsubscribeEvents()
{
    base.UnsubscribeEvents();
    if (GameEvents.Instance != null)
    {
        GameEvents.Instance.CitizenAssignedHome -= OnCitizenAssignedHome;
        GameEvents.Instance.CitizenUnhoused -= OnCitizenUnhoused;
    }
}

private void OnCitizenAssignedHome(string citizenName, int flatSegmentIndex)
{
    if (_data.CitizenName != citizenName) return;
    _homeSegmentIndex = flatSegmentIndex;
    // Start home timer if not already running
    if (_homeTimer != null && _homeTimer.IsStopped())
    {
        _homeTimer.WaitTime = HomeTimerMin + GD.Randf() * (HomeTimerMax - HomeTimerMin);
        _homeTimer.Start();
    }
}

private void OnCitizenUnhoused(string citizenName)
{
    if (_data.CitizenName != citizenName) return;
    _homeSegmentIndex = -1;
    _homeTimer?.Stop();
}
```

**Filter pattern:** Each CitizenNode filters events by its own name. This is identical to the existing `OnWishNudgeRequested` pattern where every CitizenNode receives the event but only the named citizen acts on it. With ~50 citizens max, the string comparison overhead is negligible.

### 5. CitizenInfoPanel -- Home Display

Add a `_homeLabel` between `_nameLabel` and `_categoryLabel` in the VBoxContainer:

```csharp
// In _Ready(), after _nameLabel:
_homeLabel = new Label
{
    MouseFilter = MouseFilterEnum.Ignore
};
_homeLabel.AddThemeFontSizeOverride("font_size", 13);
vbox.AddChild(_homeLabel);

// In ShowForCitizen():
int homeSegment = HousingManager.Instance?.GetHomeSegment(citizen.Data.CitizenName) ?? -1;
if (homeSegment >= 0)
{
    var roomInfo = BuildManager.Instance?.GetPlacedRoom(homeSegment);
    string roomName = roomInfo?.Definition.RoomName ?? "Unknown";
    var (row, pos) = SegmentGrid.FromIndex(homeSegment);
    string location = $"{(row == SegmentRow.Outer ? "Outer" : "Inner")} {pos + 1}";
    _homeLabel.Text = $"Home: {roomName} ({location})";
    _homeLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.85f, 0.75f));
}
else
{
    _homeLabel.Text = "Home: \u2014"; // em dash
    _homeLabel.AddThemeColorOverride("font_color", new Color(0.65f, 0.63f, 0.60f));
}
```

### 6. SegmentInteraction -- Tooltip Resident List

**Current:** `SegmentInteraction.UpdateHover()` builds a label from `SegmentGrid.GetLabel()` and passes it to `_tooltip.Show()`.

**After:** Append resident names when hovering a housing room:

```csharp
// In UpdateHover(), after building the base label:
string label = _ringVisual.Grid.GetLabel(row, segIndex);

// Append resident info for housing rooms
var roomInfo = BuildManager.Instance?.GetPlacedRoom(flatIndex);
if (roomInfo?.Definition.Category == RoomDefinition.RoomCategory.Housing)
{
    int anchorIndex = roomInfo.Value.AnchorIndex;
    var residents = HousingManager.Instance?.GetResidents(anchorIndex);
    if (residents != null && residents.Count > 0)
    {
        label += "\n  " + string.Join(", ", residents);
    }
}

_tooltip?.Show(label, _lastMousePos);
```

### 7. PopulationDisplay -- Count/Capacity Format

**Current:** Shows just citizen count (e.g., "5").

**After:** Shows count/capacity (e.g., "5/7"):

```csharp
private void UpdateDisplay()
{
    int count = CitizenManager.Instance?.CitizenCount ?? 0;
    int capacity = HousingManager.Instance?.CalculateHousingCapacity() ?? 0;
    _countLabel.Text = $"{count}/{capacity}";
}
```

Add subscriptions to `RoomPlaced` and `RoomDemolished` events (for capacity changes) in addition to existing `CitizenArrived` subscription.

### 8. SaveManager -- Housing Persistence

**Save format version bump to 3.**

**SavedCitizen addition (recommended over separate dictionary):**

```csharp
public class SavedCitizen
{
    // ... existing fields ...
    public int? HomeSegmentIndex { get; set; }  // null = unhoused
}
```

**Why nullable int:** When deserializing a v2 save, `HomeSegmentIndex` is absent from JSON and defaults to `null` in C#. This correctly represents "unhoused" for legacy saves. Using plain `int` would default to 0, which is a valid segment index -- a subtle but critical bug.

**CollectGameState changes:**

```csharp
// In the citizen serialization loop:
HomeSegmentIndex = HousingManager.Instance?.GetHomeSegment(citizenData.CitizenName)
```

Note: `GetHomeSegment` returns -1 for unhoused. Save as -1, restore interprets -1 as unhoused.

Actually, given the nullable approach: if `GetHomeSegment` returns -1, store `null` in the save. If >= 0, store the value. This keeps the nullable semantics clean:

```csharp
int home = HousingManager.Instance?.GetHomeSegment(citizenData.CitizenName) ?? -1;
HomeSegmentIndex = home >= 0 ? home : null;
```

**ApplySceneState changes:**

```csharp
// After restoring citizens, restore housing assignments:
if (HousingManager.Instance != null)
{
    HousingManager.Instance.ClearAssignments();

    foreach (var citizen in data.Citizens)
    {
        if (citizen.HomeSegmentIndex.HasValue && citizen.HomeSegmentIndex.Value >= 0)
        {
            HousingManager.Instance.RestoreAssignment(
                citizen.Name, citizen.HomeSegmentIndex.Value);
        }
    }

    // Attempt fresh assignment for any still-unhoused citizens
    HousingManager.Instance.AssignAllUnhoused();
}
```

**Version-gated behavior:**
- v1/v2 loads: `HomeSegmentIndex` is null for all citizens. All start unhoused. `AssignAllUnhoused()` runs and assigns them based on placed rooms.
- v3 loads: Assignments restored from save data. Any gaps filled by `AssignAllUnhoused()`.

**SaveData.HousingCapacity removal:** This field was used by HappinessManager.RestoreState(). Since HousingManager now computes capacity from placed rooms (which are already saved and restored), the field is redundant. Keep it in SaveData for v2 backward compatibility (C# will silently ignore it when deserializing v3 saves that omit it, and v2 saves that include it won't cause errors). Just stop writing it in new saves.

**StateLoaded flag for HousingManager:**

```csharp
// In SaveManager.ApplyState():
HousingManager.StateLoaded = true;  // prevents _Ready from scanning rooms
```

HousingManager._Ready() uses this to skip `InitializeFromPlacedRooms()` when loading from save, since ApplySceneState will restore assignments explicitly.

**SaveManager autosave subscriptions:** Add `CitizenAssignedHome` and `CitizenUnhoused` to the debounce triggers in SaveManager:

```csharp
private Action<string, int> _onCitizenAssignedHome;
private Action<string> _onCitizenUnhoused;

_onCitizenAssignedHome = (_, _) => OnAnyStateChanged();
_onCitizenUnhoused = _ => OnAnyStateChanged();

GameEvents.Instance.CitizenAssignedHome += _onCitizenAssignedHome;
GameEvents.Instance.CitizenUnhoused += _onCitizenUnhoused;
```

---

## Data Flow

### Flow 1: New Citizen Arrives

```
HappinessManager.OnArrivalCheck()
    |
    v  pop < capacity? (queries HousingManager.CalculateHousingCapacity)
    |
CitizenManager.SpawnCitizen()
    |
    v  emits CitizenArrived(name)
GameEvents
    |
    +---> HousingManager.OnCitizenArrived(name)
    |       |
    |       v  FindBestRoom() -- room with fewest occupants + available capacity
    |       |
    |       v  _assignments[name] = anchorIndex
    |       |  _roomResidents[anchorIndex].Add(name)
    |       |
    |       v  emits CitizenAssignedHome(name, anchorIndex)
    |             |
    |             +---> CitizenNode.OnCitizenAssignedHome() -- sets _homeSegmentIndex, starts home timer
    |             +---> SaveManager -- triggers debounced autosave
    |
    +---> PopulationDisplay -- updates count
    +---> SaveManager -- triggers debounced autosave
```

### Flow 2: Housing Room Built

```
BuildManager confirms placement
    |
    v  emits RoomPlaced(roomId, anchorIndex)
GameEvents
    |
    +---> HousingManager.OnRoomPlaced(roomId, anchorIndex)
    |       |
    |       v  is Housing category? query BuildManager.GetPlacedRoom()
    |       |
    |       v  YES: compute effective capacity = BaseCapacity + (segmentCount - 1)
    |       |       _roomCapacities[anchorIndex] = effectiveCapacity
    |       |       _roomResidents[anchorIndex] = new List<string>()
    |       |
    |       v  attempt to assign unhoused citizens (oldest first from _unhousedCitizens)
    |       |  for each assigned: emit CitizenAssignedHome(name, anchorIndex)
    |       |
    +---> WishBoard.OnRoomPlaced() (existing, unchanged)
    +---> PopulationDisplay -- updates capacity display
    +---> SaveManager -- triggers debounced autosave
```

### Flow 3: Housing Room Demolished

```
BuildManager confirms demolition
    |
    v  emits RoomDemolished(anchorIndex)
GameEvents
    |
    +---> HousingManager.OnRoomDemolished(anchorIndex)
    |       |
    |       v  is this a tracked housing room? _roomCapacities.ContainsKey(anchorIndex)?
    |       |
    |       v  YES: get all residents from _roomResidents[anchorIndex]
    |       |       for each resident:
    |       |           _assignments.Remove(name)
    |       |           emit CitizenUnhoused(name) -> CitizenNode stops home timer
    |       |
    |       v  remove room tracking: _roomCapacities.Remove, _roomResidents.Remove
    |       |
    |       v  attempt reassignment of displaced citizens to other rooms with capacity
    |       |  for each successfully reassigned: emit CitizenAssignedHome(name, newAnchor)
    |       |  remaining citizens stay in _unhousedCitizens list
    |
    +---> WishBoard.OnRoomDemolished() (existing, unchanged)
    +---> PopulationDisplay -- updates capacity display
    +---> SaveManager -- triggers debounced autosave
```

### Flow 4: Citizen Returns Home

```
CitizenNode._homeTimer fires
    |
    v  Guards:
    |    _isVisiting? -> skip, reset timer
    |    _isReturningHome? -> skip (shouldn't happen)
    |    _homeSegmentIndex == -1? -> skip (unhoused)
    |    active wish + nearby matching room? -> skip (wish priority)
    |
    v  All guards pass
_isReturningHome = true
_activeTween?.Kill()
    |
    v  StartReturnHome() tween sequence:
        Phase 0: Walk to home segment angle (angular tween)
        Phase 1: Drift to room edge (radial tween)
        Phase 2: Spawn Zzz FloatingText
        Phase 3: Fade out + hide + emit CitizenEnteredRoom
        Phase 4: _wishTimer.Stop() -- pause wish generation during rest
        Phase 5: Wait 8-15 seconds
        Phase 6: Show + fade in + emit CitizenExitedRoom
        Phase 7: Drift back to walkway
        Phase 8: _wishTimer.Start() -- resume wish generation
                 _isReturningHome = false
                 _activeTween = null
                 Reset home timer with new random interval
```

### Flow 5: Save/Load Round Trip

```
SAVE (SaveManager.CollectGameState):
  for each citizen in CitizenManager.Citizens:
      savedCitizen.HomeSegmentIndex = HousingManager.GetHomeSegment(name)
                                       (-1 stored as null)
  data.Version = 3

LOAD (SaveManager.ApplyState):
  HousingManager.StateLoaded = true
  (scene transition occurs)

LOAD (SaveManager.ApplySceneState, 2 frames later):
  1. Restore rooms via BuildManager.RestorePlacedRoom()
  2. Restore citizens via CitizenManager.SpawnCitizenFromSave()
  3. Restore housing:
     a. HousingManager.ClearAssignments()
     b. HousingManager.InitializeRoomCapacities()  -- scans placed rooms
     c. For each citizen with HomeSegmentIndex:
          HousingManager.RestoreAssignment(name, segment)
     d. HousingManager.AssignAllUnhoused()  -- fills gaps
```

---

## Patterns to Follow

### Pattern 1: Autoload Singleton with Static Instance

Every autoload follows the Instance singleton pattern. HousingManager must do the same.

```csharp
public partial class HousingManager : Node
{
    public static HousingManager Instance { get; private set; }
    public static bool StateLoaded { get; set; }

    public override void _Ready()
    {
        Instance = this;
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.RoomPlaced += OnRoomPlaced;
            GameEvents.Instance.RoomDemolished += OnRoomDemolished;
            GameEvents.Instance.CitizenArrived += OnCitizenArrived;
        }
        if (!StateLoaded)
            InitializeFromPlacedRooms();
    }

    public override void _ExitTree()
    {
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.RoomPlaced -= OnRoomPlaced;
            GameEvents.Instance.RoomDemolished -= OnRoomDemolished;
            GameEvents.Instance.CitizenArrived -= OnCitizenArrived;
        }
    }
}
```

Note: Extends `Node` directly (like HappinessManager, EconomyManager, BuildManager), NOT `SafeNode`. Autoloads manage their own subscription in `_Ready`/`_ExitTree` because they have different lifecycle needs than scene nodes. SafeNode uses `_EnterTree`/`_ExitTree` symmetry designed for scene nodes that may be re-parented.

### Pattern 2: Config Resource with Fallback Loading

```csharp
[GlobalClass]
public partial class HousingConfig : Resource
{
    [ExportGroup("Return Home")]
    [Export] public float HomeTimerMin { get; set; } = 90.0f;
    [Export] public float HomeTimerMax { get; set; } = 150.0f;
    [Export] public float RestDurationMin { get; set; } = 8.0f;
    [Export] public float RestDurationMax { get; set; } = 15.0f;
}
```

Loaded with the same fallback pattern as HappinessConfig and EconomyConfig:

```csharp
if (Config == null)
    Config = ResourceLoader.Load<HousingConfig>("res://Resources/Housing/default_housing.tres");
if (Config == null)
{
    GD.PushWarning("HousingManager: No HousingConfig found. Using code defaults.");
    Config = new HousingConfig();
}
```

### Pattern 3: Event-Driven Communication via GameEvents

All cross-system notifications go through GameEvents. No direct method calls for state-change notifications between singletons.

### Pattern 4: Kill-Before-Create Tween

Return-home tween must follow the same `_activeTween?.Kill()` pattern already used for visits. Since visits and home returns are mutually exclusive (guards prevent overlap), they share the same `_activeTween` field safely.

### Pattern 5: Null-Safe Singleton Access

Always `?.` when accessing singletons:

```csharp
int capacity = HousingManager.Instance?.CalculateHousingCapacity() ?? 0;
GameEvents.Instance?.EmitCitizenAssignedHome(name, segment);
```

### Pattern 6: StateLoaded Guard for Save/Load

```csharp
public override void _Ready()
{
    Instance = this;
    // ...event subscriptions...
    if (!StateLoaded)
        InitializeFromPlacedRooms();
}
```

---

## Anti-Patterns to Avoid

### Anti-Pattern 1: Dual Ownership of Housing Capacity

**What:** Both HappinessManager and HousingManager tracking housing capacity independently.
**Why bad:** State desynchronization. Two sources of truth for the same concept. Already a risk because HappinessManager currently owns capacity.
**Instead:** HousingManager is the single owner. HappinessManager queries it. Remove ALL capacity tracking from HappinessManager.

### Anti-Pattern 2: CitizenNode Polling HousingManager in _Process

**What:** CitizenNode checking HousingManager every frame to see if its home changed.
**Why bad:** O(N) queries per frame. Wasteful when assignments change rarely (only on room build/demolish).
**Instead:** Use events. CitizenNode subscribes to CitizenAssignedHome/CitizenUnhoused and filters by name. Same pattern as WishNudgeRequested.

### Anti-Pattern 3: Storing Home Data on CitizenData Resource

**What:** Adding HomeSegmentIndex to the CitizenData Resource class.
**Why bad:** CitizenData is identity + appearance (name, body, colors). Home assignment is runtime state that changes dynamically. Mixing concerns.
**Instead:** _homeSegmentIndex lives on CitizenNode as a private field. Authoritative mapping lives in HousingManager's dictionaries.

### Anti-Pattern 4: Direct Singleton Method Calls for Notifications

**What:** `BuildManager` calling `HousingManager.Instance.OnRoomBuilt()` directly.
**Why bad:** Breaks the event-driven architecture. Creates tight coupling. Every other system uses events for cross-system communication.
**Instead:** BuildManager emits RoomPlaced/RoomDemolished events. HousingManager subscribes independently. This is how all existing singletons communicate.

### Anti-Pattern 5: Using Regular Int for HomeSegmentIndex in SaveData

**What:** `public int HomeSegmentIndex { get; set; }` in SavedCitizen.
**Why bad:** C# default for int is 0, which is a valid segment index (Outer 0). When deserializing a v2 save that lacks this field, all citizens would appear to live at Outer 0.
**Instead:** Use `int?` (nullable). Absent JSON fields deserialize to null, which correctly represents "unhoused" for legacy saves.

---

## New vs Modified Files Summary

### New Files (2)

| File | Type | Purpose | Estimated LOC |
|------|------|---------|---------------|
| `Scripts/Autoloads/HousingManager.cs` | Autoload singleton | Assignment map, capacity tracking, assign/unassign/reassign | ~180 |
| `Scripts/Data/HousingConfig.cs` | Resource | Tunable timing constants for return-home behavior | ~30 |

### Modified Files (8)

| File | Change Scope | What Changes |
|------|-------------|-------------|
| `Scripts/Autoloads/GameEvents.cs` | Small | Add 2 events + 2 emit methods (~10 LOC) |
| `Scripts/Citizens/CitizenNode.cs` | Medium | Add _homeSegmentIndex, _homeTimer, StartReturnHome(), Zzz floater, event subscriptions (~80 LOC) |
| `Scripts/UI/CitizenInfoPanel.cs` | Small | Add _homeLabel, display home room name and location (~15 LOC) |
| `Scripts/Ring/SegmentInteraction.cs` | Small | Append resident names to tooltip label (~10 LOC) |
| `Scripts/UI/PopulationDisplay.cs` | Small | Show count/capacity format, subscribe to room events (~15 LOC) |
| `Scripts/Autoloads/SaveManager.cs` | Medium | Serialize/deserialize housing assignments, version bump, new autosave triggers (~30 LOC) |
| `Scripts/Autoloads/HappinessManager.cs` | Medium | Remove capacity tracking fields/methods, query HousingManager instead (~-50 LOC net removal) |
| `project.godot` | Tiny | Add HousingManager autoload entry (~1 line) |

### Untouched Files

| File | Why Untouched |
|------|--------------|
| `BuildManager.cs` | Emits same events. HousingManager subscribes to them. No changes. |
| `EconomyManager.cs` | Housing has no economy integration beyond existing room costs. |
| `WishBoard.cs` | Wish system is orthogonal to housing. |
| `CitizenManager.cs` | Spawning and tracking unchanged. HousingManager subscribes to CitizenArrived. |
| `MoodSystem.cs` | Internal to HappinessManager. No housing dependency. |
| `RoomDefinition.cs` | BaseCapacity already exists. No changes needed. |
| `CitizenData.cs` | Identity data only. Home is runtime state. |
| `FloatingText.cs` | Reused as-is for Zzz floater. |

---

## Suggested Build Order

Phases ordered by dependency chain. Each phase produces testable, independently verifiable results.

### Phase 1: Foundation (GameEvents + HousingConfig + SaveData)

**Files:** GameEvents.cs (2 new events), HousingConfig.cs (new resource), SaveManager.cs (SavedCitizen.HomeSegmentIndex field)
**Why first:** Pure data definitions with zero behavior. Everything else depends on them.
**Verifiable:** Project compiles. HousingConfig .tres can be created in Inspector.
**Dependencies:** None.

### Phase 2: HousingManager Core

**Files:** HousingManager.cs (new autoload), project.godot (autoload entry)
**Why second:** Core business logic must exist before anything consumes it. Assignment algorithm, capacity tracking, event handling.
**Verifiable:** Build a housing room -> HousingManager logs assignment. Demolish -> logs unhousing. New citizen arrives -> gets assigned.
**Dependencies:** Phase 1 (events exist).

### Phase 3: Capacity Transfer from HappinessManager

**Files:** HappinessManager.cs (remove capacity fields/methods, query HousingManager)
**Why third:** Must happen after HousingManager exists. Removes duplicate state.
**Verifiable:** Arrival gate uses HousingManager capacity. No regression in arrival behavior. No compile errors from removed HappinessManager methods.
**Dependencies:** Phase 2 (HousingManager provides capacity API).

### Phase 4: Return-Home Behavior on CitizenNode

**Files:** CitizenNode.cs (home timer, return-home tween, Zzz floater, event subscriptions)
**Why fourth:** The largest and most visible feature. Depends on HousingManager for assignment events and home segment lookup.
**Verifiable:** Citizens visibly return to home rooms. Zzz floater appears. Wish timer pauses during rest. Unhoused citizens don't attempt home returns.
**Dependencies:** Phase 2 (assignments exist), Phase 1 (events for CitizenAssignedHome/Unhoused).

### Phase 5: UI Updates

**Files:** CitizenInfoPanel.cs, SegmentInteraction.cs (tooltip), PopulationDisplay.cs
**Why fifth:** Pure consumers of HousingManager state. No downstream impact.
**Verifiable:** Click citizen -> see home room in info panel. Hover housing room -> see resident names. Population HUD shows count/capacity.
**Dependencies:** Phase 2 (HousingManager provides query APIs).

### Phase 6: Save/Load Integration

**Files:** SaveManager.cs (serialize/deserialize assignments, version bump, autosave triggers)
**Why last:** Must serialize all housing state, which means all features must be complete. Save version bump to v3. Backward compatibility with v2 saves.
**Verifiable:** Save -> quit -> reload: housing assignments persist. Load v2 save: citizens start unhoused, get auto-assigned.
**Dependencies:** All previous phases.

### Phase Ordering Rationale

```
Phase 1 (Foundation) --> no deps
    |
    v
Phase 2 (HousingManager) --> depends on Phase 1
    |
    +---> Phase 3 (Capacity Transfer) --> depends on Phase 2
    |
    +---> Phase 4 (Return-Home) --> depends on Phase 2
    |
    +---> Phase 5 (UI) --> depends on Phase 2
    |
    v
Phase 6 (Save/Load) --> depends on all above
```

Phases 3, 4, and 5 are independent of each other after Phase 2 and could theoretically be done in any order. The recommended sequence (3 -> 4 -> 5) puts the riskiest change (capacity transfer) first to catch integration issues early, then the largest feature (return-home), then the simplest changes (UI).

---

## Scalability Considerations

| Concern | At 10 citizens | At 50 citizens | At 200 citizens |
|---------|---------------|----------------|-----------------|
| Assignment lookup | Dictionary O(1) | Dictionary O(1) | Dictionary O(1) |
| FindBestRoom (on arrival) | Scan ~2-4 rooms, instant | Scan ~5-10 rooms, instant | Scan ~10-20 rooms, instant |
| Reassignment on demolish | Displace ~2-4, reassign O(rooms) | Displace ~4-6, O(rooms) | Displace ~6-8, O(rooms) |
| Home timer instances | 10 Godot Timers | 50 Timers, negligible | 200 Timers, Godot handles fine |
| Event filtering (name check) | 10 string compares | 50 string compares | 200 string compares, ~0.01ms |
| Save file size | ~100 bytes extra | ~500 bytes extra | ~2KB extra |

No performance concerns at the expected scale of a single ring (max ~50-100 citizens).

---

## Sources

- Direct codebase analysis of all source files (HIGH confidence -- primary source)
- PRD at `docs/prd-housing.md` (HIGH confidence -- project design document)
- `project.godot` autoload configuration (HIGH confidence -- runtime config)
- `.planning/PROJECT.md` project context (HIGH confidence -- project decisions log)
- Existing v1.1 architecture patterns (7 working autoloads, typed C# events, Config resources)

---

*Architecture research for: Orbital Rings v1.2 -- Housing System Integration*
*Researched: 2026-03-05*
