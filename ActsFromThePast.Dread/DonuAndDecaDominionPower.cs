using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Dread;

/// <summary>
///     Donu &amp; Deca's Dominion, on each enemy: at the start of every player turn it gains Block on odd turns (Deca's Square of
///     Protection) and Strength on even turns (Donu's Circle of Power).
/// </summary>
public sealed class DonuAndDecaDominionPower : DreadPowerModel
{
    private const int BlockAmount = 8;
    private const int StrengthAmount = 2;
    private const string BlockKey = "BlockAmount";
    private const string StrengthKey = "StrengthAmount";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldScaleInMultiplayer => false;

    protected override string IconEntry => "GUARDED_POWER";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new(BlockKey, BlockAmount),
        new(StrengthKey, StrengthAmount)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
    HoverTipFactory.FromPower<StrengthPower>(StrengthAmount),
    HoverTipFactory.Static(StaticHoverTip.Block)
];

public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != CombatSide.Player || Owner.IsDead) return;

        Flash();
        if (combatState.RoundNumber % 2 == 1)
            await CreatureCmd.GainBlock(Owner, DynamicVars[BlockKey].BaseValue, ValueProp.Move, null);
        else
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner,
                DynamicVars[StrengthKey].BaseValue, Owner, null);
    }
}
