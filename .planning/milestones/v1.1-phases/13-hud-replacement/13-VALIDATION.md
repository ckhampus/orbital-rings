---
phase: 13
slug: hud-replacement
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-05
---

# Phase 13 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | None (Godot 4 C# — no unit test framework) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build` + manual visual verification in Godot editor |
| **Estimated runtime** | ~15 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build` + manual visual check in Godot editor
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 13-01-01 | 01 | 1 | HUD-01 | manual-only | Run scene, fulfill wish, observe heart+count pulse | N/A | ⬜ pending |
| 13-01-02 | 01 | 1 | HUD-02 | manual-only | Run scene, observe tier label; trigger tier change, observe color cross-fade | N/A | ⬜ pending |
| 13-01-03 | 01 | 1 | MCOM-01 | manual-only | Trigger tier change, observe "Station mood: X" floating text | N/A | ⬜ pending |
| 13-01-04 | 01 | 1 | CLEANUP | build | `dotnet build` | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No test framework to install — this is a UI-only phase verified by build success and manual visual inspection.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Wish counter displays and pulses on increment | HUD-01 | Visual animation (scale bounce timing, elastic settle) — no headless UI test runner in Godot 4 C# | Run scene → fulfill a wish → observe heart icon + count label pulse animation |
| Tier label shows tier name in tier-colored text | HUD-02 | Color appearance and cross-fade animation — requires visual confirmation | Run scene → observe tier label → fulfill wishes to trigger tier change → observe color cross-fade (~0.3s) |
| Floating text on tier change | MCOM-01 | Drift-and-fade animation — requires visual confirmation | Trigger tier change → observe "Station mood: X" floating text appears and fades |
| HappinessBar fully removed, no build errors | CLEANUP | Build verification | Run `dotnet build` → confirm no compilation errors referencing HappinessBar |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
