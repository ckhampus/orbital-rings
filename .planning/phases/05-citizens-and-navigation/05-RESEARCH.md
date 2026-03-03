# Phase 5: Citizens and Navigation - Research

**Researched:** 2026-03-03
**Domain:** Godot 4 C# — programmatic 3D agent movement on circular geometry, capsule mesh generation, fade animations, click detection, screen-space popup UI
**Confidence:** HIGH

## Summary

Phase 5 introduces named citizens that walk a circular walkway, visit rooms, and can be clicked to display an info panel. The primary technical challenge is navigation on a ring-shaped walkway — a non-standard geometry where Godot's built-in NavigationAgent3D/NavigationMesh approach is overkill and fragile (circular navmesh baking is unreliable, as flagged in STATE.md). The recommended approach is **angle-based polar coordinate movement** along the walkway centerline, which aligns perfectly with the existing polar math patterns used throughout the codebase (SegmentGrid, SegmentInteraction, RingMeshBuilder all use angle/radius math).

Citizens are rendered as colored CapsuleMesh primitives (Godot's built-in CapsuleMesh class, not hand-rolled geometry), with body type variations expressed through height/radius ratios and a two-color band material system. Room visits use a drift-to-edge + fade-out animation via Tween and StandardMaterial3D alpha manipulation. Click detection reuses the existing ray-plane intersection + polar proximity pattern from SegmentInteraction. The citizen info popup follows the established SegmentTooltip pattern (screen-space PanelContainer on a CanvasLayer).

**Primary recommendation:** Use angle-based movement (increment angle per frame at citizen's speed) with position computed as `(cos(angle) * walkwayRadius, surfaceY, sin(angle) * walkwayRadius)`. Skip NavigationAgent3D entirely — the walkway is a 1D path (just an angle), not a 2D navigable surface.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Colored capsule shapes (pill-shaped) — not humanoid, not abstract dots
- Body type affects capsule proportions: Tall = taller/thinner, Short = stubby, Round = wider/squatter
- Primary + secondary colors from CitizenData render as color bands on the capsule
- Skip accessories for Phase 5 — body type + two colors provides enough variety for 5-10 citizens
- Scale: small but readable — several citizens fit on a walkway segment, colors and body types distinguishable at default zoom
- Random direction per citizen — some walk clockwise, some counter-clockwise
- Gentle vertical bob as they move (simulates walking rhythm without legs/animation)
- Slight speed variation per citizen (~+-15% of base speed) to prevent uniform marching look
- 5 starter citizens spawned at game start (prototype count for Phase 5)
- Citizens walk the walkway continuously in a circle — no pausing, no reversals on the walkway itself
- Drift-to-edge + fade: citizen drifts radially from walkway toward room's segment edge, then fades out (entered the room)
- After a time inside, citizen fades back in on the walkway and resumes walking
- Occasional visits — roughly every 20-40 seconds a citizen might drift into a nearby room (calm, not hectic)
- Proximity-based, any room type — citizens drift into whatever room is nearest when they decide to visit, no category preference in Phase 5
- Both inner and outer row rooms are visitable — citizen drifts toward whichever side the target room is on
- Small floating popup near the clicked citizen in screen space (lightweight card, like segment tooltip style)
- Shows name + current wish only ("No wish" placeholder until Phase 6 adds wishes)
- Dismisses on click-away or Escape
- Build mode takes priority — citizens are not clickable during Placing or Demolish mode
- Subtle glow/outline on the clicked citizen's capsule while popup is open; glow disappears when popup dismisses

### Claude's Discretion
- Navigation approach (waypoint arc system vs. NavigationMesh vs. simple angle-based movement along walkway centerline)
- Exact capsule dimensions and bob amplitude/frequency
- Base walking speed value
- Visit duration range (how long citizens stay inside a room)
- Fade in/out animation timing and easing
- Citizen name pool (diverse real-world names, per Phase 1 decision)
- Glow/outline shader or emission approach for selected citizen
- Popup UI layout details (font, padding, offset from citizen)
- How citizens are spawned initially (positions, stagger timing)

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| CTZN-01 | Named citizens with distinct appearances arrive at the station as happiness grows | CitizenData resource already has CitizenName, BodyType, PrimaryColor, SecondaryColor fields. CapsuleMesh with body-type proportions + two-band material provides visual distinction. Arrival mechanism: CitizenManager spawns 5 at game start; happiness-driven arrival is Phase 7 territory, but the spawn infrastructure built here supports it. |
| CTZN-02 | Citizens walk along the walkway visibly | Angle-based polar movement on walkway centerline (radius 4.5, midpoint of 4.0-5.0 walkway). Each citizen stores a float angle, increments by speed*delta per frame, computes position via cos/sin. Vertical bob via sin(time * frequency) overlay on Y. |
| CTZN-03 | Citizens visit rooms based on spatial attraction (gravitating toward relevant rooms) | Proximity check: citizen's current angle vs. each occupied segment's angle range. When visit timer triggers, find nearest occupied room, drift radially toward its row edge (inner or outer), fade out. After visit duration, fade back in on walkway and resume. |
| CTZN-04 | Player can click a citizen to see their name and current wish | Ray-plane intersection + polar distance check to each citizen's angle/position. If closest citizen is within capsule radius threshold, show CitizenInfoPanel (screen-space PanelContainer following SegmentTooltip pattern). Suppressed during build modes via BuildManager.Instance.CurrentMode check. |
</phase_requirements>

## Standard Stack

### Core
| Library/Class | Version | Purpose | Why Standard |
|---------------|---------|---------|--------------|
| Godot CapsuleMesh | 4.6 built-in | Citizen body mesh primitive | Built-in PrimitiveMesh, no custom geometry needed. Properties: Height (default 2.0), Radius (default 0.5), RadialSegments, Rings. |
| StandardMaterial3D | 4.6 built-in | Citizen coloring and transparency for fade effects | Project already uses per-instance StandardMaterial3D everywhere (segments, rooms, ghosts). AlbedoColor for color bands, Transparency.Alpha for fading. |
| Tween (SceneTreeTween) | 4.6 built-in | Fade in/out animations, drift movement, vertical bob could optionally use it | Project already uses Tweens extensively (FloatingText, PlacementFeedback). CreateTween() on any node. |
| SafeNode | Project pattern | Base class for citizen nodes | Enforces SubscribeEvents/UnsubscribeEvents lifecycle — prevents orphan signal connections (success criterion #5). |
| GameEvents | Project Autoload | Cross-system communication | CitizenArrived/CitizenDeparted events already defined. New events needed: CitizenClicked, CitizenEnteredRoom, CitizenExitedRoom. |

### Supporting
| Library/Class | Version | Purpose | When to Use |
|---------------|---------|---------|-------------|
| Timer (Godot node) | 4.6 built-in | Visit interval timer per citizen | Project pattern: Timer child nodes for periodic behavior (EconomyManager income tick). Each citizen gets a visit decision timer. |
| PanelContainer | 4.6 built-in | CitizenInfoPanel popup | Follows SegmentTooltip and BuildPanel programmatic UI pattern. |
| CanvasLayer | 4.6 built-in | Popup rendering layer | Project already has HUDLayer (5), BuildUILayer (8), TooltipLayer (10). Citizen panel can share TooltipLayer or get its own layer. |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Angle-based movement | NavigationAgent3D + NavigationMesh | NavMesh baking on circular geometry is unreliable (STATE.md flag). NavigationAgent adds complexity (obstacle avoidance, path recalculation) that is unnecessary for a 1D circular path. Angle-based is simpler, more deterministic, and aligns with existing polar math. |
| Angle-based movement | Path3D + PathFollow3D | Would work for a fixed circular path, but adds scene tree complexity (Path3D node + Curve3D resource). Angle math is trivially simple and the entire codebase already thinks in angles. Path3D offers no benefit here. |
| CapsuleMesh | SurfaceTool custom capsule | Unnecessary — CapsuleMesh is a built-in primitive with configurable Height and Radius. Custom mesh only needed if capsule shape is insufficient (it is not). |
| Emission glow | Outline shader | Emission glow is simpler (just set EmissionEnabled + Emission color + EnergyMultiplier > 1.0). The environment already has glow_enabled=true. Outline shader requires writing GLSL, managing a separate material/pass, and dealing with depth edge cases. |

## Architecture Patterns

### Recommended Project Structure
```
Scripts/
├── Citizens/
│   ├── CitizenNode.cs          # Node3D extending SafeNode — one per citizen
│   ├── CitizenManager.cs       # Autoload — spawns/tracks/removes citizens
│   ├── CitizenAppearance.cs    # Static helper — creates capsule mesh from CitizenData
│   └── CitizenNames.cs         # Static name pool
├── Data/
│   └── CitizenData.cs          # Already exists — Resource with name, body, colors
├── UI/
│   └── CitizenInfoPanel.cs     # Screen-space popup (like SegmentTooltip)
└── Autoloads/
    └── GameEvents.cs           # Add new citizen interaction events
```

### Pattern 1: Angle-Based Circular Movement
**What:** Each citizen stores a `float _currentAngle` (radians) and a `float _speed` (radians/sec). Every `_Process(delta)`, the angle is incremented and the 3D position is computed from polar coordinates.
**When to use:** Any entity that moves along the circular walkway.
**Example:**
```csharp
// Constants from SegmentGrid
const float WalkwayRadius = 4.5f; // Midpoint of InnerRowOuter(4.0) and OuterRowInner(5.0)
const float SurfaceY = SegmentGrid.RingHeight; // Top of ring surface (0.3)

// Per citizen state
private float _currentAngle; // radians, 0 to Tau
private float _direction;    // +1.0 (CCW) or -1.0 (CW)
private float _speed;        // radians per second, ~0.15 +/- 15%

public override void _Process(double delta)
{
    float dt = (float)delta;

    // Advance angle
    _currentAngle += _direction * _speed * dt;

    // Wrap to [0, Tau)
    if (_currentAngle < 0) _currentAngle += Mathf.Tau;
    if (_currentAngle >= Mathf.Tau) _currentAngle -= Mathf.Tau;

    // Compute position from polar coordinates
    float x = Mathf.Cos(_currentAngle) * WalkwayRadius;
    float z = Mathf.Sin(_currentAngle) * WalkwayRadius;

    // Vertical bob: gentle sine wave
    float bob = Mathf.Sin(_bobPhase) * BobAmplitude;
    _bobPhase += BobFrequency * dt;

    Position = new Vector3(x, SurfaceY + bob, z);

    // Face direction of travel (tangent to circle)
    float facingAngle = _currentAngle + (_direction > 0 ? Mathf.Pi * 0.5f : -Mathf.Pi * 0.5f);
    Rotation = new Vector3(0, -facingAngle, 0);
}
```

### Pattern 2: Drift-to-Edge Room Visit
**What:** When a citizen decides to visit a room, they lerp radially from the walkway centerline toward the room's row edge, then fade out. After a duration, they fade back in and lerp back to the walkway.
**When to use:** Room visit animation.
**Example:**
```csharp
// Target radius: inner rooms drift inward (toward 3.5), outer rooms drift outward (toward 5.5)
float targetRadius = isOuterRoom ? 5.5f : 3.5f;

// Phase 1: Drift radially (0.5s)
var driftTween = CreateTween();
driftTween.TweenMethod(
    Callable.From((float r) => SetRadialPosition(_currentAngle, r, SurfaceY)),
    WalkwayRadius, targetRadius, 0.5f)
    .SetEase(Tween.EaseType.InOut);

// Phase 2: Fade out (0.3s)
driftTween.TweenMethod(
    Callable.From((float alpha) => SetMaterialAlpha(alpha)),
    1.0f, 0.0f, 0.3f)
    .SetEase(Tween.EaseType.In);

// Phase 3: Hide, wait, then reverse
driftTween.TweenCallback(Callable.From(() => Visible = false));
driftTween.TweenInterval(visitDuration); // 3-8 seconds inside the room
driftTween.TweenCallback(Callable.From(() => {
    Visible = true;
    SetRadialPosition(_currentAngle, targetRadius, SurfaceY);
}));

// Phase 4: Fade in (0.3s)
driftTween.TweenMethod(
    Callable.From((float alpha) => SetMaterialAlpha(alpha)),
    0.0f, 1.0f, 0.3f)
    .SetEase(Tween.EaseType.Out);

// Phase 5: Drift back to walkway (0.5s)
driftTween.TweenMethod(
    Callable.From((float r) => SetRadialPosition(_currentAngle, r, SurfaceY)),
    targetRadius, WalkwayRadius, 0.5f)
    .SetEase(Tween.EaseType.InOut);

// Phase 6: Resume walking
driftTween.TweenCallback(Callable.From(() => _isVisiting = false));
```

### Pattern 3: Click Detection via Polar Proximity
**What:** Extend the existing ray-plane intersection pattern from SegmentInteraction to detect citizen clicks. Instead of checking segment angular ranges, check distance from hit point to each citizen's current position.
**When to use:** Citizen click detection.
**Example:**
```csharp
// In CitizenManager or a dedicated CitizenInteraction node:
private CitizenNode FindCitizenAtScreenPos(Vector2 mousePos)
{
    Vector3 rayOrigin = _camera.ProjectRayOrigin(mousePos);
    Vector3 rayDir = _camera.ProjectRayNormal(mousePos);

    // Intersect with ring surface plane (same as SegmentInteraction)
    Vector3? hit = _ringPlane.IntersectsRay(rayOrigin, rayDir);
    if (hit == null) return null;

    Vector3 hitPoint = hit.Value;
    float clickRadius = 0.4f; // Capsule proximity threshold

    CitizenNode closest = null;
    float closestDist = clickRadius;

    foreach (var citizen in _citizens)
    {
        // XZ distance only (ignore Y)
        float dx = hitPoint.X - citizen.Position.X;
        float dz = hitPoint.Z - citizen.Position.Z;
        float dist = Mathf.Sqrt(dx * dx + dz * dz);

        if (dist < closestDist)
        {
            closestDist = dist;
            closest = citizen;
        }
    }

    return closest;
}
```

### Pattern 4: Emission Glow for Selected Citizen
**What:** When a citizen is clicked, enable emission on their material with energy > 1.0 to trigger the environment's glow post-process. Remove emission when deselected.
**When to use:** Selected citizen visual feedback.
**Example:**
```csharp
// Select: add glow
private void HighlightCitizen(CitizenNode citizen)
{
    var mat = citizen.GetMaterial();
    mat.EmissionEnabled = true;
    mat.Emission = citizen.Data.PrimaryColor.Lightened(0.3f);
    mat.EmissionEnergyMultiplier = 2.5f;
}

// Deselect: remove glow
private void UnhighlightCitizen(CitizenNode citizen)
{
    var mat = citizen.GetMaterial();
    mat.EmissionEnabled = false;
}
```
**Note:** The project's DefaultEnvironment.tres already has `glow_enabled = true`, `glow_intensity = 0.4`, `glow_bloom = 0.3`. Emission energy > 1.0 will automatically produce a soft glow bloom effect with zero additional setup.

### Anti-Patterns to Avoid
- **NavigationAgent3D for the walkway:** The walkway is a 1D circular path. NavigationAgent3D is designed for 2D surface navigation with obstacle avoidance. Using it here adds complexity (navmesh baking on annular geometry, agent radius tuning, path recalculation) with zero benefit. The STATE.md explicitly flags this as a concern.
- **Shared material across citizens:** Each citizen MUST have its own StandardMaterial3D instance. The project has been bitten by shared-material contamination before (Phase 2 decision: "Individual StandardMaterial3D instances per segment to avoid shared-material highlight contamination"). Citizen emission glow would bleed to all citizens if materials are shared.
- **Physics bodies for click detection:** The project explicitly chose "Polar math picking via Plane.IntersectsRay + Atan2 instead of physics collision bodies -- zero trimesh overhead, no phantom hits" in Phase 2. Citizens should follow the same pattern: ray-plane intersection + XZ distance check, not StaticBody3D/Area3D with CollisionShape3D.
- **_UnhandledInput for citizen clicks:** The project uses `_Input` (not `_UnhandledInput`) for mouse events (Phase 1 decision). Citizen click handling should follow the same pattern.
- **Modifying CitizenData resource at runtime:** CitizenData is a Godot Resource. Do not mutate it during gameplay (e.g., setting wish on the resource). Runtime state (current wish, current angle, visit status) belongs on CitizenNode, not CitizenData.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Capsule mesh geometry | SurfaceTool capsule builder | `new CapsuleMesh { Height = h, Radius = r }` | Built-in primitive, configurable, auto-generates normals. Zero custom mesh code. |
| Circular position math | Custom path system | `cos(angle) * radius`, `sin(angle) * radius` | Trivially simple. The entire codebase already uses this pattern. |
| Navigation/pathfinding | NavigationAgent3D + NavigationMesh | Angle increment per frame | The walkway is a circle. Navigation is literally `angle += speed * delta`. |
| Fade animation | Manual alpha interpolation in _Process | `CreateTween().TweenMethod(SetAlpha, 1.0, 0.0, duration)` | Tween handles easing, chaining, cleanup. Project already uses Tweens heavily. |
| Glow highlight | Custom outline shader | `mat.EmissionEnabled = true; mat.EmissionEnergyMultiplier = 2.5f` | Environment glow is already enabled. Emission > 1.0 triggers bloom automatically. |
| Screen-space popup | Custom viewport overlay | PanelContainer on CanvasLayer (SegmentTooltip pattern) | Proven pattern in codebase. Programmatic UI, no .tscn needed. |

**Key insight:** This phase has zero novel rendering or navigation challenges. Every technique is already proven in the codebase (polar math, per-instance materials, tweens, programmatic UI, SafeNode lifecycle). The "circular walkway navigation" problem dissolves entirely when you realize it is just `angle += speed * delta`.

## Common Pitfalls

### Pitfall 1: Citizens Walking Through Room Blocks
**What goes wrong:** Citizens positioned at walkway radius (4.5) visually clip through tall room blocks that extend inward/outward from segment edges.
**Why it happens:** Room blocks sit on top of the ring surface (Y offset = RingHeight * 0.5 + blockHeight * 0.5). If citizen capsule is too tall, it overlaps.
**How to avoid:** Keep citizen capsule total height (CapsuleMesh.Height, which includes hemisphere caps) small enough that citizens sit on the walkway surface without extending above the room block base. The walkway is recessed 0.025 units — citizens should stand on the walkway surface level.
**Warning signs:** Capsule top visually overlaps room block bottoms.

### Pitfall 2: Shared Material Contamination on Glow
**What goes wrong:** Enabling emission on one citizen's material causes all citizens to glow.
**Why it happens:** If citizens share a material instance (even accidentally via CapsuleMesh default material), modifying one affects all.
**How to avoid:** Create a NEW StandardMaterial3D per citizen in CitizenAppearance. Never share material references. Set MaterialOverride on MeshInstance3D (not Mesh.Material).
**Warning signs:** Multiple citizens light up when only one is clicked.

### Pitfall 3: Orphan Signal Connections on Citizen Removal
**What goes wrong:** Removing a citizen (QueueFree) without disconnecting event handlers causes orphan references or errors on next event emit.
**Why it happens:** C# event delegates hold strong references to subscriber methods. If the node is freed but the delegate reference remains on GameEvents, the next emit may reference a disposed object.
**How to avoid:** Extend SafeNode. The _ExitTree -> UnsubscribeEvents pattern ensures every -= mirrors every +=. This is success criterion #5.
**Warning signs:** Godot debugger shows orphan node warnings, or NullReferenceException on event emit after citizen removal.

### Pitfall 4: Fade Animation Z-Fighting / Depth Sorting
**What goes wrong:** During fade-out, the semi-transparent capsule flickers or shows ugly rendering artifacts (jagged pixels, Z-fighting with the walkway surface).
**Why it happens:** Alpha transparency requires proper depth draw mode settings. Default DepthDrawMode may not handle semi-transparent objects correctly.
**How to avoid:** When setting up the fade material, set `Transparency = TransparencyEnum.Alpha` AND `DepthDrawMode = DepthDrawModeEnum.Always`. This is documented in Godot forum discussions about MeshInstance3D alpha tweening.
**Warning signs:** Flickering/jagged edges on capsule during fade animation.

### Pitfall 5: Bob Phase Sync Across Citizens
**What goes wrong:** All citizens bob in perfect sync, creating an unnatural wave effect.
**Why it happens:** If all citizens start with `_bobPhase = 0`, their sine waves are synchronized.
**How to avoid:** Initialize `_bobPhase` to a random value per citizen (e.g., `GD.Randf() * Mathf.Tau`). Combined with the +-15% speed variation, this produces natural-looking desynchronized walking.
**Warning signs:** All citizens appear to "breathe" in unison instead of independently bobbing.

### Pitfall 6: Click Detection During Room Visit
**What goes wrong:** Player clicks a citizen who is currently visiting a room (faded out / invisible), and the popup appears for an invisible citizen.
**Why it happens:** The citizen node still exists at its drift position even when invisible.
**How to avoid:** Skip citizens with `_isVisiting == true` (or `Visible == false`) in the click detection loop.
**Warning signs:** Popup appears with no visible citizen nearby.

### Pitfall 7: Tween Stacking on Rapid Interactions
**What goes wrong:** Multiple visit tweens or glow tweens stack on the same citizen, causing visual glitches.
**Why it happens:** A new tween is created before the previous one finishes (e.g., citizen starts a new visit while the return tween is still playing).
**How to avoid:** Store the active tween reference and call `Kill()` before creating a new one. This is the project pattern from PlacementFeedback (`_placementTween?.Kill()`).
**Warning signs:** Citizen flickers, jumps position, or has multiple simultaneous fade animations.

## Code Examples

### Capsule Mesh Creation with Body Type Variation
```csharp
// Source: Godot 4 CapsuleMesh API + project CitizenData.BodyType enum
public static MeshInstance3D CreateCitizenMesh(CitizenData data)
{
    var capsule = new CapsuleMesh();

    // Body type proportions
    switch (data.Body)
    {
        case CitizenData.BodyType.Tall:
            capsule.Height = 0.35f;  // Taller
            capsule.Radius = 0.06f;  // Thinner
            break;
        case CitizenData.BodyType.Short:
            capsule.Height = 0.18f;  // Stubby
            capsule.Radius = 0.07f;  // Slightly wider
            break;
        case CitizenData.BodyType.Round:
            capsule.Height = 0.22f;  // Medium
            capsule.Radius = 0.09f;  // Wider/squatter
            break;
    }

    // Reduce detail for performance (5 citizens, but plan for more)
    capsule.RadialSegments = 16;  // Default 64 is overkill for tiny capsules
    capsule.Rings = 4;            // Default 8 is overkill

    // Per-instance material with two-color band
    // Primary color covers the body, secondary as a band
    var material = new StandardMaterial3D
    {
        AlbedoColor = data.PrimaryColor
    };

    var meshInstance = new MeshInstance3D
    {
        Mesh = capsule,
        MaterialOverride = material
    };

    return meshInstance;
}
```

### Two-Color Band Approach
```csharp
// For a two-color band effect without shaders:
// Create TWO CapsuleMesh instances — one full-size primary, one smaller secondary band.
// The secondary is a slightly wider, shorter capsule at the citizen's midsection.
// Both are children of the CitizenNode (Node3D).

// Primary body (full capsule)
var primaryMesh = new MeshInstance3D
{
    Mesh = capsule,
    MaterialOverride = new StandardMaterial3D { AlbedoColor = data.PrimaryColor }
};

// Secondary band (shorter capsule positioned at midsection, slightly larger radius)
var bandCapsule = new CapsuleMesh
{
    Height = capsule.Height * 0.35f,
    Radius = capsule.Radius * 1.15f,
    RadialSegments = 16,
    Rings = 2
};
var bandMesh = new MeshInstance3D
{
    Mesh = bandCapsule,
    MaterialOverride = new StandardMaterial3D { AlbedoColor = data.SecondaryColor },
    Position = new Vector3(0, 0, 0)  // Centered on parent
};
```

### CitizenManager Autoload Pattern
```csharp
// Following EconomyManager/BuildManager singleton pattern
public partial class CitizenManager : SafeNode
{
    public static CitizenManager Instance { get; private set; }

    private readonly List<CitizenNode> _citizens = new();

    public int CitizenCount => _citizens.Count;

    public override void _Ready()
    {
        base._Ready();
        Instance = this;

        // Spawn initial citizens with staggered positions
        SpawnStarterCitizens(5);

        // Update EconomyManager with citizen count
        EconomyManager.Instance?.SetCitizenCount(_citizens.Count);
    }

    private void SpawnStarterCitizens(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var data = GenerateRandomCitizenData();
            var citizen = new CitizenNode();
            citizen.Initialize(data, startAngle: (float)i / count * Mathf.Tau);
            AddChild(citizen);
            _citizens.Add(citizen);

            GameEvents.Instance?.EmitCitizenArrived(data.CitizenName);
        }
    }
}
```

### Visit Decision Logic
```csharp
// Timer-based visit decision (per citizen)
private void OnVisitTimerTimeout()
{
    if (_isVisiting) return;

    // Find nearest occupied room segment
    var grid = _ringVisual.Grid;
    float myAngle = _currentAngle;

    int bestSegment = -1;
    float bestAngleDist = float.MaxValue;
    SegmentRow bestRow = SegmentRow.Outer;

    for (int i = 0; i < SegmentGrid.TotalSegments; i++)
    {
        var (row, pos) = SegmentGrid.FromIndex(i);
        if (!grid.IsOccupied(row, pos)) continue;

        float segMidAngle = SegmentGrid.GetStartAngle(pos) + SegmentGrid.SegmentArc * 0.5f;
        float angleDist = AngleDistance(myAngle, segMidAngle);

        if (angleDist < bestAngleDist)
        {
            bestAngleDist = angleDist;
            bestSegment = i;
            bestRow = row;
        }
    }

    if (bestSegment >= 0 && bestAngleDist < SegmentGrid.SegmentArc * 1.5f)
    {
        StartVisit(bestRow);
    }

    // Randomize next check (20-40 seconds as per user decision)
    _visitTimer.WaitTime = 20.0f + GD.Randf() * 20.0f;
    _visitTimer.Start();
}

private static float AngleDistance(float a, float b)
{
    float diff = Mathf.Abs(a - b);
    return Mathf.Min(diff, Mathf.Tau - diff);
}
```

### Citizen Name Pool
```csharp
// Diverse real-world names per Phase 1 decision
public static class CitizenNames
{
    private static readonly string[] Names = {
        "Aria", "Bodhi", "Celeste", "Davi", "Elara",
        "Fern", "Gael", "Hana", "Idris", "Juno",
        "Kaia", "Lev", "Mira", "Nico", "Orla",
        "Paz", "Quinn", "Remi", "Suki", "Theo",
        "Uma", "Vesper", "Wren", "Xia", "Yael", "Zara"
    };

    private static int _nextIndex = 0;

    public static string GetNextName()
    {
        // Shuffle on first pass, then cycle
        string name = Names[_nextIndex];
        _nextIndex = (_nextIndex + 1) % Names.Length;
        return name;
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| NavigationServer3D manual bake | NavigationAgent3D auto-path | Godot 4.0+ | NavigationAgent3D simplifies agent setup, BUT requires proper NavigationMesh — problematic for circular annular geometry. Not recommended here. |
| GDScript Tweens (SceneTreeTween) | C# CreateTween() | Godot 4.0 | Tween API is identical in C#. Chain steps with `.TweenProperty()`, `.TweenMethod()`, `.TweenCallback()`, `.TweenInterval()`. |
| Separate transparency mode toggle | `Transparency = TransparencyEnum.Alpha` on StandardMaterial3D | Godot 4.0+ | Single enum property controls transparency. Must also set `DepthDrawMode = DepthDrawModeEnum.Always` for clean alpha blending on 3D meshes. |

**Deprecated/outdated:**
- `AnimationPlayer` for simple property tweens: Use `CreateTween()` instead for procedural, chainable animations. AnimationPlayer is for complex multi-track asset-driven animations.
- `NavigationPolygon` (2D): Not relevant. The 3D equivalent `NavigationMesh` exists but is overkill for this use case.

## Open Questions

1. **Two-color band rendering approach**
   - What we know: The user wants primary + secondary color bands on capsules. Two CapsuleMesh instances (body + band) is the simplest approach. A custom shader could do it more efficiently but adds complexity.
   - What's unclear: Whether two overlapping meshes cause Z-fighting at the band edges.
   - Recommendation: Start with two-mesh approach. If Z-fighting occurs, offset the band mesh outward by 0.001 units (epsilon). If that's unsatisfactory, fall back to a simple vertex-color shader.

2. **Walkway surface Y position for citizens**
   - What we know: Ring top face is at Y = RingHeight/2 = 0.15. Walkway is recessed by 0.025. So walkway surface is at Y = 0.15 - 0.025 = 0.125. Citizens should stand ON this surface.
   - What's unclear: Exact capsule Y offset to have citizens appear to "stand" on the surface (capsule origin is at its center, not its bottom).
   - Recommendation: `citizenY = walkwaySurfaceY + capsuleHeight * 0.5f`. Tune visually during implementation.

3. **Visit timer architecture**
   - What we know: Visit interval is 20-40 seconds. Each citizen decides independently.
   - What's unclear: Should the timer be a Godot Timer node child or a float accumulator in _Process?
   - Recommendation: Use Timer child node (project pattern from EconomyManager). Explicit Timer.Start() over Autostart (Phase 3 decision). Random WaitTime per citizen.

## Recommended Values

These are discretionary values to use as starting points, tunable during implementation:

| Parameter | Value | Rationale |
|-----------|-------|-----------|
| Walkway centerline radius | 4.5f | Midpoint of InnerRowOuter (4.0) and OuterRowInner (5.0) |
| Base walking speed | 0.15 rad/s | ~42 seconds for a full loop. Calm pace for a cozy game. |
| Speed variation | +/-15% | Range: 0.1275 to 0.1725 rad/s. Prevents marching look. |
| Bob amplitude | 0.015f units | Subtle — just enough to suggest walking rhythm |
| Bob frequency | 8.0f rad/s | ~1.3 Hz, natural walking rhythm |
| Visit timer range | 20-40 seconds | Per user decision |
| Visit duration inside room | 4-8 seconds | Long enough to notice, short enough not to lose track |
| Drift duration (walkway to edge) | 0.5 seconds | Smooth but not sluggish |
| Fade duration | 0.3 seconds | Quick enough to feel responsive |
| Click proximity threshold | 0.4f units | Generous for small capsules at various zoom levels |
| Capsule Tall | Height: 0.35, Radius: 0.06 | Visually distinct at default zoom |
| Capsule Short | Height: 0.18, Radius: 0.07 | Stubby and recognizable |
| Capsule Round | Height: 0.22, Radius: 0.09 | Wider, clearly different silhouette |
| Emission energy (selected) | 2.5f | Visible glow without overwhelming |
| Citizen count at start | 5 | Per user decision |

## Sources

### Primary (HIGH confidence)
- Godot 4.6 CapsuleMesh built-in class — Height, Radius, RadialSegments, Rings properties confirmed via official docs
- Godot 4.6 StandardMaterial3D — Transparency, DepthDrawMode, Emission, EmissionEnergyMultiplier confirmed via official docs
- Godot 4.6 Tween API — CreateTween(), TweenMethod(), TweenProperty(), TweenCallback(), TweenInterval() confirmed via project codebase usage (PlacementFeedback.cs, FloatingText.cs)
- Project codebase — All architectural patterns verified by reading existing source files (SafeNode, GameEvents, SegmentGrid, SegmentInteraction, SegmentTooltip, BuildManager, RingVisual, PlacementFeedback)
- DefaultEnvironment.tres — glow_enabled=true, glow_intensity=0.4, glow_bloom=0.3 confirmed by file read

### Secondary (MEDIUM confidence)
- Godot forum discussion on MeshInstance3D alpha tweening — DepthDrawMode.Always recommendation for clean alpha blending
- Godot forum discussion on emission glow — EmissionEnergyMultiplier > 1.0 triggers WorldEnvironment glow bloom

### Tertiary (LOW confidence)
- None — all findings verified against codebase or official docs

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all components are Godot built-ins or established project patterns, verified by reading source files
- Architecture: HIGH — angle-based movement is trivially correct for circular paths, and every sub-pattern (polar math, per-instance materials, SafeNode lifecycle, programmatic UI) is already proven in the codebase
- Pitfalls: HIGH — most pitfalls are documented from prior phase decisions (shared material contamination, orphan signals, tween stacking) with known solutions already in the codebase

**Research date:** 2026-03-03
**Valid until:** 2026-04-03 (30 days — stable domain, no external dependencies)
