---
phase: quick-4
plan: 1
type: execute
wave: 1
depends_on: []
files_modified:
  - Scripts/UI/MuteToggle.cs
  - Scenes/QuickTest/QuickTestScene.tscn
autonomous: true
requirements: ["QUICK-4"]
must_haves:
  truths:
    - "No mute button is visible in the HUD"
    - "Pressing M still toggles audio mute on/off"
  artifacts:
    - path: "Scripts/UI/MuteToggle.cs"
      provides: "Keyboard-only mute toggle (no visible UI)"
    - path: "Scenes/QuickTest/QuickTestScene.tscn"
      provides: "MuteToggle node as non-visual Node (not Button)"
  key_links:
    - from: "Scripts/UI/MuteToggle.cs"
      to: "AudioServer master bus"
      via: "M key press in _Input"
      pattern: "AudioServer\\.SetBusMute"
---

<objective>
Remove the visible mute button from the HUD while preserving the M keyboard shortcut for toggling audio mute.

Purpose: The user wants the mute functionality accessible via keyboard only, without a button cluttering the HUD.
Output: A non-visual MuteToggle node that only responds to the M key.
</objective>

<execution_context>
@/home/node/.claude/get-shit-done/workflows/execute-plan.md
@/home/node/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@Scripts/UI/MuteToggle.cs
@Scenes/QuickTest/QuickTestScene.tscn
</context>

<tasks>

<task type="auto">
  <name>Task 1: Convert MuteToggle from Button to Node and remove from HUDLayer</name>
  <files>Scripts/UI/MuteToggle.cs, Scenes/QuickTest/QuickTestScene.tscn</files>
  <action>
1. Refactor `Scripts/UI/MuteToggle.cs`:
   - Change base class from `Button` to `Node` (no visual presence).
   - Remove ALL visual/button setup from `_Ready()`: ToggleMode, ButtonPressed, Text, anchors, offsets, StyleBoxFlat creation, AddThemeStyleboxOverride calls, AddThemeColorOverride calls, and the `Toggled += OnToggled` signal connection.
   - Remove the `OnToggled(bool muted)` method entirely.
   - Add a private `bool _muted = false;` field to track state internally.
   - Keep `_Input(InputEvent @event)` but update it: on M key press, toggle `_muted`, then call `AudioServer.SetBusMute(AudioServer.GetBusIndex("Master"), _muted)` directly. No need for ButtonPressed since this is no longer a Button.
   - Update the class doc comment to reflect it is now a keyboard-only mute toggle (no visible UI element).

2. Update `Scenes/QuickTest/QuickTestScene.tscn`:
   - Change the MuteToggle node from `type="Button"` to `type="Node"`.
   - Move the MuteToggle node out of `HUDLayer` and make it a direct child of the root `QuickTestScene` node (change `parent="HUDLayer"` to `parent="."`). A non-visual Node does not belong in a CanvasLayer.
   - Keep the ext_resource reference to the script (id="11_mutetoggle") -- just update the type from `Script` to `Script` (it stays the same, only the node type changes).
  </action>
  <verify>
    <automated>cd /workspace && dotnet build 2>&1 | tail -20</automated>
  </verify>
  <done>MuteToggle is a non-visual Node, no button appears in HUD, M key shortcut still toggles audio mute via AudioServer, project builds without errors.</done>
</task>

</tasks>

<verification>
- `dotnet build` succeeds with no errors
- MuteToggle.cs extends Node, not Button
- MuteToggle.cs contains no visual/UI code (no StyleBox, no Text, no anchors)
- MuteToggle.cs still handles M key in _Input and calls AudioServer.SetBusMute
- QuickTestScene.tscn has MuteToggle as type="Node" with parent="."
</verification>

<success_criteria>
The mute button is completely invisible in the HUD. Pressing M still mutes/unmutes all audio via the master bus. The project compiles cleanly.
</success_criteria>

<output>
After completion, create `.planning/quick/4-remove-the-mute-button-but-keep-the-keyb/4-SUMMARY.md`
</output>
