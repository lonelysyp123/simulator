using System.Globalization;
using System.Text;

namespace EssSimulator.Display
{
    /// <summary>主电气接线 ASCII 单线图：220kV 纵向主回路，35kV 母线并联负载与各储能单元。</summary>
    internal static class GuiMainLineRenderer
    {
        private const int SpineCol = 11;
        private const int MainBoxWidth = 36;
        private const int SideBoxWidth = 22;
        private const int UnitColWidth = 32;
        private const int UnitColGap = 1;
        private const int UnitColumnGap = 1;

        public static string Render(
            MainLineSnapshot snap,
            int unitStart,
            int unitEndExclusive,
            int channelStart,
            int channelEndExclusive,
            int sectionIndex,
            int sectionCount,
            int unitCount,
            int channelCount)
        {
            var time = DateTime.Now.ToLongTimeString();
            var sb = new StringBuilder();

            sb.AppendLine();
            AppendHeader(sb, time, snap, sectionIndex, sectionCount, unitStart, unitEndExclusive, unitCount, channelCount);
            sb.AppendLine();
            AppendOneLineDiagram(sb, snap, channelStart, channelEndExclusive, channelCount);
            sb.AppendLine();
            string navHint = sectionCount > 1 ? "↑/↓ 翻页" : "↑/↓ 区域";
            sb.AppendLine($"操作: {navHint} | Tab 表格 | :/C 命令 | Esc 返回");
            return sb.ToString();
        }

        private static void AppendOneLineDiagram(
            StringBuilder sb,
            MainLineSnapshot snap,
            int channelStart,
            int channelEndExclusive,
            int channelCount)
        {
            AppendLayerLabel(sb, "220kV");
            AppendCenteredBox(sb, "电网·BUS_GRID", BuildGridLayerLines(snap), MainBoxWidth);
            AppendSpine(sb);

            AppendCenteredBox(sb, "主断·主变", BuildMainBreakerTransformerLines(snap, channelStart, channelEndExclusive), MainBoxWidth);
            AppendSpine(sb);

            AppendCenteredBox(sb, "PCC电表", BuildMeterBoxLines(snap), MainBoxWidth);
            AppendSpine(sb);

            AppendLayerLabel(sb, "35kV");
            AppendBus35ParallelLayer(sb, snap, channelCount);
        }

        /// <summary>35 kV：BUS_35 向下分出水平母线，负载与各单元并列挂接。</summary>
        private static void AppendBus35ParallelLayer(StringBuilder sb, MainLineSnapshot snap, int channelCount)
        {
            var units = snap.Units.OrderBy(u => u.UnitIndex).ToList();
            int busLeft = Math.Max(0, SpineCol - MainBoxWidth / 2);
            AppendBoxAt(sb, busLeft, "BUS_35", BuildBus35Lines(snap).ToList(), MainBoxWidth, spineOutlet: true);
            AppendLoadFromSpineRight(sb, snap);
            sb.AppendLine(PadToDisplayWidth("", SpineCol) + "│");

            if (units.Count == 0)
                return;

            int pcsW = ResolvePcsBoxWidth(units.Count);

            var columns = units
                .Select(u => BuildUnitColumnLines(u, channelCount, pcsW))
                .ToList();
            int colWidth = columns.Count > 0
                ? columns.Max(c => c.Width)
                : pcsW * 2 + UnitColGap;
            columns = columns
                .Select(c => new UnitColumnRender(colWidth, ReformatColumnLines(c.Lines, c.Width, colWidth)))
                .ToList();
            int totalWidth = units.Count * colWidth + (units.Count - 1) * UnitColumnGap;
            int groupLeft = Math.Max(0, SpineCol - totalWidth / 2);

            var centers = new int[units.Count];
            for (int i = 0; i < units.Count; i++)
                centers[i] = groupLeft + i * (colWidth + UnitColumnGap) + colWidth / 2;

            sb.AppendLine(BuildHorizontalBusBar(groupLeft, totalWidth, centers, SpineCol));

            int maxRows = columns.Max(c => c.Lines.Count);
            for (int r = 0; r < maxRows; r++)
            {
                var segments = new List<string>();
                for (int i = 0; i < columns.Count; i++)
                {
                    string line = r < columns[i].Lines.Count
                        ? columns[i].Lines[r]
                        : "";
                    segments.Add(FormatColumnLine(line, colWidth));
                }
                sb.AppendLine(PadToDisplayWidth("", groupLeft) + string.Join(new string(' ', UnitColumnGap), segments));
            }
        }

        private readonly record struct UnitColumnRender(int Width, List<string> Lines);

        private static List<string> ReformatColumnLines(IReadOnlyList<string> lines, int fromWidth, int toWidth)
        {
            if (fromWidth == toWidth)
                return lines.ToList();
            return lines.Select(l => FormatColumnLine(l, toWidth)).ToList();
        }

        private static int ResolvePcsBoxWidth(int unitCount) => 0;

        private static void AppendLoadFromSpineRight(StringBuilder sb, MainLineSnapshot snap)
        {
            int loadLeft = Math.Max(0, SpineCol - MainBoxWidth / 2) + MainBoxWidth + 2;
            var loadBox = RenderBox("负载", BuildLoadLines(snap).ToList(), SideBoxWidth);
            sb.AppendLine(PadToDisplayWidth("", SpineCol) + "├" + new string('─', Math.Max(1, loadLeft - SpineCol - 1)) + "┐");
            foreach (var row in loadBox)
            {
                sb.AppendLine(PadToDisplayWidth("", SpineCol) + "│"
                    + PadToDisplayWidth("", loadLeft - SpineCol - 1)
                    + FormatColumnLine(row, SideBoxWidth));
            }
        }

        private static string BuildHorizontalBusBar(int groupLeft, int totalWidth, int[] centers, int spineCol)
        {
            int lineEnd = groupLeft + totalWidth;
            var chars = new char[lineEnd];
            Array.Fill(chars, ' ');

            int barLeft = groupLeft;
            int barRight = lineEnd - 1;
            chars[barLeft] = '┌';
            chars[barRight] = '┐';

            for (int c = barLeft + 1; c < barRight; c++)
                chars[c] = '─';

            if (spineCol >= barLeft && spineCol <= barRight)
                chars[spineCol] = '┴';

            foreach (int center in centers)
            {
                if (center > barLeft && center < barRight)
                    chars[center] = '┬';
            }

            return new string(chars);
        }

        private static UnitColumnRender BuildUnitColumnLines(
            UnitBranchSnapshot unit,
            int channelCount,
            int pcsW)
        {
            string uTitle = $"U{unit.UnitIndex + 1}";
            var uBody = BuildUnitBranchNodeLines(unit).ToList();
            var busBody = BuildBus690Lines(unit).ToList();

            int pcsBoxW = MeasurePcsBmsBoxWidth(unit, channelCount, pcsW);
            int pcsGroupW = pcsBoxW * 2 + UnitColGap;
            int colWidth = Math.Max(pcsGroupW,
                Math.Max(MeasureBoxWidth(uTitle, uBody), MeasureBoxWidth("690V", busBody)));

            var lines = new List<string>();
            int center = colWidth / 2;

            lines.Add(FormatColumnLine(PadToDisplayWidth("", center) + "│", colWidth));
            lines.AddRange(RenderBox(uTitle, uBody, colWidth));
            lines.Add(FormatColumnLine(PadToDisplayWidth("", center) + "│", colWidth));
            lines.AddRange(RenderBox("690V", busBody, colWidth));
            lines.AddRange(BuildPcsBmsTreeLines(unit, channelCount, colWidth, pcsBoxW));
            return new UnitColumnRender(colWidth, lines);
        }

        private static int MeasurePcsBmsBoxWidth(UnitBranchSnapshot unit, int channelCount, int minWidth)
        {
            int u = unit.UnitIndex;
            int idxA = u * 2;
            int idxB = u * 2 + 1;
            int width = minWidth;

            if (idxA < channelCount && unit.PcsA != null)
            {
                width = Math.Max(width, MeasureBoxWidth($"PCS{idxA + 1}", BuildPcsLines(unit, unit.PcsA.Value, idxA, 0)));
                width = Math.Max(width, MeasureBoxWidth($"舱{idxA + 1}", BuildBmsLines(idxA)));
            }

            if (idxB < channelCount && unit.PcsB != null)
            {
                width = Math.Max(width, MeasureBoxWidth($"PCS{idxB + 1}", BuildPcsLines(unit, unit.PcsB.Value, idxB, 1)));
                width = Math.Max(width, MeasureBoxWidth($"舱{idxB + 1}", BuildBmsLines(idxB)));
            }

            return width;
        }

        private static List<string> BuildPcsBmsTreeLines(
            UnitBranchSnapshot unit,
            int channelCount,
            int colWidth,
            int pcsW)
        {
            int u = unit.UnitIndex;
            int idxA = u * 2;
            int idxB = u * 2 + 1;

            var pcsBoxes = new List<List<string>>();
            var bmsBoxes = new List<List<string>>();

            if (idxA < channelCount && unit.PcsA != null)
            {
                pcsBoxes.Add(RenderBox($"PCS{idxA + 1}", BuildPcsLines(unit, unit.PcsA.Value, idxA, 0), pcsW));
                bmsBoxes.Add(RenderBox($"舱{idxA + 1}", BuildBmsLines(idxA), pcsW));
            }

            if (idxB < channelCount && unit.PcsB != null)
            {
                pcsBoxes.Add(RenderBox($"PCS{idxB + 1}", BuildPcsLines(unit, unit.PcsB.Value, idxB, 1), pcsW));
                bmsBoxes.Add(RenderBox($"舱{idxB + 1}", BuildBmsLines(idxB), pcsW));
            }

            if (pcsBoxes.Count == 0)
                return new List<string> { FormatColumnLine(PadToDisplayWidth("", colWidth / 2) + "（无PCS）", colWidth) };

            int mergedBoxW = pcsBoxes.Concat(bmsBoxes).Max(b => b.Max(GetDisplayWidth));
            int groupWidth = pcsBoxes.Count * mergedBoxW + (pcsBoxes.Count - 1) * UnitColGap;
            int splitLeft = (colWidth - groupWidth) / 2;
            int mid = colWidth / 2;

            var lines = new List<string>();

            if (pcsBoxes.Count == 1)
            {
                lines.Add(FormatColumnLine(PadToDisplayWidth("", mid) + "│", colWidth));
                lines.AddRange(OffsetToColumn(MergeBoxRows(pcsBoxes), splitLeft, colWidth));
                lines.Add(FormatColumnLine(PadToDisplayWidth("", mid) + "│DC", colWidth));
                lines.AddRange(OffsetToColumn(MergeBoxRows(bmsBoxes), splitLeft, colWidth));
                return lines;
            }

            lines.Add(FormatColumnLine(PadToDisplayWidth("", mid) + "│", colWidth));
            lines.Add(FormatPcsSplitLine(colWidth, splitLeft, groupWidth, mid));
            lines.AddRange(OffsetToColumn(MergeBoxRows(pcsBoxes), splitLeft, colWidth));
            lines.Add(FormatPcsToBmsLinkLine(colWidth, splitLeft, mergedBoxW, pcsBoxes.Count));
            lines.AddRange(OffsetToColumn(MergeBoxRows(bmsBoxes), splitLeft, colWidth));
            return lines;
        }

        private static string FormatPcsSplitLine(int colWidth, int splitLeft, int groupWidth, int mid)
        {
            var sb = new StringBuilder();
            sb.Append(PadToDisplayWidth("", splitLeft));
            sb.Append('┌');
            sb.Append(new string('─', Math.Max(0, mid - splitLeft - 1)));
            sb.Append('┴');
            sb.Append(new string('─', Math.Max(0, splitLeft + groupWidth - mid - 1)));
            sb.Append('┐');
            return FormatColumnLine(sb.ToString(), colWidth);
        }

        private static List<string> OffsetToColumn(IReadOnlyList<string> lines, int leftOffset, int colWidth) =>
            lines.Select(l => PadDisplayWidth(PadToDisplayWidth("", leftOffset) + l, colWidth)).ToList();

        private static List<string> MergeBoxRows(IReadOnlyList<List<string>> boxes)
        {
            if (boxes.Count == 0)
                return new List<string>();

            int boxWidth = boxes.Max(b => b.Max(GetDisplayWidth));
            int totalWidth = boxes.Count * boxWidth + (boxes.Count - 1) * UnitColGap;
            int rows = boxes.Max(b => b.Count);
            var lines = new List<string>();
            for (int r = 0; r < rows; r++)
            {
                var segments = new List<string>();
                for (int i = 0; i < boxes.Count; i++)
                {
                    string line = r < boxes[i].Count ? boxes[i][r] : FormatBoxBody("", boxWidth);
                    segments.Add(PadDisplayWidth(line, boxWidth));
                }
                lines.Add(PadDisplayWidth(string.Join(new string(' ', UnitColGap), segments), totalWidth));
            }
            return lines;
        }

        /// <summary>仅填充，不截断（框线行不可裁剪）。</summary>
        private static string PadDisplayWidth(string text, int targetWidth)
        {
            int width = GetDisplayWidth(text);
            if (width >= targetWidth)
                return text;
            return text + new string(' ', targetWidth - width);
        }

        private static string FormatPcsToBmsLinkLine(int colWidth, int splitLeft, int mergedBoxW, int boxCount)
        {
            if (boxCount == 1)
                return FormatColumnLine(PadToDisplayWidth("", splitLeft + mergedBoxW / 2) + "│DC", colWidth);

            int c0 = splitLeft + mergedBoxW / 2;
            int c1 = splitLeft + mergedBoxW + UnitColGap + mergedBoxW / 2;
            string line = PadToDisplayWidth("", c0) + "│DC"
                + PadToDisplayWidth("", c1 - c0 - GetDisplayWidth("│DC")) + "│DC";
            return FormatColumnLine(line, colWidth);
        }

        private const int BoxSidePadding = 2; // │…│ 左右边框

        private static List<string> RenderBox(string title, IReadOnlyList<string> bodyLines, int boxWidth)
        {
            boxWidth = Math.Max(boxWidth, MeasureBoxWidth(title, bodyLines));
            var lines = new List<string> { FormatBoxTop(title, boxWidth) };
            foreach (string body in bodyLines)
                lines.Add(FormatBoxBody(body, boxWidth));
            lines.Add(FormatBoxBottom(boxWidth));
            return lines;
        }

        private static int MeasureBoxWidth(string title, IReadOnlyList<string> bodyLines)
        {
            int contentMax = bodyLines.Count > 0 ? bodyLines.Max(GetDisplayWidth) : 0;
            int innerNeed = Math.Max(GetDisplayWidth(title) + 2, contentMax);
            return innerNeed + BoxSidePadding;
        }

        private static string FormatBoxTop(string title, int boxWidth)
        {
            int innerW = boxWidth - 2;
            string fill = PadToDisplayWidth($"─{title}─", innerW, '─');
            return ExactDisplayWidth($"┌{fill}┐", boxWidth);
        }

        private static string FormatBoxBody(string content, int boxWidth)
        {
            int innerW = boxWidth - BoxSidePadding;
            string inner = PadToDisplayWidth(content, innerW);
            return ExactDisplayWidth($"│{inner}│", boxWidth);
        }

        private static string FormatBoxBottom(int boxWidth) =>
            ExactDisplayWidth($"└{new string('─', boxWidth - 2)}┘", boxWidth);

        private static string FormatBoxBottomWithSpine(int boxLeft, int boxWidth)
        {
            int spineAt = SpineCol - boxLeft;
            if (spineAt <= 0 || spineAt >= boxWidth - 1)
                return FormatBoxBottom(boxWidth);
            string bottom = "└" + new string('─', spineAt - 1) + "┴" + new string('─', boxWidth - spineAt - 2) + "┘";
            return ExactDisplayWidth(bottom, boxWidth);
        }

        /// <summary>保证字符串恰好占 target 个终端显示列。</summary>
        private static string ExactDisplayWidth(string text, int targetWidth)
        {
            int width = GetDisplayWidth(text);
            if (width == targetWidth)
                return text;
            if (width > targetWidth)
                return TrimToDisplayWidth(text, targetWidth);
            return text + new string(' ', targetWidth - width);
        }

        private static string FormatColumnLine(string line, int colWidth) =>
            ExactDisplayWidth(line, colWidth);

        private static void AppendLayerLabel(StringBuilder sb, string label) =>
            sb.AppendLine(PadToDisplayWidth(label, SpineCol - 2));

        private static void AppendSpine(StringBuilder sb, int lines = 1)
        {
            for (int i = 0; i < lines; i++)
                sb.AppendLine(PadToDisplayWidth("", SpineCol) + "│");
        }

        private static void AppendCenteredBox(StringBuilder sb, string title, IEnumerable<string> bodyLines, int minWidth)
        {
            var body = bodyLines.ToList();
            int boxWidth = Math.Max(minWidth, MeasureBoxWidth(title, body));
            int left = Math.Max(0, SpineCol - boxWidth / 2);
            AppendBoxAt(sb, left, title, body, boxWidth, spineOutlet: true);
        }

        private static void AppendBoxAt(StringBuilder sb, int left, string title, IReadOnlyList<string> bodyLines, int boxWidth, bool spineOutlet = false)
        {
            boxWidth = Math.Max(boxWidth, MeasureBoxWidth(title, bodyLines));
            var rendered = RenderBox(title, bodyLines, boxWidth);
            for (int i = 0; i < rendered.Count; i++)
            {
                string line = rendered[i];
                if (spineOutlet && i == rendered.Count - 1)
                    line = FormatBoxBottomWithSpine(left, boxWidth);
                sb.AppendLine(PadToDisplayWidth("", left) + line);
            }
        }

        private static void AppendHeader(
            StringBuilder sb,
            string time,
            MainLineSnapshot snap,
            int sectionIndex,
            int sectionCount,
            int unitStart,
            int unitEndExclusive,
            int unitCount,
            int channelCount)
        {
            sb.AppendLine($"电气主接线  [{time}]  单元{unitCount} 通道{channelCount}  显示{unitStart + 1}~{unitEndExclusive}({sectionIndex + 1}/{sectionCount})");
            sb.AppendLine($"  {(snap.PropagationEnabled ? "径向 V-I-φ" : "Legacy")}  PCC {GuiStatusFormatters.FormatVoltage(snap.PccLineVoltageV)}  35kV {GuiStatusFormatters.FormatVoltage(snap.StationBus35LineVoltageV)}");
            sb.AppendLine("  │=220kV主回路  ┬/┴=35kV并联  ├──=支路  │DC=电池耦合");
        }

        private static List<string> BuildPcsLines(
            UnitBranchSnapshot unit,
            PcsChannelSnapshot pcs,
            int pcsIndex,
            int slotInUnit)
        {
            int u = unit.UnitIndex;
            var ac = pcs.AcOutput;
            return new List<string>
            {
                GuiStatusFormatters.FormatPcsMainLineDeviceState(u, slotInUnit, pcsIndex),
                GuiStatusFormatters.FormatPcsMainLineStartStop(u, slotInUnit),
                GuiStatusFormatters.FormatPcsMainLineBlackStart(u, slotInUnit, pcsIndex),
                $"V{FormatCompactV(ac.LineVoltageV)} I{ac.LineCurrentA:0}A φ{ac.PhaseAngleDeg:0}°",
                GuiStatusFormatters.FormatPcsMainLineTargetPower(u, slotInUnit),
                GuiStatusFormatters.FormatPcsMainLineActualPower(pcsIndex, pcs.ActivePowerKw),
                GuiStatusFormatters.FormatPcsMainLineTargetReactive(u, slotInUnit),
                GuiStatusFormatters.FormatPcsMainLineActualReactive(pcsIndex, pcs.ReactivePowerKw),
                ShortPcsMode(pcs)
            };
        }

        private static List<string> BuildBmsLines(int bmsIndex)
        {
            double soc = 100 * GuiSimDataAccess.SafeGetDouble($"ess._batteryRacks[{bmsIndex}]._currentState.MinClusterSOC");
            double vdc = GuiSimDataAccess.SafeGetDouble($"ess._batteryRacks[{bmsIndex}]._currentState.TotalVoltage");
            double idc = GuiSimDataAccess.SafeGetDouble($"ess._batteryRacks[{bmsIndex}]._currentState.TotalCurrent");
            return new List<string>
            {
                $"SOC{soc:0.0}% Vdc{vdc:0}V",
                $"Idc{idc:0.0}A",
                GuiStatusFormatters.FormatBmsMainLineGridConnect(bmsIndex),
                GuiStatusFormatters.FormatBmsMainLineBlackStart(bmsIndex)
            };
        }

        private static IEnumerable<string> BuildBus690Lines(UnitBranchSnapshot unit)
        {
            if (unit.Bus690 != null)
            {
                var b = unit.Bus690.Value;
                yield return $"{b.BusId} V {FormatCompactV(b.LineVoltageV)} I {b.LineCurrentA:0}A";
            }
            else
            {
                yield return "无数据";
            }
        }

        private static string ShortPcsMode(PcsChannelSnapshot pcs)
        {
            string g = GuiStatusFormatters.FormatGridModeLabel(pcs.GridMode);
            if (!pcs.BlackStartEnabled)
                return g;
            return g + " " + GuiStatusFormatters.FormatBlackStartPhaseLabel(pcs.BlackStartPhase);
        }

        private static string FormatCompactV(double v) =>
            v >= 1000 ? $"{v / 1000:0.0}kV" : $"{v:0}V";

        private static IEnumerable<string> BuildGridLayerLines(MainLineSnapshot snap)
        {
            if (snap.BusGrid != null)
            {
                var b = snap.BusGrid.Value;
                var p = new AcPhasorSnapshot(b.LineVoltageV, b.LineCurrentA, b.PhaseAngleDeg, b.FrequencyHz);
                yield return $"V {FormatCompactV(b.LineVoltageV)} I {b.LineCurrentA:0}A";
                yield return $"P {p.ActivePowerKw:0} Q {p.ReactivePowerKvar:0} kvar";
            }
            else
            {
                yield return $"PCC {GuiStatusFormatters.FormatVoltage(snap.PccLineVoltageV)}";
            }
        }

        private static IEnumerable<string> BuildMainBreakerTransformerLines(
            MainLineSnapshot snap,
            int channelStart,
            int channelEndExclusive)
        {
            yield return $"主断 {GuiStatusFormatters.FormatBreakerState(snap.MainBreakerClosed, snap.MainBreakerTripped)}";
            yield return $"黑启 {GuiStatusFormatters.BuildBlackStartSwitchSummary(channelStart, channelEndExclusive)}";
            yield return $"主变 P {snap.MainTransformerSecondary.ActivePowerKw:0} Q {snap.MainTransformerSecondary.ReactivePowerKvar:0}";
        }

        private static IEnumerable<string> BuildMeterBoxLines(MainLineSnapshot snap)
        {
            double f = snap.MeterPrimary.FrequencyHz;
            yield return $"f {FormatCompactFrequency(f)}";
            yield return $"V {FormatCompactV(snap.MeterPrimary.LineVoltageV)} I {snap.MeterPrimary.LineCurrentA:0.0}A";
            yield return $"P {snap.MeterPrimary.ActivePowerKw:0} Q {snap.MeterPrimary.ReactivePowerKvar:0} kvar";
        }

        private static string FormatCompactFrequency(double frequencyHz) =>
            frequencyHz > 0.05 ? $"{frequencyHz:0.##} Hz" : "0 Hz";

        private static IEnumerable<string> BuildBus35Lines(MainLineSnapshot snap)
        {
            if (snap.Bus35Propagation != null)
            {
                var b = snap.Bus35Propagation.Value;
                var p = new AcPhasorSnapshot(b.LineVoltageV, b.LineCurrentA, b.PhaseAngleDeg, b.FrequencyHz);
                yield return $"V {FormatCompactV(b.LineVoltageV)} I {b.LineCurrentA:0.0}A φ{b.PhaseAngleDeg:0}°";
                yield return $"P {p.ActivePowerKw:0} Q {p.ReactivePowerKvar:0} kvar";
            }
            else
            {
                yield return GuiStatusFormatters.FormatVoltage(snap.StationBus35LineVoltageV);
            }
        }

        private static IEnumerable<string> BuildLoadLines(MainLineSnapshot snap)
        {
            yield return $"P {snap.LoadActivePowerKw:0.0} kW";
            yield return $"Q {snap.LoadReactivePowerKvar:0.0} kvar";
        }

        private static IEnumerable<string> BuildUnitBranchNodeLines(UnitBranchSnapshot unit)
        {
            yield return $"单元断 {GuiStatusFormatters.FormatBreakerState(unit.UnitBreakerClosed, unit.UnitBreakerTripped)}";
            yield return $"一次 P {unit.UnitTransformerPrimary.ActivePowerKw:0} Q {unit.UnitTransformerPrimary.ReactivePowerKvar:0}";
            yield return $"二次 P {unit.UnitTransformerSecondary.ActivePowerKw:0} Q {unit.UnitTransformerSecondary.ReactivePowerKvar:0}";
        }

        private static int GetDisplayWidth(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
            int width = 0;
            foreach (char c in text)
                width += IsWideDisplayChar(c) ? 2 : 1;
            return width;
        }

        private static bool IsWideDisplayChar(char c)
        {
            if (c <= 0x7F)
                return false;
            if (c is >= (char)0xFF61 and <= (char)0xFF9F)
                return false;
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                return false;
            return c is >= (char)0x1100 and <= (char)0x115F
                or (char)0x2329 or (char)0x232A
                or >= (char)0x2E80 and <= (char)0xA4CF
                or >= (char)0xAC00 and <= (char)0xD7A3
                or >= (char)0xF900 and <= (char)0xFAFF
                or >= (char)0xFE30 and <= (char)0xFE6F
                or >= (char)0xFF00 and <= (char)0xFF60
                or >= (char)0xFFE0 and <= (char)0xFFE6;
        }

        private static string PadToDisplayWidth(string text, int targetWidth, char padChar = ' ')
        {
            int width = GetDisplayWidth(text);
            if (width >= targetWidth)
                return TrimToDisplayWidth(text, targetWidth);
            return text + new string(padChar, targetWidth - width);
        }

        private static string TrimToDisplayWidth(string text, int maxWidth)
        {
            if (GetDisplayWidth(text) <= maxWidth)
                return text;
            var sb = new StringBuilder();
            int width = 0;
            foreach (char c in text)
            {
                int charWidth = IsWideDisplayChar(c) ? 2 : 1;
                if (width + charWidth > maxWidth)
                    break;
                sb.Append(c);
                width += charWidth;
            }
            return sb.ToString();
        }
    }
}
