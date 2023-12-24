using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeparateCameras
{
    [BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private const string MOD_GUID = "CptKumquat.SeparateCameras";
        private const string MOD_NAME = "Separate Cameras";
        private const string MOD_VERSION = "0.1.0";

        private readonly Harmony harmony = new Harmony(MOD_GUID);

        public static Plugin INSTANCE;

        public ManualLogSource mls;

        private ManualCameraRenderer terminalMap;

        public static void LogInfo(string message)
        {

            INSTANCE.mls.LogInfo(message);
        }

        void Awake()
        {
            if (INSTANCE == null)
            {
                INSTANCE = this;
            }
            mls = BepInEx.Logging.Logger.CreateLogSource(MOD_GUID);
            mls.LogInfo("Separate Cameras has loaded!");
            harmony.PatchAll(typeof(Plugin));
            harmony.PatchAll(typeof(Patch.TerminalPatch));
            harmony.PatchAll(typeof(Patch.MCRPatch));
        }
    }
}
