<template>
  <div>
    <div class="card">
      <p class="card-title">电站概览</p>
      <div class="metric-grid">
        <div class="metric-item"><div class="label">PCC 电压</div><div class="value">{{ fmtVolt(snap.pccLineVoltageV) }}</div></div>
        <div class="metric-item"><div class="label">35kV 母线</div><div class="value">{{ fmtVolt(snap.stationBus35LineVoltageV) }}</div></div>
        <div class="metric-item"><div class="label">主断路器</div><div class="value" :class="snap.mainBreakerTripped ? 'tag-offline' : (snap.mainBreakerClosed ? 'tag-online' : 'tag-offline')">{{ breakerLabel }}</div></div>
        <div class="metric-item"><div class="label">求解模式</div><div class="value">{{ snap.propagationEnabled ? '径向传播' : 'Legacy' }}</div></div>
        <div class="metric-item"><div class="label">电表 P</div><div class="value">{{ fmtKw(snap.meterPrimary?.activePowerKw) }}</div></div>
        <div class="metric-item"><div class="label">电表 Q</div><div class="value">{{ fmtKvar(snap.meterPrimary?.reactivePowerKvar) }}</div></div>
        <div class="metric-item"><div class="label">35kV 负载 P</div><div class="value">{{ fmtKw(snap.loadActivePowerKw) }}</div></div>
        <div class="metric-item"><div class="label">35kV 负载 Q</div><div class="value">{{ fmtKvar(snap.loadReactivePowerKvar) }}</div></div>
      </div>
    </div>

    <div class="card">
      <p class="card-title">
        电气主接线
        <span class="card-hint">左键点击断路器 · 右键拖动平移 · 滚轮缩放 · PCS/BMS 卡片内按钮控制</span>
      </p>
      <MainLineSvg
        :snap="snap"
        @toggle-main-breaker="onToggleMainBreaker"
        @toggle-unit-breaker="onToggleUnitBreaker"
        @pcs-start="onPcsStart"
        @pcs-stop="onPcsStop"
        @bms-power-on="onBmsPowerOn"
        @bms-power-off="onBmsPowerOff"
      />
    </div>

    <div class="card">
      <p class="card-title">传播母线节点</p>
      <el-table :data="busRows" size="small" border stripe>
        <el-table-column prop="busId" label="节点" width="160" />
        <el-table-column label="V / I / φ"><template #default="{ row }">{{ row.phasor }}</template></el-table-column>
        <el-table-column label="P / Q / PF"><template #default="{ row }">P {{ row.p }} kW &nbsp; Q {{ row.q }} kvar &nbsp; PF {{ row.pf }}</template></el-table-column>
      </el-table>
    </div>

    <div class="card">
      <p class="card-title">交流设备</p>
      <el-table :data="acRows" size="small" border stripe>
        <el-table-column prop="name" label="设备" width="120" />
        <el-table-column prop="phasor" label="相量" />
        <el-table-column prop="power" label="功率" />
      </el-table>
    </div>

    <div class="card">
      <p class="card-title">储能单元明细</p>
      <el-table :data="unitRows" size="small" border stripe>
        <el-table-column prop="unit" label="UNIT" width="80" fixed />
        <el-table-column label="单元断/单元变" min-width="200"><template #default="{ row }">{{ row.xf }}</template></el-table-column>
        <el-table-column label="PCS-A" min-width="240"><template #default="{ row }">{{ row.pcsA }}</template></el-table-column>
        <el-table-column label="PCS-B" min-width="240"><template #default="{ row }">{{ row.pcsB }}</template></el-table-column>
        <el-table-column label="舱-A" min-width="200"><template #default="{ row }">{{ row.bmsA }}</template></el-table-column>
        <el-table-column label="舱-B" min-width="200"><template #default="{ row }">{{ row.bmsB }}</template></el-table-column>
      </el-table>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getMainLine, postMainBreaker, postUnitBreaker, postCommand } from '@/services/api.js'
import { useRealtime, RealtimeMethods, RealtimeChannels } from '@/services/useRealtime.js'
import MainLineSvg from '@/components/MainLineSvg.vue'

const snap = ref({ units: [] })

function fmtVolt(v) {
  if (v == null) return '—'
  return v >= 1000 ? `${(v / 1000).toFixed(1)} kV` : `${(v || 0).toFixed(1)} V`
}
function fmtKw(v) { return v == null ? '—' : `${Number(v).toFixed(1)} kW` }
function fmtKvar(v) { return v == null ? '—' : `${Number(v).toFixed(1)} kvar` }
function fmtPhasor(p) {
  if (!p) return '—'
  return `${fmtVolt(p.lineVoltageV)} / ${Number(p.lineCurrentA || 0).toFixed(1)} A / φ${Number(p.phaseAngleDeg || 0).toFixed(1)}° / ${Number(p.frequencyHz || 0).toFixed(1)} Hz`
}
function fmtPhasorViPhi(p) {
  if (!p) return '—'
  return `${fmtVolt(p.lineVoltageV)} / ${Number(p.lineCurrentA || 0).toFixed(1)} A / φ${Number(p.phaseAngleDeg || 0).toFixed(1)}°`
}
function fmtBreaker(closed, tripped) { return tripped ? '跳闸' : closed ? '合' : '分' }

function fmtPcsChannel(ch) {
  if (!ch) return '—'
  return [ch.pcsDeviceState, ch.pcsStartStop, ch.pcsTargetP, ch.pcsActualP, ch.pcsTargetQ, ch.pcsActualQ, ch.pcsBlackStart, `模式:${ch.pcsGridMode}`].join(' ')
}
function fmtBmsChannel(ch) {
  if (!ch) return '—'
  return `${ch.bmsCompact} | 并网:${ch.gridConnect} | ${ch.bmsBlackStart}`
}

const breakerLabel = computed(() =>
  snap.value.mainBreakerLabel || fmtBreaker(snap.value.mainBreakerClosed, snap.value.mainBreakerTripped)
)

const busRows = computed(() => {
  const rows = []
  const push = b => {
    if (!b) return
    const p = { lineVoltageV: b.lineVoltageV, lineCurrentA: b.lineCurrentA, phaseAngleDeg: b.phaseAngleDeg, frequencyHz: b.frequencyHz }
    const ap = p.lineVoltageV * p.lineCurrentA * Math.cos((p.phaseAngleDeg || 0) * Math.PI / 180) / 1000
    rows.push({
      busId: b.busId,
      phasor: fmtPhasor(p),
      p: Number(ap).toFixed(1),
      q: '0.0',
      pf: '—'
    })
  }
  push(snap.value.busGrid)
  push(snap.value.bus35Propagation)
  for (const u of snap.value.units || []) push(u.bus690)
  return rows
})

const acRows = computed(() => [
  { name: 'PCC电表', phasor: fmtPhasor(snap.value.meterPrimary), power: `P ${fmtKw(snap.value.meterPrimary?.activePowerKw)}  Q ${fmtKvar(snap.value.meterPrimary?.reactivePowerKvar)}` },
  { name: '主变一次', phasor: fmtPhasorViPhi(snap.value.mainTransformerPrimary), power: `P ${fmtKw(snap.value.mainTransformerPrimary?.activePowerKw)}  Q ${fmtKvar(snap.value.mainTransformerPrimary?.reactivePowerKvar)}` },
  { name: '主变二次', phasor: fmtPhasorViPhi(snap.value.mainTransformerSecondary), power: `P ${fmtKw(snap.value.mainTransformerSecondary?.activePowerKw)}  Q ${fmtKvar(snap.value.mainTransformerSecondary?.reactivePowerKvar)}` },
  { name: '35kV负载', phasor: '—', power: `P ${fmtKw(snap.value.loadActivePowerKw)}  Q ${fmtKvar(snap.value.loadReactivePowerKvar)}` }
])

const unitRows = computed(() => (snap.value.units || []).map(u => ({
  unit: `UNIT ${u.unitNumber ?? u.unitIndex + 1}`,
  xf: `${u.unitBreakerLabel || fmtBreaker(u.unitBreakerClosed, u.unitBreakerTripped)} | ${u.unitTransformerLine || fmtPhasorViPhi(u.unitTransformerSecondary)}`,
  pcsA: fmtPcsChannel(u.channelA),
  pcsB: fmtPcsChannel(u.channelB),
  bmsA: fmtBmsChannel(u.channelA),
  bmsB: fmtBmsChannel(u.channelB)
})))

async function onToggleMainBreaker() {
  if (snap.value.mainBreakerTripped) {
    ElMessage.warning('主断路器已跳闸，请先复位')
    return
  }
  try {
    const next = !snap.value.mainBreakerClosed
    const r = await postMainBreaker(next)
    ElMessage[r.success ? 'success' : 'error'](r.message)
  } catch (e) {
    ElMessage.error(e.message)
  }
}

async function onToggleUnitBreaker(unitIndex) {
  const u = (snap.value.units || []).find(x => x.unitIndex === unitIndex)
  if (u?.unitBreakerTripped) {
    ElMessage.warning(`UNIT ${unitIndex + 1} 单元断路器已跳闸`)
    return
  }
  try {
    const next = !(u?.unitBreakerClosed ?? false)
    const r = await postUnitBreaker(unitIndex + 1, next)
    ElMessage[r.success ? 'success' : 'error'](r.message)
  } catch (e) {
    ElMessage.error(e.message)
  }
}

async function runChannelCommand(input) {
  try {
    const r = await postCommand(input)
    ElMessage[r.success ? 'success' : 'error'](r.message)
  } catch (e) {
    ElMessage.error(e.message)
  }
}

/** PCS 启停：esscmd pcsN start|stop（内部复用 dpc 控制管道） */
async function onPcsStart(pcsNumber) {
  await runChannelCommand(`esscmd pcs${pcsNumber} start`)
}

async function onPcsStop(pcsNumber) {
  await runChannelCommand(`esscmd pcs${pcsNumber} stop`)
}

async function onBmsPowerOn(bmsNumber) {
  await runChannelCommand(`esscmd setbms${bmsNumber} power on`)
}

async function onBmsPowerOff(bmsNumber) {
  await runChannelCommand(`esscmd setbms${bmsNumber} power off`)
}

onMounted(async () => {
  try { snap.value = await getMainLine() } catch (e) { console.warn(e) }
})

useRealtime(RealtimeChannels.MainLine, {
  [RealtimeMethods.ReceiveMainLine]: data => { snap.value = data }
})
</script>

<style scoped>
.card-hint { font-size: 12px; color: #909399; font-weight: normal; margin-left: 12px; }
</style>
