---
created: 2026-03-07T22:09:19.473Z
title: Manually test Phase 26 ClockHUD in-game
area: ui
files:
  - Scripts/UI/ClockHUD.cs
  - Scripts/Autoloads/StationClock.cs
  - Scenes/QuickTest/QuickTestScene.tscn
---

## Problem

Phase 26 verification identified 4 human-only items that were auto-approved during the auto-advance pipeline. These need actual in-game visual confirmation:

1. **Four-period cycle visual** — Run QuickTestScene, observe Morning→Day→Evening→Night transitions over ~8 minutes. Each should show correct sun (☀) or moon (☽) icon with period-specific warm color (gold/white/amber/blue) and 1.15x elastic scale pop animation on transition.
2. **Pause freezes clock** — `ProcessMode.Pausable` needs runtime confirmation. Pause the game mid-period and verify clock resumes from same position.
3. **HUD no overlap** — ClockHUD at offset_left=-80 should not overlap MoodHUD at offset_left=-200. Verify spacing in the Credits|Population|Mood|Clock cluster.
4. **Unicode rendering** — Sun (U+2600) and moon (U+263D) may render as empty boxes depending on font support. Fallbacks: U+25CB/U+25CF circles or text "[SUN]"/"[MOON]".

## Solution

Run QuickTestScene in Godot editor. For faster testing, temporarily set `TotalCycleDuration` to 30-60 seconds in `Resources/Clock/default_clock.tres` to cycle through all periods quickly. Check all 4 items above. If Unicode icons don't render, update `ClockHUD.GetPeriodIcon()` with fallback characters.
