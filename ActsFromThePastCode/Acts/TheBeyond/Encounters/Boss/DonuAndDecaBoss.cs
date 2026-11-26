using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class DonuAndDecaBoss() : AftpEncounterModel(RoomType.Boss)
{
    public override string BossNodePath => "res://ActsFromThePast/map_boss_icons/donu_and_deca";

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Deca>(),
        ModelDb.Monster<Donu>()
    ];

    public override bool IsValidForAct(ActModel act)
    {
        return act is TheBeyondAct;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Deca>().ToMutable(), null),
            (ModelDb.Monster<Donu>().ToMutable(), null)
        };
    }
}