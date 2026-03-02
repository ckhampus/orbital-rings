# Architecture

**Analysis Date:** 2026-03-02

## Pattern Overview

**Overall:** Component-based modular architecture within Godot 4.6 (C# 12 / .NET 8)

**Key Characteristics:**
- Game divided into discrete systems (Ring, Room, Citizen, Economy, Wish)
- Each system encapsulates specific game domain logic
- Scene-based object hierarchy with script attachment (Godot pattern)
- No external API dependencies initially—pure single-player simulation
- Procedural generation for room interiors and citizen properties

## Layers

**Presentation Layer (Godot Scenes & UI):**
- Purpose: Render 3D game world, display UI, handle camera and input
- Location: Godot scenes (`.tscn` files, to be created in `res://scenes/`)
- Contains: Camera system, UI panels, visual effects, animation states
- Depends on: Core game systems (Ring, Room, Citizen managers)
- Used by: Player input, visual feedback loop

**Game Systems Layer:**
- Purpose: Core gameplay logic and state management
- Location: `res://src/systems/` (to be created)
- Contains: RingManager, RoomManager, CitizenManager, EconomyManager, WishSystem
- Depends on: Models and services
- Used by: Presentation layer, other systems

**Models & Data Layer:**
- Purpose: Data structures representing game entities
- Location: `res://src/models/`
- Contains: Ring, Room, Citizen, Wish, RoomBlueprint classes
- Depends on: Minimal—pure data structures
- Used by: Systems layer, presentation layer

**Services & Utilities Layer:**
- Purpose: Reusable algorithms and helper functions
- Location: `res://src/services/`, `res://src/utils/`
- Contains: ProceduralGenerator (room interiors), PathFinder (citizen navigation), RandomUtil, TimeManager
- Depends on: Models
- Used by: Systems layer

**Configuration & Data:**
- Purpose: Game balance, room definitions, citizen traits
- Location: `res://data/` (JSON or ScriptableObjects)
- Contains: RoomBlueprints, TraitDefinitions, EconomyConfig, WishTemplates
- Depends on: None
- Used by: All systems

## Data Flow

**Building a Room (Player Action):**

1. Player clicks placement UI and selects segment, size, room type
2. Input handler forwards to RoomManager.PlaceRoom()
3. RoomManager validates placement (segments available, cost available)
4. If valid: creates Room instance, generates interior via ProceduralGenerator
5. Room added to Ring's segment list, visual representation spawned in scene
6. Economy deducts credits from player account
7. Citizens notified of new room → may express new wishes
8. UI updates to show available balance and ring fullness

**Citizen Wish Fulfillment Loop:**

1. Citizen wanders walkway, enters rooms
2. CitizenManager tracks visit history and mood
3. If unmet wish exists (e.g., "need Observatory"), WishSystem marks it active
4. Wish displayed in UI as gentle suggestion
5. If player builds matching room type, WishSystem.CheckWish() triggers
6. Happiness awarded to citizen and globally
7. New citizens may arrive as global happiness increases
8. RoomManager unlocks new blueprints at happiness milestones

**Ring Expansion:**

1. RingManager detects all 24 segments of current ring are full
2. Player unlocks "Add Ring" button
3. Player spends credits to expand
4. New Ring instance created, stacked on previous ring
5. New elevator/connection visually created in walkway
6. Camera scrolling expanded to include new ring vertical position

**State Management:**

- **Game State (Singleton):** Persistent across scenes, holds current player progress
  - Current ring count, total happiness, credits balance
  - List of active citizens with their properties
  - History of placed rooms for undo/redo

- **Ring State:** Each ring maintains its own segment occupancy and room list

- **Citizen State:** Each citizen holds personal happiness, current location, active wishes, traits, relationships

## Key Abstractions

**Room:**
- Purpose: Represents a placed building occupying 1-3 consecutive segments
- Examples: `res://src/models/Room.cs`, `res://src/models/RoomBlueprint.cs`
- Pattern: Data class + SceneNode wrapper (model-view separation)
- Properties: Type, Size, SegmentRange, InteriorLayout, ProducedResources

**Ring:**
- Purpose: Container for 24 segments (12 outer + 12 inner), walkway, citizens
- Examples: `res://src/models/Ring.cs`
- Pattern: Collection manager with segment occupancy tracking
- Methods: CanPlaceRoom(), GetSegmentOccupants(), GetFullness()

**Citizen:**
- Purpose: Named NPC with personality, schedule, wishes, relationships
- Examples: `res://src/models/Citizen.cs`
- Pattern: Entity with trait composition (traits affect behavior and wishes)
- Properties: Name, Appearance, Traits[], Favorites[], Relationships[], CurrentMood

**Wish:**
- Purpose: A citizen's desire (goal for player to fulfill)
- Examples: `res://src/models/Wish.cs`
- Pattern: Event-driven—created, tracked, fulfilled
- Types: Social (housing), Comfort (rooms), Curiosity (specific rooms), Variety (new types)

**ProceduralGenerator:**
- Purpose: Creates unique interior layouts for rooms
- Examples: `res://src/services/ProceduralGenerator.cs`
- Pattern: Strategy pattern—different generators for each room category
- Output: Furniture placements, color variations, decoration scatter

## Entry Points

**Main Game Scene:**
- Location: `res://scenes/main.tscn` (to be created)
- Triggers: Game starts (Godot _Ready() lifecycle)
- Responsibilities: Initialize GameState singleton, load first ring, set up camera, display UI

**Input Handler:**
- Location: `res://src/input/InputManager.cs` (to be created)
- Triggers: Mouse clicks, camera orbit, zoom, vertical scroll
- Responsibilities: Translate input to system calls (place room, demolish, pan camera)

**Game Loop / TimeManager:**
- Location: `res://src/services/TimeManager.cs`
- Triggers: Every frame (_Process()) and periodic updates (_PhysicsProcess())
- Responsibilities: Advance citizen schedules, generate credits, check happiness thresholds, trigger events

**Scene Lifecycle:**
- Godot's _Ready(): Initialize systems
- Godot's _Process(delta): Update visuals, handle input
- Godot's _PhysicsProcess(delta): Advance game time, citizen movement, economy ticks

## Error Handling

**Strategy:** Defensive validation at system boundaries + informative player feedback

**Patterns:**

- **Placement Validation:** RoomManager.PlaceRoom() returns Result<Room, PlacementError> to gracefully reject invalid placements
  - Invalid: Insufficient space, insufficient credits, overlapping rooms
  - Feedback: Tooltip message shown, no state change

- **Economy Constraints:** EconomyManager.TrySpendCredits() returns bool, only deducts if sufficient balance
  - Prevents going negative
  - Player sees greyed-out "build" button if insufficient funds

- **Data Loading:** ConfigLoader wraps JSON parsing with try-catch, logs errors, provides sensible defaults
  - Ensures missing room blueprints don't crash game
  - Falls back to minimal viable room set

- **Citizen Pathing:** If route becomes invalid (room demolished), citizens reroute via PathFinder.Reroute()
  - Prevents citizens from getting stuck
  - Logged as minor event, no player-facing error

## Cross-Cutting Concerns

**Logging:**
- Framework: Godot's built-in GD.Print() and custom Logger wrapper
- Destination: Console (development) and optional file log (production)
- Pattern: Categorized by system (e.g., "[Ring]", "[Economy]", "[Wish]")
- Levels: Info (state changes), Warning (validation rejections), Error (crashes)

**Validation:**
- Responsibility: Each system validates inputs before state mutation
- Common checks: Range validation (0-24 segments), cost validation (credits > 0), existence checks (room exists in list)
- Pattern: Guard clauses at method entry, return early on failure

**Authentication:**
- Not applicable (single-player, no backend)

**Save/Load:**
- Pattern: Serialization of GameState to JSON
- Location: `res://src/services/SaveManager.cs` (to be created)
- Scope: Persists all rings, rooms, citizens, happiness, credits
- Trigger: Auto-save on every major state change + explicit save on demand

