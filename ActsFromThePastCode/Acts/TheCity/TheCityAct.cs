using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Unlocks;

namespace ActsFromThePast.Acts.TheCity;

public sealed class TheCityAct() : CustomActModel(2)
{
    public override int Index => 1;

    public override bool IsDefault => false;

    public override IEnumerable<EventModel> AllEvents =>
    [
        ModelDb.Event<TrashHeap>()
    ];

    public override IEnumerable<AncientEventModel> AllAncients => Act2Ancients;

    // One FMOD event per theme (picked by run seed); the base game's NRunMusicController drives them through the
    // global Progress parameter. See fmod/build.py and MusicPatches.
    public override string[] BgMusicOptions =>
    [
        "event:/music/actsfromthepast/city_1",
        "event:/music/actsfromthepast/city_2"
    ];

    public override string[] MusicBankPaths => [AFTPModAudio.MusicBank, AFTPModAudio.MusicBank];
    public override string AmbientSfx => "event:/ambience/actsfromthepast/city_ambience";

    // The act's combat background is a scene whose script draws everything (see the *Background class).
    protected override string CustomBackgroundScenePath =>
        "res://ActsFromThePast/scenes/backgrounds/the_city_act/the_city_act_background.tscn";

    protected override BackgroundAssets CustomGenerateBackgroundAssets(Rng rng)
    {
        return new CustomBackgroundAssets(CustomBackgroundScenePath, [], null!);
    }

    // Act 2 chest assets
    public override string ChestSpineResourcePath =>
        "res://animations/backgrounds/treasure_room/chest_room_act_2_skel_data.tres";

    public override string ChestSpineSkinNameNormal => "act2";
    public override string ChestSpineSkinNameStroke => "act2_stroke";
    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act2";

    // These happen to match CustomActModel's defaults, but keeping them explicit doesn't hurt
    public override Color MapTraveledColor => new("27221C");
    public override Color MapUntraveledColor => new("6E7750");
    public override Color MapBgColor => new("9B9562");
    protected override int NumberOfWeakEncounters => 2;

    protected override string CustomMapTopBgPath =>
        "res://ActsFromThePast/images/packed/map/map_bgs/the_city_act/map_top_the_city_act.png";

    protected override string CustomMapMidBgPath =>
        "res://ActsFromThePast/images/packed/map/map_bgs/the_city_act/map_middle_the_city_act.png";

    protected override string CustomMapBotBgPath =>
        "res://ActsFromThePast/images/packed/map/map_bgs/the_city_act/map_middle_the_city_act.png";

    protected override string CustomRestSiteBackgroundPath => "res://scenes/rest_site/hive_rest_site.tscn";

    public override IEnumerable<EncounterModel> GenerateAllEncounters()
    {
        return
        [
        ];
    }

    public override bool IsUnlocked(UnlockState unlockState)
    {
        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is TheCityAct;
    }

    public override int GetHashCode()
    {
        return typeof(TheCityAct).GetHashCode();
    }
}