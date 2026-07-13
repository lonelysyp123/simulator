using EssSimulator.Display;

namespace EssSimulator.Web
{
    /// <summary>电池舱总览快照（对应 TUI DrawBatteryInfo 的总览表）。</summary>
    public sealed class BatteryOverviewDto
    {
        public int UnitIndex { get; set; }      // 0-based
        public int UnitNumber => UnitIndex + 1;
        public double TotalVoltage { get; set; }
        public double TotalCurrent { get; set; }
        public double SOC { get; set; }
        public double SOH { get; set; }
        public double MaxCellVoltage { get; set; }
        public double MinCellVoltage { get; set; }
        public int MaxCellVoltageClusterId { get; set; }
        public int MaxCellVoltagePackId { get; set; }
        public int MaxCellVoltageCellId { get; set; }
        public int MinCellVoltageClusterId { get; set; }
        public int MinCellVoltagePackId { get; set; }
        public int MinCellVoltageCellId { get; set; }
        public string GridConnectStatus { get; set; } = "";
        public string BlackStartModeStatus { get; set; } = "";
        public List<ClusterDto> Clusters { get; set; } = new();
    }

    public sealed class ClusterDto
    {
        public int ClusterId { get; set; }
        public double TotalVoltage { get; set; }
        public double TotalCurrent { get; set; }
        public double PowerKw { get; set; }
        public double SOC { get; set; }
        public double SOH { get; set; }
        public double AvgCellVoltage { get; set; }
        public double MaxCellVoltage { get; set; }
        public double MinCellVoltage { get; set; }
    }

    /// <summary>电池单体电压快照：4 包 × 104 节。</summary>
    public sealed class CellVoltageDto
    {
        public int UnitIndex { get; set; }
        public int ClusterId { get; set; }
        public int PackCount { get; set; }
        public int CellsPerPack { get; set; }
        /// <summary>按 pack 分组的单体电压（单位 V）；外层索引=包号，内层=单体在该包内的序号。</summary>
        public List<List<float>> Packs { get; set; } = new();
        public double MinCellVoltage { get; set; }
        public double MaxCellVoltage { get; set; }
        public int MaxCellVoltagePackId { get; set; }
        public int MaxCellVoltageCellId { get; set; }
        public int MinCellVoltagePackId { get; set; }
        public int MinCellVoltageCellId { get; set; }
    }

    public static class BatterySnapshotReader
    {
        public static BatteryOverviewDto ReadOverview(int unitIndex0)
        {
            string basePath = $"bms{unitIndex0 + 1}.BatteryStacks[0]";

            double totVolt = GuiSimDataAccess.SafeGetDouble($"{basePath}.TotalVoltage");
            double totCurr = GuiSimDataAccess.SafeGetDouble($"{basePath}.Current");
            double soc = GuiSimDataAccess.SafeGetDouble($"{basePath}.SOC") * 100;
            double soh = GuiSimDataAccess.SafeGetDouble($"{basePath}.SOH") * 100;
            double maxCellV = GuiSimDataAccess.SafeGetDouble($"{basePath}.MaxCellVoltage");
            double minCellV = GuiSimDataAccess.SafeGetDouble($"{basePath}.MinCellVoltage");
            int maxClusterId = (int)GuiSimDataAccess.SafeGetDouble($"{basePath}.MaxCellVoltageClusterId");
            int maxPackId = (int)GuiSimDataAccess.SafeGetDouble($"{basePath}.MaxCellVoltagePackId");
            int maxCellId = (int)GuiSimDataAccess.SafeGetDouble($"{basePath}.MaxCellVoltageCellId");
            int minClusterId = (int)GuiSimDataAccess.SafeGetDouble($"{basePath}.MinCellVoltageClusterId");
            int minPackId = (int)GuiSimDataAccess.SafeGetDouble($"{basePath}.MinCellVoltagePackId");
            int minCellId = (int)GuiSimDataAccess.SafeGetDouble($"{basePath}.MinCellVoltageCellId");

            var dto = new BatteryOverviewDto
            {
                UnitIndex = unitIndex0,
                TotalVoltage = totVolt,
                TotalCurrent = totCurr,
                SOC = soc,
                SOH = soh,
                MaxCellVoltage = maxCellV,
                MinCellVoltage = minCellV,
                MaxCellVoltageClusterId = maxClusterId,
                MaxCellVoltagePackId = maxPackId,
                MaxCellVoltageCellId = maxCellId,
                MinCellVoltageClusterId = minClusterId,
                MinCellVoltagePackId = minPackId,
                MinCellVoltageCellId = minCellId,
                GridConnectStatus = GuiStatusFormatters.FormatGridConnectStatus(unitIndex0),
                BlackStartModeStatus = GuiStatusFormatters.FormatBlackStartModeStatus(unitIndex0)
            };

            int clusterCount = GuiSimDataAccess.GetClusterCount();
            for (int i = 0; i < clusterCount; i++)
            {
                double cCurr = GuiSimDataAccess.SafeGetDouble($"{basePath}.Cluseter[{i}].Measurements.Current");
                double cVolt = GuiSimDataAccess.SafeGetDouble($"{basePath}.Cluseter[{i}].Measurements.TotalVoltage");
                double cSoc = GuiSimDataAccess.SafeGetDouble($"{basePath}.Cluseter[{i}].Measurements.SOC") * 100;
                double cSoh = GuiSimDataAccess.SafeGetDouble($"{basePath}.Cluseter[{i}].Measurements.SOH") * 100;
                double cAvg = GuiSimDataAccess.SafeGetDouble($"{basePath}.Cluseter[{i}].Measurements.AvgCellVoltage");
                double cMax = GuiSimDataAccess.SafeGetDouble($"{basePath}.Cluseter[{i}].Measurements.MaxCellVoltage");
                double cMin = GuiSimDataAccess.SafeGetDouble($"{basePath}.Cluseter[{i}].Measurements.MinCellVoltage");

                dto.Clusters.Add(new ClusterDto
                {
                    ClusterId = i,
                    TotalVoltage = cVolt,
                    TotalCurrent = cCurr,
                    PowerKw = cCurr * cVolt / 1000.0,
                    SOC = cSoc,
                    SOH = cSoh,
                    AvgCellVoltage = cAvg,
                    MaxCellVoltage = cMax,
                    MinCellVoltage = cMin
                });
            }

            return dto;
        }

        public static CellVoltageDto ReadCells(int unitIndex0, int clusterId)
        {
            string basePath = $"bms{unitIndex0 + 1}.BatteryStacks[0]";
            int clusterCount = Math.Max(1, GuiSimDataAccess.GetClusterCount());
            clusterId = Math.Clamp(clusterId, 0, clusterCount - 1);

            const int packCount = 4;
            const int cellsPerPack = 104;
            string clusterPath = $"{basePath}.Cluseter[{clusterId}]";

            var dto = new CellVoltageDto
            {
                UnitIndex = unitIndex0,
                ClusterId = clusterId,
                PackCount = packCount,
                CellsPerPack = cellsPerPack,
                MaxCellVoltage = GuiSimDataAccess.SafeGetDouble($"{clusterPath}.Measurements.MaxCellVoltage"),
                MinCellVoltage = GuiSimDataAccess.SafeGetDouble($"{clusterPath}.Measurements.MinCellVoltage")
            };

            int maxIdFlat = (int)GuiSimDataAccess.SafeGetDouble($"{clusterPath}.Measurements.MaxCellVoltageId");
            int minIdFlat = (int)GuiSimDataAccess.SafeGetDouble($"{clusterPath}.Measurements.MinCellVoltageId");
            dto.MaxCellVoltagePackId = maxIdFlat / cellsPerPack;
            dto.MaxCellVoltageCellId = maxIdFlat % cellsPerPack;
            dto.MinCellVoltagePackId = minIdFlat / cellsPerPack;
            dto.MinCellVoltageCellId = minIdFlat % cellsPerPack;

            for (int pack = 0; pack < packCount; pack++)
            {
                var packCells = new List<float>(cellsPerPack);
                for (int c = 0; c < cellsPerPack; c++)
                {
                    int cellIdx = pack * cellsPerPack + c;
                    float v = 0;
                    try
                    {
                        v = (float)SimServer.GetExtIfVariableVal(
                            $"{clusterPath}.ClusterCellVoltages.CellVoltages[{cellIdx}]");
                    }
                    catch { v = 0; }

                    packCells.Add(v);
                }
                dto.Packs.Add(packCells);
            }

            return dto;
        }
    }
}
