---
phase: 16
slug: capacity-transfer
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-06
---

# Phase 16 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | None — no automated test infrastructure exists |
| **Config file** | None |
| **Quick run command** | `dotnet build` (compile check) |
| **Full suite command** | `dotnet build` + manual smoke test |
| **Estimated runtime** | ~15 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` (compile verification — refactoring phase)
- **After every plan wave:** Manual smoke test: start new game, build housing, verify arrival cap works
- **Before `/gsd:verify-work`:** Compile clean + manual verification that arrival gating respects housing capacity
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 16-01-01 | 01 | 1 | INFR-03 | compile | `dotnet build` | N/A | ⬜ pending |
| 16-01-02 | 01 | 1 | INFR-03 | compile | `dotnet build` | N/A | ⬜ pending |
| 16-01-03 | 01 | 1 | INFR-03 | compile | `dotnet build` | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

*No test infrastructure to create. This phase is mechanical refactoring verified by compilation and manual testing.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Arrival gating queries HousingManager.TotalCapacity | INFR-03 | No unit test framework; requires in-game verification | Start new game, build housing rooms, verify citizens stop arriving when pop >= StarterCitizenCapacity + TotalCapacity |
| Building/demolishing rooms updates capacity in one place | INFR-03 | Requires runtime state inspection | Build and demolish housing rooms, verify arrival cap updates correctly without desync |
| HappinessManager no longer tracks capacity | INFR-03 | Code structure verification | Grep for `_housingCapacity` confirms zero results outside HousingManager |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
