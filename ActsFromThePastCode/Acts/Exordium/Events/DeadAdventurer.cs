using ActsFromThePast.Compatibility;
using ActsFromThePast.Patches.RoomEvents;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class DeadAdventurer : AftpEventModel
{

    private const int GoldReward = 30;
    private const int EncounterChanceStart = 35;
    private const int EncounterChanceRamp = 25;

    private int _encounterChance;
    private int _enemyType;
    private int _numSearches;
    private List<RewardType> _rewards = [];
    public override ActModel[] Acts => [ModelDb.Act<ExordiumAct>()];

    public override bool IsShared => true;
    public override EventLayoutType LayoutType => EventLayoutType.Combat;

    public override EncounterModel CanonicalEncounter => _enemyType switch
    {
        0 => ModelDb.Encounter<DeadAdventurerSentries>(),
        1 => ModelDb.Encounter<DeadAdventurerGremlinNob>(),
        _ => ModelDb.Encounter<DeadAdventurerLagavulin>()
    };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("EncounterChance", EncounterChanceStart)
    ];

    public override LocString InitialDescription
    {
        get
        {
            var key = _enemyType switch
            {
                0 => "ACTSFROMTHEPAST-DEAD_ADVENTURER.pages.INITIAL.description.SENTRIES",
                1 => "ACTSFROMTHEPAST-DEAD_ADVENTURER.pages.INITIAL.description.NOB",
                _ => "ACTSFROMTHEPAST-DEAD_ADVENTURER.pages.INITIAL.description.LAGAVULIN"
            };
            return L10NLookup(key);
        }
    }

    public override bool IsAllowed(IRunState runState)
    {
        return runState.TotalFloor >= 7;
    }

    public override void CalculateVars()
    {
        _encounterChance = EncounterChanceStart;
        _numSearches = 0;
        _enemyType = Rng.NextInt(3);

        _rewards = [RewardType.Gold, RewardType.Nothing, RewardType.Relic];
        for (var i = _rewards.Count - 1; i > 0; i--)
        {
            var j = Rng.NextInt(i + 1);
            (_rewards[i], _rewards[j]) = (_rewards[j], _rewards[i]);
        }

        DynamicVars["EncounterChance"].BaseValue = _encounterChance;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
            return
            [
                Option(Search),
                Option(WalkAway, "INITIAL_REBALANCED")
            ];

        return
        [
            Option(Search),
            Option(Leave)
        ];
    }

    private async Task Search()
    {
        if (Rng.NextInt(100) < _encounterChance)
            await TriggerCombat();
        else
            await GrantReward();
    }

    private Task TriggerCombat()
    {
        DeadAdventurerPatches.RevealEnemies();
        SetEventState(
            PageDescription("FIGHT"),
            [Option(EnterCombat, "FIGHT")]);
        return Task.CompletedTask;
    }

    private Task EnterCombat()
    {
        if (Owner == null) return Task.CompletedTask;
        var rewards = new List<Reward>
        {
            GameApiCallCompat.CreateCardReward(
                CardCreationOptions.ForRoom(Owner, RoomType.Elite),
                3, Owner),
            new GoldReward(25, 35, Owner)
        };

        foreach (var rewardType in _rewards)
            switch (rewardType)
            {
                case RewardType.Gold:
                    rewards.Add(new GoldReward(GoldReward, GoldReward, Owner));
                    break;
                case RewardType.Relic:
                    var relic = GameApiCallCompat.PullNextRelicFromFront(Owner).ToMutable();
                    rewards.Add(GameApiCallCompat.CreateRelicReward(relic, Owner));
                    break;
            }

        EnterCombatWithoutExitingEvent(
            CanonicalEncounter,
            rewards,
            false);
        return Task.CompletedTask;
    }

    private async Task GrantReward()
    {
        if (Owner == null) return;
        _numSearches++;
        _encounterChance += EncounterChanceRamp;
        DynamicVars["EncounterChance"].BaseValue = _encounterChance;

        var reward = _rewards[0];
        _rewards.RemoveAt(0);
        var wasLastReward = _numSearches >= 3;

        switch (reward)
        {
            case RewardType.Gold:
                await PlayerCmd.GainGold(GoldReward, Owner);
                break;
            case RewardType.Nothing:
                break;
            case RewardType.Relic:
                var relic = GameApiCallCompat.PullNextRelicFromFront(Owner).ToMutable();
                await RelicCmd.Obtain(relic, Owner);
                break;
        }

        if (wasLastReward)
        {
            SetEventFinished(PageDescription("SUCCESS"));
        }
        else
        {
            var pageKey = reward switch
            {
                RewardType.Gold => "GOLD",
                RewardType.Nothing => "NOTHING",
                RewardType.Relic => "RELIC",
                _ => "NOTHING"
            };
            SetEventState(PageDescription(pageKey), GetPostRewardOptions());
        }
    }

    private IReadOnlyList<EventOption> GetPostRewardOptions()
    {
        if (_numSearches >= 3) return [Option(Leave, "SUCCESS")];

        return
        [
            Option(Search),
            Option(Leave)
        ];
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }

    private Task WalkAway()
    {
        if (Owner == null) return Task.CompletedTask;
        var upgradable = Owner.Deck.Cards.Where(c => c.IsUpgradable);
        if (upgradable.Any())
            CardCmd.Upgrade(Rng.NextRequired(upgradable));

        SetEventFinished(PageDescription("WALK_AWAY"));
        return Task.CompletedTask;
    }

    private enum RewardType
    {
        Gold,
        Nothing,
        Relic
    }
}