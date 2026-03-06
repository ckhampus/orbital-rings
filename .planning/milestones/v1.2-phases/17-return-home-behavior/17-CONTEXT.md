# Phase 17: Return-Home Behavior - Context

**Gathered:** 2026-03-06
**Status:** Ready for planning

<domain>
## Phase Boundary

Citizens visibly return to their home room on a periodic cycle, rest with a sleeping indicator, and resume normal behavior without disrupting the wish loop. This is the first visible housing behavior — Phases 14-16 were infrastructure only.

Requirements: BEHV-01, BEHV-02, BEHV-03, BEHV-04.

</domain>

<decisions>
## Implementation Decisions

### Zzz Indicator
- Label3D text "Zzz" with billboard mode (follows camera like _wishBadge)
- Soft blue/purple color — distinct from wish badge colors, sleep-associated
- Gentle vertical bob animation while resting (reuse citizen's bob pattern at slower frequency)
- Fade in over ~0.5s when rest begins, fade out over ~0.5s when rest ends
- Positioned above the room segment (not the citizen's last walkway position) while citizen is inside

### Home-Visit Visual Behavior
- Same visual pattern as regular room visits: walk to segment, radial drift, fade out, rest inside (invisible), emerge
- Reuse the existing 8-phase tween structure from StartVisit — home return is a variant, not a new system
- Citizen walks the ring naturally to reach their home segment (same angular-walk as regular visits)
- Wish badge hides during entire home rest cycle (avoids Zzz + wish badge stacking)

### Timer Interactions
- Home timer pauses while citizen is doing a regular room visit (_isVisiting blocks it)
- Visit timer pauses during home rest (_isAtHome blocks it)
- Wish timer pauses only during the rest-inside phase (not during walk-to-home)
- Home timer re-arms with fresh random interval after the citizen emerges back onto the walkway

### Priority & Interruption
- Active wish at home-timer fire → skip home return entirely, re-arm timer with fresh interval
- New wish appears during walk-to-home → abort home-walk tween, resume normal behavior, re-arm timer
- Home demolished during rest → immediately eject citizen (fade back in, resume walking), Zzz disappears
- Unhoused citizens have no home behavior at all (no home timer, no Zzz) — consistent with HOME-05

### Claude's Discretion
- Exact bob frequency and amplitude for Zzz animation
- Label3D font size and outline settings
- Whether to add CitizenEnteredHome/CitizenExitedHome events to GameEvents (or reuse existing room events)
- Internal implementation of tween interruption for wish-during-walk-home
- How HousingConfig timing values are passed to CitizenNode (Initialize parameter, setter, or HousingManager query)

</decisions>

<specifics>
## Specific Ideas

- Home rest should feel cozy — the Zzz is a gentle visual cue, not an alert
- The bob animation on Zzz should be slower/calmer than the citizen's walking bob
- When multiple citizens rest simultaneously, the ring should look peaceful with scattered Zzz indicators

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- CitizenNode._wishBadge (Sprite3D billboard): Pattern for positioning 3D indicators above citizens — Label3D will follow same approach
- CitizenNode.StartVisit() 8-phase tween: Walk to segment, radial drift, fade, wait, emerge — home return reuses this exact structure
- CitizenNode.SetMeshTransparencyMode/SetMeshAlpha (internal): Reusable for home-visit fade in/out
- CitizenNode._visitTimer pattern: One-shot timer with random re-arm — template for _homeTimer
- HousingConfig: HomeTimerMin/Max (90-150s), RestDurationMin/Max (8-15s) already defined with [Export]
- CitizenNode.HomeSegmentIndex: Already populated by HousingManager, O(1) read

### Established Patterns
- Boolean guard pattern: _isVisiting blocks _Process walking and OnVisitTimerTimeout — _isAtHome will follow same pattern
- Tween lifecycle: _activeTween with kill-before-create pattern for interruption
- Timer creation: Timer node added as child in Initialize(), started in _Ready(), one-shot with random interval
- Event-driven: Subscribe in SubscribeEvents/_EnterTree, unsubscribe in UnsubscribeEvents/_ExitTree

### Integration Points
- CitizenNode: New _homeTimer, _isAtHome boolean, _zzzLabel (Label3D), StartHomeReturn() method
- CitizenManager: Pass HousingConfig to CitizenNode in SpawnCitizen/SpawnCitizenFromSave
- CitizenManager._Process: Extend auto-deselect check to cover IsAtHome alongside IsVisiting
- GameEvents: Potentially new CitizenEnteredHome/CitizenExitedHome events
- HousingManager.OnRoomDemolished: Need to notify resting citizens to eject (or CitizenNode listens to RoomDemolished)

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 17-return-home-behavior*
*Context gathered: 2026-03-06*
