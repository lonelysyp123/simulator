<template>
  <div>
    <div class="card">
      <div class="card-title" style="display:flex;align-items:center;justify-content:space-between;gap:12px">
        <span>IEC 61850</span>
        <span style="display:flex;gap:8px;align-items:center">
          <el-tag :type="paused ? 'warning' : 'success'" size="small">{{ paused ? '已暂停' : '实时' }}</el-tag>
          <el-button size="small" @click="paused = !paused">{{ paused ? '继续' : '暂停' }}</el-button>
          <el-button size="small" @click="clearMessages">清空报文</el-button>
          <el-button size="small" text @click="reload">刷新</el-button>
        </span>
      </div>
      <el-alert
        type="info"
        :closable="false"
        show-icon
        style="margin-bottom:8px"
        title="仿真设备 = MMS 服务端 + GOOSE 订户。先点选上方 IED，下方只显示该台报文。端口开关请到「协议端口」。"
      />
    </div>

    <div class="card">
      <p class="card-title">IED 总览（点选）</p>
      <el-table
        :data="devices"
        size="small"
        border
        stripe
        highlight-current-row
        :row-class-name="iedRowClass"
        @current-change="onSelectIed"
      >
        <el-table-column prop="serverName" label="服务" width="120" />
        <el-table-column prop="iedName" label="IED" width="130" />
        <el-table-column prop="port" label="MMS" width="90" />
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="row.online ? 'success' : 'danger'" size="small">{{ row.online ? '在线' : '离线' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="associatedClients" label="客户端" width="80" />
        <el-table-column prop="gooseInterface" label="网卡" width="90" />
        <el-table-column label="GOOSE" width="130" align="center">
          <template #default="{ row }">
            <el-tag :type="row.gooseSubscribing ? 'success' : 'info'" size="small" :title="row.gooseSubscribeSkip || ''">
              {{ row.gooseSubscribing ? formatAppId(row.gooseSubscribeAppId) : '关' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="stNum" width="90">
          <template #default="{ row }">{{ row.lastGooseStNum ?? '—' }}</template>
        </el-table-column>
        <el-table-column label="最近">
          <template #default="{ row }">{{ formatLocalFromUtc(row.lastGooseUtc) }}</template>
        </el-table-column>
      </el-table>
    </div>

    <div v-if="selected" class="card">
      <p class="card-title">选中：{{ selected.iedName }}</p>
      <div class="muted" style="font-size:13px;line-height:1.7;margin-bottom:8px">
        <div>AppID：{{ formatAppId(selected.gooseSubscribeAppId) }} · GoCbRef：{{ selected.gooseSubscribeGoCbRef || '—' }}</div>
        <div>网卡：{{ selected.gooseInterface || '—' }}{{ selected.gooseSubscribeSkip ? ` · 订阅跳过：${selected.gooseSubscribeSkip}` : '' }}</div>
        <div>
          入向点序：
          <span v-for="p in (selected.goosePoints || [])" :key="p.paramName" style="margin-right:10px">
            {{ p.index }} {{ p.paramName }}{{ p.description ? `(${p.description})` : '' }}
          </span>
        </div>
      </div>

      <el-tabs v-model="tab">
        <el-tab-pane label="GOOSE" name="goose" />
        <el-tab-pane label="MMS" name="mms" />
      </el-tabs>

      <div v-if="tab === 'mms'" class="muted" style="padding:12px 0">
        MMS 交互报文将在后续版本提供（P1）。当前仅支持 GOOSE 入向与系统事件。
      </div>

      <template v-else>
        <div style="display:flex;gap:8px;margin-bottom:8px;flex-wrap:wrap;align-items:center">
          <el-radio-group v-model="resultFilter" size="small">
            <el-radio-button value="all">全部结果</el-radio-button>
            <el-radio-button value="applied">仅成功</el-radio-button>
          </el-radio-group>
          <span class="muted" style="font-size:12px">共 {{ filteredMessages.length }} 条（内存环）</span>
        </div>

        <el-table
          :data="filteredMessages"
          size="small"
          border
          stripe
          row-key="id"
          @row-click="onRowClick"
        >
          <el-table-column prop="localTime" label="时间" width="120" />
          <el-table-column label="方向" width="90">
            <template #default="{ row }">{{ directionLabel(row) }}</template>
          </el-table-column>
          <el-table-column prop="summary" label="摘要" min-width="280" show-overflow-tooltip />
          <el-table-column label="结果" width="140">
            <template #default="{ row }">
              <el-tag :type="resultTagType(row.result)" size="small">{{ resultLabel(row.result) }}</el-tag>
            </template>
          </el-table-column>
        </el-table>

        <div v-if="expanded" class="card" style="margin-top:12px;background:#fafafa">
          <p class="card-title">报文详情 #{{ expanded.id }}</p>
          <el-descriptions :column="2" size="small" border>
            <el-descriptions-item label="时间">{{ expanded.localTime }} ({{ expanded.utc }})</el-descriptions-item>
            <el-descriptions-item label="结果">{{ expanded.result }}</el-descriptions-item>
            <el-descriptions-item label="AppID">{{ formatAppId(expanded.appId) }}</el-descriptions-item>
            <el-descriptions-item label="GoCbRef">{{ expanded.goCbRef || '—' }}</el-descriptions-item>
            <el-descriptions-item label="stNum">{{ expanded.stNum ?? '—' }}</el-descriptions-item>
            <el-descriptions-item label="sqNum">{{ expanded.sqNum ?? '—' }}</el-descriptions-item>
            <el-descriptions-item label="test">{{ expanded.isTest ? 'true' : 'false' }}</el-descriptions-item>
            <el-descriptions-item label="摘要" :span="2">{{ expanded.summary }}</el-descriptions-item>
          </el-descriptions>
          <div v-if="expanded.writes" style="margin-top:10px">
            <p style="font-weight:600;margin:0 0 6px">写入</p>
            <el-table :data="dictRows(expanded.writes)" size="small" border>
              <el-table-column prop="key" label="ParamName" width="120" />
              <el-table-column prop="value" label="值" />
            </el-table>
          </div>
          <div v-if="expanded.values" style="margin-top:10px">
            <p style="font-weight:600;margin:0 0 6px">数据集解码</p>
            <el-table :data="dictRows(expanded.values)" size="small" border>
              <el-table-column prop="key" label="ParamName" width="120" />
              <el-table-column prop="value" label="值" />
            </el-table>
          </div>
        </div>
      </template>
    </div>

    <div v-else class="card muted">请先在上方表格点选一台 IED。</div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import {
  getIec61850, getIec61850Messages, clearIec61850Messages,
  joinHubChannel, leaveHubChannel, onHubMethod
} from '@/services/api.js'
import { RealtimeMethods, RealtimeChannels } from '@/services/constants.js'
import { ElMessage } from 'element-plus'

const devices = ref([])
const selectedKey = ref('')
const messages = ref([])
const tab = ref('goose')
const resultFilter = ref('all')
const paused = ref(false)
const expanded = ref(null)
const maxKeep = 1000

const selected = computed(() =>
  devices.value.find(d => d.serverName === selectedKey.value) || null
)

const filteredMessages = computed(() => {
  let list = messages.value
  if (selectedKey.value)
    list = list.filter(m => m.server === selectedKey.value || m.iedName === selected.value?.iedName)
  // GOOSE tab：goose + system（与本 IED 相关或无 server 的全局系统事件）
  list = list.filter(m => m.protocol === 'goose' || m.protocol === 'system')
  if (resultFilter.value === 'applied')
    list = list.filter(m => m.result === 'applied')
  return list
})

function formatAppId(id) {
  if (id == null || id === '') return '—'
  return '0x' + Number(id).toString(16).toUpperCase().padStart(4, '0')
}

function formatLocalFromUtc(utc) {
  if (!utc) return '—'
  const d = new Date(utc)
  if (Number.isNaN(d.getTime())) return '—'
  const pad = (n, w = 2) => String(n).padStart(w, '0')
  return `${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}.${pad(d.getMilliseconds(), 3)}`
}

function directionLabel(row) {
  if (row.direction === 'system' || row.protocol === 'system') return '系统'
  return '← 入向'
}

function resultTagType(result) {
  if (result === 'applied') return 'success'
  if (result === 'system') return 'info'
  if (String(result || '').startsWith('skip:')) return 'warning'
  if (result === 'error') return 'danger'
  return 'info'
}

function resultLabel(result) {
  if (result === 'applied') return '已应用'
  if (result === 'system') return '系统'
  if (result === 'skip:test') return '丢弃 test'
  if (result === 'skip:stNum') return '忽略 stNum'
  if (String(result || '').startsWith('skip:')) return result
  return result || '—'
}

function dictRows(obj) {
  return Object.entries(obj || {}).map(([key, value]) => ({ key, value: String(value) }))
}

function iedRowClass({ row }) {
  return row.serverName === selectedKey.value ? 'current-ied-row' : ''
}

function onSelectIed(row) {
  if (!row) return
  selectedKey.value = row.serverName
  expanded.value = null
  loadMessages()
}

function onRowClick(row) {
  expanded.value = row
}

function prependMessage(msg) {
  if (paused.value) return
  if (messages.value.some(m => m.id === msg.id)) return
  messages.value = [msg, ...messages.value].slice(0, maxKeep)
}

async function reload() {
  try {
    const data = await getIec61850()
    devices.value = data.devices || []
    if (!selectedKey.value && devices.value.length)
      selectedKey.value = devices.value[0].serverName
    else if (selectedKey.value && !devices.value.some(d => d.serverName === selectedKey.value))
      selectedKey.value = devices.value[0]?.serverName || ''
    await loadMessages()
  } catch (e) {
    ElMessage.error(e.message)
  }
}

async function loadMessages() {
  try {
    const data = await getIec61850Messages(selectedKey.value || undefined, 200)
    messages.value = data.messages || []
  } catch (e) {
    console.warn(e)
  }
}

async function clearMessages() {
  try {
    await clearIec61850Messages()
    messages.value = []
    expanded.value = null
    ElMessage.success('已清空')
  } catch (e) {
    ElMessage.error(e.message)
  }
}

onMounted(async () => {
  await reload()
  try {
    onHubMethod(RealtimeMethods.ReceiveIec61850Message, msg => {
      prependMessage(msg)
      // 轻量刷新 IED 行上的 stNum
      if (msg.result === 'applied' && msg.server) {
        const d = devices.value.find(x => x.serverName === msg.server)
        if (d && msg.stNum != null) {
          d.lastGooseStNum = msg.stNum
          d.lastGooseUtc = msg.utc
        }
      }
    })
    await joinHubChannel(RealtimeChannels.Iec61850)
  } catch { /* ignore */ }
})

onBeforeUnmount(() => {
  try { leaveHubChannel(RealtimeChannels.Iec61850) } catch { /* ignore */ }
})
</script>

<style scoped>
.muted { color: #909399; }
:deep(.current-ied-row) { --el-table-tr-bg-color: #ecf5ff; }
</style>
