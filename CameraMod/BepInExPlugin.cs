using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CameraMod
{
    [BepInPlugin("aedenthorn.CameraMod", "Camera Mod", "0.1.0")]
    public class BepInExPlugin : BasePlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> useScrollWheel;
        public static ConfigEntry<float> incrementFast;
        public static ConfigEntry<float> incrementNormal;
        public static ConfigEntry<string> modKeyNormal;
        public static ConfigEntry<string> modKeyFast;
        public static ConfigEntry<string> keyIncrease;
        public static ConfigEntry<string> keyDecrease;

        public static Dictionary<string, float> cameraManagerFOVs = new Dictionary<string, float>();


        public static BepInExPlugin context;
        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public override void Load()
        {

            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");

            useScrollWheel = Config.Bind<bool>("Options", "UseScrollWheel", true, "Use scroll wheel to adjust FOV");
            incrementFast = Config.Bind<float>("Options", "IncrementFast", 5, "Fast increment speed.");
            incrementNormal = Config.Bind<float>("Options", "IncrementNormal", 1, "Normal increment speed.");
            modKeyNormal = Config.Bind<string>("Options", "ModKeyNormal", "left ctrl", "Modifier key to increment at normal speed.");
            modKeyFast = Config.Bind<string>("Options", "ModKeyFast", "left alt", "Modifier key to increment at fast speed.");
            keyIncrease = Config.Bind<string>("Options", "KeyIncrease", "", "Key to increase FOV.");
            keyDecrease = Config.Bind<string>("Options", "KeyDecrease", "", "Key to decrease FOV.");

            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(PlayerCameras), nameof(PlayerCameras.Update))]
        static class CameraManager_Update_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value)
                    return;
                string name = SableGameManager.PersistentObjects.MainCamera.name;
                if (!cameraManagerFOVs.ContainsKey(name))
                    cameraManagerFOVs[name] = SableGameManager.PersistentObjects.MainCamera.fieldOfView;
                if (
                    (useScrollWheel.Value && Input.mouseScrollDelta.y != 0 && (AedenthornUtils.CheckKeyHeld(modKeyNormal.Value) || AedenthornUtils.CheckKeyHeld(modKeyFast.Value))) ||
                    ((AedenthornUtils.CheckKeyDown(keyIncrease.Value) || AedenthornUtils.CheckKeyDown(keyDecrease.Value)) && (AedenthornUtils.CheckKeyHeld(modKeyNormal.Value, false) || AedenthornUtils.CheckKeyHeld(modKeyFast.Value, false)))
                )
                {
                    float change = AedenthornUtils.CheckKeyHeld(modKeyFast.Value) ? incrementFast.Value : incrementNormal.Value;

                    if (Input.mouseScrollDelta.y > 0)
                        cameraManagerFOVs[name] -= change;
                    else if (Input.mouseScrollDelta.y < 0)
                        cameraManagerFOVs[name] += change;
                    else if (AedenthornUtils.CheckKeyDown(keyIncrease.Value))
                        cameraManagerFOVs[name] += change;
                    else if (AedenthornUtils.CheckKeyDown(keyDecrease.Value))
                        cameraManagerFOVs[name] -= change;

                    cameraManagerFOVs[name] = Mathf.Clamp(cameraManagerFOVs[name], 1, 180);

                    Dbgl($"camera {name} field of view {cameraManagerFOVs[name]}");
                }
                if (cameraManagerFOVs.ContainsKey(name))
                    SableGameManager.PersistentObjects.MainCamera.fieldOfView = cameraManagerFOVs[name];

            }

        }
    }
}
