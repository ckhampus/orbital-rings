---
phase: quick-2
plan: 01
type: execute
wave: 1
depends_on: []
files_modified: [Scripts/Citizens/CitizenManager.cs]
autonomous: true
requirements: [QUICK-2]

must_haves:
  truths:
    - "Clicking near a moving citizen selects them reliably without pixel-perfect accuracy"
    - "Citizens further away from the click point are not falsely selected"
  artifacts:
    - path: "Scripts/Citizens/CitizenManager.cs"
      provides: "Increased click proximity threshold for citizen selection"
      contains: "ClickProximityThreshold"
  key_links:
    - from: "Scripts/Citizens/CitizenManager.cs"
      to: "FindCitizenAtScreenPos"
      via: "ClickProximityThreshold constant used in XZ distance comparison"
      pattern: "dist < ClickProximityThreshold"
---

<objective>
Increase the citizen click hitbox so moving citizens are easier to select.

Purpose: Citizens are small capsules (radius 0.06-0.09 world units) moving at ~0.675 world units/sec on the walkway. The current ClickProximityThreshold of 0.4 world units makes it frustrating to click on them while they walk. Increasing the threshold provides a more forgiving click target without causing false positives, since citizens are spaced around a Tau-circumference ring (~28.3 units) with only 5 citizens.
Output: Updated CitizenManager.cs with larger click proximity threshold.
</objective>

<execution_context>
@/home/node/.claude/get-shit-done/workflows/execute-plan.md
@/home/node/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@Scripts/Citizens/CitizenManager.cs
@Scripts/Citizens/CitizenAppearance.cs
</context>

<tasks>

<task type="auto">
  <name>Task 1: Increase citizen click proximity threshold</name>
  <files>Scripts/Citizens/CitizenManager.cs</files>
  <action>
In CitizenManager.cs, increase the ClickProximityThreshold constant from 0.4f to 0.8f (doubling the click radius).

Rationale for 0.8f:
- Current capsule radii are 0.06-0.09 world units, so 0.4 was roughly 5-6x the capsule radius
- At 0.8f, the threshold is about 10-13x the capsule radius, which is generous but still precise
- Average citizen spacing is ~5.65 units apart (28.3 / 5 citizens), so 0.8 radius (1.6 diameter) will never cause overlap between adjacent citizens
- This compensates for the ~0.675 units/sec movement speed, giving the player roughly 1.2 seconds of "hover window" instead of 0.6 seconds

Change ONLY the constant value on line 76:
```csharp
private const float ClickProximityThreshold = 0.8f;
```

Do NOT change FindCitizenAtScreenPos logic, the ray-plane intersection approach, or any other code. This is a single constant change.
  </action>
  <verify>
    <automated>cd /workspace && grep -n "ClickProximityThreshold" Scripts/Citizens/CitizenManager.cs</automated>
  </verify>
  <done>ClickProximityThreshold is 0.8f. Citizens are significantly easier to click while moving. No other code changed.</done>
</task>

</tasks>

<verification>
- grep confirms ClickProximityThreshold = 0.8f in CitizenManager.cs
- No other constants or logic modified
- Build succeeds (if build tooling available)
</verification>

<success_criteria>
- ClickProximityThreshold increased from 0.4f to 0.8f in CitizenManager.cs
- Single line change, no regressions
</success_criteria>

<output>
After completion, create `.planning/quick/2-i-want-a-bigger-hitbox-for-citizens-it-i/2-SUMMARY.md`
</output>
