using ActsFromThePast.Compatibility;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Cards;

[Pool(typeof(CurseCardPool))]
public sealed class Pain() : AftpCardModel(-1,
    CardType.Curse,
    CardRarity.Curse,
    TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Unplayable
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(1M)
    ];

    public override int MaxUpgradeLevel => 0;

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card == this)
            return;

        if (cardPlay.Card.Owner != Owner)
            return;

        var hand = PileType.Hand.GetPile(Owner);
        if (!hand.Cards.Contains(this))
            return;

        await GameApiCallCompat.Damage(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars.HpLoss.BaseValue,
            DamageProps.cardHpLoss,
            this);
    }
}