using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Dread;

namespace ActsFromThePast.Dread;

/// <summary>
///     Entry point of the Dread assembly. Called by <c>DreadLoader</c> in ActsFromThePast.dll, by reflection and only on
///     games that have Dread mode (0.113.0+).
/// </summary>
public static class DreadBootstrap
{
    private const string ModId = "ActsFromThePast";

    private static bool _registered;

    public static void Register()
    {
        if (_registered) return;
        _registered = true;

        // The game only looks for models in the assemblies a mod owns, and the multiplayer content sorter needs every model type to
        // belong to a mod. This assembly is loaded by hand, so it has to be associated with the mod. ModelDb.Init, which runs after
        // the mod initializers, then finds the Dominions and their powers like any other model of the mod.
        ModManager.AssociateAssemblyWithMod(ModId, Assembly.GetExecutingAssembly());

        new Harmony("actsfromthepast.dread").PatchAll(Assembly.GetExecutingAssembly());
        new Logger("ActsFromThePast", LogType.Generic).Info("Associated the Dread assembly with the mod.");
    }
}

/// <summary>
///     Gives our boss encounters their Dominion. <c>EncounterModel.GetDominion()</c> returns null unless overridden, and
///     overriding it would put <c>DominionModel</c> into ActsFromThePast.dll's own types, so it is patched from here instead.
/// </summary>
[HarmonyPatch]
internal static class EncounterGetDominionPatch
{
    private static MethodBase? TargetMethod() => AccessTools.Method(typeof(EncounterModel), "GetDominion");

    private static void Postfix(EncounterModel __instance, ref DominionModel? __result)
    {
        if (__result != null) return;

        var type = __instance.GetType();
        if (type.Namespace?.StartsWith("ActsFromThePast", StringComparison.Ordinal) != true) return;

        __result = type.Name switch
        {
            "GuardianBoss" => ModelDb.Dominion<GuardianDominion>(),
            "HexaghostBoss" => ModelDb.Dominion<HexaghostDominion>(),
            "SlimeBossBoss" => ModelDb.Dominion<SlimeBossDominion>(),
            "BronzeAutomatonBoss" => ModelDb.Dominion<BronzeAutomatonDominion>(),
            "ChampBoss" => ModelDb.Dominion<ChampDominion>(),
            "CollectorBoss" => ModelDb.Dominion<CollectorDominion>(),
            "AwakenedOneBoss" => ModelDb.Dominion<AwakenedOneDominion>(),
            "DonuAndDecaBoss" => ModelDb.Dominion<DonuAndDecaDominion>(),
            "TimeEaterBoss" => ModelDb.Dominion<TimeEaterDominion>(),
            _ => null
        };
    }
}
