using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class JawWormHordeNormal() : AftpEncounterModel(RoomType.Monster)
{
    public override bool HasScene => true;
    public override IReadOnlyList<string> Slots => ["first", "second", "third"];

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get { yield return ModelDb.Monster<JawWorm>(); }
    }

    public override bool IsValidForAct(ActModel act)
    {
        return act is TheBeyondAct;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var worm0 = (JawWorm)ModelDb.Monster<JawWorm>().ToMutable();
        var worm1 = (JawWorm)ModelDb.Monster<JawWorm>().ToMutable();
        var worm2 = (JawWorm)ModelDb.Monster<JawWorm>().ToMutable();

        worm0.HardMode = true;
        worm1.HardMode = true;
        worm2.HardMode = true;

        return new List<(MonsterModel, string?)>
        {
            (worm0, "first"),
            (worm1, "second"),
            (worm2, "third")
        };
    }
}