# PRD: Happiness System

**Status:** Draft
**Date:** 2026-03-04

## Problem

The current happiness system is a single percentage (0–100%) that only goes up.
Once a player reaches 100%, the system goes silent — wish fulfillment no longer
moves anything meaningful. For a cozy sandbox with no intended endpoint, this
creates a finish line where there shouldn't be one.

## Design

Replace the single percentage with two independent values:

| | Lifetime Happiness | Station Mood |
|---|---|---|
| **What it is** | Total wishes ever fulfilled | Current station energy |
| **Direction** | Only goes up | Fluctuates |
| **Cap** | None | Soft (bounded by wish rate) |
| **Display** | Counter: `♥ 47` | Named tier: *Lively* |
| **Drives** | Blueprint unlocks | Citizen arrivals, economy multiplier |

### Why Two Values

A single number can't be both "permanent achievement" and "current state" without
hitting a ceiling. Splitting them lets the player always have progress (Lifetime)
while keeping the station feel dynamic (Mood). A mature station that goes idle
won't lose its unlocks, but it will feel quieter — and come alive again when the
player returns to fulfilling wishes.

---

## Lifetime Happiness

An integer that increments by 1 each time any wish is fulfilled. Never decreases.

### Blueprint Unlocks

Milestones are now wish counts instead of percentages. Calibrated to match the
current unlock pacing (25% was ~wish 4, 60% was ~wish 12):

| Wishes Fulfilled | Unlock |
|---|---|
| 4 | sky_loft (Housing), craft_lab (Work) |
| 12 | star_lounge (Comfort), comm_relay (Utility) |

Future content can add milestones at any count without worrying about a 100% cap.

### UI

Displayed as a simple number next to a heart icon in the HUD: `♥ 47`

No bar, no percentage. The number always grows — there's always a "next wish."
On wish fulfillment, the counter ticks up with a brief warm pulse (same pattern
as the credit counter flash).

---

## Station Mood

A float that rises on wish fulfillment and gently decays toward a baseline
when idle. The baseline itself rises with Lifetime Happiness, so a mature
station never feels empty.

### Mood Gain

When a wish is fulfilled:

```
mood += MoodGainPerWish
```

A flat gain (no diminishing returns). The natural ceiling comes from the decay —
you can only fulfill wishes so fast, so mood stabilizes at a level proportional
to your current activity rate.

**Suggested value:** `MoodGainPerWish = 3.0`

### Mood Decay

Every frame, mood drifts toward its baseline:

```
mood += (baseline - mood) * DecayRate * delta
```

- If mood > baseline: mood drifts down (idle station cools off)
- If mood < baseline: mood drifts up (mature station has a warm floor)

**Suggested value:** `DecayRate = 0.02` (half-life of ~35 seconds)

This means a mood spike from fulfilling a wish visibly fades over about a minute,
which matches the cozy pacing. Active players who fulfill wishes regularly will
sustain a high mood. Idle players will see their mood settle to the baseline — not
zero, just calm.

### Baseline

The floor that mood decays toward. Rises with Lifetime Happiness so that a
station with many fulfilled wishes is always at least a little warm:

```
baseline = BaselineFactor * sqrt(lifetimeHappiness)
```

**Suggested value:** `BaselineFactor = 1.0`

| Lifetime Happiness | Baseline | Resting Tier |
|---|---|---|
| 0 | 0.0 | Quiet |
| 4 | 2.0 | Cozy |
| 16 | 4.0 | Cozy |
| 36 | 6.0 | Lively |
| 100 | 10.0 | Vibrant |

The square root means early wishes raise the floor noticeably, while later
wishes raise it more gradually. A station with 100 fulfilled wishes will rest
at *Vibrant* even when the player is idle.

### Mood Tiers

Named tiers give the station a personality without exposing raw numbers:

| Tier | Mood Range | Citizen Arrival Scale | Economy Multiplier |
|---|---|---|---|
| **Quiet** | 0 – 1.9 | 0.0 (no arrivals) | 1.0x |
| **Cozy** | 2.0 – 4.9 | 0.2 | 1.1x |
| **Lively** | 5.0 – 9.9 | 0.4 | 1.2x |
| **Vibrant** | 10.0 – 17.9 | 0.6 | 1.3x |
| **Radiant** | 18.0+ | 0.8 | 1.4x |

- **Citizen arrival probability** per check: `ArrivalScale × ArrivalProbabilityBase`
  (preserves the existing ~60s check interval)
- **Economy multiplier** applied the same way as today, just keyed to tier
  instead of raw happiness value
- **Radiant** is intentionally hard to sustain — it requires active wish
  fulfillment on a station with a high baseline. It's the "everything is
  clicking" state, not a permanent achievement

### UI

Displayed as the tier name in the HUD, with a color that matches the tier:

| Tier | Color |
|---|---|
| Quiet | Soft gray |
| Cozy | Warm coral |
| Lively | Sunny amber |
| Vibrant | Bright gold |
| Radiant | Soft white glow |

On tier change, a brief floating text drifts up: *"Station mood: Lively"*

The tier label gently pulses or glows when mood is near the top of its range
(about to promote) — a subtle visual hint that the station is on the verge of
shifting.

---

## What Changes From v1

| Concern | v1 (Current) | v2 (New) |
|---|---|---|
| Data model | Single `float _happiness` (0–1) | `int _lifetimeHappiness` + `float _mood` |
| Blueprint unlocks | % thresholds (0.25, 0.60) | Wish-count thresholds (4, 12) |
| Citizen arrivals | `happiness × 0.6` | Tier-based arrival scale |
| Economy multiplier | `1 + (happiness × 0.3)` | Tier-based multiplier |
| Display | Percentage bar `♥ 63%` | Counter `♥ 47` + tier label *Lively* |
| Ceiling | 100% | None |
| Decay | Never | Mood decays toward rising baseline |
| After cap | System goes quiet | Mood keeps fluctuating forever |

---

## Cozy Promise (Updated)

The original promise was "happiness never goes down." The new promise:

> **Your achievements are permanent. Your station's energy is alive.**
>
> Lifetime Happiness never decreases — every wish you've ever fulfilled counts
> forever. Station Mood breathes with your activity: it rises when you're
> engaged and settles to a warm baseline when you step away. A mature station
> is never empty — it's just waiting for you to light it up again.

Mood decay is gentle, not punitive. The baseline ensures a well-loved station
always feels at least *Cozy*. The player is never punished for stepping away —
they just have the joy of watching the mood climb when they return.

---

## Save Migration

For existing v1 saves:

- `_happiness` (0.0–1.0) maps to an estimated `lifetimeHappiness` by
  inverting the diminishing returns formula: `wishes ≈ (happiness / HappinessGainBase) × (1 + happiness)`
- Initial `mood` is set to the baseline derived from the estimated lifetime
- Unlocked rooms and milestone count carry over unchanged
- Housing capacity unchanged

---

## Open Questions

- **Should the mood tier name be visible at all times**, or only appear on
  change (like a notification) and otherwise be shown as ambient visual
  treatment (station glow, ambient sound shift)?
- **Should there be more unlock milestones** now that Lifetime Happiness is
  unbounded? (e.g., cosmetic unlocks at wish 30, 50, 100)
- **Exact tuning values** (MoodGainPerWish, DecayRate, BaselineFactor, tier
  thresholds) will need playtesting — the numbers in this doc are starting
  points calibrated against the current ~60s arrival check and 20–25 minute
  session length
