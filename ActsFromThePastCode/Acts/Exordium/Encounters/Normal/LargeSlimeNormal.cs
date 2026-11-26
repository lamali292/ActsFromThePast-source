using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class LargeSlimeNormal() : AftpEncounterModel(RoomType.Monster)
{
    public override IEnumerable<EncounterTag> Tags => [EncounterTag.Slimes];
    public override bool IsWeak => false;

    private static MonsterModel[] LargeSlimes =>
    [
        ModelDb.Monster<AcidSlimeLarge>(),
        ModelDb.Monster<SpikeSlimeLarge>()
    ];

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<AcidSlimeLarge>();
            yield return ModelDb.Monster<SpikeSlimeLarge>();
            yield return ModelDb.Monster<AcidSlimeMedium>();
            yield return ModelDb.Monster<SpikeSlimeMedium>();
        }
    }

    public override bool IsValidForAct(ActModel act)
    {
        return act is ExordiumAct;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (Rng.NextRequired(LargeSlimes).ToMutable(), null)
        };
    }
}