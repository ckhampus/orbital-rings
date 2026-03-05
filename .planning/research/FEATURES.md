# Feature Landscape

**Domain:** Cozy space station builder / management game with citizen wishes
**Researched:** 2026-03-05
**Confidence:** MEDIUM — based on genre analysis of comparable games; no direct competitor in the cozy-space-station-wish-loop niche. Findings cross-validated across multiple sources.
**Scope note:** This document covers the original v1.0 game features, the v1.1 Happiness v2 mood/tier system, and a new section specific to the v1.2 Housing System milestone.

---

## v1.2 Housing System -- Feature Landscape

*Focused research for citizen home assignment, return-home behavior, housing manager, resident display, and save/load housing data. Added 2026-03-05.*

### Problem the Milestone Solves

Housing rooms exist on the ring but function as a faceless capacity gate -- a global integer that permits or blocks citizen arrivals. Citizens have no concept of "home." The player builds cozy living spaces but nobody lives in them. The relationship between citizens and their housing is invisible.

The v1.2 milestone makes housing personal: each citizen has a home room they visibly return to, creating a visible bond between citizen identity and the spaces the player built.

---

### Table Stakes for a Housing Assignment System

Features players expect from any game where named characters have homes. Missing these makes the system feel broken or incomplete.

| Feature | Why Expected | Complexity | Dependencies | Notes |
|---------|--------------|------------|--------------|-------|
| Automatic home assignment on arrival | Banished, Oxygen Not Included, and Dwarf Fortress all auto-assign housing. In a cozy game with no management burden, forcing manual drag-to-assign would violate the genre. Players expect a newly arrived citizen to just have a home | LOW | Requires: HousingManager singleton, RoomPlaced/RoomDemolished events | Algorithm: pick housing room with fewest occupants (even spread). Ties broken randomly. Existing event infrastructure (GameEvents.RoomPlaced, CitizenArrived) already provides the triggers |
| Reassignment on room demolish | Every builder with demolish (Banished, Cities Skylines, Before We Leave) handles displaced residents. If the player tears down a Bunk Pod and the two residents just vanish from the housing map with no feedback, it feels like a bug | LOW | Requires: HousingManager tracking citizen-to-room map, RoomDemolished event | Displaced citizens become unhoused, then immediately attempt reassignment to other housing rooms with capacity. Oldest citizen first for fairness |
| Unhoused citizens handled gracefully | The cozy philosophy demands no punishment. Stardew Valley NPCs without a home still function; Spiritfarer spirits without a cabin still exist on the boat. If citizens without housing sulked, lost mood, or left, it would break the no-fail-state promise | LOW | None beyond existing citizen behavior | Unhoused citizens walk, wish, and fulfill identically. They just skip the return-home cycle. Info panel shows "Home: --" (em dash). No sad faces, no debuffs, no departure |
| Visible return-home behavior | Dwarf Fortress dwarves sleep in beds. Stardew Valley NPCs walk home at night. RimWorld colonists return to their rooms. The act of going home is table stakes for "citizens have homes." Without it, housing assignment is invisible data -- it might as well not exist | MEDIUM | Requires: home assignment working, existing visit animation pipeline, new home timer on CitizenNode | Reuses the existing drift-fade-wait-fade-drift animation pipeline from room visits. Longer rest duration (8-15s vs 4-8s) to feel like sleeping, not a quick visit. 90-150s cycle timer |
| "Zzz" visual indicator during home rest | Stardew Valley shows characters sleeping with Zzz. Dwarf Fortress wiki explicitly discusses sleep indicators. Animal Crossing NPCs show resting states. Without a visual distinction, a home visit looks identical to a regular room visit -- the player cannot tell the system exists | LOW | Requires: return-home behavior, FloatingText or Sprite3D | Reuse the existing FloatingText class with smaller, lighter-colored "Zzz" text. Appears when citizen enters home room. Subtle, not attention-grabbing. Same self-destruct-after-animation pattern as credit floaters |
| Home location shown in citizen info panel | When the player clicks a citizen, they expect to see where that citizen lives. Stardew Valley's social tab shows where each NPC lives. RimWorld's info panel shows assigned bedroom. This is the read-back mechanism -- without it, the player has no way to discover assignments | LOW | Requires: CitizenInfoPanel (exists), home segment index from HousingManager | Add one label line: "Home: Bunk Pod (Outer 3)" or "Home: --" for unhoused. Approximately 15 lines of code in existing CitizenInfoPanel.ShowForCitizen() |
| Save/load preserves housing assignments | Every game with persistent state preserves housing. If the player saves with 5 citizens in homes, loads, and finds them all unhoused, it breaks trust in the save system | LOW | Requires: SaveData extension, SaveManager changes, HousingManager.RestoreState | Add `Dictionary<string, int> CitizenHomes` to SaveData (citizen name to segment index, -1 for unhoused). On load, restore assignments. On invalid segment (demolished room in save), treat as unhoused and attempt reassignment |
| New housing fills unhoused citizens | Banished and Dwarf Fortress both auto-assign homeless residents when new housing is built. If the player builds a Sky Loft and unhoused citizens just keep wandering with no home, the player has to wonder if it is broken | LOW | Requires: HousingManager subscribed to RoomPlaced event | On RoomPlaced for Housing category: find all unhoused citizens, assign oldest-first until new room is full. Uses the same even-spread algorithm |

---

### Differentiators for This Housing System

Features that go beyond baseline housing implementations. Not assumed by players, but create a more satisfying and distinctive system.

| Feature | Value Proposition | Complexity | Dependencies | Notes |
|---------|-------------------|------------|--------------|-------|
| Size-scaled capacity (base + segments - 1) | Most builders use fixed capacity per room type regardless of size. Giving larger rooms more capacity (a 2-segment Bunk Pod holds 3 instead of 2) rewards the player's spatial investment and matches the intuition that bigger rooms hold more people. Before We Leave uses tile-count for building effectiveness; this is the same principle applied to housing | LOW | Requires: HousingManager reading segment count from PlacedRoom data | Formula: `BaseCapacity + (segmentCount - 1)`. A 1-seg Bunk Pod = 2, 2-seg = 3. A 1-seg Sky Loft = 4, 2-seg = 5, 3-seg = 6. This replaces the fixed BaseCapacity read. One line of math, large perceived fairness gain |
| HousingConfig resource for tunable timing | Following the established pattern of HappinessConfig and EconomyConfig, a HousingConfig resource makes home-return timing adjustable in the Inspector without code changes. Designer-tunable constants are a pattern differentiator for this codebase -- it signals professional design discipline | LOW | None -- standalone Godot Resource | Fields: HomeTimerMin, HomeTimerMax, RestDurationMin, RestDurationMax, WishTimerPausedDuringRest (bool). Approximately 15 LOC. Matches existing config resource pattern exactly |
| Wish timer paused during home rest | When a citizen is resting at home, their wish generation timer pauses. This is a subtle but meaningful interaction: resting citizens are not thinking about new desires. It creates a realistic rhythm and prevents the player from seeing a wish bubble appear at a home room (which would feel wrong -- they just went to sleep) | LOW | Requires: return-home behavior, access to wish timer state | Stop _wishTimer when entering home, restart when exiting. Four lines of code in the home visit tween sequence |
| Room tooltip shows current residents | The existing SegmentTooltip shows "Bunk Pod -- Outer 3" when hovering a room. Adding "Residents: Pip, Nova" transforms the tooltip into a social window. This is a cozy differentiator -- it makes housing feel communal and personal. Spiritfarer's boat shows which spirit lives in which cabin through visual placement; Orbital Rings achieves the same through text | MEDIUM | Requires: HousingManager citizen-to-room lookup, SegmentTooltip extension | Need a reverse lookup: given a segment index, return list of citizen names assigned to that room. HousingManager maintains this as a Dictionary<int, List<string>>. SegmentTooltip adds residents line when room is Housing category |
| Even-spread algorithm (fewest-occupants-first) | Most auto-assign systems use first-available. Even-spread (always assign to the room with fewest current occupants) prevents a single Bunk Pod from being packed while a Sky Loft sits empty. The player sees citizens distributed across their housing, which feels fair and alive | LOW | Requires: HousingManager occupancy tracking | Sort available rooms by current occupant count ascending, pick first. Random tiebreak. Three lines of LINQ or loop logic |
| Home return is lower priority than active wishes | If a citizen has an active wish and the home timer fires, the wish takes priority. This prevents the frustrating scenario where a citizen ignores a wish-matching room they are walking past because their home timer went off. Wish fulfillment is the core loop; home behavior must never interrupt it | LOW | Requires: home timer check, access to _currentWish state | Guard clause: if `_currentWish != null`, reset home timer and skip. One if-statement. Preserves the primacy of the wish-driven building loop |
| HousingManager as separate autoload (not extending HappinessManager) | Housing assignment is a distinct concern from mood/happiness. Keeping it separate follows the single-responsibility pattern established by the existing 7 autoloads. HappinessManager already handles mood, tiers, arrival gating, and unlock milestones -- adding assignment mapping would bloat it | LOW | Requires: new autoload registration in project.godot, GameEvents subscription | Approximately 150 LOC. Subscribes to RoomPlaced, RoomDemolished, CitizenArrived. Owns the bidirectional citizen-to-room mapping. Clean separation of concerns |

---

### Anti-Features for This Housing System

Features that seem reasonable for a housing system but would undermine the cozy philosophy, add unacceptable complexity, or are explicitly scoped out.

| Anti-Feature | Why It Seems Reasonable | Why It Is Wrong Here | What to Do Instead |
|--------------|------------------------|---------------------|--------------------|
| Player-managed room assignments (drag citizen to room) | Adds strategic depth. Games like Oxygen Not Included and RimWorld let players assign specific beds/rooms to specific colonists | Introduces management burden. The design doc explicitly scopes this out: "fully automatic, no micromanagement." Cozy games minimize busywork. If the player has to manually assign 15 citizens to rooms, the housing system becomes a chore, not a delight | Fully automatic assignment with even-spread algorithm. The player's only decision is whether to build more housing -- same as v1.0 |
| Housing quality tiers or upgrades (comfort levels) | Bunk Pod = basic, Sky Loft = luxury. Citizens could prefer better housing. RimWorld has impressive/awful bedroom mood modifiers | Introduces a hierarchy that punishes early-game rooms. If citizens are unhappy in Bunk Pods and happy in Sky Lofts, the player is pressured to demolish early rooms. This creates optimization anxiety and undermines the "every room you build matters" ethos. The PRD explicitly excludes this | All housing rooms are equally valid homes. Bunk Pod and Sky Loft differ only in capacity and cost, not quality. No citizen preference for room type |
| Citizen preferences or roommate compatibility | "Pip prefers living near the Garden Nook" or "Nova and Pip are friends, give bonus when sharing a room." Spiritfarer has per-spirit preferences | Adds a combinatorial optimization problem. With 15+ citizens and multiple housing rooms, the player would need to track compatibility matrices. This is Dwarf Fortress territory, not cozy territory. The PRD explicitly defers this to later milestones | Random assignment with even spread. Citizens are happy wherever they are placed. Roommate display in tooltips creates a sense of community without mechanical consequences |
| Negative consequences for unhoused citizens (sad faces, mood penalty, departure) | "Realism" -- homeless people are unhappy. Cities Skylines shows homelessness as a problem. RimWorld colonists sleeping on floors get mood debuffs | Directly violates the no-fail-state philosophy. 5 starter citizens spawn before any housing exists. If being unhoused caused penalties, the game would start in a penalty state. Unhoused citizens must be indistinguishable from housed citizens in every way except the home-return cycle | Unhoused citizens walk, wish, and fulfill identically. The only consequence of insufficient housing is the existing one: new citizens will not arrive until capacity opens |
| Day/night cycle driving home-return timing | "Citizens go home at night" is a natural pattern from Stardew Valley, Animal Crossing, and real life. Tying home returns to a day/night cycle would feel more realistic | Requires implementing the deferred day/night system. The PRD and PROJECT.md explicitly defer day/night to future milestones. A timer-based cycle (90-150s) achieves the same visual effect of periodic home returns without depending on a time-of-day system | Random timer per citizen (90-150s). Creates organic, staggered home-return behavior. Looks alive without requiring a clock |
| Complex pathfinding to home room | Citizens should walk to their home room following the walkway, not teleport | Citizens already walk angularly along the walkway to visit rooms. The existing visit animation pipeline handles this: walk to segment, drift to room edge, fade out. Home visits reuse this exact pipeline. True A* pathfinding on the 1D circular walkway would be over-engineering -- the walkway is a circle, not a graph | Reuse existing visit animation pipeline. Angular walk to home segment, drift, fade, rest, fade back. Same code path as room visits, different duration and visual indicator |
| Room interior customization | "Since citizens live there, let players decorate the room interior" | Scope explosion. Procedural room interiors are explicitly deferred in PROJECT.md. Placeholder colored blocks are the current interior representation. Adding customization to housing rooms alone would create an inconsistency with all other room types | Placeholder interiors remain. The "Zzz" floater and the info panel "Home: Bunk Pod (Outer 3)" provide the personal connection without needing visible interiors |
| Housing capacity affecting mood or economy | "More housing = happier station" or "housing quality multiplier on income" | Housing already gates citizen arrivals. Adding another economic lever creates coupling between housing and the happiness system that makes both harder to tune. The economy was balanced around mood tier multipliers (1.0x-1.4x). Adding a housing multiplier would require re-tuning the entire economy | Housing gates arrivals only (existing behavior). Mood and economy remain driven by the wish-fulfillment loop and mood tier system. Clean separation of concerns |

---

### Feature Dependencies for v1.2 Housing

```
[HousingManager Autoload (NEW)]
    ├── subscribes to ──> [GameEvents.RoomPlaced]         (existing)
    ├── subscribes to ──> [GameEvents.RoomDemolished]      (existing)
    ├── subscribes to ──> [GameEvents.CitizenArrived]      (existing)
    ├── owns ──> [citizen-to-room assignment map]
    ├── owns ──> [room-to-citizens reverse lookup]
    └── provides ──> [AssignHome / UnassignHome / GetHomeSegment API]

[HousingConfig Resource (NEW)]
    └── consumed by ──> [HousingManager + CitizenNode home timer]

[CitizenNode home behavior (NEW)]
    ├── requires ──> [HousingManager.GetHomeSegment(citizenName)]
    ├── reuses ──> [existing visit animation pipeline (drift-fade-wait-fade-drift)]
    ├── adds ──> [home timer (90-150s cycle)]
    ├── adds ──> ["Zzz" FloatingText on home entry]
    └── adds ──> [wish timer pause during rest]

[CitizenInfoPanel update (EXTEND)]
    ├── requires ──> [HousingManager.GetHomeSegment(citizenName)]
    └── adds ──> [Home: Room Name (Location) line]

[SegmentTooltip update (EXTEND)]
    ├── requires ──> [HousingManager.GetResidentsForRoom(segmentIndex)]
    └── adds ──> [Residents: Name, Name line for Housing rooms]

[SaveData extension (EXTEND)]
    ├── adds ──> [CitizenHomes dictionary (citizenName -> segmentIndex)]
    └── requires ──> [HousingManager.GetAllAssignments() / RestoreAssignments()]

[SaveManager extension (EXTEND)]
    ├── collects ──> [housing assignments in CollectGameState()]
    └── restores ──> [housing assignments in ApplySceneState()]

[HappinessManager housing capacity (REFACTOR)]
    └── size-scaled capacity replaces fixed BaseCapacity read
        └── formula: BaseCapacity + (segmentCount - 1)
```

#### Dependency Notes for v1.2

- **HousingManager must initialize after BuildManager and CitizenManager:** It needs to query placed rooms and citizen list during startup. Autoload order in project.godot matters. Place it after HappinessManager (currently 6th), making it 8th (after SaveManager).
- **Home behavior requires HousingManager to be functional first:** CitizenNode's home timer queries HousingManager.GetHomeSegment(). If HousingManager is not ready, the timer fires but finds no assignment -- this is safe (citizen just skips the home visit), not a crash.
- **Save format must be backward-compatible with v2:** The new CitizenHomes dictionary defaults to empty/null when deserializing v2 saves without housing data. On load with missing housing data, all citizens start unhoused and get reassigned. No v2-to-v3 version bump needed -- use null-check on the new field.
- **SegmentTooltip extension depends on HousingManager reverse lookup:** The tooltip must be able to ask "who lives here?" for any segment index. HousingManager maintains this mapping and updates it on every assign/unassign.
- **Home return animation reuses the visit pipeline, not a new animation system:** No new scenes, no new shaders, no new animation resources. The tween sequence is nearly identical to StartVisit(), with different timing constants and the addition of a "Zzz" floater.
- **Wish timer pause creates a dependency between home behavior and wish system:** The _wishTimer is already a field on CitizenNode. Pausing it during home rest is a Stop/Start call pair embedded in the home visit tween. No new system coupling -- it is local to CitizenNode.

---

### Complexity Assessment

| Feature | Effort Estimate | Risk | Touch Points |
|---------|-----------------|------|--------------|
| HousingManager autoload | Low (~150 LOC) | Low -- follows established autoload pattern exactly | New file, project.godot registration |
| HousingConfig resource | Very Low (~15 LOC) | None -- identical pattern to HappinessConfig | New file, default .tres resource |
| Auto-assign on citizen arrival | Low (~30 LOC) | Low -- LINQ/loop over housing rooms | HousingManager |
| Reassign on room demolish | Low (~30 LOC) | Medium -- must handle rapid demolish-then-rebuild without stale references | HousingManager, GameEvents |
| Assign unhoused on new room build | Low (~20 LOC) | Low | HousingManager |
| Size-scaled capacity formula | Very Low (~5 LOC) | Low -- one arithmetic expression | HappinessManager.OnRoomPlaced |
| Return-home behavior on CitizenNode | Medium (~80 LOC) | Medium -- tween sequencing mirrors existing visit code, but must not conflict with active visit tween | CitizenNode |
| "Zzz" floater on home entry | Low (~15 LOC) | Low -- reuses FloatingText class | CitizenNode |
| Wish timer pause during rest | Very Low (~4 LOC) | Low | CitizenNode |
| CitizenInfoPanel home line | Very Low (~15 LOC) | None | CitizenInfoPanel |
| SegmentTooltip residents line | Low (~25 LOC) | Low -- need to determine where tooltip gets room data | SegmentTooltip, SegmentInteraction |
| SaveData + SaveManager extension | Low (~30 LOC) | Medium -- backward-compat with v2 saves that lack housing data | SaveManager, SaveData |
| Home return lower priority than wishes | Very Low (~5 LOC) | None -- simple guard clause | CitizenNode |

**Total estimated new/modified code:** ~420 LOC across 6-7 files. No architectural changes. No new scenes or assets.

---

### v1.2 MVP Definition

#### Must Ship (v1.2 Core)

All of these are required for the milestone to be considered complete. Without any one of them, the housing system feels incomplete.

- [ ] **HousingManager autoload** -- the singleton that owns citizen-to-room assignments
- [ ] **Automatic home assignment on citizen arrival** -- even-spread algorithm
- [ ] **Reassignment on room demolish** -- displaced citizens find new homes or become unhoused
- [ ] **Assign unhoused on new room build** -- oldest citizens first
- [ ] **Return-home behavior with tween animation** -- periodic cycle, reuses visit pipeline
- [ ] **"Zzz" floater on home entry** -- visual distinction from regular visits
- [ ] **Home line in CitizenInfoPanel** -- "Home: Bunk Pod (Outer 3)" or "Home: --"
- [ ] **Unhoused citizens handled gracefully** -- no penalty, no home cycle, just keep walking
- [ ] **Save/load housing assignments** -- preserve across sessions, backward-compatible

#### Should Ship (v1.2 Enhanced)

High-value additions that make the system feel polished. Implement if time allows.

- [ ] **Size-scaled capacity** -- `BaseCapacity + (segmentCount - 1)` -- one line of math, large fairness gain
- [ ] **HousingConfig resource** -- tunable timing constants, follows codebase pattern
- [ ] **Wish timer pause during home rest** -- subtle but correct
- [ ] **Home return lower priority than active wishes** -- protects core loop primacy

#### Defer to v1.3+ (Nice to Have)

Features that enhance housing but are not required for the milestone.

- [ ] **Room tooltip shows current residents** -- requires SegmentTooltip extension and reverse-lookup API. Adds scope to SegmentInteraction, which is the most complex input-handling code in the project
- [ ] **Home-return visual trail / path indicator** -- "see where citizen is going" -- adds scope without core value
- [ ] **Citizen roommate display in info panel** -- "Lives with: Nova, Pixel" -- requires more info panel layout work

---

### Feature Prioritization Matrix (v1.2 Scope)

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| HousingManager autoload | HIGH (foundation) | Low | P1 |
| Auto-assign on arrival | HIGH | Low | P1 |
| Reassign on demolish | HIGH | Low | P1 |
| Assign unhoused on new build | HIGH | Low | P1 |
| Return-home behavior | HIGH (the whole point) | Medium | P1 |
| "Zzz" floater | HIGH (visual proof system exists) | Low | P1 |
| Home line in info panel | HIGH (read-back mechanism) | Very Low | P1 |
| Graceful unhoused handling | HIGH (cozy promise) | Very Low | P1 |
| Save/load housing | HIGH (persistence trust) | Low | P1 |
| Size-scaled capacity | MEDIUM | Very Low | P1 |
| HousingConfig resource | MEDIUM | Very Low | P1 |
| Wish timer pause during rest | LOW | Very Low | P2 |
| Home return vs wish priority | MEDIUM | Very Low | P1 |
| Room tooltip residents | MEDIUM | Medium | P2 |

**Priority key:**
- P1: Required for v1.2 milestone
- P2: Valuable polish, implement if time allows

---

### Comparable Game Analysis (Housing Systems)

| Aspect | Banished | Dwarf Fortress | Stardew Valley | RimWorld | Oxygen Not Included | Orbital Rings v1.2 |
|--------|---------|----------------|----------------|----------|--------------------|--------------------|
| Assignment model | Auto (AI optimizes every 5 min) | Auto (claim nearest bed) | Fixed NPC schedules | Manual bed assignment | Manual room assignment | Auto (even-spread, fewest-occupants-first) |
| Reassignment on demolish | Auto-relocate | Reclaim new bed | N/A (fixed homes) | Manual re-assign | Manual re-assign | Auto-relocate, oldest-first |
| Sleep/rest visual | Citizens walk to home, enter | Dwarves sleep in bed with Zzz | NPCs walk home, screen shows sleeping | Colonists lie in bed | Duplicants lie in cot | Citizens drift-fade into home room, "Zzz" floater |
| Unhoused handling | Homeless = unhappy, may die | Sleep on floor = negative thought | N/A | Sleep on floor = mood penalty | Sleep on floor = stress | No penalty. Skip home cycle. Walk/wish normally |
| Capacity model | Per-house fixed | Per-room (bed count) | Fixed per building | Per-bed | Per-cot | Size-scaled: base + (segments - 1) |
| Player management burden | Low (auto) | Medium (manual zones) | None | High (manual) | High (manual) | None (fully automatic) |
| Info display | Town hall stats | Room query | NPC schedule page | Colonist needs tab | Room overlay | Citizen info panel + room tooltip |

**Key observation:** Orbital Rings sits in the "zero management burden" end of the spectrum (closest to Stardew Valley and Banished's auto-assign). The critical difference from Banished: no negative consequences for unhoused citizens. From Stardew: citizens visibly return home rather than just existing in preset locations. The "Zzz" floater is the visual proof that the system exists -- without it, players would not know housing assignment is happening.

---

## Happiness v2 Mood System -- Feature Landscape

*Focused research for the dual-value happiness system (Lifetime counter + Station Mood with decay and tiers). Added 2026-03-04.*

### Problem the Milestone Solves

Single-number happiness that only goes up hits a ceiling. Once a player reaches 100%, the progression system goes silent. For a cozy sandbox with no intended endpoint, this creates a finish line where there shouldn't be one. The goal is a system that stays alive indefinitely without punishing the player.

---

### Table Stakes for a Mood/Tier System

Features players expect from any mood system with named tiers and decay. Missing these makes the system feel incomplete or broken.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Named tier labels (not raw numbers) | Spiritfarer's "Ecstatic/Happy/Content" pattern; Kingdoms and Castles' colored indicators; Planet Zoo's welfare categories all establish that players prefer human-readable state names over raw floats. Seeing "Lively" is more emotionally resonant than "0.62" | LOW | Tier names are data, not code. The enum is simple; the names carry all the weight |
| Always-visible current mood state in HUD | Cozy game HUD research confirms: information players need frequently must be on-screen without requiring interaction. If players must open a menu to see mood, they will not act on it. SimCity 2013's happiness map, Planet Zoo's welfare indicators -- all always visible in some form | LOW | One label in the HUD corner. The design already specifies this correctly |
| Feedback on tier change | Kingdoms and Castles shows speech bubbles when citizens are unhappy; Spiritfarer shows mood changes through NPC dialogue cues; SimCity uses popup notifications on major changes. Players need to know when the state machine transitions -- they cannot track a float | LOW | Floating text on tier change is established, minimal, and correct. "Station mood: Lively" is sufficient -- no need for a full modal |
| Permanent progress counter separate from fluctuating state | Incremental/idle game design (well-documented in prestige literature) has proven that players need one metric that never goes backward alongside metrics that fluctuate. The "lifetime achievements" pattern (total cookies baked, total wishes fulfilled) prevents frustration when the fluctuating value dips | LOW | The split into Lifetime Happiness (int, monotonically increasing) + Station Mood (float, fluctuates) maps exactly to this established pattern |
| Decay that is clearly gentle, not punishing | Cozy game design research (Kitfox/Designing for Coziness) defines the genre around "no punishment loops." Mood decay in a cozy game must feel like settling/breathing, not losing. If the decay feels like punishment, it violates the genre promise. RimWorld's mood system is the anti-pattern: named tiers leading to bad consequences (mental breaks) | MEDIUM | The design's choice of a rising baseline (decay toward floor, not toward zero) is correct and essential. The baseline is what separates "cozy mood decay" from "punishment decay" |
| Mood behavior that rewards engagement without penalizing absence | Animal Crossing's town rating, Spiritfarer's spirit moods, Stardew Valley's friendship -- all maintain that the game should be better when you play, not worse when you don't. Absence should mean "not at peak," not "degraded from where you left off" | MEDIUM | The decays-to-baseline pattern handles this correctly: an idle player settles at baseline (warm, not zero), an active player peaks above it |
| Visual color differentiation per tier | Planet Zoo uses red/orange/green; Kingdoms and Castles uses color on house indicators; Spiritfarer uses dot position on a face scale. Color is a faster read than text alone, especially for ambient state communication | LOW | The design's tier colors (gray -> coral -> amber -> gold -> soft white) are appropriate. Each should be visually distinct at a glance |

---

### Differentiators for This Mood System

Features that go beyond baseline mood system implementations. Not assumed by players, but create a more satisfying and distinctive system.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Rising baseline tied to permanent progress | Most mood systems have a fixed floor (usually zero or a hardcoded low value). Tying the decay floor to lifetime wish count means a mature, well-loved station always feels warm. This is a direct expression of the "achievements are permanent" promise -- the station remembers its history | LOW | The sqrt(lifetimeHappiness) baseline formula is already designed. Low code complexity, high emotional payoff. This is the key differentiator of this mood design |
| "About to tier up" visual hint | Spiritfarer shows mood as a dot on a scale -- the player can see proximity to the next tier. The design spec calls for a subtle pulse/glow when mood is near the top of its tier range. This creates anticipation and rewards consistent play without requiring a percentage readout | MEDIUM | Requires a threshold calculation per-frame (is mood within X of next tier boundary?) and a shader/animation state. Worth doing -- it turns the tier system into a "almost there" motivator |
| Tier label color as ambient mood indicator | Rather than a separate "mood meter" bar (common in city builders), the tier label text itself changes color. This is space-efficient and keeps the HUD minimal. Color communicates intensity; the label name communicates category. No bar needed | LOW | Already in design spec. Validate that the five colors are distinguishable at the soft-3D pastel art style. Confirm contrast against the HUD background |
| Wish counter as the "score" that always feels good | In cozy builders without explicit scores (Townscaper, Tiny Glade), players invent their own metrics. Giving players a legible "score" that always increases (wish counter) satisfies the incremental game instinct without introducing competitive pressure. The heart icon + number is a brag number that anyone understands | LOW | No new systems needed -- it's just displaying the existing count. The heart icon + number framing is warm and unambiguous |
| Mood tiers drive citizen arrivals (qualitative thresholds, not continuous formula) | Most city builders (Kingdoms and Castles, SimCity) use continuous formulas: happiness x arrival_rate. Using discrete tier thresholds instead means the player sees a clear "if I reach Lively, more citizens arrive" cause-effect relationship. Qualitative tiers are more legible than a continuous multiplier | LOW | The design's tier to arrival scale table (0.0 / 0.2 / 0.4 / 0.6 / 0.8) replaces the v1 `happiness * 0.6` formula. Simpler to understand as a player |

---

### Anti-Features for This Mood System

Features commonly implemented in mood systems that would undermine the cozy promise or add unnecessary complexity.

| Anti-Feature | Why It Seems Reasonable | Why It's Wrong Here | What to Do Instead |
|--------------|------------------------|---------------------|--------------------|
| Mood decay toward zero | Standard in simulation games (RimWorld stress, Dwarf Fortress needs, even Animal Crossing town ratings decay if ignored for months) | Zero-floor decay means an idle station eventually hits the lowest tier. That feels punishing. A player returning after a week should not be greeted with "your station is Quiet" | Decay toward a rising baseline. The baseline is the cozy promise in code |
| Mood as a failure state / negative consequences | RimWorld mental breaks, Dwarf Fortress mood spirals, SimCity citizen emigration when unhappy -- all use mood failure to create pressure | The design doc explicitly says "no fail state." A mood tier that causes citizens to leave, rooms to close, or economy to go negative is a punishment loop. Violates the genre. | The Quiet tier means "no new arrivals" -- citizens who are here stay. Existing station is always intact. The only consequence of low mood is opportunity cost (slower growth), never loss |
| Showing the raw float to the player | "Transparency" -- players could see the exact mood value for precise feedback | Knowing mood is 7.3 vs 7.8 creates optimization anxiety. Named tiers make the system approachable. Spiritfarer hides the numeric mood and shows a face-scale for this exact reason | Tier label only in HUD. Raw value accessible in debug mode only |
| Diminishing returns on mood gain (per wish) | v1 happiness used diminishing returns (each wish raised happiness less as it got higher) | With a decay-toward-baseline system, diminishing returns are already built in naturally -- you can only fulfill wishes so fast, so mood stabilizes at an activity-proportional level. Adding explicit diminishing returns on top makes the math complex and harder to tune | Flat gain per wish (MoodGainPerWish = 3.0 as designed). The decay rate provides the natural ceiling |
| Multiple simultaneous mood dimensions (citizen mood + station mood + individual room mood) | Spiritfarer has per-spirit moods; Planet Zoo has per-animal welfare categories | For this milestone, introducing room-level mood or citizen-level mood would require new UI to surface them, new systems to maintain them, and new design to explain their relationship to station mood. Scope explosion. | One station mood, one lifetime counter. Keep it dual, not triple or quad. Individual citizen reactions (animated response to tier changes) are cosmetic, not tracked values |
| Animated mood bars and gauges | Standard builder UI (SimCity, Kingdoms and Castles, Fabledom) uses progress bars for happiness | Bars show progress toward a target. In a fluctuating system, a bar constantly animating up/down creates visual noise and implies "fill the bar" thinking. The tier system is designed to avoid this | Tier label text + color only. No bar. If players want to see "how close to the next tier," the "about to tier up" pulse provides that non-numerically |
| Persistent tier-change banners that require dismissal | Many games show achievement banners that block the view until clicked | Mood changes should be ambient, not imperative. A floating text that auto-fades in 2 seconds is the correct pattern for a non-interrupting cozy game | Auto-fading floating text on tier change. No blocking UI. No "press X to continue" |
| Mood decay that resets on session load | Some idle games reset fluctuating values on load for simplicity | If players load a save and see their mood has dropped significantly from where they left, it feels punishing even if technically "correct" | Persist both lifetimeHappiness and mood in the save. On load, reconstruct the baseline from persisted lifetime count. Mood value carries over as saved |

---

### Feature Dependencies for Happiness v2

```
[v1 HappinessManager]
    └──replaced by──> [v2 HappinessManager]
                           ├── tracks ──> [lifetimeHappiness: int]  (monotonic)
                           └── tracks ──> [mood: float]              (fluctuating)
                                              ├── gains from ──> [WishBoard.WishFulfilled event]
                                              ├── decays toward ──> [baseline = sqrt(lifetime)]
                                              └── drives ──> [MoodTier enum]

[MoodTier enum]
    ├── drives ──> [CitizenManager.ArrivalScale]     (replaces v1 happiness*0.6)
    ├── drives ──> [EconomyManager.IncomeMultiplier] (replaces v1 1 + happiness*0.3)
    └── drives ──> [HUD tier label + color]

[lifetimeHappiness]
    ├── drives ──> [Blueprint unlocks at 4, 12]     (replaces v1 0.25, 0.60 thresholds)
    └── drives ──> [HUD wish counter N]

[HUD]
    ├── shows ──> [N counter]    (always visible, warm-pulse on increment)
    └── shows ──> [Tier label]      (always visible, color per tier)

[Tier change detection]
    └── emits ──> [floating text "Station mood: Lively"]   (auto-fades ~2s)

[SaveManager]
    ├── persists ──> [lifetimeHappiness]
    ├── persists ──> [mood]
    └── migrates ──> [v1 _happiness float -> estimated lifetime + baseline mood]
```

#### Dependency Notes for v1.1

- **WishBoard.WishFulfilled must emit before HappinessManager processes:** The event-driven architecture already handles this. HappinessManager subscribes to the wish fulfillment event. No new event bus plumbing needed.
- **CitizenManager and EconomyManager only need tier, not raw mood:** Pass the enum value, not the float. This simplifies callers and makes tier thresholds easy to tune without touching arrival/economy code.
- **Blueprint unlock check now uses lifetimeHappiness integer:** Simpler comparison than the v1 float threshold. No longer possible to "almost unlock" -- milestones are exact counts.
- **SaveMigration is one-way:** v1 to v2 migration at load time. No need to write v1-compatible saves after migration. Keep migration code but do not complicate it.
- **"About to tier up" hint requires no new data:** Current mood, current tier boundaries, and a threshold (e.g. within 1.0 unit of next boundary) are all already computed. It's a HUD rendering concern, not a new system.

---

### Complexity Assessment

| Feature | Effort Estimate | Risk |
|---------|-----------------|------|
| lifetimeHappiness integer counter | Very Low | None -- simpler than existing float |
| mood float with gain on wish event | Low | None -- existing event subscription pattern |
| Decay-toward-baseline in _Process | Low | Tuning risk: DecayRate and BaselineFactor need playtesting |
| MoodTier enum with 5 states | Very Low | None -- simple range lookup |
| CitizenManager: tier to arrival scale | Low | None -- replaces a formula with a lookup |
| EconomyManager: tier to income multiplier | Low | None -- replaces a formula with a lookup |
| HUD: wish counter with pulse on increment | Low | None -- matches existing credit counter pattern |
| HUD: tier label with tier color | Low | Low -- color lookup per enum value |
| Floating text on tier change | Low | Ordering risk: must not fire on initial load (check previous tier != new tier) |
| "About to tier up" subtle pulse | Medium | Medium -- shader/animation state; skip if timing is tight |
| v1 save migration | Low | Medium -- migration formula is an estimate; test with real v1 saves |
| Blueprint unlocks (count thresholds) | Very Low | None -- simpler logic than v1 float comparison |

---

## Mood Communication Patterns -- Reference from Other Games

Research findings on how established games communicate mood state changes to players.

### How Games Show Tier/State Names

**Spiritfarer (Thunder Lotus, 2020):**
- Mood shown as a dot on a face-scale (5 states: crying/sad/neutral/happy/ecstatic)
- Player checks by talking to the spirit; not always-on HUD
- Influences listed as text with up/down arrows showing contribution
- Temporary influences marked with a clock icon that fades
- Verdict: Good pattern for per-entity mood; too complex for station-level aggregate

**Kingdoms and Castles:**
- Per-house happiness shown as color (red/yellow/green)
- Global happiness shown as a bar 0-100
- No named tiers -- just numeric value
- Verdict: Numeric bars feel more like management sim than cozy game

**Planet Zoo:**
- Per-animal welfare shown as color-coded categories (Nutrition, Social, Stress, etc.)
- Overall welfare shown as a percentage
- No named tiers for overall state
- Verdict: Color-coded categories useful; named tiers absent in favor of raw %

**RimWorld:**
- Named mood states (Broken, Minor Break, Major Break, Fine, Happy, etc.)
- Always visible in colonist overview bar
- Threshold-based with named consequences
- Verdict: Named states are correct. Consequence-based tiers are wrong for cozy. The lesson: use the naming, not the punishments.

**Animal Crossing (series):**
- Town rating named (Trashy/Average/Good/Great/Perfect) and communicated via NPC dialogue
- Not always-on HUD; player must ask the owl character
- Feedback is through environmental changes (perfect town flowers)
- Verdict: Named tiers work. Environmental feedback (flowers blooming) is beautiful but requires more art investment than a HUD label.

### How Games Communicate State Changes

**Floating text (most common, least intrusive):**
- Standard "damage numbers" / "XP gained" pattern adapted for state changes
- Text floats up from a point, fades after 1-3 seconds
- No player action required
- Used in: most ARPGs and city builders for minor notifications
- Verdict: Correct pattern for tier change notification. Duration: 2 seconds is standard.

**Ambient visual treatment (no text, environmental):**
- Animal Crossing's perfect town flowers, color shifts in the world
- Requires art support; hard to retrofit
- Verdict: Ideal long-term direction; out of scope for this milestone

**Modal/banner notifications (blocking or semi-blocking):**
- Achievement banners (Steam, console)
- Full-screen messages (some city builders on major events)
- Verdict: Wrong for cozy. Even a non-blocking banner for a tier change would feel jarring. Floating text is sufficient.

**Persistent ambient HUD label (recommended pattern for this game):**
- Status label always on screen, updates in place without animation on non-change frames
- Brief animation (pulse, color shift) on the label itself when tier changes
- Floating text provides the "something happened" moment; the HUD label provides ongoing state reference
- Verdict: The design spec's approach (always-visible tier label + floating text on change) is exactly right.

---

## Original Feature Landscape (v1.0)

*Preserved from 2026-03-02 research. Covers the full game feature set.*

### Table Stakes (Users Expect These)

Features cozy builder players assume exist. Missing these = product feels incomplete or wrong genre.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Free-form placement with immediate visual feedback | Every cozy builder (Townscaper, Tiny Glade, Islanders) teaches players to expect instant visual response to placement actions | LOW | Segment snapping to the ring satisfies this; the donut grid is the constraint that makes it tractable |
| No fail state / no punishment loop | Cozy genre definition: "safety, abundance, softness." Players leave if they can be punished. Islanders, Townscaper, Before We Leave all remove hard failure | LOW | Already in design; unfulfilled wishes linger harmlessly -- this is correct |
| Single soft progression currency | Cozy players reject multi-resource economies. Before We Leave, Fabledom both reduce economy to 1-2 simple resources. Complexity = stress = not cozy | MEDIUM | Credits alone is right; happiness as a second "currency" driving unlocks is the well-established cozy pattern |
| Visible named characters that feel alive | Spiritfarer, Stardew Valley, Fabledom: named NPCs with persistent identity are table stakes for the "community" subgenre of cozy. Anonymous pawns break emotional connection | MEDIUM | Named citizens walking the ring and entering rooms establishes this. Even minimal animations matter enormously |
| Satisfying build placement audio/visual feedback | Townscaper's "snap" sound is cited repeatedly as core to its appeal. Cozy games use sound to signal reward. Placement must feel good to touch | LOW | Snap sound + brief animation on room placement. Cannot ship without this -- it IS the feel |
| Readable room diversity at a glance | Players need to see at a glance what each room is. Identical-looking rooms with different labels = frustration | MEDIUM | Five categories with visually distinct silhouettes/colors (placeholder art must still be distinct-per-type) |
| Clear wish / request display | Players cannot respond to citizen needs they cannot read. Speech bubbles, a wish board, or a sidebar notification -- any format works, but wishes must be visible and legible | LOW | Speech bubbles above citizens are the simplest implementation. A persistent wish list panel is v1.x |
| Graceful unlock progression | Players expect to unlock new options as they grow. Islanders uses pack selection; Fabledom uses happiness milestones. Blank slate forever = boredom | MEDIUM | Happiness-gated blueprint unlocks are the right shape. Must have at least 2-3 unlock moments in v1 |
| Orbiting/panning camera with zoom | Standard expectation from any 3D builder. Fixed-tilt diorama cameras (Fabledom, Aven Colony) are the norm for cozy 3D. Free-look is bonus | MEDIUM | Horizontal orbit + zoom is v1. Vertical pan becomes necessary only when second ring arrives |
| Demolish / rearrange without heavy penalty | Cozy players experiment. Penalizing demolition is anti-cozy. Fabledom, Before We Leave, and Islanders all allow this freely | LOW | Partial refund on demolish is correct; full refund would remove all economic tension |
| Ambient visual "aliveness" | Tiny Glade's ivy, Townscaper's water, Slime Rancher's slimes wandering -- something must move in the world when the player is idle | MEDIUM | Citizens walking the walkway and entering rooms provides this. Idle ambient motion on citizens is essential |

---

### Differentiators (Competitive Advantage)

Features that set Orbital Rings apart from other cozy builders. Not assumed by genre players, but create distinctive identity and word-of-mouth.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| The ring as constrained canvas | The donut ring is a unique puzzle: 24 segments, inner + outer rows, spatial scarcity by design. Islanders uses scoring pressure; Orbital Rings uses *geometric constraint* as creative tension without punishment | MEDIUM | This is the core differentiator. The ring shape forces meaningful layout decisions that flat grids don't. Lean into it -- highlight the geometry in all communication |
| Citizen wish to build to growth feedback loop | Spiritfarer does wishes as quests; Fabledom does needs as meters. Orbital Rings' version -- passive speech-bubble wishes that reward but never punish -- is a gentler and more personal take. Citizens have names and individual wishes, not just aggregate "food: 40%" | HIGH | This is the second core differentiator. Every other space builder (Aven Colony, Space Station Tycoon) tracks aggregate morale metrics, not individual named wishes. The personal scale is the bet |
| Space setting for a cozy genre | Cozy builders default to pastoral/medieval. Space = rare setting for coziness. Before We Leave and Aven Colony exist but both lean simulation-heavy. A genuinely soft-3D cozy space station is an underserved position | LOW | Low implementation complexity (it's an art/tone choice), HIGH market differentiation. The pastel-palette space diorama is visually distinctive |
| Modular ring expansion as milestone celebration | Completing the first ring feels like an achievement. Adding a new ring stacked on top gives the player a moment of "look what I built." This is more legible as progress than watching a stat counter increment | HIGH | Deferred to v2+, but the *architecture* of the ring system must support it. Single ring for v1 must leave expansion hooks |
| Citizens visiting specific rooms (spatial preference) | When a citizen walks past three empty rooms and enters the Observatory, it shows they care about that room. Spatial preference feedback (citizens gravitating toward fulfilled wishes) makes the world readable and rewarding without a UI tooltip | MEDIUM | Requires pathing that respects room preferences, not just random walking. Medium complexity, HIGH emotional payoff |
| No tutorial -- learn through citizen wishes | Cozy games increasingly remove tutorials (Tiny Glade has none; Islanders minimal). Citizens become the teaching mechanism: a wish for a Cafe means "build a Cafe." This is novel as a pure implicit tutorial design | LOW | Low implementation cost once wishes exist. Eliminates tutorial debt. Aligns with the PROJECT.md decision to start with empty ring |

---

### Anti-Features (Commonly Requested, Often Problematic)

Features that seem beneficial but would undermine the cozy loop or create unacceptable scope risk for v1.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Real-time resource depletion / maintenance costs | "Depth" -- players expect management games to have upkeep | Introduces punishment loop. Citizens leaving, power failures, debt -- all break cozy promise. Aven Colony's elections mechanic is frequently cited as stressful | Passive income only. Economy is a pacing tool, not a threat mechanism |
| Multiple resource types (power, oxygen, water) | Aven Colony, Space Station Alpha model this. Feels thematic for a space station | Complexity without coziness payoff. Every resource type is another meter to watch, another way to fail | Single credit currency. Life Support rooms exist as *aesthetic/purpose categories* but don't gate a separate oxygen meter in v1 |
| Combat / threat events (asteroids, pirates) | "Keeps the game interesting long-term" | Directly contradicts the cozy promise. Players choosing a cozy space builder are opting OUT of threat management | Random celestial events (comet, nebula) are the positive-only equivalent -- deferred to v1.x |
| Complex citizen scheduling and pathfinding | "Realism" -- citizens should have full daily routines, work shifts, queuing | Before We Leave had complex colonist routing; it was cited as a source of frustration when it broke | Citizens walk the walkway, visit rooms when nearby. Simplified rule-based "attraction" is enough for v1 |
| Adjacency bonuses (explicit numbers) | Islanders uses proximity bonuses heavily. Players who know Islanders may request this | Adds optimization pressure -- players start min-maxing placement instead of building what feels right. Cozy builders (Tiny Glade, Townscaper) are explicitly non-optimal | Optional later feature. For v1: rooms work the same regardless of neighbors. Simple adjacency preference (citizens prefer rooms near their housing) is emotional, not numerical |
| Multiplayer / shared stations | "Social" -- building with friends is cozy | Scope explosion. Netcode, conflict resolution, session management. Townscaper has a solo-only design that works perfectly. Cozy builders are fundamentally solo experiences | Stay solo. Sharing screenshots and letting others view your station design is the social layer |
| Save slots / cloud save complexity | "Quality of life" | Not anti-feature per se, but scope risk for v1. Autosave-only keeps initial scope lean | Single autosave for v1. Save slots are v1.x |
| In-depth citizen relationship graphs / romance | Stardew Valley envy -- players may want deep NPC relationship trees | Heavy content creation burden. Spiritfarer succeeded with deep NPC stories but as a team investment, not a mechanic template | Named citizens with individual wishes creates perceived personality without requiring authored relationship arcs in v1 |
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
    (vertical pan only needed when multi-ring arrives -- deferred to v2+)

[Citizens Visiting Rooms] ──conflicts with complexity of──> [Full Daily Scheduling / Pathfinding]
    (spatial attraction model chosen instead -- see anti-features)
```

### Dependency Notes

- **Credit Economy requires being designed before Room Placement:** Room costs and income rates must be balanced before rooms are playable. Under-designed economy = either trivially easy (no pacing) or punishing (not cozy).
- **Citizen Wish Loop requires both Named Citizens and Room Diversity:** A wish for a room type only makes sense if that room type either exists (build more) or can be unlocked. Wishes targeting unavailable room types need to be gated by blueprint unlock state.
- **Blueprint Unlocks require Happiness Tracking:** Happiness is the unlock key. Happiness tracking must be designed and observable before unlocks feel meaningful.
- **Ambient Aliveness enhances Citizens Walking Walkway:** The walking loop is the minimum. Room entry animations, idle behaviors inside rooms, and reaction to wish fulfillment all enhance it -- but are additive, not prerequisite.
- **Demolish / Rearrange has no prerequisite:** Can be built alongside placement. Should ship with placement to eliminate any punishing feeling in early testing.

---

## MVP Definition

### Launch With (v1 -- Single Ring)

Minimum viable to prove the core build-wish-grow loop on one ring.

- [x] **Segment grid + room placement (1-3 segment sizes)** -- Without this, nothing else exists. The ring is the game.
- [x] **Credit economy (passive income + room costs + partial demolish refund)** -- Pacing requires economics. Keep it simple: 1 currency, no debt possible.
- [x] **5 room categories with visually distinct placeholder art** -- Players must be able to read the ring at a glance.
- [x] **Named citizens that walk the walkway and enter rooms** -- Emotional core. Static citizens kill the cozy feel.
- [x] **Citizen wish system with speech bubbles** -- The defining differentiator. Must ship with v1 or the game has no identity.
- [x] **Happiness tracking driving citizen arrival + blueprint unlocks** -- The growth half of build-wish-grow. Must have at least 2-3 unlock moments.
- [x] **Satisfying placement audio + visual feedback** -- Non-negotiable table stakes for the genre. This is feel, not feature.
- [x] **Orbiting 3D camera with zoom** -- Playable without this; shippable without this. But the diorama fantasy requires it.
- [x] **Demolish rooms with partial refund** -- Prevents any punishing trap-states in early layout experimentation.

### Add After Validation (v1.1 -- Happiness v2, COMPLETE)

- [x] **Lifetime wish counter replacing percentage display** -- The monotonic progress number. Always increases. Blueprint unlocks keyed to this.
- [x] **Station mood with decay toward rising baseline** -- The fluctuating state. Rises on wish fulfillment, settles to warmth baseline when idle.
- [x] **Five named mood tiers (Quiet/Cozy/Lively/Vibrant/Radiant)** -- Replaces raw float as the player-facing state. Named tiers drive citizen arrivals and economy multiplier.
- [x] **HUD update: wish counter + tier label with tier color** -- Always-visible. Tier label color changes per tier.
- [x] **Floating text on tier change** -- Auto-fades ~2 seconds. Non-blocking. "Station mood: Lively."
- [x] **v1 save migration** -- One-way. Estimate lifetime from old float; set initial mood to baseline.

### v1.2 Housing System (CURRENT MILESTONE)

- [ ] **HousingManager autoload** -- Singleton owning citizen-to-room assignment map.
- [ ] **Automatic home assignment** -- Even-spread, fewest-occupants-first.
- [ ] **Reassignment on demolish** -- Displaced citizens relocate or become unhoused.
- [ ] **Assign unhoused on new build** -- Oldest citizens first.
- [ ] **Return-home behavior with Zzz floater** -- Periodic cycle, visual distinction from visits.
- [ ] **Home line in CitizenInfoPanel** -- Read-back mechanism for player.
- [ ] **HousingConfig resource** -- Tunable timing constants.
- [ ] **Save/load housing assignments** -- Backward-compatible with v2.
- [ ] **Size-scaled capacity** -- Base + (segments - 1).
- [ ] **Room tooltip shows residents** -- P2, implement if time allows.

### Add After v1.2 (v1.x)

Features to add once housing and core loop are both confirmed.

- [ ] **"About to tier up" visual hint** -- Subtle pulse when mood is within ~1.0 unit of the next tier boundary. Creates anticipation without showing numbers.
- [ ] **Persistent wish board / notification panel** -- Supplement speech bubbles with a scannable list. Add once citizens number > 10 and bubbles become noise.
- [ ] **Room-entry animations + citizen reactions to wish fulfillment** -- Deepens the emotional payoff of fulfilling a wish.
- [ ] **Random positive events (celestial, visitor, milestone)** -- Adds variety without threatening safety.
- [ ] **Day/night ambient lighting cycle** -- Cosmetic. Adds warmth and rhythm, zero mechanical weight.
- [ ] **Additional blueprint milestones** -- Cosmetic unlocks at wish 30, 50, 100. Extends the sense of progression for long sessions.
- [ ] **Save slots** -- Single autosave sufficient for early access.
- [ ] **Tier change notification** -- Deferred from v1.1.
- [ ] **Citizen personality traits** -- Adds depth, deferred to after core loop proves out.

### Future Consideration (v2+)

Features to defer until product-market fit is established.

- [ ] **Multi-ring vertical expansion** -- The architecture should support it, but do not build it for v1. One ring must stand on its own.
- [ ] **Citizen personality traits and daily routines** -- Adds depth and replay, but is a content/design investment.
- [ ] **Citizen relationships and shared wishes** -- Spiritfarer-style emotional depth. Requires player attachment to individual citizens first.
- [ ] **Procedural room interiors** -- High effort, high delight. Deferred correctly.
- [ ] **Adjacency bonuses (as optional optimization layer)** -- Can be added without breaking the non-punishing design if framed as bonus, not penalty for absence.

---

## Feature Prioritization Matrix (v1.1 Scope -- COMPLETE)

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| lifetimeHappiness integer counter | HIGH | Very Low | P1 |
| Blueprint unlocks keyed to wish count | HIGH | Very Low | P1 |
| Mood float with gain on wish event | HIGH | Low | P1 |
| Decay toward rising baseline | HIGH | Low | P1 |
| MoodTier enum with 5 states | HIGH | Very Low | P1 |
| CitizenManager: tier to arrival scale | HIGH | Low | P1 |
| EconomyManager: tier to income multiplier | MEDIUM | Low | P1 |
| HUD: wish counter with pulse | HIGH | Low | P1 |
| HUD: tier label with tier color | HIGH | Low | P1 |
| Floating text on tier change | MEDIUM | Low | P1 |
| v1 save migration | HIGH (correctness) | Low | P1 |
| "About to tier up" pulse | MEDIUM | Medium | P2 |
| Additional unlock milestones (30, 50, 100) | LOW | Very Low | P2 |

---

## Competitor Feature Analysis

| Feature | Townscaper | Islanders | Before We Leave | Aven Colony | Spiritfarer | Orbital Rings v1.2 Approach |
|---------|------------|-----------|-----------------|-------------|-------------|------------------------|
| Fail state | None | Soft (score-based, can restart) | None (no combat) | Hard (voted out) | None | None -- wishes linger harmlessly |
| Economy | None | None (score only) | Single resource (food/water) | Multi-resource + electricity | None | Single currency (credits) |
| Citizens/NPCs | None | None | "Peeps" (anonymous aggregate) | Colonists (aggregate mood) | Named spirits with individual moods | Named individual citizens with wishes and homes |
| Housing system | None | None | Per-peep house auto-assign | Residential zones (aggregate) | Per-spirit cabin (manual) | Auto-assign, even-spread, no management burden |
| Home behavior | None | None | Peeps walk between buildings | Colonists commute | Spirits wander deck | Citizens return home periodically with "Zzz" floater |
| Unhoused handling | None | None | Peeps unhappy without house | Homeless = problem | Spirits always on boat | No penalty. Walk/wish normally. Skip home cycle only |
| Wish/request system | None | None | Needs meters | Morale factors menu | Per-spirit mood with influences | Individual speech bubbles |
| Happiness display | None | Score | None explicit | Morale % | Per-spirit face-scale | Dual: lifetime wishes + 5 mood tiers |
| Housing info display | None | None | None | Population overlay | Cabin assignment screen | Citizen info panel home line + room tooltip residents |
| Space setting | No | No | No (planetary) | Yes (alien planet) | No | Yes -- cozy, not sim-heavy |

**Key observation:** No competitor combines (1) named individual citizens with wishes and homes, (2) fully automatic zero-management housing assignment, (3) a cozy no-punishment loop where unhoused citizens are not penalized, (4) visible return-home behavior as ambient aliveness, and (5) a space setting with soft-3D aesthetics. Spiritfarer is closest on named-entity housing but requires manual cabin assignment. Banished has automatic assignment but punishes homelessness. Orbital Rings occupies a unique point: automatic, visible, and gentle.

---

## Sources

### v1.2 Housing Research
- [Housing - Banished Wiki](https://banished-wiki.com/wiki/Housing) -- MEDIUM confidence (community wiki, cross-referenced with Steam discussions)
- [Assign or Unassign A House/Citizen -- Banished Steam Discussions](https://steamcommunity.com/app/242920/discussions/0/3034850411002338653/) -- MEDIUM confidence (community forum, multiple consistent reports)
- [Optimal house-job distances -- Banished Steam Discussions](https://steamcommunity.com/app/242920/discussions/0/1456202492178239599/) -- MEDIUM confidence (community analysis)
- [Sleep - Dwarf Fortress Wiki](https://dwarffortresswiki.org/index.php/Sleep) -- HIGH confidence (community wiki, developer-maintained)
- [Rest - RimWorld Wiki](https://rimworldwiki.com/wiki/Rest) -- HIGH confidence (official-adjacent wiki)
- [Modding:Schedule data - Stardew Valley Wiki](https://stardewvalleywiki.com/Modding:Schedule_data) -- HIGH confidence (community wiki, developer-supported)
- [NPC Creation: Schedules - LemurKat](https://lemurkat.wordpress.com/2020/10/10/npc-creation-schedules/) -- LOW confidence (dev blog analysis)
- [Steam Community Guide: Space Haven Basics](https://steamcommunity.com/sharedfiles/filedetails/?id=2105271294) -- MEDIUM confidence (community guide)
- [Oxygen Not Included Wiki](https://oxygennotincluded.wiki.gg/) -- HIGH confidence (official wiki)
- [A Look At the City Builder Genre - Game Developer](https://www.gamedeveloper.com/design/a-look-at-the-city-builder-genre) -- HIGH confidence (industry publication)
- [Pile Up! Game Review - THE MAGIC RAIN](https://themagicrain.com/2025/08/pile-up-is-a-surprisingly-realistic-cozy-city-builder-game-review/) -- LOW confidence (game review)
- [Synergy - Cozy City Builder on Steam](https://store.steampowered.com/app/1989070/Synergy__Cozy_City_Builder/) -- HIGH confidence (primary source)

### v1.1 Happiness Research (preserved)
- [Mood | Spiritfarer Wiki | Fandom](https://spiritfarer.fandom.com/wiki/Mood) -- MEDIUM confidence (community wiki, cross-referenced with gameplay discussions)
- [Spiritfarer Steam Community -- mood tier discussions](https://steamcommunity.com/app/972660/discussions/0/3198118348339745840/) -- LOW confidence (community forum, multiple consistent reports)
- [Happiness - Kingdoms and Castles Fandom Wiki](https://kingdomsandcastles.fandom.com/wiki/Happiness) -- MEDIUM confidence (community wiki, consistent with gameplay)
- [Animal Information Panel | Planet Zoo Wiki](https://planetzoo.fandom.com/wiki/Animal_Information_Panel) -- MEDIUM confidence (community wiki)
- [Mental break - RimWorld Wiki](https://rimworldwiki.com/wiki/Mental_break) -- HIGH confidence (official-adjacent wiki, developer-maintained)
- [Mood - RimWorld Wiki](https://rimworldwiki.com/wiki/Mood) -- HIGH confidence (official-adjacent wiki)
- [Designing for Coziness - Game Developer](https://www.gamedeveloper.com/design/designing-for-coziness) -- HIGH confidence (industry publication, Kitfox Games designer article)
- [Designing for Coziness - Kitfox Medium](https://medium.com/kitfox-games/designing-for-coziness-d33d2519a59e) -- HIGH confidence (developer blog, same authorship)
- [Cozy Games - Lostgarden](https://lostgarden.com/2018/01/24/cozy-games/) -- HIGH confidence (foundational genre theory, widely cited)
- [Incremental game - Wikipedia](https://en.wikipedia.org/wiki/Incremental_game) -- MEDIUM confidence (Wikipedia, covers prestige/permanent progress patterns)
- [Before We Leave on Steam](https://store.steampowered.com/app/1073910/Before_We_Leave/) -- HIGH confidence (primary source)
- [Aven Colony on Steam](https://store.steampowered.com/app/484900/Aven_Colony/) -- HIGH confidence (primary source)
- [Tiny Glade on Steam](https://store.steampowered.com/app/2198150/Tiny_Glade) -- HIGH confidence (primary source)

---

*Feature research for: Cozy space station builder / management game (Orbital Rings)*
*v1.0 research: 2026-03-02 | v1.1 Happiness v2 addendum: 2026-03-04 | v1.2 Housing addendum: 2026-03-05*
