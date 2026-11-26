using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Dread;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ActsFromThePast.Dread;

/// <summary>Sharp Hide: all enemies gain 1 Thorns (2 for elites).</summary>
public sealed class GuardianDominion : DominionModel, ICustomModel
{
    private const int ThornsAmount = 1;
    private const int EliteThornsAmount = 2;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromPowerWithPowerHoverTips<ThornsPower>(EliteThornsAmount);

    protected override Task AfterEnemiesEnteredCombat(IEnumerable<Creature> creatures)
    {
        var enemies = creatures.ToArray();
        if (enemies.Length == 0) return Task.CompletedTask;

        return PowerCmd.Apply<ThornsPower>(new ThrowingPlayerChoiceContext(), enemies,
            DominionHelpers.IsEliteCombat(enemies[0]) ? EliteThornsAmount : ThornsAmount, null, null);
    }
}
