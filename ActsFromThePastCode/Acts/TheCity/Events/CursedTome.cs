using ActsFromThePast.Compatibility;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class CursedTome : AftpEventModel
{

    private const int DmgPage1 = 1;
    private const int DmgPage2 = 2;
    private const int DmgPage3 = 3;
    private const int DmgStop = 3;
    private const int DmgObtain = 15;
    private const int DmgRebalanced = 6;
    private const int DowngradeBase = 1;
    private const int UpgradeBase = 2;

    private int _downgrades;
    private int _upgrades;

    public override ActModel[] Acts => [ModelDb.Act<TheCityAct>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("DmgPage1", DmgPage1),
        new IntVar("DmgPage2", DmgPage2),
        new IntVar("DmgPage3", DmgPage3),
        new IntVar("DmgStop", DmgStop),
        new IntVar("DmgObtain", DmgObtain),
        new IntVar("DmgRebalanced", DmgRebalanced),
        new IntVar("Downgrades", DowngradeBase),
        new IntVar("Upgrades", UpgradeBase)
    ];

    public override void CalculateVars()
    {
        _downgrades = DowngradeBase;
        _upgrades = UpgradeBase;
        DynamicVars["Downgrades"].BaseValue = _downgrades;
        DynamicVars["Upgrades"].BaseValue = _upgrades;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
            return
            [
                Option(Read, "INITIAL_REBALANCED").ThatDoesDamage(DmgRebalanced),
                PullAwayOption()
            ];

        return
        [
            Option(Read),
            Option(Leave)
        ];
    }

    private void AdvancePullAway()
    {
        _downgrades += DowngradeBase;
        _upgrades += UpgradeBase;
        DynamicVars["Downgrades"].BaseValue = _downgrades;
        DynamicVars["Upgrades"].BaseValue = _upgrades;
    }

    private EventOption PullAwayOption()
    {
        return new EventOption(this, PullAway,
            $"{Id.Entry}.pages.ALL.options.PULL_AWAY").ThatHasDynamicTitle();
    }

    private async Task PullAway()
    {
        if (Owner == null) return;
        var upgradedCards = Owner.Deck.Cards.Where(c => c.IsUpgraded).ToList();
        for (var i = 0; i < _downgrades && upgradedCards.Count > 0; i++)
        {
            var card = Rng.NextRequired(upgradedCards);
            upgradedCards.Remove(card);
            CardCmd.Downgrade(card);
            CardCmd.Preview(card, style: CardPreviewStyle.MessyLayout);
            await Cmd.CustomScaledWait(0.3f, 0.5f);
        }

        var upgradableCards = Owner.Deck.Cards.Where(c => c.IsUpgradable).ToList();
        for (var i = 0; i < _upgrades && upgradableCards.Count > 0; i++)
        {
            var card = Rng.NextRequired(upgradableCards);
            upgradableCards.Remove(card);
            CardCmd.Upgrade(card, CardPreviewStyle.MessyLayout);
            await Cmd.CustomScaledWait(0.3f, 0.5f);
        }

        await Cmd.CustomScaledWait(0.6f, 1.2f);
        SetEventFinished(PageDescription("PULL_AWAY"));
    }

    private async Task Read()
    {
        if (Owner == null) return;
        AFTPModAudio.Play("events", "cursed_tome");
        if (ActsFromThePastConfig.RebalancedMode)
        {
            await GameApiCallCompat.Damage(
                new BlockingPlayerChoiceContext(),
                Owner.Creature,
                DmgRebalanced,
                DamageProps.nonCardHpLoss,
                null);
            AdvancePullAway();
            SetEventState(PageDescription("PAGE_1"), [
                new EventOption(this, Page1Continue,
                    $"{Id.Entry}.pages.ALL.options.CONTINUE_REBALANCED").ThatDoesDamage(DmgRebalanced),
                PullAwayOption()
            ]);
        }
        else
        {
            SetEventState(PageDescription("PAGE_1"), [
                new EventOption(this, Page1Continue,
                    $"{Id.Entry}.pages.PAGE_1.options.CONTINUE").ThatDoesDamage(DmgPage1)
            ]);
        }
    }

    private async Task Page1Continue()
    {
        if (Owner == null) return;
        AFTPModAudio.Play("events", "cursed_tome");
        await GameApiCallCompat.Damage(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            ActsFromThePastConfig.RebalancedMode ? DmgRebalanced : DmgPage1,
            DamageProps.nonCardHpLoss,
            null);

        if (ActsFromThePastConfig.RebalancedMode)
        {
            AdvancePullAway();
            SetEventState(PageDescription("PAGE_2"), [
                new EventOption(this, Page2Continue,
                    $"{Id.Entry}.pages.ALL.options.CONTINUE_REBALANCED").ThatDoesDamage(DmgRebalanced),
                PullAwayOption()
            ]);
        }
        else
        {
            SetEventState(PageDescription("PAGE_2"), [
                new EventOption(this, Page2Continue,
                    $"{Id.Entry}.pages.PAGE_2.options.CONTINUE").ThatDoesDamage(DmgPage2)
            ]);
        }
    }

    private async Task Page2Continue()
    {
        if (Owner == null) return;
        AFTPModAudio.Play("events", "cursed_tome");
        await GameApiCallCompat.Damage(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            ActsFromThePastConfig.RebalancedMode ? DmgRebalanced : DmgPage2,
            DamageProps.nonCardHpLoss,
            null);

        if (ActsFromThePastConfig.RebalancedMode)
        {
            AdvancePullAway();
            SetEventState(PageDescription("PAGE_3"), [
                new EventOption(this, Page3Continue,
                    $"{Id.Entry}.pages.ALL.options.CONTINUE_REBALANCED").ThatDoesDamage(DmgRebalanced),
                PullAwayOption()
            ]);
        }
        else
        {
            SetEventState(PageDescription("PAGE_3"), [
                new EventOption(this, Page3Continue,
                    $"{Id.Entry}.pages.PAGE_3.options.CONTINUE").ThatDoesDamage(DmgPage3)
            ]);
        }
    }

    private async Task Page3Continue()
    {
        if (Owner == null) return;
        AFTPModAudio.Play("events", "cursed_tome");
        await GameApiCallCompat.Damage(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            ActsFromThePastConfig.RebalancedMode ? DmgRebalanced : DmgPage3,
            DamageProps.nonCardHpLoss,
            null);

        if (ActsFromThePastConfig.RebalancedMode)
        {
            AdvancePullAway();
            SetEventState(PageDescription("LAST_PAGE"), [
                Option(Obtain, "LAST_PAGE_REBALANCED").ThatDoesDamage(DmgRebalanced),
                PullAwayOption()
            ]);
        }
        else
        {
            SetEventState(PageDescription("LAST_PAGE"), [
                Option(Obtain, "LAST_PAGE").ThatDoesDamage(DmgObtain),
                Option(Stop, "LAST_PAGE").ThatDoesDamage(DmgStop)
            ]);
        }
    }

    private async Task Obtain()
    {
        if (Owner == null) return;
        await GameApiCallCompat.Damage(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            ActsFromThePastConfig.RebalancedMode ? DmgRebalanced : DmgObtain,
            DamageProps.nonCardHpLoss,
            null);
        var relic = GetRandomBook().ToMutable();
        await RewardsCmd.OfferCustom(Owner, [GameApiCallCompat.CreateRelicReward(relic, Owner)]);
        SetEventFinished(PageDescription("OBTAIN"));
    }

    private async Task Stop()
    {
        if (Owner == null) return;
        await GameApiCallCompat.Damage(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            DmgStop,
            DamageProps.nonCardHpLoss,
            null);
        SetEventFinished(PageDescription("STOP"));
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
    }

    private RelicModel GetRandomBook()
    {
        if (Owner == null) throw new InvalidOperationException("Event has no owner.");
        var possibleBooks = new List<RelicModel>();
        if (!Owner.Relics.Any(r => r is Necronomicon))
            possibleBooks.Add(ModelDb.Relic<Necronomicon>());
        if (!Owner.Relics.Any(r => r is Enchiridion))
            possibleBooks.Add(ModelDb.Relic<Enchiridion>());
        if (!Owner.Relics.Any(r => r is NilrysCodex))
            possibleBooks.Add(ModelDb.Relic<NilrysCodex>());
        return possibleBooks.Count == 0 ? GameApiCallCompat.PullNextRelicFromFront(Owner) : Rng.NextRequired(possibleBooks);
    }
}