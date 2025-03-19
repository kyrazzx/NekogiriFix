using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Steamworks;
using Newtonsoft.Json;

namespace NetworkConnectMod
{
    [BepInPlugin("kirigiri.repo.networkconnect", "NetworkConnect Mod By Kirigiri", "1.0.0.0")]
    public class NetworkConnectMod : BaseUnityPlugin
    {
        // If punVoiceClient is a prefab or an existing object, assign it in the Unity Inspector


        private void Awake()
        {
            // Set up plugin logging
            Logger.LogInfo("NetworkConnectMod has loaded!");

            // Create a Harmony instance and apply the patch
            var harmony = new Harmony("kirigiri.repo.networkconnect");
            harmony.PatchAll();  // Automatically patch all methods that have the PatchAttribute

            // Optionally log that the patch has been applied
            Logger.LogInfo("Harmony patch applied to the Start method of NetworkConnect.");
        }

        // The custom method to replace the original Start method
        private void CustomStart()
        {
            Logger.LogInfo("Custom Start method executed!");

            // Load server settings from a configuration file
            ServerSettings serverSettings = Resources.Load<ServerSettings>("PhotonServerSettings");

            try
            {
                // Define the path to your INI file
                string configFilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Kirigiri.ini");

                if (File.Exists(configFilePath))
                {
                    // Read all lines from the INI file
                    var settings = File.ReadAllLines(configFilePath)
                                       .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith(";"))
                                       .Select(line => line.Split('='))
                                       .Where(parts => parts.Length == 2)
                                       .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

                    // Assign values from the INI file
                    if (settings.ContainsKey("AppIdRealtime"))
                        serverSettings.AppSettings.AppIdRealtime = settings["AppIdRealtime"];

                    if (settings.ContainsKey("AppIdChat"))
                        serverSettings.AppSettings.AppIdChat = settings["AppIdChat"];

                    if (settings.ContainsKey("AppIdVoice"))
                        serverSettings.AppSettings.AppIdVoice = settings["AppIdVoice"];

                    if (settings.ContainsKey("AppIdFusion"))
                        serverSettings.AppSettings.AppIdFusion = settings["AppIdFusion"];

                    Logger.LogInfo($"Address read are {serverSettings.AppSettings.AppIdRealtime} & {serverSettings.AppSettings.AppIdVoice}");

                    // Handle FixedRegion setting
                    if (settings.ContainsKey("FixedRegion"))
                    {
                        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = settings["FixedRegion"];
                    }
                    else
                    {
                        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "";
                    }

                    Logger.LogInfo($"Photon settings loaded from {configFilePath}");
                }
                else
                {
                    PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "";
                    Logger.LogWarning($"Settings file not found at {configFilePath}. Using default values.");
                }
            }
            catch (System.Exception ex)
            {
                PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "";
                Logger.LogError($"Error loading Photon settings: {ex.Message}. Using default values.");
            }
        }

        // Patch the original Start method with the custom one
        [HarmonyPatch(typeof(NetworkConnect), "Start")]
        public class NetworkConnectPatch
        {
            // Prefix is called before the original method is called
            // Suffix is called after the original method is executed

            [HarmonyPrefix]
            public static bool Prefix()
            {
                // Instead of the original Start method, call CustomStart
                Debug.Log("Patching NetworkConnect.Start method.");
                new NetworkConnectMod().CustomStart();

                // Return false to skip the original Start method
                return true; // Skipping the original Start method
            }
        }

        // The PhotonAppSettings class should be outside CustomStart, at the class level
        [Serializable]
        public class PhotonAppSettings
        {
            public string AppIdRealtime = "";
            public string AppIdChat = "";
            public string AppIdVoice = "";
            public string AppIdFusion = "";
            public string FixedRegion = "";
        }
    }
}
