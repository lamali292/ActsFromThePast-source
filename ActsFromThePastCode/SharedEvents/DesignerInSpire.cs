using ActsFromThePast.Compatibility;
using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.SharedEvents;

public sealed class DesignerInSpire : AftpEventModel, IActRestricted, IShrineEvent
{

    private const int AdjustCost = 50;
    private const int CleanUpCost = 75;
    private const int FullServiceCost = 110;
    private const int HpLoss = 5;

    private bool _adjustmentUpgradesOne;
    private bool _cleanUpRemovesCards;

    public override ActModel[] Acts => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("AdjustCost", AdjustCost),
        new IntVar("CleanUpCost", CleanUpCost),
        new IntVar("FullServiceCost", FullServiceCost),
        new HpLossVar(HpLoss)
    ];

    public int[] AllowedActIndices => [2, 3];

    bool IShrineEvent.IsOneTimeEvent => true;

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.All(p => p.Gold >= CleanUpCost);
    }

    public override void CalculateVars()
    {
        _adjustmentUpgradesOne = Rng.NextInt(2) == 0;
        _cleanUpRemovesCards = Rng.NextInt(2) == 0;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [Option(Continue)];
    }

    private Task Continue()
    {
        if (Owner == null) return Task.CompletedTask;
        var options = new List<EventOption>();

        var canAffordAdjust = Owner.Gold >= AdjustCost;
        var hasUpgradable = Owner.Deck.Cards.Any(c => c.IsUpgradable);
        var canAffordCleanUp = Owner.Gold >= CleanUpCost;
        var canAffordFullService = Owner.Gold >= FullServiceCost;
        var hasRemovable = Owner.Deck.Cards.Any(c => c.IsRemovable);
        var hasTwoRemovable = Owner.Deck.Cards.Count(c => c.IsRemovable) >= 2;

        // Adjustments
        if (canAffordAdjust && hasUpgradable)
        {
            var optionKey = _adjustmentUpgradesOne
                ? $"{Id.Entry}.pages.MAIN.options.ADJUST_UPGRADE_ONE"
                : $"{Id.Entry}.pages.MAIN.options.ADJUST_UPGRADE_TWO";
            options.Add(new EventOption(this,
                _adjustmentUpgradesOne ? AdjustUpgradeOne : AdjustUpgradeTwo,
                optionKey));
        }
        else
        {
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.MAIN.options.ADJUST_LOCKED"));
        }

        // Clean Up
        if (_cleanUpRemovesCards)
        {
            if (canAffordCleanUp && hasRemovable)
                options.Add(new EventOption(this, CleanUpRemove,
                    $"{Id.Entry}.pages.MAIN.options.CLEANUP_REMOVE"));
            else
                options.Add(new EventOption(this, null,
                    $"{Id.Entry}.pages.MAIN.options.CLEANUP_LOCKED"));
        }
        else
        {
            if (canAffordCleanUp && hasTwoRemovable)
                options.Add(new EventOption(this, CleanUpTransform,
                    $"{Id.Entry}.pages.MAIN.options.CLEANUP_TRANSFORM"));
            else
                options.Add(new EventOption(this, null,
                    $"{Id.Entry}.pages.MAIN.options.CLEANUP_LOCKED"));
        }

        // Full Service
        if (canAffordFullService && hasRemovable)
            options.Add(new EventOption(this, FullService,
                $"{Id.Entry}.pages.MAIN.options.FULL_SERVICE"));
        else
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.MAIN.options.FULL_SERVICE_LOCKED"));

        // Punch
        options.Add(new EventOption(this, Punch,
            $"{Id.Entry}.pages.MAIN.options.PUNCH").ThatDoesDamage(HpLoss));

        SetEventState(PageDescription("MAIN"), options);
        return Task.CompletedTask;
    }

    private async Task AdjustUpgradeOne()
    {
        if (Owner == null) return;
        await PlayerCmd.LoseGold(AdjustCost, Owner, GoldLossType.Spent);
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1);
        var card = (await CardSelectCmd.FromDeckForUpgrade(Owner, prefs)).FirstOrDefault();
        if (card != null)
            CardCmd.Upgrade(card);
        SetEventFinished(PageDescription("SERVICE"));
    }

    private async Task AdjustUpgradeTwo()
    {
        if (Owner == null) return;
        await PlayerCmd.LoseGold(AdjustCost, Owner, GoldLossType.Spent);
        foreach (var card in Owner.Deck.Cards
                     .Where(c => c.IsUpgradable)
                     .ToList()
                     .StableShuffle(Owner.RunState.Rng.Niche)
                     .Take(2))
            CardCmd.Upgrade(card);
        SetEventFinished(PageDescription("SERVICE"));
    }

    private async Task CleanUpRemove()
    {
        if (Owner == null) return;
        await PlayerCmd.LoseGold(CleanUpCost, Owner, GoldLossType.Spent);
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
        await CardPileCmd.RemoveFromDeck(
            (await CardSelectCmd.FromDeckForRemoval(Owner, prefs)).ToList());
        SetEventFinished(PageDescription("SERVICE"));
    }

    private async Task CleanUpTransform()
    {
        if (Owner == null) return;
        await PlayerCmd.LoseGold(CleanUpCost, Owner, GoldLossType.Spent);
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 2);
        foreach (var original in (await CardSelectCmd.FromDeckForTransformation(Owner, prefs)).ToList())
            await CardCmd.TransformToRandom(original, Rng, CardPreviewStyle.EventLayout);
        SetEventFinished(PageDescription("SERVICE"));
    }

    private async Task FullService()
    {
        if (Owner == null) return;
        await PlayerCmd.LoseGold(FullServiceCost, Owner, GoldLossType.Spent);
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
        await CardPileCmd.RemoveFromDeck(
            (await CardSelectCmd.FromDeckForRemoval(Owner, prefs)).ToList());

        var upgradable = Owner.Deck.Cards
            .Where(c => c.IsUpgradable)
            .ToList()
            .StableShuffle(Owner.RunState.Rng.Niche)
            .Take(1);
        foreach (var card in upgradable)
            CardCmd.Upgrade(card);

        SetEventFinished(PageDescription("SERVICE"));
    }

    private async Task Punch()
    {
        if (Owner == null) return;
        await GameApiCallCompat.Damage(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            HpLoss,
            DamageProps.nonCardHpLoss,
            null);

        if (Node?.FindChild("Portrait", true, false) is TextureRect portrait)
            portrait.Texture = PreloadManager.Cache.GetTexture2D(
                "res://ActsFromThePast/images/events/designer_in_spire_punched.png");

        NDebugAudioManager.Instance?.Play("blunt_attack.mp3");

        SetEventFinished(PageDescription("PUNCH"));
    }
}