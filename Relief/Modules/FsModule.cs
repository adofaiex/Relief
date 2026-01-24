using Jint;
using Jint.Native;
using Relief.Modules.Internal;

namespace Relief.Modules
{
    public static class FsModule
    {
        public static void Register(Engine engine, string scriptDir)
        {
            var fs = new Fs(scriptDir);
            engine.Modules.Add("fs", builder => {
                builder.ExportFunction("readFileSync", args => JsValue.FromObject(engine, fs.readFileSync(args[0].AsString(), args.Length > 1 ? args[1].AsString() : "utf8")));
                builder.ExportFunction("writeFileSync", args => JsValue.FromObject(engine, fs.writeFileSync(args[0].AsString(), args[1].AsString(), args.Length > 2 ? args[2].AsString() : "utf8")));
                builder.ExportFunction("existsSync", args => JsValue.FromObject(engine, fs.existsSync(args[0].AsString())));
                builder.ExportFunction("mkdirSync", args => JsValue.FromObject(engine, fs.mkdirSync(args[0].AsString())));
                builder.ExportFunction("readdirSync", args => JsValue.FromObject(engine, fs.readdirSync(args[0].AsString())));
                builder.ExportFunction("unlinkSync", args => JsValue.FromObject(engine, fs.unlinkSync(args[0].AsString())));
                builder.ExportFunction("rmdirSync", args => JsValue.FromObject(engine, fs.rmdirSync(args[0].AsString())));
            });
        }
    }
}