/**
 * 由组态工程图推导 3D 场站布局。
 * 站侧结构、设备台数、电压/容量全部来自节点与连线，不写死主变台数或单元内部路数。
 */
import { buildTopologyMainLineLayout } from '../topology/topologyMainLineLayout.js'
import { pvArrayFieldSize } from './pvArrayLayout.js'

/** 单线图像素 → 3D 米 */
export const PX = 0.06
export const CABLE_Y = 0.55
export const LABEL_Y = 6.5

export function paramNum(node, key, fallback = null) {
  const v = Number(node?.parameters?.[key])
  return Number.isFinite(v) ? v : fallback
}

export function fmtVolt(v) {
  const n = Number(v)
  if (!Number.isFinite(n) || n <= 0) return '—'
  return n >= 1000
    ? `${(n / 1000).toFixed(n % 1000 === 0 ? 0 : 1)} kV`
    : `${n.toFixed(0)} V`
}

/**
 * 组态模板与 3D 模型对应。复合模板（emu / pv_unit）在布局中按内部设备展开，
 * 不发明台数； primitive 模板各有独立网格。
 */
export const TOPOLOGY_TEMPLATE_3D = {
  grid: 'primitive',
  ac_bus: 'primitive',
  ac_breaker: 'primitive',
  transformer: 'primitive',
  ac_meter: 'primitive',
  load: 'primitive',
  bms: 'primitive',
  dc_bus: 'primitive',
  emu: 'composite',
  pv_unit: 'composite'
}

function splitCount(n) {
  const total = Math.max(0, Math.round(Number(n) || 0))
  const a = Math.ceil(total / 2)
  return { a, b: total - a }
}

/**
 * 沿母线均匀排开 count 台设备；count=1 居中。
 * @param {number} cx
 * @param {number} count
 * @param {number} pitch
 */
export function slotXs(cx, count, pitch = 5.5) {
  const n = Math.max(0, count | 0)
  if (n <= 0) return []
  if (n === 1) return [cx]
  const span = (n - 1) * pitch
  const x0 = cx - span / 2
  const xs = []
  for (let i = 0; i < n; i++) xs.push(x0 + i * pitch)
  return xs
}

function fingerprintNode(n) {
  const p = n?.parameters || {}
  const keys = [
    'inverterCount', 'pcsCount', 'primaryVoltage', 'secondaryVoltage',
    'nominalVoltage', 'outputVoltage', 'unitXfPrimaryV', 'unitXfSecondaryV',
    'unitXfRatedKva', 'ratedPowerKva', 'stringCount', 'modulesPerString'
  ]
  const sig = keys.map(k => `${k}=${p[k] ?? ''}`).join(',')
  return `${n.id}:${n.templateId}:${Math.round(n.x || 0)}:${Math.round(n.y || 0)}:${sig}`
}

/**
 * 组态或运行时变化时重建 3D。
 * @param {object} snap
 */
export function stationKey(snap) {
  const topo = snap?.topology
  if (topo?.nodes?.length) {
    const nodes = [...topo.nodes].map(fingerprintNode).sort().join('|')
    const edges = [...(topo.edges || [])]
      .map(e => `${e.fromNodeId}>${e.toNodeId}`)
      .sort()
      .join(',')
    return `t:${nodes}#${edges}`
  }
  const ess = snap?.units?.length || 0
  const pv = snap?.pvUnits?.length || 0
  return `s:${ess}:${pv}`
}

function originOf(sld) {
  const xs = []
  const ys = []
  if (sld.gridX != null) xs.push(sld.gridX)
  for (const b of sld.buses || []) {
    xs.push(b.x1, b.x2)
    ys.push(b.y)
  }
  for (const t of sld.transformers || []) {
    xs.push(t.x)
    ys.push(t.y)
  }
  for (const u of sld.units || []) {
    xs.push(u.cx)
    ys.push(u.originY)
  }
  const x0 = xs.length ? Math.min(...xs) : 0
  const y0 = sld.yGrid ?? (ys.length ? Math.min(...ys) : 0)
  return { x0, y0 }
}

function toX(px, origin) {
  return (px - origin.x0) * PX
}

function toZ(py, origin) {
  return (py - origin.y0) * PX
}

function addItem(items, item) {
  items.push(item)
  return item
}

function addCable(cables, a, b, extra = {}) {
  cables.push({
    ax: a.x, ay: a.y ?? CABLE_Y, az: a.z,
    bx: b.x, by: b.y ?? CABLE_Y, bz: b.z,
    ...extra
  })
}

function xfBoxType(node) {
  const sec = paramNum(node, 'secondaryVoltage', 0) || 0
  const pri = paramNum(node, 'primaryVoltage', 0) || 0
  return sec > 0 && sec < 3000 && pri > 0 && pri < 80000
}

function xfScale(node) {
  const kva = paramNum(node, 'ratedPowerKva', 0) || paramNum(node, 'unitXfRatedKva', 0) || 0
  if (kva <= 0) return 1
  return Math.max(0.55, Math.min(1.7, Math.cbrt(kva / 10000)))
}

/** 组态参数优先；缺省不发明台数。 */
function countFromParam(node, key, liveCount = 0) {
  const n = paramNum(node, key, null)
  if (n != null && n >= 0) return Math.max(0, Math.round(n))
  return Math.max(0, liveCount | 0)
}

function expandEmu(unit, origin, items, cables, ctx) {
  const cx = toX(unit.cx, origin)
  const busCx = unit.busCx != null ? toX(unit.busCx, origin) : cx
  const zBus = toZ(unit.originY, origin)
  const zBr = toZ(unit.originY + unit.unitBrkMid, origin)
  const zXf = toZ(unit.originY + unit.unitXfmrTop + 8, origin)
  const z690 = toZ(unit.originY + unit.unitBus690Y, origin)
  const zPcs = toZ(unit.originY + unit.pcsTop, origin)
  const zBms = toZ(unit.originY + unit.bmsTop, origin)
  // 方阵区需在 z 方向避开所有单元（含储能 BMS/直流母线深度），记录单元底边；
  // 同时记录储能单元列的 x 范围，供方阵区在 x 方向避让
  if (ctx) {
    ctx.maxUnitBottomZ = Math.max(ctx.maxUnitBottomZ, toZ(unit.originY + unit.bottom, origin))
    const halfW = 6
    ctx.emuMinX = ctx.emuMinX == null ? cx - halfW : Math.min(ctx.emuMinX, cx - halfW)
    ctx.emuMaxX = ctx.emuMaxX == null ? cx + halfW : Math.max(ctx.emuMaxX, cx + halfW)
  }
  const node = unit.emu
  const livePcs = [unit.pcsA, unit.pcsB].filter(Boolean).length
  const pcsCount = countFromParam(node, 'pcsCount', livePcs)
  const bmsNodes = unit.bmsNodes || []
  const pcsXs = slotXs(cx, pcsCount, 5.5)
  const bmsCount = bmsNodes.length
  const bmsXs = slotXs(cx, bmsCount, 5.5)

  addItem(items, {
    key: `emu-title-${unit.index}`,
    templateId: 'label',
    kind: 'unit-title',
    x: cx - 3,
    z: zBus + 0.4,
    y: LABEL_Y,
    text: [unit.label],
    title: true,
    unitIndex: unit.unitSnap?.unitIndex ?? unit.index
  })

  addItem(items, {
    key: `emu-br-${unit.index}`,
    templateId: 'ac_breaker',
    kind: 'unit-breaker',
    x: cx,
    z: zBr,
    node,
    pickId: `unit-${unit.unitSnap?.unitIndex ?? unit.index}`,
    unitIndex: unit.unitSnap?.unitIndex ?? unit.index,
    label: '单元断',
    labelOffset: { x: 2.4, y: 3.4, z: 0 }
  })
  // 支路电缆：单元断路器 → 母线汇流点（星型接线，先南北到母线带再东西汇入，y 分层避免重合）
  addCable(cables, { x: cx, y: 0.5, z: zBr }, { x: busCx, y: 0.5, z: zBus }, {
    role: 'unit-drop',
    unitIndex: unit.unitSnap?.unitIndex ?? unit.index,
    static: true,
    midY: 0.35 + (unit.index % 8) * 0.05
  })

  const xfNode = {
    parameters: {
      primaryVoltage: paramNum(node, 'unitXfPrimaryV', paramNum(node, 'acVoltage')),
      secondaryVoltage: paramNum(node, 'unitXfSecondaryV'),
      ratedPowerKva: paramNum(node, 'pcsRatedPowerKw', 0) * Math.max(0, pcsCount)
    },
    label: node?.label
  }
  addItem(items, {
    key: `emu-xf-${unit.index}`,
    templateId: 'transformer',
    kind: 'unit-xf',
    x: cx,
    z: zXf,
    node: xfNode,
    boxType: true,
    scale: xfScale(xfNode),
    label: `单元变 ${unit.unitXfLabel || ''}`.trim(),
    labelOffset: { x: 2.8, y: 3.8, z: 0 },
    unitIndex: unit.unitSnap?.unitIndex ?? unit.index
  })
  addCable(cables, { x: cx, y: 0.5, z: zBr }, { x: cx, y: 0.5, z: zXf }, {
    role: 'unit-xf',
    unitIndex: unit.unitSnap?.unitIndex ?? unit.index,
    static: true
  })

  let busX = cx
  let busZ = z690
  if (!unit.omitBus690) {
    const lvV = paramNum(node, 'unitXfSecondaryV')
    const bus = addItem(items, {
      key: `emu-lv-${unit.index}`,
      templateId: 'ac_bus',
      kind: 'bus-node',
      x: cx,
      z: z690,
      y: CABLE_Y,
      node: {
        label: lvV != null ? fmtVolt(lvV) : '低压母线',
        parameters: { nominalVoltage: lvV }
      },
      radius: 0.22,
      busRole: 'unit-lv-bus',
      unitIndex: unit.unitSnap?.unitIndex ?? unit.index
    })
    busX = bus.x
    busZ = bus.z
    addCable(cables, { x: cx, y: 0.5, z: zXf }, { x: busX, y: 0.5, z: busZ }, {
      role: 'unit-690',
      unitIndex: unit.unitSnap?.unitIndex ?? unit.index,
      static: true
    })
  } else {
    busZ = zXf
    addCable(cables, { x: cx, y: 0.5, z: zXf }, { x: pcsXs[0] ?? cx, y: 0.5, z: zPcs }, {
      role: 'unit-690',
      unitIndex: unit.unitSnap?.unitIndex ?? unit.index,
      static: true
    })
  }

  const channels = [unit.pcsA, unit.pcsB].filter(Boolean)
  pcsXs.forEach((x, i) => {
    const ch = channels[i] || null
    const side = i === 0 ? 'A' : String.fromCharCode(65 + i)
    const unitIndex = unit.unitSnap?.unitIndex ?? unit.index
    addItem(items, {
      key: `pcs-${unitIndex}-${side}`,
      templateId: 'pcs',
      kind: 'pcs',
      x,
      z: zPcs,
      panelKey: `pcs-${unitIndex}-${side}`,
      panelType: 'pcs',
      unitIndex,
      side,
      channel: ch,
      labelOffset: { x: 0, y: 4.4, z: 0.3 }
    })
    if (!unit.omitBus690 || i > 0) {
      addCable(cables, { x: busX, y: 0.5, z: busZ }, { x, y: 0.5, z: zPcs }, {
        role: 'pcs-feed',
        unitIndex,
        side,
        static: true
      })
    }
  })

  bmsXs.forEach((x, i) => {
    const bmsNode = bmsNodes[i] || null
    const side = i === 0 ? 'A' : String.fromCharCode(65 + i)
    const unitIndex = unit.unitSnap?.unitIndex ?? unit.index
    const pcsX = pcsXs[Math.min(i, pcsXs.length - 1)] ?? x
    addItem(items, {
      key: `bms-${unitIndex}-${side}`,
      templateId: 'bms',
      kind: 'bms',
      x,
      z: zBms,
      node: bmsNode,
      panelKey: `bms-${unitIndex}-${side}`,
      panelType: 'bms',
      unitIndex,
      side,
      channel: channels[i] || null,
      labelOffset: { x: 0, y: 3.6, z: 0.3 }
    })
    addCable(cables, { x: pcsX, y: 0.5, z: zPcs }, { x, y: 0.5, z: zBms }, {
      role: 'dc-link',
      unitIndex,
      side,
      static: true
    })
  })

  if (unit.dcBus && !unit.omitDcBus && bmsCount > 1) {
    const zDc = toZ(unit.originY + (unit.dcBusY ?? 0), origin)
    const xs = bmsXs.length ? bmsXs : [cx]
    const x1 = Math.min(...xs) - 1.2
    const x2 = Math.max(...xs) + 1.2
    addItem(items, {
      key: `node-${unit.dcBus.id || `emu-dc-${unit.index}`}`,
      templateId: 'dc_bus',
      kind: 'dc-bus',
      x: (x1 + x2) / 2,
      z: zDc,
      y: CABLE_Y,
      x1,
      x2,
      // 与交流母线同一规则：半径随挂接规模自适应
      radius: Math.max(0.26, Math.min(0.5, Math.abs(x2 - x1) * 0.03)),
      node: unit.dcBus,
      voltage: paramNum(unit.dcBus, 'nominalVoltage'),
      label: unit.dcBus.label,
      labelOffset: { x: 1.2, y: 1.1, z: -0.8 }
    })
  }
}

function pvGroupsFromUnit(unit) {
  const inv = countFromParam(unit.pv, 'inverterCount', 0)
  const split = splitCount(inv)
  const groups = []
  if (split.a > 0) groups.push({ side: 'A', inverterCount: split.a, group: unit.groupA })
  if (split.b > 0) groups.push({ side: 'B', inverterCount: split.b, group: unit.groupB })
  return { inverterCount: inv, groups }
}

function expandPv(unit, origin, items, cables, pvSnap, ctx) {
  const cx = toX(unit.cx, origin)
  const busCx = unit.busCx != null ? toX(unit.busCx, origin) : cx
  const zBus = toZ(unit.originY, origin)
  const zBr = toZ(unit.originY + unit.unitBrkMid, origin)
  const zXf = toZ(unit.originY + unit.xfmrCardTop + 20, origin)
  const zInv = toZ(unit.originY + unit.arraySplitY, origin)
  const node = unit.pv
  const pvIndex = unit.pvIndex
  const live = (pvSnap || []).find(p => (p.pvIndex ?? -1) === pvIndex) || (pvSnap || [])[pvIndex] || null

  // 逆变器台数按组态 inverterCount（A/B 分组）；未配置时不发明台数
  const invTotal = countFromParam(node, 'inverterCount', 0)
  if (invTotal <= 0) return
  const split = splitCount(invTotal)
  const groups = [
    { side: 'A', count: Number(unit.groupA?.inverterCount) || split.a },
    { side: 'B', count: Number(unit.groupB?.inverterCount) || split.b }
  ].filter(g => g.count > 0)
  if (!groups.length) groups.push({ side: 'A', count: invTotal })

  const stringCount = countFromParam(node, 'stringCount', 0)
  const modulesPerString = countFromParam(node, 'modulesPerString', 0)
  if (ctx) {
    ctx.unitCxs.push(cx)
    ctx.stringCount = Math.max(ctx.stringCount, stringCount)
    ctx.modulesPerString = Math.max(ctx.modulesPerString, modulesPerString)
    ctx.maxInvZ = Math.max(ctx.maxInvZ, zInv)
    ctx.maxUnitBottomZ = Math.max(ctx.maxUnitBottomZ, toZ(unit.originY + unit.bottom, origin))
  }

  addItem(items, {
    key: `pv-${pvIndex}`,
    templateId: 'label',
    kind: 'pv-title',
    x: cx - 3,
    z: zBus + 0.4,
    y: LABEL_Y,
    text: [unit.label],
    title: true,
    pvIndex
  })

  addItem(items, {
    key: `pv-br-${pvIndex}`,
    templateId: 'ac_breaker',
    kind: 'pv-breaker',
    x: cx,
    z: zBr,
    node,
    pickId: `pv-br-${pvIndex}`,
    pvIndex,
    label: '单元断',
    labelOffset: { x: 2.4, y: 3.4, z: 0 }
  })
  // 支路电缆：单元断路器 → 母线汇流点（星型接线，y 分层避免重合）
  addCable(cables, { x: cx, y: 0.5, z: zBr }, { x: busCx, y: 0.5, z: zBus }, {
    role: 'pv-drop',
    pvIndex,
    static: true,
    midY: 0.35 + ((unit.index ?? 0) % 8) * 0.05
  })

  const xfNode = {
    parameters: {
      primaryVoltage: paramNum(node, 'unitXfPrimaryV', paramNum(node, 'acVoltage')),
      secondaryVoltage: paramNum(node, 'unitXfSecondaryV'),
      ratedPowerKva: paramNum(node, 'unitXfRatedKva', 0)
        || (paramNum(node, 'inverterRatedPowerKw', 0) * invTotal)
    },
    label: node?.label
  }
  addItem(items, {
    key: `pvxf-${pvIndex}`,
    templateId: 'transformer',
    kind: 'pv-xf',
    x: cx,
    z: zXf,
    node: xfNode,
    boxType: true,
    scale: xfScale(xfNode),
    panelKey: `pvxf-${pvIndex}`,
    panelType: 'pv',
    pvIndex,
    pvUnit: live,
    label: `箱变 ${unit.unitXfLabel || ''}`.trim(),
    labelOffset: { x: 2.8, y: 3.8, z: 0 }
  })
  addCable(cables, { x: cx, y: 0.5, z: zBr }, { x: cx, y: 0.5, z: zXf }, {
    role: 'pv-xf',
    pvIndex,
    static: true
  })

  // —— 逆变器排：A/B 分居单元中心两侧，紧凑排布（不随方阵宽度拉开，避免跨单元重叠）——
  const invOffset = 5.5
  groups.forEach((g, i) => {
    const invX = cx + (i === 0 ? -invOffset : invOffset)
    const arrKey = `pvarr-${pvIndex}-${g.side}`
    addItem(items, {
      key: `pvinv-${pvIndex}-${g.side}`,
      templateId: 'pv_inverter',
      kind: 'pv-inverter',
      x: invX,
      z: zInv,
      panelKey: arrKey,
      panelType: 'pv-array',
      pvIndex,
      side: g.side,
      inverterCount: g.count,
      pvUnit: live,
      pvArray: g.side === 'B' ? live?.arrayB : live?.arrayA,
      labelOffset: { x: 0, y: 4.4, z: 0.3 }
    })
    // —— 主干：箱变出线口（A/B 错开）→ 逆变器排前方汇流点，贴地正交一条线 ——
    const mainX = cx + i * 0.15
    addCable(cables, { x: mainX, y: 0.5, z: zXf + 1.15 }, { x: invX, y: 0.5, z: zInv - 0.9 }, {
      role: 'pv-inv-main',
      pvIndex,
      side: g.side,
      static: true,
      radius: 0.07
    })
    // —— 分支：汇流点 → 每台逆变器柜面向箱变侧（-z），短段可分辨 ——
    for (let k = 0; k < g.count; k++) {
      const off = inverterCabinetOffset(k, g.count)
      addCable(cables, { x: invX, y: 0.5, z: zInv - 0.9 }, { x: invX + off.x, y: 0.5, z: zInv + off.z - 0.35 }, {
        role: 'pv-inv-branch',
        pvIndex,
        side: g.side,
        static: true,
        radius: 0.045
      })
      // 每台逆变器请求一块方阵（1:1），方阵位置由 placePvArrays 统一排布
      if (ctx) {
        ctx.requests.push({
          pvIndex,
          side: g.side,
          invIdx: k,
          invCount: g.count,
          invX,
          zInv,
          pvUnit: live,
          pvArray: g.side === 'B' ? live?.arrayB : live?.arrayA
        })
      }
    }
  })
}

/**
 * 逆变器柜在排内的位置偏移（与 createPvInverterRow 的排布公式一致），
 * 用于把每根主 dc 电缆接到排内对应柜位，避免线束在排中心重合。
 */
function inverterCabinetOffset(invIdx, count) {
  const n = Math.max(1, count | 0)
  const cols = Math.min(n, 8)
  const rows = Math.ceil(n / cols)
  const col = invIdx % cols
  const row = Math.floor(invIdx / cols)
  const pitch = n > 8 ? 0.48 : 0.72
  const rowPitch = 0.7
  return {
    x: (col - (cols - 1) / 2) * pitch,
    z: (row - (rows - 1) / 2) * rowPitch
  }
}

/**
 * 光伏方阵区：所有单元每台逆变器一块方阵（1:1），集中排在设备区后方网格，
 * 场地尺寸随方阵数量/大小自适应。每块方阵的组串出线（每串一根单独出线 → 汇流母线）
 * 由 buildStation 生成；此处生成方阵 item 与“汇流母线 → 逆变器柜”静态 dc 电缆。
 */
function placePvArrays(ctx, items, cables) {
  const reqs = ctx.requests || []
  if (!reqs.length) return
  const { fieldW, fieldD } = pvArrayFieldSize(ctx.stringCount, ctx.modulesPerString)
  const perRow = 8
  const gapX = 3
  const gapZ = 3
  const meanCx = ctx.unitCxs.length ? ctx.unitCxs.reduce((a, b) => a + b, 0) / ctx.unitCxs.length : 0
  let startX = meanCx - ((perRow - 1) * (fieldW + gapX)) / 2
  // 方阵区与储能单元列在 x 方向重叠时，整体平移到储能单元靠光伏一侧，
  // 避免方阵 dc 出线穿越储能单元 BMS/直流母线区域
  if (ctx.emuMinX != null && ctx.emuMaxX != null) {
    const span = (perRow - 1) * (fieldW + gapX)
    const fieldLeft = startX - fieldW / 2
    const fieldRight = startX + span + fieldW / 2
    if (fieldLeft < ctx.emuMaxX && fieldRight > ctx.emuMinX) {
      const edgeGap = 6
      if (meanCx >= (ctx.emuMinX + ctx.emuMaxX) / 2) startX = ctx.emuMaxX + edgeGap + fieldW / 2
      else startX = ctx.emuMinX - edgeGap - fieldW / 2 - span
    }
  }
  // 方阵区排在所有单元设备（含储能 BMS/直流母线）后方，避免与储能单元重叠
  const startZ = Math.max(ctx.maxInvZ, ctx.maxUnitBottomZ) + fieldD / 2 + 10

  let x = startX
  let z = startZ
  let col = 0
  for (const req of reqs) {
    // key 每块方阵唯一；panelKey 同组共享一个面板（pvarr-{pvIndex}-{side}）
    const key = `pvarr-${req.pvIndex}-${req.side}-${req.invIdx ?? 0}`
    const panelKey = `pvarr-${req.pvIndex}-${req.side}`
    addItem(items, {
      key,
      templateId: 'pv_array',
      kind: 'pv-array',
      x,
      z,
      // 真实占地：供 finalize 推算场景边界，场地环境随方阵区自适应
      footprint: { w: fieldW, d: fieldD },
      panelKey,
      panelType: 'pv-array',
      pvIndex: req.pvIndex,
      side: req.side,
      inverterCount: 1,
      stringCount: ctx.stringCount,
      modulesPerString: ctx.modulesPerString,
      pvUnit: req.pvUnit,
      pvArray: req.pvArray,
      labelOffset: { x: req.side === 'A' ? -2.2 : 2.2, y: 2.6, z: 0 }
    })
    // 静态 dc 电缆：方阵汇流竖排底部（贴地）→ 逆变器柜朝光伏侧（+z）底部进线，
    // 全程贴地正交走线，终点分散到各柜位
    const off = inverterCabinetOffset(req.invIdx ?? 0, req.invCount ?? 1)
    addCable(cables, { x, y: 0.35, z: z - fieldD / 2 - 0.35 }, { x: req.invX + off.x, y: 0.5, z: req.zInv + off.z + 0.4 }, {
      role: 'pv-dc',
      pvIndex: req.pvIndex,
      side: req.side,
      static: true
    })
    col++
    if (col >= perRow) {
      col = 0
      x = startX
      z += fieldD + gapZ
    } else {
      x += fieldW + gapX
    }
  }
}

function makePvCtx() {
  return {
    requests: [],
    unitCxs: [],
    maxInvZ: 0,
    maxUnitBottomZ: 0,
    emuMinX: null,
    emuMaxX: null,
    stringCount: 0,
    modulesPerString: 0
  }
}

function collectSldNodeIds(sld) {
  const ids = new Set()
  const add = x => { if (x) ids.add(x) }
  add(sld.grid?.id)
  for (const b of sld.stemBreakers || []) add(b.id)
  for (const t of sld.transformers || []) add(t.id)
  for (const b of sld.buses || []) add(b.id)
  for (const m of sld.meters || []) add(m.id)
  for (const l of sld.loads || []) add(l.id)
  for (const u of sld.units || []) {
    add(u.emu?.id)
    add(u.pv?.id)
    for (const b of u.bmsNodes || []) add(b.id)
    add(u.dcBus?.id)
  }
  return ids
}

/**
 * 兜底绘制：单线图布局未精确定位的基础模板节点（独立 BMS/DC 母线、母线支路断路器、
 * 孤立设备等），只要用户画了且模板有效，就按画布坐标生成基础 3D 模型与连线。
 */
function drawFallbackNodesAndCables(topology, origin, items, cables, placedIds) {
  const placed = new Set(placedIds)
  const byId = new Map((topology?.nodes || []).map(n => [n.id, n]))

  for (const n of topology?.nodes || []) {
    if (placed.has(n.id)) continue
    if (TOPOLOGY_TEMPLATE_3D[n.templateId] !== 'primitive') continue
    const x = toX(n.x, origin)
    const z = toZ(n.y, origin)
    const item = {
      key: `node-${n.id}`,
      templateId: n.templateId,
      kind: n.templateId,
      x,
      z,
      node: n,
      label: n.label,
      labelOffset: { x: 1.6, y: 3.2, z: 0 }
    }
    if (n.templateId === 'ac_breaker') {
      item.pickId = `brk-${n.id}`
    } else if (n.templateId === 'ac_bus') {
      item.kind = 'bus-node'
      item.y = CABLE_Y
      item.radius = 0.24
    } else if (n.templateId === 'dc_bus') {
      item.x1 = x - 3
      item.x2 = x + 3
      item.y = CABLE_Y
      item.radius = 0.24
    }
    addItem(items, item)
  }

  // 两个端点都未由 sld 精确定位时，用画布坐标补一条贴地正交静态连线
  for (const e of topology?.edges || []) {
    if (placed.has(e.fromNodeId) || placed.has(e.toNodeId)) continue
    const a = byId.get(e.fromNodeId)
    const b = byId.get(e.toNodeId)
    if (!a || !b) continue
    if (TOPOLOGY_TEMPLATE_3D[a.templateId] !== 'primitive' || TOPOLOGY_TEMPLATE_3D[b.templateId] !== 'primitive') continue
    addCable(cables,
      { x: toX(a.x, origin), y: 0.5, z: toZ(a.y, origin) },
      { x: toX(b.x, origin), y: 0.5, z: toZ(b.y, origin) },
      { role: 'sld-wire', static: true }
    )
  }
}

function fromTopology(topology, unitsSnap, pvSnap) {
  const sld = buildTopologyMainLineLayout(topology, unitsSnap || [])
  const origin = originOf(sld)
  const items = []
  const cables = []
  const ctx = makePvCtx()

  if (sld.grid) {
    const x = toX(sld.gridX, origin)
    const z = toZ(sld.yGrid, origin)
    addItem(items, {
      key: `node-${sld.grid.id}`,
      templateId: 'grid',
      kind: 'grid',
      x,
      z,
      node: sld.grid,
      voltage: paramNum(sld.grid, 'outputVoltage'),
      labelOffset: { x: 3.8, y: LABEL_Y, z: 0 }
    })
  }

  for (const br of sld.stemBreakers || []) {
    const x = toX(br.x ?? sld.gridX, origin)
    const z = toZ(br.y, origin)
    addItem(items, {
      key: `node-${br.id}`,
      templateId: 'ac_breaker',
      kind: br.isMain ? 'main-breaker' : 'stem-breaker',
      x,
      z,
      node: br.node,
      pickId: br.isMain ? 'main' : `brk-${br.id}`,
      isMain: !!br.isMain,
      label: br.label,
      labelOffset: { x: 2.4, y: 3.6, z: 0 }
    })
  }

  for (const xf of sld.transformers || []) {
    addItem(items, {
      key: `node-${xf.id}`,
      templateId: 'transformer',
      kind: 'station-xf',
      x: toX(xf.x, origin),
      z: toZ(xf.y + xf.span / 2, origin),
      node: xf.node,
      boxType: xfBoxType(xf.node),
      scale: xfScale(xf.node),
      label: xf.label,
      ratioLabel: xf.ratioLabel,
      kvaLabel: xf.kvaLabel,
      labelOffset: { x: 3.6, y: 5.0, z: 0 }
    })
  }

  for (const bus of sld.buses || []) {
    const x1 = toX(bus.x1, origin)
    const x2 = toX(bus.x2, origin)
    // 汇流点画在设备汇聚中心（frame.cx，不含挂件外扩），与支路电缆终点一致
    const cx = bus.cx != null ? toX(bus.cx, origin) : (x1 + x2) / 2
    const z = toZ(bus.y, origin)
    // 统一规则：母线绘制为一个汇流点（星型接线），不再画长条母线管；
    // 半径随母线挂接规模（长度）自适应，电压用于配色
    const radius = Math.max(0.26, Math.min(0.5, Math.abs(x2 - x1) * 0.03))
    addItem(items, {
      key: `node-${bus.id}`,
      templateId: 'ac_bus',
      kind: 'bus-node',
      x: cx,
      z,
      y: CABLE_Y,
      node: bus.node,
      voltage: bus.voltage,
      label: bus.label,
      radius,
      busRole: 'ac-bus',
      labelOffset: { x: 1.2, y: 1.1, z: -0.8 }
    })
  }

  for (const m of sld.meters || []) {
    addItem(items, {
      key: `node-${m.id}`,
      templateId: 'ac_meter',
      kind: 'meter',
      x: toX(m.x, origin),
      z: toZ(m.y, origin),
      node: m.node,
      isPcc: !!m.isPcc,
      label: m.label,
      labelOffset: { x: 1.8, y: 2.4, z: 0 }
    })
  }

  for (const l of sld.loads || []) {
    addItem(items, {
      key: `node-${l.id}`,
      templateId: 'load',
      kind: 'load',
      x: toX(l.x, origin),
      z: toZ(l.busY + (l.stub || 0) + (l.symbolH || 0) / 2, origin),
      node: l.node,
      label: l.label,
      labelOffset: { x: 1.6, y: 2.2, z: 0 }
    })
  }

  let wireIdx = 0
  for (const w of sld.wires || []) {
    // 母线支线（起点在母线层、x 不在母线中心）：重定向到母线汇流点（星型接线），y 分层避免重合
    const busAtY = (sld.buses || []).find(b => Math.abs(b.y - w.y1) < 0.5)
    if (busAtY && Math.abs(w.x1 - (busAtY.cx ?? busAtY.x1)) > 1) {
      const busCx = toX(busAtY.cx ?? (busAtY.x1 + busAtY.x2) / 2, origin)
      const busZ = toZ(w.y1, origin)
      addCable(
        cables,
        { x: toX(w.x2, origin), y: 0.5, z: toZ(w.y2, origin) },
        { x: busCx, y: 0.5, z: busZ },
        { role: 'sld-wire', static: true, midY: 0.35 + (wireIdx++ % 8) * 0.05 }
      )
      continue
    }
    addCable(
      cables,
      { x: toX(w.x1, origin), y: 0.5, z: toZ(w.y1, origin) },
      { x: toX(w.x2, origin), y: 0.5, z: toZ(w.y2, origin) },
      { role: 'sld-wire', static: true }
    )
  }

  for (const unit of sld.units || []) {
    if (unit.kind === 'pv') expandPv(unit, origin, items, cables, pvSnap, ctx)
    else expandEmu(unit, origin, items, cables, ctx)
  }

  // 方阵区统一排布（每台逆变器一块方阵）
  placePvArrays(ctx, items, cables)

  // 兜底绘制：sld 未精确定位但用户已画的基础模板节点与连线
  drawFallbackNodesAndCables(topology, origin, items, cables, collectSldNodeIds(sld))

  return finalize(items, cables)
}

function fromSnapFallback(snap) {
  const items = []
  const cables = []
  const ctx = makePvCtx()
  const units = snap?.units || []
  const pvs = snap?.pvUnits || []
  const n = units.length + pvs.length
  const spacing = n >= 16 ? 16 : n >= 10 ? 18 : 22
  const xs = []
  for (let i = 0; i < n; i++) xs.push(i * spacing)
  const mainX = xs[0] || 0

  const gridV = snap?.gridNominalLineVoltageV
  addItem(items, {
    key: 'grid',
    templateId: 'grid',
    kind: 'grid',
    x: mainX,
    z: -22,
    node: { label: '电网', parameters: { outputVoltage: gridV } },
    voltage: gridV,
    labelOffset: { x: 3.8, y: LABEL_Y, z: 0 }
  })
  addItem(items, {
    key: 'main-breaker',
    templateId: 'ac_breaker',
    kind: 'main-breaker',
    x: mainX,
    z: -14,
    pickId: 'main',
    isMain: true,
    node: { label: snap?.mainBreakerLabel || '主断路器' },
    label: snap?.mainBreakerLabel || '主断',
    labelOffset: { x: 2.4, y: 3.6, z: 0 }
  })
  if (snap?.mainTransformerSecondary || snap?.mainTransformerPrimary) {
    addItem(items, {
      key: 'main-xf',
      templateId: 'transformer',
      kind: 'station-xf',
      x: mainX,
      z: -7,
      node: {
        label: '主变',
        parameters: {
          primaryVoltage: snap?.gridNominalLineVoltageV,
          secondaryVoltage: snap?.stationBus35LineVoltageV
        }
      },
      boxType: false,
      scale: 1.2,
      labelOffset: { x: 3.6, y: 5.0, z: 0 }
    })
  }
  if (n > 1) {
    addItem(items, {
      key: 'bus-lv',
      templateId: 'ac_bus',
      kind: 'bus-node',
      x: mainX,
      z: 0,
      y: CABLE_Y,
      node: { label: '母线', parameters: { nominalVoltage: snap?.stationBus35LineVoltageV } },
      voltage: snap?.stationBus35LineVoltageV,
      radius: 0.32,
      busRole: 'ac-bus',
      labelOffset: { x: 1.2, y: 1.1, z: -0.8 }
    })
  }
  addCable(cables, { x: mainX, y: 0.5, z: -21.5 }, { x: mainX, y: 0.5, z: -14 }, { role: 'grid-main', static: true })
  if (snap?.mainTransformerSecondary || snap?.mainTransformerPrimary) {
    addCable(cables, { x: mainX, y: 0.5, z: -14 }, { x: mainX, y: 0.5, z: -7 }, { role: 'main-xf', static: true })
    addCable(cables, { x: mainX, y: 0.5, z: -5.6 }, { x: mainX, y: 0.5, z: 0 }, { role: 'xf-bus35', static: true })
  } else {
    addCable(cables, { x: mainX, y: 0.5, z: -14 }, { x: mainX, y: 0.5, z: 0 }, { role: 'sld-wire', static: true })
  }

  units.forEach((u, i) => {
    const fake = {
      index: i,
      kind: 'emu',
      cx: (xs[i] || 0) / PX,
      busCx: (xs[0] || 0) / PX,
      originY: 0,
      unitBrkMid: 4 / PX,
      unitXfmrTop: 9 / PX,
      unitBus690Y: 13 / PX,
      pcsTop: 18 / PX,
      bmsTop: 26 / PX,
      drawPcsSlots: [u.channelA, u.channelB].filter(Boolean).length,
      pcsA: u.channelA,
      pcsB: u.channelB,
      bmsNodes: [u.channelA, u.channelB].filter(Boolean).map((_, k) => ({ id: `ch${k}` })),
      dcParallel: [u.channelA, u.channelB].filter(Boolean).length > 1,
      omitBus690: [u.channelA, u.channelB].filter(Boolean).length <= 1,
      emu: { parameters: {}, label: `UNIT ${u.unitNumber ?? i + 1}` },
      unitSnap: u,
      unitXfLabel: u.unitTransformerLine || '',
      label: `UNIT ${u.unitNumber ?? i + 1}`
    }
    expandEmu(fake, { x0: 0, y0: 0 }, items, cables, ctx)
  })
  pvs.forEach((pv, i) => {
    const ux = xs[units.length + i] || 0
    const inv = Math.max(0, Number(pv.gridConnectedDeviceCount) || 0)
    const fakePv = {
      index: i,
      kind: 'pv',
      cx: ux / PX,
      busCx: (xs[0] || 0) / PX,
      originY: 0,
      unitBrkMid: 4 / PX,
      xfmrCardTop: 9 / PX,
      arraySplitY: 18 / PX,
      bmsTop: 26 / PX,
      pvIndex: pv.pvIndex ?? i,
      pv: {
        label: `光伏单元 ${pv.pvNumber ?? i + 1}`,
        parameters: { inverterCount: inv || undefined }
      },
      inverterCount: inv,
      groupA: {},
      groupB: {},
      unitXfLabel: '',
      label: `光伏单元 ${pv.pvNumber ?? i + 1}`
    }
    expandPv(fakePv, { x0: 0, y0: 0 }, items, cables, pvs, ctx)
  })

  // 方阵区统一排布（每台逆变器一块方阵）
  placePvArrays(ctx, items, cables)

  return finalize(items, cables)
}

function finalize(items, cables) {
  let minX = 0
  let maxX = 8
  let minZ = -8
  let maxZ = 8
  for (const it of items) {
    // 用设备真实占地推算边界（方阵按 footprint 半宽/半深，其余按固定 4 米边距），
    // 保证场地环境、地面、相机随设备数量与大小自适应。
    const f = it.footprint
    const hw = f && f.w > 0 ? f.w / 2 : 4
    const hd = f && f.d > 0 ? f.d / 2 : 4
    minX = Math.min(minX, it.x - hw)
    maxX = Math.max(maxX, it.x + hw)
    minZ = Math.min(minZ, it.z - hd)
    maxZ = Math.max(maxZ, it.z + hd)
  }
  return {
    items,
    cables,
    bounds: { minX, maxX, minZ, maxZ, busStartX: minX, busEndX: maxX }
  }
}

/**
 * @param {object} snap MainLineViewModel
 */
export function buildStation3dLayout(snap) {
  const topology = snap?.topology
  if (topology?.nodes?.length) {
    return fromTopology(topology, snap.units || [], snap.pvUnits || [])
  }
  return fromSnapFallback(snap || {})
}
