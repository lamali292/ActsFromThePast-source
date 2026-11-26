using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ActsFromThePast.Dread;

/// <summary>
///     Hexaghost's Dominion, on the player: at the start of every 2nd turn (2, 4, 6, ...) Amount Burns are added to your hand
///     before the draw. The counter is display only: 1 on the turn before a Burn, 2 when it lands, then 0 a second later.
/// </summary>
public sealed class HexaghostDominionPower : DreadPowerModel
{
    private const int BurnInterval = 2;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private const float ResetDelay = 1f;

    // Display only: the position in the cycle (1, then 2 when the Burn lands), which falls back to 0 a second after the Burn.
    private int _shownAmount;
    private int _shownVersion;

    public override int DisplayAmount => _shownAmount;

    protected override string IconEntry => "PYRE_POWER";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<Burn>();

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != Owner.Player || player.PlayerCombatState is not { } state) return;

        if (state.TurnNumber % BurnInterval != 0)
        {
            SetShownAmount(state.TurnNumber % BurnInterval);
            return;
        }

        SetShownAmount(BurnInterval);
        Flash();
        await CardPileCmd.AddToCombatAndPreview<Burn>(Owner, PileType.Hand, Amount, player);

        _ = ResetShownAmountAfterDelay();
    }

    private void SetShownAmount(int amount)
    {
        _shownAmount = amount;
        _shownVersion++;
        InvokeDisplayAmountChanged();
    }

    // Not awaited on purpose: the counter resets visually after a second without holding up the action queue.
    // A newer value (the next turn) cancels a pending reset.
    private async Task ResetShownAmountAfterDelay()
    {
        var version = _shownVersion;
        await Cmd.Wait(ResetDelay);
        if (_shownVersion == version) SetShownAmount(0);
    }
}
