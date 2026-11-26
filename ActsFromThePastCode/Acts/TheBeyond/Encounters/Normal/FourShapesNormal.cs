using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class FourShapesNormal() : AftpEncounterModel(RoomType.Monster)
{
    public override IEnumerable<EncounterTag> Tags => [CustomEncounterTags.Shapes];
    public override bool HasScene => true;
    public override IReadOnlyList<string> Slots => ["first", "second", "third", "fourth"];

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<Repulsor>();
            yield return ModelDb.Monster<Exploder>();
            yield return ModelDb.Monster<Spiker>();
        }
    }

    public override bool IsValidForAct(ActModel act)
    {
        return act is TheBeyondAct;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var pool = new List<Func<MonsterModel>>
        {
            () => ModelDb.Monster<Repulsor>().ToMutable(),
            () => ModelDb.Monster<Repulsor>().ToMutable(),
            () => ModelDb.Monster<Exploder>().ToMutable(),
            () => ModelDb.Monster<Exploder>().ToMutable(),
            () => ModelDb.Monster<Spiker>().ToMutable(),
            () => ModelDb.Monster<Spiker>().ToMutable()
        };

        var result = new List<(MonsterModel, string?)>();

        for (var i = 0; i < Slots.Count; i++)
        {
            var index = Rng.NextInt(pool.Count);
            result.Add((pool[index](), Slots[i]));
            pool.RemoveAt(index);
        }

        return result;
    }
}