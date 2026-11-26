using ActsFromThePast.Compatibility;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class MaskedBandits : AftpEventModel
{

    public static bool WaitingForMapEasterEgg;
    public static bool WaitingForBrandishEasterEgg;
    public override ActModel[] Acts => [ModelDb.Act<TheCityAct>()];

    public override bool IsShared => true;

    public override EventLayoutType LayoutType => EventLayoutType.Combat;

    public override EncounterModel CanonicalEncounter =>
        ModelDb.Encounter<RedMaskBanditsEvent>();

    public override bool IsAllowed(IRunState runState)
    {
        return runState.TotalFloor >= 23 &&
               (!runState.Players.Any(p => p.Relics.Any(r => r is RedMask))
                || ActsFromThePastConfig.RebalancedMode);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (Owner == null) return [];
        if (ActsFromThePastConfig.RebalancedMode
            && Owner.RunState.Players.Count == 1
            && Owner.Relics.Any(r => r is RedMask))
            return
            [
                Option(BrandishMask, "INITIAL_REBALANCED",
                    HoverTipFactory.FromCardWithCardHoverTips<HandOfGreed>().ToArray()),
                Option(Fight)
            ];
        return
        [
            Option(Pay),
            Option(Fight)
        ];
    }

    public override void OnRoomEnter()
    {
        WaitingForMapEasterEgg = false;
        WaitingForBrandishEasterEgg = false;
    }

    private async Task Pay()
    {
        if (Owner == null) return;
        var goldToLose = Owner.Gold;
        if (goldToLose > 0)
            await PlayerCmd.LoseGold(goldToLose, Owner, GoldLossType.Stolen);

        SetEventState(PageDescription("PAID_1"), [
            new EventOption(this, Paid2,
                $"{Id.Entry}.pages.PAID_1.options.CONTINUE")
        ]);
    }

    private Task Paid2()
    {
        SetEventState(PageDescription("PAID_2"), [
            new EventOption(this, Paid3,
                $"{Id.Entry}.pages.PAID_2.options.CONTINUE")
        ]);
        return Task.CompletedTask;
    }

    private Task Paid3()
    {
        WaitingForMapEasterEgg = true;
        SetEventFinished(PageDescription("PAID_3"));
        return Task.CompletedTask;
    }

    private Task Fight()
    {
        if (Owner == null) return Task.CompletedTask;
        var redMaskRelic = ModelDb.Relic<RedMask>().ToMutable();
        var rewards = new List<Reward>
        {
            new GoldReward(25, 35, Owner),
            GameApiCallCompat.CreateRelicReward(redMaskRelic, Owner)
        };
        EnterCombatWithoutExitingEvent<RedMaskBanditsEvent>(rewards, false);
        return Task.CompletedTask;
    }

    private async Task BrandishMask()
    {
        if (Owner == null) return;
        var card = Owner.RunState.CreateCard(ModelDb.Card<HandOfGreed>(), Owner);
        var result = await CardPileCmd.Add(card, PileType.Deck);
        CardCmd.PreviewCardPileAdd([result], 2f);
        await Cmd.Wait(0.75f);
        SetEventState(PageDescription("BRANDISH_1"), [
            new EventOption(this, Brandish2,
                $"{Id.Entry}.pages.BRANDISH_1.options.CONTINUE")
        ]);
    }

    private Task Brandish2()
    {
        SetEventState(PageDescription("BRANDISH_2"), [
            new EventOption(this, Brandish3,
                $"{Id.Entry}.pages.BRANDISH_2.options.CONTINUE")
        ]);
        return Task.CompletedTask;
    }

    private Task Brandish3()
    {
        WaitingForBrandishEasterEgg = true;
        SetEventFinished(PageDescription("BRANDISH_3"));
        return Task.CompletedTask;
    }
}