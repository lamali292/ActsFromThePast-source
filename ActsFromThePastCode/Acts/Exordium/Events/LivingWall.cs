using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class LivingWall : AftpEventModel
{

    public override ActModel[] Acts => [ModelDb.Act<ExordiumAct>()];

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.All(p => p.Deck.Cards.Any(c => c.IsRemovable));
    }

    public override void OnRoomEnter()
    {
        AFTPModAudio.Play("events", "living_wall");
    }

    private bool HasUpgradableCards()
    {
        if (Owner == null) return false;
        return PileType.Deck.GetPile(Owner).Cards.Any(c => c is { IsUpgradable: true });
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>
        {
            Option(Forget),
            Option(Change)
        };

        if (HasUpgradableCards())
            options.Add(Option(Grow));
        else
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.INITIAL.options.GROW_LOCKED"));

        return options;
    }

    private async Task Forget()
    {
        if (Owner == null) return;
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
        var selectedCards = await CardSelectCmd.FromDeckForRemoval(Owner, prefs);
        await CardPileCmd.RemoveFromDeck(selectedCards.ToList());
        SetEventFinished(PageDescription("RESULT"));
    }

    private async Task Change()
    {
        if (Owner == null) return;
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1);
        var selectedCards = await CardSelectCmd.FromDeckForTransformation(Owner, prefs);
        foreach (var card in selectedCards.ToList()) await CardCmd.TransformToRandom(card, Owner.RunState.Rng.Niche);
        SetEventFinished(PageDescription("RESULT"));
    }

    private async Task Grow()
    {
        if (Owner == null) return;
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1);
        var selectedCards = await CardSelectCmd.FromDeckForUpgrade(Owner, prefs);
        foreach (var card in selectedCards) CardCmd.Upgrade(card);
        SetEventFinished(PageDescription("RESULT"));
    }
}