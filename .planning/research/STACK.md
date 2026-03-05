# Technology Stack: v1.2 Housing System

**Project:** Orbital Rings
**Researched:** 2026-03-05
**Scope:** Stack additions for citizen housing assignment, return-home behavior, HousingManager autoload, HousingConfig resource, room tooltip residents, and save/load housing data
**Confidence:** HIGH -- all recommendations based on patterns already working in production code within this codebase

---

## Verdict: Zero New Dependencies

The housing system requires **no new libraries, packages, or external dependencies**. Every feature maps directly onto existing Godot 4.4 C# APIs and project patterns already validated in v1.0/v1.1. This is a feature addition, not a technology change.

---

## Recommended Stack (Additions Only)

### New Autoload Singleton: HousingManager

| Component | Technology | Version | Purpose | Why |
|-----------|-----------|---------|---------|-----|
| HousingManager | Godot `Node` (C#) | Godot 4.4 | Citizen-to-room assignment mapping, reassignment on build/demolish, resident queries | Follows the exact pattern of all 7 existing autoloads. New singleton because housing assignment is a distinct concern from mood/happiness (PRD recommendation). Keeps HappinessManager focused on mood math, not room-citizen bookkeeping. |

**Registration:** Add to Project > Autoloads in project.godot, ordered **after HappinessManager, before SaveManager**. HousingManager needs GameEvents and BuildManager (both load earlier). SaveManager must load last to subscribe to all state-change events including housing events.

**Namespace:** `OrbitalRings.Autoloads` -- same as all other autoloads.

**Instance pattern:** Static singleton `HousingManager.Instance` set in `_EnterTree()`, matching GameEvents pattern. Include `StateLoaded` guard to skip initialization on save restore, matching HappinessManager/CitizenManager pattern.

**File location:** `Scripts/Autoloads/HousingManager.cs`

### New Config Resource: HousingConfig

| Component | Technology | Version | Purpose | Why |
|-----------|-----------|---------|---------|-----|
| HousingConfig | Godot `Resource` (`[GlobalClass]`) | Godot 4.4 | Inspector-tunable timing constants for return-home cycle | Follows HappinessConfig and EconomyConfig pattern exactly. Resource file at `res://Resources/Housing/default_housing.tres`. Loaded in HousingManager._Ready() with fallback to code defaults. |

**Exported fields (all `[Export]`):**

| Field | Type | Default | Rationale |
|-------|------|---------|-----------|
| `HomeTimerMin` | `float` | `90.0f` | Lower bound of return-home cycle (PRD spec: 90-150s) |
| `HomeTimerMax` | `float` | `150.0f` | Upper bound of return-home cycle |
| `RestDurationMin` | `float` | `8.0f` | Min time "inside" home room (PRD: 8-15s, intentionally longer than regular 4-8s visits to feel like sleeping) |
| `RestDurationMax` | `float` | `15.0f` | Max time inside home room |
| `ZzzFontSize` | `int` | `14` | Smaller than standard FloatingText (18) per PRD "subtle" requirement |

**File locations:**
- `Scripts/Data/HousingConfig.cs` (follows `HappinessConfig.cs` pattern)
- `Resources/Housing/default_housing.tres` (follows `Resources/Happiness/default_happiness.tres` pattern)

**Resource loading pattern** (identical to HappinessManager):
```csharp
if (Config == null)
    Config = ResourceLoader.Load<HousingConfig>("res://Resources/Housing/default_housing.tres");
if (Config == null)
{
    GD.PushWarning("HousingManager: No HousingConfig found. Using code defaults.");
    Config = new HousingConfig();
}
```

---

## Godot APIs Used (All Existing in 4.4, All Validated in This Codebase)

### Timer (Godot.Timer) -- Return-Home Cycle

**Used for:** Per-citizen periodic return-home timer on CitizenNode.

**Pattern:** Identical to existing `_visitTimer` and `_wishTimer` in CitizenNode:
- Create `new Timer()`, configure `OneShot = true`, set `WaitTime` from random range, connect `Timeout`, call `AddChild()`, start in `_Ready()`
- Re-arm with new random interval in timeout handler
- One-shot timer that re-arms itself (same as `_wishTimer`)

**Why Timer, not `_Process` accumulator:** The project uses Timer nodes for all periodic behavior (visit checks at 20-40s, wish generation at 30-60s, arrival checks at 60s, autosave debounce at 0.5s). Timer nodes automatically pause with the scene tree (`ProcessMode.Pausable`), handle object lifetime correctly via the scene tree, and add zero per-frame overhead when not firing.

**Priority interaction with visit/wish timers:** The home timer fires independently of visit/wish timers. When home timer fires during an active visit or wish pursuit, it simply resets (no interruption). This is implemented as a guard at the top of the handler:
```csharp
if (_isVisiting) { ResetHomeTimer(); return; }
if (_currentWish != null) { ResetHomeTimer(); return; }
```

**Confidence: HIGH** -- Timer pattern used in 6+ places across the codebase. Exact same configuration and lifecycle.

### Tween (Godot.Tween) -- Return-Home Animation

**Used for:** Return-home animation sequence (walk to home segment, drift to room edge, fade out, rest inside, fade in, drift back).

**Pattern:** Structurally identical to `StartVisit()` in CitizenNode. The return-home animation is the same tween chain with three differences:
1. Target segment comes from `_homeSegmentIndex` instead of proximity search
2. Rest duration is 8-15s instead of 4-8s (from HousingConfig)
3. Zzz floater spawns before fade-out

The existing `StartVisit()` tween chain has 8 phases. The return-home chain will have 9 (inserting Zzz spawn):
1. `TweenMethod` -- angular walk to home segment mid-angle
2. `TweenCallback` -- update `_currentAngle`
3. `TweenMethod` -- radial drift to room edge
4. `TweenCallback` -- spawn Zzz floater
5. `TweenMethod` -- fade out (alpha 0)
6. `TweenCallback` -- hide + emit `CitizenEnteredRoom`
7. `TweenInterval` -- rest duration (8-15s, from HousingConfig)
8. `TweenCallback` -- show + emit `CitizenExitedRoom`
9. `TweenMethod` -- fade in (alpha 1)
10. `TweenMethod` -- drift back to walkway
11. `TweenCallback` -- restore state, resume walking, reset home timer

**Kill-before-create pattern:** Reuses existing `_activeTween?.Kill()` before creating new tween (pitfall #7 from v1.0). A single `_activeTween` field governs both visit and home-return tweens since they cannot overlap.

**Confidence: HIGH** -- Tween chain pattern proven in CitizenNode.StartVisit() with ~80 lines of working code.

### FloatingText (OrbitalRings.UI.FloatingText) -- Zzz Floater

**Used for:** Subtle "Zzz" text appearing when citizen enters home room.

**Pattern:** Same as arrival text in HappinessManager.SpawnArrivalText():
- `new FloatingText()`, add to CanvasLayer, call `Setup(text, color, position)`
- Self-destructs after animation (~1.1s)

**Screen-space positioning from 3D world position:**
```csharp
var camera = GetViewport().GetCamera3D();
Vector2 screenPos = camera.UnprojectPosition(citizen.GlobalPosition);
floater.Setup("Zzz", new Color(0.75f, 0.70f, 0.85f, 0.7f), screenPos + new Vector2(15, -30));
```

**Why FloatingText (2D), not Sprite3D (3D):**

| Approach | Pros | Cons |
|----------|------|------|
| FloatingText (2D Label) with UnprojectPosition | Zero new code, self-destructs, proven system | Screen-space position won't track if camera moves during 0.9s float |
| Sprite3D "Zzz" badge (like wish badges) | Stays in 3D world, visually consistent with badges | Requires texture creation, manual lifecycle, more complex |

**Decision: FloatingText.** The Zzz appears for <1 second. The camera almost never moves in that window (orbit is player-initiated). The PRD says "same style as existing FloatingText but smaller and lighter colored" -- so the spec itself directs FloatingText reuse.

**Visual tuning:** Font size 14 (vs standard 18), muted lavender color `Color(0.75f, 0.70f, 0.85f)` for subtlety per PRD.

**Confidence: HIGH** -- FloatingText is battle-tested in production. UnprojectPosition is standard Godot 4.4 Camera3D API.

### C# Event Delegates (System.Action) -- Housing Events

**Used for:** New events on GameEvents signal bus for housing state changes.

**New events on GameEvents:**

| Event | Signature | Emitted By | Consumed By |
|-------|-----------|------------|-------------|
| `CitizenHoused` | `Action<string, int>` (citizenName, segmentIndex) | HousingManager | SaveManager (autosave trigger) |
| `CitizenUnhoused` | `Action<string>` (citizenName) | HousingManager | SaveManager (autosave trigger) |

**Why only two new events:** Housing assignment/unassignment are the only new cross-system state mutations. The existing `RoomPlaced`, `RoomDemolished`, and `CitizenArrived` events already fire at the right times -- HousingManager subscribes to these as triggers. The new events propagate assignment *results* to SaveManager for autosave triggering.

**Pattern:** Identical to all existing GameEvents: `public event Action<...>`, `public void Emit...()` with `?.Invoke()`. SaveManager adds stored delegate references for these events in its subscription list.

**Confidence: HIGH** -- C# event delegate pattern used for all 20+ existing events without issue.

### Dictionary<string, int> -- Citizen-to-Room Mapping

**Used for:** Internal assignment map in HousingManager (`citizenName -> anchorSegmentIndex`).

**Why citizen name as key:** Citizen names are unique (enforced by `CitizenNames.GetNextName()` which draws from a pool without replacement). Using names avoids object references that break across save/load cycles. The existing save system already keys `ActiveWishes` by citizen name (`Dictionary<string, string>` in WishBoard), validating this approach.

**Reverse lookup (room to residents, for tooltip):** Iterate the mapping filtered by segment index. With max ~30-50 citizens in a typical game, linear scan of a Dictionary is negligible. No bidirectional map needed.

**Sentinel value:** `-1` means unhoused. Do not use `0` because `0` is a valid segment index (Outer position 0).

**Confidence: HIGH** -- Same collection types and keying strategy as existing WishBoard tracking.

---

## Data Model Changes

### SavedCitizen (Add Field)

```csharp
public class SavedCitizen
{
    // ... existing fields unchanged ...
    public int HomeSegmentIndex { get; set; } = -1;  // NEW: -1 = unhoused
}
```

**Serialization behavior:** System.Text.Json will write `-1` for unhoused citizens. When deserializing v2 saves that lack this field, the JSON default for `int` is `0` -- but the version gate handles this (see below).

### SaveData (Version Bump)

```csharp
public class SaveData
{
    public int Version { get; set; } = 3;  // bumped from 2
    // ... all existing fields unchanged ...
}
```

**Version-gated restore in SaveManager:**
```csharp
// In ApplySceneState, after restoring citizens:
if (data.Version >= 3)
{
    // Restore housing assignments from saved HomeSegmentIndex values
    foreach (var citizen in data.Citizens)
    {
        if (citizen.HomeSegmentIndex >= 0)
            HousingManager.Instance?.AssignFromSave(citizen.Name, citizen.HomeSegmentIndex);
    }
}
else
{
    // v2 saves: no housing data, run fresh assignment for all citizens
    HousingManager.Instance?.AssignAllUnhoused();
}
```

**Backward compatibility matrix:**

| Save Version | Loaded By v1.2 Code | Behavior |
|-------------|---------------------|----------|
| v1 | Existing v1 restore path, then fresh housing assignment | Works |
| v2 | Existing v2 restore path, then fresh housing assignment | Works |
| v3 | Full restore including housing assignments | Works |

| Save Version | Loaded By v1.1 Code (forward compat) | Behavior |
|-------------|--------------------------------------|----------|
| v3 | `HomeSegmentIndex` silently ignored by System.Text.Json | Works (housing data lost but no crash) |

**Confidence: HIGH** -- Version-gated restore already proven in v1-to-v2 migration. System.Text.Json missing/extra field behavior validated in production.

---

## Integration Points

### HousingManager Subscribes To (Existing Events)

| Event | Source | HousingManager Response |
|-------|--------|------------------------|
| `RoomPlaced` | BuildManager via GameEvents | Check if Housing category via BuildManager.GetPlacedRoom(). If yes, assign unhoused citizens (oldest first, even-spread by lowest occupancy). |
| `RoomDemolished` | BuildManager via GameEvents | If demolished room was housing (check _housingRoomCapacities in HappinessManager or track separately), unhouse its residents, attempt reassignment to other housing rooms. |
| `CitizenArrived` | CitizenManager via GameEvents | Assign new citizen to housing room with fewest occupants. Ties broken randomly. |

### HousingManager Reads From (Existing Singletons)

| Singleton | API | Purpose |
|-----------|-----|---------|
| `BuildManager.Instance` | `GetPlacedRoom(segmentIndex)` | Get room definition and category to verify housing rooms |
| `CitizenManager.Instance` | `Citizens` list | Iterate citizens for reassignment after demolish |

### HousingManager Provides To (Existing Systems)

| Consumer | API | Purpose |
|----------|-----|---------|
| `CitizenNode` | `GetHomeSegment(citizenName)` | Return home segment index for return-home animation target |
| `CitizenInfoPanel` | `GetHomeRoomName(citizenName)` | Display "Home: Bunk Pod (Outer 3)" |
| `SegmentTooltip` | `GetResidents(segmentIndex)` | Display "Residents: Pip, Nova" in room hover tooltip |
| `SaveManager` | Consumes `CitizenHoused`/`CitizenUnhoused` events | Triggers debounced autosave |

### BuildManager API Change Needed

`BuildManager.GetPlacedRoom()` currently returns `(RoomDefinition Definition, int AnchorIndex, int Cost)?`. HousingManager needs segment count for size-scaled capacity calculation (`BaseCapacity + segmentCount - 1`).

**Change:** Add `SegmentCount` to the return tuple:
```csharp
// Before:
public (RoomDefinition Definition, int AnchorIndex, int Cost)? GetPlacedRoom(int flatIndex)

// After:
public (RoomDefinition Definition, int AnchorIndex, int SegmentCount, int Cost)? GetPlacedRoom(int flatIndex)
```

**Impact:** Only HappinessManager.OnRoomPlaced() and HappinessManager.OnRoomDemolished() call this method externally. Both need the added field anyway (to compute size-scaled capacity). The change is additive -- add the field to destructuring at each call site.

**Confidence: HIGH** -- Return type is internal to the codebase with only 2 external callers identified.

---

## Existing Systems Modified (Minimal Changes)

| System | Change | Estimated LOC |
|--------|--------|--------------|
| **CitizenNode** | Add `_homeSegmentIndex` field, `_homeTimer` Timer, `StartHomeVisit()` method (mirrors `StartVisit()`), Zzz floater spawn, `SetHomeSegment()` setter for HousingManager | ~80 |
| **CitizenInfoPanel** | Add `_homeLabel` Label showing "Home: Bunk Pod (Outer 3)" or "Home: ---" in VBoxContainer | ~15 |
| **SegmentTooltip** (via SegmentInteraction) | Append resident names when hovering housing room: query HousingManager.GetResidents() | ~20 |
| **SaveManager** | Add `HomeSegmentIndex` to SavedCitizen serialization, bump Version to 3, version-gated restore, subscribe to new housing events | ~25 |
| **GameEvents** | Add `CitizenHoused` and `CitizenUnhoused` events with emit methods | ~10 |
| **BuildManager** | Add `SegmentCount` to `GetPlacedRoom()` return tuple | ~5 |

**Architecture unchanged:** No new design patterns, no new node types, no scene changes. All modifications follow existing code conventions exactly.

---

## What NOT to Add

| Technology | Why Not | Use Instead |
|------------|---------|-------------|
| **NavigationAgent3D** | Citizens use polar coordinate movement on a 1D circular walkway. Return-home uses the same angular walk as room visits. Navigation mesh is massive overkill for angle-based movement. | Existing `TweenMethod` angular interpolation in `StartVisit()` pattern |
| **Godot Signals (`[Signal]`)** | Project-wide locked decision: pure C# event delegates avoid marshalling overhead and IsConnected bugs (GitHub #76690, #72994). Validated across 9+ phases. | `System.Action` delegates on GameEvents |
| **Any NuGet package** | Zero external dependencies needed. System.Text.Json (in .NET 8) handles serialization. All game logic uses Godot built-in types. | Built-in .NET 8 and Godot 4.4 APIs |
| **State machine library** | Citizen behavior states (walking, visiting, resting-at-home) are simple enough for boolean flags and tween chains. Four states do not justify a formal FSM framework. | `_isVisiting` bool + `_activeTween` null check (existing pattern) |
| **Observable collections** | The GameEvents event bus already provides change notification. Adding reactive wrappers creates a second notification mechanism with no benefit. | GameEvents `CitizenHoused`/`CitizenUnhoused` events |
| **Separate .tscn scene for HousingManager** | All autoloads are pure C# scripts registered in project.godot with no associated scene. | Script-only autoload registration |
| **Database / SQLite** | Housing data is one `int` per citizen added to existing JSON save. No query patterns, no relational data, no volume justifying a database. | `System.Text.Json` POCO serialization (already in use) |
| **Unit test framework** | Not in scope for this milestone. Worth adding later but should be a separate effort with its own research. | Manual testing in-engine |

---

## Capacity Tracking: HousingManager vs HappinessManager

Housing capacity tracking (`_housingCapacity`, `_housingRoomCapacities`) currently lives in HappinessManager because it gates citizen arrivals there. The PRD raised whether to move it.

**Decision: Keep capacity tracking in HappinessManager.** HousingManager owns only the citizen-to-room *mapping*.

**Rationale:**
- Capacity tracking gates arrivals in `HappinessManager.OnArrivalCheck()`. Moving it to HousingManager would force HappinessManager to call `HousingManager.Instance.CalculateHousingCapacity()` on every arrival check -- adding a cross-singleton dependency to a hot path.
- Size-scaled capacity (`BaseCapacity + segmentCount - 1`) affects the capacity count, which is already computed in HappinessManager.OnRoomPlaced(). The formula change is a one-line edit there.
- HousingManager needs to know room capacity only for assignment (how many citizens can live in a room). It queries BuildManager for this, not HappinessManager.

This keeps responsibilities clean: HappinessManager owns "can citizens arrive?" (capacity gate), HousingManager owns "where does each citizen live?" (assignment mapping).

---

## Sources

All findings based on direct codebase analysis with HIGH confidence:
- `Scripts/Autoloads/GameEvents.cs` -- event bus pattern, C# delegate conventions
- `Scripts/Citizens/CitizenNode.cs` -- Timer lifecycle, Tween chain pattern, visit animation pipeline
- `Scripts/Citizens/CitizenManager.cs` -- citizen spawning, singleton pattern, save/load API
- `Scripts/Autoloads/HappinessManager.cs` -- housing capacity tracking, arrival gating, config resource loading
- `Scripts/Autoloads/SaveManager.cs` -- version-gated save format, System.Text.Json serialization, debounced autosave
- `Scripts/Data/HappinessConfig.cs` -- [GlobalClass] Resource pattern, [Export] fields with defaults
- `Scripts/Data/EconomyConfig.cs` -- same Resource pattern, establishing convention for config files
- `Scripts/Data/RoomDefinition.cs` -- BaseCapacity field, RoomCategory enum
- `Scripts/UI/FloatingText.cs` -- self-destructing floating text, Setup() API
- `Scripts/UI/CitizenInfoPanel.cs` -- programmatic UI panel, VBoxContainer labels
- `Scripts/UI/SegmentTooltip.cs` -- tooltip text composition pattern
- `Scripts/Build/BuildManager.cs` -- GetPlacedRoom() API, room tracking dictionary
- `Scripts/Ring/SegmentGrid.cs` -- flat index mapping, ToIndex/FromIndex
- `docs/prd-housing.md` -- feature requirements, design decisions, timing constants

---

*Stack research for: Orbital Rings v1.2 -- Housing system*
*Researched: 2026-03-05*
