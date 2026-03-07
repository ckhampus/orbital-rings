---
phase: 25
slug: singleton-integration-tests
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-07
---

# Phase 25 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Chickensoft.GoDotTest 2.0.30 |
| **Config file** | Tests/TestRunner.tscn + Tests/TestRunner.cs |
| **Quick run command** | `godot --headless --run-tests --quit-on-finish` |
| **Full suite command** | `godot --headless --run-tests --quit-on-finish` |
| **Estimated runtime** | ~5 seconds |

---

## Sampling Rate

- **After every task commit:** Run `godot --headless --run-tests --quit-on-finish`
- **After every plan wave:** Run `godot --headless --run-tests --quit-on-finish`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 5 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 25-01-01 | 01 | 1 | INTG-04 | integration | `godot --headless --run-tests --quit-on-finish` | ❌ W0 | ⬜ pending |
| 25-01-02 | 01 | 1 | INTG-04 | integration | `godot --headless --run-tests --quit-on-finish` | ❌ W0 | ⬜ pending |
| 25-01-03 | 01 | 1 | INTG-04 | integration | `godot --headless --run-tests --quit-on-finish` | ❌ W0 | ⬜ pending |
| 25-01-04 | 01 | 1 | INTG-04 | integration | `godot --headless --run-tests --quit-on-finish` | ❌ W0 | ⬜ pending |
| 25-01-05 | 01 | 1 | INTG-05 | integration | `godot --headless --run-tests --quit-on-finish` | ❌ W0 | ⬜ pending |
| 25-01-06 | 01 | 1 | INTG-05 | integration | `godot --headless --run-tests --quit-on-finish` | ❌ W0 | ⬜ pending |
| 25-01-07 | 01 | 1 | INTG-06 | integration | `godot --headless --run-tests --quit-on-finish` | ❌ W0 | ⬜ pending |
| 25-01-08 | 01 | 1 | INTG-06 | integration | `godot --headless --run-tests --quit-on-finish` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Tests/Integration/SingletonIntegrationTests.cs` — integration tests for INTG-04, INTG-05, INTG-06
- [ ] Production: `HousingManager.SubscribeToEvents()` + `SeedRoomForTest()` methods
- [ ] Production: `HappinessManager.SubscribeToEvents()` method
- [ ] Production: `EconomyManager.SubscribeToEvents()` method (if needed for tier propagation)
- [ ] Infrastructure: `TestHelper.ResubscribeAllSingletons()` orchestrator
- [ ] Infrastructure: `GameTestClass.[Setup]` updated to call resubscribe after reset

*Existing test infrastructure covers framework setup. Production changes add test-support APIs following established Reset() pattern.*

---

## Manual-Only Verifications

*All phase behaviors have automated verification.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 5s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
