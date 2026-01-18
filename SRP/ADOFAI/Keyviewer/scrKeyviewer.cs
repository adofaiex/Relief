using System.Collections.Generic;
using System.Linq;
using SRP.ADOFAI.Keyviewer.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TinyJson;

namespace SRP.ADOFAI.Keyviewer;

/// <summary>
/// A key viewer that shows if a list of given keys are currently being
/// pressed.
/// </summary>
internal class scrKeyviewer : MonoBehaviour {
    private static readonly Dictionary<KeyCode, string> KEY_TO_STRING =
        new Dictionary<KeyCode, string>() {
            { KeyCode.Alpha0, "0" },
            { KeyCode.Alpha1, "1" },
            { KeyCode.Alpha2, "2" },
            { KeyCode.Alpha3, "3" },
            { KeyCode.Alpha4, "4" },
            { KeyCode.Alpha5, "5" },
            { KeyCode.Alpha6, "6" },
            { KeyCode.Alpha7, "7" },
            { KeyCode.Alpha8, "8" },
            { KeyCode.Alpha9, "9" },
            { KeyCode.Keypad0, "0" },
            { KeyCode.Keypad1, "1" },
            { KeyCode.Keypad2, "2" },
            { KeyCode.Keypad3, "3" },
            { KeyCode.Keypad4, "4" },
            { KeyCode.Keypad5, "5" },
            { KeyCode.Keypad6, "6" },
            { KeyCode.Keypad7, "7" },
            { KeyCode.Keypad8, "8" },
            { KeyCode.Keypad9, "9" },
            { KeyCode.KeypadPlus, "+" },
            { KeyCode.KeypadMinus, "-" },
            { KeyCode.KeypadMultiply, "*" },
            { KeyCode.KeypadDivide, "/" },
            { KeyCode.KeypadEnter, "↵" },
            { KeyCode.KeypadEquals, "=" },
            { KeyCode.KeypadPeriod, "." },
            { KeyCode.Return, "↵" },
            { KeyCode.None, " " },
            { KeyCode.Tab, "⇥" },
            { KeyCode.Backslash, "\\" },
            { KeyCode.Slash, "/" },
            { KeyCode.Minus, "-" },
            { KeyCode.Equals, "=" },
            { KeyCode.LeftBracket, "[" },
            { KeyCode.RightBracket, "]" },
            { KeyCode.Semicolon, ";" },
            { KeyCode.Comma, "," },
            { KeyCode.Period, "." },
            { KeyCode.Quote, "'" },
            { KeyCode.UpArrow, "↑" },
            { KeyCode.DownArrow, "↓" },
            { KeyCode.LeftArrow, "←" },
            { KeyCode.RightArrow, "→" },
            { KeyCode.Space, "␣" },
            { KeyCode.BackQuote, "`" },
            { KeyCode.LeftShift, "L⇧" },
            { KeyCode.RightShift, "R⇧" },
            { KeyCode.LeftControl, "LCtrl" },
            { KeyCode.RightControl, "RCtrl" },
            { KeyCode.LeftAlt, "LAlt" },
            { KeyCode.RightAlt, "AAlt" },
            { KeyCode.Delete, "Del" },
            { KeyCode.PageDown, "Pg↓" },
            { KeyCode.PageUp, "Pg↑" },
            { KeyCode.Insert, "Ins" },
            { KeyCode.Backspace, "Back" },
            { KeyCode.F1, "F1" },
            { KeyCode.F2, "F2" },
            { KeyCode.F3, "F3" },
            { KeyCode.F4, "F4" },
            { KeyCode.F5, "F5" },
            { KeyCode.F6, "F6" },
            { KeyCode.F7, "F7" },
            { KeyCode.F8, "F8" },
            { KeyCode.F9, "F9" },
            { KeyCode.F10, "F10" },
            { KeyCode.F11, "F11" },
            { KeyCode.F12, "F12" },
        };

    private const float EASE_DURATION = 0.1f;
    private const float SHRINK_FACTOR = 0.9f;
    private const float KEY_WIDTH = 100;
    private const int KEY_FONT_SIZE = (int)(KEY_WIDTH * 3 / 4);
    private const int KEY_COUNT_FONT_SIZE = (int)(KEY_WIDTH / 2);
    private const float RAINKEY_FADE_SPEED = 1.0f;

    private Dictionary<KeyCode, long> keyCounts = new Dictionary<KeyCode, long>();

    private class RainKeyInstance {
        public RectTransform Rect;
        public Image Image;
        public float SpawnY;
        public float CurrentHeight;
        public KeyCode Code;
        public KeyConfig Config;
    }

    private List<RainKeyInstance> _activeRainKeys = new List<RainKeyInstance>();
    private GameObject _rainKeyContainer;

    private GameObject keysObject;
    private GameObject footerObject;
    private Dictionary<KeyCode, Image> keyBgImages;
    private Dictionary<KeyCode, Image> keyOutlineImages;
    private Dictionary<KeyCode, Text> keyTexts;
    private Dictionary<KeyCode, Text> keyCountTexts;
    private Dictionary<KeyCode, Image> keyBorderImages;
    private Dictionary<KeyCode, bool> keyPrevStates;
    private RectTransform keysRectTransform;
    private Text kpsText;
    private Text totalText;
    private List<float> pressTimes = new List<float>();
    private List<KeyCode> _tempKeys = new List<KeyCode>();

    private KeyViewerProfile _profile = new KeyViewerProfile();
    public string ConfigPath { get; set; }

    /// <summary>
    /// The current profile that this key viewer is using.
    /// </summary>
    public KeyViewerProfile Profile {
        get => _profile;
        set {
            if (_profile == value) return;
            _profile = value;
            if (gameObject.activeInHierarchy) {
                UpdateKeys();
            }
        }
    }

    /// <summary>
    /// Unity's Awake lifecycle event handler. Creates the key viewer.
    /// </summary>
    protected void Awake() {
        Canvas mainCanvas = gameObject.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 10001;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Don't call UpdateKeys here if Profile is about to be set
    }

    protected void OnDestroy() {
        // SaveProfile() moved to a safer place or avoided during rapid refresh
    }

    private string _lastSavedJson = "";

    public void SaveProfile() {
        if (!string.IsNullOrEmpty(ConfigPath) && Profile != null) {
            try {
                // Ensure all counts are up to date in Profile
                foreach (var pair in keyCounts) {
                    var config = Profile.KeyConfigs.FirstOrDefault(x => x.Code == pair.Key);
                    if (config != null) config.Count = pair.Value;
                }
                
                string json = Profile.ToJson();
                if (string.IsNullOrEmpty(json)) {
                    MainClass.Logger.Error("生成的 JSON 为空，取消保存。");
                    return;
                }

                // Optimization: Don't save if nothing changed
                if (json == _lastSavedJson) return;

                MainClass.RequestSave(ConfigPath, json);
                _lastSavedJson = json;
            } catch (System.Exception e) {
                MainClass.Logger.Error($"自动保存配置文件失败: {e.Message}\n{e.StackTrace}");
            }
        }
    }

    private void Update() {
        if (Profile == null || Profile.ActiveKeys == null) return;
        if (gameObject == null || !enabled) return;

        float deltaTime = Time.unscaledDeltaTime;

        // Update RainKeys
        UpdateRainKeys(deltaTime);

        // Use a local list to avoid collection modified exception
        _tempKeys.Clear();
        _tempKeys.AddRange(Profile.ActiveKeys);

        foreach (KeyCode code in _tempKeys) {
            bool isPressed = Input.GetKey(code);
            UpdateKeyState(code, isPressed, deltaTime);
        }

        // KPS update
        float unscaledTime = Time.unscaledTime;
        int initialCount = pressTimes.Count;
        pressTimes.RemoveAll(t => unscaledTime - t > 1f);
        if (kpsText != null && (initialCount != pressTimes.Count || Time.frameCount % 5 == 0)) {
            kpsText.text = $"KPS <color=#D8B4FE>{pressTimes.Count}</color>";
        }

        // Update Total
        if (totalText != null && _needsTotalUpdate) {
            _needsTotalUpdate = false;
            long total = 0;
            foreach (var val in keyCounts.Values) total += val;
            totalText.text = $"Total <color=#D8B4FE>{total}</color>";
        }
    }

    /// <summary>
    /// Updates the state of a specific key.
    /// </summary>
    public void UpdateKeyState(KeyCode code, bool isPressed, float deltaTime) {
        if (Profile == null) return;

        if (!keyTargetScales.ContainsKey(code)) keyTargetScales[code] = 1f;
        if (!keyCurrentScales.ContainsKey(code)) keyCurrentScales[code] = 1f;

        keyTargetScales[code] = isPressed ? 0.92f : 1f;
        keyCurrentScales[code] = Mathf.Lerp(keyCurrentScales[code], keyTargetScales[code], deltaTime * 15f);

        if (keyBgImages.TryGetValue(code, out var bgImage)) {
            Vector3 targetScale = Vector3.one * keyCurrentScales[code];
            bgImage.rectTransform.localScale = targetScale;
            
            if (keyOutlineImages.TryGetValue(code, out var outline)) outline.rectTransform.localScale = targetScale;
            if (keyTexts.TryGetValue(code, out var text)) text.rectTransform.localScale = targetScale;
            if (keyCountTexts.TryGetValue(code, out var countText)) countText.rectTransform.localScale = targetScale;
            if (keyBorderImages.TryGetValue(code, out var border)) border.rectTransform.localScale = targetScale;

            var config = Profile.KeyConfigs.FirstOrDefault(x => x.Code == code);

            // Color Lerp
            Color targetBg = isPressed ? 
                (config?.PressedBackgroundColor ?? Profile.PressedBackgroundColor) : 
                (config?.ReleasedBackgroundColor ?? Profile.ReleasedBackgroundColor);
            bgImage.color = Color.Lerp(bgImage.color, targetBg, deltaTime * 20f);

            Color targetOutline = isPressed ? 
                (config?.PressedBorderColor ?? Profile.PressedOutlineColor) : 
                (config?.ReleasedBorderColor ?? Profile.ReleasedOutlineColor);
            if (keyOutlineImages.TryGetValue(code, out var outlineImg)) 
                outlineImg.color = Color.Lerp(outlineImg.color, targetOutline, deltaTime * 20f);
            
            if (keyBorderImages.TryGetValue(code, out var borderImg)) 
                borderImg.color = Color.Lerp(borderImg.color, targetOutline, deltaTime * 20f);
            
            Color targetText = isPressed ? 
                (config?.PressedTextColor ?? Profile.PressedTextColor) : 
                (config?.ReleasedTextColor ?? Profile.ReleasedTextColor);
            if (keyTexts.TryGetValue(code, out var textComp)) 
                textComp.color = Color.Lerp(textComp.color, targetText, deltaTime * 20f);
            if (keyCountTexts.TryGetValue(code, out var countComp)) 
                countComp.color = Color.Lerp(countComp.color, targetText, deltaTime * 20f);
        }

        if (isPressed) {
            if (!keyPrevStates.ContainsKey(code) || !keyPrevStates[code]) {
                // Just pressed
                pressTimes.Add(Time.unscaledTime);
                if (!keyCounts.ContainsKey(code)) keyCounts[code] = 0;
                keyCounts[code]++;
                
                // Update profile count
                var config = Profile.KeyConfigs.FirstOrDefault(x => x.Code == code);
                if (config != null) config.Count = keyCounts[code];

                if (keyCountTexts.TryGetValue(code, out var countText)) {
                    countText.text = keyCounts[code].ToString();
                }
                _needsTotalUpdate = true;

                // Spawn RainKey
                SpawnRainKey(code);
            } else {
                // Holding
                ExtendRainKey(code, deltaTime);
            }
        }
        
        keyPrevStates[code] = isPressed;
    }

    /// <summary>
    /// Updates state for a batch of keys.
    /// </summary>
    public void UpdateState(Dictionary<KeyCode, bool> states) {
        float deltaTime = Time.unscaledDeltaTime;
        foreach (var pair in states) {
            UpdateKeyState(pair.Key, pair.Value, deltaTime);
        }
    }

    private void SpawnRainKey(KeyCode code) {
        var config = Profile.KeyConfigs.FirstOrDefault(x => x.Code == code);
        if (config == null || !config.EnableRainKey) return;

        if (_rainKeyContainer == null) {
            _rainKeyContainer = new GameObject("RainKeys");
            _rainKeyContainer.transform.SetParent(keysObject.transform);
            _rainKeyContainer.transform.SetAsFirstSibling(); // Behind keys
            var rt = _rainKeyContainer.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        GameObject rkObj = new GameObject("RainKey_" + code);
        rkObj.transform.SetParent(_rainKeyContainer.transform);
        var rtRk = rkObj.AddComponent<RectTransform>();
        var img = rkObj.AddComponent<Image>();
        img.color = config.RainKeyColor;

        // Position at key top
        if (keyBgImages.TryGetValue(code, out var bg)) {
            rtRk.anchorMin = Vector2.zero;
            rtRk.anchorMax = Vector2.zero;
            rtRk.pivot = new Vector2(0.5f, 0f); // Bottom center pivot
            rtRk.sizeDelta = new Vector2(bg.rectTransform.sizeDelta.x, 0);
            rtRk.anchoredPosition = new Vector2(bg.rectTransform.anchoredPosition.x, bg.rectTransform.anchoredPosition.y + bg.rectTransform.sizeDelta.y / 2);
            
            var instance = new RainKeyInstance {
                Rect = rtRk,
                Image = img,
                SpawnY = rtRk.anchoredPosition.y,
                CurrentHeight = 0,
                Code = code,
                Config = config
            };
            lock (_activeRainKeys) {
                _activeRainKeys.Add(instance);
            }
        }
    }

    private void ExtendRainKey(KeyCode code, float deltaTime) {
        var instance = _activeRainKeys.LastOrDefault(x => x.Code == code && x.CurrentHeight < x.Config.RainKeyMaxHeight);
        if (instance != null) {
            // If still pressing, increase height (extend downwards visually but actually upwards since pivot is bottom)
            instance.CurrentHeight += instance.Config.RainKeySpeed * deltaTime;
            instance.Rect.sizeDelta = new Vector2(instance.Rect.sizeDelta.x, instance.CurrentHeight);
        }
    }

    private void UpdateRainKeys(float deltaTime) {
        if (_activeRainKeys == null) return;

        lock (_activeRainKeys) {
            for (int i = _activeRainKeys.Count - 1; i >= 0; i--) {
                RainKeyInstance rk = null;
                try {
                    rk = _activeRainKeys[i];
                } catch { continue; }

                if (rk == null || rk.Rect == null || rk.Image == null) {
                    try { _activeRainKeys.RemoveAt(i); } catch { }
                    continue;
                }

                // Move upwards
                rk.Rect.anchoredPosition += new Vector2(0, rk.Config.RainKeySpeed * deltaTime);
                
                float distanceTravelled = rk.Rect.anchoredPosition.y - rk.SpawnY;
                
                // Fade out
                if (distanceTravelled > rk.Config.RainKeyMaxHeight - rk.Config.RainKeyFadeOutDistance) {
                    float fadeStart = rk.Config.RainKeyMaxHeight - rk.Config.RainKeyFadeOutDistance;
                    float alpha = 1f - (distanceTravelled - fadeStart) / rk.Config.RainKeyFadeOutDistance;
                    Color c = rk.Image.color;
                    c.a = Mathf.Clamp01(alpha * rk.Config.RainKeyColor.a);
                    rk.Image.color = c;
                }

                // Remove if finished
                if (distanceTravelled >= rk.Config.RainKeyMaxHeight || rk.Image.color.a <= 0) {
                    Destroy(rk.Rect.gameObject);
                    try { _activeRainKeys.RemoveAt(i); } catch { }
                }
            }
        }
    }

    private bool _needsTotalUpdate = true;

    private Dictionary<KeyCode, float> keyTargetScales = new Dictionary<KeyCode, float>();
    private Dictionary<KeyCode, float> keyCurrentScales = new Dictionary<KeyCode, float>();

    /// <summary>
    /// Updates what keys are displayed on the key viewer.
    /// </summary>
    public void UpdateKeys() {
        if (keysObject) {
            // Clean up RainKeys before destroying keysObject
            lock (_activeRainKeys) {
                _activeRainKeys.Clear();
            }
            _rainKeyContainer = null;
            Destroy(keysObject);
        }

        keysObject = new GameObject("Keys");
        keysObject.transform.SetParent(transform);
        keysRectTransform = keysObject.AddComponent<RectTransform>();

        keyBgImages = new Dictionary<KeyCode, Image>();
        keyOutlineImages = new Dictionary<KeyCode, Image>();
        keyBorderImages = new Dictionary<KeyCode, Image>();
        keyTexts = new Dictionary<KeyCode, Text>();
        keyCountTexts = new Dictionary<KeyCode, Text>();
        keyPrevStates = new Dictionary<KeyCode, bool>();

        Font font = null;
        try {
            font = RDString.GetFontDataForLanguage(SystemLanguage.English).font;
        } catch { }
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        foreach (KeyCode code in Profile.ActiveKeys) {
            var config = Profile.KeyConfigs.FirstOrDefault(x => x.Code == code);
            if (config == null) {
                config = new KeyConfig(code);
                Profile.KeyConfigs.Add(config);
            }
            keyCounts[code] = config.Count;

            GameObject keyBgObj = new GameObject(code.ToString() + " BG");
            keyBgObj.transform.SetParent(keysObject.transform);
            Image bgImage = keyBgObj.AddComponent<Image>();
            bgImage.sprite = TweakAssets.KeyBackgroundSprite;
            bgImage.color = config.ReleasedBackgroundColor ?? Profile.ReleasedBackgroundColor;
            keyBgImages[code] = bgImage;
            bgImage.type = Image.Type.Sliced;

            GameObject keyOutlineObj = new GameObject(code.ToString() + " Outline");
            keyOutlineObj.transform.SetParent(keysObject.transform);
            Image outlineImage = keyOutlineObj.AddComponent<Image>();
            outlineImage.sprite = TweakAssets.KeyOutlineSprite;
            outlineImage.color = config.ReleasedBorderColor ?? Profile.ReleasedOutlineColor;
            keyOutlineImages[code] = outlineImage;
            outlineImage.type = Image.Type.Sliced;

            GameObject keyBorderObj = new GameObject(code.ToString() + " Border");
            keyBorderObj.transform.SetParent(keysObject.transform);
            Image borderImage = keyBorderObj.AddComponent<Image>();
            borderImage.sprite = TweakAssets.KeyOutlineSprite; // Reuse outline sprite for border
            borderImage.color = config.ReleasedBorderColor ?? Profile.ReleasedOutlineColor;
            borderImage.type = Image.Type.Sliced;
            keyBorderImages[code] = borderImage;
            borderImage.gameObject.SetActive(config.EnableBorder);

            GameObject keyTextObj = new GameObject(code.ToString() + " Text");
            keyTextObj.transform.SetParent(keysObject.transform);
            Text text = keyTextObj.AddComponent<Text>();
            text.font = font;
            text.color = config.ReleasedTextColor ?? Profile.ReleasedTextColor;
            text.alignment = TextAnchor.UpperCenter;
            if (!KEY_TO_STRING.TryGetValue(code, out string codeString)) {
                codeString = config.Name ?? code.ToString();
            }
            text.text = codeString;
            keyTexts[code] = text;

            GameObject keyCountTextObj = new GameObject(code.ToString() + " Count");
            keyCountTextObj.transform.SetParent(keysObject.transform);
            Text countText = keyCountTextObj.AddComponent<Text>();
            countText.font = font;
            countText.color = config.ReleasedTextColor ?? Profile.ReleasedTextColor;
            countText.alignment = TextAnchor.LowerCenter;
            countText.text = keyCounts[code] + "";
            keyCountTexts[code] = countText;

            keyPrevStates[code] = false;
        }

        // Create Footer for KPS and Total
        footerObject = new GameObject("Footer");
        footerObject.transform.SetParent(keysObject.transform);
        
        GameObject kpsObj = new GameObject("KPS");
        kpsObj.transform.SetParent(footerObject.transform);
        kpsText = kpsObj.AddComponent<Text>();
        kpsText.font = font;
        kpsText.color = Profile.ReleasedTextColor;
        kpsText.alignment = TextAnchor.MiddleLeft;
        kpsText.fontSize = KEY_COUNT_FONT_SIZE;

        GameObject totalObj = new GameObject("Total");
        totalObj.transform.SetParent(footerObject.transform);
        totalText = totalObj.AddComponent<Text>();
        totalText.font = font;
        totalText.color = Profile.ReleasedTextColor;
        totalText.alignment = TextAnchor.MiddleRight;
        totalText.fontSize = KEY_COUNT_FONT_SIZE;

        UpdateLayout();
    }

    /// <summary>
    /// Updates the position, size, and color of the displayed keys.
    /// </summary>
    private static string TweenIdForKeyCode(KeyCode code, int instanceId) {
        return $"srp.key_viewer.{instanceId}.{code}";
    }

    public void UpdateLayout() {
        if (Profile == null || keysRectTransform == null) return;
        int count = Profile.ActiveKeys.Count;
        int keysPerRow = 8;
        int rows = Mathf.CeilToInt((float)count / keysPerRow);
        if (rows == 0) rows = 1;

        float keyHeight = Profile.ShowKeyPressTotal ? 120 : 100;
        float spacing = 10;
        float footerHeight = 40;
        
        float width = Mathf.Min(count, keysPerRow) * KEY_WIDTH + (Mathf.Min(count, keysPerRow) - 1) * spacing;
        float totalHeight = rows * keyHeight + (rows - 1) * spacing + (Profile.ShowKeyPressTotal ? footerHeight + spacing : 0);

        Vector2 pos = new Vector2(Profile.KeyViewerXPos, Profile.KeyViewerYPos);

        keysRectTransform.anchorMin = pos;
        keysRectTransform.anchorMax = pos;
        keysRectTransform.pivot = new Vector2(0.5f, 0.5f); // Use center pivot for easier positioning
        keysRectTransform.sizeDelta = new Vector2(width, totalHeight);
        keysRectTransform.anchoredPosition = Vector2.zero;
        keysRectTransform.localScale = Vector3.one * (Profile.KeyViewerSize / 100f);

        float x = 0;
        float y = totalHeight - keyHeight;
        int i = 0;
        int instanceId = GetInstanceID();
        
        foreach (KeyCode code in Profile.ActiveKeys) {
            if (!keyBgImages.TryGetValue(code, out var bgImage)) continue;

            // Kill existing tweens for this specific instance
            try {
                DG.Tweening.DOTween.Kill(TweenIdForKeyCode(code, instanceId));
            } catch { }

            bgImage.rectTransform.anchorMin = Vector2.zero;
            bgImage.rectTransform.anchorMax = Vector2.zero;
            bgImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            bgImage.rectTransform.sizeDelta = new Vector2(KEY_WIDTH, keyHeight);
            bgImage.rectTransform.anchoredPosition = new Vector2(x + KEY_WIDTH / 2, y + keyHeight / 2);

            if (keyOutlineImages.TryGetValue(code, out var outlineImage)) {
                outlineImage.rectTransform.anchorMin = Vector2.zero;
                outlineImage.rectTransform.anchorMax = Vector2.zero;
                outlineImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                outlineImage.rectTransform.sizeDelta = new Vector2(KEY_WIDTH, keyHeight);
                outlineImage.rectTransform.anchoredPosition = new Vector2(x + KEY_WIDTH / 2, y + keyHeight / 2);
            }

            var config = Profile.KeyConfigs.FirstOrDefault(k => k.Code == code);
            if (config != null && keyBorderImages.TryGetValue(code, out var border)) {
                border.rectTransform.anchorMin = Vector2.zero;
                border.rectTransform.anchorMax = Vector2.zero;
                border.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                border.rectTransform.sizeDelta = new Vector2(KEY_WIDTH + config.BorderWidth * 2, keyHeight + config.BorderWidth * 2);
                border.rectTransform.anchoredPosition = new Vector2(x + KEY_WIDTH / 2, y + keyHeight / 2);
            }

            float heightOffset = Profile.ShowKeyPressTotal ? keyHeight / 4f : 0;
            if (keyTexts.TryGetValue(code, out var text)) {
                text.rectTransform.anchorMin = Vector2.zero;
                text.rectTransform.anchorMax = Vector2.zero;
                text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                text.rectTransform.sizeDelta = new Vector2(KEY_WIDTH, keyHeight);
                text.rectTransform.anchoredPosition = new Vector2(x + KEY_WIDTH / 2, y + keyHeight / 2 + heightOffset);
                text.fontSize = KEY_FONT_SIZE;
                text.alignment = Profile.ShowKeyPressTotal ? TextAnchor.UpperCenter : TextAnchor.MiddleCenter;
            }

            if (keyCountTexts.TryGetValue(code, out var countText)) {
                countText.rectTransform.anchorMin = Vector2.zero;
                countText.rectTransform.anchorMax = Vector2.zero;
                countText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                countText.rectTransform.sizeDelta = new Vector2(KEY_WIDTH, keyHeight / 2f);
                countText.rectTransform.anchoredPosition = new Vector2(x + KEY_WIDTH / 2, y + keyHeight / 4f);
                countText.fontSize = KEY_COUNT_FONT_SIZE;
                countText.gameObject.SetActive(Profile.ShowKeyPressTotal);
            }

            x += KEY_WIDTH + spacing;
            i++;
            if (i % keysPerRow == 0) {
                x = 0;
                y -= (keyHeight + spacing);
            }
        }

        // Update footer
        if (footerObject != null) {
            var footerRect = footerObject.GetComponent<RectTransform>();
            if (footerRect == null) footerRect = footerObject.AddComponent<RectTransform>();
            footerRect.anchorMin = Vector2.zero;
            footerRect.anchorMax = Vector2.zero;
            footerRect.pivot = new Vector2(0.5f, 0f);
            footerRect.sizeDelta = new Vector2(width, footerHeight);
            footerRect.anchoredPosition = new Vector2(width / 2, -footerHeight - spacing);
            footerObject.SetActive(Profile.ShowKeyPressTotal);
            
            if (kpsText != null) {
                kpsText.rectTransform.anchorMin = Vector2.zero;
                kpsText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                kpsText.rectTransform.offsetMin = Vector2.zero;
                kpsText.rectTransform.offsetMax = Vector2.zero;
            }
            if (totalText != null) {
                totalText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                totalText.rectTransform.anchorMax = Vector2.one;
                totalText.rectTransform.offsetMin = Vector2.zero;
                totalText.rectTransform.offsetMax = Vector2.zero;
            }
        }
    }



    /// <summary>
    /// Clears the current key counts.
    /// </summary>
    public void ClearCounts() {
        foreach (KeyCode key in keyCounts.Keys.ToList()) {
            keyCounts[key] = 0;
            if (keyCountTexts.ContainsKey(key)) {
                keyCountTexts[key].text = "0";
            }
        }
    }


}
