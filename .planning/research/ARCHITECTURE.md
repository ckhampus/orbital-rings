# Architecture Research

**Domain:** Godot 4 C# cozy builder game with citizen AI
**Researched:** 2026-03-02
**Confidence:** MEDIUM-HIGH (Godot 4 patterns well-documented; ring-specific geometry is novel application of standard patterns)

---

## Standard Architecture

### System Overview

```
┌────────────────────────────────────────────────────────────────────┐
│                        AUTOLOADS (Singletons)                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│  │  GameEvents  │  │  EconomyMgr  │  │ HappinessMgr │              │
│  │ (event bus)  │  │ (credits $)  │  │  (score/flow)│              │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘              │
│         │                 │                  │                      │
├─────────┼─────────────────┼──────────────────┼──────────────────────┤
│                        GAME WORLD SCENE                             │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  GameWorld (Node3D)                                          │   │
│  │  ├── Ring (Node3D)                                           │   │
│  │  │   ├── RingGeometry (MeshInstance3D — flat donut)          │   │
│  │  │   ├── SegmentGrid (Node3D — 24 placement slots)           │   │
│  │  │   │   ├── Segment[0..11] outer (RoomSlot)                 │   │
│  │  │   │   └── Segment[0..11] inner (RoomSlot)                 │   │
│  │  │   ├── Walkway (Node3D — circular corridor)                │   │
│  │  │   │   └── NavigationRegion3D (baked navmesh)              │   │
│  │  │   └── RoomContainer (Node3D — instanced rooms)            │   │
│  │  │       └── Room[...] (instanced Room scenes)               │   │
│  │  ├── CitizenContainer (Node3D — instanced citizens)          │   │
│  │  │   └── Citizen[...] (instanced Citizen scenes)             │   │
│  │  └── WishBoard (Node3D — active wish display)                │   │
│  └──────────────────────────────────────────────────────────────┘   │
├──────────────────────────────────────────────────────────────────────┤
│                            UI LAYER                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│  │  HUD         │  │  BuildPanel  │  │  WishTracker │              │
│  │  (credits,   │  │  (room type  │  │  (active     │              │
│  │   happiness) │  │   selector)  │  │   wishes UI) │              │
│  └──────────────┘  └──────────────┘  └──────────────┘              │
├──────────────────────────────────────────────────────────────────────┤
│                       CAMERA (CameraRig)                             │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  CameraRig (Node3D — pivot at ring center)                   │   │
│  │  └── SpringArm3D (collision-safe arm at fixed tilt)          │   │
│  │      └── Camera3D                                            │   │
│  └──────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Responsibility | Typical Implementation |
|-----------|----------------|------------------------|
| `GameEvents` | Global signal bus — decouples systems | Autoload Node; emits signals; no state |
| `EconomyManager` | Credits balance, income ticks, cost validation | Autoload Node; `float Credits`; timer for passive income |
| `HappinessManager` | Station-wide happiness score, milestone tracking, citizen arrival trigger | Autoload Node; aggregates per-citizen happiness |
| `Ring` | Owns segment state: which are occupied, what room is in each | Node3D; holds `RingData` resource; 24-slot array |
| `SegmentGrid` | Visual and spatial representation of 24 placement slots; raycasts for click → slot | Child of Ring; maps world positions to slot indices |
| `RoomSlot` | A single placeable slot — knows segment index, inner/outer, occupancy | Node3D; lightweight; stores reference to placed Room |
| `Room` | Visual + data for a placed room; emits happiness/income events | Instanced scene: MeshInstance3D + CollisionShape3D + RoomLogic script |
| `Walkway` | The circular corridor — navmesh surface citizens walk on | Node3D wrapping NavigationRegion3D with baked circular navmesh |
| `Citizen` | Named NPC with state machine: Idle → Walking → Visiting → Wishing | Instanced scene: CharacterBody3D + NavigationAgent3D + CitizenLogic |
| `WishBoard` | Tracks and surfaces active wishes; provides targets for player | Autoload or singleton node; list of `WishData` resources |
| `BuildPanel` | UI for selecting room type and size before placement | Control node; communicates to Ring via GameEvents |
| `CameraRig` | Orbiting camera anchored at ring center; fixed tilt, horizontal orbit, zoom | Node3D pivot + SpringArm3D + Camera3D |

---

## Recommended Project Structure

```
res://
├── autoloads/
│   ├── GameEvents.cs          # Signal bus (all cross-system signals defined here)
│   ├── EconomyManager.cs      # Credits state + income ticks
│   ├── HappinessManager.cs    # Station happiness + milestones
│   └── WishBoard.cs           # Active wish list management
├── scenes/
│   ├── game_world/
│   │   ├── GameWorld.tscn     # Root game scene
│   │   └── GameWorld.cs
│   ├── ring/
│   │   ├── Ring.tscn
│   │   ├── Ring.cs            # Segment state, room placement logic
│   │   ├── RoomSlot.tscn
│   │   ├── RoomSlot.cs
│   │   ├── Walkway.tscn       # NavigationRegion3D + navmesh
│   │   └── Walkway.cs
│   ├── rooms/
│   │   ├── Room.tscn          # Base room scene (inherited/instanced)
│   │   ├── Room.cs            # Base room logic
│   │   ├── RoomHousing.tscn
│   │   ├── RoomCafe.tscn
│   │   └── ...                # One tscn per room type
│   ├── citizens/
│   │   ├── Citizen.tscn       # CharacterBody3D + NavigationAgent3D
│   │   └── Citizen.cs         # State machine: Idle/Walking/Visiting/Wishing
│   ├── camera/
│   │   ├── CameraRig.tscn
│   │   └── CameraRig.cs       # Orbit input, zoom, fixed tilt
│   └── ui/
│       ├── HUD.tscn
│       ├── BuildPanel.tscn
│       └── WishTracker.tscn
├── resources/
│   ├── RoomDefinition.cs      # [GlobalClass] Resource: name, cost, size, category
│   ├── WishData.cs            # [GlobalClass] Resource: citizen ref, wish type, target room
│   ├── CitizenData.cs         # [GlobalClass] Resource: name, happiness, active wishes
│   └── RingData.cs            # [GlobalClass] Resource: 24-slot occupancy array
├── data/
│   ├── rooms/
│   │   ├── room_housing_quarters.tres
│   │   ├── room_comfort_cafe.tres
│   │   └── ...                # One .tres per room type (RoomDefinition data)
│   └── wishes/
│       └── wish_catalog.tres  # WishCatalog resource with all possible wish types
└── tests/                     # GUT or similar test scenes
```

### Structure Rationale

- **autoloads/:** Only truly global systems live here — economy, happiness, event bus, wish board. Kept small. Godot official docs recommend autoloads only for "broad-scoped tasks that manage their own data."
- **scenes/:** Each gameplay concept is its own scene. Ring, rooms, citizens, camera are all independently loadable and testable.
- **resources/:** C# classes extending `Resource` with `[GlobalClass]` — the Godot equivalent of Unity ScriptableObjects. These are data containers, not logic.
- **data/:** Actual `.tres` files (resource instances) containing room definitions, wish catalogs. Editor-editable without code changes.

---

## Architectural Patterns

### Pattern 1: Event Bus (GameEvents Autoload)

**What:** A single autoload node (`GameEvents.cs`) that declares all cross-system signals. Systems emit signals on `GameEvents`; other systems subscribe to it. No direct references between distant systems.

**When to use:** Whenever two systems need to communicate but should not hold direct references to each other. Examples: citizen fulfills wish → happiness changes; room placed → economy charged; happiness threshold crossed → new citizen spawns.

**Trade-offs:** Simple and idiomatic in Godot. Risk of "signal spaghetti" if every tiny event goes through it — use only for cross-system boundaries, not intra-scene communication.

**Example:**
```csharp
// GameEvents.cs (Autoload)
public partial class GameEvents : Node
{
    public static GameEvents Instance { get; private set; }

    [Signal] public delegate void WishFulfilledEventHandler(CitizenData citizen, WishData wish);
    [Signal] public delegate void RoomPlacedEventHandler(int segmentIndex, bool isOuter, RoomDefinition def);
    [Signal] public delegate void CreditsChangedEventHandler(float newTotal);
    [Signal] public delegate void HappinessChangedEventHandler(float newScore);
    [Signal] public delegate void NewCitizenArrivedEventHandler(CitizenData citizen);

    public override void _Ready() => Instance = this;
}

// EconomyManager.cs subscribes:
GameEvents.Instance.RoomPlaced += OnRoomPlaced;

// Ring.cs emits after placing:
GameEvents.Instance.EmitSignal(GameEvents.SignalName.RoomPlaced, segmentIndex, isOuter, def);
```

### Pattern 2: Custom Resource as Data Definition (RoomDefinition, WishData)

**What:** Game data (room types, wish templates, citizen stats) is defined as C# classes extending `Resource` with `[GlobalClass]` and `[Export]` attributes. Instances are saved as `.tres` files and loaded at runtime.

**When to use:** Any data that needs to be editor-configurable, asset-referenceable, and shared between multiple systems without duplication. This is Godot's equivalent of Unity ScriptableObjects.

**Trade-offs:** Clean separation of data from logic. The resource file is the single source of truth. Editor integration is excellent. Does not support `_Ready()` — no setup logic in Resource classes.

**Example:**
```csharp
// RoomDefinition.cs
[GlobalClass]
public partial class RoomDefinition : Resource
{
    [Export] public string RoomName { get; set; } = "";
    [Export] public RoomCategory Category { get; set; }
    [Export] public float BaseCost { get; set; }
    [Export] public int MinSize { get; set; } = 1;
    [Export] public int MaxSize { get; set; } = 3;
    [Export] public float HappinessPerVisit { get; set; }
    [Export] public PackedScene RoomScene { get; set; }
}
```

### Pattern 3: Citizen State Machine (FSM on Node)

**What:** Each `Citizen` node runs a simple finite state machine with states: `Idle`, `WalkingToRoom`, `VisitingRoom`, `WalkingHome`, `Wishing`. State transitions trigger via signals or direct method calls on the citizen.

**When to use:** Any NPC whose behavior changes based on discrete conditions. Keeps NPC logic readable and testable. Avoids boolean flag soup.

**Trade-offs:** Straightforward for 4–5 states. If citizen personality and daily routines are added later (deferred per PROJECT.md), consider upgrading to a behavior tree. For the first milestone, FSM is sufficient and simpler.

**Example:**
```csharp
// Citizen.cs
public partial class Citizen : CharacterBody3D
{
    private enum CitizenState { Idle, WalkingToRoom, VisitingRoom, WalkingHome, Wishing }
    private CitizenState _state = CitizenState.Idle;

    [Export] public CitizenData Data { get; set; }
    private NavigationAgent3D _agent;

    private void TransitionTo(CitizenState newState)
    {
        _state = newState;
        // state entry logic
    }
}
```

### Pattern 4: Ring as Authoritative State Owner

**What:** The `Ring` node (or its `RingData` resource) is the single source of truth for segment occupancy. All room placement, removal, and validation goes through `Ring`. No other system directly mutates segment state.

**When to use:** Always, for the ring. The ring layout is the core game state. Centralizing mutations prevents bugs where the UI, economy, and room nodes disagree about what's placed where.

**Trade-offs:** Ring becomes a coordination point. Acceptable — it has clear boundaries. If multi-ring expansion lands later, `Ring` stays scoped; a new `Station` node manages the collection of rings.

**Example:**
```csharp
// Ring.cs
public partial class Ring : Node3D
{
    private RoomDefinition[] _outerSlots = new RoomDefinition[12];
    private RoomDefinition[] _innerSlots = new RoomDefinition[12];

    public bool CanPlace(int segmentIndex, bool isOuter, int size)
    {
        // Check slots [segmentIndex .. segmentIndex+size-1] are empty
    }

    public void PlaceRoom(int segmentIndex, bool isOuter, int size, RoomDefinition def)
    {
        // Validate, fill slots, spawn Room scene, emit GameEvents.RoomPlaced
    }

    public void DemolishRoom(int segmentIndex, bool isOuter)
    {
        // Clear slots, remove Room node, emit GameEvents.RoomDemolished
    }
}
```

---

## Data Flow

### Room Placement Flow

```
Player clicks segment in world
    ↓
SegmentGrid.cs (raycast → segment index)
    ↓
Ring.CanPlace(index, isOuter, selectedSize)
    ↓ [valid]
EconomyManager.TrySpend(def.BaseCost * sizeMultiplier)
    ↓ [sufficient credits]
Ring.PlaceRoom(index, isOuter, size, def)
    ↓
GameEvents.EmitSignal(RoomPlaced)
    ↓                          ↓
EconomyManager              WishBoard
(credits deducted)     (check if wish fulfilled)
                             ↓ [wish matched]
                       GameEvents.EmitSignal(WishFulfilled, citizen, wish)
                             ↓
                       HappinessManager
                       (increase happiness, check milestones)
```

### Citizen Tick Flow (per frame / physics process)

```
Citizen._PhysicsProcess(delta)
    ↓
CitizenFSM: current state
    │
    ├── Idle: wait timer → pick target room from WishBoard or random comfort room
    │         → TransitionTo(WalkingToRoom)
    │
    ├── WalkingToRoom: NavigationAgent3D.GetNextPathPosition()
    │                  → move CharacterBody3D toward target
    │                  → on arrival: TransitionTo(VisitingRoom)
    │
    ├── VisitingRoom: wait timer → Room.OnCitizenVisit(this)
    │                → HappinessManager.AddCitizenHappiness(citizenData, amount)
    │                → TransitionTo(Wishing or WalkingHome)
    │
    └── Wishing: WishBoard.AddWish(citizenData, wishType)
                 → emit speech bubble signal on Citizen
                 → TransitionTo(Idle)
```

### Happiness / Economy Passive Flow

```
EconomyManager._Process(delta)
    ↓ (every income tick, e.g. 5 seconds)
income = baseCitizenIncome * citizenCount * happinessMultiplier
    ↓
EconomyManager.Credits += income
    ↓
GameEvents.EmitSignal(CreditsChanged, Credits)
    ↓
HUD.UpdateCreditsDisplay()

HappinessManager watches:
    CitizenData.Happiness per citizen → aggregates → StationHappiness
    StationHappiness crosses milestone threshold
        → GameEvents.EmitSignal(HappinessMilestone, threshold)
        → Spawn new citizen OR unlock new RoomDefinition
```

### State Management Summary

```
Permanent game state lives in:
  EconomyManager.Credits (Autoload, in-memory, serialized on save)
  HappinessManager.StationHappiness (Autoload, derived from citizens)
  Ring._outerSlots / _innerSlots (Ring node, serialized on save)
  CitizenData resources (one per citizen, serialized on save)

Transient state lives in:
  Citizen FSM states (re-derived on load from CitizenData)
  NavigationAgent3D paths (re-computed on demand)
  UI panel visibility (never saved)
```

---

## Component Boundaries

| Boundary | Communication Method | Notes |
|----------|---------------------|-------|
| Ring ↔ EconomyManager | `GameEvents.RoomPlaced` signal | Ring does not call EconomyManager directly |
| Ring ↔ HappinessManager | `GameEvents.WishFulfilled` signal | Decoupled; happiness responds to events |
| Citizen ↔ WishBoard | Direct call: `WishBoard.Instance.AddWish()` | Acceptable direct ref — WishBoard is an Autoload |
| Citizen ↔ Room | Direct call: `room.OnCitizenVisit(this)` | Room is the target; citizen has reference when navigating to it |
| BuildPanel (UI) ↔ Ring | `GameEvents.PlacementRequested` signal | UI never directly calls Ring; events decouple UI from game logic |
| HappinessManager ↔ Citizen | Citizen emits `HappinessChanged` on CitizenData; HappinessMgr watches all citizens | Observer pattern on per-citizen data |
| CameraRig ↔ everything | None — camera reads only input; nothing reads from camera | Intentionally isolated |

---

## Godot Scene Tree Recommendations

### Root Scene Structure

The main scene (`GameWorld.tscn`) should be kept shallow. Use composition via instanced sub-scenes rather than deep node trees inline.

```
GameWorld (Node3D)
├── Ring (instance of Ring.tscn)
├── CitizenContainer (Node3D — parent for all citizen instances)
├── CameraRig (instance of CameraRig.tscn)
└── UI (CanvasLayer)
    ├── HUD (instance of HUD.tscn)
    ├── BuildPanel (instance of BuildPanel.tscn)
    └── WishTracker (instance of WishTracker.tscn)
```

**Why CanvasLayer for UI:** All UI nodes under a `CanvasLayer` render on top of 3D content and are unaffected by the 3D camera. This is the correct Godot pattern for game HUDs.

### Ring Scene Internal Structure

The Ring scene handles geometry, slots, and rooms. The walkway navmesh is a child of Ring so it can be baked relative to the ring's transform.

```
Ring (Node3D — Ring.cs)
├── RingGeometry (MeshInstance3D — TorusMesh or procedural flat donut)
│   └── StaticBody3D + CollisionShape3D (click detection on ring surface)
├── Walkway (Node3D — Walkway.cs)
│   └── NavigationRegion3D
│       └── NavigationMesh (baked circular corridor)
├── SegmentGrid (Node3D — SegmentGrid.cs)
│   ├── OuterSlot_0 (RoomSlot.tscn) ... OuterSlot_11
│   └── InnerSlot_0 (RoomSlot.tscn) ... InnerSlot_11
└── RoomContainer (Node3D — parent for dynamically spawned rooms)
```

**RoomSlot positioning:** Slots are positioned procedurally at `_Ready()` using trig: `angle = (segmentIndex / 12.0f) * 2 * Mathf.Pi`. Each slot knows its world position for both room spawning and citizen navigation targets.

### Citizen Scene Structure

```
Citizen (CharacterBody3D — Citizen.cs)
├── MeshInstance3D (citizen visual)
├── CollisionShape3D (physics capsule)
├── NavigationAgent3D (pathfinding)
└── SpeechBubble (Node3D — parented above head)
    └── BillboardSprite3D or SubViewport label
```

**NavigationAgent3D usage:** `GetNextPathPosition()` called each physics frame; citizen moves toward it. When `IsNavigationFinished()` returns true, state transitions. Navigation target is set to the `RoomSlot.GlobalPosition` of the destination room.

### Camera Scene Structure

```
CameraRig (Node3D — CameraRig.cs, positioned at ring center 0,0,0)
└── SpringArm3D (length = zoom distance, fixed tilt via local rotation)
    └── Camera3D
```

**Fixed tilt:** The `SpringArm3D` rotation x-axis is set at scene creation to 30–45 degrees and never modified by input. Input only rotates the parent `CameraRig` around the Y axis (horizontal orbit) and adjusts `SpringArm3D.SpringLength` (zoom). This preserves the "diorama" feel.

---

## Scaling Considerations

This is a single-player PC game; "scaling" means complexity growth as features are added, not server load.

| Scale | Architecture Adjustments |
|-------|--------------------------|
| 1 ring, 5-10 citizens (first milestone) | Current architecture — no changes needed |
| 3-5 rings, 20-30 citizens | Add `Station` node wrapping array of `Ring`; HappinessManager aggregates across rings; navmesh spans connected rings |
| Full game with personalities, events | Citizen FSM upgrades to behavior tree (BehaviourToolkit asset or Chickensoft); `WishBoard` gains priority/weight system; `EventSystem` Autoload handles random events |

### Scaling Priorities

1. **First bottleneck — navmesh rebaking:** When rooms are placed or demolished, NavigationRegion3D must rebake. For the circular walkway, rooms don't block the walkway, so rebaking is rarely needed. Citizens navigate the walkway perimeter; rooms are visited by simply walking to their slot position. If rooms do affect navigation, use `NavigationServer3D.BakeFromSourceGeometryDataAsync()` to rebake off-thread.
2. **Second bottleneck — citizen _PhysicsProcess count:** 20-30 citizens each running `_PhysicsProcess` is fine. If citizens scale to 50+, consider an ECS-style update manager that batches citizen ticks rather than each citizen self-updating.

---

## Anti-Patterns

### Anti-Pattern 1: Everything in One Scene Inline

**What people do:** Build the entire Ring, all slots, all rooms, and all citizens as one massive `.tscn` file.
**Why it's wrong:** Impossible to work on in teams, slow to load in editor, can't test Ring in isolation, merge conflicts on every change.
**Do this instead:** Each logical concept is its own scene (`Ring.tscn`, `Room.tscn`, `Citizen.tscn`). The game world composes them via `PackedScene.Instantiate()`.

### Anti-Pattern 2: Bypassing GameEvents for Cross-System Calls

**What people do:** `EconomyManager.Instance.Spend(cost)` called directly from inside `Ring.PlaceRoom()`.
**Why it's wrong:** Ring now has a hard dependency on EconomyManager. Testing Ring requires EconomyManager to exist. Adding a new system that cares about room placement requires modifying Ring.
**Do this instead:** Ring emits `GameEvents.RoomPlaced`; EconomyManager and WishBoard both subscribe. Ring has zero knowledge of downstream consumers.

### Anti-Pattern 3: Storing Game State in Node Properties Without a Resource

**What people do:** `Ring._slots` is a C# array on the Ring node — fine at runtime, but lost on scene reload and can't be saved.
**Why it's wrong:** Save/load requires serializing the array manually. Editor inspection is limited. Data is coupled to the node's lifecycle.
**Do this instead:** Wrap ring state in a `RingData` Resource. Ring node holds a reference to it. Serializing `RingData` to `.tres` gives a complete save file for the ring layout.

### Anti-Pattern 4: Baking a Full NavigationMesh Over the Entire Ring Geometry

**What people do:** Drop a `NavigationRegion3D` covering the full ring mesh and bake it. Rooms end up as obstacles.
**Why it's wrong:** Room placement and removal triggers expensive rebakes. The walkway is a fixed circular corridor — citizens don't navigate through rooms, they walk to room door positions on the walkway.
**Do this instead:** The NavigationMesh covers only the walkway (the circular corridor surface). Citizens navigate to the closest walkway point adjacent to a room slot, not into the room itself. Room visits are simulated (animation, timer) not physically navigated. This makes the navmesh static and eliminates rebaking entirely.

### Anti-Pattern 5: Overloading Autoloads

**What people do:** Put game logic, UI state, and data all in one `GameManager` autoload.
**Why it's wrong:** Becomes a god object. Untestable. Every system depends on it. Changes break unrelated systems.
**Do this instead:** Three narrow autoloads with clear scopes: `GameEvents` (signals only, no state), `EconomyManager` (credits only), `HappinessManager` (happiness only). UI state lives in UI nodes. Ring state lives in Ring. Autoloads hold only data with system-wide scope.

---

## Build Order (Dependencies Between Components)

```
Phase 1 — Foundation (no dependencies)
├── GameEvents autoload (signal definitions only)
├── RoomDefinition resource class
├── RingData resource class
└── CameraRig scene (no game dependencies)

Phase 2 — Ring Geometry (depends on: Foundation)
├── Ring scene + SegmentGrid + RoomSlots
├── Flat donut mesh (TorusMesh or procedural)
└── Walkway scene with NavigationRegion3D

Phase 3 — Economy + Build Flow (depends on: Ring, GameEvents)
├── EconomyManager autoload
├── BuildPanel UI
└── Room placement + demolish via Ring.PlaceRoom()

Phase 4 — Citizens (depends on: Ring Geometry, Walkway navmesh)
├── Citizen scene with NavigationAgent3D
├── CitizenData resource class
├── Citizen FSM (Idle → Walking → Visiting)
└── CitizenContainer spawn logic in GameWorld

Phase 5 — Wishes + Happiness (depends on: Citizens, Economy, Rooms)
├── WishData resource class
├── WishBoard autoload
├── HappinessManager autoload
├── WishTracker UI
└── Citizen Wishing state + speech bubbles

Phase 6 — Polish + Loop Closure (depends on: all above)
├── New citizen arrival at happiness milestones
├── Blueprint unlocks at happiness milestones
├── HUD wired to EconomyManager + HappinessManager signals
└── Save/load (serialize RingData + CitizenData resources)
```

**Key dependency insight:** The NavigationMesh walkway (Phase 2) must exist before citizens can navigate (Phase 4). Room placement mechanics (Phase 3) must exist before the wish fulfillment check in Phase 5 can work. Citizens must exist before happiness aggregation in Phase 5 is meaningful.

---

## Integration Points

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| Ring ↔ EconomyManager | `GameEvents` signals | Ring never directly references EconomyManager |
| Ring ↔ HappinessManager | `GameEvents` signals | Ring never directly references HappinessManager |
| Citizen ↔ WishBoard | Direct: `WishBoard.Instance.AddWish()` | WishBoard is autoload; acceptable direct call |
| Citizen ↔ Room | Direct: room reference during navigation | Citizen gets room reference from WishBoard target resolution |
| HappinessManager ↔ Citizen | Signal on CitizenData resource | `CitizenData` emits `HappinessChanged`; manager subscribes at citizen spawn |
| BuildPanel (UI) ↔ Ring | `GameEvents.PlacementRequested` | UI emits; Ring listens; UI never reaches into Ring directly |
| CameraRig ↔ rest | None (input only) | Camera is intentionally isolated from game logic |

---

## Sources

- [Godot 4 Scene Organization Best Practices](https://docs.godotengine.org/en/stable/tutorials/best_practices/scene_organization.html) — MEDIUM confidence (official docs, fetched indirectly via search)
- [Autoloads vs. Regular Nodes — Godot 4.x](https://docs.godotengine.org/en/stable/tutorials/best_practices/autoloads_versus_internal_nodes.html) — HIGH confidence (official Godot documentation, multiple versions listed)
- [Singletons (Autoload) — Godot 4.x](https://docs.godotengine.org/en/stable/tutorials/scripting/singletons_autoload.html) — HIGH confidence (official Godot documentation)
- [Event Bus Singleton — GDQuest](https://www.gdquest.com/tutorial/godot/design-patterns/event-bus-singleton/) — MEDIUM confidence (authoritative community source, GDQuest)
- [Using NavigationAgents — Godot 4.x](https://docs.godotengine.org/en/stable/tutorials/navigation/navigation_using_navigationagents.html) — HIGH confidence (official Godot documentation)
- [Resources — Godot Engine](https://docs.godotengine.org/en/stable/tutorials/scripting/resources.html) — HIGH confidence (official Godot documentation)
- [Chickensoft: Enjoyable Game Architecture with Godot & C#](https://talks.godotengine.org/godotcon-us-2025/talk/MPC3BC/) — MEDIUM confidence (Chickensoft is authoritative C# Godot tooling source, GodotCon 2025)
- [Finite State Machine in Godot 4 — GDQuest](https://www.gdquest.com/tutorial/godot/design-patterns/finite-state-machine/) — MEDIUM confidence (GDQuest, authoritative community source)
- [Making a basic FSM in Godot 4/C#](https://medium.com/codex/making-a-basic-finite-state-machine-godot4-c-fe5ccc0e8cd7) — LOW confidence (single Medium article, unverified)
- [Custom Resources in C# — Godot Forum](https://forum.godotengine.org/t/custom-resources-in-c/55910) — LOW confidence (community forum, supplementary only)
- [Third-person camera with SpringArm — Godot docs](https://docs.godotengine.org/en/latest/tutorials/3d/spring_arm.html) — HIGH confidence (official Godot documentation)

---
*Architecture research for: Godot 4 C# cozy space station builder (Orbital Rings)*
*Researched: 2026-03-02*
