---
phase: quick-7
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Scripts/Citizens/CitizenNode.cs
autonomous: true
requirements: [QUICK-7]
must_haves:
  truths:
    - "Zzz label appears above housing room when citizen is resting at home"
    - "Zzz label fades in, bobs, and fades out during home rest sequence"
    - "Zzz label is removed when citizen exits home or home is demolished"
  artifacts:
    - path: "Scripts/Citizens/CitizenNode.cs"
      provides: "Fixed Zzz label visibility during home rest"
      contains: "GetParent().AddChild"
  key_links:
    - from: "CreateZzzLabel"
      to: "GetParent().AddChild"
      via: "Adding label to parent node instead of self"
      pattern: "GetParent\\(\\)\\.AddChild\\(_zzzLabel\\)"
---

<objective>
Fix Zzz label not showing when a citizen goes home to rest.

Purpose: The Zzz indicator is created as a child of the CitizenNode, but the CitizenNode sets Visible=false during the rest phase. In Godot 4, TopLevel=true only prevents transform inheritance, NOT visibility inheritance. Children of a hidden node are also hidden. The Zzz label is invisible the entire time.

Output: Working Zzz indicator that floats above the housing room while a citizen rests inside.
</objective>

<execution_context>
@/home/node/.claude/get-shit-done/workflows/execute-plan.md
@/home/node/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/STATE.md
@Scripts/Citizens/CitizenNode.cs

<interfaces>
From Scripts/Citizens/CitizenNode.cs (relevant fields and methods):
```csharp
// Fields
private Label3D _zzzLabel;
private Tween _zzzBobTween;

// The bug: CreateZzzLabel adds label as child of CitizenNode
private void CreateZzzLabel(Vector3 worldPosition)
{
    _zzzLabel = new Label3D { /* ... */ TopLevel = true };
    _zzzLabel.GlobalPosition = worldPosition;
    AddChild(_zzzLabel);  // BUG: parent CitizenNode is Visible=false at this point
}

// Phase 3 of StartHomeReturn sets Visible=false BEFORE creating Zzz:
// Visible = false;
// CreateZzzLabel(new Vector3(zzzX, zzzY, zzzZ));

// RemoveZzzLabel cleans up:
private void RemoveZzzLabel()
{
    if (_zzzLabel == null) return;
    _zzzBobTween?.Kill();
    _zzzBobTween = null;
    _zzzLabel.QueueFree();
    _zzzLabel = null;
}
```
</interfaces>
</context>

<tasks>

<task type="auto">
  <name>Task 1: Fix Zzz label visibility by adding to parent node</name>
  <files>Scripts/Citizens/CitizenNode.cs</files>
  <action>
In `CreateZzzLabel()`, change `AddChild(_zzzLabel)` to `GetParent().AddChild(_zzzLabel)`.

This adds the Zzz label as a sibling of the CitizenNode (child of CitizenManager) rather than as a child of the CitizenNode itself. Since CitizenManager stays visible, the label will render correctly even when the CitizenNode sets Visible=false.

The label already uses TopLevel=true and GlobalPosition for positioning, so reparenting has no effect on where it renders. It just escapes the parent's visibility inheritance.

No other changes needed -- RemoveZzzLabel() uses `_zzzLabel.QueueFree()` which works regardless of which node it's parented to. The `_ExitTree` cleanup (`_zzzLabel?.QueueFree()`) also works fine since QueueFree is not parent-dependent.

Update the doc comment on CreateZzzLabel to explain why the label is added to the parent node instead of self:
"Added to parent node (not self) because CitizenNode.Visible is false during rest -- Godot visibility propagates to children regardless of TopLevel."
  </action>
  <verify>
    <automated>cd /workspace && dotnet build 2>&1 | tail -5</automated>
  </verify>
  <done>
    - Zzz Label3D is added to GetParent() instead of self
    - Build succeeds with no errors
    - Comment explains the visibility inheritance reason
  </done>
</task>

</tasks>

<verification>
1. `dotnet build` succeeds
2. `dotnet format --verify-no-changes` passes (or run `dotnet format` first)
3. Manual verification: Run game, build a housing room, wait for citizen to go home, observe Zzz label appearing above the room
</verification>

<success_criteria>
- Zzz label is visible above housing rooms when citizens rest at home
- Label fades in, bobs gently, then fades out before citizen emerges
- Build compiles cleanly
- No regressions in room visit, home demolish, or citizen unhousing flows
</success_criteria>

<output>
After completion, create `.planning/quick/7-the-zzz-isn-t-showing-when-a-citizen-goe/7-SUMMARY.md`
</output>
