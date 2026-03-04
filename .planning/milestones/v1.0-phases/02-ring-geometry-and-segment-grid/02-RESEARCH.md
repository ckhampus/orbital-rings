# Phase 2: Ring Geometry and Segment Grid - Research

**Researched:** 2026-03-02
**Domain:** Godot 4.6 procedural 3D mesh generation, polar-coordinate mouse picking, screen-space UI
**Confidence:** HIGH

## Summary

Phase 2 replaces the CSG placeholder ring with 24 individual procedural MeshInstance3D segments (12 outer, 12 inner) plus a walkway annulus, implements polar-math mouse-to-segment detection without physics collision, and adds hover/selection visual feedback with a screen-space tooltip. The core technical challenge is generating annular sector meshes procedurally via SurfaceTool, then mapping 2D mouse coordinates to ring segments using a camera ray-to-ground-plane intersection followed by Cartesian-to-polar conversion.

All of this is well-supported by Godot 4.6's built-in APIs. SurfaceTool provides a clean vertex-by-vertex mesh builder. Camera3D.ProjectRayOrigin/ProjectRayNormal combined with Plane.IntersectsRay gives physics-free mouse-to-world mapping. Atan2-based polar math converts world XZ coordinates to segment indices. StandardMaterial3D with per-segment AlbedoColor handles the pastel color scheme and highlight states.

**Primary recommendation:** Use SurfaceTool to generate each segment as an independent ArrayMesh committed to its own MeshInstance3D. Use Plane.IntersectsRay (Y=0.15 plane) for mouse-to-world, then Mathf.Atan2 for angle and distance for row. No physics bodies needed for hover/selection.

<user_constraints>

## User Constraints (from CONTEXT.md)

### Locked Decisions

**Ring Proportions:**
- Keep overall outer radius ~6 Godot units (fits camera zoom range 5-25)
- Thin disc height ~0.3 units (board game piece feel, not chunky platform)
- Equal thirds allocation: outer row, walkway, and inner row each get roughly equal radial width
- Both rows use 30 degrees per segment (12 segments each) -- outer segments are physically wider due to larger radius
- Perfectly flat top surface (no curvature)
- Static ring (no rotation, camera moves around it)

**Mesh Approach:**
- Each segment is its own MeshInstance3D (24 total segment meshes)
- Individual pieces enable easy per-segment highlighting, coloring, and future animation
- CSG placeholder ring is replaced entirely by the segmented mesh

**Segment Boundaries:**
- Color alternation between adjacent segments (no physical gaps or etched lines)
- Subtle shade variation between neighboring segments within the same row
- Outer row uses one pastel base color (e.g., soft pink), inner row uses a different pastel base color (e.g., soft lavender)
- Inner vs. outer row distinction is immediately readable at a glance via different base colors

**Hover and Selection:**
- Hover: brightness boost on the hovered segment (lighter/brighter version of its base color)
- Selection (click): stronger brightness boost plus a soft accent color shift -- clearly distinct from hover state
- Two-state feedback: hover = subtle brightening, selected = obvious brightening + accent
- Left-click selects; Escape deselects (from Phase 1 input philosophy)

**Position Indicators:**
- Clock positions 1-12, shared by both inner and outer rows
- Labeled as "Outer 3", "Inner 7" etc. to distinguish row
- Numbers appear on hover only (not permanently visible on the surface)
- Screen-space tooltip near the cursor (not 3D text on the ring surface)
- Tooltip shows position + status: "Outer 3 -- Empty" or "Inner 7 -- Occupied"

**Walkway:**
- Continuous unbroken circular strip (not segmented into 12 sections)
- Clearly different color from the room rows (e.g., soft grey or warm beige -- reads as "path")
- Very subtle recess depth (~0.02-0.03 units below row surfaces)
- Passive element -- no mouse interaction (hover/click) in this phase
- Structurally ready for citizen pathfinding in Phase 5

### Claude's Discretion

- Exact pastel colors for outer row, inner row, and walkway (within warm pastel palette)
- Segment mesh generation approach (procedural in code vs. pre-authored)
- Polar math implementation for mouse-to-segment mapping
- Hover brightness multiplier and selection accent color values
- Tooltip UI implementation details (font, size, offset from cursor)
- Camera zoom threshold for showing segment numbers (if zoom-based visibility is added)

### Deferred Ideas (OUT OF SCOPE)

None -- discussion stayed within phase scope

</user_constraints>

<phase_requirements>

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| RING-01 | Player sees a flat donut ring divided into 12 outer and 12 inner segments with a walkway between them | SurfaceTool procedural mesh generation for annular sectors; 24 MeshInstance3D nodes plus 1 walkway mesh; ring proportions (outer=6, inner=3, equal thirds) |
| RING-03 | Segments are visually distinct (numbered positions, inner vs outer clearly readable) | Per-segment StandardMaterial3D with pastel color alternation; screen-space tooltip via CanvasLayer + Label; polar math for segment identification on hover |

</phase_requirements>

## Standard Stack

### Core

| Library/API | Version | Purpose | Why Standard |
|-------------|---------|---------|--------------|
| Godot.SurfaceTool | 4.6.1 (.NET SDK) | Procedural mesh generation vertex-by-vertex | Official Godot API for building meshes in code; handles normals, colors, indices; cleaner API than raw ArrayMesh |
| Godot.MeshInstance3D | 4.6.1 | Displays generated mesh in scene tree | Standard node for rendering meshes; one per segment enables independent material/visibility |
| Godot.StandardMaterial3D | 4.6.1 | Per-segment coloring and highlight states | Built-in PBR material; AlbedoColor for base color, programmatic color changes for hover/select |
| Godot.Plane | 4.6.1 | Ray-plane intersection for mouse picking | Built-in struct with IntersectsRay() -- no physics needed |
| Godot.Camera3D | 4.6.1 | ProjectRayOrigin/ProjectRayNormal for mouse-to-world | Built-in methods on Camera3D for screen-to-world ray casting |
| System.MathF / Godot.Mathf | .NET 8 / 4.6.1 | Atan2, polar coordinate math | Standard math for angle/distance calculations |
| Godot.CanvasLayer + Label | 4.6.1 | Screen-space tooltip overlay | Standard Godot UI layer for HUD elements above 3D scene |

### Supporting

| Library/API | Version | Purpose | When to Use |
|-------------|---------|---------|-------------|
| Godot.InputEventMouseMotion | 4.6.1 | Track mouse position for hover detection | In _Input() to get current mouse screen position |
| Godot.InputEventMouseButton | 4.6.1 | Detect left-click for segment selection | In _Input() to handle click events |
| GameEvents (project autoload) | Custom | Cross-system event bus for segment events | When other systems need to react to segment hover/selection |
| SafeNode (project base class) | Custom | Event lifecycle management | Base class for ring/segment manager nodes |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| SurfaceTool | Raw ArrayMesh arrays | Slightly faster generation but much harder API; not worth it for 25 meshes |
| SurfaceTool | ImmediateMesh | ImmediateMesh regenerates every frame (designed for debug lines); wrong tool for static geometry |
| StandardMaterial3D per segment | ShaderMaterial with uniform | More flexible but overkill; StandardMaterial3D.AlbedoColor is sufficient for solid colors |
| Plane.IntersectsRay | PhysicsDirectSpaceState.IntersectRay | Requires collision shapes on every segment; decision explicitly says "not trimesh collision" |
| CanvasLayer tooltip | Control._MakeCustomTooltip | Built-in tooltip has delay and anchors to Control nodes, not 3D world positions; custom is better here |

## Architecture Patterns

### Recommended Project Structure

```
Scripts/
  Ring/
    SegmentGrid.cs          # Data model: 24 segment slots, occupancy tracking
    RingMeshBuilder.cs      # Static helper: generates annular sector ArrayMesh
    RingVisual.cs           # Node3D: owns 24 MeshInstance3D children + walkway
    SegmentInteraction.cs   # Handles mouse hover/click via polar math
  UI/
    SegmentTooltip.cs       # CanvasLayer + Label: shows "Outer 3 -- Empty"
Scenes/
  Ring/
    Ring.tscn               # Scene with RingVisual + SegmentInteraction nodes
```

### Pattern 1: Annular Sector Mesh Generation with SurfaceTool

**What:** Generate a single ring segment (annular sector) as a closed 3D shape using SurfaceTool.
**When to use:** For each of the 24 segment meshes and the walkway mesh.
**Example:**

```csharp
// Generates an annular sector mesh (a "pizza slice of a donut")
// Parameters: inner radius, outer radius, start angle, end angle, height
public static ArrayMesh CreateAnnularSector(
    float innerRadius, float outerRadius,
    float startAngleRad, float endAngleRad,
    float height, int arcSegments = 4)
{
    var st = new SurfaceTool();
    st.Begin(Mesh.PrimitiveType.Triangles);

    // Generate vertices for top face, bottom face, and 4 side faces
    // Top face: fan of triangles between inner and outer arcs
    for (int i = 0; i < arcSegments; i++)
    {
        float t0 = (float)i / arcSegments;
        float t1 = (float)(i + 1) / arcSegments;
        float a0 = Mathf.Lerp(startAngleRad, endAngleRad, t0);
        float a1 = Mathf.Lerp(startAngleRad, endAngleRad, t1);

        // Four corners of this quad on the top face
        Vector3 outerA = new(outerRadius * Mathf.Cos(a0), height, outerRadius * Mathf.Sin(a0));
        Vector3 outerB = new(outerRadius * Mathf.Cos(a1), height, outerRadius * Mathf.Sin(a1));
        Vector3 innerA = new(innerRadius * Mathf.Cos(a0), height, innerRadius * Mathf.Sin(a0));
        Vector3 innerB = new(innerRadius * Mathf.Cos(a1), height, innerRadius * Mathf.Sin(a1));

        // Top face normal = UP
        st.SetNormal(Vector3.Up);
        st.AddVertex(innerA);
        st.SetNormal(Vector3.Up);
        st.AddVertex(outerA);
        st.SetNormal(Vector3.Up);
        st.AddVertex(outerB);

        st.SetNormal(Vector3.Up);
        st.AddVertex(innerA);
        st.SetNormal(Vector3.Up);
        st.AddVertex(outerB);
        st.SetNormal(Vector3.Up);
        st.AddVertex(innerB);
    }

    // Bottom face, outer edge, inner edge, two radial sides...
    // (similar quad generation with appropriate normals)

    return st.Commit();
}
```

**Key insight:** Each segment needs `arcSegments` subdivisions (4 is plenty for 30-degree arcs) to look curved rather than flat-sided. The top face is the only one visible at typical camera angles, so side faces can be minimal.

### Pattern 2: Polar Math Mouse-to-Segment Mapping

**What:** Convert mouse screen position to a segment index using ray-plane intersection + polar coordinates.
**When to use:** Every frame (or on mouse move) to determine which segment the cursor is over.
**Example:**

```csharp
// Ring geometry constants
const float OuterRadius = 6.0f;
const float InnerRadius = 3.0f;
const float WalkwayInnerEdge = 4.0f;  // inner row outer edge
const float WalkwayOuterEdge = 5.0f;  // outer row inner edge
const int SegmentsPerRow = 12;
const float SegmentArcRad = Mathf.Tau / SegmentsPerRow; // 30 degrees

// The ring surface plane (Y = half the ring height)
private readonly Plane _ringPlane = new(Vector3.Up, 0.15f);

public (int segmentIndex, bool isOuter)? GetSegmentUnderMouse(Vector2 mousePos, Camera3D camera)
{
    // Step 1: Cast ray from camera through mouse position
    Vector3 rayOrigin = camera.ProjectRayOrigin(mousePos);
    Vector3 rayDir = camera.ProjectRayNormal(mousePos);

    // Step 2: Intersect with the ring's horizontal plane
    Vector3? hitPoint = _ringPlane.IntersectsRay(rayOrigin, rayDir);
    if (hitPoint is null) return null;

    Vector3 hit = hitPoint.Value;

    // Step 3: Convert to polar coordinates (XZ plane)
    float distance = Mathf.Sqrt(hit.X * hit.X + hit.Z * hit.Z);
    float angle = Mathf.Atan2(hit.Z, hit.X); // radians, -PI to PI

    // Step 4: Determine row (inner vs outer vs walkway vs outside)
    bool isOuter;
    if (distance >= WalkwayOuterEdge && distance <= OuterRadius)
        isOuter = true;
    else if (distance >= InnerRadius && distance <= WalkwayInnerEdge)
        isOuter = false;
    else
        return null; // In walkway, inside inner, or outside outer

    // Step 5: Convert angle to segment index (clock positions 1-12)
    // Normalize angle to 0..TAU range
    if (angle < 0) angle += Mathf.Tau;
    // Offset so segment 0 starts at "12 o'clock" (positive Z axis)
    float offsetAngle = (angle + Mathf.Pi / 2) % Mathf.Tau;
    int segIndex = (int)(offsetAngle / SegmentArcRad) % SegmentsPerRow;

    return (segIndex, isOuter);
}
```

**Key insight:** The angle offset determines where "position 1" starts on the clock. Adjusting the `+ Mathf.Pi / 2` term rotates the numbering system. The walkway gap between WalkwayInnerEdge and WalkwayOuterEdge returns null (no selection).

### Pattern 3: Material-Based Hover/Select Feedback

**What:** Change segment appearance by swapping or modifying StandardMaterial3D.AlbedoColor.
**When to use:** When hover state or selection state changes.
**Example:**

```csharp
// Each segment stores references to its MeshInstance3D and materials
private StandardMaterial3D _baseMaterial;
private StandardMaterial3D _hoverMaterial;
private StandardMaterial3D _selectedMaterial;

public void SetState(SegmentState state)
{
    _meshInstance.MaterialOverride = state switch
    {
        SegmentState.Normal => _baseMaterial,
        SegmentState.Hovered => _hoverMaterial,
        SegmentState.Selected => _selectedMaterial,
        _ => _baseMaterial
    };
}
```

**Important:** Each segment needs its own material instances (not shared). Use `MaterialOverride` on MeshInstance3D rather than modifying the mesh's surface material, so the mesh resource stays shared if desired.

### Pattern 4: Screen-Space Tooltip via CanvasLayer

**What:** A CanvasLayer with a Label (or PanelContainer + Label) that follows the mouse position.
**When to use:** When hovering over a segment to show "Outer 3 -- Empty".
**Example:**

```csharp
// SegmentTooltip.cs -- child of a CanvasLayer node
public partial class SegmentTooltip : PanelContainer
{
    private Label _label;
    private static readonly Vector2 CursorOffset = new(16, 16);

    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
        Visible = false;
        // MouseFilter = Ignore so tooltip doesn't block clicks
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void Show(string text, Vector2 mousePos)
    {
        _label.Text = text;
        Visible = true;
        // Position with offset, clamped to viewport
        var viewport = GetViewport().GetVisibleRect().Size;
        var pos = mousePos + CursorOffset;
        // Clamp to keep tooltip on screen
        pos.X = Mathf.Min(pos.X, viewport.X - Size.X);
        pos.Y = Mathf.Min(pos.Y, viewport.Y - Size.Y);
        GlobalPosition = pos;
    }

    public void Hide()
    {
        Visible = false;
    }
}
```

### Anti-Patterns to Avoid

- **Trimesh collision for segment picking:** Creates phantom hits on inner faces of the donut, is fragile with procedural meshes, and adds unnecessary physics overhead. Polar math is both more reliable and more performant.
- **Shared material instances:** If multiple segments share the same StandardMaterial3D instance, changing the color of one changes all of them. Always create individual material instances per segment.
- **Regenerating meshes for state changes:** The meshes are static geometry. Only the material needs to change for hover/select. Never rebuild the ArrayMesh just to change color.
- **Using _Process for hover when _Input suffices:** Mouse motion events via _Input(InputEventMouseMotion) are more efficient than polling GetGlobalMousePosition() in _Process. However, since the camera orbits continuously, _Process polling may actually be needed to update which segment is under the (stationary) mouse when the camera moves. Consider hybrid: update on mouse move AND on camera orbit.
- **GridMap for ring segments:** GridMap is designed for rectangular grid tiles, not polar/annular segments. The decision to use custom polar math is correct.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Annular sector mesh | Manual vertex array packing | SurfaceTool with Begin/AddVertex/Commit | SurfaceTool handles index generation, normal computation, and format packing |
| Ray-plane intersection | Custom line-plane math | Plane.IntersectsRay() | Built-in, handles edge cases (parallel rays, behind-camera) |
| Screen-to-world ray | Manual projection matrix math | Camera3D.ProjectRayOrigin/ProjectRayNormal | Built-in, accounts for FOV, aspect ratio, near/far planes |
| Angle normalization | Custom modulo with negative handling | `if (angle < 0) angle += Mathf.Tau` | Simple one-liner, but easy to get wrong with custom wrappers |
| Tooltip viewport clamping | Manual edge detection | Clamp position against GetViewport().GetVisibleRect().Size | Standard pattern, but must account for tooltip's own size |

**Key insight:** The "hard parts" of this phase (mesh generation, ray casting, polar math) are all well-served by Godot's built-in API. The custom code is glue logic connecting these APIs.

## Common Pitfalls

### Pitfall 1: Winding Order and Face Culling
**What goes wrong:** Procedural mesh faces are invisible because vertices are wound clockwise instead of counter-clockwise (or vice versa).
**Why it happens:** Godot uses counter-clockwise winding for front faces by default. If you define triangle vertices in clockwise order, the face points away from the camera and gets culled.
**How to avoid:** Use consistent CCW winding for all outward-facing triangles. For the top face (visible from above), vertices should go CCW when viewed from above (positive Y). Test with `CullMode = Disabled` during development, then fix winding.
**Warning signs:** Mesh appears in wireframe view but not in shaded view; mesh is visible from below but not above.

### Pitfall 2: Camera Orbit Invalidates Hover State
**What goes wrong:** Mouse is stationary but camera orbits, causing the hovered segment to change. If hover detection only runs on mouse move events, the highlight "sticks" to the wrong segment during orbit.
**Why it happens:** _Input(InputEventMouseMotion) only fires when the mouse moves, not when the camera moves.
**How to avoid:** Also run segment detection in _Process when the camera is orbiting (check GameEvents.CameraOrbitStarted/Stopped or check orbit velocity). Alternatively, always run detection in _Process using GetViewport().GetMousePosition().
**Warning signs:** Hover highlight lags behind or sticks during camera orbit.

### Pitfall 3: Atan2 Coordinate System Mismatch
**What goes wrong:** Segment numbering is rotated or mirrored compared to expectations because Atan2's reference axis doesn't match the visual "12 o'clock" position.
**Why it happens:** Mathf.Atan2(z, x) returns 0 at the positive X axis, not at the top of the screen. Godot's 3D coordinate system has Z pointing "out of the screen" (toward the camera at default view), and the camera orbits, changing which world direction is "up" on screen.
**How to avoid:** Define segment 0 (clock position 12) at a fixed world angle (e.g., positive Z axis = angle PI/2 from Atan2). Do NOT try to align with screen orientation since the camera orbits. Use debug logging to verify: `GD.Print($"Angle: {angle}, Segment: {segIndex}")`.
**Warning signs:** Clicking on visually obvious segments reports wrong indices.

### Pitfall 4: Ring Proportions Don't Match CSG Placeholder
**What goes wrong:** The procedural ring doesn't align with the existing scene's scale, causing the camera zoom range to feel wrong or placeholder boxes to float.
**Why it happens:** The CSG ring uses outer=6, inner=3, height=0.3. If procedural ring uses different proportions, everything breaks.
**How to avoid:** Extract constants from the CSG ring: OuterRadius=6.0, InnerRadius=3.0, Height=0.3. The "equal thirds" split means: inner row 3.0-4.0, walkway 4.0-5.0, outer row 5.0-6.0 (each 1.0 unit of radial width).
**Warning signs:** Camera zoom feels different; placeholder boxes from Phase 1 no longer sit on the ring surface.

### Pitfall 5: Material Instance Sharing
**What goes wrong:** Highlighting one segment highlights all segments of the same row.
**Why it happens:** StandardMaterial3D is a Resource. If you assign the same instance to multiple MeshInstance3D nodes, changing AlbedoColor on one changes all.
**How to avoid:** Create a new StandardMaterial3D instance for each segment. Use `material.Duplicate()` or `new StandardMaterial3D()` per segment. Use `MaterialOverride` on MeshInstance3D (not surface material on the mesh itself).
**Warning signs:** Multiple segments change color simultaneously.

### Pitfall 6: Walkway Recess Creates Z-Fighting
**What goes wrong:** The walkway surface flickers or shows visual artifacts where it meets the row surfaces.
**Why it happens:** The 0.02-0.03 unit recess puts two surfaces very close together at the boundary, causing depth buffer precision issues.
**How to avoid:** Ensure there is a tiny gap (no overlapping geometry) between the walkway mesh edges and the row mesh edges. The annular sector mesh inner/outer radii should exactly match the walkway mesh edges, with no overlap. The recess (lower Y position) means they don't share the same plane, so Z-fighting should only occur at the vertical edges.
**Warning signs:** Flickering lines at walkway/row boundaries.

## Code Examples

### Complete Segment Grid Data Model

```csharp
// Source: Project-specific pattern following RoomDefinition.cs conventions
namespace OrbitalRings.Ring;

public enum SegmentRow { Outer, Inner }

public class SegmentGrid
{
    public const int SegmentsPerRow = 12;
    public const int TotalSegments = SegmentsPerRow * 2;

    public const float OuterRadius = 6.0f;
    public const float InnerRadius = 3.0f;
    public const float OuterRowInner = 5.0f;   // outer row starts here
    public const float InnerRowOuter = 4.0f;    // inner row ends here
    public const float RingHeight = 0.3f;
    public const float WalkwayRecess = 0.025f;  // walkway sits this far below row surfaces

    // Segment arc in radians (30 degrees)
    public const float SegmentArc = Mathf.Tau / SegmentsPerRow;

    private readonly bool[] _occupied = new bool[TotalSegments];

    public bool IsOccupied(SegmentRow row, int position)
    {
        return _occupied[ToIndex(row, position)];
    }

    public void SetOccupied(SegmentRow row, int position, bool occupied)
    {
        _occupied[ToIndex(row, position)] = occupied;
    }

    /// <summary>
    /// Converts row + clock position (0-11) to flat array index.
    /// Outer segments: 0-11, Inner segments: 12-23.
    /// </summary>
    public static int ToIndex(SegmentRow row, int position)
    {
        return row == SegmentRow.Outer ? position : SegmentsPerRow + position;
    }

    /// <summary>
    /// Converts flat array index back to row + position.
    /// </summary>
    public static (SegmentRow row, int position) FromIndex(int index)
    {
        return index < SegmentsPerRow
            ? (SegmentRow.Outer, index)
            : (SegmentRow.Inner, index - SegmentsPerRow);
    }

    /// <summary>
    /// Returns the start angle in radians for a given clock position (0-11).
    /// Position 0 = "12 o'clock" = starting at angle offset.
    /// </summary>
    public static float GetStartAngle(int position)
    {
        return position * SegmentArc;
    }

    /// <summary>
    /// Returns the display label for a segment: "Outer 1" through "Inner 12".
    /// Clock positions are 1-based for display (1-12), 0-based internally (0-11).
    /// </summary>
    public string GetLabel(SegmentRow row, int position)
    {
        string rowName = row == SegmentRow.Outer ? "Outer" : "Inner";
        int clockPos = position + 1; // 1-based display
        string status = IsOccupied(row, position) ? "Occupied" : "Empty";
        return $"{rowName} {clockPos} -- {status}";
    }

    /// <summary>
    /// Returns inner and outer radius for a given row.
    /// </summary>
    public static (float inner, float outer) GetRowRadii(SegmentRow row)
    {
        return row == SegmentRow.Outer
            ? (OuterRowInner, OuterRadius)
            : (InnerRadius, InnerRowOuter);
    }
}
```

### Color Palette (Recommended Discretion Choice)

```csharp
// Warm pastel palette -- board game aesthetic
public static class RingColors
{
    // Outer row: soft rose/pink alternating shades
    public static readonly Color OuterBase = new(0.91f, 0.78f, 0.80f);     // soft rose
    public static readonly Color OuterAlt = new(0.88f, 0.74f, 0.77f);      // slightly deeper rose

    // Inner row: soft lavender alternating shades
    public static readonly Color InnerBase = new(0.82f, 0.78f, 0.91f);     // soft lavender
    public static readonly Color InnerAlt = new(0.78f, 0.74f, 0.88f);      // slightly deeper lavender

    // Walkway: warm beige (reads as "path")
    public static readonly Color Walkway = new(0.88f, 0.85f, 0.78f);       // warm beige

    // Hover: brighten by ~15%
    public static Color Brighten(Color c, float factor = 0.15f)
    {
        return new Color(
            Mathf.Min(c.R + factor, 1.0f),
            Mathf.Min(c.G + factor, 1.0f),
            Mathf.Min(c.B + factor, 1.0f)
        );
    }

    // Selected: brighten by ~25% and shift toward warm accent
    public static Color SelectionHighlight(Color c)
    {
        Color brightened = Brighten(c, 0.25f);
        // Slight warm gold accent shift
        return new Color(
            Mathf.Min(brightened.R + 0.05f, 1.0f),
            Mathf.Min(brightened.G + 0.03f, 1.0f),
            brightened.B
        );
    }

    public static Color GetBaseColor(SegmentRow row, int position)
    {
        bool isEven = position % 2 == 0;
        return row == SegmentRow.Outer
            ? (isEven ? OuterBase : OuterAlt)
            : (isEven ? InnerBase : InnerAlt);
    }
}
```

### SurfaceTool Mesh Commit Pattern

```csharp
// Source: Godot 4.6 SurfaceTool documentation
// https://docs.godotengine.org/en/stable/tutorials/3d/procedural_geometry/surfacetool.html

public static ArrayMesh BuildSegmentMesh(float innerR, float outerR,
    float startAngle, float endAngle, float height, int subdivisions = 4)
{
    var st = new SurfaceTool();
    st.Begin(Mesh.PrimitiveType.Triangles);

    float halfH = height * 0.5f;

    for (int i = 0; i < subdivisions; i++)
    {
        float t0 = (float)i / subdivisions;
        float t1 = (float)(i + 1) / subdivisions;
        float a0 = Mathf.Lerp(startAngle, endAngle, t0);
        float a1 = Mathf.Lerp(startAngle, endAngle, t1);

        // Top face (Y = +halfH), CCW winding viewed from above
        var tOA = new Vector3(outerR * Mathf.Cos(a0), halfH, outerR * Mathf.Sin(a0));
        var tOB = new Vector3(outerR * Mathf.Cos(a1), halfH, outerR * Mathf.Sin(a1));
        var tIA = new Vector3(innerR * Mathf.Cos(a0), halfH, innerR * Mathf.Sin(a0));
        var tIB = new Vector3(innerR * Mathf.Cos(a1), halfH, innerR * Mathf.Sin(a1));

        AddQuad(st, tIA, tOA, tOB, tIB, Vector3.Up);

        // Bottom face (Y = -halfH), reversed winding
        var bOA = new Vector3(outerR * Mathf.Cos(a0), -halfH, outerR * Mathf.Sin(a0));
        var bOB = new Vector3(outerR * Mathf.Cos(a1), -halfH, outerR * Mathf.Sin(a1));
        var bIA = new Vector3(innerR * Mathf.Cos(a0), -halfH, innerR * Mathf.Sin(a0));
        var bIB = new Vector3(innerR * Mathf.Cos(a1), -halfH, innerR * Mathf.Sin(a1));

        AddQuad(st, tOA.With(y: -halfH), tIA.With(y: -halfH),
                tIB.With(y: -halfH), tOB.With(y: -halfH), Vector3.Down);

        // Outer curved face
        var outerNormalA = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0));
        var outerNormalB = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
        // (similar for inner face, start radial face, end radial face)
    }

    return st.Commit();
}

private static void AddQuad(SurfaceTool st,
    Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
{
    // Triangle 1: A, B, C
    st.SetNormal(normal); st.AddVertex(a);
    st.SetNormal(normal); st.AddVertex(b);
    st.SetNormal(normal); st.AddVertex(c);
    // Triangle 2: A, C, D
    st.SetNormal(normal); st.AddVertex(a);
    st.SetNormal(normal); st.AddVertex(c);
    st.SetNormal(normal); st.AddVertex(d);
}
```

### GameEvents Extension for Segment Events

```csharp
// Following established pure C# delegate pattern from GameEvents.cs
// Add to existing GameEvents class:

// Segment Events (Phase 2)
public event Action<int, bool> SegmentHovered;   // segmentIndex, isOuter
public event Action SegmentUnhovered;
public event Action<int, bool> SegmentSelected;  // segmentIndex, isOuter
public event Action SegmentDeselected;

public void EmitSegmentHovered(int index, bool isOuter)
    => SegmentHovered?.Invoke(index, isOuter);
public void EmitSegmentUnhovered()
    => SegmentUnhovered?.Invoke();
public void EmitSegmentSelected(int index, bool isOuter)
    => SegmentSelected?.Invoke(index, isOuter);
public void EmitSegmentDeselected()
    => SegmentDeselected?.Invoke();
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| CSG for final geometry | CSG for prototyping only; procedural mesh for production | Godot 4.0+ | CSG is slow at runtime and doesn't support per-face coloring; procedural meshes are the standard approach |
| ImmediateMesh for procedural geometry | SurfaceTool + ArrayMesh | Godot 4.0 | ImmediateMesh redraws every frame; SurfaceTool creates static geometry drawn by GPU |
| Godot [Signal] for C# events | Pure C# delegates | Godot 4.x C# | Avoids marshalling overhead and signal bugs (project convention) |
| _UnhandledInput for mouse picking | _Input with explicit consume | Godot 4.x | More reliable event delivery (project convention from Phase 1) |

**Deprecated/outdated:**
- CSGMesh for gameplay geometry: Fine for prototyping (Phase 1) but not for per-segment interactivity. Replace in Phase 2.
- Physics-based mouse picking for flat surfaces: Plane intersection is simpler, faster, and more reliable for a known-geometry planar surface.

## Open Questions

1. **Segment arc subdivision count**
   - What we know: 30-degree arcs need subdivision to look curved. 4 subdivisions per segment gives 7.5-degree steps.
   - What's unclear: Whether 4 is enough for visual smoothness at close zoom, or if 6-8 is needed.
   - Recommendation: Start with 4, visually test at ZoomMin (5 units). Increase if edges are visible. This is a single constant to tune.

2. **Escape key binding for deselect**
   - What we know: Phase 1 establishes Escape as deselect. But Escape might already be mapped in Godot for other purposes (e.g., quitting).
   - What's unclear: Whether an "escape" input action exists or needs to be registered like orbit/zoom actions.
   - Recommendation: Register "deselect" input action programmatically in _Ready() following the EnsureInputActions pattern from OrbitalCamera.

3. **Camera orbit vs. mouse hover priority**
   - What we know: Right-click drag orbits the camera. Left-click selects segments. Mouse motion updates hover.
   - What's unclear: Should hover detection pause during right-click drag (camera orbit)? Or continue updating?
   - Recommendation: Continue updating hover during orbit (it provides useful visual feedback). Only suppress left-click selection during active drag (check _isDragging equivalent).

4. **Walkway as single mesh vs. ring of quads**
   - What we know: Walkway is a continuous unbroken strip, not segmented. It needs to be ready for pathfinding in Phase 5.
   - What's unclear: Whether a single mesh torus annulus or a subdivided ring is better for future NavigationMesh baking.
   - Recommendation: Single continuous annulus mesh for now. Phase 5 will need to address circular navmesh regardless. Don't over-engineer for future pathfinding.

## Sources

### Primary (HIGH confidence)
- Godot 4.6 SurfaceTool documentation - procedural mesh API, Begin/AddVertex/Commit pattern
- Godot 4.6 Camera3D API - ProjectRayOrigin, ProjectRayNormal method signatures
- Godot 4.6 Plane struct API - IntersectsRay method, nullable Vector3 return
- Godot 4.6 StandardMaterial3D API - AlbedoColor, MaterialOverride on MeshInstance3D
- Project codebase: OrbitalCamera.cs, GameEvents.cs, SafeNode.cs, RoomDefinition.cs, QuickTestScene.tscn

### Secondary (MEDIUM confidence)
- Godot community forums - ray-plane intersection pattern for ground-plane picking without physics
- Godot documentation on CanvasLayer for screen-space UI overlays
- SurfaceTool C# API reference (straydragon.github.io) - SetColor, SetNormal, AddVertex order

### Tertiary (LOW confidence)
- Exact subdivision count needed for visual smoothness (4 vs 6 vs 8) -- needs visual testing
- Whether Vector3.With() extension exists in Godot 4.6 C# -- may need manual construction

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All APIs are core Godot 4.6, verified against official docs and project codebase
- Architecture: HIGH - Pattern follows established project conventions (SafeNode, GameEvents, namespace hierarchy); mesh generation is well-documented
- Pitfalls: HIGH - Winding order, material sharing, and coordinate system issues are well-known procedural mesh challenges; camera-orbit hover invalidation is project-specific but obvious from OrbitalCamera.cs analysis

**Research date:** 2026-03-02
**Valid until:** 2026-04-02 (stable -- Godot 4.6 APIs are mature)
