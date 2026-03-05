# Phase 13: HUD Replacement - Context

**Gathered:** 2026-03-05
**Status:** Ready for planning

<domain>
## Phase Boundary

Replace the old HappinessBar (single percentage bar, now broken since Phase 10 removed HappinessChanged emissions) with two new HUD elements: a lifetime wish counter showing "♥ N" with pulse animation, and a mood tier label with tier-colored text. Delete all deprecated happiness display code. Tier change notification (MCOM-01) included as lightweight addition.

</domain>

<decisions>
## Implementation Decisions

### Tier Colors
- Warm spectrum progression: cool-to-warm as tiers rise (Quiet=soft grey-blue, up to Radiant=bright gold)
- Claude picks specific RGB values that harmonize with existing HUD palette (gold star credits, mint smiley population, coral heart)
- Tier label shows name only — no emoji or symbol prefix (e.g., "Cozy" not "☆ Cozy")
- Color cross-fade (~0.3s tween) on tier transition, not instant snap

### Animation Feel
- Wish counter pulse: scale bounce (brief scale-up + elastic settle), same pattern as PopulationDisplay citizen arrival animation
- No floating "+1" text on wish fulfillment — scale bounce is sufficient (wish celebration chime + speech bubble already signal fulfillment)
- Tier change: color cross-fade + brief scale pop on the tier name label
- Include floating "Station mood: Lively" text on tier change (MCOM-01) — single FloatingText spawn, trivial alongside HUD work

### HUD Layout
- Replace HappinessBar in the same rightmost position in top-right cluster
- Layout order: Credits | Population | ♥ N | TierName
- One combined widget (single MarginContainer class, e.g., MoodHUD) containing both heart counter and tier label side-by-side
- Font size 20 for both wish counter number and tier label — matches CreditHUD and PopulationDisplay
- Heart icon reuses existing coral color (0.95, 0.55, 0.55) from HappinessBar
- Wish counter number uses warm white (0.95, 0.93, 0.90) matching other HUD text
- No tooltip on hover — tier label is self-explanatory

### Cleanup
- Delete HappinessBar.cs entirely and remove its node from QuickTestScene.tscn
- Remove deprecated `Happiness` property shim from HappinessManager (no consumers left)
- Remove `HappinessChanged` event and `EmitHappinessChanged` from GameEvents (no subscribers left)
- Keep `Happiness` field in SaveData — still needed for v1 save backward compatibility

### Claude's Discretion
- Exact RGB values for each tier in the warm spectrum (harmonize with existing palette)
- Exact scale bounce magnitude and timing (match PopulationDisplay feel)
- Exact cross-fade duration for tier color transition
- Floating text position and style for tier change notification
- Whether MoodHUD builds UI in _Ready() programmatically (following CreditHUD pattern) or uses a .tscn scene

</decisions>

<specifics>
## Specific Ideas

- The HUD cluster should feel uniform: all four elements (credits, population, wishes, mood) at the same font size and visual weight
- Tier colors should feel like "warming up" — Quiet is cool/muted, Radiant is warm/bright
- The scale bounce on wish fulfillment should feel satisfying but not distracting (cozy, not gamey)
- Tier change floating text is non-modal — it drifts and fades like existing FloatingText

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `FloatingText` (Scripts/UI/FloatingText.cs): Reusable drift-and-fade label — use for tier change notification
- `PopulationDisplay` (Scripts/UI/PopulationDisplay.cs): Scale bounce animation pattern to replicate for wish counter pulse
- `CreditHUD` (Scripts/UI/CreditHUD.cs): Programmatic UI building pattern in _Ready(), event subscription/unsubscription lifecycle
- Heart color constant `HeartColor = new(0.95f, 0.55f, 0.55f)` from HappinessBar — carry forward

### Established Patterns
- All HUD widgets are MarginContainer subclasses with programmatic child creation in _Ready()
- Kill-before-create tween pattern for animations (kill existing tween before starting new one)
- Event subscription in _Ready(), unsubscription in _ExitTree() (not _EnterTree — autoload init order)
- MouseFilter.Ignore on decorative labels to prevent blocking clicks

### Integration Points
- `GameEvents.WishCountChanged(int newCount)` — subscribe for wish counter updates
- `GameEvents.MoodTierChanged(MoodTier newTier, MoodTier previousTier)` — subscribe for tier label updates + notification
- `HappinessManager.LifetimeWishes` (int) — read for initial counter value on _Ready()
- `HappinessManager.CurrentTier` (MoodTier) — read for initial tier on _Ready()
- `QuickTestScene.tscn` HUDLayer node — remove HappinessBar, add new MoodHUD in its place
- `MoodTier` enum (Quiet=0..Radiant=4) — use for color lookup

</code_context>

<deferred>
## Deferred Ideas

- MCOM-02: Tier label pulse/glow when mood is near the top of its range (about to promote) — future milestone
- Mood tooltip showing tier progression or mood details — explicitly declined for now

</deferred>

---

*Phase: 13-hud-replacement*
*Context gathered: 2026-03-05*
