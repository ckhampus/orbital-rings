# Phase 15: HousingManager Core - Context

**Gathered:** 2026-03-06
**Status:** Ready for planning

<domain>
## Phase Boundary

Citizens are automatically assigned to housing rooms with even distribution, and reassigned or gracefully unhoused when rooms change. This is the assignment ENGINE — pure logic with no visual feedback. Visual behaviors (Phase 17) and UI (Phase 18) come later.

Requirements: HOME-01, HOME-02, HOME-03, HOME-04, HOME-05, INFR-01 (full implementation).

</domain>

<decisions>
## Implementation Decisions

### Assignment Feedback
- Silent engine — no visual feedback (no floating text, no sounds) when citizens get or lose homes
- Events DO fire on every assignment/displacement (CitizenAssignedHome, CitizenUnhoused) for future subscribers
- SaveManager wired to housing events for autosave (same pattern as RoomPlaced/CitizenArrived)
- Visual feedback deferred to Phase 17 (return-home behavior) and Phase 18 (UI panels)

### Demolish Reassignment
- Instant reassignment: displaced citizens are reassigned in the same frame as demolition
- Partial reassignment: reassign as many displaced citizens as possible to available rooms
- Priority: oldest-arrived citizens get reassigned first (same rule as new-room assignment)
- Citizens that can't be reassigned remain unhoused (no penalty — HOME-05)

### Capacity Ownership
- HousingManager tracks its own housing capacity (Dictionary<anchorIndex, capacity>)
- HousingManager maintains its own registry of housing rooms (subscribes to RoomPlaced/RoomDemolished)
- HappinessManager keeps its own arrival gating via _housingCapacity until Phase 16 transfers it
- Both systems track capacity in parallel during Phase 15 — Phase 16 removes HappinessManager's copy

### Capacity Formula
- `BaseCapacity + (segmentCount - 1)` — +1 citizen per extra segment
- Bunk Pod: 1-seg=2, 2-seg=3. Sky Loft: 1-seg=4, 2-seg=5, 3-seg=6
- HousingManager.ComputeCapacity(definition, segmentCount) is the authority — formula lives in one place
- Trust BuildManager for segment count validation — no double-checking in HousingManager

### Assignment Algorithm
- Fewest-occupants-first (even spread), ties broken randomly (PRD §1)
- "Oldest" means spawn/arrival order (position in CitizenManager's list)
- Batch assignment: all eligible citizens assigned in same frame when a new room is built
- No staggered delays — since there's no visual feedback in Phase 15, staggering is invisible

### Save/Load
- Restore from saved HomeSegmentIndex — rebuild HousingManager's internal mapping from save data
- Do NOT re-run assignment algorithm on load (would shuffle homes)
- Stale assignments (saved home references demolished room) handled as unhoused, then reassigned

### Claude's Discretion
- Internal data structure for citizen→room mapping (Dictionary, List, etc.)
- Exact method signatures and API surface on HousingManager
- How to obtain placed room segment count from BuildManager
- Whether to use a public method or property for unhoused citizen queries

</decisions>

<specifics>
## Specific Ideas

No specific requirements — follow established patterns from HappinessManager (capacity tracking), GameEvents (event emission), and SaveManager (autosave wiring). The PRD at `docs/prd-housing.md` is the design reference.

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- HousingManager.cs: Phase 14 skeleton with Instance singleton and Config export — ready for full implementation
- GameEvents.cs: CitizenAssignedHome/CitizenUnhoused events already defined (Phase 14) — just need to call EmitCitizenAssignedHome/EmitCitizenUnhoused
- SaveManager.cs: Existing autosave pattern with event subscriptions — add housing events to the same pattern
- HappinessManager.cs: _housingRoomCapacities Dictionary<int, int> pattern — reuse same approach in HousingManager
- RoomDefinition.cs: BaseCapacity field on room definitions — input to capacity formula
- CitizenManager.cs: SpawnCitizen/SpawnCitizenFromSave APIs and _citizens list — integration points for assignment

### Established Patterns
- Event-driven: Systems subscribe to GameEvents in _Ready(), unsubscribe in cleanup. SafeNode handles lifecycle.
- Capacity tracking: HappinessManager uses Dictionary<anchorIndex, capacity> to survive demolish (room gone before event fires) — same pattern needed
- Autoload singletons: Instance property set in _EnterTree() or _Ready(). Access via ClassName.Instance.
- Save wiring: SaveManager stores Action delegates, subscribes in SubscribeToGameEvents(), unsubscribes in UnsubscribeFromGameEvents()

### Integration Points
- RoomPlaced event (string roomType, int segmentIndex) — trigger to register housing room and assign unhoused citizens
- RoomDemolished event (int segmentIndex) — trigger to unregister housing room and reassign/unhouse displaced citizens
- CitizenArrived event (string citizenName) — trigger to attempt home assignment for new citizen
- BuildManager.Instance.GetPlacedRoom(segmentIndex) — query for room definition and actual segment count
- CitizenManager.Instance.Citizens — read-only list for iterating unhoused citizens
- SaveManager — wire CitizenAssignedHome/CitizenUnhoused to autosave trigger

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 15-housingmanager-core*
*Context gathered: 2026-03-06*
