using UnityEngine;
using Jint;
using Jint.Native;
using System;
using Jint.Runtime.Interop;

namespace Relief.Modules
{
    /// <summary>
    /// 为 JS 提供更友好的 GameObject 操作接口
    /// </summary>
    public class ReliefGameObject
    {
        public GameObject GameObject { get; }
        public string Id { get; }
        private readonly UnityBridge _bridge;

        public ReliefGameObject(GameObject gameObject, string id, Engine engine, UnityBridge bridge)
        {
            GameObject = gameObject;
            Id = id;
            _bridge = bridge;
        }

        // 模拟 GameObject 的属性
        public string name
        {
            get => GameObject.name;
            set => GameObject.name = value;
        }

        public Transform transform => GameObject.transform;
        public int layer { get => GameObject.layer; set => GameObject.layer = value; }
        public string tag { get => GameObject.tag; set => GameObject.tag = value; }
        public bool activeSelf => GameObject.activeSelf;
        public bool activeInHierarchy => GameObject.activeInHierarchy;

        public void AddComponentJ(JsValue typeOrName)
        {
            if (typeOrName.IsString())
            {
                var typeName = typeOrName.AsString();
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
        public void Destroy() => UnityEngine.Object.Destroy(GameObject);

        // 提供一些快捷访问
        public Vector3 position
        {
            get => GameObject.transform.position;
            set => GameObject.transform.position = value;
        }

        public Vector3 localPosition
        {
            get => GameObject.transform.localPosition;
            set => GameObject.transform.localPosition = value;
        }

        public Vector3 localScale
        {
            get => GameObject.transform.localScale;
            set => GameObject.transform.localScale = value;
        }

        public Quaternion rotation
        {
            get => GameObject.transform.rotation;
            set => GameObject.transform.rotation = value;
        }

        public override string ToString() => GameObject.ToString();
    }
}
