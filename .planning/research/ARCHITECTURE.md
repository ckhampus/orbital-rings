# Architecture Research

**Domain:** Citizen AI integration (traits, utility scoring, day/night cycle, state machine) for Godot 4 C# cozy space station builder
**Researched:** 2026-03-07
**Confidence:** HIGH -- derived from direct codebase inspection of all 8 autoload singletons, CitizenNode behavior, event bus topology, and save format. Utility AI patterns verified against Game AI Pro references and established game development literature.

---

## Executive Summary

The v1.4 milestone adds four interlocking systems -- StationClock, citizen traits, utility scoring, and a citizen state machine -- on top of 8 existing autoload singletons coordinated through a typed C# event bus. The architectural challenge is that CitizenNode currently owns three independent timers (visit, wish, home) with boolean guards (`_isVisiting`, `_isAtHome`) acting as a flat state machine. The v1.4 state machine must replace this implicit state with an explicit enum-driven state machine while preserving the proven tween-based animation sequences.

The recommended architecture introduces one new autoload singleton (StationClock, bringing the total to 9), three new POCO/data types (CitizenTrait, ScheduleTemplate, UtilityScorer), one new scene node (StationLighting), and significant internal refactoring of CitizenNode. The key integration insight is that StationClock drives period transitions through GameEvents (a new `PeriodChanged` event), which CitizenNode's state machine consumes to trigger schedule evaluation. The utility scorer replaces the current proximity-only room selection with a weighted multi-factor decision that incorporates traits, recency, proximity, and wish bonuses.

Save format advances to v4 with two new fields on SavedCitizen (InterestTrait, RhythmTrait) and two new fields on SaveData (ClockElapsed, ClockPeriod). The existing version-gating pattern (v1/v2/v3) extends cleanly to v4 with null/default fallbacks for older saves.

---

## System Overview

### Current Architecture (v1.3)

```
+---------------------------------------------------------------+
|                     Autoload Singletons (8)                    |
|                                                                |
|  GameEvents ---- EconomyManager ---- BuildManager              |
|       |               |                   |                    |
|  WishBoard       HappinessManager    CitizenManager            |
|       |               |                   |                    |
|  SaveManager     HousingManager      (owns CitizenNodes)       |
+---------------------------------------------------------------+
|                                                                |
|  CitizenNode (per citizen)                                     |
|  - _visitTimer (20-40s) --> OnVisitTimerTimeout --> StartVisit  |
|  - _wishTimer (30-60s) --> OnWishTimerTimeout --> GenerateWish  |
|  - _homeTimer (90-150s) --> OnHomeTimerTimeout --> StartHomeReturn |
|  - _isVisiting / _isAtHome boolean guards                      |
|  - Room selection: nearest occupied segment within 1.5 arcs    |
+---------------------------------------------------------------+
```

### Target Architecture (v1.4)

```
+---------------------------------------------------------------+
|                   Autoload Singletons (9)                      |
|                                                                |
|  GameEvents ---+--- EconomyManager --- BuildManager            |
|       |        |         |                 |                   |
|  WishBoard     |    HappinessManager  CitizenManager           |
|       |        |         |                 |                   |
|  SaveManager   |    HousingManager    (owns CitizenNodes)      |
|                |                                               |
|           StationClock  <-- NEW (9th singleton)                |
|           (owns time, emits PeriodChanged)                     |
+---------------------------------------------------------------+
|                                                                |
|  Scene Layer                                                   |
|  +------------------+     +---------------------------+        |
|  | StationLighting  |     | CitizenNode (refactored)  |        |
|  | (Node3D child    |     | - CitizenStateMachine     |        |
|  |  of game scene)  |     | - UtilityScorer (POCO)    |        |
|  | - DirectionalLight|    | - CitizenSchedule (POCO)  |        |
|  | - env transitions|     | - CitizenTraits (data)    |        |
|  +------------------+     +---------------------------+        |
|                                                                |
|  Data Layer                                                    |
|  +---------------+  +----------------+  +-----------------+    |
|  | CitizenData   |  | ScheduleConfig |  | StationClockConfig| |
|  | + Interest    |  | (Resource)     |  | (Resource)       |  |
|  | + Rhythm      |  | period weights |  | period durations |  |
|  +---------------+  +----------------+  +-----------------+    |
+---------------------------------------------------------------+
```

### Component Responsibilities

| Component | Responsibility | Type | New/Modified |
|-----------|----------------|------|--------------|
| **StationClock** | Owns elapsed time, current period (Morning/Day/Evening/Night), emits PeriodChanged. Ticks in _Process. | Autoload singleton | NEW |
| **StationClockConfig** | Inspector-tunable period durations, total day length | Godot Resource | NEW |
| **StationLighting** | Responds to PeriodChanged, tweens DirectionalLight energy/color, env fog, room window emissives | Scene Node3D | NEW |
| **CitizenData** | Extended with Interest and Rhythm trait enums | Godot Resource | MODIFIED |
| **CitizenNode** | Refactored: explicit state machine replaces boolean guards, decision timer replaces visit+home timers | Scene Node3D | MODIFIED (major) |
| **CitizenStateMachine** | Enum-driven state (Walking/Evaluating/Visiting/Resting), manages transitions, owns decision timer | POCO (inner class or separate) | NEW |
| **UtilityScorer** | Scores each room for a given citizen: trait affinity + proximity + recency + wish bonus. Pure function. | POCO (static class) | NEW |
| **CitizenSchedule** | Maps (period, rhythm) to activity probability weights (visit, rest, wander). Consults ScheduleConfig. | POCO | NEW |
| **ScheduleConfig** | Resource defining per-period activity weights for each Rhythm type | Godot Resource | NEW |
| **GameEvents** | Extended with PeriodChanged, CitizenStateChanged events | Autoload singleton | MODIFIED |
| **SaveManager** | Extended for v4 format: clock state, citizen traits | Autoload singleton | MODIFIED |
| **SaveData** | Extended with ClockElapsed, ClockPeriod fields | POCO | MODIFIED |
| **SavedCitizen** | Extended with InterestTrait, RhythmTrait fields | POCO | MODIFIED |
| **ClockHUD** | Sun/moon icon in HUD corner showing current period | UI Control | NEW |
| **CitizenInfoPanel** | Extended to show trait icons | UI Control | MODIFIED |

---

## Recommended Project Structure

### New Files

```
Scripts/
+-- Autoloads/
|   +-- StationClock.cs              # NEW: 9th autoload singleton
+-- Clock/
|   +-- StationLighting.cs           # NEW: day/night visual transitions
|   +-- ClockHUD.cs                  # NEW: period indicator in HUD
+-- Citizens/
|   +-- CitizenStateMachine.cs       # NEW: enum state machine for citizen behavior
|   +-- UtilityScorer.cs             # NEW: room scoring pure functions
|   +-- CitizenSchedule.cs           # NEW: period-weighted activity selection
|   +-- CitizenNode.cs               # MODIFIED: integrate state machine
+-- Data/
|   +-- CitizenData.cs               # MODIFIED: add Interest + Rhythm enums
|   +-- StationClockConfig.cs        # NEW: period duration config
|   +-- ScheduleConfig.cs            # NEW: activity weight tables
+-- UI/
|   +-- CitizenInfoPanel.cs          # MODIFIED: show traits
```

### Structure Rationale

- **Scripts/Clock/:** New folder for clock-related scene nodes. StationLighting is a visual node (not an autoload) because it manages DirectionalLight3D children that belong in the scene tree. ClockHUD is a UI node.
- **Scripts/Citizens/:** UtilityScorer and CitizenSchedule live alongside CitizenNode because they are citizen-domain logic. CitizenStateMachine is tightly coupled to CitizenNode's lifecycle.
- **Scripts/Data/:** Config resources follow the established pattern (HappinessConfig, EconomyConfig, HousingConfig) -- inspector-tunable, `new()` constructors for testability.
- **StationClock as Autoload:** Must be an autoload because multiple consumers need it (CitizenNode via schedule evaluation, StationLighting via period transitions, ClockHUD via display, SaveManager via save/load). Autoload guarantees it initializes before all scene nodes.

---

## Architectural Patterns

### Pattern 1: Enum State Machine Replacing Boolean Guards

**What:** CitizenNode currently uses `_isVisiting` and `_isAtHome` booleans with cascading guard checks in `_Process`, `OnVisitTimerTimeout`, and `OnHomeTimerTimeout`. This implicit state machine has 4 effective states but no single point of truth. Replace with an explicit `CitizenState` enum and a transition method that enforces valid transitions.

**When to use:** When CitizenNode's behavior adds a new state dimension (Evaluating) and the boolean guards would require a third flag creating 8 theoretical combinations.

**Trade-offs:** More code upfront, but eliminates the "what happens if _isVisiting AND _isAtHome are both true?" class of bugs. The existing tween-based animations remain unchanged -- only the entry/exit guards change.

**Implementation:**

```csharp
public enum CitizenState
{
    Walking,      // Moving along walkway, can be interrupted
    Evaluating,   // Running utility scoring, picking next action
    Visiting,     // Tween sequence: walk to room, fade, wait, fade back
    Resting       // Tween sequence: walk home, fade, sleep, fade back
}

// Inside CitizenNode:
private CitizenState _state = CitizenState.Walking;

public CitizenState State => _state;

private bool TryTransition(CitizenState newState)
{
    // Valid transitions:
    // Walking -> Evaluating (decision timer fires)
    // Evaluating -> Walking (no good action found)
    // Evaluating -> Visiting (room selected)
    // Evaluating -> Resting (schedule says rest)
    // Visiting -> Walking (visit complete)
    // Resting -> Walking (rest complete)
    bool valid = (_state, newState) switch
    {
        (CitizenState.Walking, CitizenState.Evaluating) => true,
        (CitizenState.Evaluating, CitizenState.Walking) => true,
        (CitizenState.Evaluating, CitizenState.Visiting) => true,
        (CitizenState.Evaluating, CitizenState.Resting) => true,
        (CitizenState.Visiting, CitizenState.Walking) => true,
        (CitizenState.Resting, CitizenState.Walking) => true,
        _ => false
    };

    if (!valid) return false;
    _state = newState;
    return true;
}

public override void _Process(double delta)
{
    if (_state != CitizenState.Walking) return;
    // ... walking movement (unchanged) ...
}
```

**Migration from current code:** The existing `_isVisiting` boolean becomes `_state == CitizenState.Visiting`. The existing `_isAtHome` becomes `_state == CitizenState.Resting`. The three separate timers (visit, wish, home) collapse into a single decision timer that fires periodically and transitions to Evaluating state.

### Pattern 2: Decision Timer Replacing Three Timers

**What:** Currently CitizenNode has three independent timers: `_visitTimer` (20-40s), `_homeTimer` (90-150s), and `_wishTimer` (30-60s). The visit and home timers both trigger room-selection behavior with different targets. Replace visit and home timers with a single `_decisionTimer` (15-30s) that transitions to Evaluating state, where the schedule and utility scorer determine whether to visit a room or go home. The wish timer remains separate because wish generation is orthogonal to movement decisions.

**When to use:** When the number of possible actions grows beyond two (visit room, go home) to include period-aware behavior where the same timer should route to different actions based on schedule.

**Trade-offs:** The decision timer fires more frequently than the old visit timer, but the Evaluating state is computationally cheap (score a handful of rooms + one rest check). The benefit is unified decision-making that naturally incorporates time-of-day, traits, and recency.

**Implementation:**

```csharp
// Replace _visitTimer + _homeTimer with:
private Timer _decisionTimer;

private void OnDecisionTimerTimeout()
{
    if (_state != CitizenState.Walking) return;
    TryTransition(CitizenState.Evaluating);
    EvaluateNextAction();
}

private void EvaluateNextAction()
{
    var period = StationClock.Instance?.CurrentPeriod ?? StationPeriod.Day;
    var schedule = CitizenSchedule.GetActivityWeights(period, _data.Rhythm);

    // Roll weighted random: visit room vs rest vs continue walking
    float roll = GD.Randf();
    if (roll < schedule.RestWeight && HomeSegmentIndex != null)
    {
        TryTransition(CitizenState.Resting);
        StartHomeReturn();  // existing tween sequence
    }
    else if (roll < schedule.RestWeight + schedule.VisitWeight)
    {
        int bestRoom = UtilityScorer.SelectRoom(this, _grid);
        if (bestRoom >= 0)
        {
            TryTransition(CitizenState.Visiting);
            StartVisit(bestRoom);  // existing tween sequence
        }
        else
        {
            TryTransition(CitizenState.Walking);  // nothing to visit
        }
    }
    else
    {
        TryTransition(CitizenState.Walking);  // continue walking
    }

    // Re-arm decision timer
    _decisionTimer.WaitTime = DecisionTimerMin
        + GD.Randf() * (DecisionTimerMax - DecisionTimerMin);
    _decisionTimer.Start();
}
```

### Pattern 3: Pure Function Utility Scoring

**What:** Room selection moves from "nearest occupied segment with wish-matching distance reduction" to a multi-factor utility score computed as a pure function. Each factor (trait affinity, proximity, recency, wish bonus) produces a normalized 0-1 score, then they are combined with weighted multiplication.

**When to use:** Use utility scoring because it is the established pattern for cozy/simulation games where NPCs should exhibit preference-based behavior without hard rules. It naturally handles the "all else being equal, prefer variety" principle through recency decay.

**Trade-offs:** More parameters to tune than the current distance-only approach. Mitigated by using a ScheduleConfig Godot Resource with Inspector-exposed weights.

**Implementation:**

```csharp
// Scripts/Citizens/UtilityScorer.cs
public static class UtilityScorer
{
    /// <summary>
    /// Scores all occupied rooms for a citizen and returns the best
    /// flat segment index, or -1 if no room scores above threshold.
    /// </summary>
    public static int SelectRoom(CitizenNode citizen, SegmentGrid grid)
    {
        int bestSegment = -1;
        float bestScore = 0f;

        for (int i = 0; i < SegmentGrid.TotalSegments; i++)
        {
            var (row, pos) = SegmentGrid.FromIndex(i);
            if (!grid.IsOccupied(row, pos)) continue;

            // Skip home room (resting is handled separately)
            if (citizen.HomeSegmentIndex == i) continue;

            float score = ScoreRoom(citizen, i);
            if (score > bestScore)
            {
                bestScore = score;
                bestSegment = i;
            }
        }

        return bestScore > MinScoreThreshold ? bestSegment : -1;
    }

    private static float ScoreRoom(CitizenNode citizen, int flatIndex)
    {
        var placedRoom = BuildManager.Instance?.GetPlacedRoom(flatIndex);
        if (placedRoom == null) return 0f;

        float proximity = ScoreProximity(citizen.CurrentAngle, flatIndex);
        float affinity = ScoreTraitAffinity(
            citizen.Data.Interest, placedRoom.Value.Definition.Category);
        float recency = ScoreRecency(citizen, flatIndex);
        float wishBonus = ScoreWishMatch(citizen, placedRoom.Value.Definition);

        // Weighted product: each factor is 0-1, weights control influence
        return (proximity * ProximityWeight)
             + (affinity * AffinityWeight)
             + (recency * RecencyWeight)
             + (wishBonus * WishWeight);
    }
}
```

**Key design decisions for scoring factors:**

| Factor | Input | Curve | Weight | Rationale |
|--------|-------|-------|--------|-----------|
| **Proximity** | Angular distance from citizen to room | Linear falloff, 1.0 at distance=0, 0.0 at max range | 0.3 | Citizens should prefer nearby rooms but not exclusively |
| **Trait affinity** | Citizen's Interest enum vs room's Category enum | Binary: 1.0 if match, 0.3 if no match (not zero -- citizens still visit non-preferred rooms) | 0.3 | Traits bias behavior, not dictate it |
| **Recency** | Time since last visit to this room | Exponential growth from 0 to 1 as recency increases (haven't visited recently = higher score) | 0.2 | Prevents repetitive visits to the same room |
| **Wish bonus** | Active wish matches room type | Binary: 1.0 if match, 0.0 if not | 0.2 | Preserves existing wish-nudge behavior |

### Pattern 4: StationClock as Time Authority

**What:** A single autoload singleton that owns the station's elapsed time and current period. Advances time in `_Process`, emits `PeriodChanged` through GameEvents when the period transitions. All other systems query StationClock.Instance.CurrentPeriod rather than tracking time independently.

**When to use:** Always. A single time authority prevents drift between systems (lighting thinks it's Night but citizens think it's Day).

**Trade-offs:** One more autoload singleton (9 total). The alternative -- making clock a scene node -- would require lazy discovery like RingVisual, which is fragile (proven by the existing `_grid == null` workaround in CitizenManager._Process).

**Implementation:**

```csharp
public enum StationPeriod { Morning, Day, Evening, Night }

public partial class StationClock : Node
{
    public static StationClock Instance { get; private set; }

    [Export] public StationClockConfig Config { get; set; }

    private float _elapsed;
    private StationPeriod _currentPeriod = StationPeriod.Morning;

    public float Elapsed => _elapsed;
    public StationPeriod CurrentPeriod => _currentPeriod;

    /// <summary>
    /// Returns normalized progress within the current period (0.0 to 1.0).
    /// Used by StationLighting for smooth interpolation within a period.
    /// </summary>
    public float PeriodProgress => CalculatePeriodProgress();

    public override void _EnterTree() => Instance = this;

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        float totalDay = Config.TotalDayLength;

        // Wrap elapsed time to day cycle
        if (_elapsed >= totalDay)
            _elapsed -= totalDay;

        // Determine period from elapsed time
        var newPeriod = CalculatePeriod(_elapsed);
        if (newPeriod != _currentPeriod)
        {
            var previous = _currentPeriod;
            _currentPeriod = newPeriod;
            GameEvents.Instance?.EmitPeriodChanged(newPeriod, previous);
        }
    }

    // Save/load API
    public void RestoreState(float elapsed)
    {
        _elapsed = elapsed;
        _currentPeriod = CalculatePeriod(_elapsed);
    }
}
```

---

## Data Flow

### Clock-Driven Decision Flow

```
StationClock._Process(delta)
    |
    | (every frame: advance _elapsed, check period boundary)
    |
    v
[Period boundary crossed?] --NO--> done
    |
    YES
    |
    v
GameEvents.EmitPeriodChanged(newPeriod, previousPeriod)
    |
    +---> StationLighting.OnPeriodChanged
    |     (tween light energy, color, fog, window emissives)
    |
    +---> ClockHUD.OnPeriodChanged
    |     (update sun/moon icon)
    |
    +---> SaveManager.OnAnyStateChanged
          (debounced autosave)
```

### Citizen Decision Flow (per citizen, on decision timer timeout)

```
DecisionTimer fires
    |
    v
[State == Walking?] --NO--> re-arm timer, done
    |
    YES
    |
    v
Transition to Evaluating
    |
    v
Query StationClock.CurrentPeriod
    |
    v
CitizenSchedule.GetActivityWeights(period, citizen.Rhythm)
    |
    v
Returns: { VisitWeight, RestWeight, WanderWeight }
    |
    v
Weighted random roll
    |
    +--[Rest]--> [Has home?] --YES--> Transition to Resting
    |                          |       StartHomeReturn()
    |                          NO
    |                          |
    |                          v
    |                       Fall through to Visit
    |
    +--[Visit]--> UtilityScorer.SelectRoom(citizen, grid)
    |             |
    |             v
    |          [bestRoom >= 0?] --YES--> Transition to Visiting
    |                                    StartVisit(bestRoom)
    |             |
    |             NO
    |             |
    |             v
    |          Transition to Walking (nothing to visit)
    |
    +--[Wander]--> Transition to Walking (continue walking)

    v
Re-arm decision timer with fresh random interval
```

### Save/Load Data Flow (v4 additions)

```
SaveManager.CollectGameState()
    |
    v
SaveData v4:
    - (existing v3 fields unchanged)
    + ClockElapsed: float (StationClock.Instance.Elapsed)
    + ClockPeriod: int (cast of StationPeriod enum)

SavedCitizen v4:
    - (existing v3 fields unchanged)
    + InterestTrait: int? (nullable for v3 backward compat)
    + RhythmTrait: int? (nullable for v3 backward compat)

    v
SaveManager.ApplyState(data)
    |
    v
[Version >= 4?]
    YES: StationClock.RestoreState(data.ClockElapsed)
    NO:  StationClock starts at Morning (fresh cycle)

    v
SaveManager.ApplySceneState(data)
    |
    v
SpawnCitizenFromSave() extended:
    - If SavedCitizen.InterestTrait != null: set from save
    - If SavedCitizen.InterestTrait == null: assign random (v3 compat)
```

---

## Integration Points with Existing Singletons

### GameEvents Extensions (new events)

| Event | Signature | Emitter | Consumers |
|-------|-----------|---------|-----------|
| `PeriodChanged` | `Action<StationPeriod, StationPeriod>` (new, previous) | StationClock | StationLighting, ClockHUD, SaveManager |
| `CitizenStateChanged` | `Action<string, CitizenState>` (name, newState) | CitizenNode | CitizenInfoPanel (optional, for debug/tooltip) |

These follow the existing event pattern: typed C# delegates with `Emit*` helper methods and null-safe `?.Invoke()`.

### Singleton Interaction Map

| From | To | Integration Point | Change Type |
|------|-----|-------------------|-------------|
| **StationClock** | GameEvents | Emits PeriodChanged | New event |
| **StationClock** | SaveManager | Provides Elapsed/Period for save | New save fields |
| **CitizenNode** | StationClock | Reads CurrentPeriod in EvaluateNextAction | New read dependency |
| **CitizenNode** | BuildManager | Reads GetPlacedRoom in UtilityScorer (existing) | Unchanged |
| **CitizenNode** | WishBoard | Reads via WishNudgeRequested (existing) | Unchanged |
| **CitizenManager** | CitizenNode | SpawnCitizen assigns traits at creation | Modified spawn |
| **CitizenManager** | CitizenNode | SpawnCitizenFromSave restores traits | Modified save spawn |
| **SaveManager** | StationClock | Restores clock state on load | New restore call |
| **SaveManager** | CitizenManager | Restores trait data per citizen | Modified restore |
| **HousingManager** | CitizenNode | HomeSegmentIndex (existing) | Unchanged |
| **StationLighting** | StationClock | Subscribes to PeriodChanged | New subscription |
| **ClockHUD** | StationClock | Reads CurrentPeriod, subscribes PeriodChanged | New UI |

### What Does NOT Change

These singletons and systems are untouched by v1.4:

- **EconomyManager** -- income formulas, credit management, tier multipliers all unchanged
- **HappinessManager** -- mood system, tier transitions, arrival gating all unchanged
- **HousingManager** -- assignment algorithm, capacity tracking unchanged
- **WishBoard** -- wish tracking, nudge system unchanged (utility scorer replaces the distance-based weighting in CitizenNode, not WishBoard)
- **BuildManager** -- room placement, demolition unchanged
- **MoodSystem** -- POCO unchanged

---

## Detailed Component Specifications

### StationClockConfig (Godot Resource)

```csharp
[GlobalClass]
public partial class StationClockConfig : Resource
{
    [ExportGroup("Day Cycle")]
    [Export] public float TotalDayLength { get; set; } = 480.0f; // 8 minutes real time

    [ExportGroup("Period Durations (fraction of day)")]
    [Export] public float MorningFraction { get; set; } = 0.2f;  // 96s
    [Export] public float DayFraction { get; set; } = 0.35f;     // 168s
    [Export] public float EveningFraction { get; set; } = 0.2f;  // 96s
    [Export] public float NightFraction { get; set; } = 0.25f;   // 120s
    // Fractions must sum to 1.0 -- validated in StationClock._Ready()
}
```

**Rationale for 8-minute day:** At the current visit frequency (20-40s between visits), an 8-minute day gives citizens 12-24 visits per day cycle. This is enough for the player to observe pattern differences between Morning (active) and Night (resting) without the cycle feeling rushed. The day is the longest period because it is the primary "active play" window.

### CitizenData Extensions

```csharp
public enum InterestTrait
{
    Social,     // Prefers Comfort rooms (reading nook, garden)
    Industrious, // Prefers Work rooms (workshop, craft lab)
    Curious,    // Prefers Utility rooms (comm relay, star lounge)
    Homebody    // Prefers Housing-adjacent rooms, rests more
}

public enum RhythmTrait
{
    EarlyBird,  // Active Morning/Day, rests Evening/Night
    NightOwl,   // Rests Morning, active Day/Evening/Night
    Steady      // Balanced across all periods
}

// Added to CitizenData:
[ExportGroup("Traits")]
[Export] public InterestTrait Interest { get; set; }
[Export] public RhythmTrait Rhythm { get; set; }
```

**Rationale for exactly 2 traits:** The PROJECT.md specifies "1 Interest + 1 Rhythm per citizen." Interest creates observable room preferences (player sees a citizen repeatedly visiting the workshop and infers they are Industrious). Rhythm creates observable time-of-day patterns (EarlyBird citizens are walking at Morning while NightOwls are resting). Two traits are enough to make each citizen feel distinct without overwhelming the player with personality data.

### ScheduleConfig (Activity Weights)

```csharp
[GlobalClass]
public partial class ScheduleConfig : Resource
{
    // Weights per (Period, Rhythm) combination
    // Each row sums to 1.0: VisitWeight + RestWeight + WanderWeight = 1.0

    [ExportGroup("EarlyBird")]
    [Export] public Vector3 EarlyBirdMorning { get; set; } = new(0.6f, 0.1f, 0.3f);
    [Export] public Vector3 EarlyBirdDay { get; set; } = new(0.5f, 0.15f, 0.35f);
    [Export] public Vector3 EarlyBirdEvening { get; set; } = new(0.3f, 0.4f, 0.3f);
    [Export] public Vector3 EarlyBirdNight { get; set; } = new(0.1f, 0.7f, 0.2f);

    [ExportGroup("NightOwl")]
    [Export] public Vector3 NightOwlMorning { get; set; } = new(0.1f, 0.7f, 0.2f);
    [Export] public Vector3 NightOwlDay { get; set; } = new(0.4f, 0.2f, 0.4f);
    [Export] public Vector3 NightOwlEvening { get; set; } = new(0.6f, 0.1f, 0.3f);
    [Export] public Vector3 NightOwlNight { get; set; } = new(0.5f, 0.15f, 0.35f);

    [ExportGroup("Steady")]
    [Export] public Vector3 SteadyMorning { get; set; } = new(0.45f, 0.2f, 0.35f);
    [Export] public Vector3 SteadyDay { get; set; } = new(0.45f, 0.2f, 0.35f);
    [Export] public Vector3 SteadyEvening { get; set; } = new(0.4f, 0.25f, 0.35f);
    [Export] public Vector3 SteadyNight { get; set; } = new(0.25f, 0.45f, 0.3f);
}
```

**Note:** Using `Vector3` for (visit, rest, wander) triples is a pragmatic choice because Godot's Inspector natively displays Vector3 with labeled X/Y/Z fields, making tuning straightforward. A custom struct would require a custom Inspector plugin.

### StationLighting (Scene Node)

StationLighting is a **Node3D added to the game scene** (not an autoload) because it manages visual children (DirectionalLight3D, potentially WorldEnvironment overrides). It subscribes to PeriodChanged via GameEvents and tweens visual properties.

```csharp
public partial class StationLighting : Node3D
{
    private DirectionalLight3D _sunLight;
    private Tween _lightTween;

    // Period-indexed lighting presets
    private static readonly Dictionary<StationPeriod, LightPreset> Presets = new()
    {
        { StationPeriod.Morning, new(energy: 0.6f, color: new Color(1.0f, 0.85f, 0.7f)) },
        { StationPeriod.Day,     new(energy: 1.0f, color: new Color(1.0f, 0.98f, 0.95f)) },
        { StationPeriod.Evening, new(energy: 0.5f, color: new Color(1.0f, 0.7f, 0.5f)) },
        { StationPeriod.Night,   new(energy: 0.15f, color: new Color(0.4f, 0.45f, 0.7f)) },
    };

    private void OnPeriodChanged(StationPeriod newPeriod, StationPeriod previous)
    {
        var preset = Presets[newPeriod];
        _lightTween?.Kill();
        _lightTween = CreateTween();
        _lightTween.SetParallel(true);
        _lightTween.TweenProperty(_sunLight, "light_energy",
            preset.Energy, 3.0f).SetTrans(Tween.TransitionType.Sine);
        _lightTween.TweenProperty(_sunLight, "light_color",
            preset.Color, 3.0f).SetTrans(Tween.TransitionType.Sine);
    }
}
```

**Why scene node, not autoload:** StationLighting owns visual children (DirectionalLight3D). Autoloads are Nodes added outside the scene tree's main hierarchy. Placing lights under an autoload would require manual reparenting or TopLevel flags. A scene node naturally parents the light into the 3D world.

---

## Anti-Patterns

### Anti-Pattern 1: Behavior Trees for This Scope

**What people do:** Reach for a behavior tree library (LimboAI, BehaviourToolkit) to implement citizen AI.
**Why it's wrong:** The citizen has exactly 4 states and 6 transitions. A behavior tree adds node-graph complexity, plugin dependencies, and debugging overhead for a problem that an enum + switch solves in 50 lines. Behavior trees shine when there are 15+ states with dynamic priorities. This is not that project.
**Do this instead:** Enum-driven state machine with a pure-function utility scorer. No external AI libraries.

### Anti-Pattern 2: Per-Frame Utility Evaluation

**What people do:** Score all rooms every frame to allow instant behavior changes.
**Why it's wrong:** 24 rooms * 20 citizens = 480 scoring calls per frame. Not catastrophically expensive, but wasteful. More importantly, per-frame evaluation causes oscillation -- a citizen ping-pongs between two equally-scored rooms.
**Do this instead:** Evaluate on a timer (15-30s). Once a decision is made (Visiting/Resting), commit to it until the tween completes. Re-evaluate only on the next decision timer fire. This also means scoring costs are amortized: ~1 evaluation per citizen per 20s = 1 citizen per second at 20 citizens.

### Anti-Pattern 3: Clock as Scene Node with Lazy Discovery

**What people do:** Make StationClock a scene node (like RingVisual) and use the `FindChild` lazy discovery pattern that CitizenManager already uses for `_grid`.
**Why it's wrong:** The lazy discovery pattern (`if (_grid == null) { FindChild... }`) is a known wart in the codebase. It exists because RingVisual is a 3D scene node that can't be an autoload. StationClock has no visual children and no reason to be in the scene tree. Making it an autoload avoids the null-discovery problem entirely.
**Do this instead:** Register StationClock as the 9th autoload in project.godot. It initializes before any scene node's `_Ready()`, so `StationClock.Instance` is always available.

### Anti-Pattern 4: Complex Trait Interactions

**What people do:** Design traits that interact with each other (e.g., "Social + EarlyBird means they invite other EarlyBirds to morning gatherings").
**Why it's wrong:** Cross-trait interactions create combinatorial complexity that is invisible to the player. The player sees a citizen visiting a garden and has no way to know if that is because of their Interest trait, their Rhythm trait, or a combination. Visible behavior must map to simple causes.
**Do this instead:** Interest affects WHICH room. Rhythm affects WHEN (what period). They are orthogonal axes with no interaction. The player learns "this citizen likes the workshop" (Interest) and "this citizen is active at night" (Rhythm) as two independent observations.

### Anti-Pattern 5: Extending Existing Event Signatures

**What people do:** Add the current period to existing events like CitizenEnteredRoom(name, segment, period) to avoid consumers needing to query StationClock.
**Why it's wrong:** Changing event signatures breaks all existing subscribers. The 10+ event handlers in the codebase would all need parameter updates. This is a high-risk, low-value change.
**Do this instead:** Consumers that need the current period call `StationClock.Instance.CurrentPeriod`. This is a property read, not a method call. Zero allocation, zero indirection.

---

## Build Order Considering Dependencies

### Dependency Graph

```
StationClock (autoload)
    |
    +---> StationLighting (scene node, subscribes to PeriodChanged)
    |
    +---> ClockHUD (UI, subscribes to PeriodChanged)
    |
    +---> CitizenSchedule (POCO, reads CurrentPeriod)
              |
              +---> CitizenStateMachine (inside CitizenNode, uses schedule)
                        |
                        +---> UtilityScorer (POCO, called during Evaluating state)
                                  |
                                  +---> Requires CitizenData.Interest (trait affinity scoring)

CitizenData.Interest + CitizenData.Rhythm (data, no runtime deps)
    |
    +---> CitizenManager.SpawnCitizen (assigns traits at creation)
    |
    +---> SaveManager (v4 format with trait fields)

SaveManager v4 format
    |
    +---> Requires StationClock (save elapsed time)
    +---> Requires CitizenData traits (save trait enums)
```

### Recommended Build Order

**Phase 1: StationClock + GameEvents Extension**
- Add StationPeriod enum, StationClockConfig resource
- Implement StationClock autoload (time advancement, period detection)
- Add PeriodChanged event to GameEvents + ClearAllSubscribers cleanup
- Add StationClock.Reset() + SubscribeToEvents() for test infrastructure
- Register as 9th autoload in project.godot
- **Dependencies:** None (standalone system)
- **Delivers:** `StationClock.Instance.CurrentPeriod` queryable from any system
- **Testable:** StationClockConfig + period calculation are pure functions

**Phase 2: Day/Night Visuals (StationLighting + ClockHUD)**
- Create StationLighting node with DirectionalLight3D child
- Subscribe to PeriodChanged, tween light energy/color
- Add room window emissive changes (iterate room meshes on period change)
- Create ClockHUD with sun/moon icon
- **Dependencies:** Phase 1 (StationClock emits PeriodChanged)
- **Delivers:** Visible day/night cycle, player can observe time passing
- **Testable:** Manual visual verification

**Phase 3: Citizen Traits (Data + Assignment)**
- Add InterestTrait and RhythmTrait enums to CitizenData
- Modify CitizenManager.SpawnCitizen to assign random traits
- Modify CitizenManager.SpawnCitizenFromSave to restore traits
- Update CitizenInfoPanel to show trait icons
- **Dependencies:** None (pure data additions, independent of clock)
- **Delivers:** Each citizen has visible traits, info panel shows them
- **Testable:** Trait assignment coverage, info panel display

**Phase 4: Schedule Templates + Utility Scoring**
- Implement ScheduleConfig resource with activity weight tables
- Implement CitizenSchedule POCO (period + rhythm -> weights)
- Implement UtilityScorer static class (proximity + affinity + recency + wish)
- **Dependencies:** Phase 1 (CurrentPeriod), Phase 3 (Interest trait for affinity)
- **Delivers:** Scoring infrastructure ready for state machine integration
- **Testable:** UtilityScorer is a pure function -- unit testable with known inputs

**Phase 5: Citizen State Machine (Core Refactor)**
- Add CitizenState enum and TryTransition to CitizenNode
- Replace _visitTimer + _homeTimer with single _decisionTimer
- Implement EvaluateNextAction using CitizenSchedule + UtilityScorer
- Preserve existing StartVisit and StartHomeReturn tween sequences
- Remove `_isVisiting` and `_isAtHome` boolean fields (replaced by state enum)
- Update CitizenManager._Process auto-deselect to check state enum
- Wire wish timer abort and wish nudge to state machine
- **Dependencies:** Phase 4 (schedule + scorer), Phase 1 (clock period)
- **Delivers:** Citizens make schedule-aware, trait-biased decisions
- **Testable:** State transition validation, decision flow with mock periods

**Phase 6: Save/Load v4**
- Add ClockElapsed + ClockPeriod to SaveData
- Add InterestTrait + RhythmTrait (nullable int?) to SavedCitizen
- Bump SaveData.Version to 4
- Extend SaveManager.CollectGameState for clock + traits
- Extend SaveManager.ApplyState with version >= 4 gating
- Extend SaveManager.ApplySceneState for trait restoration
- Add StationClock state to ClearAllSubscribers/Reset test infrastructure
- **Dependencies:** Phase 1 (clock state), Phase 3 (trait data), Phase 5 (state machine working)
- **Delivers:** Full round-trip save/load with clock position and citizen traits
- **Testable:** JSON round-trip for v4 format, v3 backward compatibility (traits null, clock defaults)

**Phase 7: Room Tooltip Visitors**
- Add visitor tracking to room tooltip (which citizens are currently visiting)
- Subscribe to CitizenEnteredRoom / CitizenExitedRoom events
- **Dependencies:** Phase 5 (state machine emits room visit events correctly)
- **Delivers:** Player can see which citizens are in which rooms
- **Testable:** Event subscription, visitor count accuracy

### Phase Ordering Rationale

```
Phase 1 (Clock)          Phase 3 (Traits)
    |                        |
    v                        v
Phase 2 (Visuals)       Phase 4 (Schedule + Scoring)
                             |
                             v
                        Phase 5 (State Machine)
                             |
                             v
                        Phase 6 (Save/Load v4)
                             |
                             v
                        Phase 7 (Room Tooltips)
```

- **Phases 1 and 3 are independent** and can be built in parallel or either order. Phase 1 first because it unblocks both visuals (Phase 2) and the decision system (Phase 4).
- **Phase 2 before Phase 5** because day/night visuals give immediate player-visible feedback before the complex state machine refactor. If Phase 5 hits blockers, the milestone still has a shipped visual feature.
- **Phase 5 is the riskiest phase** because it refactors 400+ lines of CitizenNode behavior code. All earlier phases add new code alongside existing code. Phase 5 is the only phase that replaces existing working code.
- **Phase 6 last in the critical path** because save/load must capture all new state. Building it last means all the state it needs to persist actually exists.

---

## Recency Tracking (New Data Structure in CitizenNode)

The utility scorer needs to know when a citizen last visited each room to compute recency scores. This requires a small per-citizen dictionary:

```csharp
// Inside CitizenNode:
private readonly Dictionary<int, float> _lastVisitTime = new();

// At the end of StartVisit tween completion callback:
_lastVisitTime[targetSegment] = StationClock.Instance?.Elapsed ?? 0f;

// In UtilityScorer:
public static float ScoreRecency(CitizenNode citizen, int flatIndex)
{
    float elapsed = StationClock.Instance?.Elapsed ?? 0f;
    if (!citizen.LastVisitTimes.TryGetValue(flatIndex, out float lastVisit))
        return 1.0f; // Never visited = max recency score

    float timeSince = elapsed - lastVisit;
    if (timeSince < 0) timeSince += StationClock.Instance.Config.TotalDayLength;

    // Normalize: 0 at just-visited, 1.0 at half-day-ago or more
    float halfDay = StationClock.Instance.Config.TotalDayLength * 0.5f;
    return Mathf.Clamp(timeSince / halfDay, 0f, 1f);
}
```

Recency data is ephemeral -- it does NOT need to be saved. On load, all recency scores start at max (1.0), which is correct behavior (citizen hasn't visited anything "recently" in the loaded session).

---

## Scaling Considerations

| Concern | At 10 citizens | At 30 citizens | At 50 citizens |
|---------|----------------|----------------|----------------|
| **Decision evaluations** | ~0.5/sec (1 per 20s each) | ~1.5/sec | ~2.5/sec |
| **Rooms scored per evaluation** | Up to 24 segments | Up to 24 segments | Up to 24 segments |
| **Total scoring calls/sec** | ~12 | ~36 | ~60 |
| **Memory per citizen** | ~200 bytes (recency dict) | ~200 bytes | ~200 bytes |

This is negligible. The scoring is simple arithmetic (4 multiplications + 1 addition per room). Even at 50 citizens, the total scoring work is under 1500 arithmetic operations per second. The existing tween-based animations are far more expensive.

### First Bottleneck

The first bottleneck will be **tween count**, not AI computation. With 50 citizens, many will be in mid-tween simultaneously (walking to rooms, fading, resting). Godot's tween system handles this well, but the visual density of 50 capsules on a 24-segment ring may feel crowded. This is a design concern, not an architecture concern.

---

## Sources

- Direct codebase inspection of all 40+ C# source files including GameEvents.cs (343 lines), CitizenNode.cs (1107 lines), CitizenManager.cs (423 lines), SaveManager.cs (538 lines), HousingManager.cs (525 lines), HappinessManager.cs (367 lines), EconomyManager.cs (366 lines), WishBoard.cs (407 lines), MoodSystem.cs (127 lines), and all Data/ resources (HIGH confidence)
- [Game AI Pro Chapter 9: Introduction to Utility Theory](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter09_An_Introduction_to_Utility_Theory.pdf) -- utility scoring fundamentals, response curves (HIGH confidence)
- [Game AI Pro 3 Chapter 13: Choosing Effective Utility-Based Considerations](http://www.gameaipro.com/GameAIPro3/GameAIPro3_Chapter13_Choosing_Effective_Utility-Based_Considerations.pdf) -- scoring factor design, weight tuning (HIGH confidence)
- [Utility System - Wikipedia](https://en.wikipedia.org/wiki/Utility_system) -- utility AI overview, scoring patterns (MEDIUM confidence)
- [GDQuest: Finite State Machine in Godot 4](https://www.gdquest.com/tutorial/godot/design-patterns/finite-state-machine/) -- enum state machine pattern for Godot (MEDIUM confidence)
- [Medium: C# FSM Implementation in Godot](https://medium.com/codex/making-a-basic-finite-state-machine-godot4-c-fe5ccc0e8cd7) -- C# state machine patterns (MEDIUM confidence)
- [GameDev Academy: Day Night Cycle in Godot 4](https://gamedevacademy.org/godot-day-night-cycle/) -- DirectionalLight tweening approach (MEDIUM confidence)
- PROJECT.md and existing codebase patterns (HappinessConfig, HousingConfig, MoodSystem POCO) for config resource and testability patterns (HIGH confidence)

---
*Architecture research for: Orbital Rings v1.4 -- Citizen AI, Day/Night Cycle, Utility Scoring*
*Researched: 2026-03-07*
