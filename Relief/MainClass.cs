using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using System.IO;
using Jint;
using Jint.Runtime.Modules;
using System.Threading;
using Relief.Modules;
using Relief.Modules.BuiltIn;
using Relief.Loaders;
using Relief.Console;
using Relief.UI;

namespace Relief
{
    public static class MainClass
    {
        public static bool IsEnabled { get; private set; }
        public static Engine engine { get; private set; }
        public static Engine transformEngine { get; private set; }
        public static TypeScriptModuleLoader typeScriptLoader { get; private set; }
        public static EventSystem eventSystem { get; private set; }
        public static JsConsole jsConsole { get; private set; }
        
        static Thread scriptThread;

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

        public static string ScriptDir => Path.Combine(AssemblyDirectory, "Scripts");
        public static string DllDir => Path.GetDirectoryName(typeof(MonoBehaviour).Assembly.Location);
        public static string TypingsDir => Path.Combine(AssemblyDirectory, "Typings");

        public static readonly string[] DllNames = new string[]
        {
            "Unity.AI.Navigation.dll", "Unity.Addressables.dll", "Unity.MemoryProfiler.dll", "Unity.ResourceManager.dll",
            "Unity.ScriptableBuildPipeline.dll", "Unity.Services.Core.Analytics.dll", "Unity.Services.Core.Configuration.dll",
            "Unity.Services.Core.Device.dll", "Unity.Services.Core.Environments.Internal.dll", "Unity.Services.Core.Environments.dll",
            "Unity.Services.Core.Internal.dll", "Unity.Services.Core.Networking.dll", "Unity.Services.Core.Registration.dll",
            "Unity.Services.Core.Scheduler.dll", "Unity.Services.Core.Telemetry.dll", "Unity.Services.Core.Threading.dll",
            "Unity.Services.Core.dll", "Unity.TextMeshPro.dll", "Unity.Timeline.dll", "UnityEngine.AIModule.dll",
            "UnityEngine.ARModule.dll", "UnityEngine.AccessibilityModule.dll", "UnityEngine.AndroidJNIModule.dll",
            "UnityEngine.AnimationModule.dll", "UnityEngine.AssetBundleModule.dll", "UnityEngine.AudioModule.dll",
            "UnityEngine.ClothModule.dll", "UnityEngine.ClusterInputModule.dll", "UnityEngine.ClusterRendererModule.dll",
            "UnityEngine.ContentLoadModule.dll", "UnityEngine.CoreModule.dll", "UnityEngine.CrashReportingModule.dll",
            "UnityEngine.DSPGraphModule.dll", "UnityEngine.DirectorModule.dll", "UnityEngine.GIModule.dll",
            "UnityEngine.GameCenterModule.dll", "UnityEngine.GridModule.dll", "UnityEngine.HotReloadModule.dll",
            "UnityEngine.IMGUIModule.dll", "UnityEngine.ImageConversionModule.dll", "UnityEngine.InputLegacyModule.dll",
            "UnityEngine.InputModule.dll", "UnityEngine.JSONSerializeModule.dll", "UnityEngine.LocalizationModule.dll",
            "UnityEngine.NVIDIAModule.dll", "UnityEngine.ParticleSystemModule.dll", "UnityEngine.PerformanceReportingModule.dll",
            "UnityEngine.Physics2DModule.dll", "UnityEngine.PhysicsModule.dll", "UnityEngine.ProfilerModule.dll",
            "UnityEngine.PropertiesModule.dll", "UnityEngine.Purchasing.AppleCore.dll", "UnityEngine.Purchasing.AppleMacosStub.dll",
            "UnityEngine.Purchasing.AppleStub.dll", "UnityEngine.Purchasing.Codeless.dll", "UnityEngine.Purchasing.SecurityCore.dll",
            "UnityEngine.Purchasing.SecurityStub.dll", "UnityEngine.Purchasing.Stores.dll", "UnityEngine.Purchasing.WinRTCore.dll",
            "UnityEngine.Purchasing.WinRTStub.dll", "UnityEngine.Purchasing.dll", "UnityEngine.RuntimeInitializeOnLoadManagerInitializerModule.dll",
            "UnityEngine.ScreenCaptureModule.dll", "UnityEngine.SharedInternalsModule.dll", "UnityEngine.SpriteMaskModule.dll",
            "UnityEngine.SpriteShapeModule.dll", "UnityEngine.StreamingModule.dll", "UnityEngine.SubstanceModule.dll",
            "UnityEngine.SubsystemsModule.dll", "UnityEngine.TLSModule.dll", "UnityEngine.TerrainModule.dll",
            "UnityEngine.TerrainPhysicsModule.dll", "UnityEngine.TextCoreFontEngineModule.dll", "UnityEngine.TextCoreTextEngineModule.dll",
            "UnityEngine.TextRenderingModule.dll", "UnityEngine.TilemapModule.dll", "UnityEngine.UI.dll",
            "UnityEngine.UIElementsModule.dll", "UnityEngine.UIModule.dll", "UnityEngine.UmbraModule.dll",
            "UnityEngine.UnityAnalyticsCommonModule.dll", "UnityEngine.UnityAnalyticsModule.dll", "UnityEngine.UnityConnectModule.dll",
            "UnityEngine.UnityCurlModule.dll", "UnityEngine.UnityTestProtocolModule.dll", "UnityEngine.UnityWebRequestAssetBundleModule.dll",
            "UnityEngine.UnityWebRequestAudioModule.dll", "UnityEngine.UnityWebRequestModule.dll", "UnityEngine.UnityWebRequestTextureModule.dll",
            "UnityEngine.UnityWebRequestWWWModule.dll", "UnityEngine.VFXModule.dll", "UnityEngine.VRModule.dll",
            "UnityEngine.VehiclesModule.dll", "UnityEngine.VideoModule.dll", "UnityEngine.VirtualTexturingModule.dll",
            "UnityEngine.WindModule.dll", "UnityEngine.XRModule.dll", "UnityEngine.dll",
            "Assembly-CSharp.dll", "Assembly-CSharp-firstpass.dll"
        };

        public static UnityModManager.ModEntry.ModLogger Logger { get; private set; }
        private static Harmony harmony;

        internal static void Setup(UnityModManager.ModEntry modEntry)
        {
            Logger = modEntry.Logger;
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = Relief.UI.Options.OnGUI;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            IsEnabled = value;
            if (value) StartMod(modEntry);
            else StopMod(modEntry);
            return true;
        }

        private static void StartMod(UnityModManager.ModEntry modEntry)
        {
            harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            if (!Directory.Exists(TypingsDir)) Directory.CreateDirectory(TypingsDir);
            
            string unityEngineDtsPath = Path.Combine(TypingsDir, "unity-engine.d.ts");
            if (!File.Exists(unityEngineDtsPath))
            {
                Relief.Modules.EngineManager.GenerateTypeDefinitions(DllDir, unityEngineDtsPath, DllNames);
            }
            
            GenerateReactTypings(TypingsDir);
            GenerateBuiltinTypings(TypingsDir);

            try
            {
                if (!Directory.Exists(ScriptDir)) Directory.CreateDirectory(ScriptDir);

                jsConsole = new JsConsole(Logger);

                // Initialize Transform Engine for TS
                transformEngine = new Engine(options =>
                {
                    options.EnableModules(ScriptDir);
                    options.ExperimentalFeatures = ExperimentalFeature.All;
                    options.AllowClr();
                });
                transformEngine.SetValue("console", jsConsole);
                transformEngine.Execute(Properties.Resources.tsc);

                typeScriptLoader = new TypeScriptModuleLoader(transformEngine, ScriptDir, Logger, new DefaultModuleLoader(ScriptDir));

                // Initialize Main Engine
                eventSystem = new EventSystem(null); // Will set engine later
                engine = EngineManager.CreateEngine(ScriptDir, DllDir, typeScriptLoader, eventSystem, jsConsole, DllNames);
                
                // Re-initialize EventSystem with engine for JS callback support
                var engineField = typeof(EventSystem).GetField("_jsEngine", BindingFlags.NonPublic | BindingFlags.Instance);
                engineField?.SetValue(eventSystem, engine);

                EngineManager.RegisterModAssembly(engine, modEntry.Assembly);

                // Setup Unity Events GameObject
                var unityEventsGameObject = new GameObject("ReliefUnityEvents");
                var reliefUnityEvents = unityEventsGameObject.AddComponent<ReliefUnityEvents>();
                reliefUnityEvents.EventSystem = eventSystem;

                // Start script scanning thread
                scriptThread = new Thread(() => {
                    var scriptManager = new ScriptManager(engine, ScriptDir, typeScriptLoader, eventSystem, Logger);
                    scriptManager.ScanAndLoad();
                });
                scriptThread.Start();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        private static void GenerateBuiltinTypings(string typingsDir)
        {
            string builtinPath = Path.Combine(typingsDir, "builtin.d.ts");
            if (!File.Exists(builtinPath))
            {
                string builtinContent = @"declare module 'fs' {
    export function readFileSync(path: string, encoding?: string): string;
    export function writeFileSync(path: string, data: string, encoding?: string): void;
    export function existsSync(path: string): boolean;
    export function mkdirSync(path: string, options?: { recursive?: boolean }): void;
    export function readdirSync(path: string): string[];
    export function unlinkSync(path: string): void;
    export function rmdirSync(path: string): void;
}

declare module 'path' {
    export function join(...parts: string[]): string;
    export function resolve(...parts: string[]): string;
    export function basename(path: string): string;
    export function dirname(path: string): string;
    export function extname(path: string): string;
    export function isAbsolute(path: string): boolean;
}

declare module 'process' {
    export function cwd(): string;
    export function uptime(): number;
}

declare module 'eventHandler' {
    export function registerEvent(name: string, callback: (...args: any[]) => void): any;
    export function unregisterEvent(name: string): void;
    export function triggerEvent(name: string, ...args: any[]): void;
}

declare module 'uitext' {
    export const instance: {
        setText(text: string): void;
        setPosition(x: number, y: number): void;
        setSize(size: number): void;
        setAlignment(align: number): void;
        setFontStyle(style: number): void;
        setShadowEnabled(enabled: boolean): void;
    };
}

declare module 'resource-manager' {
    import { Texture2D, Sprite, Font, AssetBundle } from 'unity-engine';
    import { TMP_FontAsset } from 'tmpro';

    export function loadTexture(path: string): Texture2D;
    export function loadSprite(path: string): Sprite;
    export function loadFont(path: string): Font;
    export function loadTMPFont(path: string): TMP_FontAsset;
    export function getOSFont(name: string, size?: number): Font;
    export function createTMPFontFromFont(font: Font): TMP_FontAsset;
    export function loadAssetBundle(path: string): AssetBundle;
}

declare function setTimeout(callback: () => void, delay: number): number;
declare function setInterval(callback: () => void, delay: number): number;
declare function clearTimeout(id: number): void;
declare function clearInterval(id: number): void;
";
                File.WriteAllText(builtinPath, builtinContent);
            }
        }

        private static void GenerateReactTypings(string typingsDir)
        {
            string reactPath = Path.Combine(typingsDir, "react.d.ts");
            string jsxRuntimePath = Path.Combine(typingsDir, "react-jsx-runtime.d.ts");
            string unityComponentsPath = Path.Combine(typingsDir, "unity-components.d.ts");
            string reactUnityPath = Path.Combine(typingsDir, "react-unity.d.ts");

            // 始终生成/更新类型定义
            string reactContent = @"declare module 'react' {
    export function createElement(type: any, props?: any, ...children: any[]): any;
    export function useState<T>(initialState: T | (() => T)): [T, (newState: T) => void];
    export function useEffect(effect: () => void | (() => void), deps?: any[]): void;
}";
            File.WriteAllText(reactPath, reactContent);

            string reactUnityContent = @"declare module 'react-unity' {
    export interface Root {
        render(element: any): void;
        unmount(): void;
    }
    export function createRoot(container: any): Root;
    export function useState<T>(initialState: T | (() => T)): [T, (newState: T) => void];
    export function useEffect(effect: () => void | (() => void), deps?: any[]): void;
}";
            File.WriteAllText(reactUnityPath, reactUnityContent);

            string jsxRuntimeContent = @"/// <reference path=""unity-components.d.ts"" />
declare module 'react/jsx-runtime' {
    export function jsx(type: any, props: any, key?: any): any;
    export function jsxs(type: any, props: any, key?: any): any;

    export namespace JSX {
        interface Element {
            [key: string]: any;
            SetActive(value: boolean): void;
            Destroy(): void;
        }
        interface IntrinsicElements extends Relief.JSX.UnityElements {
            [elemName: string]: any;
        }
    }
}";
            File.WriteAllText(jsxRuntimePath, jsxRuntimeContent);

            string unityComponentsContent = @"/// <reference path=""unity-engine.d.ts"" />

declare module 'react/unityComponents' {
    export const Canvas: string;
    export const Image: string;
    export const Text: string;
    export const TextMeshPro: string;
    export const Button: string;
    export const UIText: string;
}

declare namespace Relief.JSX {
    interface UnityElementProps {
        name?: string;
        active?: boolean;
        layer?: number;
        tag?: string;
        dontDestroyOnLoad?: boolean;
        components?: any[];
        children?: any;
        
        // Transform props
        position?: { x: number, y: number, z: number };
        localPosition?: { x: number, y: number, z: number };
        localScale?: { x: number, y: number, z: number };
        rotation?: { x: number, y: number, z: number, w: number };
    }

    interface RectTransformProps extends UnityElementProps {
        anchoredPosition?: { x: number, y: number };
        sizeDelta?: { x: number, y: number };
        anchorMin?: { x: number, y: number };
        anchorMax?: { x: number, y: number };
        pivot?: { x: number, y: number };
    }

    interface CanvasProps extends RectTransformProps {
        renderMode?: number;
        sortingOrder?: number;
        referenceResolution?: { x: number, y: number };
    }

    interface ImageProps extends RectTransformProps {
        color?: { r: number, g: number, b: number, a: number };
        sprite?: any;
        raycastTarget?: boolean;
    }

    interface TextProps extends RectTransformProps {
        text?: string;
        fontSize?: number;
        font?: string | any;
        color?: { r: number, g: number, b: number, a: number };
        alignment?: string | number;
    }

    interface TextMeshProProps extends RectTransformProps {
        text?: string;
        fontSize?: number;
        font?: string | any;
        fontSizeMin?: number;
        fontSizeMax?: number;
        enableAutoSizing?: boolean;
        color?: { r: number, g: number, b: number, a: number };
        alignment?: string | number;
        fontStyle?: string | number;
        enableWordWrapping?: boolean;
        overflowMode?: string | number;
        margin?: { x: number, y: number, z: number, w: number };
        characterSpacing?: number;
        wordSpacing?: number;
        lineSpacing?: number;
        paragraphSpacing?: number;
        richText?: boolean;
        raycastTarget?: boolean;
    }

    interface UnityElements {
        Canvas: CanvasProps;
        Image: ImageProps;
        Text: TextProps;
        TextMeshPro: TextMeshProProps;
        Button: RectTransformProps;
        UIText: UnityElementProps;
    }
}
";
            File.WriteAllText(unityComponentsPath, unityComponentsContent);
        }

        private static void StopMod(UnityModManager.ModEntry modEntry)
        {
            harmony?.UnpatchAll(modEntry.Info.Id);
            engine = null;
        }
    }
}

