using Jint;
using Jint.Native;
using Jint.Runtime.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityModManagerNet;

namespace Relief
{
    public class TypeScriptModuleLoader : IModuleLoader
    {
        private readonly Engine _transformEngine;
        private readonly string _baseDirectory;
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly IModuleLoader _nativeLoader; // 保存Jint原生加载器

        // 需要转换的文件扩展名
        private readonly HashSet<string> _transformExtensions = new HashSet<string> { ".ts", ".tsx", ".jsx" };

        public TypeScriptModuleLoader(Engine transformEngine, string baseDirectory,
                                     UnityModManager.ModEntry.ModLogger logger, IModuleLoader nativeLoader)
        {
            _transformEngine = transformEngine;
            _baseDirectory = Path.GetFullPath(baseDirectory ?? Directory.GetCurrentDirectory());
            _logger = logger;
            _nativeLoader = nativeLoader; // 接收原生加载器实例
        }

        public ResolvedSpecifier Resolve(string referencingModuleLocation, ModuleRequest moduleRequest)
        {
            if (_nativeLoader == null)
            {
                return new ResolvedSpecifier(moduleRequest, moduleRequest.Specifier, new Uri("file:///" + Path.Combine(_baseDirectory, moduleRequest.Specifier).Replace("\\", "/")), SpecifierType.RelativeOrAbsolute);
            }
            // 优先使用原生加载器进行解析
            return _nativeLoader.Resolve(referencingModuleLocation, moduleRequest);
        }

        public Module LoadModule(Engine engine, ResolvedSpecifier resolved)
        {
            try
            {
                // 获取文件路径并标准化
                var filePath = GetNormalizedPath(resolved);
                if (string.IsNullOrEmpty(filePath))
                {
                    return _nativeLoader.LoadModule(engine, resolved);
                }

                // 检查是否是需要转换的文件类型
                var extension = Path.GetExtension(filePath).ToLower();
                if (_transformExtensions.Contains(extension))
                {
                    // 处理需要转换的文件
                    var sourceCode = File.ReadAllText(filePath, Encoding.UTF8);
                    var transformedCode = TransformTypeScript(sourceCode, extension, Path.GetFileName(filePath));
                    return ModuleFactory.BuildSourceTextModule(engine, resolved, transformedCode);
                }
                else
                {
                    // 非转换类型文件，使用原生加载器加载
                    return _nativeLoader.LoadModule(engine, resolved);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex);
                throw new InvalidOperationException($"Failed to load module {resolved.Uri}: {ex.Message}", ex);
            }
        }

        private string GetNormalizedPath(ResolvedSpecifier resolved)
        {
            if (resolved.Uri == null) return null;

            // 处理URI转换为本地路径，避免相对URI问题
            if (resolved.Uri.IsAbsoluteUri)
            {
                if (resolved.Uri.Scheme == Uri.UriSchemeFile)
                {
                    return resolved.Uri.LocalPath;
                }
                return null;
            }

            // 相对URI则结合基础目录转换为绝对路径
            // 只有当看起来像路径时才处理，否则可能是内部模块名
            var specifier = resolved.Uri.OriginalString;
            if (specifier.StartsWith(".") || specifier.Contains("/") || specifier.Contains("\\"))
            {
                try
                {
                    return Path.GetFullPath(Path.Combine(_baseDirectory, specifier));
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        public string TransformTypeScript(string sourceCode, string extension, string fileName)
        {
            try
            {
                var presets = @"({
                    target: 'ES6',
                    module: 'ES6',
                    jsx: 'react-jsx',
                    allowJs: true,
                    esModuleInterop: true,
                    moduleResolution: 'bundler',
                    skipLibCheck: true,
                    forceConsistentCasingInFileNames: true
                })";

                var transformScript = $@"
                        ts.transpileModule(`{EscapeJavaScript(sourceCode)}`, {{
                            compilerOptions: {presets},
                            fileName: '{fileName}'
                        }}).outputText
                 ";
                var result = _transformEngine.Evaluate(transformScript);
                return result.AsString();
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex);
                throw new InvalidOperationException($"TypeScript transformation failed: {ex.Message}", ex);
            }
        }

        private string EscapeJavaScript(string input)
        {
            return input.Replace("\\", "\\\\")
                       .Replace("`", "\\`")
                       .Replace("$", "\\$")
                       .Replace("\r", "\\r")
                       .Replace("\n", "\\n");
        }
    }
}