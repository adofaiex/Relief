using Jint;
using Jint.Native;

namespace Relief.Modules
{
    public static class ProcessModule
    {
        public static void Register(Engine engine)
        {
            var host = new ReliefHostMethods(""); // 或根据需要传递参数
            engine.Modules.Add("process", builder => {
                builder.ExportFunction("cwd", args => JsValue.FromObject(engine, host.processCwd()));
                builder.ExportFunction("uptime", args => JsValue.FromObject(engine, host.processUptime()));
            });
        }
    }
}