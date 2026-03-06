---
status: resolved
trigger: "Investigate why HousingManager.RestoreFromSave reports Stale home reference for ALL citizens on save/load"
created: 2026-03-06T00:00:00Z
updated: 2026-03-06T00:00:00Z
---

## Current Focus

hypothesis: RestorePlacedRoom does NOT emit RoomPlaced event, so HousingManager._housingRoomCapacities is never populated before RestoreFromSave runs
test: Read RestorePlacedRoom source -- confirmed it does not call EmitRoomPlaced
expecting: If no event fires, OnRoomPlaced never runs, dictionary stays empty, all lookups fail
next_action: Return root cause diagnosis

## Symptoms

expected: After loading a save, citizens retain their saved home assignments
actual: ALL citizens get "Stale home reference" warning and become unhoused
errors: "Housing: Stale home reference for {name} at segment {N} -- citizen is unhoused" for every citizen
reproduction: Save game with citizens assigned to housing rooms, then load the save
started: Since HousingManager was introduced (phase 15)

## Eliminated

(none -- first hypothesis confirmed)

## Evidence

- timestamp: 2026-03-06T00:01:00Z
  checked: HousingManager._Ready() lines 90-108
  found: When StateLoaded==true, InitializeExistingRooms() is SKIPPED. This is the only non-event path that populates _housingRoomCapacities.
  implication: During save/load, _housingRoomCapacities starts empty and stays empty unless OnRoomPlaced fires.

- timestamp: 2026-03-06T00:02:00Z
  checked: SaveManager.ApplyState() line 399
  found: Sets HousingManager.StateLoaded = true BEFORE scene transition. This prevents InitializeExistingRooms from running in _Ready().
  implication: Confirms _housingRoomCapacities will NOT be populated via the _Ready() path.

- timestamp: 2026-03-06T00:03:00Z
  checked: BuildManager.RestorePlacedRoom() lines 311-337
  found: RestorePlacedRoom tracks the room in _placedRooms and creates the mesh, but does NOT call GameEvents.Instance.EmitRoomPlaced(). Compare with the normal placement path (line 456) which DOES call EmitRoomPlaced.
  implication: HousingManager.OnRoomPlaced never fires during save restoration. _housingRoomCapacities remains empty.

- timestamp: 2026-03-06T00:04:00Z
  checked: SaveManager.ApplySceneState() lines 439-474
  found: Calls BuildManager.RestorePlacedRoom for each room (no event emitted), then CitizenManager.SpawnCitizenFromSave, then HousingManager.RestoreFromSave. The RestoreFromSave method checks _housingRoomCapacities (line 182) which is empty, so EVERY citizen hits the "Stale home reference" branch.
  implication: The entire call chain confirms the hypothesis. No path populates _housingRoomCapacities before RestoreFromSave checks it.

## Resolution

root_cause: |
  HousingManager._housingRoomCapacities is empty when RestoreFromSave runs during save/load.

  Two paths normally populate this dictionary:
  1. InitializeExistingRooms() in _Ready() -- but this is SKIPPED because StateLoaded=true (set by SaveManager.ApplyState at line 399)
  2. OnRoomPlaced() event handler -- but BuildManager.RestorePlacedRoom() does NOT emit the RoomPlaced event (compare line 336 vs line 456)

  So when RestoreFromSave iterates over saved assignments and checks _housingRoomCapacities.ContainsKey(homeIndex.Value) at line 182, the dictionary is always empty, and every citizen falls through to the "Stale home reference" warning branch.

fix: (not applied -- diagnosis only)
verification: (not applied -- diagnosis only)
files_changed: []
