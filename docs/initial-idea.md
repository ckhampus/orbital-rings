# Orbital Rings — Game Design Pitch

## Concept

A cozy space station builder where players construct a modular station one ring at a time. The station is viewed top-down (or slightly isometric). Each ring is divided into segments where the player places rooms to attract citizens and fulfill their wishes. There is no fail state — the station always grows, just faster or slower depending on how well the player responds to citizen desires.

## Core Structure: The Ring

Each ring consists of:

- **12 outer segments** — the exterior arc of the ring
- **12 inner segments** — the interior arc of the ring
- **A walkway** — a circular corridor running between the inner and outer segments, used by citizens to move around

Segments are numbered 1–12 (like a clock face). Outer and inner segments at the same position share a wall but are independently buildable.

### Room Placement

- Rooms are placed into either the outer or inner segment row.
- A room occupies **1 to 3 adjacent segments** along the ring. The player chooses the size during placement.
- Rooms cannot span radially (i.e., a single room cannot occupy both an inner and outer segment).
- Larger rooms are more effective but limit layout flexibility.

### Ring Completion & Expansion

- When all 24 segments (12 outer + 12 inner) of the current ring are occupied, the player unlocks the ability to **add a new ring** stacked on top.
- New rings connect to lower rings via elevator shafts or access tubes, extending the walkway network vertically.
- Each new ring follows the same 12+12 structure but may unlock new room types or citizen tiers.

## Room Types (Starter Set)

Rooms fall into broad categories. Specific rooms within each category can be introduced gradually:

| Category | Example Rooms | Purpose |
|----------|--------------|---------|
| **Housing** | Quarters, Suite, Dormitory | Citizen capacity — more housing attracts more people |
| **Life Support** | Garden, Oxygen Farm, Water Recycler | Passive background systems — required thresholds to support population |
| **Work** | Workshop, Lab, Command Center | Generate resources or unlock new blueprints |
| **Comfort** | Café, Lounge, Observatory | Fulfill citizen wishes and raise happiness |
| **Utility** | Storage, Power Node, Comm Array | Support infrastructure — boost efficiency of adjacent rooms |

Room size (1–3 segments) affects capacity, output, or effectiveness. A 3-segment Garden produces more than a 1-segment Garden, but takes up more of the limited ring space.

## Procedural Room Interiors

No two rooms should look the same, even if they share a type. When a room is placed, its interior is procedurally generated:

- **Furniture layout** — Tables, chairs, beds, equipment, and decorations are placed from a pool of valid items for that room type, with randomized positions and orientations within the room's footprint.
- **Detail variation** — Small props and clutter (plants, mugs, screens, personal items) are scattered semi-randomly to give each room a lived-in feel.
- **Color and material variation** — Subtle differences in upholstery colors, wall panel tints, or surface materials so that two Cafés feel distinct at a glance.
- **Size-aware generation** — A 3-segment room isn't just a stretched 1-segment room. Larger rooms should generate meaningfully different layouts with more furniture clusters, open space, or unique features that only appear at that size.

The goal is that the player's station feels handcrafted and personal even though they only chose the room type and placement — the interior details emerge on their own.

## Citizens & The Wish System

Citizens are the heart of the game. They arrive, live in the station, and express **wishes** — gentle requests that guide the player's building choices.

### How Wishes Work

- Citizens periodically surface wishes via speech bubbles or a wish board (e.g., *"I'd love a place to stargaze"* → build an Observatory).
- Fulfilling a wish grants **happiness** to that citizen and a small global happiness boost.
- Unfulfilled wishes simply linger — **nothing bad happens**. Citizens don't leave, get angry, or suffer. They just... keep wishing.
- High station happiness passively attracts new citizens and unlocks new room blueprints.

### Wish Categories

- **Social** — "I wish I had neighbors nearby" (housing adjacency)
- **Comfort** — "A café would be wonderful" (build a comfort room)
- **Curiosity** — "I'd love to see the stars" (Observatory, Lab)
- **Variety** — "The station could use something new" (build a room type not yet present)

### Progression Through Happiness

Happiness is the single soft currency that drives progression:

- **More citizens** arrive as happiness grows (no active recruitment needed).
- **New blueprints** unlock at happiness milestones.
- **Ring expansion** becomes available once a ring is full, but higher happiness makes new rings richer (more room options, citizen variety).

## Economy

The station runs on a single currency: **Credits**.

### Earning Credits

- **Citizens generate income passively** — each citizen living on the station produces a small, steady stream of credits over time. More citizens means more income.
- **Work rooms boost earnings** — citizens assigned to work rooms (Workshop, Lab, etc.) generate bonus credits on top of their base income.
- **Happiness multiplier** — higher overall station happiness slightly increases credit generation. This creates a gentle positive feedback loop: happy citizens → more credits → better rooms → happier citizens.

### Spending Credits

- **Building rooms costs credits** — each room type has a base cost, scaled by size (a 3-segment room costs more than a 1-segment room, but not 3x more — there's a slight discount for building big).
- **Demolishing refunds partially** — moving or demolishing a room returns a portion of its cost, keeping rearrangement low-risk but not completely free.
- **New rings have an expansion cost** — unlocking a new ring requires a one-time credit investment, creating a natural milestone moment.

### Design Intent

The economy is a gentle pacing mechanism, not a pressure system. The player should never feel broke for long — just occasionally need to wait a little or prioritize one room over another. There is no debt, no maintenance cost, and no way to lose money passively.

## Gameplay Loop

1. **Build** — Place rooms in available segments on the current ring.
2. **Observe** — Watch citizens move along the walkway, enter rooms, and express wishes.
3. **Respond** — Build or rearrange rooms to fulfill wishes.
4. **Grow** — As happiness rises, new citizens arrive, new rooms unlock.
5. **Expand** — Fill the ring, add a new one, repeat with more options.

## Tone & Feel

- **Cozy and unhurried** — no timers, no crises, no resource scarcity emergencies.
- **Gentle feedback** — the station hums along regardless; good decisions make it hum louder.
- **Visual warmth** — soft lighting, citizens with simple idle animations, ambient space backdrop.
- **Discovery-driven** — new room types and citizen personalities are the reward, not survival.

## Camera & Perspective

The player views the station from a fixed-tilt angle, orbiting around the center of the ring. The camera can:

- **Orbit horizontally** — rotate freely around the ring's central axis to view any side of the station.
- **Move vertically** — scroll up and down the stack of rings as the station grows taller.
- **Zoom** — zoom in to inspect individual rooms and their procedurally generated interiors, or zoom out to see the full station silhouette against the space backdrop.

The tilt angle is fixed to maintain the diorama feel and keep the UI predictable. The station should always feel like a little snow globe the player is peering into.

## Day/Night Cycle

The station has a visible day/night cycle that gives the game rhythm:

- **Lighting shifts** — during the station's "day," rooms are brightly lit from within, walkways are active, and the space backdrop is a deep blue. At "night," interior lights dim to warm glows, exterior windows emit soft light, and the backdrop darkens to show more stars.
- **Citizen behavior changes** — during the day, citizens work, socialize, and visit comfort rooms. At night, they return to their housing, and the walkways quiet down. Night owls or insomniacs might still wander.
- **Cosmetic, not mechanical** — the cycle sets mood and rhythm but doesn't gate gameplay. The player can build at any time of day. There are no "daytime-only" or "nighttime-only" actions.

## Citizen Personality System

Each citizen is a unique named character with a persistent identity:

- **Name and appearance** — procedurally generated, giving each citizen a distinct look.
- **Traits (2–3 per citizen)** — personality tags that influence their behavior and wishes. Examples: *Introvert* (prefers quiet rooms, avoids crowded areas), *Curious* (wishes for Labs and Observatories more often), *Social Butterfly* (happier near Cafés and Lounges), *Green Thumb* (drawn to Gardens).
- **Favorite rooms** — each citizen develops preferences over time based on which rooms they visit most. A citizen who frequents the Observatory might start wishing for a second one on a new ring.
- **Daily routines** — citizens follow simple schedules along the walkway: housing → work room → comfort room → housing. Their routes are visible, and the player can click on a citizen to see their current mood, traits, and active wishes.
- **Simple relationships** — citizens who spend time in the same rooms develop friendships. Friends may generate shared wishes (e.g., *"Alex and I would love a Lounge near our quarters"*). Relationships are lightweight — no conflict, no drama, just warmth.

## Random Events

Occasional low-stakes events add surprise and variety without threatening the player's station:

- **Visitors** — a traveling merchant or diplomat docks temporarily, offering a rare room blueprint or a cosmetic item for a room.
- **Celestial events** — a comet passes by, a nebula becomes visible, or a meteor shower lights up the backdrop. Citizens gather at Observatories and windows to watch. Happiness boost for the duration.
- **Citizen milestones** — a citizen's birthday, a friendship anniversary, or someone discovering a new hobby. Small notification with a tiny happiness reward.
- **Discovery** — a work room (Lab, Workshop) occasionally produces a small breakthrough: a new furniture variant for procedural generation, a cosmetic upgrade, or a hint toward a new room type.

Events are always positive or neutral — never crises, disasters, or threats. They're gentle surprises that reward the player for paying attention.

## Design Decisions

- **Room rearrangement** — Players can freely demolish and move rooms at any time. No penalties. This keeps the experience stress-free and encourages experimentation.
- **Adjacency bonuses** — Not in the initial version. This is a candidate for a future update once the core loop is solid.
- **Citizen individuality** — Citizens are unique named characters with persistent preferences. Each citizen has their own personality, name, and wish tendencies. This creates personal attachment and makes fulfilling wishes feel meaningful rather than mechanical.
- **Vertical ring interaction** — Rings do not directly affect each other. Elevators and escalators are placed within the walkway to connect rings vertically, but there are no cross-ring room synergies.
- **Win condition** — The game is open-ended sandbox. There is no defined end state. The player builds, grows, and tends to their station for as long as they enjoy it.
- **Art style** — Soft 3D. Rounded geometry, gentle lighting, pastel-leaning palette. Think cozy miniature diorama rather than hard sci-fi.
- **Target platform** — PC (keyboard + mouse). Initial distribution via itch.io.
- **Engine & language** — Godot (C#). The project should use Godot's 3D rendering pipeline, with C# for all gameplay logic, systems, and tooling.
