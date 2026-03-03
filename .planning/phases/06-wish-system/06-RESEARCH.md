# Phase 6: Wish System - Research

**Researched:** 2026-03-03
**Domain:** Godot 4.6 C# game logic -- wish generation, badge display (Sprite3D billboard), wish-room matching via citizen visit system, WishBoard Autoload singleton
**Confidence:** HIGH

## Summary

The wish system is primarily a game logic layer on top of existing infrastructure. Citizens (CitizenNode) already have a visit system with timers, tween-based drift-fade animations, and angle-based proximity detection. WishTemplate resources already exist with WishCategory enum, TextVariants[], and FulfillingRoomIds[]. GameEvents already has WishGenerated/WishFulfilled event stubs. The main new technical challenge is Sprite3D billboard badges floating above citizens -- Godot 4.6's Sprite3D node natively supports `BillboardMode.Enabled` for camera-facing behavior in world space.

The implementation decomposes into: (1) WishTemplate .tres resource instances mapping wishes to room IDs, (2) wish generation/fulfillment logic in CitizenNode using the existing visit timer pattern, (3) Sprite3D badge display with billboard mode and tween-based pop animation, (4) WishBoard Autoload tracking active wishes via GameEvents, (5) CitizenInfoPanel updates for wish text display, and (6) wish-aware visit targeting modifying the existing proximity check. No external libraries are needed -- everything uses built-in Godot and C# constructs.

**Primary recommendation:** Extend CitizenNode with wish state (current WishTemplate + badge Sprite3D child), create WishBoard as a new Autoload singleton following the established pattern, and author WishTemplate .tres resources in a Resources/Wishes/ folder.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Icon-only badge (not speech bubble or thought cloud) -- small circular icon floating above the citizen
- 3D billboard Sprite3D that always faces the camera -- lives in world space, scales naturally with zoom
- Positioned directly above the citizen's head with a gentle bob, moves with the citizen
- Each wish category has a distinct Tabler icon exported as 64x64 PNG (Social, Comfort, Curiosity, Variety)
- Pre-exported PNGs loaded as Sprite3D textures from a Resources/Icons folder
- On wish fulfillment: badge does a quick scale-up + fade-out "pop" animation, then disappears
- Citizens get their first wish within 30-60 seconds of spawning (not immediate, not long delay)
- One wish at a time per citizen -- single badge, simple and clear
- Multiple citizens can independently wish for the same room type (duplicates allowed)
- Building one matching room fulfills ALL citizens with that wish (when they visit it -- see matching below)
- After fulfillment, 30-90 second cooldown before citizen generates a new wish
- Wishes fulfill when the citizen visits (drifts into) a matching room -- not just when the room exists
- Citizens with active wishes preferentially visit matching room types (weighted proximity check, not exclusive)
- When a matching room is newly built, citizens with that wish get their visit timer reset to a short delay (~5-10 seconds) -- "nudge" for responsive feedback loop
- WishBoard Autoload tracks all active wishes internally -- no player-facing board UI in Phase 6 (QOLX-01 deferred to v2)
- Wish text shown in CitizenInfoPanel (replaces "No wish" placeholder) uses personal, cozy first-person tone
- "I'd love a place to stargaze", "A cozy spot to read would be nice" -- hints at room type without naming it explicitly
- Player discovers the wish-to-room mapping by connecting hints to build panel options
- 4 categories per WishTemplate.WishCategory: Social, Comfort, Curiosity, Variety
- Multiple text variants per wish template for natural variety

### Claude's Discretion
- Exact Tabler icon choices for each category
- Badge size, bob amplitude/frequency, vertical offset from capsule
- Pop animation timing and easing curve
- Visit timer reset delay exact value (within the ~5-10s range)
- Wish weighting algorithm for preferring matching rooms vs normal proximity visits
- Number of WishTemplate instances per category (enough for variety across a 10+ minute session)
- Specific wish text variants and which room IDs each maps to
- How WishBoard structures its internal tracking (dictionary, list, etc.)
- CitizenInfoPanel updates for displaying wish text and category

### Deferred Ideas (OUT OF SCOPE)
- Persistent wish board / notification panel (QOLX-01) -- v2 requirement, tracked in REQUIREMENTS.md
- Wish fulfillment happiness boost -- Phase 7 (WISH-02 happiness effect wired there)
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| WISH-01 | Citizens express wishes via speech bubbles (e.g., "I'd love a place to stargaze") | User decision: icon badge above citizen (not speech bubble); wish TEXT shown in CitizenInfoPanel. Sprite3D billboard for badge, CitizenInfoPanel update for text. WishTemplate.TextVariants[] already defined. |
| WISH-02 | Fulfilling a wish grants happiness to that citizen and a small global happiness boost | Phase 6 scope: wish fulfillment detection (citizen visits matching room -> badge pops -> WishFulfilled event). Happiness effect deferred to Phase 7 per user decision. GameEvents.WishFulfilled already stubbed. |
| WISH-03 | Unfulfilled wishes linger harmlessly -- nothing bad happens | Badge stays visible indefinitely. No penalty timer, no happiness drain, no credit cost. Pure display + tracking. |
| WISH-04 | Wishes span categories: social, comfort, curiosity, variety | WishTemplate.WishCategory enum already defines all 4. Need enough WishTemplate .tres instances per category. Room mapping must cover rooms across categories. |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Godot Engine | 4.6 | Game engine -- Sprite3D, Node3D, Tween, Timer | Project runtime |
| Godot.NET.Sdk | 4.6.1 | C# SDK for Godot | Project build system |
| .NET | 10.0 | Runtime | Project target framework |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Sprite3D (Godot built-in) | 4.6 | Billboard icon badge floating above citizens | Badge display -- BillboardMode.Enabled for camera-facing |
| Tween (Godot built-in) | 4.6 | Pop animation on wish fulfillment | Scale-up + fade-out animation sequence |
| Timer (Godot built-in) | 4.6 | Wish generation timing, cooldown timing | Periodic wish generation like existing visit timer |
| ImageTexture (Godot built-in) | 4.6 | Loading pre-exported PNG icons as Sprite3D textures | Badge icon texture loading |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Sprite3D billboard | Control node + camera.UnprojectPosition | Sprite3D is simpler -- one node, no frame-by-frame unprojection math. Already in world space. |
| Pre-exported PNG | Runtime SVG/procedural icon | PNGs are simpler, load instantly, no runtime rendering overhead. User locked this decision. |
| Timer child node | _Process delta accumulation | Timer pattern established in CitizenNode (visit timer) and EconomyManager (income tick). Consistent with project. |

**Installation:** No additional packages needed. All built-in Godot and .NET.

## Architecture Patterns

### Recommended Project Structure
```
Scripts/
  Citizens/
    CitizenNode.cs        # Extended: wish state, badge Sprite3D, wish-aware visits
    CitizenAppearance.cs  # No changes needed
    CitizenManager.cs     # No changes needed (WishBoard accesses Citizens list)
  Autoloads/
    GameEvents.cs         # Already has WishGenerated/WishFulfilled events
    WishBoard.cs          # NEW: Autoload tracking active wishes
  Data/
    WishTemplate.cs       # Already exists with WishCategory, TextVariants, FulfillingRoomIds
  UI/
    CitizenInfoPanel.cs   # Extended: show wish text + category instead of "No wish"
Resources/
  Wishes/               # NEW: WishTemplate .tres instances
    wish_social_*.tres
    wish_comfort_*.tres
    wish_curiosity_*.tres
    wish_variety_*.tres
  Icons/                 # NEW: Pre-exported Tabler PNGs (64x64)
    wish_social.png
    wish_comfort.png
    wish_curiosity.png
    wish_variety.png
```

### Pattern 1: Sprite3D Billboard Badge
**What:** A Sprite3D child of CitizenNode with BillboardMode.Enabled, positioned above the citizen's head. Moves with the citizen automatically since it's a child node.
**When to use:** Any time a world-space icon needs to face the camera.
**Example:**
```csharp
// Godot 4.6 Sprite3D billboard setup
var badge = new Sprite3D();
badge.Texture = ResourceLoader.Load<Texture2D>("res://Resources/Icons/wish_social.png");
badge.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
badge.PixelSize = 0.005f;  // Scale: 64px * 0.005 = 0.32 world units wide
badge.Position = new Vector3(0, verticalOffset, 0);  // Above citizen head
badge.Shaded = false;  // Unlit -- badge should be always visible
badge.AlphaCut = SpriteBase3D.AlphaCutMode.OpaquePrePass;  // Clean edges
AddChild(badge);
```

### Pattern 2: Wish Generation Timer (following existing Timer pattern)
**What:** A Timer child node fires periodically to generate a new wish for a citizen. Same pattern as the visit timer already in CitizenNode and the income timer in EconomyManager.
**When to use:** Any periodic game logic trigger.
**Example:**
```csharp
// Following established Timer pattern from CitizenNode
var wishTimer = new Timer
{
    Name = "WishTimer",
    OneShot = true,  // One-shot: fire once, then re-arm after fulfillment cooldown
    WaitTime = 30.0f + GD.Randf() * 30.0f  // 30-60 seconds initial delay
};
wishTimer.Timeout += OnWishTimerTimeout;
AddChild(wishTimer);
// Start deferred to _Ready() per Phase 3 decision
```

### Pattern 3: WishBoard Autoload Singleton (following established Autoload pattern)
**What:** A new Autoload registered in project.godot that tracks all active wishes. Follows the same Instance singleton pattern as GameEvents, EconomyManager, BuildManager, CitizenManager.
**When to use:** Cross-system state that needs to be queried without iterating nodes.
**Example:**
```csharp
namespace OrbitalRings.Autoloads;

public partial class WishBoard : SafeNode
{
    public static WishBoard Instance { get; private set; }

    // Dictionary<citizenName, WishTemplate> for O(1) lookup
    private readonly Dictionary<string, WishTemplate> _activeWishes = new();

    public override void _Ready()
    {
        base._Ready();
        Instance = this;
    }

    protected override void SubscribeEvents()
    {
        GameEvents.Instance.WishGenerated += OnWishGenerated;
        GameEvents.Instance.WishFulfilled += OnWishFulfilled;
        GameEvents.Instance.RoomPlaced += OnRoomPlaced;
    }

    // ... tracks active wishes, triggers visit timer resets on room placement
}
```

### Pattern 4: Tween-based Pop Animation (following existing tween pattern)
**What:** Kill-before-create tween for the badge pop animation (scale up + fade out), matching the visit sequence pattern in CitizenNode.
**When to use:** Any animated visual effect on game objects.
**Example:**
```csharp
// Pop animation: scale up 1.5x while fading out, then remove
_activeTween?.Kill();
var tween = CreateTween();
_activeTween = tween;

tween.SetParallel(true);
tween.TweenProperty(badge, "scale", Vector3.One * 1.5f, 0.3f)
    .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
tween.TweenProperty(badge, "modulate:a", 0.0f, 0.3f)
    .SetEase(Tween.EaseType.In);

tween.SetParallel(false);
tween.TweenCallback(Callable.From(() => {
    badge.QueueFree();
    _wishBadge = null;
}));
```

### Pattern 5: Wish-Aware Visit Targeting (modifying existing proximity check)
**What:** Modify OnVisitTimerTimeout in CitizenNode to weight room selection toward matching wish room types. Not exclusive -- citizen can still visit non-matching rooms, but matching rooms get a distance bonus.
**When to use:** When citizen has an active wish and a matching room exists on the ring.
**Example:**
```csharp
// In room proximity scoring, apply a weight bonus for matching wish rooms
float effectiveDist = angleDist;
if (_currentWish != null && roomTypeAtSegment != null)
{
    foreach (var fulfillingId in _currentWish.FulfillingRoomIds)
    {
        if (roomTypeAtSegment == fulfillingId)
        {
            effectiveDist *= 0.3f;  // 70% distance reduction = strong preference
            break;
        }
    }
}
```

### Anti-Patterns to Avoid
- **Shared Sprite3D textures creating badge corruption:** Each badge must get its own Sprite3D node instance. The texture can be shared (ResourceLoader caches), but the Sprite3D node itself must be unique per citizen. This mirrors the per-instance StandardMaterial3D pattern established in Phase 2.
- **Modifying visit system to be wish-exclusive:** Citizens must still visit non-matching rooms sometimes. The weighting must be a preference, not a lock. Otherwise players see citizens ignoring nearby rooms, which looks broken.
- **WishBoard iterating CitizenManager.Citizens to find wishes:** The success criteria explicitly requires WishBoard to report wishes "without iterating citizens directly." Use event-driven tracking with a dictionary.
- **Coupling wish fulfillment to room existence instead of room visit:** User locked decision: wish fulfills on citizen visit to matching room, not on room placement. The room placement event only triggers a visit timer reset (the "nudge").

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Camera-facing badge | Manual billboard math (LookAt camera each frame) | Sprite3D.Billboard = BillboardModeEnum.Enabled | Godot handles billboard rotation automatically at render time, zero per-frame cost |
| Texture loading | Manual Image.Load + ImageTexture.CreateFromImage | ResourceLoader.Load<Texture2D>("res://path.png") | ResourceLoader handles caching, format conversion, thread safety |
| Periodic timing | _Process with delta accumulation + manual thresholds | Timer child node (OneShot or repeating) | Consistent with project pattern, pauses with scene tree, no float drift |
| Cross-system event bus | Custom observer/mediator pattern | GameEvents.Instance (already exists) | WishGenerated/WishFulfilled already stubbed, pure C# delegates |
| Wish data storage | Hardcoded wish arrays in C# | WishTemplate .tres Resource files | Inspector-editable, follows RoomDefinition pattern, hot-reloadable |

**Key insight:** The project already provides all the infrastructure patterns needed. The wish system is a composition of existing patterns (Timer, Tween, Autoload singleton, GameEvents, Resource .tres files), not a new technical challenge.

## Common Pitfalls

### Pitfall 1: Badge Visibility During Citizen Room Visit
**What goes wrong:** Citizen fades out and becomes invisible during room visit, but the badge Sprite3D stays visible -- floating icon with no citizen beneath it.
**Why it happens:** The visit system hides the citizen via `Visible = false` on the CitizenNode, but Sprite3D child visibility is independent if not explicitly managed.
**How to avoid:** Hide the badge when the citizen enters a room (`Visible = false` on the badge node). Since badge is a child of CitizenNode, setting `CitizenNode.Visible = false` should hide all children including the badge. Verify this is the case -- Godot propagates visibility to children.
**Warning signs:** Floating badge icons visible over rooms with no citizen nearby.

### Pitfall 2: Visit Timer Reset Race Condition
**What goes wrong:** When a matching room is built, WishBoard resets citizen visit timers. But if the citizen is already mid-visit (drifting/fading), the timer reset creates a second visit attempt while the first is still animating.
**Why it happens:** The visit timer can fire while `_isVisiting` is true.
**How to avoid:** The existing `OnVisitTimerTimeout` already guards with `if (_isVisiting) return;`. The timer reset just restarts the countdown -- when it fires, the guard prevents double-visits. No additional protection needed, but verify the guard is hit.
**Warning signs:** Citizens teleporting or glitching during visits.

### Pitfall 3: WishTemplate Resource Loading Order
**What goes wrong:** WishBoard tries to load WishTemplate resources in _Ready() but they haven't been registered/loaded yet.
**Why it happens:** Autoloads initialize in registration order. If WishBoard loads before resources are available, ResourceLoader returns null.
**How to avoid:** Load WishTemplate .tres resources via ResourceLoader.Load (synchronous, blocking) in _Ready(). Godot resources load synchronously and are available immediately. Also add null checks with GD.PushWarning following EconomyManager's ResourceLoader fallback chain pattern.
**Warning signs:** "No wish templates loaded" warnings at startup, citizens never generating wishes.

### Pitfall 4: RoomPlaced Event Missing RoomId for Wish Matching
**What goes wrong:** WishBoard needs to know WHICH room type was placed to check against WishTemplate.FulfillingRoomIds. The RoomPlaced event already includes roomType (the RoomId string) as its first parameter.
**Why it happens:** N/A -- the event signature `Action<string, int>` already provides the RoomId.
**How to avoid:** Use `GameEvents.Instance.RoomPlaced += OnRoomPlaced` where handler receives `(string roomType, int segmentIndex)`. This maps directly to WishTemplate.FulfillingRoomIds comparison.
**Warning signs:** No issue expected -- event signature is correct.

### Pitfall 5: Badge Bob Animation Conflicting with Citizen Bob
**What goes wrong:** CitizenNode already has a vertical bob via `_bobPhase` in UpdatePositionFromAngle. If the badge also bobs independently, the two bobs combine unpredictably.
**Why it happens:** The badge is a child of CitizenNode, so it inherits the citizen's position (including bob). Adding a second bob on the badge creates double-bobbing.
**How to avoid:** Choose ONE of: (a) let the badge inherit the citizen's bob naturally (no additional animation on badge) -- simpler, consistent, or (b) add a subtle independent bob on the badge that's visually distinct (different frequency/amplitude). Recommendation: option (a) -- the citizen's existing bob already provides gentle movement. The badge just needs a fixed Y offset above the citizen.
**Warning signs:** Badge vibrating or bouncing erratically.

### Pitfall 6: RoomDemolished Event Lacks Room Type
**What goes wrong:** When a room is demolished, WishBoard needs to know if any matching room types still exist for active wishes. But `RoomDemolished` only emits `int segmentIndex`, not the room type.
**Why it happens:** The event was designed for economy/visual cleanup, not wish tracking.
**How to avoid:** WishBoard should track placed room types internally by listening to both `RoomPlaced` (add to tracking) and `RoomDemolished` (remove from tracking). Maintain a `Dictionary<string, int>` counting instances of each room type. On demolish, WishBoard can look up which room type was at that segment from its own tracking before decrementing.
**Warning signs:** Wishes resolving against rooms that no longer exist (stale room type data).

### Pitfall 7: Sprite3D AlphaCut vs Transparency for Clean Edges
**What goes wrong:** PNG icons with transparency render with visible edges or halos around the icon.
**Why it happens:** Default Sprite3D rendering treats alpha as standard transparency, which causes blending artifacts at edges.
**How to avoid:** Set `AlphaCut = SpriteBase3D.AlphaCutMode.OpaquePrePass` for clean alpha-tested edges. This discards pixels below a threshold rather than blending them.
**Warning signs:** White or dark halos around badge icons.

## Code Examples

Verified patterns from the existing codebase:

### Loading a Resource .tres file (EconomyManager pattern)
```csharp
// Source: Scripts/Autoloads/EconomyManager.cs lines 45-53
var template = ResourceLoader.Load<WishTemplate>("res://Resources/Wishes/wish_social_hangout.tres");
if (template == null)
{
    GD.PushWarning("WishBoard: Failed to load wish template at path.");
}
```

### Creating a Timer child node (CitizenNode pattern)
```csharp
// Source: Scripts/Citizens/CitizenNode.cs lines 170-178
var timer = new Timer
{
    Name = "WishTimer",
    OneShot = true,
    WaitTime = 30.0f + GD.Randf() * 30.0f  // 30-60s initial delay
};
timer.Timeout += OnWishTimerTimeout;
AddChild(timer);
// Timer.Start() deferred to _Ready() per established pattern
```

### Tween kill-before-create (CitizenNode visit pattern)
```csharp
// Source: Scripts/Citizens/CitizenNode.cs lines 302-303
_activeTween?.Kill();
var tween = CreateTween();
_activeTween = tween;
// ... chain TweenProperty/TweenCallback/TweenInterval calls
```

### SafeNode subscribe/unsubscribe lifecycle (project standard)
```csharp
// Source: Scripts/Core/SafeNode.cs -- used by CitizenManager, WishBoard should follow
protected override void SubscribeEvents()
{
    GameEvents.Instance.WishGenerated += OnWishGenerated;
    GameEvents.Instance.WishFulfilled += OnWishFulfilled;
    GameEvents.Instance.RoomPlaced += OnRoomPlaced;
    GameEvents.Instance.RoomDemolished += OnRoomDemolished;
}

protected override void UnsubscribeEvents()
{
    GameEvents.Instance.WishGenerated -= OnWishGenerated;
    GameEvents.Instance.WishFulfilled -= OnWishFulfilled;
    GameEvents.Instance.RoomPlaced -= OnRoomPlaced;
    GameEvents.Instance.RoomDemolished -= OnRoomDemolished;
}
```

### GameEvents emit pattern (existing wish stubs)
```csharp
// Source: Scripts/Autoloads/GameEvents.cs lines 172-182
// Already defined -- no changes needed to GameEvents.cs
GameEvents.Instance?.EmitWishGenerated(citizenName, wishTemplate.WishId);
GameEvents.Instance?.EmitWishFulfilled(citizenName, wishTemplate.WishId);
```

## Room Type Mapping Reference

The following room types exist in the project (from Resources/Rooms/*.tres). WishTemplate.FulfillingRoomIds must reference these RoomId strings:

| Room Name | RoomId | RoomCategory (enum) | Category Name |
|-----------|--------|---------------------|---------------|
| Bunk Pod | bunk_pod | 0 | Housing |
| Sky Loft | sky_loft | 0 | Housing |
| Air Recycler | air_recycler | 1 | LifeSupport |
| Garden Nook | garden_nook | 1 | LifeSupport |
| Workshop | workshop | 2 | Work |
| Craft Lab | craft_lab | 2 | Work |
| Star Lounge | star_lounge | 3 | Comfort |
| Reading Nook | reading_nook | 3 | Comfort |
| Comm Relay | comm_relay | 4 | Utility |
| Storage Bay | storage_bay | 4 | Utility |

**Mapping wish categories to room types (recommendation):**

| Wish Category | Example Wish Text | Fulfilling RoomIds |
|---------------|-------------------|--------------------|
| Social | "I'd love a cozy spot to chat with friends" | star_lounge, garden_nook, comm_relay |
| Comfort | "A cozy spot to read would be nice" | reading_nook, bunk_pod, sky_loft |
| Curiosity | "I'd love a place to stargaze" | star_lounge, craft_lab, workshop |
| Variety | "It'd be nice to try something different" | garden_nook, comm_relay, storage_bay |

Note: Each wish template is a specific wish-to-rooms mapping. Multiple templates per category with different room targets ensures variety. At least 2-3 templates per category provides enough variety for 10+ minute sessions.

## Wish-Aware Visit Targeting Design

The existing `OnVisitTimerTimeout` in CitizenNode finds the nearest occupied segment by angular distance. To add wish awareness:

1. **When citizen has NO wish:** Existing behavior unchanged (visit nearest occupied segment)
2. **When citizen HAS a wish:** Apply a distance multiplier to matching room segments. A factor of 0.3x on angular distance for matching rooms means a matching room 3x further away still "looks" equally close as a non-matching room nearby. This creates a strong but not exclusive preference.
3. **Room type lookup:** CitizenNode needs to know WHAT room type is at each segment. Two options:
   - (a) WishBoard maintains a segment-to-roomId map (populated from RoomPlaced/RoomDemolished events) and exposes a `GetRoomIdAtSegment(int flatIndex)` method
   - (b) BuildManager already has `GetPlacedRoom(int flatIndex)` which returns the RoomDefinition

   **Recommendation:** Option (b) -- use `BuildManager.Instance.GetPlacedRoom(flatIndex)?.Definition.RoomId`. This avoids duplicating room tracking state. BuildManager already tracks all placed rooms with full RoomDefinition references.

4. **Visit timer reset ("nudge"):** WishBoard listens to `RoomPlaced`, checks if any active wishes match the placed room type, and resets those citizens' visit timers to ~7 seconds (within 5-10s range). This requires either:
   - WishBoard having a reference to CitizenNode instances to call a `ResetVisitTimer()` method
   - A new event `GameEvents.WishNudge(string citizenName)` that CitizenNode listens to

   **Recommendation:** WishBoard calls a new public method on CitizenNode: `NudgeVisit()`. WishBoard can access citizens through `CitizenManager.Instance.Citizens` list (already public IReadOnlyList). This is simpler than adding another event round-trip.

## WishBoard Internal Tracking Design

**Recommended structure:**
```csharp
// Active wishes: citizen name -> wish template
private readonly Dictionary<string, WishTemplate> _activeWishes = new();

// Placed room types: room ID -> count of placed instances
private readonly Dictionary<string, int> _placedRoomTypes = new();
```

**Query API:**
- `GetActiveWishes()` -- returns the dictionary for iteration
- `GetWishForCitizen(string citizenName)` -- returns WishTemplate or null
- `GetActiveWishCount()` -- returns total count
- `IsRoomTypeAvailable(string roomId)` -- checks if at least one instance exists on the ring

**Event flow:**
1. `WishGenerated(citizenName, wishId)` -> add to `_activeWishes`
2. `WishFulfilled(citizenName, wishId)` -> remove from `_activeWishes`
3. `RoomPlaced(roomType, segmentIndex)` -> increment `_placedRoomTypes[roomType]`, check if any active wishes match and nudge those citizens
4. `RoomDemolished(segmentIndex)` -> look up room type from own tracking, decrement count

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Godot 3.x BillboardMode on SpatialMaterial | Godot 4.x BillboardMode on Sprite3D.Billboard property (BaseMaterial3D.BillboardModeEnum) | Godot 4.0 | Direct Sprite3D property, no material-level billboard needed |
| Godot Signal-based events | Pure C# event delegates (project decision Phase 1) | Phase 1 | All events use Action<> delegates, not [Signal] |
| Scene-based UI (.tscn) | Programmatic UI construction (project pattern) | Phase 2+ | All UI nodes created in code, no .tscn dependency |

**Deprecated/outdated:**
- Godot [Signal] attribute: Project explicitly avoids this (Phase 1 decision). All cross-system communication uses pure C# event delegates via GameEvents.
- .tscn scene files for UI: Project uses programmatic node creation consistently. WishBoard and badge creation should follow this pattern.

## Open Questions

1. **Tabler Icon Export Format**
   - What we know: Tabler icons (https://tabler.io/icons) need to be exported as 64x64 PNGs and placed in Resources/Icons/
   - What's unclear: Which specific icons best represent each category. The icons must be visually distinct at 64px.
   - Recommendation: This is under Claude's discretion. Select simple, recognizable icons: heart/people for Social, bed/couch for Comfort, telescope/lightbulb for Curiosity, sparkles/dice for Variety. Create placeholder colored circles if Tabler export is impractical in the build environment, with documented icon names for future replacement.

2. **Badge Visibility at Different Zoom Levels**
   - What we know: Sprite3D with billboard mode scales naturally with camera distance (closer = larger, further = smaller).
   - What's unclear: Whether `PixelSize` of 0.005 gives appropriate visual size at both close zoom and max zoom-out.
   - Recommendation: Set initial PixelSize based on citizen capsule proportions (~0.32 world units for 64px at 0.005 pixel size). Tune via Export property if needed. The tallest citizen is 0.35 world units, so a 0.32-unit badge is proportional.

3. **WishTemplate .tres File Authoring**
   - What we know: WishTemplate.cs already exists with all fields. Need to create .tres instances.
   - What's unclear: Whether to create them programmatically (Write tool) or via Godot editor format.
   - Recommendation: Write .tres files directly using the same format as existing room .tres files. The format is well-established in the project (see Resources/Rooms/*.tres examples).

## Sources

### Primary (HIGH confidence)
- Existing codebase analysis: Scripts/Citizens/CitizenNode.cs, Scripts/Autoloads/GameEvents.cs, Scripts/Data/WishTemplate.cs, Scripts/Citizens/CitizenManager.cs, Scripts/UI/CitizenInfoPanel.cs, Scripts/Build/BuildManager.cs
- Existing resource files: Resources/Rooms/*.tres (10 room definitions with RoomId values)
- Project configuration: project.godot (Godot 4.6, autoload registration order), Orbital Rings.csproj (Godot.NET.Sdk 4.6.1, net10.0)
- Phase 6 CONTEXT.md: User decisions locked 2026-03-03

### Secondary (MEDIUM confidence)
- Godot 4.6 Sprite3D API: Billboard property, AlphaCut, PixelSize, Shaded -- based on Godot 4.x documentation patterns. Sprite3D.Billboard accepts BaseMaterial3D.BillboardModeEnum.Enabled for camera-facing behavior.

### Tertiary (LOW confidence)
- None -- all findings verified against existing codebase patterns or Godot built-in API.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- no external dependencies, all Godot built-in
- Architecture: HIGH -- follows established project patterns (Autoload singleton, Timer, Tween, SafeNode, GameEvents, programmatic node creation)
- Pitfalls: HIGH -- identified from direct codebase analysis of visit system, material handling, and event signatures
- Room mapping: HIGH -- all 10 room .tres files analyzed, RoomId values confirmed
- Sprite3D billboard: MEDIUM -- standard Godot API, but no existing usage in project to validate against

**Research date:** 2026-03-03
**Valid until:** 2026-04-03 (stable -- no external library version concerns)
