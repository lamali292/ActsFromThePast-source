using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class FlightPower : AftpPowerModel
{
    private const string StoredAmountKey = "StoredAmount";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldScaleInMultiplayer => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new(StoredAmountKey, 0M)
    ];

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        DynamicVars[StoredAmountKey].BaseValue = Amount;
        return Task.CompletedTask;
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Side)
            return;

        var stored = (int)DynamicVars[StoredAmountKey].BaseValue;
        if (Amount != stored)
        {
            var offset = stored - Amount;
            await PowerCmd.ModifyAmount(choiceContext, this, offset, null, null);
        }
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        return target != Owner || !props.IsPoweredAttack() ? 1M : 0.5M;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage <= 0)
            return;
        if (!props.IsPoweredAttack())
            return;
        if (target.CurrentHp <= 0)
            return;

        Flash();
        await PowerCmd.Decrement(this);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        await base.AfterRemoved(oldOwner);

        if (oldOwner.Monster is Byrd byrd) await byrd.OnFlightBroken();
    }
}