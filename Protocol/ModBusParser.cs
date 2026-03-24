using System.Data;
using log4net;

namespace EssSimulator
{
    /// <summary>
    /// ModBus协议解析器
    /// @author : ljw
    /// @date : 2024/04/26
    /// </summary>
    public class ModbusParser
    {
        private List<MapEntry[]> modbusPointMap;
        ILog log = LogManager.GetLogger(typeof(ModbusParser));
        public ModbusParser(List<MapEntry[]> modbusPointMap)
        {
            this.modbusPointMap = modbusPointMap;
        }

        /// <summary>
        /// 数据下发转换
        /// </summary>
        /// <param name="models">下发的参数名与值对应关系的Dictionary<string, object></param>
        /// <returns>参数名与字节数组对应关系的Dictionary</returns>
        public Dictionary<string, byte[]> DataEncryption(Dictionary<string, object> models)
        {
            Dictionary<string, byte[]> reslut = new Dictionary<string, byte[]>();
            object value = 0;

            foreach (var item in models)
            {
                var point = FindPointByName(item.Key);

                string typeofPoint = "";
                // 当点位的size位32时，类型强制转为System.Int32，当size为16时，类型强制转为System.Int16，当size为1时，类型强制转为System.Boolean
                if (point.Size == 32)
                {
                    typeofPoint = "System.Int32";
                }
                else if (point.Size == 16)
                {
                    typeofPoint = "System.Int16";
                }
                else if (point.Size == 1)
                {
                    typeofPoint = "System.Boolean";
                }

                if (string.IsNullOrEmpty(typeofPoint)) throw new NullReferenceException("type is null");
                Type? type = Type.GetType(typeofPoint);
                if (type == null) throw new Exception("type is error");
                if (item.Value == null) throw new NullReferenceException("value is null");
                double actualValue = double.Parse(item.Value.ToString()!);

                value = Convert.ChangeType(actualValue, type);

                byte[] resultBytes = Common.DataUnTranslation(value, typeofPoint);
                //GetBytes方法默认转换成小端序，低位在前，所以要反转
                Array.Reverse(resultBytes);
                reslut.Add(item.Key, resultBytes);
            }
            return reslut;
        }

        /// <summary>
        /// 数据解析
        /// </summary>
        /// <param name="originalData">参数名与读取上来的字节数组对应关系</param>
        /// <returns>参数名与解析后的实际值对应关系的Dictionary<string, object></returns>
        public Dictionary<string, object> DataParse(Dictionary<string, object> originalData)
        {
            try
            {
                var ResultArray = new Dictionary<string, object>();
                foreach (var item in originalData)
                {
                    byte[]? data = item.Value as byte[];
                    // TODO: s 待优化，应该让modbusPointMap和数据有一个共同的索引，不用每次都遍历整个map
                    MapEntry? point = FindPointByName(item.Key);

                    string typeofPoint = "";
                    // 当点位的size位32时，类型强制转为System.Int32，当size为16时，类型强制转为System.Int16，当size为1时，类型强制转为System.Boolean
                    string dataOrder = "";
                    if (point.Size == 32)
                    {
                        typeofPoint = "System.Int32";
                        dataOrder = "CDAB";
                    }
                    else if (point.Size == 16)
                    {
                        typeofPoint = "System.Int16";
                        dataOrder = "AB";
                    }
                    else if (point.Size == 1)
                    {
                        typeofPoint = "System.Boolean";
                    }
                    if (point == null || typeofPoint == "" || data == null)
                    {
                        continue;
                    }
                    //根据数据点表定义的顺序调整数据格式
                    //当类型为bool时，不需要调整字节顺序
                    if (typeofPoint == "System.Boolean")
                    {
                        byte[] newData =  data;
                        object actualData = Common.DataTranslation(newData, 0, point.Size, typeofPoint);
                        ResultArray.Add(item.Key, actualData);
                    }
                    else
                    {
                        byte[] newData = Common.ConverByteOrder(data, dataOrder);
                        object actualData = Common.DataTranslation(newData, 0, point.Size, typeofPoint);
                        object valueToConvert = Convert.ToDouble(actualData) / point.Scale;
                        ResultArray.Add(item.Key, valueToConvert);
                    }
                }
                return ResultArray;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return new Dictionary<string, object>();
            }
        }

        private MapEntry FindPointByName(string paramName)
        {
            foreach (var map in modbusPointMap)
            {
                var point = map.FirstOrDefault(x => x.ParamName == paramName);
                if (point != null)
                {
                    return point;
                }
            }
            throw new Exception($"Point with name {paramName} not found.");
        }
    }
}
