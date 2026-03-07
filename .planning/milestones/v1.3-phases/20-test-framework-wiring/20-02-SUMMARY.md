---
phase: 20-test-framework-wiring
plan: 02
subsystem: testing
tags: [godottest, shouldly, test-runner, smoke-test, cli-testing, headless]

# Dependency graph
requires:
  - phase: 20-01
    provides: NuGet packages, conditional compilation, RUN_TESTS define
provides:
  - TestRunner.tscn scene for hosting test execution
  - TestRunner.cs script with CLI arg parsing and GoDotTest invocation
  - HousingTests.cs smoke test proving framework discovers and executes tests
  - Domain-organized test directory structure (Housing, Mood, Economy, Save)
affects: [21, 22, 23, 24, 25]

# Tech tracking
tech-stack:
  added: []
  patterns: [test-class-extends-TestClass, chickensoft-godottest-namespace, domain-organized-test-dirs]

key-files:
  created: [Tests/TestRunner.tscn, Tests/TestRunner.cs, Tests/Housing/HousingTests.cs]
  modified: []

key-decisions:
  - "Used Chickensoft.GoDotTest namespace (not GoDotTest) matching v2.0.30 DLL exports"
  - "Used OrbitalRings.Autoloads namespace for HousingManager (actual codebase namespace, not root)"

patterns-established:
  - "Test classes use 'using Chickensoft.GoDotTest;' for TestClass, [Test], GoTest, TestEnvironment"
  - "Test CLI: godot res://Tests/TestRunner.tscn --run-tests --quit-on-finish --headless"
  - "Domain test directories mirror production: Tests/Housing/, Tests/Mood/, etc."

requirements-completed: [FRMW-01, FRMW-02, FRMW-03]

# Metrics
duration: 3min
completed: 2026-03-07
---

# Phase 20 Plan 02: Test Runner + Smoke Test Summary

**GoDotTest test runner scene with CLI headless execution and Shouldly smoke test proving ComputeCapacity assertion passes end-to-end**

## Performance

- **Duration:** 3 min
- **Started:** 2026-03-07T09:31:46Z
- **Completed:** 2026-03-07T09:35:16Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments
- TestRunner.tscn/cs wired with GoDotTest behind #if RUN_TESTS, using CallDeferred pattern for scene tree readiness
- HousingTests smoke test exercises HousingManager.ComputeCapacity with Shouldly assertion, verified passing via CLI
- Full end-to-end CLI test execution confirmed: `godot res://Tests/TestRunner.tscn --run-tests --quit-on-finish --headless` exits with code 0, reporting Passed: 1 | Failed: 0 | Skipped: 0
- Domain test directories (Mood, Economy, Save) created with .gitkeep for future phases

## Task Commits

Each task was committed atomically:

1. **Task 1: Create test runner scene and script** - `ae6e5b4` (feat)
2. **Task 2: Create smoke test and verify end-to-end test execution** - `76e2f70` (feat)

## Files Created/Modified
- `Tests/TestRunner.tscn` - Minimal Node2D scene with TestRunner.cs script attached
- `Tests/TestRunner.cs` - Test runner with #if RUN_TESTS gated GoDotTest integration
- `Tests/Housing/HousingTests.cs` - Smoke test with ComputeCapacity Shouldly assertion + commented deliberate failure
- `Tests/Mood/.gitkeep` - Placeholder for future mood system tests
- `Tests/Economy/.gitkeep` - Placeholder for future economy tests
- `Tests/Save/.gitkeep` - Placeholder for future save/load tests

## Decisions Made
- Used `Chickensoft.GoDotTest` namespace (not `GoDotTest`) -- the research examples from the README mixed old and new patterns, but the actual DLL exports v2.0.30 types under `Chickensoft.GoDotTest`
- Used `OrbitalRings.Autoloads` for HousingManager import -- the plan assumed root namespace but actual code uses `namespace OrbitalRings.Autoloads;`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Corrected GoDotTest namespace from `GoDotTest` to `Chickensoft.GoDotTest`**
- **Found during:** Task 1 (TestRunner.cs creation)
- **Issue:** Plan specified `using GoDotTest;` but v2.0.30 DLL exports types under `Chickensoft.GoDotTest` namespace
- **Fix:** Changed to `using Chickensoft.GoDotTest;` in TestRunner.cs
- **Files modified:** Tests/TestRunner.cs
- **Verification:** `dotnet build` succeeds
- **Committed in:** ae6e5b4 (Task 1 commit)

**2. [Rule 1 - Bug] Corrected HousingManager namespace from `OrbitalRings` to `OrbitalRings.Autoloads`**
- **Found during:** Task 2 (HousingTests.cs creation)
- **Issue:** Plan specified `using OrbitalRings;` for HousingManager but actual file uses `namespace OrbitalRings.Autoloads;`
- **Fix:** Used `using OrbitalRings.Autoloads;` in HousingTests.cs
- **Files modified:** Tests/Housing/HousingTests.cs
- **Verification:** `dotnet build` succeeds, test passes via CLI
- **Committed in:** 76e2f70 (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (2 bugs - incorrect namespaces)
**Impact on plan:** Both auto-fixes necessary for correctness. No scope creep.

## Issues Encountered
None

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Complete test infrastructure is wired and proven: packages restore, code compiles conditionally, test runner discovers tests, CLI execution works headless with exit code 0
- Phase 21 can build integration test infrastructure (singleton reset, event cleanup) on top of this foundation
- Phases 22-24 can add domain test classes following the HousingTests pattern

## Self-Check: PASSED

All artifacts verified:
- Tests/TestRunner.tscn: FOUND
- Tests/TestRunner.cs: FOUND
- Tests/Housing/HousingTests.cs: FOUND
- Tests/Mood/.gitkeep: FOUND
- Tests/Economy/.gitkeep: FOUND
- Tests/Save/.gitkeep: FOUND
- 20-02-SUMMARY.md: FOUND
- Commit ae6e5b4: FOUND
- Commit 76e2f70: FOUND
- GoTest.RunTests pattern: VERIFIED
- ShouldBe assertion: VERIFIED
- ComputeCapacity usage: VERIFIED
- Deliberate failure comment: VERIFIED

---
*Phase: 20-test-framework-wiring*
*Completed: 2026-03-07*
