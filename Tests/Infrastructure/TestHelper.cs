namespace OrbitalRings.Tests.Infrastructure;

using OrbitalRings.Autoloads;
using OrbitalRings.Build;
using OrbitalRings.Citizens;

/// <summary>
/// Static orchestrator for resetting all game singletons between tests.
/// Ensures every test starts with a clean slate, preventing cross-test
/// state leaks that cause flaky failures.
///
/// Call order matters: ClearAllSubscribers() MUST run before any Reset()
/// to prevent Reset() side effects (e.g., EconomyManager clearing credits
/// would emit CreditsChanged to a still-subscribed SaveManager, triggering
/// autosave during teardown).
/// </summary>
public static class TestHelper
{
    /// <summary>
    /// Resets all game singletons to a clean "just loaded, no game data" state.
    /// Safe to call even when singletons are null (headless test context).
    /// </summary>
    public static void ResetAllSingletons()
    {
        // 1. Clear ALL event subscribers FIRST.
        //    This prevents Reset() operations from triggering stale handlers
        //    (e.g., EconomyManager.Reset() clearing credits would emit CreditsChanged
        //    to SaveManager, which would trigger autosave).
        GameEvents.Instance?.ClearAllSubscribers();

        // 2. Reset each singleton. Order doesn't matter after events are cleared,
        //    since no cross-singleton communication can happen.
        EconomyManager.Instance?.Reset();
        BuildManager.Instance?.Reset();
        CitizenManager.Instance?.Reset();
        WishBoard.Instance?.Reset();
        HappinessManager.Instance?.Reset();
        HousingManager.Instance?.Reset();
        SaveManager.Instance?.Reset();
    }
}
