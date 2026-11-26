using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Afflictions;

public sealed class EntangledOriginal : AfflictionModel
{
    private bool WasAlreadyUnplayable
    {
        get;
        set
        {
            AssertMutable();
            field = value;
        }
    }

    public override bool CanAfflictCardType(CardType cardType)
    {
        return cardType == CardType.Attack;
    }

    public override void AfterApplied()
    {
        WasAlreadyUnplayable = Card.Keywords.Contains(CardKeyword.Unplayable);
        if (WasAlreadyUnplayable)
            return;
        Card.AddKeyword(CardKeyword.Unplayable);
    }

    public override void BeforeRemoved()
    {
        if (WasAlreadyUnplayable)
            return;
        Card.RemoveKeyword(CardKeyword.Unplayable);
    }
}