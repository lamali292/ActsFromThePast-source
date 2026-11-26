using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Compatibility;

// Call-site shims for APIs whose signature differs between 0.111.1 and 0.113.x. They resolve the real member by name at
// runtime, so a build compiled against 0.113.0 never bakes a signature in that the older game does not have.
// build/check-compat.ps1 lists every such mismatch.
public static partial class GameApiCallCompat
{
    /// <summary>Arguments for a reflection call: <paramref name="given" /> first, every remaining parameter at its declared default.</summary>
    private static object?[] FillDefaults(ParameterInfo[] parameters, params object?[] given) =>
        parameters.Select((p, i) => i < given.Length ? given[i] : p.HasDefaultValue ? p.DefaultValue : null).ToArray();

    /// <summary>
    ///     <c>CreatureCmd.Damage(ctx, target, amount, props, cardSource)</c> without a dealer: both supported game versions take a trailing
    ///     <c>CardPlay</c> that this mod never has.
    /// </summary>
    public static Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext choiceContext, Creature target, decimal amount,
        ValueProp props, CardModel? cardSource) =>
        CreatureCmd.Damage(choiceContext, target, amount, props, null, cardSource, null);

    private static readonly ConstructorInfo CardRewardCtor = typeof(CardReward).GetConstructors()
        .First(c => c.GetParameters() is { Length: >= 3 } ps && ps[0].ParameterType == typeof(CardCreationOptions)
                                                              && ps[1].ParameterType == typeof(int));

    /// <summary><c>new CardReward(options, count, player)</c>; 0.113.x appends optional parameters that a compiled call would bake into the signature.</summary>
    public static CardReward CreateCardReward(CardCreationOptions options, int count, Player player) =>
        (CardReward)CardRewardCtor.Invoke(FillDefaults(CardRewardCtor.GetParameters(), options, count, player));

    private static readonly ConstructorInfo RelicRewardCtor = typeof(RelicReward).GetConstructors()
        .First(c => c.GetParameters() is { Length: >= 2 } ps && ps[0].ParameterType == typeof(RelicModel)
                                                              && ps[1].ParameterType == typeof(Player));

    /// <summary><c>new RelicReward(relic, player)</c>; 0.113.x appends an optional <c>Rng</c>.</summary>
    public static RelicReward CreateRelicReward(RelicModel relic, Player player) =>
        (RelicReward)RelicRewardCtor.Invoke(FillDefaults(RelicRewardCtor.GetParameters(), relic, player));

    private static readonly MethodInfo PlayOnCreatureCenterMethod = AccessTools.Method(typeof(VfxCmd), "PlayOnCreatureCenter");

    /// <summary><c>VfxCmd.PlayOnCreatureCenter(target, path)</c>; 0.113.x appends an optional <c>forceIfDead</c>.</summary>
    public static void PlayOnCreatureCenter(Creature target, string path) =>
        PlayOnCreatureCenterMethod.Invoke(null, FillDefaults(PlayOnCreatureCenterMethod.GetParameters(), target, path));
}
