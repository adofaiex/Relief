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
using Relief.Modules.BuiltInModules;

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
            modEntry.OnGUI = Options.OnGUI;
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
            Relief.Modules.EngineManager.GenerateTypeDefinitions(DllDir, Path.Combine(TypingsDir, "unity-engine.d.ts"), DllNames);
            GenerateReactTypings(TypingsDir);

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

        private static void GenerateReactTypings(string typingsDir)
        {
            string reactPath = Path.Combine(typingsDir, "react.d.ts");
            string jsxRuntimePath = Path.Combine(typingsDir, "react-jsx-runtime.d.ts");

            string reactContent = @"declare module 'react' {
    export function createElement(type: any, props?: any, ...children: any[]): any;
}";
            string jsxRuntimeContent = @"declare module 'react/jsx-runtime' {
    export function jsx(type: any, props: any, key?: any): any;
    export function jsxs(type: any, props: any, key?: any): any;

    export namespace JSX {
        interface IntrinsicElements {
            [elemName: string]: any;
        }
        interface Element {
            [key: string]: any;
        }
    }
}";
            File.WriteAllText(reactPath, reactContent);
            File.WriteAllText(jsxRuntimePath, jsxRuntimeContent);
        }

        private static void StopMod(UnityModManager.ModEntry modEntry)
        {
            harmony?.UnpatchAll(modEntry.Info.Id);
            engine = null;
        }
    }
}

