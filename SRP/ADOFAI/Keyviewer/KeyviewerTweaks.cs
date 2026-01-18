using System;
using System.Collections.Generic;
using System.IO;
using SRP.ADOFAI.Keyviewer.Core;
using SRP.ADOFAI.Keyviewer.Core.Attributes;
using SRP.UI;
using UnityEngine;
using TinyJson;

namespace SRP.ADOFAI.Keyviewer;

/// <summary>
/// A tweak for showing which keys are being pressed.
/// </summary>
[RegisterTweak(
    id: "key_viewer",
    settingsType: typeof(KeyViewerSettings),
    patchesType: typeof(KeyViewerPatches))]
public class KeyViewerTweak : Tweak
{
    public override string Name => "Key Viewer";
    public override string Description => "Shows key presses on screen";

    /// <summary>
    /// Keys that should not be listened to.
    /// </summary>
    private static readonly ISet<KeyCode> SKIPPED_KEYS = new HashSet<KeyCode>() {
        KeyCode.Mouse0,
        KeyCode.Mouse1,
        KeyCode.Mouse2,
        KeyCode.Mouse3,
        KeyCode.Mouse4,
        KeyCode.Mouse5,
        KeyCode.Mouse6,
        KeyCode.Escape,
    };

    [SyncTweakSettings]
    private KeyViewerSettings Settings { get; set; }

    [SyncTweakSettings]
    private KeyLimiterSettings LimiterSettings { get; set; }

    private KeyViewerProfile CurrentProfile { get => Settings.CurrentProfile; }

    private Dictionary<KeyCode, bool> keyState;
    private scrKeyviewer _scrKeyviewer;

    /// <inheritdoc/>
    public override void OnHideGUI() {
        Settings.CurrentDetectionMode = DetectionMode.None;
    }

    /// <inheritdoc/>
    public override void OnSettingsGUI() {
        GUILayout.BeginVertical(PinkUI.Box);
        
        GUILayout.Label("<b>KeyViewer 配置文件管理</b>", PinkUI.Label);
        DrawProfileSettingsGUI();
        
        GUILayout.Space(12f);
        MoreGUILayout.HorizontalLine(1f, 400f);
        GUILayout.Space(8f);
        
        GUILayout.Label("<b>按键侦测与设置</b>", PinkUI.Label);
        DrawKeyRegisterSettingsGUI();
        
        GUILayout.Space(8f);
        
        GUILayout.Label("<b>视觉效果设置</b>", PinkUI.Label);
        DrawKeyViewerSettingsGUI();
        
        GUILayout.EndVertical();
    }

    private void DrawProfileSettingsGUI() {
        GUILayout.Space(4f);

        // New, Duplicate, Delete, Export buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(TweakStrings.Get(TranslationKeys.KeyViewer.NEW), PinkUI.Button)) {
            Settings.Profiles.Add(new KeyViewerProfile());
            Settings.ProfileIndex = Settings.Profiles.Count - 1;
            Settings.CurrentProfile.Name += "Profile " + Settings.Profiles.Count;
            _scrKeyviewer.Profile = Settings.CurrentProfile;
        }
        if (GUILayout.Button(TweakStrings.Get(TranslationKeys.KeyViewer.DUPLICATE), PinkUI.Button)) {
            Settings.Profiles.Add(Settings.CurrentProfile.Copy());
            Settings.ProfileIndex = Settings.Profiles.Count - 1;
            Settings.CurrentProfile.Name += " Copy";
            _scrKeyviewer.Profile = Settings.CurrentProfile;
        }

        if (Settings.Profiles.Count > 1
            && GUILayout.Button(TweakStrings.Get(TranslationKeys.KeyViewer.DELETE), PinkUI.Button)) {
            Settings.Profiles.RemoveAt(Settings.ProfileIndex);
            Settings.ProfileIndex =
                Math.Min(Settings.ProfileIndex, Settings.Profiles.Count - 1);
            _scrKeyviewer.Profile = Settings.CurrentProfile;
        }

        if (GUILayout.Button("导出配置", PinkUI.Button)) {
            ExportProfile();
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(4f);

        // Profile name
        GUILayout.BeginHorizontal();
        GUILayout.Label(TweakStrings.Get(TranslationKeys.KeyViewer.PROFILE_NAME), PinkUI.Label, GUILayout.Width(100));
        Settings.CurrentProfile.Name = GUILayout.TextField(Settings.CurrentProfile.Name, PinkUI.TextField, GUILayout.Width(300));
        GUILayout.EndHorizontal();

        // Profile list
        GUILayout.Label(TweakStrings.Get(TranslationKeys.KeyViewer.PROFILES), PinkUI.Label);
        int selected = Settings.ProfileIndex;
        
        string[] profileNames = new string[Settings.Profiles.Count];
        for (int i = 0; i < Settings.Profiles.Count; i++) profileNames[i] = Settings.Profiles[i].Name;
        
        int newSelected = GUILayout.SelectionGrid(selected, profileNames, 4, PinkUI.Button);
        if (newSelected != selected) {
            Settings.ProfileIndex = newSelected;
            _scrKeyviewer.Profile = Settings.CurrentProfile;
        }
    }

    private void ExportProfile() {
        try {
            if (CurrentProfile == null) return;

            string kvDir = Path.Combine(MainClass.AssemblyDirectory, "Keyviewer");
            if (!Directory.Exists(kvDir)) Directory.CreateDirectory(kvDir);
            
            string name = CurrentProfile.Name ?? "Unnamed";
            string safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            string path = Path.Combine(kvDir, safeName + ".json");
            
            MainClass.Logger.Log($"正在导出配置文件: {path}");
            
            string json = CurrentProfile.ToJson();
            if (string.IsNullOrEmpty(json)) {
                MainClass.Logger.Error("生成的 JSON 为空！");
                return;
            }

            File.WriteAllText(path, json);
            MainClass.Logger.Log("导出成功。");
            
            MainClass.QueueRefresh();
        } catch (Exception e) {
            MainClass.Logger.Error($"导出配置文件失败: {e.Message}\n{e.StackTrace}");
        }
    }

    private void DrawKeyRegisterSettingsGUI() {
        // List of registered keys
        GUILayout.Label(TweakStrings.Get(TranslationKeys.KeyViewer.REGISTERED_KEYS), PinkUI.Label);
        GUILayout.BeginHorizontal(PinkUI.Box);
        
        string keysStr = string.Join(", ", CurrentProfile.ActiveKeys);
        if (string.IsNullOrEmpty(keysStr)) keysStr = "<i>无已注册按键</i>";
        GUILayout.Label(keysStr, new GUIStyle(PinkUI.Label) { fontStyle = FontStyle.Normal, wordWrap = true });
        
        GUILayout.EndHorizontal();
        GUILayout.Space(12f);

        // Detection buttons
        GUILayout.BeginHorizontal();
        
        if (Settings.CurrentDetectionMode == DetectionMode.Add) {
            if (GUILayout.Button("停止侦测", PinkUI.Button)) {
                Settings.CurrentDetectionMode = DetectionMode.None;
                ExportProfile(); // Auto save on stop
            }
            GUILayout.Label("<color=green>正在侦测按键并添加...</color>", PinkUI.Label);
        } else if (Settings.CurrentDetectionMode == DetectionMode.Delete) {
            if (GUILayout.Button("停止侦测", PinkUI.Button)) {
                Settings.CurrentDetectionMode = DetectionMode.None;
                ExportProfile(); // Auto save on stop
            }
            GUILayout.Label("<color=red>正在侦测按键并删除...</color>", PinkUI.Label);
        } else {
            if (GUILayout.Button("开始侦测 (添加)", PinkUI.Button)) {
                Settings.CurrentDetectionMode = DetectionMode.Add;
            }
            if (GUILayout.Button("开始侦测 (删除)", PinkUI.Button)) {
                Settings.CurrentDetectionMode = DetectionMode.Delete;
            }
        }

        if (GUILayout.Button(TweakStrings.Get(TranslationKeys.KeyViewer.CLEAR_KEY_COUNT), PinkUI.Button)) {
            _scrKeyviewer.ClearCounts();
        }
        
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private void DrawKeyViewerSettingsGUI() {
        // Show only in gameplay toggle
        CurrentProfile.ViewerOnlyGameplay =
            GUILayout.Toggle(
                CurrentProfile.ViewerOnlyGameplay,
                TweakStrings.Get(TranslationKeys.KeyViewer.VIEWER_ONLY_GAMEPLAY), PinkUI.Toggle);

        // Animate keys toggle
        CurrentProfile.AnimateKeys =
            GUILayout.Toggle(
                CurrentProfile.AnimateKeys,
                TweakStrings.Get(TranslationKeys.KeyViewer.ANIMATE_KEYS), PinkUI.Toggle);

        // Key press total toggle
        bool newShowTotal =
            GUILayout.Toggle(
                CurrentProfile.ShowKeyPressTotal,
                TweakStrings.Get(TranslationKeys.KeyViewer.SHOW_KEY_PRESS_TOTAL), PinkUI.Toggle);
        if (newShowTotal != CurrentProfile.ShowKeyPressTotal) {
            CurrentProfile.ShowKeyPressTotal = newShowTotal;
            _scrKeyviewer.UpdateLayout();
        }

        // Sliders with Pink style
        CurrentProfile.KeyViewerSize = NamedSliderPink(TweakStrings.Get(TranslationKeys.KeyViewer.KEY_VIEWER_SIZE), CurrentProfile.KeyViewerSize, 10f, 200f);
        CurrentProfile.KeyViewerXPos = NamedSliderPink(TweakStrings.Get(TranslationKeys.KeyViewer.KEY_VIEWER_X_POS), CurrentProfile.KeyViewerXPos, 0f, 1f);
        CurrentProfile.KeyViewerYPos = NamedSliderPink(TweakStrings.Get(TranslationKeys.KeyViewer.KEY_VIEWER_Y_POS), CurrentProfile.KeyViewerYPos, 0f, 1f);

        _scrKeyviewer.UpdateLayout();

        GUILayout.Space(8f);
        // Colors can be added here if needed, but keeping it simple for now as requested.
    }

    private float NamedSliderPink(string label, float value, float min, float max) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, PinkUI.Label, GUILayout.Width(150));
        float newVal = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(200));
        GUILayout.Label(newVal.ToString("F2"), PinkUI.Label, GUILayout.Width(50));
        GUILayout.EndHorizontal();
        return newVal;
    }

    /// <inheritdoc/>
    public override void OnEnable() {
        if (Settings.Profiles.Count == 0) {
            Settings.Profiles.Add(new KeyViewerProfile() { Name = "Default Profile" });
        }
        if (Settings.ProfileIndex < 0 || Settings.ProfileIndex >= Settings.Profiles.Count) {
            Settings.ProfileIndex = 0;
        }

        MigrateOldSettings();

        GameObject keyViewerObj = new GameObject();
        GameObject.DontDestroyOnLoad(keyViewerObj);
        _scrKeyviewer = keyViewerObj.AddComponent<scrKeyviewer>();
        _scrKeyviewer.Profile = CurrentProfile;
        ReplayInput.ScrKeyviewer = _scrKeyviewer;

        UpdateViewerVisibility();

        keyState = new Dictionary<KeyCode, bool>();
        ReplayInput.KeyState = keyState;
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime) {
        UpdateViewerVisibility();

        // Sync detection mode to DetectionManager
        if (DetectionManager.Instance != null) {
            DetectionManager.Instance.Mode = Settings.CurrentDetectionMode;
            DetectionManager.Instance.TargetProfile = CurrentProfile;
        }
    }

    /// <summary>
    /// Migrates old KeyLimiter settings to a KeyViewer profile if there are
    /// settings to migrate.
    /// TODO: Delete this after a few releases.
    /// </summary>
    private void MigrateOldSettings() {
        // Check if there are settings to migrate
        if (LimiterSettings.PressedBackgroundColor == Color.black
            && LimiterSettings.ReleasedBackgroundColor == Color.black
            && LimiterSettings.PressedOutlineColor == Color.black
            && LimiterSettings.ReleasedOutlineColor == Color.black
            && LimiterSettings.PressedTextColor == Color.black
            && LimiterSettings.ReleasedTextColor == Color.black) {
            return;
        }

        // Copy into new profile
        KeyViewerProfile profile = new KeyViewerProfile {
            Name = "Old Profile",
            ActiveKeys = new List<KeyCode>(LimiterSettings.ActiveKeys),
            ViewerOnlyGameplay = LimiterSettings.ViewerOnlyGameplay,
            AnimateKeys = LimiterSettings.AnimateKeys,
            KeyViewerSize = LimiterSettings.KeyViewerSize,
            KeyViewerXPos = LimiterSettings.KeyViewerXPos,
            KeyViewerYPos = LimiterSettings.KeyViewerYPos,
            PressedOutlineColor = LimiterSettings.PressedOutlineColor,
            ReleasedOutlineColor = LimiterSettings.ReleasedOutlineColor,
            PressedBackgroundColor = LimiterSettings.PressedBackgroundColor,
            ReleasedBackgroundColor = LimiterSettings.ReleasedBackgroundColor,
            PressedTextColor = LimiterSettings.PressedTextColor,
            ReleasedTextColor = LimiterSettings.ReleasedTextColor,
        };

        // Set current to migrated profile
        Settings.Profiles.Insert(0, profile);
        Settings.ProfileIndex = 0;

        // Clear old settings
        LimiterSettings.PressedBackgroundColor = Color.black;
        LimiterSettings.ReleasedBackgroundColor = Color.black;
        LimiterSettings.PressedOutlineColor = Color.black;
        LimiterSettings.ReleasedOutlineColor = Color.black;
        LimiterSettings.PressedTextColor = Color.black;
        LimiterSettings.ReleasedTextColor = Color.black;
    }

    /// <inheritdoc/>
    public override void OnDisable() {
        if (_scrKeyviewer != null) {
            GameObject.Destroy(_scrKeyviewer.gameObject);
        }
        ReplayInput.ScrKeyviewer = null;
    }

    private void UpdateViewerVisibility() {
        bool showViewer = true;
        if (CurrentProfile.ViewerOnlyGameplay
            && scrController.instance
            && scrConductor.instance) {
            bool playing = !scrController.instance.paused && scrConductor.instance.isGameWorld;
            showViewer &= playing;
        }
        if (showViewer != _scrKeyviewer.gameObject.activeSelf) {
            _scrKeyviewer.gameObject.SetActive(showViewer);
        }
    }
}
