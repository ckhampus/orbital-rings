# Requirements: Orbital Rings

**Defined:** 2026-03-02
**Core Value:** The wish-driven building loop: citizens express wishes, the player builds rooms to fulfill them, happiness rises, new citizens arrive, new wishes emerge.

## v1 Requirements

Requirements for initial release (single playable ring proving the core loop).

### Ring Structure

- [ ] **RING-01**: Player sees a flat donut ring divided into 12 outer and 12 inner segments with a walkway between them
- [ ] **RING-02**: Player can orbit the camera horizontally around the ring and zoom in/out at a fixed tilt angle
- [ ] **RING-03**: Segments are visually distinct (numbered positions, inner vs outer clearly readable)

### Building

- [ ] **BLDG-01**: Player can place a room into 1-3 adjacent empty segments in the outer or inner row
- [ ] **BLDG-02**: Player can choose from 5 room categories: Housing, Life Support, Work, Comfort, Utility
- [ ] **BLDG-03**: Each room category has at least 2 specific room types with visually distinct placeholder art
- [ ] **BLDG-04**: Player hears a satisfying snap sound and sees a visual response when placing a room
- [ ] **BLDG-05**: Player can demolish any room and receive a partial credit refund
- [ ] **BLDG-06**: Larger rooms (2-3 segments) are more effective but cost more, with a slight size discount

### Economy

- [ ] **ECON-01**: Player starts with enough credits to place a few rooms on an empty ring
- [ ] **ECON-02**: Each citizen generates a small passive credit income over time
- [ ] **ECON-03**: Citizens assigned to work rooms generate bonus credits
- [ ] **ECON-04**: Higher station happiness slightly multiplies credit generation
- [ ] **ECON-05**: Room costs scale by segment size with diminishing returns for larger rooms

### Citizens

- [ ] **CTZN-01**: Named citizens with distinct appearances arrive at the station as happiness grows
- [ ] **CTZN-02**: Citizens walk along the walkway visibly
- [ ] **CTZN-03**: Citizens visit rooms based on spatial attraction (gravitating toward relevant rooms)
- [ ] **CTZN-04**: Player can click a citizen to see their name and current wish

### Wishes

- [ ] **WISH-01**: Citizens express wishes via speech bubbles (e.g., "I'd love a place to stargaze")
- [ ] **WISH-02**: Fulfilling a wish grants happiness to that citizen and a small global happiness boost
- [ ] **WISH-03**: Unfulfilled wishes linger harmlessly — nothing bad happens
- [ ] **WISH-04**: Wishes span categories: social, comfort, curiosity, variety

### Progression

- [ ] **PROG-01**: Station happiness is tracked and visible to the player
- [ ] **PROG-02**: New citizens arrive passively as happiness grows
- [ ] **PROG-03**: New room blueprints unlock at happiness milestones (at least 2-3 unlock moments)

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Visual Polish

- **VISL-01**: Procedural room interior generation (furniture layout, detail variation, color variation)
- **VISL-02**: Day/night ambient lighting cycle (cosmetic, no mechanical effect)
- **VISL-03**: Room-entry animations and citizen reactions to wish fulfillment

### Citizen Depth

- **CDEP-01**: Citizen personality traits (2-3 per citizen) influencing behavior and wishes
- **CDEP-02**: Daily routines (housing → work → comfort → housing)
- **CDEP-03**: Citizen relationships and shared wishes
- **CDEP-04**: Favorite rooms that develop over time

### Expansion

- **EXPN-01**: Multi-ring vertical expansion (new rings stacked on top)
- **EXPN-02**: Elevator shafts connecting rings
- **EXPN-03**: New room types and citizen tiers per ring level

### Events

- **EVNT-01**: Random positive events (visitors, celestial events, citizen milestones)
- **EVNT-02**: Work room discoveries (new furniture variants, cosmetic upgrades)

### Quality of Life

- **QOLX-01**: Persistent wish board / notification panel
- **QOLX-02**: Save slots (beyond single autosave)

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Resource depletion / maintenance costs | Introduces punishment loop, breaks cozy promise |
| Multiple resource types (power, oxygen, water) | Complexity without coziness payoff — single currency is correct |
| Combat / threat events | Directly contradicts cozy promise |
| Complex citizen scheduling / pathfinding | Spatial attraction model is sufficient; full scheduling creates frustration when it breaks |
| Adjacency bonuses (explicit numerical) | Adds min-max optimization pressure, breaks "build what feels right" ethos |
| Multiplayer / shared stations | Scope explosion; cozy builders are fundamentally solo |
| Mobile / controller support | PC keyboard+mouse only for v1 |
| Tutorial system | Citizens' wishes are the implicit tutorial |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| RING-01 | Phase 2 | Pending |
| RING-02 | Phase 1 | Pending |
| RING-03 | Phase 2 | Pending |
| BLDG-01 | Phase 4 | Pending |
| BLDG-02 | Phase 4 | Pending |
| BLDG-03 | Phase 4 | Pending |
| BLDG-04 | Phase 4 | Pending |
| BLDG-05 | Phase 4 | Pending |
| BLDG-06 | Phase 4 | Pending |
| ECON-01 | Phase 3 | Pending |
| ECON-02 | Phase 3 | Pending |
| ECON-03 | Phase 3 | Pending |
| ECON-04 | Phase 3 | Pending |
| ECON-05 | Phase 3 | Pending |
| CTZN-01 | Phase 5 | Pending |
| CTZN-02 | Phase 5 | Pending |
| CTZN-03 | Phase 5 | Pending |
| CTZN-04 | Phase 5 | Pending |
| WISH-01 | Phase 6 | Pending |
| WISH-02 | Phase 6 | Pending |
| WISH-03 | Phase 6 | Pending |
| WISH-04 | Phase 6 | Pending |
| PROG-01 | Phase 7 | Pending |
| PROG-02 | Phase 7 | Pending |
| PROG-03 | Phase 7 | Pending |

**Coverage:**
- v1 requirements: 25 total
- Mapped to phases: 25
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-02*
*Last updated: 2026-03-02 after roadmap creation — all 25 requirements mapped*
