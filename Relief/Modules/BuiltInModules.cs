using System;
using System.Linq;
using Jint;
using Jint.Native;
using Relief.Modules;
using Relief.Console;
using Relief.Modules.BuiltIn;
using Relief.Modules.BuiltInModules;
using Relief.Modules.vm;
using UnityEngine;
using UnityEngine.UI;
using Jint.Native.Object;
using Jint.Native.Function;
using Jint.Runtime.Interop;

namespace Relief.Modules
{
    /// <summary>
    /// </summary>
    public static class BuiltInModuleRegistry
    {
        /// <summary>
        /// 注册所有内置模块到JavaScript引擎
        /// </summary>
        public static void RegisterAll(Engine engine, string scriptDir, EventSystem eventSystem, JsConsole jsConsole)
        {
            PathModule.Register(engine);
            FsModule.Register(engine, scriptDir);
            ProcessModule.Register(engine);
            TimerModule.Register(engine);

            // Register ResourceManager module
            var resourceManager = new ResourceManager();
            engine.Modules.Add("resource-manager", builder =>
            {
                builder.ExportFunction("loadTexture", args => JsValue.FromObject(engine, resourceManager.LoadTexture(args[0].AsString())));
                builder.ExportFunction("loadSprite", args => JsValue.FromObject(engine, resourceManager.LoadSprite(args[0].AsString())));
                builder.ExportFunction("loadFont", args => JsValue.FromObject(engine, resourceManager.LoadFont(args[0].AsString())));
                builder.ExportFunction("loadTMPFont", args => JsValue.FromObject(engine, resourceManager.LoadTMPFont(args[0].AsString())));
                builder.ExportFunction("getOSFont", args => JsValue.FromObject(engine, resourceManager.GetOSFont(args[0].AsString(), args.Length > 1 ? (int)args[1].AsNumber() : 14)));
                builder.ExportFunction("createTMPFontFromFont", args => JsValue.FromObject(engine, ResourceManager.GetOrCreateTMPFont(args[0].ToObject() as Font)));
                builder.ExportFunction("loadAssetBundle", args => JsValue.FromObject(engine, resourceManager.LoadAssetBundle(args[0].AsString())));
            });

            // Register eventHandler module
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

            // JSX Runtime implementation
            var unityBridge = new UnityBridge(engine);

            // Register Unity Built-in types
            UnityTypeModule.Register(engine, unityBridge);

            // Register UIText module (uitext)
            engine.Modules.Add("uitext", builder =>
            {
                var go = unityBridge.CreateGameObject("uitext");
                var unityGo = go.GameObject;
                unityGo.SetActive(true); // Ensure Awake is called
                UnityEngine.Object.DontDestroyOnLoad(unityGo);
                var uiTextInstance = unityGo.GetComponent<Relief.Modules.BuiltIn.UIText>();

                builder.ExportObject("instance", new
                {
                    setText = new Action<string>(uiTextInstance.setText),
                    setPosition = new Action<float, float>(uiTextInstance.setPosition),
                    setSize = new Action<int>(uiTextInstance.setSize),
                    setAlignment = new Action<int>(align => uiTextInstance.setAlignment(uiTextInstance.toAlign(align))),
                    setFontStyle = new Action<int>(style => uiTextInstance.setFontStyle((FontStyle)style)),
                    setShadowEnabled = new Action<bool>(uiTextInstance.setShadowEnabled)
                });
            });

            // Register react-unity module
            var reactUnity = new ReactUnity(engine, unityBridge);
            engine.Modules.Add("react-unity", builder =>
            {
                builder.ExportFunction("createRoot", (args) => {
                    var root = reactUnity.CreateRoot(args[0]);
                    return JsValue.FromObject(engine, root);
                });
                builder.ExportFunction("useState", args => reactUnity.UseState(args.Length > 0 ? args[0] : JsValue.Undefined));
                builder.ExportFunction("useEffect", args => reactUnity.UseEffect(
                    args.Length > 0 ? args[0] : JsValue.Undefined, 
                    args.Length > 1 ? args[1] : JsValue.Undefined
                ));
            });

            // Register react/unityComponents module
            engine.Modules.Add("react/unityComponents", builder =>
            {
                builder.ExportValue("Canvas", "Canvas");
                builder.ExportValue("Image", "Image");
                builder.ExportValue("Text", "Text");
                builder.ExportValue("TextMeshPro", "TextMeshPro");
                builder.ExportValue("Button", "Button");
                builder.ExportValue("UIText", "UIText");
            });

            JsValue JsxFactory(JsValue[] args)
            { 
                var typeValue = args[0];
                var propsValue = args[1];
                
                // 如果是组件（对象或函数），返回一个描述符对象，由 VirtualDOM 处理
                if (typeValue.IsObject())
                {
                    var descriptor = new Jint.Native.JsObject(engine);
                    descriptor.Set("type", typeValue);
                    descriptor.Set("props", propsValue);
                    if (args.Length > 2 && !args[2].IsUndefined())
                    {
                        descriptor.Set("key", args[2]);
                    }
                    return descriptor;
                }

                // 如果是字符串，则创建实际的 GameObject（保持兼容性）
                string type = typeValue.AsString();
                var props = propsValue.AsObject();
                
                // 创建 GameObject
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
                                
                                // 处理描述符（递归渲染）
                                if (childObj.HasProperty("type"))
                                {
                                    var vnode = reactUnity.GetVirtualDOM().CreateVNode(child);
                                    reactUnity.GetVirtualDOM().MountVNode(vnode, unityGo);
                                    return;
                                }

                                var raw = childObj.ToObject();
                                if (raw is ReliefGameObject childGo)
                                {
                                    childGo.GameObject.transform.SetParent(unityGo.transform, false);
                                }
                                else if (childObj.HasProperty("gameObject"))
                                {
                                    var goVal = childObj.Get("gameObject");
                                    GameObject innerGo = null;
                                    if (goVal.IsString()) innerGo = unityBridge.GetGameObject(goVal.AsString());
                                    else innerGo = goVal.ToObject() as GameObject;
                                    
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
                    else if (key == "dontDestroyOnLoad" && value.AsBoolean())
                    {
                        UnityEngine.Object.DontDestroyOnLoad(unityGo);
                    }
                    else if (key == "active")
                    {
                        go.SetActive(value.AsBoolean());
                    }
                    else
                    {
                        unityBridge.SetGameObjectProperty(unityGo, key, value);
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
