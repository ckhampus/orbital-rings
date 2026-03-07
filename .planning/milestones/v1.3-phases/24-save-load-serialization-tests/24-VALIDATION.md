---
phase: 24
slug: save-load-serialization-tests
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-07
---

# Phase 24 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Chickensoft.GoDotTest 2.0.30 + Shouldly 4.3.0 |
| **Config file** | Orbital Rings.csproj (conditional PackageReference) |
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
| 24-01-01 | 01 | 1 | SAVE-01 | unit | `godot --headless --run-tests --quit-on-finish` | ❌ W0 | ⬜ pending |
| 24-01-02 | 01 | 1 | SAVE-02 | unit | `godot --headless --run-tests --quit-on-finish` | ❌ W0 | ⬜ pending |
| 24-01-03 | 01 | 1 | SAVE-03 | unit | `godot --headless --run-tests --quit-on-finish` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Tests/Save/SaveDataTests.cs` — stubs for SAVE-01, SAVE-02, SAVE-03 + empty collections edge case

*Existing infrastructure covers framework and fixture requirements.*

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
