using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ActsFromThePast.Enchantments;

public sealed class Fearful : CustomEnchantmentModel
{
    private const int VulnerableAmount = 1;

    public override bool ShowAmount => false;
    public override bool HasExtraCardText => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VulnerablePower>()
    ];

    public override bool CanEnchant(CardModel card)
    {
        return base.CanEnchant(card) && card is { Type: CardType.Skill, GainsBlock: true };
    }

    public override decimal EnchantBlockMultiplicative(decimal originalBlock)
    {
        return 3M;
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (Status != EnchantmentStatus.Normal)
            return;

        var power = await PowerCmd.Apply<VulnerablePower>(
            choiceContext,
            Card.Owner.Creature,
            VulnerableAmount,
            Card.Owner.Creature,
            Card);

        power?.SkipNextDurationTick = false;
    }
}