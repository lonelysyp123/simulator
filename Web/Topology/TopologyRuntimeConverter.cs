using EssSimulator.Configuration;
using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.Web.Topology
{
    /// <summary>
    /// 将组态工程映射为径向电站运行时配置（规模 + 参数）。
    /// 电气接线仍按 NetworkTopologyBuilder 固定模板展开：储能单元数 = 含 PCS 的 EMU 虚拟节点数，
    /// 每单元 PCS 数 = 通过 emuId 归入该 EMU 的 PCS 节点数；
    /// 光伏单元展开为 overlay.PvUnits，运行时挂在 35 kV 母线送电。
    /// 工程须至少包含一个含 PCS 的 EMU 或光伏单元。
    /// </summary>
    public static class TopologyRuntimeConverter
    {
        /// <summary>
        /// 应用到仿真前：先走保存级校验（电网/主断路器/并网点电表），再生成 overlay。
        /// </summary>
        public static (TopologyRuntimeOverlay? Overlay, TopologyValidationResult Validation) ConvertForApply(TopologyProject project)
        {
            var save = TopologyValidator.ValidateProjectForSave(project);
            if (!save.Ok)
                return (null, save);
            return Convert(project);
        }

        public static (TopologyRuntimeOverlay? Overlay, TopologyValidationResult Validation) Convert(TopologyProject project)
        {
            if (project == null)
                return (null, Fail("PROJECT_NULL", "工程为空"));

            TopologyValidator.RefreshAcBusEnergization(project);

            var emus = project.Nodes.Where(n => n.TemplateId == "emu").OrderBy(n => n.Y).ThenBy(n => n.X).ToList();
            var pcsNodes = project.Nodes.Where(n => n.TemplateId == "pcs").OrderBy(n => n.X).ThenBy(n => n.Y).ToList();
            var pvUnits = project.Nodes.Where(n => n.TemplateId == "pv_unit").OrderBy(n => n.Y).ThenBy(n => n.X).ToList();
            var emusWithPcs = emus
                .Select(e => (Emu: e, Pcs: pcsNodes.Where(p => TopologyParamHelper.GetString(p.Parameters, "emuId") == e.Id).ToList()))
                .Where(g => g.Pcs.Count > 0)
                .ToList();
            if (emusWithPcs.Count == 0 && pvUnits.Count == 0)
                return (null, Fail("NO_GENERATION_UNIT", "工程中至少需要一个含 PCS 的 EMU 储能单元或光伏单元"));

            var overlay = new TopologyRuntimeOverlay
            {
                SourceProjectId = project.Id,
                SourceProjectName = project.Name,
                GeneratedAtUtc = DateTime.UtcNow
            };

            var grid = project.Nodes.FirstOrDefault(n => n.TemplateId == "grid");
            if (grid != null)
            {
                overlay.Pcc = new PccConfig
                {
                    NominalLineVoltage = TopologyParamHelper.GetDouble(grid.Parameters, "outputVoltage", 220000),
                    ShortCircuitMva = TopologyParamHelper.GetDouble(grid.Parameters, "shortCircuitMva", 750),
                    StationBusNominalLineVoltage = 35000
                };
                overlay.Notes.Add($"电网 → Pcc {overlay.Pcc.NominalLineVoltage / 1000:0.#} kV");
            }

            var mainXfmr = project.Nodes
                .Where(n => n.TemplateId == "transformer")
                .OrderByDescending(n => TopologyParamHelper.GetDouble(n.Parameters, "primaryVoltage", 0))
                .FirstOrDefault();
            if (mainXfmr != null)
            {
                overlay.Transformer = new TransformerConfig
                {
                    PrimaryVoltage = TopologyParamHelper.GetDouble(mainXfmr.Parameters, "primaryVoltage", 220000),
                    SecondaryVoltage = TopologyParamHelper.GetDouble(mainXfmr.Parameters, "secondaryVoltage", 35000),
                    RatedPower = TopologyParamHelper.GetDouble(mainXfmr.Parameters, "ratedPowerKva", 31500),
                    ImpedancePercent = TopologyParamHelper.GetDouble(mainXfmr.Parameters, "impedancePercent", 4),
                    NoLoadLoss = TopologyParamHelper.GetDouble(mainXfmr.Parameters, "noLoadLossW", 100),
                    LoadLoss = TopologyParamHelper.GetDouble(mainXfmr.Parameters, "loadLossW", 200)
                };
                if (overlay.Pcc != null)
                    overlay.Pcc.StationBusNominalLineVoltage = overlay.Transformer.SecondaryVoltage;
                overlay.Notes.Add($"主变 → {overlay.Transformer.PrimaryVoltage / 1000:0.#}/{overlay.Transformer.SecondaryVoltage / 1000:0.#} kV");
            }

            var meter = project.Nodes.FirstOrDefault(n =>
                           n.TemplateId == "ac_meter" && TopologyParamHelper.GetBool(n.Parameters, "isPccMeter"))
                       ?? project.Nodes.FirstOrDefault(n => n.TemplateId == "ac_meter");
            if (meter != null)
            {
                // 组态三拐角只建模一次侧；二次额定固定 PT 100V / CT 5A（行业常见二次值）
                const double PtSecondaryV = 100;
                const double CtSecondaryA = 5;
                double ptPri = TopologyParamHelper.GetDouble(meter.Parameters, "ptPrimaryVoltage", 220000);
                double ctPri = TopologyParamHelper.GetDouble(meter.Parameters, "ctPrimaryCurrent", 2000);
                overlay.Meter = new MeterConfig
                {
                    PccMeter = new MeterInstanceConfig
                    {
                        MountDescription = "组态工程 PCC 电表（一次侧统一抽头）",
                        ReportedQuantity = MeterReportedQuantity.Primary,
                        AccuracyClass = TopologyParamHelper.GetString(meter.Parameters, "accuracyClass", "0.2S"),
                        Pt = new PtConfig
                        {
                            PrimaryLineVoltageV = ptPri,
                            SecondaryLineVoltageV = PtSecondaryV,
                            Connection = ThreePhaseConnection.Star
                        },
                        Ct = new CtConfig
                        {
                            PrimaryCurrentA = ctPri,
                            SecondaryCurrentA = CtSecondaryA
                        }
                    }
                };
                overlay.Notes.Add($"电表一次侧 · {ptPri / 1000:0.#} kV / {ctPri:0.#} A（二次固定 {PtSecondaryV:0}V / {CtSecondaryA:0}A）");
            }

            // 单元变取首个 EMU 参数；PCS 额定取首台 PCS；仅有光伏时用光伏箱变/逆变器参数
            var firstPcs = emusWithPcs.SelectMany(g => g.Pcs).FirstOrDefault();
            if (emusWithPcs.Count > 0)
            {
                var emuSource = emusWithPcs[0].Emu;
                var pcsSource = firstPcs!;
                overlay.UnitTransformer = new UnitTransformerConfig
                {
                    PrimaryVoltage = TopologyParamHelper.GetDouble(emuSource.Parameters, "unitXfPrimaryV", 35000),
                    SecondaryVoltage = TopologyParamHelper.GetDouble(emuSource.Parameters, "unitXfSecondaryV", 690),
                    RatedPower = TopologyParamHelper.GetDouble(emuSource.Parameters, "unitXfRatedKva", 6300)
                };
                overlay.Pcs = new PcsPhysicalConfig
                {
                    RatedPower = TopologyParamHelper.GetDouble(pcsSource.Parameters, "pcsRatedPowerKw", 1250),
                    MaxPower = TopologyParamHelper.GetDouble(pcsSource.Parameters, "pcsMaxPowerKw", 1250),
                    Efficiency = TopologyParamHelper.GetDouble(pcsSource.Parameters, "pcsEfficiency", 0.99),
                    DcVoltageRangeMin = TopologyParamHelper.GetDouble(pcsSource.Parameters, "dcVoltageMin", 1000),
                    DcVoltageRangeMax = TopologyParamHelper.GetDouble(pcsSource.Parameters, "dcVoltageMax", 1500),
                    AcVoltageNominal = TopologyParamHelper.GetDouble(emuSource.Parameters, "unitXfSecondaryV", 690)
                };
            }
            else if (pvUnits.Count > 0)
            {
                var pvSource = pvUnits[0];
                overlay.UnitTransformer = new UnitTransformerConfig
                {
                    PrimaryVoltage = TopologyParamHelper.GetDouble(pvSource.Parameters, "unitXfPrimaryV", 35000),
                    SecondaryVoltage = TopologyParamHelper.GetDouble(pvSource.Parameters, "unitXfSecondaryV", 690),
                    RatedPower = TopologyParamHelper.GetDouble(pvSource.Parameters, "unitXfRatedKva", 5120)
                };
                overlay.Pcs = new PcsPhysicalConfig
                {
                    RatedPower = TopologyParamHelper.GetDouble(pvSource.Parameters, "inverterRatedPowerKw", 320),
                    MaxPower = TopologyParamHelper.GetDouble(pvSource.Parameters, "inverterMaxPowerKw", 352),
                    Efficiency = TopologyParamHelper.GetDouble(pvSource.Parameters, "inverterEfficiency", 0.99),
                    DcVoltageRangeMin = TopologyParamHelper.GetDouble(pvSource.Parameters, "dcVoltageMin", 500),
                    DcVoltageRangeMax = TopologyParamHelper.GetDouble(pvSource.Parameters, "dcVoltageMax", 1500),
                    AcVoltageNominal = TopologyParamHelper.GetDouble(pvSource.Parameters, "unitXfSecondaryV", 690)
                };
            }

            // 负载：绑定电站概览；有功仅允许消耗（≤0），缺省节点或未填时初始化为 0/0
            var loadNode = project.Nodes.FirstOrDefault(n => n.TemplateId == "load");
            double loadP = loadNode == null
                ? 0
                : TopologyParamHelper.GetDouble(loadNode.Parameters, "activePowerKw", 0);
            double loadQ = loadNode == null
                ? 0
                : TopologyParamHelper.GetDouble(loadNode.Parameters, "reactivePowerKvar", 0);
            if (loadP > 0) loadP = 0; // 禁止向电网释放有功
            overlay.Load = new LoadConfig
            {
                ActivePowerPlan = loadP,
                ReactivePowerPlan = loadQ
            };
            overlay.Notes.Add(loadNode == null
                ? "负载：组态无负载节点，初始化 P/Q = 0 / 0"
                : $"负载「{loadNode.Label}」→ 概览绑定 · P {loadP:0.##} kW · Q {loadQ:0.##} kvar（有功仅消耗）");

            var pvCount = pvUnits.Count;
            int pvIndex = 0;
            foreach (var node in pvUnits)
            {
                pvIndex++;
                var unit = ToPvUnit(node, pvIndex);
                overlay.PvUnits.Add(unit);
                overlay.Notes.Add($"{unit.Name}: 逆变器×{unit.InverterCount}（{unit.InverterRatedPowerKw:0.#} kW）");
            }
            if (pvCount > 0)
                overlay.Notes.Add($"光伏单元×{pvCount}：已展开运行时配置");

            int unitIndex = 0;
            foreach (var group in emusWithPcs)
            {
                unitIndex++;
                var emu = group.Emu;
                var unit = new EssUnitConfig
                {
                    Name = string.IsNullOrWhiteSpace(emu.Label) ? $"Unit-{unitIndex}" : emu.Label
                };

                // 单元断路器/电表：按 emuId 归入本单元的组态节点（各至多 1 台，保存校验已保证）
                var unitBreaker = project.Nodes.FirstOrDefault(n =>
                    n.TemplateId == "ac_breaker" && TopologyParamHelper.GetString(n.Parameters, "emuId") == emu.Id);
                var unitMeter = project.Nodes.FirstOrDefault(n =>
                    n.TemplateId == "ac_meter" && TopologyParamHelper.GetString(n.Parameters, "emuId") == emu.Id);
                unit.HasUnitBreaker = unitBreaker != null;
                unit.UnitBreakerName = NodeDisplayName(unitBreaker);
                unit.HasUnitMeter = unitMeter != null;
                unit.UnitMeterName = NodeDisplayName(unitMeter);

                int pcsSeq = 0;
                int linkedBms = 0;
                var usedBms = new HashSet<string>(StringComparer.Ordinal);
                foreach (var pcs in group.Pcs)
                {
                    pcsSeq++;
                    char slot = (char)('A' + Math.Min(pcsSeq - 1, 25));
                    unit.Pcs.Add(new Configuration.PcsDeviceConfig
                    {
                        Name = string.IsNullOrWhiteSpace(pcs.Label) ? $"PCS-{unitIndex}{slot}" : pcs.Label
                    });
                    // BMS 与 PCS 按位对齐：沿该 PCS 的 DC 连线找 1 台未占用的 BMS，缺位用默认配置补齐
                    var bms = FindBmsForPcs(project, pcs.Id).FirstOrDefault(b => !usedBms.Contains(b.Id));
                    if (bms != null)
                    {
                        linkedBms++;
                        usedBms.Add(bms.Id);
                    }
                    unit.Bms.Add(ToBmsConfig(bms, $"BMS-{unitIndex}{slot}"));
                }

                overlay.EssUnits.Add(unit);
                overlay.Notes.Add($"{unit.Name}: PCS×{unit.Pcs.Count} · BMS×{linkedBms}（每台 PCS 对齐 1 路 BMS，未连线时用默认补齐）"
                    + $" · 单元断路器：{unit.UnitBreakerName ?? "未绑定"} · 单元电表：{unit.UnitMeterName ?? "未绑定"}");
            }

            // 无 PCS 归入的 EMU 节点不生成单元，仅提示
            foreach (var emu in emus.Where(e => emusWithPcs.All(g => g.Emu.Id != e.Id)))
                overlay.Notes.Add($"EMU「{(string.IsNullOrWhiteSpace(emu.Label) ? emu.Id : emu.Label)}」无归属 PCS，已跳过");

            string message = overlay.EssUnits.Count > 0 && pvCount > 0
                ? $"已生成 {overlay.EssUnits.Count} 个储能单元、{pvCount} 个光伏单元配置"
                : overlay.EssUnits.Count > 0
                    ? $"已生成 {overlay.EssUnits.Count} 个储能单元配置"
                    : $"已展开 {pvCount} 个光伏单元运行时配置";
            return (overlay, new TopologyValidationResult
            {
                Ok = true,
                Message = message,
                Details = overlay.Notes.ToList()
            });
        }

        private static string? NodeDisplayName(TopologyNode? node)
        {
            if (node == null) return null;
            var name = TopologyParamHelper.GetString(node.Parameters, "name");
            return !string.IsNullOrWhiteSpace(node.Label) ? node.Label
                : !string.IsNullOrWhiteSpace(name) ? name
                : node.Id;
        }

        private static List<TopologyNode> FindBmsForPcs(TopologyProject project, string pcsId)
        {
            // PCS —dc→ DC母线 —dc→ BMS，或 PCS 直接连 BMS
            var neighborIds = Neighbors(project, pcsId).ToHashSet();
            var dcBuses = project.Nodes.Where(n => n.TemplateId == "dc_bus" && neighborIds.Contains(n.Id)).Select(n => n.Id).ToHashSet();

            var bms = new List<TopologyNode>();
            foreach (var n in project.Nodes.Where(n => n.TemplateId == "bms"))
            {
                var nb = Neighbors(project, n.Id).ToHashSet();
                if (nb.Contains(pcsId) || nb.Overlaps(dcBuses))
                    bms.Add(n);
            }

            return bms.OrderBy(n => n.X).ThenBy(n => n.Y).ToList();
        }

        private static IEnumerable<string> Neighbors(TopologyProject project, string nodeId)
        {
            foreach (var e in project.Edges)
            {
                if (e.FromNodeId == nodeId) yield return e.ToNodeId;
                else if (e.ToNodeId == nodeId) yield return e.FromNodeId;
            }
        }

        private static BmsDeviceConfig ToBmsConfig(TopologyNode? node, string fallbackName)
        {
            if (node == null) return new BmsDeviceConfig { Name = fallbackName };
            var p = node.Parameters;
            return new BmsDeviceConfig
            {
                Name = TopologyParamHelper.GetString(p, "name", string.IsNullOrWhiteSpace(node.Label) ? fallbackName : node.Label),
                ClusterCount = (int)Math.Max(1, TopologyParamHelper.GetDouble(p, "clusterCount", 12)),
                PackCount = (int)Math.Max(1, TopologyParamHelper.GetDouble(p, "packCount", 4)),
                CellSeriesCount = (int)Math.Max(1, TopologyParamHelper.GetDouble(p, "cellSeriesCount", 104)),
                CellParallelCount = (int)Math.Max(1, TopologyParamHelper.GetDouble(p, "cellParallelCount", 1)),
                CellNominalVoltage = TopologyParamHelper.GetDouble(p, "cellNominalVoltage", 3.2),
                CellNominalCapacity = TopologyParamHelper.GetDouble(p, "cellNominalCapacity", 314),
                CellInitialSoc = TopologyParamHelper.GetDouble(p, "cellInitialSoc", 0.5),
                RackInternalResistance = TopologyParamHelper.GetDouble(p, "rackInternalResistance", 0.02)
            };
        }

        private static PvUnitRuntimeConfig ToPvUnit(TopologyNode node, int index)
        {
            var p = node.Parameters;
            int invCount = (int)Math.Max(1, TopologyParamHelper.GetDouble(p, "inverterCount", 16));
            double ratedKw = TopologyParamHelper.GetDouble(p, "inverterRatedPowerKw", 320);
            return new PvUnitRuntimeConfig
            {
                Name = string.IsNullOrWhiteSpace(node.Label) ? $"PV-{index}" : node.Label,
                InverterCount = invCount,
                StringCount = (int)Math.Max(1, TopologyParamHelper.GetDouble(p, "stringCount", 16)),
                ModulesPerString = (int)Math.Max(1, TopologyParamHelper.GetDouble(p, "modulesPerString", 30)),
                InverterRatedPowerKw = ratedKw,
                InverterMaxPowerKw = TopologyParamHelper.GetDouble(p, "inverterMaxPowerKw", 352),
                InverterEfficiency = TopologyParamHelper.GetDouble(p, "inverterEfficiency", 0.99),
                InverterAcVoltageV = TopologyParamHelper.GetDouble(p, "inverterAcVoltage", 690),
                UnitXfPrimaryV = TopologyParamHelper.GetDouble(p, "unitXfPrimaryV", 35000),
                UnitXfSecondaryV = TopologyParamHelper.GetDouble(p, "unitXfSecondaryV", 690),
                UnitXfRatedKva = TopologyParamHelper.GetDouble(p, "unitXfRatedKva", invCount * ratedKw),
                DcVoltageMin = TopologyParamHelper.GetDouble(p, "dcVoltageMin", 500),
                DcVoltageMax = TopologyParamHelper.GetDouble(p, "dcVoltageMax", 1500),
                ModuleModel = TopologyParamHelper.GetString(p, "moduleModel", "TSM-NEG21C.20Q")
            };
        }

        private static TopologyValidationResult Fail(string code, string message) =>
            new() { Ok = false, Code = code, Message = message };
    }
}
