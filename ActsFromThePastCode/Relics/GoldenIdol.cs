using System.Runtime.CompilerServices;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class GoldenIdol : AftpRelicModel
{
    private const decimal GoldMultiplier = 0.25M;

    internal sealed record Boost(int BaseAmount, int Bonus);

    // What the relic added to a reward, for the reward screen's "base (+bonus)" label. Keyed weakly by the reward, so
    // nothing outlives the reward screen.
    internal static readonly ConditionalWeakTable<GoldReward, Boost> BoostedRewards = new();

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || room == null || !room.RoomType.IsCombatRoom())
            return false;

        var modified = false;
        foreach (var reward in rewards.OfType<GoldReward>())
        {
            if (!reward.IsPopulated)
                continue;

            var baseAmount = reward.Amount;
            var bonus = (int)(baseAmount * GoldMultiplier);

            reward.Amount = baseAmount + bonus;
            BoostedRewards.AddOrUpdate(reward, new Boost(baseAmount, bonus));
            modified = true;
        }

        return modified;
    }

    public override Task AfterModifyingRewards()
    {
        Flash();
        return Task.CompletedTask;
    }
}