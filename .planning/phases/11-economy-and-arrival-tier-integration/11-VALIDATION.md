---
phase: 11
slug: economy-and-arrival-tier-integration
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-04
---

# Phase 11 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | None (Godot 4.6 C# — no test runner installed) |
| **Config file** | none — manual verification only |
| **Quick run command** | Manual: launch game, trigger tier change, observe behavior |
| **Full suite command** | Manual gameplay verification (see Per-Task map) |
| **Estimated runtime** | ~5 minutes per full verification pass |

---

## Sampling Rate

- **After every task commit:** Verify the specific changed behavior manually (e.g., after adding `SetMoodTier`, confirm income formula reads `_currentTierMultiplier`; after wiring HappinessManager, confirm EconomyManager receives tier updates)
- **After every plan wave:** Launch game, trigger a tier change, confirm both income and arrival probability visibly change with tier
- **Before `/gsd:verify-work`:** All three success criteria must be manually confirmed green

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 11-01-01 | 01 | 1 | TIER-03 | manual smoke | Launch game → check income tick changes on tier change | ❌ manual only | ⬜ pending |
| 11-01-02 | 01 | 1 | TIER-03 | manual smoke | Verify `GetIncomeBreakdown()` also uses tier multiplier | ❌ manual only | ⬜ pending |
| 11-02-01 | 02 | 1 | TIER-02 | manual smoke | Launch game → trigger tier change → observe arrival frequency change | ❌ manual only | ⬜ pending |
| 11-02-02 | 02 | 1 | TIER-02 | manual smoke | Verify 60s timer not reset on tier change (only probability updates) | ❌ manual only | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements via manual verification.

No automated test framework is installed. Manual gameplay verification is the validation path for all tasks in this phase. If automated tests are desired in the future, install GdUnit4 and create `Tests/EconomyTierTests.cs` and `Tests/ArrivalTierTests.cs` — out of scope for Phase 11.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Citizens arrive more frequently at higher mood tiers | TIER-02 | No test framework; behavior requires live Godot runtime with citizen spawn system | 1. Launch game. 2. Use debug controls to set tier to Quiet. 3. Observe arrivals over 2-3 minutes (expect ~7 min avg). 4. Switch to Radiant. 5. Observe arrivals increase significantly (~1.3 min avg). |
| Room income scales by tier multiplier (1.0x–1.4x) | TIER-03 | No test framework; requires live economy tick system | 1. Launch game with one room occupied. 2. Note income per tick at Quiet. 3. Switch to Radiant. 4. Confirm income is ~1.4x higher. 5. Check income breakdown panel shows updated multiplier. |
| Tier change takes effect immediately (no save/load needed) | TIER-02, TIER-03 | Requires runtime observation of live state change | 1. While game is running, trigger tier change (fulfill wish or use debug). 2. Confirm income changes on the NEXT tick (≤ 1 tick delay). 3. Confirm arrival probability updates immediately (effective on next 60s check). |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 300s (5 min manual pass)
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
