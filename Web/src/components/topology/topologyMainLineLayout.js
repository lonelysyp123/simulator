/**
 * 由组态工程图（节点 + 连线）推导电气主接线单线图。
 * 站侧结构随工程变化：母线、变压器、电表、负载按连通递归布局；
 * 储能支路以「同一 emuId 的 PCS 组」为单元展开（EMU 为虚拟节点不参与连线），
 * 光伏单元按设备类型展开内部图例，不按某个具体工程写死站侧骨架。
 */

function truthy(v) {
  return v === true || v === 'true' || v === 1
}

function paramNum(node, key, fallback = 0) {
  const v = Number(node?.parameters?.[key])
  return Number.isFinite(v) ? v : fallback
}

function paramStr(node, key, fallback = '') {
  const v = node?.parameters?.[key]
  if (v == null || String(v).trim() === '') return fallback
  return String(v)
}

function splitCount(n) {
  const total = Math.max(0, Math.round(Number(n) || 0))
  const a = Math.ceil(total / 2)
  return { a, b: total - a }
}

function fmtKv(v) {
  const n = Number(v)
  if (!Number.isFinite(n) || n <= 0) return '—'
  if (n >= 1000) return `${(n / 1000).toFixed(n % 1000 === 0 ? 0 : 1)}kV`
  return `${n.toFixed(0)}V`
}

function neighborsOf(adj, nodeId) {
  return adj.get(nodeId) || []
}

export function makeGraph(nodes, edges) {
  const byId = new Map((nodes || []).map(n => [n.id, n]))
  const adj = new Map()
  for (const n of nodes || []) adj.set(n.id, new Set())
  for (const e of edges || []) {
    if (!adj.has(e.fromNodeId) || !adj.has(e.toNodeId)) continue
    adj.get(e.fromNodeId).add(e.toNodeId)
    adj.get(e.toNodeId).add(e.fromNodeId)
  }
  const listAdj = new Map()
  for (const [id, set] of adj) listAdj.set(id, [...set])
  return { nodes: nodes || [], edges: edges || [], byId, adj: listAdj }
}

/** 相邻设备；断路器视为透明，取其另一侧 */
function acHops(graph, fromId, skipIds = new Set()) {
  const hops = []
  const seen = new Set()
  for (const nid of neighborsOf(graph.adj, fromId)) {
    if (skipIds.has(nid)) continue
    const n = graph.byId.get(nid)
    if (!n) continue
    if (n.templateId === 'ac_breaker') {
      for (const farId of neighborsOf(graph.adj, n.id)) {
        if (farId === fromId || skipIds.has(farId)) continue
        const far = graph.byId.get(farId)
        if (!far || far.templateId === 'ac_breaker') continue
        if (seen.has(far.id)) continue
        seen.add(far.id)
        hops.push({ node: far, viaBreaker: n })
      }
    } else if (!seen.has(n.id)) {
      seen.add(n.id)
      hops.push({ node: n, viaBreaker: null })
    }
  }
  return hops
}

function xfmrConnectedBuses(graph, xfmrId) {
  const buses = []
  const seen = new Set()
  for (const hop of acHops(graph, xfmrId)) {
    if (hop.node.templateId !== 'ac_bus' || seen.has(hop.node.id)) continue
    seen.add(hop.node.id)
    buses.push(hop.node)
  }
  return buses
}

function classifyXfmr(graph, xf) {
  const buses = xfmrConnectedBuses(graph, xf.id)
  const priV = paramNum(xf, 'primaryVoltage', 0)
  const secV = paramNum(xf, 'secondaryVoltage', 0)
  const score = (bus, v) => {
    const bv = paramNum(bus, 'nominalVoltage', 0)
    if (v > 0 && bv > 0) return Math.abs(bv - v)
    return 1e12
  }
  if (buses.length >= 2) {
    let best = null
    for (const a of buses) {
      for (const b of buses) {
        if (a.id === b.id) continue
        const s = score(a, priV) + score(b, secV)
        const yBias = (a.y || 0) - (b.y || 0)
        const key = s + yBias * 1e-6
        if (!best || key < best.key) best = { pri: a, sec: b, key }
      }
    }
    return { pri: best.pri, sec: best.sec }
  }
  if (buses.length === 1) {
    const bus = buses[0]
    return score(bus, priV) <= score(bus, secV)
      ? { pri: bus, sec: null }
      : { pri: null, sec: bus }
  }
  return { pri: null, sec: null }
}

const STEM_SKIP = new Set(['transformer', 'pcs', 'pv_unit', 'load', 'ac_meter', 'bms', 'dc_bus'])

function pathToFirstBus(graph, startId) {
  const q = [{ id: startId, breakers: [] }]
  const seen = new Set([startId])
  while (q.length) {
    const cur = q.shift()
    const node = graph.byId.get(cur.id)
    if (node?.templateId === 'ac_bus') return { bus: node, breakers: cur.breakers }
    for (const nid of neighborsOf(graph.adj, cur.id)) {
      if (seen.has(nid)) continue
      const nb = graph.byId.get(nid)
      if (!nb || STEM_SKIP.has(nb.templateId)) continue
      seen.add(nid)
      q.push({
        id: nid,
        breakers: nb.templateId === 'ac_breaker' ? [...cur.breakers, nb] : cur.breakers
      })
    }
  }
  return { bus: null, breakers: [] }
}

function hangKind(node) {
  if (node.templateId === 'pcs' || node.templateId === 'pv_unit') return 'feeder'
  return node.templateId
}

function buildBusFrame(graph, bus, incomingXfmrId, visitedBuses, visitedXfmrs) {
  visitedBuses.add(bus.id)
  const skip = new Set([incomingXfmrId].filter(Boolean))
  const hangs = []
  const xfmrs = []
  for (const hop of acHops(graph, bus.id, skip)) {
    const n = hop.node
    if (!n || n.templateId === 'grid' || n.templateId === 'dc_bus' || n.templateId === 'bms') continue
    if (n.templateId === 'transformer') {
      if (visitedXfmrs.has(n.id)) continue
      visitedXfmrs.add(n.id)
      const sides = classifyXfmr(graph, n)
      const others = xfmrConnectedBuses(graph, n.id).filter(b => b.id !== bus.id)
      const far = (sides.pri?.id === bus.id ? sides.sec : sides.pri) || others[0] || null
      let downstream = null
      if (far && far.id !== bus.id && !visitedBuses.has(far.id)) {
        downstream = buildBusFrame(graph, far, n.id, visitedBuses, visitedXfmrs)
      }
      xfmrs.push({ xfmr: n, downstream })
      continue
    }
    if (n.templateId === 'pcs' || n.templateId === 'pv_unit' || n.templateId === 'load' || n.templateId === 'ac_meter') {
      // 已归入 EMU 的电表在单元框内绘制，不再作为母线挂件重复入图
      if (n.templateId === 'ac_meter' && paramStr(n, 'emuId')) continue
      hangs.push({ node: n })
    }
  }
  return { node: bus, incomingXfmrId, hangs, xfmrs, synthetic: false }
}

function walkFrames(frame, visit) {
  visit(frame)
  for (const xf of frame.xfmrs) {
    if (xf.downstream) walkFrames(xf.downstream, visit)
  }
}

export function findBmsForPcs(graph, pcsId) {
  const neighborIds = new Set(neighborsOf(graph.adj, pcsId))
  const dcBuses = graph.nodes
    .filter(n => n.templateId === 'dc_bus' && neighborIds.has(n.id))
    .map(n => n.id)
  const dcSet = new Set(dcBuses)
  return graph.nodes
    .filter(n => {
      if (n.templateId !== 'bms') return false
      const nb = new Set(neighborsOf(graph.adj, n.id))
      return nb.has(pcsId) || [...nb].some(id => dcSet.has(id))
    })
    .sort((a, b) => (a.x - b.x) || (a.y - b.y))
}

function findDcBusForPcs(graph, pcsNodes) {
  for (const p of pcsNodes || []) {
    const neighborIds = new Set(neighborsOf(graph.adj, p.id))
    const bus = graph.nodes.find(n => n.templateId === 'dc_bus' && neighborIds.has(n.id))
    if (bus) return bus
  }
  return null
}

/** 同一 emuId 的 PCS 归为一个储能支路；未归属的各自成组 */
function groupOfPcs(graph, pcsNode) {
  const emuId = paramStr(pcsNode, 'emuId') || '__unassigned__'
  const list = graph.nodes
    .filter(n => n.templateId === 'pcs' && (paramStr(n, 'emuId') || '__unassigned__') === emuId)
    .sort((a, b) => (a.x - b.x) || (a.y - b.y))
  return {
    key: emuId,
    leader: list[0] || pcsNode,
    list,
    emuNode: graph.byId.get(paramStr(pcsNode, 'emuId')) || null
  }
}

function pcsHang(graph, pcsNode) {
  const g = groupOfPcs(graph, pcsNode)
  return { node: g.leader, pcsGroup: g.list, emuNode: g.emuNode, groupKey: g.key }
}

/** 帧内多台 PCS 按 emuId 合并为一个支路挂点 */
function groupFramePcsHangs(graph, frame) {
  if (!frame.hangs.some(h => h.node.templateId === 'pcs')) return
  const others = []
  const merged = new Map()
  for (const h of frame.hangs) {
    if (h.node.templateId !== 'pcs') { others.push(h); continue }
    const hang = pcsHang(graph, h.node)
    if (!merged.has(hang.groupKey)) {
      merged.set(hang.groupKey, hang)
      others.push(hang)
    }
  }
  frame.hangs = others.sort((a, b) =>
    (a.node.x - b.node.x) || String(a.node.id).localeCompare(String(b.node.id)))
}

function canvasX(slotItem) {
  if (slotItem.kind === 'xfmr') return slotItem.xf.xfmr.x || 0
  return slotItem.hang.node.x || 0
}

function busLabel(bus, fallbackV = 0) {
  if (!bus) return `${fmtKv(fallbackV)} 母线`
  const v = paramNum(bus, 'nominalVoltage', fallbackV)
  return `${bus.label || 'AC母线'} ${fmtKv(v)}`
}

function expandFeederUnit(opts) {
  const {
    feeder, cx, xfmrId, originY, index, emuRank, pvRank, unitsSnap, graph,
    busCx, UNIT_W, LINK_STUB, BRK_SPAN, pcsGroup, emuNode
  } = opts
  const kind = feeder?.templateId === 'pv_unit' ? 'pv' : 'emu'
  // 储能支路：同一 EMU 的 PCS 组；EMU 为虚拟节点，仅承载单元变参数与标签
  const pcsNodes = kind === 'emu'
    ? (pcsGroup?.length ? pcsGroup : (feeder?.templateId === 'pcs' ? [feeder] : []))
    : []
  const emu = kind === 'emu' ? (emuNode || null) : null
  const pv = kind === 'pv' ? feeder : null
  // EMU 绑定的单元断路器 / 电表（组态中通过 emuId 归入本单元，各至多 1 台；未绑定为 null；组级绑定不参与单元级绘制）
  const unitBreakerNode = kind === 'emu' && emu
    ? (graph.nodes.find(n => n.templateId === 'ac_breaker' && paramStr(n, 'emuId') === emu.id && !paramStr(n, 'groupId')) || null)
    : null
  const unitMeterNode = kind === 'emu' && emu
    ? (graph.nodes.find(n => n.templateId === 'ac_meter' && paramStr(n, 'emuId') === emu.id && !paramStr(n, 'groupId')) || null)
    : null
  // 实时编号与运行时转换器同序（含 PCS 的 EMU 按 (Y,X)、PV 按 (Y,X)），
  // 与画布绘制顺序解耦，保证实时数据 / 控制命令落在与运行时一致的单元上
  const unitSnap = kind === 'emu' && emu
    ? (unitsSnap[emuRank.get(emu.id)] || null)
    : null
  const pvIndex = kind === 'pv' ? (pvRank.get(feeder.id) ?? -1) : -1
  const bmsNodes = []
  const bmsSeen = new Set()
  for (const p of pcsNodes) {
    for (const b of findBmsForPcs(graph, p.id)) {
      if (bmsSeen.has(b.id)) continue
      bmsSeen.add(b.id)
      bmsNodes.push(b)
    }
  }
  const dcBus = findDcBusForPcs(graph, pcsNodes)
  const pcsA = unitSnap?.channelA || null
  const pcsB = unitSnap?.channelB || null
  const pcsHangCount = (pcsA ? 1 : 0) + (pcsB ? 1 : 0)
  // 单线图每单元最多展示 2 台（channelA/channelB 视图模型）
  const expectPcs = Math.min(2, Math.max(1, pcsNodes.length || 2))
  const drawPcsSlots = kind === 'pv'
    ? 0
    : (pcsHangCount > 0 ? pcsHangCount : expectPcs)
  // 绑定单元电表时强制画出 690 母线，作为电表取电挂点
  const omitBus690 = kind === 'pv' || (drawPcsSlots <= 1 && !unitMeterNode)
  const runtimeMissing = kind === 'emu' && pcsNodes.length > 0 && !unitSnap
  const dcParallel = kind === 'emu' && !!dcBus
  const bmsHangCount = dcParallel ? Math.max(bmsNodes.length, drawPcsSlots) : 0
  const omitDcBus = dcParallel && bmsHangCount <= 1

  const unitBrkTop = LINK_STUB
  const unitBrkMid = unitBrkTop + BRK_SPAN / 2
  const unitBrkBottom = unitBrkTop + BRK_SPAN
  const unitXfmrTop = unitBrkBottom + LINK_STUB
  const unitXfmrSpan = kind === 'pv' ? 0 : 38
  const unitBus690Y = unitXfmrTop + unitXfmrSpan + LINK_STUB
  const channelX = 92
  const xfmrCardH = kind === 'pv' ? 160 : 256
  const xfmrCardTop = unitXfmrTop
  const pcsTop = kind === 'pv' ? xfmrCardTop : (unitBus690Y + 22)
  const pcsH = kind === 'pv' ? xfmrCardH : 228
  const dcBusY = pcsTop + pcsH + LINK_STUB * 2
  const bmsTop = kind === 'pv' ? (xfmrCardTop + xfmrCardH + LINK_STUB * 2) : (dcBusY + LINK_STUB * 2)
  const bmsH = kind === 'pv' ? 124 : 214
  const arraySplitY = kind === 'pv' ? (xfmrCardTop + xfmrCardH + LINK_STUB) : 0
  const unitBottom = bmsTop + bmsH + 16
  const boxTop = 4
  // 虚线框覆盖完整单元内容：上含单元断路器，下含 BMS / 直流母线
  const boxBottom = unitBottom
  // 单元电表挂点：690 母线右侧（PCS-B 卡片外侧）；未绑定不画
  const UNIT_METER_HALF_W = 32
  const UNIT_METER_H = 72
  const unitMeterX = 196
  const unitMeterTopY = unitBus690Y + LINK_STUB

  const inverterCount = Math.max(1, Math.round(paramNum(pv, 'inverterCount', 16)))
  const inverterRatedKw = paramNum(pv, 'inverterRatedPowerKw', 320)
  const stringCount = Math.max(1, Math.round(paramNum(pv, 'stringCount', 16)))
  const modulesPerString = Math.max(1, Math.round(paramNum(pv, 'modulesPerString', 30)))
  const xfPrimary = paramNum(pv || emu, 'unitXfPrimaryV', 35000)
  const xfSecondary = paramNum(pv || emu, 'unitXfSecondaryV', 690)
  const split = splitCount(inverterCount)
  const moduleModel = paramStr(pv, 'moduleModel', 'TSM-NEG21C.20Q')
  const dcVoltageMin = paramNum(pv, 'dcVoltageMin', 500)
  const dcVoltageMax = paramNum(pv, 'dcVoltageMax', 1500)
  const inverterEfficiency = paramNum(pv, 'inverterEfficiency', 0.99)
  const inverterAcVoltage = paramNum(pv, 'inverterAcVoltage', xfSecondary)
  const makePvGroup = (side, count) => ({
    side,
    inverterCount: count,
    inverterRatedKw,
    inverterMaxKw: paramNum(pv, 'inverterMaxPowerKw', inverterRatedKw),
    inverterEfficiency,
    inverterAcVoltage,
    totalRatedKw: count * inverterRatedKw,
    moduleModel,
    modulesPerString,
    stringCount,
    totalModuleCount: count * stringCount * modulesPerString,
    dcVoltageMin,
    dcVoltageMax
  })

  const unit = {
    index,
    kind,
    cx,
    busCx: busCx ?? cx,
    originY,
    xfmrId,
    emu,
    pv,
    pcsNodes,
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
    label: (emu || pv)?.label || (unitSnap?.unitNumber != null
      ? `UNIT ${unitSnap.unitNumber}`
      : `UNIT ${index + 1}`),
    unitXfLabel: `${fmtKv(xfPrimary)}/${fmtKv(xfSecondary)}`,
    inverterCount,
    inverterRatedKw,
    inverterMaxKw: paramNum(pv, 'inverterMaxPowerKw', inverterRatedKw),
    inverterEfficiency,
    inverterAcVoltage,
    totalRatedKw: inverterCount * inverterRatedKw,
    xfRatedKva: paramNum(pv, 'unitXfRatedKva', inverterCount * inverterRatedKw),
    moduleModel,
    modulesPerString,
    stringCount,
    totalModuleCount: inverterCount * stringCount * modulesPerString,
    dcVoltageMin,
    dcVoltageMax,
    groupA: makePvGroup('A', split.a),
    groupB: makePvGroup('B', split.b),
    pvIndex,
    xfmrCardTop,
    xfmrCardH,
    arraySplitY,
    pvLvOffset: 7,
    pvLvBottomY: unitXfmrTop + unitXfmrSpan / 2 + 17,
    pvSplitY: arraySplitY,
    dcVoltageLabel: fmtKv(paramNum(dcBus, 'nominalVoltage', 1200)),
    dcParallel,
    unitBreakerNode,
    unitMeterNode,
    unitMeterX,
    unitMeterTopY,
    unitMeterHalfW: UNIT_METER_HALF_W,
    unitMeterH: UNIT_METER_H
  }

  // 虚线框：默认贴单元两侧；绑定电表时向右扩出电表位
  const boxLeftOffset = -UNIT_W / 2 + 12
  const boxRightOffset = unitMeterNode
    ? Math.max(UNIT_W / 2 - 12, unitMeterX + UNIT_METER_HALF_W + 8)
    : (UNIT_W / 2 - 12)
  const group = kind === 'emu'
    ? {
      id: `${kind}-${index}`,
      kind,
      x: cx + boxLeftOffset,
      y: originY + boxTop,
      w: boxRightOffset - boxLeftOffset,
      h: boxBottom - boxTop
    }
    : null

  return { unit, group, unitBottom }
}

/**
 * @param {object} topology TopologyProject
 * @param {object[]} units MainLineUnitViewModel[]
 */
export function buildTopologyMainLineLayout(topology, units = []) {
  const graph = makeGraph(topology?.nodes, topology?.edges)
  const nodes = graph.nodes
  const unitsSnap = units || []

  const UNIT_W = 360
  const HANG_W = 168
  const MARGIN_X = 48
  const MARGIN_TOP = 24
  const BAY_GAP = 48
  const ISLAND_GAP = 72
  const LINK_STUB = 18
  const BRK_SPAN = 28
  const XFMR_SPAN = 44
  const METER_H = 72
  const LOAD_SYMBOL_H = 36
  const METER_HALF_W = 32

  const grid = nodes.find(n => n.templateId === 'grid') || null
  const acBuses = nodes.filter(n => n.templateId === 'ac_bus').sort((a, b) =>
    paramNum(b, 'nominalVoltage') - paramNum(a, 'nominalVoltage') || (a.x - b.x)
  )
  const feeders = nodes
    .filter(n => n.templateId === 'pcs' || n.templateId === 'pv_unit')
    .sort((a, b) => (a.x - b.x) || (a.y - b.y))

  // 实时单元编号秩：与 TopologyRuntimeConverter 的排序规则保持一致
  const emuRank = new Map()
  nodes
    .filter(n => n.templateId === 'emu')
    .sort((a, b) => (a.y - b.y) || (a.x - b.x))
    .filter(e => nodes.some(p => p.templateId === 'pcs' && paramStr(p, 'emuId') === e.id))
    .forEach((e, i) => emuRank.set(e.id, i))
  const pvRank = new Map()
  nodes
    .filter(n => n.templateId === 'pv_unit')
    .sort((a, b) => (a.y - b.y) || (a.x - b.x))
    .forEach((n, i) => pvRank.set(n.id, i))

  const visitedBuses = new Set()
  const visitedXfmrs = new Set()
  const stem = pathToFirstBus(graph, grid?.id)
  const startBus = stem.bus || acBuses[0] || null
  const roots = []
  if (startBus) roots.push(buildBusFrame(graph, startBus, null, visitedBuses, visitedXfmrs))
  for (const bus of acBuses) {
    if (!visitedBuses.has(bus.id)) {
      roots.push(buildBusFrame(graph, bus, null, visitedBuses, visitedXfmrs))
    }
  }

  for (const root of roots) walkFrames(root, fr => groupFramePcsHangs(graph, fr))
  const hangingIds = new Set()
  for (const root of roots) walkFrames(root, fr => {
    for (const h of fr.hangs) {
      hangingIds.add(h.node.id)
      for (const p of h.pcsGroup || []) hangingIds.add(p.id)
    }
  })
  const frames = []
  for (const root of roots) walkFrames(root, fr => frames.push(fr))
  for (const f of feeders) {
    if (hangingIds.has(f.id)) continue
    // 同一 emuId 的 PCS 组只挂一次，整组标记为已挂
    const hang = f.templateId === 'pcs' ? pcsHang(graph, f) : { node: f }
    let best = frames[0] || null
    let bestDist = Infinity
    for (const fr of frames) {
      const d = Math.abs((hang.node.x || 0) - (fr.node?.x || 0))
      if (d < bestDist) {
        best = fr
        bestDist = d
      }
    }
    if (best) {
      best.hangs.push(hang)
      hangingIds.add(hang.node.id)
      for (const p of hang.pcsGroup || []) hangingIds.add(p.id)
    }
  }
  if (!roots.length && feeders.length) {
    const hangs = []
    const seenGroups = new Set()
    for (const f of feeders) {
      if (f.templateId === 'pcs') {
        const hang = pcsHang(graph, f)
        if (seenGroups.has(hang.groupKey)) continue
        seenGroups.add(hang.groupKey)
        hangs.push(hang)
      } else {
        hangs.push({ node: f })
      }
    }
    roots.push({
      node: null,
      incomingXfmrId: null,
      hangs,
      xfmrs: [],
      synthetic: true
    })
  }

  const kept = roots.filter(fr => fr.hangs.length > 0 || fr.xfmrs.length > 0 || fr === roots[0])
  const forest = kept.length ? kept : roots

  function measure(frame) {
    const items = [
      ...frame.xfmrs.map(xf => ({ kind: 'xfmr', xf })),
      ...frame.hangs
        .filter(hang => hangKind(hang.node) === 'feeder')
        .map(hang => ({ kind: 'feeder', hang }))
    ].sort((a, b) => canvasX(a) - canvasX(b) || String(a.hang?.node?.id || a.xf?.xfmr.id).localeCompare(String(b.hang?.node?.id || b.xf?.xfmr.id)))
    frame.taps = frame.hangs.filter(hang => {
      const k = hangKind(hang.node)
      return k === 'ac_meter' || k === 'load'
    }).sort((a, b) => (a.node.x - b.node.x) || String(a.node.id).localeCompare(b.node.id))
    const slots = []
    for (const item of items) {
      if (item.kind === 'xfmr') {
        if (item.xf.downstream) measure(item.xf.downstream)
        slots.push({ item, w: Math.max(item.xf.downstream?.width || 0, UNIT_W) })
      } else {
        slots.push({ item, w: UNIT_W })
      }
    }
    frame.slots = slots
    const tapCount = frame.taps.length
    if (!slots.length) {
      frame.width = Math.max(UNIT_W, tapCount * HANG_W)
      frame.omit = tapCount <= 1
      return
    }
    frame.width = slots.reduce((s, sl) => s + sl.w, 0) + BAY_GAP * (slots.length - 1)
    frame.omit = (slots.length + tapCount) <= 1
  }
  for (const root of forest) measure(root)

  const scene = {
    wires: [],
    buses: [],
    transformers: [],
    meters: [],
    loads: [],
    placements: []
  }

  function placeFrame(frame, x0, yBus) {
    frame.x0 = x0
    frame.y = yBus
    frame.width = Math.max(frame.width || UNIT_W, UNIT_W)
    frame.x1 = x0 + 40
    frame.x2 = x0 + frame.width - 40
    frame.cx = x0 + frame.width / 2
    if (frame.node) {
      const v = paramNum(frame.node, 'nominalVoltage', 0)
      scene.buses.push({
        id: frame.node.id,
        node: frame.node,
        x1: frame.x1,
        x2: frame.x2,
        cx: frame.cx,
        y: yBus,
        omit: !!frame.omit,
        label: busLabel(frame.node, v),
        voltage: v
      })
    }

    const slots = frame.slots || []
    const taps = frame.taps || []
    const hasXfmr = slots.some(s => s.item.kind === 'xfmr')
    const hasMeter = taps.some(t => t.node.templateId === 'ac_meter')
    const hasLoad = taps.some(t => t.node.templateId === 'load')
    const equipH = Math.max(
      hasXfmr ? XFMR_SPAN : 0,
      hasMeter ? METER_H : 0,
      hasLoad ? LOAD_SYMBOL_H + LINK_STUB : 0
    )
    const yEquip = yBus + LINK_STUB
    const structXs = []
    const busRec = frame.node ? scene.buses[scene.buses.length - 1] : null

    let x = x0
    slots.forEach((slot, idx) => {
      const cx = x + slot.w / 2
      const item = slot.item
      const hasRight = idx < slots.length - 1 || taps.length > 0
      if (item.kind === 'xfmr') {
        const xf = item.xf.xfmr
        const downstream = item.xf.downstream
        const pri = paramNum(xf, 'primaryVoltage', 0)
        const sec = paramNum(xf, 'secondaryVoltage', 0)
        scene.wires.push({ x1: cx, y1: yBus, x2: cx, y2: yEquip })
        const rec = {
          id: xf.id,
          node: xf,
          x: cx,
          y: yEquip,
          span: XFMR_SPAN,
          label: xf.label || '变压器',
          ratioLabel: `${fmtKv(pri)}/${fmtKv(sec)}`,
          kvaLabel: paramNum(xf, 'ratedPowerKva', 0) > 0
            ? `${paramNum(xf, 'ratedPowerKva', 0).toFixed(0)} kVA`
            : '',
          labelSide: hasRight ? 'left' : 'right',
          omitBusLv: downstream ? !!downstream.omit : true,
          busLeft: x + 40,
          busRight: x + slot.w - 40
        }
        scene.transformers.push(rec)
        structXs.push(cx)
        if (downstream) {
          const yChild = yEquip + equipH + LINK_STUB + (downstream.omit ? LINK_STUB : 24)
          scene.wires.push({ x1: cx, y1: yEquip + XFMR_SPAN, x2: cx, y2: yChild })
          placeFrame(downstream, x, yChild)
          rec.busLeft = downstream.x1
          rec.busRight = downstream.x2
          rec.omitBusLv = !!downstream.omit
        }
      } else if (item.kind === 'feeder') {
        structXs.push(cx)
        scene.placements.push({
          feeder: item.hang.node,
          cx,
          xfmrId: frame.incomingXfmrId || null,
          originY: yBus,
          busCx: frame.cx,
          pcsGroup: item.hang.pcsGroup || null,
          emuNode: item.hang.emuNode || null
        })
      }
      x += slot.w + BAY_GAP
    })

    const busX = frame.node?.x ?? frame.cx
    let rightX = Math.max(frame.cx, ...structXs)
    let leftX = Math.min(frame.cx, ...structXs)
    for (const tap of taps) {
      const n = tap.node
      const toRight = (n.x || 0) >= busX
      let tapX
      if (toRight) {
        rightX += HANG_W
        tapX = rightX
      } else {
        leftX -= HANG_W
        tapX = leftX
      }
      if (n.templateId === 'ac_meter') {
        scene.wires.push({ x1: tapX, y1: yBus, x2: tapX, y2: yEquip })
        scene.meters.push({
          id: n.id,
          node: n,
          busId: frame.node?.id || null,
          x: tapX,
          y: yEquip,
          h: METER_H,
          label: n.label || '电表',
          isPcc: truthy(n.parameters?.isPccMeter)
        })
      } else if (n.templateId === 'load') {
        scene.wires.push({ x1: tapX, y1: yBus, x2: tapX, y2: yBus + LINK_STUB })
        scene.loads.push({
          id: n.id,
          node: n,
          busId: frame.node?.id || null,
          x: tapX,
          busY: yBus,
          stub: LINK_STUB,
          symbolH: LOAD_SYMBOL_H,
          label: n.label || n.parameters?.name || '负载'
        })
      }
      if (busRec) {
        busRec.x1 = Math.min(busRec.x1, tapX - 40)
        busRec.x2 = Math.max(busRec.x2, tapX + 40)
      }
      frame.x1 = Math.min(frame.x1, tapX - 40)
      frame.x2 = Math.max(frame.x2, tapX + 40)
    }
  }

  let y = MARGIN_TOP
  const yGrid = y
  y += 50
  const stemBreakers = []
  let yCursor = y + LINK_STUB
  const pathBreakers = grid ? (stem.breakers || []) : []
  if (pathBreakers.length) {
    for (const br of pathBreakers) {
      const yTop = yCursor
      const yMid = yTop + BRK_SPAN / 2
      const yBot = yTop + BRK_SPAN
      stemBreakers.push({
        id: br.id,
        node: br,
        yTop,
        y: yMid,
        yBottom: yBot,
        isMain: truthy(br.parameters?.isMainBreaker),
        label: br.label || br.parameters?.name || '断路器'
      })
      yCursor = yBot + LINK_STUB
    }
  }
  const yRoot = yCursor

  let cursorX = MARGIN_X
  for (const root of forest) {
    placeFrame(root, cursorX, yRoot)
    cursorX += (root.width || UNIT_W) + ISLAND_GAP
  }

  const gridX = forest[0]?.cx ?? (MARGIN_X + UNIT_W / 2)
  if (grid) {
    const yFrom = yGrid + 8
    if (stemBreakers.length) {
      scene.wires.push({ x1: gridX, y1: yFrom, x2: gridX, y2: stemBreakers[0].yTop })
      for (let i = 0; i < stemBreakers.length; i++) {
        const b = stemBreakers[i]
        const yTo = i < stemBreakers.length - 1 ? stemBreakers[i + 1].yTop : yRoot
        scene.wires.push({ x1: gridX, y1: b.yBottom, x2: gridX, y2: yTo })
      }
    } else {
      scene.wires.push({ x1: gridX, y1: yFrom, x2: gridX, y2: yRoot })
    }
  }
  for (const b of stemBreakers) b.x = gridX

  const unitLayouts = []
  const groups = []
  let unitBottom = 400
  scene.placements.forEach((p, i) => {
    const built = expandFeederUnit({
      feeder: p.feeder,
      cx: p.cx,
      busCx: p.busCx,
      xfmrId: p.xfmrId,
      originY: p.originY,
      index: i,
      emuRank,
      pvRank,
      unitsSnap,
      graph,
      UNIT_W,
      LINK_STUB,
      BRK_SPAN,
      pcsGroup: p.pcsGroup,
      emuNode: p.emuNode
    })
    unitLayouts.push(built.unit)
    if (built.group) groups.push(built.group)
    unitBottom = Math.max(unitBottom, built.unitBottom)
  })

  const yUnitTop = unitLayouts[0]?.originY ?? yRoot
  const yBusHv = forest[0]?.y ?? yRoot
  const feederBuses = scene.buses.filter(b => unitLayouts.some(u => Math.abs(u.originY - b.y) < 0.5))
  const omitBusHv = forest[0] ? !!forest[0].omit : true
  const omitBusLv = feederBuses.length
    ? feederBuses.every(b => b.omit)
    : unitLayouts.length <= 1
  const firstXfmr = scene.transformers[0] || null
  const pccMeter = scene.meters.find(m => m.isPcc) || scene.meters[0] || null
  const mainBreakerNode = stemBreakers.find(b => b.isMain)?.node
    || nodes.find(n => n.templateId === 'ac_breaker' && truthy(n.parameters?.isMainBreaker))
    || null
  const firstLoad = scene.loads[0] || null
  const rootBus = forest[0]?.node || null
  const firstLvBus = scene.buses.find(b => b.id && b.id !== rootBus?.id)?.node || null

  let maxX = gridX + 80
  let maxY = yRoot + 80
  for (const b of scene.buses) {
    maxX = Math.max(maxX, b.x2 + 16)
    maxY = Math.max(maxY, b.y + 24)
  }
  for (const t of scene.transformers) {
    maxX = Math.max(maxX, t.x + 80)
    maxY = Math.max(maxY, t.y + t.span + 24)
  }
  for (const m of scene.meters) {
    maxX = Math.max(maxX, m.x + METER_HALF_W + 16)
    maxY = Math.max(maxY, m.y + m.h + 16)
  }
  for (const u of unitLayouts) {
    maxX = Math.max(maxX, u.cx + UNIT_W / 2)
    maxY = Math.max(maxY, u.originY + u.bottom + 36)
  }
  for (const l of scene.loads) {
    maxX = Math.max(maxX, l.x + 80)
  }

  const hvV = paramNum(rootBus, 'nominalVoltage', paramNum(grid, 'outputVoltage', 220000))
  const lvV = paramNum(firstLvBus, 'nominalVoltage', paramNum(firstXfmr?.node, 'secondaryVoltage', 35000))
  const mainBrk = stemBreakers.find(b => b.isMain) || stemBreakers[0] || null

  return {
    width: Math.max(720, maxX + MARGIN_X),
    height: Math.max(320, maxY),
    wires: scene.wires,
    buses: scene.buses,
    transformers: scene.transformers,
    meters: scene.meters,
    loads: scene.loads,
    stemBreakers,
    gridX,
    stationCenterX: gridX,
    xfmrX: firstXfmr?.x ?? gridX,
    stationXfmrs: scene.transformers,
    meterX: pccMeter?.x ?? gridX,
    rowPeerGap: HANG_W,
    xfmrLabelSide: scene.transformers.length > 1 || pccMeter ? 'left' : 'right',
    busLeft: forest[0]?.x1 ?? MARGIN_X + 40,
    busRight: forest[0]?.x2 ?? MARGIN_X + UNIT_W - 40,
    linkStub: LINK_STUB,
    yGrid,
    yAfterGrid: yGrid + 50,
    yMainBreaker: mainBrk?.y ?? null,
    yBrkTop: mainBrk?.yTop ?? null,
    yBrkBottom: mainBrk?.yBottom ?? null,
    yBusHv,
    omitBusHv,
    yXfmr: firstXfmr?.y ?? yRoot + LINK_STUB,
    xfmrSpan: XFMR_SPAN,
    meterH: METER_H,
    yBusLv: yUnitTop,
    omitBusLv,
    yUnitTop,
    hasMainBreaker: !!mainBreakerNode,
    hasPccMeter: !!pccMeter,
    hasLoad: scene.loads.length > 0,
    load: firstLoad?.node || null,
    loadBus: firstLoad ? graph.byId.get(firstLoad.busId) || null : null,
    loadAttachSide: firstLoad
      ? (firstLoad.busId && firstLoad.busId === rootBus?.id ? 'hv' : 'lv')
      : null,
    loadX: firstLoad?.x ?? null,
    loadStub: LINK_STUB,
    loadSymbolH: LOAD_SYMBOL_H,
    loadLabel: firstLoad?.label || '负载',
    grid,
    mainBreaker: mainBreakerNode,
    pccMeter: pccMeter?.node || null,
    mainXfmr: firstXfmr?.node || null,
    busHv: rootBus,
    busLv: firstLvBus,
    busHvLabel: busLabel(rootBus, hvV),
    busLvLabel: busLabel(firstLvBus, lvV),
    xfmrLabel: firstXfmr
      ? firstXfmr.ratioLabel
      : `${fmtKv(hvV)}/${fmtKv(lvV)}`,
    meterLabel: pccMeter?.label || '并网点电表',
    units: unitLayouts,
    groups
  }
}
