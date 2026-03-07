# Requirements: Orbital Rings

**Defined:** 2026-03-07
**Core Value:** The wish-driven building loop: citizens express wishes, the player builds rooms to fulfill them, happiness rises, new citizens arrive, new wishes emerge.

## v1.4 Requirements

Requirements for Citizen AI & Day/Night Cycle milestone. Each maps to roadmap phases.

### Clock

- [x] **CLOCK-01**: Station cycles through Morning, Day, Evening, and Night periods in an 8-minute real-time loop
- [x] **CLOCK-02**: Clock cycle length and period proportions are configurable via Inspector resource
- [ ] **CLOCK-03**: Player can see the current station period via an ambient icon in the HUD

### Lighting

- [ ] **LIT-01**: Station lighting smoothly transitions between period-appropriate color/intensity presets at period boundaries
- [ ] **LIT-02**: Station backdrop shifts between period-appropriate appearances (deeper blue at night, lighter during day)

### Traits

- [ ] **TRAIT-01**: Each citizen is assigned one Interest trait at creation (Curious, Social, Green Thumb, or Industrious)
- [ ] **TRAIT-02**: Each citizen is assigned one Rhythm trait at creation (Night Owl, Early Bird, Homebody, or Wanderer)
- [ ] **TRAIT-03**: Citizen info panel displays both traits on a single line between body type and home

### Behavior

- [ ] **BHV-01**: Citizens operate in an explicit state machine with Walking, Evaluating, Visiting, and Resting states
- [ ] **BHV-02**: Citizens use a unified decision timer (15-30s) replacing separate visit and home timers
- [ ] **BHV-03**: Citizens select rooms via utility scoring (trait affinity, proximity, recency, wish bonus, jitter)
- [ ] **BHV-04**: Schedule templates bias citizen activity by time of day (more visits during Day, more rest at Night)
- [ ] **BHV-05**: Rhythm traits modify schedule weights (Night Owls prefer evening/night activity, Early Birds prefer morning)
- [ ] **BHV-06**: Wish bonus always outweighs trait affinity so wishes remain the primary behavior driver
- [ ] **BHV-07**: Citizens handle room demolition gracefully in any state (visiting or resting)

### Persistence

- [ ] **SAVE-01**: Station clock position is saved and restored on load
- [ ] **SAVE-02**: Citizen traits are saved and restored on load
- [ ] **SAVE-03**: v3 saves load correctly with deterministic trait assignment and clock defaulting to Morning

### UI

- [ ] **UI-01**: Room tooltip shows names of citizens currently visiting

## Future Requirements

Deferred to future milestones. Tracked but not in current roadmap.

### Visual Polish

- **VIS-01**: Room window emissives respond to day/night via global shader parameter
- **VIS-02**: Visible thinking indicator during Evaluating state

### Expanded Traits

- **ETRAIT-01**: Additional trait categories (e.g., Social for relationships)
- **ETRAIT-02**: Trait-influenced wish generation

### Time Controls

- **TIME-01**: Fast-forward / time speed controls

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Citizen relationships/friendships | Future milestone — trait system lays groundwork |
| Individual citizen mood values | No per-citizen happiness — station mood is collective |
| Player-directed citizen behavior | Automatic behavior is core to cozy philosophy |
| Complex needs simulation (hunger, energy) | Creates fail states, contradicts cozy philosophy |
| Mood penalties from unmet schedules | Optimization anxiety is anti-cozy |
| New room types or room-specific interactions | Existing 10 room types sufficient for AI showcase |
| Citizen speech or dialogue | Traits are shown by behavior, not words |
| Period-specific economy effects | Creates optimization pressure, breaks "build what feels right" |
| Player-configurable citizen schedules | Turns cozy builder into spreadsheet optimizer |
| Full darkness during Night period | Station must always feel warm and inviting |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| CLOCK-01 | Phase 26 | Complete |
| CLOCK-02 | Phase 26 | Complete |
| CLOCK-03 | Phase 26 | Pending |
| LIT-01 | Phase 27 | Pending |
| LIT-02 | Phase 27 | Pending |
| TRAIT-01 | Phase 28 | Pending |
| TRAIT-02 | Phase 28 | Pending |
| TRAIT-03 | Phase 28 | Pending |
| BHV-01 | Phase 30 | Pending |
| BHV-02 | Phase 30 | Pending |
| BHV-03 | Phase 29 | Pending |
| BHV-04 | Phase 29 | Pending |
| BHV-05 | Phase 29 | Pending |
| BHV-06 | Phase 29 | Pending |
| BHV-07 | Phase 30 | Pending |
| SAVE-01 | Phase 31 | Pending |
| SAVE-02 | Phase 31 | Pending |
| SAVE-03 | Phase 31 | Pending |
| UI-01 | Phase 32 | Pending |

**Coverage:**
- v1.4 requirements: 19 total
- Mapped to phases: 19
- Unmapped: 0

---
*Requirements defined: 2026-03-07*
*Last updated: 2026-03-07 after roadmap creation (traceability complete)*
