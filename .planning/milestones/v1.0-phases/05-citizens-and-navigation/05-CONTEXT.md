# Phase 5: Citizens and Navigation - Context

**Gathered:** 2026-03-03
**Status:** Ready for planning

<domain>
## Phase Boundary

Spawn named walking citizens on the ring walkway with distinct appearances, continuous circular navigation, proximity-based room visits, and a click-to-inspect popup. Navigation approach must be prototyped before full implementation (circular walkway is non-standard geometry). Citizens don't generate wishes yet (Phase 6) or respond to happiness (Phase 7) — this phase proves they exist, move, visit rooms, and are inspectable.

</domain>

<decisions>
## Implementation Decisions

### Citizen Appearance
- Colored capsule shapes (pill-shaped) — not humanoid, not abstract dots
- Body type affects capsule proportions: Tall = taller/thinner, Short = stubby, Round = wider/squatter
- Primary + secondary colors from CitizenData render as color bands on the capsule
- Skip accessories for Phase 5 — body type + two colors provides enough variety for 5-10 citizens
- Scale: small but readable — several citizens fit on a walkway segment, colors and body types distinguishable at default zoom

### Walking Behavior
- Random direction per citizen — some walk clockwise, some counter-clockwise
- Gentle vertical bob as they move (simulates walking rhythm without legs/animation)
- Slight speed variation per citizen (~±15% of base speed) to prevent uniform marching look
- 5 starter citizens spawned at game start (prototype count for Phase 5)
- Citizens walk the walkway continuously in a circle — no pausing, no reversals on the walkway itself

### Room Visits
- Drift-to-edge + fade: citizen drifts radially from walkway toward room's segment edge, then fades out (entered the room)
- After a time inside, citizen fades back in on the walkway and resumes walking
- Occasional visits — roughly every 20-40 seconds a citizen might drift into a nearby room (calm, not hectic)
- Proximity-based, any room type — citizens drift into whatever room is nearest when they decide to visit, no category preference in Phase 5
- Both inner and outer row rooms are visitable — citizen drifts toward whichever side the target room is on

### Citizen Info Panel
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

</decisions>

<specifics>
## Specific Ideas

- Capsule citizens fit the diorama/tabletop miniature aesthetic — they're game pieces walking a board
- The gentle bob sells "walking" without any skeletal animation or limbs
- Drift-to-edge + fade is deliberately simple — avoids complex off-walkway pathing while clearly showing "this citizen entered that room"
- Occasional visits keep the ring feeling calm and cozy, not frantic
- "No wish" placeholder in the popup prepares the UI for Phase 6 without blocking Phase 5

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- CitizenData (Scripts/Data/CitizenData.cs) — Resource with CitizenName, BodyType enum (Tall/Short/Round), PrimaryColor, SecondaryColor, Accessory fields
- GameEvents (Scripts/Autoloads/GameEvents.cs) — Already has CitizenArrived/CitizenDeparted events ready
- SafeNode (Scripts/Core/SafeNode.cs) — Base class for signal lifecycle; citizen nodes should extend this
- SegmentGrid (Scripts/Ring/SegmentGrid.cs) — Walkway radii (InnerRowOuter=4.0, OuterRowInner=5.0), segment positions and occupancy
- RingVisual (Scripts/Ring/RingVisual.cs) — Walkway mesh built as annular sector between radii 4.0-5.0, recessed 0.025 units
- SegmentTooltip (Scripts/UI/SegmentTooltip.cs) — Screen-space tooltip pattern for floating UI near 3D objects
- RingMeshBuilder (Scripts/Ring/RingMeshBuilder.cs) — CreateAnnularSector could be adapted for capsule mesh generation
- BuildManager (Scripts/Build/BuildManager.cs) — BuildMode enum and mode state needed to suppress citizen clicks during build mode

### Established Patterns
- Autoload singleton pattern (GameEvents.Instance, EconomyManager.Instance, BuildManager.Instance)
- Pure C# event delegates for cross-system communication
- Polar math picking (Plane.IntersectsRay + Atan2) — citizen click detection can reuse this approach
- Per-instance StandardMaterial3D for independent color control
- Programmatic UI (CreditHUD, BuildPanel, SegmentTooltip all built in code, no .tscn scenes)

### Integration Points
- Citizen nodes added to QuickTestScene as children (or a CitizenManager container node)
- CitizenManager registers as Autoload for cross-system access (citizen count needed by EconomyManager in Phase 3)
- EconomyManager already has citizen income calculation ready — needs citizen count from CitizenManager
- Click detection: citizens need their own hit detection (capsule proximity check vs. ray-plane intersection)
- BuildManager.Instance._mode check to suppress citizen clicks during Placing/Demolish modes

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 05-citizens-and-navigation*
*Context gathered: 2026-03-03*
