# Economy Balance Spreadsheet

All economy numbers calibrated here before any code changes. This is the single source of truth for
EconomyConfig defaults, cost formulas, and income curves.

---

## Core Parameters

| Parameter               | Value  | Rationale                                          |
|-------------------------|--------|----------------------------------------------------|
| BaseStationIncome       | 1.0    | Trickle at 0 citizens so the player is never stuck |
| PassiveIncomePerCitizen | 2.0    | Coefficient applied to sqrt(citizenCount)          |
| WorkBonusMultiplier     | 1.25   | Small per-worker bonus (down from 1.5)             |
| HappinessMultiplierCap  | 1.3    | Subtle cap (down from 2.0) to prevent runaway      |
| IncomeTickInterval      | 5.5s   | Seconds between income ticks                       |
| BaseRoomCost            | 100    | Anchor cost for all room calculations              |
| SizeDiscountFactor      | 0.92   | ~8% discount per additional segment (down from 0.85)|
| OuterRowCostMultiplier  | 1.1    | Outer segments physically larger, cost 10% more    |
| DemolishRefundRatio     | 0.5    | 50% refund on demolish                             |
| StartingCredits         | 750    | Affords 5-6 cheap rooms (see validation below)     |

### Category Cost Multipliers

| Category    | Multiplier | Rationale                                  |
|-------------|------------|--------------------------------------------|
| Housing     | 0.70       | Cheapest -- essential for onboarding       |
| LifeSupport | 0.85       | Slightly more than Housing, still cheap    |
| Work        | 1.00       | Baseline                                   |
| Comfort     | 1.15       | Premium feel, unlocked later               |
| Utility     | 1.30       | Most expensive, specialized infrastructure |

---

## Income Table (per tick)

**Formula:** `tickIncome = round(baseStationIncome + perCitizen * sqrt(citizenCount) + workBonus) * happinessMult)`

Where:
- `baseStationIncome` = 1
- `perCitizen` = 2.0
- `workBonus` = workerCount * WorkBonusMultiplier (1.25)
- `happinessMult` = 1.0 + (happiness * (cap - 1.0)) = 1.0 + (happiness * 0.3)

### Citizen Milestones: Income at Various Happiness Levels

Assumptions for work bonus: ~30% of citizens are workers (rounded down).

| Citizens | sqrt(n) | Base + Citizen Income | Workers | Work Bonus | Raw Total | Hap=0.0 (x1.0) | Hap=0.5 (x1.15) | Hap=1.0 (x1.3) |
|----------|---------|----------------------|---------|------------|-----------|-----------------|------------------|-----------------|
| 0        | 0.00    | 1 + 0.00 = 1.00     | 0       | 0.00       | 1.00      | **1**           | **1**            | **1**           |
| 5        | 2.24    | 1 + 4.47 = 5.47     | 1       | 1.25       | 6.72      | **7**           | **8**            | **9**           |
| 15       | 3.87    | 1 + 7.75 = 8.75     | 4       | 5.00       | 13.75     | **14**          | **16**           | **18**          |
| 30       | 5.48    | 1 + 10.95 = 11.95   | 9       | 11.25      | 23.20     | **23**          | **27**           | **30**          |

**Key observations:**
- At 0 citizens the player earns 1 credit per tick (5.5 seconds). Slow but never zero.
- At 5 citizens with moderate happiness, income is ~8/tick -- enough to buy a cheap room every ~9 ticks (~50 seconds).
- At 30 citizens with max happiness, income is 30/tick -- a Comfort room every ~4 ticks (~22 seconds).
- The curve flattens thanks to sqrt: 6x more citizens (5 to 30) only yields ~3.5x more income.

---

## Cost Table

**Formula:** `cost = round(baseCost * categoryMult * segments * (sizeDiscount ^ (segments-1)) * rowMult)`

Where:
- `baseCost` = 100 (or BaseCostOverride if set on the RoomDefinition)
- `sizeDiscount` = 0.92
- `rowMult` = 1.0 (inner) or 1.1 (outer)

### Inner Row (rowMult = 1.0)

| Category     | Mult | 1-seg | 2-seg              | 3-seg                |
|-------------|------|-------|--------------------|----------------------|
| Housing     | 0.70 | 70    | 129 (200*0.92*0.7) | 178 (300*0.846*0.7)  |
| LifeSupport | 0.85 | 85    | 156 (200*0.92*0.85)| 216 (300*0.846*0.85) |
| Work        | 1.00 | 100   | 184 (200*0.92*1.0) | 254 (300*0.846*1.0)  |
| Comfort     | 1.15 | 115   | 212 (200*0.92*1.15)| 292 (300*0.846*1.15) |
| Utility     | 1.30 | 130   | 239 (200*0.92*1.3) | 330 (300*0.846*1.3)  |

Detailed calculation for 2-seg: `100 * 2 * 0.92^1 * categoryMult * 1.0` = `184 * categoryMult`
Detailed calculation for 3-seg: `100 * 3 * 0.92^2 * categoryMult * 1.0` = `253.92 * categoryMult`

### Outer Row (rowMult = 1.1)

| Category     | Mult | 1-seg | 2-seg | 3-seg |
|-------------|------|-------|-------|-------|
| Housing     | 0.70 | 77    | 142   | 196   |
| LifeSupport | 0.85 | 94    | 172   | 237   |
| Work        | 1.00 | 110   | 202   | 279   |
| Comfort     | 1.15 | 127   | 233   | 321   |
| Utility     | 1.30 | 143   | 263   | 363   |

Calculation: same as inner row, multiplied by 1.1 and rounded.

---

## Starting Credits Validation

**StartingCredits = 750**

Can the player afford 5-6 cheap 1-segment rooms at game start?

| Purchase                  | Cost | Running Total | Remaining |
|--------------------------|------|---------------|-----------|
| 1x Housing (inner, 1-seg) | 70  | 70            | 680       |
| 2x Housing (inner, 1-seg) | 70  | 140           | 610       |
| 3x Housing (inner, 1-seg) | 70  | 210           | 540       |
| 1x LifeSupport (inner)    | 85  | 295           | 455       |
| 2x LifeSupport (inner)    | 85  | 380           | 370       |
| 3x LifeSupport (inner)    | 85  | 465           | 285       |

After 3 Housing + 3 LifeSupport (6 rooms): **285 credits remaining.**
Enough for one more Work room (100) or nearly two more Housing rooms.

**Alternative opening:** 5 Housing (350) + 2 LifeSupport (170) = 520 spent, 230 remaining. Affordable and flexible.

**Verdict:** 750 starting credits allows the player to establish a viable station foundation (housing + life support) with budget left for their first Work or Comfort room. This avoids the "stuck at start" problem while not giving so much that early income feels irrelevant.

---

## Accumulation Curves

How long to afford various rooms at each citizen milestone?

Tick interval = 5.5 seconds. Assumes moderate happiness (x1.15 multiplier, happiness = 0.5).

| Milestone | Income/Tick | Housing 1s (70) | LifeSupport 1s (85) | Work 1s (100) | Comfort 2s (212) | Utility 3s (330) |
|-----------|------------|------------------|---------------------|---------------|------------------|------------------|
| 0 cit     | 1/tick     | 70 ticks (385s, **6.4 min**) | 85 ticks (468s, **7.8 min**) | 100 ticks (550s, **9.2 min**) | 212 ticks (1166s, **19.4 min**) | 330 ticks (1815s, **30.3 min**) |
| 5 cit     | 8/tick     | 9 ticks (50s, **0.8 min**)   | 11 ticks (61s, **1.0 min**)  | 13 ticks (72s, **1.2 min**)  | 27 ticks (149s, **2.5 min**)  | 42 ticks (231s, **3.9 min**)  |
| 15 cit    | 16/tick    | 5 ticks (28s)                | 6 ticks (33s)                | 7 ticks (39s)               | 14 ticks (77s, **1.3 min**)  | 21 ticks (116s, **1.9 min**)  |
| 30 cit    | 27/tick    | 3 ticks (17s)                | 4 ticks (22s)                | 4 ticks (22s)               | 8 ticks (44s)                | 13 ticks (72s, **1.2 min**)  |

**Pacing feels:**
- **0 citizens:** Very slow. The player spends their starting credits and must wait. Income trickle prevents total stall.
- **5 citizens:** Comfortable. A new cheap room every minute. The player starts feeling momentum.
- **15 citizens:** Active. Rooms come fast enough to plan multi-room expansions.
- **30 citizens:** Flowing. Even expensive 3-segment Utility rooms take just over a minute. The player is optimizing, not waiting.

---

## Runaway Validation

The research phase flagged that a positive feedback loop (more citizens -> more income -> more rooms -> more citizens) can go exponential without guards.

### Guard 1: sqrt scaling on citizen income

| Citizens | Linear Income (2.0 * n) | Sqrt Income (2.0 * sqrt(n)) | Ratio (sqrt/linear) |
|----------|-------------------------|------------------------------|---------------------|
| 5        | 10.0                    | 4.47                         | 0.45                |
| 15       | 30.0                    | 7.75                         | 0.26                |
| 30       | 60.0                    | 10.95                        | 0.18                |

Sqrt scaling provides strong diminishing returns. At 30 citizens, income is only 18% of what linear scaling would give.

### Guard 2: Happiness multiplier cap at 1.3x

| Happiness | Multiplier (cap=1.3) | Multiplier (old cap=2.0) |
|-----------|---------------------|--------------------------|
| 0.0       | 1.00                | 1.00                     |
| 0.5       | 1.15                | 1.50                     |
| 1.0       | 1.30                | 2.00                     |

The 1.3x cap means perfect happiness only adds 30% more income instead of doubling it. This keeps the feedback loop bounded.

### Guard 3: 30-citizen income vs 5-citizen income

**Test: 30-cit income must NOT exceed 10x the 5-cit income.**

- 5 citizens, happiness=1.0: **9** credits/tick
- 30 citizens, happiness=1.0: **30** credits/tick
- Ratio: 30/9 = **3.3x** -- well within the 10x safety threshold.

Even at maximum divergence (5-cit at happiness=0, 30-cit at happiness=1.0):
- 5 citizens, happiness=0.0: **7** credits/tick
- 30 citizens, happiness=1.0: **30** credits/tick
- Ratio: 30/7 = **4.3x** -- still safely under 10x.

### Conclusion

The combination of sqrt scaling + 1.3x happiness cap effectively prevents runaway growth. Income grows sublinearly with citizen count, and the happiness multiplier provides a modest bonus rather than an exponential amplifier. The economy should feel rewarding without becoming trivially fast at higher citizen counts.

---

## Demolish Refund Examples

DemolishRefundRatio = 0.5 (50%)

| Room                     | Build Cost | Refund |
|--------------------------|-----------|--------|
| Housing 1s inner         | 70        | 35     |
| LifeSupport 2s outer     | 172       | 86     |
| Comfort 3s inner         | 292       | 146    |
| Utility 1s outer         | 143       | 72     |

Refunds are meaningful enough to encourage experimentation without removing economic pressure.

---

*Last updated: 2026-03-03*
*Status: Calibrated -- ready for EconomyConfig implementation*
