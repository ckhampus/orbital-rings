# Phase 8: Polish and Loop Closure - Research

**Researched:** 2026-03-04
**Domain:** Save/Load, Ambient Audio, Title Screen, Wish Celebration, HUD Integration (Godot 4.6 C#)
**Confidence:** HIGH

## Summary

Phase 8 is a cross-cutting integration phase that touches every prior system to close the build-wish-grow loop. The four core workstreams are: (1) Save/Load with autosave on every state change, (2) Ambient soundscape via procedural AudioStreamWav, (3) Title screen as new main scene entry point, and (4) Wish fulfillment celebration with chime + gold sparkle particles. All HUD elements (credits, happiness, population) already exist and update in real time.

The codebase is exceptionally well-structured for this phase. All state is centralized in Autoload singletons (GameEvents, EconomyManager, BuildManager, CitizenManager, WishBoard, HappinessManager) which persist across scenes. All cross-system communication uses pure C# event delegates via GameEvents. The procedural audio pattern (GenerateTone in PlacementFeedback.cs) and GPUParticles3D one-shot pattern are already established and can be directly reused.

**Primary recommendation:** Build SaveManager as a new Autoload singleton that subscribes to all state-change events for autosave triggers, serializes state to `user://save.json` using System.Text.Json with custom converters for Godot types, and provides Load/Clear methods for the title screen. The title screen replaces QuickTestScene as the main scene in project.godot.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Autosave on every state change (room placed, room demolished, wish fulfilled, citizen arrived, happiness changed) -- player can quit anytime and lose nothing
- Single autosave slot -- one station, one save (multiple slots deferred to v2 per QOLX-02)
- JSON format using System.Text.Json -- human-readable, easy to debug, matches existing data patterns
- Full state persisted: ring layout (segment occupancy, room types, sizes), citizens (names, colors, walkway positions, current wishes, visit state), happiness value, credits balance, unlocked blueprints
- Citizens resume at exact walkway positions -- truly seamless resume
- Corrupted/incompatible save files fall back to new station with a brief warning message -- no crash
- Simple title screen shown on launch: "Orbital Rings" title text over a dark background
- Two buttons: "Continue" (only shown if save exists) and "New Station"
- "New Station" requires confirmation dialog: "Start a new station? Your current station will be lost."
- No save preview on Continue button -- just the option to continue
- Minimal visual design -- text on dark, clean and fast to build
- Procedural space drone: soft, low-frequency hum with gentle modulation -- like the distant hum of a space station life support
- Procedurally generated (matches zero-asset audio pattern from PlacementFeedback.cs)
- Static drone -- does not change with station state (no evolving soundscape for v1)
- Continuous loop from game start
- Existing placement snap sound (523 Hz chime) is fine as-is -- no adjustment needed
- Simple mute toggle button/key for all audio
- Warm chime sound on every wish fulfillment -- distinct from placement chime (523 Hz), recognizable reward tone
- Same chime every time -- becomes the signature "reward sound" the player associates with progress
- Gold/yellow sparkle particle burst at the citizen's 3D position -- universal reward color, spatial feedback
- Combined with existing effects: badge pop animation + "+X%" floating text on happiness bar
- Total fulfillment feedback stack: chime + gold sparkles at citizen + badge pop + "+X%" text

### Claude's Discretion
- Ambient drone frequency, harmonics, and modulation parameters
- Wish fulfillment chime frequency and envelope (must feel warm and distinct from placement chime)
- Particle burst count, spread, size, and fade timing for wish fulfillment sparkles
- Mute toggle UI placement and key binding
- Title screen font, layout, button styling
- Save file location (user://saves/ or similar Godot convention)
- Autosave debouncing strategy (if rapid state changes occur)
- JSON serialization structure and schema
- Any additional HUD integration gaps discovered during implementation

### Deferred Ideas (OUT OF SCOPE)
- Multiple save slots (QOLX-02) -- v2 requirement
- Volume slider / settings panel -- v2
- Evolving ambient soundscape that changes with station state -- v2
- Background music / composed audio tracks -- v2
</user_constraints>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| System.Text.Json | .NET 8.0 built-in | Save file serialization/deserialization | Locked decision; zero additional dependencies, human-readable JSON output |
| AudioStreamWav | Godot 4.6 built-in | Procedural ambient drone + wish chime | Established project pattern (PlacementFeedback.cs uses this exact API) |
| GPUParticles3D | Godot 4.6 built-in | Gold sparkle particles for wish celebration | Established project pattern (demolish puff in PlacementFeedback.cs) |
| FileAccess | Godot 4.6 built-in | File I/O for save/load | Standard Godot file API with user:// path support |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| DirAccess | Godot 4.6 built-in | Check if save file exists | Title screen "Continue" button visibility |
| AudioStreamPlayer | Godot 4.6 built-in | Audio playback nodes | Ambient drone (looping) + wish chime (one-shot) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| System.Text.Json | Newtonsoft.Json | More features but adds NuGet dependency; export issues reported (godot#58844) |
| System.Text.Json | Godot.FileAccess.StoreVar | Godot binary format, not human-readable, harder to debug |
| AudioStreamWav loop | AudioStreamGenerator | Real-time synthesis but more complex buffer management; WAV loop is simpler for static drone |

## Architecture Patterns

### New Files to Create
```
scripts/
  Autoloads/
    SaveManager.cs      # Autoload singleton -- save/load orchestration
  Audio/
    AmbientDrone.cs     # Procedural ambient drone generator + player
    WishCelebration.cs  # Wish fulfillment chime + sparkle particles
  UI/
    TitleScreen.cs      # Title screen with Continue/New Station buttons
    MuteToggle.cs       # Simple mute toggle button
Scenes/
  TitleScreen/
    TitleScreen.tscn    # Title screen scene (new main scene)
```

### Pattern 1: SaveManager as Autoload Singleton
**What:** SaveManager follows the established Autoload singleton pattern (like GameEvents, EconomyManager). It subscribes to state-change events and serializes the entire game state to JSON.
**When to use:** For all save/load operations.
**Architecture:**
```
SaveManager (Autoload)
  |-- Subscribes to: RoomPlaced, RoomDemolished, WishFulfilled,
  |                   CitizenArrived, HappinessChanged, CreditsChanged,
  |                   BlueprintUnlocked
  |-- On any event: debounce, then serialize full state to user://save.json
  |-- SaveData class: plain C# POCO with all game state
  |-- Load(): deserialize JSON, restore all singleton state
  |-- Clear(): delete save file
  |-- HasSave(): check if save file exists
```

**Save data flow:**
```
State Change Event --> SaveManager.OnStateChanged()
  --> Debounce (0.5s Timer, restart on each event)
  --> CollectGameState() reads from all singletons
  --> JsonSerializer.Serialize(saveData)
  --> FileAccess.Open("user://save.json", Write)
  --> Write JSON string, close file
```

**Load data flow:**
```
TitleScreen "Continue" clicked
  --> SaveManager.Load()
  --> FileAccess.Open("user://save.json", Read)
  --> JsonSerializer.Deserialize<SaveData>(json)
  --> SaveManager.ApplyState(saveData) restores all singletons
  --> GetTree().ChangeSceneToFile("res://Scenes/QuickTest/QuickTestScene.tscn")
  --> Scene _Ready() methods pick up restored state
```

### Pattern 2: Debounced Autosave
**What:** A Timer child node that resets on every state change. Only when the timer actually fires (no state changes for 0.5s) does the save execute.
**When to use:** Prevents rapid-fire saves during batch operations (e.g., multiple citizens arriving at once, wish fulfillment chain).
**Example:**
```csharp
// Debounce pattern (established Timer pattern from EconomyManager)
private Timer _debounceTimer;

private void OnAnyStateChanged()
{
    // Reset timer -- save only fires after 0.5s of quiet
    _debounceTimer.Stop();
    _debounceTimer.Start();
}

private void OnDebounceFired()
{
    PerformSave();
}
```

### Pattern 3: Scene Transition with Autoload State Preservation
**What:** Autoloads persist across `GetTree().ChangeSceneToFile()` calls. State is loaded into Autoload singletons BEFORE transitioning to the game scene, so when scene nodes call `_Ready()`, they read the already-restored state.
**When to use:** Title screen to game scene transition on "Continue".
**Critical ordering:**
1. SaveManager.Load() restores all Autoload state
2. GetTree().ChangeSceneToFile() transitions to game scene
3. Game scene nodes enter tree, call _Ready(), read restored state from singletons

### Pattern 4: Procedural Audio Loop (Ambient Drone)
**What:** Generate a longer AudioStreamWav buffer (2-4 seconds) with a low-frequency waveform and set LoopMode to Forward for seamless continuous playback.
**When to use:** Ambient background audio that plays continuously during gameplay.
**Key API:**
```csharp
var stream = new AudioStreamWav();
stream.Format = AudioStreamWav.FormatEnum.Format16Bits;
stream.MixRate = 22050;
stream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
stream.LoopBegin = 0;
stream.LoopEnd = sampleCount;  // Loop entire buffer
stream.Data = pcmData;
```

### Pattern 5: Title Screen as Entry Point
**What:** A minimal Control-based scene that replaces QuickTestScene as the main scene in project.godot.
**Architecture:**
```
TitleScreen (Control)
  |-- Title Label ("Orbital Rings")
  |-- VBoxContainer
  |     |-- Continue Button (visible only if SaveManager.HasSave())
  |     |-- New Station Button
  |-- Confirmation Dialog (hidden, shown on "New Station" if save exists)
```

### Anti-Patterns to Avoid
- **Serializing Godot types directly:** System.Text.Json cannot serialize Godot.Color, Godot.Vector3, etc. Use plain C# types (float r/g/b/a, float x/y/z) in the SaveData class.
- **Saving in _Process:** Never save every frame. Use event-driven + debounce.
- **Reading state from scene nodes during save:** Read only from Autoload singletons. Scene nodes may be mid-transition.
- **Changing scene before state is loaded:** The Load -> ChangeScene ordering is critical. If reversed, scene _Ready() reads default (empty) state.
- **Shared AudioStreamPlayer for different sounds:** Ambient drone and wish chime must use separate AudioStreamPlayer instances to avoid cutting each other off.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| JSON serialization | Custom string builder | System.Text.Json | Edge cases (escaping, encoding, null handling) are handled correctly |
| Audio looping | Manual buffer restart in _Process | AudioStreamWav.LoopMode = Forward | Engine handles seamless loop transition at sample level |
| File I/O | System.IO.File with manual path resolution | Godot FileAccess with user:// | user:// resolves correctly on all platforms; System.IO doesn't understand Godot paths |
| Scene transition | Manual node tree manipulation | GetTree().ChangeSceneToFile() | Correctly handles cleanup, signals, and Autoload preservation |
| Debounce timer | Manual delta accumulation | Timer child node | Established project pattern (EconomyManager, HappinessManager both use Timer) |

**Key insight:** The project already has every building block needed. PlacementFeedback.cs has GenerateTone() and GPUParticles3D patterns. All Autoloads have the singleton Instance pattern. The Timer debounce pattern exists in EconomyManager. This phase is assembly, not invention.

## Common Pitfalls

### Pitfall 1: System.Text.Json Cannot Serialize Godot Structs
**What goes wrong:** Attempting to serialize a SaveData class containing Godot.Color, Godot.Vector3, or any Godot struct throws `NotSupportedException`.
**Why it happens:** Godot structs are not annotated with `[Serializable]` and have no default parameterless constructors that System.Text.Json can use.
**How to avoid:** Use plain C# types in SaveData. Store colors as `float R, G, B, A` fields. Store positions as `float Angle` (citizens only need their walkway angle). Store enums as strings or ints.
**Warning signs:** `JsonException` or `NotSupportedException` during serialization.

### Pitfall 2: Autoload Initialization Order on Load
**What goes wrong:** Loading save data into Autoloads works, but when the game scene loads, some systems reinitialize and overwrite the loaded state.
**Why it happens:** Autoload _Ready() runs once at app start, but scene node _Ready() runs every time the scene enters the tree. Systems like CitizenManager.SpawnStarterCitizens(5) in _Ready() would spawn new citizens on top of loaded ones.
**How to avoid:** Add a `bool _stateLoaded` flag checked in initialization paths. When SaveManager has loaded state, scene systems skip their default initialization. Alternatively, SaveManager can set state AFTER the scene is loaded, using a call_deferred or one-frame delay.
**Warning signs:** Duplicate citizens, wrong credit balance, double-spawned rooms.

### Pitfall 3: BuildManager._placedRooms Is Private
**What goes wrong:** SaveManager cannot read the placed rooms dictionary to serialize it.
**Why it happens:** _placedRooms is `private readonly Dictionary<int, PlacedRoom>` and PlacedRoom is a private record.
**How to avoid:** Add public read-only API to BuildManager: `GetAllPlacedRooms()` returning serializable data. Similarly add `RestorePlacedRoom()` method for loading. Same pattern needed for other private state.
**Warning signs:** Compilation errors when SaveManager tries to access private fields.

### Pitfall 4: Citizen State Is Complex
**What goes wrong:** Citizens have many runtime fields: angle, direction, speed, bob phase, current wish, visit state, timers. Serializing all of them exactly is fragile.
**Why it happens:** CitizenNode mixes persistent state (identity, angle, wish) with ephemeral state (tween progress, timer countdown).
**How to avoid:** Serialize only the persistent subset: name, body type, primary color, secondary color, current angle, direction, current wish ID (or null). Ephemeral state (bob phase, speed variation, timer state) can be re-randomized on load -- the player won't notice.
**Warning signs:** Citizens appearing stuck in walls, mid-tween positions, or with wrong facing after load.

### Pitfall 5: File Access Requires Explicit Disposal in C#
**What goes wrong:** FileAccess leaks if not disposed, especially on error paths.
**Why it happens:** Godot's C# FileAccess implements IDisposable but doesn't auto-dispose.
**How to avoid:** Always use `using var file = FileAccess.Open(...)` pattern. Check for null return (file open failed).
**Warning signs:** File lock errors on subsequent saves, or save file corruption.

### Pitfall 6: AudioStreamWav Loop Gap
**What goes wrong:** Audible click/pop at the loop point of the ambient drone.
**Why it happens:** The waveform doesn't end at zero crossing, creating a discontinuity.
**How to avoid:** Calculate buffer length to be an exact multiple of the waveform period. For a 60 Hz drone at 22050 Hz sample rate: period = 22050/60 = 367.5 samples. Use 368 * N samples for the buffer.
**Warning signs:** Periodic clicking in the ambient audio.

### Pitfall 7: Title Screen Must Handle No-Save and Corrupted-Save
**What goes wrong:** "Continue" button shown when no save exists, or game crashes on corrupted save.
**Why it happens:** Missing existence check, or no try/catch around deserialization.
**How to avoid:** SaveManager.HasSave() checks FileAccess.FileExists(). Load() wraps deserialization in try/catch, returning null on failure. TitleScreen shows warning and falls back to "New Station" on load failure.
**Warning signs:** Crash on first launch (no save file), crash on version mismatch.

### Pitfall 8: WishBoard._activeWishes and WishBoard._placedRoomTypes Are Private
**What goes wrong:** SaveManager cannot access wish board state for serialization.
**Why it happens:** Same private field access issue as BuildManager.
**How to avoid:** Add public API methods: `GetActiveWishes()` already exists (returns IReadOnlyDictionary). Need `RestoreActiveWishes()` and similar restore methods. Also need `GetPlacedRoomTypes()` for serialization.
**Warning signs:** Wishes lost on load, or wish nudge system broken after load.

### Pitfall 9: HappinessManager._unlockedRooms and _crossedMilestoneCount Are Private
**What goes wrong:** Save doesn't capture which blueprints are unlocked, so player loses progression on load.
**Why it happens:** Private state without public accessors.
**How to avoid:** Add `GetUnlockedRooms()` and `RestoreState(float happiness, HashSet<string> unlockedRooms, int milestoneCount)` to HappinessManager.
**Warning signs:** All rooms re-locked after loading a late-game save.

## Code Examples

### Save Data Schema (System.Text.Json POCO)
```csharp
// Plain C# class -- no Godot types, fully serializable
public class SaveData
{
    public int Version { get; set; } = 1;
    public int Credits { get; set; }
    public float Happiness { get; set; }
    public int CrossedMilestoneCount { get; set; }
    public List<string> UnlockedRooms { get; set; } = new();
    public List<SavedRoom> PlacedRooms { get; set; } = new();
    public List<SavedCitizen> Citizens { get; set; } = new();
    public Dictionary<string, string> ActiveWishes { get; set; } = new(); // citizenName -> wishId
}

public class SavedRoom
{
    public string RoomId { get; set; }
    public int AnchorIndex { get; set; }
    public int StartPos { get; set; }
    public int SegmentCount { get; set; }
    public bool IsOuter { get; set; }
    public int Cost { get; set; }
}

public class SavedCitizen
{
    public string Name { get; set; }
    public int BodyType { get; set; }  // enum as int
    public float PrimaryR { get; set; }
    public float PrimaryG { get; set; }
    public float PrimaryB { get; set; }
    public float SecondaryR { get; set; }
    public float SecondaryG { get; set; }
    public float SecondaryB { get; set; }
    public float WalkwayAngle { get; set; }
    public float Direction { get; set; }
    public string CurrentWishId { get; set; }  // null if no wish
}
```

### Procedural Ambient Drone Generation
```csharp
// Extend GenerateTone pattern from PlacementFeedback.cs
// Low-frequency layered sine waves with gentle modulation
private static AudioStreamWav GenerateAmbientDrone(
    float baseFreq = 60f,   // Low hum
    float duration = 4.0f,   // Long buffer for smooth loop
    int sampleRate = 22050)
{
    // Ensure buffer is exact multiple of base period for seamless loop
    float period = sampleRate / baseFreq;
    int periods = (int)(duration * baseFreq);
    int sampleCount = (int)(periods * period);

    byte[] data = new byte[sampleCount * 2];

    for (int i = 0; i < sampleCount; i++)
    {
        float t = (float)i / sampleRate;
        // Fundamental + soft harmonics
        float sample = MathF.Sin(2 * MathF.PI * baseFreq * t) * 0.4f;
        sample += MathF.Sin(2 * MathF.PI * baseFreq * 1.5f * t) * 0.15f;  // Fifth
        sample += MathF.Sin(2 * MathF.PI * baseFreq * 2f * t) * 0.1f;     // Octave
        // Gentle amplitude modulation (slow wobble)
        float mod = 0.85f + 0.15f * MathF.Sin(2 * MathF.PI * 0.3f * t);
        sample *= mod * 0.3f;  // Keep quiet

        short pcm = (short)(sample * short.MaxValue);
        data[i * 2] = (byte)(pcm & 0xFF);
        data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
    }

    var stream = new AudioStreamWav();
    stream.Format = AudioStreamWav.FormatEnum.Format16Bits;
    stream.MixRate = sampleRate;
    stream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
    stream.LoopBegin = 0;
    stream.LoopEnd = sampleCount;
    stream.Data = data;
    return stream;
}
```

### Wish Celebration Chime (Warm Tone)
```csharp
// Distinct from placement chime (523 Hz C5)
// Use G4 (392 Hz) with soft overtones for warmth
private static AudioStreamWav GenerateWishChime(int sampleRate = 22050)
{
    float duration = 0.5f;  // Longer than placement chime (0.15s)
    int sampleCount = (int)(sampleRate * duration);
    byte[] data = new byte[sampleCount * 2];

    for (int i = 0; i < sampleCount; i++)
    {
        float t = (float)i / sampleRate;
        float envelope = MathF.Exp(-3f * t);  // Exponential decay (warmer than linear)
        float sample = MathF.Sin(2 * MathF.PI * 392f * t) * 0.5f;   // G4 fundamental
        sample += MathF.Sin(2 * MathF.PI * 784f * t) * 0.2f;        // G5 octave
        sample += MathF.Sin(2 * MathF.PI * 587.33f * t) * 0.15f;    // D5 (perfect fifth)
        sample *= envelope * 0.5f;

        short pcm = (short)(sample * short.MaxValue);
        data[i * 2] = (byte)(pcm & 0xFF);
        data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
    }

    var stream = new AudioStreamWav();
    stream.Format = AudioStreamWav.FormatEnum.Format16Bits;
    stream.MixRate = sampleRate;
    stream.Data = data;
    return stream;
}
```

### Gold Sparkle Particles (Wish Celebration)
```csharp
// Reuse GPUParticles3D one-shot pattern from PlacementFeedback.OnDemolishConfirmed()
private void SpawnGoldSparkles(Vector3 citizenPosition)
{
    var particles = new GpuParticles3D();
    particles.OneShot = true;
    particles.Emitting = false;
    particles.Amount = 20;       // More than demolish puff (12)
    particles.Lifetime = 0.8f;   // Slightly longer for celebration feel
    particles.Explosiveness = 0.85f;

    var material = new ParticleProcessMaterial();
    material.Direction = new Vector3(0, 1, 0);
    material.Spread = 60f;       // Wider spread than demolish
    material.InitialVelocityMin = 1.5f;
    material.InitialVelocityMax = 3.5f;
    material.Gravity = new Vector3(0, -2f, 0);  // Lighter gravity for floaty feel
    material.ScaleMin = 0.03f;
    material.ScaleMax = 0.08f;
    material.Color = new Color(1.0f, 0.85f, 0.3f, 0.9f);  // Gold/yellow

    particles.ProcessMaterial = material;
    particles.DrawPass1 = new SphereMesh { Radius = 0.03f, Height = 0.06f };
    particles.Position = citizenPosition + new Vector3(0, 0.3f, 0);  // Above citizen center

    GetTree().Root.AddChild(particles);
    particles.Restart();
    particles.Emitting = true;

    // Self-cleanup (established pattern)
    particles.Finished += () =>
    {
        if (IsInstanceValid(particles))
            particles.QueueFree();
    };
}
```

### Save File I/O Pattern
```csharp
private const string SavePath = "user://save.json";

private void PerformSave()
{
    var saveData = CollectGameState();
    string json = JsonSerializer.Serialize(saveData, new JsonSerializerOptions
    {
        WriteIndented = true  // Human-readable for debugging
    });

    using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
    if (file == null)
    {
        GD.PushWarning($"SaveManager: Could not open {SavePath} for writing");
        return;
    }
    file.StoreString(json);
}

public SaveData Load()
{
    if (!FileAccess.FileExists(SavePath))
        return null;

    using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
    if (file == null) return null;

    try
    {
        string json = file.GetAsText();
        return JsonSerializer.Deserialize<SaveData>(json);
    }
    catch (JsonException ex)
    {
        GD.PushWarning($"SaveManager: Corrupted save file: {ex.Message}");
        return null;
    }
}

public bool HasSave() => FileAccess.FileExists(SavePath);

public void ClearSave()
{
    if (FileAccess.FileExists(SavePath))
        DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
}
```

### Title Screen Scene Transition
```csharp
// In TitleScreen.cs
private void OnContinuePressed()
{
    var saveData = SaveManager.Instance.Load();
    if (saveData == null)
    {
        // Corrupted/incompatible -- show warning, fall back
        ShowWarning("Save file could not be loaded. Starting a new station.");
        return;
    }
    SaveManager.Instance.ApplyState(saveData);
    GetTree().ChangeSceneToFile("res://Scenes/QuickTest/QuickTestScene.tscn");
}

private void OnNewStationPressed()
{
    if (SaveManager.Instance.HasSave())
    {
        // Show confirmation dialog (locked decision)
        _confirmDialog.Visible = true;
    }
    else
    {
        StartNewStation();
    }
}

private void StartNewStation()
{
    SaveManager.Instance.ClearSave();
    GetTree().ChangeSceneToFile("res://Scenes/QuickTest/QuickTestScene.tscn");
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Godot.Json.Stringify/Parse | System.Text.Json (C# native) | Godot 4.x / .NET 8 | Stronger typing, custom converters, no Godot runtime dependency for serialization |
| AudioStreamSample | AudioStreamWav | Godot 4.0 | Renamed class; same API, better naming |
| get_tree().change_scene() | GetTree().ChangeSceneToFile() | Godot 4.0 | Takes file path directly, deprecated old method |

## Existing Systems Requiring Modification

The following existing systems need public API additions for save/load access:

### BuildManager
- **Need:** `GetAllPlacedRooms()` returning list of serializable room data
- **Need:** `RestorePlacedRoom(roomId, row, startPos, segmentCount, cost)` to rebuild a room from save
- **Need:** `ClearAllRooms()` for "New Station" after existing save
- **Current private state:** `_placedRooms` dictionary, `PlacedRoom` record

### CitizenManager
- **Need:** `GetAllCitizens()` returning serializable citizen data with positions
- **Need:** `ClearCitizens()` to remove all citizens before loading
- **Need:** `SpawnCitizenFromSave(data, angle, direction, wishId)` to restore with exact state
- **Need:** Guard in `_Ready()` to skip `SpawnStarterCitizens(5)` when loading
- **Current private state:** `_citizens` list

### HappinessManager
- **Need:** `GetUnlockedRoomIds()` returning HashSet<string> copy
- **Need:** `RestoreState(happiness, unlockedRooms, milestoneCount, housingCapacity)` to restore progression
- **Need:** Guard in `_Ready()` to skip `InitializeHousingCapacity()` when loading
- **Current private state:** `_happiness`, `_unlockedRooms`, `_crossedMilestoneCount`, `_housingCapacity`

### EconomyManager
- **Need:** `RestoreCredits(int credits)` to set balance on load
- **Current:** `_credits` set only in `_Ready()` from config

### WishBoard
- **Need:** `RestoreActiveWishes(Dictionary<string, string>)` to restore citizen wishes
- **Current:** `GetActiveWishes()` already exists for read; need write path

### project.godot
- **Change:** `run/main_scene` from QuickTestScene to TitleScreen
- **Change:** Add SaveManager to `[autoload]` section

## Open Questions

1. **Citizen visit state on save**
   - What we know: Citizens can be mid-visit (inside a room, invisible) when autosave triggers
   - What's unclear: Should we save them as "visiting" or snap them back to walkway?
   - Recommendation: Snap to walkway angle on save. Simpler, and the player won't notice a 4-8 second visit reset. Avoids serializing complex tween state.

2. **Economy timer state on load**
   - What we know: EconomyManager has a 5.5s income timer. Loading resets it.
   - What's unclear: Should the timer resume from where it was?
   - Recommendation: Let it restart fresh. 5.5s is too short for the player to notice. Simplifies save data.

3. **BuildPanel locked room state on load**
   - What we know: BuildPanel filters rooms via HappinessManager.IsRoomUnlocked(). After load, HappinessManager needs restored unlocks before BuildPanel._Ready() runs.
   - What's unclear: Timing of restoration vs scene initialization.
   - Recommendation: SaveManager.ApplyState() restores HappinessManager state (including unlocked rooms) BEFORE calling ChangeSceneToFile(). BuildPanel._Ready() will then read the correct unlock state.

## Sources

### Primary (HIGH confidence)
- Codebase analysis of all 30+ source files in scripts/ directory -- architecture, patterns, and API surfaces directly inspected
- project.godot -- Autoload order, scene structure, engine version (4.6)
- QuickTestScene.tscn -- HUD layout, CanvasLayer structure, node hierarchy

### Secondary (MEDIUM confidence)
- [Godot AudioStreamWav docs](https://docs.godotengine.org/en/stable/classes/class_audiostreamwav.html) -- LoopMode, LoopBegin, LoopEnd properties
- [Godot file paths docs](https://docs.godotengine.org/en/stable/tutorials/io/data_paths.html) -- user:// path conventions
- [Godot FileAccess docs](https://docs.godotengine.org/en/stable/classes/class_fileaccess.html) -- C# disposal requirements
- [Godot Autoload/Singleton docs](https://docs.godotengine.org/en/stable/tutorials/scripting/singletons_autoload.html) -- persistence across scene changes
- [Godot scene change docs](https://docs.godotengine.org/en/stable/tutorials/scripting/change_scenes_manually.html) -- ChangeSceneToFile API
- [Lightweight Save/Load in Godot 4 C# (Medium)](https://medium.com/@romain.mouillard.fr/lightweight-saving-loading-system-in-godot-4-with-c-a-practical-guide-2cb6cbd2faa3) -- System.Text.Json patterns
- [Godot proposals #8335](https://github.com/godotengine/godot-proposals/issues/8335) -- Godot types not serializable, confirmed need for custom POCOs
- [Chickensoft serialization blog](https://chickensoft.games/blog/serialization-for-csharp-games) -- C# game serialization patterns

### Tertiary (LOW confidence)
- None -- all findings verified against codebase or official docs

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- all libraries already used in project or .NET built-in
- Architecture: HIGH -- directly extends established patterns visible in codebase
- Pitfalls: HIGH -- identified by reading actual private field visibility in source code
- Save/Load schema: MEDIUM -- schema design is sound but serialization edge cases with Godot types need runtime validation
- Audio parameters: MEDIUM -- frequency/harmony choices are artistic; will need tuning by ear

**Research date:** 2026-03-04
**Valid until:** 2026-04-04 (stable domain, no fast-moving dependencies)
