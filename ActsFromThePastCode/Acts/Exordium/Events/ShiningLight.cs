using ActsFromThePast.Compatibility;
using ActsFromThePast.Enchantments;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class ShiningLight : AftpEventModel
{

    private const decimal HpLossPercent = 0.30M;
    private const int UpgradeCount = 2;

    public override ActModel[] Acts => [ModelDb.Act<ExordiumAct>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Damage", 0),
        new CardsVar(UpgradeCount)
    ];

    public override void CalculateVars()
    {
        if (Owner == null) return;
        DynamicVars["Damage"].BaseValue = Math.Floor(Owner.Creature.MaxHp * HpLossPercent);
    }

    public override void OnRoomEnter()
    {
        AFTPModAudio.Play("events", "shining_light");
    }

    private bool HasUpgradableCards()
    {
        if (Owner == null) return false;
        return PileType.Deck.GetPile(Owner).Cards.Any(c => c is { IsUpgradable: true });
    }

    private bool HasEnchantableAttack()
    {
        if (Owner == null) return false;
        var burnBright = ModelDb.Enchantment<BurnBright>();
        return PileType.Deck.GetPile(Owner).Cards.Any(c => burnBright.CanEnchant(c));
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();

        if (HasUpgradableCards())
            options.Add(Option(Enter).ThatDoesDamage(DynamicVars["Damage"].BaseValue));
        else
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.INITIAL.options.ENTER_LOCKED"));

        if (ActsFromThePastConfig.RebalancedMode)
        {
            if (HasEnchantableAttack())
                options.Add(Option(Bask, "INITIAL",
                    HoverTipFactory.FromEnchantment<BurnBright>().ToArray()));
            else
                options.Add(new EventOption(this, null,
                    $"{Id.Entry}.pages.INITIAL.options.BASK_LOCKED"));
        }
        else
        {
            options.Add(Option(Leave));
        }

        return options;
    }

    private async Task Enter()
    {
        if (Owner == null) return;
        await GameApiCallCompat.Damage(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["Damage"].BaseValue,
            DamageProps.nonCardHpLoss,
            null);

        var upgradableCards = PileType.Deck.GetPile(Owner).Cards
            .Where(c => c is { IsUpgradable: true })
            .ToList()
            .StableShuffle(Owner.RunState.Rng.Niche)
            .Take(DynamicVars.Cards.IntValue);

        foreach (var card in upgradableCards) CardCmd.Upgrade(card);

        SetEventFinished(PageDescription("ENTER"));
    }

    private async Task Bask()
    {
        if (Owner == null) return;
        var burnBright = ModelDb.Enchantment<BurnBright>();
        var eligible = PileType.Deck.GetPile(Owner).Cards
            .Where(c => burnBright.CanEnchant(c))
            .ToList()
            .UnstableShuffle(Owner.RunState.Rng.Niche);

        var card = eligible.FirstOrDefault();
        if (card != null)
        {
            CardCmd.Enchant<BurnBright>(card, 1M);
            var child = NCardEnchantVfx.Create(card);
            if (child != null)
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(child);
        }

        SetEventFinished(PageDescription("BASK"));
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
    }

    public override bool IsAllowed(IRunState runState)
    {
        if (!ActsFromThePastConfig.RebalancedMode)
            return true;

        var burnBright = ModelDb.Enchantment<BurnBright>();
        return runState.Players.All(p =>
            PileType.Deck.GetPile(p).Cards.Any(c => burnBright.CanEnchant(c)));
    }
}