using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Saves;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace ActsFromThePast.Tests;

/// <summary>
///     Entry point for the in-game test run. With <c>AFTP_RUN_TESTS=1</c> the game runs every <see cref="AftpTestAttribute" />
///     test once the essential init is done, writes a JSON result to <c>AFTP_TEST_OUTPUT</c> and quits (exit code 0 = all
///     passed). Use <c>build/test.ps1</c> rather than setting the variables by hand.
/// </summary>
public static class TestBootstrap
{
    private static readonly Logger Logger = new("ActsFromThePastTests", LogType.Generic);
    private static bool _started;

    internal static void OnEssentialReady()
    {
        if (!TestModeEnv.Armed || _started) return;
        _started = true;
        // Let the call stack (still inside NGame.GameStartup) unwind before creating combat state.
        Callable.From(() => TaskHelper.RunSafely(AutoRunAsync())).CallDeferred();
    }

    private static bool PrefsLoaded() => SaveManager.Instance.IsPrefsLoaded;

    private static async Task AutoRunAsync()
    {
        var exitCode = 1;
        try
        {
            // GameStartup finishes loading the prefs a few steps after ExecuteEssential returns; effects that read them
            // (e.g. TalkCmd's FastMode check) would intermittently NRE without this wait.
            var tree = (SceneTree)Engine.GetMainLoop();
            for (var i = 0; i < 300 && !PrefsLoaded(); i++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            var result = await new TestRunner().RunAllAsync(SeedHelper.GetRandomSeed(),
                System.Environment.GetEnvironmentVariable(TestModeEnv.Filter), cts.Token);

            var output = System.Environment.GetEnvironmentVariable(TestModeEnv.Output);
            if (string.IsNullOrEmpty(output)) output = ProjectSettings.GlobalizePath("user://aftp_tests.json");
            result.WriteJson(output);
            Logger.Info($"Test results written to {output}");
            exitCode = result.Success ? 0 : 1;
        }
        catch (Exception e)
        {
            Logger.Error($"Test run crashed: {e}");
        }
        finally
        {
            ((SceneTree)Engine.GetMainLoop()).Quit(exitCode);
        }
    }
}
