using System;
using System.Collections.Generic;
using System.Linq;
using Jint;
using Jint.Native;
using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime.Interop;
using UnityEngine;

namespace Relief.Modules.vm
{
    /// <summary>
    /// React组件生命周期管理器
    /// </summary>
    public class ReactComponent
    {
        private readonly Engine _engine;
        private readonly ReactState _reactState;
        private readonly Dictionary<string, ComponentInstance> _components = new Dictionary<string, ComponentInstance>();
        private readonly Dictionary<string, ComponentDefinition> _componentDefinitions = new Dictionary<string, ComponentDefinition>();

        public ReactComponent(Engine engine, ReactState reactState)
        {
            _engine = engine;
            _reactState = reactState;
        }

        /// <summary>
        /// 组件定义
        /// </summary>
        public class ComponentDefinition
        {
            public string Name { get; set; }
            public JsValue Constructor { get; set; }
            public JsValue Render { get; set; }
            public JsValue ComponentDidMount { get; set; }
            public JsValue ComponentDidUpdate { get; set; }
            public JsValue ComponentWillUnmount { get; set; }
            public JsValue GetDerivedStateFromProps { get; set; }
            public JsValue ShouldComponentUpdate { get; set; }
            public bool IsClassComponent { get; set; }
        }

        /// <summary>
        /// 组件实例
        /// </summary>
        public class ComponentInstance
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public ComponentDefinition Definition { get; set; }
            public JsValue Props { get; set; }
            public JsValue State { get; set; }
            public JsValue Context { get; set; }
            public JsValue RenderedElement { get; set; }
            public ComponentLifecyclePhase Phase { get; set; }
            public bool IsMounted { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime LastUpdated { get; set; }
            public JsValue ComponentObject { get; set; } // For class components
        }

        /// <summary>
        /// 组件生命周期阶段
        /// </summary>
        public enum ComponentLifecyclePhase
        {
            Created,
            Mounting,
            Mounted,
            Updating,
            Updated,
            Unmounting,
            Unmounted
        }

        /// <summary>
        /// 注册组件定义
        /// </summary>
        public void RegisterComponentDefinition(string name, JsValue componentDefinition)
        {
            try
            {
                var definition = new ComponentDefinition
                {
                    Name = name,
                    IsClassComponent = false
                };

                if (componentDefinition.IsObject() && componentDefinition.AsObject() is JsValue func)
                {
                    // Function component
                    definition.Render = func;
                    definition.IsClassComponent = false;
                }
                else if (componentDefinition.IsObject())
                {
                    // Class component
                    var obj = componentDefinition.AsObject();
                    definition.IsClassComponent = true;

                    if (obj.HasProperty("constructor"))
                        definition.Constructor = obj.Get("constructor").AsFunctionInstance();
                    if (obj.HasProperty("render"))
                        definition.Render = obj.Get("render").AsFunctionInstance();
                    if (obj.HasProperty("componentDidMount"))
                        definition.ComponentDidMount = obj.Get("componentDidMount").AsFunctionInstance();
                    if (obj.HasProperty("componentDidUpdate"))
                        definition.ComponentDidUpdate = obj.Get("componentDidUpdate").AsFunctionInstance();
                    if (obj.HasProperty("componentWillUnmount"))
                        definition.ComponentWillUnmount = obj.Get("componentWillUnmount").AsFunctionInstance();
                    if (obj.HasProperty("getDerivedStateFromProps"))
                        definition.GetDerivedStateFromProps = obj.Get("getDerivedStateFromProps").AsFunctionInstance();
                    if (obj.HasProperty("shouldComponentUpdate"))
                        definition.ShouldComponentUpdate = obj.Get("shouldComponentUpdate").AsFunctionInstance();
                }

                _componentDefinitions[name] = definition;
                Debug.Log($"Registered component definition: {name}");
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error registering component {name}: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建组件实例
        /// </summary>
        public ComponentInstance CreateComponent(string name, JsValue props, JsValue context = null)
        {
            try
            {
                if (!_componentDefinitions.TryGetValue(name, out var definition))
                {
                    Debug.LogError($"Component definition not found: {name}");
                    return null;
                }

                var instance = new ComponentInstance
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Definition = definition,
                    Props = props ?? JsValue.Undefined,
                    Context = context ?? JsValue.Undefined,
                    Phase = ComponentLifecyclePhase.Created,
                    CreatedAt = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    IsMounted = false
                };

                if (definition.IsClassComponent)
                {
                    // Create class component instance
                    instance.ComponentObject = CreateClassComponentInstance(definition, props);
                    instance.State = GetComponentState(instance.ComponentObject);
                }
                else
                {
                    // Function component uses React hooks for state
                    instance.State = JsValue.Undefined;
                }

                _components[instance.Id] = instance;

                // Set component context for hooks
                _reactState.SetCurrentComponent(instance.Id);

                Debug.Log($"Created component instance: {name} ({instance.Id})");
                return instance;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating component {name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 创建类组件实例
        /// </summary>
        private JsValue CreateClassComponentInstance(ComponentDefinition definition, JsValue props)
        {
            try
            {
                if (definition.Constructor != null)
                {
                    var componentInstance = definition.Constructor.Call(new[] { props });
                    return componentInstance;
                }
                else
                {
                    // Create basic component object
                    var componentObj = new JsObject(_engine);
                    componentObj.Set("props", props);
                    componentObj.Set("state", new JsObject(_engine));

                    // Add setState method
                    var setState = new ClrFunction(_engine, "setState", (thisObj, arguments) =>
                    {
                        if (arguments.Length > 0)
                        {
                            var newState = arguments[0];
                            var currentState = componentObj.Get("state").AsObject();

                            if (newState.IsObject())
                            {
                                var newStateObj = newState.AsObject();
                                foreach (var prop in newStateObj.GetOwnProperties())
                                {
                                    currentState.Set(prop.Key, prop.Value.Value);
                                }
                            }

                            // Trigger update
                            TriggerComponentUpdate(componentObj);
                        }
                        return JsValue.Undefined;
                    });

                    componentObj.Set("setState", setState);
                    return componentObj;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating class component instance: {ex.Message}");
                return JsValue.Undefined;
            }
        }

        /// <summary>
        /// 获取组件状态
        /// </summary>
        private JsValue GetComponentState(JsValue componentObject)
        {
            if (componentObject.IsObject() && componentObject.AsObject().HasProperty("state"))
            {
                return componentObject.AsObject().Get("state");
            }
            return JsValue.Undefined;
        }

        /// <summary>
        /// 挂载组件
        /// </summary>
        public JsValue MountComponent(ComponentInstance instance)
        {
            try
            {
                if (instance.IsMounted)
                {
                    Debug.LogWarning($"Component {instance.Name} is already mounted");
                    return instance.RenderedElement;
                }

                instance.Phase = ComponentLifecyclePhase.Mounting;

                // Set component context for hooks
                _reactState.SetCurrentComponent(instance.Id);

                JsValue renderedElement = JsValue.Undefined;

                if (instance.Definition.IsClassComponent)
                {
                    // Class component lifecycle

                    // getDerivedStateFromProps (static method)
                    if (instance.Definition.GetDerivedStateFromProps != null)
                    {
                        var derivedState = instance.Definition.GetDerivedStateFromProps.Call(
                            JsValue.Undefined,
                            new[] { instance.Props, instance.State }
                        );

                        if (!derivedState.IsNull() && derivedState.IsObject())
                        {
                            // Update state with derived state
                            var currentState = instance.State.AsObject();
                            var derivedStateObj = derivedState.AsObject();
                            foreach (var prop in derivedStateObj.GetOwnProperties())
                            {
                                currentState.Set(prop.Key, prop.Value.Value);
                            }
                        }
                    }

                    // render
                    if (instance.Definition.Render != null)
                    {
                        renderedElement = instance.Definition.Render.Call(
                            instance.ComponentObject,
                            new JsValue[0]
                        );
                    }
                }
                else
                {
                    // Function component
                    if (instance.Definition.Render != null)
                    {
                        renderedElement = instance.Definition.Render.Call(
                            JsValue.Undefined,
                            new[] { instance.Props }
                        );
                    }
                }

                instance.RenderedElement = renderedElement;
                instance.Phase = ComponentLifecyclePhase.Mounted;
                instance.IsMounted = true;
                instance.LastUpdated = DateTime.Now;

                // componentDidMount
                if (instance.Definition.IsClassComponent && instance.Definition.ComponentDidMount != null)
                {
                    try
                    {
                        instance.Definition.ComponentDidMount.Call(instance.ComponentObject, new JsValue[0]);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error in componentDidMount for {instance.Name}: {ex.Message}");
                    }
                }

                Debug.Log($"Mounted component: {instance.Name} ({instance.Id})");
                return renderedElement;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error mounting component {instance.Name}: {ex.Message}");
                instance.Phase = ComponentLifecyclePhase.Unmounted;
                return JsValue.Undefined;
            }
        }

        /// <summary>
        /// 更新组件
        /// </summary>
        public JsValue UpdateComponent(ComponentInstance instance, JsValue newProps)
        {
            try
            {
                if (!instance.IsMounted)
                {
                    Debug.LogWarning($"Cannot update unmounted component {instance.Name}");
                    return instance.RenderedElement;
                }

                var prevProps = instance.Props;
                var prevState = instance.State;

                instance.Phase = ComponentLifecyclePhase.Updating;
                instance.Props = newProps ?? instance.Props;

                // Set component context for hooks
                _reactState.SetCurrentComponent(instance.Id);

                bool shouldUpdate = true;

                if (instance.Definition.IsClassComponent)
                {
                    // getDerivedStateFromProps
                    if (instance.Definition.GetDerivedStateFromProps != null)
                    {
                        var derivedState = instance.Definition.GetDerivedStateFromProps.Call(
                            JsValue.Undefined,
                            new[] { instance.Props, instance.State }
                        );

                        if (!derivedState.IsNull() && derivedState.IsObject())
                        {
                            var currentState = instance.State.AsObject();
                            var derivedStateObj = derivedState.AsObject();
                            foreach (var prop in derivedStateObj.GetOwnProperties())
                            {
                                currentState.Set(prop.Key, prop.Value.Value);
                            }
                        }
                    }

                    // shouldComponentUpdate
                    if (instance.Definition.ShouldComponentUpdate != null)
                    {
                        var shouldUpdateResult = instance.Definition.ShouldComponentUpdate.Call(
                            instance.ComponentObject,
                            new[] { instance.Props, instance.State }
                        );
                        shouldUpdate = shouldUpdateResult.AsBoolean();
                    }
                }

                if (shouldUpdate)
                {
                    JsValue renderedElement = JsValue.Undefined;

                    if (instance.Definition.IsClassComponent)
                    {
                        if (instance.Definition.Render != null)
                        {
                            renderedElement = instance.Definition.Render.Call(
                                instance.ComponentObject,
                                new JsValue[0]
                            );
                        }
                    }
                    else
                    {
                        if (instance.Definition.Render != null)
                        {
                            renderedElement = instance.Definition.Render.Call(
                                JsValue.Undefined,
                                new[] { instance.Props }
                            );
                        }
                    }

                    instance.RenderedElement = renderedElement;
                    instance.Phase = ComponentLifecyclePhase.Updated;
                    instance.LastUpdated = DateTime.Now;

                    // componentDidUpdate
                    if (instance.Definition.IsClassComponent && instance.Definition.ComponentDidUpdate != null)
                    {
                        try
                        {
                            instance.Definition.ComponentDidUpdate.Call(
                                instance.ComponentObject,
                                new[] { prevProps, prevState }
                            );
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Error in componentDidUpdate for {instance.Name}: {ex.Message}");
                        }
                    }

                    Debug.Log($"Updated component: {instance.Name} ({instance.Id})");
                    return renderedElement;
                }
                else
                {
                    Debug.Log($"Component update skipped: {instance.Name} ({instance.Id})");
                    return instance.RenderedElement;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error updating component {instance.Name}: {ex.Message}");
                return instance.RenderedElement;
            }
        }

        /// <summary>
        /// 卸载组件
        /// </summary>
        public void UnmountComponent(ComponentInstance instance)
        {
            try
            {
                if (!instance.IsMounted)
                {
                    Debug.LogWarning($"Component {instance.Name} is already unmounted");
                    return;
                }

                instance.Phase = ComponentLifecyclePhase.Unmounting;

                // componentWillUnmount
                if (instance.Definition.IsClassComponent && instance.Definition.ComponentWillUnmount != null)
                {
                    try
                    {
                        instance.Definition.ComponentWillUnmount.Call(instance.ComponentObject, new JsValue[0]);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error in componentWillUnmount for {instance.Name}: {ex.Message}");
                    }
                }

                // Clean up React state (hooks)
                _reactState.CleanupComponent(instance.Id);

                instance.Phase = ComponentLifecyclePhase.Unmounted;
                instance.IsMounted = false;
                instance.RenderedElement = JsValue.Undefined;

                Debug.Log($"Unmounted component: {instance.Name} ({instance.Id})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error unmounting component {instance.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// 触发组件更新
        /// </summary>
        private void TriggerComponentUpdate(JsValue componentObject)
        {
            // Find component instance by component object
            foreach (var instance in _components.Values)
            {
                if (instance.ComponentObject == componentObject)
                {
                    UpdateComponent(instance, instance.Props);
                    break;
                }
            }
        }

        /// <summary>
        /// 获取组件实例
        /// </summary>
        public ComponentInstance GetComponent(string componentId)
        {
            return _components.TryGetValue(componentId, out var instance) ? instance : null;
        }

        /// <summary>
        /// 获取所有组件实例
        /// </summary>
        public IEnumerable<ComponentInstance> GetAllComponents()
        {
            return _components.Values;
        }

        /// <summary>
        /// 清理所有组件
        /// </summary>
        public void Cleanup()
        {
            foreach (var instance in _components.Values.ToArray())
            {
                UnmountComponent(instance);
            }
            _components.Clear();
            _componentDefinitions.Clear();
        }
    }
}
