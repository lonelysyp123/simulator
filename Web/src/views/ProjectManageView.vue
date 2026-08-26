<template>
  <div class="proj-page">
    <div class="card">
      <div class="head">
        <div>
          <h3 class="card-title">工程配置管理</h3>
          <p class="desc">管理组态工程：新增空工程进入编辑，或打开已有工程修改后保存。</p>
        </div>
        <div class="actions">
          <el-button @click="reload" :loading="loading">刷新</el-button>
          <el-button type="primary" @click="onCreate">新增工程</el-button>
        </div>
      </div>

      <el-table :data="projects" v-loading="loading" stripe border size="small" empty-text="暂无工程，请点击「新增工程」">
        <el-table-column prop="name" label="工程名称" min-width="180" />
        <el-table-column prop="emuCount" label="储能单元" width="90" align="center" />
        <el-table-column prop="pvCount" label="光伏单元" width="90" align="center" />
        <el-table-column prop="nodeCount" label="节点数" width="90" align="center" />
        <el-table-column label="更新时间" width="180">
          <template #default="{ row }">{{ fmtTime(row.updatedAtUtc) }}</template>
        </el-table-column>
        <el-table-column label="状态" width="110">
          <template #default="{ row }">
            <el-tag v-if="row.id === activeProjectId" type="warning" size="small">运行中</el-tag>
            <el-tag v-else type="info" size="small" effect="plain">已保存</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="260" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="onEdit(row)">修改</el-button>
            <el-button link type="primary" @click="onCopy(row)">复制</el-button>
            <el-button link type="danger" @click="onDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <div class="card tip-card">
      <div class="card-title">说明</div>
      <ul class="tips">
        <li><strong>新增</strong>：清空画布并跳转到「组态编辑」，搭建完成后填写名称并保存。</li>
        <li><strong>修改</strong>：将该工程导入组态编辑器，可继续改拓扑与参数；保存时若名称与其他工程冲突会提示是否替换。</li>
        <li><strong>复制</strong>：以该工程为模板生成新工程（新 Id），副本名默认「原名-副本」，重名自动加序号；复制后可直接打开编辑。</li>
        <li><strong>删除</strong>：从工程库移除；若该工程正在系统配置中作为运行模板，将清除激活引用。</li>
        <li>应用到仿真请到「系统配置」开启工程模式并选择工程后确认重启。</li>
      </ul>
    </div>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  getTopologyProjects,
  getSystemConfig,
  postTopologyProjectNew,
  postTopologyProjectOpen,
  postTopologyProjectCopy,
  deleteTopologyProject
} from '@/services/api.js'

const router = useRouter()
const loading = ref(false)
const projects = ref([])
const activeProjectId = ref(null)

function fmtTime(v) {
  if (!v) return '—'
  const d = new Date(v)
  if (Number.isNaN(d.getTime())) return String(v)
  return d.toLocaleString()
}

async function reload() {
  loading.value = true
  try {
    const [list, cfg] = await Promise.all([
      getTopologyProjects(),
      getSystemConfig().catch(() => null)
    ])
    projects.value = list || []
    activeProjectId.value = cfg?.activeProjectId || null
  } catch (e) {
    ElMessage.error(e.message || '加载工程列表失败')
  } finally {
    loading.value = false
  }
}

async function onCreate() {
  try {
    await ElMessageBox.confirm(
      '将清空当前组态画布并创建新工程，是否继续？',
      '新增工程',
      { type: 'warning', confirmButtonText: '创建并编辑', cancelButtonText: '取消' }
    )
  } catch {
    return
  }

  try {
    const p = await postTopologyProjectNew({ name: '未命名工程' })
    ElMessage.success('已创建空工程，请开始搭建拓扑')
    await router.push({ path: '/topology', query: { mode: 'new', id: p.id } })
  } catch (e) {
    ElMessage.error(e.message || '创建失败')
  }
}

async function onEdit(row) {
  try {
    await ElMessageBox.confirm(
      `将工程「${row.name}」导入组态编辑器，当前画布内容会被覆盖，是否继续？`,
      '修改工程',
      { type: 'info', confirmButtonText: '打开编辑', cancelButtonText: '取消' }
    )
  } catch {
    return
  }

  try {
    await postTopologyProjectOpen(row.id)
    ElMessage.success(`已载入工程「${row.name}」`)
    await router.push({ path: '/topology', query: { mode: 'edit', id: row.id } })
  } catch (e) {
    ElMessage.error(e.message || '打开失败')
  }
}

async function onCopy(row) {
  let name
  try {
    const res = await ElMessageBox.prompt(
      `以工程「${row.name}」为模板复制新工程，请输入副本名称：`,
      '复制工程',
      {
        confirmButtonText: '复制',
        cancelButtonText: '取消',
        inputValue: `${row.name}-副本`,
        inputValidator: v => (v && v.trim() ? true : '名称不能为空')
      }
    )
    name = res.value.trim()
  } catch {
    return
  }

  try {
    const copy = await postTopologyProjectCopy(row.id, name)
    ElMessage.success(`已复制为「${copy.name}」`)
    await reload()
    try {
      await ElMessageBox.confirm(
        `是否立即打开副本「${copy.name}」进入组态编辑？当前画布内容会被覆盖。`,
        '复制成功',
        { type: 'success', confirmButtonText: '打开编辑', cancelButtonText: '留在列表' }
      )
    } catch {
      return
    }
    await postTopologyProjectOpen(copy.id)
    await router.push({ path: '/topology', query: { mode: 'edit', id: copy.id } })
  } catch (e) {
    ElMessage.error(e.message || '复制失败')
  }
}

async function onDelete(row) {
  const running = row.id === activeProjectId.value
  try {
    await ElMessageBox.confirm(
      running
        ? `工程「${row.name}」当前为运行模板，删除后将清除激活引用。确定删除？`
        : `确定删除工程「${row.name}」？此操作不可恢复。`,
      '删除工程',
      { type: 'warning', confirmButtonText: '删除', cancelButtonText: '取消' }
    )
  } catch {
    return
  }

  try {
    await deleteTopologyProject(row.id)
    ElMessage.success('已删除')
    await reload()
  } catch (e) {
    ElMessage.error(e.message || '删除失败')
  }
}

onMounted(reload)
</script>

<style scoped>
.proj-page { display: flex; flex-direction: column; gap: 12px; }
.head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 12px;
}
.card-title { margin: 0 0 6px; font-size: 16px; }
.desc { margin: 0; font-size: 13px; color: #606266; }
.actions { display: flex; gap: 8px; flex-shrink: 0; }
.tips {
  margin: 0;
  padding-left: 18px;
  font-size: 13px;
  color: #606266;
  line-height: 1.7;
}
.tip-card .card-title { margin-bottom: 8px; }
</style>
