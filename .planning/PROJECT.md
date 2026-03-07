# Orbital Rings

## What This Is

A cozy space station builder where players construct a modular orbital ring one room at a time. The ring is a flat donut divided into 24 segments (12 outer, 12 inner) where rooms attract citizens who express wishes via speech bubbles. Fulfilling wishes grows lifetime happiness (permanent progress) and raises station mood (dynamic feel), with five mood tiers driving citizen arrivals and economy. Citizens are assigned home rooms and visibly return to rest, making the station feel alive and personal. There is no fail state — the station always grows. Built in Godot 4 with C#, targeting PC (itch.io). v1.2 shipped with the complete housing system.

## Core Value

The wish-driven building loop: citizens express wishes, the player builds rooms to fulfill them, happiness rises, new citizens arrive, new wishes emerge. This loop must feel satisfying and alive.

## Current Milestone: v1.3 Testing

**Goal:** Introduce GoDotTest + GodotTestDriver testing framework and cover critical paths (save/load, housing, economy, mood, wishes) for confidence as the codebase grows.

**Target features:**
- Testing framework integration (GoDotTest + GodotTestDriver)
- Test runner scene for in-editor and CI execution
- Critical path test suites for core game systems

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
- ✓ Tier-driven citizen arrivals and economy income multiplier (1.0x-1.4x) — v1.1
- ✓ HUD shows lifetime wish counter and mood tier label with tier-colored text — v1.1
- ✓ Save format v2 with version-gated backward compatibility — v1.1
- ✓ Automatic home assignment with fewest-occupants-first algorithm — v1.2
- ✓ Size-scaled housing capacity (BaseCapacity + segments - 1) — v1.2
- ✓ Return-home behavior with Zzz indicator (90-150s cycle, 8-15s rest) — v1.2
- ✓ Citizen info panel shows home room name and location — v1.2
- ✓ Housing room tooltip shows resident names — v1.2
- ✓ Population display shows count/capacity format — v1.2
- ✓ HousingManager autoload singleton with capacity tracking — v1.2
- ✓ HousingConfig resource for Inspector-tunable timing constants — v1.2
- ✓ Unhoused citizens handled gracefully (no penalty) — v1.2
- ✓ Save/load housing assignments with backward compatibility (v3 format) — v1.2

### Active

- [ ] GoDotTest + GodotTestDriver framework wired up with test runner scene
- [ ] Save/load round-trip tests across all format versions (v1, v2, v3)
- [ ] Housing assignment tests (fewest-occupants-first, capacity scaling, demolish/reassign)
- [ ] Economy calculation tests (income multipliers, room costs, work bonuses)
- [ ] Mood system tests (decay, tier transitions, hysteresis)
- [ ] Wish fulfillment loop tests (wish → build → satisfy → happiness)

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
- Tier change notification — deferred, potential v1.3+
- Save migration v1→v2 — deferred, not blocking (v1 saves load safely with defaults)
- Additional blueprint unlocks (30, 50, 100) — deferred, potential v1.3+
- Cosmetic unlocks tied to lifetime milestones — deferred, potential v1.3+
- Home-return path indicator / visual trail — polish, defer to v1.3+
- Citizen roommate display in info panel — minor UI scope, defer to v1.3+

## Context

Shipped v1.2 with 9,508 LOC C# across 19 phases (40 plans) in 5 days.
Tech stack: Godot 4.4, C#, .NET 8, procedural mesh/audio generation.
Architecture: 8 Autoload singletons (GameEvents, EconomyManager, BuildManager, CitizenManager, WishBoard, HappinessManager, SaveManager, HousingManager) coordinated via typed C# event delegates.
Art style: Soft 3D with pastel palette, rounded capsule citizens, warm segment colors.
Full design document at `IDEA.md` covers multi-ring expansion, proc gen interiors, citizen personalities, day/night cycle — all deferred.
v1.2 added HousingManager as 8th autoload with fewest-occupants-first assignment, periodic Zzz rest cycle, three UI components (info panel, tooltip, population display), and save format v3 with nullable HomeSegmentIndex. Capacity ownership transferred from HappinessManager to HousingManager.

## Constraints

- **Engine**: Godot 4 with C# — all gameplay logic in C#, using Godot's 3D rendering pipeline
- **Platform**: PC (keyboard + mouse), distributed via itch.io
- **No fail state**: The game must never punish the player — wishes linger, nothing bad happens from ignoring them
- **Architecture**: 8 Autoload singletons with event-driven communication via GameEvents signal bus

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Flat donut (not full torus) | Simpler geometry, easier camera/interaction, captures the ring feel | ✓ Good — polar math picking works perfectly, no physics bodies needed |
| Placeholder interiors for v1 | Focus on ring mechanics and wish loop before investing in proc gen | ✓ Good — colored blocks per category are readable and sufficient |
| Named citizens but no traits/routines | Gives the loop personality without complex AI scheduling | ✓ Good — names + wishes + walking creates enough personality |
| Start with empty ring, no tutorial | Cozy games teach through play; keeps first milestone lean | ✓ Good — wishes serve as implicit tutorial |
| Defer day/night cycle | Cosmetic system, not needed to prove core loop | ✓ Good — ambient drone provides atmosphere without visual complexity |
| Single ring for first milestone | Prove the core loop before adding vertical expansion | ✓ Good — loop is complete and satisfying on one ring |
| Pure C# events (not Godot signals) | Avoids marshalling overhead and IsConnected bugs | ✓ Good — zero signal connection issues across 19 phases |
| Polar math picking (not trimesh collision) | Zero physics overhead, no phantom hits | ✓ Good — reliable segment selection at any camera angle |
| Spreadsheet-first economy | Calibrate numbers before coding to avoid rework | ✓ Good — sqrt scaling + 1.3x happiness cap created balanced progression |
| Procedural audio (no .wav assets) | Zero external asset dependencies | ✓ Good — placement chime + wish celebration chime feel distinct and satisfying |
| Debounced autosave on state events | Prevents rapid-fire saves during batch operations | ✓ Good — save/load works reliably with 0.5s debounce |
| MoodSystem as POCO (not Node) | Testable in isolation, HappinessManager owns lifecycle | ✓ Good — clean separation, exponential decay + hysteresis encapsulated |
| Tier-space economy (not float-space) | Discrete MoodTier enum eliminates floating-point edge cases in economy | ✓ Good — switch expressions are readable, no float comparison bugs |
| Hysteresis on tier demotion | Prevents rapid oscillation near boundaries | ✓ Good — width=0.05 eliminates tier chatter completely |
| Save format versioning (v1/v2/v3) | Version-gated restore path keeps old saves loadable | ✓ Good — three versions load safely with progressive defaults |
| MoodHUD replaces HappinessBar entirely | Clean break, no dual-display confusion | ✓ Good — deprecated code fully removed, no lingering shims |
| Per-tier config fields (not formula) | Designer-tunable per-tier values in Inspector | ✓ Good — arrival/income per tier visible and adjustable without code changes |
| New HousingManager autoload (8th singleton) | Separate concerns — HappinessManager shouldn't own housing state | ✓ Good — clean API boundary, capacity transfer was straightforward |
| Size-scaled capacity (base + segments - 1) | Larger rooms should hold more citizens | ✓ Good — 1-seg=2, 2-seg=3, 3-seg=4 creates natural building incentive |
| Fewest-occupants-first with reservoir sampling | Even distribution without player intervention | ✓ Good — citizens spread across rooms naturally, tiebreaker feels random |
| Label3D with TopLevel for Zzz indicator | Parent-independent visibility during tween animation | ✓ Good — reparenting to CitizenManager fixed visibility during rest |
| Capacity transfer from HappinessManager | Single source of truth eliminates desynchronization | ✓ Good — occupancy-based arrival gating is simpler than additive formula |
| Nullable int? HomeSegmentIndex (not int) | Distinguish "no home" from "segment 0" in save format | ✓ Good — System.Text.Json serializes null correctly, no sentinel values needed |
| XML doc audit trail for save/load | Document verified code paths for future readers | ✓ Good — Phase 19 audit added RestoreFromSave comments with zero code changes |

---
*Last updated: 2026-03-07 after v1.3 milestone started*
