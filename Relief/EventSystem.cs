using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Jint;
using Jint.Native;
using Jint.Native.Function;
using Jint.Native.Object;

namespace Relief
{
    public class EventSystem
    {
        private readonly Engine _jsEngine;
        private readonly Dictionary<string, List<HandlerEntry>> _eventHandlers = new();
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

        private class HandlerEntry
        {
            public string Id { get; init; } = Guid.NewGuid().ToString("N");
            public int Priority { get; init; }
            public bool Once { get; init; }
            public Delegate CsHandler { get; init; }
            public Function JsFunction { get; init; }
        }

        public EventSystem(Engine jsEngine)
        {
            _jsEngine = jsEngine;
        }

        public bool RegisterEvent(string eventName, Delegate handler)
        {
            if (string.IsNullOrEmpty(eventName) || handler == null) return false;
            AddHandler(eventName, new HandlerEntry { CsHandler = handler, Priority = 0, Once = false });
            return true;
        }

        public string RegisterEvent(string eventName, Delegate handler, int priority = 0, bool once = false, string id = null)
        {
            if (string.IsNullOrEmpty(eventName) || handler == null) return null;
            var entry = new HandlerEntry { CsHandler = handler, Priority = priority, Once = once, Id = id ?? Guid.NewGuid().ToString("N") };
            AddHandler(eventName, entry);
            return entry.Id;
        }

        public bool RegisterEvent(string eventName, JsValue callback)
        {
            if (string.IsNullOrEmpty(eventName)) throw new ArgumentException(nameof(eventName));
            if (callback == null || callback == JsValue.Undefined || callback == JsValue.Null) throw new ArgumentException(nameof(callback));
            if (callback is not Function func) throw new ArgumentException(nameof(callback));
            var entry = new HandlerEntry { JsFunction = func, Priority = 0, Once = false };
            AddHandler(eventName, entry);
            return true;
        }

        public string RegisterEvent(string eventName, JsValue callback, int priority = 0, bool once = false, string id = null)
        {
            if (string.IsNullOrEmpty(eventName)) throw new ArgumentException(nameof(eventName));
            if (callback == null || callback == JsValue.Undefined || callback == JsValue.Null) throw new ArgumentException(nameof(callback));
            if (callback is not Function func) throw new ArgumentException(nameof(callback));
            var entry = new HandlerEntry { JsFunction = func, Priority = priority, Once = once, Id = id ?? Guid.NewGuid().ToString("N") };
            AddHandler(eventName, entry);
            return entry.Id;
        }

        public void UnregisterEvent(string eventName)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            _lock.EnterWriteLock();
            try
            {
                _eventHandlers.Remove(eventName);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool UnregisterEvent(string eventName, string handlerId)
        {
            if (string.IsNullOrEmpty(eventName) || string.IsNullOrEmpty(handlerId)) return false;
            _lock.EnterWriteLock();
            try
            {
                if (_eventHandlers.TryGetValue(eventName, out var list))
                {
                    var removed = list.RemoveAll(h => h.Id == handlerId) > 0;
                    if (list.Count == 0) _eventHandlers.Remove(eventName);
                    return removed;
                }
                return false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void TriggerEvent(string eventName, params object[] args)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            List<HandlerEntry> snapshot = null;
            _lock.EnterReadLock();
            try
            {
                if (_eventHandlers.TryGetValue(eventName, out var list))
                    snapshot = list.OrderByDescending(h => h.Priority).ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
            if (snapshot == null || snapshot.Count == 0) return;
            var toRemove = new List<string>();
            foreach (var entry in snapshot)
            {
                try
                {
                    if (entry.CsHandler != null)
                    {
                        if (entry.CsHandler is Action<object[]> arrAction)
                        {
                            arrAction(args);
                        }
                        else
                        {
                            var method = entry.CsHandler.Method;
                            var parameters = method.GetParameters();
                            if (parameters.Length == 0)
                            {
                                entry.CsHandler.DynamicInvoke();
                            }
                            else if (parameters.Length == args.Length)
                            {
                                entry.CsHandler.DynamicInvoke(args);
                            }
                            else
                            {
                                // 尝试匹配参数数量
                                var newArgs = new object[parameters.Length];
                                for (int i = 0; i < Math.Min(parameters.Length, args.Length); i++)
                                {
                                    newArgs[i] = args[i];
                                }
                                entry.CsHandler.DynamicInvoke(newArgs);
                            }
                        }
                    }
                    else if (entry.JsFunction != null)
                    {
                        var arg = PrepareJsArg(args);
                        var thisArg = entry.JsFunction.Engine.Global;
                        entry.JsFunction.Call(thisArg, arg);
                    }
                }
                catch (Exception ex)
                {
                    LogCallbackError(eventName, ex, args);
                }
                if (entry.Once) toRemove.Add(entry.Id);
            }
            if (toRemove.Count > 0)
            {
                _lock.EnterWriteLock();
                try
                {
                    if (_eventHandlers.TryGetValue(eventName, out var list))
                    {
                        list.RemoveAll(h => toRemove.Contains(h.Id));
                        if (list.Count == 0) _eventHandlers.Remove(eventName);
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        public async Task TriggerEventAsync(string eventName, CancellationToken cancellationToken = default, params object[] args)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            List<HandlerEntry> snapshot = null;
            _lock.EnterReadLock();
            try
            {
                if (_eventHandlers.TryGetValue(eventName, out var list))
                    snapshot = list.OrderByDescending(h => h.Priority).ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
            if (snapshot == null || snapshot.Count == 0) return;
            var toRemove = new List<string>();
            foreach (var entry in snapshot)
            {
                if (cancellationToken.IsCancellationRequested) break;
                try
                {
                    if (entry.CsHandler != null)
                    {
                        if (entry.CsHandler is Func<Task> funcTask)
                        {
                            await funcTask().ConfigureAwait(false);
                        }
                        else if (entry.CsHandler is Func<object[], Task> funcArgsTask)
                        {
                            await funcArgsTask(args).ConfigureAwait(false);
                        }
                        else
                        {
                            var method = entry.CsHandler.Method;
                            var parameters = method.GetParameters();
                            if (parameters.Length == 0)
                            {
                                entry.CsHandler.DynamicInvoke();
                            }
                            else if (parameters.Length == args.Length)
                            {
                                entry.CsHandler.DynamicInvoke(args);
                            }
                            else
                            {
                                // 尝试匹配参数数量
                                var newArgs = new object[parameters.Length];
                                for (int i = 0; i < Math.Min(parameters.Length, args.Length); i++)
                                {
                                    newArgs[i] = args[i];
                                }
                                entry.CsHandler.DynamicInvoke(newArgs);
                            }
                        }
                    }
                    else if (entry.JsFunction != null)
                    {
                        var arg = PrepareJsArg(args);
                        var thisArg = entry.JsFunction.Engine.Global;
                        entry.JsFunction.Call(thisArg, arg);
                    }
                }
                catch (Exception ex)
                {
                    LogCallbackError(eventName, ex, args);
                }
                if (entry.Once) toRemove.Add(entry.Id);
            }
            if (toRemove.Count > 0)
            {
                _lock.EnterWriteLock();
                try
                {
                    if (_eventHandlers.TryGetValue(eventName, out var list))
                    {
                        list.RemoveAll(h => toRemove.Contains(h.Id));
                        if (list.Count == 0) _eventHandlers.Remove(eventName);
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        private JsValue PrepareJsArg(object[] args)
        {
            if (args != null && args.Length == 1 && args[0] is Dictionary<string, object> dict)
                return JsValue.FromObject(_jsEngine, dict);
            return JsValue.FromObject(_jsEngine, args);
        }

        private void LogCallbackError(string eventName, Exception ex, object[] args)
        {
            string argsStr = args == null ? "null" : $"[{string.Join(", ", args.Select(a => a?.ToString() ?? "null"))}]";
            MainClass.Logger.Error($"Arguments: {argsStr}");
            if (ex.InnerException != null)
            {
                MainClass.Logger.Error($"Inner exception: {ex.InnerException.Message}");
                MainClass.Logger.Error($"Inner exception stack trace: {ex.InnerException.StackTrace}");
            }
            MainClass.Logger.Error($"Stack trace: {ex.StackTrace}");
        }

        private void AddHandler(string eventName, HandlerEntry entry)
        {
            _lock.EnterWriteLock();
            try
            {
                if (!_eventHandlers.TryGetValue(eventName, out var list))
                {
                    list = new List<HandlerEntry>();
                    _eventHandlers[eventName] = list;
                }
                list.Add(entry);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}

