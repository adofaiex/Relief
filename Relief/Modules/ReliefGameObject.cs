using UnityEngine;
using Jint;
using Jint.Native;
using System;
using Jint.Runtime.Interop;

namespace Relief
{
    /// <summary>
    /// 为 JS 提供更友好的 GameObject 操作接口
    /// </summary>
    public class ReliefGameObject
    {
        public GameObject GameObject { get; }
        private readonly Engine _engine;
        private readonly UnityBridge _bridge;

        public ReliefGameObject(GameObject gameObject, Engine engine, UnityBridge bridge)
        {
            GameObject = gameObject;
            _engine = engine;
            _bridge = bridge;
        }

        public string name
        {
            get => GameObject.name;
            set => GameObject.name = value;
        }

        public void AddComponentJ(JsValue typeOrName)
        {
            if (typeOrName.IsString())
            {
                var typeName = typeOrName.AsString();
                // 使用 UnityBridge 的逻辑解析类型并添加
                var type = _bridge.GetType().GetMethod("ResolveUnityType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(_bridge, new object[] { typeName }) as Type;
                
                if (type != null)
                {
                    GameObject.AddComponent(type);
                }
            }
            else
            {
                var obj = typeOrName.ToObject();
                if (obj is Type type)
                {
                    GameObject.AddComponent(type);
                }
            }
        }

        public Component GetComponentJ(JsValue typeOrName)
        {
            if (typeOrName.IsString())
            {
                var typeName = typeOrName.AsString();
                var type = _bridge.GetType().GetMethod("ResolveUnityType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(_bridge, new object[] { typeName }) as Type;
                
                return type != null ? GameObject.GetComponent(type) : null;
            }
            else
            {
                var obj = typeOrName.ToObject();
                if (obj is Type type)
                {
                    return GameObject.GetComponent(type);
                }
            }
            return null;
        }

        public void SetActive(bool value) => GameObject.SetActive(value);

        // 允许直接访问底层 GameObject
        public GameObject ToUnityObject() => GameObject;
    }
}
