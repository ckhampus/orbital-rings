# Pitfalls Research

**Domain:** Cozy space station builder game — Godot 4 C#, circular ring geometry, citizen simulation, wish-driven economy
**Researched:** 2026-03-02
**Confidence:** MEDIUM (WebSearch + official Godot docs + community issue trackers; no Context7 library coverage for Godot C# specifics)

---

## Critical Pitfalls

### Pitfall 1: Treating the Circular Walkway Like a Grid

**What goes wrong:**
The walkway is a closed circular arc, not a straight path. Developers instinctively reach for Godot's NavigationRegion3D + auto-baked NavigationMesh, which works well for rectilinear geometry. On a curved ring, the auto-bake produces polygon approximations of the arc with subtle gaps, seam artifacts at the 12 o'clock segment join, and agents that cut corners across the arc instead of following the curve. The result: citizens walk through walls, skip segments, or take the "long way around" because the navmesh accidentally represents a shorter straight-line path across the open center.

**Why it happens:**
The NavigationMesh baking algorithm tessellates the source mesh into triangles and computes walkable polys. Curved geometry — especially thin walkway rings — is approximated, and the tessellation resolution may be too coarse relative to the arc's curvature. Additionally, the ring is a closed loop: an agent navigating from segment 11 to segment 1 has two valid routes of equal or nearly-equal cost. The navmesh cannot inherently express "go clockwise" vs "go counterclockwise", so agents may choose the long path.

**How to avoid:**
Do not rely on auto-baked navmesh for the ring walkway. Instead, hand-author the NavigationMesh by constructing it programmatically in C# at ring creation time: generate the polygon vertices for the walkway arc explicitly, respecting the discrete 12-segment layout. Register this mesh with NavigationServer3D directly. Because the walkway is a fixed-topology discrete structure (not a dynamic terrain), a statically authored navmesh per ring is vastly more reliable than runtime baking. If citizens only ever move along the walkway (not through room interiors), a custom path system — a sorted list of waypoint positions around the ring, with arc-distance-based pathfinding — is simpler and more controllable than the full NavigationAgent3D stack.

**Warning signs:**
- Citizens visually cutting across the ring's empty interior
- Citizens getting stuck at segment boundaries (particularly segment 12 → segment 1)
- Citizens choosing the long arc (180+ degrees) when the short arc is obvious
- NavigationAgent3D "path_desired_distance" and "target_desired_distance" tuning needed to prevent jitter at waypoints

**Phase to address:**
Phase covering citizen movement / walkway implementation — prototype both approaches (custom waypoint list vs. NavigationAgent3D) before committing to the full nav stack.

---

### Pitfall 2: C# Signal Connection Memory Leaks

**What goes wrong:**
In Godot 4 C#, connecting signals using the `+=` delegate operator (e.g., `citizen.WishFulfilled += OnWishFulfilled`) creates a strong reference from the signal source to the subscriber. If the subscriber is freed or removed from the scene without first disconnecting the signal (`-=`), the GC cannot collect it because Godot's unmanaged side still holds the reference. With many citizens coming and going, leaked connections accumulate. Symptoms appear as slow memory growth and stale callbacks firing after nodes are supposedly dead.

A related bug: using lambdas as signal callbacks creates anonymous delegate instances that cannot be unsubscribed, causing permanent leaks. As of Godot 4.2+, lambda/callable memory leaks are a confirmed engine-level issue in certain versions.

**Why it happens:**
Godot 4 C# uses a dual-ownership model: Godot's unmanaged engine holds the canonical GodotObject lifetime, and .NET's GC manages the managed wrapper. The .NET GC cannot collect a wrapper if the engine still references it (e.g., via a connected signal). Calling `Dispose()` on a GodotObject only releases the managed handle — it does NOT `Free()` the engine object. This is a documented pitfall: Dispose() ≠ Free() in Godot C#. Citizens that are "removed" by calling Dispose() instead of QueueFree()/Free() leave orphaned engine objects that continue to fire signals.

**How to avoid:**
- Always disconnect signals in `_ExitTree()` using the `-=` operator, or use `Connect()`/`Disconnect()` with named methods that can be tracked.
- Never use anonymous lambdas as long-lived signal callbacks; always use named instance methods.
- Always free Godot nodes with `QueueFree()` or `Free()`, never with `Dispose()` alone.
- For the citizen lifecycle specifically: when a citizen node is removed, ensure `_ExitTree()` disconnects all outbound signal subscriptions.
- Consider using the event-style signals Godot 4 generates (`[Signal]` + `EventHandler`) which are automatically disconnected when the GodotObject is freed — but verify this with the current engine version, as behavior has changed across Godot 4.x releases.

**Warning signs:**
- Gradual memory growth over time that does not plateau
- Godot's debugger "Orphan Nodes" count rising as citizens are added/removed
- Signal callbacks firing on objects that have already been freed (prints warning "Object was freed")
- Memory profile showing GodotObject instances growing unbounded

**Phase to address:**
Phase 1 (citizen scene setup) — establish the lifecycle pattern early; retrofitting signal hygiene across a large codebase is expensive.

---

### Pitfall 3: Torus/Ring Collision Shape — No Native Support

**What goes wrong:**
The ring geometry (flat donut) has no corresponding primitive collision shape in Godot 4. Developers who create the visual ring mesh and then try to add a "CollisionShape3D" child expecting a torus option will find it doesn't exist. Godot's physics engine (GodotPhysics or Jolt) does not support torus primitives. Attempting to use a ConcaveMeshShape3D (trimesh collision) generated from the visual mesh works, but detects collisions on both the top AND inside/underside surfaces, causing phantom collisions where citizens or room placement raycasts hit the invisible inner face.

**Why it happens:**
Tori are non-convex shapes. Most real-time physics engines only natively support convex primitives (box, sphere, capsule, cylinder) and approximate concave shapes via triangle meshes. The Godot community has been requesting a torus CollisionShape since at least 2022 (GitHub Discussion #6244) with no resolution as of 2025. CSGTorus can generate mesh collision, but inherits the same inner-face problem.

**How to avoid:**
Use an explicit approximation strategy determined by what collisions actually need to exist:
- For the ring walkway surface (citizen foot placement): a flat annular mesh (the walkway only) as a StaticBody3D with a ConcaveMeshShape3D or composed BoxShape3D tiles approximating the arc is sufficient. Citizens walk on it; the underside never needs to be walkable.
- For segment-level room placement hit detection (mouse picking to select a segment): use mathematical collision — project the mouse ray to the ring's plane (Y=0), convert the 2D hit position to polar coordinates, and determine segment from the angle and radius band. This avoids collision shapes entirely for selection.
- For room occupancy boundaries: invisible BoxShape3D or CylinderShape3D volumes per segment work for overlap detection.
- Never use the visual ring mesh directly as a trimesh collider for interactive picking; it will cause inner-face hits.

**Warning signs:**
- Raycasts hitting invisible faces on the underside of the ring
- Room placement cursor "teleporting" to the opposite side of the ring on mouse movement
- Physics bodies falling through the walkway at segment seams

**Phase to address:**
Phase 1 (ring geometry / room placement) — the collision strategy must be decided before implementing room placement UI.

---

### Pitfall 4: C# Rebuild-to-See-Changes Kills Iteration Speed

**What goes wrong:**
In Godot 4, C# does not support true hot reload of gameplay logic. Every time a C# script is changed, the entire .NET assembly must be recompiled, and the game session must be restarted to pick up the changes. GDScript supports near-instant in-editor reload. With a citizen simulation that requires watching 10-20 characters move, interact with rooms, and surface wishes, losing the running simulation state on every code change is extremely disruptive. Developers accustomed to Unity's (limited) hot reload or GDScript's instant reload will underestimate how much this compounds iteration time.

**Why it happens:**
Godot 4's C# integration runs the .NET runtime in a separate process from the editor. Assembly hot-swap was planned (GitHub Proposal #7746) but as of Godot 4.2, "hot reloading only implements not restarting the Godot editor — you still need to restart the game." Tool script reloading resets instantiated script state on rebuild, which has side effects for editor tools. This is a known pain point with active proposals but no resolution in the near term.

**How to avoid:**
- Design systems with data-logic separation: keep citizen state in plain C# data objects (not GodotObject-derived nodes where possible) so that scene-tree restarts don't lose in-memory state during development.
- Invest early in editor debug tooling: expose wish state, happiness values, and citizen positions as Inspector properties or debug overlay UI so you can observe system state without relying on long play sessions.
- Use Godot's `[Tool]` attribute on scene setup scripts carefully — tool scripts reload on every build, and side-effectful constructors will run unexpectedly.
- Keep citizen simulation parameters (wish frequency, happiness thresholds, credit generation rates) in `Resource` files editable in the Inspector, so tuning does not require recompilation.
- Establish a "quick test scene" — a minimal scene with 2-3 citizens and one ring — for rapid iteration without loading the full simulation.

**Warning signs:**
- Compile + restart cycle exceeding 30 seconds on the development machine
- Developers making multiple back-to-back tweaks to values that "should" be data, not code
- Reluctance to change C# files because the restart cost is painful

**Phase to address:**
Phase 1 (project architecture) — establish the data/logic separation pattern before writing citizen simulation code.

---

### Pitfall 5: Wish Economy Positive Feedback Loop Runaway

**What goes wrong:**
The wish-driven economy has a structural runaway risk: more citizens generate more credits → more rooms can be built → more wishes are fulfilled → more citizens arrive → more credits. With no fail state and no credit sink beyond room construction costs, the credit supply rapidly outpaces the demand (room costs). Once players have a few dozen citizens, they become effectively unconstrained — credits accumulate faster than rooms can be built, wishes are trivially satisfiable, and the game loses tension. Conversely, early game progression can stall if the income curve is too shallow: one or two citizens generate almost nothing, the player cannot afford even a basic room, and the game feels broken.

**Why it happens:**
Open-ended builder games without a fail state have no natural corrective mechanism. Positive feedback loops (wealth → more wealth) dominate without explicit counters. The design intentionally avoids debt, maintenance costs, and scarcity crises — but this removes the tools typically used to control runaway growth. The happiness multiplier on credit generation compounds the problem: as happiness rises, credits flow faster, wishes are fulfilled more often, happiness rises more, repeat.

**How to avoid:**
- Apply diminishing returns to the citizens-per-happiness curve: the rate of citizen arrival should increase steeply at first and flatten significantly at high happiness. Use a sigmoid or square-root-shaped relationship, not linear.
- Apply diminishing returns or step increases to credit generation per citizen: the 50th citizen should generate meaningfully less per capita than the 5th, reflecting "mature station economics."
- Scale room costs with ring number rather than room type only: ring 2 rooms should cost more than ring 1 equivalents, acting as a natural credit sink that keeps pace with income growth.
- Introduce a credit-per-citizen income cap: once citizens are above a threshold happiness, they generate at a flat rate, not a multiplier-boosted rate.
- Test the economy in a spreadsheet simulation before implementing it in code: model 30 turns of the loop with expected player behavior to verify it does not go infinite within 20 minutes.

**Warning signs:**
- Playtester has more credits than they know what to do with by the 15-minute mark
- Playtester stops looking at the wish board because they can just build everything
- Credit balance never dips below 50% of building cost of the most expensive room
- Playtester says "I'm not sure what I'm supposed to do" — the game has no meaningful choices left

**Phase to address:**
Phase covering economy implementation — before connecting happiness to citizen arrival rate; verify the curve shape before wiring it to citizen spawning.

---

### Pitfall 6: GDScript-Only Addons Blocking C# Workflow

**What goes wrong:**
The Godot asset library and community ecosystem is approximately 84% GDScript. When reaching for a third-party addon (save system, localization, UI tweening, analytics, dialogue), the most popular options will be GDScript-only. Calling a GDScript addon from C# is possible via `GD.Load<GDScript>()` and `Call()`, but this:
1. Loses type safety entirely
2. Incurs marshalling overhead on every call
3. Cannot call GDExtension-backed addons at all (Godot does not generate C# bindings for GDExtensions)
4. Breaks IDE autocomplete and refactoring

For Orbital Rings, the most likely addon needs are: UI management, save/load (serialization), and possibly localization. Each of these has GDScript-first implementations in the ecosystem.

**Why it happens:**
Only ~16% of the Godot community uses C# (per 2025 survey). Plugin authors optimize for the majority. The official plugin documentation defaults to GDScript. GDExtension — the high-performance native extension API — explicitly has no C# binding generation.

**How to avoid:**
- Before adopting any addon, check: is there a C# port? Is the .NET NuGet ecosystem an alternative? For save/load: use `System.Text.Json` or `Newtonsoft.Json` directly — these are vastly better than Godot's built-in serialization and have no GDScript dependency. For UI tweening: Godot's `Tween` class is fully C# accessible. For localization: Godot's built-in CSV/PO localization system is accessible from C#.
- Maintain a policy: if an addon cannot be used natively from C# with full type safety, either port the relevant parts to C# or solve the problem with the .NET ecosystem instead.
- Do not mix C# and GDScript in the same game logic layer. Reserve GDScript calls exclusively for editor tooling or one-off integration shims.

**Warning signs:**
- `GD.Load<GDScript>()` appearing anywhere outside of editor-tool scripts
- IDE showing "no completions" warnings on addon API calls
- Runtime errors instead of compile-time errors when calling addon methods

**Phase to address:**
Phase 1 (project setup / dependency decisions) — evaluate all potential addons against C# compatibility before writing any code that depends on them.

---

## Technical Debt Patterns

Shortcuts that seem reasonable but create long-term problems.

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Store all citizen state in Node properties (not separate data objects) | Easy to inspect in editor | Cannot simulate or test citizen logic without a running scene; rebuild kills state | Never — keep data in plain C# classes from the start |
| Auto-bake NavigationMesh for the ring walkway | Zero setup time | Curved geometry bakes badly; agents cut corners or take wrong arc | Never — hand-author the ring navmesh |
| Use anonymous lambda for signal connections | Concise code | Lambda/Callable memory leaks; cannot disconnect | Never for signals on long-lived objects |
| Hard-code economy numbers as C# constants | Fast to write | Every balance change requires recompile + restart | MVP only — move to Resource files before beta |
| Use trimesh collision on the full ring mesh | Single collision node | Inner-face phantom hits on raycasts; room placement cursor misbehaves | Never — use mathematical segment selection instead |
| Heap-allocate per-citizen Update() data each frame | Simple code | GC pressure with 20+ citizens; frame spikes during GC | Acceptable for prototype; profile before shipping |

---

## Integration Gotchas

Common mistakes when connecting systems in this specific project.

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| Citizen → Wish system | Citizen node owns and manages its own wishes inline | Wish system is a separate singleton/manager; citizens emit events, manager tracks wish state independently |
| Room placement → Segment grid | Using 3D collision picking on ring geometry to determine which segment was clicked | Project mouse ray to ring plane, convert polar coordinates to segment index mathematically |
| Economy → Happiness | Wiring happiness as a direct linear multiplier on credit income | Apply happiness as a soft bonus with diminishing returns; cap maximum multiplier at 1.5x or 2x |
| Navigation → Room placement | Rebaking the walkway navmesh every time a room is placed | Room placement does not change the walkway; only bake navmesh when walkway geometry actually changes (never, for v1) |
| C# NuGet packages → Godot build | Adding NuGet packages without verifying AOT/trimming compatibility | Check NuGet package for AOT-safe or Godot-compatible tag; avoid packages that use heavy reflection |

---

## Performance Traps

Patterns that work at small scale but degrade as citizen count grows.

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Each citizen calls `NavigationServer3D.MapGetClosestPoint()` every physics frame | Frame drops as citizen count increases | Citizens on a fixed-topology ring don't need continuous navmesh queries; use arc-distance waypoint paths | ~15-20 citizens with per-frame nav queries |
| All citizen state updates in `_Process()` | CPU time scales linearly with citizen count | Use `_PhysicsProcess()` only for movement; batch happiness/wish updates on a timer (e.g., every 2 seconds) | ~30+ citizens with complex per-frame logic |
| Scripted citizen nodes with expensive `_Process()` | Godot 4 has known perf issues with many scripted nodes calling `_Process` | Confirmed engine bug: adding `_Process` to scripts instantiated ~4000 times causes severe slowdown; even 50-100 scripted citizens with busy `_Process` can show measurable cost | Scales poorly; measure at 20 citizens |
| Instantiating citizen scenes at runtime without pooling | Hitches when citizens arrive | Pre-instantiate a small pool of citizen nodes at scene load; reuse on arrival | Noticeable hitch at each citizen spawn |
| Re-baking NavigationMesh at runtime when rooms change | Frame drop on room placement | Room placement does not change the walkway mesh; navmesh never needs runtime rebake for v1 | Every room placement event |
| Reading/writing GodotObject properties in tight loops | Marshalling overhead on every property access | Cache GodotObject property values in local C# variables before loops; avoid repeated engine interop in hot paths | Any loop touching 10+ GodotObject properties per frame |

---

## UX Pitfalls

Common user experience mistakes specific to this game's design.

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Too many simultaneous wishes visible | Player paralysis — unclear which to act on | Limit visible active wishes to 3-5 at a time; queue the rest; surface them gradually |
| Wish text too abstract | Player doesn't understand what to build | Pair wish text with a room type icon or highlight the relevant build menu category |
| No feedback when a wish is fulfilled | Player doesn't feel rewarded | Animate the citizen's speech bubble with a "thank you" moment; update happiness bar visibly |
| Camera orbit with mouse drag also triggers room placement | Misclicks during orbit place unwanted rooms | Separate camera control (right-mouse-button drag or middle-mouse) from room selection (left-click) |
| Fixed camera tilt makes inner vs. outer segment ambiguous | Player places rooms on wrong ring face | Use hover highlight to clearly indicate which segment (inner/outer) is under the cursor before click confirmation |
| Economy progress invisible | Player doesn't know if they're "doing well" | Show credit income rate (credits/minute) in the HUD, not just current balance |
| Citizen names never reinforced | Attachment to named citizens doesn't form | Surface citizen names in wish bubbles, tooltips, and happiness events — repetition builds attachment |

---

## "Looks Done But Isn't" Checklist

Things that appear complete but are missing critical pieces.

- [ ] **Citizen pathfinding:** Citizens move to destinations — but verify they take the shorter arc around the ring, not always clockwise or always counterclockwise
- [ ] **Room placement:** Rooms visually snap to segments — but verify collision/selection works correctly on the inner arc (smaller radius) vs. outer arc, which subtend different angular widths in screen space
- [ ] **Wish fulfillment:** Wishes disappear when a room is built — but verify the matching logic accounts for room TYPE and SIZE (a 1-segment Café should not fulfill a wish that requires a 3-segment Lounge)
- [ ] **Economy:** Credits accumulate and rooms can be built — but verify the credit income formula has been tested at 5 citizens, 15 citizens, and 30 citizens to confirm it does not go flat or go runaway
- [ ] **Signal connections:** Citizens connect to wish/happiness signals at spawn — but verify they disconnect cleanly at removal (check Godot Orphan Nodes counter)
- [ ] **Camera orbit:** Camera orbits around the ring center — but verify the tilt angle is fixed and does not drift over long orbit sessions (floating-point quaternion accumulation error)
- [ ] **Segment coordinate system:** Segments are numbered and positioned correctly — but verify that segment 1 and segment 12 are adjacent (the ring closes correctly) and that the "clock face" numbering is consistent between the data model and the visual geometry

---

## Recovery Strategies

When pitfalls occur despite prevention, how to recover.

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Auto-baked navmesh producing bad paths | HIGH | Throw out the NavMesh approach; implement custom waypoint arc system; 2-5 days rework |
| Signal leak causing memory growth | MEDIUM | Audit all signal connections in citizen and room scripts; add disconnect calls to _ExitTree(); run with Godot memory profiler to confirm resolution |
| Economy runaway discovered in late playtesting | MEDIUM | Economy is data-driven (Resource files); adjust income/cost curves without recompile; 1-2 days tuning |
| GDScript addon dependency embedded in core systems | HIGH | Port the addon subset needed to C# or replace with .NET NuGet equivalent; difficulty scales with how deeply the addon is integrated |
| Torus collision causing phantom raycast hits | MEDIUM | Replace trimesh collider with mathematical polar-coordinate segment selection; 1-2 days rework |
| Citizen count causing frame drops | MEDIUM | Profile to identify hot path; batch update logic; reduce _Process frequency; may require citizen pooling rework |

---

## Pitfall-to-Phase Mapping

How roadmap phases should address these pitfalls.

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Circular walkway navmesh | Ring geometry + citizen movement phase | Playtest: 5 citizens, verify arc choice, no stuck states |
| C# signal memory leaks | Phase 1 architecture (citizen lifecycle) | Run for 10 minutes; check Godot Orphan Nodes count = 0 |
| No torus collision shape | Ring geometry / room placement phase | Verify raycast segment selection uses math, not trimesh collision |
| C# rebuild iteration speed | Phase 1 setup | Economy and wish params in Resource files; quick test scene established |
| Economy runaway | Economy implementation phase | Spreadsheet model before coding; playtested at 5/15/30 citizens |
| GDScript addon incompatibility | Phase 1 dependency audit | All dependencies verified C#-native or .NET NuGet before first use |
| Camera float drift | Camera implementation phase | Orbit 360 degrees 10 times; verify tilt angle unchanged |
| Wish paralysis | Wish system UI phase | Playtest: verify player always has clear next action from wish board |

---

## Sources

- Chickensoft — GDScript vs C# in Godot 4: https://chickensoft.games/blog/gdscript-vs-csharp
- Godot Engine — What's new in C# for Godot 4.0: https://godotengine.org/article/whats-new-in-csharp-for-godot-4-0/
- Godot Forum — GC in C# via Godot 4.3 and memory leak with += operator: https://forum.godotengine.org/t/gc-in-c-via-godot-4-3-and-memory-leak-with-using-operator-to-signals/101189
- Godot GitHub Issue — Lambda/Callable memory leak #85112: https://github.com/godotengine/godot/issues/85112
- Godot GitHub Issue — Dispose() causes memory leaks #107579: https://github.com/godotengine/godot/issues/107579
- Godot GitHub Discussion — Torus collision shape #6244: https://github.com/godotengine/godot-proposals/discussions/6244
- Godot GitHub Proposal — Add support for C# hot reloading #7746: https://github.com/godotengine/godot-proposals/issues/7746
- Godot Forum — Terrible performance on .NET with many scripted nodes #98175: https://github.com/godotengine/godot/issues/98175
- Godot Forum — NavigationAgent3D cuts corners and gets stuck #88237: https://github.com/godotengine/godot/issues/88237
- Godot Forum — Dynamically update NavigationRegion3D: https://forum.godotengine.org/t/dynamically-update-navigationregion3d-navigationmesh/63865
- Godot Docs — Optimizing Navigation Performance: https://docs.godotengine.org/en/latest/tutorials/navigation/navigation_optimizing_performance.html
- Godot Docs — When and how to avoid using nodes for everything: https://docs.godotengine.org/en/stable/tutorials/best_practices/node_alternatives.html
- Machinations.io — Game economy inflation: https://machinations.io/articles/what-is-game-economy-inflation-how-to-foresee-it-and-how-to-overcome-it-in-your-game-design
- Game Developer — Economy design handbook: https://www.gamedeveloper.com/production/i-designed-economies-for-150m-games-here-s-my-ultimate-handbook
- Godot 4 Recipes — Camera Gimbal: https://kidscancode.org/godot_recipes/4.x/3d/camera_gimbal/index.html
- Godot Forum — Cross-language scripting (C# interop): https://docs.godotengine.org/en/stable/tutorials/scripting/cross_language_scripting.html

---
*Pitfalls research for: Orbital Rings — cozy space station builder (Godot 4 C#)*
*Researched: 2026-03-02*
