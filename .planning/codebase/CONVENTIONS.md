# Coding Conventions

**Analysis Date:** 2026-03-02

## Overview

This is a Godot 4.6 game engine project using C# as the primary language. The project is in early-stage development with the core project structure defined but implementation not yet begun. Conventions are based on Godot C# best practices, .NET standards, and configured development environment.

## Naming Patterns

**Files:**
- C# source files: PascalCase (e.g., `GameManager.cs`, `RingBuilder.cs`, `CitizenAI.cs`)
- Classes and scripts in `/Assets` or source directories follow their namespace structure
- Godot scripts (if any): Use PascalCase matching the class name
- Test files: `[ClassName]Tests.cs` or `[ClassName]Test.cs`

**Classes & Types:**
- Public classes: PascalCase (e.g., `Ring`, `Citizen`, `WishSystem`, `GameState`)
- Interfaces: PascalCase prefixed with `I` (e.g., `IWishable`, `IRoom`, `IEventListener`)
- Enums: PascalCase (e.g., `RoomType`, `CitizenTrait`, `GameState`)
- Abstract classes: PascalCase with "Base" or "Abstract" suffix optional (e.g., `RoomBase`)

**Methods:**
- Public methods: PascalCase (e.g., `BuildRoom()`, `FulfillWish()`, `CalculateHappiness()`)
- Private methods: camelCase or PascalCase (camelCase preferred for internal helpers)
- Async methods: End with `Async` (e.g., `LoadCitizenAsync()`)
- Event handlers: Prefix with `On` (e.g., `OnWishFulfilled()`, `OnRingCompleted()`)

**Properties:**
- Public properties: PascalCase (e.g., `CurrentHappiness`, `CitizenCount`, `AvailableSegments`)
- Private fields: camelCase prefixed with underscore (e.g., `_currentHappiness`, `_citizenList`)
- Use C# properties with auto-properties where possible: `public int Happiness { get; set; }`

**Variables:**
- Local variables: camelCase (e.g., `totalCredits`, `selectedRoom`, `isComplete`)
- Constants: UPPERCASE_WITH_UNDERSCORES (e.g., `MAX_CITIZENS_PER_RING`, `SEGMENT_COUNT`)
- Boolean variables: Prefix with `is`, `has`, `can`, or `should` (e.g., `isComplete`, `hasWish`, `canBuild`)

**Godot Nodes:**
- Node names: PascalCase in scenes (e.g., `RingContainer`, `UIPanel`, `CitizenSprite`)
- Exported variables in Godot: Use `[Export]` attribute, camelCase names in editor

## Code Style

**Formatting:**
- Tool: Prettier (configured in devcontainer) with C# support via extensions
- Line length: 100 characters (soft limit for readability in devcontainer)
- Indentation: 2 spaces (per `.editorconfig`)
- Spacing: One space around operators, after keywords, and in type declarations

**Brace Style:**
- Allman style (opening braces on new line) or K&R (same line) - follow Godot C# conventions (K&R preferred)
```csharp
public void BuildRoom(Room room)
{
  _rooms.Add(room);
}
```

**Linting:**
- Tool: ESLint (for JavaScript/TypeScript when present)
- C# code style enforced via IDE code analysis (.NET Roslyn analyzers)
- Recommended: Microsoft.CodeAnalysis.NetAnalyzers
- Devcontainer includes: ms-dotnettools.csdevkit extension for VS Code

**Editor Configuration:**
- File: `.editorconfig` (present but minimal)
- Key settings:
  - Character set: UTF-8
  - Line endings: LF (enforced via `.gitattributes` - all files)
  - Formatting: enabled on save in devcontainer VS Code settings

## Import Organization

**Order (in C#):**
1. System imports (`using System;`, `using System.Collections.Generic;`)
2. Godot imports (`using Godot;`)
3. Third-party imports (if any)
4. Project namespace imports (`using OrbitalRings.Core;`, `using OrbitalRings.Systems;`)
5. Blank line before namespace declaration

**Example:**
```csharp
using System;
using System.Collections.Generic;
using Godot;
using OrbitalRings.Core;
using OrbitalRings.Systems;

namespace OrbitalRings.Gameplay
{
  public class Ring : Node3D
  {
  }
}
```

**Path Aliases:**
- Root namespace: `OrbitalRings`
- Organized by feature/layer:
  - `OrbitalRings.Core` - Core game logic
  - `OrbitalRings.Gameplay` - Gameplay mechanics
  - `OrbitalRings.Systems` - Game systems (economy, happiness, etc.)
  - `OrbitalRings.UI` - User interface
  - `OrbitalRings.Procedural` - Procedural generation
  - `OrbitalRings.Data` - Data models and structures

## Godot C# Specific Patterns

**Node Structure:**
- Inherit from appropriate Godot base classes (`Node`, `Node3D`, `Control`, etc.)
- Use `[Export]` attribute for inspector-editable fields
- Override Godot lifecycle methods: `_Ready()`, `_Process(float delta)`, `_PhysicsProcess(float delta)`

**Signals (Events):**
- Define signals at class top level
- Naming: PascalCase, emit with `EmitSignal(nameof(SignalName), args)`
- Example: `[Signal] public delegate void RoomBuilt(Room room);`

**Scene-Unique Names:**
- Use `%NodeName` syntax in GDScript, or find nodes by name in C#
- In C#: `GetNode<NodeType>("%UniqueName")` or `GetNodeOrNull<NodeType>()`

## Error Handling

**Exception Types:**
- Use specific exception types: `ArgumentException`, `InvalidOperationException`, `NotSupportedException`
- Avoid bare `catch (Exception)` - catch specific types
- Log exceptions with context before rethrowing or handling

**Pattern:**
```csharp
try
{
  // Operation
}
catch (ArgumentException ex)
{
  GD.PrintErr($"Invalid argument: {ex.Message}");
  // Handle gracefully
}
catch (Exception ex)
{
  GD.PrintErr($"Unexpected error: {ex}");
  throw;
}
```

**Validation:**
- Validate inputs at method entry
- Throw `ArgumentException` for invalid parameters: `if (room == null) throw new ArgumentException(nameof(room));`
- Use guard clauses for early returns

## Logging

**Framework:** Godot's built-in logging (`GD` class)

**Patterns:**
- `GD.Print("Message")` - General info
- `GD.PrintDebug("Message")` - Debug info (only in debug builds)
- `GD.PrintErr("Message")` - Errors
- `GD.PrintStack()` - Print stack trace
- Include context in messages: `GD.Print($"Building {room.Type} in segment {room.Segment}");`

**When to log:**
- Major state changes: building rooms, citizens arriving
- Errors and exceptions: always log before handling
- Debug info: algorithm decisions, path calculations
- NOT for frame-by-frame events (performance impact)

## Comments

**When to Comment:**
- Complex algorithms or non-obvious logic (e.g., procedural generation math)
- Why a decision was made (not what the code does - code should be self-documenting)
- Workarounds or temporary solutions: always prefix with `TODO:` or `HACK:`
- Gotchas or non-intuitive behavior

**XMLDoc/JSDoc:**
- Public methods and classes: include XMLDoc comments
- Format: `/// <summary>Description</summary>`

**Example:**
```csharp
/// <summary>
/// Calculates happiness modifier based on station population.
/// Happiness increases non-linearly to avoid early saturation.
/// </summary>
private float CalculatePopulationModifier(int citizenCount)
{
  // Use logarithmic scale to prevent explosive growth
  return Mathf.Log(citizenCount + 1) * 0.5f;
}
```

**Avoid:**
- Comments that restate code: `x++; // increment x`
- Comments in dead code - delete it instead
- Outdated comments (update or remove when code changes)

## Function Design

**Size:**
- Keep functions small and focused (ideally under 30 lines)
- Extract complex logic into helper functions
- Single responsibility principle: one job per method

**Parameters:**
- Limit to 3-4 parameters; use data objects for more
- Use `out` or `ref` sparingly; prefer return values
- Use optional parameters for sensible defaults

**Return Values:**
- Explicit about what is returned
- Use tuples for multiple return values: `(bool success, string message)`
- Return empty collections instead of null: `new List<Room>()` not `null`

**Async:**
- Follow async/await patterns consistently
- Suffix method name with `Async`: `LoadDataAsync()`
- Use `Task` or `Task<T>`; avoid `async void` except for event handlers

## Module Design

**Exports:**
- Public: only what's needed by other modules
- Internal: `internal` keyword for same-assembly visibility
- Private: default for implementation details
- Use interfaces to define public contracts

**Barrel Files:**
- Create `[Feature]Exports.cs` in each feature namespace for convenience exports
- Example: `OrbitalRings.Systems` exports core types for public use

**Dependencies:**
- Inject dependencies via constructor when possible
- Use Godot's node tree for scene-based dependencies
- Avoid circular dependencies; establish clear dependency direction

## Godot-Specific Conventions

**Project Root Namespace:**
- `RootNamespace`: `OrbitalRings` (defined in `.csproj`)

**Script Naming:**
- Match C# class name to script/file name exactly
- Example: `class Ring` in file `Ring.cs`

**3D Nodes:**
- Inherit from `Node3D` for 3D game objects
- Use `Transform3D` for positioning/rotation
- Scale-related operations: use `Scale` property (Vector3)

**Resources:**
- Use `.tres` (text resources) for data that needs to be edited in editor
- Load with `ResourceLoader.Load<T>(path)`
- Cache frequently-accessed resources

## SOLID Principles Application

- **S**ingle Responsibility: Each class has one reason to change
- **O**pen/Closed: Open for extension, closed for modification (use inheritance/interfaces)
- **L**iskov Substitution: Derived classes are substitutable for base classes
- **I**nterface Segregation: Interfaces are focused and specific
- **D**ependency Inversion: Depend on abstractions, not concrete implementations

---

*Convention analysis: 2026-03-02*
