using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class DeadAdventurerLagavulin() : AftpEncounterModel(RoomType.Elite)
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get { yield return ModelDb.Monster<Lagavulin>(); }
    }

    public override bool IsValidForAct(ActModel act)
    {
        return false;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var lagavulin = (Lagavulin)ModelDb.Monster<Lagavulin>().ToMutable();
        lagavulin.StartsAwake = true;
        return new List<(MonsterModel, string?)>
        {
            (lagavulin, null)
        };
    }
}