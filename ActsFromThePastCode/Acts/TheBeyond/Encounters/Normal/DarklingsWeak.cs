using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public class DarklingsWeak() : AftpEncounterModel(RoomType.Monster)
{
    public override bool IsWeak => true;


    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get { yield return ModelDb.Monster<Darkling>(); }
    }

    public override bool IsValidForAct(ActModel act)
    {
        return act is TheBeyondAct;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var darklings = new List<(MonsterModel, string?)>();
        for (var i = 0; i < 3; i++)
        {
            var darkling = (Darkling)ModelDb.Monster<Darkling>().ToMutable();
            darkling.SlotIndex = i;
            darklings.Add((darkling, null));
        }

        return darklings;
    }
}