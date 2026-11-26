using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Dread;

/// <summary>
///     Champ's Dominion, on each enemy. Like the Ceremonial Beast's <c>PlowPower</c>, <see cref="PowerModel.Amount" /> is the HP the
///     owner has to drop to (half of its max HP, set when the power is applied) and is shown as the counter. The first time it does,
///     the owner loses all its debuffs and gains Strength, then this power is gone.
/// </summary>
public sealed class ChampDominionPower : DreadPowerModel
{
    public const int StrengthAmount = 3;
    private const string StrengthKey = "StrengthAmount";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // The threshold comes from the owner's actual max HP, which is already scaled for the player count.
    public override bool ShouldScaleInMultiplayer => false;

    protected override string IconEntry => "ENRAGE_POWER";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new(StrengthKey, StrengthAmount)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromPowerWithPowerHoverTips<StrengthPower>(StrengthAmount);

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || Owner.IsDead || result.UnblockedDamage <= 0 || Owner.CurrentHp > Amount) return;

        Flash();
        foreach (var debuff in Owner.Powers.Where(p => p.Type == PowerType.Debuff).ToList())
            await PowerCmd.Remove(debuff);
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, DynamicVars[StrengthKey].BaseValue, Owner, null);
        await PowerCmd.Remove(this);
    }
}
