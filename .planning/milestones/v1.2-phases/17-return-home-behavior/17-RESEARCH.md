# Phase 17: Return-Home Behavior - Research

**Researched:** 2026-03-06
**Domain:** Godot 4 citizen behavior system (C#), tween animation, Label3D indicators
**Confidence:** HIGH

## Summary

Phase 17 adds the first visible housing behavior: housed citizens periodically walk to their home room, rest inside with a "Zzz" Label3D indicator, then resume normal behavior. This builds entirely on existing patterns in CitizenNode -- the 8-phase tween visit sequence, timer-based scheduling, and boolean guard pattern. The implementation is a variant of the existing room visit system, not a new system.

The core challenge is managing three interacting timer systems (visit timer, wish timer, home timer) with proper mutual exclusion, plus the Zzz visual indicator lifecycle. All building blocks exist: the StartVisit tween structure provides the animation template, _visitTimer provides the timer pattern, _wishBadge provides the 3D indicator pattern, and HousingConfig already has the timing constants.

**Primary recommendation:** Implement as a StartHomeReturn() method in CitizenNode that reuses the StartVisit() tween structure with modifications: target segment comes from HomeSegmentIndex instead of proximity search, Zzz Label3D replaces wish badge visibility during rest phase, and wish timer pauses during the rest-inside interval.

<user_constraints>

## User Constraints (from CONTEXT.md)

### Locked Decisions
- **Zzz Indicator:** Label3D text "Zzz" with billboard mode, soft blue/purple color, gentle vertical bob animation (slower than walking bob), fade in/out over ~0.5s, positioned above the room segment while citizen is inside
- **Home-Visit Visual Behavior:** Same visual pattern as regular room visits (walk to segment, radial drift, fade out, rest inside invisible, emerge). Reuse existing 8-phase tween structure. Citizen walks ring naturally via angular-walk. Wish badge hides during entire home rest cycle
- **Timer Interactions:** Home timer pauses during _isVisiting. Visit timer pauses during _isAtHome. Wish timer pauses only during rest-inside phase (not walk-to-home). Home timer re-arms with fresh random interval after emerge
- **Priority & Interruption:** Active wish at home-timer fire -> skip, re-arm. New wish during walk-to-home -> abort, resume, re-arm. Home demolished during rest -> eject immediately. Unhoused citizens have no home behavior

### Claude's Discretion
- Exact bob frequency and amplitude for Zzz animation
- Label3D font size and outline settings
- Whether to add CitizenEnteredHome/CitizenExitedHome events to GameEvents (or reuse existing room events)
- Internal implementation of tween interruption for wish-during-walk-home
- How HousingConfig timing values are passed to CitizenNode (Initialize parameter, setter, or HousingManager query)

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope.

</user_constraints>

<phase_requirements>

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| BEHV-01 | Housed citizens periodically return to their home room (90-150s cycle) | _homeTimer pattern mirrors _visitTimer; HousingConfig.HomeTimerMin/Max already defined; HomeSegmentIndex already on CitizenNode |
| BEHV-02 | Home rest lasts 8-15s with a "Zzz" FloatingText indicator | Label3D with billboard mode; HousingConfig.RestDurationMin/Max already defined; positioned above room segment during rest phase |
| BEHV-03 | Citizen wish timer pauses during home rest | _wishTimer.Stop() at rest-inside start, _wishTimer.Start() at emerge; only during rest phase, not walk-to-home |
| BEHV-04 | Home return is lower priority than active wish fulfillment | Guard in OnHomeTimerTimeout: if _currentWish != null, skip and re-arm; abort walk-to-home tween if wish appears mid-walk |

</phase_requirements>

## Standard Stack

### Core
| Library/API | Version | Purpose | Why Standard |
|-------------|---------|---------|--------------|
| Godot.Label3D | 4.3+ | "Zzz" text indicator above resting citizen | Billboard mode, font_size, modulate, outline -- same pattern as Sprite3D _wishBadge but for text |
| Godot.Tween | 4.3+ | Multi-phase animation for home visit sequence | Already used by StartVisit(); kill-before-create pattern established |
| Godot.Timer | 4.3+ | _homeTimer for periodic home return scheduling | Same pattern as _visitTimer and _wishTimer already in CitizenNode |

### Supporting
| Library/API | Version | Purpose | When to Use |
|-------------|---------|---------|-------------|
| HousingConfig | Project | HomeTimerMin/Max, RestDurationMin/Max constants | All timing values come from this Inspector-tunable resource |
| HousingManager | Project | Home assignment data, FindCitizenNode, RoomDemolished event | Query for home segment, listen for demolish-eject |
| GameEvents | Project | Cross-system communication | Emit/subscribe for home enter/exit and demolish events |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Label3D for Zzz | FloatingText (2D Label) | STATE.md originally said FloatingText; CONTEXT.md locks Label3D -- 3D billboard is correct for world-space indicator above a room segment |
| New CitizenEnteredHome event | Reuse CitizenEnteredRoom | Separate events are cleaner -- home rest has different semantics (wish pause, Zzz display) and listeners should distinguish home visits from regular visits |

## Architecture Patterns

### Recommended Modification Points

```
Scripts/
  Citizens/
    CitizenNode.cs       # ADD: _homeTimer, _isAtHome, _zzzLabel, _housingConfig,
                         #      StartHomeReturn(), OnHomeTimerTimeout(),
                         #      CreateZzzLabel(), RemoveZzzLabel(),
                         #      AbortHomeReturn(), EjectFromHome()
                         # MODIFY: _Process (guard _isAtHome), OnVisitTimerTimeout (guard _isAtHome),
                         #          OnWishTimerTimeout (guard _isAtHome for generation),
                         #          SubscribeEvents/UnsubscribeEvents (RoomDemolished),
                         #          Initialize (accept HousingConfig), _Ready (start _homeTimer),
                         #          _ExitTree (cleanup _zzzLabel)
    CitizenManager.cs    # MODIFY: SpawnCitizen/SpawnCitizenFromSave (pass HousingConfig),
                         #          _Process (extend auto-deselect to cover IsAtHome)
  Autoloads/
    GameEvents.cs        # ADD: CitizenEnteredHome/CitizenExitedHome events
```

### Pattern 1: Home Timer (mirrors _visitTimer)
**What:** One-shot Timer that fires periodically with random interval from HousingConfig
**When to use:** Only for housed citizens (HomeSegmentIndex != null)
**Example:**
```csharp
// In Initialize(), after existing timer creation:
_homeTimer = new Timer
{
    Name = "HomeTimer",
    OneShot = true,
    WaitTime = _housingConfig.HomeTimerMin
              + GD.Randf() * (_housingConfig.HomeTimerMax - _housingConfig.HomeTimerMin)
};
_homeTimer.Timeout += OnHomeTimerTimeout;
AddChild(_homeTimer);
// Start deferred to _Ready() -- but ONLY if HomeSegmentIndex != null
```

### Pattern 2: Boolean Guard (_isAtHome mirrors _isVisiting)
**What:** Boolean flag that blocks incompatible behaviors during home rest
**When to use:** Guards in _Process, OnVisitTimerTimeout, and OnWishTimerTimeout
**Example:**
```csharp
public override void _Process(double delta)
{
    if (_isVisiting || _isAtHome) return;
    // ... walking logic
}

private void OnVisitTimerTimeout()
{
    if (_isVisiting || _isAtHome) return;
    // ... visit logic
}
```

### Pattern 3: StartHomeReturn() (variant of StartVisit)
**What:** Reuses the 8-phase tween structure but targets HomeSegmentIndex
**When to use:** When OnHomeTimerTimeout fires and guards pass
**Key differences from StartVisit:**
1. Target segment from HomeSegmentIndex instead of proximity search
2. No wish fulfillment check at end
3. Zzz Label3D created at rest-inside phase, removed at emerge
4. Wish timer paused during rest-inside, resumed at emerge
5. Wish badge hidden for entire sequence (not just during fade-out)
6. Uses _isAtHome flag instead of modifying _isVisiting

### Pattern 4: Zzz Label3D (mirrors _wishBadge Sprite3D)
**What:** Label3D with billboard mode showing "Zzz" above resting citizen
**When to use:** Created when citizen enters rest-inside phase, removed on emerge
**Example:**
```csharp
private void CreateZzzLabel()
{
    _zzzLabel = new Label3D
    {
        Text = "Zzz",
        Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
        FontSize = 32,
        OutlineSize = 4,
        Modulate = new Color(0.6f, 0.55f, 0.9f, 0.0f), // soft purple, start invisible
        OutlineModulate = new Color(0.3f, 0.25f, 0.5f, 0.0f),
        PixelSize = 0.005f,
        NoDepthTest = true,
        Shaded = false,
        DoubleSided = true,
        HorizontalAlignment = HorizontalAlignment.Center,
    };
    // Position above the room segment, not the citizen
    // (citizen is invisible during rest, Zzz floats above room)
    AddChild(_zzzLabel);
}
```

### Pattern 5: Tween Interruption for Wish-During-Walk
**What:** Kill active home tween, restore citizen to walking state, re-arm home timer
**When to use:** When _currentWish becomes non-null during walk-to-home phase
**Approach:** Track a _walkingToHome boolean (true during angular walk phase only, false during rest). When a wish nudge arrives or wish generates during walk, call AbortHomeReturn().
**Example:**
```csharp
private void AbortHomeReturn()
{
    _activeTween?.Kill();
    _activeTween = null;
    _isAtHome = false;
    _walkingToHome = false;

    // Restore visual state
    SetMeshAlpha(1.0f);
    SetMeshTransparencyMode(false);
    Visible = true;
    RemoveZzzLabel(); // safety

    // Re-arm home timer
    RearmHomeTimer();
}
```

### Anti-Patterns to Avoid
- **Sharing _isVisiting for home visits:** Home rest has different semantics (wish pause, Zzz indicator, different priority rules). Use separate _isAtHome boolean.
- **Starting home timer for unhoused citizens:** Guard timer start behind HomeSegmentIndex != null check. When citizen becomes unhoused (home demolished), stop home timer.
- **Modifying StartVisit() directly:** Home return should be a separate method (StartHomeReturn) that follows the same structure. Do not add if-branches inside StartVisit.
- **Putting Zzz Label3D on citizen node as child with local offset:** During rest, citizen is invisible (Visible=false). Label3D as child would also become invisible. Instead, position Zzz using world coordinates calculated from the home segment angle, or add it as a child but set its own Visible independently from the citizen's Visible flag. The cleanest approach: add Zzz to the citizen's parent (CitizenManager) with absolute positioning, or use TopLevel=true on the Label3D so it ignores parent visibility.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Timer scheduling | Manual _Process delta accumulation | Godot Timer node | Timer handles pause, tree lifecycle; already the pattern for visit/wish timers |
| Angular walk animation | Manual per-frame angle interpolation | Tween with TweenMethod | Established pattern from StartVisit; handles interruption via Kill() |
| Billboard text | Custom shader or _Process camera-facing | Label3D.Billboard = Enabled | Built-in billboard mode; same approach as Sprite3D _wishBadge |
| Fade animation | Manual alpha tracking in _Process | Tween TweenProperty on Modulate.A | Cleaner, interruptible, follows established tween pattern |

**Key insight:** Every piece of infrastructure needed for home-return behavior already exists in CitizenNode. The implementation is composition of existing patterns, not invention of new ones.

## Common Pitfalls

### Pitfall 1: Label3D Invisible as Child of Hidden Node
**What goes wrong:** When citizen enters room (Visible = false), the Zzz Label3D also becomes invisible because child nodes inherit parent visibility.
**Why it happens:** Godot's visibility is hierarchical -- setting parent.Visible = false hides all children.
**How to avoid:** Either (a) set TopLevel = true on the Label3D so it renders independently, or (b) add the Label3D to a different parent (e.g., CitizenManager or a dedicated container). Option (a) is simpler -- just set Position in global coordinates.
**Warning signs:** Zzz never appears during testing despite rest phase running.

### Pitfall 2: Timer Running for Unhoused Citizens
**What goes wrong:** Unhoused citizens fire OnHomeTimerTimeout, try to access HomeSegmentIndex (null), crash or silently fail.
**Why it happens:** Home timer started unconditionally in _Ready().
**How to avoid:** Only create and start _homeTimer when HomeSegmentIndex has a value. When citizen becomes unhoused (CitizenUnhoused event or home demolished), stop and remove the timer. When citizen gains a home (CitizenAssignedHome event), create and start the timer.
**Warning signs:** Null reference exceptions from OnHomeTimerTimeout; unhoused citizens pausing unexpectedly.

### Pitfall 3: Wish Badge + Zzz Label Stacking
**What goes wrong:** Both wish badge and Zzz indicator appear simultaneously, creating visual clutter.
**Why it happens:** Wish badge wasn't explicitly hidden during home return sequence.
**How to avoid:** Hide wish badge at the START of StartHomeReturn() (before walk phase), restore at end. The CONTEXT.md locks this: "Wish badge hides during entire home rest cycle."
**Warning signs:** Two overlapping indicators visible during home return.

### Pitfall 4: Home Timer Drift After Skip
**What goes wrong:** When home timer fires but is skipped (active wish), if the timer is simply restarted without resetting WaitTime, the next trigger uses the old interval.
**Why it happens:** Godot Timer WaitTime persists between Start() calls.
**How to avoid:** Always set a fresh random WaitTime before calling Start() when re-arming.
**Warning signs:** Citizens returning home at suspiciously regular intervals.

### Pitfall 5: Stale HomeSegmentIndex After Demolish
**What goes wrong:** Home demolished while citizen is walking to it (not yet resting). Tween continues to the now-empty segment.
**Why it happens:** HomeSegmentIndex set to null by HousingManager.OnRoomDemolished, but walk tween captured the old angle.
**How to avoid:** Listen to RoomDemolished in CitizenNode. If _isAtHome and the demolished segment matches home, call EjectFromHome(). If _walkingToHome, call AbortHomeReturn().
**Warning signs:** Citizen fading out at location where room no longer exists.

### Pitfall 6: Three-Way Timer Deadlock
**What goes wrong:** All three timers pause each other in a cycle: _isVisiting blocks home timer, _isAtHome blocks visit timer, but edge case where both flags set simultaneously.
**Why it happens:** Incorrect flag management during state transitions.
**How to avoid:** Make state transitions atomic: clear _isAtHome before setting _isVisiting (and vice versa). Never have both flags true simultaneously.
**Warning signs:** Citizen permanently stuck in one state.

### Pitfall 7: Outline Alpha Mismatch on Label3D
**What goes wrong:** When fading the Zzz label, the outline and text fade at different rates, creating an ugly look.
**Why it happens:** Modulate and OutlineModulate are separate properties; tweening only Modulate leaves outline fully visible.
**How to avoid:** Tween both Modulate.A and OutlineModulate.A in parallel (SetParallel) to the same values.
**Warning signs:** Visible outline halo remaining after text has faded out.

## Code Examples

### Home Timer Timeout Handler
```csharp
// Follows established OnVisitTimerTimeout pattern
private void OnHomeTimerTimeout()
{
    // Guard: don't go home while visiting or already at home
    if (_isVisiting || _isAtHome) return;

    // Guard: must have a home (should not happen if timer management is correct)
    if (HomeSegmentIndex == null) return;

    // Guard: active wish takes priority (BEHV-04)
    if (_currentWish != null)
    {
        RearmHomeTimer();
        return;
    }

    StartHomeReturn();
}

private void RearmHomeTimer()
{
    if (_homeTimer == null || _housingConfig == null) return;
    _homeTimer.WaitTime = _housingConfig.HomeTimerMin
                        + GD.Randf() * (_housingConfig.HomeTimerMax - _housingConfig.HomeTimerMin);
    _homeTimer.Start();
}
```

### Zzz Bob Animation (during rest-inside phase)
```csharp
// Within the rest-inside tween interval, a parallel tween bobs the Zzz label
// Bob frequency: ~2.0 rad/s (slower/calmer than citizen's 8.0 walking bob)
// Bob amplitude: ~0.03 units (gentle float)
private Tween _zzzBobTween;

private void StartZzzBob()
{
    if (_zzzLabel == null) return;
    _zzzBobTween?.Kill();

    var baseY = _zzzLabel.Position.Y;
    _zzzBobTween = CreateTween();
    _zzzBobTween.SetLoops(); // infinite loop
    _zzzBobTween.TweenProperty(_zzzLabel, "position:y", baseY + 0.03f, 1.5f)
        .SetEase(Tween.EaseType.InOut)
        .SetTrans(Tween.TransitionType.Sine);
    _zzzBobTween.TweenProperty(_zzzLabel, "position:y", baseY, 1.5f)
        .SetEase(Tween.EaseType.InOut)
        .SetTrans(Tween.TransitionType.Sine);
}
```

### Eject From Home on Room Demolish
```csharp
// Called when RoomDemolished event fires for citizen's home segment
private void EjectFromHome()
{
    // Kill any active home tween
    _activeTween?.Kill();
    _activeTween = null;

    // Stop wish timer pause (if was paused during rest)
    _wishTimer?.Start();

    // Clean up Zzz
    _zzzBobTween?.Kill();
    _zzzBobTween = null;
    RemoveZzzLabel();

    // Restore citizen visibility and state
    Visible = true;
    SetMeshAlpha(1.0f);
    SetMeshTransparencyMode(false);
    _isAtHome = false;
    _walkingToHome = false;

    // Restore wish badge if has active wish
    if (_currentWish != null && _wishBadge != null)
        _wishBadge.Visible = true;

    // Don't re-arm home timer -- citizen is now unhoused
    // (HousingManager already set HomeSegmentIndex = null)
    _homeTimer?.Stop();
}
```

### Passing HousingConfig to CitizenNode
```csharp
// Recommendation: Add HousingConfig parameter to Initialize()
// This follows the existing pattern of passing SegmentGrid reference

// In CitizenNode:
public void Initialize(CitizenData data, float startAngle, SegmentGrid grid,
                       HousingConfig housingConfig = null)
{
    // ... existing init code ...
    _housingConfig = housingConfig;

    // Home timer created but NOT started here
    // Started in _Ready() only if HomeSegmentIndex != null
    if (_housingConfig != null)
    {
        _homeTimer = new Timer
        {
            Name = "HomeTimer",
            OneShot = true,
            WaitTime = _housingConfig.HomeTimerMin
                      + GD.Randf() * (_housingConfig.HomeTimerMax - _housingConfig.HomeTimerMin)
        };
        _homeTimer.Timeout += OnHomeTimerTimeout;
        AddChild(_homeTimer);
    }
}

// In _Ready():
public override void _Ready()
{
    _visitTimer?.Start();
    _wishTimer?.Start();
    // Only start home timer if citizen has a home
    if (HomeSegmentIndex != null)
        _homeTimer?.Start();
}

// In CitizenManager.SpawnCitizen():
citizen.Initialize(data, angle, _grid, HousingManager.Instance?.Config);
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| FloatingText (2D Label) for Zzz | Label3D with billboard | Phase 17 CONTEXT.md | 3D world-space indicator that follows camera, consistent with Sprite3D pattern |
| Single _isVisiting flag | Separate _isVisiting + _isAtHome | Phase 17 | Clean separation of visit and home-rest states with distinct timer interactions |

**STATE.md vs CONTEXT.md note:** STATE.md records "Zzz visual reuses FloatingText" but CONTEXT.md (newer, from discussion session) locks the decision as Label3D. CONTEXT.md takes precedence.

## Open Questions

1. **TopLevel vs Reparenting for Zzz Visibility**
   - What we know: Child nodes inherit parent Visible=false. Zzz must be visible while citizen is invisible.
   - What's unclear: Whether TopLevel=true on Label3D works cleanly with position updates, or if reparenting to CitizenManager is cleaner.
   - Recommendation: Use TopLevel=true and set GlobalPosition. Simpler than reparenting, and Label3D position only needs to be set once (at the home segment location) since the citizen is stationary during rest.

2. **Home Timer Start/Stop on Assignment Changes**
   - What we know: CitizenAssignedHome and CitizenUnhoused events exist.
   - What's unclear: Whether CitizenNode should subscribe to these events, or if the timer should be managed externally.
   - Recommendation: CitizenNode subscribes in SubscribeEvents() to CitizenAssignedHome and CitizenUnhoused. On assigned: create/start timer if not exists. On unhoused: stop timer. This keeps behavior self-contained.

3. **Wish Appearance During Walk-to-Home Detection**
   - What we know: OnWishTimerTimeout generates wishes. If a wish generates during walk-to-home, we need to abort.
   - What's unclear: Best detection mechanism -- check _walkingToHome in OnWishTimerTimeout, or have a WishGenerated event handler.
   - Recommendation: In OnWishTimerTimeout, after generating the wish, check if _walkingToHome is true and call AbortHomeReturn(). This is the simplest approach since the wish timer fires on the same citizen that would need to abort.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None -- Godot C# project without automated test infrastructure |
| Config file | None |
| Quick run command | Manual: run game in Godot editor, observe citizen behavior |
| Full suite command | Manual: run game, build housing rooms, wait 90-150s for home returns |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| BEHV-01 | Citizens periodically return to home room | manual-only | Run game, place housing, wait ~90-150s, observe citizen walks to home segment | N/A |
| BEHV-02 | Zzz indicator appears during 8-15s rest | manual-only | Observe "Zzz" Label3D above room segment during home rest, time the duration | N/A |
| BEHV-03 | Wish timer pauses during home rest | manual-only | Observe that no new wish appears during home rest (requires long observation or reduced timers) | N/A |
| BEHV-04 | Active wish defers home return | manual-only | Give citizen a wish, wait for home timer, verify citizen skips home return | N/A |

**Manual-only justification:** This is a Godot game project with visual behavior testing. Behaviors involve tween animations, 3D rendering, and timer interactions that require the game runtime. No automated test framework is configured.

### Sampling Rate
- **Per task commit:** Run game in editor, verify specific behavior added by task
- **Per wave merge:** Full playthrough: spawn citizens, build housing, observe home returns, verify Zzz, test demolish eject
- **Phase gate:** All four BEHV requirements manually verified with HousingConfig set to shorter intervals for faster iteration

### Wave 0 Gaps
- No automated test infrastructure exists and none is needed for this Godot project
- Manual testing with reduced HousingConfig timing values (e.g., HomeTimerMin=10, RestDurationMin=3) recommended for faster iteration during development

## Sources

### Primary (HIGH confidence)
- `/workspace/Scripts/Citizens/CitizenNode.cs` -- Full StartVisit 8-phase tween, timer patterns, wish badge, boolean guards
- `/workspace/Scripts/Data/HousingConfig.cs` -- HomeTimerMin/Max, RestDurationMin/Max with [Export]
- `/workspace/Scripts/Autoloads/HousingManager.cs` -- Home assignment, OnRoomDemolished, FindCitizenNode
- `/workspace/Scripts/Autoloads/GameEvents.cs` -- Event pattern, existing CitizenEnteredRoom/ExitedRoom
- `/workspace/Scripts/Citizens/CitizenManager.cs` -- SpawnCitizen, SpawnCitizenFromSave, auto-deselect
- [Label3D Godot 4.3 Docs](https://rokojori.com/en/labs/godot/docs/4.3/label3d-class) -- Properties: Billboard, FontSize, Modulate, OutlineModulate, PixelSize
- [Label3D Godot 4.4 Official Docs](https://docs.godotengine.org/en/4.4/classes/class_label3d.html) -- Official reference

### Secondary (MEDIUM confidence)
- [Godot Tween C# API](https://straydragon.github.io/godot-csharp-api-doc/4.3-stable/main/Godot.Tween.html) -- Kill(), SetLoops(), TweenProperty
- [Label3D Billboard Issue #92379](https://github.com/godotengine/godot/issues/92379) -- Known issue: billboard doesn't work with material_override (avoid using material_override on Label3D)

### Tertiary (LOW confidence)
- None -- all findings verified against codebase and official docs

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- all APIs already used in codebase (Tween, Timer, Sprite3D billboard); Label3D follows identical pattern
- Architecture: HIGH -- direct extension of established CitizenNode patterns (StartVisit, _visitTimer, _wishBadge)
- Pitfalls: HIGH -- pitfalls derived from analyzing actual code structure and Godot visibility hierarchy behavior

**Research date:** 2026-03-06
**Valid until:** 2026-04-06 (stable -- Godot 4.x APIs, existing codebase patterns)
