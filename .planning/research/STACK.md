# Technology Stack: Citizen AI, Day/Night Cycle, Utility Scoring

**Project:** Orbital Rings v1.4 Citizen AI Milestone
**Researched:** 2026-03-07
**Focus:** Godot 4 APIs for utility-based AI, day/night lighting transitions, trait systems, and citizen state machines

## Current Project Stack (Verified from .csproj and project.godot)

| Technology | Version | Notes |
|------------|---------|-------|
| Godot Engine | 4.6 | config/features in project.godot; Forward Plus renderer |
| Godot.NET.Sdk | 4.6.1 | From .csproj |
| .NET Target | net10.0 | From .csproj TargetFramework |
| C# Events | Pure C# delegates | 8 Autoload singletons coordinated via GameEvents signal bus |
| Renderer | Forward Plus | Supports DirectionalLight3D, WorldEnvironment, shader globals |

**No new NuGet packages needed.** This milestone uses only built-in Godot APIs. No external AI libraries, no behavior tree plugins, no lighting middleware.

## Recommended Stack Additions

### New Autoload Singleton: StationClock

| Technology | API | Purpose | Why |
|------------|-----|---------|-----|
| Timer (Godot) | `Timer` node, OneShot=false | Drive clock tick every N seconds | Consistent with project pattern (HappinessManager, EconomyManager). Timer pauses with scene tree via ProcessMode. |
| Enum (C#) | `enum StationPeriod { Morning, Day, Evening, Night }` | Four time periods | Matches PROJECT.md spec. Discrete periods avoid floating-point time comparisons. |
| Resource (Godot) | `StationClockConfig : Resource` with `[Export]` fields | Inspector-tunable period durations | Follows HappinessConfig/EconomyConfig/HousingConfig pattern. Keeps timing constants out of code. |

**Why a new Autoload (9th singleton):** The station clock is cross-cutting state consumed by multiple systems (lighting, citizen AI, UI, save/load). It mirrors HousingManager's rationale: "separate concerns -- HappinessManager shouldn't own clock state." The clock is not owned by any single scene node.

### Day/Night Visual System: DayNightManager (Scene Node, NOT Autoload)

| Technology | API | Purpose | Why |
|------------|-----|---------|-----|
| DirectionalLight3D | `LightColor: Color`, `LightEnergy: float` | Sun/moon representation | Built-in Godot 3D light. Properties are directly tweakable. No shadows needed (stylized game). |
| WorldEnvironment | `Environment` resource | Ambient light color/energy, background color | Built-in. Hosts the Environment resource that controls ambient fill light. |
| Environment (Resource) | `AmbientLightColor`, `AmbientLightEnergy`, `BackgroundMode`, `BackgroundColor` | Per-period atmosphere | Environment.BGMode.Color (value 1) for flat color background. AmbientSource.Color (value 2) for direct color control without sky. |
| Tween (Godot) | `CreateTween().TweenProperty()` | Smooth transitions between periods | Already used extensively in CitizenNode (visit/home sequences). Proven pattern. Kill-before-create for safety. |
| RenderingServer | `GlobalShaderParameterSet(StringName, Variant)` | Drive emissive intensity on room window materials | Global shader uniforms let ALL room materials respond to time-of-day without per-material references. Define `global uniform float emissive_strength;` in Project Settings. |

**Why NOT an Autoload:** DayNightManager owns scene nodes (DirectionalLight3D, WorldEnvironment). Autoloads are `Node` subclasses, not `Node3D`. Scene-tree ownership is cleaner: DayNightManager lives as a child of the Ring scene and manages its own light/environment children.

**Why NOT AnimationPlayer:** AnimationPlayer requires pre-authored keyframes in the editor. Tween-based transitions are code-driven, match the project's procedural philosophy (zero .tres animation resources), and integrate naturally with the StationClock events.

### Citizen Traits

| Technology | API | Purpose | Why |
|------------|-----|---------|-----|
| Enum (C#) | `enum InterestTrait { Social, Studious, Creative, Nature, Comfort }` | Citizen interest bias | Maps to room categories. Simple enum avoids over-engineering a trait system. |
| Enum (C#) | `enum RhythmTrait { EarlyBird, NightOwl, Steady }` | Time-of-day activity preference | Affects utility scores during different StationPeriods. Three values is sufficient for visible behavioral variety. |
| CitizenData extension | Add `[Export] InterestTrait Interest` and `[Export] RhythmTrait Rhythm` | Store traits on existing Resource | CitizenData already holds identity + appearance. Adding two enum fields is a natural extension, not a new class. |

**Why enums, not a trait component system:** The project has 5 room categories and 4 time periods. Two enum fields create 5x3=15 behavioral archetypes. A component system (ECS-like traits) is overkill for this granularity and conflicts with the project's POCO/Resource pattern.

### Utility Scoring System (Pure C# POCO)

| Technology | API | Purpose | Why |
|------------|-----|---------|-----|
| Static method or POCO class | `UtilityScorer.ScoreRoom(citizen, room, period, ...)` | Calculate per-room utility score | POCO follows MoodSystem pattern (testable in isolation, no Godot Node dependency). Static method is even simpler since scoring is stateless. |
| float scoring | Weighted sum of normalized 0-1 factors | Combine trait affinity, proximity, recency, wish bonus, period weight | Weighted sum is the simplest utility AI approach. No need for response curves or action hierarchies for this scope. |
| Dictionary<int, double> | C# Dictionary | Track last-visited timestamps per room | Recency penalty prevents citizens from visiting the same room repeatedly. Per-citizen state, stored on CitizenNode. |

**Why NOT behavior trees:** Behavior trees add selection/sequence/decorator node hierarchies. The citizen decision is one question: "which room should I visit?" A single scoring function answers this directly. Behavior trees solve action sequencing; the citizen state machine (below) handles that.

**Why NOT an external utility AI library:** No C# utility AI libraries exist for Godot 4 that are maintained and lightweight. Rolling a 30-line scoring function is simpler than integrating a framework.

### Citizen State Machine (Enum-Based)

| Technology | API | Purpose | Why |
|------------|-----|---------|-----|
| Enum (C#) | `enum CitizenState { Walking, Evaluating, Visiting, Resting }` | Explicit state tracking | Replaces implicit state via `_isVisiting` and `_isAtHome` booleans. Four states match the PROJECT.md spec. |
| switch expression (C#) | `_currentState switch { ... }` in `_Process` | State-dependent behavior dispatch | Pattern already used for MoodTier income multipliers and wish category icons. switch expressions are exhaustive with enum. |
| Tween (Godot) | Chained tween sequences | Animate transitions between states | Proven in CitizenNode (8-phase visit sequence, 8-phase home return). Same kill-before-create pattern. |
| Timer (Godot) | OneShot Timer for evaluation cooldown | Periodic decision-making | Existing pattern: `_visitTimer`, `_wishTimer`, `_homeTimer`. Replace with single `_evaluationTimer` that fires during Walking state. |

**Why enum-based, not node-based state machine:** The GDQuest and community patterns show two approaches: (1) enum + switch in `_Process`, (2) separate Node per state. This project's citizens are simple (4 states, no complex enter/exit logic beyond tween sequences). Enum-based keeps all citizen logic in one file, avoids node allocation per citizen (performance with many citizens), and matches the project's "one script per concern" philosophy.

**Why 4 states, not more:** Walking (default), Evaluating (choosing a room via utility scoring), Visiting (tween sequence at a room), Resting (home return tween sequence). The existing CitizenNode already has these behaviors spread across booleans (`_isVisiting`, `_isAtHome`, `_walkingToHome`). The state machine formalizes what already exists.

### Schedule Templates

| Technology | API | Purpose | Why |
|------------|-----|---------|-----|
| Resource (Godot) | `ScheduleTemplate : Resource` with `[Export]` arrays | Per-period activity weights | Inspector-tunable. Designers can adjust without code changes. |
| Dictionary<StationPeriod, float[]> or nested arrays | C# data structure | Map period to per-RoomCategory weight multipliers | Lookup table: `weights[period][category]` returns a 0-1 multiplier applied to utility scores. |

**Why Resource, not hardcoded:** The project consistently uses `[GlobalClass] Resource` subclasses for tunable data (HappinessConfig, EconomyConfig, HousingConfig, RoomDefinition, WishTemplate). Schedule templates follow the same pattern.

### Save Format v4

| Technology | API | Purpose | Why |
|------------|-----|---------|-----|
| System.Text.Json | Existing serializer | Serialize clock position and citizen traits | Already used for v1/v2/v3 save format. No new serialization dependency. |
| SaveData extension | Add fields with defaults | Backward-compatible v4 fields | Pattern established: v2 added LifetimeHappiness (default 0), v3 added HomeSegmentIndex (default null). v4 adds ClockProgress (default 0f), citizen Interest/Rhythm (default values). |

**Why version 4, not migration:** Same approach as v2 and v3. New fields have sensible defaults when deserializing old saves. Clock starts at Morning, traits get randomly assigned on first load. No migration needed.

## Godot API Reference (Verified Properties)

### DirectionalLight3D (Inherits Light3D)

Key properties for day/night animation:

| C# Property | Type | Purpose | Tween Path |
|-------------|------|---------|------------|
| `LightColor` | `Color` | Sun/moon color shift | `"light_color"` |
| `LightEnergy` | `float` | Brightness (0=off, 1=normal) | `"light_energy"` |
| `LightTemperature` | `float` | Color temperature in Kelvin | `"light_temperature"` |
| `SkyMode` | `DirectionalLight3D.SkyModeEnum` | Whether light affects sky | N/A (set once) |

**DirectionalLight3D.SkyModeEnum values:** `LightAndSky`, `LightOnly`, `SkyOnly`

Use `SkyMode = SkyModeEnum.LightOnly` because there is no sky in this project (flat color background).

**Tween example for light transition:**
```csharp
var tween = CreateTween();
tween.SetParallel(true);
tween.TweenProperty(_sunLight, "light_color", nightColor, transitionDuration)
    .SetTrans(Tween.TransitionType.Sine)
    .SetEase(Tween.EaseType.InOut);
tween.TweenProperty(_sunLight, "light_energy", nightEnergy, transitionDuration)
    .SetTrans(Tween.TransitionType.Sine)
    .SetEase(Tween.EaseType.InOut);
```

**Confidence:** HIGH -- properties verified from official Godot 4.4 docs class reference for Light3D and DirectionalLight3D.

### WorldEnvironment + Environment Resource

Key properties for ambient atmosphere:

| C# Property | Type | Purpose | Tween Path |
|-------------|------|---------|------------|
| `AmbientLightColor` | `Color` | Fill light color | Access via `_environment.AmbientLightColor = ...` |
| `AmbientLightEnergy` | `float` | Fill light intensity | Access via `_environment.AmbientLightEnergy = ...` |
| `BackgroundColor` | `Color` | Flat background color | Access via `_environment.BackgroundColor = ...` |
| `BackgroundEnergyMultiplier` | `float` | Background brightness | Access via `_environment.BackgroundEnergyMultiplier = ...` |

**Environment.BGModeEnum values:** `ClearColor` (0), `Color` (1), `Sky` (2), `Canvas` (3), `Keep` (4), `CameraFeed` (5)

Use `BackgroundMode = Environment.BGModeEnum.Color` for flat color background (no sky asset needed).

**Environment.AmbientSourceEnum values:** `Bg` (0), `Disabled` (1), `Color` (2), `Sky` (3)

Use `AmbientLightSource = Environment.AmbientSourceEnum.Color` for direct color control.

**TweenMethod pattern for Environment properties (cannot use TweenProperty on sub-resources):**
```csharp
// Environment is a sub-resource -- TweenProperty cannot traverse "environment:ambient_light_color"
// Use TweenMethod with Callable.From instead (proven pattern from CitizenNode)
var env = _worldEnvironment.Environment;
var startColor = env.AmbientLightColor;
var endColor = new Color(0.1f, 0.1f, 0.2f); // night ambient

tween.TweenMethod(
    Callable.From((float t) => {
        env.AmbientLightColor = startColor.Lerp(endColor, t);
    }),
    0.0f, 1.0f, transitionDuration
);
```

**Confidence:** HIGH -- Environment properties verified from official docs and ROKOJORI Godot API mirror.

### RenderingServer Global Shader Parameters

For driving emissive materials across all room windows simultaneously:

| C# Method | Signature | Purpose |
|-----------|-----------|---------|
| `RenderingServer.GlobalShaderParameterSet` | `(StringName name, Variant value)` | Set global uniform value at runtime |
| `RenderingServer.GlobalShaderParameterAdd` | `(StringName name, RenderingServer.GlobalShaderParameterType type, Variant defaultValue)` | Create global uniform (also settable via Project Settings) |

**Setup in Project Settings:**
1. Project > Project Settings > Shader Globals
2. Add `emissive_strength` as `float`, default `0.0`

**In room window shader:**
```glsl
shader_type spatial;
global uniform float emissive_strength;
uniform vec3 emissive_color : source_color;

void fragment() {
    EMISSION = emissive_color * emissive_strength;
}
```

**In C# DayNightManager:**
```csharp
// Tween emissive_strength from 0.0 (day) to 1.0 (night)
tween.TweenMethod(
    Callable.From((float strength) => {
        RenderingServer.GlobalShaderParameterSet("emissive_strength", strength);
    }),
    0.0f, 1.0f, transitionDuration
);
```

**Confidence:** MEDIUM -- API verified from official docs and community examples. The `GlobalShaderParameterSet` method name is confirmed PascalCase in C# bindings. However, setting globals from Project Settings vs. code has had past issues (godotengine/godot#77988). Define in Project Settings first, set from code at runtime.

### Tween API (Already Proven in Codebase)

The project already uses Tweens extensively. Key patterns already established:

| Pattern | Where Used | Reuse For |
|---------|-----------|-----------|
| Kill-before-create | `_activeTween?.Kill()` in CitizenNode | DayNightManager transition tween |
| TweenMethod + Callable.From | Visit/home animations | Environment property interpolation |
| SetParallel(true) | Wish badge pop animation | Parallel light + ambient + emissive transitions |
| Chained sequences | 8-phase visit/home tween | Multi-property day/night transitions |
| SetEase + SetTrans | Badge scale animation | Sine/InOut for natural lighting curves |

**No new Tween patterns needed.** All required tween techniques are already in the codebase.

### Timer Nodes (Already Proven in Codebase)

| Pattern | Where Used | Reuse For |
|---------|-----------|-----------|
| OneShot periodic re-arm | `_wishTimer`, `_homeTimer` | `_evaluationTimer` in citizen state machine |
| Non-OneShot repeating | `_visitTimer` | `StationClock` tick timer |
| WaitTime randomization | `GD.Randf() * range` | Evaluation cooldown variation |

## Architecture Integration Points

### New Events for GameEvents Signal Bus

```csharp
// Clock Events
public event Action<StationPeriod, StationPeriod> PeriodChanged;  // (newPeriod, oldPeriod)
public event Action<float> ClockTicked;  // normalized progress 0-1 within period

// Citizen AI Events
public event Action<string, int, float> CitizenEvaluatedRoom;  // (citizenName, segmentIndex, score)
```

**PeriodChanged** follows the MoodTierChanged pattern: (new, old) tuple lets listeners react to transitions.

### New Autoload Registration Order

```ini
[autoload]
GameEvents="*res://Scripts/Autoloads/GameEvents.cs"
EconomyManager="*res://Scripts/Autoloads/EconomyManager.cs"
BuildManager="*res://Scripts/Build/BuildManager.cs"
CitizenManager="*res://Scripts/Citizens/CitizenManager.cs"
WishBoard="*res://Scripts/Autoloads/WishBoard.cs"
HappinessManager="*res://Scripts/Autoloads/HappinessManager.cs"
HousingManager="*res://Scripts/Autoloads/HousingManager.cs"
StationClock="*res://Scripts/Autoloads/StationClock.cs"     # NEW (9th)
SaveManager="*res://Scripts/Autoloads/SaveManager.cs"        # stays last
```

**StationClock before SaveManager** because SaveManager collects state from all singletons. SaveManager must always be last.

### DayNightManager Scene Integration

```
Ring.tscn
  Ring (RingVisual)
    SegmentInteraction
    DayNightManager (Node3D)        # NEW
      SunLight (DirectionalLight3D)
      WorldEnvironment
```

DayNightManager subscribes to `GameEvents.PeriodChanged` and drives its child nodes.

## Alternatives Considered

| Category | Recommended | Alternative | Why Not |
|----------|-------------|-------------|---------|
| AI Decision | Utility scoring (weighted sum) | Behavior tree (e.g., Beehave addon) | Overkill. Citizens make one decision: "which room?" A single scoring function is sufficient. Behavior trees solve action sequencing, which the state machine already handles. |
| AI Decision | Utility scoring | GOAP (Goal-Oriented Action Planning) | Way overkill. GOAP is for complex multi-step plans. Citizens pick a room and walk to it. |
| State machine | Enum + switch | Node-per-state pattern | Allocates a node per state per citizen. With 20+ citizens, that is 80+ extra nodes. Enum + switch keeps it in one class, matches project's "one script per concern" pattern. |
| State machine | Enum + switch | Stateless (current boolean flags) | Current `_isVisiting`/`_isAtHome`/`_walkingToHome` booleans are already an implicit state machine. Formalizing with an enum prevents illegal state combinations (e.g., `_isVisiting && _isAtHome` simultaneously). |
| Lighting | DirectionalLight3D + WorldEnvironment | Custom shader-only approach | DirectionalLight3D provides physically-based lighting that interacts with StandardMaterial3D (already used on all meshes). A shader-only approach would require replacing all materials. |
| Lighting | Tween-based transitions | AnimationPlayer | AnimationPlayer requires editor-authored .tres files. Tweens are code-driven, matching the project's procedural philosophy. |
| Emissive control | Global shader parameter | Per-material property setting | The project builds room visuals programmatically with StandardMaterial3D. Adding emissive requires either (a) tracking all room materials and updating each one, or (b) one global uniform. Global uniform is O(1) regardless of room count. |
| Trait storage | Enums on CitizenData Resource | Separate TraitComponent class | CitizenData already exists as the citizen identity record. Two additional enum fields are trivial. A separate component adds indirection with no benefit at this scale. |
| Schedule data | Resource with Export arrays | Hardcoded arrays | Resources are Inspector-tunable. Hardcoded arrays require recompilation to adjust weights. Following the Config resource pattern (HappinessConfig, EconomyConfig). |
| Clock | Timer-based Autoload | _Process delta accumulation | Timer nodes pause with the scene tree (ProcessMode.Pausable) automatically. _Process accumulation requires manual pause handling. Timers match the project's existing clock-like patterns. |

## What NOT to Add

| Do NOT Add | Why |
|------------|-----|
| Beehave or other behavior tree plugins | Single-decision utility scoring does not need action sequencing infrastructure |
| GOAP library | No multi-step planning needed. Citizens walk to rooms. |
| External utility AI framework | No maintained C# Godot utility AI library exists. A 30-line scoring function is simpler. |
| Sky resource / ProceduralSkyMaterial | The game uses a flat color background. A sky adds visual complexity that conflicts with the soft/cozy art style. Use Environment.BGModeEnum.Color. |
| HDR / Tonemapping changes | The game's pastel palette relies on LDR colors. HDR tonemapping would alter the established look. |
| Shadow mapping | Stylized game with pastel capsule citizens. Shadows add visual noise and GPU cost with no readability benefit. Keep `ShadowEnabled = false`. |
| OmniLight3D / SpotLight3D for room interiors | Room interiors are placeholder colored blocks. Per-room lighting is deferred to a potential interior generation milestone. |
| Navigation / pathfinding | Citizens walk a 1D circular path (angle += speed * delta). NavigationAgent3D is not needed and the comment in CitizenNode explicitly states this. |
| Godot [Signal] attribute | Project uses pure C# event delegates exclusively (documented decision: avoids marshalling overhead and IsConnected bugs). |
| New NuGet packages | All required functionality is built into Godot 4.x and .NET. |

## New File Inventory

| File | Type | Purpose |
|------|------|---------|
| `Scripts/Autoloads/StationClock.cs` | Autoload singleton | Clock state, period transitions, tick timer |
| `Scripts/Data/StationClockConfig.cs` | Resource | Period durations, total cycle time |
| `Scripts/Data/StationPeriod.cs` | Enum | Morning/Day/Evening/Night |
| `Scripts/DayNight/DayNightManager.cs` | Scene node (Node3D) | Light/environment transitions, emissive control |
| `Scripts/Data/DayNightConfig.cs` | Resource | Per-period colors, energies, transition duration |
| `Scripts/Data/InterestTrait.cs` | Enum | Social/Studious/Creative/Nature/Comfort |
| `Scripts/Data/RhythmTrait.cs` | Enum | EarlyBird/NightOwl/Steady |
| `Scripts/Citizens/UtilityScorer.cs` | Static class or POCO | Room scoring function |
| `Scripts/Data/ScheduleTemplate.cs` | Resource | Per-period activity weight arrays |
| `Scripts/Citizens/CitizenState.cs` | Enum | Walking/Evaluating/Visiting/Resting |

No new scenes needed beyond modifying `Ring.tscn` to add DayNightManager children.

## Sources

- [DirectionalLight3D -- Godot 4.4 Docs](https://docs.godotengine.org/en/4.4/classes/class_directionallight3d.html) -- SkyMode enum, shadow properties
- [Light3D -- Godot 4.4 Docs](https://docs.godotengine.org/en/4.4/classes/class_light3d.html) -- LightColor, LightEnergy, LightTemperature
- [Environment -- Godot 4.4 Docs](https://docs.godotengine.org/en/4.4/classes/class_environment.html) -- AmbientLightColor, BackgroundMode, BGMode enum
- [Environment and Post-Processing -- Godot 4.4 Tutorial](https://docs.godotengine.org/en/4.4/tutorials/3d/environment_and_post_processing.html) -- Ambient light setup
- [RenderingServer -- Godot Stable Docs](https://docs.godotengine.org/en/stable/classes/class_renderingserver.html) -- GlobalShaderParameterSet, GlobalShaderParameterAdd
- [Godot 4.0 Global Shader Uniforms Announcement](https://godotengine.org/article/godot-40-gets-global-and-instance-shader-uniforms/) -- Global uniform setup and usage
- [Tween -- Godot Stable Docs](https://docs.godotengine.org/en/stable/classes/class_tween.html) -- TweenProperty, TweenMethod, SetParallel
- [DirectionalLight3D C# API (4.2.2)](https://straydragon.github.io/godot-csharp-api-doc/4.2.2-stable/main/Godot.DirectionalLight3D.html) -- PascalCase property names
- [Environment C# API](https://haxegodot.github.io/godot/godot/Environment.html) -- C# property names for ambient/background
- [ROKOJORI Environment Class Reference](https://rokojori.com/en/labs/godot/docs/4.3/environment-class) -- BGMode and AmbientSource enum values
- [GDQuest Finite State Machine Tutorial](https://www.gdquest.com/tutorial/godot/design-patterns/finite-state-machine/) -- Enum vs node-based state machine patterns
- [Global Shader Parameter Issue #77988](https://github.com/godotengine/godot/issues/77988) -- Known issue with GlobalShaderParameterAdd from code
- Existing codebase: `CitizenNode.cs` (tween patterns), `GameEvents.cs` (event bus), `MoodSystem.cs` (POCO pattern), `HappinessConfig.cs` (Resource pattern), `SaveManager.cs` (versioned save format)

---

*Stack research for: Orbital Rings v1.4 -- Citizen AI, Day/Night Cycle, Utility Scoring*
*Researched: 2026-03-07*
