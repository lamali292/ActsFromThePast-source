using ActsFromThePast.Compatibility;
using ActsFromThePast.Patches;
using ActsFromThePast.Patches.Ancients;
using ActsFromThePast.Patches.Audio;
using ActsFromThePast.Patches.Cards;
using ActsFromThePast.Patches.Creatures;
using ActsFromThePast.Patches.Events;
using ActsFromThePast.Patches.Powers;
using ActsFromThePast.Patches.Relics;
using ActsFromThePast.Patches.RoomEvents;
using ActsFromThePast.Tests;
using MegaCrit.Sts2.Core.Logging;

namespace ActsFromThePast;

/// <summary>
///     Every Harmony patch class of the mod, registered by hand (no <c>PatchAll</c>): one failing patch is logged and skipped
///     instead of taking the whole mod initializer down, and patch classes that do not apply to the running game version opt
///     out with a static <c>bool Prepare()</c>. A new <c>[HarmonyPatch]</c> class does nothing until it is added here.
/// </summary>
public static class ActsFromThePastPatchManager
{
    public static void HarmonyPatches()
    {
        ModPatcher.Create("actsfromthepast.actsfromthepast", new Logger("ActsFromThePast", LogType.Generic))
            .Add(typeof(AfterDeathSourceCompatPatch))
            .Add(typeof(AfterTransformedToCompatPatch))
            .Add(typeof(DarvPatches))
            .Add(typeof(DarvUniqueOffersPatch))
            .Add(typeof(DeferredInitializationFmodFlushPatch))
            .Add(typeof(MusicPatches.UpdateTrackPatch))
            .Add(typeof(MusicPatches.AfterCombatEndPatch))
            .Add(typeof(BurnUpgradePatch))
            .Add(typeof(TagClassicSlimedPatch))
            .Add(typeof(ClassicSlimedDescriptionPatch))
            .Add(typeof(ClassicSlimedOnPlayPatch))
            .Add(typeof(TheBoxFreePurchasePatch))
            .Add(typeof(TheBoxIgnoreCostPatch))
            .Add(typeof(TheBoxRemovalPatch))
            .Add(typeof(TheBoxResetAfterPurchasePatch))
            .Add(typeof(TheBoxResetOnShopEnterPatch))
            .Add(typeof(TheBoxGreenPricePatch))
            .Add(typeof(TheBoxMerchantOpenPatch))
            .Add(typeof(TheBoxPurchaseDialoguePatch))
            .Add(typeof(NecronomicurseTransformPatch))
            .Add(typeof(VisualsPatches.CreatureVisualsLayerPatch))
            .Add(typeof(VisualsPatches.ResetCreatureZOnGameOverPatch))
            .Add(typeof(ColosseumPatches.ReplayWriterFixPatch))
            .Add(typeof(ColosseumPatches.RewardsPatch))
            .Add(typeof(MindBloomPatches.RewardsPatch))
            .Add(typeof(ShrinePatches.EventPoolPatch))
            .Add(typeof(ShrinePatches.RepeatableShrineValidityPatch))
            .Add(typeof(PowerPatches.OddMushroomPatch))
            .Add(typeof(RelicPatches.NRewardButtonGoldenIdolPatch))
            .Add(typeof(RelicPatches.RelicCollectionTranspiler))
            .Add(typeof(DeadAdventurerPatches.VisualsPatch))
            .Add(typeof(DeadAdventurerPatches.DeadAdventurerRewardsPatch))
            .Add(typeof(MaskedBanditsPatches.RewardsPatch))
            .Add(typeof(MaskedBanditsPatches.MapClosePatch))
            .Add(typeof(MushroomPatches.VisualsPatch))
            .Add(typeof(MysteriousSpherePatches.VisualsPatch))
            .Add(typeof(MysteriousSpherePatches.RewardsPatch))
            .Add(typeof(BootTimingPatch))
            .Add(typeof(FtueTestModePatch))
            .Add(typeof(ScreenShakeTestModePatch))
            .PatchAll();
    }
}
