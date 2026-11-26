using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Tests;

/// <summary>Test-only shims for harness APIs whose signatures differ between 0.111.1 and 0.113.x.</summary>
internal static class TestCompat
{
    private static readonly MethodInfo? DebugRandomizeRngMethod = AccessTools.Method(typeof(EncounterModel), "DebugRandomizeRng");

    private static readonly MethodInfo CreateForTestMethod = AccessTools.Method(typeof(RunState), "CreateForTest");

    private static readonly ConstructorInfo CombatStateCtor =
        typeof(CombatState).GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();

    /// <summary><c>EncounterModel.DebugRandomizeRng()</c> returns void on 0.111.1 and the seed on 0.113.x; the result is unused.</summary>
    public static void DebugRandomizeRngCompat(this EncounterModel encounter) => DebugRandomizeRngMethod?.Invoke(encounter, null);

    /// <summary><c>RunState.CreateForTest</c> gained optional parameters; calling it by parameter name keeps both builds working.</summary>
    public static RunState CreateRunStateForTest(IReadOnlyList<Player> players, string seed)
    {
        var args = CreateForTestMethod.GetParameters().Select(p => p.Name switch
        {
            "players" => players,
            "seed" => seed,
            _ => p.DefaultValue
        }).ToArray();
        return (RunState)CreateForTestMethod.Invoke(null, args)!;
    }

    /// <summary>
    ///     The trailing <c>CombatState</c> constructor parameters differ between builds, so each one is filled from the
    ///     same-named (PascalCase) property of the run state when it exists.
    /// </summary>
    public static CombatState CreateCombatState(EncounterModel encounter, RunState run)
    {
        var args = CombatStateCtor.GetParameters().Select(p =>
        {
            if (p.Name == "encounter") return encounter;
            if (p.Name == "runState") return run;
            var prop = p.Name == null ? null : AccessTools.Property(typeof(RunState), char.ToUpperInvariant(p.Name[0]) + p.Name[1..]);
            return prop?.GetValue(run);
        }).ToArray();
        return (CombatState)CombatStateCtor.Invoke(args);
    }

    /// <summary><c>EventModel.BeginEvent(player, combatSynchronizer, isPreFinished)</c> with a fresh synchronizer, as the game creates one.</summary>
    public static Task BeginEvent(EventModel ev, Player player, RunState run) =>
        ev.BeginEvent(player, new EventCombatSynchronizer(run, run), false);

    /// <summary>
    ///     0.113.x keeps per-player selectors in <c>RunManager.CardSelectorStack</c>; 0.111.1 has a global
    ///     <c>CardSelectCmd.PushSelector(selector)</c>. Both are resolved by name so neither build fails to load.
    /// </summary>
    public static void PushCardSelector(Player player, object selector)
    {
        var stackProperty = AccessTools.Property(typeof(RunManager), "CardSelectorStack");
        if (stackProperty != null)
        {
            var stack = stackProperty.GetValue(RunManager.Instance)!;
            AccessTools.Method(stack.GetType(), "PushSelector").Invoke(stack, [player, selector]);
            return;
        }

        AccessTools.Method(typeof(MegaCrit.Sts2.Core.Commands.CardSelectCmd), "PushSelector").Invoke(null, [selector, false]);
    }

    /// <summary>An auto-slay card selector with a fixed seed.</summary>
    public static object NewAutoCardSelector() =>
        new MegaCrit.Sts2.Core.AutoSlay.Helpers.AutoSlayCardSelector(new MegaCrit.Sts2.Core.Random.Rng(1));
}
