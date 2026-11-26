using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Dread;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Dread;

/// <summary>Time Warp: every 10 cards you play your turn ends and all enemies gain 2 Strength. The counting is done by <see cref="TimeWarpDominionPower" />.</summary>
public sealed class TimeEaterDominion : DominionModel, ICustomModel
{
    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromPowerWithPowerHoverTips<TimeWarpDominionPower>(1);

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is CombatRoom combatRoom)
            await PowerCmd.Apply<TimeWarpDominionPower>(new ThrowingPlayerChoiceContext(),
                combatRoom.CombatState.Players.Select((Player p) => p.Creature), 1m, null, null);
    }
}
