# Phase 4: Room Placement and Build Interaction - Research

**Researched:** 2026-03-03
**Domain:** Godot 4.6 C# -- room placement system, build UI, drag-to-resize, feedback animations, audio
**Confidence:** HIGH

## Summary

Phase 4 implements the core building loop: a bottom toolbar with 5 category tabs, room-first placement with drag-to-resize onto adjacent ring segments, demolish with partial refund, and satisfying audio-visual feedback. The existing codebase provides strong foundations -- SegmentGrid tracks occupancy, SegmentInteraction handles polar math picking, EconomyManager has TrySpend/Refund/CalculateRoomCost ready, GameEvents has RoomPlaced/RoomDemolished events declared, and RoomDefinition defines the data model. The primary new work is: (1) a BuildManager state machine orchestrating Normal/Placing/Demolish modes, (2) a BuildPanel toolbar UI, (3) RoomVisual for 3D room block meshes on segments, (4) 10 RoomDefinition .tres resources, and (5) placement/demolish feedback (tween animations, particles, audio).

The standard approach uses Godot's built-in Tween API for squash-and-stretch animations, StandardMaterial3D emission for white flash, GPUParticles3D for demolish puff, and AudioStreamPlayer for sound effects. All UI is built programmatically following the established pattern (CreditHUD, SegmentTooltip). No external libraries needed -- the built-in Godot APIs cover everything.

**Primary recommendation:** Build a BuildManager autoload that owns a simple enum-based state machine (Normal/Placing/Demolish), intercepts SegmentInteraction clicks based on current mode, and coordinates between BuildPanel UI, RoomVisual rendering, and EconomyManager transactions.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Bottom toolbar, hidden by default -- press B or click a build button to open
- Escape or clicking away closes the toolbar
- 5 category tabs with visible hotkey numbers (e.g., "1 Housing", "2 Life Support")
- Clicking a category tab shows room types as small icon + name cards in a horizontal row
- Hover over a room card shows cost and segment size info
- Separate demolish button on the toolbar for entering demolish mode
- Room-first: player picks a room from the toolbar, then clicks a segment to position it
- Drag-to-resize: after clicking initial segment, drag to adjacent segments to expand (1-3 consecutive segments in the same row)
- Rooms within same row only -- no cross-row spanning (inner or outer, not both)
- All rooms start at 1 segment and can be resized up to MaxSegments -- no enforced minimum
- Separate confirm click to finalize placement (not release-to-confirm)
- Escape cancels placement at any point
- Live cost preview updates in real time as the room is resized -- shows near the ghost preview
- Red/insufficient indicator if not enough credits
- Ghost preview shows the actual room block shape and category color at ~50% opacity
- Ghost becomes solid on confirm
- Occupied segments are dimmed/desaturated during placement mode (preventive feedback)
- Invalid attempts on occupied segments also flash red (reactive feedback)
- Dedicated demolish button on the toolbar -- click to enter demolish mode
- Hovering a placed room in demolish mode shows the refund amount
- Clicking a room shows a "Demolish? (+N)" confirm popup -- click again or Enter to confirm
- Escape exits demolish mode
- Raised colored 3D blocks sitting on top of segment surfaces
- Each category has a distinct base color
- Individual room types within a category have slightly different shapes or heights for visual variety
- Modest block height (0.2-0.4 Godot units) -- rooms sit just above the ring, ring still dominates silhouette
- Multi-segment rooms appear as one continuous block (no visible seams between segments)
- Thematic/cozy room names that fit the space station vibe
- 2 room types per category, 10 total -- all available from the start
- Phase 7 will add NEW room types that don't exist yet (no locked/greyed-out slots)
- Visual placement feedback: scale bounce + white flash (room pops up larger then settles to final size, squash-and-stretch)
- Sound placement feedback: soft chime -- gentle melodic ding, warm and rewarding, not sharp
- Combined with existing floating "-N" spend text from CreditHUD
- Demolish visual: quick shrink-to-nothing with a small puff of particles (soap bubble pop)
- Demolish sound: light pop sound
- Combined with existing floating "+N" refund text from CreditHUD
- Invalid placement feedback: red flash on the rejected segment + short low-pitched buzz/error tone

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

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| BLDG-01 | Player can place a room into 1-3 adjacent empty segments in the outer or inner row | BuildManager state machine + SegmentGrid occupancy + drag-to-resize interaction pattern + SegmentInteraction polar math |
| BLDG-02 | Player can choose from 5 room categories: Housing, Life Support, Work, Comfort, Utility | BuildPanel toolbar UI with TabBar/HBoxContainer + 5 category tabs + RoomDefinition.RoomCategory enum already exists |
| BLDG-03 | Each room category has at least 2 specific room types with visually distinct placeholder art | 10 RoomDefinition .tres resources + RoomVisual with category-colored raised blocks + height/shape variation per type |
| BLDG-04 | Player hears a satisfying snap sound and sees a visual response when placing a room | Tween squash-and-stretch + StandardMaterial3D emission flash + AudioStreamPlayer with .wav/.ogg chime |
| BLDG-05 | Player can demolish any room and receive a partial credit refund | BuildManager Demolish mode + EconomyManager.CalculateDemolishRefund + EconomyManager.Refund + confirm popup |
| BLDG-06 | Larger rooms (2-3 segments) are more effective but cost more, with a slight size discount | EconomyManager.CalculateRoomCost already implements size discount formula; RoomDefinition.Effectiveness scaled by segment count |
</phase_requirements>

## Standard Stack

### Core
| Library/API | Version | Purpose | Why Standard |
|-------------|---------|---------|--------------|
| Godot Tween | Built-in 4.6 | Placement bounce, demolish shrink, flash animations | CreateTween() already used in CreditHUD; no external dependency needed |
| StandardMaterial3D | Built-in 4.6 | Room block materials, ghost preview opacity, white flash via EmissionEnabled | Already used for all segment materials in RingVisual |
| GPUParticles3D | Built-in 4.6 | Demolish puff particle effect | Built-in one-shot particle system; no shader authoring needed |
| AudioStreamPlayer | Built-in 4.6 | Placement chime, demolish pop, error buzz | Non-positional audio for UI feedback; lightweight single node |
| Control/Container | Built-in 4.6 | Build panel toolbar UI (PanelContainer, HBoxContainer, TabBar, Button) | Programmatic UI pattern established by CreditHUD and SegmentTooltip |
| RingMeshBuilder | Custom (existing) | Room block mesh generation via CreateAnnularSector | Already proven for segment meshes; reuse for room blocks with different height |

### Supporting
| Library/API | Version | Purpose | When to Use |
|-------------|---------|---------|-------------|
| AudioStreamWav | Built-in 4.6 | Procedural sound generation for chime/pop/buzz | If no .wav/.ogg assets provided; can generate simple tones programmatically |
| ParticleProcessMaterial | Built-in 4.6 | Configure demolish particle behavior (direction, gravity, fade) | Applied to GPUParticles3D ProcessMaterial for one-shot burst |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Built-in Tween | GTweensGodot (NuGet) | Richer API (punch, shake) but adds dependency; built-in covers squash-and-stretch fine |
| GPUParticles3D | CpuParticles3D | CPU particles are simpler but less performant; GPU particles are standard for 3D |
| Procedural audio | Pre-recorded .wav files | Pre-recorded sounds are simpler to implement but require asset creation; procedural gives full control |
| Enum state machine | Node-based FSM | Node FSM is overkill for 3 states; enum + switch is simpler and matches codebase style |

## Architecture Patterns

### Recommended Project Structure
```
Scripts/
  Build/
    BuildManager.cs          # Autoload state machine: Normal/Placing/Demolish modes
    BuildPanel.cs            # Bottom toolbar UI (Control node)
    RoomVisual.cs            # 3D room block rendering on ring segments
    PlacementPreview.cs      # Ghost preview during placement mode
    PlacementFeedback.cs     # Tween animations + particles + audio for place/demolish
  Data/
    RoomDefinition.cs        # Already exists -- no changes needed
  Ring/
    SegmentGrid.cs           # Already exists -- extend with multi-segment occupancy helpers
    SegmentInteraction.cs    # Already exists -- extend with build mode delegation
Resources/
  Rooms/
    bunk_pod.tres            # 10 RoomDefinition .tres files
    sky_loft.tres
    air_recycler.tres
    garden_nook.tres
    workshop.tres
    craft_lab.tres
    star_lounge.tres
    reading_nook.tres
    storage_bay.tres
    comm_relay.tres
```

### Pattern 1: Enum State Machine for Build Modes
**What:** BuildManager owns a simple enum `BuildMode { Normal, Placing, Demolish }` and a switch statement in its input handler. SegmentInteraction delegates to BuildManager when a click occurs.
**When to use:** Small number of modes (3), clear transitions, no complex per-state data.
**Example:**
```csharp
// BuildManager.cs (Autoload)
public enum BuildMode { Normal, Placing, Demolish }

private BuildMode _mode = BuildMode.Normal;
private RoomDefinition _selectedRoom;
private int _anchorFlatIndex = -1;  // First segment clicked
private int _currentSize = 1;       // Current drag size (1-3)

public void EnterPlacingMode(RoomDefinition room)
{
    _selectedRoom = room;
    _mode = BuildMode.Placing;
    _anchorFlatIndex = -1;
    _currentSize = 1;
    // Dim occupied segments via RingVisual
}

public void OnSegmentClicked(int flatIndex)
{
    switch (_mode)
    {
        case BuildMode.Normal:
            // Default selection behavior (delegate to SegmentInteraction)
            break;
        case BuildMode.Placing:
            HandlePlacementClick(flatIndex);
            break;
        case BuildMode.Demolish:
            HandleDemolishClick(flatIndex);
            break;
    }
}
```

### Pattern 2: Multi-Segment Room Block Mesh
**What:** Room blocks span 1-3 adjacent segments as a single continuous raised mesh. Use RingMeshBuilder.CreateAnnularSector with the combined arc range and a taller height.
**When to use:** Every room placement -- the room block sits on top of segment surfaces.
**Example:**
```csharp
// RoomVisual.cs -- create a room block mesh spanning multiple segments
public MeshInstance3D CreateRoomBlock(SegmentRow row, int startPos, int segmentCount,
                                       Color categoryColor, float blockHeight)
{
    (float innerR, float outerR) = SegmentGrid.GetRowRadii(row);
    float startAngle = SegmentGrid.GetStartAngle(startPos);
    float endAngle = startAngle + SegmentGrid.SegmentArc * segmentCount;

    ArrayMesh mesh = RingMeshBuilder.CreateAnnularSector(
        innerR, outerR, startAngle, endAngle, blockHeight, subdivisions: 4 * segmentCount);

    var material = new StandardMaterial3D { AlbedoColor = categoryColor };

    var meshInstance = new MeshInstance3D
    {
        Mesh = mesh,
        MaterialOverride = material,
        // Position above the ring surface
        Transform = new Transform3D(Basis.Identity,
            new Vector3(0, SegmentGrid.RingHeight * 0.5f + blockHeight * 0.5f, 0))
    };

    return meshInstance;
}
```

### Pattern 3: Ghost Preview with Transparency
**What:** During placement, show the room block at ~50% opacity using StandardMaterial3D transparency. Update every frame as the player drags to adjacent segments.
**When to use:** Active placement mode, before confirm click.
**Example:**
```csharp
// PlacementPreview.cs
private void UpdateGhostPreview(SegmentRow row, int startPos, int size, Color categoryColor)
{
    // Remove old ghost
    _ghostMesh?.QueueFree();

    float blockHeight = GetBlockHeight(_selectedRoom);
    _ghostMesh = CreateRoomBlock(row, startPos, size, categoryColor, blockHeight);

    // Make semi-transparent
    var mat = (StandardMaterial3D)_ghostMesh.MaterialOverride;
    mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
    mat.AlbedoColor = new Color(categoryColor.R, categoryColor.G, categoryColor.B, 0.5f);

    AddChild(_ghostMesh);
}
```

### Pattern 4: Placement Feedback (Squash-and-Stretch + Flash)
**What:** On confirm, animate the room block with a scale overshoot then settle, plus a brief white emission flash. Uses Godot's built-in Tween API.
**When to use:** Every successful room placement.
**Example:**
```csharp
// PlacementFeedback.cs
public void PlayPlacementFeedback(MeshInstance3D roomMesh, AudioStreamPlayer sfxPlayer)
{
    // Squash-and-stretch: overshoot then settle
    var tween = roomMesh.CreateTween();
    Vector3 finalScale = roomMesh.Scale;
    Vector3 overshoot = new Vector3(finalScale.X * 1.2f, finalScale.Y * 1.4f, finalScale.Z * 1.2f);

    tween.TweenProperty(roomMesh, "scale", overshoot, 0.1f)
        .SetTrans(Tween.TransitionType.Back)
        .SetEase(Tween.EaseType.Out);
    tween.TweenProperty(roomMesh, "scale", finalScale, 0.15f)
        .SetTrans(Tween.TransitionType.Elastic)
        .SetEase(Tween.EaseType.Out);

    // White flash via emission
    var mat = (StandardMaterial3D)roomMesh.MaterialOverride;
    mat.EmissionEnabled = true;
    mat.Emission = Colors.White;
    mat.EmissionEnergyMultiplier = 2.0f;

    var flashTween = roomMesh.CreateTween();
    flashTween.TweenProperty(mat, "emission_energy_multiplier", 0.0f, 0.3f)
        .SetEase(Tween.EaseType.Out);
    flashTween.TweenCallback(Callable.From(() => mat.EmissionEnabled = false));

    // Sound
    sfxPlayer.Play();
}
```

### Pattern 5: Drag-to-Resize via Hover Tracking
**What:** After the anchor segment click, track hovered segments. If the hovered segment is adjacent to the current selection (same row, within MaxSegments), expand the ghost preview. The drag direction is determined by which side the mouse moves to.
**When to use:** During placement mode after initial anchor click, before confirm.
**Example:**
```csharp
// BuildManager.cs
private void HandlePlacementHover(int hoveredFlatIndex)
{
    if (_anchorFlatIndex < 0 || _mode != BuildMode.Placing) return;

    var (anchorRow, anchorPos) = SegmentGrid.FromIndex(_anchorFlatIndex);
    var (hoverRow, hoverPos) = SegmentGrid.FromIndex(hoveredFlatIndex);

    // Must be same row
    if (hoverRow != anchorRow) return;

    // Calculate contiguous range from anchor to hovered
    int delta = hoverPos - anchorPos;
    // Handle wrap-around for circular ring (position 0 adjacent to position 11)
    if (delta > 6) delta -= 12;
    if (delta < -6) delta += 12;

    int newSize = Mathf.Abs(delta) + 1;
    newSize = Mathf.Clamp(newSize, 1, _selectedRoom.MaxSegments);

    if (newSize != _currentSize)
    {
        _currentSize = newSize;
        int startPos = delta >= 0 ? anchorPos : anchorPos + delta;
        // Normalize to 0-11 range
        startPos = ((startPos % 12) + 12) % 12;
        UpdateGhostPreview(anchorRow, startPos, _currentSize);
    }
}
```

### Anti-Patterns to Avoid
- **Shared material for ghost preview:** The ghost needs its own material instance with alpha transparency. Never modify the base segment material -- always create a new StandardMaterial3D per ghost mesh.
- **Physics raycasts for placement detection:** The project uses polar math (Plane.IntersectsRay + Atan2) for all segment detection. Adding collision bodies would be inconsistent and add trimesh overhead. Continue using the polar math approach.
- **Node-based state machine for 3 modes:** Overkill. An enum + switch in BuildManager is simpler and matches the project's lightweight autoload pattern.
- **Modifying SegmentInteraction directly:** Keep SegmentInteraction focused on detection. BuildManager should subscribe to a "segment clicked" event and handle mode-specific logic, not branch inside SegmentInteraction itself.
- **QueueFree for ghost preview updates:** Don't QueueFree + re-create the ghost mesh every hover frame. Instead, reuse a single MeshInstance3D and swap its Mesh property, or keep a small pool.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Tween/animation system | Custom interpolation in _Process | Godot Tween API (CreateTween) | Handles easing, chaining, kill/cleanup; already used in CreditHUD |
| Particle burst effect | Manual mesh scatter + tween per particle | GPUParticles3D with OneShot=true | GPU-accelerated, handles lifetime/fade/gravity automatically |
| Ring segment mesh generation | Custom vertex buffer code | RingMeshBuilder.CreateAnnularSector (existing) | Already handles all 6 faces, normals, arbitrary arc range |
| Cost calculation | Inline math in BuildManager | EconomyManager.CalculateRoomCost (existing) | Centralizes formula, already tested with config values |
| Refund calculation | Inline percentage in demolish handler | EconomyManager.CalculateDemolishRefund (existing) | Single source of truth for refund ratio |
| Circular adjacency math | Manual modulo everywhere | SegmentGrid helper methods (add new) | Circular wrap-around (pos 0 adjacent to pos 11) is error-prone; centralize it |

**Key insight:** The Phase 3 economy code and Phase 2 ring code already provide most of the data-layer plumbing. Phase 4 is primarily an interaction and rendering phase -- the new code is about input handling, UI, and visual feedback, not data modeling.

## Common Pitfalls

### Pitfall 1: Ghost Mesh Material Leaking to Placed Rooms
**What goes wrong:** Creating the ghost preview material, then reusing it for the confirmed room. The alpha transparency stays on the placed room.
**Why it happens:** Forgetting to create a fresh opaque material when transitioning from ghost to placed.
**How to avoid:** On confirm, create a new StandardMaterial3D with full opacity for the placed room. Never reuse the ghost material.
**Warning signs:** Placed rooms appear semi-transparent.

### Pitfall 2: Circular Adjacency Wrap-Around Bugs
**What goes wrong:** Position 11 and position 0 are treated as 11 segments apart instead of 1 segment apart. Player can't build a room spanning segments 11-0.
**Why it happens:** Naive subtraction without modular arithmetic on a 12-position ring.
**How to avoid:** Always normalize position differences: `if (delta > 6) delta -= 12; if (delta < -6) delta += 12;`
**Warning signs:** Rooms can't be placed crossing the 0/11 boundary.

### Pitfall 3: GPUParticles3D OneShot Restart Race Condition
**What goes wrong:** Calling `Emitting = true` on a one-shot GPUParticles3D immediately after it finishes doesn't re-trigger emission.
**Why it happens:** Known Godot bug -- the particle system needs Restart() called before re-enabling Emitting.
**How to avoid:** Always call `Restart()` then set `Emitting = true`. Or use a fresh GPUParticles3D instance per demolish (they're cheap to create).
**Warning signs:** Second demolish has no particle effect.

### Pitfall 4: Tween Stacking on Rapid Placement
**What goes wrong:** Placing rooms rapidly creates multiple overlapping tweens on the same node, causing jittery or corrupted animations.
**Why it happens:** Not killing previous tweens before starting new ones.
**How to avoid:** Always call `existingTween?.Kill()` before creating a new tween on the same property. The CreditHUD already demonstrates this pattern.
**Warning signs:** Rapid room placement produces visual glitches.

### Pitfall 5: Build Panel Intercepting 3D Viewport Clicks
**What goes wrong:** Clicks on the build panel toolbar also register as segment clicks in the 3D viewport, causing accidental room placement.
**Why it happens:** UI Control nodes with MouseFilter.Stop don't automatically prevent _Input from reaching Node3D nodes.
**How to avoid:** Check `GetViewport().GuiGetFocusOwner()` or use `SetInputAsHandled()` in the panel's _GuiInput. Alternatively, check if the mouse is over any UI element before processing segment clicks.
**Warning signs:** Clicking a room card in the panel also places a room on the segment behind it.

### Pitfall 6: Shared Material Between Segment Meshes and Room Meshes
**What goes wrong:** Room block uses the same StandardMaterial3D instance as the underlying segment. Dimming occupied segments during build mode also dims placed room blocks.
**Why it happens:** Sharing material references across visually independent objects.
**How to avoid:** Room blocks must have their own independent StandardMaterial3D instances, separate from segment materials. RingVisual already demonstrates per-instance materials.
**Warning signs:** Visual state changes on segments affect room blocks.

### Pitfall 7: Occupancy Not Cleaned Up on Cancelled Placement
**What goes wrong:** Player starts placing a room, segments are marked as "reserved" for preview, then presses Escape. The segments remain marked.
**Why it happens:** Preview/reservation state not cleared in the cancel path.
**How to avoid:** Only mark segments as occupied via SegmentGrid.SetOccupied on final confirm. Ghost preview should check occupancy but never modify it.
**Warning signs:** After cancelling a placement, those segments are unavailable.

## Code Examples

### Room Definition .tres Resource Pattern
```
// Create via code: new RoomDefinition() with exported properties
// Or create .tres files in Resources/Rooms/
// Example for "Bunk Pod" (Housing, 1-2 segments):

[gd_resource type="Resource" script_class="RoomDefinition"]
[ext_resource type="Script" path="res://Scripts/Data/RoomDefinition.cs" id="1"]
[resource]
script = ExtResource("1")
RoomName = "Bunk Pod"
RoomId = "bunk_pod"
Category = 0  // Housing
MinSegments = 1
MaxSegments = 2
BaseCapacity = 2
Effectiveness = 1.0
BaseCostOverride = 0  // Uses global BaseRoomCost
```

### Build Panel Category Tab Pattern
```csharp
// BuildPanel.cs -- programmatic toolbar UI
private void BuildCategoryTabs()
{
    var tabNames = new[] { "1 Housing", "2 Life Support", "3 Work", "4 Comfort", "5 Utility" };
    var categories = new[] {
        RoomDefinition.RoomCategory.Housing,
        RoomDefinition.RoomCategory.LifeSupport,
        RoomDefinition.RoomCategory.Work,
        RoomDefinition.RoomCategory.Comfort,
        RoomDefinition.RoomCategory.Utility
    };

    _tabBar = new TabBar();
    for (int i = 0; i < tabNames.Length; i++)
    {
        _tabBar.AddTab(tabNames[i]);
    }
    _tabBar.TabChanged += OnTabChanged;

    _roomCardsContainer = new HBoxContainer();
    // Room cards populated on tab change
}

private void OnTabChanged(long tabIndex)
{
    PopulateRoomCards(categories[tabIndex]);
}
```

### Demolish Confirm Popup Pattern
```csharp
// BuildManager.cs -- demolish mode click handler
private void HandleDemolishClick(int flatIndex)
{
    if (_pendingDemolishIndex == flatIndex)
    {
        // Second click = confirm demolish
        ExecuteDemolish(flatIndex);
        _pendingDemolishIndex = -1;
    }
    else
    {
        // First click = show confirm popup
        _pendingDemolishIndex = flatIndex;
        int refund = EconomyManager.Instance.CalculateDemolishRefund(_placedRoomCosts[flatIndex]);
        ShowDemolishConfirm(flatIndex, refund);
    }
}
```

### Demolish Particle Effect Pattern
```csharp
// PlacementFeedback.cs -- one-shot particle burst
public void PlayDemolishEffect(Vector3 worldPosition, Color roomColor)
{
    var particles = new GpuParticles3D();
    particles.OneShot = true;
    particles.Emitting = false;
    particles.Amount = 12;
    particles.Lifetime = 0.6f;
    particles.Explosiveness = 0.9f;

    var material = new ParticleProcessMaterial();
    material.Direction = new Vector3(0, 1, 0);
    material.Spread = 45f;
    material.InitialVelocityMin = 1.0f;
    material.InitialVelocityMax = 3.0f;
    material.Gravity = new Vector3(0, -4f, 0);
    material.ScaleMin = 0.05f;
    material.ScaleMax = 0.12f;
    material.Color = new Color(roomColor.R, roomColor.G, roomColor.B, 0.8f);

    particles.ProcessMaterial = material;
    // Use a small sphere mesh for particles
    particles.DrawPass1 = new SphereMesh { Radius = 0.04f, Height = 0.08f };

    particles.Position = worldPosition;
    GetTree().Root.AddChild(particles);

    particles.Emitting = true;
    // Self-cleanup after particles finish
    particles.Finished += () => particles.QueueFree();
}
```

### Category Color Palette (Claude's Discretion)
```csharp
// Recommended warm pastel colors per category
public static class RoomColors
{
    // Housing: warm coral/salmon
    public static readonly Color Housing = new(0.95f, 0.72f, 0.65f);
    // Life Support: soft mint/sage green
    public static readonly Color LifeSupport = new(0.65f, 0.88f, 0.75f);
    // Work: muted amber/golden
    public static readonly Color Work = new(0.92f, 0.82f, 0.55f);
    // Comfort: soft lilac/lavender
    public static readonly Color Comfort = new(0.80f, 0.70f, 0.90f);
    // Utility: warm grey-blue/slate
    public static readonly Color Utility = new(0.70f, 0.78f, 0.85f);
}
```

### Room Type Definitions (Claude's Discretion)
```
Housing (warm coral):
  - Bunk Pod (1-2 segments, height 0.25, capacity 2-4)
  - Sky Loft (1-3 segments, height 0.35, capacity 3-6)

Life Support (mint green):
  - Air Recycler (1-2 segments, height 0.22, cylindrical accent)
  - Garden Nook (1-3 segments, height 0.30, taller for "greenhouse")

Work (amber):
  - Workshop (1-2 segments, height 0.28, standard block)
  - Craft Lab (1-3 segments, height 0.32, slightly taller)

Comfort (lavender):
  - Star Lounge (1-2 segments, height 0.24, low-profile)
  - Reading Nook (1-3 segments, height 0.28, cozy proportion)

Utility (grey-blue):
  - Storage Bay (1-2 segments, height 0.20, squat/dense)
  - Comm Relay (1-3 segments, height 0.38, tallest -- antenna feel)
```

### Sound Effect Approach (Claude's Discretion)
```csharp
// Option A: Procedural AudioStreamWav generation (no asset files needed)
// Create a short sine wave "chime" at a pleasant frequency
public static AudioStreamWav GenerateChime(float frequency = 523.25f, float duration = 0.15f)
{
    int sampleRate = 22050;
    int sampleCount = (int)(sampleRate * duration);
    byte[] data = new byte[sampleCount * 2]; // 16-bit

    for (int i = 0; i < sampleCount; i++)
    {
        float t = (float)i / sampleRate;
        float envelope = 1.0f - (t / duration); // Linear decay
        float sample = MathF.Sin(2 * MathF.PI * frequency * t) * envelope * 0.5f;
        short pcm = (short)(sample * short.MaxValue);
        data[i * 2] = (byte)(pcm & 0xFF);
        data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
    }

    var stream = new AudioStreamWav();
    stream.Format = AudioStreamWav.FormatEnum.Format16Bits;
    stream.MixRate = sampleRate;
    stream.Data = data;
    return stream;
}
// Placement chime: 523 Hz (C5) -- warm, melodic
// Demolish pop: 220 Hz (A3) very short 0.05s with quick decay
// Error buzz: 110 Hz (A2) 0.1s -- low, clear rejection
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Godot 3 SceneTreeTween | Godot 4 CreateTween() (node-bound) | Godot 4.0 | Tweens bind to node lifecycle; auto-killed on QueueFree |
| Godot 3 [Signal] for C# | Pure C# event delegates | Godot 4.0+ | Avoids marshalling; project already uses this pattern exclusively |
| CPUParticles for small effects | GPUParticles3D even for small effects | Godot 4.0+ | GPU particles are standard; API is consistent with process materials |
| Manual input action mapping in project.godot | Programmatic InputMap registration in _Ready | Project convention | Avoids fragile serialization; OrbitalCamera already demonstrates this |

**Deprecated/outdated:**
- SceneTreeTween (Godot 3): Replaced by CreateTween() in Godot 4
- Godot [Signal] attribute for C#: This project avoids it due to marshalling bugs; use pure C# events

## Open Questions

1. **Sound asset format: procedural vs pre-recorded?**
   - What we know: Godot supports both AudioStreamWav (procedural) and importing .wav/.ogg files. Procedural generation avoids asset management but produces simpler tones. Pre-recorded .wav files sound richer.
   - What's unclear: Whether the user prefers procedural tones or wants asset files.
   - Recommendation: Use procedural AudioStreamWav for the initial implementation (zero external assets, full code control). If the sounds feel too "synthetic," swap in .wav files later -- the AudioStreamPlayer interface is the same either way.

2. **Ghost preview mesh recreation frequency**
   - What we know: During drag-to-resize, the ghost mesh changes size (1-3 segments). Recreating an ArrayMesh every hover frame is expensive.
   - What's unclear: Whether mesh creation is fast enough for real-time drag.
   - Recommendation: Pre-create meshes for all possible sizes (1, 2, 3 segments for each row) at init time. Swap between pre-built meshes during drag rather than generating on the fly. Only 6 meshes per room type needed (2 rows x 3 sizes).

3. **BuildManager as autoload vs scene node**
   - What we know: GameEvents and EconomyManager are autoloads. BuildManager needs to persist across scene changes and be accessible globally.
   - What's unclear: Whether the build state should survive scene transitions.
   - Recommendation: Make BuildManager an autoload for consistency with the project pattern. It coordinates multiple systems (UI, ring, economy) and needs global access.

## Sources

### Primary (HIGH confidence)
- Codebase analysis: SegmentGrid.cs, SegmentInteraction.cs, RingVisual.cs, EconomyManager.cs, GameEvents.cs, RoomDefinition.cs, CreditHUD.cs, RingMeshBuilder.cs, FloatingText.cs, SafeNode.cs, OrbitalCamera.cs
- Project scene tree: QuickTestScene.tscn, Ring.tscn, project.godot autoload configuration
- [Godot 4 C# Tween API](https://straydragon.github.io/godot-csharp-api-doc/4.3-stable/main/Godot.Tween.html) - TweenProperty, SetTrans, SetEase, TransitionType/EaseType enums
- [Godot 4 C# GPUParticles3D API](https://straydragon.github.io/godot-csharp-api-doc/4.3-stable/main/Godot.GpuParticles3D.html) - OneShot, Restart(), Finished signal, EmitParticle
- [Godot 4 AudioStreamPlayer docs](https://docs.godotengine.org/en/stable/classes/class_audiostreamplayer.html) - Non-positional audio playback
- [Godot 4 AudioStreamWav docs](https://docs.godotengine.org/en/stable/classes/class_audiostreamwav.html) - Programmatic WAV generation

### Secondary (MEDIUM confidence)
- [Godot 4 StandardMaterial3D docs](https://docs.godotengine.org/en/stable/classes/class_standardmaterial3d.html) - EmissionEnabled, Transparency enum for ghost preview
- [GPUParticles3D one-shot restart workaround](https://github.com/godotengine/godot/issues/79689) - Known bug requiring Restart() before re-enabling Emitting
- [Godot 4 TabContainer C# API](https://straydragon.github.io/godot-csharp-api-doc/4.3-stable/main/Godot.TabContainer.html) - Programmatic tab creation
- [Godot 4 UI containers overview](https://school.gdquest.com/courses/learn_2d_gamedev_godot_4/start_a_dialogue/all_the_containers) - Container layout patterns

### Tertiary (LOW confidence)
- [AudioStreamGenerator for procedural audio](https://docs.godotengine.org/en/stable/classes/class_audiostreamgenerator.html) - Real-time audio generation (alternative to AudioStreamWav; higher complexity)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All APIs are built-in Godot 4; no external dependencies. Verified against C# API docs and existing codebase patterns.
- Architecture: HIGH - BuildManager state machine, mesh-based room blocks, and programmatic UI all follow established project conventions. Every pattern has a precedent in the existing code.
- Pitfalls: HIGH - Ghost material leaking, circular adjacency wrap-around, particle one-shot restart, and tween stacking are all well-documented issues with verified workarounds.

**Research date:** 2026-03-03
**Valid until:** 2026-04-03 (stable -- Godot 4.6 APIs are mature)
