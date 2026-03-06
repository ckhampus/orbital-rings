# Phase 16: Capacity Transfer - Context

**Gathered:** 2026-03-06
**Status:** Ready for planning

<domain>
## Phase Boundary

HousingManager becomes the single source of truth for housing capacity. HappinessManager's duplicate capacity tracking (fields, event subscriptions, methods) is fully removed. Arrival gating stays in HappinessManager but queries HousingManager for capacity.

Requirements: INFR-03.

</domain>

<decisions>
## Implementation Decisions

### Starter Citizen Allowance
- StarterCitizenCapacity = 5 stays as a private const in HappinessManager (arrival gating owner)
- Arrival check formula becomes: `StarterCitizenCapacity + HousingManager.Instance.TotalCapacity`
- No floor clamp needed — TotalCapacity >= 0, so the sum is always >= 5
- This is a game-start bootstrap value, not a housing tunable — doesn't belong in HousingConfig

### Capacity Removal from HappinessManager
- Remove `_housingCapacity` field and `_housingRoomCapacities` dictionary entirely
- Remove `CalculateHousingCapacity()` and `GetHousingCapacity()` public methods (no external callers after SaveManager update)
- Remove `InitializeHousingCapacity()` private method
- Remove `StateLoaded` flag — it only guarded InitializeHousingCapacity; nothing else uses it
- Remove `OnRoomPlaced()` and `OnRoomDemolished()` handlers — they only did capacity tracking
- Remove RoomPlaced and RoomDemolished event subscriptions from `_Ready()` and `_ExitTree()`
- Update class-level doc comment to reflect that arrival gating now queries HousingManager

### Save Data Cleanup
- Remove `HousingCapacity` field from SaveData — capacity is derivable from placed rooms
- Drop `int housingCapacity` parameter from `HappinessManager.RestoreState()`
- Update SaveManager call sites to stop saving/passing housing capacity
- No save version bump — removing a field is backward-compatible (System.Text.Json ignores unknown properties)

### Claude's Discretion
- Exact wording of updated HappinessManager doc comment
- Order of cleanup within files (top-down vs grouped by concern)
- Whether to add a comment on the arrival check explaining the StarterCitizenCapacity + TotalCapacity formula

</decisions>

<specifics>
## Specific Ideas

No specific requirements — this is a mechanical refactoring. Remove HappinessManager's duplicate tracking, redirect the one arrival-gating line to query HousingManager, and clean up SaveManager.

</specifics>

<code_context>
## Existing Code Insights

### What Gets Removed from HappinessManager
- `StarterCitizenCapacity` constant (stays, but usage changes)
- `_housingCapacity` field (line 87) — replaced by HousingManager query
- `_housingRoomCapacities` dictionary (line 94) — duplicate of HousingManager's
- `CalculateHousingCapacity()` (line 128) — no callers after cleanup
- `GetHousingCapacity()` (line 141) — only called by SaveManager, which stops
- `StateLoaded` property (line 33) — only guarded InitializeHousingCapacity
- `OnRoomPlaced()` (line 365) — only did capacity tracking
- `OnRoomDemolished()` (line 386) — only did capacity tracking
- `InitializeHousingCapacity()` (line 403) — entire method removed
- RoomPlaced/RoomDemolished subscriptions in `_Ready()` (lines 177-178) and `_ExitTree()` (lines 218-219)

### What Changes in HappinessManager
- `OnArrivalCheck()` (line 303): `currentPop >= _housingCapacity` becomes `currentPop >= StarterCitizenCapacity + (HousingManager.Instance?.TotalCapacity ?? 0)`
- `RestoreState()` (line 148): drop `housingCapacity` parameter, remove `_housingCapacity = housingCapacity` line

### What Changes in SaveManager
- `SaveData.HousingCapacity` property (line 26) — removed
- Save creation (line 293) — stop calling `GetHousingCapacity()`
- Restore calls (lines 413, 424) — stop passing `data.HousingCapacity`

### Integration Points
- HappinessManager.cs — main cleanup target
- SaveManager.cs — remove capacity field and update save/restore calls
- HousingManager.cs — already has TotalCapacity property (no changes needed)

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 16-capacity-transfer*
*Context gathered: 2026-03-06*
