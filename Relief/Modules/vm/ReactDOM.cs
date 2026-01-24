using System;
using System.Collections.Generic;
using Jint;
using Jint.Native;
using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime.Interop;
using UnityEngine;

namespace Relief.Modules.vm
{
    /// <summary>
    /// React DOM implementation for Unity
    /// Provides createRoot, render, and other DOM manipulation methods
    /// </summary>
    public class ReactDOM
    {
        private readonly Engine _engine;
        private readonly ReactUnity _reactUnity;
        private readonly Dictionary<string, ReactUnity.Root> _roots = new Dictionary<string, ReactUnity.Root>();

        public ReactDOM(Engine engine, ReactUnity reactUnity)
        {
            _engine = engine;
            _reactUnity = reactUnity;
            RegisterMethods();
        }

        private void RegisterMethods()
        {
            _engine.SetValue("ReactUnity", this);
        }

        /// <summary>
        /// Create a React root for the given container
        /// </summary>
        public JsValue CreateRoot(JsValue containerElement)
        {
            try
            {
                GameObject container = null;

                if (containerElement.IsObject() && containerElement.AsObject().HasProperty("gameObject"))
                {
                    string gameObjectId = containerElement.AsObject().Get("gameObject").AsString();
                    container = _reactUnity.GetVirtualDOM().GetCachedVNode(gameObjectId)?.ComponentInstance;
                }
                else if (containerElement.IsString())
                {
                    // Find GameObject by name
                    container = GameObject.Find(containerElement.AsString());
                }

                if (container == null)
                {
                    // Create a new container GameObject
                    container = new GameObject("ReactRoot");
                }

                var root = _reactUnity.CreateRoot(container);
                var rootId = root.GetRootId();
                _roots[rootId] = root;

                // Return a JavaScript object that represents the root
                var rootObject = new JsObject(_engine);
                rootObject.Set("_rootId", rootId);

                // Add render method to the root object
                var renderMethod = new ClrFunction(_engine, "render", (thisObj, arguments) =>
                {
                    if (arguments.Length > 0)
                    {
                        root.render(arguments[0]);
                    }
                    return JsValue.Undefined;
                });

                rootObject.Set("render", renderMethod);

                // Add unmount method
                var unmountMethod = new ClrFunction(_engine, "unmount", (thisObj, arguments) =>
                {
                    try
                    {
                        _roots.Remove(rootId);
                        if (container != null)
                        {
                            UnityEngine.Object.Destroy(container);
                        }
                        return JsValue.Undefined;
                    }
                    catch (Exception ex)
                    {
                        MainClass.Logger.Log($"Error unmounting root: {ex.Message}");
                        return JsValue.Undefined;
                    }
                });

                rootObject.Set("unmount", unmountMethod);

                return rootObject;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating React root: {ex.Message}");
                return JsValue.Undefined;
            }
        }

        /// <summary>
        /// Legacy render method (React 17 style)
        /// </summary>
        public JsValue Render(JsValue element, JsValue container, JsValue callback = null)
        {
            try
            {
                var root = CreateRoot(container);
                if (root.IsObject())
                {
                    var renderMethod = root.AsObject().Get("render").AsFunctionInstance();
                    var result = renderMethod.Call(root, new[] { element });

                    // Call callback if provided
                    if (callback != null && callback.IsObject() && callback.AsObject() is Function callbackFunc)
                    {
                        callbackFunc.Call(JsValue.Undefined);
                    }

                    return result;
                }
                return JsValue.Undefined;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in legacy render: {ex.Message}");
                return JsValue.Undefined;
            }
        }

        /// <summary>
        /// Unmount component at node
        /// </summary>
        public bool UnmountComponentAtNode(JsValue container)
        {
            try
            {
                // Find and unmount the root associated with this container
                foreach (var kvp in _roots)
                {
                    var root = kvp.Value;
                    // Check if this root's container matches
                    // This is a simplified check - in a real implementation you'd want more robust matching
                    if (root.GetContainer() != null)
                    {
                        UnityEngine.Object.Destroy(root.GetContainer());
                        _roots.Remove(kvp.Key);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error unmounting component: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Find DOM node (Unity GameObject) for a component
        /// </summary>
        public JsValue FindDOMNode(JsValue component)
        {
            try
            {
                // This would need to be implemented based on your component system
                // For now, return undefined
                Debug.LogWarning("FindDOMNode not fully implemented");
                return JsValue.Undefined;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error finding DOM node: {ex.Message}");
                return JsValue.Undefined;
            }
        }

        /// <summary>
        /// Create portal (render children into a different part of the DOM tree)
        /// </summary>
        public JsValue CreatePortal(JsValue children, JsValue container)
        {
            try
            {
                // Create a portal element that will be handled specially during rendering
                var portalElement = new JsObject(_engine);
                portalElement.Set("$$typeof", "react.portal");
                portalElement.Set("children", children);
                portalElement.Set("containerInfo", container);
                portalElement.Set("key", JsValue.Null);

                return portalElement;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating portal: {ex.Message}");
                return JsValue.Undefined;
            }
        }

        /// <summary>
        /// Flush sync (force synchronous rendering)
        /// </summary>
        public void FlushSync(JsValue callback = null)
        {
            try
            {
                // Process any pending state updates
                _reactUnity.GetReactState().ProcessRenderQueue();

                // Call callback if provided
                if (callback != null && callback.IsObject() && callback.AsObject() is Function callbackFunc)
                {
                    callbackFunc.Call(JsValue.Undefined);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in flushSync: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all active roots
        /// </summary>
        public IEnumerable<ReactUnity.Root> GetAllRoots()
        {
            return _roots.Values;
        }

        /// <summary>
        /// Cleanup all roots
        /// </summary>
        public void Cleanup()
        {
            foreach (var root in _roots.Values)
            {
                if (root.GetContainer() != null)
                {
                    UnityEngine.Object.Destroy(root.GetContainer());
                }
            }
            _roots.Clear();
        }
    }
}
