# Phase 20: Test Framework Wiring - Context

**Gathered:** 2026-03-07
**Status:** Ready for planning

<domain>
## Phase Boundary

Test infrastructure exists and proves it works. A test runner discovers and executes test classes, runs headless from CLI, and excludes test code from release builds. NuGet packages (GoDotTest, GodotTestDriver, Shouldly, LightMoq) restore without manual intervention.

</domain>

<decisions>
## Implementation Decisions

### Test file placement
- Tests live in `Tests/` at project root (not inside `Scripts/`)
- Organized by domain: `Tests/Mood/`, `Tests/Economy/`, `Tests/Housing/`, `Tests/Save/`
- Test runner scene at `Tests/TestRunner.tscn`
- Test files named `{System}Tests.cs` (e.g., `MoodSystemTests.cs`, `HousingTests.cs`)

### Smoke test design
- One real passing test using `ComputeCapacity` — proves framework works AND exercises game logic
- One commented-out deliberate failure with instructions to uncomment for verifying Shouldly error readability
- Minimal scope: one passing method + one commented failure block in a single test class

### Test runner experience
- Supports both in-editor launch (F5 on test scene) and CLI execution
- CLI invocation: `godot --run-tests --quit-on-finish` with custom arg parsing in test runner
- Output to stdout only — no log files, no visual UI in editor
- Test results print to Godot Output panel when run in editor

### Namespace structure
- Mirror production domains: `OrbitalRings.Tests.Mood`, `OrbitalRings.Tests.Economy`, etc.
- Test classes extend GoDotTest's `TestClass` base directly (no custom project base class)
- Test methods use `[Test]` attribute for discovery
- Shouldly assertions used directly (`value.ShouldBe(expected)`) — no project wrappers

### Test dependencies
- GoDotTest, GodotTestDriver, Shouldly, and LightMoq added as NuGet packages
- LightMoq (Chickensoft) wired as package only in Phase 20 — actual mock usage in later phases
- NuGet.Config updated to include nuget.org alongside existing godot-local source

### Claude's Discretion
- Exact GoDotTest test runner scene node structure
- How CLI args (`--run-tests`, `--quit-on-finish`) are parsed and routed
- Export exclusion mechanism (`.gdignore`, export preset filter, or conditional compilation)
- Which ComputeCapacity assertion to use for the smoke test

</decisions>

<specifics>
## Specific Ideas

- User specifically requested LightMoq (chickensoft-games/LightMoq) — source-generator-based mocking that works with Godot's collectible assemblies, unlike Moq
- REQUIREMENTS.md lists "Mocking framework (Moq-style)" as out of scope due to singletons using static instances, but LightMoq is wired for future phases where interfaces may be introduced

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `HousingManager.ComputeCapacity(int segments)`: Pure static method, ideal smoke test target
- `EconomyManager.CalculateRoomCost()`, `CalculateTickIncome()`, `CalculateDemolishRefund()`: Pure calculations for later test phases

### Established Patterns
- 8 autoload singletons registered in `project.godot` — tests will need to be aware of singleton lifecycle
- All production code under `Scripts/` with domain subdirectories (`Autoloads/`, `Build/`, `Citizens/`, etc.)
- Root namespace `OrbitalRings` defined in `.csproj`
- Single `.csproj` (no separate test project) — tests compile in same assembly for internal member access

### Integration Points
- `Orbital Rings.csproj`: Add PackageReference entries for test packages
- `NuGet.Config`: Add nuget.org package source
- `project.godot`: No autoload changes needed — test runner is a standalone scene
- Export presets: Tests/ directory must be excluded from release builds

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 20-test-framework-wiring*
*Context gathered: 2026-03-07*
