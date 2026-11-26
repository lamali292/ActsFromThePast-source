namespace ActsFromThePast;

public static class LegacyBossHelper
{
    // The stinger replaces the act's music event (NRunMusicController.PlayCustomMusic), so UpdateTrack leaves it alone.
    public static void OnBossVictory(bool skipStingerIntro = false)
    {
        AFTPModAudio.Play("boss", "boss_victory_stinger");
        AFTPModAudio.PlayBossStinger(skipStingerIntro);
    }
}
