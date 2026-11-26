using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ThreeByrdsWeak() : AftpEncounterModel(RoomType.Monster)
{
    public override IEnumerable<EncounterTag> Tags => [];
    public override bool IsWeak => true;
    public override bool HasScene => true;
    public override IReadOnlyList<string> Slots => ["first", "second", "third"];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Byrd>()
    ];

    public override bool IsValidForAct(ActModel act)
    {
        return act is TheCityAct;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Byrd>().ToMutable(), "first"),
            (ModelDb.Monster<Byrd>().ToMutable(), "second"),
            (ModelDb.Monster<Byrd>().ToMutable(), "third")
        };
    }
}