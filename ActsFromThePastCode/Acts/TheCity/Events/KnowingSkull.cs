using ActsFromThePast.Compatibility;
using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class KnowingSkull : AftpEventModel, IShrineEvent
{

    private const int BaseCost = 6;
    private const int GoldReward = 90;
    private readonly int _leaveCost = BaseCost;
    private int _cardCost = BaseCost;
    private int _goldCost = BaseCost;
    private int _potionCost = BaseCost;

    public override ActModel[] Acts => [ModelDb.Act<TheCityAct>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("PotionCost", BaseCost),
        new IntVar("CardCost", BaseCost),
        new IntVar("GoldCost", BaseCost),
        new IntVar("LeaveCost", BaseCost),
        new IntVar("GoldReward", GoldReward)
    ];

    bool IShrineEvent.IsOneTimeEvent => true;

    private void UpdateDynamicVars()
    {
        DynamicVars["PotionCost"].BaseValue = _potionCost;
        DynamicVars["CardCost"].BaseValue = _cardCost;
        DynamicVars["GoldCost"].BaseValue = _goldCost;
        DynamicVars["LeaveCost"].BaseValue = _leaveCost;
    }

    public override void OnRoomEnter()
    {
        AFTPModAudio.Play("events", "knowing_skull");
    }

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.All((Func<Player, bool>)(p => p.Creature.CurrentHp >= 13));
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [Option(Continue)];
    }

    private Task Continue()
    {
        SetAskState(PageDescription("ASK"));
        return Task.CompletedTask;
    }

    private void SetAskState(LocString description)
    {
        UpdateDynamicVars();
        SetEventState(description, [
            Option(Potion, "ASK").ThatDoesDamage(_potionCost),
            Option(Gold, "ASK").ThatDoesDamage(_goldCost),
            Option(Card, "ASK").ThatDoesDamage(_cardCost),
            Option(Leave, "ASK").ThatDoesDamage(_leaveCost)
        ]);
    }

    private async Task Potion()
    {
        if (Owner == null) return;
        await GameApiCallCompat.Damage(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            _potionCost,
            DamageProps.nonCardHpLoss,
            null);
        _potionCost++;

        var potion = PotionFactory.CreateRandomPotionOutOfCombat(Owner, Owner.RunState.Rng.Niche).ToMutable();
        await PotionCmd.TryToProcure(potion, Owner);
        SetAskState(PageDescription("POTION"));
    }

    private async Task Gold()
    {
        if (Owner == null) return;
        await GameApiCallCompat.Damage(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            _goldCost,
            DamageProps.nonCardHpLoss,
            null);
        _goldCost++;
        await PlayerCmd.GainGold(GoldReward, Owner);
        SetAskState(PageDescription("GOLD"));
    }

    private async Task Card()
    {
        if (Owner == null) return;
        await GameApiCallCompat.Damage(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            _cardCost,
            DamageProps.nonCardHpLoss,
            null);
        _cardCost++;

        var colorlessCards = ModelDb.CardPool<ColorlessCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Rarity == CardRarity.Uncommon)
            .ToList();

        var chosenCard = Owner.RunState.Rng.Niche.NextRequired(colorlessCards);
        var card = Owner.RunState.CreateCard(chosenCard, Owner);
        var result = await CardPileCmd.Add(card, PileType.Deck);
        CardCmd.PreviewCardPileAdd(result, 2f);
        SetAskState(PageDescription("CARD"));
    }

    private async Task Leave()
    {
        if (Owner == null) return;
        await GameApiCallCompat.Damage(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            _leaveCost,
            DamageProps.nonCardHpLoss,
            null);
        SetEventFinished(PageDescription("LEAVE"));
    }
}