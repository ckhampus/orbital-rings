---
phase: 21
slug: integration-test-infrastructure
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-07
---

# Phase 21 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Chickensoft.GoDotTest 2.0.30 |
| **Config file** | `Orbital Rings.csproj` (conditional PackageReference) |
| **Quick run command** | `godot --headless --run-tests --quit-on-finish` |
| **Full suite command** | `godot --headless --run-tests --quit-on-finish` |
| **Estimated runtime** | ~10 seconds |

---

## Sampling Rate

- **After every task commit:** Run `godot --headless --run-tests --quit-on-finish`
- **After every plan wave:** Run `godot --headless --run-tests --quit-on-finish`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 10 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 21-01-01 | 01 | 1 | INTG-01 | unit | `godot --headless --run-tests --quit-on-finish` | ❌ W0 | ⬜ pending |
| 21-01-02 | 01 | 1 | INTG-01 | unit | `godot --headless --run-tests --quit-on-finish` | ❌ W0 | ⬜ pending |
| 21-01-03 | 01 | 1 | INTG-02 | unit | `godot --headless --run-tests --quit-on-finish` | ❌ W0 | ⬜ pending |
| 21-01-04 | 01 | 1 | INTG-03 | unit | `godot --headless --run-tests --quit-on-finish` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Tests/Infrastructure/TestHelper.cs` — core reset orchestrator
- [ ] `Tests/Infrastructure/GameTestClass.cs` — base class with [Setup] auto-reset
- [ ] `Tests/Integration/SingletonResetTests.cs` — verification tests for INTG-01, INTG-03
- [ ] `Tests/Integration/GameEventsTests.cs` — verification test for INTG-02

*These are the primary deliverables of this phase, not pre-existing gaps.*

---

## Manual-Only Verifications

*All phase behaviors have automated verification.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 10s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
