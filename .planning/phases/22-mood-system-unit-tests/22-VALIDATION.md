---
phase: 22
slug: mood-system-unit-tests
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-07
---

# Phase 22 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Chickensoft.GoDotTest 2.0.30 + Shouldly 4.3.0 |
| **Config file** | `Orbital Rings.csproj` (conditional PackageReferences) |
| **Quick run command** | `godot --headless --scene-path res://Tests/TestRunner.tscn -- --run-tests=MoodSystemTests --quit-on-finish` |
| **Full suite command** | `godot --headless --scene-path res://Tests/TestRunner.tscn -- --run-tests --quit-on-finish` |
| **Estimated runtime** | ~5 seconds |

---

## Sampling Rate

- **After every task commit:** Run `godot --headless --scene-path res://Tests/TestRunner.tscn -- --run-tests=MoodSystemTests --quit-on-finish`
- **After every plan wave:** Run `godot --headless --scene-path res://Tests/TestRunner.tscn -- --run-tests --quit-on-finish`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 5 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 22-01-01 | 01 | 1 | MOOD-01 | unit | `--run-tests=MoodSystemTests` | ❌ W0 | ⬜ pending |
| 22-01-02 | 01 | 1 | MOOD-02 | unit | `--run-tests=MoodSystemTests` | ❌ W0 | ⬜ pending |
| 22-01-03 | 01 | 1 | MOOD-03 | unit | `--run-tests=MoodSystemTests` | ❌ W0 | ⬜ pending |
| 22-01-04 | 01 | 1 | MOOD-04 | unit | `--run-tests=MoodSystemTests` | ❌ W0 | ⬜ pending |
| 22-01-05 | 01 | 1 | MOOD-05 | unit | `--run-tests=MoodSystemTests` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Tests/Mood/MoodSystemTests.cs` — stubs for MOOD-01 through MOOD-05
- [ ] Remove `Tests/Mood/.gitkeep` when MoodSystemTests.cs is added

*(Wave 0 gap IS the phase itself — this phase creates the test file)*

*Existing infrastructure covers test framework and runner — no new installs needed.*

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
