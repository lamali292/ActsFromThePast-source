using ActsFromThePast.Compatibility;
using ActsFromThePast.Interfaces;
using ActsFromThePast.Minigames;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.SharedEvents;

public sealed class WheelOfChange : AftpEventModel, IShrineEvent
{

    private const decimal HpLossPercent = 0.15M;

    public override ActModel[] Acts => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(0),
        new GoldVar(0)
    ];

    public override void CalculateVars()
    {
        if (Owner == null) return;
        DynamicVars.HpLoss.BaseValue =
            (int)(Owner.Creature.MaxHp * HpLossPercent);
        DynamicVars.Gold.BaseValue = GetGoldAmount();
    }

    private int GetGoldAmount()
    {
        if (Owner == null) return 0;
        var actIndex = Owner.RunState.CurrentActIndex;
        return actIndex switch
        {
            0 => 100,
            1 => 200,
            _ => 300
        };
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [Option(Play)];
    }

    private async Task Play()
    {
        if (Owner == null) return;
        for (var i = 0; i < Owner.RunState.CurrentActIndex; i++)
            Rng.NextInt(1);

        var result = Rng.NextInt(6);
        var minigame = new WheelSpinMinigame(Owner, result, Owner.RunState.CurrentActIndex);
        await minigame.PlayMinigame();
        ShowResult(result);
    }

    private void ShowResult(int result)
    {
        var (pageKey, optionKey) = result switch
        {
            0 => ("GOLD", "PRIZE_GOLD"),
            1 => ("RELIC", "PRIZE_RELIC"),
            2 => ("HEAL", "PRIZE_HEAL"),
            3 => ("CURSE", "PRIZE_CURSE"),
            4 => ("REMOVE", "PRIZE_REMOVE"),
            _ => ("DAMAGE", "PRIZE_DAMAGE")
        };

        SetEventState(PageDescription(pageKey), [
            new EventOption(this,
                () => ApplyResult(result),
                $"{Id.Entry}.pages.RESULT.options.{optionKey}")
        ]);
    }

    private async Task ApplyResult(int result)
    {
        if (Owner == null) return;
        switch (result)
        {
            case 0:
                await PlayerCmd.GainGold(DynamicVars.Gold.IntValue, Owner);
                break;
            case 1:
                await RewardsCmd.OfferCustom(Owner, [new RelicReward(Owner)]);
                break;
            case 2:
                await CreatureCmd.Heal(Owner.Creature, Owner.Creature.MaxHp);
                break;
            case 3:
                await CardPileCmd.AddCurseToDeck<Decay>(Owner);
                break;
            case 4:
                var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
                await CardPileCmd.RemoveFromDeck(
                    (await CardSelectCmd.FromDeckForRemoval(Owner, prefs)).ToList());
                break;
            default:
                await GameApiCallCompat.Damage(
                    new BlockingPlayerChoiceContext(),
                    Owner.Creature,
                    DynamicVars.HpLoss.BaseValue,
                    DamageProps.nonCardHpLoss,
                    null);
                SfxCmd.Play("event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack");
                SetEventFinished(PageDescription("DAMAGE_RESULT"));
                return;
        }

        SetEventFinished(PageDescription("LEAVE"));
    }
}