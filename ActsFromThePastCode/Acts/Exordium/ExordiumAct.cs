using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Unlocks;

namespace ActsFromThePast.Acts;

public sealed class ExordiumAct() : CustomActModel(1)
{
    public override int Index => 0;

    public override bool IsDefault => false;

    public override IEnumerable<EventModel> AllEvents =>
    [
        ModelDb.Event<TrashHeap>()
    ];

    public override IEnumerable<AncientEventModel> AllAncients => Act1Ancients;

    // Colors differ from CustomActModel defaults (which are act 3 themed)
    public override Color MapTraveledColor => new("28231D");
    public override Color MapUntraveledColor => new("877256");
    public override Color MapBgColor => new("A78A67");

    // One FMOD event per theme (picked by run seed); the base game's NRunMusicController drives them through the
    // global Progress parameter. See fmod/build.py and MusicPatches.
    public override string[] BgMusicOptions =>
    [
        "event:/music/actsfromthepast/exordium_1",
        "event:/music/actsfromthepast/exordium_2"
    ];

    public override string[] MusicBankPaths => [AFTPModAudio.MusicBank, AFTPModAudio.MusicBank];
    public override string AmbientSfx => "event:/ambience/actsfromthepast/exordium_ambience";

    // The act's combat background is a scene whose script draws everything (see the *Background class).
    protected override string CustomBackgroundScenePath =>
        "res://ActsFromThePast/scenes/backgrounds/exordium_act/exordium_act_background.tscn";

    protected override BackgroundAssets CustomGenerateBackgroundAssets(Rng rng)
    {
        return new CustomBackgroundAssets(CustomBackgroundScenePath, [], null!);
    }

    public override string ChestSpineResourcePath =>
        "res://animations/backgrounds/treasure_room/chest_room_act_1_skel_data.tres";

    public override string ChestSpineSkinNameNormal => "act1";
    public override string ChestSpineSkinNameStroke => "act1_stroke";
    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act1";

    protected override string CustomMapTopBgPath =>
        "res://ActsFromThePast/images/packed/map/map_bgs/exordium_act/map_top_exordium_act.png";

    protected override string CustomMapMidBgPath =>
        "res://ActsFromThePast/images/packed/map/map_bgs/exordium_act/map_middle_exordium_act.png";

    protected override string CustomMapBotBgPath =>
        "res://ActsFromThePast/images/packed/map/map_bgs/exordium_act/map_middle_exordium_act.png";

    protected override string CustomRestSiteBackgroundPath => "res://scenes/rest_site/overgrowth_rest_site.tscn";

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
        return obj is ExordiumAct;
    }

    public override int GetHashCode()
    {
        return typeof(ExordiumAct).GetHashCode();
    }
}