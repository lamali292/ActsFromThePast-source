using System.Reflection;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Powers;

public sealed class StasisPower : AftpPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    private CardModel? StolenCard
    {
        get;
        set
        {
            AssertMutable();
            field = value;
        }
    }

    private Creature? CardOwner
    {
        get;
        set
        {
            AssertMutable();
            field = value;
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            return StolenCard == null
                ? Array.Empty<IHoverTip>()
                : new[] { HoverTipFactory.FromCard(StolenCard) };
        }
    }

    public Task Capture(CardModel card, Creature originalOwner)
    {
        StolenCard = card;
        CardOwner = originalOwner;
        return Task.CompletedTask;
    }

    public override async Task BeforeDeath(Creature target)
    {
        if (Owner != target || StolenCard == null || CardOwner == null)
            return;

        var combatState = CardOwner.CombatState;
        if (combatState == null)
            return;

        // Clear the removed flag
        StolenCard.HasBeenRemovedFromState = false;

        // Re-register the card with combat state
        if (combatState is CombatState { } state && !state._allCards.Contains(StolenCard))
            state._allCards.Add(StolenCard);

        await CardPileCmd.Add(StolenCard, PileType.Hand);
    }
}