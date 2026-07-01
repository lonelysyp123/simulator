using EssSimulator.DataExchange.Catalog;

namespace EssSimulator.DataExchange.Pipeline
{
    /// <summary>控制点写入 Modbus 寄存器前的值规范化（线圈 0/1；寄存器为原始 Modbus 值，与 mbpoll 一致）。</summary>
    internal static class ControlValueCoercion
    {
        public static object CoerceForModbusRegister(PointBinding binding, object valToSet)
        {
            if (valToSet is string s)
            {
                if (double.TryParse(s, out var dv))
                    valToSet = dv;
                else if (bool.TryParse(s, out var bv))
                    valToSet = bv ? 1 : 0;
            }

            if (binding.Entry.FunctionCode != 5)
                return valToSet;

            bool coilBool = valToSet switch
            {
                bool b => b,
                string str when bool.TryParse(str, out var bv) => bv,
                _ => Convert.ToDouble(valToSet) != 0
            };

            return coilBool ? 1 : 0;
        }
    }
}
