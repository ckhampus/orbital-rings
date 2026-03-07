namespace OrbitalRings.Tests.Infrastructure;

using Chickensoft.GoDotTest;
using Godot;

/// <summary>
/// Base class for integration tests that need game state isolation.
/// Automatically resets all singletons before each test via [Setup].
///
/// Integration tests (Phase 25) should extend this class.
/// Pure POCO/unit tests (Phases 22-24) can extend TestClass directly.
/// </summary>
public class GameTestClass : TestClass
{
    public GameTestClass(Node testScene) : base(testScene) { }

    [Setup]
    public void ResetGameState()
    {
        TestHelper.ResetAllSingletons();
        TestHelper.ResubscribeAllSingletons();
    }
}
