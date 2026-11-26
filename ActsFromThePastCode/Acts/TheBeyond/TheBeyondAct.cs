using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Unlocks;

namespace ActsFromThePast.Acts.TheBeyond;

public sealed class TheBeyondAct() : CustomActModel(3)
{
    public override int Index => 2;

    public override bool IsDefault => false;

    public override IEnumerable<EventModel> AllEvents =>
    [
        ModelDb.Event<TrashHeap>()
    ];

    public override IEnumerable<AncientEventModel> AllAncients => Act3Ancients;

    // One FMOD event per theme (picked by run seed); the base game's NRunMusicController drives them through the
    // global Progress parameter. See fmod/build.py and MusicPatches.
    public override string[] BgMusicOptions =>
    [
        "event:/music/actsfromthepast/beyond_1",
        "event:/music/actsfromthepast/beyond_2"
    ];

    public override string[] MusicBankPaths => [AFTPModAudio.MusicBank, AFTPModAudio.MusicBank];
    public override string AmbientSfx => "event:/ambience/actsfromthepast/beyond_ambience";

    // The act's combat background is a scene whose script draws everything (see the *Background class).
    protected override string CustomBackgroundScenePath =>
        "res://ActsFromThePast/scenes/backgrounds/the_beyond_act/the_beyond_act_background.tscn";

    protected override BackgroundAssets CustomGenerateBackgroundAssets(Rng rng)
    {
        return new CustomBackgroundAssets(CustomBackgroundScenePath, [], null!);
    }

    // Chest open SFX says act2 — intentional?
    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act2";

    public override Color MapTraveledColor => new("1D1E2F");
    public override Color MapUntraveledColor => new("60717C");
    public override Color MapBgColor => new("819A97");
    protected override int NumberOfWeakEncounters => 2;

    protected override string CustomMapTopBgPath =>
        "res://ActsFromThePast/images/packed/map/map_bgs/the_beyond_act/map_top_the_beyond_act.png";

    protected override string CustomMapMidBgPath =>
        "res://ActsFromThePast/images/packed/map/map_bgs/the_beyond_act/map_middle_the_beyond_act.png";

    protected override string CustomMapBotBgPath =>
        "res://ActsFromThePast/images/packed/map/map_bgs/the_beyond_act/map_middle_the_beyond_act.png";

    protected override string CustomRestSiteBackgroundPath => "res://scenes/rest_site/glory_rest_site.tscn";

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
        return obj is TheBeyondAct;
    }

    public override int GetHashCode()
    {
        return typeof(TheBeyondAct).GetHashCode();
    }
}