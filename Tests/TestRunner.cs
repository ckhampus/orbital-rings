namespace OrbitalRings.Tests;

using Godot;

#if RUN_TESTS
using System.Reflection;
using Chickensoft.GoDotTest;
#endif

public partial class TestRunner : Node2D
{
#if RUN_TESTS
    public TestEnvironment Environment = default!;
#endif

    public override void _Ready()
    {
#if RUN_TESTS
        Environment = TestEnvironment.From(OS.GetCmdlineArgs());
        if (Environment.ShouldRunTests)
        {
            CallDeferred(nameof(RunTests));
            return;
        }
#endif
        // If not running tests, just quit (this scene has no game purpose)
        GetTree().Quit();
    }

#if RUN_TESTS
    private void RunTests()
        => _ = GoTest.RunTests(Assembly.GetExecutingAssembly(), this, Environment);
#endif
}
