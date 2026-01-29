using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC61850_simulatorServer2.EssDeviceSimModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class IndustrialPlantLoadSimulator
    {
        // 负载类型枚举
        public enum LoadCategory
        {
            Lighting,       // 照明系统
            HVAC,           // 暖通空调
            Motors,         // 电机类设备
            Compressors,    // 压缩机
            Pumps,          // 泵类
            ProductionLine, // 生产线
            Auxiliary       // 辅助设备
        }

        // 工厂负载配置
        public class PlantLoadConfig
        {
            public string LoadId { get; set; }
            public LoadCategory Category { get; set; }
            public double RatedPower { get; set; }       // 额定功率 (kW)
            public double PowerFactor { get; set; }       // 功率因数
            public double[] DailyProfile { get; set; }    // 24小时功率百分比曲线
            public double StartupCurrentMultiplier { get; set; } = 3.0; // 启动电流倍数
            public TimeSpan StartupDuration { get; set; } = TimeSpan.FromSeconds(10);
            public bool IsCritical { get; set; } = false;
        }

        // 负载运行状态
        public class LoadOperatingState
        {
            public string LoadId { get; set; }
            public LoadCategory Category { get; set; }
            public bool IsRunning { get; set; }
            public bool IsStarting { get; set; }
            public double CurrentPower { get; set; }      // 当前有功功率 (kW)
            public double ReactivePower { get; set; }     // 当前无功功率 (kvar)
            public double Current { get; set; }           // 当前电流 (A)
            public TimeSpan RunTime { get; set; }         // 本次持续运行时间
            public TimeSpan StartTimeRemaining { get; set; } // 剩余启动时间
        }

        // 工厂负载状态
        public class PlantLoadState
        {
            public DateTime CurrentTime { get; set; }
            public double TotalActivePower { get; set; }  // 总有功功率 (kW)
            public double TotalReactivePower { get; set; } // 总无功功率 (kvar)
            public double TotalCurrent { get; set; }       // 总电流 (A)
            public double PowerFactor { get; set; }       // 整体功率因数
            public List<LoadOperatingState> LoadStates { get; set; }
        }

        private readonly List<PlantLoadConfig> _loadConfigs;
        private readonly Random _random = new Random();
        private PlantLoadState _currentState;
        private DateTime _simulationStartTime;
        private double _busVoltage;

        public IndustrialPlantLoadSimulator(List<PlantLoadConfig> configs, double busVoltage = 400.0)
        {
            _loadConfigs = configs;
            _busVoltage = busVoltage;
            _simulationStartTime = DateTime.Now;

            InitializeState();
        }

        private void InitializeState()
        {
            _currentState = new PlantLoadState
            {
                CurrentTime = _simulationStartTime,
                LoadStates = _loadConfigs.Select(c => new LoadOperatingState
                {
                    LoadId = c.LoadId,
                    Category = c.Category,
                    IsRunning = false,
                    IsStarting = false,
                    CurrentPower = 0,
                    ReactivePower = 0,
                    Current = 0,
                    RunTime = TimeSpan.Zero,
                    StartTimeRemaining = c.StartupDuration
                }).ToList()
            };
        }

        // 更新负载状态
        public void Update(TimeSpan timeStep)
        {
            _currentState.CurrentTime += timeStep;

            // 重置总功率
            _currentState.TotalActivePower = 0;
            _currentState.TotalReactivePower = 0;
            _currentState.TotalCurrent = 0;

            // 更新每个负载
            foreach (var loadState in _currentState.LoadStates)
            {
                var config = _loadConfigs.First(c => c.LoadId == loadState.LoadId);

                // 1. 确定负载是否应该运行 (基于日曲线)
                int currentHour = _currentState.CurrentTime.Hour;
                double loadFactor = config.DailyProfile[currentHour];
                bool shouldRun = loadFactor > 0 && (_random.NextDouble() < loadFactor || config.IsCritical);

                // 2. 处理负载状态转换
                if (shouldRun && !loadState.IsRunning && !loadState.IsStarting)
                {
                    // 开始启动过程
                    loadState.IsStarting = true;
                    loadState.StartTimeRemaining = config.StartupDuration;
                }
                else if (!shouldRun && loadState.IsRunning)
                {
                    // 关闭负载
                    loadState.IsRunning = false;
                    loadState.IsStarting = false;
                    loadState.RunTime = TimeSpan.Zero;
                }

                // 3. 更新运行状态
                if (loadState.IsStarting)
                {
                    UpdateStartingLoad(loadState, config, timeStep);
                }
                else if (loadState.IsRunning)
                {
                    UpdateRunningLoad(loadState, config, timeStep);
                }
                else
                {
                    // 负载关闭
                    loadState.CurrentPower = 0;
                    loadState.ReactivePower = 0;
                    loadState.Current = 0;
                }

                // 4. 累加总功率
                _currentState.TotalActivePower += loadState.CurrentPower;
                _currentState.TotalReactivePower += loadState.ReactivePower;
                _currentState.TotalCurrent += loadState.Current;
            }

            // 计算整体功率因数
            _currentState.PowerFactor = CalculatePowerFactor(
                _currentState.TotalActivePower,
                _currentState.TotalReactivePower);
        }

        private void UpdateStartingLoad(LoadOperatingState state, PlantLoadConfig config, TimeSpan timeStep)
        {
            // 减少剩余启动时间
            state.StartTimeRemaining -= timeStep;

            // 启动过程结束
            if (state.StartTimeRemaining <= TimeSpan.Zero)
            {
                state.IsStarting = false;
                state.IsRunning = true;
                state.RunTime = TimeSpan.Zero;
                state.StartTimeRemaining = TimeSpan.Zero;
            }

            // 启动期间电流为额定3倍 (简化模型)
            double startPower = config.RatedPower * config.StartupCurrentMultiplier;
            state.CurrentPower = startPower * (1 - state.StartTimeRemaining.TotalSeconds / config.StartupDuration.TotalSeconds);
            state.ReactivePower = state.CurrentPower * Math.Tan(Math.Acos(config.PowerFactor));
            state.Current = (state.CurrentPower * 1000) / (_busVoltage * config.PowerFactor) * config.StartupCurrentMultiplier;
        }

        private void UpdateRunningLoad(LoadOperatingState state, PlantLoadConfig config, TimeSpan timeStep)
        {
            state.RunTime += timeStep;

            // 正常运行时功率 (考虑±5%波动)
            double variation = 1 + (_random.NextDouble() - 0.5) * 0.05;
            int currentHour = _currentState.CurrentTime.Hour;
            double loadFactor = config.DailyProfile[currentHour];

            state.CurrentPower = config.RatedPower * loadFactor * variation;
            state.ReactivePower = state.CurrentPower * Math.Tan(Math.Acos(config.PowerFactor));
            state.Current = (state.CurrentPower * 1000) / (_busVoltage * config.PowerFactor);
        }

        private double CalculatePowerFactor(double activePower, double reactivePower)
        {
            if (activePower == 0) return 1.0;
            double apparentPower = Math.Sqrt(Math.Pow(activePower, 2) + Math.Pow(reactivePower, 2));
            return activePower / apparentPower;
        }

        // 获取当前工厂负载状态
        public PlantLoadState GetCurrentState() => _currentState;

        // 示例使用
        //public static void Main(string[] args)
        //{
        //    // 创建工厂负载配置
        //    var loadConfigs = new List<PlantLoadConfig>
        //{
        //    // 照明系统 (早6晚10运行)
        //    new PlantLoadConfig {
        //        LoadId = "Lighting-1",
        //        Category = LoadCategory.Lighting,
        //        RatedPower = 50,
        //        PowerFactor = 0.95,
        //        DailyProfile = CreateDailyProfile(6, 22, 0.8)
        //    },
            
        //    // 主生产线 (两班倒，有午休)
        //    new PlantLoadConfig {
        //        LoadId = "ProdLine-1",
        //        Category = LoadCategory.ProductionLine,
        //        RatedPower = 200,
        //        PowerFactor = 0.85,
        //        DailyProfile = CreateShiftProfile(new[] {8,13}, new[] {12,20}, 0.9),
        //        StartupCurrentMultiplier = 4.0,
        //        StartupDuration = TimeSpan.FromSeconds(15),
        //        IsCritical = true
        //    },
            
        //    // 压缩机 (间歇运行)
        //    new PlantLoadConfig {
        //        LoadId = "Compressor-1",
        //        Category = LoadCategory.Compressors,
        //        RatedPower = 75,
        //        PowerFactor = 0.8,
        //        DailyProfile = CreateDailyProfile(8, 18, 0.6),
        //        StartupCurrentMultiplier = 5.0,
        //        StartupDuration = TimeSpan.FromSeconds(20)
        //    },
            
        //    // HVAC系统 (温度调节)
        //    new PlantLoadConfig {
        //        LoadId = "HVAC-1",
        //        Category = LoadCategory.HVAC,
        //        RatedPower = 120,
        //        PowerFactor = 0.9,
        //        DailyProfile = CreateHVACProfile()
        //    }
        //};

            // 创建工厂负载模拟器
            //var plantLoad = new IndustrialPlantLoadSimulator(loadConfigs);

            //// 模拟24小时运行 (每分钟更新一次)
            //Console.WriteLine("时间\t总功率\t电流\tPF\t运行设备");
            //Console.WriteLine("----\t------\t----\t---\t--------");

            //for (int i = 0; i < 1440; i++)
            //{
            //    plantLoad.Update(TimeSpan.FromMinutes(1));
            //    PrintPlantState(plantLoad, i + 1);
            //}
        //}

        // 辅助方法：创建基本日曲线
        private static double[] CreateDailyProfile(int startHour, int endHour, double loadLevel)
        {
            var profile = new double[24];
            for (int i = 0; i < 24; i++)
            {
                profile[i] = (i >= startHour && i < endHour) ? loadLevel : 0.0;
            }
            return profile;
        }

        // 辅助方法：创建班次曲线
        private static double[] CreateShiftProfile(int[] startHours, int[] endHours, double loadLevel)
        {
            var profile = new double[24];
            for (int i = 0; i < startHours.Length; i++)
            {
                int start = startHours[i];
                int end = endHours[i];

                for (int h = start; h < end; h++)
                {
                    // 模拟午休时间功率降低
                    profile[h] = (h == start + (end - start) / 2) ? loadLevel * 0.5 : loadLevel;
                }
            }
            return profile;
        }

        // 辅助方法：创建HVAC曲线 (考虑昼夜温差)
        private static double[] CreateHVACProfile()
        {
            var profile = new double[24];
            for (int i = 0; i < 24; i++)
            {
                // 白天(8-18点)高负荷，夜间低负荷，早晚过渡
                if (i >= 8 && i < 18)
                {
                    profile[i] = 0.8 + 0.2 * Math.Sin((i - 13) * Math.PI / 10); // 中午最高
                }
                else if (i >= 6 && i < 8)
                {
                    profile[i] = 0.3 + 0.2 * (i - 6); // 早晨逐渐增加
                }
                else if (i >= 18 && i < 20)
                {
                    profile[i] = 0.5 - 0.2 * (i - 18); // 傍晚逐渐减少
                }
                else
                {
                    profile[i] = 0.1; // 夜间基础负荷
                }
            }
            return profile;
        }

        private static void PrintPlantState(IndustrialPlantLoadSimulator plant, int minute)
        {
            var state = plant.GetCurrentState();
            int hour = minute / 60;
            int min = minute % 60;

            string runningLoads = string.Join(", ",
                state.LoadStates.Where(l => l.IsRunning || l.IsStarting)
                    .OrderByDescending(l => l.CurrentPower)
                    .Take(3)
                    .Select(l => $"{l.LoadId}({l.CurrentPower:F0}kW)"));

            if (runningLoads.Length > 40) runningLoads = runningLoads.Substring(0, 40) + "...";

            Console.WriteLine($"{hour:D2}:{min:D2}\t" +
                              $"{state.TotalActivePower:F0}kW\t" +
                              $"{state.TotalCurrent:F0}A\t" +
                              $"{state.PowerFactor:F2}\t" +
                              $"{runningLoads}");
        }
    }
}
