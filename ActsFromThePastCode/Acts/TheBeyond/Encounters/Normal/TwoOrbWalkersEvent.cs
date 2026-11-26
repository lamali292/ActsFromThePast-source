using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public class TwoOrbWalkersEvent() : AftpEncounterModel(RoomType.Monster)
{
    public override bool HasScene => true;
    public override IReadOnlyList<string> Slots => ["left", "right"];

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get { yield return ModelDb.Monster<OrbWalker>(); }
    }

    public override IEnumerable<string> ExtraAssetPaths =>
    [
        "res://scenes/vfx/vfx_fire_burst.tscn"
    ];

    public override bool IsValidForAct(ActModel act)
    {
        return false;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<OrbWalker>().ToMutable(), "left"),
            (ModelDb.Monster<OrbWalker>().ToMutable(), "right")
        };
    }
}