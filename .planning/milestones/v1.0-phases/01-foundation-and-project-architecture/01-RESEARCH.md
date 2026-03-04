# Phase 1: Foundation and Project Architecture - Research

**Researched:** 2026-03-02
**Domain:** Godot 4.6 C# project architecture, signal bus, custom Resources, orbital camera
**Confidence:** HIGH

## Summary

Phase 1 establishes the architectural skeleton that every subsequent phase builds on: a GameEvents signal bus Autoload with typed C# delegates, three Resource subclasses (RoomDefinition, WishTemplate, CitizenData), an orbital camera with smooth momentum and fixed 60-degree tilt, a quick-test sandbox scene, and a signal disconnect convention. The project starts from a blank Godot 4.6 C# template (Godot.NET.Sdk 4.6.1, .NET 8.0, root namespace `OrbitalRings`, Forward Plus renderer, Jolt Physics).

Godot 4.6 C# uses `[Signal]`-attributed delegates that compile into typed event accessors (`+=`/`-=`), and the idiomatic signal bus pattern is an Autoload Node with a static `Instance` property set in `_Ready()`. Custom Resources use `[GlobalClass]` + `partial class` inheriting `Resource` with `[Export]` properties, and the file name must match the class name exactly (case-sensitive). The orbital camera is best implemented as a Node3D gimbal (pivot node rotated on Y-axis) with a Camera3D child offset at the desired tilt and distance, using `Mathf.Lerp` / `Mathf.LerpAngle` for momentum smoothing.

**Primary recommendation:** Build the GameEvents Autoload using pure C# `event Action<T>` delegates (not Godot `[Signal]`) for the signal bus, use `[GlobalClass]` Resource subclasses for data, implement the camera as a Node3D pivot + Camera3D child with lerp-based momentum, and establish signal disconnect in `_ExitTree()` as the project-wide convention.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **Camera Feel**: Smooth orbit with momentum (cinematic glide after input stops), fixed tilt ~60 degrees, zoom from segment-level close to whole-ring overview, bounded zoom-out, smooth continuous scroll-wheel zoom, no auto-centering on click, gentle slow idle orbit when not interacting, right-click drag for orbit, WASD/arrow keys as alternative, default view whole ring visible
- **Ring Proportions**: Chunky flat disc shape (not rounded torus), rooms sit on flat top surface, walkway slightly recessed between rows, static ring (no rotation)
- **Scale and Feel**: Tabletop miniature / diorama feel, at max zoom rooms and citizens clearly readable, bounded zoom limits both ends
- **Visual Starting Point**: Low-poly stylized (Monument Valley / Islanders), warm pastel palette (soft pinks, oranges, lavenders against dark space), clean starfield background, environment based on Kenney Starter Kit Basic Scene (procedural sky, filmic tonemapping, SSAO radius 0.3 intensity 0.5, soft bloom, directional sun with shadows)
- **Core Entity Identity - Citizens**: CitizenData captures name + appearance (body type, color palette, accessory), no personality traits in v1, real-world human names (diverse), appearance: body type variation, primary + secondary color, one accessory
- **Core Entity Identity - Rooms**: RoomDefinition is purely mechanical: category, size, stats; no mood/flavor tags; 5 categories: Housing, Life Support, Work, Comfort, Utility
- **Core Entity Identity - Wishes**: WishTemplate includes multiple text variants per wish type (3-5 options), explicit mapping from wish to fulfilling RoomDefinition(s)
- **Economy Schema**: Economy values live in separate EconomyConfig Resource, not inside RoomDefinition; rooms reference type, EconomyConfig centralizes balance numbers
- **Data Schema Scope**: Strictly v1 fields only, no v2 stubs
- **Input Philosophy**: Left-click = select/inspect, hover highlights with glow/outline, escape deselects then pause menu, build panel mouse-driven with hotkey bar 1-5

### Claude's Discretion
- Signal bus architecture and GameEvents Autoload design
- Signal disconnect pattern implementation
- Folder structure and namespace organization
- Camera momentum easing curves and exact zoom parameters
- Idle orbit speed and behavior
- Placeholder geometry specifics (ring dimensions in Godot units)
- Highlight/glow shader approach

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| RING-02 | Player can orbit the camera horizontally around the ring and zoom in/out at a fixed tilt angle | Camera architecture: Node3D pivot + Camera3D child pattern, right-click drag + WASD input handling, smooth lerp momentum, scroll-wheel zoom with bounded range |
</phase_requirements>

## Standard Stack

### Core
| Library/Tool | Version | Purpose | Why Standard |
|-------------|---------|---------|--------------|
| Godot Engine | 4.6 | Game engine | Already configured in project.godot |
| Godot.NET.Sdk | 4.6.1 | C# SDK for Godot | Already in .csproj |
| .NET | 8.0 | Runtime target | Already in .csproj |
| Jolt Physics | (bundled) | Physics engine | Already configured in project.godot |

### Supporting
| Asset/Resource | Version | Purpose | When to Use |
|----------------|---------|---------|-------------|
| Kenney Starter Kit Basic Scene | Latest (Godot 4.5+) | Environment lighting template | Phase 1 environment setup -- clone WorldEnvironment, DirectionalLight3D, ProceduralSkyMaterial settings |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Pure C# `event Action<T>` for signal bus | Godot `[Signal]` delegates | Godot signals have marshalling overhead crossing C#/engine boundary, `IsConnected` has known bugs with custom signals (GitHub #76690), pure C# events are faster and fully type-safe -- but invisible to Godot editor signal panel. For a centralized bus this is acceptable. |
| Node3D pivot gimbal for camera | SpringArm3D | SpringArm3D is designed for obstacle avoidance (ray-casting to prevent clipping) which is unnecessary for a fixed-tilt strategy camera orbiting a ring. A plain Node3D pivot is simpler and more predictable. |

**Installation:**
No additional packages needed. The project already has all required SDK references. Kenney Starter Kit assets will be manually adapted (not installed as a plugin).

## Architecture Patterns

### Recommended Project Structure
```
res://
├── Scenes/
│   ├── Main/              # Main game scene, entry point
│   ├── QuickTest/         # Quick-test sandbox scene
│   └── UI/                # UI scenes (future phases)
├── Scripts/
│   ├── Autoloads/         # GameEvents.cs (signal bus)
│   ├── Camera/            # OrbitalCamera.cs, CameraConfig resource
│   ├── Data/              # Resource subclasses (RoomDefinition, WishTemplate, CitizenData, EconomyConfig)
│   ├── Core/              # Base classes, SafeNode.cs (disconnect pattern)
│   └── Ring/              # Ring-related scripts (Phase 2+)
├── Resources/
│   ├── Data/              # .tres instances of custom Resources
│   └── Environment/       # WorldEnvironment .tres, sky, lighting
├── Shaders/               # Highlight/glow shaders (future)
└── Art/                   # Placeholder meshes, textures
```

**Namespace mapping:**
- `OrbitalRings.Autoloads` -- GameEvents
- `OrbitalRings.Camera` -- OrbitalCamera, CameraConfig
- `OrbitalRings.Data` -- RoomDefinition, WishTemplate, CitizenData, EconomyConfig
- `OrbitalRings.Core` -- SafeNode base class

### Pattern 1: GameEvents Signal Bus (Pure C# Events)

**What:** A singleton Autoload node that declares all game-wide events as typed C# `event Action<T>` delegates. All cross-system communication flows through this bus.

**When to use:** Any time two systems need to communicate without direct references (e.g., room placed -> UI updates, citizen arrives -> happiness recalculated).

**Example:**
```csharp
// Source: Community best practice, JetBrains Godot C# Guide, GDQuest Event Bus pattern
// File: Scripts/Autoloads/GameEvents.cs
using System;
using Godot;

namespace OrbitalRings.Autoloads;

public partial class GameEvents : Node
{
    public static GameEvents Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
    }

    // --- Camera Events ---
    public event Action CameraOrbitStarted;
    public event Action CameraOrbitStopped;

    // --- Room Events (Phase 4) ---
    public event Action<string, int> RoomPlaced;       // roomType, segmentIndex
    public event Action<int> RoomDemolished;            // segmentIndex

    // --- Citizen Events (Phase 5) ---
    public event Action<string> CitizenArrived;         // citizenName
    public event Action<string> CitizenDeparted;        // citizenName

    // --- Wish Events (Phase 6) ---
    public event Action<string, string> WishGenerated;  // citizenName, wishType
    public event Action<string, string> WishFulfilled;  // citizenName, wishType

    // --- Economy Events (Phase 3) ---
    public event Action<int> CreditsChanged;            // newBalance
    public event Action<float> HappinessChanged;        // newHappiness

    // --- Progression Events (Phase 7) ---
    public event Action<string> BlueprintUnlocked;      // roomType

    // Emit helpers (provide null-safe invocation)
    public void EmitRoomPlaced(string roomType, int segmentIndex)
        => RoomPlaced?.Invoke(roomType, segmentIndex);

    public void EmitCreditsChanged(int newBalance)
        => CreditsChanged?.Invoke(newBalance);

    // ... pattern continues for all events
}
```

**Registration in project.godot:**
```ini
[autoload]
GameEvents="*res://Scripts/Autoloads/GameEvents.cs"
```

**Why pure C# events instead of Godot [Signal]:**
1. No marshalling overhead (stays in C# runtime)
2. Full type safety with `Action<T>` generics
3. Standard C# `?.Invoke()` null-safe emission
4. `IsConnected` has known bugs with custom C# signals (GitHub #76690, #72994)
5. Disconnect via `-=` is standard C# -- no Godot-specific API needed
6. Tradeoff: events are invisible in the Godot editor Signal tab, but for a centralized bus this is acceptable since all connections are code-driven anyway

### Pattern 2: Signal Disconnect Convention (SafeNode Base Class)

**What:** A base class that establishes the pattern of subscribing to GameEvents in `_EnterTree()` and unsubscribing in `_ExitTree()`, preventing memory leaks and orphan signal connections.

**When to use:** Every node that connects to GameEvents or any other long-lived signal source.

**Example:**
```csharp
// File: Scripts/Core/SafeNode.cs
using Godot;

namespace OrbitalRings.Core;

/// <summary>
/// Base class establishing signal lifecycle convention.
/// Subclasses override SubscribeEvents() and UnsubscribeEvents().
/// </summary>
public partial class SafeNode : Node
{
    public override void _EnterTree()
    {
        SubscribeEvents();
    }

    public override void _ExitTree()
    {
        UnsubscribeEvents();
    }

    /// <summary>
    /// Override to connect to GameEvents and other signal sources.
    /// Called in _EnterTree() -- GameEvents.Instance is guaranteed available
    /// because Autoloads initialize before scene nodes.
    /// </summary>
    protected virtual void SubscribeEvents() { }

    /// <summary>
    /// Override to disconnect from all signal sources.
    /// Called in _ExitTree() -- MUST mirror every connection in SubscribeEvents().
    /// Using -= on an unconnected C# event is safe (no-op), unlike Godot signals.
    /// </summary>
    protected virtual void UnsubscribeEvents() { }
}
```

**Usage:**
```csharp
public partial class HappinessDisplay : SafeNode
{
    protected override void SubscribeEvents()
    {
        GameEvents.Instance.HappinessChanged += OnHappinessChanged;
    }

    protected override void UnsubscribeEvents()
    {
        GameEvents.Instance.HappinessChanged -= OnHappinessChanged;
    }

    private void OnHappinessChanged(float newHappiness)
    {
        // Update UI
    }
}
```

**Why this works:**
- Pure C# `-=` on a delegate that was never `+=` is a safe no-op (unlike Godot's `Disconnect` which throws)
- `_EnterTree()` / `_ExitTree()` are called symmetrically for any node lifecycle (add/remove/queue_free)
- Autoloads are always ready before scene nodes enter the tree
- Known Godot issue #89116: some C# signals don't auto-disconnect after QueueFree -- this pattern sidesteps that entirely by using pure C# events

### Pattern 3: Orbital Camera (Gimbal Pivot)

**What:** A Node3D pivot at the ring center, rotated on the Y-axis for orbit, with a Camera3D child positioned at the desired tilt angle and distance.

**When to use:** This is the single camera system for the game.

**Scene structure:**
```
CameraRig (Node3D) -- pivot point at world origin
├── Camera3D -- offset: rotated -60deg on X, translated back on Z for distance
```

**Example:**
```csharp
// File: Scripts/Camera/OrbitalCamera.cs
using Godot;

namespace OrbitalRings.Camera;

public partial class OrbitalCamera : Node3D
{
    // --- Orbit ---
    [Export] public float OrbitSpeed { get; set; } = 0.005f;
    [Export] public float OrbitMomentumDecay { get; set; } = 0.92f;
    [Export] public float KeyboardOrbitSpeed { get; set; } = 2.0f;

    // --- Zoom ---
    [Export] public float ZoomMin { get; set; } = 5.0f;
    [Export] public float ZoomMax { get; set; } = 25.0f;
    [Export] public float ZoomSpeed { get; set; } = 1.5f;
    [Export] public float ZoomSmoothing { get; set; } = 8.0f;

    // --- Idle ---
    [Export] public float IdleOrbitSpeed { get; set; } = 0.02f;
    [Export] public float IdleTimeout { get; set; } = 5.0f;

    // --- Fixed tilt ---
    [Export] public float TiltAngleDeg { get; set; } = 60.0f;

    private Camera3D _camera;
    private float _orbitVelocity;
    private float _targetZoom;
    private float _currentZoom;
    private float _idleTimer;
    private bool _isDragging;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
        _targetZoom = _currentZoom = (_zoomMin + _zoomMax) / 2f;
        // Position camera at tilt angle
        _camera.RotationDegrees = new Vector3(-TiltAngleDeg, 0, 0);
        _camera.Position = new Vector3(0, 0, _currentZoom);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Right-click drag for orbit
        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Right)
                _isDragging = mb.Pressed;
        }

        if (@event is InputEventMouseMotion mm && _isDragging)
        {
            _orbitVelocity = -mm.Relative.X * OrbitSpeed;
            _idleTimer = 0f;
        }

        // Scroll wheel zoom
        if (@event is InputEventMouseButton scroll)
        {
            if (scroll.ButtonIndex == MouseButton.WheelUp)
                _targetZoom = Mathf.Max(_targetZoom - ZoomSpeed, ZoomMin);
            if (scroll.ButtonIndex == MouseButton.WheelDown)
                _targetZoom = Mathf.Min(_targetZoom + ZoomSpeed, ZoomMax);
            _idleTimer = 0f;
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        // WASD / arrow key orbit
        float keyInput = Input.GetAxis("orbit_left", "orbit_right");
        if (Mathf.Abs(keyInput) > 0.01f)
        {
            _orbitVelocity = keyInput * KeyboardOrbitSpeed * dt;
            _idleTimer = 0f;
        }

        // Apply orbit with momentum decay
        RotateY(_orbitVelocity);
        _orbitVelocity *= OrbitMomentumDecay;

        // Smooth zoom
        _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, ZoomSmoothing * dt);
        _camera.Position = new Vector3(0, 0, _currentZoom);

        // Idle orbit
        _idleTimer += dt;
        if (_idleTimer > IdleTimeout && Mathf.Abs(_orbitVelocity) < 0.001f)
        {
            RotateY(IdleOrbitSpeed * dt);
        }
    }
}
```

### Pattern 4: Custom Resource Subclasses

**What:** `[GlobalClass]` Resource subclasses with `[Export]` properties, editable in the Godot Inspector, saved as `.tres` files.

**Requirements:**
1. Class must be `partial`
2. Class must inherit `Resource` (directly or via chain)
3. `[GlobalClass]` attribute required for Inspector visibility
4. File name MUST match class name exactly (case-sensitive) -- e.g., `RoomDefinition.cs`
5. Use `[ExportGroup]` and `[ExportSubgroup]` for Inspector organization

**Example:**
```csharp
// File: Scripts/Data/RoomDefinition.cs
using Godot;

namespace OrbitalRings.Data;

[GlobalClass]
public partial class RoomDefinition : Resource
{
    public enum RoomCategory
    {
        Housing,
        LifeSupport,
        Work,
        Comfort,
        Utility
    }

    [ExportGroup("Identity")]
    [Export] public string RoomName { get; set; } = "";
    [Export] public string RoomId { get; set; } = "";
    [Export] public RoomCategory Category { get; set; }

    [ExportGroup("Placement")]
    [Export(PropertyHint.Range, "1,3")] public int MinSegments { get; set; } = 1;
    [Export(PropertyHint.Range, "1,3")] public int MaxSegments { get; set; } = 1;

    [ExportGroup("Stats")]
    [Export] public int BaseCapacity { get; set; }
    [Export] public float Effectiveness { get; set; } = 1.0f;
}
```

```csharp
// File: Scripts/Data/CitizenData.cs
using Godot;

namespace OrbitalRings.Data;

[GlobalClass]
public partial class CitizenData : Resource
{
    public enum BodyType { Tall, Short, Round }

    [ExportGroup("Identity")]
    [Export] public string CitizenName { get; set; } = "";

    [ExportGroup("Appearance")]
    [Export] public BodyType Body { get; set; }
    [Export] public Color PrimaryColor { get; set; } = Colors.White;
    [Export] public Color SecondaryColor { get; set; } = Colors.Gray;
    [Export] public string Accessory { get; set; } = "";  // hat, glasses, scarf
}
```

```csharp
// File: Scripts/Data/WishTemplate.cs
using Godot;

namespace OrbitalRings.Data;

[GlobalClass]
public partial class WishTemplate : Resource
{
    public enum WishCategory { Social, Comfort, Curiosity, Variety }

    [ExportGroup("Wish Definition")]
    [Export] public string WishId { get; set; } = "";
    [Export] public WishCategory Category { get; set; }
    [Export] public string[] TextVariants { get; set; } = System.Array.Empty<string>();

    [ExportGroup("Fulfillment")]
    [Export] public string[] FulfillingRoomIds { get; set; } = System.Array.Empty<string>();
}
```

```csharp
// File: Scripts/Data/EconomyConfig.cs
using Godot;

namespace OrbitalRings.Data;

[GlobalClass]
public partial class EconomyConfig : Resource
{
    [ExportGroup("Income")]
    [Export] public float PassiveIncomePerCitizen { get; set; } = 1.0f;
    [Export] public float WorkBonusMultiplier { get; set; } = 1.5f;
    [Export] public float HappinessMultiplierCap { get; set; } = 2.0f;

    [ExportGroup("Costs")]
    [Export] public int BaseRoomCost { get; set; } = 100;
    [Export] public float SizeDiscountFactor { get; set; } = 0.85f;
    [Export] public float DemolishRefundRatio { get; set; } = 0.5f;

    [ExportGroup("Starting Values")]
    [Export] public int StartingCredits { get; set; } = 500;
}
```

### Anti-Patterns to Avoid
- **Godot [Signal] for bus events:** Marshalling overhead, `IsConnected` bugs with custom signals, weaker type safety for cross-system events. Use pure C# events for the bus; reserve Godot `[Signal]` for parent-child scene communication where editor wiring is valuable.
- **Static classes instead of Autoload:** Static C# singletons bypass Godot's lifecycle (`_Ready`, `_Process`, `_ExitTree`) and cannot be reset on scene change. The Autoload pattern gives you Node lifecycle + guaranteed initialization order.
- **Connecting signals in `_Ready()` and forgetting `_ExitTree()`:** This is the #1 memory leak pattern in Godot C#. The SafeNode base class enforces the symmetric subscribe/unsubscribe pattern.
- **Generic base classes with `[GlobalClass]`:** Known issue (GitHub #102057) -- generic parent classes cause "inspector out of date" errors. Keep Resource subclasses non-generic.
- **File name != class name for GlobalClass:** Godot silently fails to register the global class if the file name doesn't match the class name exactly (case-sensitive).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Camera orbit math | Custom quaternion math | Node3D `RotateY()` + child Camera3D positioning | Godot's transform system handles the rotation correctly; manual quaternion/matrix math invites gimbal lock bugs |
| Signal bus wiring | Manual observer lists | C# `event Action<T>` pattern | Delegate multicast is built into the C# runtime, handles multiple subscribers, null-safe with `?.Invoke()` |
| Resource serialization | JSON/XML file loading | Godot Resource `.tres` files with `[Export]` | Godot handles serialization, Inspector editing, and hot-reload automatically |
| Input mapping | Hardcoded key checks | Godot Input Map + `Input.GetAxis()` | Remappable, supports multiple devices, handles dead zones |
| Environment/lighting | Manual light setup from scratch | Adapt Kenney Starter Kit Basic Scene settings | Proven SSAO, bloom, sky, and lighting values designed for low-poly aesthetic |

**Key insight:** Godot's built-in systems (transform hierarchy, Resource serialization, Input Map) are the correct abstraction level. Hand-rolling these creates maintenance burden and misses engine-level optimizations.

## Common Pitfalls

### Pitfall 1: Signal Memory Leaks in C#
**What goes wrong:** Nodes connect to GameEvents signals in `_Ready()` but never disconnect. When the node is freed (QueueFree), the delegate reference keeps the managed object alive, or worse, the freed object's method gets invoked causing errors.
**Why it happens:** Godot's C# signal system has known issue #89116 where some signals don't auto-disconnect after QueueFree. Pure C# events have no auto-disconnect at all.
**How to avoid:** Use the SafeNode base class pattern. Subscribe in `_EnterTree()`, unsubscribe in `_ExitTree()`. Always mirror every `+=` with a `-=`.
**Warning signs:** "ObjectDisposedException" errors in console, increasing memory usage over time, errors when emitting signals after scene changes.

### Pitfall 2: GlobalClass File Name Mismatch
**What goes wrong:** Custom Resource class doesn't appear in the Godot Inspector's "New Resource" dropdown, or exported properties don't show.
**Why it happens:** Godot requires the C# file name to exactly match the class name (case-sensitive). `roomDefinition.cs` won't work for class `RoomDefinition`.
**How to avoid:** Always name files exactly as the class: `RoomDefinition.cs` for `RoomDefinition`, `WishTemplate.cs` for `WishTemplate`.
**Warning signs:** "Build the C# project" message in Inspector, class missing from resource creation dropdown.

### Pitfall 3: Autoload Instance Null Before _Ready
**What goes wrong:** Code tries to access `GameEvents.Instance` during static initialization or in another Autoload's constructor, getting NullReferenceException.
**Why it happens:** Autoload `_Ready()` hasn't been called yet. Godot initializes Autoloads in registration order, but the Instance property is set in `_Ready()`.
**How to avoid:** Only access `GameEvents.Instance` in `_EnterTree()`, `_Ready()`, or later lifecycle methods. Never in constructors or static initializers. Ensure GameEvents is registered first in the Autoload list.
**Warning signs:** NullReferenceException on `GameEvents.Instance` during startup.

### Pitfall 4: Camera Jitter from _Process vs _PhysicsProcess
**What goes wrong:** Camera movement stutters or jitters, especially at low frame rates.
**Why it happens:** Mixing `_Process()` (render frame) and `_PhysicsProcess()` (fixed tick) for camera updates, or not using delta time consistently.
**How to avoid:** Use `_Process()` exclusively for the camera (it's purely visual, not physics-driven). Always multiply velocities by `(float)delta`. Use `Mathf.Lerp` with `dt`-scaled interpolation for smooth transitions.
**Warning signs:** Camera stutters when frame rate dips, orbit feels different at 30fps vs 60fps.

### Pitfall 5: _ExitTree Not Called on Root Scene Node in C#
**What goes wrong:** Signal cleanup code in `_ExitTree()` doesn't execute when changing scenes.
**Why it happens:** Known Godot issue #68578 -- `_ExitTree` is not called on the main scene's root node after QueueFree in C#. `Dispose` is called instead.
**How to avoid:** For the main scene root node specifically, also implement `Dispose()` cleanup, or avoid connecting the root node directly to the signal bus. Sub-nodes of the main scene work correctly.
**Warning signs:** Stale signal connections after scene transitions.

### Pitfall 6: Exported Arrays in Resources Show as Null
**What goes wrong:** Exported arrays in custom Resources appear empty/null at runtime despite having values in the Inspector.
**Why it happens:** Known Godot issue #66907 / #70552 -- exported arrays and dictionaries in custom Resources don't always serialize correctly in C#.
**How to avoid:** Initialize arrays with defaults in the property declaration (`= System.Array.Empty<string>()`). Test that saved `.tres` files actually round-trip values. Consider using `Godot.Collections.Array<string>` instead of native C# arrays if issues persist.
**Warning signs:** Inspector shows values but runtime reads empty arrays.

## Code Examples

### Input Map Configuration
The orbital camera needs input actions defined in project.godot for keyboard orbit:

```ini
# Add to project.godot [input] section
[input]
orbit_left={
"deadzone": 0.2,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":65,"key_label":0,"unicode":97), Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":4194319,"key_label":0,"unicode":0)]
}
orbit_right={
"deadzone": 0.2,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":68,"key_label":0,"unicode":100), Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":4194321,"key_label":0,"unicode":0)]
}
```

**Note:** Input map entries are complex in project.godot raw format. It's simpler to add these via Project > Project Settings > Input Map in the Godot editor. The keys to map: A/Left Arrow = orbit_left, D/Right Arrow = orbit_right.

### Quick-Test Scene Structure
```
QuickTestScene (Node3D)
├── WorldEnvironment          # Adapted from Kenney Starter Kit
│   └── Environment resource  # Procedural sky, SSAO, bloom, tonemapping
├── DirectionalLight3D        # Sun light with shadows
├── PlaceholderRing (Node3D)  # Simple torus/cylinder mesh for camera target
│   ├── MeshInstance3D        # CSGTorus3D or imported low-poly ring
│   └── CollisionShape3D      # For future raycast testing
├── PlaceholderObject1 (MeshInstance3D)  # Box/sphere for scale reference
├── PlaceholderObject2 (MeshInstance3D)  # Another scale reference
└── CameraRig (Node3D)       # OrbitalCamera script attached
    └── Camera3D              # Offset at tilt angle
```

### Autoload Registration in project.godot
```ini
[autoload]
GameEvents="*res://Scripts/Autoloads/GameEvents.cs"
```

The `*` prefix means the Autoload is enabled. GameEvents must be the first Autoload registered so other systems can safely reference it.

### Environment Adaptation from Kenney Starter Kit
The WorldEnvironment should replicate these settings from the Kenney Starter Kit Basic Scene:
- **Sky:** ProceduralSkyMaterial (Godot built-in)
- **Tonemapping:** Filmic
- **SSAO:** Enabled, radius 0.3, intensity 0.5
- **Bloom:** Soft, low intensity
- **DirectionalLight3D:** Sun-like with soft shadows enabled

These values create a warm, well-lit 3D scene suitable for low-poly stylized art.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `[Signal] delegate` + `EmitSignal(string)` | C# events exposed as typed signal accessors (4.0+) | Godot 4.0 | Type-safe signal connections via `+=`/`-=` |
| `GetNode("/root/Singleton")` | Static `Instance` property in Autoload | Godot 4.0+ community convention | Type-safe singleton access without string paths |
| `export var` in GDScript | `[Export]` attribute + `[GlobalClass]` in C# | Godot 4.0 | Full Inspector integration for C# Resources |
| Resource `.tres` manual editing | `[ExportGroup]`/`[ExportSubgroup]` organization | Godot 4.0+ | Clean Inspector layout for complex Resources |
| Signals hidden with underscore prefix visible | Underscore signals hidden from autocomplete | Godot 4.6 | Cleaner signal discovery in editor |

**Deprecated/outdated:**
- `EmitSignal("signal_name")` string-based emission: still works but bypasses type safety. Use `?.Invoke()` for pure C# events or the generated typed signal accessor for Godot signals.
- `Connect("signal_name", target, "method_name")` string-based connection: replaced by `+=` operator and typed signal delegates.

## Open Questions

1. **Exact Kenney Starter Kit environment values**
   - What we know: The kit uses ProceduralSky, filmic tonemapping, SSAO (radius 0.3, intensity 0.5), and soft bloom. These values are from user decisions (CONTEXT.md).
   - What's unclear: Exact bloom intensity, sky color parameters, and DirectionalLight3D shadow settings would need to be extracted from the kit's `.tres` file.
   - Recommendation: Clone the Kenney Starter Kit repo, extract the WorldEnvironment and DirectionalLight3D settings, adapt into the project. The user's specified values (SSAO radius 0.3, intensity 0.5) take precedence over whatever the kit defaults to.

2. **Camera zoom range in Godot units**
   - What we know: Zoom from "segment-level close (2-3 segments fill screen)" to "whole-ring overview." Ring is a "tabletop miniature."
   - What's unclear: Exact Godot unit distances for min/max zoom depend on the ring geometry size, which is Phase 2 work.
   - Recommendation: Use placeholder values (ZoomMin=5, ZoomMax=25) that can be tuned once actual ring geometry exists. Export these as `[Export]` properties for easy adjustment.

3. **Idle orbit re-engagement**
   - What we know: Gentle slow idle orbit when player isn't interacting.
   - What's unclear: How should the idle orbit cease when the player starts interacting again? Should it blend out smoothly or stop immediately?
   - Recommendation: Reset idle timer on any input. The momentum system's lerp decay will naturally blend from idle speed to player-driven speed. No special transition logic needed.

## Sources

### Primary (HIGH confidence)
- [Godot Official Docs: C# Signals](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_signals.html) -- Signal declaration, connection, disconnection patterns
- [Godot Official Docs: C# Exported Properties](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_exports.html) -- [Export], [ExportGroup], [ExportSubgroup], [ExportCategory]
- [Godot Official Docs: C# Global Classes](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_global_classes.html) -- [GlobalClass] requirements, file naming
- [Godot Official Docs: Singletons/Autoload](https://docs.godotengine.org/en/stable/tutorials/scripting/singletons_autoload.html) -- Autoload registration, C# singleton pattern
- [Godot Official Docs: Scene Organization](https://docs.godotengine.org/en/stable/tutorials/best_practices/scene_organization.html) -- Scene structure best practices
- [Godot 4.6 Release Notes](https://godotengine.org/releases/4.6/) -- Current version features

### Secondary (MEDIUM confidence)
- [JetBrains Guide: Singletons and Autoloads with Godot and C#](https://www.jetbrains.com/guide/gamedev/tutorials/singletons-autoloads-godot-csharp/) -- Static Instance pattern
- [GDQuest: Events Bus Singleton](https://www.gdquest.com/tutorial/godot/design-patterns/event-bus-singleton/) -- Event bus architecture
- [Kenney Starter Kit Basic Scene](https://github.com/KenneyNL/Starter-Kit-Basic-Scene) -- Environment setup reference
- [GitHub: Godot4-Csharp-OrbitCamera](https://github.com/Siroro/Godot4-Csharp-OrbitCamera) -- C# orbit camera reference implementation
- [Godot Forum: Event Bus Pattern in C#](https://forum.godotengine.org/t/event-bus-pattern-in-c/104651) -- Community patterns and discussion

### Tertiary (LOW confidence)
- [GitHub Issue #89116](https://github.com/godotengine/godot/issues/89116) -- C# signals don't auto-disconnect after QueueFree (confirms SafeNode pattern necessity)
- [GitHub Issue #76690](https://github.com/godotengine/godot/issues/76690) -- IsConnected unreliable for custom C# signals (confirms pure C# events choice)
- [GitHub Issue #72994](https://github.com/godotengine/godot/issues/72994) -- Error disconnecting from signal event when not connected
- [GitHub Issue #68578](https://github.com/godotengine/godot/issues/68578) -- _ExitTree not called on main scene root node in C#
- [GitHub Issue #66907](https://github.com/godotengine/godot/issues/66907) -- Exported arrays in custom Resources show as null
- [GitHub Issue #102057](https://github.com/godotengine/godot/issues/102057) -- Generic base class with [GlobalClass] causes inspector errors

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- project already has Godot 4.6 C# configured, no new dependencies needed
- Architecture: HIGH -- signal bus, Resources, and camera pivot patterns are well-documented community standards with official docs backing
- Pitfalls: HIGH -- specific GitHub issues cited with reproduction details; SafeNode pattern directly addresses known engine bugs
- Code examples: MEDIUM -- based on synthesized patterns from multiple sources; specific API details verified against official docs but not executed in this Godot version

**Research date:** 2026-03-02
**Valid until:** 2026-04-01 (stable patterns, unlikely to change within 30 days)
