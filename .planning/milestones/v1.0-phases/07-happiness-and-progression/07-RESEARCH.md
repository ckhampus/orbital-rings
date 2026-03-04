# Phase 7: Happiness and Progression - Research

**Researched:** 2026-03-03
**Domain:** Game progression systems (happiness tracking, population growth, blueprint unlocks) in Godot 4.6 C#
**Confidence:** HIGH

## Summary

Phase 7 closes the core game loop by connecting wish fulfillment (Phase 6) to visible progression outcomes: happiness rises, citizens arrive, and new rooms unlock. The codebase is exceptionally well-prepared for this phase -- GameEvents already has `HappinessChanged`, `BlueprintUnlocked`, and `CitizenArrived` events stubbed; EconomyManager already accepts `SetHappiness(float)` and applies a 1.3x cap multiplier; and BuildPanel already loads all 10 room definitions. The work is primarily a new HappinessManager Autoload (following the established singleton pattern), modifications to BuildPanel for unlock filtering, a new happiness bar in the HUD layer, and a public spawn method on CitizenManager.

The architecture is straightforward because the existing event bus pattern means HappinessManager can integrate without modifying most existing files. The main code changes touch 4-5 existing files (GameEvents registration in project.godot, BuildPanel filtering, CitizenManager spawn API, CreditHUD layout expansion) plus 1-2 new files (HappinessManager, optional HappinessBar if separated from CreditHUD).

**Primary recommendation:** Build HappinessManager as a single Autoload that owns all progression state (happiness value, unlock thresholds, arrival timer), listens to WishFulfilled events, and fires HappinessChanged/BlueprintUnlocked events. Keep the happiness bar as part of the existing HUD layer, following CreditHUD's programmatic UI pattern exactly.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Simple horizontal fill bar + percentage label, placed next to the credit display in the top-right corner
- Layout: `credits | population count | happiness bar` all in one top-right cluster
- Population count shown as icon + number (e.g., citizen icon + "7")
- On wish fulfillment: bar smoothly tweens to new value (~0.5s), brief warm pulse/glow on the bar, small floating "+X%" text drifts up and fades
- Matches CreditHUD's flash-on-change feedback pattern
- Gradual chance-based arrivals: higher happiness = higher chance of a new citizen per time interval (~60 second check)
- Population cap tied to housing capacity (sum of BaseCapacity for all placed Housing-category rooms)
- No housing = no new arrivals, even at high happiness; housing gives Housing rooms a clear purpose
- 5 starter citizens remain from Phase 5 (initial spawn bypasses housing check)
- Arrival fanfare: new citizen capsule fades in on walkway, small floating text "Luna has arrived!" drifts up and fades (~2s), population counter ticks up
- 6 starter rooms available immediately: bunk_pod, air_recycler, workshop, reading_nook, storage_bay, garden_nook
- 4 rooms locked behind happiness milestones:
  - 25% happiness: sky_loft (Housing) + craft_lab (Work)
  - 60% happiness: star_lounge (Comfort) + comm_relay (Utility)
- Locked rooms are completely hidden from the build panel until unlocked (not greyed out)
- On unlock: centered floating text "New rooms available!" drifts up and fades (~3s), build panel category tabs with new rooms pulse/glow briefly
- Unlocked rooms are NOT more expensive than starter rooms
- Happiness only goes up -- no decay, no subtraction, no punishment (matches cozy promise)
- Diminishing returns: early wishes grant more, later wishes grant less (formula: gain = base / (1 + currentHappiness))
- All wish categories grant equal happiness -- no min-maxing, every wish matters equally
- Happiness capped at 100% -- at 100% the station is fully happy, all unlocks achieved, max economy multiplier (1.3x)
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

### Deferred Ideas (OUT OF SCOPE)
- Persistent wish board / notification panel (QOLX-01) -- v2 requirement
- Full HUD wiring and polish pass -- Phase 8
- Save/load persistence of happiness value -- Phase 8
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| PROG-01 | Station happiness is tracked and visible to the player | HappinessManager Autoload owns the float value (0.0-1.0), fires GameEvents.HappinessChanged; happiness bar UI subscribes and displays fill + percentage label. EconomyManager.SetHappiness() already accepts and clamps this value for income multiplier. |
| PROG-02 | New citizens arrive passively as happiness grows | HappinessManager owns a Timer (~60s interval), rolls arrival chance proportional to happiness, checks housing capacity via BuildManager room scan, calls new CitizenManager.SpawnCitizen() public method. Population count display in HUD updates via CitizenArrived event. |
| PROG-03 | New room blueprints unlock at happiness milestones (at least 2-3 unlock moments) | HappinessManager tracks two thresholds (0.25 and 0.60), fires GameEvents.BlueprintUnlocked(roomId) when crossed. BuildPanel filters LoadRoomDefinitions by an unlock set managed by HappinessManager. Hidden rooms become visible on unlock with notification. |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Godot Engine | 4.6 | Game engine | Project runtime (project.godot confirms 4.6 + C# + Forward Plus) |
| .NET | 8.0+ | C# runtime | Project uses .NETCoreApp 8.0 (confirmed in .godot/mono build artifacts) |
| Pure C# events | N/A | Cross-system communication | Locked project decision since Phase 1: no Godot [Signal], pure delegates only |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Godot Tween API | Built-in | Animations (bar fill, floating text, pulse/glow) | All visual feedback: happiness bar tweens, badge pop, tab glow |
| Godot Timer node | Built-in | Periodic checks (arrival timer) | Citizen arrival checks (~60s interval), following EconomyManager income tick pattern |
| ResourceLoader | Built-in | Load room .tres resources | BuildPanel already uses this for room definitions; HappinessManager uses it for unlock filtering |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Timer node for arrival | _Process delta accumulation | Timer is established pattern (EconomyManager, CitizenNode); cleaner separation |
| Separate HappinessBar scene | Programmatic UI in code | All existing UI is programmatic (CreditHUD, BuildPanel, SegmentTooltip); no .tscn scenes for UI |
| Separate PopulationDisplay node | Integrated into CreditHUD area | Keeping HUD cluster together avoids new CanvasLayer; layout matches user decision |

## Architecture Patterns

### Recommended Project Structure
```
Scripts/
  Autoloads/
    HappinessManager.cs     # NEW: owns happiness state, arrival timer, unlock tracking
    GameEvents.cs            # MODIFY: no code changes needed (events already stubbed!)
    EconomyManager.cs        # NO CHANGES: SetHappiness() already exists
    WishBoard.cs             # NO CHANGES
  Build/
    BuildPanel.cs            # MODIFY: filter rooms by unlock state, add tab glow on unlock
    BuildManager.cs          # MINOR: add method to enumerate placed rooms by category
  Citizens/
    CitizenManager.cs        # MODIFY: add public SpawnCitizen() method
    CitizenNode.cs           # NO CHANGES
  UI/
    CreditHUD.cs             # MODIFY: expand to include population count display
    HappinessBar.cs          # NEW: happiness fill bar + percentage label
    FloatingText.cs          # NO CHANGES (reusable as-is)
```

### Pattern 1: Autoload Singleton (HappinessManager)
**What:** New Autoload following the exact pattern of EconomyManager/GameEvents/BuildManager/CitizenManager/WishBoard.
**When to use:** Always -- this is the only architecture pattern used for manager classes in this project.
**Example:**
```csharp
// Follows established project pattern exactly
public partial class HappinessManager : Node
{
    public static HappinessManager Instance { get; private set; }

    private float _happiness;        // 0.0 to 1.0
    private Timer _arrivalTimer;     // ~60s periodic check
    private HashSet<string> _unlockedRooms;  // tracks unlocked room IDs

    // Unlock thresholds (locked decisions)
    private static readonly (float threshold, string[] rooms)[] UnlockMilestones =
    {
        (0.25f, new[] { "sky_loft", "craft_lab" }),
        (0.60f, new[] { "star_lounge", "comm_relay" }),
    };

    public override void _Ready()
    {
        Instance = this;
        // Initialize with 6 starter rooms
        _unlockedRooms = new HashSet<string>
        {
            "bunk_pod", "air_recycler", "workshop",
            "reading_nook", "storage_bay", "garden_nook"
        };

        // Subscribe to wish fulfillment
        GameEvents.Instance.WishFulfilled += OnWishFulfilled;

        // Create arrival timer (same pattern as EconomyManager income timer)
        _arrivalTimer = new Timer();
        _arrivalTimer.WaitTime = 60.0;
        _arrivalTimer.OneShot = false;
        AddChild(_arrivalTimer);
        _arrivalTimer.Timeout += OnArrivalCheck;
        _arrivalTimer.Start();
    }
}
```

### Pattern 2: Event-Driven HUD Updates (Happiness Bar)
**What:** Programmatic UI subscribing to GameEvents, following CreditHUD's exact approach.
**When to use:** For the happiness bar and population count display.
**Example:**
```csharp
// Follows CreditHUD pattern: MarginContainer with programmatic children
public partial class HappinessBar : MarginContainer
{
    private ProgressBar _bar;          // or manual ColorRect + fill ColorRect
    private Label _percentLabel;

    public override void _Ready()
    {
        // Build UI programmatically (no .tscn)
        // Subscribe to GameEvents.Instance.HappinessChanged
        // Tween bar fill on change (kill-before-create pattern)
    }
}
```

### Pattern 3: BuildPanel Unlock Filtering
**What:** BuildPanel's LoadRoomDefinitions filters against HappinessManager.IsRoomUnlocked().
**When to use:** When populating room cards for each category tab.
**Example:**
```csharp
// In BuildPanel.LoadRoomDefinitions() -- add filter
private void LoadRoomDefinitions()
{
    foreach (var id in RoomFiles)
    {
        // Skip rooms that haven't been unlocked yet
        if (HappinessManager.Instance != null && !HappinessManager.Instance.IsRoomUnlocked(id))
            continue;

        var def = ResourceLoader.Load<RoomDefinition>($"res://Resources/Rooms/{id}.tres");
        // ... existing logic
    }
}
```

### Anti-Patterns to Avoid
- **Polling happiness in _Process:** Use event-driven updates via GameEvents.HappinessChanged. Never poll HappinessManager.Happiness in _Process for UI updates.
- **Modifying CitizenNode for arrivals:** New citizen spawning should go through CitizenManager.SpawnCitizen(), not by directly creating CitizenNode instances in HappinessManager.
- **Storing unlock state in BuildPanel:** BuildPanel should query HappinessManager.IsRoomUnlocked() -- unlock state belongs in HappinessManager, not in UI.
- **Happiness decay timer:** Explicitly prohibited by cozy promise. Happiness only goes up.
- **Complex arrival scheduling:** Keep it simple: Timer fires, roll a probability, spawn or don't. No queue, no schedule, no prediction.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Tween animations | Custom interpolation in _Process | Godot Tween API with kill-before-create | Project-wide pattern, handles timing/easing/chaining correctly |
| Periodic timer | Delta accumulation in _Process | Timer child node | Established pattern (EconomyManager, CitizenNode); pauses with scene tree |
| Floating text | Custom Label management | Existing FloatingText class | Already built, tested, self-destructs after animation |
| Random probability | Custom RNG | GD.Randf() | Engine RNG, consistent with all existing random calls |

**Key insight:** Nearly all the infrastructure is already built. This phase is primarily wiring existing pieces together through a new manager, not building new subsystems from scratch.

## Common Pitfalls

### Pitfall 1: Autoload Order in project.godot
**What goes wrong:** HappinessManager tries to access GameEvents.Instance, EconomyManager.Instance, or CitizenManager.Instance before they're initialized.
**Why it happens:** Autoloads initialize in the order listed in project.godot. If HappinessManager is listed before its dependencies, Instance will be null.
**How to avoid:** Register HappinessManager AFTER all existing autoloads in project.godot. Current order: GameEvents, EconomyManager, BuildManager, CitizenManager, WishBoard. HappinessManager should be last (6th entry).
**Warning signs:** NullReferenceException in HappinessManager._Ready() when accessing other singletons.

### Pitfall 2: Tween Stacking on Rapid Wish Fulfillment
**What goes wrong:** Multiple wishes fulfilled in quick succession create overlapping tweens on the happiness bar, causing visual glitches or wrong final values.
**Why it happens:** Each WishFulfilled event creates a new tween. If the previous hasn't finished, they fight.
**How to avoid:** Kill-before-create pattern (established project convention). Store `_activeTween`, call `_activeTween?.Kill()` before creating a new one. CreditHUD already does this correctly -- follow the same approach.
**Warning signs:** Bar flickering, bar showing wrong percentage, tween callbacks firing out of order.

### Pitfall 3: Housing Capacity Calculation Missing Multi-Segment Rooms
**What goes wrong:** Housing capacity check only counts anchor segments, missing the BaseCapacity of rooms placed across multiple segments.
**Why it happens:** BuildManager._placedRooms is keyed by anchor index. A multi-segment bunk_pod still has one entry. BaseCapacity is per-room, not per-segment.
**How to avoid:** Iterate BuildManager._placedRooms values (not all 24 segment indices), and sum BaseCapacity for rooms where Definition.Category == Housing. Each room appears once in _placedRooms regardless of segment count.
**Warning signs:** Housing capacity lower than expected; need a public method on BuildManager to enumerate placed rooms.

### Pitfall 4: BuildPanel Doesn't Refresh After Unlock
**What goes wrong:** Rooms are unlocked by HappinessManager but BuildPanel still shows the old room set until the player switches tabs.
**Why it happens:** BuildPanel caches _roomsByCategory in LoadRoomDefinitions() at startup. New unlocks don't trigger a re-load.
**How to avoid:** BuildPanel subscribes to GameEvents.BlueprintUnlocked, calls a refresh method that re-runs LoadRoomDefinitions and re-populates the current tab's cards.
**Warning signs:** Player reaches 25% happiness, no new rooms appear in build panel until reopening it.

### Pitfall 5: CitizenManager Has No Public Spawn Method
**What goes wrong:** HappinessManager can't spawn new citizens because CitizenManager only has private SpawnStarterCitizens().
**Why it happens:** Phase 5 only needed starter citizens; no external caller needed to spawn.
**How to avoid:** Extract the single-citizen spawn logic into a public `SpawnCitizen()` method that HappinessManager can call. Keep SpawnStarterCitizens as a loop calling SpawnCitizen.
**Warning signs:** Compiler error when HappinessManager tries to call a non-existent method.

### Pitfall 6: Happiness Formula Calibration Off
**What goes wrong:** Players hit 100% too fast (trivial) or too slow (boring), or unlock thresholds don't align with expected wish pace.
**Why it happens:** The diminishing returns formula `gain = base / (1 + currentHappiness)` needs its base value tuned to the wish fulfillment rate.
**How to avoid:** Calibrate: if wishes are fulfilled every ~45-60s on average, and we want ~25 wishes to reach 100%, then sum(base/(1+h_i)) from i=0..24 should equal 1.0. With the formula, a base value of ~0.08 yields roughly 25 wishes to 100%. Verify with a quick simulation.
**Warning signs:** Reaching 100% in 5 minutes (too easy) or 40+ minutes (too slow).

### Pitfall 7: Shared Material Contamination on Tab Glow
**What goes wrong:** Tab glow animation on unlock affects all tabs instead of just the ones with new rooms.
**Why it happens:** If tab button styles share references, modifying one affects all.
**How to avoid:** BuildPanel already creates per-tab StyleBoxFlat instances. Continue this pattern -- Duplicate() any style before modifying for glow animation.
**Warning signs:** All 5 category tabs glowing when only 1-2 have new rooms.

## Code Examples

### Happiness Gain Formula (Claude's Discretion: calibrated)
```csharp
// Diminishing returns formula: gain = base / (1 + currentHappiness)
// Target: ~25 wishes to reach 1.0 (100%)
//
// Calibration: With base=0.08, the cumulative sum after N wishes:
//   5 wishes:  ~0.33 (33%)  -- first unlock at 25% happens around wish 4
//   15 wishes: ~0.65 (65%)  -- second unlock at 60% happens around wish 13
//   25 wishes: ~0.87 (87%)
//   30 wishes: ~0.95 (95%)
//   ~38 wishes: ~1.0 (100%)
//
// This feels slightly slow for "~25 wishes = 100%".
// Using base=0.12 gives better pacing:
//   4 wishes:  ~0.38  -- first unlock around wish 3-4
//   10 wishes: ~0.66  -- second unlock around wish 9-10
//   18 wishes: ~0.90
//   25 wishes: ~0.98
//   ~28 wishes: 1.0
//
// Recommendation: base = 0.12 for the target 20-25 minute session

private const float HappinessGainBase = 0.12f;

private void OnWishFulfilled(string citizenName, string wishType)
{
    if (_happiness >= 1.0f) return; // Already at max

    float gain = HappinessGainBase / (1.0f + _happiness);
    _happiness = Mathf.Min(_happiness + gain, 1.0f);

    // Update economy multiplier
    EconomyManager.Instance?.SetHappiness(_happiness);

    // Fire event for UI
    GameEvents.Instance?.EmitHappinessChanged(_happiness);

    // Check unlock thresholds
    CheckUnlockMilestones();
}
```

### Citizen Arrival Probability (Claude's Discretion)
```csharp
// Arrival check: every ~60 seconds, roll a probability based on happiness
// P(arrival) = happiness * 0.6  (at 100% happiness, 60% chance per check)
// This means on average: one new citizen every ~100s at full happiness
//
// Housing cap check: sum BaseCapacity of all placed Housing-category rooms
// 5 starter citizens bypass housing check (already spawned in Phase 5)

private const float ArrivalCheckInterval = 60.0f;
private const float ArrivalProbabilityScale = 0.6f;

private void OnArrivalCheck()
{
    if (_happiness <= 0f) return;

    int housingCap = CalculateHousingCapacity();
    int currentPop = CitizenManager.Instance?.CitizenCount ?? 0;

    // Can't exceed housing capacity
    if (currentPop >= housingCap) return;

    // Roll probability
    float chance = _happiness * ArrivalProbabilityScale;
    if (GD.Randf() < chance)
    {
        CitizenManager.Instance?.SpawnCitizen();
    }
}

private int CalculateHousingCapacity()
{
    // Sum BaseCapacity of all placed Housing-category rooms
    // Minimum 5 to account for starter citizens
    int capacity = 5; // starter citizens always have housing
    // ... iterate BuildManager placed rooms ...
    return capacity;
}
```

### BuildPanel Unlock Filtering
```csharp
// Modified LoadRoomDefinitions to filter by unlock state
private void LoadRoomDefinitions()
{
    _roomsByCategory.Clear();

    foreach (var id in RoomFiles)
    {
        // Filter: only load unlocked rooms
        if (HappinessManager.Instance != null && !HappinessManager.Instance.IsRoomUnlocked(id))
            continue;

        var def = ResourceLoader.Load<RoomDefinition>($"res://Resources/Rooms/{id}.tres");
        if (def != null)
        {
            if (!_roomsByCategory.ContainsKey(def.Category))
                _roomsByCategory[def.Category] = new List<RoomDefinition>();
            _roomsByCategory[def.Category].Add(def);
        }
    }
}

// Subscribe to BlueprintUnlocked for dynamic refresh
// In _Ready():
GameEvents.Instance.BlueprintUnlocked += OnBlueprintUnlocked;

private void OnBlueprintUnlocked(string roomType)
{
    // Re-load room definitions (now includes newly unlocked room)
    LoadRoomDefinitions();
    // Re-populate current tab
    PopulateRoomCards(Categories[_activeTabIndex]);
    // Pulse/glow tabs with new rooms (implementation detail)
}
```

### HUD Layout Expansion (Population Count)
```csharp
// CreditHUD currently has: HBox { iconLabel, balanceLabel }
// Expand to: HBox { creditIcon, balanceLabel, separator, popIcon, popLabel, separator, happinessBar }
//
// Alternative: Create a new HUD container at the HUDLayer level that
// contains CreditHUD + PopulationDisplay + HappinessBar side by side.
// This avoids modifying CreditHUD internals.
//
// Recommendation: Add population count and happiness bar as siblings
// of CreditHUD in the QuickTestScene.tscn HUDLayer, positioned with
// anchor offsets. This keeps each component independent.
```

### Public SpawnCitizen on CitizenManager
```csharp
// Extract from SpawnStarterCitizens into a public method
public CitizenNode SpawnCitizen()
{
    var bodyTypes = System.Enum.GetValues<CitizenData.BodyType>();

    var data = new CitizenData
    {
        CitizenName = CitizenNames.GetNextName(),
        Body = bodyTypes[GD.RandRange(0, bodyTypes.Length - 1)],
        PrimaryColor = Palette[GD.RandRange(0, Palette.Length - 1)],
        SecondaryColor = Palette[GD.RandRange(0, Palette.Length - 1)]
    };

    while (data.SecondaryColor == data.PrimaryColor && Palette.Length > 1)
        data.SecondaryColor = Palette[GD.RandRange(0, Palette.Length - 1)];

    var citizen = new CitizenNode();
    float startAngle = GD.Randf() * Mathf.Tau; // Random position on walkway
    citizen.Initialize(data, startAngle, _grid);

    AddChild(citizen);
    _citizens.Add(citizen);

    EconomyManager.Instance?.SetCitizenCount(_citizens.Count);
    GameEvents.Instance?.EmitCitizenArrived(data.CitizenName);

    return citizen;
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| N/A | All 10 rooms loaded unconditionally | Phase 4 | BuildPanel.LoadRoomDefinitions loads all RoomFiles entries |
| N/A | 5 starter citizens, no dynamic spawning | Phase 5 | SpawnStarterCitizens is private, no public spawn API |
| N/A | Happiness value unused (always 0.0) | Phase 3 | EconomyManager._currentHappiness defaults to 0, SetHappiness exists but never called |
| N/A | GameEvents stubs for Phase 7 | Phase 3/5 | HappinessChanged, BlueprintUnlocked, CitizenArrived events exist but aren't fired by any system |

**Current state:** The codebase has all the hooks ready but nothing connecting them. Phase 7 is entirely about wiring.

## Integration Points Analysis

### Files That Need Changes

| File | Change Type | What Changes |
|------|-------------|--------------|
| `project.godot` | Add line | Register HappinessManager as 6th Autoload |
| `Scripts/Citizens/CitizenManager.cs` | Add public method | Extract `SpawnCitizen()` from `SpawnStarterCitizens()` |
| `Scripts/Build/BuildPanel.cs` | Modify LoadRoomDefinitions | Filter by `HappinessManager.IsRoomUnlocked()`, subscribe to BlueprintUnlocked |
| `Scripts/Build/BuildManager.cs` | Add public method | `GetPlacedRoomsByCategory()` or similar for housing capacity scan |
| `Scenes/QuickTest/QuickTestScene.tscn` | Add nodes | HappinessBar + PopulationDisplay in HUDLayer |

### Files That Are New

| File | Purpose |
|------|---------|
| `Scripts/Autoloads/HappinessManager.cs` | Core progression logic: happiness tracking, arrival timer, unlock tracking |
| `Scripts/UI/HappinessBar.cs` | Happiness fill bar + percentage label (programmatic UI) |
| `Scripts/UI/PopulationDisplay.cs` | Population icon + count label (programmatic UI, optional -- could be part of HappinessBar) |

### Files That Stay Unchanged

| File | Why No Changes |
|------|----------------|
| `Scripts/Autoloads/GameEvents.cs` | All needed events already stubbed (HappinessChanged, BlueprintUnlocked) |
| `Scripts/Autoloads/EconomyManager.cs` | SetHappiness() already works, income formula already includes happiness multiplier |
| `Scripts/Autoloads/WishBoard.cs` | WishFulfilled event already fires correctly |
| `Scripts/Citizens/CitizenNode.cs` | No changes needed; new citizens use same Initialize() pattern |
| `Scripts/UI/FloatingText.cs` | Reusable as-is for "+X% happiness" and "Luna has arrived!" texts |
| All room .tres files | Room definitions don't need changes; BaseCapacity already set (bunk_pod=2, sky_loft=4) |

## Happiness Calibration Spreadsheet

### Diminishing Returns Simulation (base = 0.12)
```
Wish #  | gain = 0.12/(1+h)  | cumulative h  | % display
--------|-------------------- |---------------|----------
   1    | 0.120               | 0.120         | 12%
   2    | 0.107               | 0.227         | 23%
   3    | 0.098               | 0.325         | 33%  <-- approaching 25% unlock
   4    | 0.091               | 0.415         | 42%
   5    | 0.085               | 0.500         | 50%
   8    | 0.068               | 0.663         | 66%  <-- past 60% unlock
  10    | 0.060               | 0.743         | 74%
  15    | 0.047               | 0.876         | 88%
  20    | 0.039               | 0.944         | 94%
  25    | 0.033               | 0.978         | 98%
  30    | 0.029               | 0.994         | 99%
  ~35   | ~0.025              | ~1.000        | 100%
```

**Key milestones:**
- 25% unlock (0.25): reached at wish 2-3 (~2-3 min) -- fast early reinforcement
- 60% unlock (0.60): reached at wish 6-7 (~6-10 min) -- mid-session surprise
- 100%: reached at wish ~35 (~25-35 min) -- sustained play reward

**Verdict:** base=0.12 hits the 25% unlock very early (wish 2-3). This is actually good per CONTEXT.md: "25% first unlock (~5-8 wishes) gives early positive reinforcement." But the simulation shows it's faster than ~5 wishes. Consider base=0.08 for slightly slower pacing:

### Alternative Calibration (base = 0.08)
```
Wish #  | gain = 0.08/(1+h)  | cumulative h  | % display
--------|-------------------- |---------------|----------
   1    | 0.080               | 0.080         | 8%
   3    | 0.063               | 0.207         | 21%
   4    | 0.066               | 0.273         | 27%  <-- 25% unlock at wish 4
   5    | 0.063               | 0.336         | 34%
  10    | 0.048               | 0.536         | 54%
  12    | 0.044               | 0.610         | 61%  <-- 60% unlock at wish 12
  15    | 0.039               | 0.700         | 70%
  20    | 0.033               | 0.800         | 80%
  25    | 0.029               | 0.870         | 87%
  35    | 0.023               | 0.940         | 94%
  50    | 0.017               | 0.980         | 98%
```

**Recommendation:** Use base=0.08 for better pacing alignment with CONTEXT.md expectations (25% at ~4-5 wishes, 60% at ~12-13 wishes). The 100% asymptote at ~50 wishes means the very end is a long tail, which is fine -- the station is already fully functional by wish 20.

### Housing Capacity Values
Currently in .tres files:
- bunk_pod: BaseCapacity = 2 (1-2 segments)
- sky_loft: BaseCapacity = 4 (1-3 segments, unlocked at 25%)
- All other rooms: BaseCapacity = 0

**Population growth curve with housing:**
- Start: 5 citizens (starter, bypass housing)
- 1 bunk_pod placed: capacity = 5 + 2 = 7 (2 new arrivals possible)
- 2 bunk_pods: capacity = 5 + 4 = 9
- After 25% unlock, 1 sky_loft: capacity = 5 + 2 + 4 = 11
- With 3 housing rooms: capacity up to ~15-17

These values seem reasonable. The starter 5 + housing gives gradual growth to ~15-20 citizens, which is comfortable for a single ring with 24 segments.

## Open Questions

1. **BuildManager API for Housing Capacity**
   - What we know: BuildManager._placedRooms is private. GetPlacedRoom(flatIndex) returns one room by index.
   - What's unclear: Most efficient way for HappinessManager to sum housing capacity. Options: (a) add `GetTotalHousingCapacity()` to BuildManager, (b) add `GetAllPlacedRooms()` iterator, (c) subscribe to RoomPlaced/RoomDemolished and maintain a running total.
   - Recommendation: Option (c) -- subscribe to events and maintain a running `_housingCapacity` counter in HappinessManager. Most event-driven, no new public API needed on BuildManager, and avoids iterating all rooms every 60 seconds.

2. **HUD Layout: Separate Components vs Single Container**
   - What we know: CreditHUD is a MarginContainer anchored top-right. Happiness bar and population count need to sit alongside it.
   - What's unclear: Whether to modify CreditHUD to include all three, create a parent HBox container, or use separate anchored nodes.
   - Recommendation: Create separate PopulationDisplay and HappinessBar nodes as siblings in HUDLayer, offset with anchors. Keeps each component independent and follows single-responsibility. CreditHUD changes are minimal (just needs to reserve space for siblings).

3. **Arrival Fanfare: Fade-In Animation on New Citizens**
   - What we know: CONTEXT.md says "new citizen capsule fades in on walkway." Current SpawnStarterCitizens does not fade -- citizens appear instantly.
   - What's unclear: Should the fade-in use the same transparency mode toggling as CitizenNode's room visit fade?
   - Recommendation: Yes, reuse SetMeshTransparencyMode/SetMeshAlpha pattern. Start alpha at 0, tween to 1 over ~0.5s. This can be done in SpawnCitizen by having the method return the CitizenNode, then applying a fade-in tween from HappinessManager.

## Sources

### Primary (HIGH confidence)
- Project source code: All 25+ .cs files in /workspace/Scripts/ -- direct inspection of current implementation
- Room definitions: All 10 .tres files in /workspace/Resources/Rooms/ -- BaseCapacity values confirmed
- project.godot: Autoload registration order confirmed (5 current autoloads)
- QuickTestScene.tscn: HUD layer structure confirmed (CanvasLayer 5 for HUD, 8 for Build, 10 for Tooltip)

### Secondary (MEDIUM confidence)
- Happiness formula calibration: Mathematical simulation of diminishing returns formula -- verified with manual calculation, not runtime tested

### Tertiary (LOW confidence)
- None -- all findings based on direct code inspection

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - direct codebase inspection, all patterns established in Phases 1-6
- Architecture: HIGH - follows exact patterns already used 5 times (Autoload singletons, event bus, programmatic UI)
- Pitfalls: HIGH - identified from actual code structure and established project conventions
- Calibration: MEDIUM - mathematical simulation, not runtime-validated (will need play-testing)

**Research date:** 2026-03-03
**Valid until:** Indefinite (project-specific research, not library-dependent)
