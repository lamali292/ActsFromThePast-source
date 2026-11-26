using ActsFromThePast.Compatibility;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class SharpHidePower : AftpPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public bool AttackInProgress { get; private set; }
    public Creature? AttackSource { get; private set; }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Type != CardType.Attack) return Task.CompletedTask;
        AttackInProgress = true;
        AttackSource = cardPlay.Card.Owner?.Creature;

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        AttackInProgress = false;
        AttackSource = null;
        if (cardPlay.Card.Type != CardType.Attack)
            return;
        Flash();
        var player = cardPlay.Card.Owner?.Creature;
        if (player is { IsAlive: true })
            await GameApiCallCompat.Damage(
                choiceContext, player,
                Amount, DamageProps.nonCardUnpowered,
                null);
    }
}