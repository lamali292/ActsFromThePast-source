using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class SlimeBossBoss() : AftpEncounterModel(RoomType.Boss)
{
    public override string BossNodePath => "res://ActsFromThePast/map_boss_icons/slime_boss";

    public override bool HasScene => true;

    public override IReadOnlyList<string> Slots =>
    [
        "spike_med_1", "spike_large", "spike_med_2",
        "acid_med_1", "slime_boss", "acid_large", "acid_med_2"
    ];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        new List<MonsterModel>
        {
            ModelDb.Monster<SlimeBoss>(),
            ModelDb.Monster<SpikeSlimeLarge>(),
            ModelDb.Monster<SpikeSlimeMedium>(),
            ModelDb.Monster<AcidSlimeLarge>(),
            ModelDb.Monster<AcidSlimeMedium>()
        };

    public override bool IsValidForAct(ActModel act)
    {
        return act is ExordiumAct;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<SlimeBoss>().ToMutable(), "slime_boss")
        };
    }
}