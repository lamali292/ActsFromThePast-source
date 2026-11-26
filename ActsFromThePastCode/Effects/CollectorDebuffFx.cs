using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

/// <summary>
///     The sound and the curse effect of the Collector's Mega Debuff, for anything that wants to play them on creatures without
///     the Collector doing the move (the Dread Dominion of the Collector, in ActsFromThePast.Dread.dll). A plain static class on
///     purpose: that assembly has no reference to Godot, so it can not touch <see cref="CollectorCurseEffect" /> directly.
/// </summary>
public static class CollectorDebuffFx
{
    /// <summary>How long the Collector waits for the effect before the debuffs land.</summary>
    public const float Duration = 2.0f;

    /// <summary>Plays the sound once and the curse effect on every given creature at the same time.</summary>
    public static void PlayOn(IEnumerable<Creature> targets)
    {
        AFTPModAudio.Play("collector", "collector_debuff");

        var room = NCombatRoom.Instance;
        if (room == null) return;

        foreach (var target in targets)
        {
            var node = room.GetCreatureNode(target);
            if (node == null) continue;

            room.CombatVfxContainer.AddChildSafely(CollectorCurseEffect.Create(node.VfxSpawnPosition));
        }
    }
}
