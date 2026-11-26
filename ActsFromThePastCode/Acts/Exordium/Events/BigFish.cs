using ActsFromThePast.Compatibility;
using ActsFromThePast.Cards;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ActsFromThePast.Acts.Exordium.Events;

// 800x800, 30 stroke 50% opacity, 600 from left, 332 from bottom. big image complete gaussian blur 2750x2750
public sealed class BigFish : AftpEventModel
{

    private const int MaxHpGain = 5;
    public override ActModel[] Acts => [ModelDb.Act<ExordiumAct>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HealVar(0M),
        new MaxHpVar(MaxHpGain)
    ];

    public override void CalculateVars()
    {
        if (Owner == null) return;
        DynamicVars.Heal.BaseValue = Owner.Creature.MaxHp / 3M;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
            return
            [
                Option(Banana),
                Option(Donut),
                Option(BoxRebalanced, "INITIAL_REBALANCED",
                    HoverTipFactory.FromCardWithCardHoverTips<TheBox>().ToArray())
            ];

        return
        [
            Option(Banana),
            Option(Donut),
            Option(Box, [HoverTipFactory.FromCard(ModelDb.Card<Regret>())])
        ];
    }

    private async Task Banana()
    {
        if (Owner == null) return;
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
        SetEventFinished(PageDescription("BANANA"));
    }

    private async Task Donut()
    {
        if (Owner == null) return;
        await CreatureCmd.GainMaxHp(Owner.Creature, MaxHpGain);
        SetEventFinished(PageDescription("DONUT"));
    }

    private async Task Box()
    {
        if (Owner == null) return;
        await CardPileCmd.AddCurseToDeck<Regret>(Owner);
        var relic = GameApiCallCompat.PullNextRelicFromFront(Owner).ToMutable();
        await RelicCmd.Obtain(relic, Owner);
        SetEventFinished(PageDescription("BOX"));
    }

    private async Task BoxRebalanced()
    {
        if (Owner == null) return;
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(Owner.RunState.CreateCard<TheBox>(Owner), PileType.Deck), 2f);
        SetEventFinished(PageDescription("BOX_REBALANCED"));
    }
}