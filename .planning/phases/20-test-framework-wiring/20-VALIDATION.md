---
phase: 20
slug: test-framework-wiring
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-07
---

# Phase 20 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | GoDotTest 2.0.30 (Chickensoft) |
| **Config file** | `Orbital Rings.csproj` (conditional ItemGroups) |
| **Quick run command** | `godot res://Tests/TestRunner.tscn --run-tests --quit-on-finish` |
| **Full suite command** | `godot res://Tests/TestRunner.tscn --run-tests --quit-on-finish` |
| **Estimated runtime** | ~5 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` (verifies compilation)
- **After every plan wave:** Run `godot res://Tests/TestRunner.tscn --run-tests --quit-on-finish`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 10 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 20-01-01 | 01 | 1 | FRMW-05 | smoke | `dotnet restore` | ❌ W0 | ⬜ pending |
| 20-01-02 | 01 | 1 | FRMW-01 | smoke | `godot res://Tests/TestRunner.tscn --run-tests --quit-on-finish` | ❌ W0 | ⬜ pending |
| 20-01-03 | 01 | 1 | FRMW-03 | smoke | HousingTests passes | ❌ W0 | ⬜ pending |
| 20-01-04 | 01 | 1 | FRMW-02 | smoke | CLI exit code 0 | ❌ W0 | ⬜ pending |
| 20-01-05 | 01 | 1 | FRMW-04 | manual | Export build excludes Tests/ | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Tests/TestRunner.tscn` — test runner scene
- [ ] `Tests/TestRunner.cs` — test runner script
- [ ] `Tests/Housing/HousingTests.cs` — smoke test with ComputeCapacity
- [ ] `Tests/.gdignore` — export exclusion for Godot resource importing
- [ ] `NuGet.Config` update — add nuget.org source
- [ ] `Orbital Rings.csproj` updates — conditional test compilation with RunTests property

*All infrastructure is created by this phase — no pre-existing test framework.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Export build excludes test files | FRMW-04 | Requires actual export build; no headless export in CI-less project | Build export preset, verify no `Tests/` directory in output; verify `dotnet build -c ExportRelease` excludes test files |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 10s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
