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

    // ---------------------------------------------------------------
    // DELIBERATE FAILURE -- Uncomment to verify Shouldly error messages
    // ---------------------------------------------------------------
    // [Test]
    // public void ShouldlyErrorMessageDemo()
    // {
    //     int actual = 42;
    //     actual.ShouldBe(99, "This should fail with a readable message");
    // }
}
