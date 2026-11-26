using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class GremlinGangNormal() : AftpEncounterModel(RoomType.Monster)
{
    public override IEnumerable<EncounterTag> Tags => [];
    public override bool IsWeak => false;
    public override bool HasScene => true;
    public override IReadOnlyList<string> Slots => ["first", "second", "third", "fourth"];

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<GremlinMad>();
            yield return ModelDb.Monster<GremlinSneaky>();
            yield return ModelDb.Monster<GremlinFat>();
            yield return ModelDb.Monster<GremlinShield>();
            yield return ModelDb.Monster<GremlinWizard>();
        }
    }

    public override IEnumerable<string> ExtraAssetPaths =>
    [
        "res://scenes/vfx/vfx_fire_burst.tscn"
    ];

    public override bool IsValidForAct(ActModel act)
    {
        return act is ExordiumAct;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var pool = new List<Func<MonsterModel>>
        {
            () => ModelDb.Monster<GremlinMad>().ToMutable(),
            () => ModelDb.Monster<GremlinMad>().ToMutable(),
            () => ModelDb.Monster<GremlinSneaky>().ToMutable(),
            () => ModelDb.Monster<GremlinSneaky>().ToMutable(),
            () => ModelDb.Monster<GremlinFat>().ToMutable(),
            () => ModelDb.Monster<GremlinFat>().ToMutable(),
            () => ModelDb.Monster<GremlinShield>().ToMutable(),
            () => ModelDb.Monster<GremlinWizard>().ToMutable()
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