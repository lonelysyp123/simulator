using EssSimulator;

namespace EssSimulator.Protocol.Modbus
{
    /// <summary>从点表提取 FC5 线圈 / FC6·16 保持寄存器控制地址，供外部写事件过滤。</summary>
    internal sealed class ModbusControlAddressIndex
    {
        private readonly HashSet<ushort> _coilAddresses = new();
        private readonly List<(ushort Start, int Length)> _holdingSpans = new();

        public ModbusControlAddressIndex(MapEntry[]? pointMap)
        {
            if (pointMap == null)
                return;

            foreach (var entry in pointMap)
            {
                if (string.IsNullOrWhiteSpace(entry.ParamName))
                    continue;

                int regLen = Math.Max(1, entry.Size / 16);
                if (entry.FunctionCode == 5)
                    _coilAddresses.Add((ushort)entry.Address);
                else if (entry.FunctionCode is 6 or 16)
                    _holdingSpans.Add(((ushort)entry.Address, regLen));
            }
        }

        public bool TouchesCoilWrite(ushort startAddress, int pointCount)
        {
            if (_coilAddresses.Count == 0 || pointCount <= 0)
                return false;

            int end = startAddress + pointCount - 1;
            foreach (var addr in _coilAddresses)
            {
                if (addr >= startAddress && addr <= end)
                    return true;
            }

            return false;
        }

        public bool TouchesHoldingWrite(ushort startAddress, int pointCount)
        {
            if (_holdingSpans.Count == 0 || pointCount <= 0)
                return false;

            int writeEnd = startAddress + pointCount - 1;
            foreach (var (start, length) in _holdingSpans)
            {
                int spanEnd = start + length - 1;
                if (start <= writeEnd && startAddress <= spanEnd)
                    return true;
            }

            return false;
        }
    }
}
