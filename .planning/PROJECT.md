# Orbital Rings

## What This Is

A cozy space station builder where players construct a modular orbital ring one room at a time. The ring is a flat donut divided into 24 segments (12 outer, 12 inner) where rooms attract citizens who express wishes via speech bubbles. Fulfilling wishes grows lifetime happiness (permanent progress) and raises station mood (dynamic feel), with five mood tiers driving citizen arrivals and economy. There is no fail state — the station always grows. Built in Godot 4 with C#, targeting PC (itch.io). v1.1 shipped with the complete dual-value happiness system.

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
- ✓ Dual-value happiness: Lifetime Happiness (wish counter) + Station Mood (fluctuating float with decay) — v1.1
- ✓ Blueprint unlocks keyed to wish counts (4, 12) instead of percentage thresholds — v1.1
- ✓ Five mood tiers (Quiet/Cozy/Lively/Vibrant/Radiant) with hysteresis — v1.1
- ✓ Tier-driven citizen arrivals and economy income multiplier (1.0x–1.4x) — v1.1
- ✓ HUD shows lifetime wish counter and mood tier label with tier-colored text — v1.1
- ✓ Save format v2 with version-gated backward compatibility — v1.1

### Active

- [ ] Citizens assigned to housing rooms with automatic home assignment
- [ ] Size-scaled housing capacity (base + segments - 1)
- [ ] Return-home behavior with Zzz floater (90–150s cycle)
- [ ] Citizen info panel shows home room and location
- [ ] Room tooltip shows current residents
- [ ] New HousingManager autoload singleton
- [ ] HousingConfig resource for tunable timing constants
- [ ] Unhoused citizens handled gracefully (no penalty, no home cycle)
- [ ] Save/load housing assignments

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
- Raw mood float in player UI — optimization anxiety is anti-cozy (hide numeric values)
- Housing quality tiers or upgrades — no management burden, cozy philosophy
- Player-managed room assignments — fully automatic, no micromanagement
- Citizen preferences or roommate compatibility — deferred complexity
- Room interior customization — placeholder interiors sufficient
- Tier change notification — deferred from v1.1, not in v1.2 scope
- Save migration v1→v2 — deferred from v1.1, not in v1.2 scope
- Additional blueprint unlocks (30, 50, 100) — deferred, not in v1.2 scope
- Cosmetic unlocks tied to lifetime milestones — deferred, not in v1.2 scope

## Context

Shipped v1.1 with 8,331 LOC C# across 13 phases (32 plans) in 5 days.
Tech stack: Godot 4.4, C#, .NET 8, procedural mesh/audio generation.
Architecture: 7 Autoload singletons (GameEvents, EconomyManager, BuildManager, CitizenManager, WishBoard, HappinessManager, SaveManager) coordinated via typed C# event delegates.
Art style: Soft 3D with pastel palette, rounded capsule citizens, warm segment colors.
Full design document at `IDEA.md` covers multi-ring expansion, proc gen interiors, citizen personalities, day/night cycle — all deferred.
v1.1 replaced the single happiness float with dual-value system (MoodSystem POCO + HappinessManager refactor) and tier-space economy. HappinessBar UI fully replaced by MoodHUD. Save format versioned at v2.

## Constraints

- **Engine**: Godot 4 with C# — all gameplay logic in C#, using Godot's 3D rendering pipeline
- **Platform**: PC (keyboard + mouse), distributed via itch.io
- **No fail state**: The game must never punish the player — wishes linger, nothing bad happens from ignoring them
- **Architecture**: 7 Autoload singletons with event-driven communication via GameEvents signal bus (8 after HousingManager)

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
| MoodSystem as POCO (not Node) | Testable in isolation, HappinessManager owns lifecycle | ✓ Good — clean separation, exponential decay + hysteresis encapsulated |
| Tier-space economy (not float-space) | Discrete MoodTier enum eliminates floating-point edge cases in economy | ✓ Good — switch expressions are readable, no float comparison bugs |
| Hysteresis on tier demotion | Prevents rapid oscillation near boundaries | ✓ Good — width=0.05 eliminates tier chatter completely |
| Save format versioning (v1/v2) | Version-gated restore path keeps old saves loadable | ✓ Good — C# default values (0/0f) handle v1 deserialization safely |
| MoodHUD replaces HappinessBar entirely | Clean break, no dual-display confusion | ✓ Good — deprecated code fully removed, no lingering shims |
| Per-tier config fields (not formula) | Designer-tunable per-tier values in Inspector | ✓ Good — arrival/income per tier visible and adjustable without code changes |

## Current Milestone: v1.2 Housing

**Goal:** Give each citizen a home room they visibly return to, making housing feel personal and alive.

**Target features:**
- Automatic home assignment (citizen → housing room) with even-spread algorithm
- Size-scaled capacity (base + segments - 1)
- Return-home behavior cycle with Zzz floater
- Citizen info panel shows home location
- Room tooltip shows residents
- HousingManager autoload + HousingConfig resource
- Graceful unhoused handling (no penalty)
- Save/load housing assignments

**Design:** `docs/prd-housing.md`

---
*Last updated: 2026-03-05 after v1.2 milestone started*
