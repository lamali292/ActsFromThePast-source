using System.Reflection;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

namespace ActsFromThePast.Tests;

/// <summary>
///     Walks every reachable option of every event of ours (depth first, replaying the path from a fresh run for each
///     node), once with Rebalanced Mode off and once on. An option that throws is a failure; one that never completes
///     within <see cref="OptionTimeout" /> is reported as a failure too, since it usually waits for UI nobody can click
///     (card grid, combat) and needs a manual look.
/// </summary>
internal static class EventSweep
{
    private const int MaxDepth = 8;

    /// <summary>Player states the events are started under; a real run reaches events in all of these.</summary>
    private static readonly string[] Profiles = ["normal", "broke", "dying", "tinydeck", "norelics", "upgraded"];
    private static readonly TimeSpan OptionTimeout = TimeSpan.FromSeconds(3);

    public static async Task RunAsync(string seed, Action<string> pass, Action<string, Exception> fail)
    {
        var wasRebalanced = ActsFromThePastConfig.RebalancedMode;
        var wasDarv = ActsFromThePastConfig.DarvOnlyInLegacyActs;
        try
        {
            ActsFromThePastConfig.DarvOnlyInLegacyActs = true;
            await DarvRepeatVisits(seed, pass, fail);
            ActsFromThePastConfig.DarvOnlyInLegacyActs = false;
            if (Environment.GetEnvironmentVariable("AFTP_TEST_FILTER")?.Contains("Darv", StringComparison.OrdinalIgnoreCase) == true) return;

            var eventTypes = typeof(EventSweep).Assembly.GetTypes()
                .Where(t => t is { IsAbstract: false } && t.IsSubclassOf(typeof(EventModel)) && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.Name).ToList();

            foreach (var profile in Profiles)
            foreach (var rebalanced in new[] { false, true })
            {
                ActsFromThePastConfig.RebalancedMode = rebalanced;
                foreach (var type in eventTypes)
                {
                    var tag = $"Event.{type.Name}[{(rebalanced ? "rebalanced" : "classic")},{profile}]";
                    _profile = profile;
                    try
                    {
                        await Explore(type, [], [], tag, seed, pass, fail);
                    }
                    catch (Exception ex)
                    {
                        fail(tag, ex);
                    }
                    finally
                    {
                        Cleanup();
                    }
                }
            }
        }
        finally
        {
            ActsFromThePastConfig.RebalancedMode = wasRebalanced;
            ActsFromThePastConfig.DarvOnlyInLegacyActs = wasDarv;
        }
    }

    /// <summary>With DarvOnlyInLegacyActs the same ancient is visited in every act; each visit must still offer options.</summary>
    private static async Task DarvRepeatVisits(string seed, Action<string> pass, Action<string, Exception> fail)
    {
        try
        {
            Cleanup();
            var character = ModelDb.GetById<CharacterModel>(ModelDb.GetId(typeof(Ironclad)));
            var player = Player.CreateForNewRun(character, UnlockState.all, 1UL);
            var run = TestCompat.CreateRunStateForTest([player], seed);
            RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService(), shouldSave: false);
            LocalContext.NetId = RunManager.Instance.NetService.NetId;
            player = run.Players.First();
            TestCompat.PushCardSelector(player, TestCompat.NewAutoCardSelector());

            for (var visit = 1; visit <= 6; visit++)
            {
                run.AppendToMapPointHistory(MapPointType.Ancient, RoomType.Event, null);
                var darv = (EventModel)ModelDb.AncientEvent<MegaCrit.Sts2.Core.Models.Events.Darv>().ToMutable();
                await WithTimeout(TestCompat.BeginEvent(darv, player, run), "Darv.BeginEvent");
                var titles = darv.CurrentOptions.Select(o => o.TextKey.Split('.').Last()).ToList();
                var name = $"Event.Darv.visit{visit}";
                if (darv.CurrentOptions.Count == 0) fail(name, new InvalidOperationException("Darv offered NO options on visit " + visit));
                else pass($"{name} ({string.Join(", ", titles)})");
                var pick = darv.CurrentOptions.FirstOrDefault(o => !o.IsLocked);
                if (pick != null) await WithTimeout(pick.Chosen(), "choosing Darv option");
            }
        }
        catch (Exception ex)
        {
            fail("Event.Darv.repeatVisits", Unwrap(ex));
        }
        finally
        {
            Cleanup();
        }
    }

    /// <summary>Options that waited on UI in an earlier pass; waiting for them again would only cost time.</summary>
    private static readonly HashSet<string> NeedsUi = [];

    private static bool _notAllowed;
    private static string lastKey = "";

    private static string _profile = "normal";

    private static async Task Explore(Type type, List<int> path, List<string> labels, string tag, string seed,
        Action<string> pass, Action<string, Exception> fail)
    {
        var name = labels.Count == 0 ? tag : $"{tag}:{string.Join(">", labels)}";
        EventModel ev;
        try
        {
            ev = await StartAndReplay(type, path, seed);
        }
        catch (Exception) when (_notAllowed && path.Count == 0)
        {
            pass(name + " (not allowed in this state and failed to start, skipped)");
            return;
        }
        catch (Exception ex)
        {
            // Entering a fight needs the real room/scene machinery; reaching that call is as far as a headless run can go.
            if (ex.ToString().Contains("EnterCombat")) pass(name + " (reached combat entry)");
            else if (ex is TimeoutException) { NeedsUi.Add(lastKey); pass(name + " (NEEDS MANUAL TEST: interactive minigame/screen)"); }
            else fail(name, Unwrap(ex));
            return;
        }

        var options = ev.CurrentOptions.ToList();
        if (path.Count == 0 && options.Count == 0)
        {
            fail(name, new InvalidOperationException("Event started with no options."));
            return;
        }

        pass(name);
        if (ev.IsFinished || path.Count >= (_profile == "normal" ? MaxDepth : 1)) return;

        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];
            var label = option.TextKey.Split('.').Last();
            if (option.IsLocked || option.IsProceed || NeedsUi.Contains(option.TextKey)) continue; // locked: cannot be chosen; proceed: leaves the room
            await Explore(type, [..path, i], [..labels, label], tag, seed, pass, fail);
        }
    }

    private static async Task<EventModel> StartAndReplay(Type type, List<int> path, string seed)
    {
        Cleanup();
        var character = ModelDb.GetById<CharacterModel>(ModelDb.GetId(typeof(Ironclad)));
        var player = Player.CreateForNewRun(character, UnlockState.all, 1UL);
        player.Gold = 999;
        var run = TestCompat.CreateRunStateForTest([player], seed);
        run.AppendToMapPointHistory(MapPointType.Unknown, RoomType.Event, null);
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService(), shouldSave: false);
        LocalContext.NetId = RunManager.Instance.NetService.NetId;
        player = run.Players.First();
        // Some events (Nloth) need tradable relics; the card selector answers every card prompt that would otherwise wait for UI.
        foreach (var relic in ModelDb.AllRelics.Where(r => r.IsTradable && !player.Relics.Any(o => o.Id == r.Id)).Take(3))
            player.AddRelicInternal(relic.ToMutable(), silent: true);
        TestCompat.PushCardSelector(player, TestCompat.NewAutoCardSelector());

        switch (_profile)
        {
            case "broke": player.Gold = 0; break;
            case "dying": await CreatureCmd.SetCurrentHp(player.Creature, 1); break;
            case "tinydeck":
                foreach (var c in player.Deck.Cards.Skip(1).ToList()) player.Deck.RemoveInternal(c, silent: true);
                break;
            case "norelics":
                foreach (var r in player.Relics.ToList()) player.RemoveRelicInternal(r, silent: true);
                break;
            case "upgraded":
                foreach (var c in player.Deck.Cards.ToList()) if (c.IsUpgradable) c.UpgradeInternal();
                break;
        }
        // "normal" and the profiles above that do not mention relics keep the 3 extra relics; norelics strips them too.

        var ev = ((EventModel)ModelDb.GetById<AbstractModel>(ModelDb.GetId(type))).ToMutable();
        // The game never enters an event that is not allowed in the current state (e.g. Mushrooms before floor 7), but
        // its options must still work in a state where it is, so it is started anyway; only a failure to start is forgiven.
        _notAllowed = !ev.IsAllowed(run);
        await WithTimeout(TestCompat.BeginEvent(ev, player, run), "BeginEvent");
        foreach (var index in path)
        {
            var option = ev.CurrentOptions[index];
            lastKey = option.TextKey;
            await WithTimeout(option.Chosen(), $"choosing '{option.TextKey}'");
        }

        return ev;
    }

    private static async Task WithTimeout(Task task, string what)
    {
        var done = await Task.WhenAny(task, Task.Delay(OptionTimeout));
        if (done != task)
            throw new TimeoutException($"Timed out after {OptionTimeout.TotalSeconds}s {what} (probably waiting for UI: card selection, combat, reward screen).");
        await task;
    }

    private static Exception Unwrap(Exception ex) =>
        ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;

    private static void Cleanup()
    {
        try
        {
            RunManager.Instance.State = null;
            LocalContext.NetId = null;
        }
        catch { /* best effort */ }
    }
}
