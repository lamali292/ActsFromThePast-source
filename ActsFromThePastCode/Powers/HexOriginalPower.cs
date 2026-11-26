using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ActsFromThePast.Powers;

public sealed class HexOriginalPower : AftpPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<Dazed>()
    ];

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner)
            return;
        if (cardPlay.Card.Type == CardType.Attack)
            return;
        Flash();
        var player = cardPlay.Card.Owner?.Creature;
        if (player?.Player == null)
            return;
        var statusCards = new CardPileAddResult[Amount];
        for (var i = 0; i < Amount; ++i)
        {
            CardModel card = CombatState.CreateCard<Dazed>(player.Player);
            statusCards[i] =
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, null, CardPilePosition.Random);
        }

        CardCmd.PreviewCardPileAdd(statusCards);
        await Cmd.Wait(0.5f);
    }
}