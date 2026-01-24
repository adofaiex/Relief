using Jint;
using Jint.Native;
using Relief.Modules.Internal;

namespace Relief.Modules
{
    public static class PathModule
    {
        public static void Register(Engine engine)
        {
            var path = new Path();
            engine.Modules.Add("path", builder => {
                builder.ExportFunction("join", args => JsValue.FromObject(engine, path.join(args)));
                builder.ExportFunction("resolve", args => JsValue.FromObject(engine, path.resolve(args)));
                builder.ExportFunction("basename", args => JsValue.FromObject(engine, path.basename(args[0].AsString())));
                builder.ExportFunction("dirname", args => JsValue.FromObject(engine, path.dirname(args[0].AsString())));
                builder.ExportFunction("extname", args => JsValue.FromObject(engine, path.extname(args[0].AsString())));
                builder.ExportFunction("isAbsolute", args => JsValue.FromObject(engine, path.isAbsolute(args[0].AsString())));
            });
        }
    }
}