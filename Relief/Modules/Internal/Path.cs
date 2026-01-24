using System;
using System.IO;
using Jint.Native;
using System.Linq;
using Jint;
using Relief;

namespace Relief.Modules.Internal
{
    [JavascriptType]
    public class Path
    {

        public string join(params JsValue[] paths)
        {
            try
            {
                return System.IO.Path.Combine(paths.Select(p => p.AsString()).ToArray());
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error joining paths: {ex.Message}");
                return null;
            }
        }

        public string resolve(params JsValue[] paths)
        {
            try
            {
                return System.IO.Path.GetFullPath(System.IO.Path.Combine(paths.Select(p => p.AsString()).ToArray()));
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error resolving paths: {ex.Message}");
                return null;
            }
        }

        public string basename(string path)
        {
            try
            {
                return System.IO.Path.GetFileName(path);
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error getting basename: {ex.Message}");
                return null;
            }
        }

        public string dirname(string path)
        {
            try
            {
                return System.IO.Path.GetDirectoryName(path);
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error getting dirname: {ex.Message}");
                return null;
            }
        }

        public string extname(string path)
        {
            try
            {
                return System.IO.Path.GetExtension(path);
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error getting extname: {ex.Message}");
                return null;
            }
        }

        public bool isAbsolute(string path)
        {
            try
            {
                return System.IO.Path.IsPathRooted(path);
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error checking if path is absolute: {ex.Message}");
                return false;
            }
        }
    }
}
