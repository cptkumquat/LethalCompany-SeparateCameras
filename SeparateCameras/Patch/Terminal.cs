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

        /**
         * Override `switch` to only change terminal radar
         */
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

        /**
         * Override `switch <playerName>` to only change terminal radar
         */
        [HarmonyPatch("ParsePlayerSentence")]
        [HarmonyPrefix]
        static bool ParsePlayerSentence_Before(Terminal __instance, ref TerminalNode __result, ref bool ___broadcastedCodeThisFrame)
        {
            ___broadcastedCodeThisFrame = false;
            string s = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded);
            s = RemovePunctuation(s);
            string[] array = s.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (__instance.currentNode != null && __instance.currentNode.overrideOptions)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    TerminalNode terminalNode = ParseWordOverrideOptions(array[i], __instance.currentNode.terminalOptions);
                    if (terminalNode != null)
                    {
                        __result = terminalNode;
                        return false;
                    }
                }
                __result = null;
                return false;
            }

            if (array.Length > 1 && array[0] == "switch")
            {
                int num = CheckForPlayerNameCommand(array[0], array[1]);
                if (num != -1)
                {
                    mcr.SwitchRadarTargetAndSync(num);
                    __result = __instance.terminalNodes.specialNodes[20];
                    return false;
                }
            }
            return true;
        }

        private static string RemovePunctuation(string s)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (char c in s)
            {
                if (!char.IsPunctuation(c))
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().ToLower();
        }

        private static TerminalNode ParseWordOverrideOptions(string playerWord, CompatibleNoun[] options)
        {
            for (int i = 0; i < options.Length; i++)
            {
                for (int num = playerWord.Length; num > 0; num--)
                {
                    if (options[i].noun.word.StartsWith(playerWord.Substring(0, num)))
                    {
                        return options[i].result;
                    }
                }
            }

            return null;
        }
        private static int CheckForPlayerNameCommand(string firstWord, string secondWord)
        {
            if (firstWord == "radar")
            {
                return -1;
            }

            if (secondWord.Length <= 2)
            {
                return -1;
            }

            Debug.Log("first word: " + firstWord + "; second word: " + secondWord);
            List<string> list = new List<string>();
            for (int i = 0; i < mcr.radarTargets.Count; i++)
            {
                list.Add(mcr.radarTargets[i].name);
                Debug.Log($"name {i}: {list[i]}");
            }

            secondWord = secondWord.ToLower();
            for (int j = 0; j < list.Count; j++)
            {
                string text = list[j].ToLower();
                if (text == secondWord)
                {
                    return j;
                }
            }

            Debug.Log($"Target names length: {list.Count}");
            for (int k = 0; k < list.Count; k++)
            {
                Debug.Log("A");
                string text = list[k].ToLower();
                Debug.Log($"Word #{k}: {text}; length: {text.Length}");
                for (int num = secondWord.Length; num > 2; num--)
                {
                    Debug.Log($"c: {num}");
                    Debug.Log(secondWord.Substring(0, num));
                    if (text.StartsWith(secondWord.Substring(0, num)))
                    {
                        return k;
                    }
                }
            }

            return -1;
        }
    }


}
