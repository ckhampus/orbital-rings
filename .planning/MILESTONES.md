# Milestones

## v1.1 Happiness v2 (Shipped: 2026-03-05)

**Phases:** 4 | **Plans:** 7 | **Tasks:** 14
**Lines of code:** 8,331 C# | **Timeline:** 2 days (2026-03-04 to 2026-03-05)
**Git range:** `feat(10-01)` to `feat(13-01)` | **Requirements:** 12/12 satisfied

**Delivered:** Replaced the single happiness percentage with a dual-value system (Lifetime Happiness + Station Mood) so the core loop stays alive indefinitely — mature stations rest at higher baselines while mood dynamically fluctuates with player activity.

**Key accomplishments:**
1. Dual-value happiness: Lifetime Happiness (integer, never decreases) + Station Mood (float with exponential decay toward rising baseline)
2. Five mood tiers (Quiet/Cozy/Lively/Vibrant/Radiant) with hysteresis preventing rapid oscillation near boundaries
3. Tier-driven economy: income multiplier (1.0x–1.4x) and arrival probability keyed to discrete tiers instead of float
4. Save format v2 with version-gated backward compatibility (v1 saves load safely with defaults)
5. MoodHUD widget with pulse-animated wish counter and tier-colored mood label
6. Full deprecation cleanup: HappinessBar, HappinessChanged event, SetHappiness API, float-space economy all removed

**Archive:** `.planning/milestones/v1.1-ROADMAP.md` | `.planning/milestones/v1.1-REQUIREMENTS.md`

---

## v1.0 MVP (Shipped: 2026-03-04)

**Phases:** 9 | **Plans:** 25 | **Commits:** 161
**Lines of code:** 8,058 C# | **Timeline:** 3 days (2026-03-02 to 2026-03-04)
**Git range:** `feat(01-01)` to `feat(09-01)` | **Requirements:** 25/25 satisfied

**Delivered:** A complete cozy space station builder with the full build-wish-grow loop — players construct rooms on a ring, citizens arrive and express wishes, fulfilling wishes raises happiness, and the station grows.

**Key accomplishments:**
1. Godot 4 C# architecture with typed signal bus, SafeNode lifecycle, and orbital camera
2. Procedural ring mesh with 24 interactive segments using polar math picking (no physics bodies)
3. Spreadsheet-balanced credit economy with passive income, size-scaled costs, and rolling HUD
4. Room placement across 5 categories (10 types) with procedural audio snap feedback
5. Named citizens walking the ring walkway with drift-fade room visits
6. Wish system with speech bubbles driving build decisions across 4 wish categories
7. Happiness progression gating citizen arrivals and blueprint unlocks at milestones
8. Save/load with debounced autosave, ambient drone, wish celebration chime, and title screen
9. Work bonus economy flow wired end-to-end via citizen room visit events

**Archive:** `.planning/milestones/v1.0-ROADMAP.md` | `.planning/milestones/v1.0-REQUIREMENTS.md`

---

