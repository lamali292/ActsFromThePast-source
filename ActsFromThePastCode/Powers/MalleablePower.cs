using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class MalleablePower : AftpPowerModel
{
    private const string BaseAmountKey = "BaseAmount";

    private decimal _pendingBlock;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldScaleInMultiplayer => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(BaseAmountKey, 3M)
    ];

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        DynamicVars[BaseAmountKey].BaseValue = Amount;
        return Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner)
            return;
        if (result.UnblockedDamage <= 0)
            return;
        if (!props.IsPoweredAttack())
            return;
        if (target.CurrentHp <= 0)
            return;

        _pendingBlock += Amount;
        await PowerCmd.ModifyAmount(choiceContext, this, 1, null, null);
    }

    public override async Task AfterAttack(
        PlayerChoiceContext choiceContext,
        AttackCommand command)
    {
        if (_pendingBlock <= 0)
            return;

        Flash();
        await CreatureCmd.GainBlock(
            Owner, _pendingBlock, BlockProps.nonCardUnpowered, null);
        _pendingBlock = 0M;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side)
            return;

        if (_pendingBlock > 0)
        {
            Flash();
            await CreatureCmd.GainBlock(
                Owner, _pendingBlock, BlockProps.nonCardUnpowered, null);
            _pendingBlock = 0M;
        }

        var baseAmount = (int)DynamicVars[BaseAmountKey].BaseValue;
        if (Amount != baseAmount)
        {
            var offset = baseAmount - Amount;
            await PowerCmd.ModifyAmount(choiceContext, this, offset, null, null);
        }
    }
}