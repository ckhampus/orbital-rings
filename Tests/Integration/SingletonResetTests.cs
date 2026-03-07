namespace OrbitalRings.Tests.Integration;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using OrbitalRings.Autoloads;
using OrbitalRings.Build;
using OrbitalRings.Citizens;
using OrbitalRings.Data;

/// <summary>
/// Verification tests for singleton Reset() methods added in Phase 21 Plan 01.
/// Each test dirties a singleton's state via its public API, calls Reset(),
/// and asserts the singleton has returned to a clean "just loaded" state.
///
/// Extends TestClass (NOT GameTestClass) because we are testing the reset
/// infrastructure itself -- auto-reset in [Setup] would hide bugs.
/// </summary>
public class SingletonResetTests : TestClass
{
    public SingletonResetTests(Node testScene) : base(testScene) { }

    [Test]
    public void EconomyManagerResetClearsAllState()
    {
        var economy = EconomyManager.Instance;
        economy.ShouldNotBeNull();

        // Dirty state
        economy.Earn(500);
        economy.SetCitizenCount(10);
        economy.SetMoodTier(MoodTier.Radiant);
        EconomyManager.StateLoaded = true;

        // Verify state is dirty
        economy.Credits.ShouldBeGreaterThan(0);
        EconomyManager.StateLoaded.ShouldBeTrue();

        // Act
        economy.Reset();

        // Assert clean state
        economy.Credits.ShouldBe(0);
        EconomyManager.StateLoaded.ShouldBeFalse();
    }

    [Test]
    public void HousingManagerResetClearsAllState()
    {
        var housing = HousingManager.Instance;
        housing.ShouldNotBeNull();

        // Dirty state via StateLoaded flag (dictionaries require scene context to populate)
        HousingManager.StateLoaded = true;

        // Verify state is dirty
        HousingManager.StateLoaded.ShouldBeTrue();

        // Act
        housing.Reset();

        // Assert clean state
        HousingManager.StateLoaded.ShouldBeFalse();
        housing.TotalCapacity.ShouldBe(0);
        housing.TotalHoused.ShouldBe(0);
    }

    [Test]
    public void HappinessManagerResetClearsAllState()
    {
        var happiness = HappinessManager.Instance;
        happiness.ShouldNotBeNull();

        // Reset first to ensure known starting state, then verify reset works
        happiness.Reset();

        // Verify clean baseline
        happiness.LifetimeWishes.ShouldBe(0);
        happiness.CurrentTier.ShouldBe(MoodTier.Quiet);

        // Verify starter rooms are unlocked after reset
        happiness.IsRoomUnlocked("bunk_pod").ShouldBeTrue();
        happiness.IsRoomUnlocked("air_recycler").ShouldBeTrue();
        happiness.IsRoomUnlocked("workshop").ShouldBeTrue();
        happiness.IsRoomUnlocked("reading_nook").ShouldBeTrue();
        happiness.IsRoomUnlocked("storage_bay").ShouldBeTrue();
        happiness.IsRoomUnlocked("garden_nook").ShouldBeTrue();

        // Milestone rooms should NOT be unlocked after reset
        happiness.IsRoomUnlocked("sky_loft").ShouldBeFalse();
        happiness.IsRoomUnlocked("craft_lab").ShouldBeFalse();
        happiness.IsRoomUnlocked("star_lounge").ShouldBeFalse();
        happiness.IsRoomUnlocked("comm_relay").ShouldBeFalse();
    }

    [Test]
    public void SaveManagerResetClearsAllState()
    {
        var save = SaveManager.Instance;
        save.ShouldNotBeNull();

        // Dirty state
        save.PendingLoad = new SaveData { Credits = 999 };

        // Verify state is dirty
        save.PendingLoad.ShouldNotBeNull();

        // Act
        save.Reset();

        // Assert clean state
        save.PendingLoad.ShouldBeNull();
    }

    [Test]
    public void BuildManagerResetClearsAllState()
    {
        var build = BuildManager.Instance;
        build.ShouldNotBeNull();

        // Verify clean state after reset (build mode internals require
        // RingVisual scene context to dirty, so we verify the public property)
        build.Reset();

        build.CurrentMode.ShouldBe(BuildMode.Normal);
        build.SelectedRoom.ShouldBeNull();
        build.GetPlacedRoom(0).ShouldBeNull();
    }

    [Test]
    public void CitizenManagerResetClearsAllState()
    {
        var citizens = CitizenManager.Instance;
        citizens.ShouldNotBeNull();

        // Dirty state
        CitizenManager.StateLoaded = true;

        // Verify state is dirty
        CitizenManager.StateLoaded.ShouldBeTrue();

        // Act
        citizens.Reset();

        // Assert clean state
        CitizenManager.StateLoaded.ShouldBeFalse();
        citizens.CitizenCount.ShouldBe(0);
    }

    [Test]
    public void WishBoardResetClearsAllState()
    {
        var wishBoard = WishBoard.Instance;
        wishBoard.ShouldNotBeNull();

        // Verify clean state after reset (wish dictionaries require
        // event-driven population which needs full game context)
        wishBoard.Reset();

        wishBoard.GetActiveWishCount().ShouldBe(0);
        wishBoard.IsRoomTypeAvailable("bunk_pod").ShouldBeFalse();
    }
}
