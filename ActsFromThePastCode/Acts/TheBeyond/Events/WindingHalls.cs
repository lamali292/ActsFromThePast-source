using ActsFromThePast.Compatibility;
using ActsFromThePast.Cards;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class WindingHalls : AftpEventModel
{

    private const decimal HpLossPercent = 0.18M;
    private const decimal HealPercent = 0.20M;
    private const decimal MaxHpLossPercent = 0.05M;

    public override ActModel[] Acts => [ModelDb.Act<TheBeyondAct>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(0),
        new HealVar(0),
        new MaxHpVar(0)
    ];

    public override void CalculateVars()
    {
        if (Owner == null) return;
        DynamicVars.HpLoss.BaseValue =
            Math.Round(Owner.Creature.MaxHp * HpLossPercent);
        DynamicVars.Heal.BaseValue =
            Math.Round(Owner.Creature.MaxHp * HealPercent);
        DynamicVars.MaxHp.BaseValue =
            Math.Round(Owner.Creature.MaxHp * MaxHpLossPercent);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [Option(Continue)];
    }

    public override void OnRoomEnter()
    {
        AFTPModAudio.Play("events", "winding_halls");
    }

    private Task Continue()
    {
        if (ActsFromThePastConfig.RebalancedMode)
            SetEventState(PageDescription("CHOICE"), [
                Option(Madness, "CHOICE_REBALANCED",
                    HoverTipFactory.FromCardWithCardHoverTips<Madness>().ToArray()),
                Option(Retreat, "CHOICE")
            ]);
        else
            SetEventState(PageDescription("CHOICE"), [
                Option(Madness, "CHOICE",
                        HoverTipFactory.FromCardWithCardHoverTips<Madness>().ToArray())
                    .ThatDoesDamage(DynamicVars.HpLoss.BaseValue),
                Option(Writhe, "CHOICE",
                    HoverTipFactory.FromCardWithCardHoverTips<Writhe>().ToArray()),
                Option(Retreat, "CHOICE")
            ]);
        return Task.CompletedTask;
    }

    private async Task Madness()
    {
        if (Owner == null) return;
        int count;

        if (ActsFromThePastConfig.RebalancedMode)
        {
            count = 1;
        }
        else
        {
            count = 2;
            await GameApiCallCompat.Damage(
                new BlockingPlayerChoiceContext(),
                Owner.Creature,
                DynamicVars.HpLoss.BaseValue,
                DamageProps.nonCardHpLoss,
                null);
        }

        AFTPModAudio.Play("general", "attack_magic_slow_1");
        for (var i = 0; i < count; i++)
        {
            var card = Owner.RunState.CreateCard(ModelDb.Card<Madness>(), Owner);
            var result = await CardPileCmd.Add(card, PileType.Deck);
            CardCmd.PreviewCardPileAdd([result], 2f);
        }

        await Cmd.Wait(0.75f);
        SetEventFinished(PageDescription("MADNESS"));
    }

    private async Task Writhe()
    {
        if (Owner == null) return;
        await CreatureCmd.Heal(
            Owner.Creature,
            DynamicVars.Heal.BaseValue);
        var card = Owner.RunState.CreateCard(ModelDb.Card<Writhe>(), Owner);
        var result = await CardPileCmd.Add(card, PileType.Deck);
        CardCmd.PreviewCardPileAdd([result], 2f);
        await Cmd.Wait(0.75f);
        SetEventFinished(PageDescription("WRITHE"));
    }

    private async Task Retreat()
    {
        if (Owner == null) return;
        await GameApiCallCompat.LoseMaxHp(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars.MaxHp.BaseValue);
        SetEventFinished(PageDescription("RETREAT"));
    }
}