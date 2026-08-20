<template>
  <div class="sys-page">
    <div class="card">
      <h3 class="card-title">系统配置 · 仿真来源</h3>
      <p class="desc">
        选择模拟器设备拓扑来源：默认使用 <code>appsettings.json</code>，
        或开启工程模式后从已保存的组态工程生成并重新初始化。
      </p>

      <el-form label-width="140px" size="default" class="form" :disabled="applying">
        <el-form-item label="工程模式">
          <el-switch
            v-model="engineeringMode"
            active-text="开启"
            inactive-text="关闭"
            :disabled="applying"
            @change="onModeChange"
          />
        </el-form-item>

        <el-form-item label="工程模板" required>
          <el-select
            v-model="projectId"
            placeholder="请选择已保存的组态工程"
            filterable
            clearable
            style="width: 360px"
            :disabled="!engineeringMode || applying"
          >
            <el-option
              v-for="p in projects"
              :key="p.id"
              :label="`${p.name}（储能×${p.emuCount} · 光伏×${p.pvCount ?? 0} · 节点 ${p.nodeCount}）`"
              :value="p.id"
            />
          </el-select>
          <el-button link type="primary" style="margin-left:8px" :disabled="applying" @click="reload">刷新列表</el-button>
        </el-form-item>

        <el-form-item label="当前运行">
          <el-tag :type="state.source === 'topology' ? 'warning' : 'info'" size="small">
            {{ state.source === 'topology' ? '组态工程' : 'appsettings.json' }}
          </el-tag>
          <span class="meta">储能单元 {{ state.runtimeUnitCount }}</span>
          <span class="meta">光伏单元 {{ state.runtimePvUnitCount }}</span>
          <span v-if="state.activeProjectName" class="meta">工程：{{ state.activeProjectName }}</span>
        </el-form-item>

        <el-form-item>
          <el-button type="primary" :loading="applying" :disabled="applying" @click="apply">确认并重新初始化</el-button>
        </el-form-item>
      </el-form>

      <el-alert
        v-if="previewNotes.length"
        type="info"
        :closable="false"
        show-icon
        title="上次应用摘要"
        style="margin-top:12px"
      >
        <ul class="notes">
          <li v-for="(n, i) in previewNotes" :key="i">{{ n }}</li>
        </ul>
      </el-alert>
    </div>

    <div class="card tip-card">
      <div class="card-title">说明</div>
      <ul class="tips">
        <li>请先在「工程管理 / 组态编辑」中搭建并<strong>保存工程</strong>，再回到本页选择。</li>
        <li>确认后将按工程中的 EMU 数量生成储能单元，并重启后端以重建设备与 Modbus 端口。</li>
        <li>电站概览图会随单元数量自动更新；当前仍按标准径向接线展开（220→35→690）。</li>
        <li>重启期间页面会短暂不可用，开发模式下 <code>dev-up.sh</code> 会自动拉起后端。</li>
      </ul>
    </div>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getSystemConfig, postSystemApply, getHealth } from '@/services/api.js'
import { lockSystem, unlockSystem, updateSystemProgress } from '@/services/systemLock.js'

const router = useRouter()
const engineeringMode = ref(false)
const projectId = ref(null)
const projects = ref([])
const applying = ref(false)
const previewNotes = ref([])
const state = reactive({
  source: 'appsettings',
  runtimeUnitCount: 0,
  runtimePvUnitCount: 0,
  activeProjectName: '',
  activeProjectId: null
})

function onModeChange(on) {
  if (!on) projectId.value = null
}

async function reload() {
  const cfg = await getSystemConfig()
  engineeringMode.value = !!cfg.engineeringMode
  projectId.value = cfg.activeProjectId || null
  projects.value = cfg.projects || []
  state.source = cfg.source || 'appsettings'
  state.runtimeUnitCount = cfg.runtimeUnitCount || 0
  state.runtimePvUnitCount = cfg.runtimePvUnitCount || 0
  state.activeProjectName = cfg.activeProjectName || ''
  state.activeProjectId = cfg.activeProjectId || null
  previewNotes.value = cfg.overlaySummary?.notes || []
}

/**
 * 等待后端重启。进度区间约 35%→90%：
 * - 等待掉线：35→50
 * - 等待就绪：50→90（按超时时间估算）
 */
async function waitBackendReady(timeoutMs = 90000) {
  const start = Date.now()
  updateSystemProgress(38, '正在停止当前仿真进程…', '等待后端下线')

  for (let i = 0; i < 40; i++) {
    try {
      await getHealth()
      updateSystemProgress(
        38 + Math.min(12, Math.round((i / 40) * 12)),
        '正在停止当前仿真进程…',
        '等待后端下线'
      )
      await new Promise(r => setTimeout(r, 300))
    } catch {
      break
    }
  }

  updateSystemProgress(52, '后端已停止，正在重新拉起…', '等待后端就绪')
  while (Date.now() - start < timeoutMs) {
    const elapsed = Date.now() - start
    // 下线后到就绪：52 → 90，按时间缓慢推进（到 88 封顶，真正 ready 再到 92）
    const t = Math.min(1, elapsed / Math.max(timeoutMs * 0.45, 1))
    const p = 52 + Math.round(t * 36)
    updateSystemProgress(
      Math.min(88, p),
      '正在等待模拟器就绪（设备与 Modbus 重建中）…',
      '等待后端就绪'
    )
    try {
      const h = await getHealth()
      if (h?.status === 'ok') {
        updateSystemProgress(92, '后端已就绪，正在同步配置…', '同步配置')
        return true
      }
    } catch { /* still down */ }
    await new Promise(r => setTimeout(r, 800))
  }
  return false
}

async function apply() {
  if (engineeringMode.value && !projectId.value) {
    ElMessage.warning('请先选择组态工程模板')
    return
  }

  const modeText = engineeringMode.value
    ? `使用工程「${projects.value.find(p => p.id === projectId.value)?.name || projectId.value}」重新初始化`
    : '关闭工程模式，恢复 appsettings.json'

  try {
    await ElMessageBox.confirm(
      `${modeText}。\n当前运行状态（SOC、断路器等）将丢失，后端将重启，是否继续？`,
      '确认应用到仿真',
      { type: 'warning', confirmButtonText: '确认并重启', cancelButtonText: '取消' }
    )
  } catch {
    return
  }

  applying.value = true
  lockSystem('正在提交系统配置…', 8, '提交配置')
  try {
    updateSystemProgress(18, '正在写入工程配置 / overlay…', '应用配置')
    const res = await postSystemApply({
      engineeringMode: engineeringMode.value,
      projectId: engineeringMode.value ? projectId.value : null,
      confirmRestart: true
    })
    if (!res.ok) {
      ElMessage.error(res.message || '应用失败')
      return
    }
    previewNotes.value = res.details || res.overlay?.notes || []
    updateSystemProgress(35, res.message || '配置已提交，准备重启后端…', '准备重启')

    if (res.restarting) {
      const ok = await waitBackendReady()
      if (!ok) {
        ElMessage.error('等待后端重启超时，请检查终端或手动重启')
        return
      }
      updateSystemProgress(96, '正在刷新界面状态…', '完成收尾')
      await reload()
      updateSystemProgress(100, '模拟器已按新配置就绪', '完成')
      ElMessage.success('模拟器已按新配置就绪')
      await new Promise(r => setTimeout(r, 350))
      unlockSystem()
      applying.value = false
      router.push('/mainline')
      return
    }
    updateSystemProgress(100, '配置已更新', '完成')
    await reload()
  } catch (e) {
    // 重启瞬间请求可能被掐断，转入轮询
    if (String(e.message || '').includes('Network') || String(e.message || '').includes('ECONN')) {
      updateSystemProgress(40, '连接已中断，正在等待后端重启…', '等待后端重启')
      const ok = await waitBackendReady()
      if (ok) {
        updateSystemProgress(96, '正在刷新界面状态…', '完成收尾')
        await reload()
        updateSystemProgress(100, '模拟器已重启', '完成')
        ElMessage.success('模拟器已重启')
        await new Promise(r => setTimeout(r, 350))
        unlockSystem()
        applying.value = false
        router.push('/mainline')
        return
      }
    }
    ElMessage.error(e.message || '应用失败')
  } finally {
    applying.value = false
    unlockSystem()
  }
}

onMounted(async () => {
  try {
    await reload()
  } catch (e) {
    ElMessage.error(e.message || '加载系统配置失败')
  }
})
</script>

<style scoped>
.sys-page { max-width: 860px; }
.desc { font-size: 13px; color: #606266; line-height: 1.6; margin: 0 0 16px; }
.desc code { background: #f4f4f5; padding: 1px 6px; border-radius: 3px; }
.form { max-width: 640px; }
.meta { margin-left: 12px; font-size: 13px; color: #909399; }
.notes { margin: 6px 0 0; padding-left: 18px; font-size: 12px; color: #606266; }
.tip-card .tips { margin: 0; padding-left: 18px; font-size: 13px; color: #606266; line-height: 1.7; }
.tip-card code { background: #f4f4f5; padding: 1px 6px; border-radius: 3px; }
</style>
