---
phase: 26
slug: station-clock-foundation
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-07
---

# Phase 26 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Chickensoft.GoDotTest 2.0.30 + Shouldly 4.3.0 |
| **Config file** | `Orbital Rings.csproj` (conditional RUN_TESTS compilation) |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `godot --headless --run-tests --quit-on-finish` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet format && dotnet build`
- **After every plan wave:** Run `godot --headless --run-tests --quit-on-finish`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 26-01-01 | 01 | 1 | CLOCK-01 | unit | `godot --headless --run-tests --quit-on-finish` | ❌ W0 | ⬜ pending |
| 26-01-02 | 01 | 1 | CLOCK-01 | unit | `godot --headless --run-tests --quit-on-finish` | ❌ W0 | ⬜ pending |
| 26-01-03 | 01 | 1 | CLOCK-02 | unit | `godot --headless --run-tests --quit-on-finish` | ❌ W0 | ⬜ pending |
| 26-02-01 | 02 | 2 | CLOCK-03 | manual | Visual HUD check in editor | N/A | ⬜ pending |
| 26-02-02 | 02 | 2 | CLOCK-03 | manual | Visual HUD check in editor | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Tests/Clock/ClockTests.cs` — stubs for CLOCK-01, CLOCK-02 (period computation, cycle wrapping, weight proportions)
- [ ] `StationClock.Reset()` method — needed for test isolation via TestHelper

*Existing infrastructure (GoDotTest + Shouldly) covers framework needs.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| ClockHUD displays period icon + label | CLOCK-03 | Requires visual rendering in Godot scene tree | Run scene, verify sun/moon icon and period name display in top-right HUD |
| ClockHUD scale pop animation on period change | CLOCK-03 | Animation requires visual verification | Set short cycle (10s), watch for 1.15x elastic bounce on period transitions |
| ClockConfig fields visible in Inspector | CLOCK-02 | Requires Godot editor UI | Open default_clock.tres in Inspector, verify all fields editable |
| Period-specific warm colors display correctly | CLOCK-03 | Color appearance requires visual check | Cycle through periods, verify gold/white/amber/blue palette |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
