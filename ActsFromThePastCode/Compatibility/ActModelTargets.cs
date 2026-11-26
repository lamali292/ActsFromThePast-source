using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Compatibility;

/// <summary>Patch targets that are ambiguous by name on one of the supported game versions.</summary>
internal static class ActModelTargets
{
    /// <summary>
    ///     The <c>GenerateRooms(Rng, UnlockState, ...)</c> overload, which exists on both versions. 0.113.0 adds a
    ///     <c>GenerateRooms(RunState)</c> wrapper that makes a by-name lookup ambiguous (Harmony throws on it).
    /// </summary>
    public static MethodBase GenerateRooms() =>
        AccessTools.FirstMethod(typeof(ActModel), m => m.Name == nameof(ActModel.GenerateRooms)
                                                       && m.GetParameters() is { Length: > 0 } ps && ps[0].ParameterType == typeof(Rng))
        ?? throw new MissingMethodException(nameof(ActModel), nameof(ActModel.GenerateRooms));
}
