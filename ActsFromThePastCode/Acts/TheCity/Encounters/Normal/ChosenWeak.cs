using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ChosenWeak() : AftpEncounterModel(RoomType.Monster)
{
    public override IEnumerable<EncounterTag> Tags => [];

    public override bool IsWeak => true;

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get { yield return ModelDb.Monster<Chosen>(); }
    }

    public override IEnumerable<string> ExtraAssetPaths =>
    [
        "res://scenes/vfx/vfx_fire_burst.tscn"
    ];

    public override bool IsValidForAct(ActModel act)
    {
        return act is TheCityAct;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Chosen>().ToMutable(), null)
        };
    }
}