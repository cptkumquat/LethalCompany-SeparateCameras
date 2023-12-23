using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SeparateCameras.Patch
{
    [HarmonyPatch(typeof(ManualCameraRenderer))]
    internal class MCRPatch
    {
        [HarmonyPatch("updateMapTarget")]
        [HarmonyPrefix]
        static void updateMapTarget_Before(ManualCameraRenderer __instance)
        {
            Plugin.LogInfo($"MCR::updateMapTarget({__instance.name}): ENTER");
            Plugin.LogInfo($"MCR::updateMapTarget({__instance.name}): Old target is {__instance.radarTargets[__instance.targetTransformIndex].name}");
        }

        [HarmonyPatch("updateMapTarget")]
        [HarmonyPostfix]
        static void updateMapTarget_After(ManualCameraRenderer __instance, Coroutine ___updateMapCameraCoroutine)
        {
            Plugin.LogInfo($"MCR::updateMapTarget({__instance.name}): New target is {__instance.radarTargets[__instance.targetTransformIndex].name}");
            Plugin.LogInfo($"MCR::updateMapTarget({__instance.name}): EXIT");
        }
    }
    [HarmonyPatch(typeof(Terminal))]
    internal class TerminalPatch
    {
        static ManualCameraRenderer mcr;

        [HarmonyPatch(nameof(Terminal.RunTerminalEvents))]
        [HarmonyPrefix]
        static void RunTerminalEvents_Before(Terminal __instance, TerminalNode __0)
        {
            Plugin.LogInfo("RunTerminalEvents: ENTER");
            var node = __0;

            var viewKeyword = __instance.terminalNodes.allKeywords.FirstOrDefault((tk) => tk.word == "view");
            if (viewKeyword == null)
            {
                Plugin.LogInfo("RunTerminalEvents: EARLY EXIT (viewKeyword is null)");
                return;
            }
            var monitorNoun = viewKeyword.compatibleNouns.FirstOrDefault((n) => n.noun.word == "monitor");
            if (monitorNoun == null)
            {
                Plugin.LogInfo("RunTerminalEvents: EARLY EXIT (monitorNoun is null)");
                return;
            }
            var viewMonitorNode = monitorNoun.result;

            if (node == viewMonitorNode)
            {
                if (mcr != null || StartOfRound.Instance == null || StartOfRound.Instance.mapScreen == null || !StartOfRound.Instance.mapScreen.isActiveAndEnabled)
                {
                    return;
                }
                var mapScreen = StartOfRound.Instance.mapScreen;
                mcr = ManualCameraRenderer.Instantiate(mapScreen);
                mcr.name = "terminalMapScreen";
                mcr.cam = Camera.Instantiate(mcr.mapCamera);
                mcr.mapCamera = mcr.cam;
                viewMonitorNode.displayTexture = mcr.mapCamera.targetTexture;
            }


            if (node.terminalEvent == "switchCamera")
            {
                node.terminalEvent = "_switchCamera";
                Plugin.LogInfo($"RunTerminalEvents: setting camera to {mcr.radarTargets[mcr.targetTransformIndex+1 % mcr.radarTargets.Count].name}");
                mcr.SwitchRadarTargetForward(callRPC: true);
            }
        }

        [HarmonyPatch(nameof(Terminal.RunTerminalEvents))]
        [HarmonyPostfix]
        static void RunTerminalEvents_After(Terminal __instance, TerminalNode __0)
        {
            var node = __0;

            if (node.terminalEvent == "_switchCamera")
            {
                node.terminalEvent = "switchCamera";
            }

            Plugin.LogInfo("RunTerminalEvents: EXIT");
        }
    }


}
