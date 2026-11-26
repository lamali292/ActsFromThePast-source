using ActsFromThePast.Compatibility;
using MegaCrit.Sts2.Core.Models;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class ConstrictedPower : AftpPowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        var constrictedPower = this;
        if (side != constrictedPower.Owner.Side)
            return;
        await GameApiCallCompat.Damage(
            choiceContext, constrictedPower.Owner,
            constrictedPower.Amount, DamageProps.nonCardUnpowered,
            null);
    }

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        CardModel? source,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        var power = this;
        if (wasRemovalPrevented || creature != power.Applier)
            return;
        await PowerCmd.Remove(power);
    }
}