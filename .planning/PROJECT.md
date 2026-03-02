# Orbital Rings

## What This Is

A cozy space station builder where players construct a modular station one ring at a time. Each ring is a flat donut divided into 24 segments (12 outer, 12 inner) where the player places rooms to attract citizens and fulfill their wishes. There is no fail state — the station always grows, just faster or slower depending on how well the player responds to citizen desires. Built in Godot 4 with C#, targeting PC (itch.io).

## Core Value

The wish-driven building loop: citizens express wishes, the player builds rooms to fulfill them, happiness rises, new citizens arrive, new wishes emerge. This loop must feel satisfying and alive.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] Ring structure with 12 outer + 12 inner segments and a walkway
- [ ] Room placement (1-3 segment sizes) across 5 categories: Housing, Life Support, Work, Comfort, Utility
- [ ] Placeholder room interiors (visually distinct per type, no procedural generation yet)
- [ ] Named citizens that walk the walkway and visit rooms
- [ ] Wish system with speech bubbles guiding player building
- [ ] Happiness tracking that drives citizen arrival and blueprint unlocks
- [ ] Credit economy: citizens generate income, rooms cost credits, demolish refunds partial cost
- [ ] Orbiting 3D camera around the flat donut ring
- [ ] Starter set of room types with distinct behaviors

### Out of Scope

- Day/night cycle — deferred, cosmetic only, not needed for core loop
- Random events (visitors, celestial events, milestones) — adds variety but not core
- Citizen personality traits and daily routines — deferred to after core loop works
- Citizen relationships and shared wishes — lightweight system, add later
- Procedural room interior generation — placeholder interiors first, proc gen later
- Multi-ring vertical expansion — first milestone is one playable ring only
- Adjacency bonuses — explicitly noted as future in design doc
- Room rearrangement animations — demolish/rebuild is fine for now
- Mobile/console — PC only with keyboard + mouse
- Tutorial/guided opening — player starts with empty ring and credits, figures it out

## Context

- Starting from a blank Godot project — no existing game code
- Ring is a flat donut shape in 3D space (not a full torus), viewed with an orbiting camera at fixed tilt
- Art style target is soft 3D: rounded geometry, gentle lighting, pastel palette, diorama feel
- Full design document available at `IDEA.md` covering the complete vision including multi-ring expansion, proc gen interiors, citizen personalities, day/night cycle, and random events — all deferred past the first milestone
- The codebase has a .NET/C# devcontainer environment already configured

## Constraints

- **Engine**: Godot 4 with C# — all gameplay logic in C#, using Godot's 3D rendering pipeline
- **Platform**: PC (keyboard + mouse), distributed via itch.io
- **First milestone scope**: One playable ring proving the core build-wish-grow loop
- **No fail state**: The game must never punish the player — wishes linger, nothing bad happens from ignoring them

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Flat donut (not full torus) | Simpler geometry, easier camera/interaction, captures the ring feel | — Pending |
| Placeholder interiors for v1 | Focus on ring mechanics and wish loop before investing in proc gen | — Pending |
| Named citizens but no traits/routines | Gives the loop personality without complex AI scheduling | — Pending |
| Start with empty ring, no tutorial | Cozy games teach through play; keeps first milestone lean | — Pending |
| Defer day/night cycle | Cosmetic system, not needed to prove core loop | — Pending |
| Single ring for first milestone | Prove the core loop before adding vertical expansion | — Pending |

---
*Last updated: 2026-03-02 after initialization*
