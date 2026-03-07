# Domain Pitfalls

**Domain:** Adding utility-based citizen AI, trait system, day/night lighting cycle, and citizen state machines to an existing Godot 4 C# cozy space station builder (v1.3 -> v1.4)
**Researched:** 2026-03-07
**Confidence:** HIGH (derived from direct analysis of 20+ source files in the codebase, Godot 4 documentation, utility AI literature, and community patterns)

---

## Critical Pitfalls

Mistakes that cause rewrites, data loss, or broken saves.

---

### Pitfall 1: Timer Replacement Race -- Unified Decision Timer vs. Visit/Home Timers

**What goes wrong:**
The current CitizenNode has three independent timers: `_visitTimer` (20-40s), `_homeTimer` (90-150s), and `_wishTimer` (30-60s). The v1.4 plan replaces the visit and home timers with a single unified decision timer (15-30s). If the replacement is done as a refactor-in-place (modify CitizenNode to use the new timer), there is a window where partially-converted code has both the old timer fields and new state machine logic trying to drive citizen behavior simultaneously. A citizen could be mid-tween from the old visit system when the new state machine tries to transition them to Evaluating state.

The existing `_activeTween?.Kill()` pattern (CitizenNode.cs line 501, 839) catches most conflicts, but if the old timer fires a callback that sets `_isVisiting = true` while the state machine expects to own that flag, the citizen freezes: the state machine won't advance because `_isVisiting` is true, but no new tween will restore it because the old visit completion callback was killed.

**Why it happens:**
CitizenNode is a 1107-line file with visit logic, home return logic, wish logic, and mesh management all interleaved. The boolean flags `_isVisiting`, `_isAtHome`, and `_walkingToHome` serve as an implicit state machine -- exactly the pattern v1.4 wants to replace with an explicit one. But during the conversion, both systems can be partially active.

**Consequences:**
Citizens stuck invisible (faded out, never fade back in). Citizens walking through walls (radial position set by tween, never restored to walkway). Save/load restoring a citizen mid-visit with no tween to complete the sequence.

**Prevention:**
1. Do NOT refactor CitizenNode incrementally. Extract the state machine as a separate POCO class (`CitizenStateMachine`) that completely owns state transitions. The POCO takes an `ICitizenActions` interface for "move to walkway," "start visit tween," etc.
2. Delete the old `_visitTimer`, `_homeTimer`, `_isVisiting`, `_isAtHome`, `_walkingToHome` fields in a single commit. Replace them with `_stateMachine.CurrentState`. No co-existence period.
3. The unified decision timer should be owned by the state machine, not CitizenNode directly. When the timer fires, the state machine evaluates its current state and decides the next transition.
4. Test the state machine POCO in isolation (no tweens, no timers) before wiring it into CitizenNode. Each state transition should be a pure function of (currentState, input) -> (newState, action).

**Detection:**
Citizens that are invisible but still accumulating walkway angle (walking while "inside" a room). The `_Process` method runs but `_isVisiting` is true, so movement is skipped, but the state machine thinks the citizen is Walking.

**Phase to address:** Phase 1 (State Machine implementation) -- this is the foundational refactor that everything else builds on.

---

### Pitfall 2: Save Format v3 -> v4 Migration Breaks Existing Saves

**What goes wrong:**
The save format must add new fields for v1.4: clock position (current period/time), citizen traits (Interest enum, Rhythm enum), and potentially citizen state. The current `SaveData` class defaults `Version` to `3`. If the developer bumps this to `4` but does not add version-gated restore logic in `SaveManager.ApplyState()`, v3 saves loaded by v4 code will:

1. Deserialize `SavedCitizen` objects missing the new trait fields. System.Text.Json will set them to their C# defaults (0 for enums, null for nullable types). This is correct behavior IF the restore code handles "no traits" gracefully.
2. The clock position field will be missing. If the restore code assumes it's always present and does `data.ClockTime / PeriodDuration`, it divides by a default 0 and gets NaN or Infinity, corrupting the clock state.
3. If traits are stored as string enums (e.g., `"Interest": "Science"`) and the enum values change between versions, `JsonSerializer.Deserialize` throws `JsonException` on unknown enum values unless `JsonStringEnumConverter` with `JsonSerializerOptions.Converters` is configured for lenient parsing.

**Why it happens:**
The existing codebase has a proven version-gated pattern (v1 -> v2 -> v3), but each migration was simple (add nullable fields with safe defaults). v3 -> v4 is more complex because traits affect behavior, not just data. A citizen loaded from a v3 save with no traits must still function correctly -- they need default traits assigned, not null traits that cause NullReferenceException in utility scoring.

**Consequences:**
Players lose their saves. Worse: saves load without error but citizens have null traits, causing NullReferenceException when utility scoring tries to read `citizen.Interest.AffinityBonus`. The game crashes on the first decision timer tick, which happens 15-30 seconds after load -- long enough that the autosave fires with corrupted state, overwriting the valid v3 save.

**Prevention:**
1. New `SavedCitizen` fields must be nullable or have safe defaults:
   ```csharp
   // SavedCitizen additions for v4
   public string Interest { get; set; }  // null when loading v3 saves
   public string Rhythm { get; set; }    // null when loading v3 saves
   ```
2. In `ApplySceneState`, after spawning citizens from save, check for missing traits:
   ```csharp
   if (string.IsNullOrEmpty(citizen.Interest))
       assignedInterest = TraitAssigner.AssignRandomInterest();
   ```
3. Clock position: default to Morning (period 0, time 0) when loading v3 saves. This is the natural "fresh start" for the day/night cycle.
4. Add `SaveData.Version = 4` only in the `CollectGameState()` method. Keep the version-gated restore pattern:
   ```csharp
   if (data.Version >= 4)
   {
       // Restore clock and traits from save
   }
   else
   {
       // Default clock to Morning, assign random traits
   }
   ```
5. Add a v3 backward-compatibility test that loads hand-crafted v3 JSON (no trait fields, no clock fields) and verifies citizens get assigned traits and clock starts at Morning.
6. Critically: suppress autosave during the trait assignment window. The `_isRestoring` pattern from HousingManager should be extended to cover the trait assignment phase.

**Detection:**
NullReferenceException in utility scoring 15-30 seconds after loading a v3 save. The stack trace will point to trait access on a citizen whose traits were never assigned.

**Phase to address:** Phase implementing save/load for new features -- must include v3 backward-compatibility test as acceptance criteria.

---

### Pitfall 3: Utility Scoring Returns Garbage When No Rooms or All Equal Scores

**What goes wrong:**
The utility scoring system evaluates all built rooms and picks the highest-scoring one for a citizen to visit. Three degenerate cases break naive implementations:

**Case A -- No rooms built:** The player hasn't built any rooms yet (early game or after mass demolition). The scoring function iterates an empty list and returns... what? If it returns null, the caller must handle null. If it returns a default segment index of 0, the citizen walks to segment 0 and tries to visit an empty segment, causing the visit tween to drift toward a non-existent room.

**Case B -- All rooms score identically:** With no traits (v3 save loaded without traits) or with a citizen whose trait has no room affinity, all rooms get the same base proximity score. The `>` comparison picks the first room in iteration order (same pitfall documented in the existing visit system). Citizens cluster at segment 0 instead of spreading naturally across the ring.

**Case C -- Only housing rooms built:** The player has only built housing. Housing rooms should be excluded from utility scoring for "visit" decisions (citizens go home via the rest state, not the visit state). If housing isn't excluded, citizens "visit" their own home room through the visit pathway instead of the rest pathway, bypassing the Zzz indicator and rest duration logic.

**Why it happens:**
The current visit system (CitizenNode.cs lines 429-488) has a simple proximity-based selection with a `VisitProximityThreshold` guard. The utility system replaces this with multi-factor scoring but may not replicate all the implicit guards. The wish-aware weighting (`WishMatchDistanceMultiplier = 0.3f`) is a multiplicative bonus -- if the base distance is 0 (citizen is standing at the room), the weighted distance is still 0, and all wish-matching rooms at the same location score identically.

**Consequences:**
Case A: Citizens attempt to visit empty segments, drift to incorrect radial positions, get stuck. Case B: All citizens converge on the same room, breaking the "spread across the station" aesthetic. Case C: Rest behavior is bypassed, Zzz indicators never show, home timer and rest timer logic becomes dead code.

**Prevention:**
1. The utility scorer must return a `RoomChoice?` (nullable struct), not a bare segment index. The caller (state machine Evaluating state) must handle null by transitioning to Walking instead of Visiting.
2. Break ties with reservoir sampling (the existing `FindBestRoom` in HousingManager already does this correctly -- reuse the pattern):
   ```csharp
   if (score > bestScore) { bestScore = score; bestIndex = i; tieCount = 1; }
   else if (score == bestScore) { tieCount++; if (GD.Randi() % tieCount == 0) bestIndex = i; }
   ```
3. Filter out housing rooms from the visit candidate list before scoring. The existing `RoomDefinition.RoomCategory` enum makes this a one-line check.
4. Add a "recency penalty" factor that reduces scores for recently-visited rooms. This naturally breaks ties and prevents clustering even without traits.
5. Normalize all scoring factors to 0.0-1.0 range before combining them. The proximity factor uses angular distance (0 to Pi radians), the trait affinity is an enum bonus, and the wish bonus is a multiplier -- these have completely different scales. Without normalization, the largest-scale factor dominates and the others are irrelevant.

**Detection:**
All citizens walking to the same room. Citizens visiting empty segments. Zero Zzz indicators despite citizens having homes.

**Phase to address:** Phase implementing utility scoring -- normalization and edge cases must be unit tested before integration.

---

### Pitfall 4: Day/Night Lighting Transition Causes Visual Artifacts in Existing Scene

**What goes wrong:**
The game currently has a static lighting setup: a single `DirectionalLight3D` (or ambient), a `WorldEnvironment` with fixed settings, and room blocks using `StandardMaterial3D` with static `AlbedoColor`. Adding day/night transitions means tweening multiple properties simultaneously:
- `DirectionalLight3D.light_energy` (brightness)
- `DirectionalLight3D.light_color` (warm -> cool)
- `WorldEnvironment.environment.ambient_light_color`
- `WorldEnvironment.environment.ambient_light_energy`
- Room `StandardMaterial3D.emission_enabled` + `emission_energy_multiplier` (windows glow at night)

Several things go wrong:

**Artifact 1 -- Emission glow conflict:** The existing citizen selection system (CitizenManager.cs lines 274-279) enables `EmissionEnabled = true` and sets `EmissionEnergyMultiplier = 2.5f` on the selected citizen's material for a bloom/glow effect. If the day/night system also modifies emission on room materials, and the WorldEnvironment glow threshold changes during transitions, selected citizens may lose their glow (threshold raised) or ALL emissive materials bloom uncontrollably (threshold lowered).

**Artifact 2 -- Material instance sharing:** `RoomVisual.CreateRoomBlock()` creates a new `StandardMaterial3D` per room. But if the day/night system tries to modify materials by iterating room meshes and setting emission, it must ensure it's modifying the room's OWN material, not a shared resource. The `MaterialOverride` pattern used in the codebase is safe (each room gets its own instance), but any code that creates materials via `ResourceLoader.Load<StandardMaterial3D>()` would share instances.

**Artifact 3 -- Transition popping:** Abrupt property changes between periods (Morning -> Day -> Evening -> Night) cause visible "pops" in lighting. A tween that interpolates `light_energy` from 1.0 to 0.3 over 2 seconds looks smooth, but if the environment `ambient_light_color` changes from warm to cool in the same 2 seconds, the combined effect can produce a muddy intermediate color that flashes on screen.

**Why it happens:**
The existing scene was designed for a single static lighting state. Room materials use flat `AlbedoColor` with no emission. The WorldEnvironment's glow settings (if any) were tuned for the static case. Adding animated lighting requires coordinating changes across multiple nodes and materials that were never designed to be animated.

**Consequences:**
Ugly intermediate colors during transitions. Citizens' selection glow disappearing or rooms glowing too brightly. Player perceives the day/night cycle as "janky" rather than "cozy."

**Prevention:**
1. Create a `LightingProfile` Resource class with all lighting parameters for each period (sun energy, sun color, ambient color, ambient energy, glow threshold, glow bloom). Transition by interpolating between two profiles, not by tweening individual properties independently.
2. Separate glow channels: citizen selection glow should use a DIFFERENT technique than room window emission. Option: citizen glow uses `EmissionEnergyMultiplier` above the glow threshold, while room windows use values BELOW the threshold (just visible color, no bloom). Or use separate material properties (emission vs. albedo brightness).
3. Use a single master tween for each transition that controls all properties in parallel with the same duration and easing curve. Never have lighting properties transition at different speeds.
4. Keep the WorldEnvironment glow threshold constant. Change the emission intensity on emissive materials to cross/uncross the threshold, not the threshold itself. This prevents side effects on citizen selection glow.
5. Test transitions by teleporting the clock (advance period instantly) and verifying no visual artifacts. Then test with normal transition speed.

**Detection:**
Sudden color flashes during period transitions. Citizens lose glow highlight when selected during evening/night. Room blocks that were blue (comfort category) now look purple because emission interacts with albedo.

**Phase to address:** Phase implementing day/night lighting -- must be prototyped visually before committing to a specific approach.

---

### Pitfall 5: State Machine Interrupt -- Citizen Visiting Room That Gets Demolished

**What goes wrong:**
The existing code handles room demolition during a home visit (CitizenNode.cs `OnRoomDemolished` -> `EjectFromHome`, lines 1022-1029). But the state machine introduces a new `Visiting` state where citizens visit non-home rooms. If a room is demolished while a citizen is in the Visiting state (invisible, inside the room during the visit tween):

1. The visit tween is playing a `TweenInterval` (Phase 4 of `StartVisit`, line 568). The citizen is invisible (`Visible = false`).
2. `GameEvents.Instance.RoomDemolished` fires.
3. The citizen's current visit target segment matches the demolished room.
4. The tween needs to be killed, the citizen made visible, restored to walkway position, and transitioned to Walking state.

The existing `OnRoomDemolished` handler only checks `HomeSegmentIndex` (line 1025). It does NOT check if the citizen is visiting a non-home room that was demolished. The citizen remains invisible, the tween continues playing against a now-demolished room, and eventually completes -- restoring the citizen at the position of a room that no longer exists.

**Why it happens:**
The implicit state machine (boolean flags) only tracks home-related interrupts because that was the only case in v1.2-v1.3. The visit system didn't need demolish handling because visit durations are short (4-8s) and the cosmetic impact of completing a visit to a demolished room is minor. But with the explicit state machine and longer visit durations (potentially), this becomes a real issue.

**Consequences:**
Citizen briefly appears at the demolished room's position before snapping to the walkway. The `CitizenExitedRoom` event fires with the demolished room's segment index, which causes EconomyManager's `_workingCitizens` set to try removing the citizen from a room that no longer exists (this actually works because it uses set membership, not room lookup -- EconomyManager.cs line 269).

**Prevention:**
1. The state machine's `OnRoomDemolished` handler must check BOTH `HomeSegmentIndex` AND the current visit target:
   ```csharp
   if (currentState == State.Visiting && _visitTargetSegment == segmentIndex)
       TransitionTo(State.Walking); // kills tween, restores visibility
   ```
2. The `TransitionTo(Walking)` logic must include the same restoration sequence as `AbortHomeReturn()`: kill tween, restore visibility, restore walkway position, restore mesh alpha.
3. Factor the "restore citizen to walkway" logic into a single `RestoreToWalkway()` method shared by all interrupt handlers. Currently this logic is duplicated across `AbortHomeReturn()`, `EjectFromHome()`, and will need to be in the visit interrupt too.

**Detection:**
Citizen briefly visible at inner/outer radius after demolishing a room they were visiting. Citizen count in room tooltip shows 0 but a citizen is still "inside."

**Phase to address:** Phase implementing the state machine -- interrupt handling for all states must be specified in the state transition table.

---

### Pitfall 6: Trait Assignment on Existing Citizens Creates Non-Deterministic Behavior

**What goes wrong:**
When loading a v3 save (no traits), each existing citizen needs traits assigned. If traits are assigned randomly using `GD.Randi()` or `GD.Randf()`, the assignment is non-deterministic: loading the same save twice produces different trait distributions. This means:

1. Player saves, quits, reloads -- their citizens behave differently because traits were re-rolled.
2. Bug reports are unreproducible because the trait assignment varies per load.
3. If the random assignment happens to give all citizens the same Interest trait, the utility scoring degeneracy from Pitfall 3 Case B re-emerges.

**Why it happens:**
The codebase uses `GD.Randf()` and `GD.Randi()` for all randomness (citizen speed, direction, visit timing). These are stateful PRNGs that depend on Godot's global random seed, which changes every launch. There is no seeded randomness for deterministic behavior.

**Consequences:**
Player perception: "my citizens changed personality after I reloaded." This violates the cozy principle -- citizens should feel consistent and personal.

**Prevention:**
1. When assigning traits to a citizen without them (v3 save migration), seed the assignment deterministically from the citizen's name:
   ```csharp
   int seed = citizenName.GetHashCode();
   var interest = (InterestTrait)(Math.Abs(seed) % interestCount);
   var rhythm = (RhythmTrait)(Math.Abs(seed >> 16) % rhythmCount);
   ```
   This ensures the same citizen always gets the same traits on every load.
2. Once traits are assigned and saved (v4 format), they persist normally. The deterministic assignment is only needed for the v3 -> v4 migration path.
3. Ensure the trait distribution is reasonable: with 5 citizens and (say) 4 Interest types, name-seeded assignment might produce duplicates. Accept this -- it's better than random re-rolling. The player will get variety as new citizens arrive with random traits.
4. Document the deterministic assignment as a locked decision so future developers don't "simplify" it to `GD.Randi()`.

**Detection:**
Citizens changing behavior after save/load cycle without any player action. Identical saves producing different citizen movement patterns.

**Phase to address:** Phase implementing trait assignment -- the deterministic migration path must be tested with known citizen names.

---

## Moderate Pitfalls

---

### Pitfall 7: GameEvents Subscriber Explosion from New Events

**What goes wrong:**
v1.4 needs several new events on GameEvents: `ClockPeriodChanged`, `CitizenStateChanged`, `CitizenTraitsAssigned`, possibly `UtilityScoreComputed` (for debug UI). Each new event requires:
1. A new `event Action<...>` field in GameEvents.
2. An `EmitX()` method.
3. Adding the field to `ClearAllSubscribers()`.
4. Any singleton that subscribes must store a delegate reference for clean unsubscription.
5. SaveManager must decide whether to autosave on this event.
6. TestHelper.ResubscribeAllSingletons() may need updates.

Missing ANY of these steps causes either stale subscribers (memory leak + ObjectDisposedException), missed autosaves (player progress lost on crash), or test state leaking.

**Why it happens:**
The event bus pattern scales linearly in boilerplate. With 8 singletons and 30+ events, it's easy to add an event field but forget to null it in `ClearAllSubscribers()` or forget to add it to SaveManager's subscription list.

**Prevention:**
1. Create a checklist for adding new events. Every new event must touch: GameEvents (field + emit), ClearAllSubscribers, SaveManager subscription (if state-changing), subscriber unsubscription.
2. Only add events that are genuinely needed for cross-system communication. `CitizenStateChanged` might be useful for debug UI but if nothing subscribes to it, don't add it.
3. The `ClockPeriodChanged` event should trigger autosave (period transitions represent meaningful state changes). `CitizenStateChanged` should NOT trigger autosave (too frequent, would cause save spam).
4. Add a test that verifies `ClearAllSubscribers()` nulls ALL event fields. Use reflection to find all `event Action` fields and assert they're null after clearing.

**Phase to address:** Each phase that adds new events -- the checklist should be enforced at PR review time.

---

### Pitfall 8: Clock Period Duration Mismatch with Decision Timer

**What goes wrong:**
The station clock has four periods (Morning/Day/Evening/Night). The citizen decision timer fires every 15-30 seconds. If a period lasts 60 seconds and the decision timer is 25 seconds, a citizen makes approximately 2-3 decisions per period. This means schedule weighting (e.g., "Citizens prefer social rooms in the Evening") has very few samples to express itself. The player won't perceive a pattern because the random variation in individual decisions overwhelms the schedule bias.

Conversely, if periods are too long (300 seconds = 5 minutes), the day/night cycle feels sluggish. At 25 second decisions, a citizen makes ~12 decisions per period, which is enough for the schedule to be visible, but the player has to wait 20 minutes for a full day cycle.

**Why it happens:**
The decision timer and period duration are independently configurable parameters. Without calibration, they can be set to values where the schedule system has no visible effect or where the day/night cycle feels wrong for a "cozy" game.

**Prevention:**
1. Calibrate together: aim for 4-6 decisions per period. If decision timer is 20s average, periods should be ~100s each (400s full day = ~6.5 minutes).
2. Make both configurable from a single config resource so they can be tuned together in the Inspector.
3. Add a debug overlay that shows "decisions this period: N, schedule distribution: {Social: 3, Rest: 2, Work: 1}" to validate that the schedule is expressing itself.
4. Consider the "cozy" pacing: the game has no time pressure. A 5-8 minute full day cycle is appropriate. Shorter feels frantic, longer feels static.

**Phase to address:** Phase implementing the clock system -- must be tuned before schedule templates are wired up.

---

### Pitfall 9: Citizen State Machine Doesn't Pause During Build Mode

**What goes wrong:**
The current code suppresses citizen clicks during build mode (`BuildManager.Instance?.CurrentMode != BuildMode.Normal`, CitizenManager.cs line 199). But citizens continue walking, visiting rooms, and going home during build mode. With the state machine, citizens will also be making utility-scored decisions during build mode. If the player is placing a room and a citizen decides to visit the segment where the room is being placed, the citizen walks to an empty segment (room not yet placed), looks broken.

More subtly: the day/night clock should continue during build mode (it's a cosmetic system), but citizen decisions should factor in the room currently being placed (if any) -- or explicitly NOT factor it in. The current visit system doesn't have this problem because room visits only target occupied segments.

**Prevention:**
1. Keep citizen behavior running during build mode -- pausing would make the station feel "frozen" which is anti-cozy.
2. The utility scorer should only consider actually-placed rooms (not placement previews). This is already how the current visit system works (checks `_grid.IsOccupied()`), so preserve this behavior.
3. Do NOT add special-case code for build mode in the state machine. The state machine should be build-mode-agnostic. Build mode is a UI concern, not a citizen AI concern.

**Phase to address:** Non-issue if the utility scorer uses the same `BuildManager.GetPlacedRoom()` / `SegmentGrid.IsOccupied()` pattern as the current visit system.

---

### Pitfall 10: Emissive Room Windows Interact Badly with Transparency Mode

**What goes wrong:**
Citizens use `SetMeshTransparencyMode(true/false)` (CitizenNode.cs lines 1039-1059) to toggle `BaseMaterial3D.TransparencyEnum.Alpha` during fade-in/out animations. If room materials also get transparency mode toggled for day/night transitions (e.g., windows become translucent at night to show interior glow), and a citizen walks past a room during a lighting transition, the render order between transparent citizen and transparent room window can produce Z-fighting or incorrect draw order.

Godot 4's rendering pipeline sorts transparent objects by distance to camera, but annular sectors (ring segments) have complex geometry where the "center" distance is ambiguous. Citizens are small capsules that may sort behind a room wall despite being in front of it.

**Why it happens:**
The existing codebase avoids this by keeping room materials opaque at all times. Room blocks use `StandardMaterial3D` with `Transparency = Disabled` (RoomVisual.cs line 52). Only ghost previews use alpha transparency. Introducing emissive windows with potential transparency creates a new render-order concern.

**Prevention:**
1. Do NOT use alpha transparency for room window emission. Use additive emission on opaque materials instead: set `EmissionEnabled = true` and increase `EmissionEnergyMultiplier` to make windows "glow" without making the material transparent.
2. If a window glow effect requires transparency (e.g., to show light through walls), use a separate child mesh for the window that has a higher render priority (`material.render_priority`), not the room's main material.
3. Test citizen fade-in/out animations at all four lighting periods. The transparency mode toggle should work identically regardless of lighting state because room materials remain opaque.

**Phase to address:** Phase implementing day/night lighting -- test with citizen visit animations at each period.

---

## Minor Pitfalls

---

### Pitfall 11: Clock UI Shows Wrong Period After Save/Load

**What goes wrong:**
If the clock position is saved as a float (accumulated time within the current period) but the period is saved as an enum, deserialization might restore the time within the period but not the period itself, or vice versa. The UI shows "Morning" but the lighting is set to "Night" because the clock state and the environment state were restored in different order.

**Prevention:**
Save the clock as a single float (total elapsed time since the cycle started) and derive the current period from it. This eliminates the two-field desync issue. On load, set both the clock time and immediately apply the corresponding lighting profile without a transition animation.

**Phase to address:** Phase implementing clock save/load.

---

### Pitfall 12: Trait Display in CitizenInfoPanel Overflows Layout

**What goes wrong:**
The existing `CitizenInfoPanel` shows citizen name and home room. Adding trait labels ("Interest: Science", "Rhythm: Night Owl") may cause the panel to overflow its bounds, especially with long trait names. The panel is positioned at mouse click position (CitizenManager.cs line 283), which means it can overflow off-screen if clicked near an edge AND has extra content.

**Prevention:**
Design trait display with maximum string lengths in mind. Use short trait labels (single word or icon + word). Test panel at all four screen edges with the longest possible content.

**Phase to address:** Phase implementing trait UI.

---

### Pitfall 13: Schedule Template Configuration Complexity

**What goes wrong:**
Schedule templates define per-period activity weights (e.g., Morning: {Work: 0.4, Social: 0.3, Rest: 0.3}). With 4 periods, 5 room categories, and multiple traits, the configuration matrix grows to 4 * 5 * N_traits entries. If stored as Godot Resources, this requires many .tres files or deeply nested export properties that are tedious to tune.

**Prevention:**
1. Keep it simple: define 2-3 schedule templates (not one per trait combination). Traits modify the base schedule, not replace it. E.g., "Night Owl" template has higher Evening/Night activity, "Early Bird" has higher Morning/Day activity.
2. Store schedules as simple C# dictionaries in code, not as .tres resources. The number of schedules is small and fixed. Inspector tuning adds complexity for minimal benefit.
3. Weights don't need to sum to 1.0. The utility scorer normalizes them. This avoids the "oops, my weights sum to 0.7" configuration error.

**Phase to address:** Phase implementing schedule templates.

---

### Pitfall 14: Test Infrastructure Needs Expansion for New Singletons

**What goes wrong:**
v1.4 adds a new singleton (StationClock or similar) and new state on existing singletons (traits on CitizenData, state machine state on CitizenNode). The existing `TestHelper.ResetAllSingletons()` and `TestHelper.ResubscribeAllSingletons()` must be updated to include the new singleton. If forgotten, tests leak clock state between suites -- the clock is at Night in test 1, Morning is expected in test 2, test 2 fails.

**Prevention:**
1. Every new singleton must have a `Reset()` method and a `SubscribeToEvents()` method following the established pattern.
2. Add the new singleton to `TestHelper.ResetAllSingletons()` and `TestHelper.ResubscribeAllSingletons()` in the same commit that creates the singleton.
3. Add the new events to `GameEvents.ClearAllSubscribers()` in the same commit that adds them to GameEvents.
4. Run the full test suite after adding the singleton to verify no test contamination.

**Phase to address:** Phase implementing the clock singleton -- must update test infrastructure in the same commit.

---

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| Station Clock singleton | Clock period saved/loaded out of sync with lighting (Pitfall 11) | Save as single elapsed-time float, derive period on load |
| Day/night lighting | Emission conflicts with citizen selection glow (Pitfall 4, Artifact 1) | Keep glow threshold constant, separate emission channels |
| Day/night lighting | Transparent windows Z-fight with citizen fade animation (Pitfall 10) | Use additive emission on opaque materials, no transparency |
| Citizen traits | v3 save loads citizens without traits, NullRef in scoring (Pitfall 2) | Nullable trait fields, deterministic name-seeded assignment (Pitfall 6) |
| Citizen traits | Re-rolling traits on every load of v3 save (Pitfall 6) | Seed from citizen name hash, not GD.Randi() |
| Utility scoring | All rooms score identically, citizens cluster (Pitfall 3, Case B) | Reservoir sampling tie-break + recency penalty |
| Utility scoring | No rooms built, scorer returns garbage (Pitfall 3, Case A) | Return nullable, state machine handles null by staying in Walking |
| Utility scoring | Housing rooms scored for visits (Pitfall 3, Case C) | Filter housing from visit candidates before scoring |
| State machine | Timer replacement race condition (Pitfall 1) | Delete old timers atomically, extract state machine as POCO |
| State machine | Room demolished while citizen visiting (Pitfall 5) | State machine OnRoomDemolished checks visit target, not just home |
| State machine | _isVisiting / _isAtHome flags conflict with state enum (Pitfall 1) | Replace all boolean flags with single state enum, no co-existence |
| Schedule templates | Config matrix too complex to tune (Pitfall 13) | 2-3 templates with trait modifiers, not per-trait-per-period configs |
| Save format v4 | v3 saves crash on load (Pitfall 2) | Version-gated restore, nullable new fields, suppress autosave during migration |
| New GameEvents | Missing ClearAllSubscribers entry (Pitfall 7) | Checklist for every new event, reflection-based test to verify |
| Test infrastructure | New singleton not in ResetAllSingletons (Pitfall 14) | Update TestHelper in same commit as singleton creation |
| Clock + Decision timer | Schedule bias not visible due to timing mismatch (Pitfall 8) | Calibrate together: 4-6 decisions per period |

## Sources

- Direct codebase analysis of CitizenNode.cs (1107 lines), CitizenManager.cs, SaveManager.cs, HousingManager.cs, HappinessManager.cs, EconomyManager.cs, GameEvents.cs, RoomVisual.cs, RingVisual.cs, MoodSystem.cs, CitizenData.cs, SaveData/SavedCitizen/SavedRoom POCOs, TestHelper.cs, GameTestClass.cs, SaveDataTests.cs
- [Utility AI Tutorial for Godot 4 (Minoqi)](https://minoqi.vercel.app/posts/godot-4-tutorials/utility-ai-godot-4-tutorial/) -- score normalization requirement, tie-breaking omission, missing availability handling
- [Game Programming Patterns: State](https://gameprogrammingpatterns.com/state.html) -- state machine interrupt handling, concurrent state transitions
- [AI Decision-Making with Utility Scores (Forty Years of Code)](https://mcguirev10.com/2019/01/03/ai-decision-making-with-utility-scores-part-1.html) -- tie-breaking mechanisms, secondary criteria
- [An Introduction to Utility Theory (Game AI Pro)](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter09_An_Introduction_to_Utility_Theory.pdf) -- normalization across different-scale inputs
- [Standard Material 3D Documentation (Godot)](https://docs.godotengine.org/en/stable/tutorials/3d/standard_material_3d.html) -- emission, transparency, render priority
- [Godot Forum: Emission Material Glow](https://godotforums.org/d/36272-emission-material-doesnt-have-glow-around-it) -- WorldEnvironment glow threshold interaction
- [Godot Tween Documentation](https://docs.godotengine.org/en/stable/classes/class_tween.html) -- kill() behavior, fire-and-forget lifecycle
- [System.Text.Json Nullable Annotations (Microsoft)](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/nullable-annotations) -- missing field default behavior
- [Godot Forum: State Machine Best Practice](https://forum.godotengine.org/t/state-machine-best-practice/108704) -- POCO vs Node-based state machines

---
*Pitfalls research for: Orbital Rings v1.4 Citizen AI & Day/Night Cycle*
*Researched: 2026-03-07*
