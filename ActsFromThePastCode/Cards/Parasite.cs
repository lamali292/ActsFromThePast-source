using ActsFromThePast.Compatibility;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ActsFromThePast.Cards;

[Pool(typeof(CurseCardPool))]
public sealed class Parasite() : AftpCardModel(-1,
    CardType.Curse,
    CardRarity.Curse,
    TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Unplayable
    ];

    public override int MaxUpgradeLevel => 0;

    public override async Task BeforeCardRemoved(CardModel card)
    {
        if (card != this)
            return;

        await GameApiCallCompat.LoseMaxHp(new BlockingPlayerChoiceContext(), Owner.Creature, 3);
        AFTPModAudio.Play("general", "blood_swish");
    }
}