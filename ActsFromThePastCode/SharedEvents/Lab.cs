using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;

namespace ActsFromThePast.SharedEvents;

public sealed class Lab : AftpEventModel, IShrineEvent
{

    private const int PotionCount = 2;

    public override ActModel[] Acts => [];

    bool IShrineEvent.IsOneTimeEvent => true;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
            return
            [
                Option(Search),
                Option(Ransack, "INITIAL_REBALANCED")
            ];
        return [Option(Search)];
    }

    public override void OnRoomEnter()
    {
        AFTPModAudio.Play("events", "lab");
    }

    private async Task Search()
    {
        if (Owner == null) return;
        var rewards = new List<Reward>(PotionCount);
        for (var i = 0; i < PotionCount; i++) rewards.Add(new PotionReward(Owner));
        await RewardsCmd.OfferCustom(Owner, rewards);
        SetEventFinished(PageDescription("SEARCH"));
    }

    private async Task Ransack()
    {
        if (Owner == null) return;
        await PlayerCmd.GainMaxPotionCount(1, Owner);
        SetEventFinished(PageDescription("RANSACK"));
    }
}