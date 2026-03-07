# Phase 24: Save/Load Serialization Tests - Context

**Gathered:** 2026-03-07
**Status:** Ready for planning

<domain>
## Phase Boundary

Save data integrity is regression-proof — JSON round-trips preserve all fields and legacy formats deserialize with correct defaults. Covers requirements SAVE-01 through SAVE-03 plus empty-collection edge case. Pure POCO serialization tests only (no singletons, no scene tree, no file I/O).

</domain>

<decisions>
## Implementation Decisions

### Legacy JSON format
- Inline string literals directly in test methods — most readable, you see exactly what's being deserialized next to the assertions
- Minimal but complete payloads: include all fields that existed in that version, with 1 room, 1 citizen, 1 wish
- v1 JSON explicitly sets `"Version": 1` (mirrors real v1 saves; production ApplyState checks `data.Version >= 2`)
- v2 JSON explicitly sets `"Version": 2` with v2 fields present but HomeSegmentIndex omitted
- No forward-compatibility test (Version: 99) — stick to the three real formats

### Round-trip data richness
- Rich but readable: 2-3 rooms, 2-3 citizens (one with HomeSegmentIndex set, one with null), 1-2 active wishes, some unlocked rooms
- Construct SaveData programmatically in C#, serialize to JSON, deserialize back, compare — tests the actual POCO → JSON → POCO path
- Private helper method `CreateTestSaveData()` returns a fully-populated SaveData — single source of truth, consistent with CreateMoodSystem() pattern from Phase 22
- Use production JsonSerializerOptions (WriteIndented = true) to mirror SaveManager.PerformSave() exactly

### Edge cases
- Empty collections: SaveData with empty PlacedRooms, Citizens, ActiveWishes, PlacedRoomTypes — verify non-null AND empty (ShouldNotBeNull + ShouldBeEmpty) to catch removed initializers
- Null HomeSegmentIndex covered naturally by the round-trip test (CreateTestSaveData includes one citizen with null, one with a value)
- No corrupted/malformed JSON test — that's SaveManager.Load() integration concern, out of scope
- No float precision edge case test — covered implicitly by round-trip with exact equality

### Assertion approach
- Field-by-field on every property including nested objects (SavedRoom, SavedCitizen) — crystal-clear what broke if a test fails
- Exact equality for float fields (color components, WalkwayAngle, Direction) — System.Text.Json preserves float values exactly, no tolerance needed
- Legacy format tests verify BOTH version-specific defaults AND that fields present in all versions deserialize correctly — complete picture per test
- Comment headers group tests by requirement: `// --- SAVE-01: v3 Round-Trip ---`, `// --- SAVE-02: v1 Backward Compat ---`, `// --- SAVE-03: v2 Backward Compat ---`, `// --- Edge Cases ---`

### Claude's Discretion
- Exact field values used in CreateTestSaveData() (room IDs, citizen names, color values, angles)
- Exact field values used in inline v1/v2 JSON strings
- Test method ordering within each requirement group
- Whether to assert collection counts before element-level assertions

</decisions>

<specifics>
## Specific Ideas

- Extends `TestClass` directly (not `GameTestClass`) — pure POCO, no singleton reset needed
- Namespace: `OrbitalRings.Tests.Save`
- File: `Tests/Save/SaveDataTests.cs` (replaces `.gitkeep` if present)
- Shouldly assertions used directly, no wrappers
- Behavior-focused method names: `V3RoundTripPreservesAllFields`, `V1JsonDeserializesWithCorrectDefaults`, `V2JsonDeserializesWithCorrectDefaults`, `EmptyCollectionsRoundTripAsNonNull`

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `SaveData`, `SavedRoom`, `SavedCitizen`: POCOs defined in SaveManager.cs — all plain C# types, no Godot dependencies for serialization
- `System.Text.Json.JsonSerializer`: Used by SaveManager for serialize/deserialize — same API used in tests
- `JsonSerializerOptions { WriteIndented = true }`: Production serialization options from SaveManager.PerformSave()

### Established Patterns
- Tests/Mood/MoodSystemTests.cs: CreateMoodSystem() helper pattern, comment-header grouping, behavior-focused naming
- Tests/Economy/EconomyTests.cs: pre-computed expected values, comment headers by topic
- TestClass base (not GameTestClass): for POCO tests that don't need singleton reset
- Single .csproj compilation: test code has internal member access to SaveData types

### Integration Points
- Tests/Save/SaveDataTests.cs: new file in existing Tests/Save/ directory
- No production code changes needed — SaveData, SavedRoom, SavedCitizen are already public POCOs
- JsonSerializer API from System.Text.Json (already referenced in project)

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 24-save-load-serialization-tests*
*Context gathered: 2026-03-07*
