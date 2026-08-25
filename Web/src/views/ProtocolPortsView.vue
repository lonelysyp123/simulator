<template>
  <div>
    <div class="card">
      <div class="card-title" style="display:flex;align-items:center;justify-content:space-between">
        <span>协议端口配置</span>
        <span>
          <el-button size="small" type="primary" :loading="saving" :disabled="!dirty" @click="save">保存配置</el-button>
          <el-button size="small" type="success" :loading="applying" @click="applyNow">立即生效</el-button>
          <el-button size="small" @click="resetDefaults">恢复默认</el-button>
          <el-button size="small" text @click="reload">刷新</el-button>
        </span>
      </div>
      <el-alert
        v-if="overridesError"
        :title="overridesError"
        type="warning"
        :closable="false"
        show-icon
        style="margin-bottom:8px"
      />
      <el-alert
        type="info"
        :closable="false"
        show-icon
        style="margin-bottom:8px"
        title="同端口同从站号的多个设备将合并点表（地址不可重叠）；同端口不同从站号注册为独立从站。保存后重启生效，或点击「立即生效」热重建协议层（会断开现有 Modbus 连接）。"
      />
      <el-alert
        v-for="(err, idx) in clientErrors"
        :key="'ce' + idx"
        :title="err"
        type="error"
        :closable="false"
        show-icon
        style="margin-bottom:8px"
      />
      <el-alert
        v-for="(g, idx) in mergeGroups"
        :key="'mg' + idx"
        :title="`端口 ${g[0].port} 从站号 ${g[0].slaveId}：${g.map(d => d.name).join('、')} 将合并点表，点位地址不可重叠（保存时服务端自动查重）`"
        type="warning"
        :closable="false"
        show-icon
        style="margin-bottom:8px"
      />
      <el-table
        :data="devices"
        size="small"
        border
        stripe
        :row-class-name="rowClass"
      >
        <el-table-column prop="name" label="设备" width="130" />
        <el-table-column label="类型" width="120">
          <template #default="{ row }">{{ typeLabel(row.type) }}</template>
        </el-table-column>
        <el-table-column prop="pointMapFile" label="点表" width="130" />
        <el-table-column label="端口" width="150">
          <template #default="{ row }">
            <el-input-number v-model="row.port" :min="1" :max="65535" :step="1" size="small" controls-position="right" style="width:120px" @change="markDirty" />
          </template>
        </el-table-column>
        <el-table-column label="从站号" width="130">
          <template #default="{ row }">
            <el-input-number v-model="row.slaveId" :min="1" :max="247" :step="1" size="small" controls-position="right" style="width:100px" @change="markDirty" />
          </template>
        </el-table-column>
        <el-table-column label="默认" width="110">
          <template #default="{ row }">{{ row.defaultPort }} / {{ row.defaultSlaveId }}</template>
        </el-table-column>
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag v-if="!row.registered" type="info" size="small">未注册</el-tag>
            <el-tag v-else :type="row.online ? 'success' : 'danger'" size="small">{{ row.online ? '在线' : '离线' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="备注">
          <template #default="{ row }">
            <span v-if="!row.isDefault" class="mark-override">手动覆盖</span>
            <span v-if="sharedPorts.has(row.port)" class="mark-shared">共享端口</span>
            <span v-if="row.rackCount > 0" class="mark-rack">簇从站 {{ row.slaveId + 1 }}-{{ row.slaveId + row.rackCount }}</span>
            <span v-for="(err, idx) in row.errors" :key="idx" class="mark-error">{{ err }}</span>
          </template>
        </el-table-column>
      </el-table>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import {
  getProtocolPorts, putProtocolPorts, postProtocolPortsApply, postProtocolPortsReset
} from '@/services/api.js'
import { ElMessage, ElMessageBox } from 'element-plus'

const devices = ref([])
const overridesError = ref('')
const dirty = ref(false)
const saving = ref(false)
const applying = ref(false)

const TYPE_LABELS = {
  0: 'BMS 电池',
  1: 'EMU / PCS',
  2: 'EM 电表',
  3: 'LC 就地控制',
  4: '光伏 Logger',
  5: '光伏电表'
}

function typeLabel(t) { return TYPE_LABELS[t] ?? String(t) }

const sharedPorts = computed(() => {
  const count = new Map()
  for (const d of devices.value) count.set(d.port, (count.get(d.port) || 0) + 1)
  return new Set([...count.entries()].filter(([, n]) => n > 1).map(([p]) => p))
})

const clientErrors = computed(() => {
  const errors = []
  // 端口/从站号范围
  for (const d of devices.value) {
    if (!Number.isInteger(d.port) || d.port < 1 || d.port > 65535)
      errors.push(`${d.name}: 端口 ${d.port} 超出合法范围 1-65535`)
    if (!Number.isInteger(d.slaveId) || d.slaveId < 1 || d.slaveId > 247)
      errors.push(`${d.name}: 从站号 ${d.slaveId} 超出合法范围 1-247`)
  }
  return errors
})

// 同端口同从站号的合并组（提示：点表将合并，地址不可重叠，保存时服务端做点位查重）
const mergeGroups = computed(() => {
  const map = new Map()
  for (const d of devices.value) {
    const key = `${d.port}#${d.slaveId}`
    if (!map.has(key)) map.set(key, [])
    map.get(key).push(d)
  }
  return [...map.values()].filter(g => g.length > 1)
})

function rowClass({ row }) {
  return sharedPorts.value.has(row.port) ? 'shared-port-row' : ''
}

function markDirty() { dirty.value = true }

async function reload() {
  try {
    const data = await getProtocolPorts()
    devices.value = data.devices || []
    overridesError.value = data.overridesError || ''
    dirty.value = false
  } catch (e) {
    ElMessage.error(e.message)
  }
}

async function save() {
  if (clientErrors.value.length > 0) {
    ElMessage.error('请先修正界面上的配置错误')
    return
  }
  saving.value = true
  try {
    const entries = devices.value.map(d => ({ name: d.name, port: d.port, slaveId: d.slaveId }))
    const r = await putProtocolPorts(entries)
    ElMessage.success(r.message || '已保存')
    await reload()
  } catch (e) {
    ElMessage.error(e.message)
  } finally {
    saving.value = false
  }
}

async function applyNow() {
  try {
    await ElMessageBox.confirm(
      '热重建将断开所有现有 Modbus TCP 连接并按最新配置重新监听，是否继续？',
      '立即生效',
      { type: 'warning', confirmButtonText: '继续', cancelButtonText: '取消' }
    )
  } catch { return }

  applying.value = true
  try {
    const r = await postProtocolPortsApply()
    const failed = (r.devices || []).filter(d => !d.started)
    if (failed.length === 0) ElMessage.success(r.message || '协议层已重建')
    else ElMessage.warning(`${failed.length} 个设备启动失败：${failed.map(d => d.name).join('、')}`)
    await reload()
  } catch (e) {
    ElMessage.error(e.message)
    await reload()
  } finally {
    applying.value = false
  }
}

async function resetDefaults() {
  let rebuild = false
  try {
    const action = await ElMessageBox.confirm(
      '将删除手动端口覆盖并恢复配置文件默认值。是否同时立即热重建？',
      '恢复默认',
      {
        type: 'warning',
        confirmButtonText: '恢复并立即生效',
        cancelButtonText: '仅保存',
        distinguishCancelAndClose: true
      }
    )
    rebuild = action === 'confirm'
  } catch (act) {
    if (act === 'cancel') rebuild = false
    else return
  }

  try {
    const r = await postProtocolPortsReset(rebuild)
    ElMessage.success(r.message || '已恢复默认')
    await reload()
  } catch (e) {
    ElMessage.error(e.message)
    await reload()
  }
}

onMounted(reload)
</script>

<style scoped>
.mark-override { color: #e6a23c; margin-right: 8px; }
.mark-shared { color: #409eff; margin-right: 8px; }
.mark-rack { color: #909399; margin-right: 8px; }
.mark-error { color: #f56c6c; display: block; }
:deep(.shared-port-row) { background-color: rgba(64, 158, 255, 0.06); }
</style>
