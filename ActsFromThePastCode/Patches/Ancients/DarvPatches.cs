using ActsFromThePast.Compatibility;
using System.Collections;
using System.Reflection;
using ActsFromThePast.Acts.TheBeyond;
using ActsFromThePast.Acts.TheCity;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Patches.Ancients;

[HarmonyPatch]
public class DarvPatches
{
    private static MethodBase TargetMethod() => ActModelTargets.GenerateRooms();


    public static void Postfix(ActModel __instance)
    {
        if (!ActsFromThePastConfig.DarvOnlyInLegacyActs)
            return;

        if (__instance is not TheCityAct and not TheBeyondAct)
            return;

        var rooms = __instance._rooms;

        var darv = ModelDb.AncientEvent<Darv>();
        rooms.Ancient = darv;
    }
}

[HarmonyPatch(typeof(Darv), "GenerateInitialOptions")]
public class DarvUniqueOffersPatch
{
    // The parameters differ between the supported game versions: (relic, pageName, customDonePage) on 0.111.1 and
    // (relic, pageName) on 0.113.0.
    private static readonly MethodInfo RelicOptionMethod =
        AccessTools.Method(typeof(AncientEventModel), "RelicOption", [typeof(RelicModel), typeof(string)])
        ?? AccessTools.Method(typeof(AncientEventModel), "RelicOption",
            [typeof(RelicModel), typeof(string), typeof(string)])
        ?? throw new MissingMethodException(nameof(AncientEventModel), "RelicOption");

    public static bool Prefix(Darv __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (!ActsFromThePastConfig.DarvOnlyInLegacyActs)
            return true;

        var owner = __instance.Owner;
        if (owner == null)
            return true;
        var previousTitles = DarvOfferTracker.GetPreviouslyOfferedTitles(owner);

        // First visit — no history yet, let vanilla handle it
        if (previousTitles.Count == 0)
            return true;

        var rng = __instance.Rng;
        var candidates = (Darv._validRelicSets.Where(set => set.filter(owner))
            .Select(set => set.relics.Where(r => !previousTitles.Contains(r.Title.GetFormattedText())).ToArray())
            .Where(available => available.Length > 0)
            .Select(available => rng.NextRequired(available))).ToList();

        candidates = candidates.UnstableShuffle(rng);

        var dustyTomeAvailable = !previousTitles.Contains(
            ModelDb.Relic<DustyTome>().Title.GetFormattedText());

        List<EventOption> list;
        if (rng.NextBool() && dustyTomeAvailable)
        {
            list = candidates.Take(2)
                .Select(r => MakeRelicOption(__instance, r.ToMutable()))
                .ToList();

            var dustyTome = (DustyTome)ModelDb.Relic<DustyTome>().ToMutable();
            if (owner != null) dustyTome.SetupForPlayer(owner);
            list.Add(MakeRelicOption(__instance, dustyTome));
        }
        else
        {
            list = candidates.Take(3)
                .Select(r => MakeRelicOption(__instance, r.ToMutable()))
                .ToList();
        }

        // Once most relics have been offered in earlier acts the unique pool runs dry. An ancient with no options
        // softlocks the run (empty event), and a single option is no choice, so let vanilla generate the offer instead
        // (it may repeat a relic, which is better than nothing).
        if (list.Count < 2)
            return true;

        __result = list;
        return false;
    }

    private static EventOption MakeRelicOption(AncientEventModel instance, RelicModel relic)
    {
        object?[] args = RelicOptionMethod.GetParameters().Length == 2 ? [relic, "INITIAL"] : [relic, "INITIAL", null];
        return (EventOption)RelicOptionMethod.Invoke(instance, args)!;
    }
}