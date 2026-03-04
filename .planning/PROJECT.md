# Orbital Rings

## What This Is

A cozy space station builder where players construct a modular orbital ring one room at a time. The ring is a flat donut divided into 24 segments (12 outer, 12 inner) where rooms attract citizens who express wishes via speech bubbles. Fulfilling wishes raises happiness, which drives citizen arrivals and blueprint unlocks. There is no fail state — the station always grows. Built in Godot 4 with C#, targeting PC (itch.io). v1.0 shipped with a complete build-wish-grow loop on a single ring.

## Core Value

The wish-driven building loop: citizens express wishes, the player builds rooms to fulfill them, happiness rises, new citizens arrive, new wishes emerge. This loop must feel satisfying and alive.

## Requirements

### Validated

- ✓ Ring structure with 12 outer + 12 inner segments and a walkway — v1.0
- ✓ Room placement (1-3 segment sizes) across 5 categories — v1.0
- ✓ Placeholder room interiors (visually distinct per type) — v1.0
- ✓ Named citizens that walk the walkway and visit rooms — v1.0
- ✓ Wish system with speech bubbles guiding player building — v1.0
- ✓ Happiness tracking that drives citizen arrival and blueprint unlocks — v1.0
- ✓ Credit economy: passive income, work bonus, room costs, demolish refunds — v1.0
- ✓ Orbiting 3D camera with tilt adjustment — v1.0
- ✓ 10 room types across 5 categories with distinct behaviors — v1.0
- ✓ Save/load with debounced autosave — v1.0
- ✓ Ambient audio and wish celebration feedback — v1.0
- ✓ Title screen with New Station / Continue — v1.0

### Active

(None yet — define with `/gsd:new-milestone`)

### Out of Scope

- Day/night cycle — cosmetic only, not needed for core loop
- Random events (visitors, celestial events) — adds variety but not core
- Citizen personality traits and daily routines — deferred to after core loop proves out
- Citizen relationships and shared wishes — lightweight system, add later
- Procedural room interior generation — placeholder interiors work well
- Multi-ring vertical expansion — single ring is complete, expansion is v2+ territory
- Adjacency bonuses — adds min-max pressure, breaks "build what feels right" ethos
- Mobile/console — PC only with keyboard + mouse
- Tutorial/guided opening — citizens' wishes serve as implicit tutorial (validated in v1.0)

## Context

Shipped v1.0 with 8,058 LOC C# across 9 phases (25 plans) in 3 days.
Tech stack: Godot 4.4, C#, .NET 8, procedural mesh/audio generation.
Architecture: 7 Autoload singletons (GameEvents, EconomyManager, BuildManager, CitizenManager, WishBoard, HappinessManager, SaveManager) coordinated via typed C# event delegates.
Art style: Soft 3D with pastel palette, rounded capsule citizens, warm segment colors.
Full design document at `IDEA.md` covers multi-ring expansion, proc gen interiors, citizen personalities, day/night cycle — all deferred past v1.0.

## Constraints

- **Engine**: Godot 4 with C# — all gameplay logic in C#, using Godot's 3D rendering pipeline
- **Platform**: PC (keyboard + mouse), distributed via itch.io
- **No fail state**: The game must never punish the player — wishes linger, nothing bad happens from ignoring them
- **Architecture**: 7 Autoload singletons with event-driven communication via GameEvents signal bus

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Flat donut (not full torus) | Simpler geometry, easier camera/interaction, captures the ring feel | ✓ Good — polar math picking works perfectly, no physics bodies needed |
| Placeholder interiors for v1 | Focus on ring mechanics and wish loop before investing in proc gen | ✓ Good — colored blocks per category are readable and sufficient |
| Named citizens but no traits/routines | Gives the loop personality without complex AI scheduling | ✓ Good — names + wishes + walking creates enough personality |
| Start with empty ring, no tutorial | Cozy games teach through play; keeps first milestone lean | ✓ Good — wishes serve as implicit tutorial |
| Defer day/night cycle | Cosmetic system, not needed to prove core loop | ✓ Good — ambient drone provides atmosphere without visual complexity |
| Single ring for first milestone | Prove the core loop before adding vertical expansion | ✓ Good — loop is complete and satisfying on one ring |
| Pure C# events (not Godot signals) | Avoids marshalling overhead and IsConnected bugs | ✓ Good — zero signal connection issues across 9 phases |
| Polar math picking (not trimesh collision) | Zero physics overhead, no phantom hits | ✓ Good — reliable segment selection at any camera angle |
| Spreadsheet-first economy | Calibrate numbers before coding to avoid rework | ✓ Good — sqrt scaling + 1.3x happiness cap created balanced progression |
| Procedural audio (no .wav assets) | Zero external asset dependencies | ✓ Good — placement chime + wish celebration chime feel distinct and satisfying |
| Debounced autosave on state events | Prevents rapid-fire saves during batch operations | ✓ Good — save/load works reliably with 0.5s debounce |

---
*Last updated: 2026-03-04 after v1.0 milestone*
