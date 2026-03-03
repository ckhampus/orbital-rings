# Phase 4: Room Placement and Build Interaction - Context

**Gathered:** 2026-03-03
**Status:** Ready for planning

<domain>
## Phase Boundary

Wire room placement into the ring with all 5 categories (Housing, Life Support, Work, Comfort, Utility), 10 room types with placeholder art (2 per category), multi-segment drag-to-resize, demolish with partial refund, and satisfying placement feedback. The player can place and demolish rooms anywhere on the ring and feel a satisfying snap response every time.

</domain>

<decisions>
## Implementation Decisions

### Build Panel Layout
- Bottom toolbar, hidden by default — press B or click a build button to open
- Escape or clicking away closes the toolbar
- 5 category tabs with visible hotkey numbers (e.g., "1 Housing", "2 Life Support")
- Clicking a category tab shows room types as small icon + name cards in a horizontal row
- Hover over a room card shows cost and segment size info
- Separate demolish button on the toolbar for entering demolish mode

### Placement Flow
- Room-first: player picks a room from the toolbar, then clicks a segment to position it
- Drag-to-resize: after clicking initial segment, drag to adjacent segments to expand (1-3 consecutive segments in the same row)
- Rooms within same row only — no cross-row spanning (inner or outer, not both)
- All rooms start at 1 segment and can be resized up to MaxSegments — no enforced minimum
- Separate confirm click to finalize placement (not release-to-confirm)
- Escape cancels placement at any point
- Live cost preview updates in real time as the room is resized — shows near the ghost preview
- Red/insufficient indicator if not enough credits

### Placement Preview
- Ghost preview shows the actual room block shape and category color at ~50% opacity
- Ghost becomes solid on confirm
- Occupied segments are dimmed/desaturated during placement mode (preventive feedback)
- Invalid attempts on occupied segments also flash red (reactive feedback)

### Demolish Flow
- Dedicated demolish button on the toolbar — click to enter demolish mode
- Hovering a placed room in demolish mode shows the refund amount
- Clicking a room shows a "Demolish? (+N)" confirm popup — click again or Enter to confirm
- Escape exits demolish mode

### Room Visuals
- Raised colored 3D blocks sitting on top of segment surfaces
- Each category has a distinct base color
- Individual room types within a category have slightly different shapes or heights for visual variety
- Modest block height (0.2-0.4 Godot units) — rooms sit just above the ring, ring still dominates silhouette
- Multi-segment rooms appear as one continuous block (no visible seams between segments)
- Thematic/cozy room names that fit the space station vibe (e.g., "Bunk Pod", "Sky Loft", "Star Lounge")
- 2 room types per category, 10 total — all available from the start
- Phase 7 will add NEW room types that don't exist yet (no locked/greyed-out slots)

### Placement Feedback
- Visual: scale bounce + white flash (room pops up larger then settles to final size, squash-and-stretch)
- Sound: soft chime — gentle melodic ding, warm and rewarding, not sharp
- Combined with existing floating "-N" spend text from CreditHUD

### Demolish Feedback
- Visual: quick shrink-to-nothing with a small puff of particles (soap bubble pop)
- Sound: light pop sound
- Combined with existing floating "+N" refund text from CreditHUD

### Invalid Placement Feedback
- Red flash on the rejected segment + short low-pitched buzz/error tone
- Clear rejection without being jarring

### Claude's Discretion
- Specific category colors for the 5 room block types (within warm pastel palette)
- Room shape variations within categories (height differences, edge rounding, etc.)
- Exact thematic room names and their stats/effectiveness values
- Ghost preview opacity and animation details
- Toolbar UI implementation details (font, spacing, card dimensions)
- Drag detection threshold and direction logic
- Confirm button/popup visual design
- Sound effect selection/generation approach
- Squash-and-stretch animation curve parameters
- Particle poof effect specifics (count, spread, fade)

</decisions>

<specifics>
## Specific Ideas

- Room names should be evocative of a cozy space station: "Bunk Pod", "Sky Loft", "Star Lounge", "Garden Nook" — not generic "Small Room", "Large Room"
- Placement should feel like placing a piece in a zen puzzle game — the chime confirms a satisfying choice
- Demolish is playful not punishing — a soap bubble pop, not a destruction animation
- The toolbar toggle keeps the ring view clean when not building — maximizes the diorama feel
- Drag-to-resize is the key interaction innovation — player can explore sizes fluidly before committing
- All rooms starting at 1 segment means no room is inaccessible due to space constraints — very cozy

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- SegmentGrid (Scripts/Ring/SegmentGrid.cs) — Has IsOccupied/SetOccupied for occupancy tracking, ToIndex/FromIndex for flat index conversion, GetRowRadii for geometry
- SegmentInteraction (Scripts/Ring/SegmentInteraction.cs) — Polar math hover/click already working; placement mode will extend or replace this interaction
- RingVisual (Scripts/Ring/RingVisual.cs) — Per-segment MeshInstance3D with material swap states; room blocks will be added as children of segments
- EconomyManager (Scripts/Autoloads/EconomyManager.cs) — TrySpend, Refund, CalculateRoomCost, CalculateDemolishRefund all ready to call
- GameEvents (Scripts/Autoloads/GameEvents.cs) — RoomPlaced/RoomDemolished events already declared
- RoomDefinition (Scripts/Data/RoomDefinition.cs) — Category enum, MinSegments/MaxSegments, BaseCostOverride ready; MinSegments will be 1 for all rooms per user decision
- CreditHUD (Scripts/UI/CreditHUD.cs) — Floating text for spend/refund already implemented
- RingMeshBuilder (Scripts/Ring/RingMeshBuilder.cs) — CreateAnnularSector can be used for room block geometry
- SafeNode (Scripts/Core/SafeNode.cs) — Base class for event lifecycle; new build system nodes should extend this

### Established Patterns
- Pure C# event delegates for cross-system communication (GameEvents)
- Autoload singleton pattern (GameEvents.Instance, EconomyManager.Instance)
- Inspector-editable Resource subclasses for data configuration
- Per-segment individual MeshInstance3D with pre-allocated material triplets
- Polar math picking (Plane.IntersectsRay + Atan2) — no physics collision bodies

### Integration Points
- SegmentInteraction needs a "build mode" state where clicks place rooms instead of selecting segments
- RingVisual.SetSegmentState needs extension for occupied/build-preview states
- EconomyManager.TrySpend called on placement confirm, EconomyManager.Refund on demolish
- GameEvents.RoomPlaced/RoomDemolished fired after successful operations
- Room block meshes added as children of RingVisual or a sibling RoomVisual node
- RoomDefinition .tres files need to be created for all 10 room types with names, categories, and stats

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 04-room-placement-and-build-interaction*
*Context gathered: 2026-03-03*
