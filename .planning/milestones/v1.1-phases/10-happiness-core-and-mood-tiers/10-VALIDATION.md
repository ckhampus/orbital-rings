---
phase: 10
slug: happiness-core-and-mood-tiers
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-04
---

# Phase 10 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | None — Godot 4 C# project, no test runner detected |
| **Config file** | None — project validates by running the game |
| **Quick run command** | Run game → observe Happiness debug output |
| **Full suite command** | Run game → complete full wish-fulfill-idle cycle |
| **Estimated runtime** | ~5–10 minutes manual play |

> **Note:** This Godot project has no automated test infrastructure. MoodSystem is designed as a POCO to be testable, but the project's validation pattern is in-game observation. All verifications below are manual.

---

## Sampling Rate

- **After every task commit:** Run game briefly, check debug output for no regressions
- **After every plan wave:** Run full manual verification cycle for that wave's requirements
- **Before `/gsd:verify-work`:** All success criteria must be observed green
- **Max feedback latency:** ~10 minutes per wave

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 10-01-xx | 01 | 1 | HCORE-01 | Manual | N/A | N/A | ⬜ pending |
| 10-01-xx | 01 | 1 | HCORE-02 | Manual | N/A | N/A | ⬜ pending |
| 10-01-xx | 01 | 1 | HCORE-03 | Manual | N/A | N/A | ⬜ pending |
| 10-01-xx | 01 | 1 | HCORE-04 | Manual | N/A | N/A | ⬜ pending |
| 10-01-xx | 01 | 1 | HCORE-05 | Manual | N/A | N/A | ⬜ pending |
| 10-02-xx | 02 | 2 | TIER-01 | Manual | N/A | N/A | ⬜ pending |
| 10-02-xx | 02 | 2 | TIER-04 | Manual | N/A | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

No test framework setup required — project uses in-game validation.

*Existing infrastructure covers all phase requirements (manual observation pattern).*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Lifetime counter increments, never decreases | HCORE-01 | No test infra; requires game runtime | Fulfill 5 wishes; verify HappinessManager.LifetimeWishes shows 5, then 6 after next wish |
| Mood rises on wish fulfillment | HCORE-02 | Requires game runtime | Watch Happiness debug output — mood should jump by ~0.20 per wish |
| Mood decays toward baseline | HCORE-03 | Time-based behavior | Idle for 5 minutes after wish; verify mood drops toward baseline (not to zero) |
| Baseline rises with sqrt(lifetime) | HCORE-04 | Requires accumulated state | Fulfill 10 wishes, idle until stable; verify mood floor ~0.05, not zero |
| Blueprint unlocks at wish 4 and 12 | HCORE-05 | UI/event driven | Fulfill 4 wishes — blueprint unlock notification fires; fulfill 12 — second unlock fires |
| Five tiers with correct ranges | TIER-01 | Requires game runtime | GD.Print tier on each change; verify Quiet/Cozy/Lively/Vibrant/Radiant at correct thresholds |
| No rapid oscillation at tier boundary | TIER-04 | Emergent behavior | Tune mood to hover near 0.30 boundary; confirm tier stays stable (hysteresis working) |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 600s (10 min)
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
