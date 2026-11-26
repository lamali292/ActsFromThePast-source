using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class OrbWalkerWeak() : AftpEncounterModel(RoomType.Monster)
{
    public override bool IsWeak => true;

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
        return act is TheBeyondAct;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<OrbWalker>().ToMutable(), null)
        };
    }
}