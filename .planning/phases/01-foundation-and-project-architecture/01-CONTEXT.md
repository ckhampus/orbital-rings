# Phase 1: Foundation and Project Architecture - Context

**Gathered:** 2026-03-02
**Status:** Ready for planning

<domain>
## Phase Boundary

Establish signal bus, data schema, camera, and architectural patterns before any gameplay code is written. The project gets a working Godot 4 C# structure with GameEvents Autoload, Resource subclasses (RoomDefinition, WishTemplate, CitizenData), an orbital camera system, a quick-test scene, and signal disconnect conventions. No gameplay mechanics — pure scaffolding.

</domain>

<decisions>
## Implementation Decisions

### Camera Feel
- Smooth orbit with momentum — camera glides after input stops (cinematic, cozy)
- Fixed tilt angle at ~60° (high angle, strategy-game overview like SimCity/Tropico)
- Zoom range: segment-level close (2-3 segments fill screen) to whole-ring overview
- Bounded zoom-out — ring always stays prominent, can't zoom out to a dot
- Smooth continuous scroll-wheel zoom (not stepped levels)
- Camera stays put when clicking segments — no auto-centering
- Gentle slow idle orbit when player isn't interacting — station feels alive
- Right-click drag for camera orbit
- WASD / arrow keys as alternative orbit input
- Default view: whole ring visible (zoomed out)

### Ring Proportions
- Chunky flat disc shape (not rounded torus) — rooms sit on a flat top surface, like a platform in space
- Walkway between inner and outer rows is slightly recessed — shallow groove gives visual separation
- Static ring — no rotation, only the camera moves

### Scale and Feel
- Tabletop miniature / diorama feel — the whole ring fits comfortably at default zoom
- At max zoom (segment-level), rooms and citizens are clearly readable, not abstract
- Bounded zoom limits on both ends

### Visual Starting Point
- Low-poly stylized art direction (Monument Valley / Islanders aesthetic)
- Warm pastel color palette — soft pinks, oranges, lavenders against dark space
- Clean starfield background — simple scattered stars, not distracting
- Environment setup based on Kenney's Starter Kit Basic Scene (https://github.com/KenneyNL/Starter-Kit-Basic-Scene)
  - Procedural sky, filmic tonemapping, SSAO (radius 0.3, intensity 0.5), soft bloom
  - Directional sun with shadows

### Core Entity Identity — Citizens
- CitizenData captures: name + appearance (body type, color palette, accessory)
- No personality traits in v1 — that's v2 territory
- Real-world human names (diverse: Maya, Leon, Asha) — grounded and relatable
- Appearance properties: body type variation (tall/short/round), primary + secondary color, one accessory (hat, glasses, scarf)

### Core Entity Identity — Rooms
- RoomDefinition is purely mechanical: category, size, stats
- No mood/flavor tags — visual art and room names carry the flavor
- 5 categories: Housing, Life Support, Work, Comfort, Utility

### Core Entity Identity — Wishes
- WishTemplate includes multiple text variants per wish type (3-5 options for natural variety)
- Explicit mapping from wish to fulfilling RoomDefinition(s) — predictable, easy to balance

### Economy Schema
- Economy values (costs, income rates, multipliers) live in a separate EconomyConfig Resource, not inside RoomDefinition
- Rooms reference their type; EconomyConfig centralizes all balance numbers for easy iteration

### Data Schema Scope
- Strictly v1 fields only — no v2 stubs (Traits[], Relationships[])
- Add fields when those features are actually built

### Input Philosophy
- Left-click = select/inspect (segments, rooms, citizens)
- Hover highlights objects with glow or outline — immediate spatial feedback
- Escape key: deselects current selection first, opens pause menu if nothing selected
- Build panel: mouse-driven UI as primary, hotkey bar (1-5 for room categories) as shortcuts

### Claude's Discretion
- Signal bus architecture and GameEvents Autoload design
- Signal disconnect pattern implementation
- Folder structure and namespace organization
- Camera momentum easing curves and exact zoom parameters
- Idle orbit speed and behavior
- Placeholder geometry specifics (ring dimensions in Godot units)
- Highlight/glow shader approach

</decisions>

<specifics>
## Specific Ideas

- Environment based on Kenney's Starter Kit Basic Scene — use its lighting, procedural sky, SSAO, and bloom setup as the starting point
- "Tabletop miniature" is the core spatial metaphor — player peers at a cozy diorama in space
- Camera should feel like Google Earth momentum — cinematic glide, not jarring stop
- Ring should feel like a chunky platform you build on top of, not a thin band

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- None — project is a blank Godot 4.6 C# template with only project.godot and .csproj

### Established Patterns
- Root namespace: `OrbitalRings` (defined in .csproj)
- .NET 8.0 target with Godot.NET.Sdk 4.6.1
- Jolt Physics engine configured
- Forward Plus rendering pipeline
- 2-space indentation, UTF-8, LF line endings (.editorconfig)

### Integration Points
- project.godot needs Autoload entries for GameEvents (and future managers)
- Main scene entry point to be configured in project.godot
- Kenney Starter Kit environment .tres to be adapted into project

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 01-foundation-and-project-architecture*
*Context gathered: 2026-03-02*
