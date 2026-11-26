using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Dread;

namespace ActsFromThePast.Dread;

/// <summary>Goop: 2 Slimed are shuffled into your draw pile at the start of each combat.</summary>
public sealed class SlimeBossDominion : DominionModel, ICustomModel
{
    private const int SlimedCount = 2;

    public override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<Slimed>();

    protected override async Task AfterPlayersEnteredCombat(IReadOnlyList<Player> players)
    {
        AssertMutable();
        foreach (var player in players)
            await CardPileCmd.AddToCombatAndPreview<Slimed>(player.Creature, PileType.Draw, SlimedCount, player,
                CardPilePosition.Random);
    }
}
