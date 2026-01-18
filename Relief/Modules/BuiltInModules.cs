using System;
using System.Linq;
using Jint;
using Jint.Native;
using Relief.Modules;
using UnityEngine;
using UnityEngine.UI;
using Jint.Native.Object;
using Jint.Native.Function;
using Jint.Runtime.Interop;

namespace Relief.Modules.BuiltInModules
{
    /// <summary>
    /// </summary>
    public static class BuiltInModules
    {
        /// <summary>
        /// 注册所有内置模块到JavaScript引擎
        /// </summary>
        /// <param name="engine">JavaScript引擎实例</param>
        /// <param name="eventSystem">事件系统实例</param>
        /// <param name="scriptDir">脚本目录路径</param>
        public static void RegisterAllModules(Engine engine, EventSystem eventSystem, string scriptDir)
        {
            TimerModule.Register(engine);
            FsModule.Register(engine, scriptDir);
            PathModule.Register(engine);
            ProcessModule.Register(engine);
            engine.Modules.Add("eventHandler", builder => {
                builder.ExportFunction("registerEvent", args => {
                    var name = args[0].AsString();
                    var cb = args[1];
                    return JsValue.FromObject(engine, eventSystem.RegisterEvent(name, cb));
                });
                builder.ExportFunction("unregisterEvent", args => {
                    var name = args[0].AsString();
                    eventSystem.UnregisterEvent(name);
                    return JsValue.Undefined;
                });
                builder.ExportFunction("triggerEvent", args => {
                    var name = args[0].AsString();
                    var eventArgs = args.Skip(1).Select(a => a.ToObject()).ToArray();
                    eventSystem.TriggerEvent(name, eventArgs);
                    return JsValue.Undefined;
                });
            });


            // Register UIText module
            engine.Modules.Add("uitext", builder =>
            {
                GameObject uiTextGameObject = new GameObject("UITextModule");
                UnityEngine.Object.DontDestroyOnLoad(uiTextGameObject);
                UIText uiTextInstance = uiTextGameObject.AddComponent<UIText>();

                builder.ExportObject("instance", new
                {
                    setText = new Action<string>(uiTextInstance.setText),
                    setPosition = new Action<float, float>(uiTextInstance.setPosition),
                    setSize = new Action<int>(uiTextInstance.setSize),
                    // setFont = new Action<Font>(uiTextInstance.setFont), // Font cannot be directly passed from JS
                    setAlignment = new Action<int>(align => uiTextInstance.setAlignment(uiTextInstance.toAlign(align))),
                    setFontStyle = new Action<int>(style => uiTextInstance.setFontStyle((FontStyle)style)),
                    setShadowEnabled = new Action<bool>(uiTextInstance.setShadowEnabled)
                });
            });

            // JSX Runtime implementation
            var unityBridge = new UnityBridge(engine);
            
            JsValue JsxFactory(JsValue[] args)
            {
                var type = args[0].AsString();
                var props = args[1].AsObject();
                
                var go = unityBridge.CreateGameObject(type);
                var unityGo = go.GameObject;

                // Apply props
                foreach (var prop in props.GetOwnProperties())
                {
                    var key = prop.Key.ToString();
                    var value = prop.Value.Value;
                    
                    if (key == "children")
                    {
                        void ProcessChild(JsValue child)
                        {
                            if (child.IsObject())
                            {
                                var childObj = child.AsObject();
                                var raw = childObj.ToObject();
                                if (raw is ReliefGameObject childGo)
                                {
                                    childGo.GameObject.transform.SetParent(unityGo.transform, false);
                                }
                                else if (childObj.HasProperty("GameObject")) // 兼容性处理
                                {
                                    var innerGo = childObj.Get("GameObject").ToObject() as GameObject;
                                    if (innerGo != null) innerGo.transform.SetParent(unityGo.transform, false);
                                }
                            }
                        }

                        if (value.IsArray())
                        {
                            var arr = value.AsArray();
                            for (int i = 0; i < (int)arr.Length; i++) ProcessChild(arr.Get(i.ToString()));
                        }
                        else ProcessChild(value);
                    }
                    else if (key == "components" && value.IsArray())
                    {
                        var arr = value.AsArray();
                        for (int i = 0; i < (int)arr.Length; i++)
                        {
                            go.AddComponentJ(arr.Get(i.ToString()));
                        }
                    }
                    else if (key == "dontDestoryOnLoad" && value.AsBoolean())
                    {
                        UnityEngine.Object.DontDestroyOnLoad(unityGo);
                    }
                    else
                    {
                        unityBridge.SetGameObjectProperty(unityGo.name, key, value);
                    }
                }

                return JsValue.FromObject(engine, go);
            }

            engine.Modules.Add("react/jsx-runtime", builder =>
            {
                builder.ExportFunction("jsx", JsxFactory);
                builder.ExportFunction("jsxs", JsxFactory);
            });

            engine.Modules.Add("react", builder =>
            {
                builder.ExportObject("createElement", JsValue.Undefined); // Minimal react shim
            });
        }

    }
}
