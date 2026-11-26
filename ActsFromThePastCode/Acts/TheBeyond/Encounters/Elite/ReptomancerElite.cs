using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters.Elite;

public sealed class ReptomancerElite() : AftpEncounterModel(RoomType.Elite)
{
    public override bool HasScene => true;

    public override IReadOnlyList<string> Slots =>
    [
        "dagger3", "dagger1", "reptomancer", "dagger2", "dagger4"
    ];

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            yield return ModelDb.Monster<Reptomancer>();
            yield return ModelDb.Monster<SnakeDagger>();
        }
    }

    public override bool IsValidForAct(ActModel act)
    {
        return act is TheBeyondAct;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var result = new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<SnakeDagger>().ToMutable(), "dagger3"),
            (ModelDb.Monster<SnakeDagger>().ToMutable(), "dagger4"),
            (ModelDb.Monster<Reptomancer>().ToMutable(), "reptomancer")
        };

        return result;
    }
}