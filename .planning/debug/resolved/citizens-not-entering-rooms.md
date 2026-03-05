---
status: resolved
trigger: "Citizens walk around but never enter rooms. Started after recent v1.1 changes."
created: 2026-03-05T00:00:00Z
updated: 2026-03-05T00:03:00Z
---

## Current Focus

hypothesis: CONFIRMED - Starter citizens get null _grid because they're spawned on the title screen before Ring exists
test: Build compiles, code path analysis confirms fix propagates grid to existing citizens
expecting: Citizens will now receive valid _grid when Ring is discovered, enabling room visits
next_action: Session complete. Human confirmed fix works.

## Symptoms

expected: Citizens should pathfind to room entrances and go inside
actual: Citizens walk around but never go into rooms - they move but ignore rooms entirely
errors: No errors in Godot console
reproduction: Run the game, build rooms, observe citizens - they walk but never enter any rooms
started: After recent v1.1 changes (mood tiers, economy integration, HUD replacement, quick tasks)

## Eliminated

- hypothesis: Recent v1.1 code changes (HappinessManager, EconomyManager, GameEvents) broke room visit logic
  evidence: Reviewed all v1.1 diffs (phases 11-13, quick-4). No changes touch CitizenNode visit logic, CitizenManager spawn logic, or SegmentGrid. Changes were only to HappinessManager (tier multipliers, arrival probability), EconomyManager (tier income), GameEvents (removed HappinessChanged event), SaveManager (v2 fields), and HUD (MoodHUD, MuteToggle).
  timestamp: 2026-03-05T00:01:00Z

## Evidence

- timestamp: 2026-03-05T00:00:30Z
  checked: Autoload initialization order in project.godot
  found: CitizenManager is 4th autoload. Main scene is TitleScreen (no Ring). CitizenManager._Ready() calls FindChild("Ring") which returns null, so _grid=null. Then SpawnStarterCitizens(5) spawns citizens with Initialize(data, angle, _grid=null).
  implication: All starter citizens have null _grid from birth.

- timestamp: 2026-03-05T00:00:40Z
  checked: CitizenNode.OnVisitTimerTimeout() guard clause
  found: Line 351: "if (_grid == null) return;" -- silently aborts room visit attempts. No error logged.
  implication: Citizens with null _grid will NEVER visit rooms. No console error produced (matches symptoms).

- timestamp: 2026-03-05T00:00:50Z
  checked: CitizenManager._Process() lazy discovery
  found: _Process() finds Ring and sets CitizenManager._grid after scene loads, but this does NOT propagate to already-spawned citizens' private _grid field.
  implication: Lazy discovery fixed CitizenManager's reference but left a dangling null in all citizen nodes.

- timestamp: 2026-03-05T00:00:55Z
  checked: "Continue" flow vs "New Station" flow
  found: Continue flow: ApplySceneState runs after 2 frames, calls ClearCitizens + SpawnCitizenFromSave. By then _grid is valid. New Station flow: no re-spawn happens, citizens from _Ready() persist with null _grid.
  implication: Bug only affects "New Station" path. "Continue" (load save) would work correctly. This explains why bug may have existed since title screen (commit 78bbb18, 73 commits ago) but only noticed now.

- timestamp: 2026-03-05T00:01:00Z
  checked: When title screen was added (commit 78bbb18) vs when lazy discovery was added (commit 1b620b5)
  found: Title screen added first, lazy discovery fix added immediately after. But lazy discovery only fixed CitizenManager._grid, not the citizens' own _grid references.
  implication: The bug has existed since the title screen was introduced, but was latent -- user likely used Continue (from save) most of the time. Starting fresh (New Station) after v1.1 changes would expose it.

- timestamp: 2026-03-05T00:01:30Z
  checked: Build verification
  found: dotnet build succeeds with 0 warnings, 0 errors after fix applied.
  implication: Fix is syntactically correct and type-safe.

## Resolution

root_cause: CitizenManager._Ready() spawns starter citizens when the title screen is showing (Ring not loaded). Citizens get null _grid reference. CitizenManager._Process() lazy discovery updates its own _grid but never propagates to already-spawned citizens. The null _grid guard in OnVisitTimerTimeout silently aborts all room visits. "Continue" flow masks the bug because citizens are re-spawned after scene load with valid _grid. Bug has existed since title screen was added (commit 78bbb18) but only manifests in "New Station" path.
fix: Added CitizenNode.SetGrid(SegmentGrid) internal method. Modified CitizenManager._Process() lazy discovery to propagate the grid to all existing citizens when first discovered.
verification: Build compiles. Code path analysis confirms fix. Human verified in-game: citizens now enter rooms correctly after "New Station" flow.
files_changed:
  - Scripts/Citizens/CitizenNode.cs
  - Scripts/Citizens/CitizenManager.cs
