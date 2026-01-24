using Jint.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jint;

namespace Relief.Modules
{
    [JavascriptType]
    public class ReliefHostMethods
    {
        private readonly string _scriptDir;

        public ReliefHostMethods(string scriptDir)
        {
            _scriptDir = scriptDir;
        }

        
        public string readFileSync(string filePath, string encoding = "utf8")
        {
            try
            {
                return File.ReadAllText(System.IO.Path.Combine(_scriptDir, filePath), encoding.ToLower() == "utf8" ? System.Text.Encoding.UTF8 : System.Text.Encoding.GetEncoding(encoding));
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error reading file {filePath}: {ex.Message}");
                throw ex;
            }
        }

        public void readFile(string filePath, string encoding, Action<string, string> callback)
        {
            try
            {
                string content = File.ReadAllText(System.IO.Path.Combine(_scriptDir, filePath), encoding.ToLower() == "utf8" ? System.Text.Encoding.UTF8 : System.Text.Encoding.GetEncoding(encoding));
                callback(null, content);
            }
            catch (Exception ex)
            {
                callback(ex.Message, null);
            }
        }

        public void readFile(string filePath, Action<string, string> callback)
        {
            readFile(filePath, "utf8", callback);
        }

        public bool writeFileSync(string filePath, string data, string encoding = "utf8")
        {
            try
            {
                File.WriteAllText(System.IO.Path.Combine(_scriptDir, filePath), data, encoding.ToLower() == "utf8" ? System.Text.Encoding.UTF8 : System.Text.Encoding.GetEncoding(encoding));
                return true;
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error writing file {filePath}: {ex.Message}");
                throw ex;
            }
        }

        public void writeFile(string filePath, string data, string encoding, Action<string> callback)
        {
            try
            {
                File.WriteAllText(System.IO.Path.Combine(_scriptDir, filePath), data, encoding.ToLower() == "utf8" ? System.Text.Encoding.UTF8 : System.Text.Encoding.GetEncoding(encoding));
                callback(null);
            }
            catch (Exception ex)
            {
                callback(ex.Message);
            }
        }

        public void writeFile(string filePath, string data, Action<string> callback)
        {
            writeFile(filePath, data, "utf8", callback);
        }

        public bool existsSync(string filePath)
        {
            return File.Exists(System.IO.Path.Combine(_scriptDir, filePath)) || Directory.Exists(System.IO.Path.Combine(_scriptDir, filePath));
        }

        public bool mkdirSync(string dirPath)
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.Combine(_scriptDir, dirPath));
                return true;
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error creating directory {dirPath}: {ex.Message}");
                return false;
            }
        }

        public JsValue readdirSync(string dirPath)
        {
            try
            {
                var entries = Directory.GetFileSystemEntries(Path.Combine(_scriptDir, dirPath));
                return JsValue.FromObject(MainClass.engine, entries.Select(e => Path.GetFileName(e)).ToArray());
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error reading directory {dirPath}: {ex.Message}");
                return JsValue.Undefined;
            }
        }

        public bool unlinkSync(string filePath)
        {
            try
            {
                File.Delete(Path.Combine(_scriptDir, filePath));
                return true;
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error deleting file {filePath}: {ex.Message}");
                return false;
            }
        }

        public bool rmdirSync(string dirPath)
        {
            try
            {
                Directory.Delete(Path.Combine(_scriptDir, dirPath), true);
                return true;
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error deleting directory {dirPath}: {ex.Message}");
                return false;
            }
        }

        public string pathJoin(params JsValue[] paths)
        {
            try
            {
                return Path.Combine(paths.Select(p => p.AsString()).ToArray());
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error joining paths: {ex.Message}");
                return null;
            }
        }

        public string pathResolve(params JsValue[] paths)
        {
            try
            {
                return Path.GetFullPath(Path.Combine(paths.Select(p => p.AsString()).ToArray()));
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error resolving paths: {ex.Message}");
                return null;
            }
        }

        public string pathBasename(string path)
        {
            try
            {
                return Path.GetFileName(path);
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error getting basename: {ex.Message}");
                return null;
            }
        }

        public string pathDirname(string path)
        {
            try
            {
                return Path.GetDirectoryName(path);
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error getting dirname: {ex.Message}");
                return null;
            }
        }

        public string pathExtname(string path)
        {
            try
            {
                return Path.GetExtension(path);
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error getting extname: {ex.Message}");
                return null;
            }
        }

        public bool pathIsAbsolute(string path)
        {
            try
            {
                return Path.IsPathRooted(path);
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error checking if path is absolute: {ex.Message}");
                return false;
            }
        }

        // Process methods
        public string processCwd()
        {
            return Environment.CurrentDirectory;
        }

        public JsValue processEnv(Jint.Engine engine)
        {
            return JsValue.Null;
        }

        public string processPlatform()
        {
            return Environment.OSVersion.Platform.ToString();
        }

        public string processVersion()
        {
            return Environment.Version.ToString();
        }

        public string processArch()
        {
            return Environment.Is64BitProcess ? "x64" : "x86";
        }

        public int processPid()
        {
            return System.Diagnostics.Process.GetCurrentProcess().Id;
        }

        public double processUptime()
        {
            TimeSpan uptime = DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime;
            return uptime.TotalSeconds;
        }
    }
}

