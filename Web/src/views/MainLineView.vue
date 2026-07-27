<template>
  <div>
    <div class="card">
      <p class="card-title">电站概览</p>
      <div class="metric-grid">
        <div class="metric-item"><div class="label">PCC 电压</div><div class="value">{{ fmtVolt(snap.pccLineVoltageV) }}</div></div>
        <div class="metric-item"><div class="label">电网频率</div><div class="value">{{ fmtHz(snap.systemFrequencyHz) }}</div></div>
        <div class="metric-item"><div class="label">35kV 母线</div><div class="value">{{ fmtVolt(snap.stationBus35LineVoltageV) }}</div></div>
        <div class="metric-item"><div class="label">主断路器</div><div class="value" :class="snap.mainBreakerTripped ? 'tag-offline' : (snap.mainBreakerClosed ? 'tag-online' : 'tag-offline')">{{ breakerLabel }}</div></div>
        <div class="metric-item"><div class="label">求解模式</div><div class="value">{{ snap.propagationEnabled ? '径向传播' : 'Legacy' }}</div></div>
        <div class="metric-item"><div class="label">电表 P</div><div class="value">{{ fmtKw(snap.meterPrimary?.activePowerKw) }}</div></div>
        <div class="metric-item"><div class="label">电表 Q</div><div class="value">{{ fmtKvar(snap.meterPrimary?.reactivePowerKvar) }}</div></div>
        <div class="metric-item metric-item-editable">
          <div class="label">电网电压设定</div>
          <div class="value">{{ fmtVolt(snap.gridNominalLineVoltageV) }}</div>
          <div class="metric-set">
            <input
              v-model="gridVDraft"
              type="text"
              inputmode="decimal"
              class="metric-input"
              placeholder="V"
              @keydown.enter="onSetGridVoltage"
            />
            <button type="button" class="metric-set-btn" @click="onSetGridVoltage">设定</button>
          </div>
        </div>
        <div class="metric-item metric-item-editable">
          <div class="label">电网频率设定</div>
          <div class="value">{{ fmtHz(snap.gridNominalFrequencyHz) }}</div>
          <div class="metric-set">
            <input
              v-model="gridFDraft"
              type="text"
              inputmode="decimal"
              class="metric-input"
              placeholder="Hz"
              @keydown.enter="onSetGridFrequency"
            />
            <button type="button" class="metric-set-btn" @click="onSetGridFrequency">设定</button>
          </div>
        </div>
        <div class="metric-item metric-item-editable">
          <div class="label">35kV 负载 P</div>
          <div class="value">{{ fmtKw(snap.loadActivePowerKw) }}</div>
          <div class="metric-set">
            <input
              v-model="loadPDraft"
              type="text"
              inputmode="decimal"
              class="metric-input"
              placeholder="kW"
              @keydown.enter="onSetLoadActive"
            />
            <button type="button" class="metric-set-btn" @click="onSetLoadActive">设定</button>
          </div>
        </div>
        <div class="metric-item metric-item-editable">
          <div class="label">35kV 负载 Q</div>
          <div class="value">{{ fmtKvar(snap.loadReactivePowerKvar) }}</div>
          <div class="metric-set">
            <input
              v-model="loadQDraft"
              type="text"
              inputmode="decimal"
              class="metric-input"
              placeholder="kvar"
              @keydown.enter="onSetLoadReactive"
            />
            <button type="button" class="metric-set-btn" @click="onSetLoadReactive">设定</button>
          </div>
        </div>
      </div>
    </div>

    <div class="card">
      <p class="card-title">
        电气主接线
        <span class="card-hint">左键点击断路器 · 右键拖动平移 · 滚轮缩放 · PCS 卡片可启停/设定有功无功</span>
      </p>
      <MainLineSvg
        :snap="snap"
        @toggle-main-breaker="onToggleMainBreaker"
        @toggle-unit-breaker="onToggleUnitBreaker"
        @pcs-start="onPcsStart"
        @pcs-stop="onPcsStop"
        @pcs-set-power="onPcsSetPower"
        @pcs-set-reactive="onPcsSetReactive"
        @bms-power-on="onBmsPowerOn"
        @bms-power-off="onBmsPowerOff"
        @bms-fault-clear="onBmsFaultClear"
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
        <el-table-column prop="phasor" label="总相量" min-width="220" />
        <el-table-column prop="lineVoltages" label="线电压" min-width="220" />
        <el-table-column prop="phaseCurrents" label="相电流" min-width="200" />
        <el-table-column prop="power" label="功率" min-width="200" />
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
const loadPDraft = ref('')
const loadQDraft = ref('')
const gridVDraft = ref('')
const gridFDraft = ref('')
let lastLoadSetP = null
let lastLoadSetQ = null
let lastGridSetV = null
let lastGridSetF = null

function syncLoadDrafts(force = false) {
  const setP = snap.value.loadActivePowerSetKw
  const setQ = snap.value.loadReactivePowerSetKvar
  if (force || (setP != null && setP !== lastLoadSetP)) {
    loadPDraft.value = Number(setP ?? 0).toFixed(1)
    lastLoadSetP = setP
  }
  if (force || (setQ != null && setQ !== lastLoadSetQ)) {
    loadQDraft.value = Number(setQ ?? 0).toFixed(1)
    lastLoadSetQ = setQ
  }

  const setV = snap.value.gridNominalLineVoltageV
  const setF = snap.value.gridNominalFrequencyHz
  if (force || (setV != null && setV !== lastGridSetV)) {
    gridVDraft.value = Number(setV ?? 220000).toFixed(0)
    lastGridSetV = setV
  }
  if (force || (setF != null && setF !== lastGridSetF)) {
    gridFDraft.value = Number(setF ?? 50).toFixed(2)
    lastGridSetF = setF
  }
}

function fmtVolt(v) {
  if (v == null) return '—'
  return v >= 1000 ? `${(v / 1000).toFixed(1)} kV` : `${(v || 0).toFixed(1)} V`
}
function fmtHz(v) { return v == null ? '—' : `${Number(v).toFixed(2)} Hz` }
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
function fmtLineVoltages(m) {
  if (!m) return '—'
  return `Uab ${fmtVolt(m.lineVoltageAB)} / Ubc ${fmtVolt(m.lineVoltageBC)} / Uca ${fmtVolt(m.lineVoltageCA)}`
}
function fmtPhaseCurrents(m) {
  if (!m) return '—'
  return `Ia ${Number(m.phaseACurrent || 0).toFixed(1)} A / Ib ${Number(m.phaseBCurrent || 0).toFixed(1)} A / Ic ${Number(m.phaseCCurrent || 0).toFixed(1)} A`
}
function fmtBreaker(closed, tripped) { return tripped ? '跳闸' : closed ? '合' : '分' }

function fmtPcsChannel(ch) {
  if (!ch) return '—'
  return [ch.pcsDeviceState, ch.pcsStartStop, ch.pcsTargetP, ch.pcsActualP, ch.pcsTargetQ, ch.pcsActualQ, ch.pcsBlackStart, `模式:${ch.pcsGridMode}`].join(' ')
}
function fmtBmsChannel(ch) {
  if (!ch) return '—'
  const energy = ch.bmsEnergy || `累计充 ${(ch.cumulativeChargeEnergyKwh ?? 0).toFixed(1)} / 放 ${(ch.cumulativeDischargeEnergyKwh ?? 0).toFixed(1)} kWh`
  const run = ch.bmsRunStatus || '运行:—'
  return `${ch.bmsCompact} | ${run} | ${energy} | 并网:${ch.gridConnect} | ${ch.bmsBlackStart}`
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
  {
    name: 'PCC电表',
    phasor: fmtPhasor(snap.value.meterPrimary),
    lineVoltages: fmtLineVoltages(snap.value.meterThreePhase),
    phaseCurrents: fmtPhaseCurrents(snap.value.meterThreePhase),
    power: `P ${fmtKw(snap.value.meterPrimary?.activePowerKw)}  Q ${fmtKvar(snap.value.meterPrimary?.reactivePowerKvar)}`
  },
  {
    name: '主变一次',
    phasor: fmtPhasorViPhi(snap.value.mainTransformerPrimary),
    lineVoltages: '—',
    phaseCurrents: '—',
    power: `P ${fmtKw(snap.value.mainTransformerPrimary?.activePowerKw)}  Q ${fmtKvar(snap.value.mainTransformerPrimary?.reactivePowerKvar)}`
  },
  {
    name: '主变二次',
    phasor: fmtPhasorViPhi(snap.value.mainTransformerSecondary),
    lineVoltages: '—',
    phaseCurrents: '—',
    power: `P ${fmtKw(snap.value.mainTransformerSecondary?.activePowerKw)}  Q ${fmtKvar(snap.value.mainTransformerSecondary?.reactivePowerKvar)}`
  },
  {
    name: '35kV负载',
    phasor: '—',
    lineVoltages: '—',
    phaseCurrents: '—',
    power: `P ${fmtKw(snap.value.loadActivePowerKw)}  Q ${fmtKvar(snap.value.loadReactivePowerKvar)}`
  }
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

/** PCS 有功设定：dpc simEmu{N}.yt0|yt4 set raw（Scale=10，kW×10） */
async function onPcsSetPower({ emuUnit, ytPoint, powerKw }) {
  const kw = Number(powerKw)
  if (!Number.isFinite(kw)) {
    ElMessage.warning('请输入有效的有功功率')
    return
  }
  const raw = Math.round(kw * 10)
  await runChannelCommand(`dpc simEmu${emuUnit}.${ytPoint} set ${raw}`)
}

/** PCS 无功设定：dpc simEmu{N}.yt1|yt5 set raw（Scale=10，kvar×10） */
async function onPcsSetReactive({ emuUnit, ytPoint, reactiveKvar }) {
  const kvar = Number(reactiveKvar)
  if (!Number.isFinite(kvar)) {
    ElMessage.warning('请输入有效的无功功率')
    return
  }
  const raw = Math.round(kvar * 10)
  await runChannelCommand(`dpc simEmu${emuUnit}.${ytPoint} set ${raw}`)
}

async function onSetLoadActive() {
  const kw = Number(loadPDraft.value)
  if (!Number.isFinite(kw)) {
    ElMessage.warning('请输入有效的负载有功（kW）')
    return
  }
  await runChannelCommand(`esscmd setLoad activePower ${kw}`)
  lastLoadSetP = kw
}

async function onSetLoadReactive() {
  const kvar = Number(loadQDraft.value)
  if (!Number.isFinite(kvar)) {
    ElMessage.warning('请输入有效的负载无功（kvar）')
    return
  }
  await runChannelCommand(`esscmd setLoad reactivePower ${kvar}`)
  lastLoadSetQ = kvar
}

async function onSetGridVoltage() {
  const volts = Number(gridVDraft.value)
  if (!Number.isFinite(volts) || volts <= 0) {
    ElMessage.warning('请输入有效的电网线电压（V，如 220000）')
    return
  }
  await runChannelCommand(`esscmd setGrid voltage ${volts}`)
  lastGridSetV = volts
}

async function onSetGridFrequency() {
  const hz = Number(gridFDraft.value)
  if (!Number.isFinite(hz) || hz <= 0 || hz > 75) {
    ElMessage.warning('请输入有效的电网频率（Hz，范围 (0, 75]）')
    return
  }
  await runChannelCommand(`esscmd setGrid frequency ${hz}`)
  lastGridSetF = hz
}

async function onBmsPowerOn(bmsNumber) {
  await runChannelCommand(`esscmd setbms${bmsNumber} power on`)
}

async function onBmsPowerOff(bmsNumber) {
  await runChannelCommand(`esscmd setbms${bmsNumber} power off`)
}

async function onBmsFaultClear(bmsNumber) {
  await runChannelCommand(`esscmd bms${bmsNumber} fault clear`)
}

onMounted(async () => {
  try {
    snap.value = await getMainLine()
    syncLoadDrafts(true)
  } catch (e) { console.warn(e) }
})

useRealtime(RealtimeChannels.MainLine, {
  [RealtimeMethods.ReceiveMainLine]: data => {
    snap.value = data
    syncLoadDrafts()
  }
})
</script>

<style scoped>
.card-hint { font-size: 12px; color: #909399; font-weight: normal; margin-left: 12px; }

.metric-item-editable {
  position: relative;
  min-height: 72px;
  padding-bottom: 30px;
}

.metric-set {
  position: absolute;
  right: 8px;
  bottom: 6px;
  display: flex;
  align-items: center;
  gap: 4px;
}

.metric-input {
  width: 62px;
  font-size: 11px;
  padding: 2px 4px;
  border: 1px solid #dcdfe6;
  border-radius: 3px;
  box-sizing: border-box;
}

.metric-set-btn {
  font-size: 11px;
  line-height: 1.2;
  padding: 2px 6px;
  border: 1px solid #c0c4cc;
  border-radius: 3px;
  background: #fff;
  color: #303133;
  cursor: pointer;
}

.metric-set-btn:hover {
  border-color: #409eff;
  color: #409eff;
}
</style>
