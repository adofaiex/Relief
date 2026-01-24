using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityModManagerNet.UnityModManager.Param;
using UnityEngine;
using UnityModManagerNet;

namespace Relief.UI
{
    public enum ExampleEnum
    {
        OptionA,
        OptionB,
        OptionC
    }

    public class Settings : UnityModManager.ModSettings
    {
        public bool EnableFeature { get; set; } = false;
        public string SomeTextSetting { get; set; } = "Default Value";
        public float FloatSliderSetting { get; set; } = 0.5f;
        public ExampleEnum EnumSetting { get; set; } = ExampleEnum.OptionA;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        public void ResetToDefaults()
        {
            EnableFeature = false;
            SomeTextSetting = "Default Value";
            FloatSliderSetting = 0.5f;
            EnumSetting = ExampleEnum.OptionA;
        }
    }

    public class Options
    {
        private static Settings _settings;

        public static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (_settings == null)
            {
                _settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            }

            GUILayout.Label("<b>通用设置</b>");
            _settings.EnableFeature = GUILayout.Toggle(_settings.EnableFeature, "启用某项功能");
            GUILayout.BeginHorizontal();
            GUILayout.Label("文本设置:", GUILayout.Width(100));
            _settings.SomeTextSetting = GUILayout.TextField(_settings.SomeTextSetting, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("浮点数滑块:", GUILayout.Width(100));
            _settings.FloatSliderSetting = GUILayout.HorizontalSlider(_settings.FloatSliderSetting, 0f, 1f, GUILayout.Width(200));
            GUILayout.Label(_settings.FloatSliderSetting.ToString("F2"));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("枚举设置:", GUILayout.Width(100));
            _settings.EnumSetting = (ExampleEnum)GUILayout.SelectionGrid((int)_settings.EnumSetting, Enum.GetNames(typeof(ExampleEnum)), 3, GUILayout.Width(300));
            GUILayout.EndHorizontal();

            if (GUILayout.Button("保存设置"))
            {
                _settings.Save(modEntry);
            }

            if (GUILayout.Button("重置为默认值"))
            {
                _settings.ResetToDefaults();
                _settings.Save(modEntry);
            }

            if (GUILayout.Button("输出日志"))
            {
                MainClass.Logger.Log("按钮被点击");
            }
        }
    }
}
