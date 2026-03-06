---
phase: quick-8
plan: 1
type: execute
wave: 1
depends_on: []
files_modified:
  - Scripts/Autoloads/HappinessManager.cs
autonomous: true
requirements: [quick-8]
must_haves:
  truths:
    - "No new citizen arrives when all housing beds are occupied"
    - "Citizens still arrive normally when housing beds are available"
    - "Up to 5 starter citizens can arrive before any housing is built"
  artifacts:
    - path: "Scripts/Autoloads/HappinessManager.cs"
      provides: "Fixed arrival gate checking actual housing vacancy"
      contains: "TotalHoused"
  key_links:
    - from: "Scripts/Autoloads/HappinessManager.cs"
      to: "Scripts/Autoloads/HousingManager.cs"
      via: "TotalHoused and TotalCapacity properties"
      pattern: "HousingManager\\.Instance\\?\\.(TotalCapacity|TotalHoused)"
---

<objective>
Fix the citizen arrival gate so new citizens only arrive when there is actual housing vacancy, not just theoretical capacity.

Purpose: The current arrival check on line 268 of HappinessManager.cs uses `currentPop >= StarterCitizenCapacity + TotalCapacity`. This is flawed because starter citizens (the first 5) get auto-assigned to housing rooms when built, consuming beds. The formula assumes starters never occupy beds, so it allows arrivals even when all beds are taken. For example: 5 citizens exist, a 3-bed housing room is built, 3 starters fill all 3 beds, but the gate sees `5 < 5+3=8` and lets 3 more citizens arrive with no beds for them.

Output: Corrected arrival gate that checks actual housing vacancy (TotalHoused < TotalCapacity) instead of the faulty additive formula.
</objective>

<execution_context>
@/home/node/.claude/get-shit-done/workflows/execute-plan.md
@/home/node/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/STATE.md
@Scripts/Autoloads/HappinessManager.cs
@Scripts/Autoloads/HousingManager.cs

<interfaces>
From Scripts/Autoloads/HousingManager.cs:
```csharp
// Line 67-76: Total bed count across all housing rooms
public int TotalCapacity { get; }

// Line 79: Number of citizens currently assigned to a home
public int TotalHoused => _citizenHomes.Count;
```

From Scripts/Autoloads/HappinessManager.cs:
```csharp
// Line 40: First 5 citizens don't require housing
private const int StarterCitizenCapacity = 5;

// Line 265-291: OnArrivalCheck -- the method to fix
// Line 268 is the buggy gate:
//   if (currentPop >= StarterCitizenCapacity + (HousingManager.Instance?.TotalCapacity ?? 0)) return;
```
</interfaces>
</context>

<tasks>

<task type="auto">
  <name>Task 1: Fix arrival gate to check actual housing vacancy</name>
  <files>Scripts/Autoloads/HappinessManager.cs</files>
  <action>
In `OnArrivalCheck()` (line 265), replace the current gate check on line 268:

```csharp
if (currentPop >= StarterCitizenCapacity + (HousingManager.Instance?.TotalCapacity ?? 0)) return;
```

With a corrected check that uses actual housing occupancy:

```csharp
int housingCapacity = HousingManager.Instance?.TotalCapacity ?? 0;
int housed = HousingManager.Instance?.TotalHoused ?? 0;
bool belowStarterCap = currentPop < StarterCitizenCapacity;
bool hasFreeBed = housingCapacity > 0 && housed < housingCapacity;

if (!belowStarterCap && !hasFreeBed) return;
```

Logic explanation:
- `belowStarterCap`: The first 5 citizens can arrive without any housing (pre-housing phase of the game).
- `hasFreeBed`: Once at 5+ citizens, only allow arrival if there is an actual unoccupied housing bed. Uses `TotalHoused` (citizens with assigned homes) vs `TotalCapacity` (total beds).
- The `housingCapacity > 0` guard ensures that `hasFreeBed` is false when no housing rooms exist at all (prevents 0 < 0 edge case, though that's already false).

Update the XML doc comment for `OnArrivalCheck` to reflect the new logic:
- "If population is below StarterCitizenCapacity (5), arrival is allowed regardless of housing."
- "Otherwise, arrival requires at least one unoccupied housing bed (TotalHoused < TotalCapacity)."

Run `dotnet format` after the change.
  </action>
  <verify>
    <automated>cd /workspace && dotnet build 2>&1 | tail -5</automated>
  </verify>
  <done>
- The arrival gate uses TotalHoused vs TotalCapacity instead of the additive formula.
- Citizens cannot arrive when all housing beds are occupied (regardless of StarterCitizenCapacity math).
- Citizens below the starter cap (5) can still arrive without housing.
- Build succeeds with no errors.
  </done>
</task>

</tasks>

<verification>
- `dotnet build` succeeds with no errors
- `dotnet format --verify-no-changes` passes (formatting clean)
- Manual spot-check: Read the modified OnArrivalCheck and verify the logic handles these scenarios:
  1. 0 citizens, 0 housing rooms -> allows arrival (below starter cap)
  2. 4 citizens, 0 housing rooms -> allows arrival (below starter cap)
  3. 5 citizens, 0 housing rooms -> blocks arrival (at starter cap, no housing)
  4. 5 citizens, 3-bed room, 3 housed -> blocks arrival (at cap, no free beds)
  5. 5 citizens, 3-bed room, 2 housed -> allows arrival (free bed exists)
  6. 7 citizens, 3-bed room, 3 housed -> blocks arrival (above cap, no free beds)
</verification>

<success_criteria>
New citizens only arrive when there is actual housing vacancy (an unoccupied bed) or population is below the starter citizen threshold of 5.
</success_criteria>

<output>
After completion, create `.planning/quick/8-fix-new-citizens-arriving-when-no-housin/8-SUMMARY.md`
</output>
