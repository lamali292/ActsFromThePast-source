using System.Reflection;
using System.Runtime.Loader;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;

namespace ActsFromThePast.Compatibility;

/// <summary>
///     Loads <c>ActsFromThePast.Dread.dll</c> (the Dread-mode Dominions) on games that have Dread mode (0.113.0+).
///     The Dominions live in their own assembly because <c>DominionModel</c> does not exist on 0.111.1 and the game calls
///     <c>GetTypes()</c> on the mod dll, which would throw there for any type deriving from it. This class must not
///     reference anything from that assembly.
/// </summary>
internal static class DreadLoader
{
    private const string DominionModelType = "MegaCrit.Sts2.Core.Models.Dread.DominionModel";
    private const string DreadAssemblyFile = "ActsFromThePast.Dread.dll";
    private const string BootstrapType = "ActsFromThePast.Dread.DreadBootstrap";

    public static void Load()
    {
        var logger = new Logger("ActsFromThePast", LogType.Generic);
        try
        {
            if (AccessTools.TypeByName(DominionModelType) == null)
            {
                logger.Info("This game version has no Dread mode, skipping Dominions.");
                return;
            }

            var modAssembly = typeof(DreadLoader).Assembly;
            var path = Path.Combine(Path.GetDirectoryName(modAssembly.Location) ?? "", DreadAssemblyFile);
            if (!File.Exists(path))
            {
                logger.Warn($"{DreadAssemblyFile} not found next to the mod dll, skipping Dominions.");
                return;
            }

            var context = AssemblyLoadContext.GetLoadContext(modAssembly) ?? AssemblyLoadContext.Default;
            var dread = context.LoadFromAssemblyPath(path);
            dread.GetType(BootstrapType, true)!
                .GetMethod("Register", BindingFlags.Public | BindingFlags.Static)!
                .Invoke(null, null);
        }
        catch (Exception e)
        {
            // Dread Dominions are optional: never let them take the rest of the mod down.
            logger.Error($"Failed to load the Dread Dominions: {e}");
        }
    }
}
