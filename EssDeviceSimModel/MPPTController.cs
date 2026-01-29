using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC61850_simulatorServer2.EssDeviceSimModel
{
    public class MPPTController
    {
        public double TrackMaximumPower(double[] voltageSweep, double[] currentSweep)
        {
            // 实现最大功率点跟踪算法
            double maxPower = 0;
            double bestVoltage = 0;

            for (int i = 0; i < voltageSweep.Length; i++)
            {
                double power = voltageSweep[i] * currentSweep[i];
                if (power > maxPower)
                {
                    maxPower = power;
                    bestVoltage = voltageSweep[i];
                }
            }

            return bestVoltage;
        }

        // 在PCS中集成MPPT
        public void UpdateMPPT(double[] vArray, double[] iArray)
        {
            //if (_mpptController == null)
            //    _mpptController = new MPPTController();

            //double targetVoltage = _mpptController.TrackMaximumPower(vArray, iArray);
            // 调整PCS工作点...
        }
    }
}
