using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace ActsFromThePast;

public static class SpineBones
{
    /// <summary>The Spine bone object of the named bone, for attaching effects to it.</summary>
    public static GodotObject? Find(MegaSprite? body, string boneName)
    {
        return body?.GetSkeleton()?.FindBone(boneName)?.BoundObject;
    }
}
