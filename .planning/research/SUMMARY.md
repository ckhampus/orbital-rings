# Project Research Summary

**Project:** Orbital Rings
**Domain:** Cozy 3D space station builder / management game with citizen wish loop
**Researched:** 2026-03-02
**Confidence:** MEDIUM-HIGH

## Executive Summary

Orbital Rings is a cozy 3D builder in a largely unoccupied market position: no competitor combines named individual citizens with personal wishes, a genuinely no-punishment loop, and a soft-3D space setting. The engine foundation is already locked — Godot 4.6 C# with Forward Plus rendering and Jolt Physics — and is the right choice for this scope. The architecture is well-understood: a layered Node + service pattern with a small set of Autoload singletons, custom Resource data definitions, and an EventBus for cross-system communication. The ring geometry is the only genuinely novel engineering problem, and it has a clear solution: a custom polar-coordinate SegmentGrid (not Godot's GridMap), mathematical segment selection (not trimesh collision), and a hand-authored walkway navmesh (not auto-baked).

The core feature loop is build → wish → grow, driven by a single credit currency and a happiness score that gates both citizen arrivals and blueprint unlocks. This is a simpler and more coherent economy than most management games in this space — intentionally so, to preserve the cozy genre promise of no punishment and no stress. The risk is not complexity but permissiveness: without a fail state or resource scarcity, the positive feedback loop (citizens → credits → rooms → more citizens) can go runaway within 15-20 minutes of play. Economy balance must be treated as a first-class design concern, modeled in a spreadsheet before any credit numbers are baked into code.

The most critical pitfalls are all addressable at project setup time: C# signal lifecycle hygiene, data/logic separation (keeping parameters in Resource files, not C# constants), mathematical segment selection, and hand-authored ring navmesh. These decisions must be made in the first phase; retrofitting them is expensive. The citizen simulation at ~20 citizens is well within Godot's comfortable performance range, so ECS and other architectural complexity are unnecessary — the project's biggest risk is early architectural shortcuts, not scale.

---

## Key Findings

### Recommended Stack

The project is already configured on a solid foundation (Godot 4.6, C#/.NET 8, Forward Plus, Jolt). No engine decisions remain open. All gameplay logic should be C# exclusively — mixing GDScript creates a split codebase with no shared typing. The primary architectural pattern is a layered Node + service model: thin Node scripts drive visuals and handle input, pure C# classes hold game rules, and a minimal set of Autoload singletons manage cross-cutting state (credits, happiness, event bus). The Chickensoft ecosystem (LogicBlocks for state machines, AutoInject for dependency wiring) is available as a NuGet upgrade path but is not required for initial scope.

Custom Godot Resource subclasses with `[GlobalClass]` are the right data model for room definitions, wish templates, and citizen archetypes — the direct equivalent of Unity ScriptableObjects. This enables Inspector editing, asset referencing, and native save/load via `ResourceSaver.Save()`. JSON should be avoided for save files because Godot types require manual conversion.

**Core technologies:**
- Godot 4.6 / Forward Plus / Jolt: engine, renderer, physics — already configured, no changes needed
- C# / .NET 8: all gameplay logic, systems, data — enforces type safety, enables NuGet and Chickensoft toolchain
- Custom `SegmentGrid` C# class: polar ring layout — GridMap is rectilinear and cannot represent arc segments
- Godot `Resource` subclasses: game data (rooms, wishes, citizens) — Godot's ScriptableObject equivalent
- `GameEvents` Autoload (signal bus): cross-system decoupling — emits typed C# delegate signals, no direct system references
- `NavigationAgent3D` + hand-authored navmesh: citizen pathfinding — auto-baked navmesh fails on curved ring geometry
- `CharacterBody3D`: citizen movement — never `RigidBody3D`, which fights pathfinding
- JetBrains Rider + gdUnit4Net: IDE and testing — Rider 2024.2+ has native Godot plugin; gdUnit4Net v5 runs logic tests without Godot runtime

See `/workspace/.planning/research/STACK.md` for full stack detail.

### Expected Features

The cozy builder genre has clear table stakes: immediate visual feedback on placement, no fail state, a single soft-progression currency, visible named NPCs, satisfying audio/visual feedback, readable room diversity, and graceful unlock progression. Every one of these is already present in the design — the project is correctly scoped for the genre.

The two core differentiators are: (1) the ring as a constrained canvas (geometric scarcity creates meaningful layout decisions without punishment), and (2) individual named citizen wishes as the teaching and reward mechanism (no other space builder does individual wishes at this personal scale). These must ship in v1 or the game has no identity. The space setting in a soft-3D pastel palette is a third differentiator that is essentially free — it is an art and tone decision, not a feature to build.

**Must have (v1 table stakes and differentiators):**
- Segment grid + room placement (1-3 segment sizes) — the ring is the game
- Credit economy (single currency, passive income, partial demolish refund) — pacing without punishment
- 5 room categories with visually distinct placeholder art — legibility at a glance
- Named citizens walking the walkway and entering rooms — emotional core; static citizens kill cozy feel
- Citizen wish system with speech bubbles — the defining differentiator; must ship with v1
- Happiness tracking driving citizen arrival + blueprint unlocks — the growth half of the loop
- Satisfying placement audio/visual feedback — non-negotiable genre feel; this IS the feel
- Orbiting 3D camera with zoom — diorama fantasy requires it
- Demolish with partial refund — prevents trap states in layout experimentation

**Should have (v1.x, after core loop validated):**
- Persistent wish board / notification panel — supplements speech bubbles when citizen count exceeds ~10
- Room-entry animations + citizen reactions to wish fulfillment — deepens emotional payoff
- Random positive events (celestial, visitor, milestone) — adds variety without threat
- Day/night ambient lighting cycle — cosmetic warmth, zero mechanical weight

**Defer (v2+):**
- Multi-ring vertical expansion — architecture must support it, but do not build it for v1
- Citizen personality traits and daily routines — proves base wish loop first
- Procedural room interiors — high effort, high delight; placeholder interiors ship v1
- Citizen relationships and shared wishes — requires player attachment that develops over time

See `/workspace/.planning/research/FEATURES.md` for full feature analysis and competitor matrix.

### Architecture Approach

The architecture is a scene-composition model with three narrow Autoload singletons (`GameEvents` as pure signal bus, `EconomyManager` for credits, `HappinessManager` for happiness/milestones), a `Ring` node that is the authoritative owner of all segment state, and individual `Citizen` scenes running simple FSMs. All cross-system communication flows through `GameEvents` signals — no system directly calls another. The `Ring` node is the single source of truth for placement; UI never reaches into Ring directly. Citizen state is held in `CitizenData` Resource objects (not Node properties) so it survives scene restarts and serializes cleanly for save/load.

**Major components:**
1. `GameEvents` (Autoload) — pure signal bus; all cross-system event declarations live here; no state
2. `EconomyManager` (Autoload) — credits balance, income ticks, cost validation
3. `HappinessManager` (Autoload) — station happiness score, milestone tracking, citizen arrival trigger
4. `Ring` (Node3D scene) — owns 24-slot segment array; all placement, validation, and demolish goes through here; emits `GameEvents` signals on change
5. `SegmentGrid` (child of Ring) — visual/spatial 24-slot layout; maps world positions to segment indices via polar math, not raycast trimesh
6. `Citizen` (CharacterBody3D scene) — named NPC with FSM: Idle → WalkingToRoom → VisitingRoom → Wishing; state in `CitizenData` Resource
7. `WishBoard` (Autoload or Node) — tracks active wishes; resolves targets for citizen navigation
8. `CameraRig` (Node3D scene) — SpringArm3D at fixed tilt; input drives Y-axis orbit and zoom only; intentionally isolated from game logic
9. `BuildPanel` / `HUD` / `WishTracker` (UI, CanvasLayer) — emit `GameEvents` signals; never directly reference Ring or Citizens

**Build order dependency chain (from ARCHITECTURE.md):**
Foundation → Ring Geometry → Economy + Build Flow → Citizens → Wishes + Happiness → Polish + Loop Closure

See `/workspace/.planning/research/ARCHITECTURE.md` for full scene tree, data flow diagrams, and component boundary table.

### Critical Pitfalls

1. **Circular walkway navmesh baking produces bad paths** — Do not auto-bake NavigationMesh on ring geometry. Curved arcs tessellate poorly; agents cut corners or take the long arc. Hand-author the walkway navmesh programmatically in C# at ring creation, or use a custom sorted-waypoint arc system. Prototype both approaches before committing. The navmesh covers only the walkway corridor, not the full ring mesh — room placement does not change the walkway, so navmesh never needs runtime rebaking.

2. **C# signal connections leak memory when citizens are removed** — The `+=` delegate operator creates strong references. Lambdas as callbacks cannot be unsubscribed and are a confirmed Godot engine-level leak. Always disconnect signals in `_ExitTree()` using `-=`. Always free nodes with `QueueFree()`, never `Dispose()` alone. Establish this pattern in Phase 1 — retrofitting signal hygiene across a large codebase is expensive.

3. **No native torus collision shape in Godot** — Do not use trimesh collision on the ring mesh for segment selection. Inner-face phantom hits will break room placement. Use mathematical segment selection: project mouse ray to ring plane (Y=0), convert to polar coordinates, derive segment index from angle and radius band. Use ConcaveMeshShape3D only for the walkway surface where citizens need foot placement.

4. **C# rebuild kills iteration speed** — Every code change requires full assembly recompile and game restart; there is no hot reload. Keep all tunable parameters (economy rates, happiness thresholds, wish frequency) in `Resource` files editable in the Inspector without recompile. Establish a minimal quick-test scene (2-3 citizens, one ring) for rapid iteration. Separate citizen state into plain C# data objects so scene-tree restarts do not lose simulation state during development.

5. **Wish economy positive feedback loop can go runaway** — More citizens → more credits → more rooms → more wish fulfillment → more citizens → repeat. Without a fail state or credit sink, credit supply outpaces demand within 15-20 minutes. Apply diminishing returns to citizen arrival rate at high happiness (sigmoid curve, not linear). Cap happiness multiplier on credit income at 1.5x-2x. Model the economy in a spreadsheet before writing any credit numbers in code.

6. **GDScript-only addons block C# workflow** — ~84% of Godot asset library is GDScript. Before adopting any addon, verify C# native access or .NET NuGet equivalence. Establish a policy: if an addon cannot be used with full type safety, port or replace it. Evaluate all potential addons before writing any dependent code.

See `/workspace/.planning/research/PITFALLS.md` for full pitfall analysis, recovery strategies, and verification checklists.

---

## Implications for Roadmap

The architecture research provides a clear build-order dependency chain. These map directly to roadmap phases.

### Phase 1: Foundation and Project Setup
**Rationale:** All other systems depend on signal definitions, data schema, and camera being established first. Signal hygiene and data/logic separation are architectural decisions that are expensive to retrofit — they must be locked in before gameplay code is written. This phase has no prerequisites and carries the highest architectural leverage.
**Delivers:** Working Godot project structure, signal bus skeleton, Resource data classes, camera rig, quick-test scene, addon policy established.
**Features addressed:** Orbiting 3D camera with zoom (camera rig); segment grid data schema (RoomDefinition, RingData resources)
**Pitfalls addressed:** C# signal memory leaks (establish lifecycle pattern early); GDScript addon incompatibility (audit before first use); C# rebuild iteration speed (quick-test scene + Resource-based parameters from day one)
**Research flag:** Standard patterns — well-documented Godot architecture; no research phase needed.

### Phase 2: Ring Geometry and Room Placement
**Rationale:** The ring is the game. Every other system — citizens, economy, wishes — depends on rooms existing on the ring. Segment collision and placement interaction are the highest-risk engineering problems due to the polar geometry. This must be proven before building systems on top of it.
**Delivers:** Placeable and demolishable rooms on a 24-segment polar ring; visual segment selection; 5 distinct room category placeholder meshes; partial refund demolish.
**Features addressed:** Segment grid + room placement (1-3 segment sizes); 5 room categories with distinct art; demolish with partial refund
**Pitfalls addressed:** No torus collision shape (mathematical polar segment selection, not trimesh); ring as authoritative state owner (Ring.PlaceRoom() is the only mutation path); CSG nodes not used at runtime
**Research flag:** Needs deeper research during planning — the polar-coordinate segment selection math, RoomSlot positioning via trig, and the ring navmesh strategy (custom waypoint list vs. NavigationAgent3D) should be prototyped and verified before full implementation.

### Phase 3: Economy Foundation
**Rationale:** Economy must be designed and balanced before rooms are meaningfully interactive. Credit costs and income rates must be set before the wish loop can be tuned. The economy is pure math — no scene tree dependency — so it can be developed in parallel with or immediately after the ring geometry.
**Delivers:** EconomyManager Autoload, passive income tick, room cost deduction, credit display in HUD, economy balance spreadsheet.
**Features addressed:** Credit economy (single currency, passive income); HUD credit display
**Pitfalls addressed:** Economy runaway (spreadsheet model before code; diminishing returns baked in from the start); hard-coded constants avoided (all rates in Resource files)
**Research flag:** Standard patterns — Godot Autoload + timer-based income tick is well-documented. Economy balance is a design problem, not a research problem.

### Phase 4: Citizens and Navigation
**Rationale:** Citizens are the emotional core and the other half of the build-wish-grow loop. They depend on the ring geometry (walkway navmesh) and the camera being stable. Navigation approach must be prototyped here — the custom waypoint arc vs. NavigationAgent3D decision cannot be deferred.
**Delivers:** Named citizens walking the ring walkway; CharacterBody3D + NavigationAgent3D (or custom arc waypoint system); citizen spawning at game start; idle ambient movement; CitizenData Resource lifecycle.
**Features addressed:** Named citizens walking the walkway; ambient visual aliveness; citizens entering rooms (spatial attraction)
**Pitfalls addressed:** Circular walkway navmesh (hand-authored or custom waypoint; prototype both; navmesh covers walkway only); C# signal memory leaks (citizen _ExitTree() disconnect pattern implemented here); citizen _Process performance (batch update on timer, not every frame)
**Research flag:** Needs deeper research during planning — the walkway navmesh strategy (hand-authored NavigationMesh via NavigationServer3D vs. custom arc-distance waypoint system) is a non-standard problem with no single clear best practice. A prototype spike is warranted.

### Phase 5: Wish System and Happiness Loop
**Rationale:** The wish system is the defining differentiator and can only be built after citizens, rooms, and economy all exist. All three feed into it. This phase closes the core game loop: build → wish → grow.
**Delivers:** WishBoard Autoload; citizen wish generation and speech bubbles; wish fulfillment detection on room placement; HappinessManager; happiness-gated citizen arrival; happiness-gated blueprint unlocks (2-3 unlock moments minimum).
**Features addressed:** Citizen wish system with speech bubbles; happiness tracking; blueprint unlocks; citizen arrival growth; no-fail-state (wishes linger harmlessly)
**Pitfalls addressed:** Wish economy runaway (diminishing returns on citizen arrival rate baked in); wish paralysis UX (limit visible active wishes to 3-5; pair wish text with room type icon); wish fulfillment matching validates room TYPE and SIZE
**Research flag:** Standard patterns — WishBoard as Autoload, WishData as Resource, FSM Wishing state in Citizen are all well-documented Godot patterns. No research phase needed, but the economy balance verification (5/15/30 citizen playtests) should be a gate before this phase is considered complete.

### Phase 6: Polish and Loop Closure
**Rationale:** Once the core build-wish-grow loop is proven, this phase adds the feel layer that makes the game feel finished vs. a prototype. Audio, visual feedback, and UI polish are what separate a cozy game from a management tool.
**Delivers:** Satisfying room placement snap sound + animation; wish fulfillment celebration moment; HUD wired to all signals; save/load (RingData + CitizenData serialized via ResourceSaver); ambient sound; camera orbit float-drift fix verified.
**Features addressed:** Satisfying placement audio/visual feedback; wish fulfillment visual feedback; complete HUD; save/load
**Pitfalls addressed:** Camera float drift (verify tilt angle after 10+ full orbits); signal connections verified complete (Orphan Nodes counter = 0); wish fulfillment feedback closes the emotional loop
**Research flag:** Standard patterns — Godot Tween for UI animation, ResourceSaver for save/load, AudioManager Autoload for pooled sounds are all well-documented. No research phase needed.

### Phase Ordering Rationale

- Foundation before everything: signal definitions, Resource schema, and camera must precede any system that uses them.
- Ring before citizens: citizens need a walkway to navigate and rooms to visit.
- Economy before wishes: wishes need rooms to target; room costs need an economy to validate against.
- Citizens before wishes: wishes are generated by citizens; wish fulfillment is observed by citizens.
- Wishes before polish: no point polishing a loop that does not yet close.
- This ordering directly matches the dependency chain in ARCHITECTURE.md and the feature dependency tree in FEATURES.md. No phase introduces a dependency that its predecessor has not satisfied.

### Research Flags

Phases needing deeper research during planning:
- **Phase 2 (Ring Geometry):** Non-standard polar geometry. The mathematical segment selection, RoomSlot trigonometric positioning, and ring mesh construction should be prototyped before full implementation. Prototype: place one room in one segment, verify inner/outer selection, verify click-to-segment math.
- **Phase 4 (Citizens and Navigation):** The walkway navigation strategy is the highest-risk technical decision in the project. Hand-authored NavigationMesh via NavigationServer3D is the recommended approach but has sparse documentation for circular geometry. A prototype spike comparing custom arc-waypoint vs. NavigationAgent3D should be the first deliverable of this phase.

Phases with standard patterns (skip research-phase):
- **Phase 1 (Foundation):** Godot Autoload, Resource, signal bus, SpringArm3D camera — all well-documented with multiple verified sources.
- **Phase 3 (Economy):** Timer-based income tick on an Autoload, Inspector-editable Resource parameters — standard patterns.
- **Phase 5 (Wish System):** WishBoard Autoload, WishData Resource, FSM Wishing state — established Godot patterns.
- **Phase 6 (Polish):** Tween animations, ResourceSaver save/load, AudioManager — fully documented.

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Engine already configured in project.godot. Core technology decisions sourced directly from Godot 4.6 release notes, official C# docs, and Chickensoft. Only supporting library versions (LogicBlocks, AutoInject) are MEDIUM — verify against current NuGet versions before adding. |
| Features | MEDIUM | No direct competitor in cozy-space-station-wish-loop niche. Findings cross-validated across multiple cozy builders (Townscaper, Islanders, Fabledom, Before We Leave, Aven Colony, Spiritfarer). Table stakes are well-established; differentiator value is an informed bet, not a proven pattern. |
| Architecture | MEDIUM-HIGH | Godot 4 patterns are well-documented in official sources. Ring-specific geometry (polar SegmentGrid, circular navmesh, mathematical segment selection) is a novel application of standard patterns — confident in the approach, but prototype validation is warranted before full implementation. |
| Pitfalls | MEDIUM | Sourced from official Godot issue trackers (GitHub), forum threads, and Godot docs. C# signal leaks and navmesh limitations are confirmed engine-level issues. Economy runaway is an informed extrapolation from cozy game design theory and general game economy research — not a confirmed failure mode from a shipped Orbital Rings playtest. |

**Overall confidence:** MEDIUM-HIGH

### Gaps to Address

- **Walkway navmesh strategy:** The hand-authored NavigationMesh approach for circular geometry has limited documentation. A proof-of-concept prototype (citizens navigating a circular ring without corner-cutting or wrong-arc choices) must validate the approach in Phase 4 before committing to the full NavigationAgent3D stack.
- **Economy balance numbers:** No concrete credit rates, room costs, or happiness multiplier values have been researched — these are game design decisions, not engineering ones. A spreadsheet model must be produced before Phase 3 implementation. The shape (diminishing returns sigmoid) is decided; the parameters are not.
- **Citizen count ceiling:** Performance analysis at 20 citizens is projected from Godot forum data about scripted node overhead, not from a profiled prototype. The 20-citizen target should be profiled in Phase 4 with the actual Citizen scene before scaling assumptions are locked in.
- **Wish matching algorithm:** The mechanics of how wishes are matched to placed rooms (room type + size validation, citizen proximity, wish priority) are not fully specified. This needs design clarity before Phase 5 implementation.
- **Save/load scope for v1:** Whether v1 ships with save/load or autosave-only is not resolved. ResourceSaver supports it architecturally, but the design decision affects Phase 6 scope.

---

## Sources

### Primary (HIGH confidence)
- [Godot 4.6 Release Notes](https://godotengine.org/releases/4.6/) — Jolt default, Forward Plus, C# bindings
- [Godot C# Signals docs](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_signals.html) — Typed signal pattern, delegate lifecycle
- [Godot Resources docs](https://docs.godotengine.org/en/stable/tutorials/scripting/resources.html) — Resource as ScriptableObject equivalent
- [Godot Singletons/Autoload docs](https://docs.godotengine.org/en/stable/tutorials/scripting/singletons_autoload.html) — Autoload best practices
- [Godot NavigationAgent3D docs](https://docs.godotengine.org/en/stable/tutorials/navigation/navigation_using_navigationagents.html) — Navigation system
- [Godot CSG docs](https://docs.godotengine.org/en/stable/tutorials/3d/csg_tools.html) — CSG prototyping only
- [Godot Why Not ECS](https://godotengine.org/article/why-isnt-godot-ecs-based-game-engine/) — Anti-ECS rationale
- [Godot Autoloads vs. Regular Nodes](https://docs.godotengine.org/en/stable/tutorials/best_practices/autoloads_versus_internal_nodes.html) — Autoload scope guidance
- [Godot SpringArm3D docs](https://docs.godotengine.org/en/latest/tutorials/3d/spring_arm.html) — Camera rig pattern
- [Godot .NET 8 announcement](https://godotengine.org/article/godotsharp-packages-net8/) — NuGet compatibility confirmed
- [Townscaper on Steam](https://store.steampowered.com/app/1291340/Townscaper) — Cozy builder table stakes
- [Tiny Glade on Steam](https://store.steampowered.com/app/2198150/Tiny_Glade) — No-tutorial cozy design
- [Aven Colony on Steam](https://store.steampowered.com/app/484900/Aven_Colony/) — Space builder reference
- [Before We Leave on Steam](https://store.steampowered.com/app/1073910/Before_We_Leave/) — Space cozy builder reference
- [Designing for Coziness — Game Developer / Kitfox Games](https://www.gamedeveloper.com/design/designing-for-coziness) — Genre theory
- [Cozy Games — Lostgarden](https://lostgarden.com/2018/01/24/cozy-games/) — Foundational genre theory

### Secondary (MEDIUM confidence)
- [Chickensoft game architecture blog](https://chickensoft.games/blog/game-architecture) — Layered architecture, signal vs event
- [Chickensoft LogicBlocks GitHub](https://github.com/chickensoft-games/LogicBlocks) — State machine library
- [Chickensoft AutoInject GitHub](https://github.com/chickensoft-games/AutoInject) — Dependency injection
- [GDQuest Event Bus Singleton](https://www.gdquest.com/tutorial/godot/design-patterns/event-bus-singleton/) — EventBus pattern
- [GDQuest Finite State Machine](https://www.gdquest.com/tutorial/godot/design-patterns/finite-state-machine/) — FSM pattern
- [GDQuest save/load guide](https://www.gdquest.com/library/save_game_godot4/) — Resource-based saves
- [Godot Forum: NavigationAgent3D cuts corners #88237](https://github.com/godotengine/godot/issues/88237) — Navmesh curved geometry limitations
- [Godot GitHub: Torus collision shape #6244](https://github.com/godotengine/godot-proposals/discussions/6244) — Confirmed missing primitive
- [Godot GitHub: C# hot reload proposal #7746](https://github.com/godotengine/godot-proposals/issues/7746) — No hot reload in 4.x
- [Godot GitHub: Lambda/Callable memory leak #85112](https://github.com/godotengine/godot/issues/85112) — Confirmed signal leak
- [Godot GitHub: Dispose() memory leak #107579](https://github.com/godotengine/godot/issues/107579) — Dispose != Free
- [Godot GitHub: Scripted node _Process performance #98175](https://github.com/godotengine/godot/issues/98175) — Known perf issue with many scripted nodes
- [Fabledom reviews](https://game8.co/articles/reviews/fabledom-review) — Cozy builder happiness progression
- [Howp Townscaper Works — Game Developer](https://www.gamedeveloper.com/game-platforms/how-townscaper-works-a-story-four-games-in-the-making) — Sound design as core cozy feel

### Tertiary (LOW confidence)
- [Godot Forum: NavigationAgent3D large agent counts](https://godotforums.org/d/31934-how-to-handle-large-amounts-of-navigationagents-pathfinding) — Avoidance overhead; forum source
- [SDLC Corp: Cozy game mechanics](https://sdlccorp.com/post/balancing-game-mechanics-for-relaxation-and-engagement-in-cozy-games/) — Industry blog, unverified claims
- [Machinations.io: Game economy inflation](https://machinations.io/articles/what-is-game-economy-inflation-how-to-foresee-it-and-how-to-overcome-it-in-your-game-design) — Economy design theory; general, not Godot-specific
- [Godot 4 Recipes: Camera Gimbal](https://kidscancode.org/godot_recipes/4.x/3d/camera_gimbal/index.html) — Camera rig supplementary reference

---
*Research completed: 2026-03-02*
*Ready for roadmap: yes*
