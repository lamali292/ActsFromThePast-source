using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.SharedEvents;

public sealed class TheDivineFountain : AftpEventModel, IShrineEvent
{

    private const int MaxHpPerCurse = 3;

    public override ActModel[] Acts => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new MaxHpVar(0)
    ];

    bool IShrineEvent.IsOneTimeEvent => true;

    public override void CalculateVars()
    {
        if (Owner == null) return;
        if (!ActsFromThePastConfig.RebalancedMode) return;
        var curseCount = Owner.Deck.Cards.Count(c => c.Type == CardType.Curse);
        DynamicVars.MaxHp.BaseValue = curseCount * MaxHpPerCurse;
    }

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.All(p =>
            PileType.Deck.GetPile(p).Cards.Any(c =>
                c is { Type: CardType.Curse, IsRemovable: true } and not Guilty));
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
            return
            [
                Option(Drink),
                Option(Bathe, "INITIAL_REBALANCED")
            ];
        return
        [
            Option(Drink),
            Option(Leave)
        ];
    }

    private async Task Drink()
    {
        if (Owner == null) return;
        var curses = Owner.Deck.Cards
            .Where(c => c is { Type: CardType.Curse, IsRemovable: true })
            .ToList();

        await CardPileCmd.RemoveFromDeck(curses);

        SetEventFinished(PageDescription("DRINK"));
    }

    private async Task Bathe()
    {
        if (Owner == null) return;
        var curseCount = Owner.Deck.Cards.Count(c => c.Type == CardType.Curse);
        var maxHpGain = curseCount * MaxHpPerCurse;

        if (maxHpGain > 0)
            await CreatureCmd.GainMaxHp(Owner.Creature, maxHpGain);

        SetEventFinished(PageDescription("BATHE"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}