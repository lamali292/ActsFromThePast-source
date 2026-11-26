using ActsFromThePast.Compatibility;
using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace ActsFromThePast.SharedEvents;

public sealed class Transmogrifier : AftpEventModel, IShrineEvent
{

    private const int KneelMaxHpLoss = 10;
    public override ActModel[] Acts => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new MaxHpVar(KneelMaxHpLoss)
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
            return
            [
                Option(Pray),
                Option(Kneel, "INITIAL_REBALANCED")
                    .ThatDecreasesMaxHp(KneelMaxHpLoss)
            ];
        return
        [
            Option(Pray),
            Option(Leave)
        ];
    }

    private async Task Pray()
    {
        if (Owner == null) return;
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1);
        foreach (var original in (await CardSelectCmd.FromDeckForTransformation(Owner, prefs)).ToList())
            await CardCmd.TransformToRandom(original, Rng, CardPreviewStyle.EventLayout);

        SetEventFinished(PageDescription("PRAY"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }

    private async Task Kneel()
    {
        if (Owner == null) return;
        await GameApiCallCompat.LoseMaxHp(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            KneelMaxHpLoss);

        var cards = Owner.Deck.Cards
            .Where(c => c.Type != CardType.Quest && c.IsTransformable)
            .ToList()
            .StableShuffle(Owner.RunState.Rng.Niche)
            .Take(2)
            .ToList();

        foreach (var original in cards)
        {
            var transformed = CardFactory.CreateRandomCardForTransform(original, false, Owner.RunState.Rng.Niche);
            CardCmd.Upgrade(transformed);
            await CardCmd.Transform(original, transformed);
        }

        SetEventFinished(PageDescription("PRAY"));
    }
}