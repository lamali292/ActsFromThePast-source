using ActsFromThePast.Compatibility;
using ActsFromThePast.Cards;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class Vampires : AftpEventModel
{

    private const decimal HpDrainPercent = 0.3M;
    private const int BiteCount = 5;

    public override ActModel[] Acts => [ModelDb.Act<TheCityAct>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new MaxHpVar(0)
    ];

    public override void CalculateVars()
    {
        if (Owner == null) return;
        var maxHpLoss = (int)Math.Ceiling(Owner.Creature.MaxHp * HpDrainPercent);
        if (maxHpLoss >= Owner.Creature.MaxHp)
            maxHpLoss = Owner.Creature.MaxHp - 1;
        DynamicVars.MaxHp.BaseValue = maxHpLoss;
    }

    private bool HasBloodVial()
    {
        return Owner != null && Owner.Relics.Any(r => r is BloodVial);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>
        {
            Option(Accept, "INITIAL", HoverTipFactory.FromCard(ModelDb.Card<Bite>()))
        };
        if (HasBloodVial())
            options.Add(Option(Vial, "INITIAL", HoverTipFactory.FromCard(ModelDb.Card<Bite>())));

        options.Add(ActsFromThePastConfig.RebalancedMode
            ? Option(Hesitate, "INITIAL_REBALANCED", HoverTipFactory.FromRelic<BloodBank>().ToArray())
            : Option(Leave));

        return options;
    }

    private async Task ReplaceStrikesWithBites()
    {
        if (Owner == null) return;
        var strikesToRemove = Owner.Deck.Cards
            .Where(c => c.Rarity == CardRarity.Basic && c.Tags.Contains(CardTag.Strike))
            .ToList();
        foreach (var strike in strikesToRemove) await CardPileCmd.RemoveFromDeck([strike]);

        var biteResults = new List<CardPileAddResult>();
        for (var i = 0; i < BiteCount; i++)
        {
            var bite = Owner.RunState.CreateCard(ModelDb.Card<Bite>(), Owner);
            biteResults.Add(await CardPileCmd.Add(bite, PileType.Deck));
        }

        CardCmd.PreviewCardPileAdd(biteResults, 2f);
        await Cmd.Wait(0.75f);
    }

    private async Task Accept()
    {
        if (Owner == null) return;
        AFTPModAudio.Play("general", "bite");
        var maxHpLoss = (int)DynamicVars.MaxHp.BaseValue;
        await GameApiCallCompat.LoseMaxHp(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            maxHpLoss);
        await ReplaceStrikesWithBites();
        SetEventFinished(PageDescription("ACCEPT"));
    }

    private async Task Vial()
    {
        if (Owner == null) return;
        AFTPModAudio.Play("general", "bite");
        var vial = Owner.Relics.First(r => r is BloodVial);
        await RelicCmd.Remove(vial);
        await ReplaceStrikesWithBites();
        SetEventFinished(PageDescription("VIAL"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }

    private async Task Hesitate()
    {
        if (Owner == null) return;
        await RelicCmd.Obtain<BloodBank>(Owner);
        SetEventFinished(PageDescription("HESITATE"));
    }
}