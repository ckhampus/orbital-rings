# Phase 18: Housing UI - Context

**Gathered:** 2026-03-06
**Status:** Ready for planning

<domain>
## Phase Boundary

Players can see which citizen lives where, who occupies each housing room, and overall population vs capacity at a glance. Three UI additions: citizen info panel home label, room tooltip with resident names, and population count/capacity display.

Requirements: UI-01, UI-02, UI-03.

</domain>

<decisions>
## Implementation Decisions

### Citizen Home Display (UI-01)
- Home label positioned below citizen name, above wish text (groups identity info together)
- Format for housed citizens: "Dormitory (Outer 3)" — room name + ring position
- Format for unhoused citizens: "No home" — cozy tone, not dashes
- Muted warm tone color (similar to wish text range, ~0.65-0.70) — doesn't compete with name or wish
- New Label inserted into existing VBox between _nameLabel and _categoryLabel

### Room Resident Tooltip (UI-02)
- Multi-line tooltip: existing segment label on line 1, room name on line 2, resident names on line 3
- Room name shown for ALL room types (not just housing) — e.g., "Outer 3 -- Cafe"
- Housing rooms additionally show resident list: "Residents: Luna, Felix, Milo"
- Show all names — no truncation or "+N more" (capacity caps at ~4-5 per room)
- SegmentTooltip.Show() needs room-aware text from SegmentInteraction (query BuildManager + HousingManager)

### Population Count/Capacity (UI-03)
- Format: "5/7" (housed count / total capacity) replacing current count-only display
- Show "5/0" when no housing rooms exist (consistent format, makes zero capacity obvious)
- Tick animation triggers on both citizen arrivals AND housing changes (room placed/demolished)
- Subscribe to RoomPlaced and RoomDemolished events in addition to existing CitizenArrived

### Claude's Discretion
- Exact muted warm tone RGB values for home label
- Font size for the home label (likely 12-13 to fit between name at 16 and wish at 13)
- Whether "/" in "5/7" uses a different color or opacity to visually separate count from capacity
- Tooltip line spacing and whether room name uses a different color than resident names

</decisions>

<specifics>
## Specific Ideas

No specific requirements — follow established programmatic UI patterns from CitizenInfoPanel, SegmentTooltip, and PopulationDisplay. All three changes are additive to existing components.

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- CitizenInfoPanel (Scripts/UI/CitizenInfoPanel.cs): VBox with name + category + wish labels. Insert home label between name and category.
- SegmentTooltip (Scripts/UI/SegmentTooltip.cs): Single-label PanelContainer. Text set by SegmentInteraction.UpdateHover() via Show(text, mousePos).
- PopulationDisplay (Scripts/UI/PopulationDisplay.cs): HBox with smiley icon + count label. Subscribes to CitizenArrived with elastic tick animation.
- HousingManager API: GetHomeForCitizen(name) -> int?, GetOccupants(anchorIndex) -> IReadOnlyList<string>, TotalCapacity, TotalHoused
- BuildManager.GetPlacedRoom(flatIndex) -> (RoomDefinition, AnchorIndex, SegmentCount, Cost)?
- RoomDefinition.RoomName (string) — human-readable room name
- SegmentGrid.GetLabel(row, position) -> "Outer 3 -- Occupied" — current tooltip source

### Established Patterns
- Programmatic UI (no .tscn): All HUD/tooltip components build nodes in _Ready()
- Dark semi-transparent StyleBoxFlat with rounded corners (BgColor ~0.1-0.15, alpha 0.85)
- Warm white text (0.95, 0.93, 0.90) at font sizes 13-20
- MouseFilter.Ignore on all non-interactive UI elements
- Kill-before-create tween pattern for animations
- Event subscription in _Ready(), unsubscription in _ExitTree()

### Integration Points
- CitizenInfoPanel.ShowForCitizen(): Add HousingManager.GetHomeForCitizen() + BuildManager.GetPlacedRoom() lookup
- SegmentInteraction.UpdateHover(): Build room-aware tooltip text before passing to SegmentTooltip.Show()
- PopulationDisplay: Add RoomPlaced/RoomDemolished subscriptions, change count format to "housed/capacity"

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 18-housing-ui*
*Context gathered: 2026-03-06*
