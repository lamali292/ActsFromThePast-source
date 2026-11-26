using ActsFromThePast.Compatibility;
using System.Reflection;
using ActsFromThePast.Acts;
using ActsFromThePast.Acts.TheBeyond;
using ActsFromThePast.Acts.TheCity;
using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Patches.Events;

public class ShrinePatches
{
    [HarmonyPatch]
    [HarmonyPriority(Priority.Low)]
    public static class EventPoolPatch
    {
        private static MethodBase TargetMethod() => ActModelTargets.GenerateRooms();

        private const float ShrineChance = 0.25f;

        private static readonly Assembly ModAssembly = typeof(EventPoolPatch).Assembly;

        private static readonly HashSet<EventModel> ModSharedEvents =
            [..CustomContentDictionary.SharedCustomEvents.Where(e => e.GetType().Assembly == ModAssembly)];

        private static bool IsLegacyAct(ActModel act)
        {
            return act is ExordiumAct or TheCityAct or TheBeyondAct;
        }

        private static bool IsModSharedEvent(EventModel e)
        {
            return ModSharedEvents.Contains(e);
        }

        private static bool IsBaseGameSharedEvent(EventModel e)
        {
            return ModelDb.AllSharedEvents.Contains(e) && !IsModSharedEvent(e);
        }

        private static int GetActNumber(ActModel act)
        {
            return act switch
            {
                Overgrowth or Underdocks => 1,
                Hive => 2,
                Glory => 3,
                CustomActModel custom => custom.Index + 1,
                _ => -1
            };
        }

        public static void Postfix(ActModel __instance, Rng rng)
        {
            var rooms = __instance._rooms;

            // --- Phase 1: Filter ---

            if (IsLegacyAct(__instance) && !ActsFromThePastConfig.AllowNonLegacySharedEventsInLegacyActs)
                rooms.events.RemoveAll(e => IsBaseGameSharedEvent(e));

            if (!IsLegacyAct(__instance) && !ActsFromThePastConfig.AllowLegacySharedEventsInNonLegacyActs)
                rooms.events.RemoveAll(e => IsModSharedEvent(e));

            var actNumber = GetActNumber(__instance);
            if (actNumber >= 0)
                rooms.events.RemoveAll(e =>
                    e is IActRestricted restricted &&
                    !restricted.AllowedActIndices.Contains(actNumber));

            // --- Phase 2: Interleave shrines ---

            var shrines = rooms.events.Where(e => e is IShrineEvent).ToList();
            if (shrines.Count == 0) return;

            var regular = rooms.events.Where(e => e is not IShrineEvent).ToList();

            var result = new List<EventModel>();
            var shrineIdx = 0;
            var regularIdx = 0;
            var totalEvents = shrines.Count + regular.Count;

            for (var i = 0; i < totalEvents; i++)
            {
                var tryShrine = rng.NextFloat() < ShrineChance;

                if (tryShrine && shrineIdx < shrines.Count)
                    result.Add(shrines[shrineIdx++]);
                else if (regularIdx < regular.Count)
                    result.Add(regular[regularIdx++]);
                else
                    result.Add(shrines[shrineIdx++]);
            }

            rooms.events.Clear();
            rooms.events.AddRange(result);
        }
    }

    [HarmonyPatch(typeof(RoomSet), nameof(RoomSet.EnsureNextEventIsValid))]
    public static class RepeatableShrineValidityPatch
    {
        [ThreadStatic] private static List<ModelId>? _temporarilyRemoved;

        public static void Prefix(RoomSet __instance, RunState runState)
        {
            _temporarilyRemoved = null;
            if (__instance.events.Count == 0) return;

            var visited = runState._visitedEventIds;

            foreach (var e in __instance.events)
                if (e is IShrineEvent { IsOneTimeEvent: false } &&
                    visited.Contains(e.Id))
                {
                    _temporarilyRemoved ??= [];
                    _temporarilyRemoved.Add(e.Id);
                }

            if (_temporarilyRemoved != null)
                foreach (var id in _temporarilyRemoved)
                    visited.Remove(id);
        }

        public static void Postfix(RunState runState)
        {
            if (_temporarilyRemoved == null) return;

            var visited = runState._visitedEventIds;

            foreach (var id in _temporarilyRemoved)
                visited.Add(id);

            _temporarilyRemoved = null;
        }
    }
}