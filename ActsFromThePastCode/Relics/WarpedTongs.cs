using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class WarpedTongs : AftpRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != CombatSide.Player)
            return Task.CompletedTask;

        var pile = PileType.Hand.GetPile(Owner);
        var upgradableCards = pile.Cards.Where(c => c.IsUpgradable).ToList();

        if (upgradableCards.Count == 0)
            return Task.CompletedTask;

        Flash();
        var card = Owner.RunState.Rng.CombatCardSelection.NextItem(upgradableCards);
        if (card == null)
            return Task.CompletedTask;
        CardCmd.Upgrade(card);
        return Task.CompletedTask;
        // TODO make card flash
    }
}