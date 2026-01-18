using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Relief.Modules.Internal
{
    public class TypeDefinitionGenerator
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly Dictionary<string, List<Type>> _modules = new Dictionary<string, List<Type>>();
        private readonly Dictionary<Type, string> _typeToModule = new Dictionary<Type, string>();
        private readonly HashSet<string> _reservedKeywords = new HashSet<string> {
            "break", "case", "catch", "class", "const", "continue", "debugger", "default", "delete",
            "do", "else", "enum", "export", "extends", "false", "finally", "for", "function",
            "if", "import", "in", "instanceof", "new", "null", "return", "super", "switch",
            "this", "throw", "true", "try", "typeof", "var", "void", "while", "with", "yield",
            "abstract", "as", "boolean", "any", "constructor", "declare", "get", "is", "module",
            "namespace", "readonly", "set", "type", "from", "of",
            "event", "checked", "fixed", "operator", "params", "internal", "protected", "private", "public", "static", "package"
        };

        public void AddAssembly(Assembly assembly, string ns, string moduleName)
        {
            var types = assembly.GetExportedTypes()
                .Where(t => t.IsPublic && t.Namespace == ns && !t.IsNested)
                .ToList();

            if (!_modules.ContainsKey(moduleName))
                _modules[moduleName] = new List<Type>();

            foreach (var type in types)
            {
                if (!_modules[moduleName].Contains(type))
                {
                    _modules[moduleName].Add(type);
                    _typeToModule[type] = moduleName;
                }
            }
        }

        public string Generate()
        {
            _sb.Clear();
            _sb.AppendLine("// Auto-generated TypeScript definitions for Relief Unity Bridge");
            _sb.AppendLine("// Strictly follows unity-engine/* module structure");
            _sb.AppendLine();

            foreach (var module in _modules)
            {
                _sb.AppendLine($"declare module '{module.Key}' {{");
                
                // Add dependencies based on used types in this module
                var dependencies = GetDependencies(module.Value);
                foreach (var dep in dependencies)
                {
                    if (dep != module.Key)
                    {
                        _sb.AppendLine($"    import * as {GetModuleAlias(dep)} from '{dep}';");
                    }
                }

                var sortedTypes = module.Value.OrderBy(t => t.IsEnum ? 0 : 1).ToList();
                var exportedNames = new HashSet<string>();

                foreach (var type in sortedTypes)
                {
                    if (type.Name.Contains("<") || type.Name.Contains("$") || type.Name.Contains("`") && !type.IsGenericType) continue;
                    if (type.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false).Any()) continue;

                    string typeName = GetSafeTypeName(type);
                    if (type.IsGenericType)
                    {
                        typeName += "_" + type.GetGenericArguments().Length;
                    }

                    if (!exportedNames.Add(typeName)) continue;

                    try {
                        GenerateType(type, "    ", module.Key, typeName);
                    } catch (Exception ex) {
                        _sb.AppendLine($"    // Error generating type {type.FullName}: {ex.Message}");
                    }
                }
                _sb.AppendLine("}");
                _sb.AppendLine();
            }

            return _sb.ToString();
        }

        private HashSet<string> GetDependencies(List<Type> types)
        {
            var deps = new HashSet<string>();
            foreach (var type in types)
            {
                // Check base type and interfaces
                if (type.BaseType != null) AddTypeDependency(type.BaseType, deps);
                foreach (var iface in type.GetInterfaces()) AddTypeDependency(iface, deps);

                // Check properties, fields, methods for types in other modules
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    AddTypeDependency(prop.PropertyType, deps);
                }
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    AddTypeDependency(field.FieldType, deps);
                }
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    AddTypeDependency(method.ReturnType, deps);
                    foreach (var p in method.GetParameters())
                    {
                        AddTypeDependency(p.ParameterType, deps);
                    }
                }
            }
            return deps;
        }

        private void AddTypeDependency(Type t, HashSet<string> deps)
        {
            if (t == null) return;
            while (t.IsArray) t = t.GetElementType();
            if (t.IsGenericType)
            {
                foreach (var arg in t.GetGenericArguments()) AddTypeDependency(arg, deps);
                t = t.GetGenericTypeDefinition();
            }
            if (_typeToModule.TryGetValue(t, out var mod))
            {
                deps.Add(mod);
            }
        }

        private string GetModuleAlias(string moduleName)
        {
            return moduleName.Replace("unity-engine/", "").Replace("/", "_").Replace("-", "_");
        }

        private void GenerateType(Type type, string indent, string currentModule, string overrideName = null)
        {
            string typeName = overrideName ?? GetSafeTypeName(type);

            if (type.BaseType == typeof(MulticastDelegate) || type.BaseType == typeof(Delegate))
            {
                var invokeMethod = type.GetMethod("Invoke");
                if (invokeMethod != null)
                {
                    var parameters = string.Join(", ", invokeMethod.GetParameters().Select((p, i) => $"{EscapeName(p.Name ?? $"arg{i}")}: {MapType(p.ParameterType, currentModule)}"));
                    _sb.AppendLine($"{indent}export type {typeName} = ({parameters}) => {MapType(invokeMethod.ReturnType, currentModule)};");
                    return;
                }
            }

            if (type.IsEnum)
            {
                _sb.AppendLine($"{indent}export enum {typeName} {{");
                var names = Enum.GetNames(type);
                foreach (var name in names)
                {
                    _sb.AppendLine($"{indent}    {name},");
                }
                _sb.AppendLine($"{indent}}}");
            }
            else if (type.IsClass || type.IsValueType || type.IsInterface)
            {
                string kind = type.IsInterface ? "interface" : "class";
                string extends = "";
                string genericParams = "";

                if (type.IsGenericTypeDefinition)
                {
                    var args = type.GetGenericArguments();
                    genericParams = $"<{string.Join(", ", args.Select(a => a.Name))}>";
                }
                
                if (type.BaseType != null && type.BaseType != typeof(object) && type.BaseType != typeof(ValueType) && type.BaseType != typeof(MulticastDelegate))
                {
                    string mappedBase = MapType(type.BaseType, currentModule);
                    if (mappedBase != "any")
                    {
                        extends = $" extends {mappedBase}";
                    }
                }

                _sb.AppendLine($"{indent}export {kind} {typeName}{genericParams}{extends} {{");
                
                var nestedTypes = type.GetNestedTypes(BindingFlags.Public);
                var nestedNames = new HashSet<string>(nestedTypes.Select(t => GetSafeTypeName(t)));

                // Fields
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (nestedNames.Contains(field.Name)) continue; // Skip if name conflicts with nested type
                    var isStatic = field.IsStatic ? "static " : "";
                    _sb.AppendLine($"{indent}    {isStatic}{EscapeName(field.Name)}: {MapType(field.FieldType, currentModule)};");
                }

                // Properties
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (nestedNames.Contains(prop.Name)) continue; // Skip if name conflicts with nested type
                    var isStatic = prop.GetAccessors().Any(a => a.IsStatic) ? "static " : "";
                    _sb.AppendLine($"{indent}    {isStatic}{EscapeName(prop.Name)}: {MapType(prop.PropertyType, currentModule)};");
                }

                // Methods
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Where(m => !m.IsSpecialName && !m.IsGenericMethod)
                    .Where(m => !m.Name.Contains("<") && !m.Name.Contains("$"))
                    .GroupBy(m => m.Name);

                foreach (var group in methods)
                {
                    if (nestedNames.Contains(group.Key)) continue; // Skip if name conflicts with nested type
                    foreach (var method in group)
                    {
                        var isStatic = method.IsStatic ? "static " : "";
                        var parameters = string.Join(", ", method.GetParameters().Select((p, i) => $"{EscapeName(p.Name ?? $"arg{i}")}: {MapType(p.ParameterType, currentModule)}"));
                        _sb.AppendLine($"{indent}    {isStatic}{EscapeName(method.Name)}({parameters}): {MapType(method.ReturnType, currentModule)};");
                    }
                }

                _sb.AppendLine($"{indent}}}");

                // Nested Types
                if (nestedTypes.Length > 0)
                {
                    _sb.AppendLine($"{indent}export namespace {typeName} {{");
                    foreach (var nestedType in nestedTypes)
                    {
                        string nestedName = GetSafeTypeName(nestedType);
                        if (nestedType.IsGenericType) nestedName += "_" + nestedType.GetGenericArguments().Length;
                        GenerateType(nestedType, indent + "    ", currentModule, nestedName);
                    }
                    _sb.AppendLine($"{indent}}}");
                }
            }
        }

        private string EscapeName(string name)
        {
            if (_reservedKeywords.Contains(name)) return $"_{name}";
            return name;
        }

        private string GetSafeTypeName(Type type)
        {
            string name = type.Name;
            if (type.IsGenericType)
            {
                name = type.Name.Split('`')[0];
                var genericArgs = type.GetGenericArguments();
                // If there are other types with the same name but different generic arity, 
                // we might need to append the count to avoid TS collision
                // But for now, let's just use the name and handle collisions in Generate
            }
            return name.Replace("<", "_").Replace(">", "_").Replace("$", "_").Replace("`", "_");
        }

        private string MapType(Type type, string currentModule)
        {
            if (type == typeof(void)) return "void";
            if (type == typeof(bool)) return "boolean";
            if (type == typeof(int) || type == typeof(float) || type == typeof(double) || 
                type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
                type == typeof(decimal) || type == typeof(uint) || type == typeof(ulong)) return "number";
            if (type == typeof(string) || type == typeof(char)) return "string";
            if (type == typeof(object)) return "any";
            if (type.IsArray) return $"{MapType(type.GetElementType(), currentModule)}[]";

            if (type.Name.StartsWith("ReadOnlySpan") || type.Name.StartsWith("Span")) return "any";
            
            if (type == typeof(Action)) return "() => void";
            
            if (type.IsGenericType)
            {
                var def = type.GetGenericTypeDefinition();
                if (def.Name.StartsWith("Action"))
                {
                    var args = type.GetGenericArguments();
                    var argList = string.Join(", ", args.Select((a, i) => $"arg{i}: {MapType(a, currentModule)}"));
                    return $"({argList}) => void";
                }
                if (def.Name.StartsWith("Func"))
                {
                    var args = type.GetGenericArguments();
                    var argList = string.Join(", ", args.Take(args.Length - 1).Select((a, i) => $"arg{i}: {MapType(a, currentModule)}"));
                    var returnType = MapType(args.Last(), currentModule);
                    return $"({argList}) => {returnType}";
                }
                if (def == typeof(Nullable<>))
                {
                    return $"{MapType(Nullable.GetUnderlyingType(type), currentModule)} | null";
                }
                if (def == typeof(List<>) || def == typeof(IEnumerable<>) || def == typeof(ICollection<>))
                {
                    return $"{MapType(type.GetGenericArguments()[0], currentModule)}[]";
                }
            }

            Type lookupType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

            if (_typeToModule.TryGetValue(lookupType, out var mod))
            {
                string typeName = GetSafeTypeName(lookupType);
                if (lookupType.IsGenericType)
                {
                    typeName += "_" + lookupType.GetGenericArguments().Length;
                }

                string result = (mod == currentModule) ? typeName : $"{GetModuleAlias(mod)}.{typeName}";

                if (type.IsGenericType && !type.IsGenericTypeDefinition)
                {
                    var args = type.GetGenericArguments();
                    if (args.Length > 0)
                    {
                        var argNames = string.Join(", ", args.Select(a => MapType(a, currentModule)));
                        return $"{result}<{argNames}>";
                    }
                }
                return result;
            }

            if (type.IsGenericParameter) return type.Name;

            return "any";
        }

        public void SaveToFile(string path)
        {
            File.WriteAllText(path, Generate());
        }
    }
}
