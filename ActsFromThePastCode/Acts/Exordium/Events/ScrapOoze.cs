using ActsFromThePast.Compatibility;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class ScrapOoze : AftpEventModel
{

    private const int BaseDamage = 5;
    private const int BaseRelicChance = 25;
    private const int ChanceIncreasePerAttempt = 10;
    private const int DamageIncreasePerAttempt = 1;
    private const int BloatGoldMin = 90;
    private const int BloatGoldMax = 130;

    public override ActModel[] Acts => [ModelDb.Act<ExordiumAct>()];

    private int Attempts
    {
        get;
        set
        {
            AssertMutable();
            field = value;
        }
    }

    private int CurrentDamage => BaseDamage + Attempts * DamageIncreasePerAttempt;
    private int CurrentRelicChance => BaseRelicChance + Attempts * ChanceIncreasePerAttempt;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Damage", CurrentDamage),
        new IntVar("RelicChance", CurrentRelicChance),
        new GoldVar(BloatGoldMax)
    ];

    public override void CalculateVars()
    {
        DynamicVars.Gold.BaseValue = BloatGoldMax - Rng.NextInt(BloatGoldMax - BloatGoldMin + 1);
    }

    public override void OnRoomEnter()
    {
        AFTPModAudio.Play("events", "scrap_ooze");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
            return
            [
                Option(Reach).ThatDoesDamage(CurrentDamage),
                Owner != null && Owner.Gold >= DynamicVars.Gold.BaseValue
                    ? Option(Bloat, "INITIAL_REBALANCED")
                    : new EventOption(this, null, $"{Id.Entry}.pages.INITIAL_REBALANCED.options.BLOAT_LOCKED")
            ];

        return
        [
            Option(Reach).ThatDoesDamage(CurrentDamage),
            Option(Leave)
        ];
    }

    public override bool IsAllowed(IRunState runState)
    {
        if (ActsFromThePastConfig.RebalancedMode)
            return runState.Players.All(p => p.Gold >= DynamicVars.Gold.BaseValue);

        return true;
    }

    private async Task Reach()
    {
        if (Owner == null) return;
        // TODO poison sfx when it exists
        await GameApiCallCompat.Damage(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            CurrentDamage,
            DamageProps.nonCardHpLoss,
            null);

        var roll = Rng.NextInt(100);
        var threshold = 100 - CurrentRelicChance;

        if (roll >= threshold)
        {
            var relic = GameApiCallCompat.PullNextRelicFromFront(Owner).ToMutable();
            await RelicCmd.Obtain(relic, Owner);
            SetEventFinished(PageDescription("SUCCESS"));
        }
        else
        {
            Attempts++;
            DynamicVars["Damage"].BaseValue = CurrentDamage;
            DynamicVars["RelicChance"].BaseValue = CurrentRelicChance;
            SetEventState(PageDescription("FAIL"), [
                Option(Reach, "FAIL").ThatDoesDamage(CurrentDamage),
                Option(Leave, "FAIL")
            ]);
        }
    }

    private async Task Bloat()
    {
        if (Owner == null) return;
        await PlayerCmd.LoseGold(DynamicVars.Gold.BaseValue, Owner, GoldLossType.Spent);
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/twig_slime_m/twig_slime_m_die");
        var relic = GameApiCallCompat.PullNextRelicFromFront(Owner).ToMutable();
        await RelicCmd.Obtain(relic, Owner);
        SetEventFinished(PageDescription("BLOAT"));
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
    }
}