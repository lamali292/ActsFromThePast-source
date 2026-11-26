using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class PairOfParasitesNormal() : AftpEncounterModel(RoomType.Monster)
{
    public override IEnumerable<EncounterTag> Tags => [];

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<ShelledParasite>();
            yield return ModelDb.Monster<FungiBeast>();
        }
    }

    public override bool IsValidForAct(ActModel act)
    {
        return act is TheCityAct;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<ShelledParasite>().ToMutable(), null),
            (ModelDb.Monster<FungiBeast>().ToMutable(), null)
        };
    }
}