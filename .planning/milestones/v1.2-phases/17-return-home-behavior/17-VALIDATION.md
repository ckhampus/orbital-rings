---
phase: 17
slug: return-home-behavior
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-06
---

# Phase 17 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | None — Godot C# project without automated test infrastructure |
| **Config file** | None |
| **Quick run command** | Manual: run game in Godot editor, observe citizen behavior |
| **Full suite command** | Manual: run game, build housing rooms, wait 90-150s for home returns |
| **Estimated runtime** | ~120 seconds (manual observation with default timers) |

---

## Sampling Rate

- **After every task commit:** Run game in editor, verify specific behavior added by task
- **After every plan wave:** Full playthrough: spawn citizens, build housing, observe home returns, verify Zzz, test demolish eject
- **Before `/gsd:verify-work`:** All four BEHV requirements manually verified
- **Max feedback latency:** ~120 seconds (default home timer range)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| TBD | TBD | TBD | BEHV-01 | manual-only | Run game, place housing, wait ~90-150s, observe citizen walks to home segment | N/A | ⬜ pending |
| TBD | TBD | TBD | BEHV-02 | manual-only | Observe "Zzz" Label3D above room segment during home rest, time the duration | N/A | ⬜ pending |
| TBD | TBD | TBD | BEHV-03 | manual-only | Observe that no new wish appears during home rest (requires reduced timers) | N/A | ⬜ pending |
| TBD | TBD | TBD | BEHV-04 | manual-only | Give citizen a wish, wait for home timer, verify citizen skips home return | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- Existing infrastructure covers all phase requirements.
- Manual testing with reduced HousingConfig timing values (e.g., HomeTimerMin=10, RestDurationMin=3) recommended for faster iteration during development.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Periodic home return | BEHV-01 | Visual behavior with tween animations in Godot runtime | Run game, build housing rooms, assign citizens, wait 90-150s, observe citizen walking to home segment |
| Zzz indicator during rest | BEHV-02 | 3D Label rendering requires game runtime | Observe "Zzz" Label3D appears above room segment during home rest with fade in/out and bob animation |
| Wish timer pauses during rest | BEHV-03 | Timer interaction requires game runtime observation | Set reduced timers, observe no new wish generates during home rest period |
| Active wish defers home return | BEHV-04 | Behavior priority requires game runtime | Give citizen active wish, wait for home timer, verify citizen skips/defers home return |

**Manual-only justification:** This is a Godot game project with visual behavior testing. Behaviors involve tween animations, 3D rendering, and timer interactions that require the game runtime. No automated test framework is configured.

---

## Validation Sign-Off

- [x] All tasks have manual verification instructions
- [x] Sampling continuity: manual verification after each task commit
- [x] Wave 0 covers all MISSING references (none needed — manual only)
- [x] No watch-mode flags
- [x] Feedback latency < 120s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
