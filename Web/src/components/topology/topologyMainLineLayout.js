/**
 * 由组态工程「连通关系 + 角色」推导电气主接线单线图布局。
 * 图例风格对齐原 MainLineSvg（单母线、断路器符、双圈变压器），不复用组态画布方块图案。
 */

function neighbors(edges, nodeId) {
  const ids = []
  for (const e of edges || []) {
    if (e.fromNodeId === nodeId) ids.push(e.toNodeId)
    else if (e.toNodeId === nodeId) ids.push(e.fromNodeId)
  }
  return ids
}

function truthy(v) {
  return v === true || v === 'true' || v === 1
}

function paramNum(node, key, fallback = 0) {
  const v = Number(node?.parameters?.[key])
  return Number.isFinite(v) ? v : fallback
}

function fmtKv(v) {
  const n = Number(v)
  if (!Number.isFinite(n) || n <= 0) return '—'
  if (n >= 1000) return `${(n / 1000).toFixed(n % 1000 === 0 ? 0 : 1)}kV`
  return `${n.toFixed(0)}V`
}

function findBmsForEmu(nodes, edges, emuId) {
  const neighborIds = new Set(neighbors(edges, emuId))
  const dcBuses = nodes
    .filter(n => n.templateId === 'dc_bus' && neighborIds.has(n.id))
    .map(n => n.id)
  const dcSet = new Set(dcBuses)
  return nodes
    .filter(n => {
      if (n.templateId !== 'bms') return false
      const nb = new Set(neighbors(edges, n.id))
      return nb.has(emuId) || [...nb].some(id => dcSet.has(id))
    })
    .sort((a, b) => (a.x - b.x) || (a.y - b.y))
}

function findDcBusForEmu(nodes, edges, emuId) {
  const neighborIds = new Set(neighbors(edges, emuId))
  return nodes.find(n => n.templateId === 'dc_bus' && neighborIds.has(n.id)) || null
}

/** 负载挂接点：直连 AC 母线，或经断路器接到 AC 母线 */
function findLoadMountBus(nodes, edges, loadId) {
  const byId = new Map(nodes.map(n => [n.id, n]))
  for (const nbId of neighbors(edges, loadId)) {
    const nb = byId.get(nbId)
    if (!nb) continue
    if (nb.templateId === 'ac_bus') return nb
    if (nb.templateId === 'ac_breaker') {
      for (const farId of neighbors(edges, nbId)) {
        if (farId === loadId) continue
        const far = byId.get(farId)
        if (far?.templateId === 'ac_bus') return far
      }
    }
  }
  return null
}

/** 按连通母线或额定电压，判定负载挂在 HV / LV */
function resolveLoadAttachSide(load, loadBus, busHv, busLv) {
  if (!load) return null
  if (loadBus && busHv && loadBus.id === busHv.id) return 'hv'
  if (loadBus && busLv && loadBus.id === busLv.id) return 'lv'
  // 未识别到连线时，按额定电压靠近哪段母线推断
  const rated = paramNum(load, 'ratedVoltage', 0)
  if (rated > 0) {
    const hvV = paramNum(busHv, 'nominalVoltage', 220000)
    const lvV = paramNum(busLv, 'nominalVoltage', 35000)
    if (Math.abs(rated - hvV) <= Math.abs(rated - lvV)) return 'hv'
    return 'lv'
  }
  return busHv ? 'hv' : (busLv ? 'lv' : null)
}

/**
 * @param {object} topology TopologyProject
 * @param {object[]} units MainLineUnitViewModel[]
 */
export function buildTopologyMainLineLayout(topology, units = []) {
  const nodes = topology?.nodes || []
  const edges = topology?.edges || []

  const grid = nodes.find(n => n.templateId === 'grid') || null
  const mainBreaker = nodes.find(n =>
    n.templateId === 'ac_breaker' && truthy(n.parameters?.isMainBreaker)
  ) || null
  const pccMeter = nodes.find(n =>
    n.templateId === 'ac_meter' && truthy(n.parameters?.isPccMeter)
  ) || nodes.find(n => n.templateId === 'ac_meter') || null

  const transformers = nodes.filter(n => n.templateId === 'transformer').sort((a, b) => a.x - b.x)
  const acBuses = nodes.filter(n => n.templateId === 'ac_bus').sort((a, b) =>
    paramNum(b, 'nominalVoltage') - paramNum(a, 'nominalVoltage') || a.y - b.y
  )
  const emus = nodes.filter(n => n.templateId === 'emu').sort((a, b) => a.x - b.x || a.y - b.y)
  const load = nodes.find(n => n.templateId === 'load') || null

  // 高压母线：额定最高或接电网/主断一侧
  const busHv = acBuses[0] || null
  const busLv = acBuses.find(b => b.id !== busHv?.id) || null
  const mainXfmr = transformers[0] || null

  // 负载挂接母线由组态连线决定（非写死 HV/LV）
  const loadBus = load ? findLoadMountBus(nodes, edges, load.id) : null
  const loadAttachSide = resolveLoadAttachSide(load, loadBus, busHv, busLv)
  const hasLoad = !!load && !!loadAttachSide
  const loadOnHv = loadAttachSide === 'hv'
  const loadOnLv = loadAttachSide === 'lv'

  const UNIT_W = 360
  const MARGIN_X = 48
  const MARGIN_TOP = 24
  /** 设备与设备/母线之间的最短可见引线（单线图规则：禁止贴连） */
  const LINK_STUB = 18
  const BRK_SPAN = 28
  const unitCount = Math.max(1, emus.length || units.length || 1)

  /**
   * 规则：母线下方只挂 1 台设备时可省略母线，上下设备用黑线直连。
   * - HV：下方挂主变(+可选并网点电表、挂在 HV 的负载)；仅 1 挂时省略
   * - LV：下方挂各 EMU(+挂在 LV 的负载)；合计仅 1 挂时省略
   */
  const hvFeedersBelow = 1 + (pccMeter ? 1 : 0) + (loadOnHv ? 1 : 0)
  const omitBusHv = hvFeedersBelow <= 1
  const lvFeedersBelow = unitCount + (loadOnLv ? 1 : 0)
  const omitBusLv = lvFeedersBelow <= 1

  const stationCenterX = MARGIN_X + (unitCount * UNIT_W) / 2
  /**
   * 规则：同行相邻设备中心距（避免符号/标签/框体重叠）。
   * 电表框半宽 32 + 主变标签约 90 + 空隙 ≈ 150+
   */
  const ROW_PEER_GAP = 168
  const METER_HALF_W = 32
  // 单单元省略 LV 时主变落在中轴，与单元断直连；有电表则电表右移保证间距
  const xfmrX = omitBusLv
    ? stationCenterX
    : (pccMeter ? stationCenterX - ROW_PEER_GAP / 2 : stationCenterX - 40)
  const meterX = pccMeter
    ? (omitBusLv ? xfmrX + ROW_PEER_GAP : stationCenterX + ROW_PEER_GAP / 2)
    : stationCenterX + ROW_PEER_GAP
  // 负载 X：挂哪段母线就画在哪段，并与同母线其它挂点避让
  let loadX = null
  if (loadOnHv) loadX = xfmrX - ROW_PEER_GAP
  else if (loadOnLv) loadX = unitCount === 1 ? stationCenterX - ROW_PEER_GAP : MARGIN_X + 72
  const loadStub = LINK_STUB
  const loadSymbolH = 36
  // 母线左右端需包住挂接设备（含电表框、负载）
  let busLeft = Math.min(MARGIN_X + 40, xfmrX - 40)
  if (hasLoad) busLeft = Math.min(busLeft, loadX - 40)
  const busRight = Math.max(
    MARGIN_X + unitCount * UNIT_W - 40,
    pccMeter ? meterX + METER_HALF_W + 16 : stationCenterX + 40
  )

  // 纵向站侧：电网 —引线—（主断）—引线— [HV母线?] —引线— 主变 —引线— [LV母线?] → 各单元
  let y = MARGIN_TOP
  const yGrid = y
  y += 50
  const yAfterGrid = y

  let yMainBreaker = null
  let yBrkTop = null
  let yBrkBottom = null
  if (mainBreaker) {
    yBrkTop = yAfterGrid + LINK_STUB
    yMainBreaker = yBrkTop + BRK_SPAN / 2
    yBrkBottom = yBrkTop + BRK_SPAN
    y = yBrkBottom + LINK_STUB
  } else {
    y = yAfterGrid + LINK_STUB
  }

  // 有 HV 母线时占位；省略时该 Y 仅为上下设备引线交接点（不画母线）
  const yBusHv = y
  y += LINK_STUB
  const yXfmr = y
  const xfmrSpan = 44
  /** 并网点电表框高度；两母线间距须容纳电表，避免被 LV 母线/单元支路遮挡 */
  const meterH = 72
  const bayDeviceH = Math.max(xfmrSpan, pccMeter ? meterH : xfmrSpan)
  y += bayDeviceH + LINK_STUB + (omitBusLv ? LINK_STUB : 24)
  // 有 LV 母线时画粗线；省略时该 Y 为主变→单元断的引线交接点
  const yBusLv = y
  const yUnitTop = yBusLv

  // 单元支路纵向：… —引线— 单元断 —引线— 单元变 —引线— [690母线?] —引线— PCS
  const unitBrkTop = LINK_STUB
  const unitBrkMid = unitBrkTop + BRK_SPAN / 2
  const unitBrkBottom = unitBrkTop + BRK_SPAN
  const unitXfmrTop = unitBrkBottom + LINK_STUB
  const unitXfmrSpan = 38
  const unitBus690Y = unitXfmrTop + unitXfmrSpan + LINK_STUB
  const channelX = 92
  const pcsTop = unitBus690Y + 22
  const pcsH = 228
  const gap = LINK_STUB * 2
  const dcBusY = pcsTop + pcsH + gap
  const bmsTop = dcBusY + LINK_STUB * 2
  const bmsH = 198
  const unitBottom = bmsTop + bmsH + 16

  // 虚线框仅为透视遮罩坐标，不参与电气布局计算
  const groups = []
  const unitLayouts = []

  for (let i = 0; i < unitCount; i++) {
    const emu = emus[i] || null
    const unitSnap = units[i] || units.find(u => (u.unitIndex ?? 0) === i) || null
    const bmsNodes = emu ? findBmsForEmu(nodes, edges, emu.id) : []
    const dcBus = emu ? findDcBusForEmu(nodes, edges, emu.id) : null
    const cx = MARGIN_X + i * UNIT_W + UNIT_W / 2
    const pcsA = unitSnap?.channelA || null
    const pcsB = unitSnap?.channelB || null
    const pcsHangCount = (pcsA ? 1 : 0) + (pcsB ? 1 : 0)
    // 组态默认双 PCS；运行时尚未加载时仍按组态占位画 690 母线/双支路
    const expectPcs = Math.min(2, Math.max(1, paramNum(emu, 'pcsCount', 2) || 2))
    const drawPcsSlots = pcsHangCount > 0 ? pcsHangCount : expectPcs
    const omitBus690 = drawPcsSlots <= 1
    const runtimeMissing = !!emu && !unitSnap
    const dcParallel = !!dcBus
    const bmsHangCount = dcParallel
      ? Math.max(bmsNodes.length, drawPcsSlots)
      : 0
    // 直流母线下方挂 BMS；仅 1 路时省略（双路并联仍画母线）
    const omitDcBus = dcParallel && bmsHangCount <= 1

    const emuBoxTop = 4
    const emuBoxH = pcsTop + pcsH + 8

    unitLayouts.push({
      index: i,
      cx,
      emu,
      dcBus,
      bmsNodes,
      unitSnap,
      pcsA,
      pcsB,
      drawPcsSlots,
      runtimeMissing,
      channelX,
      unitBrkTop,
      unitBrkMid,
      unitBrkBottom,
      unitXfmrTop,
      unitXfmrSpan,
      unitBus690Y,
      omitBus690,
      pcsTop,
      pcsH,
      dcBusY,
      omitDcBus,
      bmsTop,
      bmsH,
      bottom: unitBottom,
      label: emu?.label || (unitSnap?.unitNumber != null
        ? `UNIT ${unitSnap.unitNumber}`
        : `UNIT ${i + 1}`),
      dcVoltageLabel: fmtKv(paramNum(dcBus, 'nominalVoltage', 1200)),
      dcParallel
    })

    if (emu) {
      groups.push({
        id: `emu-${i}`,
        x: cx - UNIT_W / 2 + 12,
        y: yUnitTop + emuBoxTop,
        w: UNIT_W - 24,
        h: emuBoxH
      })
    }
  }

  const height = yUnitTop + unitBottom + 36
  const width = Math.max(
    720,
    MARGIN_X * 2 + unitCount * UNIT_W,
    busRight + MARGIN_X,
    (pccMeter ? meterX + METER_HALF_W : stationCenterX) + MARGIN_X,
    hasLoad ? loadX + 80 + MARGIN_X : 0
  )

  const hvV = paramNum(busHv, 'nominalVoltage', paramNum(grid, 'outputVoltage', 220000))
  const lvV = paramNum(busLv, 'nominalVoltage', paramNum(mainXfmr, 'secondaryVoltage', 35000))

  return {
    width,
    height,
    stationCenterX,
    xfmrX,
    meterX,
    rowPeerGap: ROW_PEER_GAP,
    /** 有并网点电表时主变标签放左侧，避免与右侧电表抢位 */
    xfmrLabelSide: pccMeter ? 'left' : 'right',
    busLeft,
    busRight,
    linkStub: LINK_STUB,
    yGrid,
    yAfterGrid,
    yMainBreaker,
    yBrkTop,
    yBrkBottom,
    yBusHv,
    omitBusHv,
    yXfmr,
    xfmrSpan,
    meterH,
    yBusLv,
    omitBusLv,
    yUnitTop,
    hasMainBreaker: !!mainBreaker,
    hasPccMeter: !!pccMeter,
    hasLoad,
    load,
    loadBus,
    /** 'hv' | 'lv'：由组态连通决定挂接点 */
    loadAttachSide,
    loadX,
    loadStub,
    loadSymbolH,
    loadLabel: load?.label || load?.parameters?.name || '负载',
    grid,
    mainBreaker,
    pccMeter,
    mainXfmr,
    busHv,
    busLv,
    busHvLabel: busHv
      ? `${busHv.label || 'AC母线'} ${fmtKv(hvV)}`
      : `${fmtKv(hvV)} 母线`,
    busLvLabel: busLv
      ? `${busLv.label || 'AC母线'} ${fmtKv(lvV)}`
      : `${fmtKv(lvV)} 母线`,
    xfmrLabel: mainXfmr
      ? `${fmtKv(paramNum(mainXfmr, 'primaryVoltage', hvV))}/${fmtKv(paramNum(mainXfmr, 'secondaryVoltage', lvV))}`
      : `${fmtKv(hvV)}/${fmtKv(lvV)}`,
    meterLabel: pccMeter?.label || '并网点电表',
    units: unitLayouts,
    groups
  }
}
