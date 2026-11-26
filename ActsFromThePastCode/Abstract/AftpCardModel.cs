using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace ActsFromThePast;

public abstract class AftpCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target)
    : CustomCardModel(baseCost, type, rarity, target)
{
    public override string? CustomPortraitPath => Id.Entry.CardPortraitPath();
}
