using ActsFromThePast.Acts.TheBeyond.Events;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast;

/// <summary>
///     Encounters that only exist for the Mind Bloom event, where they are bosses. The event room stays at the bottom of
///     the room stack for the whole event, including its combat.
/// </summary>
public abstract class MindBloomEncounterModel() : AftpEncounterModel(RoomType.Monster)
{
    public override RoomType RoomType =>
        RunManager.Instance.State?.BaseRoom is EventRoom { CanonicalEvent: MindBloom }
            ? RoomType.Boss
            : base.RoomType;
}
