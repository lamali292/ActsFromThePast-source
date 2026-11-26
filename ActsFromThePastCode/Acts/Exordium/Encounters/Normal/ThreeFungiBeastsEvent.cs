using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ThreeFungiBeastsEvent() : AftpEncounterModel(RoomType.Monster)
{
    public override bool IsWeak => false;

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get { yield return ModelDb.Monster<FungiBeast>(); }
    }

    public override bool IsValidForAct(ActModel act)
    {
        return false;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<FungiBeast>().ToMutable(), null),
            (ModelDb.Monster<FungiBeast>().ToMutable(), null),
            (ModelDb.Monster<FungiBeast>().ToMutable(), null)
        };
    }
}