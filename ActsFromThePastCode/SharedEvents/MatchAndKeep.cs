using ActsFromThePast.Interfaces;
using ActsFromThePast.Minigames;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.SharedEvents;

public sealed class MatchAndKeep : AftpEventModel, IShrineEvent
{

    private const int Attempts = 5;

    public override ActModel[] Acts => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Attempts", Attempts)
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [Option(Continue)];
    }

    private Task Continue()
    {
        SetEventState(PageDescription("RULES"), [
            Option(Play, "RULES")
        ]);
        return Task.CompletedTask;
    }

    private async Task Play()
    {
        if (Owner == null) return;
        var minigame = new MatchAndKeepMinigame(Owner, Rng, Attempts, Owner.RunState.CurrentActIndex);
        await minigame.PlayMinigame();
        SetEventFinished(PageDescription("COMPLETE"));
    }
}