# Phase 2: Ring Geometry and Segment Grid - Context

**Gathered:** 2026-03-02
**Status:** Ready for planning

<domain>
## Phase Boundary

Build the segmented ring mesh with 24 positions (12 outer, 12 inner), polar-math-based segment selection via mouse hover/click (not trimesh collision), visible segment differentiation between inner and outer rows, and a walkway corridor between them ready for future citizen pathfinding. The CSG placeholder ring from Phase 1 is replaced with proper segmented geometry.

</domain>

<decisions>
## Implementation Decisions

### Ring Proportions
- Keep overall outer radius ~6 Godot units (fits camera zoom range 5-25)
- Thin disc height ~0.3 units (board game piece feel, not chunky platform)
- Equal thirds allocation: outer row, walkway, and inner row each get roughly equal radial width
- Both rows use 30° per segment (12 segments each) — outer segments are physically wider due to larger radius
- Perfectly flat top surface (no curvature)
- Static ring (no rotation, camera moves around it)

### Mesh Approach
- Each segment is its own MeshInstance3D (24 total segment meshes)
- Individual pieces enable easy per-segment highlighting, coloring, and future animation
- CSG placeholder ring is replaced entirely by the segmented mesh

### Segment Boundaries
- Color alternation between adjacent segments (no physical gaps or etched lines)
- Subtle shade variation between neighboring segments within the same row
- Outer row uses one pastel base color (e.g., soft pink), inner row uses a different pastel base color (e.g., soft lavender)
- Inner vs. outer row distinction is immediately readable at a glance via different base colors

### Hover and Selection
- Hover: brightness boost on the hovered segment (lighter/brighter version of its base color)
- Selection (click): stronger brightness boost plus a soft accent color shift — clearly distinct from hover state
- Two-state feedback: hover = subtle brightening, selected = obvious brightening + accent
- Left-click selects; Escape deselects (from Phase 1 input philosophy)

### Position Indicators
- Clock positions 1-12, shared by both inner and outer rows
- Labeled as "Outer 3", "Inner 7" etc. to distinguish row
- Numbers appear on hover only (not permanently visible on the surface)
- Screen-space tooltip near the cursor (not 3D text on the ring surface)
- Tooltip shows position + status: "Outer 3 — Empty" or "Inner 7 — Occupied"

### Walkway
- Continuous unbroken circular strip (not segmented into 12 sections)
- Clearly different color from the room rows (e.g., soft grey or warm beige — reads as "path")
- Very subtle recess depth (~0.02-0.03 units below row surfaces)
- Passive element — no mouse interaction (hover/click) in this phase
- Structurally ready for citizen pathfinding in Phase 5

### Claude's Discretion
- Exact pastel colors for outer row, inner row, and walkway (within warm pastel palette)
- Segment mesh generation approach (procedural in code vs. pre-authored)
- Polar math implementation for mouse-to-segment mapping
- Hover brightness multiplier and selection accent color values
- Tooltip UI implementation details (font, size, offset from cursor)
- Camera zoom threshold for showing segment numbers (if zoom-based visibility is added)

</decisions>

<specifics>
## Specific Ideas

- Ring should feel like a board game surface — segments are tiles you build on
- Color alternation inspired by a subtle chess-board pattern (not jarring contrast, just enough to read boundaries)
- Tooltip should feel lightweight — small floating label, not a heavy panel
- "Outer 3 — Empty" format: concise, immediately useful, not cluttered

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- CSGCylinder3D placeholder ring (Scenes/QuickTest/QuickTestScene.tscn) — defines current proportions (outer=6, inner=3, height=0.3) to match
- OrbitalCamera (Scripts/Camera/OrbitalCamera.cs) — already handles orbit, zoom, and input; segment hover/click will need to integrate with its input handling
- GameEvents (Scripts/Autoloads/GameEvents.cs) — has RoomPlaced/RoomDemolished events; segment selection events may be needed
- SafeNode (Scripts/Core/SafeNode.cs) — base class for nodes needing signal lifecycle; ring/segment nodes should extend this
- RoomDefinition (Scripts/Data/RoomDefinition.cs) — defines room categories and segment sizes; segment grid needs to track occupancy compatible with this

### Established Patterns
- Pure C# event delegates (not Godot [Signal]) for cross-system communication
- SafeNode subscribe/unsubscribe pattern for event lifecycle
- Programmatic input action registration in _Ready()
- Spherical coordinate camera with LookAt(origin)
- OrbitalRings namespace hierarchy (Autoloads, Core, Camera, Data)

### Integration Points
- QuickTestScene.tscn: CSG placeholder ring needs to be replaced with segmented ring scene
- OrbitalCamera._Input(): Mouse hover/click for segment detection runs alongside camera input
- GameEvents: May need segment-related events (SegmentHovered, SegmentSelected)
- RoomDefinition.MinSegments/MaxSegments: Segment grid must support multi-segment occupancy queries

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 02-ring-geometry-and-segment-grid*
*Context gathered: 2026-03-02*
