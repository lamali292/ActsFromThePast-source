using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Dread;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ActsFromThePast.Dread;

/// <summary>Anger: the first time an enemy drops to half HP it loses all its debuffs and gains 3 Strength, like the Champ's phase change. Done by <see cref="ChampDominionPower" />.</summary>
public sealed class ChampDominion : DominionModel, ICustomModel
{
    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromPowerWithPowerHoverTips<StrengthPower>(ChampDominionPower.StrengthAmount);

    protected override async Task AfterEnemiesEnteredCombat(IEnumerable<Creature> creatures)
    {
        // The threshold is per enemy: half of its own max HP, shown as the counter of the power.
        foreach (var creature in creatures)
            await PowerCmd.Apply<ChampDominionPower>(new ThrowingPlayerChoiceContext(), creature, creature.MaxHp / 2, null, null);
    }
}
