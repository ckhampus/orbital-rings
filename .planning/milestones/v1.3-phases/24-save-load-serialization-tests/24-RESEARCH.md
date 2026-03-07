# Phase 24: Save/Load Serialization Tests - Research

**Researched:** 2026-03-07
**Domain:** System.Text.Json POCO serialization, backward-compatible deserialization, unit testing
**Confidence:** HIGH

## Summary

Phase 24 tests SaveData serialization integrity: v3 round-trips preserve all fields exactly, and legacy v1/v2 JSON payloads deserialize with correct defaults for missing fields. All three POCO types (SaveData, SavedRoom, SavedCitizen) are plain C# with no Godot dependencies, making this a pure unit test phase with zero singleton or scene tree interaction.

The save format has evolved through three versions: v1 (original), v2 (added LifetimeHappiness, Mood, MoodBaseline to SaveData), and v3 (added nullable HomeSegmentIndex to SavedCitizen). System.Text.Json's default behavior makes backward compatibility automatic -- missing JSON properties retain their C# default values (0 for int, 0f for float, null for int?, initialized collections preserved). The tests verify this behavior is regression-proof.

**Primary recommendation:** Single test file extending TestClass with a CreateTestSaveData() helper, inline JSON string literals for v1/v2 payloads, and field-by-field Shouldly assertions using exact equality.

<user_constraints>

## User Constraints (from CONTEXT.md)

### Locked Decisions
- Inline string literals directly in test methods for legacy JSON -- most readable
- Minimal but complete payloads: include all fields that existed in that version, with 1 room, 1 citizen, 1 wish
- v1 JSON explicitly sets `"Version": 1`; v2 JSON explicitly sets `"Version": 2` with v2 fields present but HomeSegmentIndex omitted
- No forward-compatibility test (Version: 99) -- stick to three real formats
- Rich but readable round-trip data: 2-3 rooms, 2-3 citizens (one with HomeSegmentIndex set, one with null), 1-2 active wishes, some unlocked rooms
- Construct SaveData programmatically in C#, serialize to JSON, deserialize back, compare
- Private helper method `CreateTestSaveData()` returns a fully-populated SaveData
- Use production JsonSerializerOptions (WriteIndented = true) to mirror SaveManager.PerformSave() exactly
- Empty collections: SaveData with empty PlacedRooms, Citizens, ActiveWishes, PlacedRoomTypes -- verify non-null AND empty
- No corrupted/malformed JSON test (out of scope -- SaveManager.Load() integration concern)
- No float precision edge case test (covered implicitly by round-trip with exact equality)
- Field-by-field assertions on every property including nested objects
- Exact equality for float fields -- System.Text.Json preserves float values exactly
- Legacy format tests verify BOTH version-specific defaults AND that present fields deserialize correctly
- Comment headers group tests by requirement: `// --- SAVE-01: v3 Round-Trip ---`, etc.
- Extends `TestClass` directly (not `GameTestClass`) -- pure POCO, no singleton reset needed
- Namespace: `OrbitalRings.Tests.Save`
- File: `Tests/Save/SaveDataTests.cs` (replaces `.gitkeep`)
- Shouldly assertions used directly, no wrappers
- Behavior-focused method names: `V3RoundTripPreservesAllFields`, `V1JsonDeserializesWithCorrectDefaults`, `V2JsonDeserializesWithCorrectDefaults`, `EmptyCollectionsRoundTripAsNonNull`

### Claude's Discretion
- Exact field values used in CreateTestSaveData() (room IDs, citizen names, color values, angles)
- Exact field values used in inline v1/v2 JSON strings
- Test method ordering within each requirement group
- Whether to assert collection counts before element-level assertions

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope

</user_constraints>

<phase_requirements>

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| SAVE-01 | SaveData round-trips through JSON serialization without data loss | CreateTestSaveData() helper builds rich v3 SaveData; serialize with production JsonSerializerOptions; deserialize back; field-by-field comparison on all 10 SaveData properties, all 5 SavedRoom properties, all 12 SavedCitizen properties |
| SAVE-02 | v1 format JSON deserializes with correct defaults for missing v2/v3 fields | Inline v1 JSON with Version:1 + all v1-era fields; deserialize to SaveData; verify LifetimeHappiness=0, Mood=0f, MoodBaseline=0f (v2 defaults), HomeSegmentIndex=null (v3 default), plus all v1 fields deserialized correctly |
| SAVE-03 | v2 format JSON deserializes with correct defaults for missing v3 fields | Inline v2 JSON with Version:2 + all v2-era fields but no HomeSegmentIndex; deserialize to SaveData; verify HomeSegmentIndex=null (v3 default), plus all v2 fields deserialized correctly |

</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| System.Text.Json | .NET 10 built-in | JSON serialization/deserialization | Production code uses it in SaveManager.PerformSave()/Load(); no additional dependency |
| Chickensoft.GoDotTest | 2.0.30 | Test framework with [Test], [Setup] attributes | Project standard; TestClass base class provides Godot scene integration |
| Shouldly | 4.3.0 | Fluent assertion library | Project standard; ShouldBe, ShouldNotBeNull, ShouldBeEmpty patterns established |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| TestClass (GoDotTest) | 2.0.30 | Base class for pure POCO tests | Extends directly for tests with no singleton dependencies |

### Alternatives Considered
None -- all libraries are locked by existing project infrastructure.

**No installation needed** -- all packages already referenced in the .csproj.

## Architecture Patterns

### File Structure
```
Tests/
├── Save/
│   ├── .gitkeep          # existing, will be superseded
│   └── SaveDataTests.cs  # NEW -- all SAVE-01/02/03 tests
```

### Pattern 1: CreateTestSaveData() Helper
**What:** Private static method returning a fully-populated SaveData with realistic values across all fields, including nested SavedRoom and SavedCitizen collections.
**When to use:** Round-trip test (SAVE-01) and empty-collections edge case test.
**Example:**
```csharp
// Mirrors CreateMoodSystem() pattern from MoodSystemTests.cs
private static SaveData CreateTestSaveData()
{
    return new SaveData
    {
        Version = 3,
        Credits = 1500,
        Happiness = 0f,
        CrossedMilestoneCount = 3,
        LifetimeHappiness = 42,
        Mood = 0.65f,
        MoodBaseline = 0.10f,
        UnlockedRooms = new List<string> { "hydroponics", "solar_panel" },
        PlacedRooms = new List<SavedRoom>
        {
            new() { RoomId = "housing_basic", Row = 0, StartPos = 2, SegmentCount = 2, Cost = 129 },
            new() { RoomId = "solar_panel", Row = 1, StartPos = 5, SegmentCount = 1, Cost = 85 },
        },
        Citizens = new List<SavedCitizen>
        {
            new()
            {
                Name = "Ada", BodyType = 1,
                PrimaryR = 0.8f, PrimaryG = 0.3f, PrimaryB = 0.1f,
                SecondaryR = 0.2f, SecondaryG = 0.5f, SecondaryB = 0.9f,
                WalkwayAngle = 1.57f, Direction = -1f,
                CurrentWishId = "wish_hydroponics",
                HomeSegmentIndex = 2
            },
            new()
            {
                Name = "Bob", BodyType = 0,
                PrimaryR = 0.4f, PrimaryG = 0.6f, PrimaryB = 0.2f,
                SecondaryR = 0.7f, SecondaryG = 0.1f, SecondaryB = 0.3f,
                WalkwayAngle = 3.14f, Direction = 1f,
                CurrentWishId = null,
                HomeSegmentIndex = null
            },
        },
        ActiveWishes = new Dictionary<string, string> { { "Ada", "wish_hydroponics" } },
        PlacedRoomTypes = new Dictionary<string, int> { { "housing_basic", 1 }, { "solar_panel", 1 } },
    };
}
```

### Pattern 2: Inline JSON String Literals for Legacy Formats
**What:** Hand-crafted JSON strings embedded directly in test methods, representing v1 and v2 save formats.
**When to use:** SAVE-02 and SAVE-03 backward compatibility tests.
**Example:**
```csharp
// v1 JSON: has Version:1, all v1-era fields, no v2/v3 fields
var v1Json = """
{
    "Version": 1,
    "Credits": 500,
    "Happiness": 0.35,
    "CrossedMilestoneCount": 1,
    "UnlockedRooms": ["hydroponics"],
    "PlacedRooms": [{ "RoomId": "housing_basic", "Row": 0, "StartPos": 0, "SegmentCount": 1, "Cost": 70 }],
    "Citizens": [{ "Name": "Ada", "BodyType": 1, "PrimaryR": 0.8, ... }],
    "ActiveWishes": { "Ada": "wish_hydroponics" },
    "PlacedRoomTypes": { "housing_basic": 1 }
}
""";
```

### Pattern 3: Production JsonSerializerOptions
**What:** Use the same `new JsonSerializerOptions { WriteIndented = true }` as SaveManager.PerformSave().
**When to use:** All serialization calls in tests.
**Example:**
```csharp
var options = new JsonSerializerOptions { WriteIndented = true };
string json = JsonSerializer.Serialize(original, options);
var restored = JsonSerializer.Deserialize<SaveData>(json);
```

### Anti-Patterns to Avoid
- **Using JsonSerializerOptions with PropertyNameCaseInsensitive:** Production code does not set this. Tests must use default (case-sensitive) matching to mirror production behavior.
- **Using deep equality helpers:** Field-by-field assertions are explicitly required -- they show exactly which field broke when a test fails.
- **Extending GameTestClass:** These are POCO tests with no singleton interaction. Use TestClass directly.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| JSON serialization | Custom serializer or manual string building | System.Text.Json.JsonSerializer.Serialize/Deserialize | Matches production exactly; handles all edge cases (nullable, collections, nesting) |
| Object comparison | Custom Equals/GetHashCode on POCOs | Field-by-field Shouldly assertions | Explicit failure messages; no risk of incorrect equality implementation |
| Test data construction | Repeated inline SaveData creation | CreateTestSaveData() private helper | Single source of truth; consistent with project pattern |

**Key insight:** The test must exercise the exact same serialization path as production. Any divergence (different options, custom converters, manual JSON building for round-trip) would make the test meaningless.

## Common Pitfalls

### Pitfall 1: Forgetting to Test Fields That Exist in All Versions
**What goes wrong:** Legacy format tests only assert the defaulted (missing) fields, ignoring whether the present fields deserialized correctly.
**Why it happens:** Focus on "what's different" rather than "complete verification."
**How to avoid:** Assert every field on the deserialized SaveData, including fields that were present in v1/v2 JSON. The CONTEXT explicitly requires this.
**Warning signs:** Test has assertions only for LifetimeHappiness/Mood/MoodBaseline/HomeSegmentIndex but not Credits/Happiness/etc.

### Pitfall 2: Collection Initializer Removal Breaking Empty Round-Trip
**What goes wrong:** If someone removes `= new()` from a SaveData collection property, serialization writes `null` instead of `[]`. Deserialization then produces `null` instead of an empty list.
**Why it happens:** SaveData uses `= new()` initializers on List/Dictionary properties. If removed, default is null.
**How to avoid:** The empty-collections test must verify both `ShouldNotBeNull` AND `ShouldBeEmpty` on each collection property.
**Warning signs:** Only checking `ShouldBeEmpty` (which would throw NullReferenceException, not give a clear "was null" message).

### Pitfall 3: Using float Literals Without f Suffix in JSON
**What goes wrong:** In C# raw string literals, writing `0.8` without quotes in JSON is fine -- System.Text.Json parses it as a float. But in C# assertion code, `0.8` is a double, not float. Comparing `float.ShouldBe(double)` may fail.
**Why it happens:** C# numeric literal type inference.
**How to avoid:** Always use `f` suffix in C# assertion values: `citizen.PrimaryR.ShouldBe(0.8f)`. JSON values don't need the suffix (they're JSON numbers).
**Warning signs:** Shouldly assertion failures with tiny precision differences.

### Pitfall 4: Raw String Literal Indentation
**What goes wrong:** C# raw string literals (`"""..."""`) require the closing `"""` to set the indentation baseline. If indented incorrectly, the JSON string contains unexpected leading whitespace.
**Why it happens:** C# 11 raw string literal indentation rules.
**How to avoid:** Ensure closing `"""` is at the desired indentation level, and JSON content is indented relative to it.
**Warning signs:** JsonException during deserialization with unexpected characters.

### Pitfall 5: System.Text.Json Case Sensitivity
**What goes wrong:** System.Text.Json is case-sensitive by default. JSON property names must match C# property names exactly (e.g., `"Version"` not `"version"`).
**Why it happens:** Unlike Newtonsoft.Json, System.Text.Json defaults to case-sensitive matching.
**How to avoid:** Use PascalCase property names in hand-crafted JSON strings, matching the C# property names exactly. Production code does not set PropertyNameCaseInsensitive.
**Warning signs:** All deserialized values are default (0, null, empty) despite JSON having data.

## Code Examples

### SAVE-01: v3 Round-Trip Pattern
```csharp
// Source: SaveManager.cs lines 285-286 (production serialization)
[Test]
public void V3RoundTripPreservesAllFields()
{
    var original = CreateTestSaveData();
    var options = new JsonSerializerOptions { WriteIndented = true };

    string json = JsonSerializer.Serialize(original, options);
    var restored = JsonSerializer.Deserialize<SaveData>(json);

    // Top-level scalar fields
    restored.Version.ShouldBe(3);
    restored.Credits.ShouldBe(1500);
    restored.Happiness.ShouldBe(0f);
    restored.CrossedMilestoneCount.ShouldBe(3);
    restored.LifetimeHappiness.ShouldBe(42);
    restored.Mood.ShouldBe(0.65f);
    restored.MoodBaseline.ShouldBe(0.10f);

    // Collections
    restored.UnlockedRooms.ShouldBe(new List<string> { "hydroponics", "solar_panel" });

    // PlacedRooms[0]
    restored.PlacedRooms.Count.ShouldBe(2);
    restored.PlacedRooms[0].RoomId.ShouldBe("housing_basic");
    // ... field-by-field for all SavedRoom properties

    // Citizens[0] -- with HomeSegmentIndex set
    restored.Citizens.Count.ShouldBe(2);
    restored.Citizens[0].Name.ShouldBe("Ada");
    restored.Citizens[0].HomeSegmentIndex.ShouldBe(2);
    // ... field-by-field for all SavedCitizen properties

    // Citizens[1] -- with HomeSegmentIndex null
    restored.Citizens[1].HomeSegmentIndex.ShouldBeNull();
    // ... etc
}
```

### SAVE-02: v1 Backward Compatibility Pattern
```csharp
[Test]
public void V1JsonDeserializesWithCorrectDefaults()
{
    var v1Json = """
        {
            "Version": 1,
            "Credits": 500,
            "Happiness": 0.35,
            "CrossedMilestoneCount": 1,
            "UnlockedRooms": ["hydroponics"],
            "PlacedRooms": [
                { "RoomId": "housing_basic", "Row": 0, "StartPos": 0, "SegmentCount": 1, "Cost": 70 }
            ],
            "Citizens": [
                {
                    "Name": "Ada", "BodyType": 1,
                    "PrimaryR": 0.8, "PrimaryG": 0.3, "PrimaryB": 0.1,
                    "SecondaryR": 0.2, "SecondaryG": 0.5, "SecondaryB": 0.9,
                    "WalkwayAngle": 1.57, "Direction": -1.0,
                    "CurrentWishId": "wish_hydroponics"
                }
            ],
            "ActiveWishes": { "Ada": "wish_hydroponics" },
            "PlacedRoomTypes": { "housing_basic": 1 }
        }
        """;

    var data = JsonSerializer.Deserialize<SaveData>(v1Json);

    // v1 fields present -- should deserialize correctly
    data.Version.ShouldBe(1);
    data.Credits.ShouldBe(500);
    data.Happiness.ShouldBe(0.35f);
    // ... all v1 fields

    // v2 fields MISSING from v1 JSON -- should get C# defaults
    data.LifetimeHappiness.ShouldBe(0);
    data.Mood.ShouldBe(0f);
    data.MoodBaseline.ShouldBe(0f);

    // v3 field MISSING from v1 JSON -- should get null
    data.Citizens[0].HomeSegmentIndex.ShouldBeNull();
}
```

### Empty Collections Edge Case Pattern
```csharp
[Test]
public void EmptyCollectionsRoundTripAsNonNull()
{
    var empty = new SaveData
    {
        Version = 3,
        Credits = 0,
        // All collections left at their initialized-empty defaults
    };
    var options = new JsonSerializerOptions { WriteIndented = true };

    string json = JsonSerializer.Serialize(empty, options);
    var restored = JsonSerializer.Deserialize<SaveData>(json);

    restored.PlacedRooms.ShouldNotBeNull();
    restored.PlacedRooms.ShouldBeEmpty();
    restored.Citizens.ShouldNotBeNull();
    restored.Citizens.ShouldBeEmpty();
    restored.ActiveWishes.ShouldNotBeNull();
    restored.ActiveWishes.ShouldBeEmpty();
    restored.PlacedRoomTypes.ShouldNotBeNull();
    restored.PlacedRoomTypes.ShouldBeEmpty();
    restored.UnlockedRooms.ShouldNotBeNull();
    restored.UnlockedRooms.ShouldBeEmpty();
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Newtonsoft.Json (Json.NET) | System.Text.Json | .NET Core 3.0+ (2019) | Built-in, no NuGet dependency; case-sensitive by default; different API surface |
| [JsonProperty] attributes | Property name matching (PascalCase) | System.Text.Json default | No attributes needed when C# property names match JSON keys |
| Tolerance-based float comparison | Exact float equality | System.Text.Json preserves float round-trip | No ShouldBe(expected, tolerance) needed for serialized floats |

**Key behavior notes for System.Text.Json (verified against production code):**
- Missing JSON properties: C# property keeps its default value (0, 0f, null, or initializer value)
- `int?` with missing JSON key: remains `null` (not 0)
- `List<T>` with `= new()` initializer + serialized as `[]`: deserializes as empty list (not null)
- `List<T>` with `= new()` initializer + property missing from JSON: deserializes as empty list (initializer runs, then no JSON overwrites)
- Case-sensitive property matching by default (no `PropertyNameCaseInsensitive` set in production)
- Float values round-trip exactly through JSON (IEEE 754 representation preserved)

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Chickensoft.GoDotTest 2.0.30 + Shouldly 4.3.0 |
| Config file | Orbital Rings.csproj (conditional PackageReference) |
| Quick run command | `godot --headless --run-tests --quit-on-finish` |
| Full suite command | `godot --headless --run-tests --quit-on-finish` |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SAVE-01 | v3 SaveData round-trips through JSON with every field preserved | unit | `godot --headless --run-tests --quit-on-finish` | No -- Wave 0 |
| SAVE-02 | v1 JSON deserializes with correct v2/v3 defaults | unit | `godot --headless --run-tests --quit-on-finish` | No -- Wave 0 |
| SAVE-03 | v2 JSON deserializes with correct v3 default (null HomeSegmentIndex) | unit | `godot --headless --run-tests --quit-on-finish` | No -- Wave 0 |

### Sampling Rate
- **Per task commit:** `godot --headless --run-tests --quit-on-finish`
- **Per wave merge:** Full suite (same command -- all tests run together)
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `Tests/Save/SaveDataTests.cs` -- covers SAVE-01, SAVE-02, SAVE-03 + empty collections edge case
- No framework install needed -- already configured
- No conftest/fixtures needed -- pure POCO tests with no shared state

## Open Questions

None -- all technical questions are resolved:
1. SaveData POCO structure is fully documented in SaveManager.cs
2. System.Text.Json behavior for missing properties is well-established
3. Test patterns are established by MoodSystemTests.cs and HousingTests.cs
4. All user decisions are locked in CONTEXT.md

## Sources

### Primary (HIGH confidence)
- `/workspace/Scripts/Autoloads/SaveManager.cs` -- SaveData, SavedRoom, SavedCitizen POCO definitions; production JsonSerializerOptions; version-gated ApplyState logic
- `/workspace/Tests/Mood/MoodSystemTests.cs` -- CreateMoodSystem() helper pattern, comment headers, TestClass extension, Shouldly usage
- `/workspace/Tests/Housing/HousingTests.cs` -- TestClass extension pattern for non-singleton tests
- `/workspace/Tests/Economy/EconomyTests.cs` -- GameTestClass vs TestClass decision pattern, comment headers
- `/workspace/Tests/Infrastructure/GameTestClass.cs` -- Base class distinction (GameTestClass for singletons, TestClass for POCOs)
- `/workspace/Orbital Rings.csproj` -- Package versions, test compilation conditions

### Secondary (MEDIUM confidence)
- System.Text.Json documentation: missing properties retain C# defaults; float round-trip fidelity; case-sensitive matching by default

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- all libraries already in use, versions pinned in .csproj
- Architecture: HIGH -- patterns established by 4 prior test phases, CONTEXT.md specifies exact file/namespace/class structure
- Pitfalls: HIGH -- derived from direct code inspection of SaveData POCOs and System.Text.Json behavior

**Research date:** 2026-03-07
**Valid until:** 2026-04-06 (stable -- POCO serialization patterns and System.Text.Json behavior are mature)
