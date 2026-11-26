using System.Reflection;
using ActsFromThePast.Interfaces;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class Nloth : AftpEventModel, IShrineEvent
{

    private const string Choice1RelicKey = "Choice1Relic";
    private const string Choice2RelicKey = "Choice2Relic";

    public override ActModel[] Acts => [ModelDb.Act<TheCityAct>()];

    private IReadOnlyList<RelicModel> ChoiceRelics
    {
        get
        {
            AssertMutable();
            if (field == null)
                field = GetValidRelics(Owner ?? throw new InvalidOperationException("Event has no owner."))
                    .ToList()
                    .StableShuffle(Rng)
                    .Take(2)
                    .ToList();
            return field;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar(Choice1RelicKey),
        new StringVar(Choice2RelicKey)
    ];

    bool IShrineEvent.IsOneTimeEvent => true;

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.All(p => GetValidRelics(p).Count() >= 2);
    }

    private IEnumerable<RelicModel> GetValidRelics(Player player)
    {
        return player.Relics.Where(r => r.IsTradable);
    }

    public override void CalculateVars()
    {
        ((StringVar)DynamicVars[Choice1RelicKey]).StringValue = ChoiceRelics[0].Title.GetFormattedText();
        ((StringVar)DynamicVars[Choice2RelicKey]).StringValue = ChoiceRelics[1].Title.GetFormattedText();
    }

    public override void OnRoomEnter()
    {
        AFTPModAudio.Play("events", "ssserpent");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
            return
            [
                new EventOption(this, TradeChoice1,
                    $"{Id.Entry}.pages.INITIAL.options.TRADE_1",
                    GetTradeHoverTips(0).ToArray()),
                new EventOption(this, TradeChoice2,
                    $"{Id.Entry}.pages.INITIAL.options.TRADE_2",
                    GetTradeHoverTips(1).ToArray()),
                Option(SearchWithNloth, "INITIAL_REBALANCED")
            ];

        return
        [
            new EventOption(this, TradeChoice1,
                $"{Id.Entry}.pages.INITIAL.options.TRADE_1",
                GetTradeHoverTips(0).ToArray()),
            new EventOption(this, TradeChoice2,
                $"{Id.Entry}.pages.INITIAL.options.TRADE_2",
                GetTradeHoverTips(1).ToArray()),
            Option(Leave)
        ];
    }

    private IEnumerable<IHoverTip> GetTradeHoverTips(int index)
    {
        var giftRelic = ModelDb.Relic<NlothsGift>();
        return ChoiceRelics[index].HoverTips.Concat(giftRelic.HoverTips);
    }

    private async Task TradeChoice1()
    {
        await Trade(0);
    }

    private async Task TradeChoice2()
    {
        await Trade(1);
    }

    private async Task Trade(int index)
    {
        if (Owner == null) return;
        await RelicCmd.Remove(ChoiceRelics[index]);
        var gift = ModelDb.Relic<NlothsGift>().ToMutable();
        await RelicCmd.Obtain(gift, Owner);
        SetEventFinished(PageDescription("TRADE"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }

    private async Task SearchWithNloth()
    {
        if (Owner == null) return;
        var trashHeapCards = TrashHeap.Cards as CardModel[];

        var options = CardCreationOptions
            .ForNonCombatWithUniformOdds(
                [Owner.Character.CardPool],
                c => c.Rarity == CardRarity.Common)
            .WithFlags(CardCreationFlags.NoRarityModification);

        var results = CardFactory.CreateForReward(Owner, 5, options).ToList();

        if (trashHeapCards != null)
        {
            var shuffled = trashHeapCards.ToList().StableShuffle(Rng);
            var trashIndex = 0;
            for (var i = 0; i < results.Count; i++)
                if (Rng.NextInt(100) < 15 && trashIndex < shuffled.Count)
                {
                    var created = Owner.RunState.CreateCard(shuffled[trashIndex], Owner);
                    results[i] = new CardCreationResult(created);
                    trashIndex++;
                }
        }

        var prefs = new CardSelectorPrefs(
            L10NLookup($"{Id.Entry}.pages.SEARCH_WITH_NLOTH.selectionScreenPrompt"), 1);
        foreach (var card in await CardSelectCmd.FromSimpleGridForRewards(
                     new BlockingPlayerChoiceContext(), results, Owner, prefs))
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));

        SetEventFinished(PageDescription("SEARCH_WITH_NLOTH"));
    }
}