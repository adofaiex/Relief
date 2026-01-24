using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime.Interop;
using UnityModManagerNet;

namespace Relief.Console
{
    public class JsConsole
    {
        private readonly UnityModManager.ModEntry.ModLogger _logger;

        // 定义颜色常量
        private const string COLOR_RESET = "</color>";
        private const string COLOR_KEYWORD = "<color=#569CD6>"; // 例如：function, var, const
        private const string COLOR_STRING = "<color=#CE9178>"; // 字符串
        private const string COLOR_NUMBER = "<color=#B5CEA8>"; // 数字
        private const string COLOR_BOOLEAN = "<color=#569CD6>"; // 布尔值
        private const string COLOR_NULL = "<color=#569CD6>"; // null
        private const string COLOR_UNDEFINED = "<color=#569CD6>"; // undefined
        private const string COLOR_OBJECT = "<color=#9CDCFE>"; // 对象名/类型名
        private const string COLOR_PROPERTY_NAME = "<color=#9CDCFE>"; // 对象属性名
        private const string COLOR_BRACKET = "<color=#FFD700>"; // 括号 [] {}
        private const string COLOR_COMMENT = "<color=#6A9955>"; // 注释或额外信息
        private const string COLOR_ERROR = "<color=#F44747>"; // 错误信息
        private const string COLOR_TYPE_NAME = "<color=#4EC9B0>"; // 类型名称，如 Array, Object

        // 格式化限制
        private const int MAX_DEPTH = 5; // 最大递归深度
        private const int MAX_ARRAY_ELEMENTS = 100; // 数组最大显示元素数量
        private const int MAX_OBJECT_PROPERTIES = 50; // 对象最大显示属性数量

        public JsConsole(UnityModManager.ModEntry.ModLogger logger)
        {
            _logger = logger;
        }

        public static bool IsFunction(JsValue value)
        {
            /*
            string typeName = value.ToObject().GetType().Name;
            if (typeName == "FunctionInstance" || typeName == "JsFunction")
                return true;
            return false;*/
            return value is Jint.Native.Function.Function;
        }

        // console.log 支持多参数和对象展开
        public void log(params object[] args)
        {
            var message = ConvertArgsToString(args);
            _logger.Log(message);
        }

        // console.error 映射到LogException
        public void error(params object[] args)
        {
            var message = ConvertArgsToString(args);
            _logger.LogException("JS_Error", new Exception(message));
        }

        // console.warn 映射到Warning
        public void warn(params object[] args)
        {
            var message = ConvertArgsToString(args);
            _logger.Warning(message);
        }

        // console.dir 用于显示对象的属性列表
        public void dir(object obj)
        {
            var message = "Object:\n" + FormatObject(obj, 0);
            _logger.Log(message);
        }

        // console.dirxml 用于显示XML/HTML类结构
        public void dirxml(object obj)
        {
            var message = "XML-like structure:\n" + FormatXmlLike(obj, 0);
            _logger.Log(message);
        }

        // console.debug 等同于log
        public void debug(params object[] args) => log(args);

        // console.info 等同于log
        public void info(params object[] args) => log(args);

        // 转换参数为字符串，支持对象展开
        private string ConvertArgsToString(object[] args)
        {
            if (args == null || args.Length == 0)
                return string.Empty;

            return string.Join(" ", args.Select(arg => FormatValue(arg)));
        }

        // 格式化单个值，区分普通值和对象
        private string FormatValue(object value, int depth = 0)
        {
            if (value == null)
                return $"{COLOR_NULL}null{COLOR_RESET}";

            // 处理Jint的JS值类型
            if (value is JsValue jsValue)
            {
                if (jsValue.IsNull())
                    return $"{COLOR_NULL}null{COLOR_RESET}";
                if (jsValue.IsUndefined())
                    return $"{COLOR_UNDEFINED}undefined{COLOR_RESET}";
                if (jsValue.IsBoolean())
                    return $"{COLOR_BOOLEAN}{jsValue.AsBoolean().ToString().ToLower()}{COLOR_RESET}";
                if (jsValue.IsNumber())
                    return $"{COLOR_NUMBER}{jsValue.AsNumber()}{COLOR_RESET}";
                if (jsValue.IsString())
                    return $"{COLOR_STRING}{EscapeString(jsValue.AsString())}{COLOR_RESET}";
                if (jsValue.IsSymbol())
                    return $"{COLOR_KEYWORD}Symbol{COLOR_RESET}({COLOR_STRING}\"{EscapeString(jsValue.AsString())}\"{COLOR_RESET})";
                if (IsFunction(jsValue))
                    return $"{COLOR_KEYWORD}ƒ{COLOR_RESET} {COLOR_OBJECT}{jsValue.AsFunctionInstance().Get("name")}{COLOR_RESET}()";
                if (jsValue.IsObject())
                    return FormatObject(jsValue.AsObject(), depth);
                if (jsValue.IsArray())
                    return FormatArray(jsValue.AsArray(), depth);
            }

            if (value is string str)
                return $"{COLOR_STRING}{EscapeString(str)}{COLOR_RESET}";
            if (value is bool b)
                return $"{COLOR_BOOLEAN}{b.ToString().ToLower()}{COLOR_RESET}";
            if (value.GetType().IsPrimitive || value is decimal)
                return $"{COLOR_NUMBER}{value}{COLOR_RESET}";
            if (value.GetType().IsEnum)
                return $"{COLOR_KEYWORD}{value.GetType().Name}{COLOR_RESET}.{COLOR_PROPERTY_NAME}{value}{COLOR_RESET}";

            if (value is IEnumerable enumerable && !(value is string))
                return FormatEnumerable(enumerable, depth);

            return FormatObject(value, depth);
        }

        private string FormatArray(JsArray array, int depth)
        {
            if (array.Length == 0)
                return $"{COLOR_TYPE_NAME}Array{COLOR_RESET}{COLOR_BRACKET}[{COLOR_RESET}]";

            if (depth >= MAX_DEPTH)
                return $"{COLOR_TYPE_NAME}Array({array.Length}){COLOR_RESET}{COLOR_BRACKET}[...]{COLOR_RESET}";

            var indent = GetIndent(depth);
            var nextIndent = GetIndent(depth + 1);

            var items = new List<string>();
            for (int i = 0; i < array.Length; i++)
            {
                if (i >= MAX_ARRAY_ELEMENTS)
                {
                    items.Add($"{COLOR_COMMENT}... {array.Length - MAX_ARRAY_ELEMENTS} more ...{COLOR_RESET}");
                    break;
                }
                items.Add(FormatValue(array[i], depth + 1));
            }

            return $"{COLOR_TYPE_NAME}Array({array.Length}){COLOR_RESET} {COLOR_BRACKET}[{COLOR_RESET}\n{nextIndent}{string.Join($",\n{nextIndent}", items)}\n{indent}{COLOR_BRACKET}]{COLOR_RESET}";
        }

        private string FormatEnumerable(IEnumerable enumerable, int depth)
        {
            var items = enumerable.Cast<object>().Select(item => FormatValue(item, depth + 1)).ToList();
            if (!items.Any())
                return $"{COLOR_TYPE_NAME}Array{COLOR_RESET}{COLOR_BRACKET}[{COLOR_RESET}]";

            if (depth >= MAX_DEPTH)
                return $"{COLOR_TYPE_NAME}Array(...){COLOR_RESET}{COLOR_BRACKET}[...]{COLOR_RESET}";

            var indent = GetIndent(depth);
            var nextIndent = GetIndent(depth + 1);


            var limitedItems = items.Take(MAX_ARRAY_ELEMENTS).ToList();
            if (items.Count > MAX_ARRAY_ELEMENTS)
            {
                limitedItems.Add($"{COLOR_COMMENT}... {items.Count - MAX_ARRAY_ELEMENTS} more ...{COLOR_RESET}");
            }

            return $"{COLOR_TYPE_NAME}Array({items.Count}){COLOR_RESET} {COLOR_BRACKET}[{COLOR_RESET}\n{nextIndent}{string.Join($",\n{nextIndent}", limitedItems)}\n{indent}{COLOR_BRACKET}]{COLOR_RESET}";
        }

        private string FormatObject(object obj, int depth)
        {
            if (obj == null)
                return $"{COLOR_NULL}null{COLOR_RESET}";

            if (depth >= MAX_DEPTH)
                return $"{COLOR_TYPE_NAME}{obj.GetType().Name}{COLOR_RESET} {COLOR_BRACKET}{{{COLOR_RESET}...{COLOR_BRACKET}}}{COLOR_RESET}";

            var indent = GetIndent(depth);
            var nextIndent = GetIndent(depth + 1);

            var propStrings = new List<string>();
            string typeName = obj.GetType().Name;

            if (obj is ObjectInstance jsObject)
            {
                typeName = jsObject.GetType().Name;
                var jsProperties = jsObject.GetOwnPropertyKeys()
                    .Select(key => key.AsString())
                    .OrderBy(key => key)
                    .ToList();

                for (int i = 0; i < jsProperties.Count; i++)
                {
                    if (i >= MAX_OBJECT_PROPERTIES)
                    {
                        propStrings.Add($"{COLOR_COMMENT}... {jsProperties.Count - MAX_OBJECT_PROPERTIES} more ...{COLOR_RESET}");
                        break;
                    }

                    var propName = jsProperties[i];
                    var descriptor = jsObject.GetOwnProperty(propName);

                    // 只显示可枚举属性
                    if (descriptor != null && descriptor.Enumerable)
                    {
                        if (descriptor.IsDataDescriptor())
                        {
                            var propValue = descriptor.Value;
                            propStrings.Add($"{COLOR_PROPERTY_NAME}{propName}{COLOR_RESET}: {FormatValue(propValue, depth + 1)}");
                        }
                        else if (descriptor.IsAccessorDescriptor())
                        {
                            var getter = descriptor.Get;
                            var setter = descriptor.Set;
                            string accessorInfo = "";
                            if (getter != null) accessorInfo += "Getter";
                            if (setter != null) accessorInfo += (accessorInfo.Length > 0 ? "/" : "") + "Setter";
                            propStrings.Add($"{COLOR_PROPERTY_NAME}{propName}{COLOR_RESET}: {COLOR_COMMENT}[{accessorInfo}]{COLOR_RESET}");
                        }
                    }
                }
            }

            else
            {
                var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                .Where(p => p.CanRead)
                                .OrderBy(p => p.Name)
                                .ToList();

                for (int i = 0; i < properties.Count; i++)
                {
                    if (i >= MAX_OBJECT_PROPERTIES)
                    {
                        propStrings.Add($"{COLOR_COMMENT}... {properties.Count - MAX_OBJECT_PROPERTIES} more ...{COLOR_RESET}");
                        break;
                    }

                    var prop = properties[i];
                    try
                    {
                        var value = prop.GetValue(obj);
                        propStrings.Add($"{COLOR_PROPERTY_NAME}{prop.Name}{COLOR_RESET}: {FormatValue(value, depth + 1)}");
                    }
                    catch (Exception ex)
                    {
                        propStrings.Add($"{COLOR_PROPERTY_NAME}{prop.Name}{COLOR_RESET}: {COLOR_ERROR}[无法访问: {ex.Message}]{COLOR_RESET}");
                    }
                }
            }

            if (!propStrings.Any())
                return $"{COLOR_TYPE_NAME}{typeName}{COLOR_RESET} {COLOR_BRACKET}{{{COLOR_RESET}{COLOR_BRACKET}}}{COLOR_RESET}";

            return $"{COLOR_TYPE_NAME}{typeName}{COLOR_RESET} {COLOR_BRACKET}{{{COLOR_RESET}\n{nextIndent}{string.Join($",\n{nextIndent}", propStrings)}\n{indent}{COLOR_BRACKET}}}{COLOR_RESET}";
        }

        private string FormatXmlLike(object obj, int depth)
        {
            string xmlString = obj.ToString();

            xmlString = System.Text.RegularExpressions.Regex.Replace(xmlString,
                "<(/?[a-zA-Z][a-zA-Z0-9]*)",
                $"<{COLOR_KEYWORD}$1{COLOR_RESET}");

            return xmlString;
        }

        private string GetIndent(int depth)
        {
            return new string(' ', depth * 4);
        }

        private string EscapeString(string str)
        {
            return str.Replace("\"", "\\\"")
                      .Replace("\n", "\\n")
                      .Replace("\r", "\\r")
                      .Replace("\t", "\\t");
        }
    }
}
