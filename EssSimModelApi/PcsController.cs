using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC61850_simulatorServer2.EssSimModelApi
{
    /// <summary>
    /// PCS 控制器，包含各种PCS的控制模式和控制算法，包括恒压，限流等控制算法
    /// </summary>
    public class PcsChargeController
    {
        // 控制器参数
        private double _targetVoltage;      // 目标直流侧电压(V)
        private double _maxChargeCurrent;   // 最大允许充电电流(A)
        private double _currentChargeCurrent; // 当前充电电流(A)
        private double _kP;                 // 比例系数
        private double _kI;                 // 积分系数
        private double _integralError;      // 误差积分项

        // 电压和电流的测量值
        private double _measuredVoltage;
        private double _measuredCurrent;

        // 采样时间间隔(秒)
        private readonly double _samplingTime;

        // 电流变化率限制(A/s)
        private readonly double _currentRampRate;
        private DateTime _lastUpdateTime;

        public PcsChargeController(double targetVoltage, double maxChargeCurrent,
                                  double kP, double kI, double samplingTime,
                                  double currentRampRate = 10.0)
        {
            _targetVoltage = targetVoltage;
            _maxChargeCurrent = maxChargeCurrent;
            _kP = kP;
            _kI = kI;
            _samplingTime = samplingTime;
            _currentRampRate = currentRampRate;
            _lastUpdateTime = DateTime.Now;

            // 初始化状态
            _currentChargeCurrent = 0;
            _integralError = 0;
        }

        /// <summary>
        /// 更新控制器状态并计算新的充电电流
        /// </summary>
        /// <param name="measuredVoltage">测量的直流侧电压(V)</param>
        /// <param name="measuredCurrent">测量的充电电流(A)</param>
        /// <returns>新的充电电流设定值(A)</returns>
        public double Update(double measuredVoltage, double measuredCurrent)
        {
            // 计算时间间隔
            DateTime now = DateTime.Now;
            double deltaTime = (now - _lastUpdateTime).TotalSeconds;
            _lastUpdateTime = now;

            // 确保deltaTime不会过大（防止长时间不更新导致问题）
            deltaTime = Math.Min(deltaTime, _samplingTime * 2);

            // 更新测量值
            _measuredVoltage = measuredVoltage;
            _measuredCurrent = measuredCurrent;

            // 计算电压误差
            double voltageError = _targetVoltage - _measuredVoltage;

            // PI控制器计算
            _integralError += voltageError * deltaTime;

            // 抗积分饱和 - 限制积分项
            double maxIntegral = _maxChargeCurrent / _kI;
            _integralError = Math.Clamp(_integralError, -maxIntegral, maxIntegral);

            // PI控制输出
            double desiredCurrent = _kP * voltageError + _kI * _integralError;

            // 限制电流范围
            desiredCurrent = Math.Clamp(desiredCurrent, 0, _maxChargeCurrent);

            // 应用电流变化率限制
            double maxCurrentChange = _currentRampRate * deltaTime;
            double currentChange = desiredCurrent - _currentChargeCurrent;

            if (Math.Abs(currentChange) > maxCurrentChange)
            {
                desiredCurrent = _currentChargeCurrent + Math.Sign(currentChange) * maxCurrentChange;
            }

            // 更新当前充电电流
            _currentChargeCurrent = desiredCurrent;

            return _currentChargeCurrent;
        }

        // 属性访问器
        public double TargetVoltage
        {
            get => _targetVoltage;
            set => _targetVoltage = value;
        }

        public double MaxChargeCurrent
        {
            get => _maxChargeCurrent;
            set => _maxChargeCurrent = value;
        }

        public double CurrentChargeCurrent => _currentChargeCurrent;

        public void Reset()
        {
            _integralError = 0;
            _currentChargeCurrent = 0;
            _lastUpdateTime = DateTime.Now;
        }
    }
}
