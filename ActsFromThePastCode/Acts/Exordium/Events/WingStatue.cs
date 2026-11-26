using ActsFromThePast.Compatibility;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class WingStatue : AftpEventModel
{

    private const int Damage = 7;
    private const int RequiredDamage = 10;
    private const int MinGold = 50;
    private const int MaxGold = 80;
    private const int RebalancedMinGold = 60;
    private const int RebalancedMaxGold = 95;

    public override ActModel[] Acts => [ModelDb.Act<ExordiumAct>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Damage", Damage),
        new GoldVar(0),
        new IntVar("RequiredDamage", RequiredDamage)
    ];

    public override void CalculateVars()
    {
        if (ActsFromThePastConfig.RebalancedMode)
            DynamicVars.Gold.BaseValue = RebalancedMinGold + Rng.NextInt(RebalancedMaxGold - RebalancedMinGold + 1);
        else
            DynamicVars.Gold.BaseValue = MinGold + Rng.NextInt(MaxGold - MinGold + 1);
    }

    private bool CanAttack()
    {
        if (Owner == null) return false;
        return Owner.Deck.Cards.Any(c =>
            c.Type == CardType.Attack &&
            c.DynamicVars.ContainsKey("Damage") &&
            c.DynamicVars.Damage.BaseValue >= RequiredDamage);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>
        {
            Option(Agree).ThatDoesDamage(Damage)
        };

        if (ActsFromThePastConfig.RebalancedMode)
        {
            options.Add(Option(Attack));
        }
        else
        {
            if (CanAttack())
                options.Add(Option(Attack));
            else
                options.Add(new EventOption(this, null,
                    $"{Id.Entry}.pages.INITIAL.options.ATTACK_LOCKED"));

            options.Add(Option(Leave));
        }

        return options;
    }

    private async Task Agree()
    {
        if (Owner == null) return;
        await GameApiCallCompat.Damage(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            Damage,
            DamageProps.nonCardHpLoss,
            null);

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
        var selectedCards = await CardSelectCmd.FromDeckForRemoval(Owner, prefs);
        await CardPileCmd.RemoveFromDeck(selectedCards.ToList());
        SetEventFinished(PageDescription("AGREE"));
    }

    private async Task Attack()
    {
        if (Owner == null) return;
        await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
        SetEventFinished(PageDescription("ATTACK"));
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
    }
}