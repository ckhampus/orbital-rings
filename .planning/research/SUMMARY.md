# Project Research Summary

**Project:** Orbital Rings v1.4 — Citizen AI, Day/Night Cycle, Utility Scoring
**Domain:** Cozy space station builder — citizen AI, schedule-driven behavior, lighting atmosphere
**Researched:** 2026-03-07
**Confidence:** HIGH

## Executive Summary

Orbital Rings v1.4 adds four interlocking systems to a mature 11,380 LOC Godot 4 C# codebase: a station clock (StationClock autoload), citizen personality traits (Interest + Rhythm enums on CitizenData), a utility-scored room selection system (pure C# POCO), and an explicit citizen state machine replacing the current implicit boolean-flag soup. The research consistently points to one core principle: this is a **cozy observation game, not a management sim**. Every architectural and feature decision follows from that distinction. Traits always help, schedules always suggest, and nothing ever punishes. The competitor analysis (RimWorld, Animal Crossing, Stardew Valley, ONI) confirms that the 2-trait + utility scoring approach is the right level of complexity: readable like Animal Crossing's 1-personality-type model, personality-expressive like RimWorld's priority system, without the opacity of Dwarf Fortress or the micromanagement of player-configurable schedules.

The recommended architecture is conservative and deeply grounded in the existing codebase. One new autoload singleton (StationClock, the 9th), three new POCOs (UtilityScorer, CitizenSchedule, CitizenStateMachine), one new scene node (StationLighting/DayNightManager), and targeted modifications to CitizenNode, CitizenData, GameEvents, SaveManager, and CitizenInfoPanel. No new NuGet packages. No plugin dependencies. All required Godot APIs — DirectionalLight3D, WorldEnvironment, Tween, Timer, RenderingServer.GlobalShaderParameterSet — are already in use or closely parallel existing patterns. The day/night visual system uses global shader parameters for window emissives (O(1) regardless of room count) and Tween-based transitions (kill-before-create pattern, already proven in CitizenNode).

The main risk is the CitizenNode refactor (Phase 5). CitizenNode is 1,107 lines with visit logic, home return logic, wish logic, and mesh management all interleaved. The boolean flags `_isVisiting`, `_isAtHome`, and `_walkingToHome` form an implicit state machine that v1.4 replaces with an explicit enum. The critical mitigation: delete old timers and flags atomically in a single commit, extract the state machine as a POCO with an `ICitizenActions` interface, and test the state machine in isolation before wiring it into CitizenNode. The save format advancing to v4 is the second significant risk — v3 saves must assign traits deterministically (seeded from citizen name hash, not random) and default the clock to Morning, with autosave suppressed during the migration window to prevent corrupted v4 saves from overwriting valid v3 saves.

## Key Findings

### Recommended Stack

The v1.4 milestone requires no new dependencies. The full stack is Godot 4.6 with .NET 10 (Godot.NET.Sdk 4.6.1), using built-in Godot APIs throughout. The Forward Plus renderer supports DirectionalLight3D, WorldEnvironment, and global shader parameters natively. The project's established patterns — pure C# event delegates on GameEvents, `[GlobalClass] Resource` subclasses for config data, Tween-based animation, Timer nodes for periodic behavior, System.Text.Json for save serialization — all apply directly to v1.4 features.

**Core technologies:**
- **StationClock (Autoload singleton):** `_Process`-based time accumulation, `StationPeriod` enum, `StationClockConfig` Resource — consistent with existing singleton pattern; must be autoload (not scene node) to avoid lazy-discovery fragility proven by the `_grid` wart in CitizenManager
- **DirectionalLight3D + WorldEnvironment + Tween:** Day/night lighting — all proven APIs already used or closely parallel to existing code; `TweenMethod(Callable.From(...))` required for Environment sub-resource properties (cannot use TweenProperty on sub-resources)
- **RenderingServer.GlobalShaderParameterSet:** Window emissive control — O(1) for all room materials simultaneously; define in Project Settings first, set from code at runtime (known issue with GlobalShaderParameterAdd from code only, godotengine/godot#77988)
- **Enum-based CitizenState (POCO):** Walking/Evaluating/Visiting/Resting — enum + switch in `_Process`, avoids node-per-state allocation (80+ extra nodes for 20 citizens with node-based approach)
- **UtilityScorer (static class):** Pure function scoring — follows MoodSystem POCO pattern; stateless, unit-testable without Godot runtime
- **SaveData v4 (System.Text.Json):** Nullable new fields with version-gated restore — extends established v1/v2/v3 pattern

See `.planning/research/STACK.md` for verified API signatures and code examples.

### Expected Features

The features research distinguishes clearly between what constitutes a functional milestone and what constitutes a cozy-differentiated experience.

**Must have (table stakes):**
- Station clock with four periods (Morning/Day/Evening/Night) — without this, "daily routine" is meaningless; the visual cycle and all schedule behavior depend on it
- Citizen state machine (Walking/Evaluating/Visiting/Resting) — the current boolean soup cannot absorb new schedule-aware behavior without becoming unmaintainable; correctness prerequisite
- Citizen traits visible in info panel — players must see traits to connect them to behavior patterns they observe
- Schedule templates with period-weighted activities — without schedules, traits are invisible; this is what makes routines observable
- Utility scoring for room selection — replaces nearest-room selection; trait affinity only becomes meaningful when it influences decisions
- Clock UI in HUD — players need to know what period it is or the visual transitions are confusing
- Save/load v4 format — losing traits and clock position on reload breaks the persistence contract

**Should have (differentiators):**
- Trait-based personality (2 traits = 12 behavioral archetypes) — legible at a glance, observable after 1-2 cycles
- Room window emissives responding to day/night — the "cozy station in space" visual payoff; uses global shader parameter
- Visible thinking indicator during Evaluating state — makes AI decision-making legible, distinguishes from random wandering
- Room tooltip showing current visitors — discovery feature; events already exist, minimal UI work

**Defer (future milestones):**
- Citizen relationships / social graphs — explicitly out of scope; trait system lays groundwork without requiring it
- Fast-forward / time speed controls — requires all existing timers and tweens to respect time scale; too invasive for v1.4
- Additional trait types — 2 traits sufficient; more traits make the info panel unreadable
- Period-specific room economy effects — creates optimization pressure, breaks "build what feels right" ethos
- Player-configurable citizen schedules — turns cozy builder into spreadsheet optimizer; automatic templates are correct

**Anti-features (never build):**
- Citizen needs (hunger, energy) — creates fail states and management pressure, directly contradicts cozy philosophy
- Mood penalties from unmet schedules — optimization anxiety is explicitly anti-cozy per PROJECT.md
- Full darkness during Night — minimum ambient energy constraint ensures station always remains visible

See `.planning/research/FEATURES.md` for detailed specifications including utility scoring formula, schedule template weights, and competitor analysis.

### Architecture Approach

The v1.4 architecture adds targeted new components alongside the 8 existing autoloads without restructuring the existing system. StationClock (9th autoload) is the time authority: it advances `_elapsed` in `_Process`, detects period boundaries, and emits `PeriodChanged` through GameEvents. All other systems are subscribers, not co-authorities. StationLighting (scene node, not autoload) subscribes to `PeriodChanged` and tweens DirectionalLight3D, WorldEnvironment, and the global emissive shader parameter. CitizenNode undergoes the only major refactor: three independent timers and three boolean flags are replaced by a single decision timer and an explicit `CitizenState` enum with a `TryTransition` guard method. UtilityScorer is a pure static class called during the Evaluating state. EconomyManager, HappinessManager, HousingManager, WishBoard, and BuildManager are untouched.

**Major components:**
1. **StationClock (Autoload)** — owns station time; emits `PeriodChanged(newPeriod, previousPeriod)` via GameEvents; provides `CurrentPeriod` and `Elapsed` for all consumers; saved as single `float Elapsed` with period derived on load
2. **StationLighting / DayNightManager (Scene Node3D)** — subscribes to `PeriodChanged`; tweens DirectionalLight3D (energy, color) and WorldEnvironment (ambient color, ambient energy) using parallel Tween; drives window emissive intensity via `RenderingServer.GlobalShaderParameterSet`
3. **CitizenStateMachine (POCO)** — enum-driven (Walking/Evaluating/Visiting/Resting); `TryTransition` enforces valid transitions; single decision timer replaces three independent timers; `OnRoomDemolished` handles visit interrupts for both home and non-home rooms
4. **UtilityScorer (Static class)** — scores rooms using weighted sum: traitAffinity (0.35) + proximity (0.25) + recency (0.15) + wishBonus (0.25); returns nullable result; filters housing rooms from visit candidates; uses reservoir sampling for tie-breaking
5. **CitizenSchedule (POCO)** — maps (StationPeriod, RhythmTrait) to (VisitWeight, RestWeight, WanderWeight) from ScheduleConfig Resource; 3 templates (EarlyBird, NightOwl, Steady) with per-period activity weights
6. **CitizenData (modified)** — adds `InterestTrait` and `RhythmTrait` enums as `[Export]` fields; 4 Interest values × 3 Rhythm values = 12 behavioral archetypes
7. **SaveData v4 (modified)** — adds `ClockElapsed: float`, nullable `InterestTrait: string?`, nullable `RhythmTrait: string?`; version-gated restore

See `.planning/research/ARCHITECTURE.md` for complete data flow diagrams, integration maps, and detailed component specifications.

### Critical Pitfalls

1. **Timer replacement race condition** — CitizenNode's old timers (`_visitTimer`, `_homeTimer`) and the new state machine must never coexist. Delete all old timer fields and boolean guards (`_isVisiting`, `_isAtHome`, `_walkingToHome`) in a single atomic commit. Extract the state machine as a POCO before wiring into CitizenNode, test it in isolation first.

2. **v3 save migration corruption** — v3 saves have no trait fields and no clock position. If the utility scorer accesses `citizen.Interest` on a citizen with null traits, a NullReferenceException fires 15-30 seconds after load — after the autosave has already overwritten the valid v3 save with corrupted v4 state. Fix: nullable trait fields with version-gated restore, deterministic trait assignment from citizen name hash (not `GD.Randi()`), and suppress autosave during the migration window.

3. **Utility scorer degenerate cases** — Three cases break naive scoring: (A) no rooms built returns a garbage index, (B) all rooms score identically causes all citizens to cluster at the same room, (C) housing rooms scored for visits bypasses the Zzz rest animation. Fix: return nullable from scorer, use reservoir sampling for ties, filter housing from visit candidates, apply recency decay to naturally break ties.

4. **Day/night emission conflicts with citizen selection glow** — The existing system enables `EmissionEnergyMultiplier = 2.5f` on selected citizens. If the WorldEnvironment glow threshold changes during lighting transitions, selected citizens lose their glow or all emissive materials bloom uncontrollably. Fix: keep glow threshold constant, use additive emission on opaque room materials (never alpha transparency), separate the emission channel used by rooms from the channel used by citizen selection.

5. **Room demolished during Visiting state** — The existing `OnRoomDemolished` handler only checks `HomeSegmentIndex`. A citizen in the Visiting state visiting a non-home room that gets demolished stays invisible indefinitely. Fix: state machine `OnRoomDemolished` must check both home AND current visit target; factor "restore citizen to walkway" into a shared method used by all interrupt handlers.

See `.planning/research/PITFALLS.md` for 14 documented pitfalls with prevention strategies, detection signals, and phase assignments.

## Implications for Roadmap

The research strongly suggests a 7-phase build order derived from hard dependency constraints and risk management. Phases 1 and 3 are independent and can proceed in either order. Phase 2 (visuals) requires Phase 1 (clock). Phase 5 (state machine) is the riskiest phase and must have Phases 1, 3, and 4 fully complete before starting. Phase 6 (save/load) must be last in the critical path.

### Phase 1: Station Clock Foundation

**Rationale:** StationClock is the dependency root for all other systems. Lighting, schedules, the HUD clock indicator, and save/load all depend on knowing the current period. Building it first unblocks both the visual track (Phase 2) and the citizen AI track (Phases 4-5).
**Delivers:** `StationClock.Instance.CurrentPeriod` queryable from any system; `PeriodChanged` event on GameEvents; `StationClockConfig` Resource for inspector tuning; `ClockHUD` with period label in HUD; TestHelper updated with new singleton in same commit
**Addresses:** Station clock with four periods (table stakes), Clock UI in HUD (table stakes)
**Avoids:** Clock-as-scene-node lazy discovery fragility (Architecture Anti-Pattern 3); clock/lighting desync on save/load (Pitfall 11); period duration mismatch with decision timer (Pitfall 8 — calibrate period durations and document target 4-6 decisions/period here)

### Phase 2: Day/Night Visuals

**Rationale:** Day/night lighting is high-value player-visible feedback that depends only on Phase 1. Building it second gives the milestone a shipped visual feature if Phase 5 hits blockers. It is also the phase most likely to reveal rendering issues (glow conflicts, transparency Z-fighting) that are easier to diagnose before the state machine refactor adds more complexity.
**Delivers:** DirectionalLight3D and WorldEnvironment animated transitions between four period presets; room window emissive intensity driven by global shader parameter; cozy visual atmosphere confirmed working
**Addresses:** Day/night lighting transitions (table stakes), Room window emissives (differentiator)
**Avoids:** Emission conflict with citizen selection glow (Pitfall 4) — keep glow threshold constant, additive emission on opaque materials only; transparent window Z-fighting with citizen fade animations (Pitfall 10) — opaque materials with emission, no alpha transparency

### Phase 3: Citizen Traits (Data and Assignment)

**Rationale:** Trait enums on CitizenData are pure data additions with no runtime dependencies. This phase can proceed in parallel with Phase 2. Completing it early gives the info panel update time for polish and ensures trait data is stable when Phase 4 (scoring) consumes it.
**Delivers:** `InterestTrait` and `RhythmTrait` enums on CitizenData; random trait assignment in CitizenManager.SpawnCitizen; trait display in CitizenInfoPanel; name-hash determinism established for v3 save migration
**Addresses:** Citizen traits visible in info panel (table stakes), Trait-based personality (differentiator)
**Avoids:** Trait display layout overflow (Pitfall 12) — single-word labels, test at all screen edges; trait re-rolling on every load of v3 saves (Pitfall 6) — establish name-hash determinism here even before save/load is implemented

### Phase 4: Schedule Templates and Utility Scoring

**Rationale:** Pure POCOs with no Godot scene dependencies. Requires Phase 1 (CurrentPeriod) and Phase 3 (Interest trait for affinity scoring). Building the scoring infrastructure before the state machine means it can be unit-tested in isolation against known inputs — UtilityScorer is a pure function, this is the phase easiest to get right with tests.
**Delivers:** `ScheduleConfig` Resource with per-(Period, Rhythm) activity weights; `CitizenSchedule` POCO; `UtilityScorer` static class with normalized multi-factor scoring (affinity + proximity + recency + wish); debug overlay showing decisions per period for calibration validation
**Addresses:** Schedule templates with period-weighted activities (table stakes), Utility scoring for room selection (table stakes)
**Avoids:** Degenerate scorer cases (Pitfall 3) — nullable return, reservoir sampling, housing filter, recency decay; schedule bias invisible due to timing mismatch (Pitfall 8) — validate calibration before wiring into state machine

### Phase 5: Citizen State Machine (Core Refactor)

**Rationale:** The riskiest phase — replaces 400+ lines of working behavior code in a 1,107-line file. All earlier phases add new code alongside existing code; this phase replaces existing code. Must come after Phase 4 so scoring and schedule infrastructure is ready to wire in. The POCO extraction pattern (CitizenStateMachine with ICitizenActions interface) is the primary risk mitigation.
**Delivers:** Explicit `CitizenState` enum with `TryTransition` guard; single `_decisionTimer` replacing `_visitTimer` + `_homeTimer`; `EvaluateNextAction` calling CitizenSchedule + UtilityScorer; `OnRoomDemolished` handling both home and visit interrupts; `_isVisiting`, `_isAtHome`, `_walkingToHome` booleans deleted; Evaluating state with visible thinking indicator
**Addresses:** Citizen state machine (table stakes), Visible thinking indicator (differentiator)
**Avoids:** Timer replacement race condition (Pitfall 1) — atomic deletion of old fields, state machine POCO tested in isolation before wiring; room demolished during visit (Pitfall 5) — shared `RestoreToWalkway()` method, visit target tracked in state machine

### Phase 6: Save/Load v4

**Rationale:** Save/load must come last because it captures all new state (clock position, citizen traits). The risk of corrupting v3 saves is real — the autosave timing issue can destroy player data permanently. Building this last means the schema is final and the backward-compatibility test has real state to exercise.
**Delivers:** `SaveData` v4 with `ClockElapsed: float`; `SavedCitizen` with nullable trait fields; version-gated restore in SaveManager; v3 backward compatibility with name-hash trait assignment and Morning clock default; autosave suppressed during migration window; v3 backward-compatibility test with hand-crafted JSON as acceptance criteria
**Addresses:** Save/load v4 format (table stakes)
**Avoids:** v3 save migration corruption (Pitfall 2) — nullable fields, version-gated restore, name-hash determinism, autosave suppression; GameEvents subscriber explosion (Pitfall 7) — verify ClearAllSubscribers covers all new events added across all phases

### Phase 7: Room Tooltip Visitors

**Rationale:** Low-risk polish that depends on the state machine emitting room events correctly (Phase 5). The events already exist (`CitizenEnteredRoom`, `CitizenExitedRoom`). Pure UI work.
**Delivers:** Room hover tooltip showing names of current visitors
**Addresses:** Room tooltip showing current visitors (differentiator)
**Avoids:** No new events needed — existing events already cover this feature

### Phase Ordering Rationale

- **Phases 1 and 3 are independent** and can be built in either order or in parallel. Phase 1 is recommended first because it unblocks both visual work (Phase 2) and the AI decision track (Phase 4).
- **Phase 2 before Phase 5** — Shipping visible day/night visuals before the risky state machine refactor means the milestone delivers player value even if Phase 5 needs extra time. It also surfaces rendering issues in isolation where they are easier to debug.
- **Phase 4 before Phase 5** — UtilityScorer must be validated as a pure function (unit tests with known inputs) before it is integrated into CitizenNode's `_Process` path. Discovering a scoring bug after the state machine refactor is already integrated is significantly harder to isolate.
- **Phase 5 is the critical path bottleneck** — All citizen AI features converge here. The POCO extraction pattern and atomic deletion of old boolean flags are non-negotiable risk mitigations.
- **Phase 6 last in the critical path** — Save format must capture finalized state. The backward-compatibility risk (corrupting v3 saves) makes thorough testing after all new state is stable mandatory.
- **Phase 7 last** — Zero risk, satisfying to ship after the heavy lifting is complete.

### Research Flags

Phases likely needing deeper research or prototyping during planning:

- **Phase 2 (Day/Night Visuals):** The `TweenMethod(Callable.From(...))` pattern for Environment sub-resources is confirmed but should be prototyped in a minimal test scene before full integration. The global shader parameter setup (Pitfall 10 context, known issue godotengine/godot#77988) requires defining the uniform in Project Settings UI first — verify this works with the project's existing Project Settings before committing to it.
- **Phase 5 (State Machine Refactor):** Not a research gap but a design discipline gap. The ICitizenActions interface and the complete state transition table (all valid transitions enumerated) must be written artifacts reviewed before touching CitizenNode.cs. The 1,107-line file requires a line-by-line read to identify exactly what moves, what is deleted, and what stays unchanged.

Phases with standard patterns (research-phase not needed):

- **Phase 1 (Station Clock):** Autoload singleton with `_Process`-based accumulation directly mirrors existing HousingManager/HappinessManager rationale and the established GameEvents event emission pattern.
- **Phase 3 (Citizen Traits):** Enum fields on existing Resource, CitizenInfoPanel UI update. Both are established project patterns with multiple existing examples.
- **Phase 4 (Schedule + Scoring):** Pure C# POCOs following MoodSystem POCO pattern. Utility AI is well-documented in Game AI Pro. Unit-testable in isolation without Godot runtime.
- **Phase 6 (Save/Load v4):** Extends the pattern already done three times (v1 -> v2 -> v3). Version-gated restore with nullable new fields is the established approach.
- **Phase 7 (Room Tooltips):** Events exist, UI pattern established. No unknowns.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All APIs verified from official Godot 4.4 docs. No new dependencies. Codebase directly inspected (40+ files). One MEDIUM caveat: GlobalShaderParameterSet has a known issue with code-only setup — mitigated by defining in Project Settings first. |
| Features | HIGH | Verified against direct codebase inspection (CitizenNode.cs 1107 lines, all room .tres files, all 12 wish templates). Competitor analysis grounded in well-documented games. Feature scope appropriately bounded by cozy philosophy in PROJECT.md. |
| Architecture | HIGH | Derived from direct inspection of all 8 autoloads and 40+ source files. Component responsibilities clear. Data flow diagrams are concrete, not speculative. Integration points identified at source-line level. |
| Pitfalls | HIGH | 14 documented pitfalls, most identified from direct codebase analysis with specific line references. Mitigation strategies are specific and actionable. Phase assignments are concrete. |

**Overall confidence:** HIGH

### Gaps to Address

- **Lighting tuning values:** The day/night preset color values in FEATURES.md are reasonable starting points but will require visual tuning in the actual scene. The DayNightConfig Resource approach makes this inspector-tunable. Plan for 1-2 tuning passes after Phase 2 is implemented.
- **Decision timer calibration:** The decision timer interval and period duration must be calibrated together to achieve 4-6 decisions per period (Pitfall 8). Target: approximately 100s periods with approximately 20s average decision timer. Add a debug overlay in Phase 4 to validate before committing values to Phase 5.
- **CitizenNode refactor scope:** CitizenNode.cs is 1,107 lines. The architecture research provides pseudocode and patterns, but the complete refactor scope (which lines move, which are deleted, which are unchanged) requires a line-by-line read before Phase 5 begins. This is expected — it belongs in Phase 5 planning, not research.
- **GlobalShaderParameter compatibility:** The known issue with GlobalShaderParameterAdd from code may require defining `emissive_strength` via Project Settings UI only. Verify this in a minimal test scene during Phase 2 before building the full emissive system on top of it.

## Sources

### Primary (HIGH confidence)
- Official Godot 4.4 Docs — DirectionalLight3D, Light3D, WorldEnvironment, Environment, RenderingServer, Tween, Timer class references
- Direct codebase inspection — CitizenNode.cs (1107 lines), CitizenManager.cs (423 lines), GameEvents.cs (343 lines), SaveManager.cs (538 lines), HousingManager.cs (525 lines), HappinessManager.cs (367 lines), EconomyManager.cs (366 lines), WishBoard.cs (407 lines), MoodSystem.cs (127 lines), all Data/ resources, TestHelper.cs, GameTestClass.cs
- Game AI Pro Chapter 9: Introduction to Utility Theory — utility scoring fundamentals, normalization requirements
- Game AI Pro 3 Chapter 13: Choosing Effective Utility-Based Considerations — scoring factor design, weight tuning
- RimWorld Wiki (Traits) — trait categories and behavioral effects
- Stardew Valley Modding Wiki (Schedule data) — time-based NPC scheduling format
- PROJECT.md — cozy philosophy constraints, out-of-scope decisions

### Secondary (MEDIUM confidence)
- The Shaggy Dev: Introduction to Utility AI — scoring formulas, consideration normalization, bucketing
- GDQuest Finite State Machine Tutorial — enum vs node-based state machine patterns for Godot
- GameDev Academy: Day Night Cycle in Godot 4 — DirectionalLight3D + Environment transition approach
- Seaotter Games: Setting a Mood with Day/Night Cycle — cozy lighting design principles
- Animal Crossing personality types — 1-personality cozy design philosophy
- ROKOJORI Godot API mirror — BGMode and AmbientSource enum values verified
- Godot issue #77988 — GlobalShaderParameterAdd known issue with code-only setup
- Game Programming Patterns: State — state machine interrupt handling, concurrent state transitions
- AI Decision-Making with Utility Scores (McGuireV10) — tie-breaking mechanisms, secondary criteria

### Tertiary (LOW confidence — validate during implementation)
- Colony sim AI prototype (johncjensen.com) — goal-priority scoring with personality modifiers
- Godot Forum: Emission Material Glow — WorldEnvironment glow threshold interaction behavior

---
*Research completed: 2026-03-07*
*Ready for roadmap: yes*
