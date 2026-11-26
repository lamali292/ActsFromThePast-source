using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Dread;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ActsFromThePast.Dread;

/// <summary>Plated Servos: all enemies gain 1 Artifact (2 for elites). The Automaton opens its fight with Artifact too.</summary>
public sealed class BronzeAutomatonDominion : DominionModel, ICustomModel
{
    private const int ArtifactAmount = 1;
    private const int EliteArtifactAmount = 2;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromPowerWithPowerHoverTips<ArtifactPower>(EliteArtifactAmount);

    protected override Task AfterEnemiesEnteredCombat(IEnumerable<Creature> creatures)
    {
        var enemies = creatures.ToArray();
        if (enemies.Length == 0) return Task.CompletedTask;

        return PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(), enemies,
            DominionHelpers.IsEliteCombat(enemies[0]) ? EliteArtifactAmount : ArtifactAmount, null, null);
    }
}
