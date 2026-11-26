using ActsFromThePast.Compatibility;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class MoaiHead : AftpEventModel
{

    private const decimal HpLossPercent = 0.18M;
    private const int GoldAmount = 333;

    public override ActModel[] Acts => [ModelDb.Act<TheBeyondAct>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new MaxHpVar(0),
        new GoldVar(GoldAmount)
    ];

    public override void CalculateVars()
    {
        if (Owner == null) return;
        var hpLoss = (int)Math.Round(Owner.Creature.MaxHp * HpLossPercent);
        DynamicVars.MaxHp.BaseValue = hpLoss;
    }

    private bool HasVisitedExordium(IRunState runState)
    {
        for (var i = 0; i < runState.CurrentActIndex; i++)
            if (runState.Acts[i] is ExordiumAct)
                return true;
        return false;
    }

    public override bool IsAllowed(IRunState runState)
    {
        if (HasVisitedExordium(runState))
            return true;
        return runState.Players.Any(p =>
            (decimal)p.Creature.CurrentHp / p.Creature.MaxHp <= 0.5M);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (Owner == null) return [];
        var options = new List<EventOption>
        {
            Option(Pray)
        };

        if (Owner.Relics.Any(r => r is GoldenIdol))
            options.Add(Option(OfferIdol));
        else
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.INITIAL.options.OFFER_IDOL_LOCKED"));

        if (ActsFromThePastConfig.RebalancedMode)
            options.Add(
                Option(Harvest, "INITIAL_REBALANCED", HoverTipFactory.FromPotion(ModelDb.Potion<RegenPotion>())));
        else
            options.Add(Option(Leave));

        return options;
    }

    private async Task Pray()
    {
        if (Owner == null) return;
        await GameApiCallCompat.LoseMaxHp(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars.MaxHp.BaseValue);
        await CreatureCmd.Heal(Owner.Creature, Owner.Creature.MaxHp);
        SetEventFinished(PageDescription("PRAY"));
    }

    private async Task OfferIdol()
    {
        if (Owner == null) return;
        var goldenIdol = Owner.Relics.First(r => r is GoldenIdol);
        await RelicCmd.Remove(goldenIdol);
        await PlayerCmd.GainGold(GoldAmount, Owner);
        SetEventFinished(PageDescription("OFFER_IDOL"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }

    private async Task Harvest()
    {
        if (Owner == null) return;
        await RewardsCmd.OfferCustom(Owner, [new PotionReward(ModelDb.Potion<RegenPotion>().ToMutable(), Owner)]);
        SetEventFinished(PageDescription("HARVEST"));
    }
}