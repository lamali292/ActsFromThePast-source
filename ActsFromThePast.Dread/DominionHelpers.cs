using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Dread;

internal static class DominionHelpers
{
    /// <summary>True while the creature is fighting in an elite room.</summary>
    public static bool IsEliteCombat(Creature? creature) =>
        creature?.CombatState?.RunState.CurrentRoom is CombatRoom { Encounter.RoomType: RoomType.Elite };
}
