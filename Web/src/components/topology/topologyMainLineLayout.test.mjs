import { describe, it } from 'node:test'
import assert from 'node:assert/strict'
import { buildTopologyMainLineLayout } from './topologyMainLineLayout.js'

function node(id, templateId, label, x, parameters = {}) {
  return { id, templateId, label, x, y: 0, parameters }
}

function edge(fromNodeId, toNodeId) {
  return { fromNodeId, toNodeId, fromPortId: 'ac_a', toPortId: 'a' }
}

describe('buildTopologyMainLineLayout pv units', () => {
  it('draws a pv_unit feeder as one box transformer plus two arrays, without dashed frame', () => {
    const topology = {
      nodes: [
        node('grid', 'grid', '电网', 400, { outputVoltage: 220000 }),
        node('bus', 'ac_bus', '35kV', 400, { nominalVoltage: 35000 }),
        node('pv1', 'pv_unit', '光伏单元-1', 200, {
          inverterCount: 16,
          inverterRatedPowerKw: 320,
          unitXfPrimaryV: 35000,
          unitXfSecondaryV: 690,
          unitXfRatedKva: 5120,
          modulesPerString: 30,
          stringCount: 16,
          moduleModel: 'TSM-NEG21C.20Q'
        })
      ],
      edges: [edge('pv1', 'bus')]
    }

    const layout = buildTopologyMainLineLayout(topology, [{ unitIndex: 0, unitNumber: 1 }])
    assert.equal(layout.units.length, 1)
    const u = layout.units[0]
    assert.equal(u.kind, 'pv')
    assert.equal(u.label, '光伏单元-1')
    assert.equal(u.pvIndex, 0)
    assert.equal(u.modulesPerString, 30)
    assert.equal(u.stringCount, 16)
    assert.equal(u.inverterCount, 16)
    assert.equal(u.groupA.modulesPerString, 30)
    assert.equal(u.groupA.stringCount, 16)
    assert.equal(u.groupA.inverterCount, 8)
    assert.equal(u.groupB.modulesPerString, 30)
    assert.equal(u.groupB.stringCount, 16)
    assert.equal(u.groupB.inverterCount, 8)
    assert.equal(u.groupA.totalRatedKw, 2560)
    assert.equal(u.groupB.totalRatedKw, 2560)
    assert.equal(u.totalRatedKw, 5120)
    assert.equal(u.unitXfLabel, '35kV/690V')
    assert.equal(layout.groups.length, 0, '不再画任何外框虚线')
    assert.ok(u.xfmrCardTop >= u.unitBrkBottom)
    assert.ok(u.bmsTop >= u.xfmrCardTop + u.xfmrCardH, '两路光伏方阵在箱变模块下方')
    assert.ok(u.arraySplitY > u.xfmrCardTop + u.xfmrCardH)
    assert.ok(u.arraySplitY < u.bmsTop)
    assert.ok(u.bmsH <= 130 && u.bmsH >= 112, '光伏方阵框刚好容纳内容')
    assert.ok(u.xfmrCardH <= 172 && u.xfmrCardH >= 148, '箱变框刚好容纳内容')
  })

  it('uses topology inverterCount for array split and xfmr grid-connect layout', () => {
    const topology = {
      nodes: [
        node('pv1', 'pv_unit', '光伏单元-1', 200, {
          inverterCount: 20,
          inverterRatedPowerKw: 320,
          modulesPerString: 30,
          stringCount: 16
        })
      ],
      edges: []
    }
    const layout = buildTopologyMainLineLayout(topology, [{ unitIndex: 0, unitNumber: 1 }])
    const u = layout.units[0]
    assert.equal(u.modulesPerString, 30)
    assert.equal(u.stringCount, 16)
    assert.equal(u.inverterCount, 20)
    assert.equal(u.groupA.inverterCount, 10)
    assert.equal(u.groupB.inverterCount, 10)
  })

  it('places emu and pv left-to-right and carries device nodes on emu units', () => {
    // 新模型：储能支路由 PCS 物理拓扑展开，单元携带设备节点；2D 不绑定运行时快照（绑定在卡片/3D 层按编号完成）
    const topology = {
      nodes: [
        node('emu1', 'emu', 'EMU-1', 400),
        node('dc1', 'dc_bus', '直流母线', 420, { nominalVoltage: 800 }),
        node('pcs1', 'pcs', 'PCS-1', 380, { emuId: 'emu1' }),
        node('pcs2', 'pcs', 'PCS-2', 460, { emuId: 'emu1' }),
        node('pv1', 'pv_unit', '光伏单元-1', 100, { inverterCount: 16, inverterRatedPowerKw: 320 })
      ],
      edges: [edge('pcs1', 'dc1'), edge('pcs2', 'dc1')]
    }
    const units = [{ unitIndex: 0, unitNumber: 1, channels: [{ pcsNumber: 1 }] }]
    const layout = buildTopologyMainLineLayout(topology, units)

    assert.equal(layout.units.length, 2)
    assert.equal(layout.units[0].kind, 'pv')
    assert.equal(layout.units[1].kind, 'emu')
    assert.equal(layout.units[1].pcsNodes.length, 2)
    assert.deepEqual(layout.units[1].pcsNums, [1, 2], 'PCS 全局编号与卡片 num 一致')
    assert.equal(layout.units[1].emu?.id, 'emu1')
    assert.equal(layout.omitBusLv, false)
  })

  it('orders emu feeders by pcs canvas position, independent of emu node order', () => {
    // emu1 节点在左但其 PCS 画在右侧；绘制序按 PCS 画布坐标，emu 字段随支路反查携带
    const topology = {
      nodes: [
        node('emu1', 'emu', 'EMU-1', 100),
        node('emu2', 'emu', 'EMU-2', 700),
        node('pcs1', 'pcs', 'PCS-1', 760, { emuId: 'emu1' }),
        node('pcs2', 'pcs', 'PCS-2', 160, { emuId: 'emu2' })
      ],
      edges: []
    }
    const layout = buildTopologyMainLineLayout(topology, [])

    assert.equal(layout.units.length, 2)
    // 绘制序：pcs2（x=160）在左，其归属 emu2 随支路携带；全局编号按 (Y,X) 秩：左起 1、2
    assert.equal(layout.units[0].pcsNodes[0].id, 'pcs2')
    assert.equal(layout.units[0].emu?.id, 'emu2')
    assert.deepEqual(layout.units[0].pcsNums, [1])
    assert.equal(layout.units[1].pcsNodes[0].id, 'pcs1')
    assert.equal(layout.units[1].emu?.id, 'emu1')
    assert.deepEqual(layout.units[1].pcsNums, [2])
  })

  it('omits lv bus when only one pv feeder hangs below', () => {
    const topology = {
      nodes: [
        node('bus', 'ac_bus', '35kV', 0, { nominalVoltage: 35000 }),
        node('pv1', 'pv_unit', '光伏单元-1', 0, { inverterCount: 16, inverterRatedPowerKw: 320 })
      ],
      edges: []
    }
    const layout = buildTopologyMainLineLayout(topology, [])
    assert.equal(layout.omitBusLv, true)
  })

  it('draws both station transformers and keeps pv on their lv buses', () => {
    const topology = {
      nodes: [
        node('grid', 'grid', '电网', 400, { outputVoltage: 220000 }),
        node('hv', 'ac_bus', '220kV', 400, { nominalVoltage: 220000 }),
        node('xf1', 'transformer', '主变1', 200, { primaryVoltage: 220000, secondaryVoltage: 35000, ratedPowerKva: 31500 }),
        node('xf2', 'transformer', '主变2', 800, { primaryVoltage: 220000, secondaryVoltage: 35000, ratedPowerKva: 31500 }),
        node('lv1', 'ac_bus', '35kV-1', 200, { nominalVoltage: 35000 }),
        node('lv2', 'ac_bus', '35kV-2', 800, { nominalVoltage: 35000 }),
        node('pv1', 'pv_unit', 'PV-1', 120, { inverterCount: 16, inverterRatedPowerKw: 320 }),
        node('pv2', 'pv_unit', 'PV-2', 280, { inverterCount: 16, inverterRatedPowerKw: 320 }),
        node('pv3', 'pv_unit', 'PV-3', 720, { inverterCount: 16, inverterRatedPowerKw: 320 }),
        node('pv4', 'pv_unit', 'PV-4', 880, { inverterCount: 16, inverterRatedPowerKw: 320 })
      ],
      edges: [
        edge('hv', 'xf1'),
        edge('hv', 'xf2'),
        edge('xf1', 'lv1'),
        edge('xf2', 'lv2'),
        edge('lv1', 'pv1'),
        edge('lv1', 'pv2'),
        edge('lv2', 'pv3'),
        edge('lv2', 'pv4')
      ]
    }
    const layout = buildTopologyMainLineLayout(topology, [])
    assert.equal(layout.stationXfmrs.length, 2)
    assert.equal(layout.units.length, 4)
    assert.equal(layout.units[0].label, 'PV-1')
    assert.equal(layout.units[2].label, 'PV-3')
    assert.ok(layout.stationXfmrs[0].x < layout.stationXfmrs[1].x)
    assert.ok(layout.units[1].cx < layout.stationXfmrs[1].x)
    assert.ok(layout.units[2].cx > layout.stationXfmrs[0].x)
    assert.ok(layout.stationXfmrs[0].busRight < layout.stationXfmrs[1].busLeft)
    assert.equal(layout.stationXfmrs[0].omitBusLv, false)
    assert.equal(layout.omitBusHv, false)
    assert.equal(layout.units[0].xfmrId, 'xf1')
    assert.equal(layout.units[2].xfmrId, 'xf2')
  })
})

describe('buildTopologyMainLineLayout follows topology graph', () => {
  it('does not invent a station transformer when the project has none', () => {
    const topology = {
      nodes: [
        node('grid', 'grid', '电网', 0, { outputVoltage: 35000 }),
        node('bus', 'ac_bus', '35kV', 0, { nominalVoltage: 35000 }),
        node('pv1', 'pv_unit', 'PV-1', 0, { inverterCount: 16, inverterRatedPowerKw: 320 })
      ],
      edges: [edge('grid', 'bus'), edge('bus', 'pv1')]
    }
    const layout = buildTopologyMainLineLayout(topology, [])
    assert.equal(layout.transformers.length, 0)
    assert.equal(layout.stationXfmrs.length, 0)
    assert.equal(layout.units.length, 1)
    assert.equal(layout.units[0].kind, 'pv')
  })

  it('draws every transformer that hangs off the same high-voltage bus', () => {
    const topology = {
      nodes: [
        node('hv', 'ac_bus', '220kV', 400, { nominalVoltage: 220000 }),
        node('xf1', 'transformer', 'T1', 100, { primaryVoltage: 220000, secondaryVoltage: 35000 }),
        node('xf2', 'transformer', 'T2', 400, { primaryVoltage: 220000, secondaryVoltage: 35000 }),
        node('xf3', 'transformer', 'T3', 700, { primaryVoltage: 220000, secondaryVoltage: 35000 }),
        node('lv1', 'ac_bus', '35-1', 100, { nominalVoltage: 35000 }),
        node('lv2', 'ac_bus', '35-2', 400, { nominalVoltage: 35000 }),
        node('lv3', 'ac_bus', '35-3', 700, { nominalVoltage: 35000 }),
        node('pv1', 'pv_unit', 'PV-1', 100, { inverterCount: 8 }),
        node('pv2', 'pv_unit', 'PV-2', 400, { inverterCount: 8 }),
        node('pv3', 'pv_unit', 'PV-3', 700, { inverterCount: 8 })
      ],
      edges: [
        edge('hv', 'xf1'), edge('xf1', 'lv1'), edge('lv1', 'pv1'),
        edge('hv', 'xf2'), edge('xf2', 'lv2'), edge('lv2', 'pv2'),
        edge('hv', 'xf3'), edge('xf3', 'lv3'), edge('lv3', 'pv3')
      ]
    }
    const layout = buildTopologyMainLineLayout(topology, [])
    assert.equal(layout.transformers.length, 3)
    assert.deepEqual(layout.transformers.map(t => t.label), ['T1', 'T2', 'T3'])
    assert.equal(layout.units.length, 3)
    assert.ok(layout.transformers[0].x < layout.transformers[1].x)
    assert.ok(layout.transformers[1].x < layout.transformers[2].x)
  })

  it('stacks cascaded transformers by voltage instead of flattening to one row', () => {
    const topology = {
      nodes: [
        node('hv', 'ac_bus', '220kV', 0, { nominalVoltage: 220000 }),
        node('xf1', 'transformer', '220/110', 0, { primaryVoltage: 220000, secondaryVoltage: 110000 }),
        node('mid', 'ac_bus', '110kV', 0, { nominalVoltage: 110000 }),
        node('xf2', 'transformer', '110/35', 0, { primaryVoltage: 110000, secondaryVoltage: 35000 }),
        node('lv', 'ac_bus', '35kV', 0, { nominalVoltage: 35000 }),
        node('pv1', 'pv_unit', 'PV-1', 0, { inverterCount: 8 }),
        node('pv2', 'pv_unit', 'PV-2', 80, { inverterCount: 8 })
      ],
      edges: [
        edge('hv', 'xf1'), edge('xf1', 'mid'),
        edge('mid', 'xf2'), edge('xf2', 'lv'),
        edge('lv', 'pv1'), edge('lv', 'pv2')
      ]
    }
    const layout = buildTopologyMainLineLayout(topology, [])
    assert.equal(layout.transformers.length, 2)
    assert.ok(layout.transformers[0].y < layout.transformers[1].y)
    assert.equal(layout.units.length, 2)
    assert.equal(layout.units[0].xfmrId, 'xf2')
    assert.ok(layout.units[0].originY > layout.transformers[1].y)
  })

  it('hangs meters and loads on the bus they are actually connected to', () => {
    const topology = {
      nodes: [
        node('hv', 'ac_bus', '220kV', 0, { nominalVoltage: 220000 }),
        node('xf1', 'transformer', 'T1', 0, { primaryVoltage: 220000, secondaryVoltage: 35000 }),
        node('lv', 'ac_bus', '35kV', 0, { nominalVoltage: 35000 }),
        node('meter', 'ac_meter', '并网点', 120, { isPccMeter: true }),
        node('load', 'load', '站用变负荷', 40, { ratedVoltage: 35000 }),
        node('pv1', 'pv_unit', 'PV-1', 0, { inverterCount: 8 }),
        node('pv2', 'pv_unit', 'PV-2', 80, { inverterCount: 8 })
      ],
      edges: [
        edge('hv', 'xf1'), edge('xf1', 'lv'),
        edge('hv', 'meter'), edge('lv', 'load'),
        edge('lv', 'pv1'), edge('lv', 'pv2')
      ]
    }
    const layout = buildTopologyMainLineLayout(topology, [])
    assert.equal(layout.meters.length, 1)
    assert.equal(layout.loads.length, 1)
    const hv = layout.buses.find(b => b.node?.id === 'hv')
    const lv = layout.buses.find(b => b.node?.id === 'lv')
    assert.ok(hv && lv)
    assert.equal(layout.meters[0].busId, 'hv')
    assert.equal(layout.loads[0].busId, 'lv')
    assert.ok(Math.abs(layout.meters[0].y - (hv.y + layout.linkStub)) < 1)
    assert.equal(layout.loads[0].busY, lv.y)
  })
})

describe('buildTopologyMainLineLayout emu device binding', () => {
  function emuTopology(extraNodes = [], extraEdges = []) {
    return {
      nodes: [
        node('bus', 'ac_bus', '35kV', 400, { nominalVoltage: 35000 }),
        node('emu1', 'emu', 'EMU-1', 400),
        node('pcs1', 'pcs', 'PCS-1', 360, { emuId: 'emu1' }),
        node('pcs2', 'pcs', 'PCS-2', 440, { emuId: 'emu1' }),
        ...extraNodes
      ],
      edges: [edge('emu1', 'bus'), ...extraEdges]
    }
  }

  it('lays out pcs and bms device cards inside the unit', () => {
    // 新模型不再画虚线框；改为验证单元内容：设备卡片行 + 底边覆盖到 BMS 底部 + 槽位至少一个单元宽
    const layout = buildTopologyMainLineLayout(
      emuTopology([
        node('dc1', 'dc_bus', '直流母线', 400, { nominalVoltage: 800 }),
        node('bms1', 'bms', 'BMS-1', 400, { clusterCount: 1, packCount: 1 })
      ], [
        edge('pcs1', 'dc1'), edge('pcs2', 'dc1'), edge('bms1', 'dc1')
      ]),
      []
    )
    const u = layout.units.find(x => x.kind === 'emu')
    assert.ok(u, 'emu unit exists')
    assert.equal(layout.groups.length, 0, '不引入虚线框等虚拟概念')
    const pcsCards = u.cards.filter(c => c.tone === 'pcs')
    const bmsCards = u.cards.filter(c => c.tone === 'bms')
    assert.equal(pcsCards.length, 2)
    assert.equal(bmsCards.length, 1)
    const bmsBottom = Math.max(...bmsCards.map(c => c.y + c.h))
    assert.ok(u.bottom >= bmsBottom, 'unit bottom covers down to bms bottom')
    // 单元宽度随物理卡片行自适应（2 列：2*132 + 间隔 20）
    assert.equal(u.halfSpan * 2, 2 * 132 + 20, 'unit span follows the card rows')
  })

  it('picks bound breaker / meter nodes into the unit and keeps meter off the bus pendants', () => {
    const topology = emuTopology([
      node('brk1', 'ac_breaker', '单元断-1', 400, { emuId: 'emu1' }),
      node('meter1', 'ac_meter', '单元电表', 520, { emuId: 'emu1' })
    ])
    const layout = buildTopologyMainLineLayout(topology, [])
    const u = layout.units.find(x => x.kind === 'emu')
    assert.equal(u.unitBreakerNode?.id, 'brk1', 'bound breaker node picked')
    assert.equal(u.unitMeterNode?.id, 'meter1', 'bound meter node picked')
    // 绑定断路器在引线段绘制：母线 → 断路器 → PCS 卡，卡片行相应下移；主干引线接至断路器
    assert.equal(u.brkMid, 18 + 14)
    assert.equal(u.pcsTop, 18 + 28 + 18, 'pcs row shifted below the breaker')
    assert.ok(u.cards.filter(c => c.tone === 'pcs').every(c => c.y === u.pcsTop))
    assert.ok(layout.wires.some(w => Math.abs(w.y1 - u.originY) < 0.5 && Math.abs(w.y2 - (u.originY + u.brkBottom)) < 0.5), 'stem wire reaches the breaker')
    // EMU 绑定电表不作母线挂件（随单元下发供 3D 使用）
    assert.ok(!layout.meters.some(m => m.node?.id === 'meter1'), 'emu meter is not a bus pendant')
  })

  it('leaves breaker / meter unbound without error and keeps default slot width', () => {
    const bound = buildTopologyMainLineLayout(
      emuTopology([node('meter1', 'ac_meter', '单元电表', 520, { emuId: 'emu1' })]),
      []
    )
    const unbound = buildTopologyMainLineLayout(emuTopology(), [])
    const uBound = bound.units.find(x => x.kind === 'emu')
    const uFree = unbound.units.find(x => x.kind === 'emu')
    assert.equal(uBound.unitBreakerNode, null, 'no breaker bound')
    assert.equal(uBound.unitMeterNode?.id, 'meter1')
    assert.equal(uFree.unitBreakerNode, null)
    assert.equal(uFree.unitMeterNode, null)
    // 未绑定断路器时维持短引线（卡片行不下移）；绑定设备不影响槽位宽度（物理卡片行决定宽度）
    assert.equal(uFree.pcsTop, 18)
    assert.equal(uFree.halfSpan, uBound.halfSpan)
  })
})

describe('buildTopologyMainLineLayout sectional bus breaker', () => {
  // 单母线分段：35kV 主母线 —[中压断路器 emuId=emu1]— 35kV 分段母线 → 级变 → 690V → EMU
  function sectionTopology() {
    return {
      nodes: [
        node('main', 'ac_bus', '35kV主母线', 400, { nominalVoltage: 35000 }),
        node('sec', 'ac_breaker', '中压三相断路器', 300, { emuId: 'emu1', closed: true }),
        node('sub', 'ac_bus', '35kV分段', 200, { nominalVoltage: 35000 }),
        node('emu1', 'emu', 'EMU-1', 200),
        node('xf', 'transformer', '级变', 200, { primaryVoltage: 35000, secondaryVoltage: 690 }),
        node('lv', 'ac_bus', '690V', 200, { nominalVoltage: 690 }),
        node('pcs1', 'pcs', 'PCS-1', 160, { emuId: 'emu1' }),
        node('dc1', 'dc_bus', '直流母线', 200, { nominalVoltage: 800 }),
        node('bms1', 'bms', 'BMS-1', 200, {})
      ],
      edges: [
        edge('main', 'sec'), edge('sec', 'sub'), edge('sub', 'xf'), edge('xf', 'lv'),
        edge('lv', 'pcs1'), edge('pcs1', 'dc1'), edge('dc1', 'bms1'), edge('emu1', 'lv')
      ]
    }
  }

  it('emits the emuId-bound section breaker once, on the bus, with its unit index', () => {
    const layout = buildTopologyMainLineLayout(sectionTopology(), [])
    assert.equal(layout.tieBreakers.length, 1, 'section breaker drawn on the bus')
    const tb = layout.tieBreakers[0]
    assert.equal(tb.id, 'sec')
    assert.equal(tb.emuId, 'emu1', 'emu binding surfaced for live telemetry')
    const u = layout.units.find(x => x.kind === 'emu')
    assert.equal(tb.unitIndex, u.index, 'tie breaker points at its owning unit')
    // 遥信绑定保留，但单元内不再重复绘制、引线段也不因此加高
    assert.equal(u.unitBreakerNode?.id, 'sec')
    assert.equal(u.unitBreakerOnBus, true)
    assert.equal(u.pcsTop, 18, 'unit card is not shifted down by a breaker it does not draw')
  })
})
