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
    assert.equal(u.omitBus690, true)
    assert.equal(u.dcParallel, false)
    assert.equal(u.unitSnap, null)
    assert.equal(layout.groups.filter(g => g.kind === 'pv').length, 0, '光伏单元不再画外框虚线')
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

  it('places emu and pv left-to-right and keeps ess snapshot on emu only', () => {
    // 新模型：EMU 为虚拟节点，储能支路由归属它的 PCS 组展开；无 PCS 的 EMU 不生成支路
    const topology = {
      nodes: [
        node('emu1', 'emu', 'EMU-1', 400),
        node('pcs1', 'pcs', 'PCS-1', 380, { emuId: 'emu1' }),
        node('pcs2', 'pcs', 'PCS-2', 460, { emuId: 'emu1' }),
        node('pv1', 'pv_unit', '光伏单元-1', 100, { inverterCount: 16, inverterRatedPowerKw: 320 })
      ],
      edges: []
    }
    const units = [{ unitIndex: 0, unitNumber: 1, channelA: { pcsNumber: 1 } }]
    const layout = buildTopologyMainLineLayout(topology, units)

    assert.equal(layout.units.length, 2)
    assert.equal(layout.units[0].kind, 'pv')
    assert.equal(layout.units[0].unitSnap, null)
    assert.equal(layout.units[1].kind, 'emu')
    assert.equal(layout.units[1].unitSnap?.unitNumber, 1)
    assert.equal(layout.units[1].pcsNodes.length, 2)
    assert.equal(layout.omitBusLv, false)
  })

  it('assigns live units by emu node rank (Y,X), independent of draw order', () => {
    // emu1 节点在左但其 PCS 画在右侧；运行时单元序按 EMU 节点 (Y,X)，与绘制顺序相反
    const topology = {
      nodes: [
        node('emu1', 'emu', 'EMU-1', 100),
        node('emu2', 'emu', 'EMU-2', 700),
        node('pcs1', 'pcs', 'PCS-1', 760, { emuId: 'emu1' }),
        node('pcs2', 'pcs', 'PCS-2', 160, { emuId: 'emu2' })
      ],
      edges: []
    }
    const units = [
      { unitIndex: 0, unitNumber: 1, channelA: { pcsNumber: 1 } },
      { unitIndex: 1, unitNumber: 2, channelA: { pcsNumber: 2 } }
    ]
    const layout = buildTopologyMainLineLayout(topology, units)

    assert.equal(layout.units.length, 2)
    // 绘制序：emu2 组（leader x=160）在左
    assert.equal(layout.units[0].emu?.id, 'emu2')
    assert.equal(layout.units[0].unitSnap?.unitNumber, 2)
    assert.equal(layout.units[1].emu?.id, 'emu1')
    assert.equal(layout.units[1].unitSnap?.unitNumber, 1)
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
