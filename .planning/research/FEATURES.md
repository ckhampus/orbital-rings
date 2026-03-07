# Feature Research

**Domain:** Citizen AI traits, utility-based decision making, day/night cycle, and schedule-driven behavior for a cozy space station builder
**Researched:** 2026-03-07
**Confidence:** HIGH -- Patterns verified across RimWorld, Dwarf Fortress, Stardew Valley, Animal Crossing, and Oxygen Not Included. Utility AI theory confirmed via Game AI Pro (Dave Mark), The Shaggy Dev, and colony sim prototype implementations. Architecture grounded in direct inspection of existing 11,380 LOC codebase (8 singletons, 10 room types, 12 wish templates, existing CitizenNode state logic).

---

## Context: What Exists and What's Changing

The current CitizenNode has implicit behavior embedded in flat timers:
- **Walking:** Continuous angle-based polar movement at random speed
- **Room visiting:** 20-40s timer fires, finds nearest occupied room within proximity threshold, plays drift-fade animation, checks wish fulfillment on exit
- **Home return:** 90-150s timer fires, walks to home segment, rests 8-15s with Zzz indicator
- **Wish generation:** 30-60s timer fires, random wish from WishBoard, badge displayed

v1.4 replaces this with observable, personality-driven behavior:
- A **state machine** (Walking/Evaluating/Visiting/Resting) replaces the overlapping timer/boolean soup
- **Traits** (1 Interest + 1 Rhythm) create visible behavioral differences between citizens
- **Utility scoring** makes room selection intelligent (affinity, proximity, recency, wish bonus)
- **Schedule templates** tie behavior to a station clock with four time periods
- A **day/night cycle** provides visual rhythm through lighting and atmosphere changes

### Existing Room Map (for trait-room affinity design)

| Room | Category | RoomId | Notes |
|------|----------|--------|-------|
| Bunk Pod | Housing (0) | bunk_pod | 1-2 seg, capacity 2 |
| Sky Loft | Housing (0) | sky_loft | 1-3 seg, capacity 4 |
| Air Recycler | LifeSupport (1) | air_recycler | 1-2 seg |
| Garden Nook | LifeSupport (1) | garden_nook | 1-3 seg |
| Workshop | Work (2) | workshop | 1-2 seg |
| Craft Lab | Work (2) | craft_lab | 1-3 seg |
| Star Lounge | Comfort (3) | star_lounge | 1-2 seg |
| Reading Nook | Comfort (3) | reading_nook | 1-3 seg |
| Storage Bay | Utility (4) | storage_bay | 1-2 seg |
| Comm Relay | Utility (4) | comm_relay | 1-3 seg |

### Existing Wish Categories -> Room Mappings

| Wish Category | Example Wishes | Fulfilling Rooms |
|---------------|----------------|------------------|
| Social (0) | hangout, stargaze, comm | star_lounge, garden_nook, comm_relay |
| Comfort (1) | reading, rest, loft | reading_nook, bunk_pod, sky_loft |
| Curiosity (2) | observe, tinker, craft | workshop, craft_lab, air_recycler, garden_nook |
| Variety (3) | garden, explore, relay | garden_nook, storage_bay, comm_relay |

---

## Feature Landscape

### Table Stakes (Players Expect These)

Features that define the "alive station" promise. Without these, the milestone fails to deliver on its stated goal of making citizens feel alive with observable daily routines.

| Feature | Why Expected | Complexity | Dependencies | Notes |
|---------|--------------|------------|--------------|-------|
| Station clock with four periods | Players of any sim expect a visible time progression. RimWorld, Stardew, ONI all have time periods that structure behavior. Without it, "daily routine" is meaningless | LOW | New ClockManager autoload (9th singleton) | Four periods (Morning/Day/Evening/Night) with configurable duration per period. Total cycle ~8-12 min real time. GameEvents.PeriodChanged event notifies all systems |
| Day/night lighting transitions | Visual atmosphere is the primary way players perceive time passing. Stardew Valley's tinting, Animal Crossing's gradual sky changes -- these are baseline expectations for any game claiming a day/night cycle | MEDIUM | ClockManager, WorldEnvironment, ProceduralSkyMaterial | Tween ambient_light_color, sky_top_color, sky_horizon_color, and DirectionalLight3D energy/color between period presets. The existing ProceduralSkyMaterial already supports all needed properties |
| Citizen traits visible in info panel | If citizens have traits, players must be able to see them. Animal Crossing shows personality type, Stardew shows liked gifts. Orbital Rings already has a CitizenInfoPanel -- traits must appear there | LOW | CitizenData extended with trait fields, CitizenInfoPanel UI update | Add Interest + Rhythm display below existing name/body/home fields. Use readable labels ("Tinkerer", "Early Bird") not enum values |
| Citizen state machine (Walking/Evaluating/Visiting/Resting) | The current CitizenNode has 5+ booleans (_isVisiting, _isAtHome, _walkingToHome) and overlapping timer guards. This is already fragile. Adding schedule-awareness to the current system would create unmaintainable spaghetti. A proper state machine is table stakes for correctness | MEDIUM | Replaces CitizenNode's current timer/boolean logic | Four states with explicit transitions. Each state owns its own enter/exit behavior. Eliminates the boolean soup and makes behavior observable/debuggable |
| Schedule templates with period-weighted activities | Citizens must do different things at different times. "Working during Day, socializing during Evening, resting at Night" is the minimum expectation for schedule-driven behavior. RimWorld's Work/Recreation/Sleep blocks, ONI's schedule grid, Stardew's time-based NPC locations all establish this | MEDIUM | ClockManager, citizen state machine, trait system | Template defines activity weights per period. Activities: Work, Relax, Socialize, Rest, Wander. Weights shift based on period (Day favors Work, Evening favors Socialize, Night favors Rest) |
| Utility scoring for room selection | Current system picks the nearest occupied room. That produces random-looking visits with no personality. Utility scoring makes citizen behavior readable: "The tinkerer heads to the workshop" is meaningful; "citizen #3 visits whatever is closest" is not | MEDIUM | Trait affinity scores, ClockManager for schedule context, room visit history (recency) | Score = (traitAffinity * affinityWeight) + (proximity * proxWeight) + (recency * recencyWeight) + (wishBonus * wishWeight). Normalized 0-1 per factor, multiplicative combination. Highest-scoring room wins with small random jitter for variety |
| Clock UI in HUD | Players need to know what time period it is. Sun/moon icon or labeled indicator. Without it, the day/night visual changes are confusing rather than informative | LOW | ClockManager | Icon + period label in corner of HUD. Existing MoodHUD pattern: Label node updated on PeriodChanged event |
| Save/load for clock and traits (v4 format) | If clock position and traits are not saved, players lose their station's time state and citizen identity on reload. Backward compatibility with v1/v2/v3 is mandatory per established pattern | MEDIUM | SaveManager extension, CitizenData trait fields, ClockManager state | Add ClockPosition (float 0-1), CitizenTrait fields to SavedCitizen. Version-gated restore: v1-v3 saves get random traits and clock at Morning start. Follow existing nullable pattern |

### Differentiators (What Makes Orbital Rings Special)

Features that create the "cozy observation" experience unique to this game. Not required for functional completeness, but they're what makes players smile.

| Feature | Value Proposition | Complexity | Dependencies | Notes |
|---------|-------------------|------------|--------------|-------|
| Trait-based behavioral personality (Interest + Rhythm) | Unlike RimWorld's 30+ traits that are hard to track, or Dwarf Fortress's invisible personality facets, Orbital Rings uses exactly 2 readable traits per citizen. Interest determines WHERE they prefer to go (Tinkerer -> Workshop/Craft Lab). Rhythm determines WHEN they're most active (Early Bird -> Morning/Day, Night Owl -> Evening/Night). This is legible at a glance and creates observable individuality | LOW | CitizenData extended, trait enum definitions | Interest categories: Tinkerer (Work rooms), Socializer (Comfort+social rooms), Naturalist (LifeSupport rooms), Explorer (Utility rooms). Rhythm categories: Early Bird (active morning/day), Night Owl (active evening/night), Steady (uniform). 2 traits = 12 combinations, enough variety for 5-20 citizens |
| Room window emissives responding to day/night | Windows on room segments that glow warm at night and dim during day. Creates the "cozy station in space" feeling -- seeing little lit windows from the camera is the visual payoff of the day/night system | MEDIUM | ClockManager, RoomVisual shader or material update | Emission intensity animated inverse to ambient light. Night = bright warm windows. Day = dim windows. The Environment already has glow_enabled=true so emission will bloom naturally |
| Visible routine patterns (player learns citizen habits) | After watching for a few cycles, players notice "Luna always visits the Workshop in the morning" and "Nova hangs out at the Star Lounge at night." This creates attachment without any narrative system. Animal Crossing's NPC schedules create the same effect | LOW | Utility scoring + trait system working together | This is an emergent property of trait affinity + schedule weighting, not a separate feature. But it must be tuned so patterns are visible and consistent, not drowned in randomness |
| Room tooltip showing current visitors | Hovering a room shows who's inside. Creates a "where is everyone?" discovery moment. Players checking on their citizens builds attachment | LOW | GameEvents.CitizenEnteredRoom/ExitedRoom already exist, SegmentTooltip UI | Track visitor names per segment. Show in tooltip below room name. Clear on exit. Events already exist -- this is pure UI work |
| Evaluating state with visible "thinking" indicator | When a citizen pauses to decide where to go next, a brief thought-bubble or "..." indicator appears. Makes the AI decision process legible to the player. Differentiates from "random wandering" that plagues most colony sims | LOW | Citizen state machine Evaluating state, small visual indicator | 0.5-1.5s evaluation pause between Walking and Visiting. During this time, utility scoring runs. Optional: tiny "..." label or subtle head-turn animation |

### Anti-Features (Commonly Requested, Often Problematic)

Features that seem natural for this milestone but would undermine the cozy philosophy, add disproportionate complexity, or conflict with existing design decisions.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Citizen needs (hunger, energy, fun) | RimWorld/ONI have needs bars. Seems like the obvious next step for citizen AI | Needs create fail states and management pressure. Citizens with depleting bars require the player to maintain supply chains. This directly contradicts the "no fail state" constraint and "cozy" philosophy. Dwarf Fortress's needs are famously opaque and stressful | Use trait-driven preferences instead of needs. Citizens prefer certain rooms but never suffer from unmet needs. Wishes already serve the "citizen wants something" role without punishment |
| Citizen mood penalties from unmet schedules | If a Night Owl is forced to be active during Day, penalize their mood | Mood penalties create optimization anxiety, which is explicitly anti-cozy (PROJECT.md: "raw mood float in player UI -- optimization anxiety is anti-cozy"). Players would feel obligated to min-max trait-schedule alignment | Trait affinity boosts the utility score for preferred rooms/times but never penalizes. A Night Owl visits rooms less frequently during Day but is never unhappy about it. Positive-only reinforcement |
| Player-configurable citizen schedules | RimWorld lets players assign Work/Sleep/Recreation per hour per pawn | Player-configurable schedules turn a cozy builder into a spreadsheet optimizer. The beauty of Orbital Rings is watching emergent behavior, not micromanaging it. Already in Out of Scope: "Player-managed room assignments -- fully automatic, no micromanagement" | Schedules are automatic templates assigned by Rhythm trait. The player observes, they don't control |
| Complex trait interactions (trait combos, trait conflicts) | Dwarf Fortress has 50+ personality facets that interact in complex ways | More than 2 traits per citizen makes the info panel unreadable and the behavior unpredictable. The cozy aesthetic requires legibility. Animal Crossing uses exactly 1 personality type per villager for good reason | Exactly 2 traits (Interest + Rhythm). No interactions between them. Interest affects WHERE, Rhythm affects WHEN. Orthogonal dimensions, zero emergent complexity |
| Time-of-day affecting economy (night shift bonuses) | Seems natural -- rooms producing more at certain times | Adds optimization pressure. Players would feel compelled to build rooms matching their citizens' active periods for maximum income. Breaks "build what feels right" ethos | Economy remains time-independent. Day/night affects visuals and citizen behavior patterns, not production or income |
| Realistic day/night with actual darkness | Space station in orbit should have realistic shadow transitions | Full darkness makes the station hard to see and stops the game feeling cozy. The station is in space -- there's no natural sun cycle. The "day/night" is station lighting atmosphere, not realistic orbital mechanics | Treat day/night as station ambient mood lighting. Night dims but never darkens. Think "cozy evening" not "pitch black." Minimum ambient energy at Night should keep all rooms visible |
| Sleep requirements tied to Housing | Citizens must sleep in their assigned home during Night period | Creates a constraint that punishes players who don't have enough housing. Unhoused citizens would be "stuck" unable to sleep. Currently unhoused citizens are handled gracefully with no penalty | Resting at home is more frequent during Night period (schedule weight) but not mandatory. Citizens without homes simply wander more at night. No punishment |
| Citizen relationships or social graphs | Citizens who visit the same room at the same time build friendships | Already in Out of Scope. Adds massive state to track, save/load complexity, and UI requirements. The 2-trait system plus wish system already creates enough personality without relationships | Defer to future milestone. The trait + schedule system lays groundwork for social features later without requiring them now |
| Fast-forward / time speed controls | Players want to speed up slow periods | Adds UI complexity and requires all tween durations and timer intervals to respect a time scale multiplier. Every existing timer (visit, wish, home) would need modification. Creates bugs when time scale changes mid-tween | Defer entirely. Tune the base cycle duration (8-12 min) to feel right at 1x speed. If the cycle feels too slow, shorten it globally rather than adding speed controls |

---

## Feature Dependencies

```
ClockManager (autoload singleton)
    |
    +---> PeriodChanged event
    |       |
    |       +---> Day/Night Lighting Transitions (WorldEnvironment, DirectionalLight3D tweens)
    |       |
    |       +---> Room Window Emissives (RoomVisual emission intensity)
    |       |
    |       +---> Clock UI (HUD indicator)
    |       |
    |       +---> Schedule Templates (period-weighted activity selection)
    |               |
    |               +---> Citizen State Machine
    |                       |
    |                       +---> Utility Scoring (room selection during Evaluating state)
    |                       |       |
    |                       |       +---> Trait Affinity Scores (Interest trait -> room category preference)
    |                       |       +---> Proximity Factor (angular distance on ring)
    |                       |       +---> Recency Factor (avoid re-visiting same room)
    |                       |       +---> Wish Bonus (existing wish-matching behavior preserved)
    |                       |
    |                       +---> Evaluating "thinking" indicator
    |
    +---> Save/Load v4 (clock position persisted)

Citizen Traits (Interest + Rhythm)
    |
    +---> CitizenData extension (new enum fields)
    |       |
    |       +---> Citizen Info Panel (trait display)
    |       +---> Save/Load v4 (traits persisted per citizen)
    |
    +---> Schedule Template Selection (Rhythm determines which template)
    |
    +---> Utility Scoring (Interest determines room affinity weights)

Room Tooltip Visitors
    |
    +---> CitizenEnteredRoom / CitizenExitedRoom events (already exist)
    +---> SegmentTooltip UI (already exists, needs visitor list)
```

### Dependency Notes

- **ClockManager must exist before anything else:** All schedule, lighting, and UI features depend on knowing the current period. This is the foundational building block.
- **Citizen state machine must replace timers before utility scoring:** Utility scoring runs during the Evaluating state. Without a state machine, there's no clean place to invoke it.
- **Traits are data-only until state machine + schedule exist:** Adding trait fields to CitizenData is trivial. The traits become meaningful only when the state machine uses them for utility scoring and schedule selection.
- **Day/night visuals are independent of citizen AI:** Lighting transitions depend only on ClockManager, not on traits or schedules. Can be built in parallel with citizen AI work.
- **Room tooltip visitors are low-hanging fruit:** Events already exist. This is pure UI work with no AI dependencies.
- **Save/load v4 should come last:** Requires all new fields (clock position, traits) to be finalized before defining the save format.

---

## MVP Definition

### Phase 1: Foundation (Clock + State Machine)

- [x] **ClockManager autoload** -- Four periods, configurable timing, PeriodChanged event. Everything else builds on this.
- [x] **Citizen state machine** -- Walking/Evaluating/Visiting/Resting states replacing boolean soup. Clean transition logic. Each state owns its own behavior.
- [x] **Clock UI** -- Sun/moon icon + period label in HUD. Immediate visual feedback that time exists.

### Phase 2: Citizen Intelligence (Traits + Utility + Schedules)

- [x] **Trait enums and assignment** -- Interest (Tinkerer/Socializer/Naturalist/Explorer) + Rhythm (EarlyBird/NightOwl/Steady) on CitizenData. Random assignment at creation.
- [x] **Schedule templates** -- Period-weighted activity pools. Three templates (one per Rhythm type). Template determines activity weights per period.
- [x] **Utility scoring** -- Multi-factor room selection in Evaluating state. TraitAffinity + Proximity + Recency + WishBonus. Replaces nearest-room selection.
- [x] **Trait display in info panel** -- Show Interest and Rhythm in CitizenInfoPanel.

### Phase 3: Visual Polish (Atmosphere + UI)

- [x] **Day/night lighting transitions** -- Tween Environment and DirectionalLight3D properties between period presets.
- [x] **Room window emissives** -- Emission intensity inversely correlated with ambient light.
- [x] **Room tooltip visitors** -- Show current visitor names on room hover.
- [x] **Evaluating state indicator** -- Brief "thinking" visual during room selection.

### Phase 4: Persistence

- [x] **Save/load v4** -- Clock position, citizen traits. Version-gated backward compatibility for v1-v3 saves.

### Defer to Later Milestones

- [ ] **Citizen relationships** -- The trait system creates a foundation but relationships are explicitly out of scope
- [ ] **Time speed controls** -- Tune base cycle speed instead
- [ ] **Additional trait types** -- 2 traits is sufficient for v1.4. More traits are future expansion
- [ ] **Period-specific room effects** -- Rooms behaving differently at different times adds complexity without proportional value
- [ ] **Tier change notifications** -- Already deferred in PROJECT.md

---

## Feature Prioritization Matrix

| Feature | Player Value | Implementation Cost | Priority | Notes |
|---------|------------|---------------------|----------|-------|
| Station clock (ClockManager) | HIGH | LOW | P1 | Foundational for all other features |
| Citizen state machine | HIGH | MEDIUM | P1 | Correctness prerequisite -- current boolean soup cannot absorb new behavior |
| Clock UI | MEDIUM | LOW | P1 | Immediate player feedback, pairs with clock implementation |
| Citizen traits (data + assignment) | HIGH | LOW | P1 | Data foundation for utility scoring and schedules |
| Day/night lighting transitions | HIGH | MEDIUM | P1 | Primary visual payoff of the entire milestone. Players will judge success by this |
| Schedule templates | HIGH | MEDIUM | P1 | Without schedules, traits are invisible. Schedules make routines observable |
| Utility scoring | HIGH | MEDIUM | P1 | Without this, citizens visit random rooms. Trait affinity is meaningless without scoring |
| Trait display in info panel | MEDIUM | LOW | P1 | Players must see traits to understand behavior patterns |
| Room window emissives | MEDIUM | LOW-MEDIUM | P2 | Visual polish. High impact per line of code but not functionally necessary |
| Room tooltip visitors | MEDIUM | LOW | P2 | Discovery feature. Events already exist, minimal effort |
| Evaluating state indicator | LOW-MEDIUM | LOW | P2 | Subtle polish. Makes AI legible but citizens already pause during evaluation |
| Save/load v4 | HIGH | MEDIUM | P1 | Must ship -- losing traits/clock on reload breaks persistence contract |

---

## Competitor Feature Analysis

| Feature | RimWorld | Dwarf Fortress | Stardew Valley | Animal Crossing | Orbital Rings Approach |
|---------|----------|----------------|----------------|-----------------|------------------------|
| **Trait count per citizen** | 1-3 traits + passions + backstory | 50+ personality facets + values + beliefs | 0 (NPCs have fixed scripts) | 1 personality type | **2 traits (Interest + Rhythm).** Legible like AC, meaningful like RimWorld, without DF's opacity |
| **Schedule system** | Player-configured 24h grid per pawn | Emergent from needs | Fixed per-NPC authored paths | Real-time clock, fixed per villager | **Automatic templates per Rhythm trait.** Player observes, never manages |
| **Room selection AI** | Work priority system + hauling AI | Need-driven + job assignment | N/A (NPCs don't choose) | N/A | **Utility scoring with 4 factors.** Closest to RimWorld's priority system but simpler and positive-only |
| **Day/night visual** | Light level changes, sleep pressure | No visual cycle | Gradual sky color + lighting tint | Real-time sky + seasonal | **Period-based atmosphere presets.** Tween between 4 mood-lighting states. Cozy, never dark |
| **Fail states from AI** | Starvation, mental breaks, death | Tantrum spirals, insanity | None | None | **None.** Positive-only trait effects. No needs, no penalties, no death |
| **Behavior legibility** | Need bars + activity log visible | Virtually unreadable | Predictable authored paths | Personality dialogue differences | **Observable via watching.** Trait affinity makes patterns visible after 1-2 cycles |

### Key Insight from Competitor Analysis

The cozy games (Animal Crossing, Stardew) and the colony sims (RimWorld, DF) solve the same problem differently:

- **Colony sims** give players control (schedules, priorities, job assignment) and punish mistakes (needs, death, mood breaks). This creates engagement through management stress.
- **Cozy games** give players observation (watch NPCs, learn their habits) and reward attention (noticing a villager's schedule feels like discovery). No punishment for ignoring it.

Orbital Rings should be firmly in the cozy camp. The trait + utility + schedule system creates emergent behavior that players discover by watching, not a management system they must optimize. The key design constraint: **traits always help, never hurt. Schedules always suggest, never force.**

---

## Detailed Feature Specifications

### Citizen Trait Design

**Interest Trait** (1 per citizen -- determines WHERE they prefer to go):

| Interest | Preferred Room Categories | Specific Affinity Rooms | Flavor |
|----------|--------------------------|-------------------------|--------|
| Tinkerer | Work (2) | workshop, craft_lab | "Always has a project going" |
| Socializer | Comfort (3) | star_lounge, reading_nook | "Happiest in a crowd" |
| Naturalist | LifeSupport (1) | garden_nook, air_recycler | "Finds peace in growing things" |
| Explorer | Utility (4) | storage_bay, comm_relay | "Curious about everything" |

**Rhythm Trait** (1 per citizen -- determines WHEN they're most active):

| Rhythm | Morning Weight | Day Weight | Evening Weight | Night Weight | Flavor |
|--------|---------------|------------|----------------|--------------|--------|
| Early Bird | HIGH activity | HIGH activity | MEDIUM activity | LOW activity (prefers rest) | "Up with the lights" |
| Night Owl | LOW activity (prefers rest) | MEDIUM activity | HIGH activity | HIGH activity | "Comes alive after dark" |
| Steady | MEDIUM activity | MEDIUM activity | MEDIUM activity | MEDIUM activity | "Keeps a regular rhythm" |

**Why 2 traits, not more:**
- 4 Interest x 3 Rhythm = 12 combinations. With 5-20 citizens, most combinations appear, creating visible diversity.
- 2 traits fit on one line in the info panel. 3+ traits require a scrolling list or cramped layout.
- Each trait has a clear, orthogonal effect. Interest = spatial preference. Rhythm = temporal preference. No interaction complexity.

### Utility Scoring Formula

When a citizen enters the Evaluating state, score every non-Housing room on the station:

```
Score(room) = (A * traitAffinity) + (P * proximity) + (R * recency) + (W * wishBonus)
```

Where:
- **traitAffinity** (0.0 - 1.0): 1.0 if room category matches Interest trait's preferred category, 0.3 for adjacent categories, 0.0 for non-preferred. Creates visible preference without exclusion.
- **proximity** (0.0 - 1.0): Inverse angular distance normalized to ring circumference. `1.0 - (angularDist / PI)`. Nearby rooms score higher. Preserves the "citizens visit nearby rooms" behavior from current system.
- **recency** (0.0 - 1.0): 1.0 if never visited or visited long ago, decreasing toward 0.0 for recently visited rooms. Prevents repetitive visits to the same room. Simple timer-based decay per room.
- **wishBonus** (0.0 or 1.0): 1.0 if room fulfills active wish, 0.0 otherwise. Preserves existing wish-matching behavior. Binary, not weighted.

**Suggested weights (tunable via config resource):**
- A (affinity weight) = 0.35
- P (proximity weight) = 0.25
- R (recency weight) = 0.15
- W (wish weight) = 0.25

**Selection:** Pick highest-scoring room, with small random jitter (multiply final score by rand(0.9, 1.1)) to prevent robotic determinism.

**Why multiplicative jitter on final score rather than weighted random selection:**
Weighted random creates too much behavioral noise in a cozy game. Players need to be able to predict "the Tinkerer goes to the Workshop" after watching one cycle. Jitter introduces just enough variety that the Tinkerer occasionally visits the Garden Nook, which feels like discovery rather than randomness.

### Schedule Template Design

Three templates, one per Rhythm:

```
EarlyBird Template:
  Morning: { Work: 0.4, Wander: 0.3, Socialize: 0.2, Rest: 0.1 }
  Day:     { Work: 0.4, Socialize: 0.3, Wander: 0.2, Rest: 0.1 }
  Evening: { Socialize: 0.3, Wander: 0.3, Rest: 0.3, Work: 0.1 }
  Night:   { Rest: 0.6, Wander: 0.2, Socialize: 0.1, Work: 0.1 }

NightOwl Template:
  Morning: { Rest: 0.5, Wander: 0.3, Socialize: 0.1, Work: 0.1 }
  Day:     { Wander: 0.3, Socialize: 0.3, Work: 0.2, Rest: 0.2 }
  Evening: { Work: 0.4, Socialize: 0.3, Wander: 0.2, Rest: 0.1 }
  Night:   { Work: 0.3, Socialize: 0.3, Wander: 0.3, Rest: 0.1 }

Steady Template:
  Morning: { Wander: 0.3, Work: 0.3, Socialize: 0.2, Rest: 0.2 }
  Day:     { Work: 0.3, Socialize: 0.3, Wander: 0.2, Rest: 0.2 }
  Evening: { Socialize: 0.3, Wander: 0.3, Work: 0.2, Rest: 0.2 }
  Night:   { Rest: 0.4, Wander: 0.3, Socialize: 0.2, Work: 0.1 }
```

**Activity -> State Machine Mapping:**
- Work -> Evaluating (utility scores Work-category rooms higher)
- Socialize -> Evaluating (utility scores Comfort-category rooms higher)
- Wander -> Walking (continue walking the ring)
- Rest -> Resting (go home if housed, otherwise keep walking)

**How it works:** When the state machine finishes an action and needs the next one, it rolls against the current period's activity weights. The selected activity biases the utility scoring or directly selects a state.

### State Machine Design

```
         +----------+
         | Walking  |<---------+
         +----+-----+          |
              |                 |
    (schedule timer fires)      |
              |                 |
         +----v-----+          |
         |Evaluating|          |
         +----+-----+          |
              |                 |
     +--------+--------+       |
     |        |        |       |
     v        v        v       |
 +-------+ +------+ +-------+ |
 |Visiting| |Resting| |Walking| |
 +---+---+ +--+---+ +---+---+ |
     |         |         |     |
     +---------+---------+-----+
        (action complete)
```

**States:**
- **Walking:** Default state. Citizen walks the ring. After a random interval (tuned by Rhythm -- Early Birds have shorter intervals in morning), transitions to Evaluating.
- **Evaluating:** Brief pause (0.5-1.5s). Rolls schedule activity for current period. If Work/Socialize, runs utility scoring on rooms, transitions to Visiting. If Wander, returns to Walking. If Rest and housed, transitions to Resting. If Rest and unhoused, returns to Walking.
- **Visiting:** Walks to target room, drift-fade enter, waits inside, drift-fade exit. On completion, returns to Walking. Existing visit animation sequence preserved.
- **Resting:** Walks to home room, drift-fade enter, Zzz indicator, waits, drift-fade exit. On completion, returns to Walking. Existing home-return animation preserved.

**Key difference from current system:** Currently, visit and home-return are timer-initiated and independent. In the new system, the state machine is the single authority for what happens next. No competing timers.

### Day/Night Lighting Presets

Four atmosphere presets (all values tunable via a config resource):

| Property | Morning | Day | Evening | Night |
|----------|---------|-----|---------|-------|
| sky_top_color | (0.55, 0.65, 0.85) | (0.45, 0.55, 0.75) | (0.35, 0.30, 0.55) | (0.10, 0.08, 0.20) |
| sky_horizon_color | (0.95, 0.75, 0.55) | (0.85, 0.65, 0.50) | (0.90, 0.50, 0.35) | (0.20, 0.15, 0.25) |
| ambient_light_color | (1.0, 0.95, 0.85) | (1.0, 0.95, 0.90) | (0.95, 0.80, 0.70) | (0.60, 0.55, 0.75) |
| ambient_light_energy | 0.35 | 0.30 | 0.25 | 0.15 |
| DirectionalLight energy | 0.6 | 0.8 | 0.5 | 0.1 |
| DirectionalLight color | (1.0, 0.9, 0.7) | (1.0, 0.95, 0.9) | (0.95, 0.7, 0.5) | (0.4, 0.4, 0.7) |
| Room emission intensity | 0.3 | 0.1 | 0.6 | 1.0 |

**Design constraint:** Night ambient_light_energy never drops below 0.1. The station must always be visible. Warm room emission at night compensates for lower ambient, creating the "cozy windows in space" effect.

**Transition approach:** On PeriodChanged, create a Tween interpolating all properties from current values to target values over 3-5 seconds. Kill previous tween if period changes early. Godot's Tween system handles this cleanly.

---

## Sources

- [RimWorld Wiki: Traits](https://rimworldwiki.com/wiki/Traits) -- Trait categories, spectrum system, behavioral effects (HIGH confidence)
- [Dwarf Fortress Wiki: Personality trait](https://dwarffortresswiki.org/index.php/DF2014:Personality_trait) -- Facet system, behavioral influence, opacity concerns (HIGH confidence)
- [Stardew Valley Wiki: Schedule data](https://stardewcommunitywiki.com/Modding:Schedule_data) -- Time-based NPC scheduling format (HIGH confidence)
- [Animal Crossing Personality Types](https://www.ourmental.health/personality/animal-crossing-new-horizons-villager-personality-types-guide) -- 8 personality types, cozy design philosophy (MEDIUM confidence)
- [Game AI Pro Ch.9: Introduction to Utility Theory](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter09_An_Introduction_to_Utility_Theory.pdf) -- Utility scoring fundamentals, curve design (HIGH confidence)
- [The Shaggy Dev: Introduction to Utility AI](https://shaggydev.com/2023/04/19/utility-ai/) -- Scoring formulas, consideration normalization, bucketing, weighting (HIGH confidence)
- [Colony Sim AI Prototype](https://johncjensen.com/colonysim/) -- Goal-priority scoring with personality modifiers, threshold-based selection (MEDIUM confidence)
- [AI Decision-Making with Utility Scores](https://mcguirev10.com/2019/01/03/ai-decision-making-with-utility-scores-part-1.html) -- Multi-factor scoring combination patterns (MEDIUM confidence)
- [Seaotter Games: Setting a Mood with Day/Night Cycle](https://seaotter.games/blog/setting-a-mood-with-a-day-night-cycle) -- Cozy lighting design principles (MEDIUM confidence)
- [RimWorld Schedule Discussions](https://steamcommunity.com/app/294100/discussions/0/3365901687364934508/) -- Work/Recreation/Sleep period design, need thresholds (MEDIUM confidence)
- [ONI Schedule Strategies](https://forums.kleientertainment.com/forums/topic/95151-schedule-strategies/) -- Duplicant schedule block design (MEDIUM confidence)
- [Godot 4 Day/Night Cycle Tutorial](https://gamedevacademy.org/godot-day-night-cycle/) -- DirectionalLight3D + Environment transitions (MEDIUM confidence)
- Direct codebase inspection: CitizenNode.cs (1106 lines), CitizenManager.cs (422 lines), GameEvents.cs (343 lines), CitizenData.cs, RoomDefinition.cs, WishTemplate.cs, all 10 room .tres files, all 12 wish .tres files, DefaultEnvironment.tres

---
*Feature research for: Citizen AI traits, utility-based decision making, day/night cycle, and schedule-driven behavior*
*Researched: 2026-03-07*
