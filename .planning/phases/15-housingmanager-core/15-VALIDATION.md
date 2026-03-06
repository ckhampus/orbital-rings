---
phase: 15
slug: housingmanager-core
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-06
---

# Phase 15 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Godot 4 (no external test framework -- manual in-game validation) |
| **Config file** | None -- Godot project uses runtime testing |
| **Quick run command** | Launch game scene, build housing rooms, observe citizen assignments via GD.Print |
| **Full suite command** | Full playthrough: build, demolish, save/load, verify assignments persist |
| **Estimated runtime** | ~60 seconds (manual) |

---

## Sampling Rate

- **After every task commit:** Run game scene, verify no crashes, check GD.Print output for assignments
- **After every plan wave:** Full playthrough with save/load cycle
- **Before `/gsd:verify-work`:** All 5 HOME requirements verified manually in game
- **Max feedback latency:** 60 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 15-01-01 | 01 | 1 | INFR-01 | manual | Launch scene, verify HousingManager.Instance non-null | N/A | ⬜ pending |
| 15-01-02 | 01 | 1 | HOME-04 | manual | Build 1-seg and 2-seg Bunk Pod, verify capacity 2 vs 3 | N/A | ⬜ pending |
| 15-01-03 | 01 | 1 | HOME-01 | manual | Build 2 housing rooms, wait for citizen arrival, check fewest-occupants assignment | N/A | ⬜ pending |
| 15-01-04 | 01 | 1 | HOME-02 | manual | Demolish housing room, verify reassignment to remaining rooms | N/A | ⬜ pending |
| 15-01-05 | 01 | 1 | HOME-03 | manual | Start with unhoused citizens, build room, verify oldest-first assignment | N/A | ⬜ pending |
| 15-01-06 | 01 | 1 | HOME-05 | manual | Verify unhoused citizens walk, visit rooms, and fulfill wishes normally | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No external test framework needed.

Strategic `GD.Print` statements for assignment/displacement events to aid manual verification.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Citizen arrival assigns to fewest-occupants room | HOME-01 | Godot game -- requires visual/runtime verification | Build 2+ housing rooms, wait for citizen arrival, check GD.Print for assignment to room with fewer occupants |
| Demolish reassigns or unhomes citizens | HOME-02 | Runtime event chain requires in-game test | Build room, assign citizens, demolish, verify reassignment via GD.Print |
| New room assigns unhoused citizens oldest-first | HOME-03 | Requires multiple citizens in specific state | Start game with unhoused citizens, build housing room, verify oldest assigned first via GD.Print output order |
| Capacity scales with segments | HOME-04 | Requires building multi-segment rooms | Build 1-segment and 2-segment Bunk Pod, verify capacity difference via GD.Print |
| Unhoused citizens function identically | HOME-05 | Behavioral observation required | Play with unhoused citizens, verify walking, visiting, wishing unchanged |
| Save/load preserves assignments | INFR-01 | Requires save/load cycle | Assign citizens to rooms, save, reload, verify assignments restored via GD.Print |

---

## Validation Sign-Off

- [ ] All tasks have manual verification instructions
- [ ] Sampling continuity: GD.Print verification after every task commit
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
