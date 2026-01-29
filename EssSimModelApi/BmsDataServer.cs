using IEC61850_simulatorServer2.EssDeviceSimModel;
using IEC61850_simulatorServer2.EssSimModelApi.BatteryManagementSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IEC61850_simulatorServer2.EssDeviceSimModel.BatteryRackSimulator;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IEC61850_simulatorServer2.EssSimModelApi
{
    public class BmsDataService
    {
        private BatteryManagementSystemData _bmsData;
        private BatteryManagementSystemData _bmsData2;

        public BmsDataService(int clusterCount, int packCount)
        {
            // 初始化BMS数据
            _bmsData = BmsDataGenerator.GenerateSampleData(1, clusterCount);
            _bmsData2 = BmsDataGenerator.GenerateSampleData(1, clusterCount);
            var objectsCollect = ObjectsCollect.Instance;
            //把bms对象添加到对象容器中
            objectsCollect.AddObjects("bms1", _bmsData);
            objectsCollect.AddObjects("bms2", _bmsData2);
            //启动数据定时更新线程
            Thread bmsUpdatTh = new Thread(BmsDataUpdateTh);
            bmsUpdatTh.Start();
        }

        private void BmsDataUpdateTh()
        {
            
            var objectsCollect = ObjectsCollect.Instance;
            EnergyStorageSystem? ess = null;//(EnergyStorageSystem)objectsCollect.GetObjByName("ess");

            while (true)
            {
                if (ess == null)
                {
                    ess = (EnergyStorageSystem)objectsCollect.GetObjByName("ess");
                }else
                {
                    UpdateFromEssData(ess);
                }
                
                Thread.Sleep(100);
            }
        }

        private int BmsGetOperationStatusByCurrent(float current)
        {
            if(current == 0)
            {
                return 0;
            }else
            if(current >0)
            {
                return 1;
            }else
            if(current <0)
            {
                return 2;
            }else
            {
                return 0;
            }
        }

        /// <summary>
        /// 根据findType，查找堆内的电芯单体极值，并找出簇的编号和簇内电芯编号
        /// </summary>
        /// <param name="batteryRackData">输入的堆完整数据</param>
        /// <param name="findType">寻找的类型 1-最高电压单体 2-最低电压单体 3-最高温度单体 4-最低温度单体</param>
        /// <param name="Value"></param>
        /// <param name="clusterId"></param>
        /// <param name="cellId"></param>
        private static void FindRackMaxCellValueAndId(RackState batteryRackData, int findType, out float Value, out int clusterId, out int packId, out int cellId)
        {
            Value = 0f;
            clusterId = 0;
            packId = 0;
            cellId = 0;

            if (batteryRackData == null || batteryRackData.ClusterStates == null || batteryRackData.ClusterStates.Count == 0)
            {
                return;
            }

            bool findMax;
            Func<CellState, double> selector;
            switch (findType)
            {
                case 1: // 最高电压单体
                    findMax = true;
                    selector = c => c.Voltage;
                    break;
                case 2: // 最低电压单体
                    findMax = false;
                    selector = c => c.Voltage;
                    break;
                case 3: // 最高温度单体
                    findMax = true;
                    selector = c => c.Temperature;
                    break;
                case 4: // 最低温度单体
                    findMax = false;
                    selector = c => c.Temperature;
                    break;
                default:
                    return;
            }

            double bestVal = findMax ? double.MinValue : double.MaxValue;
            int bestCluster = 0;
            int bestPack = 0;
            int bestCell = 0;

            for (int i = 0; i < batteryRackData.ClusterStates.Count; i++)
            {
                var cluster = batteryRackData.ClusterStates[i];
                if (cluster?.PackStates == null) continue;

                for (int j = 0; j < cluster.PackStates.Count; j++)
                {
                    var pack = cluster.PackStates[j];
                    if (pack?.CellStates == null || pack.CellStates.Count == 0) continue;

                    for (int k = 0; k < pack.CellStates.Count; k++)
                    {
                        var cell = pack.CellStates[k];
                        double v = selector(cell);

                        if ((findMax && v > bestVal) || (!findMax && v < bestVal))
                        {
                            bestVal = v;
                            bestCluster = i;
                            bestPack = j;
                            bestCell = k;
                        }
                    }
                }
            }

            Value = (float)bestVal;
            clusterId = bestCluster;
            packId = bestPack;
            cellId = bestCell;
        }

        private void CopyBatteryRackToIfServer(RackState rackState, BatteryManagementSystemData bmsData)
        {
            if(rackState == null || bmsData == null)
            { 
                return; 
            }
            // 更新堆数据
            bmsData.BatteryStacks[0].TotalVoltage = (float)rackState.TotalVoltage;
            bmsData.BatteryStacks[0].Current = (float)rackState.TotalCurrent;
            //bmsData.BatteryStacks[0].Power = (float)rackState.TotalEnergy;
            bmsData.BatteryStacks[0].Power = (float)rackState.TotalVoltage * (float)rackState.TotalCurrent / 1000.0f;
            bmsData.BatteryStacks[0].SOC = (float)rackState.MinClusterSOC;
            bmsData.BatteryStacks[0].SOH = (float)rackState.StateOfHealth;
            bmsData.BatteryStacks[0].Cycles = 98;
            //_bmsData.BatteryStacks[0].InsulationPlus 
            //_bmsData.BatteryStacks[0].InsulationMinus
            //_bmsData.BatteryStacks[0].OperationStatus 
            //从各簇的最大值中找到最大值

            float val;
            int cluseterId;
            int packId;
            int cellId;
            FindRackMaxCellValueAndId(
                rackState,
                1,
                out val,
                out cluseterId,
                out packId,
                out cellId
                );

            bmsData.BatteryStacks[0].MaxCellVoltage = val;
            bmsData.BatteryStacks[0].MaxCellVoltageClusterId = cluseterId;
            bmsData.BatteryStacks[0].MaxCellVoltagePackId = packId;
            bmsData.BatteryStacks[0].MaxCellVoltageCellId = cellId;

            FindRackMaxCellValueAndId(
                rackState,
                2,
                out val,
                out cluseterId,
                out packId,
                out cellId
                );

            bmsData.BatteryStacks[0].MinCellVoltage = val;
            bmsData.BatteryStacks[0].MinCellVoltageClusterId = cluseterId;
            bmsData.BatteryStacks[0].MinCellVoltagePackId = packId;
            bmsData.BatteryStacks[0].MinCellVoltageCellId = cellId;

            FindRackMaxCellValueAndId(
               rackState,
               3,
               out val,
               out cluseterId,
            out packId,
               out cellId
               );

            bmsData.BatteryStacks[0].MaxCellTemp = val;
            bmsData.BatteryStacks[0].MaxCellTempClusterId = cluseterId;
            bmsData.BatteryStacks[0].MaxCellTempPackId = packId;
            bmsData.BatteryStacks[0].MaxCellTempCellId = cellId;

            FindRackMaxCellValueAndId(
              rackState,
              4,
              out val,
              out cluseterId,
              out packId,
              out cellId
              );

            bmsData.BatteryStacks[0].MinCellTemp = val;
            bmsData.BatteryStacks[0].MinCellTempClusterId = cluseterId;
            bmsData.BatteryStacks[0].MinCellTempPackId = packId;
            bmsData.BatteryStacks[0].MinCellTempCellId = cellId;

            bmsData.BatteryStacks[0].AvgCellVoltage = (float)rackState.TotalVoltage / 416;
            bmsData.BatteryStacks[0].CellVoltageDiff = (float)rackState.VoltageDifference;

            bmsData.BatteryStacks[0].AvgCellTemp = (float)rackState.AvgClusterTemp;
            bmsData.BatteryStacks[0].CellTempDiff = (float)rackState.MaxClusterTemp - (float)rackState.MinClusterTemp;
            bmsData.BatteryStacks[0].MaxCellSOC = (float)rackState.MaxClusterSOC;
            bmsData.BatteryStacks[0].MinCellSOC = (float)rackState.MaxClusterSOC;
            _bmsData.BatteryStacks[0].CumulativeChargeEnergy = (float)rackState.TotalChargeEnergy;
            _bmsData.BatteryStacks[0].CumulativeDischargeEnergy = (float)rackState.TotalDischargeEnergy;
            //_bmsData.BatteryStacks[0].SingleChargeEnergy
            //_bmsData.BatteryStacks[0].SingleDischargeEnergy
            //_bmsData.BatteryStacks[0].DailyChargeEnergy
            //_bmsData.BatteryStacks[0].DailyDischargeEnergy
            //_bmsData.BatteryStacks[0].AvailableChargeEnergy
            //_bmsData.BatteryStacks[0].AvailableDischargeEnergy

            // 故障告警统计
            // bmsData.BatteryStacks[0].BMSProtectionSummary = rackState.ClusterStates.Any(c => c || c.HasModerateAlarms || c.HasMildAlarms);
            // bmsData.BatteryStacks[0].BMSAlarmSummary = rackState.ModerateAlarms;
            // bmsData.BatteryStacks[0].BMSFaultSummary = rackState.SevereAlarms;
        }

        private void UpdateBatteryRacksData(EnergyStorageSystem essData)
        {
            var BatteryRackState = essData._batteryRack.GetRackState();
            var BatteryRackState2 = essData._batteryRack2.GetRackState();

            CopyBatteryRackToIfServer(BatteryRackState, _bmsData);
            CopyBatteryRackToIfServer(BatteryRackState2, _bmsData2);

            // 反向更新故障状态到电池堆模拟器
            // 根据属性BMSFaultSummary， IsChargingFault， IsDischargingFault来设置堆的故障状态, BatteryRackState.IsFault的定义为0-无故障，1-充电故障，2-放电故障，3-其他故障
            if (_bmsData.BatteryStacks[0].BMSFaultSummary != 0)
            {
                if (_bmsData.BatteryStacks[0].IsChargeFault)
                {
                    BatteryRackState.IsFault = 1;
                    // 如果同时存在充电和放电故障，则设置为其他故障
                    if (_bmsData.BatteryStacks[0].IsDischargeFault)
                    {
                        BatteryRackState.IsFault = 3;
                    }
                }
                else if (_bmsData.BatteryStacks[0].IsDischargeFault)
                {
                    BatteryRackState.IsFault = 2;
                }
                else
                {
                    BatteryRackState.IsFault = 3;
                }
            }
            else
            {
                BatteryRackState.IsFault = 0;
            }

            if (_bmsData2.BatteryStacks[0].BMSFaultSummary != 0)
            {
                if (_bmsData2.BatteryStacks[0].IsChargeFault)
                {
                    BatteryRackState2.IsFault = 1;
                    // 如果同时存在充电和放电故障，则设置为其他故障
                    if (_bmsData2.BatteryStacks[0].IsDischargeFault)
                    {
                        BatteryRackState2.IsFault = 3;
                    }
                }
                else if (_bmsData2.BatteryStacks[0].IsDischargeFault)
                {
                    BatteryRackState2.IsFault = 2;
                }
                else
                {
                    BatteryRackState2.IsFault = 3;
                }
            }
            else
            {
                BatteryRackState2.IsFault = 0;
            }

            if (_bmsData.BatteryStacks[0].SOC >= 0.95f)
            {
                if (BatteryRackState2.IsFault == 0)
                {
                    BatteryRackState.IsFault = 1;
                }
                else if (BatteryRackState2.IsFault == 2)
                {
                    BatteryRackState.IsFault = 3;
                }
            }

            BatteryRackState.IsAlarm = _bmsData.BatteryStacks[0].BMSAlarmSummary != 0;
            BatteryRackState2.IsAlarm = _bmsData2.BatteryStacks[0].BMSAlarmSummary != 0;
            BatteryRackState.IsProtection = _bmsData.BatteryStacks[0].BMSProtectionSummary != 0;
            BatteryRackState2.IsProtection = _bmsData2.BatteryStacks[0].BMSProtectionSummary != 0;
        }

        private void UpdateBatteryClusterData(EnergyStorageSystem essData)
        {
            List<BatteryRackSimulator> batteryStacks = new List<BatteryRackSimulator>();
            batteryStacks.Add(essData._batteryRack);
            batteryStacks.Add(essData._batteryRack2);
            int bmsId = 0;
            foreach (var batteryRackSimulator in batteryStacks)
            {
                var BatteryClusterState = batteryRackSimulator.GetRackState().ClusterStates;
                var BatteryClusterConfig = batteryRackSimulator.GetRackConfig().ClusterConfig;

                var packSerialCount = batteryRackSimulator._clusters[0]._packs[0].GetPackConfiguration().SeriesCount;
                BatteryManagementSystemData? bmsData = null;

                if (bmsId == 0)
                {
                    bmsId++;
                    bmsData = _bmsData;
                }else
                if(bmsId == 1)
                {
                    bmsData = _bmsData2;
                }

                if(bmsData == null)
                {
                    return;
                }

                for (int i = 0; i < BatteryClusterState.Count; i++)
                {
                    Dictionary<int, float?> bmsCellVoltDict = bmsData.BatteryStacks[0].Cluseter[i].ClusterCellVoltages.CellVoltages;
                    Dictionary<int, float?> bmsCellTempDict = bmsData.BatteryStacks[0].Cluseter[i].ClusterCellTemperatures.CellTemperatures;
                    for (int j = 0; j < BatteryClusterConfig.PackCount; j++)
                    {
                        for (int k = 0; k < packSerialCount; k++)
                        {
                            if (bmsCellVoltDict.ContainsKey(j * packSerialCount + k))
                            {
                                bmsCellVoltDict[j * packSerialCount + k] = (float)BatteryClusterState[i].PackStates[j].CellStates[k].Voltage;
                                bmsCellTempDict[j * packSerialCount + k] = (float)BatteryClusterState[i].PackStates[j].CellStates[k].Temperature;
                            }
                            else
                            {
                                bmsCellVoltDict.Add(j * packSerialCount + k, (float)BatteryClusterState[i].PackStates[j].CellStates[k].Voltage);
                                bmsCellTempDict.Add(j * packSerialCount + k, (float)BatteryClusterState[i].PackStates[j].CellStates[k].Temperature);
                            }
                        }
                    }
                    //更新基础族信息
                    bmsData.BatteryStacks[0].Cluseter[i].Measurements.TotalVoltage = (float)BatteryClusterState[i].TotalVoltage;
                    bmsData.BatteryStacks[0].Cluseter[i].Measurements.Current = (float)BatteryClusterState[i].TotalCurrent;
                    bmsData.BatteryStacks[0].Cluseter[i].Measurements.SOC = (float)BatteryClusterState[i].AvgPackSOC;
                    bmsData.BatteryStacks[0].Cluseter[i].Measurements.Power = (float)BatteryClusterState[i].TotalVoltage * (float)BatteryClusterState[i].TotalCurrent;
                    bmsData.BatteryStacks[0].Cluseter[i].Measurements.SOH = (float)BatteryClusterState[i].StateOfHealth;
                    //_bmsData.BatteryStacks[0].Clusters[i].Measurements.InsulationPlus
                    //_bmsData.BatteryStacks[0].Clusters[i].Measurements.InsulationMinus
                    bmsData.BatteryStacks[0].Cluseter[i].Measurements.OperationStatus = BmsGetOperationStatusByCurrent((float)BatteryClusterState[i].TotalCurrent);

                    var maxEntry = bmsCellVoltDict.Aggregate((x, y) => x.Value > y.Value ? x : y);
                    bmsData.BatteryStacks[0].Cluseter[i].Measurements.MaxCellVoltage = maxEntry.Value;
                    bmsData.BatteryStacks[0].Cluseter[i].Measurements.MaxCellVoltageId = maxEntry.Key;

                    var minEntry = bmsCellVoltDict.Aggregate((x, y) => x.Value < y.Value ? x : y);
                    bmsData.BatteryStacks[0].Cluseter[i].Measurements.MinCellVoltage = minEntry.Value;
                    bmsData.BatteryStacks[0].Cluseter[i].Measurements.MinCellVoltageId = minEntry.Key;

                    bmsData.BatteryStacks[0].Cluseter[i].Measurements.AvgCellVoltage = (float)BatteryClusterState[i].TotalVoltage / 416;
                    //_bmsData.BatteryStacks[0].Clusters[i].Measurements.CellVoltageDiff = BatteryClusterState[i].

                    var maxTempEntry = bmsCellTempDict.Aggregate((x, y) => x.Value > y.Value ? x : y);
                    bmsData.BatteryStacks[0].Cluseter[i].Measurements.MaxCellTemp = maxTempEntry.Value;
                    bmsData.BatteryStacks[0].Cluseter[i].Measurements.MaxCellTempId = maxTempEntry.Key;

                    var minTempEntry = bmsCellTempDict.Aggregate((x, y) => x.Value < y.Value ? x : y);
                    bmsData.BatteryStacks[0].Cluseter[i].Measurements.MinCellTemp = minTempEntry.Value;
                    bmsData.BatteryStacks[0].Cluseter[i].Measurements.MinCellTempId = minTempEntry.Key;

                    bmsData.BatteryStacks[0].Cluseter[i].Measurements.AvgCellTemp = (float)BatteryClusterState[i].AvgPackTemp;
                    bmsData.BatteryStacks[0].Cluseter[i].Measurements.MaxCellSOC = (float)BatteryClusterState[i].MaxPackSOC;
                    bmsData.BatteryStacks[0].Cluseter[i].Measurements.MinCellSOC = (float)BatteryClusterState[i].MinPackSOC;
                    bmsData.BatteryStacks[0].Cluseter[i].Measurements.CellVoltageSum = bmsCellVoltDict.Values.Sum();

                    //使用UpdateStateForUnder和UpdateStateForOver方法更新告警状态
                    // 簇电压过低告警
                    var alarmState1 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.UndervoltageProtection;
                    var alarmState2 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.UndervoltageAlarm;
                    var alarmState3 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.UndervoltageFault;
                    UpdateStateForUnder(ref alarmState1,
                        ref alarmState2,
                        ref alarmState3,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.UndervoltageThreshold1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.UndervoltageThreshold2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.UndervoltageThreshold3!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.UndervoltageRecovery1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.UndervoltageRecovery2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.UndervoltageRecovery3!.Value,
                        (float)BatteryClusterState[i].TotalVoltage);
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.UndervoltageAlarm = alarmState2;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.UndervoltageFault = alarmState3;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.UndervoltageProtection = alarmState1;

                    // 簇电压过高告警
                    alarmState1 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.OvervoltageProtection;
                    alarmState2 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.OvervoltageAlarm;
                    alarmState3 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.OvervoltageFault;
                    UpdateStateForOver(ref alarmState1,
                        ref alarmState2,
                        ref alarmState3,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.OvervoltageThreshold1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.OvervoltageThreshold2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.OvervoltageThreshold3!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.OvervoltageRecovery1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.OvervoltageRecovery2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.OvervoltageRecovery3!.Value,
                        (float)BatteryClusterState[i].TotalVoltage);
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.OvervoltageProtection = alarmState1;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.OvervoltageAlarm = alarmState2;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.OvervoltageFault = alarmState3;

                    // 充电过流告警
                    alarmState1 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.ChargeOvercurrentProtection;
                    alarmState2 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.ChargeOvercurrentAlarm;
                    alarmState3 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.ChargeOvercurrentFault;
                    UpdateStateForOver(ref alarmState1,
                        ref alarmState2,
                        ref alarmState3,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.ChargeOvercurrentThreshold1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.ChargeOvercurrentThreshold2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.ChargeOvercurrentThreshold3!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.ChargeOvercurrentRecovery1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.ChargeOvercurrentRecovery2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.ChargeOvercurrentRecovery3!.Value,
                        (float)(-BatteryClusterState[i].TotalCurrent));
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.ChargeOvercurrentProtection = alarmState1;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.ChargeOvercurrentAlarm = alarmState2;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.ChargeOvercurrentFault = alarmState3;

                    // 放电过流告警
                    alarmState1 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.DischargeOvercurrentProtection;
                    alarmState2 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.DischargeOvercurrentAlarm;
                    alarmState3 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.DischargeOvercurrentFault;
                    UpdateStateForOver(ref alarmState1,
                        ref alarmState2,
                        ref alarmState3,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.DischargeOvercurrentThreshold1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.DischargeOvercurrentThreshold2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.DischargeOvercurrentThreshold3!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.DischargeOvercurrentRecovery1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.DischargeOvercurrentRecovery2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.DischargeOvercurrentRecovery3!.Value,
                        (float)BatteryClusterState[i].TotalCurrent);
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.DischargeOvercurrentProtection = alarmState1;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.DischargeOvercurrentAlarm = alarmState2;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.DischargeOvercurrentFault = alarmState3;

                    // 获取每个pack的最低单体电压，并计算出簇的最低单体电压
                    var minCellVoltList = new List<float>();
                    var maxCellVoltList = new List<float>();
                    for (int j = 0; j < BatteryClusterConfig.PackCount; j++)
                    {
                        minCellVoltList.Add((float)BatteryClusterState[i].PackStates[j].MinCellVoltage);
                        maxCellVoltList.Add((float)BatteryClusterState[i].PackStates[j].MaxCellVoltage);
                    }
                    // 单体电压过低告警
                    alarmState1 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellUnderVoltageProtection;
                    alarmState2 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellUnderVoltageAlarm;
                    alarmState3 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellUnderVoltageFault;
                    UpdateStateForUnder(ref alarmState1,
                        ref alarmState2,
                        ref alarmState3,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellUndervoltageThreshold1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellUndervoltageThreshold2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellUndervoltageThreshold3!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellUndervoltageRecovery1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellUndervoltageRecovery2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellUndervoltageRecovery3!.Value,
                        minCellVoltList.Min());
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellUnderVoltageProtection = alarmState1;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellUnderVoltageAlarm = alarmState2;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellUnderVoltageFault = alarmState3;

                    // 单体电压过高告警
                    alarmState1 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellOverVoltageProtection;
                    alarmState2 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellOverVoltageAlarm;
                    alarmState3 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellOverVoltageFault;
                    UpdateStateForOver(ref alarmState1,
                        ref alarmState2,
                        ref alarmState3,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellOvervoltageThreshold1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellOvervoltageThreshold2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellOvervoltageThreshold3!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellOvervoltageRecovery1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellOvervoltageRecovery2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellOvervoltageRecovery3!.Value,
                        maxCellVoltList.Max());
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellOverVoltageProtection = alarmState1;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellOverVoltageAlarm = alarmState2;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellOverVoltageFault = alarmState3;

                    // 单体压差过大告警
                    alarmState1 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.VoltageDifferenceProtection;
                    alarmState2 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.VoltageDifferenceAlarm;
                    alarmState3 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.VoltageDifferenceFault;
                    UpdateStateForOver(ref alarmState1,
                        ref alarmState2,
                        ref alarmState3,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellVoltageDifferenceThreshold1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellVoltageDifferenceThreshold2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellVoltageDifferenceThreshold3!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellVoltageDifferenceRecovery1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellVoltageDifferenceRecovery2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellVoltageDifferenceRecovery3!.Value,
                        maxCellVoltList.Max() - minCellVoltList.Min());
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.VoltageDifferenceProtection = alarmState1;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.VoltageDifferenceAlarm = alarmState2;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.VoltageDifferenceFault = alarmState3;

                    // 获取每个pack的最低单体温度，并计算出簇的最低单体温度
                    var minCellTempList = new List<float>();
                    var maxCellTempList = new List<float>();
                    for (int j = 0; j < BatteryClusterConfig.PackCount; j++)
                    {
                        minCellTempList.Add((float)BatteryClusterState[i].PackStates[j].MinCellTemp);
                        maxCellTempList.Add((float)BatteryClusterState[i].PackStates[j].MaxCellTemp);
                    }
                    // 单体温差过大告警
                    alarmState1 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.TempDifferenceProtection;
                    alarmState2 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.TempDifferenceAlarm;
                    alarmState3 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.TempDifferenceFault;
                    UpdateStateForOver(ref alarmState1,
                        ref alarmState2,
                        ref alarmState3,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellTempDifferenceThreshold1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellTempDifferenceThreshold2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellTempDifferenceThreshold3!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellTempDifferenceRecovery1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellTempDifferenceRecovery2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.CellTempDifferenceRecovery3!.Value,
                        maxCellTempList.Max() - minCellTempList.Min());
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.TempDifferenceProtection = alarmState1;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.TempDifferenceAlarm = alarmState2;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.TempDifferenceAlarm = alarmState3;

                    // SOC过低告警
                    alarmState1 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.LowSOCProtection;
                    alarmState2 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.LowSOCAlarm;
                    alarmState3 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.LowSOCFault;
                    UpdateStateForUnder(ref alarmState1,
                        ref alarmState2,
                        ref alarmState3,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.LowSOCTreshold1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.LowSOCTreshold2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.LowSOCTreshold3!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.LowSOCRecovery1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.LowSOCRecovery2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.LowSOCRecovery3!.Value,
                        (float)BatteryClusterState[i].MinPackSOC);
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.LowSOCProtection = alarmState1;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.LowSOCAlarm = alarmState2;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.LowSOCFault = alarmState3;

                    // 充电温度过高告警
                    alarmState1 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellChargeHighTempProtection;
                    alarmState2 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellChargeHighTempAlarm;
                    alarmState3 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellChargeHighTempFault;
                    UpdateStateForOver(ref alarmState1,
                        ref alarmState2,
                        ref alarmState3,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.ChargeHighTempThreshold1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.ChargeHighTempThreshold2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.ChargeHighTempThreshold3!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.ChargeHighTempRecovery1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.ChargeHighTempRecovery2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.ChargeHighTempRecovery3!.Value,
                        maxCellTempList.Max());
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellChargeHighTempProtection = alarmState1;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellChargeHighTempAlarm = alarmState2;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellChargeHighTempFault = alarmState3;

                    // 充电温度过低告警
                    alarmState1 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellChargeLowTempProtection;
                    alarmState2 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellChargeLowTempAlarm;
                    alarmState3 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellChargeLowTempFault;
                    UpdateStateForUnder(ref alarmState1,
                        ref alarmState2,
                        ref alarmState3,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.ChargeLowTempThreshold1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.ChargeLowTempThreshold2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.ChargeLowTempThreshold3!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.ChargeLowTempRecovery1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.ChargeLowTempRecovery2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.ChargeLowTempRecovery3!.Value,
                        minCellTempList.Min());
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellChargeLowTempProtection = alarmState1;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellChargeLowTempAlarm = alarmState2;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellChargeLowTempFault = alarmState3;

                    // 绝缘值过低告警
                    alarmState1 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.InsulationProtection;
                    alarmState2 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.InsulationAlarm;
                    alarmState3 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.InsulationFault;
                    UpdateStateForUnder(ref alarmState1,
                        ref alarmState2,
                        ref alarmState3,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.InsulationThreshold1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.InsulationThreshold2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.InsulationThreshold3!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.InsulationRecovery1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.InsulationRecovery2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.InsulationRecovery3!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Measurements.Insulation!.Value);
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.InsulationProtection = alarmState1;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.InsulationAlarm = alarmState2;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.InsulationFault = alarmState3;

                    // 放电温度过高告警
                    alarmState1 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellDischargeHighTempProtection;
                    alarmState2 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellDischargeHighTempAlarm;
                    alarmState3 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellDischargeHighTempFault;
                    UpdateStateForOver(ref alarmState1,
                        ref alarmState2,
                        ref alarmState3,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.DischargeHighTempThreshold1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.DischargeHighTempThreshold2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.DischargeHighTempThreshold3!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.DischargeHighTempRecovery1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.DischargeHighTempRecovery2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.DischargeHighTempRecovery3!.Value,
                        maxCellTempList.Max());
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellDischargeHighTempProtection = alarmState1;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellDischargeHighTempAlarm = alarmState2;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellDischargeHighTempFault = alarmState3;

                    // 放电温度过低告警
                    alarmState1 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellDischargeLowTempProtection;
                    alarmState2 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellDischargeLowTempAlarm;
                    alarmState3 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellDischargeLowTempFault;
                    UpdateStateForUnder(ref alarmState1,
                        ref alarmState2,
                        ref alarmState3,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.DischargeLowTempThreshold1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.DischargeLowTempThreshold2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.DischargeLowTempThreshold3!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.DischargeLowTempRecovery1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.DischargeLowTempRecovery2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.DischargeLowTempRecovery3!.Value,
                        minCellTempList.Min());
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellDischargeLowTempProtection = alarmState1;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellDischargeLowTempAlarm = alarmState2;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.CellDischargeLowTempFault = alarmState3;

                    // 高压箱连接器温度过高告警
                    alarmState1 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.BatteryBoxBusbarHighTempProtection;
                    alarmState2 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.BatteryBoxBusbarHighTempAlarm;
                    alarmState3 = bmsData.BatteryStacks[0].Cluseter[i].Alarms.BatteryBoxBusbarHighTempFault;
                    UpdateStateForOver(ref alarmState1,
                        ref alarmState2,
                        ref alarmState3,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.HVBHighTempThreshold1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.HVBHighTempThreshold2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.HVBHighTempThreshold3!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.HVBHighTempRecovery1!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.HVBHighTempRecovery2!.Value,
                        bmsData.BatteryStacks[0].Cluseter[i].Thresholds.HVBHighTempRecovery3!.Value,
                        26.0f);// 假设连接器温度为26.0度的固定值
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.BatteryBoxBusbarHighTempProtection = alarmState1;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.BatteryBoxBusbarHighTempAlarm = alarmState2;
                    bmsData.BatteryStacks[0].Cluseter[i].Alarms.BatteryBoxBusbarHighTempFault = alarmState3;
                }
            }
        }

        public void UpdateStateForUnder(ref bool? alarmState1, ref bool? alarmState2, ref bool? alarmState3, float alarm1Threshold, float alarm2Threshold, float alarm3Threshold,
            float recover1Threshold, float recover2Threshold, float recover3Threshold, double clusterVoltage)
        {
            if (alarmState3 == true)// 三级告警
            {
                if (clusterVoltage > recover3Threshold)
                {
                    alarmState3 = false;
                    alarmState2 = true; // 降级到二级告警
                }
            }
            else if (alarmState2 == true)// 二级告警
            {
                if (clusterVoltage <= alarm3Threshold) 
                {
                    alarmState3 = true;// 升级到三级告警
                    alarmState2 = false;
                }
                else if (clusterVoltage > recover2Threshold) 
                {
                    alarmState2 = false;
                    alarmState1 = true; // 降级到一级告警
                }
            }
            else if (alarmState1 == true)// 一级告警
            {
                if (clusterVoltage <= alarm2Threshold)
                {
                    alarmState2 = true;// 升级到二级告警
                    alarmState1 = false;   
                }
                else if (clusterVoltage > recover1Threshold) 
                {
                    alarmState1 = false;// 恢复正常
                }
            }
            else// 无告警
            {
                if (clusterVoltage <= alarm1Threshold)
                {
                    alarmState1 = true;// 升级到一级告警
                }
            }
        }

        public void UpdateStateForOver(ref bool? alarmState1, ref bool? alarmState2, ref bool? alarmState3, float alarm1Threshold, float alarm2Threshold, float alarm3Threshold,
            float recover1Threshold, float recover2Threshold, float recover3Threshold, double clusterVoltage)
        {
            if (alarmState3 == true)// 三级告警
            {
                if (clusterVoltage < recover3Threshold)
                {
                    alarmState3 = false;
                    alarmState2 = true; // 降级到二级告警
                }
            }
            else if (alarmState2 == true)// 二级告警
            {
                if (clusterVoltage >= alarm3Threshold) 
                {
                    alarmState3 = true;// 升级到三级告警
                    alarmState2 = false;
                }
                else if (clusterVoltage < recover2Threshold) 
                {
                    alarmState2 = false;
                    alarmState1 = true; // 降级到一级告警
                }
            }
            else if (alarmState1 == true)// 一级告警
            {
                if (clusterVoltage >= alarm2Threshold)
                {
                    alarmState2 = true;// 升级到二级告警
                    alarmState1 = false;   
                }
                else if (clusterVoltage < recover1Threshold) 
                {
                    alarmState1 = false;// 恢复正常
                }
            }
            else// 无告警
            {
                if (clusterVoltage >= alarm1Threshold)
                {
                    alarmState1 = true;// 升级到一级告警
                }
            }
        }

        private void UpdateFromEssData(EnergyStorageSystem essData)
        {
            UpdateBatteryRacksData(essData);
            UpdateBatteryClusterData(essData);

            // 更新时间戳
            _bmsData.Timestamp = DateTime.Now;
        }
    }
}
