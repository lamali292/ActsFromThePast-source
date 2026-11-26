using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class SlaverNormal() : AftpEncounterModel(RoomType.Monster)
{
    private static MonsterModel[] Slavers =>
    [
        ModelDb.Monster<SlaverRed>(),
        ModelDb.Monster<SlaverBlue>()
    ];

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<SlaverRed>();
            yield return ModelDb.Monster<SlaverBlue>();
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
            (Rng.NextRequired(Slavers).ToMutable(), null)
        };
    }
}