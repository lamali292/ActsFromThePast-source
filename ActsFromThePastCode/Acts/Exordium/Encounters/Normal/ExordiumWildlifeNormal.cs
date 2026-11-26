using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ExordiumWildlifeNormal() : AftpEncounterModel(RoomType.Monster)
{
    public override IEnumerable<EncounterTag> Tags => [];
    public override bool IsWeak => false;

    private static MonsterModel[] StrongPool =>
    [
        ModelDb.Monster<FungiBeast>(),
        ModelDb.Monster<JawWorm>()
    ];

    private static MonsterModel[] WeakPool =>
    [
        ModelDb.Monster<LouseRed>(),
        ModelDb.Monster<LouseGreen>(),
        ModelDb.Monster<SpikeSlimeMedium>(),
        ModelDb.Monster<AcidSlimeMedium>()
    ];

    public override IEnumerable<MonsterModel> AllPossibleMonsters => StrongPool.Concat(WeakPool);

    public override bool IsValidForAct(ActModel act)
    {
        return act is ExordiumAct;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (Rng.NextRequired(StrongPool).ToMutable(), null),
            (Rng.NextRequired(WeakPool).ToMutable(), null)
        };
    }
}