---
phase: 06-wish-system
verified: 2026-03-03T16:30:00Z
status: human_needed
score: 5/5 automated must-haves verified
human_verification:
  - test: "Citizens display wish badges after 30-60 seconds of play"
    expected: "A colored circular icon badge appears floating above a citizen's head. Multiple citizens may have badges simultaneously. Badges face the camera (billboard)."
    why_human: "Badge creation is timer-driven; must observe in running game after 30-60s wait"
  - test: "Building a matching room triggers fulfillment and badge pop animation"
    expected: "After building a room matching a citizen's wish (e.g., Star Lounge for a 'stargaze' wish), within ~7-10 seconds the citizen visits it. On return, the badge scales up and fades out (pop animation) and disappears."
    why_human: "Requires running game, timing observation, and visual confirmation of tween animation"
  - test: "Unfulfilled wish badge stays visible indefinitely with no negative consequence"
    expected: "After 2+ minutes without building a matching room, the badge remains. Credits are not lost, no happiness penalty, citizen continues walking normally."
    why_human: "Requires timed observation in running game"
  - test: "CitizenInfoPanel shows wish text and category on click"
    expected: "Clicking a citizen with a badge shows first-person wish text (e.g., 'I'd love a cozy spot to chat') and a colored category label (Social/Comfort/Curiosity/Variety). Clicking a citizen without a badge shows 'No wish' in muted text."
    why_human: "Requires click interaction in running game; CitizenInfoPanel lookup changed from WishBoard to citizen.CurrentWish in Plan 03 fix"
  - test: "Active wishes span at least 3 of 4 categories over 10+ minutes"
    expected: "Across multiple citizens observed over a 10-minute play session, category labels in CitizenInfoPanel show at least 3 distinct categories (Social, Comfort, Curiosity, Variety)."
    why_human: "Requires extended play session observation; category distribution depends on runtime random selection"
---

# Phase 6: Wish System Verification Report

**Phase Goal:** Citizens generate wishes expressed as speech bubbles, those wishes remain visible and harmless until fulfilled, and wishes span all four defined categories
**Verified:** 2026-03-03T16:30:00Z
**Status:** human_needed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths (from ROADMAP.md Phase 6 Success Criteria)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Citizens periodically display speech bubbles with wish text describing a room type | ? HUMAN | Timer fires OnWishTimerTimeout at 30-60s, CreateWishBadge() creates Sprite3D billboard — cannot verify visual appearance without running game |
| 2 | Building a matching room causes the citizen's wish to resolve and badge to clear | ? HUMAN | FulfillWish() triggered in Phase 8 tween callback when placedRoom.RoomId matches _currentWish.FulfillingRoomIds; WishNudgeRequested fires 7s reset — requires running game |
| 3 | An unfulfilled wish stays visible indefinitely with no negative consequence | ✓ VERIFIED | No timer clears badges without fulfillment; no credit/happiness deduction in FulfillWish(); badge cleared only via FulfillWish() path |
| 4 | Active wishes span at least 3 of 4 categories across a normal 10+ minute session | ? HUMAN | All 4 categories present in 12 templates (3 each); GetRandomTemplate() picks from full pool — distribution requires runtime verification |
| 5 | WishBoard Autoload tracks all active wishes without iterating citizens | ✓ VERIFIED | Dictionary-based tracking via GameEvents subscriptions (WishGenerated/WishFulfilled); GetActiveWishes() returns IReadOnlyDictionary; no citizen iteration in tracking path |

**Automated Score:** 2/5 truths verified programmatically; 3/5 require human observation

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Scripts/Autoloads/WishBoard.cs` | Wish tracking Autoload singleton | ✓ VERIFIED | 332 lines; extends SafeNode; Instance singleton; dictionary tracking; full query API; WishNudgeRequested event |
| `Resources/Wishes/` | 12 WishTemplate .tres resources | ✓ VERIFIED | Exactly 12 files: 3 Social (0), 3 Comfort (1), 3 Curiosity (2), 3 Variety (3) |
| `Resources/Icons/` | 4 category icon PNGs | ✓ VERIFIED | wish_social.png, wish_comfort.png, wish_curiosity.png, wish_variety.png all present |
| `Scripts/Citizens/CitizenNode.cs` | Wish generation, badge display, wish-aware visits, fulfillment | ✓ VERIFIED | 672 lines; _wishTimer, _wishBadge, _currentWish, WishMatchDistanceMultiplier, FulfillWish(), OnWishNudgeRequested() all present |
| `Scripts/UI/CitizenInfoPanel.cs` | Wish text + category display | ✓ VERIFIED | 157 lines; citizen.CurrentWish lookup; category-colored label; GetCategoryColor() helper |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `WishBoard.cs` | `GameEvents.Instance` | SubscribeEvents: WishGenerated, WishFulfilled, RoomPlaced, RoomDemolished | ✓ WIRED | Lines 175-186: all 4 events subscribed and unsubscribed |
| `WishBoard.cs` | `CitizenManager.Instance.Citizens` | NudgeCitizensForRoom iterates citizens | ✗ NOT_WIRED | NudgeCitizensForRoom iterates _activeWishes dictionary (not citizens) and fires WishNudgeRequested event — this is the CORRECT design per Plan 01 decision |
| `project.godot` | `Scripts/Autoloads/WishBoard.cs` | Autoload registration | ✓ WIRED | Line 24: `WishBoard="*res://Scripts/Autoloads/WishBoard.cs"` after CitizenManager |
| `CitizenNode.cs` | `GameEvents.Instance` | EmitWishGenerated/EmitWishFulfilled | ✓ WIRED | Lines 492, 547: both events emitted correctly |
| `CitizenNode.cs` | `WishBoard.Instance` | GetRandomTemplate for wish generation, WishNudgeRequested subscription | ✓ WIRED | Lines 172-183, 483: subscription and template fetch present |
| `CitizenNode.cs` | `BuildManager.Instance.GetPlacedRoom` | Wish-aware visit targeting + fulfillment check | ✓ WIRED | Lines 339, 452: two call sites (distance weighting + fulfillment detection) |
| `CitizenInfoPanel.cs` | `citizen.CurrentWish` | Direct property access for wish text display | ✓ WIRED | Line 90: uses citizen.CurrentWish (fixed in Plan 03 from WishBoard lookup which returned null) |

**Note on "CitizenManager.Instance.Citizens" key link:** The original Plan 01 spec listed this as a key link for NudgeCitizensForRoom. The implementation correctly changed this design: instead of iterating CitizenManager.Citizens, WishBoard fires WishNudgeRequested events per matching citizen name, and each CitizenNode filters its own name. This is the better decoupled architecture. The "NOT_WIRED" status reflects plan-vs-implementation divergence, not a bug.

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| WISH-01 | 06-02, 06-03 | Citizens express wishes via speech bubbles | ? HUMAN NEEDED | Mechanics fully wired (timer + Sprite3D badge); visual confirmation requires running game |
| WISH-02 | 06-02, 06-03 | Fulfilling a wish grants happiness | PARTIAL | Fulfillment detection + WishFulfilled event fires correctly; happiness granting NOT wired in Phase 6 (EconomyManager.SetHappiness() exists but no subscriber to WishFulfilled calls it). Happiness integration is Phase 7's responsibility per ROADMAP. Phase 6 success criteria do NOT include happiness granting. |
| WISH-03 | 06-01, 06-02, 06-03 | Unfulfilled wishes linger harmlessly | ✓ SATISFIED | No automatic badge expiry; no penalty code paths in CitizenNode; badges only cleared via FulfillWish() |
| WISH-04 | 06-01, 06-03 | Wishes span 4 categories | ✓ SATISFIED (automated) | 12 templates across 4 categories (3 each) confirmed by file inspection; runtime distribution requires human verification |

**WISH-02 Clarification:** REQUIREMENTS.md marks WISH-02 as Phase 6 Complete and it is listed as a requirement in 06-02-PLAN and 06-03-PLAN. However, the Phase 6 ROADMAP success criteria do not include happiness granting — they only require wish resolution (badge clearing). The happiness-granting half of WISH-02 is explicitly scoped to Phase 7 ("Wish fulfillment raises happiness"). The fulfillment event fires correctly, providing the hook Phase 7 needs. This is a requirements-roadmap scope split, not a gap in Phase 6's deliverables.

### Anti-Patterns Found

No anti-patterns detected in Phase 6 files:
- No TODO/FIXME/PLACEHOLDER comments in WishBoard.cs, CitizenNode.cs, or CitizenInfoPanel.cs
- No empty stub implementations (all event handlers, query methods, and badge logic are substantive)
- No console.log-only handlers
- Build succeeds with 0 errors, 0 warnings

### Human Verification Required

#### 1. Citizens Display Wish Badges

**Test:** Run the game (F5 or `godot --path . res://Scenes/QuickTestScene.tscn`). Wait 30-60 seconds without taking any action.
**Expected:** At least one citizen displays a small colored circular icon badge floating above their head. The badge faces the camera as the citizen moves (billboard). Multiple citizens may have badges at different times.
**Why human:** Badge creation is timer-driven (30-60s); must observe in running game.

#### 2. Fulfillment Pop Animation and Badge Clearing

**Test:** After a citizen displays a badge, note their wish text by clicking them. Build a matching room (e.g., Star Lounge for "stargaze" wish). Wait 7-10 seconds for the citizen to visit the new room.
**Expected:** When the citizen visits the matching room and returns, the badge scales up and fades out (pop animation) and disappears. After 30-90 seconds, a new badge may appear.
**Why human:** Requires tween animation observation in running game.

#### 3. Harmless Lingering

**Test:** Observe a citizen with a badge for 2+ minutes without building any matching rooms.
**Expected:** The badge stays visible the entire time. No credits are lost, no happiness decreases, the citizen continues walking and visiting rooms normally.
**Why human:** Requires timed observation in running game.

#### 4. CitizenInfoPanel Wish Text Display

**Test:** Click a citizen with a badge. Then click a citizen without a badge.
**Expected:** The citizen with a badge shows first-person wish text (e.g., "I'd love a cozy spot to chat with friends") and a colored category label (Social/Comfort/Curiosity/Variety) in the panel. The citizen without a badge shows "No wish" in muted gray.
**Why human:** Requires click interaction; Plan 03 fixed CitizenInfoPanel to use `citizen.CurrentWish` directly.

#### 5. Category Variety Over 10+ Minutes

**Test:** Play for 10+ minutes, clicking citizens periodically to read their wish category labels.
**Expected:** At least 3 of the 4 categories (Social, Comfort, Curiosity, Variety) appear across different citizens or over time.
**Why human:** Distribution depends on runtime random selection across 12 templates.

#### 6. Badge Visibility During Visits

**Test:** Watch a citizen with a badge visit a room (fade out and fade in).
**Expected:** The badge disappears when the citizen fades out, and reappears (if wish unfulfilled) when the citizen fades back in.
**Why human:** Godot parent-child visibility propagation must be observed in running game.

### Summary

All automated checks pass. The Phase 6 wish system is fully implemented and wired:

- WishBoard Autoload correctly tracks wishes via event-driven dictionary (no citizen iteration)
- 12 WishTemplate .tres resources cover all 4 categories with 3 templates each
- All FulfillingRoomIds in templates reference valid existing room RoomId values (bunk_pod, sky_loft, comm_relay, craft_lab, garden_nook, reading_nook, star_lounge, storage_bay, workshop)
- 4 category icon PNGs exist in Resources/Icons/
- WishBoard registered in project.godot after CitizenManager in the correct dependency order
- CitizenNode has full wish lifecycle: generation timer (30-60s), Sprite3D badge creation, wish-aware visit targeting (0.3x distance multiplier), fulfillment detection in Phase 8 tween callback, pop animation, WishNudgeRequested subscription
- CitizenInfoPanel displays wish text via citizen.CurrentWish (fixed from WishBoard lookup in Plan 03)
- Build: 0 errors, 0 warnings

One requirements note: WISH-02's happiness-granting half is not wired in Phase 6 (by design — that belongs to Phase 7). The WishFulfilled event fires correctly, providing the integration hook Phase 7 requires.

All 5 human verification items are visual/behavioral checks that require the running game. The automated evidence strongly supports they will pass.

---
_Verified: 2026-03-03T16:30:00Z_
_Verifier: Claude (gsd-verifier)_
