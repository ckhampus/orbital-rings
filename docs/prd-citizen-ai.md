# PRD: Citizen AI & Day/Night Cycle

**Status**: Draft
**Version**: 0.1
**Last updated**: 2026-03-06

## Problem Statement

Citizens in Orbital Rings are currently random walkers with timers. They
pick a direction, wander the ring, and visit nearby rooms on a flat
20–40 second interval. Every citizen behaves identically — no personality,
no daily rhythm, no observable routine. The player has no reason to watch
a specific citizen because there's nothing to notice.

The original pitch envisioned citizens with traits, daily routines, and
favorite rooms. The station was meant to have a day/night cycle that
gives the game rhythm. None of this exists yet.

## Goal

Make citizens feel alive by giving them **observable daily routines**
shaped by **personal traits**, layered on a **visible day/night cycle**
that gives the station rhythm. The player should be able to watch a
citizen and think "oh, Kai always heads to the Lab in the morning"
without the game ever telling them that explicitly.

## Non-Goals

- Citizen relationships or friendships (future milestone)
- Citizen mood or individual happiness values
- Player-directed citizen behavior (assigning citizens to rooms)
- Complex needs simulation (hunger, sleep meters, etc.)
- Mechanical consequences for citizen behavior (no fail state)
- New room types or room-specific citizen interactions
- Citizen speech or dialogue

---

## Design

### 1. Station Clock

A global clock that divides station time into four periods. The clock
is **cosmetic and atmospheric** — no gameplay is gated by time of day.
The player can build, demolish, and interact freely at all times.

**Cycle length**: 8 minutes real-time for a full day (default).

| Period    | Default Duration | Default Real-Time |
|-----------|-----------------|-------------------|
| Morning   | 25%             | 2 min             |
| Day       | 25%             | 2 min             |
| Evening   | 25%             | 2 min             |
| Night     | 25%             | 2 min             |

All period durations are **configurable** via a `StationClockConfig`
resource (Inspector-tunable). The total cycle length and individual
period proportions can be adjusted independently for playtesting
without code changes. Start with equal quarters and tune from there.

**Time does not pause** when the game is paused or the player is in
a menu. Time only advances during active gameplay (using `_Process`
delta, not wall-clock time).

### 2. Day/Night Visuals

The cycle is communicated through lighting and atmosphere shifts. All
transitions are smooth (lerped over ~5 seconds at period boundaries).

| Period  | Ring Lighting | Backdrop | Room Windows |
|---------|--------------|----------|--------------|
| Morning | Warm white, brightening | Deep blue → lighter | Soft warm glow |
| Day     | Full bright, neutral white | Light blue-black | Neutral interior light |
| Evening | Amber/orange warmth | Deepening blue | Warm golden glow |
| Night   | Dim blue-gray | Dark, more stars visible | Soft dim glow, some off |

**Implementation approach:**

- **DirectionalLight3D** intensity and color animated per period
- **WorldEnvironment** ambient light and fog color shifted
- **Room windows** (existing room meshes): emissive material color/intensity
  shifted per period via a global shader parameter or per-material tween
- **Backdrop**: existing space backdrop adjusts star brightness and
  nebula tint

The visual treatment should be **cozy dim, not dramatic**. Think "warm
afternoon fading to soft evening," not "blazing sun to pitch darkness."
At night, the station dims but never goes dark — room windows provide
enough warm glow that the ring always feels inviting. Citizens should
always be clearly visible and readable at every time of day. The goal
is atmosphere, not realism.

### 3. Citizen Traits

Each citizen is assigned **2 traits** at creation from a weighted pool.
Traits are permanent and visible in the citizen info panel. No two
traits from the same category can be assigned to the same citizen.

| Trait        | Category   | Effect |
|--------------|------------|--------|
| Curious      | Interest   | +0.4 utility for Lab, Observatory |
| Social       | Interest   | +0.4 utility for Cafe, Lounge |
| Green Thumb  | Interest   | +0.4 utility for Garden, Oxygen Farm |
| Industrious  | Interest   | +0.4 utility for Workshop, Craft Lab |
| Night Owl    | Rhythm     | Schedule shifts: active during Evening/Night, rests during Morning |
| Early Bird   | Rhythm     | Schedule shifts: active during Morning/Day, rests during Evening |
| Homebody     | Rhythm     | +0.3 utility for returning home, rest duration ×1.5 |
| Wanderer     | Rhythm     | -0.2 utility for returning home, +0.2 for visiting any room |

**Trait assignment:**
- 1 Interest trait + 1 Rhythm trait per citizen
- Interest traits weighted equally (25% each)
- Rhythm traits: Night Owl 20%, Early Bird 20%, Homebody 30%, Wanderer 30%
- Starter citizens get traits on game start; saved/loaded with citizen data

**No trait is negative.** Traits bias behavior, they don't restrict it.
A Social citizen still visits Labs — just less often. A Night Owl still
walks during the day — just prefers evening activities.

### 4. Schedule Templates

Each time period defines **weighted activity pools** that the utility
scorer draws from. The schedule is a suggestion, not a mandate — utility
scoring can override it when other factors dominate.

**Default schedule** (no rhythm trait modifier):

| Period  | Activity Weights |
|---------|-----------------|
| Morning | Visit: 0.5, Walk: 0.3, Home: 0.2 |
| Day     | Visit: 0.6, Walk: 0.3, Home: 0.1 |
| Evening | Visit: 0.4, Walk: 0.2, Home: 0.4 |
| Night   | Home: 0.7, Walk: 0.2, Visit: 0.1 |

**Night Owl modifier** (applied additively, then renormalized):

| Period  | Modifier |
|---------|----------|
| Morning | Home: +0.3, Visit: -0.2 |
| Evening | Visit: +0.3, Home: -0.2 |
| Night   | Visit: +0.3, Home: -0.3 |

**Early Bird modifier**:

| Period  | Modifier |
|---------|----------|
| Morning | Visit: +0.3, Home: -0.2 |
| Evening | Home: +0.2, Visit: -0.1 |
| Night   | Home: +0.2, Visit: -0.1 |

The schedule determines *what kind* of activity the citizen wants to do.
The utility scorer determines *where specifically* they do it.

### 5. Utility Scoring

When a citizen decides to visit a room, all occupied rooms are scored
and the highest-scoring room is chosen (with a small random jitter to
prevent robotic determinism).

**Score formula for room R:**

```
score(R) = scheduleWeight
         + traitAffinity(R)
         - proximityPenalty(R)
         - recencyPenalty(R)
         + wishBonus(R)
         + jitter
```

| Factor | Value | Notes |
|--------|-------|-------|
| `scheduleWeight` | 0.0–1.0 | From schedule template for current period |
| `traitAffinity` | 0.0–0.4 | If room category matches citizen's Interest trait |
| `proximityPenalty` | 0.0–0.5 | Linear with angular distance. 0 at citizen's position, 0.5 at opposite side of ring |
| `recencyPenalty` | 0.3 | Applied if this was the citizen's last visited room |
| `wishBonus` | 0.5 | If room fulfills citizen's active wish |
| `jitter` | ±0.1 | Uniform random, prevents ties and adds variety |

**Decision flow:**

1. Schedule template is consulted for current period → activity weights
2. Weighted random selects activity type (Visit, Walk, Home)
3. If Visit: score all occupied rooms, pick highest
4. If Home: go home (uses existing housing return-home behavior)
5. If Walk: continue walking (do nothing, existing behavior)

**Decision frequency**: Citizens re-evaluate every **15–30 seconds**
(replacing the current flat 20–40 second visit timer). This single
timer replaces both the visit timer and the home timer from the
housing system.

### 6. State Machine

Citizens operate in one of these states at any time:

```
                    ┌──────────┐
          ┌────────►│ Walking  │◄────────┐
          │         └────┬─────┘         │
          │              │               │
          │         decision timer       │
          │              │               │
          │              ▼               │
          │      ┌──────────────┐        │
          │      │  Evaluating  │        │
          │      └──┬───┬───┬──┘        │
          │         │   │   │            │
          │   visit │   │   │ home       │
          │         │   │walk│           │
          │         ▼   │   ▼            │
          │   ┌─────────┐ ┌──────────┐  │
          └───┤Visiting │ │ Resting  ├──┘
              └─────────┘ └──────────┘
```

| State | Behavior | Duration | Exit |
|-------|----------|----------|------|
| **Walking** | Move along walkway (existing behavior) | Until decision timer fires | → Evaluating |
| **Evaluating** | Run utility scorer, pick action | Instant (single frame) | → Walking, Visiting, or Resting |
| **Visiting** | Walk to room, fade in/out, visit (existing tween sequence) | 4–8s inside room | → Walking |
| **Resting** | Walk to home room, fade in/out, rest with "Zzz" (existing housing behavior) | 8–15s inside room | → Walking |

The Evaluating state is invisible to the player — it happens in a
single frame between the timer firing and the next action starting.

**Visiting and Resting reuse the existing tween-based animation
pipeline** from `CitizenNode.StartVisit()`. The only difference is
which room is targeted and how long the citizen stays inside.

### 7. Integration with Existing Systems

**Wish system (unchanged):**
- Wish generation and fulfillment logic stays exactly as-is
- The utility scorer's `wishBonus` replaces the current
  `WishMatchDistanceMultiplier` — same intent, cleaner integration
- Wish nudge events still reset the decision timer for responsive
  feedback

**Housing system (simplified):**
- The separate home timer from the housing PRD is **replaced** by the
  unified decision timer + schedule weights
- Home assignment logic (HousingManager) is unchanged
- "Zzz" visual on home return is unchanged
- Unhoused citizens simply never get "Home" as a decision option

**Happiness system (unchanged):**
- Mood, tiers, and economy multipliers are unaffected
- Citizen AI is purely behavioral — it doesn't read or write mood values

**Save/load:**
- Citizen traits are saved alongside existing citizen data
- Station clock position is saved and restored
- Current state (Walking/Visiting/Resting) is NOT saved — citizens
  resume in Walking state on load, which is natural and avoids
  complex state serialization

---

## Data Model Changes

### CitizenData (new fields)

```
string InterestTrait     // "curious", "social", "green_thumb", "industrious"
string RhythmTrait       // "night_owl", "early_bird", "homebody", "wanderer"
```

### CitizenNode (modified fields)

```
// REMOVED: Timer _visitTimer (replaced by unified decision timer)
// ADDED:
Timer _decisionTimer           // 15-30s, replaces visit timer
CitizenState _currentState     // Walking, Evaluating, Visiting, Resting
int _lastVisitedSegment        // for recency penalty (-1 = none)
```

### New: StationClock (autoload singleton)

```
float _timeOfDay               // 0.0 to 1.0 (one full cycle)
StationPeriod CurrentPeriod    // Morning, Day, Evening, Night
event PeriodChanged            // fired on period transitions
StationClockConfig _config     // Inspector-tunable timing resource
```

### New: StationClockConfig (Resource)

```
float TotalCycleSeconds = 480  // 8 minutes default
float MorningFraction = 0.25   // proportion of cycle
float DayFraction = 0.25
float EveningFraction = 0.25
float NightFraction = 0.25     // fractions must sum to 1.0
```

### New: CitizenSchedule (static data)

```
// Schedule weights per period per activity type
// Trait modifiers per period
// Utility scoring weights and thresholds
```

### SavedGame (new fields)

```
float StationClockTime                    // 0.0–1.0
Dictionary<string, string[]> CitizenTraits  // name → [interest, rhythm]
```

---

## Edge Cases

| Scenario | Behavior |
|----------|----------|
| No rooms built (game start) | All decisions resolve to Walk. Citizens wander until first room is placed. |
| Only housing rooms built | Visit decisions target housing rooms (citizens can visit non-home housing). Home decisions work normally. |
| Citizen unhoused + Home decision | Falls through to Walk (no home to go to). |
| All rooms on opposite side of ring | Proximity penalty is high but not prohibitive. Citizens will still eventually visit — it just takes a high trait affinity or wish bonus to overcome distance. |
| Clock wraps during a visit | No interruption. Citizen finishes their visit, next decision uses the new period. |
| Save during Night, load during Morning | Clock restores to saved position. Citizens resume Walking and will make period-appropriate decisions on next timer fire. |
| Trait conflicts with wish | No conflict possible. Wish bonus (+0.5) always outweighs trait affinity (+0.4). Wishes remain the primary driver of behavior — traits add flavor. |

---

## UX Flow (Player Perspective)

1. **Game start**: 5 citizens walk the ring under bright station
   lighting. No rooms, no routines. The station clock ticks in the
   background but there's nothing to observe yet.

2. **First rooms built**: Citizens begin visiting rooms. No traits
   visible yet (player hasn't clicked anyone). Behavior looks similar
   to current — just with slightly more variety due to jitter.

3. **Evening arrives**: Lighting shifts to warm amber. Citizens with
   homes start drifting back to housing rooms. The walkway gets
   quieter. Player notices the rhythm for the first time.

4. **Night**: Station dims. Most citizens are resting. A Night Owl
   citizen is still out visiting the Lab. The walkway is nearly
   empty. Stars are brighter in the backdrop.

5. **Morning**: Lights come up. Citizens emerge from housing rooms.
   The walkway fills up again. The cycle repeats.

6. **Player clicks a citizen**: Info panel shows traits. "Kai —
   Curious, Night Owl." Player starts to understand why Kai is
   always at the Lab at night.

7. **Mid-game**: Player has 12+ citizens with varied traits. The
   station feels alive — different citizens prefer different rooms
   at different times. The Cafe is busy in the evening. The Lab has
   a late-night regular. Housing rooms empty out during the day.

---

## Citizen Info Panel (Updated)

```
  Kai
  Body: Tall
  Curious, Night Owl
  Home: Sky Loft (Outer 7)
  Wish: Wants to stargaze
```

Traits are displayed on a single line between Body and Home. No
icons — just the trait names in the station's UI font. This is the
only place traits are explicitly surfaced. Everything else is
inferred by observation.

---

## Room Tooltip (Updated)

When hovering a room during Day/Evening, the tooltip could show
current visitors:

```
  Cafe (Outer 3–4)
  Visitors: Pip, Nova
```

> **Open question**: Is visitor tracking in tooltips worth the
> implementation cost? It requires the room (or a manager) to know
> which citizens are currently inside. The existing
> `CitizenEnteredRoom` / `CitizenExitedRoom` events could support
> this, but it's new UI scope.

---

## Open Questions

1. ~~**Cycle length tuning**~~ — **Decided**: 8 minutes default, all
   period durations configurable via `StationClockConfig` resource.

2. ~~**Visual intensity**~~ — **Decided**: Cozy dim. Station never
   goes dark, always warm and inviting. Subtle atmospheric shift.

3. ~~**Clock UI**~~ — **Decided**: Small ambient icon (sun/moon) in
   HUD. Visible if you look for it, doesn't clutter the cozy HUD.

4. ~~**Trait count**~~ — **Decided**: 2 traits (1 Interest + 1 Rhythm).
   Can expand to 3 later (Social category for future relationships).

5. ~~**Room category mapping**~~ — **Decided**: Add a `RoomCategory`
   enum field to `RoomDefinition`. Utility scorer reads this field
   to match against Interest traits.

6. ~~**Wish nudge vs schedule**~~ — **Decided**: Wishes always
   override schedule weights. Wish nudge resets the decision timer
   regardless of current period. Core loop takes priority over
   atmospheric behavior.

---

## Cozy Promise

> **Citizens have lives, not needs.**
>
> Every citizen follows their own gentle rhythm — some are early risers,
> some are night owls, some can't stay away from the Lab. But nothing
> bad happens if a routine is disrupted. There are no unmet needs, no
> declining meters, no unhappy consequences. Citizens simply *prefer*
> certain patterns, and the player is rewarded with the quiet pleasure
> of noticing them.

---

## Implementation Sketch

This is not a plan — just a rough sense of scope.

- New `StationClock` autoload (~80 LOC): time tracking, period
  transitions, PeriodChanged event
- New `StationLighting` node (~120 LOC): responds to clock, tweens
  DirectionalLight3D + WorldEnvironment + room emissives
- New `CitizenSchedule` static class (~60 LOC): schedule templates,
  trait modifiers, weight lookup
- New `UtilityScorer` static class (~100 LOC): room scoring formula,
  jitter, decision resolution
- `CitizenData` additions (~5 LOC): two trait string fields
- `CitizenNode` modifications (~150 LOC): replace visit timer with
  decision timer, add state machine, integrate utility scorer. Remove
  manual visit selection logic (replaced by scorer)
- `CitizenManager` additions (~20 LOC): pass traits on spawn, trait
  generation
- `CitizenInfoPanel` update (~10 LOC): trait display line
- `SaveManager` additions (~15 LOC): clock time + trait persistence
- Room tooltip visitor display (~40 LOC): optional scope

Estimated new code: ~600 LOC. Estimated modified code: ~200 LOC.
Touch points: 8–10 files. One new autoload (StationClock). One new
scene node (StationLighting).

**Dependency**: This PRD assumes the Housing system (prd-housing.md)
is implemented first. The Resting state and Home decisions depend on
HousingManager and home assignment being in place.
