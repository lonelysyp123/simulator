using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC61850_simulatorServer2.EssDeviceSimModel
{
    public class Breaker
    {
        public bool IsClosed { get; set; } //断路器状态：闭合（true）或断开（false）

        public bool swState { get; set; } //断路器 隔离开关状态 true 闭合 false 断开
        public double RatedVoltage { get; set; } //额定电压 (kV)
        public double RatedCurrent { get; set; } //额定电流 (A)
        public double FaultThreshold { get; set; } //故障电流阈值 (A)

        public Breaker(double ratedVoltage = 10.0, double ratedCurrent = 55000, double faultThreshold = 60000.0)
        {
            IsClosed = true;
            RatedVoltage = ratedVoltage;
            RatedCurrent = ratedCurrent;
            FaultThreshold = faultThreshold;
        }

        public bool CheckFault(double current)
        {
            if (current > FaultThreshold)
            {
                IsClosed = false;
                return false; // 触发故障保护
            }
            return true; // 正常运行
        }

        public double GetAvailableCurrent(double requestedCurrent)
        {
            if (!IsClosed)
            {
                return 0; // 断路器断开时无电流
            }
            return Math.Min(requestedCurrent, RatedCurrent);
        }

        public void Update(double current)
        {
            CheckFault(current);
        }
    }
}
