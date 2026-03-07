# Phase 20: Test Framework Wiring - Research

**Researched:** 2026-03-07
**Domain:** Godot 4 C# testing infrastructure (GoDotTest + Shouldly + NuGet)
**Confidence:** HIGH

## Summary

This phase wires up Chickensoft's GoDotTest testing framework into an existing Godot 4.6 / .NET 10 / C# game project. The ecosystem is well-documented: Chickensoft maintains GoDotTest as the standard Godot C# test runner, with GodotTestDriver for integration tests, Shouldly for assertions, and LightMoq for source-generator mocking. The GameDemo reference project provides a battle-tested pattern for conditional test compilation, export exclusion, and CLI invocation.

The key architectural pattern is a **conditional compilation gate**: test code compiles only in Debug/ExportDebug configurations via an MSBuild `RunTests` property, and a `#if RUN_TESTS` block in the game's entry point routes to GoDotTest's `GoTest.RunTests()` when `--run-tests` is passed on the command line. Tests extend `TestClass`, use `[Test]` attributes for discovery, and run sequentially in-process within the Godot runtime. Export builds automatically exclude test files via `DefaultItemExcludes` in the `.csproj`.

**Primary recommendation:** Follow Chickensoft's GameDemo pattern exactly -- conditional `RunTests` MSBuild property, `#if RUN_TESTS` entry-point routing, `DefaultItemExcludes` for export exclusion, and a `Tests/` directory with domain-organized test classes.

<user_constraints>

## User Constraints (from CONTEXT.md)

### Locked Decisions
- Tests live in `Tests/` at project root (not inside `Scripts/`)
- Organized by domain: `Tests/Mood/`, `Tests/Economy/`, `Tests/Housing/`, `Tests/Save/`
- Test runner scene at `Tests/TestRunner.tscn`
- Test files named `{System}Tests.cs` (e.g., `MoodSystemTests.cs`, `HousingTests.cs`)
- One real passing test using `ComputeCapacity` -- proves framework works AND exercises game logic
- One commented-out deliberate failure with instructions to uncomment for verifying Shouldly error readability
- Minimal scope: one passing method + one commented failure block in a single test class
- Supports both in-editor launch (F5 on test scene) and CLI execution
- CLI invocation: `godot --run-tests --quit-on-finish` with custom arg parsing in test runner
- Output to stdout only -- no log files, no visual UI in editor
- Test results print to Godot Output panel when run in editor
- Mirror production domains: `OrbitalRings.Tests.Mood`, `OrbitalRings.Tests.Economy`, etc.
- Test classes extend GoDotTest's `TestClass` base directly (no custom project base class)
- Test methods use `[Test]` attribute for discovery
- Shouldly assertions used directly (`value.ShouldBe(expected)`) -- no project wrappers
- GoDotTest, GodotTestDriver, Shouldly, and LightMoq added as NuGet packages
- LightMoq wired as package only -- actual mock usage in later phases
- NuGet.Config updated to include nuget.org alongside existing godot-local source

### Claude's Discretion
- Exact GoDotTest test runner scene node structure
- How CLI args (`--run-tests`, `--quit-on-finish`) are parsed and routed
- Export exclusion mechanism (`.gdignore`, export preset filter, or conditional compilation)
- Which ComputeCapacity assertion to use for the smoke test

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope

</user_constraints>

<phase_requirements>

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| FRMW-01 | Test runner scene discovers and executes test classes via GoDotTest | GoDotTest `TestProvider` uses reflection to find all `TestClass` subclasses in the assembly. `GoTest.RunTests()` call in test runner script handles discovery and execution. |
| FRMW-02 | Tests run headless via command-line (`--run-tests --quit-on-finish`) | `TestEnvironment.From(OS.GetCmdlineArgs())` parses `--run-tests` and `--quit-on-finish` flags. Entry point checks `Environment.ShouldRunTests` to route to test execution. |
| FRMW-03 | Shouldly assertion library available in test code | Shouldly 4.3.0 NuGet package added as conditional PackageReference. Extension methods like `.ShouldBe()` available in test classes. |
| FRMW-04 | Test files excluded from release/export builds | Three-layer approach: (1) `DefaultItemExcludes` removes `Tests/**/*` in ExportRelease, (2) `<Compile Remove>` excludes test .cs files when `RunTests != true`, (3) `.gdignore` in Tests/ prevents Godot from importing .tscn files. |
| FRMW-05 | NuGet.Config updated to restore testing packages from nuget.org | Add `<add key="nuget.org" value="https://api.nuget.org/v3/index.json" />` to existing NuGet.Config alongside godot-local source. |

</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Chickensoft.GoDotTest | 2.0.30 | Test runner and discovery for Godot C# | Only mature Godot-native C# test runner; runs inside Godot process with scene tree access |
| Chickensoft.GodotTestDriver | 3.1.62 | Integration test utilities (drivers, fixtures) | Companion to GoDotTest; simulates input, provides node drivers, manages test fixtures |
| Shouldly | 4.3.0 | Assertion library | Framework-agnostic, readable error messages, widely adopted in .NET ecosystem |
| LightMoq | 0.1.0 | Moq-like API over LightMock.Generator | Source-generator mocking compatible with Godot's collectible assemblies (Moq uses runtime codegen which conflicts) |
| LightMock.Generator | 1.2.3 | Source-generator mock foundation | Required dependency for LightMoq; generates mocks at compile-time |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| (none for Phase 20) | - | - | LightMoq and GodotTestDriver are wired but not exercised until later phases |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| GoDotTest | gdUnit4 | gdUnit4 is heavier, has its own assertion library, and is more oriented toward GDScript; GoDotTest is pure C# and lighter |
| GoDotTest | xUnit/NUnit | Runs outside Godot process -- no scene tree access, no Godot API access |
| Shouldly | FluentAssertions | Both work; Shouldly is lighter and CONTEXT.md explicitly chose it |
| LightMoq | Moq | Moq uses System.Reflection.Emit which conflicts with Godot's collectible assemblies |

**Installation (via .csproj conditional ItemGroup):**
```xml
<ItemGroup Condition="'$(RunTests)' == 'true'">
  <PackageReference Include="Chickensoft.GoDotTest" Version="2.0.30" />
  <PackageReference Include="Chickensoft.GodotTestDriver" Version="3.1.62" />
  <PackageReference Include="Shouldly" Version="4.3.0" />
  <PackageReference Include="LightMoq" Version="0.1.0" />
  <PackageReference Include="LightMock.Generator" Version="1.2.3" />
</ItemGroup>
```

## Architecture Patterns

### Recommended Project Structure
```
Tests/
├── TestRunner.tscn          # Godot scene that hosts test execution
├── TestRunner.cs            # C# script: parses args, calls GoTest.RunTests()
├── .gdignore                # Prevents Godot from importing test assets into exports
├── Housing/
│   └── HousingTests.cs      # Smoke test with ComputeCapacity assertion
├── Mood/                     # (empty, future phase)
├── Economy/                  # (empty, future phase)
└── Save/                     # (empty, future phase)
```

### Pattern 1: Conditional Test Compilation (MSBuild)
**What:** Use an MSBuild `RunTests` property to conditionally include test code and packages
**When to use:** Always -- this is the standard Chickensoft pattern for Godot C# projects
**Example:**
```xml
<!-- Source: Chickensoft GameDemo .csproj -->
<PropertyGroup>
  <RunTests>false</RunTests>
</PropertyGroup>

<PropertyGroup Condition="('$(Configuration)' == 'Debug' or '$(Configuration)' == 'ExportDebug') and '$(SkipTests)' != 'true'">
  <RunTests>true</RunTests>
  <DefineConstants>$(DefineConstants);RUN_TESTS</DefineConstants>
</PropertyGroup>

<!-- Test packages only included when RunTests is true -->
<ItemGroup Condition="'$(RunTests)' == 'true'">
  <PackageReference Include="Chickensoft.GoDotTest" Version="2.0.30" />
  <PackageReference Include="Chickensoft.GodotTestDriver" Version="3.1.62" />
  <PackageReference Include="Shouldly" Version="4.3.0" />
  <PackageReference Include="LightMoq" Version="0.1.0" />
  <PackageReference Include="LightMock.Generator" Version="1.2.3" />
</ItemGroup>

<!-- Exclude test source files from release builds -->
<ItemGroup Condition="'$(RunTests)' != 'true'">
  <Compile Remove="Tests/**/*.cs" />
  <None Remove="Tests/**/*" />
  <EmbeddedResource Remove="Tests/**/*" />
</ItemGroup>
```

### Pattern 2: Test Runner Scene Script
**What:** A minimal Node2D scene with a C# script that detects test mode and invokes GoDotTest
**When to use:** For the `Tests/TestRunner.tscn` scene
**Example:**
```csharp
// Source: GoDotTest README + Chickensoft GameDemo Main.cs pattern
namespace OrbitalRings.Tests;

using System.Reflection;
using Godot;

#if RUN_TESTS
using GoDotTest;
#endif

public partial class TestRunner : Node2D
{
#if RUN_TESTS
    public TestEnvironment Environment = default!;
#endif

    public override void _Ready()
    {
#if RUN_TESTS
        Environment = TestEnvironment.From(OS.GetCmdlineArgs());
        if (Environment.ShouldRunTests)
        {
            CallDeferred(nameof(RunTests));
            return;
        }
#endif
        // If not running tests, just quit (this scene has no game purpose)
        GetTree().Quit();
    }

#if RUN_TESTS
    private void RunTests()
        => _ = GoTest.RunTests(Assembly.GetExecutingAssembly(), this, Environment);
#endif
}
```

### Pattern 3: Test Class Structure
**What:** Tests extend `TestClass`, use `[Test]` attribute, receive test scene via constructor
**When to use:** Every test file
**Example:**
```csharp
// Source: GoDotTest README
namespace OrbitalRings.Tests.Housing;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using OrbitalRings.Data;

public class HousingTests : TestClass
{
    public HousingTests(Node testScene) : base(testScene) { }

    [Test]
    public void ComputeCapacitySingleSegment()
    {
        var definition = new RoomDefinition { BaseCapacity = 2 };
        int capacity = HousingManager.ComputeCapacity(definition, 1);
        capacity.ShouldBe(2);  // BaseCapacity + (1 - 1) = 2
    }
}
```

### Pattern 4: CLI Invocation
**What:** GoDotTest parses `--run-tests` and `--quit-on-finish` from `OS.GetCmdlineArgs()`
**When to use:** Running tests from terminal or CI
**Example:**
```bash
# Run all tests and exit
godot --run-tests --quit-on-finish

# Run a specific test suite
godot --run-tests=HousingTests --quit-on-finish

# Run with stop-on-error
godot --run-tests --quit-on-finish --stop-on-error
```

**Critical detail:** These flags are NOT Godot engine flags -- they are custom flags that Godot passes through to `OS.GetCmdlineArgs()`. GoDotTest's `TestEnvironment.From()` parses them. The `--` separator is NOT needed because Godot passes through any unrecognized flags.

### Pattern 5: NuGet.Config with Multiple Sources
**What:** Add nuget.org alongside existing godot-local source
**When to use:** Required for test package restoration
**Example:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="godot-local" value="/usr/local/bin/GodotSharp/Tools/nupkgs" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

### Anti-Patterns to Avoid
- **Separate test .csproj:** Breaks internal member access and complicates Godot's build pipeline. Tests MUST compile in the same assembly.
- **Using Moq instead of LightMoq:** Moq uses `System.Reflection.Emit` at runtime, which conflicts with Godot's collectible assemblies. LightMoq uses source generators (compile-time).
- **Putting tests in `Scripts/`:** Mixes test and production code; makes export exclusion harder.
- **Running tests outside Godot process (xUnit/NUnit):** No access to scene tree, Godot APIs, or engine lifecycle. Tests that need `Node`, `SceneTree`, or any Godot type will fail.
- **Using `#if DEBUG` for test exclusion:** In Godot, the `DEBUG` constant may still be defined in some export scenarios. Use `#if RUN_TESTS` (custom define) instead for reliability.
- **Routing test execution through game's main scene:** Adds complexity to game code. Use a dedicated `TestRunner.tscn` that is only launched for testing.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Test discovery | Manual test registration | GoDotTest `TestProvider` reflection | Automatic discovery of all `TestClass` subclasses; zero maintenance |
| CLI arg parsing | Custom argument parser | `TestEnvironment.From(OS.GetCmdlineArgs())` | Handles `--run-tests`, `--quit-on-finish`, `--coverage`, `--stop-on-error` etc. |
| Assertion messages | Custom `Assert.AreEqual` wrappers | Shouldly `.ShouldBe()` etc. | Rich error messages with "expected X but got Y" formatting |
| Test lifecycle | Manual setup/teardown calls | `[SetupAll]`, `[Setup]`, `[Cleanup]`, `[CleanupAll]` attributes | GoDotTest handles ordering and invocation |
| Mock generation | Hand-written fakes | LightMoq + LightMock.Generator | Source-generator mocking that works with Godot's assembly model |

**Key insight:** GoDotTest is intentionally minimal -- it handles discovery, execution, and lifecycle. Don't wrap it in custom infrastructure. The framework's simplicity IS the feature.

## Common Pitfalls

### Pitfall 1: NuGet Source Not Found
**What goes wrong:** `dotnet restore` fails because test packages are only on nuget.org but NuGet.Config only has godot-local source
**Why it happens:** The existing project uses a local NuGet source for Godot SDK packages only
**How to avoid:** Add nuget.org to NuGet.Config BEFORE adding PackageReference entries to .csproj
**Warning signs:** `NU1101: Unable to find package` errors during build

### Pitfall 2: Test Code in Release Builds
**What goes wrong:** Export build includes test assemblies, increasing binary size and exposing test code
**Why it happens:** Without conditional compilation, all .cs files compile into the assembly
**How to avoid:** Three-layer exclusion: (1) MSBuild `RunTests` property gates compilation, (2) `<Compile Remove="Tests/**/*.cs">` for non-test configs, (3) `.gdignore` in Tests/ prevents scene importing
**Warning signs:** Test namespaces visible in release build via reflection, larger-than-expected export size

### Pitfall 3: .gdignore Doesn't Exclude C# from Build
**What goes wrong:** Placing `.gdignore` in Tests/ and expecting it to prevent C# compilation
**Why it happens:** `.gdignore` only prevents Godot resource importing (scenes, textures, etc.) -- it does NOT affect MSBuild/C# compilation
**How to avoid:** Use `.gdignore` for Godot resources AND `<Compile Remove>` in .csproj for C# files. Both are needed for complete exclusion.
**Warning signs:** Test classes still appearing in build output despite .gdignore

### Pitfall 4: RoomDefinition Instantiation in Tests
**What goes wrong:** `new RoomDefinition()` fails or behaves unexpectedly because `RoomDefinition` extends `Godot.Resource`
**Why it happens:** Godot `Resource` subclasses have engine-managed lifecycle
**How to avoid:** GoDotTest runs inside the Godot process, so `new RoomDefinition()` should work because the engine is initialized. But if issues arise, set properties directly after construction rather than using a constructor.
**Warning signs:** `ObjectDisposedException` or null reference on Resource properties

### Pitfall 5: Forgetting CallDeferred for Test Execution
**What goes wrong:** Tests start executing before the scene tree is fully ready
**Why it happens:** `_Ready()` fires before the current frame's deferred calls
**How to avoid:** Use `CallDeferred(nameof(RunTests))` as shown in the Chickensoft pattern
**Warning signs:** Intermittent test failures, null scene tree references

### Pitfall 6: Godot 4.6 / .NET 10 Assembly Probing Issue
**What goes wrong:** NuGet restore or test execution fails on Windows with assembly probing errors
**Why it happens:** Known Godot issue (godotengine/godot#112701) with shared framework assembly probing
**How to avoid:** Flag for validation during NuGet restore; likely does not affect testing packages but should be tested
**Warning signs:** `System.IO.FileNotFoundException` for framework assemblies

### Pitfall 7: Test Scene Not Found When Running CLI
**What goes wrong:** `godot --run-tests --quit-on-finish` launches the game's main scene instead of the test scene
**Why it happens:** GoDotTest relies on the entry point scene (the one configured in project.godot as main_scene) to check for test flags
**How to avoid:** Either (a) modify the existing main scene to check for `--run-tests` and route to TestRunner, or (b) launch Godot with an explicit scene path: `godot res://Tests/TestRunner.tscn -- --run-tests --quit-on-finish`. The CONTEXT.md specifies the CLI format as `godot --run-tests --quit-on-finish`, so approach (a) would require a small shim in the main scene OR the TestRunner.tscn needs to be directly launchable.
**Warning signs:** Game starts normally instead of running tests

## Code Examples

### Complete Test Runner Script
```csharp
// Source: GoDotTest README + Chickensoft GameDemo pattern, adapted for Orbital Rings
namespace OrbitalRings.Tests;

using Godot;

#if RUN_TESTS
using System.Reflection;
using GoDotTest;
#endif

public partial class TestRunner : Node2D
{
#if RUN_TESTS
    public TestEnvironment Environment = default!;
#endif

    public override void _Ready()
    {
#if RUN_TESTS
        Environment = TestEnvironment.From(OS.GetCmdlineArgs());
        if (Environment.ShouldRunTests)
        {
            CallDeferred(nameof(RunTests));
            return;
        }
#endif
        GetTree().Quit();
    }

#if RUN_TESTS
    private void RunTests()
        => _ = GoTest.RunTests(Assembly.GetExecutingAssembly(), this, Environment);
#endif
}
```

### Complete Smoke Test (HousingTests.cs)
```csharp
// Source: GoDotTest README patterns + project-specific ComputeCapacity
namespace OrbitalRings.Tests.Housing;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using OrbitalRings.Data;

public class HousingTests : TestClass
{
    public HousingTests(Node testScene) : base(testScene) { }

    [Test]
    public void ComputeCapacityReturnsBaseForSingleSegment()
    {
        // A 1-segment room: capacity = BaseCapacity + (1 - 1) = BaseCapacity
        var definition = new RoomDefinition { BaseCapacity = 2 };

        int capacity = HousingManager.ComputeCapacity(definition, 1);

        capacity.ShouldBe(2);
    }

    // ---------------------------------------------------------------
    // DELIBERATE FAILURE — Uncomment to verify Shouldly error messages
    // ---------------------------------------------------------------
    // [Test]
    // public void ShouldlyErrorMessageDemo()
    // {
    //     int actual = 42;
    //     actual.ShouldBe(99, "This should fail with a readable message");
    // }
}
```

### GoDotTest Lifecycle Attributes
```csharp
// Source: GoDotTest README
// Available lifecycle attributes (run in this order):
//   [SetupAll]   — once before all tests in the class
//   [Setup]      — before each [Test] method
//   [Test]       — the test method itself
//   [Cleanup]    — after each [Test] method
//   [CleanupAll] — once after all tests in the class
//   [Failure]    — called whenever any test in the suite fails

// Methods can be sync (void) or async (async Task)
```

### Updated .csproj (complete)
```xml
<Project Sdk="Godot.NET.Sdk/4.6.1">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <TargetFramework Condition=" '$(GodotTargetPlatform)' == 'android' ">net9.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <RootNamespace>OrbitalRings</RootNamespace>
    <!-- Test compilation gate -->
    <RunTests>false</RunTests>
  </PropertyGroup>

  <!-- Enable tests in Debug/ExportDebug unless explicitly skipped -->
  <PropertyGroup Condition="('$(Configuration)' == 'Debug' or '$(Configuration)' == 'ExportDebug') and '$(SkipTests)' != 'true'">
    <RunTests>true</RunTests>
    <DefineConstants>$(DefineConstants);RUN_TESTS</DefineConstants>
  </PropertyGroup>

  <!-- Test packages — only in test-enabled builds -->
  <ItemGroup Condition="'$(RunTests)' == 'true'">
    <PackageReference Include="Chickensoft.GoDotTest" Version="2.0.30" />
    <PackageReference Include="Chickensoft.GodotTestDriver" Version="3.1.62" />
    <PackageReference Include="Shouldly" Version="4.3.0" />
    <PackageReference Include="LightMoq" Version="0.1.0" />
    <PackageReference Include="LightMock.Generator" Version="1.2.3" />
  </ItemGroup>

  <!-- Exclude test files from non-test builds -->
  <ItemGroup Condition="'$(RunTests)' != 'true'">
    <Compile Remove="Tests/**/*.cs" />
    <None Remove="Tests/**/*" />
    <EmbeddedResource Remove="Tests/**/*" />
  </ItemGroup>
</Project>
```

### CLI Invocation
```bash
# Run all tests (launching the test scene directly)
godot res://Tests/TestRunner.tscn --run-tests --quit-on-finish

# Run specific test suite
godot res://Tests/TestRunner.tscn --run-tests=HousingTests --quit-on-finish

# In-editor: set Main Run Args in Project Settings to:
#   --run-tests --quit-on-finish
# Then F5 on TestRunner.tscn
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| gdUnit4 (mixed GDScript/C#) | GoDotTest (pure C#) | 2023+ | Lighter, C#-native, better Godot integration |
| Moq (runtime codegen) | LightMoq (source generators) | 2024+ | Compatible with Godot's collectible assemblies |
| `#if DEBUG` for test exclusion | `#if RUN_TESTS` custom define | Chickensoft pattern | More reliable -- `DEBUG` behavior inconsistent in Godot exports |
| Separate test .csproj | Single .csproj with conditional compilation | Chickensoft standard | Maintains internal member access, simpler build pipeline |

**Deprecated/outdated:**
- GoDotTest versions < 2.0: Older API, different namespace (`GoDotTest` vs `Chickensoft.GoDotTest`)
- Using Moq in Godot C#: Conflicts with collectible assemblies; use LightMoq instead

## Discretionary Decisions (Research Recommendations)

### Test Runner Scene Node Structure
**Recommendation:** A single `Node2D` root named "TestRunner" with the `TestRunner.cs` script attached. No child nodes needed -- GoDotTest manages test execution entirely through code. The scene file is minimal:
```
[gd_scene load_steps=2 format=3]
[ext_resource type="Script" path="res://Tests/TestRunner.cs" id="1"]
[node name="TestRunner" type="Node2D"]
script = ExtResource("1")
```

### CLI Argument Parsing
**Recommendation:** Use `TestEnvironment.From(OS.GetCmdlineArgs())` in the TestRunner script directly. Since the user wants `godot --run-tests --quit-on-finish`, launch Godot with the test scene explicitly: `godot res://Tests/TestRunner.tscn --run-tests --quit-on-finish`. This avoids modifying the game's main scene (TitleScreen). The unrecognized `--run-tests` flag passes through to `OS.GetCmdlineArgs()` where `TestEnvironment.From()` picks it up.

### Export Exclusion Mechanism
**Recommendation:** Use ALL THREE layers for robust exclusion:
1. **`.gdignore`** in `Tests/` -- prevents Godot from importing test .tscn/.tres files
2. **`<Compile Remove="Tests/**/*.cs">`** -- prevents C# compilation of test files in release builds
3. **MSBuild conditional `RunTests` property** -- gates both package references and file inclusion

The `.gdignore` alone is NOT sufficient (it doesn't affect C# compilation). The MSBuild conditions alone are NOT sufficient (Godot may still try to import .tscn files). All three together provide complete exclusion.

### ComputeCapacity Smoke Test Assertion
**Recommendation:** Test `ComputeCapacity` with a single-segment room (`segments=1`) asserting `BaseCapacity + (1-1) = BaseCapacity`. This is the simplest case and validates the formula works. The method signature is `HousingManager.ComputeCapacity(RoomDefinition definition, int segmentCount)` and it is `public static`, making it trivially testable without any Godot scene setup.

## Open Questions

1. **GoDotTest 2.0.30 compatibility with .NET 10**
   - What we know: GameDemo uses GoDotTest 2.0.30 with .NET 8 and Godot 4.6.1. This project targets .NET 10.
   - What's unclear: Whether GoDotTest 2.0.30 has been tested with .NET 10 specifically. The package targets .NET 6+ which should be forward-compatible.
   - Recommendation: Proceed with 2.0.30. If NuGet restore or compilation fails, try the latest available version. The .NET 10 TFM is forward-compatible with .NET 6+ packages.

2. **LightMoq package naming**
   - What we know: The NuGet package appears as "LightMoq" (not "Chickensoft.LightMoq") on the Chickensoft profile, version 0.1.0.
   - What's unclear: Whether the package ID has a Chickensoft prefix or not. GitHub repo is `chickensoft-games/LightMoq`.
   - Recommendation: Try `LightMoq` first. If not found, try `Chickensoft.LightMoq`. Validate during NuGet restore.

3. **LightMock.Generator as explicit dependency**
   - What we know: LightMoq depends on LightMock.Generator. The GameDemo .csproj does NOT list LightMock.Generator explicitly -- it lists Moq instead (they use Moq, not LightMoq).
   - What's unclear: Whether LightMoq pulls in LightMock.Generator transitively or whether it needs to be listed explicitly.
   - Recommendation: Add LightMock.Generator explicitly as a PackageReference with `PrivateAssets="all" OutputItemType="analyzer"` since it is a source generator. If it turns out to be transitive, the explicit reference is harmless.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | GoDotTest 2.0.30 (Chickensoft) |
| Config file | `Orbital Rings.csproj` (conditional ItemGroups) |
| Quick run command | `godot res://Tests/TestRunner.tscn --run-tests --quit-on-finish` |
| Full suite command | `godot res://Tests/TestRunner.tscn --run-tests --quit-on-finish` |

### Phase Requirements Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| FRMW-01 | Test runner discovers and executes test classes | smoke | `godot res://Tests/TestRunner.tscn --run-tests --quit-on-finish` (exit code 0) | Wave 0 |
| FRMW-02 | Tests run headless via CLI | smoke | Same command; verify exit code 0 | Wave 0 |
| FRMW-03 | Shouldly assertions compile and execute | smoke | HousingTests.ComputeCapacityReturnsBaseForSingleSegment passes | Wave 0 |
| FRMW-04 | Test files excluded from export | manual | Build export, verify no Tests/ in output; verify `dotnet build -c ExportRelease` excludes test files | Manual verification |
| FRMW-05 | NuGet restore pulls test packages | smoke | `dotnet restore` succeeds without errors | Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet build` (verifies compilation)
- **Per wave merge:** `godot res://Tests/TestRunner.tscn --run-tests --quit-on-finish` (exit code 0)
- **Phase gate:** Full test run green + manual export verification

### Wave 0 Gaps
- [ ] `Tests/TestRunner.tscn` -- test runner scene
- [ ] `Tests/TestRunner.cs` -- test runner script
- [ ] `Tests/Housing/HousingTests.cs` -- smoke test
- [ ] `Tests/.gdignore` -- export exclusion
- [ ] NuGet.Config update -- add nuget.org source
- [ ] `.csproj` updates -- conditional test compilation

## Sources

### Primary (HIGH confidence)
- [GoDotTest README](https://github.com/chickensoft-games/GoDotTest/blob/main/README.md) - Test runner setup, CLI flags, test class structure, export exclusion pattern
- [Chickensoft GameDemo .csproj](https://github.com/chickensoft-games/GameDemo) - Reference implementation of conditional test compilation with RunTests property, DefineConstants, and ItemGroup conditions
- [Chickensoft NuGet Profile](https://www.nuget.org/profiles/Chickensoft) - Package names and versions (GoDotTest 2.0.30, GodotTestDriver 3.1.62, LightMoq 0.1.0)
- [Shouldly NuGet](https://www.nuget.org/packages/shouldly/) - Version 4.3.0, .NET Standard 2.0+ compatible
- [LightMock.Generator NuGet](https://www.nuget.org/packages/LightMock.Generator) - Version 1.2.3, source generator for mocking

### Secondary (MEDIUM confidence)
- [Godot Issue #56339](https://github.com/godotengine/godot/issues/56339) - Confirmed .gdignore does NOT exclude C# from build (resource importing only)
- [Godot Forum: #if DEBUG in exports](https://forum.godotengine.org/t/why-if-debug-is-still-true-in-exported-program/127904) - Verified DEBUG constant behavior in Godot exports (unreliable, use custom define)
- [DeepWiki: GoDotTest Running Tests](https://deepwiki.com/chickensoft-games/GoDotTest/4-running-tests) - CLI invocation format and TestEnvironment parsing
- [LightMoq GitHub](https://github.com/chickensoft-games/LightMoq) - Moq-like API extensions for LightMock.Generator

### Tertiary (LOW confidence)
- LightMoq version 0.1.0: Found on Chickensoft NuGet profile listing, but unable to verify direct package page. May need validation during `dotnet restore`.
- .NET 10 compatibility with GoDotTest: Not explicitly tested per any source. Forward-compatibility expected but unverified.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Chickensoft GameDemo provides exact reference implementation with matching Godot SDK version (4.6.1)
- Architecture: HIGH - Conditional compilation pattern is well-documented and battle-tested across multiple Chickensoft projects
- Pitfalls: HIGH - Each pitfall verified against official sources (Godot issues, forum posts, NuGet docs)
- Export exclusion: MEDIUM - Three-layer approach synthesized from multiple sources; individual layers are well-documented but combined approach is a recommendation
- LightMoq specifics: LOW - Package name/version from NuGet profile listing only; needs validation during implementation

**Research date:** 2026-03-07
**Valid until:** 2026-04-07 (stable ecosystem, 30-day validity)
