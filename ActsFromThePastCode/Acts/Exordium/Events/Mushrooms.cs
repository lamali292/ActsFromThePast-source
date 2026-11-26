using ActsFromThePast.Compatibility;
using ActsFromThePast.Cards;
using ActsFromThePast.Patches.RoomEvents;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class Mushrooms : AftpEventModel
{

    private const decimal HEAL_PERCENT = 0.25M;

    public override bool IsShared => true;
    public override EventLayoutType LayoutType => EventLayoutType.Combat;

    public override EncounterModel CanonicalEncounter =>
        ModelDb.Encounter<ThreeFungiBeastsEvent>();

    public override ActModel[] Acts => [ModelDb.Act<ExordiumAct>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HealVar(0)
    ];

    public override bool IsAllowed(IRunState runState)
    {
        return runState.TotalFloor >= 7;
    }

    public override void CalculateVars()
    {
        if (Owner == null) return;
        DynamicVars.Heal.BaseValue =
            Math.Floor(Owner.Creature.MaxHp * HEAL_PERCENT);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var eatHoverTips = ActsFromThePastConfig.RebalancedMode
            ? HoverTipFactory.FromCardWithCardHoverTips<SporeMind>().ToArray()
            : HoverTipFactory.FromCardWithCardHoverTips<Parasite>().ToArray();

        return
        [
            Option(Fight),
            Option(Eat, ActsFromThePastConfig.RebalancedMode ? "INITIAL_REBALANCED" : "INITIAL", eatHoverTips)
        ];
    }

    private Task Fight()
    {
        MushroomPatches.RevealEnemies();
        SetEventState(
            PageDescription("FIGHT"),
            [Option(EnterCombat, "FIGHT")]);
        return Task.CompletedTask;
    }

    private Task EnterCombat()
    {
        if (Owner == null) return Task.CompletedTask;
        var mushroomRelic = ModelDb.Relic<OddMushroom>().ToMutable();
        var rewards = new List<Reward>
        {
            GameApiCallCompat.CreateRelicReward(mushroomRelic, Owner)
        };
        EnterCombatWithoutExitingEvent<ThreeFungiBeastsEvent>(rewards, false);
        return Task.CompletedTask;
    }

    private async Task Eat()
    {
        if (Owner == null) return;
        await CreatureCmd.Heal(
            Owner.Creature,
            DynamicVars.Heal.BaseValue);

        if (ActsFromThePastConfig.RebalancedMode)
            await CardPileCmd.AddCurseToDeck<SporeMind>(Owner);
        else
            await CardPileCmd.AddCurseToDeck<Parasite>(Owner);

        SetEventFinished(PageDescription("EAT"));
    }
}