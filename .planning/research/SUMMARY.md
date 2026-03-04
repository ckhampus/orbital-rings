# Project Research Summary

**Project:** Orbital Rings — v1.1 Happiness v2
**Domain:** Cozy 3D space station builder (Godot 4.4 C# / existing singleton architecture)
**Researched:** 2026-03-04
**Confidence:** HIGH

---

> **Milestone scope:** This summary covers the v1.1 Happiness v2 research only.
> The v1.0 foundation research (ring geometry, citizen navigation, economy, wish system)
> remains documented in the sections below the v1.1 content.

---

## Executive Summary

Orbital Rings v1.1 replaces a single monotonically increasing happiness float (v1: 0.0–1.0) with a dual-value system: a permanent `lifetimeHappiness` integer counter and a fluctuating `mood` float that decays toward a rising baseline derived from that counter. This is a well-understood pattern in incremental and cozy game design: the permanent counter satisfies the "always-progressing" instinct, while the fluctuating mood gives each active session its emotional texture. The design is correctly non-punishing because the decay floor rises with accumulated play — a player who returns after an absence finds their station at its earned resting state, not at zero.

All new technical surface area maps to APIs already present in Godot 4.4 and the existing codebase. No new packages or addons are required. The implementation is a targeted refactor of `HappinessManager`, a replacement of `HappinessBar` with two smaller HUD widgets, and a one-way save migration. Every API decision (frame-based decay via `_Process`, `AddThemeColorOverride` for tier colors, `FloatingText` reuse for tier notifications, kill-before-create `Tween` for counter pulse, version-gated `SaveData` migration) has a direct precedent already proven in the live codebase.

The primary risks are not technical complexity but correctness of migration and event wiring. The most dangerous failure mode is v1 save data being silently zeroed if the migration gate is omitted. The second most dangerous is a stale `HappinessChanged(float)` subscription surviving in `EconomyManager` after the refactor, causing the income multiplier to permanently clamp to 1.0. Both risks are mechanical to prevent: delete the old event early so the compiler surfaces every missed subscriber, and test migration with an actual v1 `save.json` before shipping.

---

## Key Findings

### Recommended Stack

The existing Godot 4.4 C# stack requires zero additions. All implementation uses built-in engine APIs with direct precedents in this codebase. See `.planning/research/STACK.md` for API-level detail.

**Core technologies:**
- `_Process(double delta)` in `HappinessManager` — smooth per-frame mood decay; the only correct approach for continuous exponential smoothing (Timer nodes produce stair-step artifacts)
- `Tween` via `CreateTween()` — all UI animations; kill-before-create pattern already established in `HappinessBar.cs`, `FloatingText.cs`, `CreditHUD.cs`
- `AddThemeColorOverride("font_color", color)` — tier label color; absolute color override immune to theme conflicts; already used in `HappinessBar.cs` and `FloatingText.cs`
- `FloatingText : Label` (existing class) — tier change notification; zero new code; proven in production for credit and happiness gain notifications
- `System.Text.Json` (.NET 8 built-in) — save migration; missing fields default to zero, enabling clean v1→v2 migration without a separate deserializer

**What NOT to add:** No third-party tween library, no Timer for mood decay, no `Label3D` for HUD notifications, no `AnimationPlayer` scene for floating text, no animated number roll-up for the lifetime counter.

### Expected Features

See `.planning/research/FEATURES.md` for full competitive analysis and dependency graph.

**Must have for v1.1 (table stakes from genre research):**
- Named mood tiers (Quiet / Cozy / Lively / Vibrant / Radiant) — players expect human-readable state names, not raw floats; established by Spiritfarer, RimWorld, Animal Crossing
- Always-visible mood tier in HUD — cozy game research confirms ambient state must be on-screen without requiring menu interaction
- Feedback on tier change — floating text auto-fading in ~2 seconds; non-blocking; "Station mood: Lively" pattern
- Permanent progress counter (♥ N) — monotonically increasing; never regresses; satisfies incremental game instinct without competitive pressure
- Decay toward rising baseline (not toward zero) — the design's key genre-correctness decision; zero-floor decay is a punishment loop, genre-violating

**Should have (differentiators for this system):**
- Rising baseline tied to lifetime wish count — mature stations rest above the lowest tier; earned warmth persists
- Tier label color as ambient mood indicator — space-efficient HUD; five distinct colors (gray → coral → amber → gold → soft white)
- Wish counter as the always-increasing "score" — ♥ 47 is a legible, warm brag number; no new systems needed

**Defer to v1.x:**
- "About to tier up" visual pulse — medium complexity; implement after core system is validated in play
- Persistent wish board supplement — relevant once citizen count exceeds ~10
- Additional blueprint milestones beyond v1.1 set (e.g., at 30, 50, 100 wishes)

**Anti-features to firmly avoid:**
- Raw mood float exposed anywhere in player-facing UI
- Mood decay toward zero (punishment loop)
- Blocking or semi-blocking tier change notifications
- Diminishing returns on mood gain per wish (decay already provides the natural ceiling)
- Multiple simultaneous mood dimensions (station + room + citizen) — scope explosion

### Architecture Approach

The refactor is surgical: `HappinessManager` is the source of truth and is fully replaced; all consumers update against its new API. The singleton event-bus pattern (`GameEvents`) is preserved and extended with three new typed events. No new singletons or scene nodes are introduced. See `.planning/research/ARCHITECTURE.md` for full data-flow diagrams and build order.

**Major components and their changes:**
1. `HappinessManager` — **Replace**: owns `int _lifetimeHappiness` + `float _mood` + `float _baseline` + `MoodTier _currentTier`; drives per-frame decay via `_Process`; calls `EconomyManager.SetMoodTier` directly on tier change
2. `GameEvents` — **Extend**: add `LifetimeHappinessChanged(int)`, `MoodTierChanged(MoodTier, MoodTier)`, `MoodChanged(float)`; remove `HappinessChanged(float)` atomically to let the compiler surface all missed subscribers
3. `EconomyManager` — **Modify**: replace `_currentHappiness float` + `SetHappiness(float)` with `_currentTier MoodTier` + `SetMoodTier(MoodTier)`; income multiplier becomes a tier lookup table (1.0 / 1.1 / 1.2 / 1.3 / 1.4), not a formula
4. `SaveManager / SaveData` — **Extend + migrate**: bump `Version` 1→2; add `LifetimeHappiness` and `Mood` fields; static `MigrateV1ToV2` method; update event subscriptions
5. `HappinessBar` (UI) — **Replace entirely**: create `HappinessCounter.cs` (subscribes to `LifetimeHappinessChanged`) and `MoodDisplay.cs` (subscribes to `MoodTierChanged`, spawns floating text)
6. `CitizenManager`, `WishBoard`, `BuildManager` — **No change**

**Build order (dependency-driven):** HappinessManager refactor → GameEvents event changes → EconomyManager update → SaveManager migration → HUD replacement → Integration smoke test.

### Critical Pitfalls

See `.planning/research/PITFALLS.md` for full detail, warning signs, and recovery strategies.

1. **Save migration data loss** — v1 saves silently zeroing all lifetime happiness if no version gate exists. Prevention: add `if (data.Version < 2) data = MigrateV1ToV2(data)` immediately after deserialization in `Load()`; test with an actual v1 `save.json`. Highest-severity risk.

2. **HappinessChanged event contract break** — stale subscribers on the old `Action<float>` event receive raw `mood` (not in [0,1]); `EconomyManager.SetHappiness` clamps to 1.0, producing maximum income multiplier forever. Prevention: delete `HappinessChanged` early; migrate all consumers in the same phase.

3. **Autosave storm from continuous mood decay** — `MoodChanged` emitted every frame from `_Process` causes `SaveManager` debounce timer to reset every frame; game never saves during idle decay. Prevention: emit `MoodChanged` only on tier change or with a 1-second minimum interval; wire `SaveManager` to `MoodTierChanged` and discrete events only.

4. **Tier boundary oscillation (chatter)** — rapid tier toggling when mood rests near a threshold produces repeated notifications that look broken. Prevention: implement hysteresis (demotion threshold = boundary - 0.5) and a minimum tier hold time of 5 seconds.

5. **Decay feels punishing at low lifetime happiness** — first wish grants Cozy tier, but half-life returns mood below threshold before the next wish. Prevention: tier-aware decay rates (lower tiers decay slower); enforce "resting tier floor" (displayed tier cannot fall below `ComputeTier(baseline)`); put decay rates in a config resource for tuning without recompile.

---

## Implications for Roadmap

`HappinessManager` is the dependency root of this milestone. All consumer phases must follow it. The save migration window is narrow — once a v2 save exists on disk, v1 migration is a legacy concern. HUD is purely cosmetic and has no downstream impact, making it the safest last step.

### Phase 1: HappinessManager Refactor + Event Bus Migration

**Rationale:** This is the dependency root. All other phases consume `HappinessManager`'s new API and `GameEvents`'s new events. `MoodTier` enum (Step 1) must exist before the new `GameEvents` signatures (Step 2) can compile. Deleting `HappinessChanged` here forces the compiler to surface every missed subscriber — the most effective early-warning mechanism available.

**Delivers:** New `HappinessManager` with `int _lifetimeHappiness` + `float _mood` dual-value state; `MoodTier` enum; three new `GameEvents` events; old `HappinessChanged` deleted; `_Process` decay loop with hysteresis and tier-aware decay rates; `MoodGainPerWish` applied on wish fulfillment; integer-threshold blueprint milestone checks.

**Avoids:** Autosave storm (emit discipline established here); tier boundary oscillation (hysteresis implemented here); event contract break (old event deleted here, compiler enforces migration).

**Research flag:** No additional research needed — all APIs verified at HIGH confidence against live codebase and Godot 4.4 docs.

---

### Phase 2: Save Migration (v1 → v2)

**Rationale:** Highest-severity risk and must be verified against a real v1 `save.json` before any v2 save is written. Once a v2 save exists, the migration window is narrow. This phase is second — before consumer updates — so migration is tested with a stable but not-yet-fully-wired system.

**Delivers:** `SaveData.Version` bumped to 2; `LifetimeHappiness` and `Mood` fields added; `Happiness` field retained for migration read-only; static `MigrateV1ToV2` in `SaveManager`; `CollectGameState` and `ApplyState` updated to new `RestoreState` signature; `SaveManager` event subscriptions migrated from `HappinessChanged` to `MoodTierChanged` + `LifetimeHappinessChanged`.

**Avoids:** Pitfall 1 (migration data loss); Pitfall 6 (blueprint double-fire — carry `CrossedMilestoneCount` forward unchanged from v1 save).

**Verification gate:** Load an actual v1 `save.json`; confirm `LifetimeHappiness > 0`; confirm no `BlueprintUnlocked` events fire on session start; confirm mood is set to computed baseline (not zero).

**Research flag:** No additional research needed — `System.Text.Json` missing-field behavior verified at HIGH confidence.

---

### Phase 3: EconomyManager Consumer Update

**Rationale:** Smallest consumer change; validates the new `MoodTier` API against a real downstream system before the HUD is built. Contains all changes in a single file.

**Delivers:** `EconomyManager._currentTier` replaces `_currentHappiness`; tier lookup table for income multiplier (1.0 / 1.1 / 1.2 / 1.3 / 1.4 per tier); `GetIncomeBreakdown()` returns tier multiplier value; `HappinessManager` calls `SetMoodTier` directly on tier change (matching the existing `SetHappiness` direct-call pattern).

**Avoids:** Pitfall 2 (economy multiplier maxing out due to stale `SetHappiness` receiving un-clamped mood float).

**Research flag:** No additional research needed — standard pattern, all changes contained in one file.

---

### Phase 4: HUD Replacement

**Rationale:** Purely a consumer with no downstream impact. A broken HUD does not break the game loop, making it the safest phase to iterate on. `HappinessBar.cs` is deleted entirely (not patched) to eliminate stale code paths.

**Delivers:** `HappinessCounter.cs` subscribing to `LifetimeHappinessChanged(int)`, displaying `♥ N` with kill-before-create scale pulse (matching `CreditHUD` pattern); `MoodDisplay.cs` subscribing to `MoodTierChanged`, updating tier name with `AddThemeColorOverride("font_color", tierColor)`, spawning `FloatingText` tier change notification; both widgets initialize from `HappinessManager.Instance` in `_Ready()`; both wired into `QuickTestScene.tscn`.

**Uses:** `FloatingText` (existing, zero modifications); `Tween` kill-before-create pulse; `AddThemeColorOverride` for tier colors.

**Avoids:** Stale `HappinessBar` node persisting alongside new widgets; fighting tweens from un-killed previous animations.

**Research flag:** No additional research needed — all patterns proven in this codebase.

---

### Phase 5: Integration Smoke Test + Tuning

**Rationale:** End-to-end verification before any v1.x additions. Decay feel is a tuning concern, not a code correctness concern — it requires playtesting with real session behavior, not further implementation.

**Delivers:** Verified smoke test suite (fresh game → first wish → tier promotion → decay → save/load round-trip → v1 migration → blueprint unlock → economy multiplier change); decay rates and tier thresholds moved to a `HappinessConfig` resource for Inspector tuning without recompile; tier color contrast validated in-engine against the soft-3D pastel art style.

**Avoids:** Pitfall 5 (punishing early-game decay) — validated through first-session playtest at low lifetime happiness counts (0–5, 5–20, 20+ wishes).

**Research flag:** Playtesting-driven. No code research needed; tuning values require iterative testing.

---

### Phase Ordering Rationale

- **HappinessManager first** — defines the contract (MoodTier enum, event signatures, RestoreState signature) that every other phase consumes. Building consumers before the source of truth is defined guarantees rework.
- **Save migration second** — the migration window is narrow; once a v2 save exists, v1 migration becomes legacy work. Testing migration against a real file before any v2 session can be created is the only safe order.
- **EconomyManager third** — smallest consumer change; serves as API validation before the more visible HUD work.
- **HUD last among consumers** — HUD failure is purely cosmetic; does not break the game loop; safest place for iteration.
- **Tuning after integration** — decay feel cannot be assessed until all systems are wired and the full wish-fulfillment-to-tier-display loop is observable in a real session.

### Research Flags

No phase in this milestone requires `/gsd:research-phase` during planning. All technical decisions are resolved at HIGH confidence.

Phases with standard patterns (all verified against live code or Godot 4.4 docs):
- **Phase 1:** HappinessManager refactor — `_Process` decay, `GameEvents` C# event delegates, `MoodTier` enum lookup table
- **Phase 2:** Save migration — `System.Text.Json` missing-field behavior, `SaveData.Version` pattern
- **Phase 3:** EconomyManager update — direct-call pattern from HappinessManager already established
- **Phase 4:** HUD replacement — `AddThemeColorOverride`, `Tween` kill-before-create, `FloatingText.Setup()` reuse
- **Phase 5:** Tuning — empirical, not research-dependent

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All APIs verified against Godot 4.4 official docs and live codebase usage; no new packages required; every pattern has a proven precedent in the existing codebase |
| Features | MEDIUM | Cozy game genre research from developer/community sources (Kitfox, Lostgarden, wiki analysis); no direct competitor in the cozy-space-station-wish-loop niche; patterns cross-validate well against established genre conventions |
| Architecture | HIGH | Based on direct reading of every affected source file in the live codebase; no speculative API research; build order confirmed against actual dependency graph; all integration points identified with specific file locations |
| Pitfalls | HIGH | Six specific pitfalls identified with warning signs, recovery strategies, and phase assignments; each grounded in direct codebase inspection; not speculative |

**Overall confidence:** HIGH

### Gaps to Address

- **Decay rate tuning:** `DecayRate = 0.02`, `BaselineFactor = 1.0`, and `MoodGainPerWish = 3.0` are design-spec values not yet validated against real play sessions. Move to `HappinessConfig` resource in Phase 5 — these numbers will change. Verify against fresh-game (0–5 wishes), early-game (5–20 wishes), and mid-game (20+ wishes) scenarios before shipping.

- **Tier threshold validation:** The design-spec thresholds (2.0 / 5.0 / 10.0 / 18.0) and their interaction with the baseline formula (`sqrt(lifetimeHappiness)`) need spreadsheet validation. At 25 wishes, baseline = 5.0 (Lively resting tier); at 50 wishes, baseline ≈ 7.07; at 100 wishes, baseline = 10.0 (Vibrant resting tier). Verify these feel earned, not automatic.

- **Tier colors in-engine:** The five tier colors (gray / coral / amber / gold / soft white) need visual validation against the game's soft-3D pastel art style and HUD background. Confirm contrast in-engine, not just in code. This cannot be verified from code inspection alone.

- **Radiant tier visual treatment:** The pitfalls research flags that reaching the peak tier deserves visual treatment beyond a text label color change. Deferred to v1.x but noted as a known emotional payoff gap at the progression ceiling.

---

## Sources

### Primary (HIGH confidence — live codebase + official docs)

- `/workspace/Scripts/Autoloads/HappinessManager.cs` — current v1 state; all change points identified
- `/workspace/Scripts/Autoloads/GameEvents.cs` — event bus pattern confirmed; event signatures verified
- `/workspace/Scripts/Autoloads/SaveManager.cs` — migration hook location confirmed
- `/workspace/Scripts/Autoloads/EconomyManager.cs` — `SetHappiness` call site confirmed; income formula confirmed
- `/workspace/Scripts/UI/HappinessBar.cs` — replacement target; widget structure analyzed
- `/workspace/Scripts/UI/FloatingText.cs` — reuse confirmed; `Setup()` API verified
- `/workspace/.planning/design/happiness-v2.md` — design spec; all formulas and tier values sourced here
- [Godot 4.4 Tween class docs](https://docs.godotengine.org/en/4.4/classes/class_tween.html) — `TweenProperty`, `Kill`, `SetParallel` verified
- [Frame Rate Independent Damping using Lerp](https://www.rorydriscoll.com/2016/03/07/frame-rate-independent-damping-using-lerp/) — exponential decay math
- [System.Text.Json missing members behavior](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members) — default-to-zero on missing fields confirmed

### Secondary (MEDIUM confidence — community/genre sources)

- [Designing for Coziness — Kitfox Games](https://medium.com/kitfox-games/designing-for-coziness-d33d2519a59e) — cozy genre design principles; no-punishment loop definition
- [Cozy Games — Lostgarden](https://lostgarden.com/2018/01/24/cozy-games/) — foundational genre theory; permanent progress vs. fluctuating state pattern
- [RimWorld Mood Wiki](https://rimworldwiki.com/wiki/Mood) — named tier anti-pattern reference (named tiers with consequences)
- [Spiritfarer Mood Wiki](https://spiritfarer.fandom.com/wiki/Mood) — named tier pattern reference (named tiers without punishment)
- [Hysteresis — Shawn Hargreaves](https://shawnhargreaves.com/blog/hysteresis.html) — tier boundary oscillation prevention
- [Hysteresis — Wikipedia](https://en.wikipedia.org/wiki/Hysteresis) — control systems context for tier chatter

### Tertiary (LOW confidence — industry blogs)

- [Balancing Relaxation and Engagement in Cozy Games — SDLC Corp](https://sdlccorp.com/post/balancing-game-mechanics-for-relaxation-and-engagement-in-cozy-games/) — general UX guidance; cross-referenced with higher-confidence sources
- [Event-Driven Architecture pitfalls — Medium/InsiderEngineering](https://medium.com/insiderengineering/common-pitfalls-in-event-driven-architectures-de84ad8f7f25) — event contract break patterns

---

---

# V1.0 Foundation Research Summary

*Preserved from 2026-03-02. Covers the original Orbital Rings v1.0 implementation research.*

---

**Project:** Orbital Rings
**Domain:** Cozy 3D space station builder / management game with citizen wish loop
**Researched:** 2026-03-02
**Confidence:** MEDIUM-HIGH

## Executive Summary (v1.0)

Orbital Rings is a cozy 3D builder in a largely unoccupied market position: no competitor combines named individual citizens with personal wishes, a genuinely no-punishment loop, and a soft-3D space setting. The engine foundation is already locked — Godot 4.6 C# with Forward Plus rendering and Jolt Physics — and is the right choice for this scope. The architecture is well-understood: a layered Node + service pattern with a small set of Autoload singletons, custom Resource data definitions, and an EventBus for cross-system communication. The ring geometry is the only genuinely novel engineering problem, and it has a clear solution: a custom polar-coordinate SegmentGrid (not Godot's GridMap), mathematical segment selection (not trimesh collision), and a hand-authored walkway navmesh (not auto-baked).

The core feature loop is build → wish → grow, driven by a single credit currency and a happiness score that gates both citizen arrivals and blueprint unlocks. This is a simpler and more coherent economy than most management games in this space — intentionally so, to preserve the cozy genre promise of no punishment and no stress. The risk is not complexity but permissiveness: without a fail state or resource scarcity, the positive feedback loop (citizens → credits → rooms → more citizens) can go runaway within 15-20 minutes of play. Economy balance must be treated as a first-class design concern, modeled in a spreadsheet before any credit numbers are baked into code.

The most critical pitfalls are all addressable at project setup time: C# signal lifecycle hygiene, data/logic separation (keeping parameters in Resource files, not C# constants), mathematical segment selection, and hand-authored ring navmesh. These decisions must be made in the first phase; retrofitting them is expensive. The citizen simulation at ~20 citizens is well within Godot's comfortable performance range, so ECS and other architectural complexity are unnecessary — the project's biggest risk is early architectural shortcuts, not scale.

---

*Research completed: 2026-03-04 (v1.1 update) | 2026-03-02 (v1.0 foundation)*
*Ready for roadmap: yes*
