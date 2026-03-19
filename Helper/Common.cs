using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Type = System.Type;

namespace EssSimulator
{
    public static class Common
    {
        private const int ADDRESS_LENGTH = 16;
        private const int BYTE_LENGTH = 8;
        
        /// <summary>
        /// 数据翻译
        /// </summary>
        /// <param name="data">地址对应的字节数组</param>
        /// <param name="index">参数对应的index</param>
        /// <param name="size">参数对应的size</param>
        /// <param name="type">参数对应的类型</param>
        /// <returns>从字节数组中截取的数据</returns>
        public static object DataTranslation(byte[] data, int index, int size, string type)
        {
            object result = 0;
            if (size > ADDRESS_LENGTH && type != "System.String")
            {
                //字节数组前面为高位，后面为低位                     
                byte[] newData = new byte[4];
                Array.Copy(data, 2, newData, 0, 2); // 复制后两个元素到新数组的前两个位置
                Array.Copy(data, 0, newData, 2, 2); // 复制前两个元素到新数组的后两个位置
                result = BitConverter.ToUInt32(newData, 0);//该转化默认小端序
            }
            //处理SN码
            else if (size > ADDRESS_LENGTH && type == "System.String")
            {
                result = Encoding.ASCII.GetString(data);
            }
            else
            {
                int concatenatedValue = (data[1] << 8) | data[0];
                result = concatenatedValue >> index & ((1 << size) - 1);
                if (type == "System.Int16")
                {
                    if ((int)result > 32767)
                    {
                        result = (int)result - 65536;
                    }
                    result = Convert.ToInt16(result); 
                }
                else
                {
                    result = Convert.ToUInt16(result); ;
                }
            }
            var t = Type.GetType(type);
            if (t == null) throw new Exception("Type Conversion Failed");
            var value = Convert.ChangeType(result, t);
            return value;
        }

        /// 数据加密
        /// </summary>
        /// <param name="data">设置值</param>
        /// <param name="index">参数对应的index</param>
        /// <param name="size">参数对应的size</param>
        /// <param name="type">参数对应的类型</param>
        /// <returns>原始数据</returns>
        public static byte[] DataUnTranslation(object data, string type)
        {
            byte[] result;
            if (type == "System.UInt16")
            {
                if (ushort.TryParse(data.ToString(), out ushort value))
                {
                    result = BitConverter.GetBytes(value);
                }
                else
                {
                    result = new byte[2];
                }
            }
            else if (type == "System.Int16")
            {
                if (short.TryParse(data.ToString(), out short value))
                {
                    result = BitConverter.GetBytes(value);
                }
                else
                {
                    result = new byte[2];
                }
            }
            else if (type == "System.Int32")
            {
                if (int.TryParse(data.ToString(), out int value))
                {
                    result = BitConverter.GetBytes(value);
                }
                else
                {
                    result = new byte[4];
                }
            }
            else if (type == "System.UInt32")
            {
                if (uint.TryParse(data.ToString(), out uint value))
                {
                    result = BitConverter.GetBytes(value);
                }
                else
                {
                    result = new byte[4];
                }
            }
            else if (type == "System.Int64")
            {
                if (long.TryParse(data.ToString(), out long value))
                {
                    result = BitConverter.GetBytes(value);
                }
                else
                {
                    result = new byte[8];
                }
            }
            else if (type == "System.UInt64")
            {
                if (long.TryParse(data.ToString(), out long value))
                {
                    result = BitConverter.GetBytes(value);
                }
                else
                {
                    result = new byte[8];
                }
            }
            else if (type == "System.Double")
            {
                if (double.TryParse(data.ToString(), out double value))
                {
                    result = BitConverter.GetBytes(value);
                }
                else
                {
                    result = new byte[8];
                }
            }
            else if (type == "System.Single")
            {
                if (float.TryParse(data.ToString(), out float value))
                {
                    result = BitConverter.GetBytes(value);
                }
                else
                {
                    result = new byte[4];
                }
            }
            else if (type == "System.Boolean")
            {
                // 将0或1放入字节数组的第一个位置
                result = new byte[1];
                result[0] = (data.ToString() == "1" || data.ToString()!.ToLower() == "true") ? (byte)1 : (byte)0;
            }
            else if (type == "System.String")
            {
                result = Encoding.ASCII.GetBytes(data.ToString() ?? "");
            }
            else
            {
                throw new Exception(type + "Unsupported Type");
            }
            return result;
        }

        /// <summary>
        /// 字节数组顺序转换
        /// </summary>
        /// <param name="original">原始输入字节数组</param>
        /// <param name="order">字节数组顺序标定"ABCD","BADC"等</param>
        /// <returns></returns>
        public static byte[] ConverByteOrder(byte[] original, string order)
        {
            byte[] result = new byte[original.Length];
            if (order != null && order.ToCharArray().Length > 0)
            {
                var dataOrderBytes = order.Where(b => b >= 'A' && b <= 'H').ToArray();

                for (int i = 0; i < dataOrderBytes.Length; i++)
                {
                    result[i] = original[dataOrderBytes[i] - 'A'];
                }

            }
            else
            {
                Array.Copy(original, 0, result, 0, original.Length);
            }
            return result;
        }


        public static ushort[] ConvertBytesToUShorts(byte[] bytes)
        {
            if (bytes.Length % 2 != 0)
                throw new ArgumentException("Byte array length must be a multiple of 2.");

            // Create the ushort array with half the size of the byte array.
            ushort[] ushorts = new ushort[bytes.Length / 2];
            // Use Marshal.Copy for memory operations, which is faster than manual copying.
            GCHandle handle = GCHandle.Alloc(ushorts, GCHandleType.Pinned);
            try
            {
                Marshal.Copy(bytes, 0, handle.AddrOfPinnedObject(), bytes.Length);
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }
            return ushorts;
        }
    }
}