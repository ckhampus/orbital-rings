namespace OrbitalRings.Tests.Integration;

using System;
using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Godot;
using OrbitalRings.Autoloads;
using OrbitalRings.Data;
using OrbitalRings.Tests.Infrastructure;
using Shouldly;

/// <summary>
/// Integration tests verifying cross-singleton event chains through real
/// singleton code and real events. Covers housing assignment (INTG-04),
/// demolition reassignment (INTG-05), and mood-economy propagation (INTG-06).
///
/// Extends GameTestClass so every test starts with clean singleton state
/// and re-subscribed events (Reset + ResubscribeAllSingletons in [Setup]).
/// </summary>
public class SingletonIntegrationTests : GameTestClass
{
    public SingletonIntegrationTests(Node testScene) : base(testScene) { }

    // --- INTG-04: Housing Assignment ---

    [Test]
    public void AssignmentDistributesEvenly()
    {
        var housing = HousingManager.Instance;
        housing.SeedRoomForTest(0, 2);
        housing.SeedRoomForTest(5, 2);

        var assigned = new List<(string name, int segment)>();
        GameEvents.Instance.CitizenAssignedHome += (name, seg) => assigned.Add((name, seg));

        GameEvents.Instance.EmitCitizenArrived("Alice");
        GameEvents.Instance.EmitCitizenArrived("Bob");
        GameEvents.Instance.EmitCitizenArrived("Carol");

        // Distribution property: no room exceeds another by more than 1 occupant
        int room0 = housing.GetOccupantCount(0);
        int room5 = housing.GetOccupantCount(5);
        Math.Abs(room0 - room5).ShouldBeLessThanOrEqualTo(1);

        // All citizens housed
        housing.GetHomeForCitizen("Alice").ShouldNotBeNull();
        housing.GetHomeForCitizen("Bob").ShouldNotBeNull();
        housing.GetHomeForCitizen("Carol").ShouldNotBeNull();

        // Assignment events fired for each citizen
        assigned.Count.ShouldBe(3);
    }

    [Test]
    public void AssignmentSingleRoomGetsAll()
    {
        var housing = HousingManager.Instance;
        housing.SeedRoomForTest(0, 4);

        GameEvents.Instance.EmitCitizenArrived("Alice");
        GameEvents.Instance.EmitCitizenArrived("Bob");
        GameEvents.Instance.EmitCitizenArrived("Carol");

        housing.GetOccupantCount(0).ShouldBe(3);
        housing.TotalHoused.ShouldBe(3);
    }

    [Test]
    public void AssignmentDifferentCapacityRooms()
    {
        var housing = HousingManager.Instance;
        housing.SeedRoomForTest(0, 2);  // 1-seg room (cap 2)
        housing.SeedRoomForTest(5, 4);  // 3-seg room (cap 4)

        GameEvents.Instance.EmitCitizenArrived("Alice");
        GameEvents.Instance.EmitCitizenArrived("Bob");
        GameEvents.Instance.EmitCitizenArrived("Carol");
        GameEvents.Instance.EmitCitizenArrived("Dave");
        GameEvents.Instance.EmitCitizenArrived("Eve");

        // No room exceeds its capacity
        housing.GetOccupantCount(0).ShouldBeLessThanOrEqualTo(2);
        housing.GetOccupantCount(5).ShouldBeLessThanOrEqualTo(4);

        // All 5 citizens housed
        housing.TotalHoused.ShouldBe(5);
    }

    [Test]
    public void AssignmentFullRoomsLeaveCitizenUnhoused()
    {
        var housing = HousingManager.Instance;
        housing.SeedRoomForTest(0, 2);
        housing.SeedRoomForTest(5, 2);

        var unhousedNames = new List<string>();
        GameEvents.Instance.CitizenUnhoused += name => unhousedNames.Add(name);

        string[] citizens = { "Alice", "Bob", "Carol", "Dave", "Eve" };
        foreach (var name in citizens)
            GameEvents.Instance.EmitCitizenArrived(name);

        // Total capacity is 4, so 4 housed and 1 unhoused
        housing.TotalHoused.ShouldBe(4);

        int unhousedCount = 0;
        foreach (var name in citizens)
        {
            if (housing.GetHomeForCitizen(name) == null)
                unhousedCount++;
        }
        unhousedCount.ShouldBe(1);
    }

    // --- INTG-05: Demolition Reassignment ---

    [Test]
    public void DemolishReassignsCitizens()
    {
        var housing = HousingManager.Instance;
        housing.SeedRoomForTest(0, 3);
        housing.SeedRoomForTest(5, 3);

        GameEvents.Instance.EmitCitizenArrived("Alice");
        GameEvents.Instance.EmitCitizenArrived("Bob");
        GameEvents.Instance.EmitCitizenArrived("Carol");

        int room0Before = housing.GetOccupantCount(0);

        // Subscribe to events AFTER initial assignment
        var unhoused = new List<string>();
        var reassigned = new List<(string name, int segment)>();
        GameEvents.Instance.CitizenUnhoused += name => unhoused.Add(name);
        GameEvents.Instance.CitizenAssignedHome += (name, seg) => reassigned.Add((name, seg));

        // Demolish room 0
        GameEvents.Instance.EmitRoomDemolished(0);

        // All displaced citizens from room 0 should fire CitizenUnhoused
        unhoused.Count.ShouldBe(room0Before);

        // All displaced citizens should have been reassigned to room 5
        foreach (var citizen in unhoused)
        {
            housing.GetHomeForCitizen(citizen).ShouldBe(5);
        }

        // No one lost -- total housed unchanged
        housing.TotalHoused.ShouldBe(3);
    }

    [Test]
    public void DemolishOnlyRoomLeavesAllUnhoused()
    {
        var housing = HousingManager.Instance;
        housing.SeedRoomForTest(0, 3);

        GameEvents.Instance.EmitCitizenArrived("Alice");
        GameEvents.Instance.EmitCitizenArrived("Bob");

        housing.TotalHoused.ShouldBe(2);

        // Subscribe to CitizenUnhoused AFTER arrival
        var unhoused = new List<string>();
        GameEvents.Instance.CitizenUnhoused += name => unhoused.Add(name);

        // Demolish the only room
        GameEvents.Instance.EmitRoomDemolished(0);

        // CitizenUnhoused fires for each citizen
        unhoused.Count.ShouldBe(2);

        // All citizens are now unhoused
        housing.GetHomeForCitizen("Alice").ShouldBeNull();
        housing.GetHomeForCitizen("Bob").ShouldBeNull();
        housing.TotalHoused.ShouldBe(0);
    }

    // --- INTG-06: Mood-Economy Propagation ---

    [Test]
    public void WishFulfillmentCrossesTierAndUpdatesEconomy()
    {
        var economy = EconomyManager.Instance;
        economy.SetCitizenCount(5);
        economy.SetWorkingCitizenCount(0);

        // Record income at Quiet tier (default after reset)
        int quietIncome = economy.CalculateTickIncome();
        quietIncome.ShouldBe(5);

        // Pump 2 wishes to cross Quiet -> Cozy (0.06 + 0.06 = 0.12 >= 0.10)
        GameEvents.Instance.EmitWishFulfilled("Alice", "comfort");
        GameEvents.Instance.EmitWishFulfilled("Alice", "comfort");

        // Verify tier changed to Cozy
        HappinessManager.Instance.CurrentTier.ShouldBe(MoodTier.Cozy);

        // Verify economy income updated with Cozy multiplier
        int cozyIncome = economy.CalculateTickIncome();
        cozyIncome.ShouldBe(6);
        cozyIncome.ShouldBeGreaterThan(quietIncome);
    }

    [Test]
    public void MoodTierChangedEventHasCorrectTiers()
    {
        var tierChanges = new List<(MoodTier newTier, MoodTier oldTier)>();
        GameEvents.Instance.MoodTierChanged += (newTier, oldTier) =>
            tierChanges.Add((newTier, oldTier));

        // Pump 2 wishes to cross Quiet -> Cozy
        GameEvents.Instance.EmitWishFulfilled("Alice", "comfort");
        GameEvents.Instance.EmitWishFulfilled("Alice", "comfort");

        tierChanges.Count.ShouldBe(1);
        tierChanges[0].newTier.ShouldBe(MoodTier.Cozy);
        tierChanges[0].oldTier.ShouldBe(MoodTier.Quiet);
    }
}
