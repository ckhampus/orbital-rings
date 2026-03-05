# Project Retrospective

*A living document updated after each milestone. Lessons feed forward into future planning.*

## Milestone: v1.0 — MVP

**Shipped:** 2026-03-04
**Phases:** 9 | **Plans:** 25 | **Commits:** 161

### What Was Built
- Complete build-wish-grow loop on a single orbital ring with 24 segments
- 7 Autoload singletons coordinated via typed C# event delegates (GameEvents signal bus)
- 10 room types across 5 categories with procedural audio snap feedback
- Named citizens with polar walkway movement, drift-fade room visits, and wish speech bubbles
- Happiness progression driving citizen arrivals and blueprint unlocks at milestones
- Save/load with debounced autosave, ambient drone, wish celebration, and title screen
- Work bonus economy flow wired end-to-end via citizen room visit events

### What Worked
- **Spreadsheet-first economy design**: Calibrating numbers in a markdown spreadsheet before writing code prevented rework in Phase 3
- **Per-instance materials pattern**: Discovered in Phase 2 (shared-material contamination), reapplied consistently in Phases 4, 5, 7 — zero material bugs after Phase 2
- **Polar math picking**: Avoiding physics bodies entirely for segment selection was correct — zero trimesh overhead, no phantom hits, works at any camera angle
- **Human verification checkpoints**: Phases 4, 5, 6 included dedicated verification plans that caught issues early
- **Event-driven architecture**: Pure C# events over Godot signals eliminated marshalling overhead and IsConnected bugs across all 9 phases
- **Procedural audio**: Zero external .wav/.ogg assets needed — all audio generated programmatically

### What Was Inefficient
- **Phase 9 gap closure**: The milestone audit found ECON-03 unsatisfied (work bonus had zero subscribers), requiring a new phase. Could have been caught earlier if the economy flow had been tested end-to-end in Phase 3
- **ROADMAP.md checkbox drift**: Phase 9 plan 09-02 showed `- [ ]` in ROADMAP.md despite having a SUMMARY.md on disk — manual checkbox tracking is fragile
- **Summary frontmatter inconsistency**: Some summaries used different YAML key formats, making automated extraction fail (no `one_liner` field)

### Patterns Established
- **Per-instance StandardMaterial3D** for every mesh to prevent shared-material contamination
- **Programmatic UI** (code-built, not .tscn scenes) for all HUD elements
- **Explicit Timer.Start()** over Autostart=true — Autostart before AddChild() is unreliable in Godot 4 C#
- **GameEvents.Instance in _EnterTree** (not _Ready) for earliest-possible singleton availability
- **Lazy node discovery in _Process** for managers that span title screen + game scene
- **HashSet tracking** for event-driven state that outlives the originating node (work bonus race condition fix)

### Key Lessons
1. **Wire end-to-end early**: Events with zero subscribers are invisible bugs. Test the full event chain when adding new events, not just the emitter.
2. **Material instances are non-negotiable**: In Godot 4 C#, always create independent StandardMaterial3D per mesh. Shared materials cause contamination bugs that are hard to debug.
3. **Polar math > physics for 2D-on-3D interaction**: When interaction is geometrically simple (ring segments), skip physics bodies entirely. Math is faster, more reliable, and has no baking step.
4. **Debounced autosave scales**: Subscribing to all state-change events with a 0.5s debounce Timer prevents both rapid-fire saves and missed saves.
5. **Frame-delay scene restoration**: After ChangeSceneToFile in Godot 4, wait 2 frames for all _Ready methods to complete before restoring state.

### Cost Observations
- Model mix: 80% opus, 15% sonnet, 5% haiku (quality profile throughout)
- Sessions: ~8 sessions across 3 days
- Notable: Average plan execution was 3.2 minutes. Human verification checkpoints (phases 4, 5, 6) added ~5 minutes each but caught real issues.

---

## Milestone: v1.1 — Happiness v2

**Shipped:** 2026-03-05
**Phases:** 4 | **Plans:** 7 | **Sessions:** ~4

### What Was Built
- Dual-value happiness: Lifetime Happiness (integer, never decreases) + Station Mood (float with exponential decay toward rising baseline)
- MoodSystem POCO encapsulating decay math, tier promotion/demotion, and hysteresis — owned by HappinessManager Autoload
- Five mood tiers (Quiet/Cozy/Lively/Vibrant/Radiant) governing both arrival probability and income multiplier
- Save format v2 with version-gated backward compatibility (v1 saves load safely)
- MoodHUD widget with pulse-animated wish counter and tier-colored mood label
- Full deprecation cleanup: HappinessBar, HappinessChanged event, SetHappiness API, float-space economy removed

### What Worked
- **Tier-space design**: Deciding early to use discrete MoodTier enum instead of continuous float for economy/arrival made switch expressions clean and eliminated float comparison edge cases
- **POCO for simulation math**: MoodSystem as a plain C# object (not a Node) kept decay/hysteresis logic testable and isolated from Godot lifecycle
- **Contracts-first plan ordering**: Phase 10 Plan 01 shipped MoodTier enum, HappinessConfig, and GameEvents v2 before Plan 02 wired the logic — zero merge conflicts between plans
- **Three-path wiring audit**: Phase 11 Plan 03 explicitly audited all three HappinessManager paths (_Process, OnWishFulfilled, RestoreState) for SetMoodTier calls — caught the RestoreState path that would have been missed
- **Milestone audit before completion**: Audit caught the orphaned HappinessMultiplierCap field — cleaned up via quick task before shipping

### What Was Inefficient
- **Summary frontmatter still missing `one_liner`**: Automated extraction via `summary-extract --fields one_liner` returned N/A for all 7 summaries — the executor doesn't generate this field
- **Quick tasks table corruption in STATE.md**: Phase execution metrics leaked into the Quick Tasks Completed table (lines 81-87) — likely a STATE.md update bug in the executor
- **Nyquist validation gap**: All 4 phases have draft VALIDATION.md but no compliant tests — no test infrastructure exists for the Godot 4 C# project

### Patterns Established
- **Tier-space APIs**: Economy and arrival systems consume discrete MoodTier enum, not continuous float — switch expressions for per-tier config lookup
- **ExportGroup sections**: HappinessConfig and EconomyConfig use [ExportGroup] for Inspector organization of related fields
- **Version-gated save restore**: SaveData.Version field gates code paths — v2 restores full state, v1 uses safe defaults
- **Tween kill-before-create**: MoodHUD maintains three independent Tween references, killing previous before creating new to prevent animation conflicts

### Key Lessons
1. **POCO simulation objects scale well**: MoodSystem proved that encapsulating math in plain C# objects keeps Autoload Nodes thin and logic testable
2. **Tier-space > float-space for game rules**: Discrete tiers eliminate edge cases and make Inspector config intuitive — five fields per system, each clearly labeled
3. **Three-path audit for integration**: Any system with multiple entry points (frame tick, event handler, restore) needs explicit coverage check at each path
4. **Clean break over gradual deprecation**: Removing HappinessBar/HappinessChanged/SetHappiness in a single phase (13) was cleaner than keeping compatibility shims across multiple phases

### Cost Observations
- Model mix: 85% opus, 10% sonnet, 5% haiku (quality profile)
- Sessions: ~4 sessions across 2 days
- Notable: 7 plans averaged ~2 min execution each. Smaller phase scope (4 phases vs 9) reduced context switching. Quick tasks 4-5 handled mid-milestone housekeeping efficiently.

---

## Cross-Milestone Trends

### Process Evolution

| Milestone | Sessions | Phases | Key Change |
|-----------|----------|--------|------------|
| v1.0 | ~8 | 9 | Full GSD workflow with research → plan → execute → verify per phase |
| v1.1 | ~4 | 4 | Contracts-first plan ordering, milestone audit before completion |

### Cumulative Quality

| Milestone | Audit Score | Requirements | E2E Flows | Tech Debt |
|-----------|-------------|--------------|-----------|-----------|
| v1.0 | 25/25 | 25/25 satisfied | 6/6 complete | 4 info-level items |
| v1.1 | 12/12 | 12/12 satisfied | 3/3 complete | 1 item (resolved before ship) |

### Top Lessons (Verified Across Milestones)

1. Spreadsheet-first design for any system with tunable numbers prevents rework (v1.0 economy, v1.1 tier thresholds)
2. Per-instance materials in Godot 4 C# — always, no exceptions
3. Event-driven architecture with typed C# delegates scales cleanly across 7+ singletons
4. Contracts-first plan ordering (enums/configs/events before logic) eliminates merge conflicts between plans (v1.0 Phase 1, v1.1 Phase 10)
5. POCO simulation objects keep Autoload Nodes thin and logic testable (v1.1 MoodSystem)
