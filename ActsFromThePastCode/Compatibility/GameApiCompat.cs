using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Compatibility;

/// <summary>
///     Version shims. The mod is compiled once against 0.113.1 and has to load unchanged on 0.111.1. Anything that
///     exists under a different name or signature on the older game is resolved at runtime here.
/// </summary>
public static class GameApiCompat
{
    // MultiplayerScalingModel lives in MegaCrit.Sts2.Core.Models on 0.113.x and in MegaCrit.Sts2.Core.Models.Singleton on 0.111.1.
    private static readonly MethodInfo? GetMultiplayerScalingMethod =
        (AccessTools.TypeByName("MegaCrit.Sts2.Core.Models.MultiplayerScalingModel")
         ?? AccessTools.TypeByName("MegaCrit.Sts2.Core.Models.Singleton.MultiplayerScalingModel"))
        ?.GetMethod("GetMultiplayerScaling", BindingFlags.Public | BindingFlags.Static);

    // AscensionHelper.IsDread exists on 0.113.0+ only (not on 0.111.1).
    private static readonly MethodInfo? IsDreadMethod =
        AccessTools.Method(typeof(MegaCrit.Sts2.Core.Helpers.AscensionHelper), "IsDread");

    /// <summary>True in a Dread run. Always false on games without Dread mode (0.111.1).</summary>
    public static bool IsDread()
    {
        try { return IsDreadMethod?.Invoke(null, null) as bool? ?? false; }
        catch { return false; }
    }

    /// <summary>Version-safe replacement for <c>MultiplayerScalingModel.GetMultiplayerScaling</c>.</summary>
    public static decimal GetMultiplayerScaling(EncounterModel? encounter, int actIndex) =>
        GetMultiplayerScalingMethod?.Invoke(null, [encounter, actIndex]) as decimal? ?? 1m;
}

/// <summary>
///     0.111.1 calls <c>CardModel.AfterTransformedTo()</c> on the replacement card; 0.113.x calls
///     <c>AfterThisWasTransformedFrom(original)</c> instead. Cards override the newer hook only, so on the older game
///     this forwards to it.
/// </summary>
[HarmonyPatch]
internal static class AfterTransformedToCompatPatch
{
    private static bool Prepare() => TargetMethod() != null;

    private static MethodBase? TargetMethod() => AccessTools.Method(typeof(CardModel), "AfterTransformedTo");

    [HarmonyPostfix]
    private static void Postfix(CardModel __instance)
    {
        // By reflection: CardModel.AfterThisWasTransformedFrom does not exist on 0.111.1, so the card's own override is called.
        if (__instance.GetType().Assembly == typeof(AfterTransformedToCompatPatch).Assembly)
            __instance.GetType().GetMethod("AfterThisWasTransformedFrom", [typeof(CardModel)])?.Invoke(__instance, [null]);
    }
}

/// <summary>Call-site shims for APIs whose signature changed between 0.111.1 and 0.113.x.</summary>
public static partial class GameApiCallCompat
{
    private static readonly MethodInfo LoseMaxHpMethod = AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.LoseMaxHp))
                                                         ?? throw new MissingMethodException(nameof(CreatureCmd), nameof(CreatureCmd.LoseMaxHp));

    /// <summary>
    ///     <c>CreatureCmd.LoseMaxHp</c> takes <c>bool isFromCard</c> on 0.111.1 and <c>CardModel? cardSource</c> on 0.113.x.
    ///     This mod never loses max HP from a card, so both forms get the "none" value.
    /// </summary>
    public static Task LoseMaxHp(PlayerChoiceContext choiceContext, Creature creature, decimal amount)
    {
        var last = LoseMaxHpMethod.GetParameters()[3].ParameterType;
        object? none = last == typeof(bool) ? false : null;
        return (Task)LoseMaxHpMethod.Invoke(null, [choiceContext, creature, amount, none])!;
    }

    private static readonly MethodInfo? PullForRewardMethod =
        AccessTools.Method(typeof(RelicFactory), "PullNextRelicForRewardFromFront", [typeof(Player)]);

    private static readonly MethodInfo? PullOldMethod =
        AccessTools.Method(typeof(RelicFactory), "PullNextRelicFromFront", [typeof(Player)]);

    /// <summary>Rolls a rarity from the player's reward rng and pulls the next relic: 0.113.x renamed this to <c>PullNextRelicForRewardFromFront</c>.</summary>
    public static RelicModel PullNextRelicFromFront(Player player) =>
        (RelicModel)(PullForRewardMethod ?? PullOldMethod
            ?? throw new MissingMethodException(nameof(RelicFactory), "PullNextRelicFromFront"))
            .Invoke(null, [player])!;

    private static readonly MethodInfo? ToggleInteractable = AccessTools.Method(typeof(NCreature), "ToggleIsInteractable", [typeof(bool)]);
    private static readonly MethodInfo? SetInteractable = AccessTools.Method(typeof(NCreature), "SetIsInteractable", [typeof(bool), typeof(bool)]);

    /// <summary>0.111.1 has <c>ToggleIsInteractable(on)</c>; 0.113.x replaced it with <c>SetIsInteractable(on, changeVisibilityImmediately)</c>.</summary>
    public static void SetInteractableCompat(this NCreature node, bool on)
    {
        if (ToggleInteractable != null) ToggleInteractable.Invoke(node, [on]);
        else SetInteractable?.Invoke(node, [on, true]);
    }

    /// <summary>0.111.1 has <c>NCombatRoom.SetCreatureIsInteractable</c>; on 0.113.x the creature node is toggled directly.</summary>
    public static void SetCreatureInteractable(this NCombatRoom room, Creature? creature, bool on) =>
        room.GetCreatureNode(creature)?.SetInteractableCompat(on);
}
