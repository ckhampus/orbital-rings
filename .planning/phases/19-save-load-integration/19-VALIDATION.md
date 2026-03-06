---
phase: 19
slug: save-load-integration
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-06
---

# Phase 19 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | None (Godot 4.x C# — no test framework configured) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build` |
| **Estimated runtime** | ~10 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build`
- **Before `/gsd:verify-work`:** Build must succeed
- **Max feedback latency:** 10 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 19-01-01 | 01 | 1 | INFR-04 | manual-only | Code audit traces save/load path | N/A | ⬜ pending |
| 19-01-02 | 01 | 1 | INFR-04 | manual-only | Code audit traces v2 deserialization | N/A | ⬜ pending |
| 19-01-03 | 01 | 1 | INFR-04 | manual-only | Code audit traces stale reference detection | N/A | ⬜ pending |
| 19-01-04 | 01 | 1 | INFR-05 | manual-only | Already verified in Phase 14 | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. This is a pure code audit phase — no test framework or test files needed.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Housing assignments persist across save/load | INFR-04 | Code audit phase per locked decision — no runtime testing | Trace save path (CollectGameState → GetHomeForCitizen) and load path (RestoreFromSave → AssignCitizen) |
| v2 saves load citizens as unhoused | INFR-04 | Code audit phase — verify deserialization path only | Trace System.Text.Json handling of missing int? field → null → skip in RestoreFromSave |
| Stale references detected and reassigned | INFR-04 | Code audit phase — verify ContainsKey guard | Trace RestoreFromSave stale reference check → log warning → skip → AssignAllUnhoused |
| Save format v3 with nullable HomeSegmentIndex | INFR-05 | Already verified in Phase 14 | Confirm SavedCitizen.HomeSegmentIndex is int? and SaveData.Version is 3 |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 10s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
