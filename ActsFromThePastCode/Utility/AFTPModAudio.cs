using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Audio;

namespace ActsFromThePast;

/// <summary>
///     The mod's sounds, played through the base game's FMOD setup. The events live in the ActsFromThePast bank
///     (authored in fmod/, built by fmod/build.py) and are routed to the game's sfx / music / ambience buses, so the
///     volume sliders apply to them natively and music fades through the events' own attack and release.
/// </summary>
public static class AFTPModAudio
{
    private const string SfxBase = "event:/sfx/actsfromthepast/";
    private const string MusicBase = "event:/music/actsfromthepast/";

    /// <summary>
    ///     Music and ambience live in their own bank, which the acts hand to the base game's per-act bank loading
    ///     (Act.MusicBankPaths). The sfx bank is loaded by the mod at startup.
    /// </summary>
    public const string MusicBank = "res://ActsFromThePast/audio/ActsFromThePastMusic.bank";

    private static readonly string[] BossStingers =
    [
        "boss_victory_stinger_1",
        "boss_victory_stinger_2",
        "boss_victory_stinger_3",
        "boss_victory_stinger_4"
    ];

    // One-shot sound effect at fmod/Assets/sfx/<folder>/<soundName>.ogg. pitchVariation and basePitch are kept so
    // existing call sites compile; FMOD events have no per-call pitch.
    public static void Play(string folder, string soundName, float volume = 0f, float pitchVariation = 0f,
        float basePitch = 1f)
    {
        SfxCmd.Play($"{SfxBase}{folder}/{soundName}", Mathf.DbToLinear(volume));
    }

    public static void Play(Creature creature, string folder, string soundName, float volume = 0f)
    {
        Play(folder, soundName, volume);
    }

    // Replaces the act's music event with a one-shot stinger. The "_skip" variant is authored to start 1.5 s into
    // the audio.
    public static void PlayBossStinger(bool skipIntro = false)
    {
        var name = BossStingers[GD.RandRange(0, BossStingers.Length - 1)] + (skipIntro ? "_skip" : "");
        NRunMusicController.Instance?.PlayCustomMusic(MusicBase + name);
    }
}
