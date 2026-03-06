# Phase 18: Housing UI - Research

**Researched:** 2026-03-06
**Domain:** Godot 4 C# programmatic UI -- Label insertion, tooltip enrichment, event-driven display updates
**Confidence:** HIGH

## Summary

Phase 18 adds three additive UI features to existing components: a home label in CitizenInfoPanel, room-aware tooltip text in SegmentTooltip (via SegmentInteraction), and a count/capacity format in PopulationDisplay. All three changes follow well-established patterns already present in the codebase -- programmatic node construction in `_Ready()`, dark semi-transparent StyleBoxFlat panels, warm-white text, MouseFilter.Ignore, and event-driven updates via GameEvents.

The key integration points are clearly defined: HousingManager already exposes `GetHomeForCitizen(name)`, `GetOccupants(anchorIndex)`, `TotalHoused`, and `TotalCapacity`. BuildManager's `GetPlacedRoom(flatIndex)` provides room name and position data. All data sources exist and are tested in prior phases. The work is purely UI wiring.

**Primary recommendation:** Treat this as three independent, small modifications to existing files. No new files needed. Each change is 10-30 lines of code in a single file (with SegmentInteraction requiring the most thought due to tooltip text assembly from multiple data sources).

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Home label positioned below citizen name, above wish text (groups identity info together)
- Format for housed citizens: "Dormitory (Outer 3)" -- room name + ring position. NOTE: actual room names are "Bunk Pod" and "Sky Loft", not "Dormitory" -- use RoomDefinition.RoomName
- Format for unhoused citizens: "No home" -- cozy tone, not dashes
- Muted warm tone color (similar to wish text range, ~0.65-0.70) -- doesn't compete with name or wish
- New Label inserted into existing VBox between _nameLabel and _categoryLabel
- Multi-line tooltip: existing segment label on line 1, room name on line 2, resident names on line 3
- Room name shown for ALL room types (not just housing) -- e.g., "Outer 3 -- Cafe"
- Housing rooms additionally show resident list: "Residents: Luna, Felix, Milo"
- Show all names -- no truncation or "+N more" (capacity caps at ~4-5 per room)
- SegmentTooltip.Show() needs room-aware text from SegmentInteraction (query BuildManager + HousingManager)
- Format: "5/7" (housed count / total capacity) replacing current count-only display
- Show "5/0" when no housing rooms exist (consistent format, makes zero capacity obvious)
- Tick animation triggers on both citizen arrivals AND housing changes (room placed/demolished)
- Subscribe to RoomPlaced and RoomDemolished events in addition to existing CitizenArrived

### Claude's Discretion
- Exact muted warm tone RGB values for home label
- Font size for the home label (likely 12-13 to fit between name at 16 and wish at 13)
- Whether "/" in "5/7" uses a different color or opacity to visually separate count from capacity
- Tooltip line spacing and whether room name uses a different color than resident names

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| UI-01 | CitizenInfoPanel shows home room name and location (or "--" if unhoused) | HousingManager.GetHomeForCitizen() returns anchor index; BuildManager.GetPlacedRoom() returns RoomDefinition with RoomName; SegmentGrid.FromIndex() gives row+position for location string. CONTEXT overrides "--" to "No home". |
| UI-02 | Housing room tooltip shows current resident names | SegmentInteraction.UpdateHover() already computes flatIndex and calls SegmentTooltip.Show(text). BuildManager.GetPlacedRoom(flatIndex) returns room info. HousingManager.GetOccupants(anchorIndex) returns name list. Assembly point is UpdateHover(). |
| UI-03 | PopulationDisplay shows count/capacity format (e.g., "5/7") | HousingManager.Instance.TotalHoused and TotalCapacity provide the values. GameEvents.RoomPlaced and RoomDemolished already exist for subscription. |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Godot 4 | 4.x | Game engine | Project engine |
| C# (.NET) | 8+ | Implementation language | Project language |

### Supporting
No new libraries needed. All functionality uses existing Godot Label, VBoxContainer, and C# string formatting.

### Alternatives Considered
None. All three changes are modifications to existing components using existing APIs.

## Architecture Patterns

### Recommended Project Structure
No new files needed. All changes modify existing files:
```
Scripts/
  UI/
    CitizenInfoPanel.cs    # +home label (UI-01)
    PopulationDisplay.cs   # +capacity format, +event subscriptions (UI-03)
  Ring/
    SegmentInteraction.cs  # +room-aware tooltip text assembly (UI-02)
```

### Pattern 1: Programmatic Label Insertion (CitizenInfoPanel)
**What:** Insert a new Label node into an existing VBox between two existing children.
**When to use:** UI-01 home label.
**Key detail:** VBox ordering is determined by child add order. The home label must be added between `_nameLabel` and `_categoryLabel` in `_Ready()`. Currently the order is: nameLabel, categoryLabel, wishLabel. The new homeLabel goes between nameLabel and categoryLabel.
**Example:**
```csharp
// In _Ready(), after _nameLabel but before _categoryLabel:
_homeLabel = new Label
{
    MouseFilter = MouseFilterEnum.Ignore
};
_homeLabel.AddThemeColorOverride("font_color", new Color(0.68f, 0.66f, 0.62f));
_homeLabel.AddThemeFontSizeOverride("font_size", 12);
vbox.AddChild(_homeLabel);

// Then _categoryLabel and _wishLabel as before
```

### Pattern 2: Multi-Source Tooltip Assembly (SegmentInteraction)
**What:** Build tooltip text by querying BuildManager and HousingManager before passing to SegmentTooltip.Show().
**When to use:** UI-02 room-aware tooltips.
**Key detail:** The current tooltip line is from `SegmentGrid.GetLabel(row, segIndex)` which returns e.g. "Outer 3 -- Occupied". The new format adds room name and optionally residents below. The assembly happens in `UpdateHover()` at line ~261 where `_tooltip?.Show(label, _lastMousePos)` is called.
**Example:**
```csharp
// Replace the single-line tooltip with multi-line assembly:
string label = _ringVisual.Grid.GetLabel(row, segIndex);
int flatIndex = SegmentGrid.ToIndex(row, segIndex);

// Check if a room is placed here
var roomInfo = OrbitalRings.Build.BuildManager.Instance?.GetPlacedRoom(flatIndex);
if (roomInfo != null)
{
    var (ringRow, ringPos) = SegmentGrid.FromIndex(roomInfo.Value.AnchorIndex);
    string rowName = ringRow == SegmentRow.Outer ? "Outer" : "Inner";
    label += $"\n{roomInfo.Value.Definition.RoomName}";

    // Housing rooms show residents
    if (roomInfo.Value.Definition.Category == RoomDefinition.RoomCategory.Housing)
    {
        var occupants = HousingManager.Instance?.GetOccupants(roomInfo.Value.AnchorIndex);
        if (occupants != null && occupants.Count > 0)
        {
            label += $"\nResidents: {string.Join(", ", occupants)}";
        }
    }
}

_tooltip?.Show(label, _lastMousePos);
```

### Pattern 3: Event-Driven Display Update (PopulationDisplay)
**What:** Subscribe to additional events (RoomPlaced, RoomDemolished) and update display format.
**When to use:** UI-03 population count/capacity.
**Key detail:** The existing pattern subscribes in `_Ready()` and unsubscribes in `_ExitTree()`. Use stored delegate references for clean unsubscription (same pattern as HousingManager). The count text changes from `count.ToString()` to `$"{housed}/{capacity}"`.
**Example:**
```csharp
// In _Ready(), add alongside existing CitizenArrived:
GameEvents.Instance.RoomPlaced += OnRoomPlaced;
GameEvents.Instance.RoomDemolished += OnRoomDemolished;

// Update method shared by all three handlers:
private void UpdateDisplay()
{
    int housed = HousingManager.Instance?.TotalHoused ?? 0;
    int capacity = HousingManager.Instance?.TotalCapacity ?? 0;
    _countLabel.Text = $"{housed}/{capacity}";

    // Kill-before-create tick animation
    _activeTween?.Kill();
    _activeTween = _countLabel.CreateTween();
    _countLabel.Scale = new Vector2(1.2f, 1.2f);
    _activeTween.TweenProperty(_countLabel, "scale", Vector2.One, 0.3f)
        .SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Elastic);
}
```

### Anti-Patterns to Avoid
- **Polling HousingManager every frame:** All updates should be event-driven. CitizenInfoPanel only queries on ShowForCitizen(), tooltip only queries during hover, PopulationDisplay only queries on events.
- **Adding using directives for unused namespaces:** SegmentInteraction will need `using OrbitalRings.Data;` for RoomDefinition.RoomCategory and `using OrbitalRings.Autoloads;` for HousingManager. Check existing usings first.
- **Forgetting _ExitTree cleanup:** Every event subscription in _Ready() must have a corresponding unsubscription in _ExitTree(). PopulationDisplay currently only unsubscribes CitizenArrived -- the new RoomPlaced/RoomDemolished handlers need cleanup too.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Citizen home lookup | Custom dictionary traversal | HousingManager.GetHomeForCitizen(name) | Already exists, returns nullable anchor index |
| Room name resolution | String constants or lookup tables | BuildManager.GetPlacedRoom(flatIndex).Definition.RoomName | Already exists, returns the RoomDefinition |
| Segment position label | Manual row/position string building | SegmentGrid.FromIndex() + manual formatting | Consistent with existing "Outer 3" style |
| Occupant name list | Iterating citizen list filtering by home | HousingManager.GetOccupants(anchorIndex) | Already exists, returns IReadOnlyList<string> |
| Capacity totals | Counting rooms and summing | HousingManager.TotalHoused / TotalCapacity | Already computed properties |

**Key insight:** All data access APIs already exist from Phase 14-16. This phase is purely UI display wiring.

## Common Pitfalls

### Pitfall 1: VBox Child Ordering
**What goes wrong:** Home label appears after wish text instead of between name and category.
**Why it happens:** VBox orders children by AddChild() call order, not by any sort property.
**How to avoid:** Insert homeLabel AddChild() call between nameLabel and categoryLabel in _Ready().
**Warning signs:** Label appears at bottom of info panel.

### Pitfall 2: Null-Safe Singleton Access
**What goes wrong:** NullReferenceException when HousingManager or BuildManager not yet initialized.
**Why it happens:** UI components may initialize before autoloads in some scene configurations.
**How to avoid:** Always use `HousingManager.Instance?.` and `BuildManager.Instance?.` with null-conditional operators. Provide sensible defaults ("No home", empty tooltip, "0/0").
**Warning signs:** Crash on scene load or save load.

### Pitfall 3: Tooltip Text for Non-Anchor Segments
**What goes wrong:** Multi-segment room only shows room name when hovering the anchor segment.
**Why it happens:** GetPlacedRoom() checks all segments via multi-segment range lookup, but the returned AnchorIndex differs from the hovered flatIndex. Must use the returned AnchorIndex for occupant lookup.
**How to avoid:** Use `roomInfo.Value.AnchorIndex` (not the hovered flatIndex) when calling GetOccupants.
**Warning signs:** Hovering non-anchor segment of a 2-3 segment housing room shows no residents.

### Pitfall 4: PopulationDisplay Initial State
**What goes wrong:** Display shows "0/0" on fresh game start even with 5 starter citizens.
**Why it happens:** Starter citizens spawn before PopulationDisplay._Ready() subscribes to events, so CitizenArrived events are missed. Also, initial display is set from CitizenManager.CitizenCount only.
**How to avoid:** Initialize display in _Ready() by reading both CitizenManager.CitizenCount and HousingManager totals. But note: on fresh start with no housing, the format is "housed/capacity" not "citizen_count/capacity". The decision says "housed count / total capacity", so initial is "0/0" on fresh game (no housing yet). This is correct per the "5/0" decision (show zero capacity).
**Warning signs:** Confusing count display that doesn't match citizen count -- this is intentional because format is housed/capacity, not total/capacity.

### Pitfall 5: SegmentInteraction Using Directives
**What goes wrong:** Compilation error from unresolved types.
**Why it happens:** SegmentInteraction is in `OrbitalRings.Ring` namespace and currently only imports `OrbitalRings.UI` and `OrbitalRings.Autoloads`. It needs `OrbitalRings.Data` for `RoomDefinition.RoomCategory` and `OrbitalRings.Build` is already accessed via fully-qualified name.
**How to avoid:** Add necessary using directive for `OrbitalRings.Data`, or use fully-qualified type names consistent with existing code style (SegmentInteraction uses `OrbitalRings.Build.BuildManager.Instance` fully qualified).
**Warning signs:** Build error.

### Pitfall 6: Event Unsubscription Mismatch in PopulationDisplay
**What goes wrong:** Memory leak or double-fire after scene reload.
**Why it happens:** PopulationDisplay currently subscribes to CitizenArrived with a direct method reference but does not use stored delegates like HousingManager does. Adding new subscriptions should follow the same pattern.
**How to avoid:** Either use stored delegates (like HousingManager) or direct method references (like current PopulationDisplay). Be consistent. The current direct method reference pattern (`GameEvents.Instance.CitizenArrived += OnCitizenArrived` / `-= OnCitizenArrived`) works fine for non-lambda handlers.
**Warning signs:** Events fire multiple times after save/load.

### Pitfall 7: Tooltip Label Node Does Not Support Rich Text
**What goes wrong:** Attempt to use BBCode for multi-colored tooltip lines fails.
**Why it happens:** SegmentTooltip uses a plain Label, not RichTextLabel.
**How to avoid:** Use plain `\n` for line breaks. All text will be single-color (warm white). The discretion item about "whether room name uses different color" cannot be achieved without converting Label to RichTextLabel, which is scope creep. Keep it simple -- single color.
**Warning signs:** BBCode tags appearing as literal text in tooltip.

## Code Examples

### UI-01: Home Label in CitizenInfoPanel

```csharp
// New field:
private Label _homeLabel;

// In _Ready(), after _nameLabel AddChild but before _categoryLabel:
_homeLabel = new Label
{
    MouseFilter = MouseFilterEnum.Ignore
};
_homeLabel.AddThemeColorOverride("font_color", new Color(0.68f, 0.66f, 0.62f));
_homeLabel.AddThemeFontSizeOverride("font_size", 12);
vbox.AddChild(_homeLabel);

// In ShowForCitizen(), after setting _nameLabel.Text:
var homeAnchor = HousingManager.Instance?.GetHomeForCitizen(citizen.Data.CitizenName);
if (homeAnchor != null)
{
    var roomInfo = BuildManager.Instance?.GetPlacedRoom(homeAnchor.Value);
    if (roomInfo != null)
    {
        var (row, pos) = SegmentGrid.FromIndex(roomInfo.Value.AnchorIndex);
        string rowName = row == SegmentRow.Outer ? "Outer" : "Inner";
        _homeLabel.Text = $"{roomInfo.Value.Definition.RoomName} ({rowName} {pos + 1})";
    }
    else
    {
        _homeLabel.Text = "No home"; // Room demolished between assignment and display
    }
}
else
{
    _homeLabel.Text = "No home";
}
```

Required new usings for CitizenInfoPanel:
```csharp
using OrbitalRings.Autoloads;   // HousingManager
using OrbitalRings.Build;       // BuildManager
using OrbitalRings.Ring;        // SegmentGrid, SegmentRow
```

### UI-02: Room-Aware Tooltip in SegmentInteraction

```csharp
// In UpdateHover(), replace the simple tooltip line (~line 261):
string label = _ringVisual.Grid.GetLabel(row, segIndex);
int flatIdx = SegmentGrid.ToIndex(row, segIndex);

var roomInfo = OrbitalRings.Build.BuildManager.Instance?.GetPlacedRoom(flatIdx);
if (roomInfo != null)
{
    label += $"\n{roomInfo.Value.Definition.RoomName}";

    if (roomInfo.Value.Definition.Category == OrbitalRings.Data.RoomDefinition.RoomCategory.Housing)
    {
        var occupants = OrbitalRings.Autoloads.HousingManager.Instance?.GetOccupants(roomInfo.Value.AnchorIndex);
        if (occupants != null && occupants.Count > 0)
        {
            label += $"\nResidents: {string.Join(", ", occupants)}";
        }
    }
}

_tooltip?.Show(label, _lastMousePos);
```

Note: Uses fully-qualified names for BuildManager (already the pattern in this file), HousingManager, and RoomDefinition to avoid adding using directives. Alternative: add `using OrbitalRings.Data;` and `using OrbitalRings.Autoloads;` for cleaner code.

### UI-03: Population Count/Capacity in PopulationDisplay

```csharp
// New delegate fields (for clean unsubscription):
private System.Action<string, int> _onRoomPlaced;
private System.Action<int> _onRoomDemolished;

// In _Ready(), after existing CitizenArrived subscription:
_onRoomPlaced = (_, _) => UpdateDisplay();
_onRoomDemolished = (_) => UpdateDisplay();
GameEvents.Instance.RoomPlaced += _onRoomPlaced;
GameEvents.Instance.RoomDemolished += _onRoomDemolished;

// Replace initial count line:
UpdateDisplay();  // instead of _countLabel.Text = initialCount.ToString();

// In _ExitTree(), add:
if (_onRoomPlaced != null) GameEvents.Instance.RoomPlaced -= _onRoomPlaced;
if (_onRoomDemolished != null) GameEvents.Instance.RoomDemolished -= _onRoomDemolished;

// Refactored update method:
private void UpdateDisplay()
{
    int housed = OrbitalRings.Autoloads.HousingManager.Instance?.TotalHoused ?? 0;
    int capacity = OrbitalRings.Autoloads.HousingManager.Instance?.TotalCapacity ?? 0;
    _countLabel.Text = $"{housed}/{capacity}";
}

// Refactored OnCitizenArrived:
private void OnCitizenArrived(string citizenName)
{
    UpdateDisplay();
    // Tick animation (existing logic)
    _activeTween?.Kill();
    _activeTween = _countLabel.CreateTween();
    _countLabel.Scale = new Vector2(1.2f, 1.2f);
    _activeTween.TweenProperty(_countLabel, "scale", Vector2.One, 0.3f)
        .SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Elastic);
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Count-only population display | Housed/capacity display | Phase 18 | Shows housing utilization at a glance |
| Generic "Occupied/Empty" tooltip | Room-aware tooltip with names | Phase 18 | Players can identify rooms and residents |
| No home info in citizen panel | Home label with room name + location | Phase 18 | Players can track which citizen lives where |

## Open Questions

1. **PopulationDisplay: "housed/capacity" vs "total citizens" confusion**
   - What we know: Decision is "housed count / total capacity" format. Before any housing is built, this shows "0/0" even with 5 citizens.
   - What's unclear: Whether players will find "0/0" confusing when they have citizens but no housing.
   - Recommendation: Follow the decision as stated. "5/0" edge case (from CONTEXT) confirms the intent -- format always shows housed/capacity, not total citizens. Players will learn the meaning quickly.

2. **Tooltip multi-color discretion**
   - What we know: SegmentTooltip uses plain Label (not RichTextLabel). CONTEXT grants discretion on whether room name uses different color.
   - What's unclear: Whether converting to RichTextLabel is worth the complexity.
   - Recommendation: Keep plain Label with `\n` line breaks. Single warm-white color for all lines. Converting to RichTextLabel is unnecessary scope for minimal visual gain given the cozy aesthetic already works well.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None -- no automated test infrastructure in project |
| Config file | None |
| Quick run command | `dotnet build` (compile-only verification) |
| Full suite command | Manual visual testing in Godot editor |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| UI-01 | Home label shows room name + location for housed citizen | manual-only | N/A -- requires running game, clicking citizen | N/A |
| UI-01 | Home label shows "No home" for unhoused citizen | manual-only | N/A -- requires running game with no housing built | N/A |
| UI-02 | Tooltip shows room name on second line for occupied segments | manual-only | N/A -- requires hovering room segment in game | N/A |
| UI-02 | Housing room tooltip shows resident names | manual-only | N/A -- requires housing room with assigned citizens | N/A |
| UI-03 | Population display shows "housed/capacity" format | manual-only | N/A -- requires game with housing rooms | N/A |
| UI-03 | Tick animation on room place/demolish events | manual-only | N/A -- requires placing/demolishing rooms | N/A |

**Manual-only justification:** All three requirements are visual UI behaviors that depend on the Godot runtime scene tree, rendering pipeline, and user interaction (click, hover). No unit-testable logic is being added -- only UI wiring.

### Sampling Rate
- **Per task commit:** `dotnet build` (ensures compilation)
- **Per wave merge:** Manual visual check in Godot editor (run scene, verify all three UI changes)
- **Phase gate:** Run game, verify: (1) click citizen shows home label, (2) hover room shows tooltip with name/residents, (3) population shows X/Y format with tick animation on room changes

### Wave 0 Gaps
None -- no test infrastructure needed. All validation is compilation + manual visual testing. The `dotnet build` command is already available. `dotnet format` is required before commit per CLAUDE.md.

## Sources

### Primary (HIGH confidence)
- Direct codebase inspection of all modified files:
  - `/workspace/Scripts/UI/CitizenInfoPanel.cs` -- current VBox structure, label patterns
  - `/workspace/Scripts/UI/SegmentTooltip.cs` -- single Label, Show(text, mousePos) API
  - `/workspace/Scripts/UI/PopulationDisplay.cs` -- event subscription pattern, tween animation
  - `/workspace/Scripts/Ring/SegmentInteraction.cs` -- UpdateHover() tooltip assembly point
  - `/workspace/Scripts/Autoloads/HousingManager.cs` -- GetHomeForCitizen, GetOccupants, TotalHoused, TotalCapacity
  - `/workspace/Scripts/Build/BuildManager.cs` -- GetPlacedRoom API
  - `/workspace/Scripts/Autoloads/GameEvents.cs` -- RoomPlaced, RoomDemolished events
  - `/workspace/Scripts/Ring/SegmentGrid.cs` -- FromIndex, GetLabel
  - `/workspace/Scripts/Data/RoomDefinition.cs` -- RoomName, Category enum
- Room definitions: Bunk Pod (Housing, BaseCapacity 2), Sky Loft (Housing, BaseCapacity 4)

### Secondary (MEDIUM confidence)
- Phase 18 CONTEXT.md decisions (verified against codebase -- all referenced APIs exist)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - no new dependencies, all modifications to existing code
- Architecture: HIGH - patterns directly observed in codebase, all integration points verified
- Pitfalls: HIGH - derived from reading actual code, not hypothetical scenarios

**Research date:** 2026-03-06
**Valid until:** 2026-04-06 (stable -- no external dependencies, all internal code)
