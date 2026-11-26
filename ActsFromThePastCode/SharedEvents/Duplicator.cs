using ActsFromThePast.Compatibility;
using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.SharedEvents;

public sealed class Duplicator : AftpEventModel, IShrineEvent
{

    private const int KneelDamage = 5;
    public override ActModel[] Acts => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("KneelDamage", KneelDamage)
    ];

    bool IShrineEvent.IsOneTimeEvent => true;

    public override bool IsAllowed(IRunState runState)
    {
        if (!ActsFromThePastConfig.RebalancedMode)
            return true;

        return runState.Players.All(p =>
            PileType.Deck.GetPile(p).Cards.Count(c => c.IsUpgraded) >= 2);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
            return
            [
                Option(Pray),
                Option(Kneel, "INITIAL_REBALANCED").ThatDoesDamage(KneelDamage)
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
        var prefs = new CardSelectorPrefs(L10NLookup($"{Id.Entry}.SELECT_DUPLICATE"), 1);
        var card = (await CardSelectCmd.FromDeckGeneric(Owner, prefs)).FirstOrDefault();

        if (card != null)
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(Owner.RunState.CloneCard(card), PileType.Deck));

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
        await GameApiCallCompat.Damage(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            KneelDamage,
            DamageProps.nonCardHpLoss,
            null);

        var upgraded = Owner.Deck.Cards
            .Where(c => c.IsUpgraded)
            .ToList()
            .StableShuffle(Owner.RunState.Rng.Niche)
            .Take(2)
            .ToList();
        foreach (var card in upgraded)
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.Add(Owner.RunState.CloneCard(card), PileType.Deck));
        SetEventFinished(PageDescription("PRAY"));
    }
}