using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EssSimulator.EssSimModelApi
{
    public static class ObjectPathResolver
    {
        public static object? GetValue(object? target, string path)
        {
            if (target == null || string.IsNullOrWhiteSpace(path))
                return null;

            object? currentObj = target;
            foreach (var segment in path.Split('.'))
            {
                if (currentObj == null)
                    return null;

                currentObj = ResolveSegment(currentObj, segment);
            }

            return currentObj;
        }

        public static bool SetValue(object target, string path, object value)
        {
            if (target == null || string.IsNullOrWhiteSpace(path))
                return false;

            var segments = path.Split('.');
            var currentObj = target;

            for (int i = 0; i < segments.Length - 1; i++)
            {
                currentObj = GetValue(currentObj, segments[i]);
                if (currentObj == null)
                    return false;
            }

            return SetSimplePropertyValue(currentObj, segments.Last(), value);
        }

        private static object? ResolveSegment(object target, string segment)
        {
            if (segment.Contains('['))
            {
                int indexStart = segment.IndexOf('[');
                int indexEnd = segment.IndexOf(']');
                if (indexStart <= 0 || indexEnd <= indexStart)
                    return null;

                string propName = segment.Substring(0, indexStart);
                string indexStr = segment.Substring(indexStart + 1, indexEnd - indexStart - 1);
                if (!int.TryParse(indexStr, out int index))
                    return null;

                return GetIndexedPropertyValue(target, propName, index);
            }

            return GetSimplePropertyValue(target, segment);
        }

        private static object? GetSimplePropertyValue(object target, string propName)
        {
            var type = target.GetType();
            var prop = type.GetProperty(propName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            return prop?.GetValue(target);
        }

        private static object? GetIndexedPropertyValue(object target, string propName, int index)
        {
            var collection = GetSimplePropertyValue(target, propName) as IEnumerable;
            if (collection == null)
                return null;

            if (collection is IList list)
                return index < list.Count ? list[index] : null;

            if (collection is IDictionary dict)
                return GetDictionaryValue(dict, index.ToString());

            var enumerator = collection.GetEnumerator();
            int currentIndex = 0;
            while (enumerator.MoveNext())
            {
                if (currentIndex == index)
                    return enumerator.Current;
                currentIndex++;
            }

            return null;
        }

        private static bool ToBool(object value)
        {
            return value switch
            {
                bool b => b,
                string s when bool.TryParse(s, out var bv) => bv,
                string s when int.TryParse(s, out var iv) => iv != 0,
                _ => Convert.ToDouble(value) != 0
            };
        }

        private static bool SetSimplePropertyValue(object target, string propName, object value)
        {
            var type = target.GetType();
            var prop = type.GetProperty(propName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (prop == null || !prop.CanWrite)
                return false;

            try
            {
                var targetType = prop.PropertyType;
                var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

                if (value == null)
                {
                    prop.SetValue(target, null);
                }
                else if (underlyingType.IsEnum)
                {
                    prop.SetValue(target, Enum.Parse(underlyingType, value.ToString()!));
                }
                else if (underlyingType == typeof(bool))
                {
                    prop.SetValue(target, ToBool(value));
                }
                else
                {
                    var convertedValue = Convert.ChangeType(value, underlyingType);
                    prop.SetValue(target, convertedValue);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static object? GetDictionaryValue(IDictionary dict, string key)
        {
            foreach (var dictKey in dict.Keys)
            {
                if (dictKey.ToString()?.Equals(key, StringComparison.OrdinalIgnoreCase) == true)
                    return dict[dictKey];
            }

            return null;
        }
    }
}
