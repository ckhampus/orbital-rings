# Feature Research

**Domain:** Cozy space station builder / management game with citizen wishes
**Researched:** 2026-03-02
**Confidence:** MEDIUM — based on genre analysis of comparable games; no direct competitor in the cozy-space-station-wish-loop niche. Findings cross-validated across multiple sources.

---

## Feature Landscape

### Table Stakes (Users Expect These)

Features cozy builder players assume exist. Missing these = product feels incomplete or wrong genre.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Free-form placement with immediate visual feedback | Every cozy builder (Townscaper, Tiny Glade, Islanders) teaches players to expect instant visual response to placement actions | LOW | Segment snapping to the ring satisfies this; the donut grid is the constraint that makes it tractable |
| No fail state / no punishment loop | Cozy genre definition: "safety, abundance, softness." Players leave if they can be punished. Islanders, Townscaper, Before We Leave all remove hard failure | LOW | Already in design; unfulfilled wishes linger harmlessly — this is correct |
| Single soft progression currency | Cozy players reject multi-resource economies. Before We Leave, Fabledom both reduce economy to 1–2 simple resources. Complexity = stress = not cozy | MEDIUM | Credits alone is right; happiness as a second "currency" driving unlocks is the well-established cozy pattern |
| Visible named characters that feel alive | Spiritfarer, Stardew Valley, Fabledom: named NPCs with persistent identity are table stakes for the "community" subgenre of cozy. Anonymous pawns break emotional connection | MEDIUM | Named citizens walking the ring and entering rooms establishes this. Even minimal animations matter enormously |
| Satisfying build placement audio/visual feedback | Townscaper's "snap" sound is cited repeatedly as core to its appeal. Cozy games use sound to signal reward. Placement must feel good to touch | LOW | Snap sound + brief animation on room placement. Cannot ship without this — it IS the feel |
| Readable room diversity at a glance | Players need to see at a glance what each room is. Identical-looking rooms with different labels = frustration | MEDIUM | Five categories with visually distinct silhouettes/colors (placeholder art must still be distinct-per-type) |
| Clear wish / request display | Players cannot respond to citizen needs they cannot read. Speech bubbles, a wish board, or a sidebar notification — any format works, but wishes must be visible and legible | LOW | Speech bubbles above citizens are the simplest implementation. A persistent wish list panel is v1.x |
| Graceful unlock progression | Players expect to unlock new options as they grow. Islanders uses pack selection; Fabledom uses happiness milestones. Blank slate forever = boredom | MEDIUM | Happiness-gated blueprint unlocks are the right shape. Must have at least 2–3 unlock moments in v1 |
| Orbiting/panning camera with zoom | Standard expectation from any 3D builder. Fixed-tilt diorama cameras (Fabledom, Aven Colony) are the norm for cozy 3D. Free-look is bonus | MEDIUM | Horizontal orbit + zoom is v1. Vertical pan becomes necessary only when second ring arrives |
| Demolish / rearrange without heavy penalty | Cozy players experiment. Penalizing demolition is anti-cozy. Fabledom, Before We Leave, and Islanders all allow this freely | LOW | Partial refund on demolish is correct; full refund would remove all economic tension |
| Ambient visual "aliveness" | Tiny Glade's ivy, Townscaper's water, Slime Rancher's slimes wandering — something must move in the world when the player is idle | MEDIUM | Citizens walking the walkway and entering rooms provides this. Idle ambient motion on citizens is essential |

---

### Differentiators (Competitive Advantage)

Features that set Orbital Rings apart from other cozy builders. Not assumed by genre players, but create distinctive identity and word-of-mouth.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| The ring as constrained canvas | The donut ring is a unique puzzle: 24 segments, inner + outer rows, spatial scarcity by design. Islanders uses scoring pressure; Orbital Rings uses *geometric constraint* as creative tension without punishment | MEDIUM | This is the core differentiator. The ring shape forces meaningful layout decisions that flat grids don't. Lean into it — highlight the geometry in all communication |
| Citizen wish → build → growth feedback loop | Spiritfarer does wishes as quests; Fabledom does needs as meters. Orbital Rings' version — passive speech-bubble wishes that reward but never punish — is a gentler and more personal take. Citizens have names and individual wishes, not just aggregate "food: 40%" | HIGH | This is the second core differentiator. Every other space builder (Aven Colony, Space Station Tycoon) tracks aggregate morale metrics, not individual named wishes. The personal scale is the bet |
| Space setting for a cozy genre | Cozy builders default to pastoral/medieval. Space = rare setting for coziness. Before We Leave and Aven Colony exist but both lean simulation-heavy. A genuinely soft-3D cozy space station is an underserved position | LOW | Low implementation complexity (it's an art/tone choice), HIGH market differentiation. The pastel-palette space diorama is visually distinctive |
| Modular ring expansion as milestone celebration | Completing the first ring feels like an achievement. Adding a new ring stacked on top gives the player a moment of "look what I built." This is more legible as progress than watching a stat counter increment | HIGH | Deferred to v2+, but the *architecture* of the ring system must support it. Single ring for v1 must leave expansion hooks |
| Citizens visiting specific rooms (spatial preference) | When a citizen walks past three empty rooms and enters the Observatory, it shows they care about that room. Spatial preference feedback (citizens gravitating toward fulfilled wishes) makes the world readable and rewarding without a UI tooltip | MEDIUM | Requires pathing that respects room preferences, not just random walking. Medium complexity, HIGH emotional payoff |
| No tutorial — learn through citizen wishes | Cozy games increasingly remove tutorials (Tiny Glade has none; Islanders minimal). Citizens become the teaching mechanism: a wish for a Café means "build a Café." This is novel as a pure implicit tutorial design | LOW | Low implementation cost once wishes exist. Eliminates tutorial debt. Aligns with the PROJECT.md decision to start with empty ring |

---

### Anti-Features (Commonly Requested, Often Problematic)

Features that seem beneficial but would undermine the cozy loop or create unacceptable scope risk for v1.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Real-time resource depletion / maintenance costs | "Depth" — players expect management games to have upkeep | Introduces punishment loop. Citizens leaving, power failures, debt — all break cozy promise. Aven Colony's elections mechanic is frequently cited as stressful | Passive income only. Economy is a pacing tool, not a threat mechanism |
| Multiple resource types (power, oxygen, water) | Aven Colony, Space Station Alpha model this. Feels thematic for a space station | Complexity without coziness payoff. Every resource type is another meter to watch, another way to fail | Single credit currency. Life Support rooms exist as *aesthetic/purpose categories* but don't gate a separate oxygen meter in v1 |
| Combat / threat events (asteroids, pirates) | "Keeps the game interesting long-term" | Directly contradicts the cozy promise. Players choosing a cozy space builder are opting OUT of threat management | Random celestial events (comet, nebula) are the positive-only equivalent — deferred to v1.x |
| Complex citizen scheduling and pathfinding | "Realism" — citizens should have full daily routines, work shifts, queuing | Before We Leave had complex colonist routing; it was cited as a source of frustration when it broke | Citizens walk the walkway, visit rooms when nearby. Simplified rule-based "attraction" is enough for v1 |
| Adjacency bonuses (explicit numbers) | Islanders uses proximity bonuses heavily. Players who know Islanders may request this | Adds optimization pressure — players start min-maxing placement instead of building what feels right. Cozy builders (Tiny Glade, Townscaper) are explicitly non-optimal | Optional later feature. For v1: rooms work the same regardless of neighbors. Simple adjacency preference (citizens prefer rooms near their housing) is emotional, not numerical |
| Multiplayer / shared stations | "Social" — building with friends is cozy | Scope explosion. Netcode, conflict resolution, session management. Townscaper has a solo-only design that works perfectly. Cozy builders are fundamentally solo experiences | Stay solo. Sharing screenshots and letting others view your station design is the social layer |
| Save slots / cloud save complexity | "Quality of life" | Not anti-feature per se, but scope risk for v1. Autosave-only keeps initial scope lean | Single autosave for v1. Save slots are v1.x |
| In-depth citizen relationship graphs / romance | Stardew Valley envy — players may want deep NPC relationship trees | Heavy content creation burden. Spiritfarer succeeded with deep NPC stories but as a team investment, not a mechanic template | Named citizens with individual wishes creates perceived personality without requiring authored relationship arcs in v1 |
| Mobile / controller support | Player requests to play on couch or tablet | Platform and input abstraction is significant scope. PC keyboard+mouse is one interaction model | PC-first only. Scoped clearly in PROJECT.md |

---

## Feature Dependencies

```
[Credit Economy]
    └──enables──> [Room Placement]
                      └──enables──> [Room Diversity (5 categories)]
                                        └──enables──> [Citizen Wish Loop]
                                                          └──enables──> [Happiness Tracking]
                                                                            └──enables──> [Blueprint Unlocks]

[Ring Segment Grid]
    └──enables──> [Room Placement]
    └──enables──> [Camera: Orbit]

[Named Citizens]
    └──enables──> [Citizen Wish Loop]
    └──enables──> [Citizens Walking Walkway]

[Citizens Walking Walkway]
    └──enables──> [Citizens Visiting Rooms]
                      └──enhances──> [Ambient Aliveness]

[Happiness Tracking] ──enables──> [Citizen Arrival (passive growth)]
[Happiness Tracking] ──enables──> [Blueprint Unlocks]

[Blueprint Unlocks] ──enhances──> [Room Diversity]

[Room Placement] ──requires──> [Demolish / Rearrange]

[Camera: Orbit] ──is prerequisite for──> [Camera: Vertical Pan]
    (vertical pan only needed when multi-ring arrives — deferred to v2+)

[Citizens Visiting Rooms] ──conflicts with complexity of──> [Full Daily Scheduling / Pathfinding]
    (spatial attraction model chosen instead — see anti-features)
```

### Dependency Notes

- **Credit Economy requires being designed before Room Placement:** Room costs and income rates must be balanced before rooms are playable. Under-designed economy = either trivially easy (no pacing) or punishing (not cozy).
- **Citizen Wish Loop requires both Named Citizens and Room Diversity:** A wish for a room type only makes sense if that room type either exists (build more) or can be unlocked. Wishes targeting unavailable room types need to be gated by blueprint unlock state.
- **Blueprint Unlocks require Happiness Tracking:** Happiness is the unlock key. Happiness tracking must be designed and observable before unlocks feel meaningful.
- **Ambient Aliveness enhances Citizens Walking Walkway:** The walking loop is the minimum. Room entry animations, idle behaviors inside rooms, and reaction to wish fulfillment all enhance it — but are additive, not prerequisite.
- **Demolish / Rearrange has no prerequisite:** Can be built alongside placement. Should ship with placement to eliminate any punishing feeling in early testing.

---

## MVP Definition

### Launch With (v1 — Single Ring)

Minimum viable to prove the core build-wish-grow loop on one ring.

- [ ] **Segment grid + room placement (1–3 segment sizes)** — Without this, nothing else exists. The ring is the game.
- [ ] **Credit economy (passive income + room costs + partial demolish refund)** — Pacing requires economics. Keep it simple: 1 currency, no debt possible.
- [ ] **5 room categories with visually distinct placeholder art** — Players must be able to read the ring at a glance.
- [ ] **Named citizens that walk the walkway and enter rooms** — Emotional core. Static citizens kill the cozy feel.
- [ ] **Citizen wish system with speech bubbles** — The defining differentiator. Must ship with v1 or the game has no identity.
- [ ] **Happiness tracking driving citizen arrival + blueprint unlocks** — The growth half of build-wish-grow. Must have at least 2–3 unlock moments.
- [ ] **Satisfying placement audio + visual feedback** — Non-negotiable table stakes for the genre. This is feel, not feature.
- [ ] **Orbiting 3D camera with zoom** — Playable without this; shippable without this. But the diorama fantasy requires it.
- [ ] **Demolish rooms with partial refund** — Prevents any punishing trap-states in early layout experimentation.

### Add After Validation (v1.x)

Features to add once core loop is confirmed fun.

- [ ] **Persistent wish board / notification panel** — Supplement speech bubbles with a scannable list. Add once citizens number > 10 and bubbles become noise.
- [ ] **Room-entry animations + citizen reactions to wish fulfillment** — Deepens the emotional payoff of fulfilling a wish. Deferred until core loop validated.
- [ ] **Random positive events (celestial, visitor, milestone)** — Adds variety without threatening safety. Requires the core loop to feel worth interrupting.
- [ ] **Day/night ambient lighting cycle** — Cosmetic. Adds warmth and rhythm, zero mechanical weight. Add when art pass happens.
- [ ] **Save slots** — Single autosave sufficient for early access; multiple slots become valuable once sessions get long.

### Future Consideration (v2+)

Features to defer until product-market fit is established.

- [ ] **Multi-ring vertical expansion** — The architecture should support it, but do not build it for v1. One ring must stand on its own.
- [ ] **Citizen personality traits and daily routines** — Adds depth and replay, but is a content/design investment. Prove the base wish loop first.
- [ ] **Citizen relationships and shared wishes** — Spiritfarer-style emotional depth. Requires player attachment to individual citizens first — which requires time in the game.
- [ ] **Procedural room interiors** — Design doc calls for this as differentiator. High effort, high delight. Deferred correctly — placeholder interiors ship v1.
- [ ] **Adjacency bonuses (as optional optimization layer)** — Can be added without breaking the non-punishing design if framed as bonus, not penalty for absence.

---

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Segment grid + room placement | HIGH | MEDIUM | P1 |
| Named citizens walking walkway | HIGH | MEDIUM | P1 |
| Citizen wish system (speech bubbles) | HIGH | MEDIUM | P1 |
| Credit economy (single currency) | HIGH | LOW | P1 |
| Satisfying placement audio/visual feedback | HIGH | LOW | P1 |
| 5 distinct room categories (placeholder art) | HIGH | MEDIUM | P1 |
| Happiness tracking + blueprint unlocks | HIGH | MEDIUM | P1 |
| Orbiting 3D camera with zoom | HIGH | MEDIUM | P1 |
| Demolish with partial refund | MEDIUM | LOW | P1 |
| Citizens entering rooms (spatial attraction) | HIGH | MEDIUM | P2 |
| Wish board / notification panel | MEDIUM | LOW | P2 |
| Room-entry animations / wish fulfillment reaction | HIGH | HIGH | P2 |
| Day/night ambient lighting | MEDIUM | MEDIUM | P2 |
| Random positive events | MEDIUM | MEDIUM | P2 |
| Citizen personality traits | HIGH | HIGH | P3 |
| Procedural room interiors | HIGH | HIGH | P3 |
| Multi-ring expansion | HIGH | HIGH | P3 |
| Citizen relationships | MEDIUM | HIGH | P3 |
| Adjacency bonuses | LOW | MEDIUM | P3 |

**Priority key:**
- P1: Must have for v1 (single ring launch)
- P2: Should have, add in v1.x after core loop validated
- P3: Nice to have, future consideration v2+

---

## Competitor Feature Analysis

| Feature | Townscaper | Islanders | Before We Leave | Aven Colony | Orbital Rings Approach |
|---------|------------|-----------|-----------------|-------------|------------------------|
| Fail state | None | Soft (score-based, can restart) | None (no combat) | Hard (voted out) | None — wishes linger harmlessly |
| Economy | None | None (score only) | Single resource (food/water) | Multi-resource + electricity | Single currency (credits) |
| Citizens/NPCs | None | None | "Peeps" (anonymous aggregate) | Colonists (aggregate mood) | Named individual citizens with wishes |
| Wish/request system | None | None | Needs meters | Morale factors menu | Individual speech bubbles |
| Camera | Free orbit | Mostly fixed | Isometric | Fixed isometric | Fixed-tilt orbit + zoom |
| Progression | None (pure toy) | Score → island unlock | Island expansion | Building unlock tree | Happiness → blueprints → citizens |
| Room/building diversity | None (colors only) | ~8 building types | ~20 building types | 50+ building types | 5 categories, starter set ~10 types |
| Sound design | Minimal procedural | Minimal | Ambient soundtrack | Full soundtrack | Satisfying snap + ambient soundtrack |
| Space setting | No | No | No (planetary) | Yes (alien planet) | Yes — but cozy, not simulation-heavy |

**Key observation:** No competitor combines (1) named individual citizens with wishes, (2) a genuinely cozy no-punishment loop, and (3) a space setting with soft-3D aesthetics. Aven Colony is closest on setting but is a management sim, not a cozy toy. This is a genuine gap.

---

## Sources

- [Islanders (video game) - Wikipedia](https://en.wikipedia.org/wiki/Islanders_(video_game)) — MEDIUM confidence (Wikipedia summary of known game)
- [Townscaper on Steam](https://store.steampowered.com/app/1291340/Townscaper) — HIGH confidence (primary source)
- [How Townscaper Works - Game Developer](https://www.gamedeveloper.com/game-platforms/how-townscaper-works-a-story-four-games-in-the-making) — MEDIUM confidence (developer postmortem)
- [Before We Leave on Steam](https://store.steampowered.com/app/1073910/Before_We_Leave/) — HIGH confidence (primary source)
- [Before We Leave Review - LadiesGamers](https://ladiesgamers.com/before-we-leave-review-2/) — MEDIUM confidence (single review, cross-referenced)
- [Aven Colony on Steam](https://store.steampowered.com/app/484900/Aven_Colony/) — HIGH confidence (primary source)
- [Tiny Glade on Steam](https://store.steampowered.com/app/2198150/Tiny_Glade) — HIGH confidence (primary source)
- [Tiny Glade Review - PC Gamer](https://www.pcgamer.com/games/city-builder/tiny-glade-review/) — MEDIUM confidence (editorial review)
- [Fabledom Review - Game8](https://game8.co/articles/reviews/fabledom-review) — MEDIUM confidence (editorial review)
- [Fabledom - RoundTable Co-Op](https://roundtablecoop.com/reviews/fabledom-full-release-review-cozy-charming-and-chilled/) — MEDIUM confidence (editorial review)
- [Designing for Coziness - Game Developer](https://www.gamedeveloper.com/design/designing-for-coziness) — HIGH confidence (industry publication, Kitfox Games designer article)
- [Cozy Games - Lostgarden](https://lostgarden.com/2018/01/24/cozy-games/) — HIGH confidence (foundational genre theory, widely cited)
- [Spiritfarer Wiki - Requests](https://spiritfarer.fandom.com/wiki/Category:Requests) — MEDIUM confidence (community wiki)
- [Balancing Relaxation and Engagement in Cozy Game Mechanics - SDLC Corp](https://sdlccorp.com/post/balancing-game-mechanics-for-relaxation-and-engagement-in-cozy-games/) — LOW confidence (industry blog, unverified claims)
- [The Impact of Audio Design on the Cozy Gaming Experience - SDLC Corp](https://sdlccorp.com/post/the-impact-of-audio-design-on-the-cozy-gaming-experience/) — LOW confidence (industry blog)
- [Cozy city builders are finally letting me design pretty places in peace - PC Gamer](https://www.pcgamer.com/cozy-city-builders-are-finally-letting-me-design-pretty-places-in-peace/) — MEDIUM confidence (editorial)
- [The 10 Best Cozy City-Building Games - The Cozy Gaming Nook](https://thecozygamingnook.com/the-10-best-cozy-city-building-games/) — LOW confidence (community list)

---

*Feature research for: Cozy space station builder / management game (Orbital Rings)*
*Researched: 2026-03-02*
