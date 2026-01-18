using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Jint;
using Jint.Native;
using Jint.Runtime;
using Jint.Runtime.Interop;
using UnityEngine;
using TMPro;
using Relief.Modules.BuiltInModules;
using Relief.Modules.Internal;

namespace Relief.Modules
{
    public static class EngineManager
    {
        public static void GenerateTypeDefinitions(string dllFolder, string outputPath, IEnumerable<string> dllNames = null)
        {
            var generator = new TypeDefinitionGenerator();
            var assemblies = LoadAssemblies(dllFolder, dllNames);

            var moduleDefinitions = new Dictionary<string, List<(Assembly assembly, string ns)>>();

            foreach (var assembly in assemblies)
            {
                var exportedTypes = GetSafeExportedTypes(assembly);
                var namespaces = exportedTypes
                    .Where(t => !string.IsNullOrEmpty(t.Namespace))
                    .Select(t => t.Namespace)
                    .Distinct();

                foreach (var ns in namespaces)
                {
                    string moduleName = GetModuleNameFromNamespace(ns);
                    if (moduleName != null)
                    {
                        if (!moduleDefinitions.ContainsKey(moduleName))
                            moduleDefinitions[moduleName] = new List<(Assembly, string)>();
                        
                        moduleDefinitions[moduleName].Add((assembly, ns));
                    }
                }
            }

            foreach (var module in moduleDefinitions)
            {
                foreach (var item in module.Value)
                {
                    generator.AddAssembly(item.assembly, item.ns, module.Key);
                }
            }

            generator.SaveToFile(outputPath);
        }

        private static List<Assembly> LoadAssemblies(string folder, IEnumerable<string> dllNames = null)
        {
            var assemblies = new List<Assembly>();
            if (!System.IO.Directory.Exists(folder)) return assemblies;

            if (dllNames != null)
            {
                foreach (var name in dllNames)
                {
                    string path = System.IO.Path.Combine(folder, name);
                    if (System.IO.File.Exists(path))
                    {
                        try
                        {
                            assemblies.Add(Assembly.LoadFrom(path));
                        }
                        catch (Exception ex)
                        {
                            Relief.MainClass.Logger.Log($"Failed to load assembly {name} from {folder}: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                foreach (var file in System.IO.Directory.GetFiles(folder, "*.dll"))
                {
                    try
                    {
                        assemblies.Add(Assembly.LoadFrom(file));
                    }
                    catch (Exception ex)
                    {
                        Relief.MainClass.Logger.Log($"Failed to load assembly {file}: {ex.Message}");
                    }
                }
            }

            return assemblies;
        }

        private static string GetModuleNameFromNamespace(string ns)
        {
            if (string.IsNullOrEmpty(ns)) return null;

            if (ns == "UnityEngine") return "unity-engine";
            if (ns == "UnityEngine.UI") return "unity-engine/ui";
            if (ns.StartsWith("UnityEngine."))
            {
                var sub = ns.Substring("UnityEngine.".Length);
                return $"unity-engine/{FormatSubNamespace(sub)}";
            }
            if (ns == "TMPro") return "tmpro";

            // Generic handling for other namespaces
            return FormatNamespaceToModule(ns);
        }

        private static string FormatSubNamespace(string sub)
        {
            var parts = sub.Split('.');
            var lastPart = parts.Last();
            
            if (lastPart.EndsWith("Module")) 
                lastPart = lastPart.Substring(0, lastPart.Length - "Module".Length);
            
            if (lastPart.Length > 0)
                lastPart = char.ToLower(lastPart[0]) + lastPart.Substring(1);

            if (parts.Length > 1)
            {
                var result = string.Join("/", parts.Take(parts.Length - 1).Select(p => p.ToLower()));
                return $"{result}/{lastPart}";
            }
            
            return lastPart;
        }

        private static string FormatNamespaceToModule(string ns)
        {
            var parts = ns.Split('.');
            var formattedParts = parts.Select(p => {
                var s = p;
                if (s.EndsWith("Module")) s = s.Substring(0, s.Length - "Module".Length);
                if (s.Length > 0)
                    s = char.ToLower(s[0]) + s.Substring(1);
                return s;
            });
            return string.Join("/", formattedParts);
        }

        public static Engine CreateEngine(string scriptDir, string dllFolder, TypeScriptModuleLoader typeScriptLoader, EventSystem eventSystem, JsConsole jsConsole, IEnumerable<string> dllNames = null)
        {
            var engine = new Engine(options =>
            {
                options.EnableModules(scriptDir);
                options.ExperimentalFeatures = ExperimentalFeature.All;
                options.AllowClr(typeof(string).Assembly, typeof(Relief.Modules.BuiltInModules.BuiltInModules).Assembly);
                options.Modules.ModuleLoader = typeScriptLoader;
            });

            engine.SetValue("window", engine);
            engine.SetValue("document", engine);
            engine.SetValue("self", engine);
            engine.SetValue("console", jsConsole);
            engine.SetValue("eventSystem", eventSystem);

            // Execute base polyfills
            engine.Execute(Properties.Resources.fetch);
            engine.Execute(Properties.Resources.base64);
            engine.Execute(Properties.Resources.abortcontroller);

            // Register Built-in Modules
            Relief.Modules.BuiltInModules.BuiltInModules.RegisterAllModules(engine, eventSystem, scriptDir);

            // Register External Libraries from DLL folder
            RegisterDynamicModules(engine, dllFolder, dllNames);

            return engine;
        }

        private static IEnumerable<Type> GetSafeExportedTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetExportedTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                Relief.MainClass.Logger.Log($"Warning: Some types could not be loaded from assembly {assembly.FullName}: {ex.Message}");
                return ex.Types.Where(t => t != null && t.IsPublic);
            }
            catch (Exception ex)
            {
                Relief.MainClass.Logger.Log($"Error getting exported types from assembly {assembly.FullName}: {ex.Message}");
                return Enumerable.Empty<Type>();
            }
        }

        private static void RegisterDynamicModules(Engine engine, string dllFolder, IEnumerable<string> dllNames = null)
        {
            var assemblies = LoadAssemblies(dllFolder, dllNames);
            var moduleDefinitions = new Dictionary<string, List<Type>>();

            foreach (var assembly in assemblies)
            {
                var exportedTypes = GetSafeExportedTypes(assembly);
                var typesByNamespace = exportedTypes
                    .Where(t => !string.IsNullOrEmpty(t.Namespace))
                    .GroupBy(t => t.Namespace);

                foreach (var group in typesByNamespace)
                {
                    var ns = group.Key;
                    string moduleName = GetModuleNameFromNamespace(ns);

                    if (moduleName != null)
                    {
                        if (!moduleDefinitions.ContainsKey(moduleName))
                            moduleDefinitions[moduleName] = new List<Type>();
                        
                        moduleDefinitions[moduleName].AddRange(group);
                    }
                }
            }

            foreach (var module in moduleDefinitions)
            {
                try
                {
                    engine.Modules.Add(module.Key, builder =>
                    {
                        var exportedNames = new HashSet<string>();
                        foreach (var type in module.Value)
                        {
                            if (exportedNames.Contains(type.Name)) continue;
                            
                            try
                            {
                                builder.ExportType(type.Name, type);
                                exportedNames.Add(type.Name);
                            }
                            catch (Exception ex)
                            {
                                Relief.MainClass.Logger.Log($"Failed to export type {type.FullName} in module {module.Key}: {ex.Message}");
                            }
                        }

                        if (module.Key == "unity-engine")
                        {
                            builder.ExportFunction("createGameObject", new Func<JsValue[], JsValue>((args) => 
                                JsValue.FromObject(engine, new GameObject(args[0].AsString()))));
                        }
                    });
                    
                    Relief.MainClass.Logger.Log($"Registered module: {module.Key} with {module.Value.Count} types");
                }
                catch (Exception ex)
                {
                    Relief.MainClass.Logger.Log($"Failed to register module {module.Key}: {ex.Message}");
                }
            }
        }

        public static void RegisterModAssembly(Engine engine, System.Reflection.Assembly assembly)
        {
            if (assembly == null) return;
            engine.Modules.Add("mod-assembly", builder =>
            {
                foreach (var type in assembly.GetExportedTypes().Where(t => t.IsPublic))
                {
                    builder.ExportType(type.FullName, type);
                }
            });
        }
    }
}
