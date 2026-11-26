using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Dread;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Dread;

/// <summary>Mega Debuff: at the start of turn 3 you gain 2 Weak, Vulnerable and Frail, like the Collector's Mega Debuff. Done by <see cref="CollectorDominionPower" />.</summary>
public sealed class CollectorDominion : DominionModel, ICustomModel
{
    private const int DebuffAmount = 2;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromPowerWithPowerHoverTips<CollectorDominionPower>(DebuffAmount);

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is CombatRoom combatRoom)
            await PowerCmd.Apply<CollectorDominionPower>(new ThrowingPlayerChoiceContext(),
                combatRoom.CombatState.Players.Select((Player p) => p.Creature), DebuffAmount, null, null);
    }
}
