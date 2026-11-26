using ActsFromThePast.Compatibility;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using GoldenIdol = ActsFromThePast.Relics.GoldenIdol;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class ForgottenAltar : AftpEventModel
{

    private const float HpLossPercent = 0.35f;
    private const int MaxHpGain = 5;

    public override ActModel[] Acts => [ModelDb.Act<TheCityAct>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(0),
        new MaxHpVar(MaxHpGain)
    ];

    public override void CalculateVars()
    {
        if (Owner == null) return;
        var hpLoss = (int)Math.Round(Owner.Creature.MaxHp * HpLossPercent);
        DynamicVars.HpLoss.BaseValue = hpLoss;
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
        return HasVisitedExordium(runState);
    }

    public override void OnRoomEnter()
    {
        AFTPModAudio.Play("events", "forgotten_altar");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (Owner == null) return [];
        var options = new List<EventOption>();

        if (Owner.Relics.Any(r => r is GoldenIdol))
            options.Add(Option(OfferIdol, "INITIAL",
                HoverTipFactory.FromRelic(ModelDb.Relic<BloodyIdol>()).ToArray()));
        else
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.INITIAL.options.OFFER_IDOL_LOCKED"));

        var netDamage = DynamicVars.HpLoss.BaseValue - MaxHpGain;
        options.Add(Option(Sacrifice).ThatDoesDamage(netDamage));

        if (ActsFromThePastConfig.RebalancedMode)
            options.Add(Option(Desecrate, "INITIAL_REBALANCED",
                HoverTipFactory.FromCardWithCardHoverTips<Decay>().ToArray()));
        else
            options.Add(Option(Desecrate, "INITIAL",
                HoverTipFactory.FromCard(ModelDb.Card<Decay>())));

        return options;
    }

    private async Task OfferIdol()
    {
        if (Owner == null) return;
        SfxCmd.Play("event:/sfx/heal_1");

        var goldenIdol = Owner.Relics.First(r => r is GoldenIdol);
        var bloodyIdol = ModelDb.Relic<BloodyIdol>().ToMutable();
        await RelicCmd.Replace(goldenIdol, bloodyIdol);
        SetEventFinished(PageDescription("OFFER_IDOL"));
    }

    private async Task Sacrifice()
    {
        if (Owner == null) return;
        SfxCmd.Play("event:/sfx/heal_3");

        await CreatureCmd.GainMaxHp(Owner.Creature, MaxHpGain);

        await GameApiCallCompat.Damage(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars.HpLoss.BaseValue,
            DamageProps.nonCardHpLoss,
            null);
        SetEventFinished(PageDescription("SACRIFICE"));
    }

    private async Task Desecrate()
    {
        if (Owner == null) return;
        SfxCmd.Play("event:/sfx/blunt_heavy");
        if (ActsFromThePastConfig.RebalancedMode)
            await CreatureCmd.GainMaxHp(Owner.Creature, MaxHpGain);
        var decay = Owner.RunState.CreateCard(ModelDb.Card<Decay>(), Owner);
        var addResult = await CardPileCmd.Add(decay, PileType.Deck);
        CardCmd.PreviewCardPileAdd([addResult], 2f);
        await Cmd.Wait(0.75f);
        SetEventFinished(PageDescription("DESECRATE"));
    }
}