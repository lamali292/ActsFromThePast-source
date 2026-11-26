using ActsFromThePast.Compatibility;
using ActsFromThePast.Cards;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class TheNest : AftpEventModel
{

    private const int HpLoss = 6;
    private const int GoldGain = 50;

    public override ActModel[] Acts => [ModelDb.Act<TheCityAct>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(HpLoss),
        new GoldVar(GoldGain)
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [Option(Investigate)];
    }

    private Task Investigate()
    {
        SetEventState(PageDescription("INVESTIGATE"), [
            Option(Steal, "INVESTIGATE"),
            Option(Join, "INVESTIGATE", HoverTipFactory.FromCard(ModelDb.Card<RitualDagger>()))
                .ThatDoesDamage(HpLoss)
        ]);
        return Task.CompletedTask;
    }

    private async Task Steal()
    {
        if (Owner == null) return;
        await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
        SetEventFinished(PageDescription("STEAL"));
    }

    private async Task Join()
    {
        if (Owner == null) return;
        await GameApiCallCompat.Damage(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            HpLoss,
            DamageProps.nonCardHpLoss,
            null);
        var ritualDagger = Owner.RunState.CreateCard(ModelDb.Card<RitualDagger>(), Owner);
        var addResult = await CardPileCmd.Add(ritualDagger, PileType.Deck);
        CardCmd.PreviewCardPileAdd([addResult], 2f);
        await Cmd.Wait(0.75f);
        SetEventFinished(PageDescription("JOIN"));
    }
}