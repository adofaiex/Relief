using System;
using System.Collections.Generic;
using Jint;
using Jint.Native;
using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime.Interop;
using UnityEngine;
using Relief.Console;

namespace Relief.Modules.vm
{
    /// <summary>
    /// React状态管理系统，处理组件状态和重新渲染
    /// </summary>
    public class ReactState
    {
        private readonly Engine _engine;
        private readonly Dictionary<string, ComponentState> _componentStates = new Dictionary<string, ComponentState>();
        private readonly Dictionary<string, List<Effect>> _componentEffects = new Dictionary<string, List<Effect>>();
        private readonly Queue<Action> _renderQueue = new Queue<Action>();
        private string _currentComponentId;
        private int _currentHookIndex;

        public ReactState(Engine engine)
        {
            _engine = engine;
        }

        /// <summary>
        /// 组件状态类
        /// </summary>
        public class ComponentState
        {
            public string ComponentId { get; set; }
            public Dictionary<int, StateHook> StateHooks { get; set; } = new Dictionary<int, StateHook>();
            public Dictionary<int, EffectHook> EffectHooks { get; set; } = new Dictionary<int, EffectHook>();
            public int HookIndex { get; set; } = 0;
            public JsValue RenderFunction { get; set; }
            public JsValue Props { get; set; }
            public bool NeedsRerender { get; set; } = false;
        }

        /// <summary>
        /// State Hook
        /// </summary>
        public class StateHook
        {
            public JsValue Value { get; set; }
            public JsValue SetState { get; set; }
        }

        /// <summary>
        /// Effect Hook
        /// </summary>
        public class EffectHook
        {
            public JsValue Effect { get; set; }
            public JsValue Dependencies { get; set; }
            public JsValue Cleanup { get; set; }
            public bool HasRun { get; set; } = false;
        }

        /// <summary>
        /// Effect类
        /// </summary>
        public class Effect
        {
            public JsValue Function { get; set; }
            public JsValue Dependencies { get; set; }
            public JsValue Cleanup { get; set; }
        }

        /// <summary>
        /// 设置当前组件上下文
        /// </summary>
        public void SetCurrentComponent(string componentId)
        {
            _currentComponentId = componentId;
            if (_componentStates.ContainsKey(componentId))
            {
                _currentHookIndex = 0;
            }
            else
            {
                _componentStates[componentId] = new ComponentState { ComponentId = componentId };
                _currentHookIndex = 0;
            }
        }

        /// <summary>
        /// useState Hook实现
        /// </summary>
        public JsValue UseState(JsValue initialValue)
        {
            if (string.IsNullOrEmpty(_currentComponentId))
            {
                throw new InvalidOperationException("useState must be called within a component");
            }

            var componentState = _componentStates[_currentComponentId];
            var hookIndex = _currentHookIndex++;

            if (!componentState.StateHooks.ContainsKey(hookIndex))
            {
                // 创建setState函数
                var setState = new ClrFunction(_engine, "setState", (thisObj, arguments) =>
                {
                    var newValue = arguments.Length > 0 ? arguments[0] : JsValue.Undefined;

                    // 如果新值是函数，则调用它并传入当前值
                    if (newValue is Jint.Native.Function.Function func)
                    {
                        newValue = func.Call(JsValue.Undefined, new[] { componentState.StateHooks[hookIndex].Value });
                    }

                    componentState.StateHooks[hookIndex].Value = newValue;
                    componentState.NeedsRerender = true;

                    // 添加到重新渲染队列
                    _renderQueue.Enqueue(() => TriggerRerender(_currentComponentId));

                    return JsValue.Undefined;
                });

                componentState.StateHooks[hookIndex] = new StateHook
                {
                    Value = initialValue,
                    SetState = setState
                };
            }

            var stateHook = componentState.StateHooks[hookIndex];
            var result = new JsArray(_engine, new[] { stateHook.Value, stateHook.SetState });
            return result;
        }

        /// <summary>
        /// useEffect Hook实现
        /// </summary>
        public JsValue UseEffect(JsValue effect, JsValue dependencies = null)
        {
            if (string.IsNullOrEmpty(_currentComponentId))
            {
                throw new InvalidOperationException("useEffect must be called within a component");
            }

            var componentState = _componentStates[_currentComponentId];
            var hookIndex = _currentHookIndex++;

            if (!componentState.EffectHooks.ContainsKey(hookIndex))
            {
                componentState.EffectHooks[hookIndex] = new EffectHook
                {
                    Effect = effect.AsFunctionInstance(),
                    Dependencies = dependencies ?? JsValue.Undefined,
                    HasRun = false
                };
            }

            var effectHook = componentState.EffectHooks[hookIndex];

            // 检查依赖是否改变
            bool shouldRun = !effectHook.HasRun || DependenciesChanged(effectHook.Dependencies, dependencies);

            if (shouldRun)
            {
                // 清理之前的effect
                if (effectHook.Cleanup != null)
                {
                    try
                    {
                        effectHook.Cleanup.Call(JsValue.Undefined);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error in effect cleanup: {ex.Message}");
                    }
                }

                // 运行新的effect
                try
                {
                    var result = effectHook.Effect.Call(JsValue.Undefined);
                    if (result is Jint.Native.Function.Function cleanup)
                    {
                        effectHook.Cleanup = cleanup;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in effect: {ex.Message}");
                }

                effectHook.Dependencies = dependencies;
                effectHook.HasRun = true;
            }

            return JsValue.Undefined;
        }

        /// <summary>
        /// 检查依赖是否改变
        /// </summary>
        private bool DependenciesChanged(JsValue oldDeps, JsValue newDeps)
        {
            if (oldDeps.IsUndefined() && newDeps.IsUndefined()) return false;
            if (oldDeps.IsUndefined() || newDeps.IsUndefined()) return true;

            if (oldDeps.IsArray() && newDeps.IsArray())
            {
                var oldArray = oldDeps.AsArray();
                var newArray = newDeps.AsArray();

                if (oldArray.Length != newArray.Length) return true;

                for (int i = 0; i < oldArray.Length; i++)
                {
                    if (!AreSameValue(oldArray.Get(i.ToString()), newArray.Get(i.ToString())))
                    {
                        return true;
                    }
                }
                return false;
            }

            return !AreSameValue(oldDeps, newDeps);
        }

        /// <summary>
        /// Custom implementation of SameValue comparison for JsValue
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
                    if (xNum == 0 && yNum == 0) return true; // +0 and -0 are same
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
        /// 触发组件重新渲染
        /// </summary>
        private void TriggerRerender(string componentId)
        {
            if (_componentStates.TryGetValue(componentId, out var componentState))
            {
                if (componentState.RenderFunction != null && componentState.RenderFunction.IsObject())
                {
                    try
                    {
                        SetCurrentComponent(componentId);
                        var renderFunc = componentState.RenderFunction.AsFunctionInstance();
                        renderFunc?.Call(JsValue.Undefined, new[] { componentState.Props ?? JsValue.Undefined });
                    }
                    catch (Exception ex)
                    {
                        MainClass.Logger.Log($"Error in rerender for {componentId}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 处理渲染队列
        /// </summary>
        public void ProcessRenderQueue()
        {
            while (_renderQueue.Count > 0)
            {
                var renderAction = _renderQueue.Dequeue();
                try
                {
                    renderAction?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing render queue: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 注册组件渲染函数
        /// </summary>
        public void RegisterComponent(string componentId, JsValue renderFunction, JsValue props)
        {
            if (!_componentStates.ContainsKey(componentId))
            {
                _componentStates[componentId] = new ComponentState { ComponentId = componentId };
            }

            var componentState = _componentStates[componentId];
            componentState.RenderFunction = renderFunction;
            componentState.Props = props;
        }

        /// <summary>
        /// 清理组件状态
        /// </summary>
        public void CleanupComponent(string componentId)
        {
            if (_componentStates.TryGetValue(componentId, out var componentState))
            {
                // 清理所有effects
                foreach (var effectHook in componentState.EffectHooks.Values)
                {
                    if (effectHook.Cleanup != null)
                    {
                        try
                        {
                            effectHook.Cleanup.Call(JsValue.Undefined);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Error in effect cleanup during component cleanup: {ex.Message}");
                        }
                    }
                }

                _componentStates.Remove(componentId);
            }

            if (_componentEffects.ContainsKey(componentId))
            {
                _componentEffects.Remove(componentId);
            }
        }

        /// <summary>
        /// 获取组件状态
        /// </summary>
        public ComponentState GetComponentState(string componentId)
        {
            return _componentStates.TryGetValue(componentId, out var state) ? state : null;
        }
    }
}
