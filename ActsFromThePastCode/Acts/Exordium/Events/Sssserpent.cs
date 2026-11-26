using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Rewards;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class Sssserpent : AftpEventModel
{

    private static int GoldReward => ActsFromThePastConfig.RebalancedMode ? 250 : 150;

    public override ActModel[] Acts => [ModelDb.Act<ExordiumAct>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new GoldVar(GoldReward)
    ];

    public override void OnRoomEnter()
    {
        AFTPModAudio.Play("events", "ssserpent");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
            return
            [
                Option(Agree, "INITIAL", HoverTipFactory.FromCard(ModelDb.Card<Doubt>())),
                Option(DisagreeRebalanced, "INITIAL_REBALANCED")
            ];

        return
        [
            Option(Agree, "INITIAL", HoverTipFactory.FromCard(ModelDb.Card<Doubt>())),
            Option(Disagree)
        ];
    }

    private Task Agree()
    {
        SetEventState(PageDescription("AGREE"), [
            Option(TakeGold, "AGREE")
        ]);
        return Task.CompletedTask;
    }

    private async Task TakeGold()
    {
        if (Owner == null) return;
        await CardPileCmd.AddCurseToDeck<Doubt>(Owner);
        await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
        SetEventFinished(PageDescription("TAKE_GOLD"));
    }

    private Task Disagree()
    {
        SetEventFinished(PageDescription("DISAGREE"));
        return Task.CompletedTask;
    }


    private async Task DisagreeRebalanced()
    {
        if (Owner == null) return;
        var potions = Owner.Character.PotionPool
            .GetUnlockedPotions(Owner.UnlockState)
            .Concat(ModelDb.PotionPool<SharedPotionPool>().GetUnlockedPotions(Owner.UnlockState))
            .Where(p => p.Rarity == PotionRarity.Uncommon);
        var potion = Owner.PlayerRng.Rewards.NextItem(potions);
        if (potion != null)
            await RewardsCmd.OfferCustom(Owner, [new PotionReward(potion.ToMutable(), Owner)]);
        SetEventFinished(PageDescription("DISAGREE_REBALANCED"));
    }
}