using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jint;
using Jint.Native;

namespace Relief.Modules.BuiltIn
{
    public static class TimerModule
    {
        private static int _nextId = 1;
        private static readonly Dictionary<int, CancellationTokenSource> _timers = new Dictionary<int, CancellationTokenSource>();

        public static void Register(Engine engine)
        {
            engine.SetValue("setTimeout", new Func<JsValue, int, int>((callback, delay) =>
            {
                int id = _nextId++;
                var cts = new CancellationTokenSource();
                _timers[id] = cts;

                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(delay, cts.Token);
                        ReliefUnityEvents.Enqueue(() =>
                        {
                            if (!cts.IsCancellationRequested)
                            {
                                callback.Call();
                                _timers.Remove(id);
                            }
                        });
                    }
                    catch (TaskCanceledException) { }
                    catch (Exception ex)
                    {
                        MainClass.Logger.Log($"Error in setTimeout: {ex}");
                    }
                }, cts.Token);

                return id;
            }));

            engine.SetValue("setInterval", new Func<JsValue, int, int>((callback, delay) =>
            {
                int id = _nextId++;
                var cts = new CancellationTokenSource();
                _timers[id] = cts;

                Task.Run(async () =>
                {
                    try
                    {
                        while (!cts.IsCancellationRequested)
                        {
                            await Task.Delay(delay, cts.Token);
                            if (cts.IsCancellationRequested) break;

                            ReliefUnityEvents.Enqueue(() =>
                            {
                                if (!cts.IsCancellationRequested)
                                {
                                    callback.Call();
                                }
                            });
                        }
                    }
                    catch (TaskCanceledException) { }
                    catch (Exception ex)
                    {
                        MainClass.Logger.Log($"Error in setInterval: {ex}");
                    }
                }, cts.Token);

                return id;
            }));

            engine.SetValue("clearTimeout", new Action<int>(id => Clear(id)));
            engine.SetValue("clearInterval", new Action<int>(id => Clear(id)));
        }

        private static void Clear(int id)
        {
            if (_timers.TryGetValue(id, out var cts))
            {
                cts.Cancel();
                _timers.Remove(id);
            }
        }
    }
}
