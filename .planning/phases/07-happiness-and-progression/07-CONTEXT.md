# Phase 7: Happiness and Progression - Context

**Gathered:** 2026-03-03
**Status:** Ready for planning

<domain>
## Phase Boundary

Station happiness is tracked visibly, fulfilling wishes raises it, happiness gates citizen arrivals and unlocks new room blueprints at observable milestones. This phase introduces: a HappinessManager Autoload, a happiness bar in the HUD, population-gated citizen arrivals, and blueprint unlock moments. Save/load, ambient audio, and full HUD polish are Phase 8.

</domain>

<decisions>
## Implementation Decisions

### Happiness Display
- Simple horizontal fill bar + percentage label, placed next to the credit display in the top-right corner
- Layout: `credits | population count | happiness bar` all in one top-right cluster
- Population count shown as icon + number (e.g., citizen icon + "7")
- On wish fulfillment: bar smoothly tweens to new value (~0.5s), brief warm pulse/glow on the bar, small floating "+X%" text drifts up and fades
- Matches CreditHUD's flash-on-change feedback pattern

### Citizen Arrival Pacing
- Gradual chance-based arrivals: higher happiness = higher chance of a new citizen per time interval (~60 second check)
- Population cap tied to housing capacity (sum of BaseCapacity for all placed Housing-category rooms)
- No housing = no new arrivals, even at high happiness; housing gives Housing rooms a clear purpose
- 5 starter citizens remain from Phase 5 (initial spawn bypasses housing check)
- Arrival fanfare: new citizen capsule fades in on walkway, small floating text "Luna has arrived!" drifts up and fades (~2s), population counter ticks up

### Blueprint Unlock Moments
- 6 starter rooms available immediately: bunk_pod, air_recycler, workshop, reading_nook, storage_bay, garden_nook
- 4 rooms locked behind happiness milestones:
  - 25% happiness: sky_loft (Housing) + craft_lab (Work)
  - 60% happiness: star_lounge (Comfort) + comm_relay (Utility)
- Locked rooms are completely hidden from the build panel until unlocked (not greyed out)
- On unlock: centered floating text "New rooms available!" drifts up and fades (~3s), build panel category tabs with new rooms pulse/glow briefly
- Unlocked rooms are NOT more expensive than starter rooms (Phase 3 decision: progression gives variety, not cost escalation)

### Happiness Formula
- Happiness only goes up — no decay, no subtraction, no punishment (matches cozy promise)
- Diminishing returns: early wishes grant more, later wishes grant less (formula: gain = base / (1 + currentHappiness))
- All wish categories grant equal happiness — no min-maxing, every wish matters equally
- Happiness capped at 100% — at 100% the station is fully happy, all unlocks achieved, max economy multiplier (1.3x)
- Past 100%: wishes still generate and fulfill (badges still pop), but happiness stays at 100%

### Claude's Discretion
- Exact arrival check interval and probability formula (within the ~60s check pattern)
- Exact diminishing returns base value (calibrate so ~25 wishes reaches 100% in a 20-25 min session)
- Housing capacity values for bunk_pod and sky_loft (ensure reasonable population growth curve)
- Happiness bar visual styling (colors, dimensions, font, glow effect)
- Population count icon choice and styling
- Floating text animation specifics (drift speed, font size, fade timing)
- Build panel pulse/glow animation for unlocks
- HappinessManager Autoload architecture and timer implementation
- Where new citizens spawn on the walkway (random position vs near a specific room)

</decisions>

<specifics>
## Specific Ideas

- Housing as population gate gives the player a clear early goal: build bunk pods to grow the station
- Gradual chance-based arrivals feel organic and alive — citizens "wander in" rather than appearing on a schedule
- Hidden locked rooms make each unlock a genuine surprise — "oh, there's a Sky Loft now!"
- Diminishing returns prevent rushing to 100% while still making early progress feel fast and rewarding
- The "+X%" floating text on wish fulfillment creates a direct visible link between "I fulfilled a wish" and "happiness went up"
- 25% first unlock (~5-8 wishes) gives early positive reinforcement; 60% second unlock (~15-20 wishes) rewards sustained play

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- GameEvents.HappinessChanged(float) — event already stubbed, ready for HappinessManager to fire
- GameEvents.BlueprintUnlocked(string) — event already stubbed, ready for unlock system to fire
- GameEvents.CitizenArrived(string) — event already stubbed, already fired by CitizenManager.SpawnCitizen
- EconomyManager.SetHappiness(float) — already accepts 0.0-1.0 and applies multiplier to income (capped at 1.3x)
- EconomyManager.GetIncomeBreakdown() — already returns happinessMult component for HUD display
- CitizenManager with SpawnCitizen(), CitizenCount, and citizen list management
- WishBoard.OnWishFulfilled — already listens for wish fulfillment events
- CreditHUD — programmatic UI pattern with rolling counter, flash feedback, floating numbers (model for happiness bar)
- BuildPanel.LoadRoomDefinitions() — currently loads all 10 rooms from hardcoded RoomFiles array; needs to be filtered by unlock state

### Established Patterns
- Autoload singleton pattern (GameEvents.Instance, EconomyManager.Instance, BuildManager.Instance, CitizenManager.Instance, WishBoard.Instance) — HappinessManager follows same pattern
- Pure C# event delegates for cross-system communication
- Timer child node for periodic actions (income tick in EconomyManager, visit timer in CitizenNode) — arrival check timer follows same pattern
- Tween-based animations with kill-before-create pattern
- Programmatic UI (CreditHUD, BuildPanel, SegmentTooltip all built in code, no .tscn scenes)
- Per-instance StandardMaterial3D for independent visual control

### Integration Points
- HappinessManager registers as Autoload in project.godot
- HappinessManager listens to GameEvents.WishFulfilled to increment happiness
- HappinessManager fires GameEvents.HappinessChanged on every update
- HappinessManager calls EconomyManager.SetHappiness() to update income multiplier
- HappinessManager checks housing capacity via BuildManager placed rooms (Housing category)
- HappinessManager spawns citizens via CitizenManager when arrival chance succeeds
- HappinessManager fires GameEvents.BlueprintUnlocked when thresholds crossed
- BuildPanel subscribes to GameEvents.BlueprintUnlocked to refresh available rooms
- Happiness bar UI subscribes to GameEvents.HappinessChanged for display updates
- CreditHUD or a new HUD container hosts the happiness bar and population count

</code_context>

<deferred>
## Deferred Ideas

- Persistent wish board / notification panel (QOLX-01) — v2 requirement
- Full HUD wiring and polish pass — Phase 8
- Save/load persistence of happiness value — Phase 8

</deferred>

---

*Phase: 07-happiness-and-progression*
*Context gathered: 2026-03-03*
