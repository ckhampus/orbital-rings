# Technology Stack

**Analysis Date:** 2026-03-02

## Languages

**Primary:**
- C# (.NET 8.0) - Game logic, systems, and gameplay implementation via Godot's C# scripting

**Secondary:**
- GDScript (optional) - Not currently in use; project uses C# exclusively
- GDShader - Rendering and shader effects (Godot native)

## Runtime

**Environment:**
- Godot Engine 4.6 (via .NET SDK integration)
- .NET 8.0 (primary runtime target)
- .NET 9.0 (fallback target for Android builds)

**Runtime Configuration:**
- SDK: Godot.NET.Sdk/4.6.1
- Dynamic loading enabled for modular code
- Root namespace: `OrbitalRings`

## Frameworks

**Core:**
- Godot Engine 4.6 - Game engine, scene system, 3D rendering, physics
  - Physics engine: Jolt Physics (configured in `project.godot`)
  - Rendering: Forward Plus rendering pipeline
  - Platform targets: Windows (D3D12), Linux, Android (net9.0 specifically)

**Build/Dev:**
- Godot.NET.Sdk 4.6.1 - .NET integration with Godot
- MSBuild - Project build system

## Key Dependencies

**Critical:**
- Godot.NET.Sdk 4.6.1 - Provides .NET/Godot integration layer
  - Includes bindings to Godot's native engine APIs
  - Handles C# compilation and runtime linking

**Infrastructure:**
- .NET 10 SDK (development toolchain) - Installed via dotnet-install.sh
  - Used for compilation and tooling in development containers
- csharp-language-server - Language server for C# IDE support
  - Installed globally in development environment
  - Provides intellisense, diagnostics, and refactoring capabilities

## Configuration

**Project:**
- `Orbital Rings.csproj` - MSBuild project configuration
  - TargetFramework: net8.0
  - Conditional TargetFramework: net9.0 for Android
  - EnableDynamicLoading: true
  - RootNamespace: OrbitalRings

**Godot:**
- `project.godot` - Engine configuration
  - Version: Godot 4.6
  - Physics: Jolt Physics engine
  - Rendering device (Windows): Direct3D 12
  - Game name: "Orbital Rings"
  - Icon: `res://icon.svg`

**Code Style:**
- `.editorconfig` - Cross-editor formatting standards
  - Enforces UTF-8 charset for all files

## Platform Requirements

**Development:**
- Node.js 24 (development container runtime)
- .NET 10 SDK (for tooling and build)
- Git 2.x
- Docker (for containerized development)
- C# language server support
- Visual Studio Code with extensions:
  - Claude Code (anthropic.claude-code)
  - C# Dev Kit (ms-dotnettools.csdevkit)
  - ESLint (dbaeumer.vscode-eslint)
  - Prettier (esbenp.prettier-vscode)
  - GitLens (eamodio.gitlens)

**Production:**
- Windows (D3D12) - Primary platform
- Linux (Vulkan/OpenGL)
- Android (via net9.0 target)
- Minimum: 4GB RAM, OpenGL 3.3+ or D3D12 compatible GPU

## Build Targets

**Configurations:**
- Debug - Development builds with symbols
- ExportDebug - Debug-optimized exports
- ExportRelease - Optimized release exports

**Platform Export Targets:**
- Windows (.exe)
- Linux (.x86_64)
- Android (.apk)

---

*Stack analysis: 2026-03-02*
