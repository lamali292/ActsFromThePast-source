using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ActsFromThePast.Dread;

/// <summary>
///     Time Eater's Dominion, on the player: counts the cards this player plays; at 10 their turn ends and all enemies gain 2 Strength.
///     The counter is per player, so in multiplayer every player has their own and only their own turn ends.
/// </summary>
public sealed class TimeWarpDominionPower : DreadPowerModel
{
    private const int CardsPerWarp = 10;
    private const int StrengthAmount = 2;
    private const string CardCountKey = "CardCount";
    private const string CountdownKey = "Countdown";

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldScaleInMultiplayer => false;
    public override int DisplayAmount => DynamicVars[CardCountKey].IntValue;

    protected override string IconEntry => "BORROWED_TIME_POWER";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new(CardCountKey, 0M),
        new(CountdownKey, CardsPerWarp)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Combat hooks reach every power for every card played, so only this player's own cards count.
        if (Owner.Player == null || cardPlay.Card.Owner != Owner.Player) return;

        DynamicVars[CardCountKey].BaseValue++;
        InvokeDisplayAmountChanged();
        if (DynamicVars[CardCountKey].IntValue < DynamicVars[CountdownKey].IntValue) return;

        DynamicVars[CardCountKey].BaseValue = 0M;
        InvokeDisplayAmountChanged();
        Flash();
        PlayerCmd.EndTurn(Owner.Player, false);

        var enemies = Owner.CombatState?.Enemies.Where(e => e.IsAlive).ToList();
        if (enemies is { Count: > 0 })
            await PowerCmd.Apply<StrengthPower>(choiceContext, enemies, StrengthAmount, Owner, null);
    }
}
