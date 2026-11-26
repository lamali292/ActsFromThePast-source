using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;

namespace ActsFromThePast.Tests;

internal static class TestModeEnv
{
    public const string RunTests = "AFTP_RUN_TESTS";
    public const string Filter = "AFTP_TEST_FILTER";
    public const string Output = "AFTP_TEST_OUTPUT";

    /// <summary>The patches below only exist while the test runner is armed, so a normal game session never carries them.</summary>
    public static bool Armed => Environment.GetEnvironmentVariable(RunTests) == "1";
}

/// <summary>Starts the automated run right after the essential init (mods, loc, ModelDb), well before the main menu is up.</summary>
[HarmonyPatch(typeof(OneTimeInitialization), nameof(OneTimeInitialization.ExecuteEssential))]
internal static class BootTimingPatch
{
    private static bool Prepare() => TestModeEnv.Armed;

    [HarmonyPostfix]
    private static void Postfix() => TestBootstrap.OnEssentialReady();
}

/// <summary>SaveManager.SeenFtue has no TestMode check, so the first shuffle of a run would wait for a popup nobody can confirm.</summary>
[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SeenFtue))]
internal static class FtueTestModePatch
{
    private static bool Prepare() => TestModeEnv.Armed;

    [HarmonyPrefix]
    private static bool Prefix(ref bool __result)
    {
        if (TestMode.IsOff) return true;
        __result = true;
        return false;
    }
}

/// <summary>The harness never creates a combat room, so NGame has no screenshake target and every hit would log a stack trace.</summary>
[HarmonyPatch(typeof(NGame))]
internal static class ScreenShakeTestModePatch
{
    private static bool Prepare() => TestModeEnv.Armed;

    [HarmonyPatch(nameof(NGame.ScreenShake))]
    [HarmonyPrefix]
    private static bool SkipShake() => TestMode.IsOff;

    [HarmonyPatch(nameof(NGame.ScreenRumble))]
    [HarmonyPrefix]
    private static bool SkipRumble() => TestMode.IsOff;

    [HarmonyPatch(nameof(NGame.ScreenShakeTrauma))]
    [HarmonyPrefix]
    private static bool SkipTrauma() => TestMode.IsOff;
}
