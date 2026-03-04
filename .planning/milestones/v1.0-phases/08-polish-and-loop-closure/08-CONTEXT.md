# Phase 8: Polish and Loop Closure - Context

**Gathered:** 2026-03-04
**Status:** Ready for planning

<domain>
## Phase Boundary

The full build-wish-grow loop feels cozy and complete. Save/load lets the player quit and resume seamlessly. Ambient audio sets the space station tone. The HUD is fully wired (credits, happiness, population all update in real time). The wish fulfillment moment produces a noticeable positive feedback response that feels like a small reward. After 30 minutes of play, the game feels cozy — no punishment, no stress, visible growth, and a genuine desire to keep building.

</domain>

<decisions>
## Implementation Decisions

### Save/Load
- Autosave on every state change (room placed, room demolished, wish fulfilled, citizen arrived, happiness changed) — player can quit anytime and lose nothing
- Single autosave slot — one station, one save (multiple slots deferred to v2 per QOLX-02)
- JSON format using System.Text.Json — human-readable, easy to debug, matches existing data patterns
- Full state persisted: ring layout (segment occupancy, room types, sizes), citizens (names, colors, walkway positions, current wishes, visit state), happiness value, credits balance, unlocked blueprints
- Citizens resume at exact walkway positions — truly seamless resume
- Corrupted/incompatible save files fall back to new station with a brief warning message — no crash

### Title Screen
- Simple title screen shown on launch: "Orbital Rings" title text over a dark background
- Two buttons: "Continue" (only shown if save exists) and "New Station"
- "New Station" requires confirmation dialog: "Start a new station? Your current station will be lost."
- No save preview on Continue button — just the option to continue
- Minimal visual design — text on dark, clean and fast to build

### Ambient Soundscape
- Procedural space drone: soft, low-frequency hum with gentle modulation — like the distant hum of a space station life support
- Procedurally generated (matches zero-asset audio pattern from PlacementFeedback.cs)
- Static drone — does not change with station state (no evolving soundscape for v1)
- Continuous loop from game start
- Existing placement snap sound (523 Hz chime) is fine as-is — no adjustment needed
- Simple mute toggle button/key for all audio

### Wish Fulfillment Celebration
- Warm chime sound on every wish fulfillment — distinct from placement chime (523 Hz), recognizable reward tone
- Same chime every time — becomes the signature "reward sound" the player associates with progress
- Gold/yellow sparkle particle burst at the citizen's 3D position — universal reward color, spatial feedback
- Combined with existing effects: badge pop animation + "+X%" floating text on happiness bar
- Total fulfillment feedback stack: chime + gold sparkles at citizen + badge pop + "+X%" text

### Claude's Discretion
- Ambient drone frequency, harmonics, and modulation parameters
- Wish fulfillment chime frequency and envelope (must feel warm and distinct from placement chime)
- Particle burst count, spread, size, and fade timing for wish fulfillment sparkles
- Mute toggle UI placement and key binding
- Title screen font, layout, button styling
- Save file location (user://saves/ or similar Godot convention)
- Autosave debouncing strategy (if rapid state changes occur)
- JSON serialization structure and schema
- Any additional HUD integration gaps discovered during implementation

</decisions>

<specifics>
## Specific Ideas

- Autosave on every state change matches the cozy promise: the game is always safe, no anxiety about losing progress
- Title screen with Continue/New Station gives the player a moment of re-entry — "oh right, my station"
- Procedural space drone maintains the zero-external-asset pattern established in Phase 4 — the entire game ships as pure code
- Gold sparkles at the citizen create a personal moment — "this citizen is happy because of what I built"
- The total fulfillment stack (chime + sparkles + badge pop + "+X%") creates layered feedback without any single element being over-the-top
- Mute toggle covers the basic need; volume sliders can come in v2

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- PlacementFeedback.cs: GenerateTone() static method for procedural audio (sine wave + linear decay envelope) — reuse for ambient drone and wish fulfillment chime
- PlacementFeedback.cs: GPUParticles3D one-shot pattern with Restart()+Emitting workaround and Finished self-cleanup — reuse for wish fulfillment sparkle particles
- FloatingText.cs: Drift-up-and-fade text pattern — already used for "+X%" happiness text
- GameEvents: WishFulfilled(string, string) event — subscribe for celebration trigger
- GameEvents: All state change events available for autosave triggers (RoomPlaced, RoomDemolished, WishFulfilled, CitizenArrived, HappinessChanged, CreditsChanged, BlueprintUnlocked)
- CreditHUD, HappinessBar, PopulationDisplay: All three HUD elements exist and update in real time already

### Established Patterns
- Autoload singleton pattern (GameEvents.Instance, EconomyManager.Instance, etc.) — SaveManager follows same pattern
- Pure C# event delegates for cross-system communication
- Procedural AudioStreamWav generation for all feedback sounds (zero external assets)
- Tween-based animations with kill-before-create
- Programmatic UI (all HUD elements built in code, no .tscn scenes)
- Per-instance StandardMaterial3D to avoid shared-material contamination

### Integration Points
- SaveManager registers as Autoload — subscribes to all state-change events for autosave
- SaveManager serializes: SegmentGrid occupancy, BuildManager placed rooms, CitizenManager citizen list (positions, wishes, colors), HappinessManager happiness value + unlock state, EconomyManager credits balance
- SaveManager loads on title screen "Continue" — restores all Autoload state before scene enters game
- AmbientAudio node (Autoload or child of main scene) — starts on game scene enter, procedural AudioStreamWav loop
- WishCelebration subscribes to GameEvents.WishFulfilled — plays chime + spawns gold particles at citizen position
- Title screen scene replaces QuickTestScene as main scene entry point
- project.godot main scene changes to title screen

</code_context>

<deferred>
## Deferred Ideas

- Multiple save slots (QOLX-02) — v2 requirement
- Volume slider / settings panel — v2
- Evolving ambient soundscape that changes with station state — v2
- Background music / composed audio tracks — v2

</deferred>

---

*Phase: 08-polish-and-loop-closure*
*Context gathered: 2026-03-04*
