using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class MindBloomGuardian() : MindBloomEncounterModel
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        new List<MonsterModel>
        {
            ModelDb.Monster<Guardian>()
        };

    public override bool IsValidForAct(ActModel act)
    {
        return false;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Guardian>().ToMutable(), null)
        };
    }
}