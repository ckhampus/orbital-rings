---
phase: 20-test-framework-wiring
plan: 01
subsystem: testing
tags: [nuget, msbuild, conditional-compilation, godottest, shouldly, lightmoq]

# Dependency graph
requires: []
provides:
  - NuGet.Config with nuget.org package source for test package resolution
  - Conditional test compilation via RunTests property (Debug/ExportDebug only)
  - RUN_TESTS preprocessor define for test-only code paths
  - Tests/.gdignore for Godot export exclusion
affects: [20-02, 21, 22, 23, 24, 25]

# Tech tracking
tech-stack:
  added: [Chickensoft.GoDotTest 2.0.30, Chickensoft.GodotTestDriver 3.1.62, Shouldly 4.3.0, LightMoq 0.1.0, LightMock.Generator 1.2.3]
  patterns: [conditional-compilation-via-RunTests, test-code-exclusion-via-Compile-Remove, gdignore-export-exclusion]

key-files:
  created: [Tests/.gdignore]
  modified: [NuGet.Config, Orbital Rings.csproj]

key-decisions:
  - "Kept all five test package references explicit (including LightMock.Generator even though transitive) for clarity"
  - "Used SkipTests escape hatch so Debug builds can optionally skip test compilation"

patterns-established:
  - "RunTests property: false by default, true in Debug/ExportDebug (unless SkipTests=true)"
  - "Test code exclusion: Compile Remove Tests/**/*.cs when RunTests != true"
  - "RUN_TESTS define: available for #if RUN_TESTS preprocessor guards in production code"

requirements-completed: [FRMW-04, FRMW-05]

# Metrics
duration: 2min
completed: 2026-03-07
---

# Phase 20 Plan 01: NuGet + Conditional Test Compilation Summary

**NuGet.org package source with MSBuild conditional compilation wiring five test packages (GoDotTest, GodotTestDriver, Shouldly, LightMoq, LightMock.Generator) for Debug/ExportDebug only**

## Performance

- **Duration:** 2 min
- **Started:** 2026-03-07T09:26:34Z
- **Completed:** 2026-03-07T09:28:45Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- NuGet.Config extended with nuget.org source alongside existing godot-local, enabling test package resolution
- .csproj wired with RunTests conditional compilation: test packages only in Debug/ExportDebug, test files excluded from release builds
- Tests/.gdignore created to prevent Godot from importing test scene files into exports
- Verified: ExportRelease build succeeds with RunTests=false, Debug build succeeds with RunTests=true and RUN_TESTS define active

## Task Commits

Each task was committed atomically:

1. **Task 1: Add nuget.org package source and configure conditional test compilation** - `c707116` (feat)
2. **Task 2: Verify export exclusion works correctly** - verification-only task, no file changes to commit

## Files Created/Modified
- `NuGet.Config` - Added nuget.org package source alongside godot-local
- `Orbital Rings.csproj` - Added RunTests property, conditional DefineConstants, five test PackageReferences, and Compile Remove for test exclusion
- `Tests/.gdignore` - Empty file preventing Godot resource import from Tests directory

## Decisions Made
- Kept all five test package references explicit (including LightMock.Generator even though it may be transitive via LightMoq) for clarity and version pinning
- Used SkipTests property as escape hatch allowing Debug builds to optionally skip test compilation when needed

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Build infrastructure is in place for all test code
- Test packages resolve correctly from nuget.org
- Plan 20-02 can now create test runner scene, TestMain entry point, and example tests that compile against this infrastructure
- Any .cs files placed in Tests/ will automatically compile in Debug/ExportDebug and be excluded from ExportRelease

## Self-Check: PASSED

All artifacts verified:
- NuGet.Config: FOUND
- Orbital Rings.csproj: FOUND
- Tests/.gdignore: FOUND
- 20-01-SUMMARY.md: FOUND
- Commit c707116: FOUND

---
*Phase: 20-test-framework-wiring*
*Completed: 2026-03-07*
