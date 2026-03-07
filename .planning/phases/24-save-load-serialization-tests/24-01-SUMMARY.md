---
phase: 24-save-load-serialization-tests
plan: 01
subsystem: testing
tags: [unit-tests, save-load, json-serialization, system-text-json, backward-compat, shouldly, godottest]

# Dependency graph
requires:
  - phase: 20-test-framework-wiring
    provides: GoDotTest + Shouldly test framework, TestRunner scene, TestClass base
provides:
  - 4 save data serialization tests covering v3 round-trip, v1/v2 backward compatibility, and empty-collections edge case
  - Regression safety for SaveData, SavedRoom, SavedCitizen POCO serialization integrity
affects: [25-housing-integration-tests]

# Tech tracking
tech-stack:
  added: []
  patterns: [pure POCO serialization testing with System.Text.Json, inline JSON string literals for legacy format verification]

key-files:
  created: [Tests/Save/SaveDataTests.cs]
  modified: []

key-decisions:
  - "Used TestClass base (not GameTestClass) for pure POCO tests with no singleton interaction"
  - "Used exact float equality (no tolerance) since System.Text.Json preserves float values exactly through JSON round-trip"
  - "Inline raw string literal JSON for v1/v2 backward compat tests -- most readable, you see exactly what's being deserialized"

patterns-established:
  - "CreateTestSaveData() helper for rich, fully-populated test data -- single source of truth"
  - "Inline JSON string literals for legacy format backward compatibility testing"
  - "Field-by-field Shouldly assertions on all properties including nested objects"

requirements-completed: [SAVE-01, SAVE-02, SAVE-03]

# Metrics
duration: 34min
completed: 2026-03-07
---

# Phase 24 Plan 01: Save/Load Serialization Tests Summary

**4 JSON serialization tests verifying SaveData v3 round-trip integrity, v1/v2 backward compatibility with correct C# defaults, and empty-collection preservation**

## Performance

- **Duration:** 34 min
- **Started:** 2026-03-07T15:08:37Z
- **Completed:** 2026-03-07T15:43:12Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments
- Created Tests/Save/SaveDataTests.cs with 4 test methods covering SAVE-01, SAVE-02, SAVE-03, and empty-collections edge case
- V3RoundTripPreservesAllFields: field-by-field verification of all 7 scalar SaveData fields, 2 SavedRooms (5 fields each), 2 SavedCitizens (12 fields each including nullable HomeSegmentIndex), 2 dictionaries, and 1 list
- V1/V2 backward compat tests verify legacy JSON payloads deserialize with correct defaults (LifetimeHappiness=0, Mood=0f, MoodBaseline=0f, HomeSegmentIndex=null)
- Full test suite passes: 77 tests (73 existing + 4 new), 0 failures, exit code 0

## Task Commits

Each task was committed atomically:

1. **Task 1: Create SaveDataTests with v3 round-trip, v1/v2 backward compat, and empty-collections tests** - `4789c1c` (test)

## Files Created/Modified
- `Tests/Save/SaveDataTests.cs` - 4 test methods: V3RoundTripPreservesAllFields, V1JsonDeserializesWithCorrectDefaults, V2JsonDeserializesWithCorrectDefaults, EmptyCollectionsRoundTripAsNonNull

## Decisions Made
- Used TestClass base (not GameTestClass) -- pure POCO tests with no singleton interaction needed
- Used exact float equality (no tolerance) since System.Text.Json preserves float values exactly through JSON round-trip
- Used inline raw string literal JSON for v1/v2 backward compat tests for maximum readability
- Used production JsonSerializerOptions (WriteIndented = true) to mirror SaveManager.PerformSave() exactly

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- Godot test runner requires the TestRunner.tscn scene to be passed explicitly as argument (`godot --headless --run-tests --quit-on-finish res://Tests/TestRunner.tscn`); without it, the TitleScreen main scene loads and hangs indefinitely. The earlier test phases also used this approach but the plan's verify command omitted the scene argument.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- All save data serialization tests passing, regression-proofing the SaveData, SavedRoom, and SavedCitizen POCO serialization
- Ready for Phase 25 (Housing Integration Tests)

## Self-Check: PASSED

- FOUND: Tests/Save/SaveDataTests.cs (4 [Test] methods)
- FOUND: commit 4789c1c
- Test suite: 77 passed, 0 failed

---
*Phase: 24-save-load-serialization-tests*
*Completed: 2026-03-07*
