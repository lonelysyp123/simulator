using log4net;

namespace EssSimulator.LocalControl
{
    /// <summary>
    /// 点表含 ModelSim 的 LC：遥测/控制由 DataExchange 写入本机 simLc Modbus，
    /// 不按 standard 的 yx/yt/yc 点名桥接 simEmu。
    /// </summary>
    internal class ModelBoundLcRuntime : LcRuntimeBase
    {
        public ModelBoundLcRuntime(ILog log) : base(log) { }

        public override void RunCycle(
            Func<string, ModbusSimServer?> resolveEmu,
            LocalControlModbusServer lc,
            int lcIdx,
            int emuPerGroup,
            int emuCount)
        {
            if (!lc.IsOnline)
                return;

            CollectExtra(resolveEmu, lc, lcIdx, emuPerGroup, emuCount);
        }
    }

    /// <summary>5.5MW 中压 LC：4 模块 / 2 台 PCS 块，采集与总控由点表 ModelSim 驱动。</summary>
    internal sealed class Trina55MwLcRuntime : ModelBoundLcRuntime
    {
        public Trina55MwLcRuntime(ILog log) : base(log) { }
    }

    /// <summary>10MW 中压 LC：8 模块 / 4 台 PCS 块，采集与总控由点表 ModelSim 驱动。</summary>
    internal sealed class Trina10MwLcRuntime : ModelBoundLcRuntime
    {
        public Trina10MwLcRuntime(ILog log) : base(log) { }
    }
}
