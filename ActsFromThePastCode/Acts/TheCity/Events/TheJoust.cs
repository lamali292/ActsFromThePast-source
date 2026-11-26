using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class TheJoust : AftpEventModel, IShrineEvent
{

    private const int BetAmount = 50;
    private const int WinMurderer = 100;
    private const int WinOwner = 250;
    private const float OwnerWinChance = 0.3f;
    private bool _betForOwner;
    private bool _ownerWins;

    public override ActModel[] Acts => [ModelDb.Act<TheCityAct>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("BetAmount", BetAmount),
        new IntVar("WinMurderer", WinMurderer),
        new IntVar("WinOwner", WinOwner)
    ];

    bool IShrineEvent.IsOneTimeEvent => true;

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.All(p => p.Gold >= BetAmount);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [Option(Continue)];
    }

    private EventOption BetOption(Func<Task> bet, string name)
    {
        return Owner is { Gold: >= BetAmount }
            ? Option(bet, "EXPLANATION")
            : new EventOption(this, null, $"{Id.Entry}.pages.EXPLANATION.options.{name}_LOCKED");
    }

    private Task Continue()
    {
        List<EventOption> options =
        [
            BetOption(BetMurderer, "BET_MURDERER"),
            BetOption(BetOwner, "BET_OWNER")
        ];
        // Without the gold both bets are locked, so the player needs a way out
        if (Owner == null || Owner.Gold < BetAmount)
            options.Add(Option(Leave, "EXPLANATION"));
        SetEventState(PageDescription("EXPLANATION"), options);
        return Task.CompletedTask;
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }

    private async Task BetMurderer()
    {
        if (Owner == null) return;
        _betForOwner = false;
        await PlayerCmd.LoseGold(BetAmount, Owner);

        SetEventState(PageDescription("BET_MURDERER"), [
            new EventOption(this, WatchJoust,
                $"{Id.Entry}.pages.BET_MURDERER.options.WATCH")
        ]);
    }

    private async Task BetOwner()
    {
        if (Owner == null) return;
        _betForOwner = true;
        await PlayerCmd.LoseGold(BetAmount, Owner);

        SetEventState(PageDescription("BET_OWNER"), [
            new EventOption(this, WatchJoust,
                $"{Id.Entry}.pages.BET_OWNER.options.WATCH")
        ]);
    }

    private async Task WatchJoust()
    {
        _ownerWins = Rng.NextFloat() < OwnerWinChance;
        NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/cultists/cultists_attack");
        await Cmd.Wait(1.0f);
        NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/assassin_ruby_raider/assassin_ruby_raider_attack");
        await Cmd.Wait(0.25f);
        NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/cultists/cultists_attack");

        SetEventState(PageDescription("COMBAT"), [
            new EventOption(this, ResolveJoust,
                $"{Id.Entry}.pages.COMBAT.options.CONTINUE")
        ]);
    }

    private async Task ResolveJoust()
    {
        if (Owner == null) return;
        if (_ownerWins)
        {
            if (_betForOwner)
            {
                await PlayerCmd.GainGold(WinOwner, Owner);
                SetEventFinished(PageDescription("OWNER_WINS_BET_WON"));
            }
            else
            {
                SetEventFinished(PageDescription("OWNER_WINS_BET_LOST"));
            }
        }
        else
        {
            if (_betForOwner)
            {
                SetEventFinished(PageDescription("MURDERER_WINS_BET_LOST"));
            }
            else
            {
                await PlayerCmd.GainGold(WinMurderer, Owner);
                SetEventFinished(PageDescription("MURDERER_WINS_BET_WON"));
            }
        }
    }
}