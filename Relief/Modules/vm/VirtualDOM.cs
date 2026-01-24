using System;
using System.Collections.Generic;
using System.Linq;
using Jint;
using Jint.Native;
using Jint.Native.Array;
using Jint.Native.Object;
using UnityEngine;

namespace Relief.Modules.vm
{
    /// <summary>
    /// Virtual DOM implementation for React-like reconciliation
    /// </summary>
    public class VirtualDOM
    {
        private readonly UnityBridge _unityBridge;
        private readonly Dictionary<string, VNode> _vnodeCache = new Dictionary<string, VNode>();
        private readonly Dictionary<string, GameObject> _gameObjectMap = new Dictionary<string, GameObject>();
        private readonly Dictionary<GameObject, string> _objectIdMap = new Dictionary<GameObject, string>();

        public VirtualDOM(Engine engine, UnityBridge unityBridge)
        {
            _unityBridge = unityBridge;
        }

        /// <summary>
        /// Virtual Node representation
        /// </summary>
        public class VNode
        {
            public string Type { get; set; }
            public string Key { get; set; }
            public Dictionary<string, JsValue> Props { get; set; } = new Dictionary<string, JsValue>();
            public List<VNode> Children { get; set; } = new List<VNode>();
            public string GameObjectId { get; set; }
            public bool IsComponent { get; set; }
            public bool IsTextNode { get; set; }
            public string TextContent { get; set; }
            // 改为GameObject包装组件实例
            public GameObject ComponentInstance { get; set; }
            public string NodeId { get; set; }

            public VNode()
            {
                NodeId = Guid.NewGuid().ToString();
            }

            public VNode Clone()
            {
                var clone = new VNode
                {
                    Type = Type,
                    Key = Key,
                    Props = new Dictionary<string, JsValue>(Props),
                    GameObjectId = GameObjectId,
                    IsComponent = IsComponent,
                    IsTextNode = IsTextNode,
                    TextContent = TextContent,
                    // 复制组件实例的GameObject引用
                    ComponentInstance = ComponentInstance,
                    NodeId = NodeId
                };

                clone.Children = Children.Select(child => child.Clone()).ToList();
                return clone;
            }
        }

        /// <summary>
        /// Create Virtual Node from JSValue element
        /// </summary>
        public VNode CreateVNode(JsValue element)
        {
            try
            {
                if (element.IsUndefined() || element.IsNull())
                {
                    return null;
                }

                // 处理文本节点
                if (element.IsString() || element.IsNumber() || element.IsBoolean())
                {
                    return new VNode
                    {
                        Type = "text",
                        IsTextNode = true,
                        TextContent = element.ToString(),
                        Key = null
                    };
                }

                // 处理数组（片段）
                if (element.IsArray())
                {
                    var fragmentNode = new VNode
                    {
                        Type = "fragment",
                        Key = null
                    };

                    var array = element.AsArray();
                    for (int i = 0; i < array.Length; i++)
                    {
                        var childElement = array.Get(i.ToString());
                        var childVNode = CreateVNode(childElement);
                        if (childVNode != null)
                        {
                            fragmentNode.Children.Add(childVNode);
                        }
                    }

                    return fragmentNode;
                }

                // 处理React元素
                if (element.IsObject())
                {
                    var obj = element.AsObject();
                    var vnode = new VNode();

                    // 获取类型
                    if (obj.HasProperty("type"))
                    {
                        var typeValue = obj.Get("type");
                        if (typeValue.IsString())
                        {
                            vnode.Type = typeValue.AsString();
                            vnode.IsComponent = false;
                        }
                        else if (typeValue.IsObject())
                        {
                            // 组件类型 - 暂存类型名称，GameObject将在Mount时创建
                            vnode.Type = typeValue.ToString();
                            vnode.IsComponent = true;
                            // 组件实例的GameObject在Mount阶段初始化，这里先置空
                            vnode.ComponentInstance = null;
                        }
                    }
                    else if (obj.HasProperty("gameObject"))
                    {
                        // 遗留格式或直接传入的GameObject
                        vnode.Type = "gameObject";
                        var goVal = obj.Get("gameObject");
                        if (goVal.IsString())
                        {
                            vnode.GameObjectId = goVal.AsString();
                        }
                        else if (goVal.IsObject())
                        {
                            // 如果是对象，尝试获取其ID或注册它
                            var rawGo = goVal.ToObject();
                            if (rawGo is GameObject go)
                            {
                                vnode.GameObjectId = _unityBridge.RegisterGameObject(go);
                            }
                            else if (rawGo is ReliefGameObject reliefGo)
                            {
                                vnode.GameObjectId = reliefGo.Id;
                            }
                        }
                    }

                    // 获取key
                    if (obj.HasProperty("key") && !obj.Get("key").IsNull())
                    {
                        vnode.Key = obj.Get("key").ToString();
                    }

                    // 获取props
                    if (obj.HasProperty("props") && obj.Get("props").IsObject())
                    {
                        var propsObj = obj.Get("props").AsObject();
                        foreach (var prop in propsObj.GetOwnProperties())
                        {
                            if (prop.Key != "children")
                            {
                                vnode.Props[prop.Key.ToString()] = prop.Value.Value;
                            }
                        }

                        // 处理子节点
                        if (propsObj.HasProperty("children"))
                        {
                            var children = propsObj.Get("children");
                            ProcessChildren(vnode, children);
                        }
                    }

                    // 处理直接子节点属性
                    if (obj.HasProperty("children"))
                    {
                        var children = obj.Get("children");
                        ProcessChildren(vnode, children);
                    }

                    return vnode;
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating VNode: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 处理子元素
        /// </summary>
        private void ProcessChildren(VNode parent, JsValue children)
        {
            if (children.IsUndefined() || children.IsNull())
                return;

            if (children.IsArray())
            {
                var childArray = children.AsArray();
                for (int i = 0; i < childArray.Length; i++)
                {
                    var child = childArray.Get(i.ToString());
                    var childVNode = CreateVNode(child);
                    if (childVNode != null)
                    {
                        parent.Children.Add(childVNode);
                    }
                }
            }
            else
            {
                var childVNode = CreateVNode(children);
                if (childVNode != null)
                {
                    parent.Children.Add(childVNode);
                }
            }
        }

        /// <summary>
        /// 协调虚拟DOM与实际DOM
        /// </summary>
        public void Reconcile(VNode newTree, VNode oldTree, GameObject container)
        {
            try
            {
                if (newTree == null && oldTree == null)
                    return;

                if (oldTree == null)
                {
                    // 挂载新树
                    MountVNode(newTree, container);
                    return;
                }

                if (newTree == null)
                {
                    // 卸载旧树
                    UnmountVNode(oldTree);
                    return;
                }

                if (ShouldReplace(newTree, oldTree))
                {
                    // 替换整个子树
                    UnmountVNode(oldTree);
                    MountVNode(newTree, container);
                    return;
                }

                // 更新现有节点
                UpdateVNode(newTree, oldTree, container);
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error reconciling Virtual DOM: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查节点是否应该被替换
        /// </summary>
        private bool ShouldReplace(VNode newNode, VNode oldNode)
        {
            return newNode.Type != oldNode.Type ||
                   newNode.Key != oldNode.Key ||
                   newNode.IsComponent != oldNode.IsComponent;
        }

        /// <summary>
        /// 将虚拟节点挂载到实际DOM
        /// </summary>
        public void MountVNode(VNode vnode, GameObject container)
        {
            try
            {
                if (vnode == null) return;

                GameObject nodeGameObject = null;

                if (vnode.IsTextNode)
                {
                    // 创建文本节点
                    nodeGameObject = new GameObject($"Text: {vnode.TextContent}");
                    nodeGameObject.SetActive(false); // 初始不激活
                    nodeGameObject.transform.SetParent(container.transform, false);
                    
                    // 挂载时激活
                    nodeGameObject.SetActive(true);

                    // 添加TextMesh组件
                    var textMesh = nodeGameObject.GetComponent<TextMesh>();
                    if (textMesh == null)
                    {
                        textMesh = nodeGameObject.AddComponent<TextMesh>();
                    }
                    textMesh.text = vnode.TextContent;
                }
                else if (vnode.Type == "fragment")
                {
                    // 片段不创建GameObject，只挂载子节点
                    foreach (var child in vnode.Children)
                    {
                        MountVNode(child, container);
                    }
                    return;
                }
                else if (vnode.IsComponent)
                {
                    // 处理组件挂载 - 使用GameObject包装组件实例
                    Debug.Log($"Mounting component: {vnode.Type}");

                    nodeGameObject = new GameObject($"Component: {vnode.Type}");
                    nodeGameObject.SetActive(false); // 初始不激活
                    nodeGameObject.transform.SetParent(container.transform, false);
                    
                    // 默认在挂载时激活，除非 props 显式指定了 active: false
                    if (!vnode.Props.ContainsKey("active"))
                    {
                        nodeGameObject.SetActive(true);
                    }

                    var compId = _unityBridge.RegisterGameObject(nodeGameObject);
                    _objectIdMap[nodeGameObject] = compId;

                    // 将创建的GameObject赋值给ComponentInstance属性
                    vnode.ComponentInstance = nodeGameObject;

                    // 应用组件属性到包装的GameObject
                    ApplyProps(nodeGameObject, vnode.Props);
                }
                else
                {
                    // 创建常规GameObject
                    if (!string.IsNullOrEmpty(vnode.GameObjectId))
                    {
                        nodeGameObject = _unityBridge.GetGameObject(vnode.GameObjectId);
                        if (nodeGameObject != null) _objectIdMap[nodeGameObject] = vnode.GameObjectId;
                    }
                    else
                    {
                        var reliefGo = _unityBridge.CreateGameObject(vnode.Type);
                        nodeGameObject = reliefGo.GameObject;
                        vnode.GameObjectId = _unityBridge.RegisterGameObject(nodeGameObject);
                        if (nodeGameObject != null) _objectIdMap[nodeGameObject] = vnode.GameObjectId;
                    }

                    if (nodeGameObject != null)
                    {
                        nodeGameObject.transform.SetParent(container.transform, false);
                        
                        // 默认在挂载时激活，除非 props 显式指定了 active: false
                        if (!vnode.Props.ContainsKey("active"))
                        {
                            nodeGameObject.SetActive(true);
                        }

                        // 应用属性
                        ApplyProps(nodeGameObject, vnode.Props);
                    }
                }

                // 存储映射关系
                if (nodeGameObject != null)
                {
                    _gameObjectMap[vnode.NodeId] = nodeGameObject;
                }

                // 挂载子节点
                foreach (var child in vnode.Children)
                {
                    MountVNode(child, nodeGameObject ?? container);
                }

                // 缓存vnode
                _vnodeCache[vnode.NodeId] = vnode;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error mounting VNode: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新现有虚拟节点
        /// </summary>
        private void UpdateVNode(VNode newNode, VNode oldNode, GameObject container)
        {
            try
            {
                // 复制节点ID以维持引用
                newNode.NodeId = oldNode.NodeId;
                newNode.GameObjectId = oldNode.GameObjectId;

                // 对于组件节点，复制组件实例的GameObject引用
                if (newNode.IsComponent)
                {
                    newNode.ComponentInstance = oldNode.ComponentInstance;
                }

                GameObject nodeGameObject = null;
                if (_gameObjectMap.TryGetValue(oldNode.NodeId, out nodeGameObject))
                {
                    if (newNode.IsTextNode)
                    {
                        // 更新文本内容
                        var textMesh = nodeGameObject.GetComponent<TextMesh>();
                        if (textMesh != null && textMesh.text != newNode.TextContent)
                        {
                            textMesh.text = newNode.TextContent;
                        }
                    }
                    else if (!newNode.IsComponent && nodeGameObject != null)
                    {
                        // 更新属性
                        UpdateProps(nodeGameObject, newNode.Props, oldNode.Props);
                    }
                    // 处理组件更新
                    else if (newNode.IsComponent && newNode.ComponentInstance != null)
                    {
                        UpdateProps(newNode.ComponentInstance, newNode.Props, oldNode.Props);
                    }
                }

                // 协调子节点
                ReconcileChildren(newNode, oldNode, nodeGameObject ?? container);

                // 更新缓存
                _vnodeCache[newNode.NodeId] = newNode;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error updating VNode: {ex.Message}");
            }
        }

        /// <summary>
        /// 协调子节点
        /// </summary>
        private void ReconcileChildren(VNode newParent, VNode oldParent, GameObject container)
        {
            var newChildren = newParent.Children;
            var oldChildren = oldParent.Children;

            int maxLength = Math.Max(newChildren.Count, oldChildren.Count);

            for (int i = 0; i < maxLength; i++)
            {
                var newChild = i < newChildren.Count ? newChildren[i] : null;
                var oldChild = i < oldChildren.Count ? oldChildren[i] : null;

                Reconcile(newChild, oldChild, container);
            }
        }

        /// <summary>
        /// 卸载虚拟节点
        /// </summary>
        private void UnmountVNode(VNode vnode)
        {
            try
            {
                if (vnode == null) return;

                // 先卸载子节点
                foreach (var child in vnode.Children)
                {
                    UnmountVNode(child);
                }

                // 移除GameObject
                if (_gameObjectMap.TryGetValue(vnode.NodeId, out var gameObject) && gameObject != null)
                {
                    UnityEngine.Object.Destroy(gameObject);
                    _gameObjectMap.Remove(vnode.NodeId);
                }

                // 移除组件实例的GameObject
                if (vnode.IsComponent && vnode.ComponentInstance != null)
                {
                    UnityEngine.Object.Destroy(vnode.ComponentInstance);
                    vnode.ComponentInstance = null;
                }

                // 从缓存移除
                _vnodeCache.Remove(vnode.NodeId);

                Debug.Log($"Unmounted VNode: {vnode.Type}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error unmounting VNode: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用属性到GameObject
        /// </summary>
        private void ApplyProps(GameObject gameObject, Dictionary<string, JsValue> props)
        {
            foreach (var prop in props)
            {
                try
                {
                    _unityBridge.SetGameObjectProperty(gameObject, prop.Key, prop.Value);
                }
                catch (Exception ex)
                {
                    MainClass.Logger.Log($"Error applying prop {prop.Key}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 更新GameObject的属性
        /// </summary>
        private void UpdateProps(GameObject gameObject, Dictionary<string, JsValue> newProps, Dictionary<string, JsValue> oldProps)
        {
            // 移除不再存在的属性
            foreach (var oldProp in oldProps)
            {
                if (!newProps.ContainsKey(oldProp.Key))
                {
                    _unityBridge.SetGameObjectProperty(gameObject, oldProp.Key, JsValue.Null);
                }
            }

            // 添加/更新新属性
            foreach (var newProp in newProps)
            {
                if (!oldProps.ContainsKey(newProp.Key) ||
                    !AreSameValue(oldProps[newProp.Key], newProp.Value))
                {
                    _unityBridge.SetGameObjectProperty(gameObject, newProp.Key, newProp.Value);
                }
            }
        }

        /// <summary>
        /// JsValue的SameValue比较
        /// </summary>
        private bool AreSameValue(JsValue x, JsValue y)
        {
            if (x.Type != y.Type) return false;

            switch (x.Type)
            {
                case Jint.Runtime.Types.Undefined:
                case Jint.Runtime.Types.Null:
                    return true;
                case Jint.Runtime.Types.Boolean:
                    return x.AsBoolean() == y.AsBoolean();
                case Jint.Runtime.Types.Number:
                    var xNum = x.AsNumber();
                    var yNum = y.AsNumber();
                    if (double.IsNaN(xNum) && double.IsNaN(yNum)) return true;
                    if (xNum == 0 && yNum == 0) return true;
                    return xNum == yNum;
                case Jint.Runtime.Types.String:
                    return x.AsString() == y.AsString();
                case Jint.Runtime.Types.Object:
                    return ReferenceEquals(x.AsObject(), y.AsObject());
                default:
                    return false;
            }
        }

        /// <summary>
        /// 获取缓存的VNode
        /// </summary>
        public VNode GetCachedVNode(string nodeId)
        {
            return _vnodeCache.TryGetValue(nodeId, out var vnode) ? vnode : null;
        }

        /// <summary>
        /// 清除所有缓存的VNodes
        /// </summary>
        public void ClearCache()
        {
            foreach (var gameObject in _gameObjectMap.Values)
            {
                if (gameObject != null)
                {
                    UnityEngine.Object.Destroy(gameObject);
                }
            }

            // 清除组件实例的GameObject
            foreach (var vnode in _vnodeCache.Values)
            {
                if (vnode.IsComponent && vnode.ComponentInstance != null)
                {
                    UnityEngine.Object.Destroy(vnode.ComponentInstance);
                }
            }

            _vnodeCache.Clear();
            _gameObjectMap.Clear();
        }

        /// <summary>
        /// 获取虚拟DOM树的字符串表示（用于调试）
        /// </summary>
        public string GetVDOMTree(VNode root, int indent = 0)
        {
            if (root == null) return "";

            var indentStr = new string(' ', indent * 2);
            var result = $"{indentStr}{root.Type}";

            if (!string.IsNullOrEmpty(root.Key))
                result += $" key={root.Key}";

            if (root.IsTextNode)
                result += $" \"{root.TextContent}\"";

            if (root.IsComponent && root.ComponentInstance != null)
                result += $" (Component: {root.ComponentInstance.name})";

            result += "\n";

            foreach (var child in root.Children)
            {
                result += GetVDOMTree(child, indent + 1);
            }

            return result;
        }
    }
}
