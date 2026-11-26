using ActsFromThePast.Acts;
using ActsFromThePast.Acts.Exordium.Events;
using ActsFromThePast.Acts.TheBeyond;
using ActsFromThePast.Acts.TheBeyond.Encounters;
using ActsFromThePast.Acts.TheBeyond.Events;
using ActsFromThePast.Acts.TheCity;
using ActsFromThePast.Acts.TheCity.Events;
using ActsFromThePast.SharedEvents;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Patches.Audio;

/// <summary>
///     The acts play their music through the base game's NRunMusicController: each act lists one FMOD event per theme
///     (Act.BgMusicOptions), the controller starts it and sets the global "Progress" parameter from the room type.
///     The events (see fmod/build.py) have an explore layer, an elite layer (Progress Elite / Elite2), a boss layer
///     and a shrine layer, the last two selected by the event-local "Mode" parameter.
///     The only thing the mod adds is the Mode, plus a few Progress overrides where the legacy game picked a
///     different track than the base game would. It is recomputed from the game state on every UpdateTrack, so there is
///     no music state of our own that could get out of sync with what is playing.
/// </summary>
public static class MusicPatches
{
    // MegaCrit.Sts2.Core.Nodes.Audio.NRunMusicController.MusicProgressTrack (private enum)
    private const int ProgressEnemy = 1;
    private const int ProgressElite = 6;

    // Must match the Mode parameter in fmod/tools/build_events.js
    private enum Mode
    {
        Normal = 0,
        Boss = 1,
        Shrine = 2,
        Silent = 3
    }

    private static RunState? RunState => RunManager.Instance.State;

    private static bool IsLegacyAct => RunState?.Act is ExordiumAct or TheCityAct or TheBeyondAct;

    private static IEnumerable<Creature> Enemies => (RunState?.CurrentRoom as CombatRoom)?.Enemies ?? Enumerable.Empty<Creature>();

    private static bool IsMindBloomEncounter =>
        (RunState?.CurrentRoom as CombatRoom)?.Encounter is MindBloomGuardian or MindBloomHexaghost or MindBloomSlimeBoss;

    private static bool IsHexaghostWaiting =>
        (RunState?.CurrentRoom as CombatRoom)?.Encounter is HexaghostBoss &&
        Enemies.Any(c => c.Monster is Hexaghost { IsActivated: false });

    private static bool IsLagavulinAsleep =>
        (RunState?.CurrentRoom as CombatRoom)?.Encounter is LagavulinElite &&
        Enemies.Any(c => c.Monster is Lagavulin { IsAwake: false });

    private static bool IsShrineEvent =>
        RunState?.CurrentRoom is EventRoom
        {
            CanonicalEvent: TheDivineFountain or Duplicator or GoldenShrine or Purifier or Transmogrifier or UpgradeShrine
        };

    // The event's combat rooms know which event they belong to; no flag of ours is needed.
    private static bool IsEliteEventCombat =>
        RunState?.CurrentRoom is CombatRoom { ParentEventId: { } eventId } &&
        (eventId == ModelDb.Event<DeadAdventurer>().Id || eventId == ModelDb.Event<MaskedBandits>().Id);

    private static bool IsArchitectEvent => RunState?.CurrentRoom is EventRoom { CanonicalEvent: TheArchitect };

    [HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.UpdateTrack), [])]
    public static class UpdateTrackPatch
    {
        public static void Postfix(NRunMusicController __instance)
        {
            if (!IsLegacyAct) return;

            var inCombat = CombatManager.Instance?.IsInProgress ?? false;
            var mode = Mode.Normal;
            int? progress = null;

            if (IsArchitectEvent)
            {
                mode = Mode.Silent;
            }
            else if (RunState?.CurrentRoom?.RoomType == RoomType.Boss)
            {
                // Before the fight starts the base game reports CombatEnd, which keeps playing the explore layer.
                // After it OnBossVictory has replaced the act's event with a stinger, which has no parameters.
                if (!inCombat) return;

                if (IsMindBloomEncounter) progress = ProgressElite;
                else if (IsHexaghostWaiting) mode = Mode.Silent;
                else mode = Mode.Boss;
            }
            else if (IsLagavulinAsleep)
            {
                progress = ProgressEnemy;
            }
            else if (inCombat && IsEliteEventCombat)
            {
                progress = ProgressElite;
            }
            else if (IsShrineEvent)
            {
                mode = Mode.Shrine;
            }

            __instance.UpdateMusicParameter("Mode", (float)mode);
            if (progress.HasValue)
                __instance._proxy?.Call("update_global_parameter", "Progress", progress.Value);
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
    public static class AfterCombatEndPatch
    {
        public static void Postfix(IRunState runState, CombatState? combatState, CombatRoom room)
        {
            // Putting Mind Bloom logic here since I don't want to patch the same hook twice in multiple classes
            if (combatState?.Encounter is MindBloomGuardian or MindBloomHexaghost or MindBloomSlimeBoss)
            {
                LegacyBossHelper.OnBossVictory(true);
            }
            else if (combatState?.Encounter is SlimeBossBoss or CollectorBoss or HexaghostBoss
                     or GuardianBoss or ChampBoss or BronzeAutomatonBoss or TimeEaterBoss
                     or AwakenedOneBoss or DonuAndDecaBoss)
            {
                LegacyBossHelper.OnBossVictory();
            }
        }
    }
}
