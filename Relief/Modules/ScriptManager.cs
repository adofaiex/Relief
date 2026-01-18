using System;
using System.IO;
using System.Collections.Generic;
using Jint;
using Jint.Native;
using TinyJson;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityModManagerNet;

using Relief.Modules.BuiltInModules;

namespace Relief.Modules
{
    public class ScriptManager
    {
        private readonly Engine _engine;
        private readonly string _scriptDir;
        private readonly TypeScriptModuleLoader _typeScriptLoader;
        private readonly EventSystem _eventSystem;
        private readonly UnityModManager.ModEntry.ModLogger _logger;

        public ScriptManager(Engine engine, string scriptDir, TypeScriptModuleLoader loader, EventSystem eventSystem, UnityModManager.ModEntry.ModLogger logger)
        {
            _engine = engine;
            _scriptDir = scriptDir;
            _typeScriptLoader = loader;
            _eventSystem = eventSystem;
            _logger = logger;
        }

        public void ScanAndLoad()
        {
            if (!Directory.Exists(_scriptDir)) return;

            foreach (var directory in Directory.GetDirectories(_scriptDir))
            {
                string projectJsonPath = Path.Combine(directory, "project.json");
                if (!File.Exists(projectJsonPath)) continue;

                try
                {
                    string projectJsonContent = File.ReadAllText(projectJsonPath);
                    var projectInfo = projectJsonContent.FromJson<ProjectInfo>();

                    if (string.IsNullOrEmpty(projectInfo.EntryPoint)) continue;

                    string entryPointFilePath = Path.Combine(directory, projectInfo.EntryPoint);
                    if (!File.Exists(entryPointFilePath)) continue;

                    string modulePath = entryPointFilePath;

                    // Handle TypeScript/JSX transformation (Can be done in background thread)
                    if (IsTranspilationRequired(projectInfo.EntryPoint))
                    {
                        string tsCode = File.ReadAllText(entryPointFilePath);
                        var transformedCode = _typeScriptLoader.TransformTypeScript(tsCode, Path.GetExtension(entryPointFilePath).ToLower(), Path.GetFileName(entryPointFilePath));
                        
                        string jsFilePath = Path.ChangeExtension(entryPointFilePath, ".temp.js");
                        File.WriteAllText(jsFilePath, transformedCode);
                        modulePath = jsFilePath;
                    }

                    _logger.Log($"Load Module <color=#debb7b>{projectInfo.Name}</color> (Inject: {projectInfo.Inject})");

                    // Use dispatcher for loading (Unity API calls must be on main thread)
                    Action loadAction = () => ReliefUnityEvents.Enqueue(() => LoadModule(_engine, modulePath, projectInfo));

                    HandleInjectionTiming(projectInfo, loadAction);
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex);
                }
            }

            ReliefUnityEvents.Enqueue(() => _eventSystem.TriggerEvent("modsLoaded"));
        }

        private bool IsTranspilationRequired(string entryPoint)
        {
            string ext = Path.GetExtension(entryPoint).ToLower();
            return ext == ".ts" || ext == ".tsx" || ext == ".jsx";
        }

        private void HandleInjectionTiming(ProjectInfo projectInfo, Action loadAction)
        {
            if (string.IsNullOrEmpty(projectInfo.Inject) || projectInfo.Inject.Equals("Loading", StringComparison.OrdinalIgnoreCase))
            {
                loadAction();
            }
            else if (projectInfo.Inject.Equals("Loaded", StringComparison.OrdinalIgnoreCase))
            {
                bool loaded = false;
                Action<object[]> wrappedAction = (args) =>
                {
                    if (loaded) return;
                    loaded = true;
                    loadAction();
                };

                _eventSystem.RegisterEvent("sceneLoaded", wrappedAction);

                var activeScene = SceneManager.GetActiveScene();
                if (activeScene.IsValid() && !string.IsNullOrEmpty(activeScene.name) && activeScene.name != "Init" && activeScene.name != "None")
                {
                    _logger.Log($"Scene '{activeScene.name}' already loaded, loading module {projectInfo.Name} immediately.");
                    wrappedAction(null);
                }
                else
                {
                    _logger.Log($"Module <color=#debb7b>{projectInfo.Name}</color> waiting for sceneLoaded.");
                }
            }
        }

        private void LoadModule(Engine engine, string modulePath, ProjectInfo projectInfo)
        {
            try
            {
                var exports = engine.Modules.Import(modulePath);
                var exportFunction = exports.Get("default").AsFunctionInstance();
                exportFunction.Call(engine.Global, new JsValue[] { projectInfo.Id, projectInfo.Name });
                _logger.Log($"Module <color=#debb7b>{projectInfo.Name}</color> Loaded.");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
                _logger.Log($"Failed to Load Module <color=#debb7b>{projectInfo.Name}</color>");
            }
        }
    }

    public class ProjectInfo
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public object[] Authors { get; set; }
        public string EntryPoint { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Inject { get; set; }
    }
}
