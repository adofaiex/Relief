using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Jint;
using Jint.Native;

namespace Relief.Modules
{
    public static class UnityTypeModule
    {
        public static void Register(Engine engine, UnityBridge unityBridge)
        {
            // 这个模块定义了所有大写的内置标签及其对应的初始化逻辑
            engine.Modules.Add("unity-types", builder => {
                var types = new Dictionary<string, Action<GameObject>>();

                types.Add("CANVAS", go => {
                    var canvas = go.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    go.AddComponent<CanvasScaler>();
                    go.AddComponent<GraphicRaycaster>();
                });

                types.Add("IMAGE", go => go.AddComponent<Image>());
                types.Add("TEXT", go => go.AddComponent<Text>());
                types.Add("TEXTMESHPRO", go => go.AddComponent<TextMeshProUGUI>());
                types.Add("BUTTON", go => go.AddComponent<Button>());
                types.Add("RECTTRANSFORM", go => {
                    if (go.GetComponent<RectTransform>() == null)
                        go.AddComponent<RectTransform>();
                });
                
                // 更多内置类型可以在这里添加...

                builder.ExportObject("types", types);
            });
        }
    }
}
