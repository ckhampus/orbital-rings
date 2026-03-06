---
status: resolved
trigger: "The Zzz label still doesn't appear when a citizen goes home to rest"
created: 2026-03-06T22:00:00Z
resolved: 2026-03-06T23:30:00Z
---

## Root Cause (3 interacting bugs)

### Bug 1: HousingConfig never initialized
`HousingManager` is a **script autoload** (not a scene autoload), so `[Export]` properties
are never populated from the inspector. `Config` was always null, meaning `_homeTimer` was
never created in `CitizenNode.Initialize()` — citizens could never go home.

**Fix:** `Config ??= new HousingConfig()` in `HousingManager._Ready()`.

### Bug 2: Home timer not started on save restore
During save restore, `HousingManager._isRestoring = true` suppresses the `CitizenAssignedHome`
event. `CitizenNode.OnCitizenAssignedHome` (which starts the timer) was never called for
restored citizens.

**Fix:** `HomeSegmentIndex` setter calls `EnsureHomeTimer()` which lazily creates and starts
the timer by fetching `HousingManager.Instance.Config` at set-time.

### Bug 3: Wish guard blocked all home returns
`BEHV-04: active wish takes priority` in `OnHomeTimerTimeout()` prevented any citizen with an
active wish from going home. Since citizens almost always have wishes, no one ever went home.

**Fix:** Removed the wish guard — citizens go home regardless of active wishes.

### Architectural improvement: Zzz label moved to RingVisual
Original design had the Zzz label as a child of CitizenNode, which becomes invisible during
rest (Godot visibility propagation). Moved Zzz ownership to RingVisual (the room segment),
triggered by CitizenEnteredHome/CitizenExitedHome events. RingVisual is always visible.

## Files Changed

- `Scripts/Autoloads/HousingManager.cs` — Initialize default Config in _Ready
- `Scripts/Citizens/CitizenNode.cs` — HomeSegmentIndex setter with EnsureHomeTimer, removed wish guard, removed old Zzz code
- `Scripts/Ring/RingVisual.cs` — Added Zzz sleep indicator system (Label3D per segment, event-driven)
