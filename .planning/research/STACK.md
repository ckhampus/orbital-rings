# Stack Research

**Domain:** Cozy 3D builder/management game (Orbital Rings)
**Researched:** 2026-03-02
**Confidence:** MEDIUM-HIGH (verified against official docs, Chickensoft ecosystem, Godot 4.6 release notes)

---

## Project Context

The project (`project.godot`) already targets **Godot 4.6** with Forward Plus rendering, Jolt Physics, and the .NET SDK at `net8.0`. This stack research is calibrated to that foundation — no engine version decisions remain open.

---

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| Godot 4 | 4.6.1 (already set) | Engine, rendering, scene tree, physics, navigation | The project is already configured; 4.6 enables Jolt as default physics, improved SSR, and better C# bindings |
| C# / .NET 8 | net8.0 (already set) | All gameplay logic, systems, tooling | Enforces type safety, enables NuGet packages, enables Chickensoft toolchain; GDScript would leave the entire C# toolchain unused |
| Godot Forward Plus | (already set) | 3D renderer | Required for SSAO, SSR, volumetric fog; correct choice for a cozy soft-lit 3D space game |
| Jolt Physics | (already set) | 3D physics | Default in 4.6, higher quality and performance than GodotPhysics; relevant for any future rigidbody room debris |

**Confidence:** HIGH — sourced from `project.godot` and [Godot 4.6 release notes](https://godotengine.org/releases/4.6/).

---

### Godot 4 Nodes and Features to Use

#### Ring and Segment Geometry

| Node / Feature | Purpose | Why |
|----------------|---------|-----|
| `MeshInstance3D` + custom `ArrayMesh` | Ring geometry, segment slots, walkway | Precise control over the flat-donut geometry; CSG is editor-only prototyping, not suitable for runtime placement |
| `StaticBody3D` + `CollisionShape3D` | Per-segment collision for mouse picking | Required for ray-casting room placement clicks |
| `SubViewport` (optional) | Minimap or UI previews | If a minimap view is added later |
| `GridMap` | NOT for this game (see What NOT to Use) | Ring is polar, not Cartesian; custom polar grid is required |

**Rationale for custom polar grid over GridMap:** The ring is divided into 24 arc segments on a circle. `GridMap` is a rectilinear system; it cannot represent arc-shaped cells. The game must track segments by index (0–23), inner/outer arc, and size (1–3). A plain C# `SegmentGrid` data class mapping to instantiated `MeshInstance3D` nodes is the correct model.

**Confidence:** HIGH — GridMap docs confirm rectilinear only; ring layout is fundamentally polar.

---

#### Camera System

| Node | Purpose | Why |
|------|---------|-----|
| `Camera3D` + `SpringArm3D` (optional) | Orbiting camera with fixed tilt | `SpringArm3D` provides collision-safe arm length; parent node rotation drives orbit |
| `Tween` (code-driven) | Smooth camera transitions, zoom easing | Godot 4's `CreateTween()` is code-friendly and GC-safe when not stored in fields unnecessarily |

Pattern: A single `CameraRig` node (child of scene root, not of the ring) with:
- Horizontal rotation driven by mouse drag or keyboard
- Fixed pitch (the "diorama tilt")
- Zoom via `SpringArm3D.SpringLength` or `Camera3D.Position.Z`

**Confidence:** HIGH — standard Godot 3D camera rig pattern.

---

#### Citizen AI and Navigation

| Node / Feature | Purpose | Why |
|----------------|---------|-----|
| `NavigationRegion3D` + `NavigationMesh` | Walkway pathfinding surface | Bake once at scene start; walkway is a fixed circular corridor |
| `NavigationAgent3D` | Per-citizen path following | Standard Godot 4 agent; handles path queries and next-position updates |
| `CharacterBody3D` | Citizen movement body | `MoveAndSlide` handles step-up, avoidance integration |
| `AnimationPlayer` or `AnimationTree` | Citizen idle/walk/enter-room animations | `AnimationTree` + blend tree for directional walking; keeps animation state separate from AI state |

**Important constraint:** Disable avoidance (`NavigationAgent3D.AvoidanceEnabled = false`) on citizens unless you actually need RVO collision avoidance. With ~20 citizens on a ring walkway, avoidance adds processing overhead without meaningful benefit because movement is single-file along a corridor. Citizens can use simple sequential path queries instead.

**Confidence:** MEDIUM — NavigationAgent3D is well-documented; the avoidance performance caveat is from [Godot forum thread on large agent counts](https://godotforums.org/d/31934-how-to-handle-large-amounts-of-navigationagents-pathfinding) and official docs.

---

#### UI System

| Node | Purpose | Why |
|------|---------|-----|
| `Control` / `CanvasLayer` | HUD, wish bubbles, credit display | Standard Godot UI; `CanvasLayer` isolates UI from 3D transforms |
| `Label3D` or `BillboardSprite3D` | Speech bubble wish display above citizens | Stays in 3D space, faces camera; cleaner than converting to screen coords |
| `NinePatchRect` | Wish card / tooltip panels | Scales correctly for variable-length wish text |
| `Tween` | UI animations (slide in, bounce, fade) | Godot 4's `Tween` is the standard for juicy UI polish; no external library needed |

**Confidence:** HIGH — standard Godot UI pattern.

---

#### Data / Resource System (Godot's "ScriptableObject" equivalent)

| Feature | Purpose | Why |
|---------|---------|-----|
| `Resource` subclasses with `[Export]` | Room definitions, citizen archetypes, wish templates | Godot's `Resource` is the direct equivalent of Unity `ScriptableObject`; supports `.tres`/`.res` serialization, Inspector editing, and type safety in C# |
| `ResourceLoader.Load<T>()` | Load room/citizen data at runtime | Type-safe C# generic API; avoids string casting |

Pattern: Create `RoomDefinition : Resource`, `WishTemplate : Resource`, `CitizenArchetype : Resource` data classes. Store instances as `.tres` files in `res://data/`. Reference them from placement system and citizen spawner.

**Confidence:** HIGH — [Godot Resource docs](https://docs.godotengine.org/en/stable/tutorials/scripting/resources.html), multiple verified sources.

---

#### Save/Load System

| Approach | Why Recommended |
|----------|-----------------|
| Custom `Resource` subclass (`SaveData : Resource`) + `ResourceSaver.Save()` / `ResourceLoader.Load()` | Natively handles all Godot types (Vector3, NodePath, etc.); no manual type conversion; integrates with the same Resource system used for game data |

Avoid `JSON` for save files: Godot types (Vector3, Color) require manual conversion, adding boilerplate and error surface. Use JSON only for config files that need to be human-editable outside the engine.

**Confidence:** MEDIUM — [GDQuest save/load guide](https://www.gdquest.com/library/save_game_godot4/), community consensus from forum threads.

---

#### Lighting and Visual Atmosphere

| Feature | Settings | Why |
|---------|----------|-----|
| `WorldEnvironment` + `Environment` resource | SSAO enabled (low radius), ambient light warm tint | SSAO adds depth to rounded geometry; warm ambient avoids the flat-lit look |
| `DirectionalLight3D` | Soft shadows (shadow blur), warm color | Single sun-equivalent for the space backdrop |
| `OmniLight3D` / `SpotLight3D` | Per-room accent lighting | Room windows and interior glow for cozy warmth |
| `GPUParticles3D` | Dust motes, window glow particles | Low particle count, additive blend mode for soft ambience |

**Confidence:** MEDIUM — [Godot environment docs](https://docs.godotengine.org/en/stable/tutorials/3d/environment_and_post_processing.html), cozy game lighting patterns from community sources.

---

### C# Architecture Patterns

#### Overall Structure: Layered Node + Service Pattern

Use three layers, consistent with [Chickensoft's architecture recommendations](https://chickensoft.games/blog/game-architecture):

```
Visual Layer        — Godot Node scripts, minimal logic, drive from state
Game Logic Layer    — State machines, wish evaluators, economy calculator
Data/Service Layer  — Autoload singletons for cross-cutting concerns
```

Keep Node scripts thin. They respond to state changes, handle input, and call into pure C# classes. Pure C# classes (not inheriting Node) hold game rules, data, and calculations.

**Why this matters for Orbital Rings:** The credit economy, wish scoring, and happiness calculations are pure math with no scene tree dependency. Putting them in `Node` scripts couples testable logic to the engine and makes unit testing harder.

---

#### Signals vs. C# Events

**Use Godot signals for node-to-node communication within a scene.** They are serialized, visible in the editor, and interoperate with GDScript if ever needed.

**Use C# events (or `Action<T>`) for communication within pure C# service/logic classes** that do not inherit from `Node`. These are faster, generate no GC pressure from Godot's signal dispatch infrastructure, and are fully typed.

Pattern:
```csharp
// In a Node script — use Godot signal
[Signal]
public delegate void RoomPlacedEventHandler(int segmentIndex, RoomDefinition room);

// In a pure C# economy service — use C# event
public event Action<int> CreditsChanged;
```

**Never use string-based `Connect("signal_name", ...)` in new C# code.** Always use the generated typed delegate form.

**Confidence:** HIGH — [Godot C# signals docs](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_signals.html), Chickensoft architecture blog.

---

#### Autoloads (Singletons)

Use a minimal set of Autoloads — one per cross-cutting concern:

| Autoload | Responsibility |
|----------|---------------|
| `GameState` | Credits, happiness, citizen count; emits signals on change |
| `EventBus` | Game-wide signal relay (room placed, wish fulfilled, citizen arrived) |
| `AudioManager` | Pooled sound effects, ambient audio |

Do NOT create an Autoload per system. The "Services" pattern consolidates access: one `Services` Autoload holds typed references to each service instance.

**Why avoid many autoloads:** Each Autoload is a scene-tree node that persists forever. Testing becomes harder when global state multiplies. Centralizing into `Services.GameState`, `Services.EventBus` keeps the global surface area small.

**Confidence:** MEDIUM — [JetBrains Godot Autoload guide](https://www.jetbrains.com/guide/gamedev/tutorials/singletons-autoloads-godot-csharp/), [Godot autoloads docs](https://docs.godotengine.org/en/stable/tutorials/scripting/singletons_autoload.html), community patterns.

---

#### State Machines for Citizens

For citizen AI state (Idle → Walking → EnteringRoom → InsideRoom → LeavingRoom), use one of:

**Option A: Custom C# FSM (recommended for this scale)**
Implement a simple `enum`-based FSM in a pure C# class. At ~20 citizens with 5–6 states, this is readable and zero-dependency.

```csharp
public enum CitizenState { Idle, Walking, EnteringRoom, InsideRoom, Wishing }
```

**Option B: Chickensoft LogicBlocks**
[LogicBlocks](https://github.com/chickensoft-games/LogicBlocks) provides hierarchical, serializable state machines with auto-generated UML diagrams. Add via NuGet: `Chickensoft.LogicBlocks`. Use if citizen state becomes complex (nested states, shared transitions, serialization for save/load).

**Do NOT use Godot's `AnimationTree` state machine for game logic.** AnimationTree state machines are for animation blending, not game state. Conflating the two creates untestable, editor-entangled logic.

**Confidence:** HIGH for pattern guidance; MEDIUM for LogicBlocks (verified from [GitHub repo](https://github.com/chickensoft-games/LogicBlocks) and Chickensoft docs).

---

#### ECS: Not Recommended for This Project

Full ECS (e.g., `Friflo.Engine.ECS`) is designed for tens of thousands of entities with data-oriented performance needs. Orbital Rings has ~20 citizens, ~24 segments, and ~30 rooms at most. ECS adds architectural complexity, fights Godot's scene tree model, and provides no meaningful performance benefit at this scale.

Use Godot's native node composition: each `Citizen` is a `CharacterBody3D` with child components (`NavigationAgent3D`, `AnimationPlayer`, `Label3D` for wish bubble). The "Entity-Component" pattern in Godot means **child nodes as components**, not a separate ECS runtime.

**Confidence:** HIGH — [Godot's own anti-ECS rationale](https://godotengine.org/article/why-isnt-godot-ecs-based-game-engine/), community consensus for small-scale games.

---

### Supporting Libraries (NuGet)

| Library | NuGet Package | Purpose | When to Use |
|---------|--------------|---------|-------------|
| Chickensoft.LogicBlocks | `Chickensoft.LogicBlocks` | Hierarchical state machines | Add if citizen or room state machines grow beyond ~6 states or require save/load serialization |
| Chickensoft.AutoInject | `Chickensoft.AutoInject` | Tree-based dependency injection, auto node-binding | Add if dependency wiring in `_Ready()` becomes repetitive; replaces manual `GetNode<T>()` chains |
| Godot-Saveable (MrRobinOfficial) | `Godot.Saveable` | JSON-based save system with encryption | Alternative if Resource-based saves prove limiting; supports multiple save slots |
| Newtonsoft.Json | `Newtonsoft.Json 13.x` | JSON config/data serialization | Only for external config files; Godot 4 C# projects support NuGet packages normally |

**Confidence:** MEDIUM — Chickensoft packages verified from [official Chickensoft site](https://chickensoft.games/) and GitHub; NuGet compatibility confirmed from [Godot .NET 8 announcement](https://godotengine.org/article/godotsharp-packages-net8/).

---

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| JetBrains Rider | Primary IDE | Rider 2024.2+ bundles the Godot plugin natively; provides signal inspection, scene tree navigation, and Godot-aware debugging. Set External Editor to Rider in Godot Project Settings → Dotnet → Editor. |
| gdUnit4Net | Unit testing | `Chickensoft.GoDotTest` or `gdUnit4Net` for C# tests; gdUnit4Net v5 runs logic-only tests up to 10x faster by skipping Godot runtime when not needed. Asset Library: [gdUnit4](https://godotengine.org/asset-library/asset/4390) |
| GodotEnv | Addon management | `Chickensoft.GodotEnv` for standardized addon installation across team members; CLI-driven |
| Blender | 3D asset authoring | Export to `.glb` (GLTF binary); Godot 4 imports `.glb` natively with full material support |

**Confidence:** HIGH for Rider (official Godot docs), MEDIUM for testing tools.

---

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| Custom polar segment grid | `GridMap` | Only if level is rectilinear (dungeon crawler, city blocks); never for ring geometry |
| `Resource` subclasses for data | Plain C# POCO + JSON | If data needs to be authored outside the Godot editor (e.g., by external tools) |
| Godot `NavigationAgent3D` | Custom waypoint system | If ring walkway is purely single-path and citizens only need next-waypoint logic (simpler but less flexible for future multi-ring expansion) |
| Chickensoft LogicBlocks | Custom enum FSM | For small projects or simple state graphs; no dependency overhead |
| Forward Plus renderer | Compatibility renderer | Only for targeting low-end hardware or web export; not needed for PC itch.io |
| `CharacterBody3D` | `RigidBody3D` for citizens | Never use rigidbody for NPC-controlled characters; physics simulation fights pathfinding |
| `MeshInstance3D` + `ArrayMesh` for ring | CSG nodes | CSG is editor prototyping only — not suitable for runtime room placement and dynamic segment geometry |

---

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| `GridMap` for room placement | Rectilinear grid, cannot represent arc segments; no runtime API for custom slot logic | Custom `SegmentGrid` C# class + `MeshInstance3D` per slot |
| CSG nodes at runtime | CSG is for editor prototyping; official docs state it is not suitable for production geometry; performance degrades with complexity | `MeshInstance3D` with authored meshes from Blender |
| GDScript for gameplay logic | Project is committed to C#; mixing GDScript and C# creates a split codebase with reduced IDE support and no shared typing | C# exclusively; GDScript only as a last resort for addons that don't expose a C# API |
| String-based signal `Connect()` | Type-unsafe, refactor-hostile, no IDE support | Typed C# delegate `[Signal]` / `Connect(SignalName.X, ...)` |
| `AnimationTree` state machines for game logic | AnimationTree is for animation blending; entangles game state with editor-created assets, untestable | Pure C# FSM or LogicBlocks |
| Many Autoload singletons | Global state explosion; each Autoload is a persistent node; testing becomes painful | One `Services` Autoload holding typed service instances |
| ECS (Friflo or similar) | Adds architectural complexity that fights Godot's scene tree; no performance benefit at <100 entities | Godot node composition (child nodes as components) |
| `RigidBody3D` for citizens | Physics simulation fights `NavigationAgent3D` pathfinding; jitter and tunneling | `CharacterBody3D` with `MoveAndSlide()` |
| Web export | C# Godot 4 web export is not production-ready as of 4.6 (ongoing technical limitation) | PC-only via itch.io as already planned |

**Confidence:** HIGH for CSG/GridMap/GDScript/RigidBody/web export — verified from official Godot docs and known limitations.

---

## Version Compatibility

| Package | Compatible With | Notes |
|---------|-----------------|-------|
| `Godot.NET.Sdk/4.6.1` | `net8.0` | Already in `.csproj`; do not downgrade to `net6.0` |
| `Chickensoft.LogicBlocks` (latest) | .NET 8, Godot 4.x | Verify NuGet version against Godot 4.6 GodotSharp version before adding |
| `Chickensoft.AutoInject` (latest) | .NET 8, Godot 4.x | Source generator; requires Roslyn analyzer support (Rider handles this) |
| `gdUnit4Net` v5.x | .NET 8, Godot 4.4+ | v5 introduced smart runtime detection; use this version or newer |
| Jolt Physics | Built into 4.6 | No separate install; already enabled in `project.godot` |
| Forward Plus renderer | Built into 4.6 | Already enabled; requires Vulkan-capable GPU |

---

## Stack Patterns by Variant

**For the ring geometry system:**
- Use C# `SegmentSlot` data records (not Nodes) to represent each of the 24 segment positions
- Each slot holds: index (0–23), arc (inner/outer), occupying room reference (nullable), and a `Node3D` handle for its visual
- Instantiate `PackedScene` room prefabs into the world when a slot is filled

**For the wish system:**
- Represent wishes as `WishTemplate : Resource` data objects loaded from `res://data/wishes/`
- Citizens hold a `List<WishTemplate>` of active wishes; the wish evaluator (pure C# class) checks if a wish is satisfied by the current ring state
- Emit signals (not direct calls) when a wish is fulfilled so UI, economy, and happiness systems react independently

**For the economy:**
- Economy is pure math: credits per citizen per second, multiplied by happiness factor, minus room costs
- Implement as a pure C# `EconomyService` class with no `Node` inheritance
- `GameState` Autoload owns the credit total; `EconomyService` is called from `_Process` at a reduced tick rate (every 0.5s, not every frame)

**For the citizen behavior loop:**
- Citizens follow a simple daily schedule: Housing → WorkRoom → ComfortRoom → Walkway → Housing
- Implement as a `CitizenBehavior` pure C# class with a `CitizenState` FSM
- Citizens do not pathfind every frame; recalculate path only on state transition or when destination room is demolished

---

## Sources

- [Godot 4.6 Release Notes](https://godotengine.org/releases/4.6/) — Jolt default, Forward Plus, SSR, C# bindings — HIGH confidence
- [Godot C# signals docs](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_signals.html) — Typed signal pattern — HIGH confidence
- [Godot Resources docs](https://docs.godotengine.org/en/stable/tutorials/scripting/resources.html) — Resource as ScriptableObject — HIGH confidence
- [Godot Singletons/Autoload docs](https://docs.godotengine.org/en/stable/tutorials/scripting/singletons_autoload.html) — Autoload pattern — HIGH confidence
- [Godot NavigationAgent3D docs](https://docs.godotengine.org/en/stable/classes/class_navigationagent3d.html) — Navigation system — HIGH confidence
- [Godot CSG docs](https://docs.godotengine.org/en/stable/tutorials/3d/csg_tools.html) — CSG for prototyping only — HIGH confidence
- [Godot Why Not ECS](https://godotengine.org/article/why-isnt-godot-ecs-based-game-engine/) — Anti-ECS rationale — HIGH confidence
- [Chickensoft game architecture blog](https://chickensoft.games/blog/game-architecture) — Layered architecture, signal vs event guidance — MEDIUM confidence (could not fetch; sourced via WebSearch summaries)
- [Chickensoft LogicBlocks GitHub](https://github.com/chickensoft-games/LogicBlocks) — State machine library — MEDIUM confidence
- [Chickensoft AutoInject GitHub](https://github.com/chickensoft-games/AutoInject) — Dependency injection — MEDIUM confidence
- [gdUnit4Net GitHub](https://github.com/MikeSchulze/gdUnit4Net) — C# testing framework — MEDIUM confidence
- [Godot .NET 8 migration announcement](https://godotengine.org/article/godotsharp-packages-net8/) — NuGet/net8 compatibility — HIGH confidence
- [GDQuest save/load guide](https://www.gdquest.com/library/save_game_godot4/) — Resource-based save system — MEDIUM confidence
- [Godot forum: NavigationAgent3D large agent counts](https://godotforums.org/d/31934-how-to-handle-large-amounts-of-navigationagents-pathfinding) — Avoidance performance — LOW-MEDIUM confidence (forum source)

---

*Stack research for: Orbital Rings — cozy 3D space station builder in Godot 4 C#*
*Researched: 2026-03-02*
