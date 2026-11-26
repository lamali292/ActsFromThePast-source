using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class SplitPower : AftpPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldStopCombatFromEnding()
    {
        return true;
    }

    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage <= 0 || target.CurrentHp > target.MaxHp / 2)
            return Task.CompletedTask;

        switch (Owner.Monster)
        {
            case AcidSlimeLarge { SplitTriggered: true }:
                break;
            case AcidSlimeLarge acidSlime:
                Flash();
                acidSlime.SplitTriggered = true;
                acidSlime.SetMoveImmediate(acidSlime.SplitState, true);
                break;
            case SpikeSlimeLarge { SplitTriggered: true }:
                break;
            case SpikeSlimeLarge spikeSlime:
                Flash();
                spikeSlime.SplitTriggered = true;
                spikeSlime.SetMoveImmediate(spikeSlime.SplitState, true);
                break;
            case SlimeBoss { SplitTriggered: true }:
                break;
            case SlimeBoss slimeBoss:
                Flash();
                slimeBoss.SplitTriggered = true;
                slimeBoss.SetMoveImmediate(slimeBoss.SplitState, true);
                break;
        }

        return Task.CompletedTask;
    }
}