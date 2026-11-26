using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ExordiumThugsNormal() : AftpEncounterModel(RoomType.Monster)
{
    public override IEnumerable<EncounterTag> Tags => [];
    public override bool IsWeak => false;

    private static MonsterModel[] FrontPool =>
    [
        ModelDb.Monster<LouseRed>(),
        ModelDb.Monster<LouseGreen>(),
        ModelDb.Monster<SpikeSlimeMedium>(),
        ModelDb.Monster<AcidSlimeMedium>()
    ];

    private static MonsterModel[] BackPool =>
    [
        ModelDb.Monster<Cultist>(),
        ModelDb.Monster<SlaverBlue>(),
        ModelDb.Monster<SlaverRed>(),
        ModelDb.Monster<Looter>()
    ];

    public override IEnumerable<MonsterModel> AllPossibleMonsters => FrontPool.Concat(BackPool);

    public override bool IsValidForAct(ActModel act)
    {
        return act is ExordiumAct;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (Rng.NextRequired(FrontPool).ToMutable(), null),
            (Rng.NextRequired(BackPool).ToMutable(), null)
        };
    }
}