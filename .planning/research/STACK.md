# Technology Stack: Testing Infrastructure

**Project:** Orbital Rings v1.3 Testing Milestone
**Researched:** 2026-03-07
**Focus:** GoDotTest + GodotTestDriver integration for existing Godot 4 C# game

## Current Project Stack (Verified from .csproj and project.godot)

| Technology | Version | Notes |
|------------|---------|-------|
| Godot Engine | 4.6 | config/features in project.godot |
| Godot.NET.Sdk | 4.6.1 | From .csproj |
| .NET Target | net10.0 | From .csproj TargetFramework |
| NuGet Sources | Local GodotSharp only | `/usr/local/bin/GodotSharp/Tools/nupkgs` |

**Important:** The milestone context references Godot 4.4 / .NET 8, but the actual project has evolved to Godot 4.6 / .NET 10.0. All package recommendations below are verified compatible with this stack.

## Recommended Test Stack

### Core Testing Packages

| Package | Version | Purpose | Why |
|---------|---------|---------|-----|
| Chickensoft.GoDotTest | 2.0.30 | Test runner, discovery, execution | The only C#-native test runner designed to run inside Godot's scene tree. Uses reflection to find TestClass subclasses, runs tests sequentially (no race conditions), supports CLI flags for CI. Chickensoft is the de facto standard for Godot C# testing. |
| Chickensoft.GodotTestDriver | 3.1.62 | Integration test utilities | Provides Fixture lifecycle management, input simulation, frame-waiting helpers, and pre-built node drivers. Essential for testing anything that touches the scene tree (housing assignment UI, citizen nodes, save/load round-trips). Same Chickensoft ecosystem as GoDotTest. |
| Shouldly | 4.3.0 | Assertion library | Readable assertion syntax (`result.ShouldBe(expected)`) with clear failure messages. Lightweight, zero dependencies, no Godot coupling. Preferred over FluentAssertions (heavier, licensing changes in 2024) and over raw `if/throw` (unreadable, poor error messages). |

### Why NOT These Alternatives

| Category | Rejected | Why Not |
|----------|----------|---------|
| Test Runner | gdUnit4 | GDScript-first design, heavier plugin installation (Godot addon + NuGet), adds editor UI panels this project does not need. GoDotTest is lighter and C#-native. |
| Test Runner | xUnit/NUnit via dotnet test | Cannot run inside Godot's scene tree. Tests needing SceneTree, nodes, or Godot APIs would fail. GoDotTest solves this by running tests as a Godot scene. |
| Assertions | FluentAssertions | License changed to paid for commercial use in late 2024. Shouldly is MIT, lighter, and the assertion syntax is comparable. |
| Assertions | GoDotTest built-in | GoDotTest provides NO assertions by design -- it is only a test runner. An assertion library is required. |
| Mocking | LightMock.Generator (1.2.3) | Source generator for compile-time mocks. NOT needed for this milestone. The test targets are POCO classes (MoodSystem), Autoload singletons (concrete classes), and data round-trips (SaveManager). None of these require mocking -- they can be tested directly by constructing instances or calling methods. Adding mocking infrastructure adds complexity with no benefit for these test suites. Revisit if future milestones need interface-based testing. |
| Mocking | Moq | Reflection-based mocking has known compatibility issues with Godot's AOT-hostile assembly loading. Also overkill for this milestone's scope. |
| Integration | Separate test .csproj | Some teams create a second .csproj for tests. This adds build complexity and makes it harder to access internal types. Since GoDotTest runs in the same assembly, a single .csproj with tests in a `test/` folder is simpler and sufficient for a project this size (~9.5K LOC). |

## NuGet Configuration Change Required

The current `NuGet.Config` only has a local GodotSharp source. The testing packages live on nuget.org and require adding the official NuGet source.

**Updated NuGet.Config:**

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="godot-local" value="/usr/local/bin/GodotSharp/Tools/nupkgs" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

## .csproj Changes Required

Add to the existing `Orbital Rings.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Chickensoft.GoDotTest" Version="2.0.30" />
  <PackageReference Include="Chickensoft.GodotTestDriver" Version="3.1.62" />
  <PackageReference Include="Shouldly" Version="4.3.0" />
</ItemGroup>
```

**No other .csproj changes needed.** The project already targets `net10.0` which is forward-compatible with these packages (they target `netstandard2.1` or `net8.0`). The `EnableDynamicLoading` is already `true`.

## Test Runner Scene Setup

GoDotTest requires a dedicated test scene with a script that bootstraps test discovery and execution. Two setup patterns exist:

### Pattern A: Separate Test Scene (Recommended for This Project)

Create `test/Tests.tscn` with a `Node2D` root and attach this script:

```csharp
using System.Reflection;
using Godot;
using GoDotTest;

public partial class Tests : Node2D
{
    public override async void _Ready()
        => await GoTest.RunTests(Assembly.GetExecutingAssembly(), this);
}
```

Run tests by playing this scene directly (F6 in editor) or from CLI:

```bash
$GODOT --run-tests --quit-on-finish
```

### Pattern B: Conditional Main Scene (For CI/CD)

Modify the existing main scene entry point to detect test flags and branch:

```csharp
public partial class Main : Node
{
    public override async void _Ready()
    {
        var environment = TestEnvironment.From(OS.GetCmdlineArgs());

        if (environment.ShouldRunTests)
        {
            await GoTest.RunTests(Assembly.GetExecutingAssembly(), this, environment);
        }
        else
        {
            GetTree().ChangeSceneToFile("res://Scenes/TitleScreen/TitleScreen.tscn");
        }
    }
}
```

**Recommendation: Use Pattern A.** The project launches from `TitleScreen.tscn` directly, and modifying the launch flow adds risk to the shipped game. A separate test scene is isolated, can be run with F6, and avoids touching production code. Pattern B can be adopted later if CI/CD integration needs a single entry point.

## Test Class Structure

```csharp
using Godot;
using GoDotTest;
using Shouldly;

public class MoodSystemTest : TestClass
{
    public MoodSystemTest(Node testScene) : base(testScene) { }

    [SetupAll]
    public void SetupAll()
    {
        // One-time setup before all tests in this class
    }

    [Setup]
    public void Setup()
    {
        // Runs before each [Test] method
    }

    [Test]
    public void MoodDecaysOverTime()
    {
        var mood = new MoodSystem();
        mood.AddMood(1.0f);
        mood.Update(10.0f); // simulate 10 seconds
        mood.CurrentMood.ShouldBeLessThan(1.0f);
    }

    [Cleanup]
    public void Cleanup()
    {
        // Runs after each [Test] method
    }

    [CleanupAll]
    public void CleanupAll()
    {
        // One-time cleanup after all tests in this class
    }
}
```

### Available Test Attributes

| Attribute | When It Runs | Use For |
|-----------|-------------|---------|
| `[SetupAll]` | Once before first test | Creating shared fixtures, loading configs |
| `[Setup]` | Before each `[Test]` | Resetting state for isolation |
| `[Test]` | The test itself | Assertions, the actual test logic |
| `[Cleanup]` | After each `[Test]` | Freeing nodes, resetting singletons |
| `[CleanupAll]` | Once after last test | Disposing shared resources |
| `[Failure]` | When any test fails | Collecting debug info on failure |

### Execution Order

Tests run **sequentially in declaration order** within a class. No parallelism, no randomization. This is intentional -- Godot is not thread-safe, and deterministic ordering prevents flaky tests.

## GodotTestDriver Usage for Integration Tests

GodotTestDriver provides `Fixture` for managing node lifecycle in tests:

```csharp
using Godot;
using GoDotTest;
using GodotTestDriver;
using Shouldly;

public class SaveLoadIntegrationTest : TestClass
{
    private Fixture _fixture = default!;

    public SaveLoadIntegrationTest(Node testScene) : base(testScene) { }

    [SetupAll]
    public void SetupAll()
    {
        _fixture = new Fixture(TestScene.GetTree());
    }

    [CleanupAll]
    public async Task CleanupAll()
    {
        await _fixture.Cleanup();
    }

    [Test]
    public async Task SaveAndLoadPreservesHousing()
    {
        // Fixture auto-frees nodes after test
        var node = _fixture.AutoFree(new Node());
        TestScene.AddChild(node);

        // ... test save/load round-trip
    }
}
```

**Key GodotTestDriver capabilities relevant to this project:**

| Feature | Use Case in Orbital Rings |
|---------|--------------------------|
| `Fixture.AutoFree()` | Prevent node leaks in tests that add nodes to scene tree |
| `Fixture.Cleanup()` | Ensure all test-created nodes are freed between suites |
| Frame waiting (`WithinSeconds()`) | Wait for async citizen behaviors, debounced save |
| Input simulation | Future: testing segment clicking, build panel interaction |

## CLI Arguments Reference

| Flag | Purpose | When to Use |
|------|---------|-------------|
| `--run-tests` | Enable test mode | Always |
| `--run-tests=SuiteName` | Run single suite | Debugging one test class |
| `--run-tests=Suite.Method` | Run single test | Debugging one test method |
| `--quit-on-finish` | Exit Godot after tests | CI/CD, batch runs |
| `--stop-on-error` | Halt on first failure | Debugging failures |
| `--sequential` | Skip remaining tests in suite on failure | When tests depend on prior test state |
| `--coverage` | Enable coverlet integration | CI/CD coverage reports |

## VSCode Debug Configuration

Add to `.vscode/launch.json` for test debugging:

```json
{
    "name": "Debug Tests",
    "type": "coreclr",
    "request": "launch",
    "preLaunchTask": "build",
    "program": "${env:GODOT}",
    "args": [
        "--run-tests",
        "--quit-on-finish",
        "--path",
        "${workspaceFolder}"
    ],
    "cwd": "${workspaceFolder}",
    "stopAtEntry": false
}
```

## Recommended Directory Structure

```
test/
  Tests.tscn              # Test runner scene (Pattern A)
  Tests.cs                 # Test runner script
  SaveLoadTest.cs          # Save/load round-trip tests
  HousingTest.cs           # Housing assignment tests
  EconomyTest.cs           # Economy calculation tests
  MoodSystemTest.cs        # Mood decay and tier tests
  WishFulfillmentTest.cs   # Wish loop tests
  Fixtures/                # Shared test data/helpers (if needed)
```

**Why `test/` at project root (not `Tests/` or inside `Scripts/`):**
- Mirrors Chickensoft conventions
- Keeps test code visually separate from production code
- Still in the same assembly (single .csproj) so tests can access all types
- Lowercase `test/` follows Chickensoft convention

## What NOT to Add

| Do NOT Add | Why |
|------------|-----|
| Separate test .csproj | Adds build complexity, prevents access to internal types, overkill for ~10K LOC project |
| Moq or NSubstitute | Reflection-based mocking has Godot compatibility issues. Not needed -- test targets are concrete classes. |
| LightMock.Generator | Source-gen mocking is the right approach IF you need mocks. This milestone tests POCOs and singletons directly. Add later if needed. |
| FluentAssertions | License concerns (commercial use restrictions since late 2024). Shouldly covers the same ground with MIT license. |
| Code coverage tooling (coverlet) | Nice-to-have but not in scope for v1.3. The `--coverage` flag is available when ready to add it. |
| gdUnit4 | Plugin-based, GDScript-first, adds editor panels and complexity. GoDotTest is leaner for C#-only projects. |
| Test parallelization | GoDotTest intentionally runs sequentially. Godot is not thread-safe. Do not try to work around this. |
| xUnit/NUnit attributes | GoDotTest has its own attribute system (`[Test]`, `[Setup]`, etc.). Do not mix test frameworks. |

## Package Version Confidence

| Package | Version | Confidence | Source |
|---------|---------|------------|--------|
| Chickensoft.GoDotTest | 2.0.30 | HIGH | NuGet API flatcontainer index verified 2026-03-07 (101 versions tracked, 2.0.30 is latest) |
| Chickensoft.GodotTestDriver | 3.1.62 | HIGH | NuGet API flatcontainer index verified 2026-03-07 (67 versions tracked, 3.1.62 is latest) |
| Shouldly | 4.3.0 | HIGH | NuGet API flatcontainer index verified 2026-03-07 (latest stable) |

## Compatibility Notes

1. **net10.0 target:** All three packages target `netstandard2.1` or `net8.0`, which are forward-compatible with `net10.0`. No TFM issues expected.

2. **Godot.NET.Sdk 4.6.1:** GoDotTest 2.x works with Godot 4.x. The 2.0.x line removed Godot-version-specific prerelease tags (those were in the 1.x line), indicating version-agnostic compatibility with Godot 4.x.

3. **NuGet source:** The project MUST add `nuget.org` to `NuGet.Config`. Without this, `dotnet restore` will fail to find the testing packages since the project currently only has a local GodotSharp source.

4. **Single assembly:** Tests and game code live in the same .csproj. This is the intended GoDotTest pattern -- it uses `Assembly.GetExecutingAssembly()` to discover test classes in the running assembly.

5. **Known Godot 4.6 / .NET 10 concern:** There is an open issue (godotengine/godot#112701) about Godot 4.5-mono failing to probe shared framework assemblies on .NET 10 on Windows. This affects `Microsoft.Extensions.*` assemblies specifically, not standalone NuGet packages like the ones recommended here. The testing packages do not depend on `Microsoft.Extensions.*`, so this issue should not block adoption. Flag for validation during initial setup.

## Installation Commands

```bash
# Add nuget.org source (required -- project currently only has local GodotSharp)
dotnet nuget add source https://api.nuget.org/v3/index.json --name nuget.org --configfile NuGet.Config

# Add testing packages
dotnet add "Orbital Rings.csproj" package Chickensoft.GoDotTest --version 2.0.30
dotnet add "Orbital Rings.csproj" package Chickensoft.GodotTestDriver --version 3.1.62
dotnet add "Orbital Rings.csproj" package Shouldly --version 4.3.0

# Verify restore succeeds
dotnet restore "Orbital Rings.csproj"
```

## Sources

- [Chickensoft GoDotTest GitHub](https://github.com/chickensoft-games/GoDotTest) - Test runner documentation, setup guide, CLI flags
- [Chickensoft GodotTestDriver GitHub](https://github.com/chickensoft-games/GodotTestDriver) - Fixture, drivers, input simulation
- [GoDotTest NuGet](https://www.nuget.org/packages/Chickensoft.GoDotTest/) - Version 2.0.30 (latest stable)
- [GodotTestDriver NuGet](https://www.nuget.org/packages/Chickensoft.GodotTestDriver/) - Version 3.1.62 (latest stable)
- [Shouldly NuGet](https://www.nuget.org/packages/Shouldly/) - Version 4.3.0 (latest stable)
- [DeepWiki GoDotTest Running Tests](https://deepwiki.com/chickensoft-games/GoDotTest/4-running-tests) - Complete test runner configuration
- [LightMock.Generator NuGet](https://www.nuget.org/packages/LightMock.Generator) - Version 1.2.3 (evaluated, not recommended for this milestone)
- [Godot .NET 10 Issue](https://github.com/godotengine/godot/issues/112701) - Shared framework assembly probing issue on Windows

---

*Stack research for: Orbital Rings v1.3 -- Testing infrastructure*
*Researched: 2026-03-07*
