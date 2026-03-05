---
phase: quick-4
plan: 1
subsystem: ui/audio
tags: [ui-cleanup, keyboard-shortcut, mute]
dependency_graph:
  requires: []
  provides: [keyboard-only-mute-toggle]
  affects: [hud-layout, audio-control]
tech_stack:
  patterns: [non-visual-node-for-input-only-behavior]
key_files:
  modified:
    - Scripts/UI/MuteToggle.cs
    - Scenes/QuickTest/QuickTestScene.tscn
decisions:
  - "MuteToggle moved to root scene child (not HUDLayer) since Node has no visual presence"
metrics:
  duration: "39s"
  completed: "2026-03-05"
---

# Quick Task 4: Remove the Mute Button but Keep the Keyboard Shortcut

Converted MuteToggle from a visible Button in HUDLayer to a non-visual Node at scene root, preserving M key mute/unmute via AudioServer master bus.

## Changes Made

### Task 1: Convert MuteToggle from Button to Node and remove from HUDLayer
**Commit:** 70f0ed0

**Scripts/UI/MuteToggle.cs:**
- Changed base class from `Button` to `Node` -- no visual presence in the HUD
- Removed all visual setup: ToggleMode, ButtonPressed, Text, anchors, offsets, StyleBoxFlat creation, theme overrides, and Toggled signal connection
- Removed `OnToggled` method entirely
- Added private `_muted` field for internal state tracking
- `_Input` now toggles `_muted` directly and calls `AudioServer.SetBusMute` without relying on Button state

**Scenes/QuickTest/QuickTestScene.tscn:**
- Changed MuteToggle node from `type="Button"` to `type="Node"`
- Moved MuteToggle from `parent="HUDLayer"` to `parent="."` (root scene child) since a non-visual Node does not belong in a CanvasLayer

## Deviations from Plan

None -- plan executed exactly as written.

## Verification

- `dotnet build` succeeds with 0 warnings, 0 errors
- MuteToggle.cs extends Node, not Button
- MuteToggle.cs contains zero visual/UI code (no StyleBox, Text, anchors, theme overrides)
- MuteToggle.cs handles M key in `_Input` and calls `AudioServer.SetBusMute`
- QuickTestScene.tscn has MuteToggle as `type="Node"` with `parent="."`

## Self-Check: PASSED

All files exist, all commits verified.
