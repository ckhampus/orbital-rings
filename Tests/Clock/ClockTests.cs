namespace OrbitalRings.Tests.Clock;

using Chickensoft.GoDotTest;
using Godot;
using OrbitalRings.Autoloads;
using OrbitalRings.Data;
using OrbitalRings.Tests.Infrastructure;
using Shouldly;

/// <summary>
/// Unit tests for StationClock period computation, cycle wrapping,
/// non-uniform weights, PeriodProgress, and Reset behavior.
///
/// Tests instantiate StationClock directly with a fresh ClockConfig
/// and use SetElapsedTime() for deterministic time positioning.
/// Event tests use GameTestClass base for GameEvents singleton access.
/// </summary>
public class ClockTests : GameTestClass
{
    public ClockTests(Node testScene) : base(testScene) { }

    private static StationClock Clock => StationClock.Instance;

    // --- Period Computation: Default Equal Weights (480s cycle, 120s per period) ---

    [Test]
    public void ComputePeriod_AtTimeZero_ReturnsMorningWithZeroProgress()
    {
        Clock.SetElapsedTime(0f);

        Clock.CurrentPeriod.ShouldBe(StationPeriod.Morning);
        Clock.PeriodProgress.ShouldBe(0f, 0.01f);
    }

    [Test]
    public void ComputePeriod_AtTime119_ReturnsMorningNearFullProgress()
    {
        // 119s into a 120s Morning period = 99.2% progress
        Clock.SetElapsedTime(119f);

        Clock.CurrentPeriod.ShouldBe(StationPeriod.Morning);
        Clock.PeriodProgress.ShouldBeGreaterThan(0.98f);
        Clock.PeriodProgress.ShouldBeLessThan(1.0f);
    }

    [Test]
    public void ComputePeriod_AtTime120_ReturnsDay()
    {
        // 120s = start of Day period (equal 120s periods)
        Clock.SetElapsedTime(120f);

        Clock.CurrentPeriod.ShouldBe(StationPeriod.Day);
        Clock.PeriodProgress.ShouldBe(0f, 0.01f);
    }

    [Test]
    public void ComputePeriod_AtTime240_ReturnsEvening()
    {
        Clock.SetElapsedTime(240f);

        Clock.CurrentPeriod.ShouldBe(StationPeriod.Evening);
        Clock.PeriodProgress.ShouldBe(0f, 0.01f);
    }

    [Test]
    public void ComputePeriod_AtTime360_ReturnsNight()
    {
        Clock.SetElapsedTime(360f);

        Clock.CurrentPeriod.ShouldBe(StationPeriod.Night);
        Clock.PeriodProgress.ShouldBe(0f, 0.01f);
    }

    // --- Cycle Wrapping ---

    [Test]
    public void ComputePeriod_AtTime480_WrapsToMorning()
    {
        // 480s = exact cycle boundary, wraps to Morning via modulo
        Clock.SetElapsedTime(480f);

        Clock.CurrentPeriod.ShouldBe(StationPeriod.Morning);
        Clock.PeriodProgress.ShouldBe(0f, 0.01f);
    }

    [Test]
    public void ComputePeriod_AtTime960_WrapsCorrectlyAfterTwoCycles()
    {
        // 960s = two full cycles, wraps back to Morning
        Clock.SetElapsedTime(960f);

        Clock.CurrentPeriod.ShouldBe(StationPeriod.Morning);
        Clock.PeriodProgress.ShouldBe(0f, 0.01f);
    }

    [Test]
    public void ComputePeriod_AtTime600_WrapsToDay()
    {
        // 600s = 480 + 120 = one full cycle + start of Day
        Clock.SetElapsedTime(600f);

        Clock.CurrentPeriod.ShouldBe(StationPeriod.Day);
        Clock.PeriodProgress.ShouldBe(0f, 0.01f);
    }

    // --- Non-Uniform Weights ---

    [Test]
    public void ComputePeriod_NonUniformWeights_MorningGetsLargerShare()
    {
        // MorningWeight=2, others=1 -> total weight=5
        // Morning gets 2/5 of 480s = 192s
        // Day gets 1/5 = 96s, starts at 192
        Clock.Config.MorningWeight = 2.0f;
        Clock.Config.DayWeight = 1.0f;
        Clock.Config.EveningWeight = 1.0f;
        Clock.Config.NightWeight = 1.0f;

        // At 100s, still in Morning (Morning is 192s)
        Clock.SetElapsedTime(100f);
        Clock.CurrentPeriod.ShouldBe(StationPeriod.Morning);

        // At 192s, Day starts
        Clock.SetElapsedTime(192f);
        Clock.CurrentPeriod.ShouldBe(StationPeriod.Day);

        // At 288s (192 + 96), Evening starts
        Clock.SetElapsedTime(288f);
        Clock.CurrentPeriod.ShouldBe(StationPeriod.Evening);

        // At 384s (288 + 96), Night starts
        Clock.SetElapsedTime(384f);
        Clock.CurrentPeriod.ShouldBe(StationPeriod.Night);

        // Reset to equal weights for subsequent tests
        Clock.Config.MorningWeight = 1.0f;
    }

    [Test]
    public void ComputePeriod_NonUniformWeights_ProgressNormalizesWithinPeriod()
    {
        // MorningWeight=2, others=1 -> Morning = 192s
        // At 96s, progress should be 96/192 = 0.5
        Clock.Config.MorningWeight = 2.0f;

        Clock.SetElapsedTime(96f);
        Clock.CurrentPeriod.ShouldBe(StationPeriod.Morning);
        Clock.PeriodProgress.ShouldBe(0.5f, 0.01f);

        // Reset to equal weights
        Clock.Config.MorningWeight = 1.0f;
    }

    // --- PeriodProgress ---

    [Test]
    public void PeriodProgress_MidPeriod_ReturnsNormalizedValue()
    {
        // 60s into a 120s Morning period = 50% progress
        Clock.SetElapsedTime(60f);

        Clock.CurrentPeriod.ShouldBe(StationPeriod.Morning);
        Clock.PeriodProgress.ShouldBe(0.5f, 0.01f);
    }

    [Test]
    public void PeriodProgress_QuarterThroughDay_Returns025()
    {
        // Day starts at 120s, 30s into Day = 25% progress
        Clock.SetElapsedTime(150f);

        Clock.CurrentPeriod.ShouldBe(StationPeriod.Day);
        Clock.PeriodProgress.ShouldBe(0.25f, 0.01f);
    }

    // --- PeriodChanged Event ---

    [Test]
    public void AdvanceTime_CrossingBoundary_FiresPeriodChangedOnce()
    {
        // Position clock near end of Morning
        Clock.SetElapsedTime(119f);
        Clock.CurrentPeriod.ShouldBe(StationPeriod.Morning);

        int fireCount = 0;
        StationPeriod? receivedNew = null;
        StationPeriod? receivedPrevious = null;

        GameEvents.Instance.PeriodChanged += (newPeriod, previousPeriod) =>
        {
            fireCount++;
            receivedNew = newPeriod;
            receivedPrevious = previousPeriod;
        };

        // Advance past the Morning/Day boundary by simulating _Process
        // SetElapsedTime doesn't fire events (by design), so we need to
        // set up the clock state then call _Process with a delta that crosses the boundary
        Clock.SetElapsedTime(119.5f);
        Clock._Process(1.0); // 119.5 + 1.0 = 120.5 -> Day

        fireCount.ShouldBe(1);
        receivedNew.ShouldBe(StationPeriod.Day);
        receivedPrevious.ShouldBe(StationPeriod.Morning);
    }

    [Test]
    public void AdvanceTime_WithinSamePeriod_DoesNotFirePeriodChanged()
    {
        Clock.SetElapsedTime(10f);

        int fireCount = 0;
        GameEvents.Instance.PeriodChanged += (_, _) => fireCount++;

        // Advance within Morning (10 + 5 = 15, still in Morning)
        Clock._Process(5.0);

        fireCount.ShouldBe(0);
    }

    // --- Reset ---

    [Test]
    public void Reset_ClearsToMorningWithZeroProgress()
    {
        // Move clock to Evening
        Clock.SetElapsedTime(250f);
        Clock.CurrentPeriod.ShouldBe(StationPeriod.Evening);

        Clock.Reset();

        Clock.CurrentPeriod.ShouldBe(StationPeriod.Morning);
        Clock.PeriodProgress.ShouldBe(0f);
        Clock.ElapsedTime.ShouldBe(0f);
    }

    // --- ElapsedTime ---

    [Test]
    public void ElapsedTime_AfterSetElapsedTime_ReflectsWrappedValue()
    {
        Clock.SetElapsedTime(100f);
        Clock.ElapsedTime.ShouldBe(100f, 0.01f);
    }

    [Test]
    public void ElapsedTime_AfterProcess_AccumulatesDelta()
    {
        Clock.Reset();
        Clock._Process(10.0);

        Clock.ElapsedTime.ShouldBe(10f, 0.01f);
    }
}
