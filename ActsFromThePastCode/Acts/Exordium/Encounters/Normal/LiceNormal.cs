using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class LiceNormal() : AftpEncounterModel(RoomType.Monster)
{
    public override IEnumerable<EncounterTag> Tags => [CustomEncounterTags.Lice];
    public override bool IsWeak => false;

    private static MonsterModel[] Lice =>
    [
        ModelDb.Monster<LouseRed>(),
        ModelDb.Monster<LouseGreen>()
    ];

    public override IEnumerable<MonsterModel> AllPossibleMonsters => Lice;

    public override bool IsValidForAct(ActModel act)
    {
        return act is ExordiumAct;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (Rng.NextRequired(Lice).ToMutable(), null),
            (Rng.NextRequired(Lice).ToMutable(), null),
            (Rng.NextRequired(Lice).ToMutable(), null)
        };
    }
}