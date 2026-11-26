using ActsFromThePast.Compatibility;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class GoldenIdol : AftpEventModel
{

    private const decimal HpLossPercent = 0.35M;
    private const decimal MaxHpLossPercent = 0.10M;
    private const int JamGoldMin = 30;
    private const int JamGoldMax = 50;

    // TODO add sfx and shakes and stuff from sts1

    public override ActModel[] Acts => [ModelDb.Act<ExordiumAct>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Damage", 0),
        new MaxHpVar(0),
        new GoldVar(0),
        new StringVar("Relic", "Relic")
    ];

    public override void CalculateVars()
    {
        if (Owner == null) return;
        DynamicVars["Damage"].BaseValue = Math.Floor(Owner.Creature.MaxHp * HpLossPercent);
        var maxHpLoss = Math.Floor(Owner.Creature.MaxHp * MaxHpLossPercent);
        if (maxHpLoss < 1)
            maxHpLoss = 1;
        DynamicVars.MaxHp.BaseValue = maxHpLoss;
        DynamicVars.Gold.BaseValue = JamGoldMin + Rng.NextInt(JamGoldMax - JamGoldMin + 1);
    }

    public override void OnRoomEnter()
    {
        AFTPModAudio.Play("events", "golden_idol");
    }

    private IEnumerable<RelicModel> GetTradableRelics()
    {
        if (Owner == null) return [];
        return Owner.Relics.Where(r =>
            r is { IsUsedUp: false, IsMelted: false, SpawnsPets: false } &&
            r.Rarity != RelicRarity.Starter &&
            r.Rarity != RelicRarity.Event &&
            r.Rarity != RelicRarity.Ancient);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
        {
            var relic = Rng.NextItem(GetTradableRelics());
            if (relic != null)
            {
                ((StringVar)DynamicVars["Relic"]).StringValue = relic.Title.GetFormattedText();
                return
                [
                    Option(Take, "INITIAL", HoverTipFactory.FromRelic(ModelDb.Relic<Relics.GoldenIdol>()).ToArray()),
                    new EventOption(this, async () => await Switcheroo(relic),
                        L10NLookup($"{Id.Entry}.pages.INITIAL_REBALANCED.options.SWITCHEROO.title"),
                        L10NLookup($"{Id.Entry}.pages.INITIAL_REBALANCED.options.SWITCHEROO.description"),
                        $"{Id.Entry}.pages.INITIAL_REBALANCED.options.SWITCHEROO",
                        relic.HoverTips).ThatHasDynamicTitle()
                ];
            }

            return
            [
                Option(Take, "INITIAL", HoverTipFactory.FromRelic(ModelDb.Relic<Relics.GoldenIdol>()).ToArray()),
                new EventOption(this, null,
                    $"{Id.Entry}.pages.INITIAL_REBALANCED.options.SWITCHEROO_LOCKED")
            ];
        }

        return
        [
            Option(Take, "INITIAL", HoverTipFactory.FromRelic(ModelDb.Relic<Relics.GoldenIdol>()).ToArray()),
            Option(Leave)
        ];
    }

    private async Task Take()
    {
        if (Owner == null) return;
        var relic = ModelDb.Relic<Relics.GoldenIdol>().ToMutable();
        await RelicCmd.Obtain(relic, Owner);

        if (ActsFromThePastConfig.RebalancedMode)
            SetEventState(PageDescription("BOULDER_REBALANCED"), [
                Option(Jam, "BOULDER_REBALANCED"),
                Option(Smash, "BOULDER").ThatDoesDamage(DynamicVars["Damage"].BaseValue),
                Option(Crawl, "BOULDER")
            ]);
        else
            SetEventState(PageDescription("BOULDER"), [
                Option(Outrun, "BOULDER", HoverTipFactory.FromCard(ModelDb.Card<Injury>())),
                Option(Smash, "BOULDER").ThatDoesDamage(DynamicVars["Damage"].BaseValue),
                Option(Crawl, "BOULDER")
            ]);
    }

    private async Task Switcheroo(RelicModel relic)
    {
        if (Owner == null) return;
        await RelicCmd.Remove(relic);
        await RelicCmd.Obtain(ModelDb.Relic<Relics.GoldenIdol>().ToMutable(), Owner);
        SetEventFinished(PageDescription("SWITCHEROO"));
    }

    private async Task Jam()
    {
        if (Owner == null) return;
        var goldenIdol = Owner.Relics.FirstOrDefault(r => r is Relics.GoldenIdol);
        if (goldenIdol != null)
            await RelicCmd.Remove(goldenIdol);

        await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
        SetEventFinished(PageDescription("JAM"));
    }

    private async Task Outrun()
    {
        if (Owner == null) return;
        await CardPileCmd.AddCurseToDeck<Injury>(Owner);
        SetEventFinished(PageDescription("OUTRUN"));
    }

    private async Task Smash()
    {
        if (Owner == null) return;
        await GameApiCallCompat.Damage(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["Damage"].BaseValue,
            DamageProps.nonCardHpLoss,
            null);
        SetEventFinished(PageDescription("SMASH"));
    }

    private async Task Crawl()
    {
        if (Owner == null) return;
        await GameApiCallCompat.LoseMaxHp(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars.MaxHp.BaseValue);
        SetEventFinished(PageDescription("CRAWL"));
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
    }
}