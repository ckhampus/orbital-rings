# Phase 14: Housing Foundation - Context

**Gathered:** 2026-03-06
**Status:** Ready for planning

<domain>
## Phase Boundary

All shared types, event signatures, and data schemas for the housing system exist so Phases 15-19 can compile against them. No runtime behavior — pure infrastructure plumbing.

Requirements: INFR-01 (skeleton), INFR-02 (config), INFR-05 (save schema).

</domain>

<decisions>
## Implementation Decisions

### Event Signatures
- CitizenAssignedHome carries `(string citizenName, int segmentIndex)` — matches existing CitizenEnteredRoom pattern
- CitizenUnhoused carries `(string citizenName)` — no previous segment, just the citizen name
- Single CitizenAssignedHome event for both first-assignment and reassignment — subscribers don't need to distinguish
- Follow existing GameEvents pattern: `event Action<...>` + `Emit...()` helper method + XML doc comments

### HousingConfig Scope
- Timing fields only: HomeTimerMin, HomeTimerMax, RestDurationMin, RestDurationMax
- No capacity constants — capacity stays on RoomDefinition.BaseCapacity where it already lives
- Single ExportGroup("Home Return Timing") wrapping all 4 fields
- `[GlobalClass] public partial class HousingConfig : Resource` — matches EconomyConfig/HappinessConfig pattern

### Save Schema
- SavedCitizen gets `int? HomeSegmentIndex` (C# nullable int) — serializes to null when unhoused, not 0 or -1
- SaveData.Version bumped to 3 in this phase (schema change = version change)
- v2 saves deserialize HomeSegmentIndex as null (default for int?) — backward-compatible

### Claude's Discretion
- Exact default values for HomeTimerMin/Max, RestDurationMin/Max (PRD says 90-150s / 8-15s as starting point)
- XML doc comment wording on events and config fields
- Whether HousingConfig .tres file is created in this phase or deferred to Phase 15

</decisions>

<specifics>
## Specific Ideas

No specific requirements — follow established patterns from EconomyConfig, HappinessConfig, and GameEvents. The PRD at `docs/prd-housing.md` is the design reference.

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- GameEvents.cs: Signal bus with pure C# events — add Housing Events section following Phase 10 pattern
- EconomyConfig.cs / HappinessConfig.cs: `[GlobalClass] Resource` with `[Export]` + `[ExportGroup]` — template for HousingConfig
- SaveManager.cs / SaveData: System.Text.Json serialization with version-gated restore — add field to SavedCitizen

### Established Patterns
- Events: `event Action<params>` + `public void Emit...() => Event?.Invoke(...)` + XML doc
- Config resources: `[GlobalClass] public partial class XConfig : Resource` in `Scripts/Data/`
- Save versioning: `SaveData.Version` integer, checked in `ApplyState()` for backward compat
- Config naming: fields use PascalCase properties with `[Export]`, grouped by `[ExportGroup]`

### Integration Points
- GameEvents.cs — add Housing Events section after Phase 10 events (line ~229)
- SavedCitizen class in SaveManager.cs — add `int? HomeSegmentIndex` property
- SaveData.Version — bump default from 2 to 3
- Scripts/Data/ directory — new HousingConfig.cs file

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 14-housing-foundation*
*Context gathered: 2026-03-06*
