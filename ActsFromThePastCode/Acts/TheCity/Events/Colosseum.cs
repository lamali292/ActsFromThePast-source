using ActsFromThePast.Compatibility;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class Colosseum : AftpEventModel
{

    public static bool NeedsReplayFix;

    public override ActModel[] Acts => [ModelDb.Act<TheCityAct>()];
    public override bool IsShared => true;

    public override bool IsAllowed(IRunState runState)
    {
        return runState is { TotalFloor: >= 23, Players.Count: 1 };
    }

    public override void OnRoomEnter()
    {
        NeedsReplayFix = false;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [Option(Enter)];
    }

    private Task Enter()
    {
        SetEventState(
            PageDescription("FIGHT_INTRO"),
            [Option(Fight, "FIGHT_INTRO")]);
        return Task.CompletedTask;
    }

    private Task Fight()
    {
        EnterCombatWithoutExitingEvent(
            ModelDb.Encounter<ColosseumFirstEncounter>(),
            [],
            true);
        return Task.CompletedTask;
    }

    public override Task Resume(AbstractRoom room)
    {
        _combatSynchronizer?.ResetState();
        _combatSynchronizer?.InitializeForEvent(this);

        if (ActsFromThePastConfig.RebalancedMode)
            SetEventState(
                PageDescription("POST_SLAVERS"),
                [
                    Option(FightAgain, "POST_SLAVERS"),
                    Option(FleeRebalanced, "POST_SLAVERS_REBALANCED")
                ]);
        else
            SetEventState(
                PageDescription("POST_SLAVERS"),
                [
                    Option(FightAgain, "POST_SLAVERS"),
                    Option(Flee, "POST_SLAVERS")
                ]);
        return Task.CompletedTask;
    }

    private Task FightAgain()
    {
        if (Owner == null) return Task.CompletedTask;
        NeedsReplayFix = true;
        var rareRelic = RelicFactory.PullNextRelicFromFront(Owner, RelicRarity.Rare).ToMutable();
        var uncommonRelic = RelicFactory.PullNextRelicFromFront(Owner, RelicRarity.Uncommon).ToMutable();
        var rewards = new List<Reward>
        {
            GameApiCallCompat.CreateRelicReward(rareRelic, Owner),
            GameApiCallCompat.CreateRelicReward(uncommonRelic, Owner),
            new GoldReward(100, Owner)
        };
        EnterCombatWithoutExitingEvent(
            ModelDb.Encounter<ColosseumSecondEncounter>(),
            rewards,
            false);
        return Task.CompletedTask;
    }

    private Task Flee()
    {
        SetEventFinished(PageDescription("FLEE"));
        return Task.CompletedTask;
    }

    private async Task FleeRebalanced()
    {
        if (Owner == null) return;
        var upgradedCards = Owner.Deck.Cards.Where(c => c.IsUpgraded).ToList();
        if (upgradedCards.Count > 0)
        {
            var card = Rng.NextRequired(upgradedCards);
            CardCmd.Downgrade(card);
            CardCmd.Preview(card, style: CardPreviewStyle.MessyLayout);
            await Cmd.CustomScaledWait(0.3f, 0.5f);
        }

        await CreatureCmd.GainMaxHp(Owner.Creature, 5);
        SetEventFinished(PageDescription("FLEE_REBALANCED"));
    }
}