<template>
  <div>
    <div class="card">
      <p class="card-title">
        实时日志
        <el-button style="float:right" size="small" @click="clear">清空</el-button>
      </p>
      <div ref="logEl" class="log-view">
        <div v-for="(l, i) in logs" :key="i" class="log-line">
          <span class="level-INFO">[{{ formatTime(l.timestamp) }}]</span>
          <span :class="`level-${l.level}`">[{{ l.level }}]</span>
          <span class="level-INFO">[{{ l.logger }}]</span>
          {{ l.message }}
          <div v-if="l.exception" class="level-ERROR">{{ l.exception }}</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount, nextTick } from 'vue'
import { getHub } from '@/services/api.js'
import { RealtimeMethods, RealtimeChannels } from '@/services/constants.js'

const logs = ref([])
const logEl = ref(null)
let hub = null
const MAX = 1000

function formatTime(t) {
  if (!t) return ''
  try { return new Date(t).toLocaleTimeString('zh-CN', { hour12: false }) } catch { return String(t) }
}

function pushLog(l) {
  logs.value.push(l)
  if (logs.value.length > MAX) logs.value.splice(0, logs.value.length - MAX)
  nextTick(() => { if (logEl.value) logEl.value.scrollTop = logEl.value.scrollHeight })
}

function clear() { logs.value = [] }

onMounted(async () => {
  try {
    hub = await getHub()
    await hub.invoke('JoinChannel', RealtimeChannels.Logs)
    hub.on(RealtimeMethods.ReceiveLog, pushLog)
  } catch (e) { console.warn(e) }
})

onBeforeUnmount(() => {
  if (hub) {
    hub.off(RealtimeMethods.ReceiveLog, pushLog)
    try { hub.invoke('LeaveChannel', RealtimeChannels.Logs) } catch { /* ignore */ }
  }
})
</script>
