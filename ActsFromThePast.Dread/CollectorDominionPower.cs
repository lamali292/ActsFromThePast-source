using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ActsFromThePast.Dread;

/// <summary>
///     Collector's Dominion, on the player: at the start of turn 3 you gain Weak, Vulnerable and Frail (Amount each), then this power
///     is gone. Same shape as the Queen's <c>YouAreMinePower</c>, with the turns left shown as the counter and the Collector's own
///     Mega Debuff sound and curse effect when it lands.
/// </summary>
public sealed class CollectorDominionPower : DreadPowerModel
{
    private const int DebuffTurn = 3;

    // The round in which the effect was last played, per combat.
    private static readonly ConditionalWeakTable<ICombatState, StrongBox<int>> LastEffectRound = new();

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>Turns until the debuffs land: 2 on turn 1, 1 on turn 2.</summary>
    public override int DisplayAmount => Math.Max(DebuffTurn - (Owner.Player?.PlayerCombatState?.TurnNumber ?? 1), 0);

    protected override string IconEntry => "WITHERING_PRESENCE_POWER";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        ..HoverTipFactory.FromPowerWithPowerHoverTips<WeakPower>(Amount),
        ..HoverTipFactory.FromPowerWithPowerHoverTips<VulnerablePower>(Amount),
        ..HoverTipFactory.FromPowerWithPowerHoverTips<FrailPower>(Amount)
    ];

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == CombatSide.Player) InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     In multiplayer every player has this power and all of them fire at the same turn start. The sound, the curse effect and the
    ///     wait are played once per round by whichever power gets here first: the curse lands on every affected player at the same time,
    ///     and the rest just apply their debuffs.
    /// </summary>
    private async Task PlayEffectOncePerRound()
    {
        if (Owner.CombatState is not { } combatState) return;

        var lastRound = LastEffectRound.GetValue(combatState, _ => new StrongBox<int>(0));
        if (lastRound.Value == combatState.RoundNumber) return;
        lastRound.Value = combatState.RoundNumber;

        CollectorDebuffFx.PlayOn(combatState.Players.Select(p => p.Creature)
            .Where(c => c.IsAlive && c.HasPower<CollectorDominionPower>()).ToList());
        await Cmd.Wait(CollectorDebuffFx.Duration);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || player.PlayerCombatState is not { TurnNumber: >= DebuffTurn }) return;

        Flash();
        await PlayEffectOncePerRound();

        await PowerCmd.Apply<WeakPower>(choiceContext, Owner, Amount, Owner, null);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, Owner, Amount, Owner, null);
        await PowerCmd.Apply<FrailPower>(choiceContext, Owner, Amount, Owner, null);
        await PowerCmd.Remove(this);
    }
}
