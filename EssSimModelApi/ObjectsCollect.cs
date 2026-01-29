using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IEC61850_simulatorServer2.EssSimModelApi
{
    public class ObjectsCollect
    {
        private readonly Dictionary<string, object> _objects = new Dictionary<string, object>();
        private static readonly Lazy<ObjectsCollect> _instance = new Lazy<ObjectsCollect>(() => new ObjectsCollect());

        private ObjectsCollect() {   }

        public static ObjectsCollect Instance => _instance.Value;

        public void AddObjects(string name,Object obj)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("对象名称不能为空或空白", nameof(name));
            }

            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj), "对象不能为null");
            }
            _objects[name] = obj;
        }

        public Object GetObjByName(string name) 
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("对象名称不能为空或空白", nameof(name));
            }
            //object targetObject;
            if (_objects.TryGetValue(name, out var targetObject))
            {
                return targetObject;
            }
            return null;
            //throw new KeyNotFoundException($"未找到名称为 '{name}' 的对象");

        }
    }
}
