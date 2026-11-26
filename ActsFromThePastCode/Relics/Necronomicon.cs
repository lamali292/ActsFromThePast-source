using ActsFromThePast.Cards;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class Necronomicon : AftpRelicModel
{
    private bool _activated = true;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<Necronomicurse>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(2)
    ];

    public override async Task AfterObtained()
    {
        AFTPModAudio.Play("relics", "necronomicon");

        // Add Necronomicurse to deck when obtained
        var curse = Owner.RunState.CreateCard(ModelDb.Card<Necronomicurse>(), Owner);
        var result = await CardPileCmd.Add(curse, PileType.Deck);
        CardCmd.PreviewCardPileAdd(result, 2f);
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
            return Task.CompletedTask;

        _activated = true;
        Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner != Owner || !_activated || card.Type != CardType.Attack || card.EnergyCost.GetResolved() < DynamicVars.Energy.IntValue)
            return playCount;

        return playCount + 1;
    }

    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        if (card.Owner != Owner || card.Type != CardType.Attack || card.EnergyCost.GetResolved() < DynamicVars.Energy.IntValue || !_activated)
            return Task.CompletedTask;

        _activated = false;
        Flash();
        Status = RelicStatus.Disabled;

        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        _activated = true;
        Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }
}