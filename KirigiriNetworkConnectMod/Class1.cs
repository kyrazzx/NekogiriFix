using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Steamworks;
using Newtonsoft.Json;
using Steamworks.Data;

namespace NetworkConnectMod
{
    [BepInPlugin("kirigiri.repo.networkconnect", "NetworkConnect Mod By Kirigiri", "1.0.0.0")]
    public class NetworkConnectMod : BaseUnityPlugin
    {
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

        // Custom method to initialize Steam with a dynamic App ID from the INI file
        private void CustomSteamAppID()
        {
            string configFilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Kirigiri.ini");
            uint appId = 480U; // Default value for AppId if not found

            try
            {
                if (File.Exists(configFilePath))
                {
                    // Read all lines from the INI file
                    var settings = File.ReadAllLines(configFilePath)
                                       .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith(";"))
                                       .Select(line => line.Split('='))
                                       .Where(parts => parts.Length == 2)
                                       .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

                    if (settings.ContainsKey("SteamAppId"))
                    {
                        // Try to parse the App ID from the file, if available
                        if (uint.TryParse(settings["SteamAppId"], out uint parsedAppId))
                        {
                            appId = parsedAppId;
                        }
                        else
                        {
                            Logger.LogWarning("Invalid SteamAppId in the INI file, defaulting to 480.");
                        }
                    }
                    else
                    {
                        Logger.LogWarning("SteamAppId not found in the INI file, defaulting to 480.");
                    }
                }
                else
                {
                    Logger.LogWarning($"Settings file not found at {configFilePath}. Using default App ID 480.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error reading SteamAppId from INI: {ex.Message}. Defaulting to App ID 480.");
            }

            // Initialize Steam client with the dynamic AppId
            SteamClient.Init(appId, true);
            Logger.LogInfo($"Steam client initialized with AppId {appId}");
        }

        public void CustomAuth()
        {
            string configFilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Kirigiri.ini");
            string authSetting = "None"; // Default value if not found or invalid

            try
            {
                if (File.Exists(configFilePath))
                {
                    // Read all lines from the INI file
                    var settings = File.ReadAllLines(configFilePath)
                                       .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith(";"))
                                       .Select(line => line.Split('='))
                                       .Where(parts => parts.Length == 2)
                                       .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

                    if (settings.ContainsKey("Auth"))
                    {
                        authSetting = settings["Auth"];
                    }
                    else
                    {
                        Logger.LogWarning("Auth setting not found in the INI file, defaulting to 'None'.");
                    }
                }
                else
                {
                    Logger.LogWarning($"Settings file not found at {configFilePath}. Using default Auth value 'None'.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error reading Auth setting from INI: {ex.Message}. Defaulting to 'None'.");
            }

            // Map the string to the corresponding CustomAuthenticationType
            PhotonNetwork.AuthValues = new AuthenticationValues();
            PhotonNetwork.AuthValues.UserId = SteamClient.SteamId.ToString();

            CustomAuthenticationType authType = CustomAuthenticationType.None; // Default to None

            // Check the Auth setting and set the corresponding authentication type
            switch (authSetting.ToLower())
            {
                case "custom":
                    authType = CustomAuthenticationType.Custom;
                    break;
                case "steam":
                    authType = CustomAuthenticationType.Steam;
                    break;
                case "facebook":
                    authType = CustomAuthenticationType.Facebook;
                    break;
                case "oculus":
                    authType = CustomAuthenticationType.Oculus;
                    break;
                case "playstation4":
                    authType = CustomAuthenticationType.PlayStation4;
                    break;
                case "xbox":
                    authType = CustomAuthenticationType.Xbox;
                    break;
                case "viveport":
                    authType = CustomAuthenticationType.Viveport;
                    break;
                case "nintendoswitch":
                    authType = CustomAuthenticationType.NintendoSwitch;
                    break;
                case "playstation5":
                    authType = CustomAuthenticationType.PlayStation5;
                    break;
                case "epic":
                    authType = CustomAuthenticationType.Epic;
                    break;
                case "facebookgaming":
                    authType = CustomAuthenticationType.FacebookGaming;
                    break;
                case "none":
                default:
                    authType = CustomAuthenticationType.None;
                    break;
            }

            PhotonNetwork.AuthValues.AuthType = authType;

            // Add the Auth parameter (e.g., the Steam ticket)
            string value = this.GetSteamAuthTicket(out this.steamAuthTicket);
            PhotonNetwork.AuthValues.AddAuthParameter("ticket", value);

            Logger.LogInfo($"Patched Auth to {PhotonNetwork.AuthValues.AuthType}!");
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

        [HarmonyPatch(typeof(SteamManager), "Awake")]
        public class SteamManagerPatch
        {
            // Prefix is called before the original method is called
            // Suffix is called after the original method is executed

            [HarmonyPrefix]
            public static bool Prefix()
            {
                // Instead of the original Start method, call CustomStart
                Debug.Log("Patching SteamManager.Awake method.");
                new NetworkConnectMod().CustomSteamAppID();

                // Return false to skip the original Start method
                return true; // Skipping the original Start method
            }
        }

        [HarmonyPatch(typeof(SteamManager), "SendSteamAuthTicket")]
        public class SendSteamAuthTicketPatch
        {
            // Prefix is called before the original method is called
            // Suffix is called after the original method is executed

            [HarmonyPrefix]
            public static bool Prefix()
            {
                // Instead of the original Start method, call CustomStart
                Debug.Log("Patching SteamManager.SendSteamAuthTicket method.");
                new NetworkConnectMod().CustomAuth();

                // Return false to skip the original Start method
                return false; // Skipping the original Start method
            }
        }

        private string GetSteamAuthTicket(out AuthTicket ticket)
        {
            Debug.Log("Getting Steam Auth Ticket...");
            ticket = SteamUser.GetAuthSessionTicket();
            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
            for (int i = 0; i < ticket.Data.Length; i++)
            {
                stringBuilder.AppendFormat("{0:x2}", ticket.Data[i]);
            }
            return stringBuilder.ToString();
        }

        internal AuthTicket steamAuthTicket;

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
