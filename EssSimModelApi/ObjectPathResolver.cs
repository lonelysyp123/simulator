using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EssSimulator.EssSimModelApi
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;

    public static class ObjectPathResolver
    {
        /// <summary>
        /// 通过路径（如 "Person.Address.City"）获取嵌套属性值。
        /// </summary>
        public static object GetValue(object target, string path)
        {
            //object current = root;
            //foreach (var segment in ParsePath(path))
            //{
            //    current = GetSegmentValue(current, segment);
            //    if (current == null) return null; // 路径中断
            //}
            //return current;

            if (target == null || string.IsNullOrWhiteSpace(path))
                return null;

            var currentObj = target;
            var segments = path.Split('.');

            foreach (var segment in segments)
            {
                if (currentObj == null)
                    return null;

                // 处理数组/列表索引
                if (segment.Contains("["))
                {
                    var indexStart = segment.IndexOf("[");
                    var indexEnd = segment.IndexOf("]");
                    if (indexStart > 0 && indexEnd > indexStart)
                    {
                        var propName = segment.Substring(0, indexStart);
                        var indexStr = segment.Substring(indexStart + 1, indexEnd - indexStart - 1);

                        if (!int.TryParse(indexStr, out int index))
                            return null;
                                              

                        currentObj = GetIndexedPropertyValue(currentObj, propName, index);
                        continue;
                    }
                }else
                {
                    // 处理普通属性
                    currentObj = GetSimplePropertyValue(currentObj, segment);
                }

               
            }

            return currentObj;
        }

        private static object GetSimplePropertyValue(object target, string propName)
        {
            var type = target.GetType();
            var prop = type.GetProperty(propName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            return prop?.GetValue(target);
        }

        private static object GetIndexedPropertyValue(object target, string propName, int index)
        {
            var collection = GetSimplePropertyValue(target, propName) as IEnumerable;
            if (collection == null)
                return null;

            // 处理IList
            if (collection is IList list)
            {
                return index < list.Count ? list[index] : null;
            }

            if (collection is IDictionary dist)
            {
                return GetDictionaryValue(dist, index.ToString());
            }

            // 处理其他IEnumerable
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

        public static bool SetValue(object target, string path, object value)
        {
            //object current = root;
            //bool isSuccess = false;
            //foreach (var segment in ParsePath(path))
            //{
            //    isSuccess = SetSegmentValue(current, segment, value);
            //}
            //return isSuccess;

            if (target == null || string.IsNullOrWhiteSpace(path))
                return false;

            var segments = path.Split('.');
            var currentObj = target;

            // 处理除最后一段外的所有路径段
            for (int i = 0; i < segments.Length - 1; i++)
            {
                var segment = segments[i];
                currentObj = GetValue(currentObj, segment);
                if (currentObj == null)
                    return false;
            }

            // 设置最后一段的属性值
            var lastSegment = segments.Last();
            return SetSimplePropertyValue(currentObj, lastSegment, value);
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
                // 处理可空类型和类型转换
                var targetType = prop.PropertyType;
                var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

                if (value == null)
                {
                    prop.SetValue(target, null);
                }
                else if (underlyingType.IsEnum)
                {
                    prop.SetValue(target, Enum.Parse(underlyingType, value.ToString()));
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

        /// <summary>
        /// 解析单个路径分段（属性、集合、字典等）。
        /// </summary>
        private static object GetSegmentValue(object target, string segment)
        {
            if (target == null) return null;

            // 处理集合索引（如 "Children[0]"）
            if (segment.EndsWith("]"))
            {
                int bracketStart = segment.IndexOf('[');
                string name = segment.Substring(0, bracketStart);
                string indexer = segment.Substring(bracketStart + 1, segment.Length - bracketStart - 2);

                // 获取集合对象
                object collection = GetPropertyOrFieldValue(target, name);
                if(collection != null)
                {
                    // 根据集合类型访问元素
                    if (collection is IList list)
                    {
                        return list[int.Parse(indexer)]; // 列表索引
                    }
                    else if (collection is IDictionary dict)
                    {
                        return dict[indexer]; // 字典键
                    }
                    else
                    {
                        throw new InvalidOperationException($"不支持的集合类型: {collection?.GetType()}");
                    }
                }else
                {
                    throw new NullReferenceException("映射读取的集合为空");
                }
                
            }
            else
            {
                // 普通属性/字段
                return GetPropertyOrFieldValue(target, segment);
            }
        }

        /// <summary>
        /// 设置值
        /// </summary>
        /// <param name="target"></param>
        /// <param name="segment"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static bool SetSegmentValue(object target, string segment,Object val)
        {
            if (target == null) return false;

            // 处理集合索引（如 "Children[0]"）
            if (segment.EndsWith("]"))
            {
                int bracketStart = segment.IndexOf('[');
                string name = segment.Substring(0, bracketStart);
                string indexer = segment.Substring(bracketStart + 1, segment.Length - bracketStart - 2);

                // 获取集合对象
                object collection = GetPropertyOrFieldValue(target, name);

                // 根据集合类型访问元素
                if (collection is IList list)
                {
                    list[int.Parse(indexer)] = val;
                    return true;
                }
                else if (collection is IDictionary dict)
                {
                    dict[indexer] = val;
                    return true; // 字典键
                }
                else
                {
                    throw new InvalidOperationException($"不支持的集合类型: {collection?.GetType()}");
                }
            }
            else
            {
                // 普通属性/字段
                return SetPropertyOrFieldValue(target, segment,val);
            }
            return false;

        }

        // 在ReflectionHelper类中添加
        private static object GetDictionaryValue(object target, string key)
        {
            var dict = target as IDictionary;
            if (dict == null)
                return null;

            foreach (var dictKey in dict.Keys)
            {
                if (dictKey.ToString().Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    return dict[dictKey];
                }
            }
            return null;
        }

        /// <summary>
        /// 获取对象的属性或字段值。
        /// </summary>
        private static object GetPropertyOrFieldValue(object target, string name)
        {
            Type type = target.GetType();

            // 尝试获取属性
            PropertyInfo property = type.GetProperty(name);
            if (property != null) return property.GetValue(target);

            // 尝试获取字段
            FieldInfo field = type.GetField(name);
            if (field != null) return field.GetValue(target);

            throw new ArgumentException($"未找到属性或字段: {name} (类型: {type.Name})");
        }

        private static bool SetPropertyOrFieldValue(object target, string name,Object val)
        {
            Type type = target.GetType();

        // 尝试获取属性
            PropertyInfo property = type.GetProperty(name);
            if (property != null)
            {
                property.SetValue(target, val);
                return true;
            }

            // 尝试获取字段
            FieldInfo field = type.GetField(name);
            if (field != null)
            {
                field.SetValue(target, val);
                return true;
            }
            return false;
           
        }
    }
}
