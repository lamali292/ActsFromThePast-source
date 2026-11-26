using ActsFromThePast.Compatibility;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class PleadingVagrant : AftpEventModel
{

    private const int GoldCost = 85;
    private const int GoldCostRebalanced = 120;

    public override ActModel[] Acts => [ModelDb.Act<TheCityAct>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new GoldVar(GoldCost)
    ];

    public override bool IsAllowed(IRunState runState)
    {
        return !ActsFromThePastConfig.RebalancedMode || runState.Players.All(p => p.Gold >= GoldCostRebalanced);
    }

    public override void CalculateVars()
    {
        if (ActsFromThePastConfig.RebalancedMode)
            DynamicVars.Gold.BaseValue = GoldCostRebalanced;
    }

    private static int Cost => ActsFromThePastConfig.RebalancedMode ? GoldCostRebalanced : GoldCost;

    private EventOption PayGoldOption()
    {
        return Owner != null && Owner.Gold >= Cost
            ? Option(PayGold)
            : new EventOption(this, null, $"{Id.Entry}.pages.INITIAL.options.PAY_GOLD_LOCKED");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
            return
            [
                PayGoldOption(),
                Option(Rob, "INITIAL", HoverTipFactory.FromCardWithCardHoverTips<Shame>().ToArray())
            ];

        var options = new List<EventOption>();
        options.Add(PayGoldOption());
        options.Add(Option(Rob, "INITIAL", HoverTipFactory.FromCard(ModelDb.Card<Shame>())));
        options.Add(Option(Leave));
        return options;
    }

    private async Task PayGold()
    {
        if (Owner == null) return;
        await PlayerCmd.LoseGold(Cost, Owner);
        var relic = GameApiCallCompat.PullNextRelicFromFront(Owner).ToMutable();
        await RelicCmd.Obtain(relic, Owner);
        SetEventFinished(PageDescription("PAY_GOLD"));
    }

    private async Task Rob()
    {
        if (Owner == null) return;
        var shame = Owner.RunState.CreateCard(ModelDb.Card<Shame>(), Owner);
        var curseResult = await CardPileCmd.Add(shame, PileType.Deck);
        CardCmd.PreviewCardPileAdd(curseResult, 2f);
        var relic = GameApiCallCompat.PullNextRelicFromFront(Owner).ToMutable();
        await RelicCmd.Obtain(relic, Owner);
        SetEventFinished(PageDescription("ROB"));
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
    }
}