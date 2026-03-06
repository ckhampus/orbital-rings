---
status: resolved
phase: 15-housingmanager-core
source: [15-01-SUMMARY.md, 15-02-SUMMARY.md]
started: 2026-03-06T10:10:00Z
updated: 2026-03-06
---

## Current Test

[testing complete]

## Tests

### 1. Citizens Assigned on Room Placement
expected: Build a Bunk Pod (1-segment, BaseCapacity 2). Check the Output/console log for GD.Print messages. You should see "Housing: [name] assigned to room at segment [N]" for up to 2 citizens (capacity of 1-seg Bunk Pod). Remaining citizens stay unhoused.
result: pass

### 2. Fewest-Occupants-First Distribution
expected: Build a second Bunk Pod (1-segment). The 2 oldest unhoused citizens should be assigned to the NEW pod (since it has 0 occupants vs the first pod's 2). Check GD.Print — assigned citizens should go to the new room's segment index, not the first room's.
result: pass

### 3. Multi-Segment Capacity Scaling
expected: Build a Sky Loft as a 3-segment room (or any multi-segment housing room). Capacity should be BaseCapacity + (segments - 1). For a 3-seg Sky Loft with BaseCapacity 4, capacity = 6. GD.Print should show up to 6 citizens assigned to that room.
result: pass

### 4. New Citizen Arrival Assignment
expected: Wait for a new citizen to arrive (or trigger arrival). The new citizen should be assigned to whichever housing room has the fewest occupants. Check GD.Print for the assignment message with the correct room segment.
result: pass

### 5. Demolish Reassignment
expected: Demolish a housing room that has citizens assigned. GD.Print should show each displaced citizen getting "unhoused" then reassigned to another room (if capacity exists). If no other rooms have space, they remain unhoused. Oldest citizens are reassigned first.
result: pass

### 6. Unhoused Citizens Behave Normally
expected: Have at least one citizen without a home (all rooms full or no rooms built). The unhoused citizen should still walk around, visit rooms, and fulfill wishes identically to housed citizens. No errors, no stuck behavior.
result: pass

### 7. Save/Load Preserves Housing
expected: With citizens assigned to housing rooms, save the game. Reload the save. Check GD.Print — citizens should have the same home assignments as before the save (same citizen names at same segment indices). No re-running of the assignment algorithm.
result: issue
reported: "I got this in the logs: Housing: Stale home reference for Aria at segment 4 -- citizen is unhoused / Housing: Stale home reference for Bodhi at segment 4 -- citizen is unhoused / Housing: Stale home reference for Celeste at segment 4 -- citizen is unhoused / Housing: Stale home reference for Davi at segment 19 -- citizen is unhoused / Housing: Stale home reference for Elara at segment 19 -- citizen is unhoused / Housing: Stale home reference for Fern at segment 9 -- citizen is unhoused / Housing: Stale home reference for Gael at segment 9 -- citizen is unhoused"
severity: major

### 8. Housing Events Trigger Autosave
expected: After building a housing room (citizens get assigned), the autosave should trigger. Check for autosave activity after a housing assignment event fires (you may see a save file update or autosave log message after a short debounce delay).
result: pass

## Summary

total: 8
passed: 7
issues: 1
pending: 0
skipped: 0

## Gaps

- truth: "Loading a save restores housing assignments exactly as saved (same citizen names at same segment indices)"
  status: resolved
  reason: "User reported: All citizens get 'Stale home reference' on load — RestoreFromSave cannot find rooms in _housingRoomCapacities. All 7 citizens become unhoused despite having valid saved home indices (segments 4, 19, 9)."
  severity: major
  test: 7
  root_cause: "_housingRoomCapacities is empty when RestoreFromSave runs. Two population paths are both disabled: (1) InitializeExistingRooms skipped because StateLoaded=true, (2) OnRoomPlaced never fires because BuildManager.RestorePlacedRoom does not emit RoomPlaced event. Fix: call InitializeExistingRooms() at the start of RestoreFromSave to scan already-restored rooms."
  artifacts:
    - path: "Scripts/Autoloads/HousingManager.cs"
      issue: "RestoreFromSave checks _housingRoomCapacities but no code populates it during save restoration"
    - path: "Scripts/Build/BuildManager.cs"
      issue: "RestorePlacedRoom (lines 311-337) does not emit RoomPlaced event unlike interactive placement (line 456)"
    - path: "Scripts/Autoloads/SaveManager.cs"
      issue: "Sets StateLoaded=true before scene load, then calls RestoreFromSave with empty capacity dict"
  missing:
    - "Call InitializeExistingRooms() at the start of RestoreFromSave to populate _housingRoomCapacities from already-restored BuildManager rooms"
  debug_session: ".planning/debug/stale-home-reference-on-load.md"
