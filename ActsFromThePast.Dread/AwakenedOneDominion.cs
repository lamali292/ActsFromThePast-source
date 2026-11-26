using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Dread;

namespace ActsFromThePast.Dread;

/// <summary>Curiosity: all enemies gain 1 Curiosity (+1 Strength whenever you play a Power), the Awakened One's own power.</summary>
public sealed class AwakenedOneDominion : DominionModel, ICustomModel
{
    private const int CuriosityAmount = 1;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromPowerWithPowerHoverTips<CuriosityPower>(CuriosityAmount);

    protected override Task AfterEnemiesEnteredCombat(IEnumerable<Creature> creatures) =>
        PowerCmd.Apply<CuriosityPower>(new ThrowingPlayerChoiceContext(), creatures.ToArray(), CuriosityAmount, null, null);
}
