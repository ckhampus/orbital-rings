# Codebase Structure

**Analysis Date:** 2026-03-02

## Directory Layout

```
res://                          # Godot project root
├── scenes/                      # Godot scene files (.tscn)
│   ├── main.tscn               # Main game scene
│   ├── ui/                      # UI panels and components
│   │   ├── HudPanel.tscn
│   │   ├── RingViewer.tscn
│   │   ├── PlacementUI.tscn
│   │   └── CitizenCard.tscn
│   ├── entities/                # Entity scene prefabs
│   │   ├── Room.tscn
│   │   ├── Citizen.tscn
│   │   └── Ring.tscn
│   └── camera/
│       └── CameraController.tscn
├── src/                         # C# source code
│   ├── systems/                 # Core gameplay systems
│   │   ├── RingManager.cs
│   │   ├── RoomManager.cs
│   │   ├── CitizenManager.cs
│   │   ├── EconomyManager.cs
│   │   └── WishSystem.cs
│   ├── models/                  # Data models and entities
│   │   ├── Ring.cs
│   │   ├── Room.cs
│   │   ├── RoomBlueprint.cs
│   │   ├── Citizen.cs
│   │   ├── CitizenTrait.cs
│   │   ├── Wish.cs
│   │   └── GameState.cs
│   ├── services/                # Reusable services and algorithms
│   │   ├── ProceduralGenerator.cs
│   │   ├── PathFinder.cs
│   │   ├── TimeManager.cs
│   │   ├── SaveManager.cs
│   │   ├── ConfigLoader.cs
│   │   └── RandomUtil.cs
│   ├── input/                   # Input handling
│   │   └── InputManager.cs
│   └── utils/                   # Utility helpers
│       ├── Logger.cs
│       ├── MathUtil.cs
│       └── Extensions.cs
├── data/                        # Configuration and balance data
│   ├── rooms.json               # Room blueprints and definitions
│   ├── traits.json              # Citizen trait definitions
│   ├── economy.json             # Economy balance parameters
│   ├── wishes.json              # Wish templates and distribution
│   └── ui_strings.json          # Localization strings (future)
├── assets/                      # Art assets
│   ├── models/                  # 3D models (.gltf, .obj)
│   ├── materials/               # Godot materials (.tres)
│   ├── textures/                # Texture files
│   ├── audio/                   # Sound effects and music
│   └── shaders/                 # Custom Godot shaders (.gdshader)
├── Orbital Rings.csproj         # C# project file
├── Orbital Rings.sln            # Visual Studio solution
├── project.godot                # Godot project configuration
└── icon.svg                     # Project icon
```

## Directory Purposes

**`res://scenes/`:**
- Purpose: All Godot scene files (node hierarchies, layout, connections)
- Contains: Main game scene, UI panels, entity templates, camera setup
- Key files: `main.tscn` (entry point), `ui/HudPanel.tscn` (player information), `entities/Room.tscn` (room visual template)

**`res://src/`:**
- Purpose: Root directory for all C# gameplay code
- Contains: Game systems, data models, services, utilities
- Organization: Subdirectories by responsibility (systems, models, services)

**`res://src/systems/`:**
- Purpose: Core gameplay system implementations
- Contains: RingManager (ring expansion logic), RoomManager (placement/demolition), CitizenManager (NPC simulation), EconomyManager (credit flow), WishSystem (desire tracking)
- Key files: `RingManager.cs` (creates rings, expands), `RoomManager.cs` (validates/places rooms)

**`res://src/models/`:**
- Purpose: Data structures representing game entities
- Contains: Plain data classes with properties, minimal logic
- Key files: `Ring.cs` (24 segments), `Room.cs` (placed building), `Citizen.cs` (named NPC), `GameState.cs` (persistent save data)

**`res://src/services/`:**
- Purpose: Reusable algorithms and helper services
- Contains: Procedural generation, pathfinding, time advancement, save/load, configuration
- Key files: `ProceduralGenerator.cs` (room interior creation), `TimeManager.cs` (game loop advancement)

**`res://src/input/`:**
- Purpose: Input capture and delegation to systems
- Contains: Mouse click handling, camera control, keyboard input mapping
- Key files: `InputManager.cs` (singleton coordinating all input)

**`res://src/utils/`:**
- Purpose: General-purpose utility functions
- Contains: Logging, math extensions, string utilities
- Key files: `Logger.cs` (debug output), `Extensions.cs` (C# extension methods)

**`res://data/`:**
- Purpose: Game balance configuration and static data
- Contains: Room definitions (cost, size, capacity), citizen traits, economy parameters, wish templates
- Format: JSON files (human-editable, hot-loadable)
- Key files: `rooms.json` (all room types), `traits.json` (personality modifiers), `economy.json` (credit rates)

**`res://assets/`:**
- Purpose: All visual, audio, and shader resources
- Contains: 3D models, materials, textures, sound effects, custom shaders
- Note: Directory structure mirrors asset type for clarity

## Key File Locations

**Entry Points:**
- `res://scenes/main.tscn`: Initial game scene (opened by Godot on startup)
- `res://src/systems/RingManager.cs`: Main system orchestrator (initialized in main scene)
- `res://Orbital Rings.csproj`: C# project manifest (defines .NET 8 target, root namespace)

**Configuration:**
- `res://project.godot`: Godot engine settings (version 4.6, physics engine, rendering mode)
- `res://data/rooms.json`: All room type definitions (Housing, Life Support, Work, Comfort, Utility categories)
- `res://data/economy.json`: Credit generation rates, building costs, happiness thresholds
- `res://Orbital Rings.sln`: Visual Studio project structure

**Core Logic:**
- `res://src/systems/RoomManager.cs`: Placement validation, segment tracking, interior generation trigger
- `res://src/systems/CitizenManager.cs`: Spawn citizens, manage schedules, path citizens along walkway
- `res://src/systems/EconomyManager.cs`: Track credits, handle transactions, notify of balance changes
- `res://src/services/ProceduralGenerator.cs`: Create unique room interiors from blueprints
- `res://src/services/TimeManager.cs`: Advance game clock, trigger citizen behaviors

**Testing (future):**
- `res://tests/` (to be created): Unit tests for RoomManager, EconomyManager, ProceduralGenerator
- `res://tests/data/` (to be created): Test fixtures and mock data

## Naming Conventions

**Files:**
- C# source: `PascalCase.cs` (e.g., `RoomManager.cs`, `ProceduralGenerator.cs`)
- Godot scenes: `PascalCase.tscn` (e.g., `main.tscn`, `HudPanel.tscn`)
- JSON data: `snake_case.json` (e.g., `rooms.json`, `economy.json`)
- Godot scripts attached to scenes: Name matches scene (e.g., `Room.cs` attached to `Room.tscn`)

**Directories:**
- C# namespaces map to directory structure: `OrbitalRings.Systems`, `OrbitalRings.Models`, `OrbitalRings.Services`
- Godot scene directories: lowercase with underscores (e.g., `scenes/ui/`, `scenes/entities/`)

**Classes:**
- Manager classes: `{Domain}Manager` (e.g., `RoomManager`, `CitizenManager`)
- Service classes: `{Feature}Service` or `{Feature}` (e.g., `ProceduralGenerator`, `PathFinder`)
- Model classes: Entity names (e.g., `Room`, `Citizen`, `Ring`, `Wish`)
- Configuration classes: `{Thing}Config` or `{Thing}Blueprint` (e.g., `RoomBlueprint`, `EconomyConfig`)

**Methods:**
- Query/getter: `Get{Thing}()`, `Is{Condition}()`, `Can{Action}()`
- Action/setter: `{Action}()`, `Set{Property}()`
- Callbacks/events: `On{Event}()`
- Validation: `Validate{Thing}()`, `Ensure{Requirement}()`

**Variables:**
- Private fields: `_camelCase` with leading underscore
- Public properties: `PascalCase`
- Local variables: `camelCase`
- Constants: `SCREAMING_SNAKE_CASE`

## Where to Add New Code

**New Feature (e.g., "Citizen Friendship System"):**
- Primary code: `res://src/systems/` — Create `RelationshipManager.cs`
- Models: `res://src/models/` — Create `Relationship.cs` if needed
- Data: `res://data/relationships.json` — New data file
- Scene: `res://scenes/ui/` — Add UI panel for viewing relationships (e.g., `RelationshipCard.tscn`)
- Tests: `res://tests/RelationshipManagerTests.cs` (if testing framework added)

**New Room Type:**
- Add definition to `res://data/rooms.json` under appropriate category
- Scene prefab: `res://scenes/entities/rooms/{RoomType}.tscn` (if custom visuals needed)
- Logic: Implement custom logic in `RoomManager.cs` or dedicated service if complex
- Interior generation: Add strategy to `ProceduralGenerator.cs`

**New UI Panel:**
- Scene: `res://scenes/ui/{Feature}Panel.tscn`
- Code-behind: `res://src/ui/{Feature}Panel.cs` (if complex interactivity)
- Strings: Add to `res://data/ui_strings.json`

**Utility Helper:**
- If domain-specific: Add to appropriate system's file (e.g., path logic in PathFinder)
- If general-purpose: Add to `res://src/utils/Extensions.cs` or create new file in `utils/`

**New System (e.g., "Weather System"):**
- Create `res://src/systems/WeatherManager.cs`
- Register in main scene or `GameState.cs` singleton
- Add data: `res://data/weather.json`
- Add visuals: New materials/shaders to `res://assets/shaders/`

## Special Directories

**`res://.godot/`:**
- Purpose: Godot engine cache and metadata
- Generated: Yes
- Committed: No (in `.gitignore`)
- Contains: Shader cache, import metadata, editor state

**`res://.planning/`:**
- Purpose: GSD (Get Shit Done) planning and analysis documents
- Generated: Yes (by GSD tools)
- Committed: Yes (outside res:// but tracked by git)
- Contains: Architecture analysis, testing patterns, coding conventions, concerns tracking

**`res://assets/models/`:**
- Purpose: 3D geometry files (exported from Blender, Godot ModelExporter, etc.)
- Generated: No (hand-created or third-party)
- Committed: Yes (versioned with code)
- Contains: `.gltf`, `.glb`, `.obj` files with corresponding `.import` metadata

**`res://obj/` (dotnet build output):**
- Purpose: C# compiled binaries, temporary build artifacts
- Generated: Yes
- Committed: No (in `.gitignore`)
- Contains: `.dll`, `.pdb`, intermediate object files

## Code Organization Principles

**Model-View Separation:**
- Models (pure data in `res://src/models/`) are independent of Godot Node3D
- Scenes (visual representation in `res://scenes/`) reference and display models
- Example: `Room.cs` (data) is separate from `Room.tscn` (visual node)

**Single Responsibility:**
- Each manager handles one domain (RoomManager handles placement only, not citizens)
- Services are stateless and reusable (ProceduralGenerator has no side effects)
- Example: EconomyManager tracks credits, separate system handles spending logic

**Dependency Inversion:**
- Managers depend on abstract interfaces or data, not on specific implementations
- ProceduralGenerator takes RoomBlueprint input, not hardcoded room types
- Allows swapping implementations (e.g., different pathfinding algorithms)

**Configuration Over Code:**
- Room definitions in `rooms.json`, not hardcoded in RoomManager
- Trait modifiers in `traits.json`, not in CitizenManager
- Allows game balance tuning without recompiling

