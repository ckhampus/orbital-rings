---
phase: 12
slug: save-format
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-05
---

# Phase 12 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | None detected (no test project) |
| **Config file** | None |
| **Quick run command** | Manual: inspect `user://save.json` fields |
| **Full suite command** | Manual: full save/load round-trip in game |
| **Estimated runtime** | ~60 seconds (manual game launch + verify) |

---

## Sampling Rate

- **After every task commit:** Manual verification: open save.json, check fields; launch game, save/load cycle
- **After every plan wave:** Full save/load round-trip: new game -> fulfill wishes -> save -> quit -> load -> verify state
- **Before `/gsd:verify-work`:** All five behaviors verified manually
- **Max feedback latency:** 60 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 12-01-01 | 01 | 1 | SAVE-01 | manual | Inspect save.json for LifetimeHappiness, Mood, MoodBaseline fields | N/A | ⬜ pending |
| 12-01-02 | 01 | 1 | SAVE-01 | manual | Save with N wishes, load, verify LifetimeWishes == N | N/A | ⬜ pending |
| 12-01-03 | 01 | 1 | SAVE-01 | manual | Save at Cozy tier, load, verify tier resumes | N/A | ⬜ pending |
| 12-01-04 | 01 | 1 | SAVE-01 | manual | Start new game, verify LifetimeWishes=0 and Quiet tier | N/A | ⬜ pending |
| 12-01-05 | 01 | 1 | SAVE-01 | manual | Load v1 save, verify mood=Happiness, lifetime=0 | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

No automated test infrastructure exists. All verification is manual (game launch + file inspection). Creating a test framework is outside Phase 12 scope.

*Existing infrastructure covers all phase requirements via manual verification.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| v2 save contains new fields | SAVE-01 | No test framework; requires game runtime | Save game, open user://save.json, verify LifetimeHappiness/Mood/MoodBaseline present |
| Load preserves lifetime happiness | SAVE-01 | Requires game runtime state inspection | Fulfill N wishes, save, reload, verify count matches |
| Load restores mood tier | SAVE-01 | Requires visual/state confirmation in game | Reach Cozy tier, save, reload, verify tier resumes |
| Fresh game starts at zero/Quiet | SAVE-01 | Requires new game flow | Start new game, check LifetimeWishes=0, CurrentTier=Quiet |
| v1 backward compat | SAVE-01 | Requires loading legacy save file | Load v1 save.json, verify mood equals old Happiness field, lifetime=0 |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
