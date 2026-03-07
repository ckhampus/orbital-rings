# Phase 21: Integration Test Infrastructure - Context

**Gathered:** 2026-03-07
**Status:** Ready for planning

<domain>
## Phase Boundary

Singleton state isolation is reliable — tests that touch game singletons cannot corrupt each other. This phase delivers Reset() methods on each singleton, ClearAllSubscribers() on GameEvents, timer suppression, a central TestHelper, a GameTestClass base class, and verification tests proving the infrastructure works. CitizenNode per-instance timers are out of scope (singleton-level only).

</domain>

<decisions>
## Implementation Decisions

### Production code changes
- Each of the 8 autoload singletons gets a public `Reset()` method in production code
- `Reset()` clears mutable state (dictionaries, lists, counters, flags) to initial defaults
- `Reset()` stops owned timers (EconomyManager._incomeTimer, HappinessManager._arrivalTimer, SaveManager._debounceTimer)
- `Reset()` resets static `StateLoaded` flags to false (EconomyManager, HousingManager, CitizenManager)
- `Reset()` does NOT touch the `Instance` reference or `[Export] Config` resources (scene tree owns those)
- Each singleton returns to a "just loaded, no game data" state after Reset()

### GameEvents cleanup
- `GameEvents` gets a public `ClearAllSubscribers()` method in production code
- Nulls all ~30 C# event delegates (CameraOrbitStarted, SegmentHovered, RoomPlaced, etc.)
- Guarantees zero stale delegate leaks between test suites

### Central test helper
- `TestHelper` static class lives in `Tests/Infrastructure/`
- `TestHelper.ResetAllSingletons()` calls each singleton's `Reset()` plus `GameEvents.Instance.ClearAllSubscribers()`
- One call in test setup resets the entire game world

### Test base class
- `GameTestClass` extends GoDotTest's `TestClass`, lives in `Tests/Infrastructure/`
- Auto-calls `TestHelper.ResetAllSingletons()` in setup before each test
- Integration tests (Phase 25) extend `GameTestClass`
- Pure POCO tests (Phases 22-24) can extend `TestClass` directly — no mandatory base class
- No convenience accessors for singletons — tests use `EconomyManager.Instance` etc. directly

### Timer suppression
- Timers stopped inside each singleton's `Reset()` — no scene tree pausing or ProcessMode manipulation
- If a test needs to simulate an income tick or arrival check, it calls the underlying method directly (e.g., the callback method)
- CitizenNode per-instance timers (visit, wish, home) are out of scope for Phase 21 — singleton timers only per INTG-03

### Verification tests
- One test per singleton: dirties the singleton (sets fields, adds data), calls Reset(), verifies clean state
- One test for GameEvents: subscribes handlers, calls ClearAllSubscribers(), verifies zero subscribers
- These tests prove the infrastructure works before Phase 25 depends on it

### Claude's Discretion
- Exact fields and collections cleared in each singleton's Reset()
- Order of operations in ResetAllSingletons()
- GoDotTest setup/teardown lifecycle hooks used by GameTestClass
- How to verify "zero subscribers" on GameEvents (reflection vs. explicit null checks)
- Namespace for test infrastructure classes

</decisions>

<specifics>
## Specific Ideas

- Singletons should return to "just loaded, no game data" state — the same state they'd be in if the game scene just started with no save file
- Stop-only for timers, no manual trigger wrappers — tests that need to simulate ticks call the callback directly

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- All 8 singletons follow identical pattern: `static Instance` set in `_EnterTree()`, mutable state in private fields
- `GameEvents` uses pure C# event delegates (not Godot signals) — nulling delegates is safe and complete
- `StateLoaded` static flag exists on EconomyManager, HousingManager, CitizenManager — controls _Ready() skip behavior

### Established Patterns
- Singletons hold state in private dictionaries/lists: `_housingRoomCapacities`, `_roomOccupants`, `_citizenHomes` (HousingManager), `_placedRoomTypes` (BuildManager), `_activeWishes` (WishBoard)
- Timers are child Timer nodes created in `_Ready()`: EconomyManager._incomeTimer, HappinessManager._arrivalTimer, SaveManager._debounceTimer
- HousingManager stores delegate references for clean unsubscription: `_onRoomPlaced`, `_onRoomDemolished`, `_onCitizenArrived`

### Integration Points
- Each singleton .cs file gets a `Reset()` method added
- `GameEvents.cs` gets `ClearAllSubscribers()` method added
- `Tests/Infrastructure/` directory created for GameTestClass.cs and TestHelper.cs
- Phase 20's smoke test in `Tests/Domain/` validates framework still works after infrastructure additions

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 21-integration-test-infrastructure*
*Context gathered: 2026-03-07*
