# Pitfalls Research

**Domain:** Adding citizen housing assignment and return-home behavior to an existing Godot 4 C# cozy space station builder with event-driven autoload architecture
**Researched:** 2026-03-05
**Confidence:** HIGH (derived from direct codebase analysis of 15+ source files, not external sources)

## Critical Pitfalls

### Pitfall 1: Event Ordering Race Between RoomDemolished Handlers

**What goes wrong:**
Both HappinessManager and the new HousingManager subscribe to `GameEvents.RoomDemolished`. HappinessManager already decrements `_housingCapacity` and removes from `_housingRoomCapacities` in its `OnRoomDemolished` handler (HappinessManager.cs:386-397). If HousingManager also subscribes to `RoomDemolished` to evict citizens from a demolished room, the execution order of these two handlers is determined by C# delegate invocation order (subscription order), which depends on Autoload initialization order in project.godot. If HousingManager tries to query HappinessManager's housing capacity during its own demolish handler, it may see pre- or post-decrement values depending on who fires first.

**Why it happens:**
C# multicast delegates invoke subscribers in subscription order, but developers rarely reason about this. The existing codebase has never had two singletons both reacting to the same event with interdependent state mutations. Adding HousingManager as an 8th autoload creates this cross-handler dependency for the first time.

**How to avoid:**
HousingManager should own its own citizen-to-room mapping and NOT depend on HappinessManager's `_housingCapacity` value during the same event frame. When `RoomDemolished` fires, HousingManager should:
1. Look up citizens assigned to that segment index from its own dictionary.
2. Mark them unhoused.
3. Attempt reassignment to other rooms (using its own room tracking, not HappinessManager's).

The key principle: each handler should be self-contained, mutating only its own state. Cross-singleton queries should happen AFTER the event propagation settles (e.g., next frame or via a deferred call).

**Warning signs:**
- Citizens remain "assigned" to a demolished room (ghost assignments).
- Capacity count becomes negative or desynchronized between HappinessManager and HousingManager.
- Reassignment silently fails because HousingManager thought there was capacity that HappinessManager already decremented.

**Phase to address:**
Phase 1 (HousingManager core) -- the very first phase must establish the principle that HousingManager tracks its own room-to-citizen map independently.

---

### Pitfall 2: Demolish-Then-Rebuild Cascade Creates Phantom Assignments

**What goes wrong:**
When a housing room is demolished, citizens are evicted and reassignment is attempted. If all other housing is full, those citizens become unhoused. But `RoomPlaced` also triggers reassignment of unhoused citizens to newly built rooms. If a player rapidly demolishes one room and places another in the same frame group (remember: the debounce timer batches saves at 0.5s), the event sequence becomes `RoomDemolished -> evict -> RoomPlaced -> reassign`. The citizen could be mid-eviction-tween (returning to walkway) when reassignment fires, sending them to a new room before they visually finished leaving the old one.

**Why it happens:**
The existing visit system in CitizenNode uses a single `_activeTween` with kill-before-create pattern (CitizenNode.cs:425). A return-home tween would need to follow the same pattern. But if HousingManager reassigns mid-animation, the citizen's home segment changes while a tween referencing the old segment is still running.

**How to avoid:**
1. HousingManager should track a `_isReturningHome` state on citizen nodes (or a flag on HousingManager's side).
2. On demolish eviction, if the citizen is mid-return-home-tween, kill the tween first, snap citizen to walkway, THEN mark unhoused.
3. On reassignment, don't start a new return-home cycle immediately -- just update the assignment data. The next natural home timer tick will use the new assignment.

**Warning signs:**
- Citizens teleport or disappear during demolish-rebuild sequences.
- The `_activeTween` is killed mid-fade, leaving a citizen with alpha=0 (invisible) on the walkway.
- Citizen appears to "visit" a room that no longer exists.

**Phase to address:**
Phase 2 (return-home behavior) -- the tween interruption logic must be designed alongside the behavior, not bolted on after.

---

### Pitfall 3: Home Timer Interfering With Existing Visit/Wish Cycle

**What goes wrong:**
CitizenNode already has `_visitTimer` (20-40s, periodic) and `_wishTimer` (30-60s, one-shot). Adding a `_homeTimer` (90-150s) creates three independent timers that can fire simultaneously or in rapid succession. The PRD says home return is lower priority than wish visits, but the existing visit system doesn't have a priority concept -- `_isVisiting` is a simple boolean guard. If `_homeTimer` fires while `_isVisiting` is false but `_visitTimer` fires 0.1s later with a wish-matching room nearby, the citizen starts going home and misses the wish visit.

**Why it happens:**
Timer-based systems have inherent race conditions. The existing system works because visit and wish timers cooperate -- a visit CAN fulfill a wish. But a home-return visit cannot fulfill a wish (different destination), so it directly competes with the wish system.

**How to avoid:**
Do NOT implement home return as a separate timer that independently starts tweens. Instead:
1. Add home-return as an alternative destination within the existing `OnVisitTimerTimeout` logic.
2. When the home timer fires, set a `_wantsToGoHome` flag.
3. On the next `OnVisitTimerTimeout`, if `_wantsToGoHome` is true AND no wish-matching room is nearby, execute the home visit. If a wish-matching room IS nearby, clear the flag and visit the wish room instead (home timer resets).
4. This preserves the existing single-tween-at-a-time architecture.

Alternatively, keep separate timers but add a priority check: when home timer fires, check if `_currentWish != null` and a fulfilling room is nearby. If yes, skip and reset. This is simpler but less elegant.

**Warning signs:**
- Citizens go home right after getting a wish (should visit the wish room instead).
- Three active tweens fighting over `_activeTween` (kill-create-kill-create churn).
- Wish fulfillment rate drops after housing is implemented (citizens spending too much time going home).

**Phase to address:**
Phase 2 (return-home behavior) -- the priority rules must be implemented as part of the core behavior, not patched after.

---

### Pitfall 4: Save Format v3 Breaking v2 Load Path

**What goes wrong:**
The save system currently handles v1->v2 migration via `data.Version >= 2` guard (SaveManager.cs:389). Adding `CitizenHomes` (Dictionary<string, int>) to SaveData creates a v3 format. If the new field is added without a version bump, old v2 saves will deserialize `CitizenHomes` as null (System.Text.Json default for missing dictionary fields is null, not empty). Every null-check-free access crashes. If the version IS bumped to 3, the existing v2 restore path needs to handle the missing field.

**Why it happens:**
System.Text.Json deserializes missing properties as their C# default value. For `Dictionary<string, int>`, the default is `null` (not an empty dictionary), unlike the `List<T>` fields which are initialized in the class definition with `= new()`. The existing SaveData initializes lists with `= new()` but a developer adding a dictionary might forget this pattern.

**How to avoid:**
1. Initialize the new field in SaveData class definition: `public Dictionary<string, int> CitizenHomes { get; set; } = new();`
2. Bump version to 3 in `CollectGameState`.
3. In `ApplySceneState`, after restoring citizens, call `HousingManager.RestoreAssignments(data.CitizenHomes)` -- but guard it: if `data.Version < 3` or `CitizenHomes` is null/empty, skip and let HousingManager auto-assign on first frame (the same path as a new game).
4. Keep the v2 restore path untouched -- it already works.

**Warning signs:**
- NullReferenceException on load with old saves.
- Citizens all become unhoused after loading a save from before housing was implemented.
- Version check cascading: adding v3 guard makes the code confusing if v4 comes later.

**Phase to address:**
Phase for save/load integration -- must be designed with backward compatibility from the start. Do NOT add the dictionary field without the `= new()` initializer.

---

### Pitfall 5: Capacity Scaling Changing BaseCapacity Interpretation Globally

**What goes wrong:**
The PRD recommends size-scaled capacity: `BaseCapacity + (segmentCount - 1)`. But `BaseCapacity` is already used by HappinessManager to track housing capacity (HappinessManager.cs:373-376). HappinessManager's `OnRoomPlaced` reads `def.BaseCapacity` directly and adds it to `_housingCapacity`. If HousingManager uses the scaled formula `BaseCapacity + (segmentCount - 1)` but HappinessManager continues using raw `BaseCapacity`, the two will track different capacity numbers. Player sees "5/7" but HousingManager thinks capacity is 8.

**Why it happens:**
`BaseCapacity` currently means "max occupants for this room type" and is treated as a fixed value in HappinessManager. The scaling formula changes its meaning to "base occupants for the smallest version of this room type." This semantic change affects every consumer of `BaseCapacity`.

**How to avoid:**
The capacity scaling formula must be centralized in ONE place. Two approaches:

**Option A (recommended):** Add a static helper method `HousingManager.CalculateCapacity(RoomDefinition def, int segmentCount)` that returns `def.BaseCapacity + (segmentCount - 1)`. Both HousingManager and HappinessManager call this method. HappinessManager's `OnRoomPlaced` and `OnRoomDemolished` are updated to use the scaled value.

**Option B:** Move all capacity tracking into HousingManager and have HappinessManager query `HousingManager.TotalCapacity` instead of tracking its own `_housingCapacity`. This is cleaner but requires more refactoring of HappinessManager.

Either way, the `_housingRoomCapacities` dictionary in HappinessManager (which stores capacity per anchor index for demolish lookup) must store the SCALED value, not the raw BaseCapacity.

**Warning signs:**
- PopulationDisplay shows different numbers than what gates citizen arrivals.
- Building a 2-segment Bunk Pod shows capacity +2 in one place and +3 in another.
- Demolishing a room subtracts the wrong capacity amount.

**Phase to address:**
Phase 1 (HousingManager core) -- capacity calculation must be centralized BEFORE any assignment logic uses it.

---

### Pitfall 6: Autoload Initialization Order Dependency

**What goes wrong:**
HousingManager is the 8th autoload. It needs references to CitizenManager (to get citizen list for assignment), BuildManager (to query placed rooms), and HappinessManager (for capacity coordination). If HousingManager is listed before any of these in project.godot's autoload order, their `.Instance` properties will be null when HousingManager's `_Ready()` runs. The existing autoloads work because they were carefully ordered: GameEvents first, then domain singletons, SaveManager last.

**Why it happens:**
Godot initializes autoloads in the order they appear in project.godot. The existing codebase documents this (SaveManager comment: "Registered as Autoload in project.godot (last, after all other singletons)"). A new autoload inserted at the wrong position breaks this chain.

**How to avoid:**
1. HousingManager must be registered AFTER CitizenManager and BuildManager but BEFORE SaveManager in project.godot.
2. In HousingManager._Ready(), assert that required singletons are non-null: `if (CitizenManager.Instance == null) GD.PushError(...)`.
3. SaveManager must be updated to also subscribe to housing-related events for autosave triggers, and its `CollectGameState` / `ApplySceneState` must include housing data.
4. Document the required order in a comment at the top of HousingManager, matching the existing pattern (e.g., HappinessManager.cs:16: "Registered as an Autoload in project.godot (6th, after all other singletons)").

**Warning signs:**
- NullReferenceException in HousingManager._Ready() on first launch.
- Housing assignments silently fail (null-conditional `?.` swallows the error).
- Save file never includes housing data because SaveManager doesn't know about HousingManager.

**Phase to address:**
Phase 1 (HousingManager core) -- autoload registration is literally the first thing to get right.

---

### Pitfall 7: FloatingText Reuse for Zzz Creates Wrong Visual Layer

**What goes wrong:**
The existing `FloatingText` is a 2D `Label` that lives on a `CanvasLayer` (HappinessManager creates `_arrivalCanvasLayer` at layer 5, CreditHUD uses layer 5). It takes a `Vector2 startPosition` in screen space. The Zzz floater needs to appear at a 3D world position (above the citizen or at the room entrance). Using FloatingText directly requires projecting the 3D position to screen space, which looks wrong when the camera orbits -- the text stays at a fixed screen position while the 3D world rotates.

**Why it happens:**
FloatingText was designed for HUD notifications (screen-center arrival text, credit +/- amounts). It self-destructs after 0.9s. The Zzz visual needs to be anchored in 3D space (billboarded, like the wish badge), not screen space.

**How to avoid:**
Two approaches:

**Option A (PRD suggestion, simpler):** Use FloatingText but spawn it with a projected screen position and accept it won't track camera movement (it's only visible for ~1s, camera rarely moves that fast). This is acceptable if the float duration is short enough.

**Option B (better visual):** Create the Zzz as a `Label3D` or `Sprite3D` (like the wish badge) attached to the citizen node. Billboard mode makes it always face the camera. This is consistent with the existing badge system and doesn't fight the 3D camera.

Option B is recommended because the PRD says "same style as the existing FloatingText but smaller and lighter colored" -- this means matching the AESTHETIC, not the implementation class. The wish badge (Sprite3D, billboard) is the better pattern to follow for 3D-anchored indicators.

**Warning signs:**
- Zzz text appears at screen center instead of near the citizen.
- Zzz text doesn't move with camera orbit (frozen in screen space).
- Multiple Zzz labels stack on top of each other when many citizens go home simultaneously.

**Phase to address:**
Phase 2 (return-home behavior) -- the Zzz visual is part of the return-home sequence and should be designed alongside it.

---

### Pitfall 8: Stale Home Assignment After Save/Load With Room Layout Changes

**What goes wrong:**
The save file stores `CitizenHomes` as `citizenName -> segmentIndex`. On load, if the player demolished and rebuilt rooms between the save and the load (possible if autosave fires mid-session and player continues playing), the segment index might now point to a different room type or an empty segment. The PRD edge case table mentions this: "Save/load with demolished home -- citizen's saved homeSegmentIndex won't match a placed room." But the implementation must actively validate, not just document.

**Why it happens:**
Segment indices are positional, not room-identity-based. There's no room UUID. A room at segment 5 today might be a different room (or no room) after demolish+rebuild. The existing save system has a similar implicit coupling -- `SavedRoom` stores position, not ID-based references -- but rooms are rebuilt from scratch on load so it works. Citizen home assignments reference rooms by position, and positions can be reused.

**How to avoid:**
On load, after restoring rooms and citizens, HousingManager must VALIDATE every assignment:
1. For each `citizenName -> segmentIndex` in the loaded data, check if `BuildManager.GetPlacedRoom(segmentIndex)` returns a Housing-category room.
2. If yes, assign. If no (room gone, or now a non-Housing room), mark citizen as unhoused.
3. After validation, run reassignment for all unhoused citizens (same logic as new game).
4. This validation loop is the same logic used when a room is demolished at runtime -- reuse it.

**Warning signs:**
- Citizens "assigned" to a Workshop or Air Recycler after load.
- Citizens assigned to empty segments (no room there).
- Population display shows assigned citizens but no home-return behavior occurs.

**Phase to address:**
Save/load integration phase -- validation must be part of the restore path.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Duplicating capacity tracking in both HappinessManager and HousingManager | Quick implementation, no refactoring | Two sources of truth that drift apart; every future capacity change must update both | Never -- centralize from day one |
| Storing home assignment on CitizenNode directly (not in HousingManager) | Fewer cross-references, simpler citizen code | Assignment logic scattered across CitizenNode and HousingManager; save/load must reach into CitizenNode to serialize | Never -- HousingManager should be the single source of truth for assignments |
| Hardcoding home timer constants in CitizenNode instead of HousingConfig | Faster first implementation | Tuning requires code changes and recompilation, breaks the established Config resource pattern | Only in initial prototype, must extract to HousingConfig before merge |
| Using `_isVisiting` flag for both regular visits and home visits | Avoids adding a new state field | Cannot distinguish "visiting a room" from "returning home" for priority logic, tooltip display, or save/load | Only if home visits and regular visits are truly identical in every external-facing way (they are not -- Zzz visual, wish timer pause, longer duration) |
| Skipping unsubscribe for HousingManager events | Saves a few lines | Memory leak if HousingManager is ever re-created (unlikely for autoload but violates SafeNode pattern established in codebase) | Never -- follow the established SubscribeEvents/UnsubscribeEvents pattern |

## Integration Gotchas

| Integration Point | Common Mistake | Correct Approach |
|-------------------|----------------|------------------|
| HousingManager + GameEvents | Adding new events to GameEvents without Emit helper methods | Follow the established pattern: add `event Action<...>` field AND `EmitX()` method. Every existing event has both (see GameEvents.cs structure) |
| HousingManager + SaveManager | Forgetting to add housing events to SaveManager's autosave trigger subscriptions | SaveManager subscribes to state-change events for debounced autosave (SaveManager.cs:144-152). Housing assignment changes MUST trigger autosave. Add subscriber delegates for any new housing events |
| HousingManager + CitizenManager.SpawnCitizen | Calling HousingManager.AssignHome inside CitizenManager.SpawnCitizen, creating a circular dependency | SpawnCitizen should emit CitizenArrived event (it already does). HousingManager subscribes to CitizenArrived and handles assignment. This follows the existing event-driven pattern |
| HousingConfig + ResourceLoader | Creating HousingConfig.tres in a different directory than existing configs | Follow existing pattern: `res://Resources/Housing/default_housing.tres` (parallel to `res://Resources/Happiness/default_happiness.tres`). Load with same fallback pattern as HappinessManager.cs:202-208 |
| SegmentTooltip + resident list | Appending resident names directly in SegmentGrid.GetLabel() | GetLabel is a pure data method on SegmentGrid (no UI concerns). Add resident info in SegmentInteraction.UpdateHover() where the tooltip text is composed, querying HousingManager for residents at that segment |
| CitizenInfoPanel + home display | Querying BuildManager for room name in CitizenInfoPanel.ShowForCitizen | CitizenInfoPanel should query HousingManager for the citizen's home segment, then query BuildManager for the room definition at that segment. Two-step lookup, not one |

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Iterating all citizens on every RoomPlaced/RoomDemolished to find affected assignments | Unnoticeable at 5-20 citizens, frame hitch at 50+ | HousingManager maintains a `Dictionary<int, List<string>>` mapping segment index to citizen names. O(1) lookup on demolish instead of O(n) scan | 50+ citizens (unlikely in current design but good practice) |
| Running reassignment algorithm (find room with fewest occupants) on every citizen arrival | Linear scan of all housing rooms per arrival | Cache a sorted/priority structure or just scan the `_housingRoomCapacities` dict -- with max 24 rooms, this is not a real problem. Do NOT over-engineer | Never breaks at 24-room scale. This is a non-issue but noted to prevent premature optimization |
| Creating new Timer nodes for each home-return cycle instead of reusing one | Timer node count grows, tree overhead | Create ONE `_homeTimer` per citizen in Initialize() (same pattern as `_visitTimer`), reuse with WaitTime reset. Never `new Timer()` per cycle | Immediately -- Godot Timer creation has scene tree overhead |
| Spawning Zzz Label3D/Sprite3D nodes that aren't freed | Node tree grows unboundedly as citizens go home repeatedly | Use self-destructing pattern (FloatingText's `TweenCallback(QueueFree)`) or reuse a single Zzz node per citizen | After 20+ home cycles per citizen (~30 min play session) |

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Showing "Unhoused" with negative connotation in citizen info panel | Players feel guilty, breaks cozy philosophy | Show `Home: --` (em dash) with no label text, matching the PRD. No drama, no negative framing |
| Zzz floater being too prominent (large text, bright color) | Visual noise when 10+ citizens go home simultaneously | Keep it small (smaller than wish badge), muted color (light gray/lavender), short duration (0.5-0.8s). Subtle > noticeable |
| Room tooltip showing long resident lists for Sky Loft (up to 6 names) | Tooltip becomes unwieldy, overlaps other UI | Cap display at 3-4 names + "and 2 more" truncation. Or use first-name-only display |
| No visual feedback when citizen is assigned to a new home | Player builds housing but doesn't see the connection to citizens | Consider a brief subtle highlight or the first home-return happening promptly (not waiting 90-150s). Use a short initial delay (5-10s) for the very first home return after assignment |
| Home-return timer resetting on save/load | Citizens all go home simultaneously after loading (timer synchronization) | On load, randomize home timer's remaining time (same as the existing visit timer randomization in CitizenNode.Initialize) |

## "Looks Done But Isn't" Checklist

- [ ] **Home assignment:** Verify citizens are EVENLY distributed across rooms (not all crammed into first-built room) -- test with multiple housing rooms of different sizes
- [ ] **Demolish reassignment:** Verify citizens from demolished room are reassigned to remaining housing, not just marked unhoused permanently -- test demolish-with-alternatives-available
- [ ] **Unhoused graceful handling:** Verify unhoused citizens still walk, visit rooms, generate wishes, and fulfill them identically to housed citizens -- test with zero housing rooms
- [ ] **Save/load round-trip:** Verify housing assignments survive save-quit-load cycle AND that loading old v2 saves without housing data doesn't crash -- test with a pre-housing save file
- [ ] **Capacity arithmetic:** Verify `PopulationDisplay` count matches `HappinessManager._housingCapacity` matches `HousingManager` room capacity tracking -- test after building/demolishing mixed room types
- [ ] **Timer independence:** Verify home timer doesn't block wish fulfillment -- test by giving citizen a wish, confirming they visit the wish room even when home timer has fired
- [ ] **Starter citizens:** Verify the 5 starter citizens (spawned before any rooms exist) get assigned when first housing room is built -- test fresh game with first build being a Bunk Pod
- [ ] **Multi-segment capacity:** Verify a 2-segment Bunk Pod holds 3 citizens (base 2 + 1) and a 3-segment Sky Loft holds 6 (base 4 + 2) -- test exact capacity boundaries
- [ ] **Zzz visual:** Verify Zzz appears at room/citizen position in 3D space, not screen center -- test with camera at different orbit angles
- [ ] **Room tooltip residents:** Verify tooltip shows current residents and updates when citizens are reassigned -- test after demolish/rebuild

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Dual capacity tracking divergence | LOW | Add a `SyncCapacity()` method that recalculates from BuildManager scan. Call on save/load as a consistency check. Similar to existing `InitializeHousingCapacity()` pattern |
| Event ordering race on demolish | MEDIUM | Refactor to use a single "process housing changes" method called once per frame (collect events, process in batch) instead of immediate event handlers. Only if the race actually manifests |
| Save format v3 crashes on old saves | LOW | Add null-coalescing: `data.CitizenHomes ?? new()`. Initialize field in SaveData class. No migration code needed since missing = unhoused = auto-assign |
| Home timer fighting wish timer | MEDIUM | Add explicit state machine (Walking, Visiting, ReturningHome, Resting) instead of boolean `_isVisiting`. Bigger refactor but eliminates all timer races. Only if simple priority check proves insufficient |
| Zzz visual implementation wrong | LOW | Swap from FloatingText to Sprite3D/Label3D. Localized change in the return-home tween sequence, no architectural impact |

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Event ordering race (demolish) | Phase 1: HousingManager core | Unit test: demolish room, verify citizens evicted AND capacity decremented correctly in both managers |
| Demolish-rebuild cascade | Phase 2: Return-home behavior | Manual test: demolish housing, immediately build new housing, verify no visual glitches |
| Home timer vs wish/visit priority | Phase 2: Return-home behavior | Manual test: give citizen wish, wait for home timer, verify wish visit takes priority |
| Save format v3 backward compat | Save/load integration phase | Automated test: load v2 save file, verify no crash, citizens auto-assigned |
| Capacity scaling interpretation | Phase 1: HousingManager core | Verify capacity formula in one central method, all consumers use it |
| Autoload initialization order | Phase 1: HousingManager core | Verify project.godot ordering, add null-check assertions in _Ready() |
| FloatingText vs 3D Zzz visual | Phase 2: Return-home behavior | Visual test: orbit camera during home return, verify Zzz tracks 3D position |
| Stale assignment on load | Save/load integration phase | Manual test: save, demolish room, load, verify evicted citizens are properly unhoused |

## Sources

- Direct codebase analysis of `/workspace/Scripts/Autoloads/GameEvents.cs` (event bus with 15+ event delegates)
- Direct codebase analysis of `/workspace/Scripts/Citizens/CitizenNode.cs` (visit tween architecture, timer patterns)
- Direct codebase analysis of `/workspace/Scripts/Autoloads/SaveManager.cs` (save format versioning, frame-delay restore)
- Direct codebase analysis of `/workspace/Scripts/Autoloads/HappinessManager.cs` (housing capacity tracking, event subscriptions)
- Direct codebase analysis of `/workspace/Scripts/Build/BuildManager.cs` (room placement/demolish flow, PlacedRoom record)
- Direct codebase analysis of `/workspace/Scripts/UI/FloatingText.cs` (2D Label, screen-space positioning)
- Direct codebase analysis of `/workspace/Scripts/UI/SegmentTooltip.cs` (tooltip text composition)
- Direct codebase analysis of `/workspace/Scripts/UI/CitizenInfoPanel.cs` (citizen panel structure)
- Direct codebase analysis of `/workspace/Scripts/Data/RoomDefinition.cs` (BaseCapacity field, RoomCategory enum)
- Direct codebase analysis of `/workspace/Scripts/Ring/SegmentGrid.cs` (flat index system, 24 total segments)
- PRD analysis of `/workspace/docs/prd-housing.md` (design decisions, edge cases, open questions)

---
*Pitfalls research for: Orbital Rings v1.2 Housing System*
*Researched: 2026-03-05*
