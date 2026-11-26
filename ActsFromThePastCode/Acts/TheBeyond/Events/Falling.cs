using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class Falling : AftpEventModel
{

    private CardModel? _attackCard;
    private CardModel? _powerCard;
    private CardModel? _skillCard;

    public override ActModel[] Acts => [ModelDb.Act<TheBeyondAct>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("SkillCard"),
        new StringVar("PowerCard"),
        new StringVar("AttackCard")
    ];

    public override void OnRoomEnter()
    {
        AFTPModAudio.Play("events", "falling");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        SetCards();
        return [Option(Continue)];
    }

    private void SetCards()
    {
        if (Owner == null) return;
        var deck = Owner.Deck.Cards;
        var skills = deck.Where(c => c is { Type: CardType.Skill, IsRemovable: true }).ToList();
        var powers = deck.Where(c => c is { Type: CardType.Power, IsRemovable: true }).ToList();
        var attacks = deck.Where(c => c is { Type: CardType.Attack, IsRemovable: true }).ToList();

        if (skills.Count > 0)
        {
            _skillCard = Rng.NextRequired(skills);
            ((StringVar)DynamicVars["SkillCard"]).StringValue = _skillCard.Title;
        }

        if (powers.Count > 0)
        {
            _powerCard = Rng.NextRequired(powers);
            ((StringVar)DynamicVars["PowerCard"]).StringValue = _powerCard.Title;
        }

        if (attacks.Count > 0)
        {
            _attackCard = Rng.NextRequired(attacks);
            ((StringVar)DynamicVars["AttackCard"]).StringValue = _attackCard.Title;
        }
    }

    private Task Continue()
    {
        var options = new List<EventOption>();

        if (_skillCard != null)
            options.Add(Option(Skill, "CHOICE", HoverTipFactory.FromCard(_skillCard)));
        else
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.CHOICE.options.SKILL_LOCKED"));

        if (_powerCard != null)
            options.Add(Option(Power, "CHOICE", HoverTipFactory.FromCard(_powerCard)));
        else
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.CHOICE.options.POWER_LOCKED"));

        if (_attackCard != null)
            options.Add(Option(Attack, "CHOICE", HoverTipFactory.FromCard(_attackCard)));
        else
            options.Add(new EventOption(this, null,
                $"{Id.Entry}.pages.CHOICE.options.ATTACK_LOCKED"));

        SetEventState(PageDescription("CHOICE"), options);
        return Task.CompletedTask;
    }

    private async Task Skill()
    {
        if (_skillCard != null) await CardPileCmd.RemoveFromDeck(_skillCard);
        SetEventFinished(PageDescription("SKILL"));
    }

    private async Task Power()
    {
        if (_powerCard != null) await CardPileCmd.RemoveFromDeck(_powerCard);
        SetEventFinished(PageDescription("POWER"));
    }

    private async Task Attack()
    {
        if (_attackCard != null) await CardPileCmd.RemoveFromDeck(_attackCard);
        SetEventFinished(PageDescription("ATTACK"));
    }
}