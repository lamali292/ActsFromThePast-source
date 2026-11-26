using ActsFromThePast.Compatibility;
using ActsFromThePast.Acts.TheBeyond.Encounters;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rewards;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class MindBloom : AftpEventModel
{

    private const int FightGold = 50;
    private const int GoldRewardAmount = 999;
    private bool _isBeforeTreasure;
    public override bool IsShared => true;
    public override ActModel[] Acts => [ModelDb.Act<TheBeyondAct>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new GoldVar(GoldRewardAmount)
    ];

    public override void CalculateVars()
    {
        if (Owner == null) return;
        var threshold = Owner.RunState.Players.Count > 1 ? 38 : 41;
        _isBeforeTreasure = Owner.RunState.TotalFloor < threshold;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>
        {
            Option(Fight),
            Option(Upgrade, "INITIAL", HoverTipFactory.FromRelic(ModelDb.Relic<MarkOfTheBloom>()).ToArray())
        };

        if (_isBeforeTreasure)
            options.Add(Option(Gold, "INITIAL", HoverTipFactory.FromCardWithCardHoverTips<Normality>().ToArray()));
        else
            options.Add(Option(Heal, "INITIAL", HoverTipFactory.FromCardWithCardHoverTips<Doubt>().ToArray()));

        return options;
    }

    private Task Fight()
    {
        if (Owner == null) return Task.CompletedTask;
        var bosses = new List<EncounterModel>
        {
            ModelDb.Encounter<MindBloomGuardian>(),
            ModelDb.Encounter<MindBloomHexaghost>(),
            ModelDb.Encounter<MindBloomSlimeBoss>()
        };
        var encounter = Rng.NextRequired(bosses);
        var rareRelic = RelicFactory.PullNextRelicFromFront(Owner, RelicRarity.Rare).ToMutable();
        var rewards = new List<Reward>
        {
            new GoldReward(FightGold, Owner),
            GameApiCallCompat.CreateRelicReward(rareRelic, Owner)
        };
        EnterCombatWithoutExitingEvent(encounter, rewards, false);
        return Task.CompletedTask;
    }

    private async Task Upgrade()
    {
        if (Owner == null) return;
        var deck = PileType.Deck.GetPile(Owner).Cards;
        foreach (var card in deck)
            if (card.IsUpgradable)
                CardCmd.Upgrade(card);
        var markOfTheBloom = ModelDb.Relic<MarkOfTheBloom>().ToMutable();
        await RelicCmd.Obtain(markOfTheBloom, Owner);
        SetEventFinished(PageDescription("UPGRADE"));
    }

    private async Task Gold()
    {
        if (Owner == null) return;
        await PlayerCmd.GainGold(GoldRewardAmount, Owner);
        for (var i = 0; i < 2; i++)
        {
            var card = Owner.RunState.CreateCard(ModelDb.Card<Normality>(), Owner);
            var result = await CardPileCmd.Add(card, PileType.Deck);
            CardCmd.PreviewCardPileAdd([result], 2f);
        }

        await Cmd.Wait(0.75f);
        SetEventFinished(PageDescription("GOLD"));
    }

    private async Task Heal()
    {
        if (Owner == null) return;
        await CreatureCmd.Heal(Owner.Creature, Owner.Creature.MaxHp);
        var card = Owner.RunState.CreateCard(ModelDb.Card<Doubt>(), Owner);
        var result = await CardPileCmd.Add(card, PileType.Deck);
        CardCmd.PreviewCardPileAdd([result], 2f);
        await Cmd.Wait(0.75f);
        SetEventFinished(PageDescription("HEAL"));
    }
}