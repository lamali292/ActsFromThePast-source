using System.Reflection;
using ActsFromThePast.Acts.TheCity;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Patches.Creatures;

public class VisualsPatches
{
    // so enemies can't be completely obscured by background elements
    [HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
    public static class CreatureVisualsLayerPatch
    {
        private static bool IsCityAct()
        {
            return RunManager.Instance.State?.Act is TheCityAct;
        }

        public static void Postfix(NCreature __instance)
        {
            if (!IsCityAct()) return;

            var visuals = __instance.GetChildren().OfType<NCreatureVisuals>().FirstOrDefault();

            visuals?.ZIndex = -5;
        }
    }

    [HarmonyPatch(typeof(NGameOverScreen), "MoveCreaturesToDifferentLayerAndDisableUi")]
    public static class ResetCreatureZOnGameOverPatch
    {
        public static void Postfix(NGameOverScreen __instance, Control ____creatureContainer)
        {
            foreach (var visuals in ____creatureContainer.GetChildren().OfType<NCreatureVisuals>())
                visuals.ZIndex = 0;
        }
    }
}