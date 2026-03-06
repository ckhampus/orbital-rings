---
phase: 14
slug: housing-foundation
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-06
---

# Phase 14 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | None — no test framework in project |
| **Config file** | None |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build` |
| **Estimated runtime** | ~10 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build` + manual Inspector verification
- **Before `/gsd:verify-work`:** Full build must succeed, all 3 success criteria verified manually
- **Max feedback latency:** 10 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 14-01-01 | 01 | 1 | INFR-02 | build | `dotnet build` | N/A | ⬜ pending |
| 14-01-02 | 01 | 1 | INFR-02 | manual | Open Inspector, verify HousingConfig fields | N/A | ⬜ pending |
| 14-01-03 | 01 | 1 | INFR-01 | build | `dotnet build` | N/A | ⬜ pending |
| 14-01-04 | 01 | 1 | INFR-01 | build | `dotnet build` | N/A | ⬜ pending |
| 14-01-05 | 01 | 1 | INFR-05 | build | `dotnet build` | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements. `dotnet build` serves as the primary automated validation. No test framework installation needed.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| HousingConfig visible in Inspector with 4 timing fields | INFR-02 | Godot Inspector UI verification requires running the editor | Open Godot, create new Resource, select HousingConfig, verify HomeTimerMin/Max and RestDurationMin/Max fields appear |
| HousingManager appears in autoload list | INFR-01 | Autoload registration requires Godot editor inspection | Open project.godot in editor, verify HousingManager listed before SaveManager |
| SavedCitizen.HomeSegmentIndex serializes to null | INFR-05 | Save format requires runtime serialization test | Start game, create save, inspect JSON file for HomeSegmentIndex: null |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 10s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
