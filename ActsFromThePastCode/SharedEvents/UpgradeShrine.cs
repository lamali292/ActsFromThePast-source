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

public sealed class UpgradeShrine : AftpEventModel, IShrineEvent
{

    private const int KneelDamage = 10;
    public override ActModel[] Acts => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("KneelDamage", KneelDamage)
    ];

    public override bool IsAllowed(IRunState runState)
    {
        if (!ActsFromThePastConfig.RebalancedMode)
            return true;

        return runState.Players.All(p =>
            PileType.Deck.GetPile(p).Cards.Any(c => c.IsUpgradable));
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (Owner == null) return [];
        var options = new List<EventOption>();

        if (Owner.Deck.Cards.Any(c => c.IsUpgradable))
            options.Add(Option(Pray));
        else
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.INITIAL.options.PRAY_LOCKED"));

        options.Add(ActsFromThePastConfig.RebalancedMode
            ? Option(Kneel, "INITIAL_REBALANCED").ThatDoesDamage(KneelDamage)
            : Option(Leave));

        return options;
    }

    private async Task Pray()
    {
        if (Owner == null) return;
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1);
        var card = (await CardSelectCmd.FromDeckForUpgrade(Owner, prefs)).FirstOrDefault();

        if (card != null)
            CardCmd.Upgrade(card);

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
        foreach (var card in Owner.Deck.Cards
                     .Where(c => c.IsUpgradable)
                     .ToList()
                     .StableShuffle(Owner.RunState.Rng.Niche)
                     .Take(2))
            CardCmd.Upgrade(card);
        SetEventFinished(PageDescription("PRAY"));
    }
}