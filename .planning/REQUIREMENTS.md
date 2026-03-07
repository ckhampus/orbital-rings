# Requirements: Orbital Rings

**Defined:** 2026-03-07
**Core Value:** The wish-driven building loop: citizens express wishes, the player builds rooms to fulfill them, happiness rises, new citizens arrive, new wishes emerge.

## v1.3 Requirements

Requirements for the Testing milestone. Each maps to roadmap phases.

### Framework Setup

- [x] **FRMW-01**: Test runner scene discovers and executes test classes via GoDotTest
- [x] **FRMW-02**: Tests run headless via command-line (`--run-tests --quit-on-finish`)
- [x] **FRMW-03**: Shouldly assertion library available in test code
- [x] **FRMW-04**: Test files excluded from release/export builds
- [x] **FRMW-05**: NuGet.Config updated to restore testing packages from nuget.org

### Unit Tests — Mood System

- [x] **MOOD-01**: MoodSystem decay math produces correct values over time
- [x] **MOOD-02**: MoodSystem tier transitions fire at correct thresholds
- [x] **MOOD-03**: MoodSystem hysteresis prevents rapid tier oscillation near boundaries
- [x] **MOOD-04**: MoodSystem wish gain increments mood correctly
- [x] **MOOD-05**: MoodSystem restore reconstructs state from saved values

### Unit Tests — Economy

- [x] **ECON-01**: CalculateRoomCost returns correct costs for all room types and sizes
- [x] **ECON-02**: CalculateTickIncome applies tier multiplier correctly across all 5 tiers
- [x] **ECON-03**: CalculateDemolishRefund returns correct partial refund

### Unit Tests — Housing

- [x] **HOUS-01**: ComputeCapacity returns correct values for all segment sizes (1/2/3)

### Unit Tests — Save/Load

- [ ] **SAVE-01**: SaveData round-trips through JSON serialization without data loss
- [ ] **SAVE-02**: v1 format JSON deserializes with correct defaults for missing v2/v3 fields
- [ ] **SAVE-03**: v2 format JSON deserializes with correct defaults for missing v3 fields

### Integration Infrastructure

- [x] **INTG-01**: Singleton reset infrastructure clears state between test suites
- [x] **INTG-02**: GameEvents subscribers cleared between test suites (no stale delegates)
- [x] **INTG-03**: Singleton timers suppressed during test execution

### Integration Tests

- [ ] **INTG-04**: Housing assignment distributes citizens with fewest-occupants-first
- [ ] **INTG-05**: Housing reassignment handles room demolition gracefully
- [ ] **INTG-06**: Mood tier change propagates correct economy multiplier

## Future Requirements

### Testing Expansion

- **TFUT-01**: Wish fulfillment end-to-end test (full scene with citizen spawning + room placement)
- **TFUT-02**: Code coverage collection via Coverlet
- **TFUT-03**: CI pipeline running tests on push/PR
- **TFUT-04**: Custom GodotTestDriver drivers for game-specific nodes
- **TFUT-05**: Snapshot testing for save data format drift detection
- **TFUT-06**: VSCode debug launch configurations for test debugging

## Out of Scope

| Feature | Reason |
|---------|--------|
| Parallel test execution | GoDotTest does not support it; race conditions with scene tree and singletons |
| Mocking framework (Moq-style) | Singletons use static instances, not interfaces; retrofitting would change production code |
| Performance/benchmark testing | No performance concerns at current scale |
| Visual regression testing | Procedural geometry makes screenshot comparison brittle and low-value |
| Mutation testing | Excessive tooling complexity for solo-developer game |
| xUnit/NUnit adapter | Runs outside Godot process; no access to scene tree or Godot APIs |
| Separate test .csproj | Breaks internal member access and complicates Godot build pipeline |
| Testing procedural mesh/audio | Rendering concern, not logic; no meaningful assertions possible |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| FRMW-01 | Phase 20 | Complete |
| FRMW-02 | Phase 20 | Complete |
| FRMW-03 | Phase 20 | Complete |
| FRMW-04 | Phase 20 | Complete |
| FRMW-05 | Phase 20 | Complete |
| MOOD-01 | Phase 22 | Complete |
| MOOD-02 | Phase 22 | Complete |
| MOOD-03 | Phase 22 | Complete |
| MOOD-04 | Phase 22 | Complete |
| MOOD-05 | Phase 22 | Complete |
| ECON-01 | Phase 23 | Complete |
| ECON-02 | Phase 23 | Complete |
| ECON-03 | Phase 23 | Complete |
| HOUS-01 | Phase 23 | Complete |
| SAVE-01 | Phase 24 | Pending |
| SAVE-02 | Phase 24 | Pending |
| SAVE-03 | Phase 24 | Pending |
| INTG-01 | Phase 21 | Complete |
| INTG-02 | Phase 21 | Complete |
| INTG-03 | Phase 21 | Complete |
| INTG-04 | Phase 25 | Pending |
| INTG-05 | Phase 25 | Pending |
| INTG-06 | Phase 25 | Pending |

**Coverage:**
- v1.3 requirements: 23 total
- Mapped to phases: 23
- Unmapped: 0

---
*Requirements defined: 2026-03-07*
*Last updated: 2026-03-07 after roadmap creation (traceability complete)*
