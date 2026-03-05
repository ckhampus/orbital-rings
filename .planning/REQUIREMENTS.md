# Requirements: Orbital Rings

**Defined:** 2026-03-04
**Core Value:** The wish-driven building loop: citizens express wishes, the player builds rooms to fulfill them, happiness rises, new citizens arrive, new wishes emerge.

## v1.1 Requirements

Requirements for the Happiness v2 milestone. Each maps to roadmap phases.

### Happiness Core

- [x] **HCORE-01**: Lifetime happiness increments by 1 on each wish fulfilled (integer, never decreases)
- [x] **HCORE-02**: Station mood is a float that rises on wish fulfillment (flat gain, no diminishing returns)
- [x] **HCORE-03**: Station mood decays toward a baseline each frame using exponential smoothing
- [x] **HCORE-04**: Mood baseline rises with sqrt(lifetime happiness) so mature stations rest above Quiet
- [x] **HCORE-05**: Blueprint unlocks trigger at wish count thresholds (4, 12) instead of percentage thresholds

### Mood Tiers

- [x] **TIER-01**: Five named mood tiers (Quiet/Cozy/Lively/Vibrant/Radiant) with defined mood ranges
- [x] **TIER-02**: Citizen arrival probability scales by current mood tier
- [x] **TIER-03**: Economy income multiplier scales by current mood tier (1.0x to 1.4x)
- [x] **TIER-04**: Hysteresis on tier demotion boundaries prevents rapid tier oscillation

### HUD

- [x] **HUD-01**: HUD displays lifetime wish counter as "♥ N" with pulse animation on increment
- [x] **HUD-02**: HUD displays current mood tier name with tier-colored text

### Save Format

- [x] **SAVE-01**: Save format stores lifetime happiness and mood values (fresh saves only, no v1 migration)

## Future Requirements

Deferred to later milestones. Tracked but not in current roadmap.

### Mood Communication

- **MCOM-01**: Floating text notification on mood tier change ("Station mood: Lively")
- **MCOM-02**: Tier label pulse/glow when mood is near the top of its range (about to promote)

### Progression

- **PROG-01**: Additional blueprint unlock milestones at wish counts 30, 50, 100
- **PROG-02**: Cosmetic unlocks tied to lifetime happiness milestones

### Mood Tuning

- **TUNE-01**: Tier-aware decay rates (lower tiers decay slower to protect early-game feel)
- **TUNE-02**: Configurable tuning values exposed via Inspector resource

### Save Migration

- **MIGR-01**: v1 save migration estimating lifetime happiness from old happiness float
- **MIGR-02**: Migration sets mood to computed baseline on load

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Raw mood float in player UI | Anti-feature — optimization anxiety is anti-cozy (research: Spiritfarer hides numeric values) |
| Mood decay toward zero | Anti-pattern — violates cozy genre promise; rising baseline is core to the design |
| Multiple mood dimensions (station + room + citizen) | Scope explosion — single station mood is sufficient |
| Diminishing returns on mood gain per wish | Unnecessary — decay provides the natural ceiling |
| Blocking/modal tier change notifications | Too intrusive for cozy genre |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| HCORE-01 | Phase 10 | Complete |
| HCORE-02 | Phase 10 | Complete |
| HCORE-03 | Phase 10 | Complete |
| HCORE-04 | Phase 10 | Complete |
| HCORE-05 | Phase 10 | Complete |
| TIER-01 | Phase 10 | Complete |
| TIER-02 | Phase 11 | Complete |
| TIER-03 | Phase 11 | Complete |
| TIER-04 | Phase 10 | Complete |
| HUD-01 | Phase 13 | Complete |
| HUD-02 | Phase 13 | Complete |
| SAVE-01 | Phase 12 | Complete |

**Coverage:**
- v1.1 requirements: 12 total
- Mapped to phases: 12
- Unmapped: 0

---
*Requirements defined: 2026-03-04*
*Last updated: 2026-03-04 after roadmap creation (traceability complete)*
