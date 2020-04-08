using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Services
{
    class AppOptions
    {
        private class Info
        {
            public readonly Action<string> SetValue;
            public readonly Action SetNullValue;
            public readonly Func<string, bool> VerifyValue;
            public readonly bool IsRequireValue;
            public readonly bool IsAllowMultiple;

            private bool _isFound;
            public bool IsFound => _isFound;
            public void MakeFound() => _isFound = true;

            public Info(Action<string> setValue, Func<string, bool> verifyValue, bool isRequireValue, bool isAllowMultiple)
            {
                SetValue = setValue;
                SetNullValue = () => setValue(null);
                VerifyValue = verifyValue;
                IsRequireValue = isRequireValue;
                IsAllowMultiple = isAllowMultiple;
            }

            public static Info Create(object obj, PropertyInfo prop)
            {
                var valueType = prop.PropertyType;

                if (valueType.IsArray)
                {
                    var elementType = valueType.GetElementType();
                    Action<string> setValue = val =>
                    {
                        var curArray = (Array)prop.GetValue(obj) ?? Array.CreateInstance(elementType, 0);
                        var newArray = Array.CreateInstance(elementType, curArray.Length + 1);
                        Array.Copy(curArray, newArray, curArray.Length);
                        newArray.SetValue(Convert.ChangeType(val, elementType), curArray.Length);
                        prop.SetValue(obj, newArray);
                    };
                    return new Info(setValue, _ => true, true, true);
                }
                else if (valueType == typeof(bool))
                {
                    Action<string> setValue = val => prop.SetValue(obj, bool.TryParse(val ?? "true", out bool result) && result);
                    return new Info(setValue, val => bool.TryParse(val, out var _), false, false);
                }
                else
                {
                    Action<string> setValue = (val) => prop.SetValue(obj, Convert.ChangeType(val, valueType));
                    return new Info(setValue, _ => true, true, false);
                }
            }
        }

        public static bool TryParse<T>(string [] args, T model)
        {
            var infos = new Dictionary<string, Info>();

            foreach (var prop in typeof(T).GetProperties())
            {
                var attrs = prop.GetCustomAttributes(typeof(AppOptionsAttribute), true);
                var info = Info.Create(model, prop);

                foreach (var attr in attrs.OfType<AppOptionsAttribute>())
                {
                    foreach (var key in attr.FullKeys)
                        infos[$"--{key}"] = info;
                    foreach (var key in attr.ShortKeys)
                        infos[$"-{key}"] = info;
                }
            }

            for (int i = 0; i < args.Length; i++)
            {
                string key = args[i];
                if (!infos.TryGetValue(key, out var info))
                {
                    // the key {key} is unknown
                    return false;
                }

                if (info.IsFound && !info.IsAllowMultiple)
                {
                    // the key {key} already has a value
                    return false;
                }

                info.MakeFound();

                if (i + 1 >= args.Length)
                {
                    if (info.IsRequireValue)
                    {
                        // the key {key} requires a value
                        return false;
                    }
                    info.SetNullValue();
                    break;
                }
                
                var value = args[i + 1];
                if (info.VerifyValue(value))
                {
                    info.SetValue(value);
                    i++;
                }
                else
                {
                    info.SetNullValue();
                }
            }

            return true;
        }

        public static void PrintHelp<T>()
        {
            var classAttr = typeof(T).GetCustomAttributes(typeof(AppOptionsAttribute), true).OfType<AppOptionsAttribute>().FirstOrDefault();
            Console.WriteLine(classAttr.Description);
            Console.WriteLine();
            Console.WriteLine("Options:");
            foreach (var prop in typeof(T).GetProperties())
            {
                var attrs = prop.GetCustomAttributes(typeof(AppOptionsAttribute), true);

                foreach (var attr in attrs.OfType<AppOptionsAttribute>())
                {
                    var keys = attr.FullKeys.Select(k => $"--{k}")
                        .Concat(attr.ShortKeys.Select(k => $"-{k}"));
                    Console.WriteLine($"{string.Join(", ", keys).PadRight(16)}  \t{prop.PropertyType.Name}. {attr.Description.TrimStart().TrimEnd('.')}");
                }
            }
            Console.WriteLine();
        }
    }
}
