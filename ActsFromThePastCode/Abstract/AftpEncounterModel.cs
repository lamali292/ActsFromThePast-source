using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public abstract class AftpEncounterModel(RoomType roomType) : CustomEncounterModel(roomType)
{
    public override string? CustomScenePath => Id.Entry.EncounterScenePath();
    public override string? CustomRunHistoryIconPath => Id.Entry.RunHistoryIconPath();
    public override string? CustomRunHistoryIconOutlinePath => Id.Entry.RunHistoryIconOutlinePath();
}
