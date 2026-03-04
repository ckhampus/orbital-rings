# Phase 6: Wish System - Context

**Gathered:** 2026-03-03
**Status:** Ready for planning

<domain>
## Phase Boundary

Citizens generate wishes expressed as icon badges floating above their capsules; wishes remain visible and harmless until the citizen visits a matching room; wishes span all four categories (social, comfort, curiosity, variety). WishBoard Autoload tracks active wishes. Happiness effects from fulfillment are Phase 7 — this phase proves wishes generate, display, and resolve through citizen-room visits.

</domain>

<decisions>
## Implementation Decisions

### Badge Appearance
- Icon-only badge (not speech bubble or thought cloud) — small circular icon floating above the citizen
- 3D billboard Sprite3D that always faces the camera — lives in world space, scales naturally with zoom
- Positioned directly above the citizen's head with a gentle bob, moves with the citizen
- Each wish category has a distinct Tabler icon exported as 64x64 PNG (Social, Comfort, Curiosity, Variety)
- Pre-exported PNGs loaded as Sprite3D textures from a Resources/Icons folder
- On wish fulfillment: badge does a quick scale-up + fade-out "pop" animation, then disappears

### Wish Generation Timing
- Citizens get their first wish within 30-60 seconds of spawning (not immediate, not long delay)
- One wish at a time per citizen — single badge, simple and clear
- Multiple citizens can independently wish for the same room type (duplicates allowed)
- Building one matching room fulfills ALL citizens with that wish (when they visit it — see matching below)
- After fulfillment, 30-90 second cooldown before citizen generates a new wish

### Wish-to-Room Matching
- Wishes fulfill when the citizen visits (drifts into) a matching room — not just when the room exists
- Citizens with active wishes preferentially visit matching room types (weighted proximity check, not exclusive)
- When a matching room is newly built, citizens with that wish get their visit timer reset to a short delay (~5-10 seconds) — "nudge" for responsive feedback loop
- WishBoard Autoload tracks all active wishes internally — no player-facing board UI in Phase 6 (QOLX-01 deferred to v2)

### Wish Content and Categories
- Wish text shown in CitizenInfoPanel (replaces "No wish" placeholder) uses personal, cozy first-person tone
- "I'd love a place to stargaze", "A cozy spot to read would be nice" — hints at room type without naming it explicitly
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

</decisions>

<specifics>
## Specific Ideas

- Icon badges fit the diorama/tabletop miniature aesthetic — tiny floating markers above game pieces, not cluttering speech bubbles
- The "nudge" mechanic (visit timer reset on room build) is key to making the build→fulfillment loop feel responsive: player builds a room, citizen walks over within seconds, badge pops — satisfying
- Hint-based wish text encourages the player to explore the build panel and make connections ("stargaze" → Observatory) without hand-holding
- Tabler icons from https://tabler.io/icons — pre-export as PNGs for simplicity
- Since multiple wishes can target the same room type, building a popular room creates a cascade of fulfillments — multiple pops in quick succession feels rewarding

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- WishTemplate (Scripts/Data/WishTemplate.cs) — Resource with WishCategory enum, TextVariants[], FulfillingRoomIds[], already fully defined
- GameEvents (Scripts/Autoloads/GameEvents.cs) — WishGenerated/WishFulfilled events already stubbed with (citizenName, wishType) signatures
- CitizenNode (Scripts/Citizens/CitizenNode.cs) — Visit system with Timer, drift-fade animation, proximity-based room targeting — needs wish-aware visit weighting
- CitizenInfoPanel (Scripts/UI/CitizenInfoPanel.cs) — Already shows "No wish" placeholder, needs real wish text + category display
- CitizenManager (Scripts/Citizens/CitizenManager.cs) — Manages citizen list, click detection, selection glow — WishBoard can query citizens through this
- RoomDefinition (Scripts/Data/RoomDefinition.cs) — RoomId field used by WishTemplate.FulfillingRoomIds for matching

### Established Patterns
- Autoload singleton pattern (GameEvents.Instance, EconomyManager.Instance, BuildManager.Instance, CitizenManager.Instance) — WishBoard follows same pattern
- Pure C# event delegates for cross-system communication (WishGenerated/WishFulfilled already defined)
- Timer child node for periodic actions (visit timer in CitizenNode, income tick in EconomyManager) — wish generation timer follows same pattern
- Per-instance StandardMaterial3D for independent visual control
- Tween-based animations with kill-before-create pattern (visit sequence in CitizenNode) — badge pop animation follows same approach
- Programmatic node creation (no .tscn scenes) — badge Sprite3D created in code

### Integration Points
- CitizenNode needs: wish state (current WishTemplate), badge Sprite3D child, wish-aware visit targeting, visit timer reset on room build notification
- CitizenInfoPanel needs: wish text display replacing "No wish" placeholder
- GameEvents.RoomPlaced event → WishBoard listens to trigger visit timer resets on matching citizens
- WishBoard Autoload → registers active wishes, listens for WishGenerated/WishFulfilled, provides query API
- BuildManager._placedRooms → WishBoard or CitizenNode needs to check which room types exist for matching (or listen to RoomPlaced/RoomDemolished events)

</code_context>

<deferred>
## Deferred Ideas

- Persistent wish board / notification panel (QOLX-01) — v2 requirement, tracked in REQUIREMENTS.md
- Wish fulfillment happiness boost — Phase 7 (WISH-02 happiness effect wired there)

</deferred>

---

*Phase: 06-wish-system*
*Context gathered: 2026-03-03*
