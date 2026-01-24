using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Jint;
using Jint.Native;
using System.Linq;
using Jint.Native.Array;
using Jint.Native.Object;
using TMPro;
using Relief.Modules.BuiltInModules;

namespace Relief.Modules
{
    /// <summary>
    /// UnityBridge类用于连接JavaScript和Unity，实现伪React功能
    /// </summary>
    public class UnityBridge
    {
        private Engine engine;
        private Dictionary<string, GameObject> gameObjectCache = new Dictionary<string, GameObject>();

        public UnityBridge(Engine jsEngine)
        {
            engine = jsEngine;
            RegisterMethods();
        }

        /// <summary>
        /// 注册JavaScript可调用的方法
        /// </summary>
        private void RegisterMethods()
        {
            engine.SetValue("UnityBridge", this);
        }

        /// <summary>
        /// 创建GameObject
        /// </summary>
        /// <param name="tag">GameObject的名称</param>
        /// <returns>创建的GameObject的ID</returns>
        public ReliefGameObject CreateGameObject(string tag)
        {
            try
            {
                GameObject gameObject = new GameObject(tag);
                gameObject.SetActive(false); // 默认不显示，直到挂载
                string id = RegisterGameObject(gameObject);

                // 根据 tag 自动添加常用组件 (支持大写和 PascalCase 内置标签)
                string tagUpper = tag.ToUpper();
                switch (tagUpper)
                {
                    case "CANVAS":
                        var canvas = gameObject.AddComponent<Canvas>();
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        gameObject.AddComponent<CanvasScaler>();
                        gameObject.AddComponent<GraphicRaycaster>();
                        break;
                    case "IMAGE":
                        gameObject.AddComponent<UnityEngine.UI.Image>();
                        break;
                    case "TEXT":
                        gameObject.AddComponent<UnityEngine.UI.Text>();
                        break;
                    case "TEXTMESHPRO":
                        gameObject.AddComponent<TextMeshProUGUI>();
                        break;
                    case "BUTTON":
                        gameObject.AddComponent<UnityEngine.UI.Button>();
                        break;
                    case "UITEXT":
                        gameObject.AddComponent<Relief.Modules.BuiltIn.UIText>();
                        break;
                }

                return new ReliefGameObject(gameObject, id, engine, this);
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error creating GameObject: {ex.Message}");
                return null;
            }
        }

        private Type ResolveUnityType(string typeName)
        {
            if (typeName.StartsWith("UnityEngine."))
            {
                var type = typeof(UnityEngine.GameObject).Assembly.GetType(typeName);
                if (type != null) return type;
                
                // 尝试在 UnityEngine.UI 中查找
                type = typeof(UnityEngine.UI.Image).Assembly.GetType(typeName);
                if (type != null) return type;
            }
            
            if (typeName.StartsWith("TMPro."))
            {
                return typeof(TMPro.TextMeshProUGUI).Assembly.GetType(typeName);
            }

            // 尝试直接通过名称查找（不带命名空间）
            var found = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == typeName);
            
            return found;
        }

        public string RegisterGameObject(GameObject gameObject)
        {
            if (gameObject == null) return null;
            string id = Guid.NewGuid().ToString();
            gameObjectCache[id] = gameObject;
            return id;
        }

        /// <summary>
        /// 设置GameObject的属性
        /// </summary>
        /// <param name="gameObjectId">GameObject的ID</param>
        /// <param name="propertyName">属性名称</param>
        /// <param name="propertyValue">属性值</param>
        /// <returns>是否设置成功</returns>
        public bool SetGameObjectProperty(GameObject gameObject, string propertyName, object propertyValue)
        {
            try
            {
                if (gameObject == null) return false;

                switch (propertyName.ToLower())
                {
                    case "position":
                        if (propertyValue is JsValue jsPosition)
                        {
                            var x = jsPosition.AsObject().Get("x").AsNumber();
                            var y = jsPosition.AsObject().Get("y").AsNumber();
                            var z = jsPosition.AsObject().Get("z").AsNumber();
                            gameObject.transform.position = new Vector3((float)x, (float)y, (float)z);
                        }
                        break;
                    case "rotation":
                        if (propertyValue is JsValue jsRotation)
                        {
                            var x = jsRotation.AsObject().Get("x").AsNumber();
                            var y = jsRotation.AsObject().Get("y").AsNumber();
                            var z = jsRotation.AsObject().Get("z").AsNumber();
                            gameObject.transform.rotation = Quaternion.Euler((float)x, (float)y, (float)z);
                        }
                        break;
                    case "scale":
                        if (propertyValue is JsValue jsScale)
                        {
                            var x = jsScale.AsObject().Get("x").AsNumber();
                            var y = jsScale.AsObject().Get("y").AsNumber();
                            var z = jsScale.AsObject().Get("z").AsNumber();
                            gameObject.transform.localScale = new Vector3((float)x, (float)y, (float)z);
                        }
                        break;
                    case "active":
                        if (propertyValue is JsValue jsActive)
                        {
                            gameObject.SetActive(jsActive.AsBoolean());
                        }
                        break;
                    case "tag":
                        if (propertyValue is JsValue jsTag)
                        {
                            gameObject.tag = jsTag.AsString();
                        }
                        break;
                    case "name":
                        if (propertyValue is JsValue jsName)
                        {
                            gameObject.name = jsName.AsString();
                        }
                        break;
                    case "renderMode":
                        {
                            var canvas = gameObject.GetComponent<UnityEngine.Canvas>();
                            if (canvas != null)
                            {
                                var valObj = propertyValue is JsValue jsv ? jsv.ToObject() : propertyValue;
                                if (valObj is UnityEngine.RenderMode rm)
                                    canvas.renderMode = rm;
                                else if (valObj is double num)
                                    canvas.renderMode = (UnityEngine.RenderMode)(int)num;
                            }
                        }
                        break;
                    case "referenceResolution":
                        {
                            var scaler = gameObject.GetComponent<UnityEngine.UI.CanvasScaler>();
                            if (scaler != null)
                            {
                                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                                if (propertyValue is JsValue jsRes)
                                {
                                    if (jsRes.IsObject() && jsRes.AsObject().HasProperty("x"))
                                    {
                                        var x = jsRes.AsObject().Get("x").AsNumber();
                                        var y = jsRes.AsObject().Get("y").AsNumber();
                                        scaler.referenceResolution = new Vector2((float)x, (float)y);
                                    }
                                    else
                                    {
                                        var valObj = jsRes.ToObject();
                                        if (valObj is Vector2 v2)
                                            scaler.referenceResolution = v2;
                                    }
                                }
                            }
                        }
                        break;
                    case "sortingOrder":
                        {
                            var canvas = gameObject.GetComponent<UnityEngine.Canvas>();
                            if (canvas != null)
                            {
                                var valObj = propertyValue is JsValue jsv ? jsv.ToObject() : propertyValue;
                                if (valObj is int i) canvas.sortingOrder = i;
                                else if (valObj is double d) canvas.sortingOrder = (int)d;
                            }
                        }
                        break;
                    case "anchorMin":
                        {
                            var rt = gameObject.GetComponent<RectTransform>();
                            if (rt != null)
                            {
                                if (propertyValue is JsValue jsv && jsv.IsObject())
                                {
                                    var x = jsv.AsObject().Get("x").AsNumber();
                                    var y = jsv.AsObject().Get("y").AsNumber();
                                    rt.anchorMin = new Vector2((float)x, (float)y);
                                }
                                else if (propertyValue is Vector2 v) rt.anchorMin = v;
                            }
                        }
                        break;
                    case "anchorMax":
                        {
                            var rt = gameObject.GetComponent<RectTransform>();
                            if (rt != null)
                            {
                                if (propertyValue is JsValue jsv && jsv.IsObject())
                                {
                                    var x = jsv.AsObject().Get("x").AsNumber();
                                    var y = jsv.AsObject().Get("y").AsNumber();
                                    rt.anchorMax = new Vector2((float)x, (float)y);
                                }
                                else if (propertyValue is Vector2 v) rt.anchorMax = v;
                            }
                        }
                        break;
                    case "pivot":
                        {
                            var rt = gameObject.GetComponent<RectTransform>();
                            if (rt != null)
                            {
                                if (propertyValue is JsValue jsv && jsv.IsObject())
                                {
                                    var x = jsv.AsObject().Get("x").AsNumber();
                                    var y = jsv.AsObject().Get("y").AsNumber();
                                    rt.pivot = new Vector2((float)x, (float)y);
                                }
                                else if (propertyValue is Vector2 v) rt.pivot = v;
                            }
                        }
                        break;
                    case "anchoredPosition":
                        {
                            var rt = gameObject.GetComponent<RectTransform>();
                            if (rt != null)
                            {
                                if (propertyValue is JsValue jsv && jsv.IsObject())
                                {
                                    var x = jsv.AsObject().Get("x").AsNumber();
                                    var y = jsv.AsObject().Get("y").AsNumber();
                                    rt.anchoredPosition = new Vector2((float)x, (float)y);
                                }
                                else if (propertyValue is Vector2 v) rt.anchoredPosition = v;
                            }
                        }
                        break;
                    default:
                        // 1. 处理自定义组件属性 (如 TextMeshPro 的特殊处理)
                        if (SetCustomProperty(gameObject, propertyName, propertyValue))
                        {
                            break;
                        }
                        
                        // 2. 尝试通过反射映射到 Unity GameObject 或 Transform 属性
                        SetPropertyViaReflection(gameObject, propertyName, propertyValue);
                        break;
                }

                return true;
            }
            catch (Exception ex)
            { 
                MainClass.Logger.Log($"Error setting GameObject property: {ex.Message}");
                return false;
            }
        }

        private bool SetPropertyViaReflection(GameObject gameObject, string propertyName, object propertyValue)
        {
            try
            {
                // 1. 尝试在 GameObject 上找
                if (TrySetReflection(gameObject, propertyName, propertyValue)) return true;

                // 2. 尝试在 Transform 上找
                if (TrySetReflection(gameObject.transform, propertyName, propertyValue)) return true;

                // 3. 如果是 RectTransform，尝试在 RectTransform 上找
                var rectTransform = gameObject.GetComponent<RectTransform>();
                if (rectTransform != null && TrySetReflection(rectTransform, propertyName, propertyValue)) return true;

                // 4. 尝试在常见组件上找
                var tmp = gameObject.GetComponent<TMPro.TextMeshProUGUI>();
                if (tmp != null && TrySetReflection(tmp, propertyName, propertyValue)) return true;

                var image = gameObject.GetComponent<UnityEngine.UI.Image>();
                if (image != null && TrySetReflection(image, propertyName, propertyValue)) return true;

                var button = gameObject.GetComponent<UnityEngine.UI.Button>();
                if (button != null && TrySetReflection(button, propertyName, propertyValue)) return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool TrySetReflection(object target, string propertyName, object value)
        {
            var type = target.GetType();
            // 查找属性（不区分大小写）
            var prop = type.GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            if (prop != null && prop.CanWrite)
            {
                try
                {
                    var convertedValue = ConvertValue(value, prop.PropertyType);
                    prop.SetValue(target, convertedValue);
                    return true;
                }
                catch { }
            }

            // 查找字段（不区分大小写）
            var field = type.GetField(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            if (field != null)
            {
                try
                {
                    var convertedValue = ConvertValue(value, field.FieldType);
                    field.SetValue(target, convertedValue);
                    return true;
                }
                catch { }
            }

            return false;
        }

        private object ConvertValue(object value, Type targetType)
        {
            if (value == null) return null;

            // 如果已经是目标类型，直接返回
            if (targetType.IsAssignableFrom(value.GetType())) return value;

            if (value is JsValue jsv)
            {
                if (jsv.IsNull() || jsv.IsUndefined()) return null;

                if (targetType == typeof(string)) return jsv.ToString();
                if (targetType == typeof(float)) return (float)jsv.AsNumber();
                if (targetType == typeof(double)) return jsv.AsNumber();
                if (targetType == typeof(int)) return (int)jsv.AsNumber();
                if (targetType == typeof(bool)) return jsv.AsBoolean();

                if (targetType == typeof(Vector2) && jsv.IsObject())
                {
                    var v2Obj = jsv.AsObject();
                    return new Vector2((float)v2Obj.Get("x").AsNumber(), (float)v2Obj.AsObject().Get("y").AsNumber());
                }
                if (targetType == typeof(Vector3) && jsv.IsObject())
                {
                    var v3Obj = jsv.AsObject();
                    return new Vector3((float)v3Obj.Get("x").AsNumber(), (float)v3Obj.Get("y").AsNumber(), (float)v3Obj.Get("z").AsNumber());
                }
                if (targetType == typeof(Vector4) && jsv.IsObject())
                {
                    var v4Obj = jsv.AsObject();
                    return new Vector4(
                        (float)(v4Obj.HasProperty("x") ? v4Obj.Get("x").AsNumber() : 0),
                        (float)(v4Obj.HasProperty("y") ? v4Obj.Get("y").AsNumber() : 0),
                        (float)(v4Obj.HasProperty("z") ? v4Obj.Get("z").AsNumber() : 0),
                        (float)(v4Obj.HasProperty("w") ? v4Obj.Get("w").AsNumber() : 0)
                    );
                }
                if (targetType == typeof(Color) && jsv.IsObject())
                {
                    var cObj = jsv.AsObject();
                    return new Color(
                        (float)(cObj.HasProperty("r") ? cObj.Get("r").AsNumber() : 1),
                        (float)(cObj.HasProperty("g") ? cObj.Get("g").AsNumber() : 1),
                        (float)(cObj.HasProperty("b") ? cObj.Get("b").AsNumber() : 1),
                        (float)(cObj.HasProperty("a") ? cObj.Get("a").AsNumber() : 1)
                    );
                }

                if (targetType.IsEnum)
                {
                    if (jsv.IsNumber()) return Enum.ToObject(targetType, (int)jsv.AsNumber());
                    if (jsv.IsString()) return Enum.Parse(targetType, jsv.AsString(), true);
                }

                var rawObj = jsv.ToObject();
                if (targetType == typeof(TMP_FontAsset) && rawObj is Font f)
                {
                    return ResourceManager.GetOrCreateTMPFont(f);
                }

                return rawObj;
            }

            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return value;
            }
        }

        /// <summary>
        /// 设置自定义属性
        /// </summary>
        private bool SetCustomProperty(GameObject gameObject, string propertyName, object propertyValue)
        {
            // 通用组件列表：props.components = [ Type | string ]
            if (propertyName == "components" && propertyValue is JsValue jsComponents)
            {
                if (jsComponents.IsArray())
                {
                    var arr = jsComponents.AsArray();
                    var length = (int)arr.Length;
                    for (int i = 0; i < length; i++)
                    {
                        var item = arr.Get(i.ToString());
                        TryAddComponent(gameObject, item);
                    }
                }
                else
                {
                    TryAddComponent(gameObject, jsComponents);
                }
                return true;
            }

            // 字体支持
            if (propertyName == "font" && propertyValue is JsValue jsFont)
            {
                if (jsFont.IsString())
                {
                    var fontName = jsFont.AsString();
                    // 优先处理 TextMeshPro
                    var tmp = gameObject.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        var fontAsset = Resources.Load<TMP_FontAsset>(fontName) ?? 
                                        Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name == fontName);
                        if (fontAsset != null)
                        {
                            tmp.font = fontAsset;
                            return true;
                        }
                    }
                    // 兼容原生 Text
                    var text = gameObject.GetComponent<Text>();
                    if (text != null)
                    {
                        var font = Resources.Load<Font>(fontName) ?? 
                                   Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(f => f.name == fontName);
                        if (font != null)
                        {
                            text.font = font;
                            return true;
                        }
                    }
                }
                else if (jsFont.IsObject())
                {
                    var fontObj = jsFont.ToObject();
                    var tmp = gameObject.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        if (fontObj is TMP_FontAsset fa) { tmp.font = fa; }
                        else if (fontObj is Font f) { tmp.font = ResourceManager.GetOrCreateTMPFont(f); }
                        
                        tmp.SetAllDirty();
                        tmp.ForceMeshUpdate();
                        return true;
                    }
                    
                    var text = gameObject.GetComponent<Text>();
                    if (text != null && fontObj is Font standardFont) { text.font = standardFont; return true; }
                }
            }

            // 文本内容：TextMeshProUGUI 或 TextMesh
            if (propertyName == "text" && propertyValue is JsValue jsText)
            {
                var value = jsText.IsString() ? jsText.AsString() : jsText.ToString();
                var tmp = gameObject.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = value;
                    return true;
                }
                var textMesh = gameObject.GetComponent<TextMesh>();
                if (textMesh != null)
                {
                    textMesh.text = value;
                    return true;
                }
            }

            if (propertyName == "fontSize" && propertyValue is JsValue jsFontSize)
            {
                var value = (float)jsFontSize.AsNumber();
                var tmp = gameObject.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = value;
                    return true;
                }
            }

            if (propertyName == "alignment" && propertyValue is JsValue jsAlign)
            {
                var tmp = gameObject.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    if (jsAlign.IsNumber()) tmp.alignment = (TextAlignmentOptions)(int)jsAlign.AsNumber();
                    else if (jsAlign.IsString())
                    {
                        if (Enum.TryParse<TextAlignmentOptions>(jsAlign.AsString(), true, out var result))
                            tmp.alignment = result;
                    }
                    return true;
                }
            }

            if (propertyName == "color" && propertyValue is JsValue jsColor)
            {
                Color color = Color.white;
                if (jsColor.IsObject())
                {
                    var colorObj = jsColor.AsObject();
                    color = new Color(
                        (float)(colorObj.HasProperty("r") ? colorObj.Get("r").AsNumber() : 1),
                        (float)(colorObj.HasProperty("g") ? colorObj.Get("g").AsNumber() : 1),
                        (float)(colorObj.HasProperty("b") ? colorObj.Get("b").AsNumber() : 1),
                        (float)(colorObj.HasProperty("a") ? colorObj.Get("a").AsNumber() : 1)
                    );
                }
                var tmp = gameObject.GetComponent<TextMeshProUGUI>();
                if (tmp != null) { tmp.color = color; return true; }
                var img = gameObject.GetComponent<UnityEngine.UI.Image>();
                if (img != null) { img.color = color; return true; }
            }

            // 兼容旧格式：component.xyz
            if (propertyName.Equals("backgroundGradient", StringComparison.OrdinalIgnoreCase) && propertyValue is JsValue jsBg)
            {
                ApplyBackgroundGradient(gameObject, jsBg);
                return true;
            }
            if (propertyName.Equals("backgroundColor", StringComparison.OrdinalIgnoreCase) && propertyValue is JsValue jsCol)
            {
                ApplyBackgroundColor(gameObject, jsCol);
                return true;
            }

            // 兼容旧格式：component.xyz
            if (propertyName.StartsWith("component.") && propertyValue is JsValue)
            {
                string[] parts = propertyName.Split('.');
                if (parts.Length >= 2)
                {
                    string componentName = parts[1];
                    switch (componentName.ToLower())
                    {
                        case "rigidbody":
                            gameObject.AddComponent<Rigidbody>();
                            return true;
                        case "boxcollider":
                            gameObject.AddComponent<BoxCollider>();
                            return true;
                    }
                }
            }

            return false;
        }

        private void TryAddComponent(GameObject gameObject, JsValue item)
        {
            try
            {
                var obj = item.ToObject();
                if (obj is Type type)
                {
                    gameObject.AddComponent(type);
                    return;
                }
                var typeName = item.IsString() ? item.AsString() : obj?.ToString();
                if (!string.IsNullOrEmpty(typeName))
                {
                    var resolved = ResolveUnityType(typeName);
                    if (resolved != null)
                    {
                        gameObject.AddComponent(resolved);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error adding component: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置父子关系
        /// </summary>
        /// <param name="childId">子GameObject的ID</param>
        /// <param name="parentId">父GameObject的ID</param>
        /// <returns>是否设置成功</returns>
        public bool SetParent(string childId, string parentId)
        {
            try
            {
                if (!gameObjectCache.TryGetValue(childId, out GameObject childObject))
                {
                    Debug.LogError($"Child GameObject with ID {childId} not found");
                    return false;
                }

                if (!gameObjectCache.TryGetValue(parentId, out GameObject parentObject))
                {
                    Debug.LogError($"Parent GameObject with ID {parentId} not found");
                    return false;
                }

                childObject.transform.SetParent(parentObject.transform, false);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error setting parent: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get GameObject
        /// </summary>
        /// <param name="gameObjectId">ID of GameObject</param>
        
        public GameObject GetGameObject(string gameObjectId)
        {
            if (gameObjectCache.TryGetValue(gameObjectId, out GameObject gameObject))
            {
                return gameObject;
            }
            return null;
        }

        public GameObject[] getGameObjects()
        {
            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            return allObjects;
        }

        public GameObject querySelector(string query)
        {
            return GameObject.Find(query);
            
        }

        public GameObject getGameObjectByTag(string tag)
        {
            return GameObject.FindWithTag(tag); 
        }

        /// <summary>
        /// 销毁GameObject
        /// </summary>
        /// <param name="gameObjectId">ID of GameObject</param>
        public bool DestroyGameObject(string gameObjectId)
        {
            try
            {
                if (!gameObjectCache.TryGetValue(gameObjectId, out GameObject gameObject))
                {
                    Debug.LogError($"GameObject with ID {gameObjectId} not found");
                    return false;
                }

                GameObject.Destroy(gameObject);
                gameObjectCache.Remove(gameObjectId);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error destroying GameObject: {ex.Message}");
                return false;
            }
        }

        private Texture2D CreateRoundedGradientTexture(int width, int height, int radius, Color colStart, Color colEnd, bool vertical)
        {
            Texture2D tex = new Texture2D(width, height);
            Color[] pix = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = 0f;
                    bool inCorner = false;
                    if (x < radius && y < radius) { dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius)); inCorner = true; }
                    else if (x > width - radius - 1 && y < radius) { dist = Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, radius)); inCorner = true; }
                    else if (x < radius && y > height - radius - 1) { dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, height - radius - 1)); inCorner = true; }
                    else if (x > width - radius - 1 && y > height - radius - 1) { dist = Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, height - radius - 1)); inCorner = true; }
                    if (inCorner && dist > radius)
                    {
                        pix[y * width + x] = Color.clear;
                    }
                    else
                    {
                        float t = vertical ? (float)y / height : (float)x / width;
                        Color baseCol = Color.Lerp(colStart, colEnd, t);
                        if (inCorner && dist > radius - 1f)
                        {
                            baseCol.a *= (radius - dist);
                        }
                        pix[y * width + x] = baseCol;
                    }
                }
            }
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        private void ApplyBackgroundGradient(GameObject gameObject, JsValue jsBg)
        {
            var o = jsBg.AsObject();
            int width = o.HasProperty("width") ? (int)o.Get("width").AsNumber() : 128;
            int height = o.HasProperty("height") ? (int)o.Get("height").AsNumber() : 48;
            int radius = o.HasProperty("radius") ? (int)o.Get("radius").AsNumber() : 8;
            bool vertical = !o.HasProperty("vertical") || o.Get("vertical").AsBoolean();
            Color cStart = Color.white;
            Color cEnd = Color.white;
            if (o.HasProperty("start") && o.Get("start").IsObject())
            {
                var s = o.Get("start").AsObject();
                cStart = new Color((float)s.Get("r").AsNumber(), (float)s.Get("g").AsNumber(), (float)s.Get("b").AsNumber(), s.HasProperty("a") ? (float)s.Get("a").AsNumber() : 1f);
            }
            if (o.HasProperty("end") && o.Get("end").IsObject())
            {
                var e = o.Get("end").AsObject();
                cEnd = new Color((float)e.Get("r").AsNumber(), (float)e.Get("g").AsNumber(), (float)e.Get("b").AsNumber(), e.HasProperty("a") ? (float)e.Get("a").AsNumber() : 1f);
            }
            var tex = CreateRoundedGradientTexture(width, height, radius, cStart, cEnd, vertical);
            var rect = new Rect(0, 0, width, height);
            var pivot = new Vector2(0.5f, 0.5f);
            var border = new Vector4(radius, radius, radius, radius);
            var sprite = Sprite.Create(tex, rect, pivot, 100f, 0, SpriteMeshType.FullRect, border);
            var img = gameObject.GetComponent<UnityEngine.UI.Image>();
            if (img == null)
            {
                img = gameObject.AddComponent<UnityEngine.UI.Image>();
            }
            img.sprite = sprite;
            img.type = UnityEngine.UI.Image.Type.Sliced;
        }

        private void ApplyBackgroundColor(GameObject gameObject, JsValue jsCol)
        {
            var img = gameObject.GetComponent<UnityEngine.UI.Image>();
            if (img == null)
            {
                img = gameObject.AddComponent<UnityEngine.UI.Image>();
            }
            if (jsCol.IsObject())
            {
                var o = jsCol.AsObject();
                var col = new Color((float)o.Get("r").AsNumber(), (float)o.Get("g").AsNumber(), (float)o.Get("b").AsNumber(), o.HasProperty("a") ? (float)o.Get("a").AsNumber() : 1f);
                img.color = col;
            }
        }
    }
}
