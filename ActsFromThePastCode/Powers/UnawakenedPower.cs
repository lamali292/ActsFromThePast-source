using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Powers;

public sealed class UnawakenedPower : AftpPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    private bool IsReviving => GetInternalData<Data>().IsReviving;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public void DoRevive()
    {
        GetInternalData<Data>().IsReviving = false;
    }

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        CardModel? source,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (wasRemovalPrevented || creature != Owner || !(creature.Monster is AwakenedOne monster))
            return;
        GetInternalData<Data>().IsReviving = true;
        await monster.TriggerDeadState();
    }

    public override bool ShouldAllowHitting(Creature creature)
    {
        return creature != Owner || !IsReviving;
    }

    public override bool ShouldStopCombatFromEnding()
    {
        if (Owner?.Monster is AwakenedOne awakened) return !awakened.ShouldDisappearFromDoom || Owner.IsDead;
        return false;
    }

    public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
    {
        return creature != Owner;
    }

    public override bool ShouldPowerBeRemovedOnDeath(PowerModel power)
    {
        return power.Type == PowerType.Debuff;
    }

    public override bool ShouldPowerBeRemovedAfterOwnerDeath()
    {
        return false;
    }

    public override bool ShouldOwnerDeathTriggerFatal()
    {
        return false;
    }

    private class Data
    {
        public bool IsReviving;
    }
}