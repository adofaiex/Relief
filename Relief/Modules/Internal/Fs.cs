using Jint.Native;
using System;
using System.IO;
using System.Linq;
using Relief;

namespace Relief.Modules.Internal
{
    [JavascriptType]
    public class Fs
    {
        private readonly string _scriptDir;

        public Fs(string scriptDir)
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
                var entries = Directory.GetFileSystemEntries(System.IO.Path.Combine(_scriptDir, dirPath));
                return JsValue.FromObject(MainClass.engine, entries.Select(e => System.IO.Path.GetFileName(e)).ToArray());
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
                File.Delete(System.IO.Path.Combine(_scriptDir, filePath));
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
                Directory.Delete(System.IO.Path.Combine(_scriptDir, dirPath), true);
                return true;
            }
            catch (Exception ex)
            {
                MainClass.Logger.Log($"Error deleting directory {dirPath}: {ex.Message}");
                return false;
            }
        }
    }
}
