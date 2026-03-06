# Requirements: Orbital Rings

**Defined:** 2026-03-05
**Core Value:** The wish-driven building loop: citizens express wishes, the player builds rooms to fulfill them, happiness rises, new citizens arrive, new wishes emerge.

## v1.2 Requirements

Requirements for the Housing milestone. Each maps to roadmap phases.

### Home Assignment

- [ ] **HOME-01**: Citizens are automatically assigned to a housing room on arrival (fewest-occupants-first)
- [ ] **HOME-02**: Citizens are reassigned when their home room is demolished (or become unhoused)
- [ ] **HOME-03**: Unhoused citizens are assigned when new housing rooms are built (oldest-first)
- [ ] **HOME-04**: Housing capacity scales with room size (BaseCapacity + segments - 1)
- [ ] **HOME-05**: Unhoused citizens function identically to housed ones (no penalty, no debuff)

### Return-Home Behavior

- [ ] **BEHV-01**: Housed citizens periodically return to their home room (90-150s cycle)
- [ ] **BEHV-02**: Home rest lasts 8-15s with a "Zzz" FloatingText indicator
- [ ] **BEHV-03**: Citizen wish timer pauses during home rest
- [ ] **BEHV-04**: Home return is lower priority than active wish fulfillment

### UI & Feedback

- [ ] **UI-01**: CitizenInfoPanel shows home room name and location (or "--" if unhoused)
- [ ] **UI-02**: Housing room tooltip shows current resident names
- [ ] **UI-03**: PopulationDisplay shows count/capacity format (e.g., "5/7")

### Infrastructure

- [x] **INFR-01**: New HousingManager autoload singleton owns citizen-to-room mapping
- [x] **INFR-02**: HousingConfig resource with Inspector-tunable timing constants
- [ ] **INFR-03**: Housing capacity tracking transferred from HappinessManager to HousingManager
- [ ] **INFR-04**: Save/load housing assignments with backward compatibility (v2 saves load as unhoused)
- [x] **INFR-05**: Save format bumped to v3 with nullable HomeSegmentIndex

## Future Requirements

Deferred from v1.2. Tracked but not in current roadmap.

### Notifications & Polish

- **NOTF-01**: Tier change notification (floating text on mood tier transition)
- **NOTF-02**: Additional blueprint unlock milestones at wish counts 30, 50, 100
- **NOTF-03**: Cosmetic unlocks tied to lifetime happiness milestones

### Save Migration

- **MIGR-01**: Save migration from v1 single happiness float to v2 dual values

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Housing quality tiers or upgrades | Adds management burden, violates cozy philosophy |
| Player-managed room assignments (drag-to-assign) | Micromanagement, anti-cozy |
| Citizen housing preferences or roommate compatibility | Combinatorial optimization problem, deferred complexity |
| Negative consequences for unhoused citizens | Violates no-fail-state philosophy |
| Room interior customization | Placeholder interiors sufficient for v1.x |
| Day/night cycle driving home-return timing | Cosmetic system deferred |
| Home-return path indicator / visual trail | Polish, defer to v1.3+ |
| Citizen roommate display in info panel | Minor UI scope, defer to v1.3+ |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| HOME-01 | Phase 15 | Pending |
| HOME-02 | Phase 15 | Pending |
| HOME-03 | Phase 15 | Pending |
| HOME-04 | Phase 15 | Pending |
| HOME-05 | Phase 15 | Pending |
| BEHV-01 | Phase 17 | Pending |
| BEHV-02 | Phase 17 | Pending |
| BEHV-03 | Phase 17 | Pending |
| BEHV-04 | Phase 17 | Pending |
| UI-01 | Phase 18 | Pending |
| UI-02 | Phase 18 | Pending |
| UI-03 | Phase 18 | Pending |
| INFR-01 | Phase 14, Phase 15 | Complete |
| INFR-02 | Phase 14 | Complete |
| INFR-03 | Phase 16 | Pending |
| INFR-04 | Phase 19 | Pending |
| INFR-05 | Phase 14, Phase 19 | Complete |

**Coverage:**
- v1.2 requirements: 17 total
- Mapped to phases: 17
- Unmapped: 0

**Coverage notes:**
- INFR-01 (HousingManager) spans Phase 14 (skeleton/autoload registration) and Phase 15 (full implementation). Primary delivery is Phase 15.
- INFR-05 (save format v3) spans Phase 14 (schema definition on SavedCitizen) and Phase 19 (serialization/deserialization wiring). Primary delivery is Phase 19.
- All other requirements map to exactly one phase.

---
*Requirements defined: 2026-03-05*
*Last updated: 2026-03-05 after roadmap creation (traceability populated)*
