using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SRP.ADOFAI.Keyviewer;
using TinyJson;
using static UnityModManagerNet.UnityModManager.Param;
using UnityEngine;
using UnityModManagerNet;

namespace SRP.UI
{
    public enum ExampleEnum
    {
        OptionA,
        OptionB,
        OptionC
    }

    public class Settings : UnityModManager.ModSettings
    {
        public bool FirstTimeLaunch = true;
        public bool EnableKeyViewer = true;
        public List<string> EnabledKVConfigs = new List<string>();
        public bool EnableSRPBuildOverlay = true;

        public SRP.ADOFAI.Planet.PlanetSettings PlanetSettings = new SRP.ADOFAI.Planet.PlanetSettings();

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        public void ResetToDefaults()
        {
            EnableKeyViewer = true;
            EnabledKVConfigs.Clear();
            EnableSRPBuildOverlay = true;
            PlanetSettings = new SRP.ADOFAI.Planet.PlanetSettings();
        }
    }

    internal class Options
    {
        private static Settings _settings => MainClass.Settings;
        private static DetectionMode _currentDetectionMode = DetectionMode.None;
        private static string _editingConfig = null;
        private static KeyViewerProfile _editingProfile = null;
        
        // Cache for config files to avoid IO in OnGUI
        private static string[] _cachedConfigFiles = null;
        private static float _lastConfigRefreshTime = 0f;
        private const float CONFIG_REFRESH_INTERVAL = 1f; // 1 second

        public static void OnHideGUI(UnityModManager.ModEntry modEntry)
        {
            if (DetectionManager.Instance != null && DetectionManager.Instance.Mode != DetectionMode.None)
            {
                DetectionManager.Instance.Mode = DetectionMode.None;
                SaveEditingProfile();
            }
            _editingProfile = null;
            _editingConfig = null;
        }

        private static void SaveEditingProfile()
        {
            if (_editingProfile != null && !string.IsNullOrEmpty(_editingConfig))
            {
                try {
                    string kvDir = Path.Combine(MainClass.AssemblyDirectory, "Keyviewer");
                    if (!Directory.Exists(kvDir)) Directory.CreateDirectory(kvDir);
                    string path = Path.Combine(kvDir, _editingConfig);
                    string json = _editingProfile.ToJson();
                    MainClass.RequestSave(path, json);
                    MainClass.QueueRefresh();
                } catch (Exception e) {
                    MainClass.Logger.Error($"保存编辑配置文件失败: {e.Message}");
                }
            }
        }

        public static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            // Refresh config cache periodically
            if (_cachedConfigFiles == null || Time.unscaledTime - _lastConfigRefreshTime > CONFIG_REFRESH_INTERVAL)
            {
                RefreshConfigCache();
            }

            GUILayout.BeginVertical(PinkUI.Box);
            
            GUILayout.Label("<b>SRP Mod 综合设置</b>", PinkUI.Label);
            GUILayout.Space(10);
            
            bool overlayEnabled = GUILayout.Toggle(_settings.EnableSRPBuildOverlay, "启用右上角版本水印 (SRP Build)", PinkUI.Toggle);
            if (overlayEnabled != _settings.EnableSRPBuildOverlay)
            {
                _settings.EnableSRPBuildOverlay = overlayEnabled;
                if (overlayEnabled) SRPBuildOverlay.Create();
                else SRPBuildOverlay.Remove();
            }

            GUILayout.Space(15);
            GUILayout.Label("<b>KeyViewer (KV) 核心功能</b>", PinkUI.Label);
            
            bool kvEnabled = GUILayout.Toggle(_settings.EnableKeyViewer, "启用按键显示器 (KeyViewer)", PinkUI.Toggle);
            if (kvEnabled != _settings.EnableKeyViewer)
            {
                _settings.EnableKeyViewer = kvEnabled;
                MainClass.QueueRefresh();
            }

            if (_settings.EnableKeyViewer)
            {
                GUILayout.BeginVertical(PinkUI.Box);
                GUILayout.Label("<b>已启用的配置文件</b>", PinkUI.Label);
                
                string kvDir = Path.Combine(MainClass.AssemblyDirectory, "Keyviewer");
                if (!Directory.Exists(kvDir)) Directory.CreateDirectory(kvDir);

                if (_cachedConfigFiles != null)
                {
                    foreach (var configFile in _cachedConfigFiles)
                    {
                        string fileName = Path.GetFileName(configFile);
                        bool isEnabled = _settings.EnabledKVConfigs.Any(x => x.Equals(fileName, StringComparison.OrdinalIgnoreCase));
                        
                        GUILayout.BeginHorizontal();
                        bool toggle = GUILayout.Toggle(isEnabled, fileName, PinkUI.Toggle);
                        
                        if (toggle != isEnabled)
                        {
                            if (toggle)
                            {
                                if (!_settings.EnabledKVConfigs.Contains(fileName))
                                {
                                    _settings.EnabledKVConfigs.Add(fileName);
                                }
                            }
                            else
                            {
                                _settings.EnabledKVConfigs.RemoveAll(x => x == fileName);
                            }
                            MainClass.QueueRefresh();
                        }

                        if (GUILayout.Button("编辑", PinkUI.Button, GUILayout.Width(60)))
                        {
                            _editingConfig = fileName;
                            try {
                                string json = File.ReadAllText(configFile);
                                _editingProfile = json.FromJson<KeyViewerProfile>();
                                if (DetectionManager.Instance != null) {
                                    DetectionManager.Instance.TargetProfile = _editingProfile;
                                    DetectionManager.Instance.Mode = DetectionMode.None;
                                }
                            } catch (Exception e) {
                                MainClass.Logger.Error($"读取配置文件失败: {e.Message}");
                            }
                        }

                        if (GUILayout.Button("导出", PinkUI.Button, GUILayout.Width(60)))
                        {
                            MainClass.QueueRefresh();
                        }
                        
                        GUILayout.EndHorizontal();
                    }
                }
                
                if (GUILayout.Button("创建新配置文件", PinkUI.Button, GUILayout.Width(200)))
                {
                    try {
                        string newName = "NewProfile_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
                        string newPath = Path.Combine(kvDir, newName);
                        var newProfile = new KeyViewerProfile() { Name = "New Profile" };
                        string json = newProfile.ToJson();
                        File.WriteAllText(newPath, json);
                        RefreshConfigCache();
                        MainClass.QueueRefresh();
                    } catch (Exception e) {
                        MainClass.Logger.Error($"创建配置文件失败: {e.Message}");
                    }
                }

                if (_editingProfile != null)
                {
                    GUILayout.BeginVertical(PinkUI.Box);
                    GUILayout.Label($"<b>正在编辑: {_editingConfig}</b>", PinkUI.Label);
                    
                    _editingProfile.Name = GUILayout.TextField(_editingProfile.Name, PinkUI.TextField);

                    GUILayout.Space(5);
                    _editingProfile.ShowKeyPressTotal = GUILayout.Toggle(_editingProfile.ShowKeyPressTotal, "显示按键次数和统计", PinkUI.Toggle);
                    _editingProfile.AnimateKeys = GUILayout.Toggle(_editingProfile.AnimateKeys, "启用按键动画", PinkUI.Toggle);
                    
                    GUILayout.Space(5);
                    GUILayout.Label("全局颜色 (默认):", PinkUI.Label);
                    _editingProfile.PressedBackgroundColor = ColorPicker("按下背景", _editingProfile.PressedBackgroundColor);
                    _editingProfile.ReleasedBackgroundColor = ColorPicker("未按下背景", _editingProfile.ReleasedBackgroundColor);
                    _editingProfile.PressedTextColor = ColorPicker("按下文字", _editingProfile.PressedTextColor);
                    _editingProfile.ReleasedTextColor = ColorPicker("未按下文字", _editingProfile.ReleasedTextColor);

                    GUILayout.Space(10);
                    GUILayout.Label("按键列表与独立配置:", PinkUI.Label);
                    
                    string keysStr = string.Join(", ", _editingProfile.ActiveKeys);
                    GUILayout.Label("当前按键: " + keysStr, PinkUI.Label);
                    
                    foreach (var code in _editingProfile.ActiveKeys.ToList()) {
                        var config = _editingProfile.KeyConfigs.FirstOrDefault(x => x.Code == code);
                        if (config == null) {
                            config = new KeyConfig(code);
                            _editingProfile.KeyConfigs.Add(config);
                        }

                        GUILayout.BeginVertical(PinkUI.Box);
                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"按键: <b>{code}</b>", PinkUI.Label);
                        if (GUILayout.Button("删除", PinkUI.Button, GUILayout.Width(60))) {
                            _editingProfile.ActiveKeys.Remove(code);
                            _editingProfile.KeyConfigs.Remove(config);
                            continue;
                        }
                        GUILayout.EndHorizontal();

                        config.Name = GUILayout.TextField(config.Name ?? code.ToString(), PinkUI.TextField);
                        config.EnableBorder = GUILayout.Toggle(config.EnableBorder, "启用边框", PinkUI.Toggle);
                        if (config.EnableBorder) {
                            config.BorderWidth = NamedSliderPink("边框宽度", config.BorderWidth, 0, 10);
                            config.PressedBorderColor = ColorPicker("按下边框颜色", config.PressedBorderColor ?? _editingProfile.PressedOutlineColor);
                            config.ReleasedBorderColor = ColorPicker("未按下边框颜色", config.ReleasedBorderColor ?? _editingProfile.ReleasedOutlineColor);
                        }

                        config.EnableRainKey = GUILayout.Toggle(config.EnableRainKey, "启用 RainingKey", PinkUI.Toggle);
                        if (config.EnableRainKey) {
                            config.RainKeyColor = ColorPicker("RainKey 颜色", config.RainKeyColor);
                            config.RainKeySpeed = NamedSliderPink("飞行速度", config.RainKeySpeed, 10, 1000);
                            config.RainKeyMaxHeight = NamedSliderPink("最大高度", config.RainKeyMaxHeight, 100, 2000);
                        }
                        
                        GUILayout.EndVertical();
                    }
                    
                    GUILayout.BeginHorizontal();
                    if (DetectionManager.Instance != null) {
                        if (DetectionManager.Instance.Mode == DetectionMode.Add) {
                            if (GUILayout.Button("停止侦测", PinkUI.Button)) {
                                try {
                                    DetectionManager.Instance.Mode = DetectionMode.None;
                                    SaveEditingProfile();
                                } catch (Exception e) {
                                    MainClass.Logger.Error($"停止侦测失败: {e.Message}");
                                }
                            }
                            GUILayout.Label("<color=green>侦测中 (添加)...</color>", PinkUI.Label);
                        } else if (DetectionManager.Instance.Mode == DetectionMode.Delete) {
                            if (GUILayout.Button("停止侦测", PinkUI.Button)) {
                                try {
                                    DetectionManager.Instance.Mode = DetectionMode.None;
                                    SaveEditingProfile();
                                } catch (Exception e) {
                                    MainClass.Logger.Error($"停止侦测失败: {e.Message}");
                                }
                            }
                            GUILayout.Label("<color=red>侦测中 (删除)...</color>", PinkUI.Label);
                        } else {
                            if (GUILayout.Button("添加按键", PinkUI.Button)) DetectionManager.Instance.Mode = DetectionMode.Add;
                            if (GUILayout.Button("移除按键", PinkUI.Button)) DetectionManager.Instance.Mode = DetectionMode.Delete;
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(5);
                    _editingProfile.KeyViewerSize = NamedSliderPink("尺寸", _editingProfile.KeyViewerSize, 10, 200);
                    _editingProfile.KeyViewerXPos = NamedSliderPink("X 位置", _editingProfile.KeyViewerXPos, 0, 1);
                    _editingProfile.KeyViewerYPos = NamedSliderPink("Y 位置", _editingProfile.KeyViewerYPos, 0, 1);
                    
                    if (GUILayout.Button("保存编辑并应用", PinkUI.Button))
                    {
                        SaveEditingProfile();
                    }
                    GUILayout.EndVertical();
                }
                
                if (GUILayout.Button("刷新配置文件列表", PinkUI.Button, GUILayout.Width(200)))
                {
                    RefreshConfigCache();
                    MainClass.QueueRefresh();
                }
                GUILayout.EndVertical();
            }

            GUILayout.Space(15);
            GUILayout.Label("<b>星球修改 (Planet Modification)</b>", PinkUI.Label);
            if (MainClass.PlanetTweak != null)
            {
                GUILayout.BeginVertical(PinkUI.Box);
                MainClass.PlanetTweak.OnSettingsGUI();
                GUILayout.EndVertical();
            }

            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("保存所有设置", PinkUI.Button, GUILayout.Width(150), GUILayout.Height(30)))
            {
                _settings.Save(modEntry);
                MainClass.QueueRefresh();
            }
            if (GUILayout.Button("重置默认", PinkUI.Button, GUILayout.Width(100), GUILayout.Height(30)))
            {
                _settings.ResetToDefaults();
                _settings.Save(modEntry);
                MainClass.QueueRefresh();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private static void RefreshConfigCache()
        {
            try {
                string kvDir = Path.Combine(MainClass.AssemblyDirectory, "Keyviewer");
                if (!Directory.Exists(kvDir)) Directory.CreateDirectory(kvDir);
                _cachedConfigFiles = Directory.GetFiles(kvDir, "*.json");
                _lastConfigRefreshTime = Time.unscaledTime;
            } catch (Exception e) {
                MainClass.Logger.Error($"刷新配置文件缓存失败: {e.Message}");
            }
        }

        private static float NamedSliderPink(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, PinkUI.Label, GUILayout.Width(100));
            float newVal = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(200));
            GUILayout.Label(newVal.ToString("F2"), PinkUI.Label, GUILayout.Width(50));
            GUILayout.EndHorizontal();
            return newVal;
        }

        private static Color ColorPicker(string label, Color color)
        {
            GUILayout.BeginVertical(PinkUI.Box);
            GUILayout.Label(label, PinkUI.Label);
            GUILayout.BeginHorizontal();
            float r = ColorSlider("R", color.r);
            float g = ColorSlider("G", color.g);
            float b = ColorSlider("B", color.b);
            float a = ColorSlider("A", color.a);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            return new Color(r, g, b, a);
        }

        private static float ColorSlider(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, PinkUI.Label, GUILayout.Width(20));
            float newVal = GUILayout.HorizontalSlider(value, 0, 1, GUILayout.Width(60));
            GUILayout.EndHorizontal();
            return newVal;
        }
    }
}
