using System.Collections.Generic;
using UnityEngine;

namespace SRP.ADOFAI.Keyviewer.Core
{
    /// <summary>
    /// Provides localized strings for tweaks.
    /// </summary>
    public static class TweakStrings
    {
        private static readonly Dictionary<SystemLanguage, Dictionary<string, string>> stringMaps = new Dictionary<SystemLanguage, Dictionary<string, string>>
        {
            {
                SystemLanguage.English, new Dictionary<string, string>
                {
                    // Key Viewer strings
                    { "KeyViewer.NAME", "Key Viewer" },
                    { "KeyViewer.DESCRIPTION", "Shows which keys are being pressed" },
                    { "KeyViewer.NEW", "New" },
                    { "KeyViewer.DUPLICATE", "Duplicate" },
                    { "KeyViewer.DELETE", "Delete" },
                    { "KeyViewer.PROFILE_NAME", "Profile Name:" },
                    { "KeyViewer.PROFILES", "Profiles:" },
                    { "KeyViewer.REGISTERED_KEYS", "Registered Keys:" },
                    { "KeyViewer.DONE", "Done" },
                    { "KeyViewer.PRESS_KEY_REGISTER", "Press a key to register/unregister it" },
                    { "KeyViewer.CHANGE_KEYS", "Change Keys" },
                    { "KeyViewer.CLEAR_KEY_COUNT", "Clear Key Count" },
                    { "KeyViewer.VIEWER_ONLY_GAMEPLAY", "Show only during gameplay" },
                    { "KeyViewer.ANIMATE_KEYS", "Animate key presses" },
                    { "KeyViewer.SHOW_KEY_PRESS_TOTAL", "Show key press count" },
                    { "KeyViewer.KEY_VIEWER_SIZE", "Size:" },
                    { "KeyViewer.KEY_VIEWER_X_POS", "X Position:" },
                    { "KeyViewer.KEY_VIEWER_Y_POS", "Y Position:" },
                    { "KeyViewer.PRESSED_OUTLINE_COLOR", "Pressed Outline Color" },
                    { "KeyViewer.RELEASED_OUTLINE_COLOR", "Released Outline Color" },
                    { "KeyViewer.PRESSED_BACKGROUND_COLOR", "Pressed Background Color" },
                    { "KeyViewer.RELEASED_BACKGROUND_COLOR", "Released Background Color" },
                    { "KeyViewer.PRESSED_TEXT_COLOR", "Pressed Text Color" },
                    { "KeyViewer.RELEASED_TEXT_COLOR", "Released Text Color" },

                    // Planet Modification strings
                    { "Planet.NAME", "Planet Modification" },
                    { "Planet.DESCRIPTION", "Changes planet colors and opacities" },
                    { "Planet.RED_PLANET", "Red Planet (Planet 1)" },
                    { "Planet.BLUE_PLANET", "Blue Planet (Planet 2)" },
                    { "Planet.GREEN_PLANET", "Green Planet (Planet 3)" },
                    { "Planet.BODY_COLOR", "Body Color:" },
                    { "Planet.TAIL_COLOR", "Tail Color:" },
                    { "Planet.BODY_OPACITY", "Body Opacity:" },
                    { "Planet.TAIL_OPACITY", "Tail Opacity:" },
                    { "Planet.RING_OPACITY", "Ring Opacity:" },
                }
            },
            {
                SystemLanguage.Chinese, new Dictionary<string, string>
                {
                    // Key Viewer strings
                    { "KeyViewer.NAME", "按键显示器" },
                    { "KeyViewer.DESCRIPTION", "显示正在按下的按键" },
                    { "KeyViewer.NEW", "新建" },
                    { "KeyViewer.DUPLICATE", "复制" },
                    { "KeyViewer.DELETE", "删除" },
                    { "KeyViewer.PROFILE_NAME", "配置文件名称:" },
                    { "KeyViewer.PROFILES", "配置文件:" },
                    { "KeyViewer.REGISTERED_KEYS", "已注册按键:" },
                    { "KeyViewer.DONE", "完成" },
                    { "KeyViewer.PRESS_KEY_REGISTER", "按下按键以注册/注销" },
                    { "KeyViewer.CHANGE_KEYS", "修改按键" },
                    { "KeyViewer.CLEAR_KEY_COUNT", "清除按键计数" },
                    { "KeyViewer.VIEWER_ONLY_GAMEPLAY", "仅在游戏时显示" },
                    { "KeyViewer.ANIMATE_KEYS", "启用按键动画" },
                    { "KeyViewer.SHOW_KEY_PRESS_TOTAL", "显示总按键次数" },
                    { "KeyViewer.KEY_VIEWER_SIZE", "尺寸:" },
                    { "KeyViewer.KEY_VIEWER_X_POS", "水平位置:" },
                    { "KeyViewer.KEY_VIEWER_Y_POS", "垂直位置:" },
                    { "KeyViewer.PRESSED_OUTLINE_COLOR", "按下时的轮廓颜色" },
                    { "KeyViewer.RELEASED_OUTLINE_COLOR", "松开时的轮廓颜色" },
                    { "KeyViewer.PRESSED_BACKGROUND_COLOR", "按下时的背景颜色" },
                    { "KeyViewer.RELEASED_BACKGROUND_COLOR", "松开时的背景颜色" },
                    { "KeyViewer.PRESSED_TEXT_COLOR", "按下时的文字颜色" },
                    { "KeyViewer.RELEASED_TEXT_COLOR", "松开时的文字颜色" },

                    // Planet Modification strings
                    { "Planet.NAME", "星球修改" },
                    { "Planet.DESCRIPTION", "修改星球的颜色和不透明度" },
                    { "Planet.RED_PLANET", "红色星球 (星球 1)" },
                    { "Planet.BLUE_PLANET", "蓝色星球 (星球 2)" },
                    { "Planet.GREEN_PLANET", "绿色星球 (星球 3)" },
                    { "Planet.BODY_COLOR", "星球颜色:" },
                    { "Planet.TAIL_COLOR", "拖尾颜色:" },
                    { "Planet.BODY_OPACITY", "星球不透明度:" },
                    { "Planet.TAIL_OPACITY", "拖尾不透明度:" },
                    { "Planet.RING_OPACITY", "光环不透明度:" },
                }
            }
        };

        static TweakStrings()
        {
            // Simplified Chinese
            stringMaps[SystemLanguage.ChineseSimplified] = stringMaps[SystemLanguage.Chinese];
            // Traditional Chinese
            stringMaps[SystemLanguage.ChineseTraditional] = new Dictionary<string, string>
            {
                { "KeyViewer.NAME", "按鍵顯示器" },
                { "KeyViewer.DESCRIPTION", "顯示正在按下的按鍵" },
                { "KeyViewer.NEW", "新建" },
                { "KeyViewer.DUPLICATE", "複製" },
                { "KeyViewer.DELETE", "刪除" },
                { "KeyViewer.PROFILE_NAME", "配置文件名稱:" },
                { "KeyViewer.PROFILES", "配置文件:" },
                { "KeyViewer.REGISTERED_KEYS", "已註冊按鍵:" },
                { "KeyViewer.DONE", "完成" },
                { "KeyViewer.PRESS_KEY_REGISTER", "按下按鍵以註冊/注銷" },
                { "KeyViewer.CHANGE_KEYS", "修改按鍵" },
                { "KeyViewer.CLEAR_KEY_COUNT", "清除按鍵計數" },
                { "KeyViewer.VIEWER_ONLY_GAMEPLAY", "僅在遊戲時顯示" },
                { "KeyViewer.ANIMATE_KEYS", "啟用按鍵動畫" },
                { "KeyViewer.SHOW_KEY_PRESS_TOTAL", "顯示總按鍵次數" },
                { "KeyViewer.KEY_VIEWER_SIZE", "尺寸:" },
                { "KeyViewer.KEY_VIEWER_X_POS", "水平位置:" },
                { "KeyViewer.KEY_VIEWER_Y_POS", "垂直位置:" },
                { "KeyViewer.PRESSED_OUTLINE_COLOR", "按下時的輪廓顏色" },
                { "KeyViewer.RELEASED_OUTLINE_COLOR", "松開時的輪廓顏色" },
                { "KeyViewer.PRESSED_BACKGROUND_COLOR", "按下時的背景顏色" },
                { "KeyViewer.RELEASED_BACKGROUND_COLOR", "松開時的背景顏色" },
                { "KeyViewer.PRESSED_TEXT_COLOR", "按下時的文字顏色" },
                { "KeyViewer.RELEASED_TEXT_COLOR", "松開時的文字顏色" },

                // Planet Modification strings
                { "Planet.NAME", "星球修改" },
                { "Planet.DESCRIPTION", "修改星球的顏色和不透明度" },
                { "Planet.RED_PLANET", "紅色星球 (星球 1)" },
                { "Planet.BLUE_PLANET", "藍色星球 (星球 2)" },
                { "Planet.GREEN_PLANET", "綠色星球 (星球 3)" },
                { "Planet.BODY_COLOR", "星球顏色:" },
                { "Planet.TAIL_COLOR", "拖尾顏色:" },
                { "Planet.BODY_OPACITY", "星球不透明度:" },
                { "Planet.TAIL_OPACITY", "拖尾不透明度:" },
                { "Planet.RING_OPACITY", "光環不透明度:" },
            };
        }

        public static string Get(string key)
        {
            SystemLanguage lang = Application.systemLanguage;
            if (stringMaps.TryGetValue(lang, out var map) && map.TryGetValue(key, out string value))
            {
                return value;
            }
            if (stringMaps[SystemLanguage.English].TryGetValue(key, out value))
            {
                return value;
            }
            return key;
        }
    }

    /// <summary>
    /// Translation keys for localized strings.
    /// </summary>
    public static class TranslationKeys
    {
        public static class KeyViewer
        {
            public const string NAME = "KeyViewer.NAME";
            public const string DESCRIPTION = "KeyViewer.DESCRIPTION";
            public const string NEW = "KeyViewer.NEW";
            public const string DUPLICATE = "KeyViewer.DUPLICATE";
            public const string DELETE = "KeyViewer.DELETE";
            public const string PROFILE_NAME = "KeyViewer.PROFILE_NAME";
            public const string PROFILES = "KeyViewer.PROFILES";
            public const string REGISTERED_KEYS = "KeyViewer.REGISTERED_KEYS";
            public const string DONE = "KeyViewer.DONE";
            public const string PRESS_KEY_REGISTER = "KeyViewer.PRESS_KEY_REGISTER";
            public const string CHANGE_KEYS = "KeyViewer.CHANGE_KEYS";
            public const string CLEAR_KEY_COUNT = "KeyViewer.CLEAR_KEY_COUNT";
            public const string VIEWER_ONLY_GAMEPLAY = "KeyViewer.VIEWER_ONLY_GAMEPLAY";
            public const string ANIMATE_KEYS = "KeyViewer.ANIMATE_KEYS";
            public const string SHOW_KEY_PRESS_TOTAL = "KeyViewer.SHOW_KEY_PRESS_TOTAL";
            public const string KEY_VIEWER_SIZE = "KeyViewer.KEY_VIEWER_SIZE";
            public const string KEY_VIEWER_X_POS = "KeyViewer.KEY_VIEWER_X_POS";
            public const string KEY_VIEWER_Y_POS = "KeyViewer.KEY_VIEWER_Y_POS";
            public const string PRESSED_OUTLINE_COLOR = "KeyViewer.PRESSED_OUTLINE_COLOR";
            public const string RELEASED_OUTLINE_COLOR = "KeyViewer.RELEASED_OUTLINE_COLOR";
            public const string PRESSED_BACKGROUND_COLOR = "KeyViewer.PRESSED_BACKGROUND_COLOR";
            public const string RELEASED_BACKGROUND_COLOR = "KeyViewer.RELEASED_BACKGROUND_COLOR";
            public const string PRESSED_TEXT_COLOR = "KeyViewer.PRESSED_TEXT_COLOR";
            public const string RELEASED_TEXT_COLOR = "KeyViewer.RELEASED_TEXT_COLOR";
        }

        public static class Planet
        {
            public const string NAME = "Planet.NAME";
            public const string DESCRIPTION = "Planet.DESCRIPTION";
            public const string RED_PLANET = "Planet.RED_PLANET";
            public const string BLUE_PLANET = "Planet.BLUE_PLANET";
            public const string GREEN_PLANET = "Planet.GREEN_PLANET";
            public const string BODY_COLOR = "Planet.BODY_COLOR";
            public const string TAIL_COLOR = "Planet.TAIL_COLOR";
            public const string BODY_OPACITY = "Planet.BODY_OPACITY";
            public const string TAIL_OPACITY = "Planet.TAIL_OPACITY";
            public const string RING_OPACITY = "Planet.RING_OPACITY";
        }
    }
}
