using ActsFromThePast.Interfaces;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.SharedEvents;

public sealed class BonfireSpirits : AftpEventModel, IShrineEvent
{

    private const int CommonHeal = 5;
    private const int RareMaxHpGain = 10;

    public override ActModel[] Acts => [];

    bool IShrineEvent.IsOneTimeEvent => true;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [Option(Continue)];
    }

    public override void OnRoomEnter()
    {
        AFTPModAudio.Play("events", "goop");
    }

    private Task Continue()
    {
        SetEventState(PageDescription("APPROACH"), [
            Option(Offer, "APPROACH")
        ]);
        return Task.CompletedTask;
    }

    private async Task Offer()
    {
        if (Owner == null) return;
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
        var card = (await CardSelectCmd.FromDeckForRemoval(Owner, prefs)).FirstOrDefault();

        if (card == null)
        {
            SetEventFinished(PageDescription("NOTHING"));
            return;
        }

        var rarity = card.Rarity;
        var isCurse = card.Type == CardType.Curse;

        await CardPileCmd.RemoveFromDeck(new List<CardModel> { card });

        if (isCurse)
        {
            await RelicCmd.Obtain(ModelDb.Relic<SpiritPoop>().ToMutable(), Owner);
            SetEventFinished(PageDescription("OFFER_CURSE"));
        }
        else switch (rarity)
        {
            case CardRarity.Basic:
                SetEventFinished(PageDescription("OFFER_BASIC"));
                break;
            case CardRarity.Common:
                await CreatureCmd.Heal(Owner.Creature, CommonHeal);
                SetEventFinished(PageDescription("OFFER_COMMON"));
                break;
            case CardRarity.Uncommon:
            case CardRarity.Quest:
                await CreatureCmd.Heal(Owner.Creature, Owner.Creature.MaxHp);
                SetEventFinished(PageDescription("OFFER_UNCOMMON"));
                break;
            case CardRarity.Rare:
            case CardRarity.Ancient:
                await CreatureCmd.GainMaxHp(Owner.Creature, RareMaxHpGain);
                await CreatureCmd.Heal(Owner.Creature, Owner.Creature.MaxHp);
                SetEventFinished(PageDescription("OFFER_RARE"));
                break;
            case CardRarity.None:
            case CardRarity.Event:
            case CardRarity.Token:
            case CardRarity.Status:
            case CardRarity.Curse:
            default:
                await CreatureCmd.Heal(Owner.Creature, CommonHeal);
                SetEventFinished(PageDescription("OFFER_COMMON"));
                break;
        }
    }
}