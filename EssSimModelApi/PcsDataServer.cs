using IEC61850_simulatorServer2.EssDeviceSimModel;
using IEC61850_simulatorServer2.EssSimModelApi.EnergyManagementSystem;
using IEC61850_simulatorServer2.EssSimModelApi.EnergyManagementSystem.EnergyManagementSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IEC61850_simulatorServer2.EssDeviceSimModel.EnergyStorageSystem;

namespace IEC61850_simulatorServer2.EssSimModelApi
{
    public class PcsDataServer
    {
        private EnergyManagementData _emuSys;
        
        public PcsDataServer() 
        {
            //构造和填充整个emu系统数据结构
            _emuSys = new EnergyManagementData();

            PcsData pcsData1 = new PcsData();
            pcsData1.PcsId = 1;
            PcsData pcsData2 = new PcsData();
            pcsData2.PcsId = 2;

            //pcs 设置的默认限值
            //保护参数设置
            pcsData1.BatteryChargeProtectionVoltage = 950;    //PCS#X-RS485-电池充电保护电压
            pcsData1.BatteryDischargeProtectionVoltage = 500; //PCS#X-RS485-电池放电保护电压
            pcsData1.BatteryChargeProtectionCurrent = 500;    //PCS#X-RS485-电池充电保护电流
            pcsData1.BatteryDischargeProtectionCurrent = 550; //PCS#X-RS485-电池放电保护电流

            //限值参数设置
            pcsData1.BatteryChargeCurrentLimit = 450;        //PCS#X-RS485-电池充电限流点
            pcsData1.BatteryChargeVoltageLimit = 950;        //PCS#X-RS485-电池充电限压点
            pcsData1.BatteryDischargeCurrentLimit = 450;     //PCS#X-RS485-电池放电限流点
            pcsData1.BatteryDischargeVoltageLimit = 450;     //PCS#X-RS485-电池放电限压点
            pcsData1.BatteryChargePowerLimit = 450;          //PCS#X-RS485-电池充电限功率点
            pcsData1.BatteryDischargePowerLimit = 450;       //PCS#X-RS485-电池放电限功率点
            pcsData1.ChargePowerLimit = 500;                 //PCS#X-RS485-充电功率限值
            pcsData1.DischargePowerLimit = 500;              //PCS#X-RS485-放电功率限值
            pcsData1.PCSRatePower = 1250;

            
            //
            pcsData1.ActivePowerDispatchMode = 1;   //有功调度模式
            pcsData1.ReactivePowerDispatchMode = 1; //无功调度模式
            pcsData1.ActiveReactivePriority = 1;    //有功无功优先
            pcsData1.FrequencyActiveSetting = 1;    //频率有功设置

            pcsData2.BatteryChargeProtectionVoltage = 950;    //PCS#X-RS485-电池充电保护电压
            pcsData2.BatteryDischargeProtectionVoltage = 500; //PCS#X-RS485-电池放电保护电压
            pcsData2.BatteryChargeProtectionCurrent = 500;    //PCS#X-RS485-电池充电保护电流
            pcsData2.BatteryDischargeProtectionCurrent = 550; //PCS#X-RS485-电池放电保护电流



            //限值参数设置
            pcsData2.BatteryChargeCurrentLimit = 450;   // PCS#X-RS485-电池充电限流点
            pcsData2.BatteryChargeVoltageLimit = 950;   // PCS#X-RS485-电池充电限压点
            pcsData2.BatteryDischargeCurrentLimit = 450;// PCS#X-RS485-电池放电限流点
            pcsData2.BatteryDischargeVoltageLimit = 450;// PCS#X-RS485-电池放电限压点
            pcsData2.BatteryChargePowerLimit = 450;     // PCS#X-RS485-电池充电限功率点
            pcsData2.BatteryDischargePowerLimit = 450;  // PCS#X-RS485-电池放电限功率点
            pcsData2.ChargePowerLimit = 500;            // PCS#X-RS485-充电功率限值
            pcsData2.DischargePowerLimit = 500;         // PCS#X-RS485-放电功率限值
                                                 
            pcsData2.ActivePowerDispatchMode = 1;   //有功调度模式
            pcsData2.ReactivePowerDispatchMode = 1; //无功调度模式
            pcsData2.ActiveReactivePriority = 1;    //有功无功优先
            pcsData2.FrequencyActiveSetting = 1;    //频率有功设置
            pcsData2.PCSRatePower = 1250;

            _emuSys.PcsList.Add(pcsData1);
            _emuSys.PcsList.Add(pcsData2);

            _emuSys.Emu.MaxChargePower = 1000;
            _emuSys.Emu.MaxDischargePower = 1000;
            var objectsCollect = ObjectsCollect.Instance;
            //把bms对象添加到对象容器中
            objectsCollect.AddObjects("emu", _emuSys);
            //启动数据定时更新线程
            Thread bmsUpdatTh = new Thread(EmuDataUpdateTh);
            bmsUpdatTh.Start();
        }

        private void EmuDataUpdateTh()
        {
            var objectsCollect = ObjectsCollect.Instance;
            EnergyStorageSystem? ess = null;

            while (true)
            {
                if(ess == null )
                {
                    ess = (EnergyStorageSystem)objectsCollect.GetObjByName("ess");
                }
                
                UpdateFromEssData(ess);
                UpdataToEssData(_emuSys, ess);

                Thread.Sleep(100);
            }
        }

        private void UpdateFromEssData(EnergyStorageSystem essData)
        {
            //获取到ess模型的pcs实时数据
            var pcs1 = essData._pcs1.GetCurrentState();
            var pcs2 = essData._pcs2.GetCurrentState();
            var bms1 = essData._batteryRack;
            var bms2 = essData._batteryRack2;
            //var transformer = essData._transformer;
            _emuSys.PcsList[0].LineVoltageAB = (float)pcs1.AcVoltage;    // PCS#X-RS485-线电压AB
            _emuSys.PcsList[0].LineVoltageBC = (float)pcs1.AcVoltage;    // PCS#X-RS485-线电压BC
            _emuSys.PcsList[0].LineVoltageCA = (float)pcs1.AcVoltage;    // PCS#X-RS485-线电压CA
            _emuSys.PcsList[0].Frequency = (float)pcs1.Frequency;        // PCS#X-RS485-频率
            _emuSys.PcsList[0].PhaseACurrent = (float)pcs1.AcCurrent;    // PCS#X-RS485-A相电流
            _emuSys.PcsList[0].PhaseBCurrent = (float)pcs1.AcCurrent;    // PCS#X-RS485-B相电流
            _emuSys.PcsList[0].PhaseCCurrent = (float)pcs1.AcCurrent;   // PCS#X-RS485-C相电流
            //直流侧电气参数
            _emuSys.PcsList[0].BatteryVoltage = (float)pcs1.DcVoltage;    // PCS#X-RS485-电池电压
            _emuSys.PcsList[0].BatteryCurrent = (float)pcs1.DcCurrent ;    // PCS#X-RS485-电池电流
            _emuSys.PcsList[0].BatteryPower = (float)pcs1.DcVoltage * (float)pcs1.DcCurrent;// PCS#X-RS485-电池功率
                                                                                              // 功率相关
            _emuSys.PcsList[0].ActivePower = (float)pcs1.ActivePower;          // PCS#X-RS485-有功功率
            _emuSys.PcsList[0].AvailableCapacity = 100;     // PCS#X-RS485-当前可用容量
            _emuSys.PcsList[0].ReactivePower = (float)pcs1.ReactivePower ;        // PCS#X-RS485-无功功率
            double denominator = Math.Sqrt(Math.Pow(pcs1.ActivePower, 2) + Math.Pow(pcs1.ReactivePower, 2));
            if (denominator == 0)
            {
                _emuSys.PcsList[0].PowerFactor = 0;
            }
            else
            {
                _emuSys.PcsList[0].PowerFactor = (float)(pcs1.ActivePower / denominator);               // PCS#X-RS485-功率因数
            }

            // 能量统计
            _emuSys.PcsList[0].TotalChargeEnergy = (float)pcs1.TotalChargeEnergy;                 // PCS#X-RS485-总充电量
            _emuSys.PcsList[0].TotalDischargeEnergy = (float)pcs1.TotalDischargeEnergy;           // PCS#X-RS485-总放电量
            _emuSys.PcsList[0].DailyChargeEnergy = (float)pcs1.DailyChargeEnergy;                 // PCS#X-RS485-日充电量
            _emuSys.PcsList[0].DailyDischargeEnergy = (float)pcs1.DailyDischargeEnergy;           // PCS#X-RS485-日放电量

            _emuSys.PcsList[1].LineVoltageAB = (float)pcs2.AcVoltage;    // PCS#X-RS485-线电压AB
            _emuSys.PcsList[1].LineVoltageBC = (float)pcs2.AcVoltage;    // PCS#X-RS485-线电压BC
            _emuSys.PcsList[1].LineVoltageCA = (float)pcs2.AcVoltage;    // PCS#X-RS485-线电压CA
            _emuSys.PcsList[1].Frequency = (float)pcs2.Frequency;        // PCS#X-RS485-频率
            _emuSys.PcsList[1].PhaseACurrent = (float)pcs2.AcCurrent;    // PCS#X-RS485-A相电流
            _emuSys.PcsList[1].PhaseBCurrent = (float)pcs2.AcCurrent;    // PCS#X-RS485-B相电流
            _emuSys.PcsList[1].PhaseCCurrent = (float)pcs2.AcCurrent;   // PCS#X-RS485-C相电流
            //直流侧电气参数
            _emuSys.PcsList[1].BatteryVoltage = (float)pcs2.DcVoltage;    // PCS#X-RS485-电池电压
            _emuSys.PcsList[1].BatteryCurrent = (float)pcs2.DcCurrent;    // PCS#X-RS485-电池电流
            _emuSys.PcsList[1].BatteryPower = (float)pcs2.DcVoltage * (float)pcs2.DcCurrent;// PCS#X-RS485-电池功率
                                                                                            // 功率相关
            _emuSys.PcsList[1].ActivePower = (float)pcs2.ActivePower;          // PCS#X-RS485-有功功率
            _emuSys.PcsList[1].AvailableCapacity = 100;     // PCS#X-RS485-当前可用容量
            _emuSys.PcsList[1].ReactivePower = (float)pcs2.ReactivePower;        // PCS#X-RS485-无功功率
            
            double denominator1 = Math.Sqrt(Math.Pow(pcs2.ActivePower, 2) + Math.Pow(pcs2.ReactivePower, 2));
            if (denominator1 == 0)
            {
                _emuSys.PcsList[1].PowerFactor = 0;
            }
            else
            {
                _emuSys.PcsList[1].PowerFactor = (float)(pcs2.ActivePower / denominator1);               // PCS#X-RS485-功率因数
            }

            // 能量统计
            _emuSys.PcsList[1].TotalChargeEnergy = (float)pcs2.TotalChargeEnergy;                 // PCS#X-RS485-总充电量
            _emuSys.PcsList[1].TotalDischargeEnergy = (float)pcs2.TotalDischargeEnergy;           // PCS#X-RS485-总放电量
            _emuSys.PcsList[1].DailyChargeEnergy = (float)pcs2.DailyChargeEnergy;                 // PCS#X-RS485-日充电量
            _emuSys.PcsList[1].DailyDischargeEnergy = (float)pcs2.DailyDischargeEnergy;           // PCS#X-RS485-日放电量

            //_emuSys.Emu.OutputActivePower = (float)pcs1.ActivePower + (float)pcs2.ActivePower;
            //_emuSys.Emu.OutputReactivePower = (float)pcs1.ReactivePower+ (float)pcs2.ReactivePower;
            _emuSys.Emu.MaxChargePower = (float)1250.0;
            _emuSys.Emu.MaxDischargePower = (float)1250.0;
            _emuSys.Emu.AverageBatterySoc = (float)bms1.GetRackState().MinClusterSOC * 100;
            if(_emuSys.PcsList[0].ActivePower > 10 || _emuSys.PcsList[1].ActivePower >10)
            {
                _emuSys.Emu.OperationStatus = 3;   //可以注入故障模拟
            }else
            if (_emuSys.PcsList[0].ActivePower < -10 || _emuSys.PcsList[1].ActivePower < -10)
            {
                _emuSys.Emu.OperationStatus = 4;
            }else
            {
                _emuSys.Emu.OperationStatus = 2;
            }
            


        }

        private void UpdataToEssData(EnergyManagementData emudata,EnergyStorageSystem ess)
        {
            var pcs1 = ess._pcs1;
            var pcs2 = ess._pcs2;

            if (emudata.PcsList != null && emudata.PcsList.Count >0)
            {
                var ActivePower1 = emudata.PcsList[0].PCSActivePowerSetting;
                var ActivePower2 = emudata.PcsList[1].PCSActivePowerSetting;
                var ReactivePower1 = emudata.PcsList[0].PCSReactivePowerSetting;
                var ReactivePower2 = emudata.PcsList[1].PCSReactivePowerSetting;

                    //先暂时更新需要控制的参数和变量到Ess模型，主要有功率调节参数，设备启停等数值
                    if (Math.Abs(ActivePower1 - pcs1.GetCurrentState().ActivePower) > 0 || Math.Abs(ReactivePower1 - pcs1.GetCurrentState().ReactivePower) > 0)
                    {
                        pcs1.SetPowerCommand(ActivePower1, ReactivePower1);
                    }

                    //先暂时更新需要控制的参数和变量到Ess模型，主要有功率调节参数，设备启停等数值
                    if (Math.Abs(ActivePower2 - pcs2.GetCurrentState().ActivePower) > 0 || Math.Abs(ReactivePower2 - pcs2.GetCurrentState().ReactivePower) > 0)
                    {
                        pcs2.SetPowerCommand(ActivePower2, ReactivePower2);
                    }

                //工作模式设定,以pcs1的工作模式为准
                if (emudata.PcsList[0].ActivePowerDispatchMode != pcs1.GetCurrentState().ActiveDispathMode)
                {
                    pcs1.GetCurrentState().ActiveDispathMode = emudata.PcsList[0].ActivePowerDispatchMode;
                }

                if (emudata.PcsList[0].ReactivePowerDispatchMode != pcs1.GetCurrentState().ReactiveDispathMode)
                {
                    pcs1.GetCurrentState().ReactiveDispathMode = emudata.PcsList[0].ReactivePowerDispatchMode;
                }

                if (emudata.PcsList[1].ActivePowerDispatchMode != pcs2.GetCurrentState().ActiveDispathMode)
                {
                    pcs2.GetCurrentState().ActiveDispathMode = emudata.PcsList[1].ActivePowerDispatchMode;
                }

                if (emudata.PcsList[1].ReactivePowerDispatchMode != pcs2.GetCurrentState().ReactiveDispathMode)
                {
                    pcs2.GetCurrentState().ReactiveDispathMode = emudata.PcsList[1].ReactivePowerDispatchMode;
                }


                //保护参数设定
                //emudata.PcsList[0].BatteryChargeProtectionVoltage;// PCS#X-RS485-电池充电保护电压
                //emudata.PcsList[0].BatteryDischargeProtectionVoltage; // PCS#X-RS485-电池放电保护电压
                //emudata.PcsList[0].BatteryChargeProtectionCurrent; // PCS#X-RS485-电池充电保护电流
                //emudata.PcsList[0].BatteryDischargeProtectionCurrent; // PCS#X-RS485-电池放电保护电流

                // 限制参数
                /*if (Math.Abs(pcs.DcLimitChgCurrent - emudata.PcsList[0].BatteryChargeCurrentLimit)>0.1)
                    pcs.DcLimitChgCurrent = emudata.PcsList[0].BatteryChargeCurrentLimit;  // PCS#X-RS485-电池充电限流点
                if(Math.Abs(pcs.DcLimitChgVoltage- emudata.PcsList[0].BatteryChargeVoltageLimit)>0.1)
                    pcs.DcLimitChgVoltage = emudata.PcsList[0].BatteryChargeVoltageLimit;  // PCS#X-RS485-电池充电限压点
                if(Math.Abs(pcs.DcLimitDsgCurrent - emudata.PcsList[0].BatteryDischargeCurrentLimit)>0.1)
                    pcs.DcLimitDsgCurrent = emudata.PcsList[0].BatteryDischargeCurrentLimit; // PCS#X-RS485-电池放电限流点
                if(Math.Abs(pcs.DcLimitDsgVoltage - emudata.PcsList[0].BatteryDischargeVoltageLimit)>0.1)
                    pcs.DcLimitDsgVoltage = emudata.PcsList[0].BatteryDischargeVoltageLimit;  // PCS#X-RS485-电池放电限压点
                if(Math.Abs(pcs.DcLimitChgPower - emudata.PcsList[0].BatteryChargePowerLimit)>0.1)
                    pcs.DcLimitChgPower   = emudata.PcsList[0].BatteryChargePowerLimit;  // PCS#X-RS485-电池充电限功率点
                if(Math.Abs(pcs.DcLimitDsgPower - emudata.PcsList[0].BatteryDischargePowerLimit) > 0.1)
                    pcs.DcLimitDsgPower   = emudata.PcsList[0].BatteryDischargePowerLimit; // PCS#X-RS485-电池放电限功率点*/

            }
        }
    }
}
