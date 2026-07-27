<template>
  <div class="app-layout">
    <header class="app-header">
      <span class="title">储能仿真模拟器</span>
      <span class="status">
        <el-tag :type="ready ? 'success' : 'warning'" size="small" effect="dark">
          {{ ready ? '仿真就绪' : '加载中' }}
        </el-tag>
        <el-tag v-if="alert.isActive" type="danger" size="small" effect="dark" style="margin-left:8px">
          严重故障：{{ alert.message }}（{{ alert.secondsUntilExit }}s 后退出）
        </el-tag>
      </span>
    </header>
    <div class="app-body">
      <aside class="app-aside">
        <el-menu :default-active="$route.path" router>
          <el-menu-item index="/mainline"><el-icon><Connection /></el-icon><span>主电气接线</span></el-menu-item>
          <el-menu-item index="/battery"><el-icon><Battery /></el-icon><span>电池堆簇信息</span></el-menu-item>
          <el-menu-item index="/cells"><el-icon><Grid /></el-icon><span>电池单体信息</span></el-menu-item>
          <el-menu-item index="/command"><el-icon><Promotion /></el-icon><span>命令输入</span></el-menu-item>
          <el-menu-item index="/droop-slices"><el-icon><DataAnalysis /></el-icon><span>下垂白盒切片</span></el-menu-item>
          <el-menu-item index="/connections"><el-icon><Link /></el-icon><span>连接信息</span></el-menu-item>
        </el-menu>
      </aside>
      <main class="app-main">
        <router-view />
      </main>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { getHealth, getAlert, getHub } from '@/services/api.js'
import { RealtimeMethods } from '@/services/constants.js'

const ready = ref(false)
const alert = reactive({ isActive: false, message: '', detail: '', secondsUntilExit: 0 })

async function pollHealth() {
  try {
    const h = await getHealth()
    ready.value = !!h.ready
  } catch { /* ignore */ }
}

onMounted(async () => {
  pollHealth()
  setInterval(pollHealth, 3000)

  try {
    const a = await getAlert()
    Object.assign(alert, a)
  } catch { /* ignore */ }

  try {
    const hub = await getHub()
    hub.on(RealtimeMethods.ReceiveAlert, a => Object.assign(alert, a))
  } catch { /* ignore */ }
})
</script>
