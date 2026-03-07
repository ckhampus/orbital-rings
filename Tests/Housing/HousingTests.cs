namespace OrbitalRings.Tests.Housing;

using Chickensoft.GoDotTest;
using Godot;
using OrbitalRings.Autoloads;
using OrbitalRings.Data;
using Shouldly;

public class HousingTests : TestClass
{
    public HousingTests(Node testScene) : base(testScene) { }

    [Test]
    public void ComputeCapacityReturnsBaseForSingleSegment()
    {
        // A 1-segment room: capacity = BaseCapacity + (1 - 1) = BaseCapacity
        var definition = new RoomDefinition { BaseCapacity = 2 };

        int capacity = HousingManager.ComputeCapacity(definition, 1);

        capacity.ShouldBe(2);
    }

    // --- ComputeCapacity: Multi-Segment --- (HOUS-01)

    [Test]
    public void ComputeCapacityTwoSegments()
    {
        // BaseCapacity + (2 - 1) = 2 + 1 = 3
        var definition = new RoomDefinition { BaseCapacity = 2 };
        HousingManager.ComputeCapacity(definition, 2).ShouldBe(3);
    }

    [Test]
    public void ComputeCapacityThreeSegments()
    {
        // BaseCapacity + (3 - 1) = 2 + 2 = 4
        var definition = new RoomDefinition { BaseCapacity = 2 };
        HousingManager.ComputeCapacity(definition, 3).ShouldBe(4);
    }

    // --- ComputeCapacity: Varied BaseCapacity --- (HOUS-01)

    [Test]
    public void ComputeCapacityHigherBase1Seg()
    {
        // BaseCapacity + (1 - 1) = 3 + 0 = 3
        var definition = new RoomDefinition { BaseCapacity = 3 };
        HousingManager.ComputeCapacity(definition, 1).ShouldBe(3);
    }

    [Test]
    public void ComputeCapacityHigherBase2Seg()
    {
        // BaseCapacity + (2 - 1) = 3 + 1 = 4
        var definition = new RoomDefinition { BaseCapacity = 3 };
        HousingManager.ComputeCapacity(definition, 2).ShouldBe(4);
    }

    [Test]
    public void ComputeCapacityHigherBase3Seg()
    {
        // BaseCapacity + (3 - 1) = 3 + 2 = 5
        var definition = new RoomDefinition { BaseCapacity = 3 };
        HousingManager.ComputeCapacity(definition, 3).ShouldBe(5);
    }
}
