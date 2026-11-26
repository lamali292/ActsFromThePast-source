using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class SphereAndTwoShapesNormal() : AftpEncounterModel(RoomType.Monster)
{
    public override IEnumerable<EncounterTag> Tags => [CustomEncounterTags.Shapes];
    public override bool IsWeak => false;
    public override bool HasScene => true;
    public override IReadOnlyList<string> Slots => ["first", "second", "sphere"];

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<Spiker>();
            yield return ModelDb.Monster<Repulsor>();
            yield return ModelDb.Monster<Exploder>();
            yield return ModelDb.Monster<SphericGuardian>();
        }
    }

    public override bool IsValidForAct(ActModel act)
    {
        return act is TheBeyondAct;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (RandomShape(), "first"),
            (RandomShape(), "second"),
            (ModelDb.Monster<SphericGuardian>().ToMutable(), "sphere")
        };
    }

    private MonsterModel RandomShape()
    {
        return Rng.NextInt(3) switch
        {
            0 => ModelDb.Monster<Spiker>().ToMutable(),
            1 => ModelDb.Monster<Repulsor>().ToMutable(),
            _ => ModelDb.Monster<Exploder>().ToMutable()
        };
    }
}