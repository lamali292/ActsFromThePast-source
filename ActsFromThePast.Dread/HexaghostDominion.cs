using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Dread;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Dread;

/// <summary>Sear: at the start of every 2nd turn (2, 4, 6, ...) a Burn is added to your hand, like Hexaghost's Sear. Done by <see cref="HexaghostDominionPower" />.</summary>
public sealed class HexaghostDominion : DominionModel, ICustomModel
{
    private const int BurnCount = 1;

    public override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<Burn>();

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is CombatRoom combatRoom)
            await PowerCmd.Apply<HexaghostDominionPower>(new ThrowingPlayerChoiceContext(),
                combatRoom.CombatState.Players.Select((Player p) => p.Creature), BurnCount, null, null);
    }
}
