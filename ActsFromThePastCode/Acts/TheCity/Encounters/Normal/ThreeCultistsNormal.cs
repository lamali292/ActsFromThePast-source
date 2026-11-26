using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ThreeCultistsNormal() : AftpEncounterModel(RoomType.Monster)
{
    public override bool HasScene => true;
    public override IReadOnlyList<string> Slots => ["first", "second", "third"];

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get { yield return ModelDb.Monster<Cultist>(); }
    }

    public override bool IsValidForAct(ActModel act)
    {
        return act is TheCityAct;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Cultist>().ToMutable(), "first"),
            (ModelDb.Monster<Cultist>().ToMutable(), "second"),
            (ModelDb.Monster<Cultist>().ToMutable(), "third")
        };
    }
}