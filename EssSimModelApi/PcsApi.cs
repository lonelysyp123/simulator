using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EssSimulator.EssDeviceSimModel;

namespace EssSimulator.EssSimModelApi
{
    using System;
    using System.Collections.Generic;

    namespace EnergyManagementSystem
    {
            /// <summary>
            /// 能量管理单元(EMU)数据模型
            /// </summary>
            public class EmuData
            {
                public DateTime Timestamp { get; set; } = DateTime.Now;

                // 高压断路器（单元变前）开合：0=分，1=合（由 emu.csv: poweronoff 点位写入）
                public ushort PowerOnOff { get; set; } = 1;

                // 基本功率信息
                public float OutputActivePower { get; set; }       // EMU-输出总有功功率
                public float OutputReactivePower { get; set; }     // EMU-输出总无功功率

                // 功率能力信息
                public float MaxChargePower { get; set; }          // EMU-最大可充电功率
                public float MaxDischargePower { get; set; }       // EMU-最大可放电功率
                public float MaxInductiveReactivePower { get; set; } // EMU-最大可用感性无功
                public float MaxCapacitiveReactivePower { get; set; } // EMU-最大可用容性无功

                // 运行状态
                public int OperationStatus { get; set; }           // EMU-工作状态 1、停机 2、待机 3、充电运行 4、放电运行 5、故障  6、零功率运行 7、告警
                public int TotalPcsCount { get; set; }             // EMU-PCS总数
                public int OnlinePcsCount { get; set; }            // EMU-在线PCS数
                public int GridConnectedPcsCount { get; set; }     // EMU-并网PCS数

                // 电池系统信息
                public int TotalBatteryCount { get; set; }         // EMU-电池总数
                public int OnlineBatteryCount { get; set; }        // EMU-在线电池数
                public float AverageBatterySoc { get; set; }       // EMU-电池平均SOC

                // PCS状态统计
                public int AlarmPcsCount { get; set; }             // EMU-告警PCS台数
                public int FaultPcsCount { get; set; }             // EMU-故障PCS台数
                public int ChargeProhibitedPcsCount { get; set; }  // EMU-禁止充电PCS台数
                public int DischargeProhibitedPcsCount { get; set; } // EMU-禁止放电PCS台数

                // 输入信号状态
                public List<DigitalInputStatus> DigitalInputs { get; set; } = new List<DigitalInputStatus>();

                // 输出控制信号
                public bool SauTripLowVoltageBreaker { get; set; }  // EMU-SAU跳闸低压断路器
                public bool SauTripHighVoltageBreaker1 { get; set; } // EMU-SAU跳闸高压断路器-1
                public bool SauTripHighVoltageBreaker2 { get; set; } // EMU-SAU跳闸高压断路器-2
                public bool FireSignal { get; set; }               // EMU-消防信号
                public bool FanControl { get; set; }               // EMU-风扇控制

                // 系统状态标志
                public bool SocBalanceSchedulingEnabled { get; set; } // EMU-SOC均衡调度投入中
                public bool PwmBlockingEnabled { get; set; }        // EMU-PWM封波投入中
                public bool AllPcsShutdown { get; set; }           // EMU-所有PCS关机
                public bool AnyPcsStarted { get; set; }            // EMU-任意PCS开机
                public bool AllPcsAlarmed { get; set; }            // EMU-所有PCS告警
                public bool AnyPcsAlarmed { get; set; }            // EMU-任意PCS告警
                public bool AllPcsFaulted { get; set; }            // EMU-所有PCS故障
                public bool AnyPcsFaulted { get; set; }            // EMU-任意PCS故障
                public bool AllPcsChargeProhibited { get; set; }   // EMU-所有PCS禁止充电
                public bool AnyPcsChargeProhibited { get; set; }   // EMU-任意PCS禁止充电
                public bool AllPcsDischargeProhibited { get; set; } // EMU-所有PCS禁止放电
                public bool AnyPcsDischargeProhibited { get; set; } // EMU-任意PCS禁止放电

                // 预留字段
                public List<float> ReservedTelemetry { get; set; } = new List<float>(); // 遥测预留
                public List<bool> ReservedSignals { get; set; } = new List<bool>();     // 遥信预留
                public List<float> ReservedAdjustments { get; set; } = new List<float>(); // 遥调预留
            }

            /// <summary>
            /// 数字输入状态
            /// </summary>
            public class DigitalInputStatus
            {
                public int Index { get; set; }
                public string Description { get; set; }
                public bool Value { get; set; }
                public bool Triggered { get; set; }
            }

            /// <summary>
            /// PCS数据模型
            /// </summary>
            public class PcsData
            {
                public int PcsId { get; set; }

                // 交流侧电气参数
                public float LineVoltageAB { get; set; }           // 线电压AB
                public float LineVoltageBC { get; set; }           // 线电压BC
                public float LineVoltageCA { get; set; }           // 线电压CA
                public float Frequency { get; set; }               // 频率
                public float PhaseACurrent { get; set; }           // A相电流
                public float PhaseBCurrent { get; set; }           // B相电流
                public float PhaseCCurrent { get; set; }           // C相电流

                // 直流侧电气参数
                public float BatteryVoltage { get; set; }          // 电池电压
                public float BatteryCurrent { get; set; }          // 电池电流
                public float BatteryPower { get; set; }            // 电池功率

                // 功率相关
                public float ApparentPower { get; set; }            // 视在功率
                public float ActivePower { get; set; }             // 有功功率
                public float AvailableCapacity { get; set; }        // 当前可用容量
                public float ReactivePower { get; set; }           // 无功功率
                public float PowerFactor { get; set; }             // 功率因数

                // 能量统计
                public float TotalChargeEnergy { get; set; }       // 总充电量
                public float TotalDischargeEnergy { get; set; }    // 总放电量
                public float DailyChargeEnergy { get; set; }       // 日充电量
                public float DailyDischargeEnergy { get; set; }    // 日放电量

                // 绝缘监测
                public float PositiveToGroundInsulation { get; set; } // 正对地绝缘阻抗值
                public float NegativeToGroundInsulation { get; set; } // 负对地绝缘阻抗值

                // 温度监测
                public float ControlCabinetTemp { get; set; }      // 控制舱温度
                public float ControlCabinetHumidity { get; set; }  // 控制舱湿度
                public float IGBTMaxTemp { get; set; }            // IGBT最大温度

                /// <summary>develop 点表别名（emu.csv yc34/yc61）。</summary>
                public float IGBTTemp
                {
                    get => IGBTMaxTemp;
                    set => IGBTMaxTemp = value;
                }

                public float PhaseAIGBTTemp { get; set; }            // A相IGBT温度
                public float PhaseBIGBTTemp { get; set; }            // B相IGBT温度
                public float PhaseCIGBTTemp { get; set; }            // C相IGBT温度
                public float AmbientTemp { get; set; }            // 环境温度

                // 保护参数
                public float BatteryChargeProtectionVoltage { get; set; } // 电池充电保护电压
                public float BatteryDischargeProtectionVoltage { get; set; } // 电池放电保护电压
                public float BatteryChargeProtectionCurrent { get; set; } // 电池充电保护电流
                public float BatteryDischargeProtectionCurrent { get; set; } // 电池放电保护电流

                // 限制参数
                public float BatteryChargeCurrentLimit { get; set; } // 电池充电限流点
                public float BatteryChargeVoltageLimit { get; set; } // 电池充电限压点
                public float BatteryDischargeCurrentLimit { get; set; } // 电池放电限流点
                public float BatteryDischargeVoltageLimit { get; set; } // 电池放电限压点
                public float BatteryChargePowerLimit { get; set; }  // 电池充电限功率点
                public float BatteryDischargePowerLimit { get; set; } // 电池放电限功率点
                public float ChargePowerLimit { get; set; }         // 充电功率限值
                public float DischargePowerLimit { get; set; }      // 放电功率限值

                public float DCCCLimit { get; set; }                // 直流恒流电流
                public float DCCVLimit { get; set; }                // 直流恒压电压
                public int ActiveReactivePriority { get; set; }    // 有功无功优先

                // 控制命令
                public bool ChargeProhibited { get; set; }         // 禁止充电
                public bool DischargeProhibited { get; set; }       // 禁止放电

                /// <summary>PCS 开关机（EMS/Modbus 可写）。默认 false，仅外部写 1 后允许进入运行；写 0 为停机。</summary>
                public bool pcsOnOffSwitch { get; set; } = false;

                /// <summary>物理仿真模式（由 MapPcsState 同步），用于 OperationStatus 与界面展示。</summary>
                public OperationMode SimulatorMode { get; set; }

                /// <summary>孤岛电压设定（V，线电压 0–690）；离网建压/黑启动由 EMS 调节。</summary>
                public ushort IslandVoltageSetting { get; set; }

                /// <summary>PCS 内部有效孤岛电压反馈（V），在 IslandVoltageRampDurationMs 内趋近设定值。</summary>
                public float IslandVoltageFeedback { get; set; }

                /// <summary>黑启动开启：EMS 有功/无功设定无效，PCS 按孤岛电压内环自动调节有功（V/f 建压）。</summary>
                public bool BlackStartEnabled { get; set; }

                public bool gridOnOffSwitch { get; set; }           //并离网控制及状态

                // 状态
                public int OperationStatus 
                { 
                    get
                    {
                        // 0-停机，1-待机，2-故障，3-充电，4-放电（正放负充）
                        bool hasAlarm = AlarmSummary1 != 0 || AlarmSummary2 != 0 || AlarmSummary3 != 0;
                        return PcsDisplayLabels.ToOperationStatusCode(
                            SimulatorMode,
                            pcsOnOffSwitch,
                            ActivePower,
                            BlackStartEnabled,
                            0,
                            hasAlarm);
                    } 
                }   // 运行状态
                public float PCSActivePowerSetting { get; set; }    //有功率设置
                public float PCSReactivePowerSetting { get; set; }    //无功率设置  
                public float PCSRatePower { get; set; }    //额定功率设置

                // 内部特性
                public float PowerRampRate { get; set; }            // 功率斜率
                public float StartStopRampRate { get; set; }        // 启停斜率

                // 告警1
                public bool InsulationAlarm { get; set; }           // 绝缘阻抗异常告警
                public bool LeakageCurrentAbnormal { get; set; }    // 漏电流异常
                public bool DcOverVoltage { get; set; }             // 直流过压
                public bool GridOverVoltage { get; set; }           // 电网过压异常
                public bool GridUnderVoltage { get; set; }          // 电网欠压异常
                public bool GridOverFrequency { get; set; }         // 电网过频异常
                public bool GridUnderFrequency { get; set; }        // 电网欠频异常
                public bool PowerModuleOverTemp { get; set; }       // 功率模块过温
                public bool GridPhaseSequenceAbnormal { get; set; } // 电网相序异常
                public bool InverterSoftwareOverCurrent { get; set; } // 逆变软件过流
                public bool DcSoftStartAbnormal { get; set; }       // 直流软启动异常
                public bool AcFanAbnormal { get; set; }             // 交流风机异常
                public bool AcMainSwitchAbnormal { get; set; }      // 交流主开关异常
                public bool InternalAbnormal { get; set; }          // 内部异常
                public UInt16 AlarmSummary1
                {
                    get
                    {
                        UInt16 summary = 0;
                        summary |= (UInt16)(ToBit(InsulationAlarm) << 0);
                        summary |= (UInt16)(ToBit(LeakageCurrentAbnormal) << 1);
                        summary |= (UInt16)(ToBit(DcOverVoltage) << 2);
                        summary |= (UInt16)(ToBit(GridOverVoltage) << 3);
                        summary |= (UInt16)(ToBit(GridUnderVoltage) << 4);
                        summary |= (UInt16)(ToBit(GridOverFrequency) << 5);
                        summary |= (UInt16)(ToBit(GridUnderFrequency) << 6);
                        summary |= (UInt16)(ToBit(PowerModuleOverTemp) << 7);
                        summary |= (UInt16)(ToBit(GridPhaseSequenceAbnormal) << 8);
                        summary |= (UInt16)(ToBit(InverterSoftwareOverCurrent) << 9);
                        summary |= (UInt16)(ToBit(DcSoftStartAbnormal) << 10);
                        summary |= (UInt16)(ToBit(false) << 11);
                        summary |= (UInt16)(ToBit(AcFanAbnormal) << 12);
                        summary |= (UInt16)(ToBit(AcMainSwitchAbnormal) << 13);
                        summary |= (UInt16)(ToBit(false) << 14);
                        summary |= (UInt16)(ToBit(InternalAbnormal) << 15);
                        return summary;
                    }
                }

                // 告警2
                public bool InternalOverTemp { get; set; }          // 机内过温
                public bool AcSoftStartAbnormal { get; set; }       // 交流软启动异常
                public bool HeatExchangerFault { get; set; }        // 热交换机故障
                public bool AcSurgeProtectorAbnormal { get; set; }  // 交流防雷器异常
                public bool InternalEmergencyStopFault { get; set; } // 内部急停故障
                public bool ExternalEmergencyStopFault { get; set; } // 外部急停故障
                public bool BusVoltageNotReady { get; set; }        // 母线电压不符合开机条件
                public bool BusOverCurrent { get; set; }            // 母线电流过流
                public bool DoorAlarm { get; set; }                 // 门禁告警
                public bool PllAbnormal { get; set; }               // 锁相异常
                public bool DcSurgeProtectorAbnormal { get; set; }  // 直流防雷器异常
                public bool InverterHardwareOverCurrent { get; set; } // 逆变硬件过流
                public bool DriveFault { get; set; }                // 驱动故障
                public bool IdConflict { get; set; }                // ID 冲突
                public UInt16 AlarmSummary2
                {
                    get
                    {
                        UInt16 summary = 0;
                        summary |= (UInt16)(ToBit(InternalOverTemp) << 0);
                        summary |= (UInt16)(ToBit(AcSoftStartAbnormal) << 1);
                        summary |= (UInt16)(ToBit(HeatExchangerFault) << 2);
                        summary |= (UInt16)(ToBit(AcSurgeProtectorAbnormal) << 3);
                        summary |= (UInt16)(ToBit(InternalEmergencyStopFault) << 4);
                        summary |= (UInt16)(ToBit(ExternalEmergencyStopFault) << 5);
                        summary |= (UInt16)(ToBit(BusVoltageNotReady) << 6);
                        summary |= (UInt16)(ToBit(BusOverCurrent) << 7);
                        summary |= (UInt16)(ToBit(false) << 8);
                        summary |= (UInt16)(ToBit(DoorAlarm) << 9);
                        summary |= (UInt16)(ToBit(PllAbnormal) << 10);
                        summary |= (UInt16)(ToBit(DcSurgeProtectorAbnormal) << 11);
                        summary |= (UInt16)(ToBit(false) << 12);
                        summary |= (UInt16)(ToBit(InverterHardwareOverCurrent) << 13);
                        summary |= (UInt16)(ToBit(DriveFault) << 14);
                        summary |= (UInt16)(ToBit(IdConflict) << 15);
                        return summary;
                    }
                }

                // 告警3
                public bool MainsUnbalance { get; set; }            // 市电不平衡（备用）
                public bool SmokeAlarm { get; set; }                // 烟雾告警
                public bool ParallelCanCommFault { get; set; }      // 并机 CAN 通讯异常
                public bool HmiCanCommFault { get; set; }           // HMI CAN 通讯异常
                public bool ModelSettingError { get; set; }         // 机型设置错误
                public bool Hmi485CommFault { get; set; }           // HMI 485 通讯异常
                public bool RemoteCommFault { get; set; }           // 远程通讯故障
                public UInt16 AlarmSummary3
                {
                    get
                    {
                        UInt16 summary = 0;
                        summary |= (UInt16)(ToBit(false) << 0);
                        summary |= (UInt16)(ToBit(false) << 1);
                        summary |= (UInt16)(ToBit(false) << 2);
                        summary |= (UInt16)(ToBit(false) << 3);
                        summary |= (UInt16)(ToBit(false) << 4);
                        summary |= (UInt16)(ToBit(MainsUnbalance) << 5);
                        summary |= (UInt16)(ToBit(SmokeAlarm) << 6);
                        summary |= (UInt16)(ToBit(ParallelCanCommFault) << 7);
                        summary |= (UInt16)(ToBit(HmiCanCommFault) << 8);
                        summary |= (UInt16)(ToBit(ModelSettingError) << 9);
                        summary |= (UInt16)(ToBit(Hmi485CommFault) << 10);
                        summary |= (UInt16)(ToBit(RemoteCommFault) << 11);
                        summary |= (UInt16)(ToBit(false) << 12);
                        summary |= (UInt16)(ToBit(false) << 13);
                        summary |= (UInt16)(ToBit(false) << 14);
                        summary |= (UInt16)(ToBit(false) << 15);
                        return summary;
                    }
                }

                // 告警4
                public bool LvrtRunning { get; set; }               // 低电压穿越运行
                public bool HvrtRunning { get; set; }               // 高电压穿越运行
                public bool DcFanAbnormal { get; set; }             // 直流风机异常
                public bool HeatsinkTempSwitchAbnormal { get; set; } // 散热器温度开关异常
                public bool ExternalTempSwitchAbnormal { get; set; } // 外部温度开关异常
                public bool AuxTransformerTempSwitchAbnormal { get; set; } // 辅源变压器温度开关异常
                public bool InductorTempSwitchAbnormal { get; set; } // 电感温度开关异常
                public bool PositiveGroundAbnormal { get; set; }    // 正极接地异常
                public bool NegativeGroundAbnormal { get; set; }    // 负极接地异常
                public bool AcGroundAbnormal { get; set; }          // 交流接地异常
                public bool GridGroundAbnormal { get; set; }        // 并网接地异常
                public bool InductorFanAbnormal { get; set; }       // 电感风机异常
                public UInt16 AlarmSummary4
                {
                    get
                    {
                        UInt16 summary = 0;
                        summary |= (UInt16)(ToBit(false) << 0);
                        summary |= (UInt16)(ToBit(LvrtRunning) << 1);
                        summary |= (UInt16)(ToBit(HvrtRunning) << 2);
                        summary |= (UInt16)(ToBit(DcFanAbnormal) << 3);
                        summary |= (UInt16)(ToBit(HeatsinkTempSwitchAbnormal) << 4);
                        summary |= (UInt16)(ToBit(ExternalTempSwitchAbnormal) << 5);
                        summary |= (UInt16)(ToBit(AuxTransformerTempSwitchAbnormal) << 6);
                        summary |= (UInt16)(ToBit(InductorTempSwitchAbnormal) << 7);
                        summary |= (UInt16)(ToBit(PositiveGroundAbnormal) << 8);
                        summary |= (UInt16)(ToBit(NegativeGroundAbnormal) << 9);
                        summary |= (UInt16)(ToBit(AcGroundAbnormal) << 10);
                        summary |= (UInt16)(ToBit(GridGroundAbnormal) << 11);
                        summary |= (UInt16)(ToBit(InductorFanAbnormal) << 12);
                        summary |= (UInt16)(ToBit(false) << 13);
                        summary |= (UInt16)(ToBit(false) << 14);
                        summary |= (UInt16)(ToBit(false) << 15);
                        return summary;
                    }
                }

                // 告警5
                public bool BatteryOverVoltage { get; set; }        // 电池过压
                public bool BatteryUnderVoltage { get; set; }       // 电池欠压
                public bool DcOverCurrent { get; set; }             // 直流过流
                public bool OutputOverVoltage { get; set; }         // 输出电压异常
                public bool OutputVoltageNotReadyForGrid { get; set; } // 输出电压不符合离网条件
                public bool OverloadProtection { get; set; }        // 过载保护
                public bool ShortCircuitProtection { get; set; }    // 短路保护
                public bool DcFuseAbnormal { get; set; }            // 直流保险丝异常
                public bool BatteryHeavyLoadUnderVoltage { get; set; } // 电池重载欠压
                public bool BatteryLowVoltageWarning { get; set; }  // 电池低压告警
                public bool BatteryReverseConnection { get; set; }  // 电池反接
                public bool BatteryVoltageNotReadyForCharge { get; set; } // 电池电压不符合充电条件
                public bool OverloadWarning { get; set; }           // 过载告警
                public UInt16 AlarmSummary5
                {
                    get
                    {
                        UInt16 summary = 0;
                        summary |= (UInt16)(ToBit(BatteryOverVoltage) << 0);
                        summary |= (UInt16)(ToBit(BatteryUnderVoltage) << 1);
                        summary |= (UInt16)(ToBit(DcOverCurrent) << 2);
                        summary |= (UInt16)(ToBit(OutputOverVoltage) << 3);
                        summary |= (UInt16)(ToBit(OutputVoltageNotReadyForGrid) << 4);
                        summary |= (UInt16)(ToBit(OverloadProtection) << 5);
                        summary |= (UInt16)(ToBit(ShortCircuitProtection) << 6);
                        summary |= (UInt16)(ToBit(false) << 7);
                        summary |= (UInt16)(ToBit(DcFuseAbnormal) << 8);
                        summary |= (UInt16)(ToBit(BatteryHeavyLoadUnderVoltage) << 9);
                        summary |= (UInt16)(ToBit(BatteryLowVoltageWarning) << 10);
                        summary |= (UInt16)(ToBit(false) << 11);
                        summary |= (UInt16)(ToBit(BatteryReverseConnection) << 12);
                        summary |= (UInt16)(ToBit(BatteryVoltageNotReadyForCharge) << 13);
                        summary |= (UInt16)(ToBit(OverloadWarning) << 14);
                        summary |= (UInt16)(ToBit(false) << 15);
                        return summary;
                    }
                }   

                // 告警6（BMS相关）
                public bool BmsSystemFault { get; set; }            // BMS 系统故障
                public bool BmsCommFault { get; set; }              // BMS 通信异常
                public bool BmsDryContactAbnormal { get; set; }     // BMS 干接点异常
                public bool BmsChargeProhibit { get; set; }         // BMS 禁充
                public bool BmsDischargeProhibit { get; set; }      // BMS 禁放
                public bool BmsStandby { get; set; }                // BMS 待机
                public bool BmsAlarm { get; set; }                  // BMS 告警
                public UInt16 AlarmSummary6
                {
                    get
                    {
                        UInt16 summary = 0;
                        summary |= (UInt16)(ToBit(BmsSystemFault) << 0);
                        summary |= (UInt16)(ToBit(BmsCommFault) << 1);
                        summary |= (UInt16)(ToBit(BmsDryContactAbnormal) << 2);
                        summary |= (UInt16)(ToBit(BmsChargeProhibit) << 3);
                        summary |= (UInt16)(ToBit(BmsDischargeProhibit) << 4);
                        summary |= (UInt16)(ToBit(BmsStandby) << 5);
                        summary |= (UInt16)(ToBit(BmsAlarm) << 6);
                        summary |= (UInt16)(ToBit(false) << 7);
                        summary |= (UInt16)(ToBit(false) << 8);
                        summary |= (UInt16)(ToBit(false) << 9);
                        summary |= (UInt16)(ToBit(false) << 10);
                        summary |= (UInt16)(ToBit(false) << 11);
                        summary |= (UInt16)(ToBit(false) << 12);
                        summary |= (UInt16)(ToBit(false) << 13);
                        summary |= (UInt16)(ToBit(false) << 14);
                        summary |= (UInt16)(ToBit(false) << 15);
                        return summary;
                    }
                }

                // 告警7
                public bool AntiPidModuleAbnormal { get; set; }     // 防 PID 模块异常
                public bool PhaseSyncAbnormal { get; set; }         // 相位同步异常
                public bool DcPathConfigAbnormal { get; set; }      // 直流路径数配置异常
                public bool AntiIslandingAbnormal { get; set; }     // 防孤岛异常
                public bool GroundLoopAbnormal { get; set; }        // 接地回路异常
                public bool MidpointContactAbnormal { get; set; }   // 中点接触器异常
                public bool AcSurgeSuppressionAbnormal { get; set; } // 交流缓冲异常
                public bool SystemOverTemp { get; set; }            // 系统过温
                public bool SystemOverHumidity { get; set; }        // 系统过湿
                public bool PvPolarityReverse { get; set; }         // PV 极性反接
                public bool TransformerOverTemp { get; set; }       // 箱变超温
                public bool DcdcCommAbnormal { get; set; }          // 储能 DCDC 通讯异常
                public bool DcdcRunningAbnormal { get; set; }       // 储能 DCDC 运行异常
                public UInt16 AlarmSummary7
                {
                    get
                    {
                        UInt16 summary = 0;
                        summary |= (UInt16)(ToBit(AntiPidModuleAbnormal) << 0);
                        summary |= (UInt16)(ToBit(PhaseSyncAbnormal) << 1);
                        summary |= (UInt16)(ToBit(DcPathConfigAbnormal) << 2);
                        summary |= (UInt16)(ToBit(AntiIslandingAbnormal) << 3);
                        summary |= (UInt16)(ToBit(false) << 4);
                        summary |= (UInt16)(ToBit(false) << 5);
                        summary |= (UInt16)(ToBit(GroundLoopAbnormal) << 6);
                        summary |= (UInt16)(ToBit(MidpointContactAbnormal) << 7);
                        summary |= (UInt16)(ToBit(AcSurgeSuppressionAbnormal) << 8);
                        summary |= (UInt16)(ToBit(false) << 9);
                        summary |= (UInt16)(ToBit(SystemOverTemp) << 10);
                        summary |= (UInt16)(ToBit(SystemOverHumidity) << 11);
                        summary |= (UInt16)(ToBit(PvPolarityReverse) << 12);
                        summary |= (UInt16)(ToBit(TransformerOverTemp) << 13);
                        summary |= (UInt16)(ToBit(DcdcCommAbnormal) << 14);
                        summary |= (UInt16)(ToBit(DcdcRunningAbnormal) << 15);
                        return summary;
                    }
                }

                private static UInt16 ToBit(bool? flag)
                {
                    return flag == true ? (UInt16)1 : (UInt16)0;
                }
            }

            /// <summary>
            /// 箱变测控(XBCK)数据模型
            /// </summary>
            public class XbckData
            {
                public int GroupId { get; set; }

                // 电气参数
                public float PhaseACurrent { get; set; }           // XBCK-第一组A相电流
                public float PhaseBCurrent { get; set; }           // XBCK-第一组B相电流
                public float PhaseCCurrent { get; set; }           // XBCK-第一组C相电流
                public float LineVoltageAB { get; set; }           // XBCK-第一组AB线电压
                public float LineVoltageBC { get; set; }           // XBCK-第一组BC线电压
                public float LineVoltageCA { get; set; }           // XBCK-第一组CA线电压
                public float ThreePhaseActivePower { get; set; }   // XBCK-第一组3相有功功率
                public float ThreePhaseReactivePower { get; set; } // XBCK-第一组3相无功功率
                public float PowerFactor { get; set; }             // XBCK-第一组功率因数
                public float Frequency { get; set; }               // XBCK-第一组频率
                public float ZeroSequenceCurrent { get; set; }     // XBCK-第一组零序电流

                // 温度监测
                public float TransformerRoomTemp { get; set; }     // XBCK-X4-10-11-PT100变压器室在线测温
                public float BusbarTemp { get; set; }              // XBCK-X4-12-13-PT100领排陀铜排在线测温

                // 模拟量输入
                public float AnalogInput1 { get; set; }            // XBCK-4-20mA 1
                public float AnalogInput2 { get; set; }            // XBCK-4-20mA 2
                public float AnalogInput3 { get; set; }            // XBCK-4-20mA 3

                // 数字输入状态
                public List<DigitalInputStatus> DigitalInputs { get; set; } = new List<DigitalInputStatus>();

                // 预留字段
                public List<float> ReservedValues { get; set; } = new List<float>();
            }

            /// <summary>
            /// 干变温控(GBWK)数据模型
            /// </summary>
            public class GbwkData
            {
                // 绕组温度
                public float Winding1PhaseATemp { get; set; }      // GBWK-绕组1 A相温度
                public float Winding1PhaseBTemp { get; set; }      // GBWK-绕组1 B相温度
                public float Winding1PhaseCTemp { get; set; }      // GBWK-绕组1 C相温度
                public float Winding1PhaseDTemp { get; set; }      // GBWK-绕组1 D路温度(预留)

                public float Winding2PhaseATemp { get; set; }      // GBWK-绕组2 A相温度(双分裂时有效)
                public float Winding2PhaseBTemp { get; set; }      // GBWK-绕组2 B相温度(双分裂时有效)
                public float Winding2PhaseCTemp { get; set; }      // GBWK-绕组2 C相温度(双分裂时有效)
                public float Winding2PhaseDTemp { get; set; }      // GBWK-绕组2 D路温度(预留)

                // 控制输出
                public bool FanControlOutput { get; set; }         // GBWK-风机控制输出位
                public bool OverTempTripOutput { get; set; }       // GBWK-超温跳闸输出位
                public bool OverTempAlarmOutput { get; set; }      // GBWK-超温报警输出位
                public bool FaultAlarmOutput { get; set; }        // GBWK-故障报警输出位

                // 预留字段
                public List<bool> ReservedOutputs { get; set; } = new List<bool>();
            }

            /// <summary>
            /// 公用测控(GYZB)数据模型
            /// </summary>
            public class GyzbData
            {
                // 电气参数
                public float PhaseACurrent { get; set; }           // GYZB-A相电流 Ia
                public float PhaseBCurrent { get; set; }           // GYZB-B相电流 Ib
                public float PhaseCCurrent { get; set; }           // GYZB-C相电流 Ic
                public float LineVoltageAB { get; set; }           // GYZB-线电压 UAB
                public float LineVoltageBC { get; set; }           // GYZB-线电压 UBC
                public float LineVoltageCA { get; set; }           // GYZB-线电压 UCA
                public float ActivePower { get; set; }             // GYZB-有功功率 P 
                public float ReactivePower { get; set; }           // GYZB-无功功率 Q 
                public float PowerFactor { get; set; }             // GYZB-功率因数 PF 
                public float Frequency { get; set; }               // GYZB-频率

                // 状态信号
                public bool CircuitBreakerClosed { get; set; }     // GYZB-断路器合闸
                public bool CircuitBreakerOpened { get; set; }     // GYZB-断路器分闸
                public bool SpringNotCharged { get; set; }         // GYZB-弹簧未储能
                public bool RemoteIndicator { get; set; }          // GYZB-远方指示
                public bool GroundSwitchClosed { get; set; }       // GYZB-接地刀合闸
                public bool HighTempWarning { get; set; }          // GYZB-高温
                public bool OverTempWarning { get; set; }          // GYZB-超温

                // 装置状态
                public bool DeviceAbnormal { get; set; }           // GYZB-装置异常
                public bool HasErrorRecord { get; set; }           // GYZB-装置是否有出错记录
            }

            /// <summary>
            /// 电表(DB)数据模型
            /// </summary>
            public class ElectricityMeterData
            {
                // 电压电流
                public float PhaseAVoltage { get; set; }           // DB-电网A相电压
                public float PhaseBVoltage { get; set; }           // DB-电网B相电压
                public float PhaseCVoltage { get; set; }           // DB-电网C相电压
                public float LineVoltageAB { get; set; }           // DB-电网AB线电压
                public float LineVoltageBC { get; set; }           // DB-电网BC线电压
                public float LineVoltageCA { get; set; }           // DB-电网CA线电压
                public float PhaseACurrent { get; set; }           // DB-电网A相电流
                public float PhaseBCurrent { get; set; }           // DB-电网B相电流
                public float PhaseCCurrent { get; set; }           // DB-电网C相电流

                // 功率
                public float PhaseAActivePower { get; set; }       // DB-A相有功功率
                public float PhaseBActivePower { get; set; }       // DB-B相有功功率
                public float PhaseCActivePower { get; set; }       // DB-C相有功功率
                public float TotalActivePower { get; set; }        // DB-总有功功率
                public float PhaseAReactivePower { get; set; }     // DB-A相无功功率
                public float PhaseBReactivePower { get; set; }     // DB-B相无功功率
                public float PhaseCReactivePower { get; set; }     // DB-C相无功功率
                public float TotalReactivePower { get; set; }      // DB-总无功功率
                public float TotalApparentPower { get; set; }      // DB-总视在功率
                public float PowerFactor { get; set; }             // DB-功率因数
                public float Frequency { get; set; }               // DB-电网频率

                // 电能
                public float ForwardActiveEnergy { get; set; }     // DB-正向有功电能
                public float ReverseActiveEnergy { get; set; }     // DB-反向有功电能
                public float InductiveReactiveEnergy { get; set; } // DB-感性无功电能
                public float CapacitiveReactiveEnergy { get; set; } // DB-容性无功电能
            }

            /// <summary>
            /// 完整的能量管理系统数据模型
            /// </summary>
            public class EnergyManagementData
            {
                public DateTime Timestamp { get; set; } = DateTime.Now;

                public EmuData Emu { get; set; } = new EmuData();
                public List<PcsData> PcsList { get; set; } = new List<PcsData>();
                public List<XbckData> XbckList { get; set; } = new List<XbckData>();
                public List<GbwkData> GbwkList { get; set; } = new List<GbwkData>();
                public GyzbData Gyzb { get; set; } = new GyzbData();
                public ElectricityMeterData ElectricityMeter { get; set; } = new ElectricityMeterData();
            }
    }
}
