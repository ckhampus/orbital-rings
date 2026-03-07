namespace OrbitalRings.Tests.Integration;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using OrbitalRings.Autoloads;

/// <summary>
/// Verification test for GameEvents.ClearAllSubscribers() added in Phase 21 Plan 01.
/// Proves that after clearing, emitting events does not invoke any previously
/// subscribed handlers -- zero stale delegate leaks.
/// </summary>
public class GameEventsTests : TestClass
{
    public GameEventsTests(Node testScene) : base(testScene) { }

    [Test]
    public void ClearAllSubscribersRemovesAllHandlers()
    {
        var events = GameEvents.Instance;
        events.ShouldNotBeNull();

        bool handlerCalled = false;

        // Subscribe to several representative events across categories
        events.CreditsChanged += _ => handlerCalled = true;
        events.RoomPlaced += (_, _) => handlerCalled = true;
        events.CitizenArrived += _ => handlerCalled = true;

        // Act
        events.ClearAllSubscribers();

        // Assert: emitting events does not call any handler
        events.EmitCreditsChanged(100);
        events.EmitRoomPlaced("test", 0);
        events.EmitCitizenArrived("test");

        handlerCalled.ShouldBeFalse();
    }
}
