using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class RedMaskBanditsEvent() : AftpEncounterModel(RoomType.Monster)
{
    public override IEnumerable<EncounterTag> Tags => [];
    public override bool IsWeak => false;
    public override bool HasScene => true;
    public override IReadOnlyList<string> Slots => ["pointy", "romeo", "bear"];

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<Pointy>();
            yield return ModelDb.Monster<Romeo>();
            yield return ModelDb.Monster<Bear>();
        }
    }

    public override bool IsValidForAct(ActModel act)
    {
        return false;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Pointy>().ToMutable(), "pointy"),
            (ModelDb.Monster<Romeo>().ToMutable(), "romeo"),
            (ModelDb.Monster<Bear>().ToMutable(), "bear")
        };
    }
}