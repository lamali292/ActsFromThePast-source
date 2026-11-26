using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

/// <summary>
///     The base game rescales the combat room's BgContainer by the window's aspect ratio (NCombatSceneContainer): 0.9
///     from 16:9 up, 1.08 below it. Its own background art is oversized to survive that range, and ours has to be as
///     well: the layers are laid out for 1.08, so they are enlarged by 1.08 / 0.9 and still cover the screen at 0.9.
///     Below 16:9 the background is larger than it needs to be, which only crops the edges.
/// </summary>
public static class LegacyBackgroundScaling
{
    private const float LaidOutForScale = 1.08f;
    private const float SmallestContainerScale = 0.9f;

    /// <summary>
    ///     The base game only sets BgContainer's scale when the window is resized, so a new combat room starts at scale 1
    ///     and then jumps to 0.9 / 1.08 at the first resize or fullscreen toggle. Running its handler once now puts the
    ///     room in the state it would end up in anyway.
    /// </summary>
    public static void ApplyContainerScale(NCombatRoom room)
    {
        (room.SceneContainer as NCombatSceneContainer)?.OnWindowChange();
    }

    public static readonly Vector2 Oversize = Vector2.One * (LaidOutForScale / SmallestContainerScale);
}
