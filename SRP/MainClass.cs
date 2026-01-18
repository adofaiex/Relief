using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityModManagerNet;
using System.Runtime;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using static UnityModManagerNet.UnityModManager;
using System.Collections.Generic;
using TinyJson;
using UnityEngine.EventSystems;
using SRP.ADOFAI.Keyviewer;
using TMPro;
using SRP.UI;

// TODO: Rename this namespace to your mod's name.
namespace SRP
{
    /// <summary>
    /// The main class for the mod. Call other parts of your code from this
    /// class.
    /// </summary>
    public static class MainClass
    {
        public static bool IsEnabled { get; private set; }
        
        static List<scrKeyviewer> _keyviewers = new List<scrKeyviewer>();
        private static bool _needsRefresh = false;
        private static float _lastRefreshTime = 0f;
        private const float MIN_REFRESH_INTERVAL = 0.5f; // 500ms minimum between refreshes
        private static GameObject _detectionManagerGO;
        public static SRP.UI.Settings Settings { get; private set; }

        private static Queue<Action> _saveQueue = new Queue<Action>();
        private static float _lastSaveTime = 0f;
        private const float SAVE_COOLDOWN = 2f;

        public static void RequestSave(string path, string json) {
            _saveQueue.Enqueue(() => {
                try {
                    File.WriteAllText(path, json);
                    Logger.Log($"Saved config to {path}");
                } catch (Exception e) {
                    Logger.Error($"Failed to save config: {e.Message}");
                }
            });
        }
        
        public static SRP.ADOFAI.Planet.PlanetTweak PlanetTweak { get; private set; }

        public static void QueueRefresh()
        {
            _needsRefresh = true;
        }

        public static void CheckRefresh()
        {
            if (_needsRefresh && Time.unscaledTime - _lastRefreshTime > MIN_REFRESH_INTERVAL)
            {
                _needsRefresh = false;
                _lastRefreshTime = Time.unscaledTime;
                RefreshKeyViewers();
            }

            if (_saveQueue.Count > 0 && Time.unscaledTime - _lastSaveTime > SAVE_COOLDOWN) {
                _lastSaveTime = Time.unscaledTime;
                var saveAction = _saveQueue.Dequeue();
                saveAction.Invoke();
            }
        }
        
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        /// <summary>
        /// UMM's logger instance. Use this to write logs to the UMM settings
        /// window under the "Logs" tab.
        /// </summary>
        public static UnityModManager.ModEntry.ModLogger Logger { get; private set; }
        public static UnityModManager.ModEntry ModEntry { get; private set; }

        private static Harmony harmony;

        /// <summary>
        /// Perform any initial setup with the mod here.
        /// </summary>
        /// <param name="modEntry">UMM's mod entry for the mod.</param>
        internal static void Setup(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;
            Logger = modEntry.Logger;
            Settings = UnityModManager.ModSettings.Load<SRP.UI.Settings>(modEntry);
            Logger.Log($"Settings loaded. EnabledKVConfigs count: {Settings.EnabledKVConfigs?.Count ?? -1}");
            if (Settings.EnabledKVConfigs != null)
            {
                foreach (var config in Settings.EnabledKVConfigs)
                {
                    Logger.Log($"  - Loaded config: {config}");
                }
            }

            // Add hooks to UMM event methods
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = Options.OnGUI;
            modEntry.OnHideGUI = Options.OnHideGUI;
            modEntry.OnSaveGUI = (entry) => Settings.Save(entry);
        }

        /// <summary>
        /// Handler for toggling the mod on/off.
        /// </summary>
        /// <param name="modEntry">UMM's mod entry for the mod.</param>
        /// <param name="value">
        /// <c>true</c> if the mod is being toggled on, <c>false</c> if the mod
        /// is being toggled off.
        /// </param>
        /// <returns><c>true</c></returns>
        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            IsEnabled = value;
            if (value)
            {
                StartMod(modEntry);
            }
            else
            {
                StopMod(modEntry);
            }
            return true;
        }

        /// <summary>
        /// Start the mod up. You can create Unity GameObjects, patch methods,
        /// etc.
        /// </summary>
        /// <param name="modEntry">UMM's mod entry for the mod.</param>
        private static void StartMod(UnityModManager.ModEntry modEntry)
        {
            // Patch everything in this assembly
            harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Initialize Planet Tweak
            PlanetTweak = new SRP.ADOFAI.Planet.PlanetTweak();
            PlanetTweak.Settings = Settings.PlanetSettings;
            SRP.ADOFAI.Planet.PlanetPatches.Settings = Settings.PlanetSettings;
            if (IsEnabled) {
                PlanetTweak.OnEnable();
            }

            if (Settings.FirstTimeLaunch)
            {
                FirstTimePopup.Create();
            }
            
            // Create overlay
            if (Settings.EnableSRPBuildOverlay)
            {
                SRPBuildOverlay.Create();
            }

            // Create detection manager
            _detectionManagerGO = new GameObject("SRP_DetectionManager");
            _detectionManagerGO.AddComponent<DetectionManager>();

            // Initialize keyviewers
            RefreshKeyViewers();
        }

        public static void RefreshKeyViewers()
        {
            try
            {
                Logger.Log("Refreshing KeyViewers...");
                // Clear existing
                for (int i = _keyviewers.Count - 1; i >= 0; i--)
                {
                    var kv = _keyviewers[i];
                    if (kv != null)
                    {
                        try {
                            kv.SaveProfile();
                            UnityEngine.Object.Destroy(kv.gameObject);
                        } catch (Exception e) {
                            Logger.Error($"Error destroying KV: {e.Message}");
                        }
                    }
                }
                _keyviewers.Clear();

                if (!Settings.EnableKeyViewer) {
                    Logger.Log("KeyViewer is disabled in settings.");
                    return;
                }

                string kvDir = Path.Combine(AssemblyDirectory, "Keyviewer");
                if (!Directory.Exists(kvDir)) Directory.CreateDirectory(kvDir);

                var configFiles = Directory.GetFiles(kvDir, "*.json");
                Logger.Log($"Found {configFiles.Length} config files in {kvDir}");
                
                foreach (var configFile in configFiles)
                {
                    string configName = Path.GetFileName(configFile);
                    if (Settings.EnabledKVConfigs != null && Settings.EnabledKVConfigs.Any(x => x.Equals(configName, StringComparison.OrdinalIgnoreCase)))
                    {
                        try
                        {
                            if (!File.Exists(configFile)) continue;
                            
                            string json = File.ReadAllText(configFile);
                            if (string.IsNullOrEmpty(json)) continue;

                            KeyViewerProfile profile = json.FromJson<KeyViewerProfile>();
                            if (profile != null)
                            {
                                if (_keyviewers.Any(x => x != null && x.ConfigPath == configFile)) {
                                    Logger.Log($"KeyViewer for {configName} already exists, skipping creation.");
                                    continue;
                                }

                                Logger.Log($"Creating KeyViewer for: {configName}");
                                GameObject go = new GameObject("scrKeyviewer_" + configName);
                                UnityEngine.Object.DontDestroyOnLoad(go);
                                var kv = go.AddComponent<scrKeyviewer>();
                                kv.ConfigPath = configFile;
                                kv.Profile = profile; // This triggers UpdateKeys()
                                _keyviewers.Add(kv);
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Error($"Error loading KV config {configName}: {e.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Critical error in RefreshKeyViewers: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Stop the mod by cleaning up anything that you created in
        /// <see cref="StartMod(UnityModManager.ModEntry)"/>.
        /// </summary>
        /// <param name="modEntry">UMM's mod entry for the mod.</param>
        private static void StopMod(UnityModManager.ModEntry modEntry)
        {
            // Remove overlay
            SRPBuildOverlay.Remove();

            if (_detectionManagerGO != null)
            {
                UnityEngine.Object.Destroy(_detectionManagerGO);
                _detectionManagerGO = null;
            }

            foreach (var kv in _keyviewers)
            {
                if (kv != null) {
                    try {
                        kv.SaveProfile();
                        UnityEngine.Object.Destroy(kv.gameObject);
                    } catch { }
                }
            }
            _keyviewers.Clear();

            // Unpatch everything
            if (PlanetTweak != null) {
                PlanetTweak.OnDisable();
                PlanetTweak = null;
            }

            if (harmony != null) {
                harmony.UnpatchAll(modEntry.Info.Id);
                harmony = null;
            }
        }
        
    }
}
