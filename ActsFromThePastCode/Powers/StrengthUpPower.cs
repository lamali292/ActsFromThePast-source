using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ActsFromThePast.Powers;

public class StrengthUpPower : AftpPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        var strengthUpPower = this;
        if (side != strengthUpPower.Owner.Side)
            return;
        strengthUpPower.Flash();
        await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(),
            strengthUpPower.Owner, strengthUpPower.Amount, strengthUpPower.Owner, null);
    }
}