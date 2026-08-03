namespace EssSimulator.Web.Topology
{
    /// <summary>内置组态基础模板（电网 / 母线 / 变压器 / 电表 / EMU / BMS / 直流母线）。</summary>
    public static class TopologyTemplates
    {
        public static IReadOnlyList<TopologyTemplate> All { get; } = BuildAll();

        public static TopologyTemplate? Get(string id) =>
            All.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));

        private static IReadOnlyList<TopologyTemplate> BuildAll() => new List<TopologyTemplate>
        {
            BuildGrid(),
            BuildAcBus(),
            BuildTransformer(),
            BuildAcMeter(),
            BuildEmu(),
            BuildBms(),
            BuildDcBus()
        };

        private static TopologyTemplate BuildGrid() => new()
        {
            Id = "grid",
            Name = "电网",
            Category = "电源",
            Description = "三相电压源。三个拐角表示 A/B/C 相，默认 220kV，可通过参数修改输出电压。",
            IsVoltageSource = true,
            Ports =
            {
                new() { Id = "a", Label = "A", Kind = "ac_phase", Phase = "A", Side = "bottom", Offset = 0.2, VoltageParam = "outputVoltage", IsVoltageSourcePort = true },
                new() { Id = "b", Label = "B", Kind = "ac_phase", Phase = "B", Side = "bottom", Offset = 0.5, VoltageParam = "outputVoltage", IsVoltageSourcePort = true },
                new() { Id = "c", Label = "C", Kind = "ac_phase", Phase = "C", Side = "bottom", Offset = 0.8, VoltageParam = "outputVoltage", IsVoltageSourcePort = true }
            },
            Parameters =
            {
                new() { Key = "outputVoltage", Label = "输出线电压", Type = "number", Unit = "V", Min = 100, Description = "三相线电压额定值" },
                new() { Key = "frequencyHz", Label = "频率", Type = "number", Unit = "Hz", Min = 45, Max = 65 },
                new() { Key = "shortCircuitMva", Label = "短路容量", Type = "number", Unit = "MVA", Min = 1 }
            },
            DefaultParameters = new Dictionary<string, object?>
            {
                ["outputVoltage"] = 220000d,
                ["frequencyHz"] = 50d,
                ["shortCircuitMva"] = 750d
            }
        };

        private static TopologyTemplate BuildAcBus() => new()
        {
            Id = "ac_bus",
            Name = "三相母线",
            Category = "母线",
            Description = "由 A/B/C 三条独立线组成。上侧拐角每相仅接一台电压源（电网或变压器二次侧）；下侧拐角可挂多台负荷设备。无电压源时下侧拒绝接入；同一母线禁止两个电压源。",
            IsVoltageSource = false,
            Ports =
            {
                new() { Id = "a", Label = "A", Kind = "ac_phase", Phase = "A", Side = "top", Offset = 0.2 },
                new() { Id = "b", Label = "B", Kind = "ac_phase", Phase = "B", Side = "top", Offset = 0.5 },
                new() { Id = "c", Label = "C", Kind = "ac_phase", Phase = "C", Side = "top", Offset = 0.8 },
                new() { Id = "a2", Label = "A'", Kind = "ac_phase", Phase = "A", Side = "bottom", Offset = 0.2 },
                new() { Id = "b2", Label = "B'", Kind = "ac_phase", Phase = "B", Side = "bottom", Offset = 0.5 },
                new() { Id = "c2", Label = "C'", Kind = "ac_phase", Phase = "C", Side = "bottom", Offset = 0.8 }
            },
            Parameters =
            {
                new() { Key = "nominalVoltage", Label = "额定电压（只读/推导）", Type = "number", Unit = "V", Description = "由接入电压源决定；也可预填期望值用于匹配校验" },
                new() { Key = "name", Label = "名称", Type = "string" }
            },
            DefaultParameters = new Dictionary<string, object?>
            {
                ["nominalVoltage"] = 0d,
                ["name"] = "AC母线"
            }
        };

        private static TopologyTemplate BuildTransformer() => new()
        {
            Id = "transformer",
            Name = "变压器",
            Category = "变电",
            Description = "上下各三相拐角。通过一次/二次电压配置成各类变压器，要求上大下小。一次侧从母线取电（非电压源），二次侧向下游母线供电；接入母线时该侧电压须与母线匹配。",
            IsVoltageSource = false,
            Ports =
            {
                // 一次侧：受电端，不计入母线电压源
                new() { Id = "pri_a", Label = "H-A", Kind = "ac_phase", Phase = "A", Side = "top", Offset = 0.2, VoltageParam = "primaryVoltage", IsVoltageSourcePort = false },
                new() { Id = "pri_b", Label = "H-B", Kind = "ac_phase", Phase = "B", Side = "top", Offset = 0.5, VoltageParam = "primaryVoltage", IsVoltageSourcePort = false },
                new() { Id = "pri_c", Label = "H-C", Kind = "ac_phase", Phase = "C", Side = "top", Offset = 0.8, VoltageParam = "primaryVoltage", IsVoltageSourcePort = false },
                // 二次侧：向低压母线供电
                new() { Id = "sec_a", Label = "L-A", Kind = "ac_phase", Phase = "A", Side = "bottom", Offset = 0.2, VoltageParam = "secondaryVoltage", IsVoltageSourcePort = true },
                new() { Id = "sec_b", Label = "L-B", Kind = "ac_phase", Phase = "B", Side = "bottom", Offset = 0.5, VoltageParam = "secondaryVoltage", IsVoltageSourcePort = true },
                new() { Id = "sec_c", Label = "L-C", Kind = "ac_phase", Phase = "C", Side = "bottom", Offset = 0.8, VoltageParam = "secondaryVoltage", IsVoltageSourcePort = true }
            },
            Parameters =
            {
                new() { Key = "primaryVoltage", Label = "一次（上）线电压", Type = "number", Unit = "V", Min = 100 },
                new() { Key = "secondaryVoltage", Label = "二次（下）线电压", Type = "number", Unit = "V", Min = 100 },
                new() { Key = "ratedPowerKva", Label = "额定容量", Type = "number", Unit = "kVA", Min = 1 },
                new() { Key = "impedancePercent", Label = "短路阻抗", Type = "number", Unit = "%", Min = 0.1 },
                new() { Key = "noLoadLossW", Label = "空载损耗", Type = "number", Unit = "W", Min = 0 },
                new() { Key = "loadLossW", Label = "负载损耗", Type = "number", Unit = "W", Min = 0 }
            },
            DefaultParameters = new Dictionary<string, object?>
            {
                ["primaryVoltage"] = 220000d,
                ["secondaryVoltage"] = 35000d,
                ["ratedPowerKva"] = 31500d,
                ["impedancePercent"] = 4d,
                ["noLoadLossW"] = 100d,
                ["loadLossW"] = 200d
            }
        };

        /// <summary>
        /// 三相电表：PT 测压 + CT 测流（贴近真实电能表接线）。
        /// PT 三相接母线相线测电压；CT 三相接同一母线测电流（仿真中为母线测量抽头）。
        /// </summary>
        private static TopologyTemplate BuildAcMeter() => new()
        {
            Id = "ac_meter",
            Name = "三相电表",
            Category = "测量",
            Description = "经 PT/CT 测量母线电压与电流。上方 PT(A/B/C) 接母线相电压；下方 CT(A/B/C) 接同一母线电流抽头。PT 一次额定须匹配母线电压。",
            IsVoltageSource = false,
            Ports =
            {
                new() { Id = "pt_a", Label = "PT-A", Kind = "ac_phase", Phase = "A", Side = "top", Offset = 0.2, VoltageParam = "ptPrimaryVoltage" },
                new() { Id = "pt_b", Label = "PT-B", Kind = "ac_phase", Phase = "B", Side = "top", Offset = 0.5, VoltageParam = "ptPrimaryVoltage" },
                new() { Id = "pt_c", Label = "PT-C", Kind = "ac_phase", Phase = "C", Side = "top", Offset = 0.8, VoltageParam = "ptPrimaryVoltage" },
                new() { Id = "ct_a", Label = "CT-A", Kind = "ac_phase", Phase = "A", Side = "bottom", Offset = 0.2 },
                new() { Id = "ct_b", Label = "CT-B", Kind = "ac_phase", Phase = "B", Side = "bottom", Offset = 0.5 },
                new() { Id = "ct_c", Label = "CT-C", Kind = "ac_phase", Phase = "C", Side = "bottom", Offset = 0.8 }
            },
            Parameters =
            {
                new() { Key = "ptPrimaryVoltage", Label = "PT 一次线电压", Type = "number", Unit = "V", Min = 100, Description = "须与被测母线电压匹配" },
                new() { Key = "ptSecondaryVoltage", Label = "PT 二次线电压", Type = "number", Unit = "V", Min = 1 },
                new() { Key = "ctPrimaryCurrent", Label = "CT 一次电流", Type = "number", Unit = "A", Min = 1 },
                new() { Key = "ctSecondaryCurrent", Label = "CT 二次电流", Type = "number", Unit = "A", Min = 1 },
                new() { Key = "accuracyClass", Label = "精度等级", Type = "string" }
            },
            DefaultParameters = new Dictionary<string, object?>
            {
                ["ptPrimaryVoltage"] = 220000d,
                ["ptSecondaryVoltage"] = 100d,
                ["ctPrimaryCurrent"] = 2000d,
                ["ctSecondaryCurrent"] = 5d,
                ["accuracyClass"] = "0.2S"
            }
        };

        private static TopologyTemplate BuildEmu() => new()
        {
            Id = "emu",
            Name = "EMU 储能单元",
            Category = "储能",
            Description = "内部简化为：高压断路器 + 35kV/690V 变压器 + 两台变流器 + 低压断路器。上三相 AC，下正/负两路 DC。",
            IsVoltageSource = false,
            Ports =
            {
                new() { Id = "ac_a", Label = "AC-A", Kind = "ac_phase", Phase = "A", Side = "top", Offset = 0.2, VoltageParam = "acVoltage" },
                new() { Id = "ac_b", Label = "AC-B", Kind = "ac_phase", Phase = "B", Side = "top", Offset = 0.5, VoltageParam = "acVoltage" },
                new() { Id = "ac_c", Label = "AC-C", Kind = "ac_phase", Phase = "C", Side = "top", Offset = 0.8, VoltageParam = "acVoltage" },
                new() { Id = "dc_pos", Label = "DC+", Kind = "dc_pos", Side = "bottom", Offset = 0.35 },
                new() { Id = "dc_neg", Label = "DC−", Kind = "dc_neg", Side = "bottom", Offset = 0.65 }
            },
            Parameters =
            {
                new() { Key = "acVoltage", Label = "交流侧线电压", Type = "number", Unit = "V", Min = 100, Description = "接入 AC 母线侧额定，默认 35kV" },
                new() { Key = "unitXfPrimaryV", Label = "单元变一次电压", Type = "number", Unit = "V" },
                new() { Key = "unitXfSecondaryV", Label = "单元变二次电压", Type = "number", Unit = "V" },
                new() { Key = "pcsRatedPowerKw", Label = "单台 PCS 额定功率", Type = "number", Unit = "kW" },
                new() { Key = "pcsMaxPowerKw", Label = "单台 PCS 最大功率", Type = "number", Unit = "kW" },
                new() { Key = "pcsEfficiency", Label = "PCS 效率", Type = "number", Min = 0.5, Max = 1 },
                new() { Key = "pcsCount", Label = "变流器数量", Type = "number", Min = 1, Max = 2 },
                new() { Key = "dcVoltageMin", Label = "直流电压下限", Type = "number", Unit = "V" },
                new() { Key = "dcVoltageMax", Label = "直流电压上限", Type = "number", Unit = "V" }
            },
            DefaultParameters = new Dictionary<string, object?>
            {
                ["acVoltage"] = 35000d,
                ["unitXfPrimaryV"] = 35000d,
                ["unitXfSecondaryV"] = 690d,
                ["pcsRatedPowerKw"] = 1725d,
                ["pcsMaxPowerKw"] = 1897.5d,
                ["pcsEfficiency"] = 0.99d,
                ["pcsCount"] = 2d,
                ["dcVoltageMin"] = 1000d,
                ["dcVoltageMax"] = 1500d
            }
        };

        private static TopologyTemplate BuildBms() => new()
        {
            Id = "bms",
            Name = "BMS 电池堆",
            Category = "储能",
            Description = "电池堆模型：电芯→Pack→簇→堆。参数对齐现有 BmsDeviceConfig。上方正/负两路 DC 拐角。",
            IsVoltageSource = false,
            Ports =
            {
                new() { Id = "dc_pos", Label = "DC+", Kind = "dc_pos", Side = "top", Offset = 0.35 },
                new() { Id = "dc_neg", Label = "DC−", Kind = "dc_neg", Side = "top", Offset = 0.65 }
            },
            Parameters =
            {
                new() { Key = "name", Label = "名称", Type = "string" },
                new() { Key = "clusterCount", Label = "簇数", Type = "number", Min = 1 },
                new() { Key = "packCount", Label = "每簇 Pack 数", Type = "number", Min = 1 },
                new() { Key = "cellSeriesCount", Label = "Pack 串联电芯数", Type = "number", Min = 1 },
                new() { Key = "cellParallelCount", Label = "并联数", Type = "number", Min = 1 },
                new() { Key = "cellNominalVoltage", Label = "电芯标称电压", Type = "number", Unit = "V" },
                new() { Key = "cellNominalCapacity", Label = "电芯标称容量", Type = "number", Unit = "Ah" },
                new() { Key = "cellInitialSoc", Label = "初始 SOC", Type = "number", Min = 0, Max = 1 },
                new() { Key = "rackInternalResistance", Label = "堆内阻", Type = "number", Unit = "Ω" }
            },
            DefaultParameters = new Dictionary<string, object?>
            {
                ["name"] = "BMS",
                ["clusterCount"] = 12d,
                ["packCount"] = 4d,
                ["cellSeriesCount"] = 104d,
                ["cellParallelCount"] = 1d,
                ["cellNominalVoltage"] = 3.2d,
                ["cellNominalCapacity"] = 314d,
                ["cellInitialSoc"] = 0.5d,
                ["rackInternalResistance"] = 0.02d
            }
        };

        private static TopologyTemplate BuildDcBus() => new()
        {
            Id = "dc_bus",
            Name = "直流母线",
            Category = "母线",
            Description = "直流母线：上下各一对正/负拐角，同极性可挂多台设备；正极只能接正极，负极只能接负极。",
            IsVoltageSource = false,
            Ports =
            {
                new() { Id = "pos_t", Label = "+", Kind = "dc_pos", Side = "top", Offset = 0.35 },
                new() { Id = "neg_t", Label = "−", Kind = "dc_neg", Side = "top", Offset = 0.65 },
                new() { Id = "pos_b", Label = "+", Kind = "dc_pos", Side = "bottom", Offset = 0.35 },
                new() { Id = "neg_b", Label = "−", Kind = "dc_neg", Side = "bottom", Offset = 0.65 }
            },
            Parameters =
            {
                new() { Key = "nominalVoltage", Label = "标称直流电压", Type = "number", Unit = "V", Min = 0 },
                new() { Key = "name", Label = "名称", Type = "string" }
            },
            DefaultParameters = new Dictionary<string, object?>
            {
                ["nominalVoltage"] = 1200d,
                ["name"] = "DC母线"
            }
        };
    }
}
