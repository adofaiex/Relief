using System;
using System.Collections.Generic;
using UnityEngine;
using Jint;
using Jint.Native;
using System.Linq;
using Jint.Native.Array;
using Jint.Native.Object;
using TMPro;

namespace Relief
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
                RegisterGameObject(gameObject);

                // 根据 tag 自动添加常用组件
                switch (tag.ToLower())
                {
                    case "canvas":
                        var canvas = gameObject.AddComponent<Canvas>();
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        break;
                    case "image":
                        gameObject.AddComponent<UnityEngine.UI.Image>();
                        break;
                    case "textmeshpro":
                        gameObject.AddComponent<TextMeshProUGUI>();
                        break;
                    case "button":
                        gameObject.AddComponent<UnityEngine.UI.Button>();
                        break;
                }

                return new ReliefGameObject(gameObject, engine, this);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating GameObject: {ex.Message}");
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
        public bool SetGameObjectProperty(string gameObjectId, string propertyName, object propertyValue)
        {
            try
            {
                if (!gameObjectCache.TryGetValue(gameObjectId, out GameObject gameObject))
                {
                    var byName = GameObject.Find(gameObjectId);
                    if (byName == null)
                    {
                        Debug.LogError($"GameObject with ID or name {gameObjectId} not found");
                        return false;
                    }
                    gameObject = byName;
                }

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
                        // 处理自定义组件属性
                        SetCustomProperty(gameObject, propertyName, propertyValue);
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error setting GameObject property: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 设置自定义属性
        /// </summary>
        private void SetCustomProperty(GameObject gameObject, string propertyName, object propertyValue)
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
                return;
            }

            // 文本内容：TextMeshProUGUI 或 TextMesh
            if (propertyName == "text" && propertyValue is JsValue jsText)
            {
                var value = jsText.IsString() ? jsText.AsString() : jsText.ToString();
                var tmp = gameObject.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = value;
                    return;
                }
                var textMesh = gameObject.GetComponent<TextMesh>();
                if (textMesh != null)
                {
                    textMesh.text = value;
                    return;
                }
            }

            if (propertyName == "fontSize" && propertyValue is JsValue jsFontSize)
            {
                var value = (float)jsFontSize.AsNumber();
                var tmp = gameObject.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = value;
                    return;
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
                    return;
                }
            }

            if (propertyName == "color" && propertyValue is JsValue jsColor)
            {
                Color color = Color.white;
                if (jsColor.IsObject())
                {
                    var obj = jsColor.AsObject();
                    color = new Color(
                        (float)(obj.HasProperty("r") ? obj.Get("r").AsNumber() : 1),
                        (float)(obj.HasProperty("g") ? obj.Get("g").AsNumber() : 1),
                        (float)(obj.HasProperty("b") ? obj.Get("b").AsNumber() : 1),
                        (float)(obj.HasProperty("a") ? obj.Get("a").AsNumber() : 1)
                    );
                }
                var tmp = gameObject.GetComponent<TextMeshProUGUI>();
                if (tmp != null) { tmp.color = color; return; }
                var img = gameObject.GetComponent<UnityEngine.UI.Image>();
                if (img != null) { img.color = color; return; }
            }

            // 兼容旧格式：component.xyz
            if (propertyName.Equals("backgroundGradient", StringComparison.OrdinalIgnoreCase) && propertyValue is JsValue jsBg)
            {
                ApplyBackgroundGradient(gameObject, jsBg);
                return;
            }
            if (propertyName.Equals("backgroundColor", StringComparison.OrdinalIgnoreCase) && propertyValue is JsValue jsCol)
            {
                ApplyBackgroundColor(gameObject, jsCol);
                return;
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
                            break;
                        case "boxcollider":
                            gameObject.AddComponent<BoxCollider>();
                            break;
                    }
                }
            }
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
