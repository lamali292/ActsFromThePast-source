using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast.Powers;

public sealed class TimeWarpPower : AftpPowerModel
{
    private const int StrengthAmount = 2;
    private const int CountdownPerPlayer = 12;
    private const string CardCountKey = "CardCount";
    private const string CountdownKey = "Countdown";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldScaleInMultiplayer => false;
    public override int DisplayAmount => DynamicVars[CardCountKey].IntValue;

    private int CountdownAmount => DynamicVars[CountdownKey].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new(CardCountKey, 0M),
        new(CountdownKey, CountdownPerPlayer)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        DynamicVars[CountdownKey].BaseValue = CountdownPerPlayer * Owner.CombatState!.Players.Count;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        DynamicVars[CardCountKey].BaseValue++;
        InvokeDisplayAmountChanged();
        if (DynamicVars[CardCountKey].IntValue >= CountdownAmount)
        {
            DynamicVars[CardCountKey].BaseValue = 0M;
            InvokeDisplayAmountChanged();
            Flash();
            AFTPModAudio.Play("time_eater", "time_warp");
            BorderFlashEffect.PlayGold();
            var effect = TimeWarpTurnEndEffect.Create();
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(effect);
            foreach (var player in Owner.CombatState!.Players)
                PlayerCmd.EndTurn(player, false);
            foreach (var enemy in Owner.CombatState!.Enemies.Where(e => e.IsAlive))
                await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), enemy, StrengthAmount, Owner,
                    null);
        }
    }
}