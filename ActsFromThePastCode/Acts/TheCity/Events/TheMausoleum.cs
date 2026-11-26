using ActsFromThePast.Compatibility;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class TheMausoleum : AftpEventModel
{
    private const int MaxHpGain = 10;
    private const string SacrificeRelicKey = "SacrificeRelic";
    private RelicModel? _sacrificeRelic;

    public override ActModel[] Acts => [ModelDb.Act<TheCityAct>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar(SacrificeRelicKey),
        new MaxHpVar(MaxHpGain)
    ];

    public override bool IsAllowed(IRunState runState)
    {
        return !ActsFromThePastConfig.RebalancedMode || runState.Players.All(p => p.Relics.Any(r => r.IsTradable));
    }

    public override void CalculateVars()
    {
        if (Owner == null) return;
        if (!ActsFromThePastConfig.RebalancedMode) return;
        _sacrificeRelic = Rng.NextItem(Owner.Relics.Where(r => r.IsTradable));
        if (_sacrificeRelic != null)
            ((StringVar)DynamicVars[SacrificeRelicKey]).StringValue = _sacrificeRelic.Title.GetFormattedText();
    }

    public override void OnRoomEnter()
    {
        AFTPModAudio.Play("events", "ghosts");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
            return
            [
                Option(Open, "INITIAL", HoverTipFactory.FromCardWithCardHoverTips<Writhe>().ToArray()),
                new EventOption(this, PayRespects,
                    L10NLookup($"{Id.Entry}.pages.INITIAL_REBALANCED.options.PAY_RESPECTS.title"),
                    L10NLookup($"{Id.Entry}.pages.INITIAL_REBALANCED.options.PAY_RESPECTS.description"),
                    $"{Id.Entry}.pages.INITIAL_REBALANCED.options.PAY_RESPECTS",
                    _sacrificeRelic?.HoverTips ?? []).ThatHasDynamicTitle()
            ];

        return
        [
            Option(Open, "INITIAL", HoverTipFactory.FromCard(ModelDb.Card<Writhe>())),
            Option(Leave)
        ];
    }

    private async Task Open()
    {
        if (Owner == null) return;
        NDebugAudioManager.Instance?.Play("blunt_attack.mp3");
        NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Long);
        var relic = GameApiCallCompat.PullNextRelicFromFront(Owner).ToMutable();
        await RelicCmd.Obtain(relic, Owner);
        var writhe = Owner.RunState.CreateCard(ModelDb.Card<Writhe>(), Owner);
        var curseResult = await CardPileCmd.Add(writhe, PileType.Deck);
        CardCmd.PreviewCardPileAdd(curseResult, 2f);
        SetEventFinished(PageDescription("OPEN_CURSED"));
    }

    private async Task PayRespects()
    {
        if (Owner == null) return;
        if (_sacrificeRelic != null)
            await RelicCmd.Remove(_sacrificeRelic);
        await CreatureCmd.GainMaxHp(Owner.Creature, MaxHpGain);
        SetEventFinished(PageDescription("PAY_RESPECTS"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}