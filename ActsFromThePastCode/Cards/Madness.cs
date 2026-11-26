using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ActsFromThePast.Cards;

[Pool(typeof(EventCardPool))]
public sealed class Madness() : AftpCardModel(1, CardType.Skill, CardRarity.Event, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c.CostsEnergyOrStars(false) || c.CostsEnergyOrStars(true))
            .ToList();
        if (hand.Count == 0)
            return Task.CompletedTask;
        var target = Owner.RunState.Rng.CombatCardSelection.NextItem(hand);
        target?.SetToFreeThisCombat();
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}