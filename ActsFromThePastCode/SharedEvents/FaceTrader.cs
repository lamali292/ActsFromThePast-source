using ActsFromThePast.Compatibility;
using ActsFromThePast.Interfaces;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.SharedEvents;

public sealed class FaceTrader : AftpEventModel, IActRestricted, IShrineEvent
{

    private const int GoldReward = 50;

    public override ActModel[] Acts => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Damage", 0),
        new GoldVar(GoldReward)
    ];

    public int[] AllowedActIndices => [1, 2];

    bool IShrineEvent.IsOneTimeEvent => true;

    public override void CalculateVars()
    {
        if (Owner == null) return;
        var dmg = (int)(Owner.Creature.MaxHp / 10M);
        if (dmg == 0) dmg = 1;
        DynamicVars["Damage"].BaseValue = dmg;
    }

    public override bool IsAllowed(IRunState runState)
    {
        if (runState.Players.Count > 1)
            return false;

        // Unavailable in multiplayer. N'loth's Hungry ahh Face can cause issues

        if (!ActsFromThePastConfig.RebalancedMode)
            return true;

        return runState.Players.All(p =>
        {
            var dmg = (int)(p.Creature.MaxHp / 10M);
            if (dmg == 0) dmg = 1;
            return p.Creature.CurrentHp > dmg;
        });
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [Option(Continue)];
    }

    private Task Continue()
    {
        var options = new List<EventOption>
        {
            Option(Touch, "MAIN").ThatDoesDamage(DynamicVars["Damage"].BaseValue),
            Option(Trade, "MAIN")
        };

        if (!ActsFromThePastConfig.RebalancedMode) options.Add(Option(Leave, "MAIN"));

        SetEventState(PageDescription("MAIN"), options);
        return Task.CompletedTask;
    }

    private async Task Touch()
    {
        if (Owner == null) return;
        await GameApiCallCompat.Damage(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["Damage"].BaseValue,
            DamageProps.nonCardHpLoss,
            null);
        await PlayerCmd.GainGold(GoldReward, Owner);
        SetEventFinished(PageDescription("TOUCH"));
    }

    private async Task Trade()
    {
        if (Owner == null) return;
        var relic = GetRandomFace();
        await RelicCmd.Obtain(relic.ToMutable(), Owner);
        SetEventFinished(PageDescription("TRADE"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }

    private RelicModel GetRandomFace()
    {
        if (Owner == null) throw new InvalidOperationException("Event has no owner.");
        var candidates = new List<RelicModel>();

        if (!Owner.Relics.Any(r => r is CultistHeadpiece))
            candidates.Add(ModelDb.Relic<CultistHeadpiece>());
        if (!Owner.Relics.Any(r => r is FaceOfCleric))
            candidates.Add(ModelDb.Relic<FaceOfCleric>());
        if (!Owner.Relics.Any(r => r is GremlinVisage))
            candidates.Add(ModelDb.Relic<GremlinVisage>());
        if (!Owner.Relics.Any(r => r is NlothsHungryFace))
            candidates.Add(ModelDb.Relic<NlothsHungryFace>());
        if (!Owner.Relics.Any(r => r is SsserpentHead))
            candidates.Add(ModelDb.Relic<SsserpentHead>());

        return candidates.Count == 0 ? ModelDb.Relic<Circlet>() : Rng.NextRequired(candidates);
    }
}