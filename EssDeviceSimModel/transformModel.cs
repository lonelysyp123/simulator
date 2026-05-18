using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EssSimulator.EssDeviceSimModel
{
    using System;
    using System.Collections.Generic;

    public class TransformerSimulator
    {
        // 变压器基本参数
        public class TransformerSpecifications
        {
            public double RatedPower { get; set; }        // 额定功率 (kVA)
            public double PrimaryVoltage { get; set; }     // 一次侧额定电压 (V)
            public double SecondaryVoltage { get; set; }   // 二次侧额定电压 (V)
            public double NoLoadLoss { get; set; }         // 空载损耗 (铁损) (W)
            public double LoadLoss { get; set; }          // 负载损耗 (铜损) (W)
            public double ImpedancePercent { get; set; }   // 阻抗百分比 (%)
            public double ReactiveVoltageInfluenceCoefficient { get; set; } = 1.0; // 并网点无功-电压影响系数
            public double NoLoadCurrentPercent { get; set; } // 空载电流百分比 (%)
            public double CoolingCoefficient { get; set; } = 0.01; // 冷却系数 (W/°C)
            public double ThermalTimeConstant { get; set; } = 180.0; // 热时间常数 (分钟)

            /// <summary>一次电压快速抬升时是否叠加励磁涌流（近似：上升率触发 + 指数衰减，叠至无功励磁支路）。</summary>
            public bool MagnetizingInrushEnabled { get; set; } = true;

            /// <summary>一次电压标幺上升率超过该值（1/s，即每秒标幺变化）时开始注入涌流。</summary>
            public double MagnetizingInrushDvDtThresholdPuPerSec { get; set; } = 0.8;

            /// <summary>单次触发在额定一次线电流上叠加的涌流峰值倍数（经强度归一后乘到 I_rated_primary）。</summary>
            public double MagnetizingInrushPeakExtraMultipleOfRatedPrimary { get; set; } = 4.0;

            /// <summary>涌流附加电流指数衰减时间常数（秒，仿真时间）。</summary>
            public double MagnetizingInrushDecayTimeConstantSec { get; set; } = 0.45;

            /// <summary>涌流无功分量上限（相对一次额定线电流的倍数）。</summary>
            public double MagnetizingInrushMaxExtraMultipleOfRatedPrimary { get; set; } = 12.0;
        }

        // 变压器运行状态
        public class TransformerState
        {
            public double Power { get; set; }              // 变压器功率
            public double PrimaryVoltage { get; set; }     // 一次侧实际电压 (V)
            public double SecondaryVoltage { get; set; }   // 二次侧电压 (V)
            public double PrimaryCurrent { get; set; }     // 一次侧电流 (A)
            public double SecondaryCurrent { get; set; }   // 二次侧电流 (A)
            public double LoadRatio { get; set; }          // 负载率 (0-1)
            public double Efficiency { get; set; }         // 当前效率 (0-1)
            public double Temperature { get; set; }        // 温度 (°C)
            public double IronLoss { get; set; }          // 铁损 (W)
            public double CopperLoss { get; set; }         // 铜损 (W)
            public double TotalLoss { get; set; }         // 总损耗 (W)
            public double PowerFactor { get; set; }        // 功率因数
            public DateTime Timestamp { get; set; }       // 状态时间

            /// <summary>本步由「快速抬压励磁」模型叠加到一次无功支路的涌流分量（A，幅值）。</summary>
            public double MagnetizingInrushCurrentPrimary { get; set; }
        }

        public TransformerSpecifications _specs { get; set; }
        public TransformerState _currentState { get; set; }
        private double _ambientTemperature;

        private double _prevPrimaryVoltagePu = -1.0;
        private double _inrushExtraPrimaryA;

        public TransformerSimulator(TransformerSpecifications specs, double ambientTemp = 25.0)
        {
            _specs = specs;
            _ambientTemperature = ambientTemp;
            _currentState = new TransformerState
            {
                PrimaryCurrent = 0,
                Temperature = ambientTemp,
                Timestamp = DateTime.Now
            };
        }

        // 获取当前状态
        public TransformerState GetCurrentState() => _currentState;

        // 更新变压器状态
        public void Update(
            double primaryVoltage,
            double secondaryCurrent,
            double powerFactor,
            double totalApparentPowerKva,
            double totalReactivePowerKvar,
            DateTime timeStamp,
            TimeSpan simulationStep)
        {
            // 1. 更新基本参数
            _currentState.PrimaryVoltage = primaryVoltage;
            _currentState.SecondaryCurrent = secondaryCurrent;
            _currentState.PowerFactor = powerFactor;
            _currentState.Timestamp = timeStamp;

            double dt = Math.Max(simulationStep.TotalSeconds, 1e-6);
            double vN = _specs.PrimaryVoltage;
            double vPuNow = vN > 1e-9 ? Math.Clamp(primaryVoltage / vN, 0.0, 1.5) : 0.0;

            if (_specs.MagnetizingInrushEnabled)
            {
                double tau = Math.Max(0.05, _specs.MagnetizingInrushDecayTimeConstantSec);
                _inrushExtraPrimaryA *= Math.Exp(-dt / tau);

                if (_prevPrimaryVoltagePu >= 0.0)
                {
                    double dvPu = vPuNow - _prevPrimaryVoltagePu;
                    if (dvPu > 1e-9)
                    {
                        double ratePuPerSec = dvPu / dt;
                        double th = _specs.MagnetizingInrushDvDtThresholdPuPerSec;
                        if (ratePuPerSec > th)
                        {
                            double iRatedPri = _specs.RatedPower * 1000 / (vN * Math.Sqrt(3));
                            double denom = Math.Max(th * 4.0, 1e-9);
                            double intensity = Math.Clamp((ratePuPerSec - th) / denom, 0.0, 1.0);
                            double add = _specs.MagnetizingInrushPeakExtraMultipleOfRatedPrimary * iRatedPri * intensity;
                            _inrushExtraPrimaryA += add;
                            double iMax = _specs.MagnetizingInrushMaxExtraMultipleOfRatedPrimary * iRatedPri;
                            if (_inrushExtraPrimaryA > iMax)
                                _inrushExtraPrimaryA = iMax;
                        }
                    }
                }

                _prevPrimaryVoltagePu = vPuNow;
            }
            else
            {
                _inrushExtraPrimaryA = 0;
                _prevPrimaryVoltagePu = vPuNow;
            }

            // 2. 计算变比
            double turnsRatio = _specs.PrimaryVoltage / _specs.SecondaryVoltage;

            // 3. 计算负载率与二次侧电压
            // 额定电流按三相线电压口径：I = S / (sqrt(3) * Uline)
            double ratedSecondaryCurrent = _specs.RatedPower * 1000 / (_specs.SecondaryVoltage * Math.Sqrt(3));
            _currentState.LoadRatio = ratedSecondaryCurrent > 0
                ? secondaryCurrent / ratedSecondaryCurrent
                : 0;

            // 并网点电压采用 Q 线性反馈（漏抗主导近似）：
            // - Q > 0 抬升电压
            // - Q < 0 下拉电压
            // 在固定 P 条件下，+/-Q 对称。
            double zPu = _specs.ImpedancePercent / 100.0;
            double reactiveShiftPu = GridFeedbackConventions.CalculatePccReactiveVoltageShiftPu(
                totalReactivePowerKvar,
                _specs.RatedPower,
                zPu,
                _specs.ReactiveVoltageInfluenceCoefficient);
            double netVoltageFactor = 1 + reactiveShiftPu;
            _currentState.SecondaryVoltage = primaryVoltage / turnsRatio * netVoltageFactor;

            // 4. 计算一次侧电流
            // 三相口径：I_rated = S / (sqrt(3) * Uline)
            // 空载电流按额定一次侧线电流百分比给定，并近似与一次侧电压成正比（励磁支路）。
            double ratedPrimaryCurrent = _specs.RatedPower * 1000 / (_specs.PrimaryVoltage * Math.Sqrt(3));
            double vRatio = _specs.PrimaryVoltage > 0 ? (primaryVoltage / _specs.PrimaryVoltage) : 0.0;
            double noLoadCurrent = (_specs.NoLoadCurrentPercent / 100.0) * ratedPrimaryCurrent * Math.Abs(vRatio);

            // 将负载电流拆分为有功/无功分量后再与励磁电流（近似纯无功滞后）做相量合成
            // - powerFactor 取值范围约定为 [-1, 1]，符号由外部约定；相角用 |pf| 计算
            double pfAbs = Math.Clamp(Math.Abs(powerFactor), 0.0, 1.0);
            double sinPhi = Math.Sqrt(Math.Max(0.0, 1.0 - pfAbs * pfAbs));
            double signQ = totalReactivePowerKvar >= 0 ? 1.0 : -1.0; // Q>0 视为感性（滞后）

            // 负载电流（线电流）优先使用传入的 secondaryCurrent 幅值；其方向符号用于功率流向约定
            double i2Mag = Math.Abs(secondaryCurrent);
            double i2W = i2Mag * pfAbs;              // 有功分量（同相）
            double i2Q = i2Mag * sinPhi * signQ;     // 无功分量（正=滞后）

            // 折算到一次侧（线电流按变比折算）
            double i1W = i2W / turnsRatio;
            double inrushQ = _specs.MagnetizingInrushEnabled ? _inrushExtraPrimaryA : 0.0;
            _currentState.MagnetizingInrushCurrentPrimary = inrushQ;
            double i1Q = i2Q / turnsRatio + noLoadCurrent + inrushQ; // 励磁 + 快速抬压涌流（无功支路）

            _currentState.PrimaryCurrent = Math.Sqrt(i1W * i1W + i1Q * i1Q);
            // 根据二次侧电流方向计算一次侧电流方向
            if (secondaryCurrent < 0)
            {
                _currentState.PrimaryCurrent = -_currentState.PrimaryCurrent;
            }

            // 10. 计算损耗
            _currentState.IronLoss = _specs.NoLoadLoss * Math.Pow(primaryVoltage / _specs.PrimaryVoltage, 2);
            _currentState.CopperLoss = _specs.LoadLoss * Math.Pow(_currentState.LoadRatio, 2);
            _currentState.TotalLoss = _currentState.IronLoss + _currentState.CopperLoss;

            // 11. 计算效率
            // 三相有功：P = sqrt(3) * Uline * Iline * pf
            double outputPower = Math.Sqrt(3.0) * _currentState.SecondaryVoltage * secondaryCurrent * powerFactor;
            var pAbs = Math.Abs(outputPower);
            _currentState.Efficiency = (pAbs + _currentState.TotalLoss) > 0
                ? pAbs / (pAbs + _currentState.TotalLoss)
                : 0;

            // 12. 温度模型
            //UpdateTemperature(timeStep);

            // 13.计算输出功率
            _currentState.Power = outputPower;
        }

        // 温度模型
        private void UpdateTemperature(TimeSpan timeStep)
        {
            // 热平衡方程
            double heatInput = _currentState.TotalLoss;
            double cooling = _specs.CoolingCoefficient *
                           (_currentState.Temperature - _ambientTemperature);

            double tempChange = (heatInput - cooling) *
                              timeStep.TotalMinutes / _specs.ThermalTimeConstant;

            _currentState.Temperature += tempChange;
        }

        // 示例使用
        //public static void Main(string[] args)
        //{
        //    // 创建一个2500kVA变压器
        //    var specs = new TransformerSpecifications
        //    {
        //        RatedPower = 2500,        // 2500kVA
        //        PrimaryVoltage = 690,     // 690V
        //        SecondaryVoltage = 230,   // 230V
        //        NoLoadLoss = 50,          // 50W
        //        LoadLoss = 200,           // 200W
        //        ImpedancePercent = 4,     // 4%
        //        NoLoadCurrentPercent = 2  // 2%
        //    };

        //    var transformer = new TransformerSimulator(specs);

        //    // 模拟24小时运行 (每5分钟更新一次)
        //    Console.WriteLine("时间\t一次电压\t二次电压\t负载率\t效率\t温度\t总损耗");

        //    for (int i = 0; i < 288; i++) // 24*60/5=288
        //    {
        //        // 模拟电网电压波动 (±5%)
        //        double primaryVoltage = 400 * (1 + 0.05 * Math.Sin(i * Math.PI / 36));

        //        // 模拟负载变化 (白天高负载，夜间低负载)
        //        int hour = i / 12;
        //        double loadFactor = (hour >= 8 && hour < 20) ? 0.7 + 0.3 * Math.Sin((hour - 14) * Math.PI / 12) : 0.2;
        //        double secondaryCurrent = (transformer._specs.RatedPower * 1000 / transformer._specs.SecondaryVoltage) *
        //                                loadFactor * (1 + 0.1 * (new Random().NextDouble() - 0.5));

        //        // 功率因数在0.8-0.95之间波动
        //        double powerFactor = 0.875 + 0.075 * Math.Sin(i * Math.PI / 72);

        //        transformer.Update(primaryVoltage, secondaryCurrent, powerFactor, totalApparentPowerKva, totalReactivePowerKvar, DateTime.Now, TimeSpan.FromMinutes(5));
        //        PrintTransformerState(transformer, i + 1);
        //    }
        //}

        private static void PrintTransformerState(TransformerSimulator transformer, int interval)
        {
            var state = transformer.GetCurrentState();
            int hour = (interval * 5) / 60;
            int minute = (interval * 5) % 60;

            Console.WriteLine($"{hour:D2}:{minute:D2}\t" +
                              $"{state.PrimaryVoltage:F1}V\t" +
                              $"{state.SecondaryVoltage:F1}V\t" +
                              $"{state.LoadRatio * 100:F1}%\t" +
                              $"{state.Efficiency * 100:F2}%\t" +
                              $"{state.Temperature:F1}°C\t" +
                              $"{state.TotalLoss:F1}W");
        }
    }
}
