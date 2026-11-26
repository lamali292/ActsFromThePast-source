using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Dread;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ActsFromThePast.Dread;

/// <summary>
///     Twin Guard: on odd turns all enemies gain Block (Deca's Square of Protection), on even turns all enemies gain Strength (Donu's
///     Circle of Power). Done by <see cref="DonuAndDecaDominionPower" /> on every enemy.
/// </summary>
public sealed class DonuAndDecaDominion : DominionModel, ICustomModel
{
    public override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block)
    ];

    protected override Task AfterEnemiesEnteredCombat(IEnumerable<Creature> creatures) =>
        PowerCmd.Apply<DonuAndDecaDominionPower>(new ThrowingPlayerChoiceContext(), creatures.ToArray(), 1, null, null);
}
