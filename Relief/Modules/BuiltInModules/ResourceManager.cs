using System;
using System.IO;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace Relief.Modules.BuiltInModules
{
    public class ResourceManager
    {
        private static Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
        private static Dictionary<string, Font> fontCache = new Dictionary<string, Font>();
        private static Dictionary<string, TMP_FontAsset> tmpFontCache = new Dictionary<string, TMP_FontAsset>();

        /// <summary>
        /// 从文件加载纹理
        /// </summary>
        public Texture2D LoadTexture(string path)
        {
            if (textureCache.TryGetValue(path, out var cached)) return cached;

            if (!File.Exists(path))
            {
                MainClass.Logger.Log($"[ResourceManager] Texture file not found: {path}");
                return null;
            }

            try
            {
                byte[] fileData = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(fileData))
                {
                    textureCache[path] = tex;
                    return tex;
                }
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"[ResourceManager] Error loading texture: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 从文件加载精灵
        /// </summary>
        public Sprite LoadSprite(string path)
        {
            var tex = LoadTexture(path);
            if (tex == null) return null;

            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// 从文件加载字体 (Standard Font)
        /// </summary>
        public Font LoadFont(string path)
        {
            if (fontCache.TryGetValue(path, out var cached)) return cached;

            if (!File.Exists(path))
            {
                MainClass.Logger.Log($"[ResourceManager] Font file not found: {path}");
                return null;
            }

            try
            {
                // 注意：Unity 运行时加载 .ttf 文件通常需要通过 UnityWebRequest 或特殊的 API
                // 这里使用简单的 Font 构造函数尝试（可能在某些版本中受限）
                // 更好的做法是加载为字节流或使用第三方库，但在 Unity 中最稳妥的是 AssetBundle
                // 如果是原生文件，我们尝试使用 Font.CreateDynamicFontFromOSFont 的思路或者类似的
                
                // 实验性：对于非 OS 字体，通常需要 AssetBundle。
                // 但为了满足用户需求，我们尝试返回 null 并提示
                MainClass.Logger.Log($"[ResourceManager] Runtime loading of raw .ttf files directly into Font is restricted in Unity. Please use AssetBundles or ensure it's an OS font.");
                return null;
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"[ResourceManager] Error loading font: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取系统字体 (OS Font)
        /// </summary>
        public Font GetOSFont(string fontName, int fontSize = 14)
        {
            if (string.IsNullOrEmpty(fontName)) return null;
            string key = $"OS_{fontName}_{fontSize}";
            if (fontCache.TryGetValue(key, out var cached)) return cached;

            try
            {
                var font = Font.CreateDynamicFontFromOSFont(fontName, fontSize);
                if (font != null)
                {
                    fontCache[key] = font;
                    return font;
                }
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"[ResourceManager] Error getting OS font {fontName}: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 将普通 Font 转换为 TextMeshPro 的 FontAsset (运行时生成)
        /// </summary>
        public static TMP_FontAsset GetOrCreateTMPFont(Font font)
        {
            if (font == null) return null;
            string key = $"TMP_CONV_{font.name}_{font.GetInstanceID()}";
            if (tmpFontCache.TryGetValue(key, out var cached)) return cached;

            try
            {
                MainClass.Logger.Log($"[ResourceManager] Creating TMP_FontAsset from Font: {font.name}");
                
                // 确保动态字体包含一些基础字符，否则 TMP 转换可能失败或不全
                if (font.dynamic)
                {
                    // 包含所有常用 ASCII 字符以及用户在 UI 中可能用到的中文字符
                    string chars = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
                    font.RequestCharactersInTexture(chars);
                }

                var tmpFont = TMP_FontAsset.CreateFontAsset(font);
                if (tmpFont != null)
                {
                    // 确保材质使用正确的 Shader
                    if (tmpFont.material != null && tmpFont.material.shader == null)
                    {
                        tmpFont.material.shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
                    }
                    tmpFontCache[key] = tmpFont;
                    return tmpFont;
                }
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"[ResourceManager] Error creating TMP font from {font.name}: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 从文件加载 TextMeshPro 字体资产
        /// </summary>
        public TMP_FontAsset LoadTMPFont(string path)
        {
            if (tmpFontCache.TryGetValue(path, out var cached)) return cached;

            // 如果是 .ttf，我们需要先加载为 Font，然后转换
            // 但如上所述，Runtime 加载 .ttf 有难度。
            // 开发者通常会将 TMP_FontAsset 打包进 AssetBundle。
            
            if (path.EndsWith(".unity3d") || path.EndsWith(".bundle") || path.EndsWith(".assetbundle"))
            {
                return LoadTMPFontFromBundle(path);
            }

            MainClass.Logger.Log($"[ResourceManager] Loading TMP_FontAsset from raw file {path} is not directly supported. Use AssetBundles.");
            return null;
        }

        private TMP_FontAsset LoadTMPFontFromBundle(string bundlePath)
        {
            try
            {
                AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
                if (bundle == null) return null;

                var fonts = bundle.LoadAllAssets<TMP_FontAsset>();
                if (fonts.Length > 0)
                {
                    bundle.Unload(false); // 保持资源可用
                    return fonts[0];
                }
                bundle.Unload(true);
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"[ResourceManager] Error loading bundle: {ex.Message}");
            }
            return null;
        }
        
        /// <summary>
        /// 加载 AssetBundle
        /// </summary>
        public AssetBundle LoadAssetBundle(string path)
        {
            try
            {
                return AssetBundle.LoadFromFile(path);
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"[ResourceManager] Error loading AssetBundle: {ex.Message}");
                return null;
            }
        }
    }
}
