using System;
using System.Threading;
using IEC61850_simulatorServer2.EssSimModelApi.ElectricMeter;
using IEC61850_simulatorServer2.Helper;
using IEC61850_simulatorServer2.EssDeviceSimModel;

namespace IEC61850_simulatorServer2.EssSimModelApi
{
    /// <summary>
    /// 电表数据服务：从运行中的电表对象同步数据到接口层 DTO，并注册到对象池。
    /// </summary>
    public class EmDataService
    {
        private EmData _emData;
        private Thread _worker;
        private bool _isRunning = true;
        private int _updateIntervalMs = 20; // 200ms 更新周期，匹配 BMS 服务

        public EmDataService()
        {
            _emData = EmDataGenerator.GenerateSampleData();
            var _objects = ObjectsCollect.Instance;
            _objects.AddObjects("em", _emData);
            _worker = new Thread(UpdateEmData) { IsBackground = true, Name = "EmDataService" };
            _worker.Start();
        }

        private DateTime? _lastUpdateUtc = null;
        private void UpdateEmData()
        {
            var objectsCollect = ObjectsCollect.Instance;
            EnergyStorageSystem ess = null;//(EnergyStorageSystem)objectsCollect.GetObjByName("ess");
            while (_isRunning)
            {
                if(ess == null)
                {
                    ess = (EnergyStorageSystem)objectsCollect.GetObjByName("ess");
                }

                var now = DateTime.UtcNow;
                if (_lastUpdateUtc == null)
                {
                    _lastUpdateUtc = now;
                }

                var dt = now - _lastUpdateUtc;
                if (dt.Value.TotalSeconds > 0)
                {
                    UpdateFromEssData(ess, dt.Value);
                }
                Thread.Sleep(_updateIntervalMs); // match BMS service cadence
            }
        }

        private double _forwardActiveEnergyKWh;
        private double _reverseActiveEnergyKWh;
        private void UpdateFromEssData(EnergyStorageSystem ess, TimeSpan dt)
        {
            var pcs1 = ess._pcs1.GetCurrentState();
            var pcs2 = ess._pcs2.GetCurrentState();
            var transformerState = ess._transformer.GetCurrentState();

            // 二次侧线电压（假定三相平衡）
            var lineVoltage = transformerState?.SecondaryVoltage > 0
                ? transformerState.SecondaryVoltage
                : ess._transformer._specs.SecondaryVoltage;

            // 负载侧有功/无功（kW / kvar）
            var loadActiveKw = ess._loadSimulator.ActivePower;
            var loadReactiveKvar = ess._loadSimulator.ReactivePower;

            // 汇总功率（kW/kvar）
            var totalActiveKw = pcs1.ActivePower + pcs2.ActivePower + loadActiveKw;
            var totalReactiveKvar = pcs1.ReactivePower + pcs2.ReactivePower + loadReactiveKvar;

            var apparentPowerKva = Math.Sqrt(totalActiveKw * totalActiveKw + totalReactiveKvar * totalReactiveKvar);
            var powerFactor = apparentPowerKva > 0 ? totalActiveKw / apparentPowerKva : 1.0;
            powerFactor = Math.Max(-1.0, Math.Min(1.0, powerFactor));

            // 电流估算：I = S/V（此处简化为等效相电压，不考虑√3，保持与现有模型的单相等效一致性）
            var lineCurrent = lineVoltage > 0 ? apparentPowerKva * 1000.0 / lineVoltage / Math.Sqrt(3.0) : 0.0;

            // 填充电表瞬时量（假定三相平衡）：线电压=二次侧电压，相电压=线电压/√3
            var phaseVoltage = lineVoltage / Math.Sqrt(3.0);
            _emData.PhaseAVoltage = (float)phaseVoltage;
            _emData.PhaseBVoltage = (float)phaseVoltage;
            _emData.PhaseCVoltage = (float)phaseVoltage;
            _emData.LineVoltageAB = (float)lineVoltage;
            _emData.LineVoltageBC = (float)lineVoltage;
            _emData.LineVoltageCA = (float)lineVoltage;

            _emData.PhaseACurrent = (float)lineCurrent;
            _emData.PhaseBCurrent = (float)lineCurrent;
            _emData.PhaseCCurrent = (float)lineCurrent;

            // 视在功率拆分三相（平衡假设）
            var perPhaseP = totalActiveKw / 3.0;
            var perPhaseQ = totalReactiveKvar / 3.0;
            _emData.PhaseAActivePower = (float)perPhaseP;
            _emData.PhaseBActivePower = (float)perPhaseP;
            _emData.PhaseCActivePower = (float)perPhaseP;
            _emData.TotalActivePower = (float)totalActiveKw;

            _emData.PhaseAReactivePower = (float)perPhaseQ;
            _emData.PhaseBReactivePower = (float)perPhaseQ;
            _emData.PhaseCReactivePower = (float)perPhaseQ;
            _emData.TotalReactivePower = (float)totalReactiveKvar;
            _emData.TotalApparentPower = (float)apparentPowerKva;
            _emData.PowerFactor = (float)powerFactor;
            _emData.Frequency = 50.0f;   // 固定频率

            // 电能累加（kWh / kvarh），按时间步积分
            var hours = dt.TotalHours;
            if (totalActiveKw >= 0)
            {
                _forwardActiveEnergyKWh += totalActiveKw * hours;
            }
            else
            {
                _reverseActiveEnergyKWh += -totalActiveKw * hours;
            }

            _emData.ForwardActiveEnergy = (float)_forwardActiveEnergyKWh;
            _emData.ReverseActiveEnergy = (float)_reverseActiveEnergyKWh;
        
        }

        public void Stop()
        {
            _isRunning = false;
            _worker.Join();
        }
    }
}
