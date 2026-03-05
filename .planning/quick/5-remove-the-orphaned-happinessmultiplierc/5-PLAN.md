---
phase: quick-5
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Scripts/Data/EconomyConfig.cs
  - Resources/Economy/default_economy.tres
autonomous: true
requirements: [CLEANUP-01]
must_haves:
  truths:
    - "HappinessMultiplierCap property does not exist in EconomyConfig"
    - "default_economy.tres does not contain HappinessMultiplierCap"
    - "Project builds without errors after removal"
  artifacts:
    - path: "Scripts/Data/EconomyConfig.cs"
      provides: "EconomyConfig without orphaned HappinessMultiplierCap field"
      contains: "IncomeTickInterval"
    - path: "Resources/Economy/default_economy.tres"
      provides: "Economy resource without orphaned HappinessMultiplierCap value"
  key_links: []
---

<objective>
Remove the orphaned `HappinessMultiplierCap` [Export] field from EconomyConfig.cs and its corresponding value in default_economy.tres. This field is dead code left over from Phase 11's migration from float-space to tier-space multipliers. The five `IncomeMult{Tier}` fields replaced it entirely.

Purpose: Eliminate dead code that confuses future maintainers seeing it in the Godot Inspector alongside the active `IncomeMultX` fields.
Output: Clean EconomyConfig.cs and default_economy.tres with no orphaned field.
</objective>

<execution_context>
@/home/node/.claude/get-shit-done/workflows/execute-plan.md
@/home/node/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@Scripts/Data/EconomyConfig.cs
@Resources/Economy/default_economy.tres
@Scripts/Autoloads/EconomyManager.cs
</context>

<tasks>

<task type="auto">
  <name>Task 1: Remove HappinessMultiplierCap from EconomyConfig.cs and default_economy.tres</name>
  <files>Scripts/Data/EconomyConfig.cs, Resources/Economy/default_economy.tres</files>
  <action>
1. In Scripts/Data/EconomyConfig.cs, delete lines 27-30 (the XML doc comment and the `[Export] public float HappinessMultiplierCap` property). This is the block:
   ```
   /// <summary>
   /// Maximum happiness multiplier on income. Formula: 1.0 + (happiness * (cap - 1.0)).
   /// Capped at 1.3 to keep feedback loop bounded (30% bonus at perfect happiness).
   /// </summary>
   [Export] public float HappinessMultiplierCap { get; set; } = 1.3f;
   ```
   Leave the blank line between WorkBonusMultiplier and IncomeTickInterval clean (one blank line separating them).

2. In Resources/Economy/default_economy.tres, delete line 10 (`HappinessMultiplierCap = 1.3`). The file should flow directly from `WorkBonusMultiplier = 1.25` to `IncomeTickInterval = 5.5`.

3. Verify no runtime code references HappinessMultiplierCap (already confirmed: only planning docs reference it, no .cs file reads it).
  </action>
  <verify>
    <automated>cd /workspace && grep -r "HappinessMultiplierCap" Scripts/ Resources/ --include="*.cs" --include="*.tres" --include="*.tscn" ; test $? -eq 1</automated>
  </verify>
  <done>HappinessMultiplierCap does not appear in any runtime source or resource file. EconomyConfig.cs compiles cleanly with only the active tier multiplier fields in the Income export group.</done>
</task>

</tasks>

<verification>
- `grep -r "HappinessMultiplierCap" Scripts/ Resources/` returns zero matches
- Project builds without errors (no code referenced this field)
</verification>

<success_criteria>
- HappinessMultiplierCap removed from EconomyConfig.cs (property + doc comment)
- HappinessMultiplierCap removed from default_economy.tres
- No remaining references in runtime code or resources
</success_criteria>

<output>
After completion, create `.planning/quick/5-remove-the-orphaned-happinessmultiplierc/5-SUMMARY.md`
</output>
