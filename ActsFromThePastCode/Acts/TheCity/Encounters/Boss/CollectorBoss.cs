using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class CollectorBoss() : AftpEncounterModel(RoomType.Boss)
{
    public override string BossNodePath => "res://ActsFromThePast/map_boss_icons/collector";
    public override bool HasScene => true;

    public override IReadOnlyList<string> Slots =>
    [
        "torch1", "torch2", "collector"
    ];

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<Collector>();
            yield return ModelDb.Monster<TorchHead>();
        }
    }

    public override bool IsValidForAct(ActModel act)
    {
        return act is TheCityAct;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Collector>().ToMutable(), "collector")
        };
    }
}