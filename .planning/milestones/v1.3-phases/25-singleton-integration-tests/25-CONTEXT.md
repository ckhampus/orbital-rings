# Phase 25: Singleton Integration Tests - Context

**Gathered:** 2026-03-07
**Status:** Ready for planning

<domain>
## Phase Boundary

Cross-singleton coordination is verified — housing assignment, demolition reassignment, and mood-economy propagation work through live autoload singletons. Covers requirements INTG-04 through INTG-06. Tests exercise real event bus wiring and singleton interactions without a full game scene.

</domain>

<decisions>
## Implementation Decisions

### Scene dependency strategy
- Use HousingManager.RestoreFromSave() to pre-seed housing state, bypassing BuildManager entirely
- No minimal test scene, no test-only seeding APIs on BuildManager
- For INTG-04: Hybrid approach — RestoreFromSave() sets up rooms with known capacities, then EmitCitizenArrived events trigger the live fewest-occupants-first algorithm
- For INTG-05: RestoreFromSave() populates state, then EmitRoomDemolished(anchorIndex) triggers the real demolition handler (HousingManager uses its cached _housingRoomCapacities, doesn't need BuildManager)
- For INTG-06: EmitWishFulfilled() repeatedly through the full chain (GameEvents → HappinessManager.OnWishFulfilled → MoodSystem → EconomyManager.SetMoodTier)

### Test scenarios
- **INTG-04 (housing assignment):** Core + targeted edges
  - Even distribution across 2-3 rooms (no room exceeds others by >1 occupant)
  - Single room gets all citizens
  - Rooms with different capacities (1-seg vs 3-seg)
  - All rooms full — additional citizen remains unhoused
- **INTG-05 (demolish/reassign):** Core + no-room edge
  - Demolish a room with 2+ citizens, verify redistribution to remaining rooms
  - Demolish the ONLY housing room — verify citizens become unhoused (CitizenUnhoused fires, GetHomeForCitizen returns null)
- **INTG-06 (mood→economy):** Tier change + income delta
  - Pump wishes until mood crosses from Quiet to Cozy
  - Verify MoodTierChanged event fires with correct old/new tiers
  - Verify CalculateTickIncome() returns higher value after tier change

### Event chain depth
- Verify key intermediate events, not just final state
- Assert CitizenAssignedHome fires with correct (name, segmentIndex) during assignment
- Assert CitizenUnhoused fires with correct citizenName during demolition
- Assert MoodTierChanged fires with correct (newTier, previousTier) during wish fulfillment
- Use inline lambdas capturing local variables for event subscription (e.g., `var assigned = new List<string>(); GameEvents.Instance.CitizenAssignedHome += (name, _) => assigned.Add(name);`)
- No reusable event recorder helper — keep it explicit and inline

### Test file organization
- Single file: `Tests/Integration/SingletonIntegrationTests.cs`
- All 3 requirements in one file with comment-header sections: `// --- INTG-04: Housing Assignment ---`, `// --- INTG-05: Demolition Reassignment ---`, `// --- INTG-06: Mood-Economy Propagation ---`
- Namespace: `OrbitalRings.Tests.Integration`
- Extends `GameTestClass` (singleton reset between tests)
- Behavior-focused method names consistent with prior phases

### Claude's Discretion
- Exact RestoreFromSave() input data (citizen names, home segment indices, room configurations)
- Number of wishes needed to cross Quiet→Cozy threshold (depends on MoodGainPerWish=0.06 and threshold=0.10)
- Whether to set up EconomyManager citizen/worker counts for INTG-06 income comparison
- Helper method design for constructing RestoreFromSave() input tuples
- Test method ordering within each requirement section

</decisions>

<specifics>
## Specific Ideas

- HousingManager.OnRoomDemolished uses its internal _housingRoomCapacities cache, not BuildManager — confirmed safe for headless testing
- HappinessManager.OnWishFulfilled calls EconomyManager.Instance.SetMoodTier(newTier) directly on tier change — the full propagation chain works without a scene
- GameTestClass auto-resets all singletons via [Setup], which also clears event subscribers — no manual teardown needed for inline lambda subscriptions

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `HousingManager.RestoreFromSave(IReadOnlyList<(string citizenName, int? homeIndex)>)`: Pre-seeds housing state without BuildManager
- `GameEvents.EmitCitizenArrived(string)`: Triggers HousingManager.OnCitizenArrived → FindBestRoom → AssignCitizen
- `GameEvents.EmitRoomDemolished(int)`: Triggers HousingManager.OnRoomDemolished → displacement + reassignment
- `GameEvents.EmitWishFulfilled(string, string)`: Triggers HappinessManager → MoodSystem → EconomyManager chain
- `HousingManager.GetHomeForCitizen(string)`, `GetOccupantCount(int)`, `GetOccupants(int)`: Query APIs for assertions
- `EconomyManager.CalculateTickIncome()`: Pure income calculation for before/after comparison
- `EconomyManager.SetCitizenCount(int)`, `SetWorkingCitizenCount(int)`: State setup for income tests

### Established Patterns
- `GameTestClass` base class with [Setup] ResetAllSingletons (Phase 21)
- Comment-header grouping by requirement (Phases 22-24)
- Shouldly assertions: `.ShouldBe()`, `.ShouldBeNull()`, `.ShouldNotBeNull()`
- Behavior-focused method names: `AssignmentDistributesEvenly`, `DemolishReassignsCitizens`
- No `#region` directives, no custom wrappers

### Integration Points
- `Tests/Integration/SingletonIntegrationTests.cs`: New file alongside existing SingletonResetTests.cs and GameEventsTests.cs
- No production code changes needed — all APIs are already public
- RestoreFromSave() internally calls InitializeExistingRooms which reads BuildManager, but operates on its own cache after initialization

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 25-singleton-integration-tests*
*Context gathered: 2026-03-07*
