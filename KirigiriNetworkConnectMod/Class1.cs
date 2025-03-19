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
                string configFilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "photon_config.json");

                if (File.Exists(configFilePath))
                {
                    // Deserialize settings from the json config
                    PhotonAppSettings photonAppSettings = JsonConvert.DeserializeObject<PhotonAppSettings>(File.ReadAllText(configFilePath));
                    serverSettings.AppSettings.AppIdRealtime = photonAppSettings.AppIdRealtime;
                    serverSettings.AppSettings.AppIdChat = photonAppSettings.AppIdChat;
                    serverSettings.AppSettings.AppIdVoice = photonAppSettings.AppIdVoice;
                    serverSettings.AppSettings.AppIdFusion = photonAppSettings.AppIdFusion;

                    Logger.LogInfo($"Address read are {photonAppSettings.AppIdRealtime} & {photonAppSettings.AppIdVoice}");

                    if (!string.IsNullOrEmpty(photonAppSettings.FixedRegion))
                    {
                        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = photonAppSettings.FixedRegion;
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
