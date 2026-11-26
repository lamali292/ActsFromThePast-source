using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class LotsOfSlimesNormal() : AftpEncounterModel(RoomType.Monster)
{
    public override IEnumerable<EncounterTag> Tags => [EncounterTag.Slimes];
    public override bool IsWeak => false;
    public override bool HasScene => true;
    public override IReadOnlyList<string> Slots => ["first", "second", "third", "fourth", "fifth"];

    private static MonsterModel[] SmallSlimes =>
    [
        ModelDb.Monster<SpikeSlimeSmall>(),
        ModelDb.Monster<AcidSlimeSmall>()
    ];

    public override IEnumerable<MonsterModel> AllPossibleMonsters => SmallSlimes;

    public override bool IsValidForAct(ActModel act)
    {
        return act is ExordiumAct;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var pool = new List<Func<MonsterModel>>
        {
            () => ModelDb.Monster<SpikeSlimeSmall>().ToMutable(),
            () => ModelDb.Monster<SpikeSlimeSmall>().ToMutable(),
            () => ModelDb.Monster<SpikeSlimeSmall>().ToMutable(),
            () => ModelDb.Monster<AcidSlimeSmall>().ToMutable(),
            () => ModelDb.Monster<AcidSlimeSmall>().ToMutable()
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