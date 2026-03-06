# Phase 14: Housing Foundation - Research

**Researched:** 2026-03-06
**Domain:** C#/Godot infrastructure -- types, events, config resources, save schema
**Confidence:** HIGH

## Summary

Phase 14 is pure infrastructure plumbing: no runtime behavior, no new singletons wired up, just shared types that Phases 15-19 compile against. The scope is narrowly defined by three requirements (INFR-01 skeleton, INFR-02 config resource, INFR-05 save schema) and all implementation decisions are locked by CONTEXT.md.

Every pattern needed already exists in the codebase. GameEvents.cs has 10 event sections following an identical `event Action<T> + Emit...()` pattern. EconomyConfig and HappinessConfig demonstrate the exact `[GlobalClass] Resource` with `[Export]` + `[ExportGroup]` pattern. SaveManager.cs uses System.Text.Json with `WriteIndented = true` and default deserialization, which handles `int?` nullable types correctly out of the box.

**Primary recommendation:** Follow existing patterns exactly -- copy the structure from EconomyConfig for HousingConfig, the Phase 10 event section for housing events, and add a simple `int?` property to SavedCitizen. No new patterns or libraries needed.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- CitizenAssignedHome carries `(string citizenName, int segmentIndex)` -- matches existing CitizenEnteredRoom pattern
- CitizenUnhoused carries `(string citizenName)` -- no previous segment, just the citizen name
- Single CitizenAssignedHome event for both first-assignment and reassignment -- subscribers don't need to distinguish
- Follow existing GameEvents pattern: `event Action<...>` + `Emit...()` helper method + XML doc comments
- Timing fields only: HomeTimerMin, HomeTimerMax, RestDurationMin, RestDurationMax
- No capacity constants -- capacity stays on RoomDefinition.BaseCapacity where it already lives
- Single ExportGroup("Home Return Timing") wrapping all 4 fields
- `[GlobalClass] public partial class HousingConfig : Resource` -- matches EconomyConfig/HappinessConfig pattern
- SavedCitizen gets `int? HomeSegmentIndex` (C# nullable int) -- serializes to null when unhoused, not 0 or -1
- SaveData.Version bumped to 3 in this phase (schema change = version change)
- v2 saves deserialize HomeSegmentIndex as null (default for int?) -- backward-compatible

### Claude's Discretion
- Exact default values for HomeTimerMin/Max, RestDurationMin/Max (PRD says 90-150s / 8-15s as starting point)
- XML doc comment wording on events and config fields
- Whether HousingConfig .tres file is created in this phase or deferred to Phase 15

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| INFR-01 | New HousingManager autoload singleton owns citizen-to-room mapping | Phase 14 delivers the skeleton file only (empty class with singleton pattern, registered in project.godot). Full implementation is Phase 15. See Architecture Patterns > HousingManager Skeleton. |
| INFR-02 | HousingConfig resource with Inspector-tunable timing constants | Four float fields in a `[GlobalClass] Resource` with `[ExportGroup]`. Follows EconomyConfig/HappinessConfig pattern exactly. See Code Examples > HousingConfig. |
| INFR-05 | Save format bumped to v3 with nullable HomeSegmentIndex | Add `int? HomeSegmentIndex` to SavedCitizen, bump SaveData.Version to 3, ensure v2 backward compat. System.Text.Json handles nullable int correctly by default. See Architecture Patterns > Save Schema Changes. |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Godot.NET.Sdk | 4.6.1 | Game engine C# SDK | Project SDK, defines `[Export]`, `[GlobalClass]`, `Resource` base class |
| System.Text.Json | .NET 10 built-in | Save serialization | Already used by SaveManager, handles `int?` nullable serialization natively |
| Godot 4.6 | 4.6 | Engine runtime | Config features, project version |

### Supporting
No additional libraries needed. This phase uses only existing project infrastructure.

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `int?` (C# nullable) | `int` with sentinel -1 | Nullable int is idiomatic C#, serializes to JSON `null` automatically, no sentinel value ambiguity |
| System.Text.Json | Newtonsoft.Json | System.Text.Json already in use, no reason to add dependency |

## Architecture Patterns

### Recommended File Structure
```
Scripts/
  Data/
    HousingConfig.cs          # NEW - [GlobalClass] Resource with timing fields
  Autoloads/
    GameEvents.cs             # MODIFIED - add Housing Events section
    SaveManager.cs            # MODIFIED - bump Version to 3, add field to SavedCitizen
    HousingManager.cs         # NEW - empty skeleton with singleton pattern
Resources/
  Housing/
    default_housing.tres      # NEW (optional) - default HousingConfig instance
```

### Pattern 1: GameEvents Event Section
**What:** Add a "Housing Events (Phase 14)" section to GameEvents.cs following the established pattern.
**When to use:** Always -- this is the only event bus pattern in the project.
**Key details:**
- Insert after the Phase 10 section (currently line 228, before closing brace on line 229)
- Each event needs: XML param docs, `event Action<...>` declaration, `Emit...()` helper
- CitizenAssignedHome: `Action<string, int>` -- matches CitizenEnteredRoom signature pattern
- CitizenUnhoused: `Action<string>` -- matches CitizenArrived signature pattern

**Example (from existing codebase):**
```csharp
// Source: Scripts/Autoloads/GameEvents.cs lines 132-160
// ---------------------------------------------------------------------------
// Citizen Events (Phase 5)
// ---------------------------------------------------------------------------

/// <param name="citizenName">Display name of the arriving citizen.</param>
public event Action<string> CitizenArrived;

/// <param name="citizenName">Display name of the clicked citizen.</param>
public event Action<string> CitizenClicked;

public void EmitCitizenArrived(string citizenName)
  => CitizenArrived?.Invoke(citizenName);
```

### Pattern 2: Config Resource ([GlobalClass] Resource)
**What:** `[GlobalClass] public partial class HousingConfig : Resource` with `[Export]` properties grouped by `[ExportGroup]`.
**When to use:** For any Inspector-tunable constant set.
**Key details:**
- File goes in `Scripts/Data/HousingConfig.cs`
- Namespace: `OrbitalRings.Data`
- Single `[ExportGroup("Home Return Timing")]` wrapping all 4 fields
- Properties use PascalCase with `{ get; set; }` and default values
- XML doc comments explain what each value controls

**Example (from existing codebase):**
```csharp
// Source: Scripts/Data/EconomyConfig.cs
[GlobalClass]
public partial class EconomyConfig : Resource
{
    [ExportGroup("Income")]

    /// <summary>Trickle income at 0 citizens so the player is never stuck.</summary>
    [Export] public float BaseStationIncome { get; set; } = 1.0f;
    // ...
}
```

### Pattern 3: Config Resource Loading
**What:** Autoload managers load config via `[Export]` property (Inspector) with fallback to `ResourceLoader.Load<T>()` then `new T()` code defaults.
**Key details:**
- Three-tier loading: Inspector-assigned > `.tres` file at default path > code defaults
- Pattern used identically in EconomyManager and HappinessManager
- HousingManager skeleton in Phase 14 should include the `[Export] public HousingConfig Config { get; set; }` property but config loading logic deferred to Phase 15 when the manager gets its `_Ready()` implementation

**Example (from existing codebase):**
```csharp
// Source: Scripts/Autoloads/EconomyManager.cs lines 52-61
if (Config == null)
{
    Config = ResourceLoader.Load<EconomyConfig>("res://Resources/Economy/default_economy.tres");
}
if (Config == null)
{
    GD.PushWarning("EconomyManager: No EconomyConfig assigned or found at default path. Using code defaults.");
    Config = new EconomyConfig();
}
```

### Pattern 4: Save Schema Changes
**What:** Add nullable `int?` property to SavedCitizen POCO and bump SaveData.Version.
**Key details:**
- SavedCitizen is a plain C# class (no Godot types) at top of SaveManager.cs
- Add `public int? HomeSegmentIndex { get; set; }` -- default is `null` for nullable value types
- System.Text.Json default serialization writes `"HomeSegmentIndex": null` when null (verified with Microsoft docs)
- System.Text.Json default deserialization assigns `null` to missing `int?` properties (v2 saves missing the field)
- SaveData.Version default changes from `2` to `3` (line 23 of SaveManager.cs: `public int Version { get; set; } = 2;` becomes `= 3;`)
- No changes needed in `CollectGameState()` or `ApplySceneState()` in this phase -- that wiring is Phase 19 (INFR-04)

### Pattern 5: HousingManager Skeleton (INFR-01 partial)
**What:** Empty autoload singleton registered in project.godot, compiled but with no behavior.
**Key details:**
- File: `Scripts/Autoloads/HousingManager.cs`
- Namespace: `OrbitalRings.Autoloads`
- Follows the same singleton pattern as all other autoloads: `public static HousingManager Instance { get; private set; }` set in `_EnterTree()`
- Include `[Export] public HousingConfig Config { get; set; }` for config loading (Phase 15 wires it)
- Register in `project.godot` [autoload] section -- order matters: after HappinessManager, before SaveManager (SaveManager must be last as it subscribes to all events)
- Full implementation (assignment logic, event handlers) is Phase 15

### Pattern 6: Autoload Registration in project.godot
**What:** Add HousingManager to the [autoload] section of project.godot.
**Key details:**
- Current autoload order (from project.godot):
  1. GameEvents
  2. EconomyManager
  3. BuildManager
  4. CitizenManager
  5. WishBoard
  6. HappinessManager
  7. SaveManager
- HousingManager should be inserted as 7th (after HappinessManager, before SaveManager)
- SaveManager MUST remain last because it subscribes to events from all other singletons
- Format: `HousingManager="*res://Scripts/Autoloads/HousingManager.cs"`

### Anti-Patterns to Avoid
- **Don't add behavior in Phase 14:** This is types-only. No event subscriptions, no assignment logic, no timers. That's all Phase 15+.
- **Don't use sentinel values for unhoused:** Use `int?` (null), not `int` with -1 or 0. The decision is locked.
- **Don't add `DefaultIgnoreCondition` to serializer options:** The default behavior already writes `null` for nullable types, which is the desired behavior.
- **Don't modify CollectGameState or ApplySceneState:** Save/load wiring is Phase 19. Phase 14 only touches the schema (POCO definition + version bump).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Nullable serialization | Custom JSON converter for int? | System.Text.Json default handling | `int?` serializes to `null` and deserializes from missing field to `null` automatically |
| Config resource UI | Custom Inspector plugin | Godot `[Export]` + `[ExportGroup]` + `[GlobalClass]` | Built-in Inspector integration, no tooling needed |
| Event bus | Custom pub/sub system | Existing GameEvents C# events pattern | All 10 existing event sections use this pattern |
| Singleton pattern | Service locator or DI | Godot Autoload + static Instance | Every autoload in the project uses this pattern |

**Key insight:** Phase 14 introduces zero new patterns. Every deliverable is a direct copy of an existing codebase pattern.

## Common Pitfalls

### Pitfall 1: SaveManager Autoload Order
**What goes wrong:** If HousingManager is registered after SaveManager in project.godot, SaveManager can't subscribe to housing events in _Ready().
**Why it happens:** Autoload initialization order in Godot follows the order in project.godot.
**How to avoid:** Insert HousingManager BEFORE SaveManager in the [autoload] section. SaveManager must remain last.
**Warning signs:** Null reference exceptions from SaveManager during startup.

### Pitfall 2: Version Bump Without Schema Change Coordination
**What goes wrong:** Bumping SaveData.Version to 3 without adding version-gated logic causes v2 saves to be treated as v3.
**Why it happens:** The Version field is read during ApplyState() for backward compatibility branching.
**How to avoid:** In Phase 14, ONLY change the default value of `Version` from 2 to 3 on SaveData (line 23). Do NOT modify ApplyState() -- that's Phase 19. The version bump is safe because v2 saves deserialized will still have Version=2 (the saved value overrides the default), and new saves will write Version=3.
**Warning signs:** None in Phase 14 -- the risk materializes in Phase 19 if the version gate is done incorrectly.

### Pitfall 3: File Name Must Match Class Name for GlobalClass
**What goes wrong:** Godot can't find the resource type in the Inspector's "New Resource" menu.
**Why it happens:** Godot C# requires the file name to match the class name exactly (case-sensitive) for `[GlobalClass]` to register.
**How to avoid:** Name the file `HousingConfig.cs` to match `class HousingConfig`.
**Warning signs:** Resource type missing from Inspector dropdown.

### Pitfall 4: Missing `partial` Keyword
**What goes wrong:** Compilation error -- Godot source generators require `partial` on all classes extending Godot types.
**Why it happens:** Easy to forget when creating a new class.
**How to avoid:** Always use `public partial class` for any class extending `Node`, `Resource`, or other Godot types.
**Warning signs:** Build error referencing source generators.

### Pitfall 5: Forgetting XML Doc on Events
**What goes wrong:** Inconsistency with existing event documentation pattern.
**Why it happens:** Events seem simple enough to skip docs.
**How to avoid:** Every event in GameEvents.cs has `/// <param name="...">` XML docs. Housing events must too.
**Warning signs:** Code review catches inconsistency.

## Code Examples

Verified patterns from the existing codebase:

### HousingConfig Resource
```csharp
// Target: Scripts/Data/HousingConfig.cs
// Pattern source: Scripts/Data/HappinessConfig.cs, Scripts/Data/EconomyConfig.cs
using Godot;

namespace OrbitalRings.Data;

/// <summary>
/// Inspector-tunable configuration for the housing return-home system.
/// All timing parameters live here for easy iteration without touching behavior code.
///
/// Default values calibrated from the PRD (docs/prd-housing.md):
/// home return cycle 90-150s, rest duration 8-15s.
/// </summary>
[GlobalClass]
public partial class HousingConfig : Resource
{
    [ExportGroup("Home Return Timing")]

    /// <summary>Minimum seconds between home-return trips. Default 90s (PRD lower bound).</summary>
    [Export] public float HomeTimerMin { get; set; } = 90.0f;

    /// <summary>Maximum seconds between home-return trips. Default 150s (PRD upper bound).</summary>
    [Export] public float HomeTimerMax { get; set; } = 150.0f;

    /// <summary>Minimum seconds a citizen rests at home. Default 8s (PRD lower bound).</summary>
    [Export] public float RestDurationMin { get; set; } = 8.0f;

    /// <summary>Maximum seconds a citizen rests at home. Default 15s (PRD upper bound).</summary>
    [Export] public float RestDurationMax { get; set; } = 15.0f;
}
```

### Housing Events in GameEvents.cs
```csharp
// Target: Scripts/Autoloads/GameEvents.cs (append before closing brace)
// Pattern source: Citizen Events (Phase 5) section, lines 132-160

// ---------------------------------------------------------------------------
// Housing Events (Phase 14)
// ---------------------------------------------------------------------------

/// <param name="citizenName">Display name of the assigned citizen.</param>
/// <param name="segmentIndex">Flat segment index of the home room.</param>
public event Action<string, int> CitizenAssignedHome;

/// <param name="citizenName">Display name of the now-unhoused citizen.</param>
public event Action<string> CitizenUnhoused;

public void EmitCitizenAssignedHome(string citizenName, int segmentIndex)
    => CitizenAssignedHome?.Invoke(citizenName, segmentIndex);

public void EmitCitizenUnhoused(string citizenName)
    => CitizenUnhoused?.Invoke(citizenName);
```

### SavedCitizen Field Addition
```csharp
// Target: Scripts/Autoloads/SaveManager.cs, SavedCitizen class (line ~57-70)
// Add after existing fields:

/// <summary>
/// Flat segment index of the citizen's home room. Null when unhoused.
/// Added in save format v3. Deserializes as null from v2 saves (backward-compatible).
/// </summary>
public int? HomeSegmentIndex { get; set; }
```

### SaveData Version Bump
```csharp
// Target: Scripts/Autoloads/SaveManager.cs, SaveData class (line 23)
// Change from:
public int Version { get; set; } = 2;
// To:
public int Version { get; set; } = 3;
```

### HousingManager Skeleton
```csharp
// Target: Scripts/Autoloads/HousingManager.cs
// Pattern source: All autoloads in Scripts/Autoloads/
using Godot;
using OrbitalRings.Data;

namespace OrbitalRings.Autoloads;

/// <summary>
/// Manages citizen-to-home-room assignment. Owns the mapping of citizens to
/// housing rooms and emits CitizenAssignedHome/CitizenUnhoused events.
///
/// Registered as an Autoload in project.godot (8th singleton, before SaveManager).
/// Access via HousingManager.Instance.
///
/// Phase 14: skeleton only (type + singleton pattern).
/// Phase 15: full assignment logic.
/// Phase 16: capacity transfer from HappinessManager.
/// </summary>
public partial class HousingManager : Node
{
    /// <summary>Singleton instance, set in _EnterTree().</summary>
    public static HousingManager Instance { get; private set; }

    /// <summary>HousingConfig resource -- set via Inspector or loaded from default path.</summary>
    [Export] public HousingConfig Config { get; set; }

    public override void _EnterTree()
    {
        Instance = this;
    }
}
```

### project.godot Autoload Registration
```ini
# Target: project.godot [autoload] section
# Insert HousingManager after HappinessManager, before SaveManager:
HappinessManager="*res://Scripts/Autoloads/HappinessManager.cs"
HousingManager="*res://Scripts/Autoloads/HousingManager.cs"
SaveManager="*res://Scripts/Autoloads/SaveManager.cs"
```

### Optional: default_housing.tres Resource File
```
[gd_resource type="Resource" script_class="HousingConfig" load_steps=2 format=3]

[ext_resource type="Script" path="res://Scripts/Data/HousingConfig.cs" id="1"]

[resource]
script = ExtResource("1")
HomeTimerMin = 90.0
HomeTimerMax = 150.0
RestDurationMin = 8.0
RestDurationMax = 15.0
```

**Recommendation on .tres file:** Create it in this phase. It costs one file and ensures the config resource is immediately testable in the Godot Inspector. The `Resources/Housing/` directory mirrors `Resources/Economy/` and `Resources/Happiness/`. Phase 15 can reference it directly without a prerequisite step.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Godot [Signal] attribute | Pure C# events | Project inception | All events use `event Action<T>` pattern, no Godot signals |
| int sentinel (-1) for unhoused | C# nullable int? | CONTEXT.md decision | JSON serializes to `null`, no ambiguity with segment index 0 |
| Single SaveData.Version = 1 | Version-gated restore | Phase 10 (v1.1) | ApplyState branches on version, new fields default safely |

**Deprecated/outdated:**
- None relevant to this phase. All patterns in use are current.

## Open Questions

1. **Default timing values for HousingConfig**
   - What we know: PRD specifies 90-150s for home return cycle, 8-15s for rest duration
   - What's unclear: Whether these are good starting defaults or need tuning
   - Recommendation: Use PRD values (90, 150, 8, 15) as defaults. They can be tuned in the Inspector at any time. This falls under Claude's Discretion per CONTEXT.md.

2. **Whether to create the .tres file in Phase 14 or defer to Phase 15**
   - What we know: EconomyConfig and HappinessConfig both have .tres files in `Resources/` subdirectories
   - What's unclear: Whether the .tres file is useful before HousingManager loads it (Phase 15)
   - Recommendation: Create it in Phase 14. It validates the `[GlobalClass]` registration works and maintains consistency with existing config resources. This falls under Claude's Discretion per CONTEXT.md.

3. **CollectGameState changes for HomeSegmentIndex**
   - What we know: Phase 14 adds the field to SavedCitizen, Phase 19 wires serialization
   - What's unclear: Nothing -- this is clearly scoped
   - Recommendation: Do NOT modify CollectGameState or ApplySceneState in Phase 14. The new field will serialize as `null` for all citizens until Phase 19 sets it. This is correct behavior.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None detected -- no test infrastructure in project |
| Config file | None |
| Quick run command | N/A |
| Full suite command | N/A |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| INFR-01 | HousingManager skeleton compiles and registers as autoload | manual-only | Build project: `dotnet build` | N/A |
| INFR-02 | HousingConfig resource visible in Godot Inspector with 4 fields | manual-only | Build + open Inspector | N/A |
| INFR-05 | SavedCitizen.HomeSegmentIndex serializes to null, SaveData.Version = 3 | manual-only | Build + create new save | N/A |

**Manual-only justification:** This is a Godot game project with no test framework. All three requirements are infrastructure/compilation concerns that are verified by successful build and Inspector inspection. The success criteria from the phase description are all compile-time or Inspector-visible.

### Sampling Rate
- **Per task commit:** `dotnet build` (verifies compilation)
- **Per wave merge:** Manual: open Godot, verify Inspector shows HousingConfig fields, verify HousingManager appears in autoloads
- **Phase gate:** All three success criteria verified manually

### Wave 0 Gaps
- No test infrastructure exists in this project
- `dotnet build` serves as the primary automated validation
- Inspector-based verification is inherently manual for a Godot project

## Sources

### Primary (HIGH confidence)
- **Existing codebase** -- All patterns verified by reading actual source files:
  - `Scripts/Autoloads/GameEvents.cs` -- event pattern (10 sections, consistent format)
  - `Scripts/Data/EconomyConfig.cs` -- `[GlobalClass] Resource` pattern
  - `Scripts/Data/HappinessConfig.cs` -- `[GlobalClass] Resource` pattern
  - `Scripts/Autoloads/SaveManager.cs` -- SaveData/SavedCitizen POCOs, serialization options
  - `Scripts/Autoloads/EconomyManager.cs` -- config loading three-tier pattern
  - `Scripts/Autoloads/HappinessManager.cs` -- config loading, singleton pattern
  - `project.godot` -- autoload registration order
  - `Resources/Economy/default_economy.tres` -- .tres file format
  - `Resources/Happiness/default_happiness.tres` -- .tres file format

### Secondary (MEDIUM confidence)
- [Microsoft Learn: System.Text.Json ignore properties](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/ignore-properties) -- Verified that default serialization writes `null` for nullable value types, and `WhenWritingNull`/`WhenWritingDefault` are opt-in behaviors
- [Godot Docs: C# global classes](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_global_classes.html) -- Verified `[GlobalClass]` requirements (file name must match class name)
- [Godot Docs: C# exported properties](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_exports.html) -- Verified `[Export]` and `[ExportGroup]` usage

### Tertiary (LOW confidence)
- None -- all claims verified against primary or secondary sources.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- project already uses all needed libraries, no additions required
- Architecture: HIGH -- every pattern is a direct copy of existing codebase patterns, verified by reading source
- Pitfalls: HIGH -- pitfalls are about Godot/C# fundamentals verified against official docs and project patterns

**Research date:** 2026-03-06
**Valid until:** 2026-04-06 (stable -- infrastructure patterns, no fast-moving dependencies)
