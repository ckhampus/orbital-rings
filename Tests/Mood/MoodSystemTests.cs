namespace OrbitalRings.Tests.Mood;

using Chickensoft.GoDotTest;
using Godot;
using OrbitalRings.Data;
using OrbitalRings.Happiness;
using Shouldly;

public class MoodSystemTests : TestClass
{
    public MoodSystemTests(Node testScene) : base(testScene) { }

    private static MoodSystem CreateMoodSystem()
    {
        return new MoodSystem(new HappinessConfig());
    }

    // --- Decay Tests --- (MOOD-01)

    [Test]
    public void DecayMovesTowardBaseline()
    {
        var mood = CreateMoodSystem();
        mood.RestoreState(0.5f, 0.0f);

        mood.Update(60f, 0);

        mood.Mood.ShouldBe(0.4014f, 0.001f);
    }

    [Test]
    public void DecayConvergesToNonZeroBaseline()
    {
        // lifetimeHappiness=39 -> baseline = min(0.20, 0.016 * sqrt(39)) = ~0.0999
        // Update overwrites the RestoreState baseline with computed value
        var mood = CreateMoodSystem();
        mood.RestoreState(0.5f, 0.0f);

        mood.Update(60f, 39);

        mood.Mood.ShouldBe(0.4211f, 0.001f);
    }

    [Test]
    public void DecayWithLargeDelta()
    {
        var mood = CreateMoodSystem();
        mood.RestoreState(0.8f, 0.0f);

        mood.Update(300f, 0);

        mood.Mood.ShouldBe(0.2667f, 0.001f);
    }

    // --- Tier Transition Tests --- (MOOD-02)

    [Test]
    public void TierPromotesAtExactThreshold()
    {
        // RestoreState uses CalculateTierFromScratch, so setting mood at each
        // promote threshold should yield the corresponding tier.
        var mood = CreateMoodSystem();

        mood.RestoreState(0.10f, 0.0f);
        mood.CurrentTier.ShouldBe(MoodTier.Cozy);

        mood.RestoreState(0.30f, 0.0f);
        mood.CurrentTier.ShouldBe(MoodTier.Lively);

        mood.RestoreState(0.55f, 0.0f);
        mood.CurrentTier.ShouldBe(MoodTier.Vibrant);

        mood.RestoreState(0.80f, 0.0f);
        mood.CurrentTier.ShouldBe(MoodTier.Radiant);
    }

    [Test]
    public void TierDoesNotPromoteBelowThreshold()
    {
        var mood = CreateMoodSystem();

        mood.RestoreState(0.09f, 0.0f);
        mood.CurrentTier.ShouldBe(MoodTier.Quiet);

        mood.RestoreState(0.29f, 0.0f);
        mood.CurrentTier.ShouldBe(MoodTier.Cozy);

        mood.RestoreState(0.54f, 0.0f);
        mood.CurrentTier.ShouldBe(MoodTier.Lively);

        mood.RestoreState(0.79f, 0.0f);
        mood.CurrentTier.ShouldBe(MoodTier.Vibrant);
    }

    // --- Hysteresis Tests --- (MOOD-03)

    [Test]
    public void HysteresisPreventsRapidDemotion()
    {
        // Start at Cozy (mood=0.10). Decay with small delta so mood drops
        // to ~0.06 (above demote threshold of 0.05). Tier should stay Cozy.
        // alpha = 1 - exp(-0.003662 * 139) = ~0.3988 -> mood = 0.10 * (1 - 0.3988) = ~0.0601
        var mood = CreateMoodSystem();
        mood.RestoreState(0.10f, 0.0f);
        mood.CurrentTier.ShouldBe(MoodTier.Cozy);

        mood.Update(139f, 0);

        mood.Mood.ShouldBeGreaterThan(0.05f);
        mood.CurrentTier.ShouldBe(MoodTier.Cozy);
    }

    [Test]
    public void HysteresisAllowsDemotionBelowGap()
    {
        // Start at Cozy (mood=0.10). Decay with large delta so mood drops
        // well below the demote threshold of 0.05. Tier should become Quiet.
        var mood = CreateMoodSystem();
        mood.RestoreState(0.10f, 0.0f);
        mood.CurrentTier.ShouldBe(MoodTier.Cozy);

        mood.Update(300f, 0);

        mood.Mood.ShouldBeLessThan(0.05f);
        mood.CurrentTier.ShouldBe(MoodTier.Quiet);
    }

    [Test]
    public void HysteresisAtExactDemoteBoundary()
    {
        // Lively demotes at 0.25 (strict less-than). Mood at or above 0.25
        // should keep Lively. RestoreState(0.30) -> Lively, then decay to
        // just above the demote boundary using a conservative delta.
        // alpha = 1 - exp(-0.003662 * 49) = ~0.1641
        // mood = 0.30 * (1 - 0.1641) = ~0.2508 (safely above 0.25)
        var mood = CreateMoodSystem();
        mood.RestoreState(0.30f, 0.0f);
        mood.CurrentTier.ShouldBe(MoodTier.Lively);

        mood.Update(49f, 0);

        mood.Mood.ShouldBeGreaterThanOrEqualTo(0.25f);
        mood.CurrentTier.ShouldBe(MoodTier.Lively);
    }

    // --- Wish Gain Tests --- (MOOD-04)

    [Test]
    public void WishGainIncrementsMood()
    {
        var mood = CreateMoodSystem();

        mood.OnWishFulfilled();

        mood.Mood.ShouldBe(0.06f);
    }

    [Test]
    public void WishFullPromotionSequence()
    {
        var mood = CreateMoodSystem();

        // Wish 1: mood ~0.06, tier=Quiet (below Cozy threshold 0.10)
        mood.OnWishFulfilled();
        mood.Mood.ShouldBe(0.06f, 0.001f);
        mood.CurrentTier.ShouldBe(MoodTier.Quiet);

        // Wish 2: mood ~0.12, tier=Cozy (crossed 0.10)
        mood.OnWishFulfilled();
        mood.Mood.ShouldBe(0.12f, 0.001f);
        mood.CurrentTier.ShouldBe(MoodTier.Cozy);

        // Wishes 3-5: mood ~0.30, stays Cozy (float rounding: 5*0.06f < 0.30f)
        for (int i = 3; i <= 5; i++)
            mood.OnWishFulfilled();
        mood.Mood.ShouldBe(0.30f, 0.001f);
        mood.CurrentTier.ShouldBe(MoodTier.Cozy);

        // Wish 6: mood ~0.36, tier=Lively (crossed 0.30)
        mood.OnWishFulfilled();
        mood.Mood.ShouldBe(0.36f, 0.001f);
        mood.CurrentTier.ShouldBe(MoodTier.Lively);

        // Wishes 7-10: mood ~0.60, tier=Vibrant (crossed 0.55)
        for (int i = 7; i <= 10; i++)
            mood.OnWishFulfilled();
        mood.Mood.ShouldBe(0.60f, 0.001f);
        mood.CurrentTier.ShouldBe(MoodTier.Vibrant);

        // Wishes 11-14: mood ~0.84, tier=Radiant (crossed 0.80)
        for (int i = 11; i <= 14; i++)
            mood.OnWishFulfilled();
        mood.Mood.ShouldBe(0.84f, 0.001f);
        mood.CurrentTier.ShouldBe(MoodTier.Radiant);

        // Wishes 15-17: mood=1.00 (capped), tier=Radiant
        for (int i = 15; i <= 17; i++)
            mood.OnWishFulfilled();
        mood.Mood.ShouldBe(1.0f);
        mood.CurrentTier.ShouldBe(MoodTier.Radiant);
    }

    // --- Restore Tests --- (MOOD-05)

    [Test]
    public void RestoreReconstructsState()
    {
        var mood = CreateMoodSystem();

        mood.RestoreState(0.55f, 0.10f);

        mood.Mood.ShouldBe(0.55f);
        mood.Baseline.ShouldBe(0.10f);
        mood.CurrentTier.ShouldBe(MoodTier.Vibrant);
    }

    [Test]
    public void RestoreClampsAboveRange()
    {
        var mood = CreateMoodSystem();

        mood.RestoreState(5.0f, 0.0f);

        mood.Mood.ShouldBe(1.0f);
        mood.CurrentTier.ShouldBe(MoodTier.Radiant);
    }

    [Test]
    public void RestoreClampsBelowRange()
    {
        var mood = CreateMoodSystem();

        mood.RestoreState(-1.0f, 0.0f);

        mood.Mood.ShouldBe(0.0f);
        mood.CurrentTier.ShouldBe(MoodTier.Quiet);
    }

    // --- Edge Cases ---

    [Test]
    public void WishAtMaxMoodCapsAtOne()
    {
        var mood = CreateMoodSystem();
        mood.RestoreState(0.98f, 0.0f);

        mood.OnWishFulfilled();

        mood.Mood.ShouldBe(1.0f);
        mood.CurrentTier.ShouldBe(MoodTier.Radiant);
    }

    [Test]
    public void DecayWithZeroBaselineNeverGoesNegative()
    {
        var mood = CreateMoodSystem();
        mood.RestoreState(0.01f, 0.0f);

        mood.Update(10000f, 0);

        mood.Mood.ShouldBeGreaterThanOrEqualTo(0f);
    }

    [Test]
    public void FrameRateIndependence()
    {
        // Single large step: 1x60s
        var moodA = CreateMoodSystem();
        moodA.RestoreState(0.5f, 0.0f);
        moodA.Update(60f, 0);

        // Many small steps: 60x1s
        var moodB = CreateMoodSystem();
        moodB.RestoreState(0.5f, 0.0f);
        for (int i = 0; i < 60; i++)
            moodB.Update(1f, 0);

        moodA.Mood.ShouldBe(moodB.Mood, 0.001f);
    }

    [Test]
    public void UndershootPreventionClampsToBaseline()
    {
        // lifetimeHappiness=39 -> baseline ~0.0999
        // With a huge delta, mood should decay to baseline but never below it.
        var mood = CreateMoodSystem();
        mood.RestoreState(0.15f, 0.0f);

        mood.Update(10000f, 39);

        mood.Mood.ShouldBeGreaterThanOrEqualTo(mood.Baseline);
    }
}
