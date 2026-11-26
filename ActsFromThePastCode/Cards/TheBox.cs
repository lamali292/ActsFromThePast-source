using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ActsFromThePast.Cards;

[Pool(typeof(QuestCardPool))]
public sealed class TheBox() : AftpCardModel(-1, CardType.Quest, CardRarity.Quest, TargetType.None)
{
    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
}

public static class TheBoxTracker
{
    public static Player? FreeNextPurchasePlayer { get; set; }
    public static bool SkipNextCompletion { get; set; }
    public static bool PlayerHasBox { get; set; }
    public static bool ShowRemovalDialogue { get; set; }
    public static bool CardRemovalUsed { get; set; }
}