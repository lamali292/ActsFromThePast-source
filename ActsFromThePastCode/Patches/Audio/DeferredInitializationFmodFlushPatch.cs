using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;

namespace ActsFromThePast.Patches.Audio;

// Vanilla's SfxCmd.Play and NAudioManager.PlayOneShot resolve our event paths natively once our banks are loaded
// (they share the base game's FMOD Studio system). This patch only flushes the queued RegisterBank calls once the
// game's deferred initialization has finished.
[HarmonyPatch(typeof(OneTimeInitialization), nameof(OneTimeInitialization.ExecuteDeferred))]
internal static class DeferredInitializationFmodFlushPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        try
        {
            FmodStudio.OnDeferredInitializationCompleted();
        }
        catch (Exception ex)
        {
            Log.Warn($"[Audio] deferred FMOD flush hook failed: {ex.Message}");
        }
    }
}
