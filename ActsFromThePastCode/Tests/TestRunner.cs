using System.Reflection;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

namespace ActsFromThePast.Tests;

public sealed class TestRunner
{
    private readonly List<(string name, Exception ex)> _failures = [];
    private readonly List<string> _passed = [];

    public async Task<TestRunResult> RunAllAsync(string seed, string? filter, CancellationToken ct)
    {
        var started = DateTime.UtcNow;
        var wasTestMode = TestMode.IsOn;
        TestMode.IsOn = true;
        try
        {
            var methods = typeof(TestRunner).Assembly.GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttribute<AftpTestAttribute>() != null || m.GetCustomAttribute<AftpPoolTestAttribute>() != null)
                .Where(m => string.IsNullOrEmpty(filter) ||
                            $"{m.DeclaringType?.Name}.{m.Name}".Contains(filter, StringComparison.OrdinalIgnoreCase))
                // Quick single-combat tests first, the per-encounter sweeps last.
                .OrderBy(m => m.GetCustomAttribute<AftpTestAttribute>() != null ? 0 : 1)
                .ThenBy(m => m.DeclaringType?.Name)
                .ThenBy(m => m.Name)
                .ToList();

            AutoSlayLog.Action($"[AftpTests] Found {methods.Count} tests" + (string.IsNullOrEmpty(filter) ? "." : $" matching '{filter}'."));

            foreach (var method in methods)
            {
                ct.ThrowIfCancellationRequested();
                var name = $"{method.DeclaringType?.Name}.{method.Name}";
                var instance = Activator.CreateInstance(method.DeclaringType!);
                try
                {
                    if (method.GetCustomAttribute<AftpTestAttribute>() is { } single)
                    {
                        await RunCase(name, single.EncounterType, ctx => (Task)method.Invoke(instance, [ctx])!, seed);
                    }
                    else
                    {
                        foreach (var testCase in (IEnumerable<AftpTestCase>)method.Invoke(instance, null)!)
                            await RunCase($"{name}:{testCase.Name}", testCase.EncounterType, testCase.Run, seed);
                    }
                }
                catch (Exception ex)
                {
                    Fail(name, ex);
                }
            }

            if (string.IsNullOrEmpty(filter) || "EventSweep".Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                filter.StartsWith("Event", StringComparison.OrdinalIgnoreCase))
                await EventSweep.RunAsync(seed, _passed.Add, Fail);
        }
        finally
        {
            TestMode.IsOn = wasTestMode;
            Report();
        }

        return new TestRunResult(seed, DateTime.UtcNow - started, _passed.ToList(),
            _failures.Select(f => new TestFailure(f.name, f.ex.Message, f.ex.ToString())).ToList(),
            ReleaseInfoManager.Instance.ReleaseInfo?.Version ?? "unknown");
    }

    private async Task RunCase(string name, Type? encounterType, Func<TestContext, Task> run, string seed)
    {
        try
        {
            var ctx = await NewCombatAsync(seed, encounterType);
            AutoSlayLog.Info($"Running: {name}");
            await run(ctx);
            _passed.Add(name);
        }
        catch (Exception ex)
        {
            Fail(name, ex);
        }
        finally
        {
            EndCombat();
        }
    }

    private void Fail(string name, Exception ex)
    {
        var actual = ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;
        _failures.Add((name, actual));
        AutoSlayLog.Error($"[FAILED] {name}: {actual.Message}");
    }

    private void Report()
    {
        if (_failures.Count == 0)
        {
            AutoSlayLog.Action("[AftpTests] All tests passed!");
            return;
        }

        AutoSlayLog.Warn($"[AftpTests] {_failures.Count} test(s) failed:");
        foreach (var (name, ex) in _failures) AutoSlayLog.Warn($"  - {name}: {ex.Message}");
    }

    private static async Task<TestContext> NewCombatAsync(string seed, Type? encounterType)
    {
        if (CombatManager.Instance.DebugOnlyGetState() != null)
            CombatManager.Instance.Reset(true);

        var character = ModelDb.GetById<CharacterModel>(ModelDb.GetId(typeof(Ironclad)));
        var players = new List<Player> { Player.CreateForNewRun(character, UnlockState.all, 1UL) };

        var run = TestCompat.CreateRunStateForTest(players, seed);
        // A bare test RunState has no map progression; code that logs to the run history NREs without one entry.
        run.AppendToMapPointHistory(MapPointType.Monster, RoomType.Monster, null);

        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService(), shouldSave: false);
        LocalContext.NetId = RunManager.Instance.NetService.NetId;
        var player = run.Players.First();

        var encounter = encounterType != null
            ? ((EncounterModel)ModelDb.GetById<AbstractModel>(ModelDb.GetId(encounterType))).ToMutable()
            : ModelDb.AllEncounters.First().ToMutable();
        encounter.DebugRandomizeRngCompat();

        var combat = TestCompat.CreateCombatState(encounter, run);
        combat.AddPlayer(player);

        if (!encounter.HaveMonstersBeenGenerated)
            encounter.GenerateMonstersWithSlots(run);
        foreach (var (monster, slot) in encounter.MonstersWithSlots)
        {
            monster.AssertMutable();
            combat.AddCreature(combat.CreateCreature(monster, CombatSide.Enemy, slot));
        }

        combat.SortEnemiesBySlotName();

        CombatManager.Instance.SetUpCombat(combat);
        CombatManager.Instance.AfterCombatRoomLoaded();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (!CombatManager.Instance.IsInProgress && sw.Elapsed < TimeSpan.FromSeconds(10))
            await Task.Yield();
        if (!CombatManager.Instance.IsInProgress)
            throw new InvalidOperationException("Combat never reached IsInProgress after AfterCombatRoomLoaded.");
        while (player.PlayerCombatState?.Phase != PlayerTurnPhase.Play && sw.Elapsed < TimeSpan.FromSeconds(10))
            await Task.Yield();

        return new TestContext(combat, player);
    }

    private static void EndCombat()
    {
        try { CombatManager.Instance.Reset(true); } catch { /* best effort */ }
        try
        {
            RunManager.Instance.State = null;
            LocalContext.NetId = null;
        }
        catch { /* best effort */ }
    }
}
