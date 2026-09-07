<template>
  <div>
    <div class="card">
      <p class="card-title">电站 EMS 策略</p>
      <p class="hint">
        站级 PPC：有功（开环/闭环/曲线 + 一次调频/惯量）与无功（开环/闭环/曲线/PF/恒压 + 下垂调压）分配到各 PCS。
        启用后占用控制权；与第三方遥控互斥，同一时刻只能有一个外部控制源。默认关闭。
      </p>
      <el-alert
        v-if="blockedByThirdParty"
        title="第三方 EMS 占用中，无法启用电站策略。请先在「第三方遥控」中释放。"
        type="warning"
        :closable="false"
        show-icon
      />
      <el-alert
        v-else-if="status.enabled"
        title="EMS 策略已占用控制权：外部下发均被拒绝，直到关闭。"
        type="info"
        :closable="false"
        show-icon
      />
      <el-alert
        v-if="snap.curveWait"
        title="计划曲线未命中（WAIT），本拍有功或无功输出为 0。"
        type="warning"
        :closable="false"
        show-icon
        style="margin-top:8px"
      />
      <div class="toolbar">
        <el-switch
          v-model="enabled"
          active-text="策略启用"
          inactive-text="策略关闭"
          :loading="busy"
          :disabled="busy || blockedByThirdParty"
          @change="onToggleEnabled"
        />
      </div>
    </div>

    <div class="card">
      <p class="card-title">有功</p>
      <div class="toolbar">
        <el-select v-model="activeMode" size="small" style="width:160px" :disabled="busy" @change="onPatch">
          <el-option :value="0" label="开环固定值" />
          <el-option :value="1" label="闭环固定值" />
          <el-option :value="2" label="闭环曲线" />
        </el-select>
        <el-input-number v-model="localP" size="small" :step="50" controls-position="right" @change="onPatch" />
        <span class="meta">本地 P kW</span>
        <el-switch v-model="pfrEnabled" active-text="一次调频" :disabled="busy || activeMode === 0" @change="onPatch" />
        <el-switch v-model="inertiaEnabled" active-text="惯量" :disabled="busy || activeMode === 0" @change="onPatch" />
      </div>
    </div>

    <div class="card">
      <p class="card-title">无功</p>
      <div class="toolbar">
        <el-select v-model="reactiveMode" size="small" style="width:160px" :disabled="busy" @change="onPatch">
          <el-option :value="0" label="开环固定值" />
          <el-option :value="1" label="闭环固定值" />
          <el-option :value="2" label="闭环曲线" />
          <el-option :value="3" label="功率因数" />
          <el-option :value="4" label="恒压" />
        </el-select>
        <el-input-number v-model="localQ" size="small" :step="50" controls-position="right" @change="onPatch" />
        <span class="meta">本地 Q kvar</span>
        <el-input-number v-model="pfSet" size="small" :min="0.1" :max="1" :step="0.01" :disabled="reactiveMode !== 3" @change="onPatch" />
        <span class="meta">目标 PF</span>
        <el-switch v-model="droopEnabled" active-text="下垂调压" :disabled="busy || (reactiveMode !== 1 && reactiveMode !== 2)" @change="onPatch" />
      </div>
    </div>

    <div class="card">
      <p class="card-title">运行快照</p>
      <div class="metric-grid">
        <div class="metric-item"><div class="label">电网频率</div><div class="value">{{ fmt(snap.frequencyHz, 3) }} Hz</div></div>
        <div class="metric-item"><div class="label">并网点 P/Q</div><div class="value">{{ fmt(snap.pccActivePowerKw, 1) }} / {{ fmt(snap.pccReactivePowerKvar, 1) }}</div></div>
        <div class="metric-item"><div class="label">PCC 电压</div><div class="value">{{ fmt(snap.pccLineVoltageV, 0) }} V</div></div>
        <div class="metric-item"><div class="label">ΔP 调频</div><div class="value">{{ fmt(snap.frequencyDeltaKw, 1) }} · {{ actionLabel(snap.frequencyAction) }}</div></div>
        <div class="metric-item"><div class="label">ΔP 惯量</div><div class="value">{{ fmt(snap.inertiaDeltaKw, 1) }} · {{ actionLabel(snap.inertiaAction) }}</div></div>
        <div class="metric-item"><div class="label">ΔQ 下垂</div><div class="value">{{ fmt(snap.droopDeltaKvar, 1) }} · {{ actionLabel(snap.droopAction) }}</div></div>
        <div class="metric-item"><div class="label">站级 P 指令</div><div class="value">{{ fmt(snap.plantActiveCommandKw, 1) }} kW</div></div>
        <div class="metric-item"><div class="label">站级 Q 指令</div><div class="value">{{ fmt(snap.plantReactiveCommandKvar, 1) }} kvar</div></div>
      </div>
      <el-table :data="snap.branches || []" size="small" border stripe style="margin-top:12px">
        <el-table-column prop="index" label="支路" width="80" />
        <el-table-column label="单元" width="80">
          <template #default="{ row }">{{ row.unitIndex0 + 1 }}</template>
        </el-table-column>
        <el-table-column label="P 设定 kW">
          <template #default="{ row }">{{ fmt(row.activePowerKw, 1) }}</template>
        </el-table-column>
        <el-table-column label="Q 设定 kvar">
          <template #default="{ row }">{{ fmt(row.reactivePowerKvar, 1) }}</template>
        </el-table-column>
      </el-table>
    </div>
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { getEmsStrategy, postEmsStrategy, postEmsStrategyEnable } from '@/services/api.js'

const busy = ref(false)
const status = ref({ enabled: false, config: {}, snapshot: {} })
const enabled = ref(false)
const activeMode = ref(1)
const reactiveMode = ref(1)
const localP = ref(0)
const localQ = ref(0)
const pfSet = ref(1)
const pfrEnabled = ref(true)
const inertiaEnabled = ref(false)
const droopEnabled = ref(false)
let timer = null

const snap = computed(() => status.value.snapshot || {})
const blockedByThirdParty = computed(() => status.value.gateOwner === 'ThirdPartyEms')

function fmt(v, n) {
  const x = Number(v)
  return Number.isFinite(x) ? x.toFixed(n) : '—'
}

function actionLabel(code) {
  return code === 1 ? 'ACTION' : 'RESET'
}

function applyStatus(s) {
  status.value = s || { enabled: false, config: {}, snapshot: {} }
  enabled.value = !!status.value.enabled
  const cfg = status.value.config || {}
  activeMode.value = cfg.activeMode ?? 1
  reactiveMode.value = cfg.reactiveMode ?? 1
  localP.value = cfg.localActiveSetKw ?? 0
  localQ.value = cfg.localReactiveSetKvar ?? 0
  pfSet.value = cfg.powerFactorSet ?? 1
  pfrEnabled.value = cfg.primaryFrequency?.enabled !== false
  inertiaEnabled.value = cfg.inertia?.enabled === true
  droopEnabled.value = cfg.voltageDroop?.enabled === true
}

async function refresh() {
  applyStatus(await getEmsStrategy())
}

async function onToggleEnabled(val) {
  busy.value = true
  try {
    const res = await postEmsStrategyEnable(val)
    applyStatus(res.status)
    ElMessage.success(res.message || (val ? '已启用' : '已关闭'))
  } catch (e) {
    enabled.value = !val
    ElMessage.error(e.message || '切换失败')
  } finally {
    busy.value = false
  }
}

async function onPatch() {
  busy.value = true
  try {
    const res = await postEmsStrategy({
      activeMode: activeMode.value,
      reactiveMode: reactiveMode.value,
      localActiveSetKw: localP.value,
      localReactiveSetKvar: localQ.value,
      powerFactorSet: pfSet.value,
      primaryFrequencyEnabled: pfrEnabled.value,
      inertiaEnabled: inertiaEnabled.value,
      voltageDroopEnabled: droopEnabled.value
    })
    applyStatus(res.status)
  } catch (e) {
    ElMessage.error(e.message || '更新失败')
    await refresh()
  } finally {
    busy.value = false
  }
}

onMounted(async () => {
  try {
    await refresh()
  } catch (e) {
    ElMessage.error(e.message || '无法读取 EMS 策略')
  }
  timer = setInterval(() => { refresh().catch(() => {}) }, 1000)
})

onBeforeUnmount(() => {
  if (timer) clearInterval(timer)
})
</script>
