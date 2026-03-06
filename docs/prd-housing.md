# PRD: Housing System

**Status**: Draft
**Version**: 0.1
**Last updated**: 2026-03-05

## Problem Statement

Housing in Orbital Rings currently functions as a faceless capacity gate:
a global number that permits or blocks citizen arrivals. Citizens have no
concept of "home." This creates a disconnect — the player builds cozy
living spaces but no citizen actually *lives* in them. The relationship
between citizens and their housing is invisible.

## Goal

Give each citizen a home room they visibly return to, making housing feel
personal and alive — without adding management burden or breaking the
cozy, no-fail-state philosophy.

## Non-Goals

- Housing quality tiers or upgrades
- Player-managed room assignments (drag citizen to room)
- Negative consequences for unhoused citizens
- Citizen preferences or compatibility (roommate chemistry)
- Room interior customization or procedural generation

---

## Design

### 1. Home Assignment

Each citizen is assigned to exactly one Housing-category room (Bunk Pod
or Sky Loft). Assignment is **fully automatic** — the player never
manages it.

**On citizen arrival:**
1. Find all Housing rooms with available capacity (occupants < BaseCapacity).
2. Among those, pick the one with the **fewest current occupants**
   (spread citizens out evenly).
3. Ties broken randomly.
4. If no housing room has capacity, the citizen is **unhoused** (see §5).

**On housing room demolished:**
1. All citizens assigned to that room become unhoused.
2. Immediately attempt reassignment to other housing rooms with capacity.
3. Citizens that can't be reassigned remain unhoused until capacity opens.

**On new housing room built:**
1. Attempt to assign any currently unhoused citizens (oldest first).

### 2. Room Capacity

Housing rooms use their existing `BaseCapacity` field to determine max
occupants:

| Room       | BaseCapacity (1 seg) | BaseCapacity (2 seg) | BaseCapacity (3 seg) |
|------------|---------------------|---------------------|---------------------|
| Bunk Pod   | 2                   | 3                   | —                   |
| Sky Loft   | 4                   | 5                   | 6                   |

> **Open question**: Should capacity scale with segment count? Currently
> `BaseCapacity` is a fixed value on the RoomDefinition, not
> size-dependent. Options:
>
> A) **Fixed capacity** (current): A 1-segment Bunk Pod holds 2 citizens
>    just like a 2-segment one. Simpler, but larger rooms feel wasteful.
>
> B) **Size-scaled capacity**: `BaseCapacity + (segmentCount - 1)`.
>    A 2-segment Bunk Pod holds 3. Larger rooms feel justified.
>
> Recommendation: **B** — it gives the player a reason to build larger
> housing and matches the spatial intuition that bigger rooms hold more
> people.

### 3. Return-Home Behavior

Citizens periodically return to their home room. This is a new behavior
cycle layered alongside the existing visit system.

**Cycle:**
- Every **90–150 seconds**, a housed citizen initiates a return-home
  sequence.
- Uses the same animation pipeline as room visits: walk angularly to
  home segment, drift to room edge, fade out, wait inside, fade back in.
- **Rest duration**: 8–15 seconds (longer than regular visits to feel
  like sleeping/resting).
- During the rest, the citizen's wish timer is **paused** (they're
  resting, not thinking about wishes).

**Priority rules:**
- A return-home trip is **lower priority** than an active wish visit.
  If the citizen has an active wish and a wish-matching room is nearby,
  they pursue the wish first.
- If a citizen is mid-visit when the home timer fires, the home timer
  simply resets (no interruption).

**Visual distinction:**
- When entering their home room, a small **"Zzz"** floater appears
  briefly at the room's position (or above the citizen before they fade
  out), distinguishing home returns from regular visits.
- The "Zzz" is subtle — same style as the existing FloatingText but
  smaller and lighter colored.

### 4. Citizen Info Panel

The existing `CitizenInfoPanel` gains a **Home** line:

```
  Pip
  Body: Round
  Home: Bunk Pod (Outer 3)
  Wish: Wants to tinker
```

- If unhoused: `Home: —` (em dash, no drama)
- The location label uses the same segment naming as the tooltip
  (e.g., "Outer 3", "Inner 7")

### 5. Unhoused Citizens

When population exceeds housing capacity, some citizens will be unhoused.
Per the cozy philosophy, this carries **no mechanical penalty**:

- Unhoused citizens walk, visit rooms, generate wishes, and fulfill
  them identically to housed citizens.
- They simply don't perform the return-home cycle (no home to return to).
- Their info panel shows `Home: —`.
- No sad faces, no debuffs, no leaving.

The only consequence of insufficient housing remains the existing one:
**new citizens won't arrive** until capacity opens up.

### 6. Starter Citizens

The 5 starter citizens spawn before any rooms exist. They begin as
unhoused and are assigned homes as the player builds Housing rooms
(via the "new housing room built" reassignment trigger in §1).

---

## Data Model Changes

### CitizenNode (new fields)

```
int _homeSegmentIndex = -1     // flat segment index of home room (-1 = unhoused)
Timer _homeTimer               // periodic return-home cycle timer
```

### SaveData (new fields)

```
Dictionary<string, int> CitizenHomes  // citizenName → segment index (-1 if unhoused)
```

### HappinessManager / HousingManager

Housing assignment logic could live in:
- **HappinessManager** (where `_housingCapacity` already lives), or
- A new **HousingManager** autoload singleton

> **Open question**: Introduce a new `HousingManager` autoload, or
> extend `HappinessManager`?
>
> Recommendation: **New HousingManager** — housing assignment is a
> distinct concern from mood/happiness. Keeps each autoload focused.
> It subscribes to `RoomPlaced` / `RoomDemolished` events and manages
> the citizen↔room mapping.

---

## Edge Cases

| Scenario | Behavior |
|----------|----------|
| All housing demolished | All citizens become unhoused. No penalty. No arrivals. |
| Housing built then immediately demolished | Citizens assigned on build, displaced on demolish, reassigned if other housing exists. |
| Save/load with demolished home | Citizen's saved `homeSegmentIndex` won't match a placed room → treated as unhoused on load, reassignment attempted. |
| Citizen arriving with zero housing | Cannot happen — arrivals gated by `population < housingCapacity`. |
| Multiple housing rooms with equal vacancy | Random tiebreak. |

---

## UX Flow (Player Perspective)

1. **Game start**: 5 citizens walk the ring. No housing exists.
   PopulationDisplay shows `5/0` (over capacity).
2. **Player builds a Bunk Pod (1-seg)**: Capacity becomes 2. Two starter
   citizens are auto-assigned. They begin periodically returning to
   the Bunk Pod with "Zzz" floaters.
3. **Player builds a Sky Loft (2-seg)**: Capacity becomes 7. Remaining 3
   unhoused starters are assigned to the Sky Loft. PopulationDisplay
   shows `5/7`.
4. **Player clicks a citizen**: Info panel shows their home room
   and location.
5. **Player demolishes the Bunk Pod**: The 2 citizens who lived there
   are reassigned to the Sky Loft (if capacity allows) or become
   unhoused.
6. **Citizen count grows**: As wishes are fulfilled and mood rises, new
   citizens arrive and are auto-assigned to housing with vacancy.

---

## Open Questions

1. **Capacity scaling with room size** — Fixed or `base + (segments - 1)`?
   (See §2. Recommendation: size-scaled.)

2. **HousingManager vs extending HappinessManager** — New autoload or
   keep it together? (See §6. Recommendation: new autoload.)

3. **Roommate visibility** — Should the info panel or a room tooltip
   show who lives together? e.g., hovering a Bunk Pod shows
   "Residents: Pip, Nova". Could be a nice touch but adds UI scope.

4. **Home-return frequency tuning** — 90–150s is a starting guess.
   Should this be configurable in a `HousingConfig` resource like
   other tuning parameters?

5. **"Zzz" visual** — FloatingText reuse vs. a small Sprite3D badge
   (like wish badges)? FloatingText is simpler. Sprite3D is more
   visually consistent with the wish system.

---

## Implementation Sketch

This is not a plan — just a rough sense of scope.

- New `HousingManager` autoload (~150 LOC): assignment map, event
  handlers, reassignment logic.
- `CitizenNode` additions (~80 LOC): home timer, return-home tween
  sequence (mirrors existing visit code), "Zzz" floater.
- `CitizenInfoPanel` update (~10 LOC): home line display.
- `SaveManager` additions (~20 LOC): serialize/deserialize home map.
- `HousingConfig` resource (~15 LOC): timing constants.
- No new scenes or assets required (reuses existing systems).

Estimated touch points: 5–6 files. No architectural changes.
