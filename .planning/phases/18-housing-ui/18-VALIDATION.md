---
phase: 18
slug: housing-ui
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-06
---

# Phase 18 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | None — no automated test infrastructure in project |
| **Config file** | None |
| **Quick run command** | `dotnet build` |
| **Full suite command** | Manual visual testing in Godot editor |
| **Estimated runtime** | ~10 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Manual visual check in Godot editor
- **Before `/gsd:verify-work`:** Full manual verification of all three UI changes
- **Max feedback latency:** 10 seconds (build)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 18-01-01 | 01 | 1 | UI-01 | manual-only | `dotnet build` | N/A | ⬜ pending |
| 18-01-02 | 01 | 1 | UI-02 | manual-only | `dotnet build` | N/A | ⬜ pending |
| 18-01-03 | 01 | 1 | UI-03 | manual-only | `dotnet build` | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. `dotnet build` is already available. No test framework installation needed.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Home label shows room name + location for housed citizen | UI-01 | Requires running game, clicking citizen | Run game → build housing → assign citizen → click citizen in ring → verify info panel shows "Bunk Pod (Outer 3)" |
| Home label shows "No home" for unhoused citizen | UI-01 | Requires running game with no housing | Run game → click citizen before building housing → verify "No home" text |
| Tooltip shows room name for occupied segments | UI-02 | Requires hovering room segment in game | Run game → build room → hover over room segment → verify tooltip has room name on second line |
| Housing room tooltip shows resident names | UI-02 | Requires housing room with assigned citizens | Run game → build housing → wait for assignment → hover housing segment → verify "Residents: Name1, Name2" |
| Population display shows "housed/capacity" format | UI-03 | Requires game with housing rooms | Run game → build housing → verify display shows "X/Y" format |
| Tick animation on room place/demolish events | UI-03 | Requires placing/demolishing rooms | Run game → place housing room → verify tick animation → demolish → verify animation again |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 10s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
