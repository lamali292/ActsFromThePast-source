using BaseLib.Config;
using Godot.Bridge;
using MegaCrit.Sts2.Core.Modding;

namespace ActsFromThePast;

[ModInitializer("Initialize")]
public class ActsFromThePastInitializer
{
    public static void Initialize()
    {
        // Lets Godot find the mod's script classes, which the act background scenes attach by path.
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(ActsFromThePastInitializer).Assembly);

        ActsFromThePastPatchManager.HarmonyPatches();

        // Dread-mode Dominions, only on game versions that have Dread mode (separate dll, see DreadLoader).
        Compatibility.DreadLoader.Load();

        ModConfigRegistry.Register("ActsFromThePast", new ActsFromThePastConfig());

        // Built from fmod/ by fmod/build.py. Master.strings.bank has to be loaded for the event paths to resolve.
        FmodStudio.RegisterBank("res://ActsFromThePast/audio/Master.strings.bank");
        FmodStudio.RegisterBank("res://ActsFromThePast/audio/ActsFromThePast.bank");
    }
}