<template>
  <div class="topo-page">
    <div class="card toolbar">
      <div class="left">
        <el-input v-model="project.name" size="small" style="width:200px" placeholder="工程名称" @change="markDirty" />
        <el-button type="primary" size="small" :loading="saving" @click="saveProject">保存工程</el-button>
        <el-button size="small" @click="reload">重新加载</el-button>
        <el-button size="small" :disabled="!canUndo" @click="undo" title="Ctrl+Z">撤销</el-button>
        <el-button size="small" :disabled="!canRedo" @click="redo" title="Ctrl+Shift+Z">重做</el-button>
        <el-button size="small" type="danger" plain :disabled="!canDelete" @click="deleteSelected">删除选中</el-button>
        <el-button size="small" :disabled="!selectedNode" @click="openSaveLibrary">存入设备库</el-button>
        <el-button size="small" type="success" plain @click="wizardOpen = true">标准拓扑向导</el-button>
        <el-button size="small" link type="primary" @click="goProjectManage">工程管理</el-button>
      </div>
      <div class="right">
        <el-tag v-if="dirty" size="small" type="warning" style="margin-right:6px">未保存</el-tag>
        <el-tag v-if="editHint" size="small" type="success" style="margin-right:6px">{{ editHint }}</el-tag>
        <el-tag size="small" type="info">节点 {{ project.nodes.length }}</el-tag>
        <el-tag size="small" type="info" style="margin-left:6px">连线 {{ project.edges.length }}</el-tag>
      </div>
    </div>

    <div class="workspace">
      <aside class="palette card">
        <div class="card-title">基础模板</div>
        <div
          v-for="t in templates"
          :key="t.id"
          class="palette-item"
          draggable="true"
          @dragstart="onDragTemplate($event, t)"
          @dblclick="addFromTemplate(t)"
        >
          <span class="dot" :style="{ background: colorOf(t.id) }" />
          <div class="meta">
            <div class="name">{{ t.name }}</div>
            <div class="desc">{{ t.category }}</div>
          </div>
        </div>

        <div class="card-title" style="margin-top:14px">EMU 储能单元</div>
        <div v-if="!emuNodes.length" class="empty">拖入「EMU 储能单元」模板，PCS 通过参数下拉框归入</div>
        <div
          v-for="e in emuNodes"
          :key="e.id"
          class="palette-item"
          :class="{ active: selectedNodeId === e.id }"
          @click="onSelectNode(e.id)"
        >
          <span class="dot" :style="{ background: colorOf('emu') }" />
          <div class="meta">
            <div class="name">{{ e.label }}</div>
            <div class="desc">PCS×{{ pcsCountOfEmu(e.id) }}</div>
            <div class="desc">断路器：{{ boundDeviceLabel(e.id, 'ac_breaker') || '未绑定' }}</div>
            <div class="desc">电表：{{ boundDeviceLabel(e.id, 'ac_meter') || '未绑定' }}</div>
            <div
              v-for="g in groupsOfEmu(e.id)"
              :key="g.id"
              class="desc emu-group-row"
              :class="{ active: selectedNodeId === g.id }"
              @click.stop="onSelectNode(g.id)"
            >└ {{ g.label }} · PCS×{{ pcsCountOfGroup(g.id) }}</div>
          </div>
          <el-button link type="danger" size="small" @click.stop="deleteEmu(e.id)">删</el-button>
        </div>

        <div class="card-title" style="margin-top:14px">设备库</div>
        <div v-if="!library.length" class="empty">改参后「存入设备库」</div>
        <div
          v-for="item in library"
          :key="item.id"
          class="palette-item"
          draggable="true"
          @dragstart="onDragLibrary($event, item)"
          @dblclick="addFromLibrary(item)"
        >
          <span class="dot" :style="{ background: colorOf(item.templateId) }" />
          <div class="meta">
            <div class="name">{{ item.name }}</div>
            <div class="desc">{{ templateName(item.templateId) }}</div>
          </div>
          <el-button link type="danger" size="small" @click.stop="removeLibrary(item.id)">删</el-button>
        </div>
      </aside>

      <div
        class="canvas-wrap card"
        @dragover.prevent="ev => { ev.dataTransfer.dropEffect = 'copy' }"
        @drop.prevent="onDrop"
      >
        <TopologyCanvas
          ref="canvasRef"
          :nodes="project.nodes"
          :edges="project.edges"
          :templates="templates"
          :selected-node-id="selectedNodeId"
          :selected-edge-id="selectedEdgeId"
          :linking="linking"
          :pointer-world="pointerWorld"
          :problem-node-ids="problemNodeIds"
          :snap="true"
          @select-node="onSelectNode"
          @select-edge="onSelectEdge"
          @port-click="onPortClick"
          @move-node="onMoveNode"
          @pointer-world="w => pointerWorld = w"
        />
        <div v-if="linking" class="linking-tip">连线中…再点目标拐角（Esc 取消）</div>
      </div>

      <aside class="props card">
        <div v-if="validationIssues.length" class="validation-box">
          <div class="card-title">校验问题</div>
          <el-alert
            :title="validationMessage || '工程配置不合理'"
            type="error"
            :closable="true"
            show-icon
            @close="clearValidation"
          >
            <ul class="issue-list">
              <li
                v-for="(issue, i) in validationIssues"
                :key="i"
                class="issue-item"
                :class="{ clickable: !!issue.nodeId }"
                @click="focusProblem(issue.nodeId)"
              >
                {{ issue.text }}
              </li>
            </ul>
          </el-alert>
        </div>

        <div class="card-title">属性</div>
        <template v-if="selectedNode && selectedTemplate">
          <el-form label-position="top" size="small">
            <el-form-item label="显示名称">
              <el-input v-model="selectedNode.label" @change="onParamEdited" />
            </el-form-item>
            <el-form-item label="模板">
              <el-tag size="small">{{ selectedTemplate.name }}</el-tag>
            </el-form-item>
            <el-divider content-position="left">参数</el-divider>
            <el-form-item
              v-for="def in selectedTemplate.parameters"
              :key="def.key"
              :label="def.unit ? `${def.label} (${def.unit})` : def.label"
            >
              <el-input-number
                v-if="def.type === 'number'"
                v-model="selectedNode.parameters[def.key]"
                v-bind="numberInputBounds(def)"
                :step="numberStep(def)"
                controls-position="right"
                style="width:100%"
                @change="onParamEdited"
              />
              <el-switch
                v-else-if="def.type === 'boolean'"
                :model-value="!!selectedNode.parameters[def.key]"
                @change="v => onBoolParamChange(def.key, v)"
              />
              <el-select
                v-else-if="def.type === 'emu_select'"
                :model-value="selectedNode.parameters[def.key] || ''"
                placeholder="选择 EMU 储能单元"
                style="width:100%"
                @change="v => onEmuParamChange(def.key, v)"
              >
                <el-option v-for="e in emuNodes" :key="e.id" :label="e.label" :value="e.id" />
              </el-select>
              <el-select
                v-else-if="def.type === 'group_select'"
                :model-value="selectedNode.parameters[def.key] || ''"
                placeholder="选择 EMU 分组（可选）"
                clearable
                style="width:100%"
                @change="v => onGroupParamChange(def.key, v)"
              >
                <el-option v-for="g in groupOptionsForSelected" :key="g.id" :label="g.label" :value="g.id" />
              </el-select>
              <el-input
                v-else
                v-model="selectedNode.parameters[def.key]"
                @change="onParamEdited"
              />
              <div v-if="def.description" class="param-hint">{{ def.description }}</div>
            </el-form-item>
          </el-form>
          <el-alert
            v-if="selectedTemplate.description"
            :title="selectedTemplate.description"
            type="info"
            :closable="false"
            show-icon
            style="margin-top:8px"
          />
        </template>
        <template v-else-if="selectedEdgeId">
          <p class="empty">已选中连线，按 Delete 可断开。</p>
          <el-button size="small" type="danger" @click="deleteSelected">断开连线</el-button>
        </template>
        <p v-else class="empty">从左侧拖入设备，或用「标准拓扑向导」一键生成储能 / 光伏径向骨架。</p>
      </aside>
    </div>

    <el-dialog v-model="libDialog" title="存入设备库" width="420px">
      <el-form label-width="80px" size="small">
        <el-form-item label="名称">
          <el-input v-model="libName" placeholder="如：1250kW PCS 单元 / 大容量 BMS" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button size="small" @click="libDialog = false">取消</el-button>
        <el-button size="small" type="primary" :loading="savingLib" @click="saveLibrary">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="wizardOpen" title="标准拓扑向导" width="480px">
      <p class="wizard-desc">生成电网→主断→220kV 母线→主变→35kV 母线→储能 EMU 和/或光伏单元的径向骨架，每个 EMU 默认含 2 台 PCS，并自动三相/直流成组连线。EMU 与光伏单元至少填 1 个。</p>
      <el-form label-width="110px" size="small">
        <el-form-item label="工程名称">
          <el-input v-model="wizardName" :placeholder="wizardNamePlaceholder" />
        </el-form-item>
        <el-form-item label="EMU 单元数">
          <el-input-number v-model="wizardEmuCount" :min="0" :max="20" controls-position="right" />
        </el-form-item>
        <el-form-item label="光伏单元数">
          <el-input-number v-model="wizardPvCount" :min="0" :max="20" controls-position="right" />
        </el-form-item>
        <el-form-item label="站用负载">
          <el-switch v-model="wizardIncludeLoad" active-text="包含" inactive-text="不含" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button size="small" @click="wizardOpen = false">取消</el-button>
        <el-button
          size="small"
          type="primary"
          :loading="wizardLoading"
          :disabled="wizardEmuCount + wizardPvCount < 1"
          @click="applyWizard"
        >生成到画布</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import TopologyCanvas from '@/components/topology/TopologyCanvas.vue'
import { nodeSize, snapToGrid, templateColor } from '@/components/topology/nodeLayout.js'
import {
  getTopologyTemplates,
  getTopologyProject,
  putTopologyProject,
  postTopologyConnect,
  postTopologyDisconnect,
  getTopologyLibrary,
  putTopologyLibrary,
  deleteTopologyLibrary,
  checkTopologyProjectName,
  postTopologyValidate,
  postTopologyScaffold
} from '@/services/api.js'

const route = useRoute()
const router = useRouter()
const editHint = ref('')

const templates = ref([])
const library = ref([])
const project = reactive({
  schemaVersion: '1.0',
  id: 'current',
  name: '未命名组态',
  nodes: [],
  edges: []
})

const selectedNodeId = ref(null)
const selectedEdgeId = ref(null)
const linking = ref(null)
const pointerWorld = ref(null)
const saving = ref(false)
const libDialog = ref(false)
const libName = ref('')
const savingLib = ref(false)
const canvasRef = ref(null)
const connecting = ref(false)
let connectSeq = 0

const dirty = ref(false)
const problemNodeIds = ref([])
const validationMessage = ref('')
const validationIssues = ref([])
const historyPast = ref([])
const historyFuture = ref([])
const HISTORY_MAX = 40
let applyingHistory = false

const wizardOpen = ref(false)
const wizardEmuCount = ref(2)
const wizardPvCount = ref(0)
const wizardIncludeLoad = ref(true)
const wizardName = ref('')
const wizardLoading = ref(false)

const wizardNamePlaceholder = computed(() => {
  const e = wizardEmuCount.value
  const p = wizardPvCount.value
  if (p > 0 && e <= 0) return `标准径向-光伏${p}单元`
  if (p > 0) return `标准径向-储能${e}/光伏${p}`
  return `标准径向-${Math.max(1, e)}单元`
})

/** 清空队列后只展示当前一条，避免连线过快时提示堆积/延后爆发 */
function showConnectFeedback(type, title, detail = '') {
  ElMessage.closeAll()
  if (type === 'success') return
  const text = detail ? `${title}：${detail}` : title
  ElMessage({
    type: type === 'error' ? 'error' : 'info',
    message: text,
    duration: 4500,
    showClose: true,
    grouping: false,
    offset: 72,
    appendTo: typeof document !== 'undefined' ? document.body : undefined
  })
}

function dropWorldPosition(ev, templateId) {
  const size = nodeSize(templateId)
  const world = canvasRef.value?.clientToWorld?.(ev.clientX, ev.clientY)
  if (!world) {
    return {
      x: snapToGrid(80 + project.nodes.length * 24),
      y: snapToGrid(80 + (project.nodes.length % 5) * 40)
    }
  }
  return {
    x: snapToGrid(world.x - size.w / 2),
    y: snapToGrid(world.y - size.h / 2)
  }
}

/** 编辑期仅拦结构错误；电气规则在保存时统一校验 */
function quickReject(edge) {
  const fromNode = project.nodes.find(n => n.id === edge.fromNodeId)
  const toNode = project.nodes.find(n => n.id === edge.toNodeId)
  if (!fromNode || !toNode) return { code: 'NODE_MISSING', message: '连线端点设备不存在' }
  if (fromNode.id === toNode.id) return { code: 'SELF_LINK', message: '不能将设备连接到自身' }
  const fromTpl = templates.value.find(t => t.id === fromNode.templateId)
  const toTpl = templates.value.find(t => t.id === toNode.templateId)
  const fromPort = fromTpl?.ports?.find(p => p.id === edge.fromPortId)
  const toPort = toTpl?.ports?.find(p => p.id === edge.toPortId)
  if (!fromPort || !toPort) return { code: 'PORT_MISSING', message: '拐角（端口）不存在' }
  return null
}

const selectedNode = computed(() => project.nodes.find(n => n.id === selectedNodeId.value) || null)
const selectedTemplate = computed(() => templates.value.find(t => t.id === selectedNode.value?.templateId) || null)
const canDelete = computed(() => !!(selectedNodeId.value || selectedEdgeId.value))
const canUndo = computed(() => historyPast.value.length > 0)
const canRedo = computed(() => historyFuture.value.length > 0)

/** EMU 虚拟节点列表（画布不渲染，仅侧栏管理） */
const emuNodes = computed(() => project.nodes.filter(n => n.templateId === 'emu'))

function pcsCountOfEmu(emuId) {
  return project.nodes.filter(n => n.templateId === 'pcs' && n.parameters?.emuId === emuId).length
}

/** 某 EMU 下的分组虚拟节点（画布不渲染，仅侧栏管理） */
function groupsOfEmu(emuId) {
  return project.nodes.filter(n => n.templateId === 'emu_group' && n.parameters?.emuId === emuId)
}

function pcsCountOfGroup(groupId) {
  return project.nodes.filter(n => n.templateId === 'pcs' && n.parameters?.groupId === groupId).length
}

/** 当前选中节点的 EMU 分组候选：仅列其所属 EMU 下的分组（未选 EMU 时无候选） */
const groupOptionsForSelected = computed(() => {
  const n = selectedNode.value
  if (!n || n.templateId === 'emu_group') return []
  return groupsOfEmu(n.parameters?.emuId || '')
})

/** 归入某 EMU 的设备节点（PCS / 断路器 / 电表） */
function devicesOfEmu(emuId) {
  return project.nodes.filter(n => ['pcs', 'ac_breaker', 'ac_meter'].includes(n.templateId) && n.parameters?.emuId === emuId)
}

/** EMU 绑定的断路器/电表展示名（排除组级绑定；未绑定返回空串） */
function boundDeviceLabel(emuId, templateId) {
  const n = project.nodes.find(x => x.templateId === templateId && x.parameters?.emuId === emuId && !x.parameters?.groupId)
  if (!n) return ''
  return n.label || n.parameters?.name || n.id
}

function onEmuParamChange(key, value) {
  if (!selectedNode.value) return
  pushHistory()
  selectedNode.value.parameters[key] = value || ''
  // 切换所属 EMU 后原分组必然失效，同步清空 groupId
  if (key === 'emuId' && 'groupId' in selectedNode.value.parameters)
    selectedNode.value.parameters.groupId = ''
  clearValidation()
}

function onGroupParamChange(key, value) {
  if (!selectedNode.value) return
  pushHistory()
  selectedNode.value.parameters[key] = value || ''
  clearValidation()
}

/** 解除该 EMU 下全部设备（pcs/ac_breaker/ac_meter）的 emuId/groupId 归属 */
function unassignDevicesFromEmu(emuId) {
  for (const n of devicesOfEmu(emuId)) {
    n.parameters.emuId = ''
    if ('groupId' in n.parameters) n.parameters.groupId = ''
  }
}

function deleteEmu(id) {
  pushHistory()
  // 同步清理指向该 EMU 的连线（旧工程 EMU 可能带 AC/DC 连线），避免保存回放报「连线端点设备不存在」
  project.edges = project.edges.filter(e => e.fromNodeId !== id && e.toNodeId !== id)
  // 一并删除该 EMU 下的分组虚拟节点，并解除设备归属（含 groupId）
  const removedGroupIds = new Set(groupsOfEmu(id).map(g => g.id))
  project.nodes = project.nodes.filter(n => n.id !== id && !removedGroupIds.has(n.id))
  const bound = devicesOfEmu(id)
  const pcsOrphans = bound.filter(n => n.templateId === 'pcs').length
  unassignDevicesFromEmu(id)
  if (selectedNodeId.value === id) selectedNodeId.value = null
  clearValidation()
  if (bound.length > 0)
    ElMessage.warning(`已删除 EMU，${pcsOrphans} 台 PCS、${bound.length - pcsOrphans} 台断路器/电表已解除归属，请重新选择所属 EMU 后再保存`)
}

function colorOf(id) { return templateColor(id) }
function templateName(id) { return templates.value.find(t => t.id === id)?.name || id }
function numberStep(def) {
  if (def.key?.toLowerCase().includes('efficiency') || def.key?.toLowerCase().includes('soc')) return 0.01
  if (Number.isFinite(def.max) && def.max <= 2) return 0.01
  return 1
}

/** null/undefined 的 max 会被 Element Plus 当成 0，触发 min>max 异常并拖垮整页交互 */
function numberInputBounds(def) {
  const bounds = {}
  if (Number.isFinite(def.min)) bounds.min = def.min
  if (Number.isFinite(def.max)) bounds.max = def.max
  return bounds
}

function uid() {
  return Math.random().toString(16).slice(2) + Date.now().toString(16)
}

function cloneParams(obj) {
  return JSON.parse(JSON.stringify(obj || {}))
}

function projectPayload() {
  return {
    schemaVersion: project.schemaVersion,
    id: project.id,
    name: project.name,
    nodes: project.nodes,
    edges: project.edges
  }
}

function snapshotJson() {
  return JSON.stringify(projectPayload())
}

function applyProject(p, { resetHistory = false, clearDirty = false } = {}) {
  project.schemaVersion = p.schemaVersion || '1.0'
  project.id = p.id || 'current'
  project.name = p.name || '未命名组态'
  project.nodes = (p.nodes || []).map(n => ({
    ...n,
    parameters: n.parameters || {}
  }))
  // 剔除端点节点不存在的悬空连线（如节点删除后的残留），画布上它们不可见也无法选中，
  // 若保留会在保存回放时报「连线端点设备不存在」
  const nodeIds = new Set(project.nodes.map(n => n.id))
  project.edges = (p.edges || []).filter(e => nodeIds.has(e.fromNodeId) && nodeIds.has(e.toNodeId))
  if (resetHistory) {
    historyPast.value = []
    historyFuture.value = []
  }
  if (clearDirty) dirty.value = false
}

function pushHistory() {
  if (applyingHistory) return
  historyPast.value.push(snapshotJson())
  if (historyPast.value.length > HISTORY_MAX) historyPast.value.shift()
  historyFuture.value = []
  dirty.value = true
}

function markDirty() {
  dirty.value = true
}

function onParamEdited() {
  pushHistory()
  clearValidation()
}

function undo() {
  if (!historyPast.value.length) return
  historyFuture.value.push(snapshotJson())
  const prev = historyPast.value.pop()
  applyingHistory = true
  try {
    applyProject(JSON.parse(prev))
    dirty.value = true
    clearValidation()
  } finally {
    applyingHistory = false
  }
}

function redo() {
  if (!historyFuture.value.length) return
  historyPast.value.push(snapshotJson())
  const next = historyFuture.value.pop()
  applyingHistory = true
  try {
    applyProject(JSON.parse(next))
    dirty.value = true
    clearValidation()
  } finally {
    applyingHistory = false
  }
}

function clearValidation() {
  problemNodeIds.value = []
  validationMessage.value = ''
  validationIssues.value = []
}

function applyValidationResult(validation) {
  const ok = validation?.ok ?? validation?.Ok
  if (ok) {
    clearValidation()
    return true
  }
  validationMessage.value = validation?.message || validation?.Message || '工程配置不合理'
  const details = validation?.details || validation?.Details || []
  const nodes = validation?.problemNodeIds || validation?.ProblemNodeIds || []
  problemNodeIds.value = [...nodes]
  validationIssues.value = details.length
    ? details.map((text, i) => ({ text, nodeId: nodes[i] || nodes[0] || null }))
    : [{ text: validationMessage.value, nodeId: nodes[0] || null }]
  return false
}

function focusProblem(nodeId) {
  if (!nodeId) return
  selectedNodeId.value = nodeId
  selectedEdgeId.value = null
}

async function confirmDiscardIfDirty(actionLabel = '继续') {
  if (!dirty.value) return true
  try {
    await ElMessageBox.confirm(
      `当前组态有未保存修改，${actionLabel}将丢失这些改动。`,
      '未保存修改',
      { type: 'warning', confirmButtonText: actionLabel, cancelButtonText: '取消' }
    )
    return true
  } catch {
    return false
  }
}

async function reload() {
  if (!(await confirmDiscardIfDirty('重新加载'))) return
  const [tpl, proj, lib] = await Promise.all([
    getTopologyTemplates(),
    getTopologyProject(),
    getTopologyLibrary()
  ])
  templates.value = tpl
  library.value = lib
  applyProject(proj, { resetHistory: true, clearDirty: true })
  selectedNodeId.value = null
  selectedEdgeId.value = null
  linking.value = null
  clearValidation()
  syncEditHintFromRoute()
}

function syncEditHintFromRoute() {
  const mode = route.query.mode
  if (mode === 'new') editHint.value = '新建工程'
  else if (mode === 'edit') editHint.value = '编辑工程'
  else editHint.value = ''
}

function goProjectManage() {
  router.push('/projects')
}

function onBoolParamChange(key, value) {
  if (!selectedNode.value) return
  pushHistory()
  selectedNode.value.parameters[key] = !!value
  if (key === 'isMainBreaker' && value) {
    for (const n of project.nodes) {
      if (n.templateId === 'ac_breaker' && n.id !== selectedNode.value.id)
        n.parameters.isMainBreaker = false
    }
  }
  if (key === 'isPccMeter' && value) {
    for (const n of project.nodes) {
      if (n.templateId === 'ac_meter' && n.id !== selectedNode.value.id)
        n.parameters.isPccMeter = false
    }
  }
  clearValidation()
}

async function saveProject() {
  const name = (project.name || '').trim()
  if (!name) {
    ElMessage.warning('请填写工程名称')
    return
  }
  project.name = name

  if (!project.id || project.id === 'current')
    project.id = uid()

  saving.value = true
  try {
    const validation = await postTopologyValidate(projectPayload())
    if (!applyValidationResult(validation)) {
      await ElMessageBox.alert(
        validationIssues.value.map(i => i.text).join('\n') || validationMessage.value,
        '无法保存',
        { type: 'error', confirmButtonText: '知道了' }
      )
      return
    }

    const check = await checkTopologyProjectName(name, project.id)
    if (check?.exists && check.project?.id) {
      try {
        await ElMessageBox.confirm(
          `已存在同名工程「${check.project.name}」。\n确定后将覆盖该工程的组态内容（保留其工程 ID）。`,
          '同名工程',
          { type: 'warning', confirmButtonText: '替换并保存', cancelButtonText: '取消' }
        )
      } catch {
        return
      }
      project.id = check.project.id
    }

    const saved = await putTopologyProject(projectPayload())
    applyProject(saved, { clearDirty: true })
    historyPast.value = []
    historyFuture.value = []
    editHint.value = '已保存'
    ElMessage.success(`工程「${saved.name}」已保存，可在工程管理 / 系统配置中选用`)
  } catch (e) {
    ElMessage.error(e.message || '保存失败')
  } finally {
    saving.value = false
  }
}

function onDragTemplate(ev, t) {
  ev.dataTransfer.effectAllowed = 'copy'
  ev.dataTransfer.setData('application/x-topo', JSON.stringify({ kind: 'template', templateId: t.id }))
}

function onDragLibrary(ev, item) {
  ev.dataTransfer.effectAllowed = 'copy'
  ev.dataTransfer.setData('application/x-topo', JSON.stringify({ kind: 'library', itemId: item.id }))
}

function onDrop(ev) {
  const raw = ev.dataTransfer.getData('application/x-topo')
  if (!raw) return
  let payload
  try { payload = JSON.parse(raw) } catch { return }
  if (payload.kind === 'template') {
    const t = templates.value.find(i => i.id === payload.templateId)
    if (!t) return
    const { x, y } = dropWorldPosition(ev, t.id)
    addFromTemplate(t, x, y)
  } else if (payload.kind === 'library') {
    const item = library.value.find(i => i.id === payload.itemId)
    if (!item) return
    const { x, y } = dropWorldPosition(ev, item.templateId)
    addFromLibrary(item, x, y)
  }
}

function addFromTemplate(t, x = 120, y = 100) {
  pushHistory()
  const node = {
    id: uid(),
    templateId: t.id,
    libraryItemId: null,
    label: t.name,
    x: snapToGrid(x),
    y: snapToGrid(y),
    parameters: cloneParams(t.defaultParameters)
  }
  // PCS 新增时自动归入第一个 EMU 虚拟单元（若有）；分组自动归入第一个 EMU
  if ((t.id === 'pcs' || t.id === 'emu_group') && emuNodes.value.length > 0)
    node.parameters.emuId = emuNodes.value[0].id
  project.nodes.push(node)
  selectedNodeId.value = node.id
  selectedEdgeId.value = null
  clearValidation()
}

function addFromLibrary(item, x = 140, y = 120) {
  pushHistory()
  const t = templates.value.find(i => i.id === item.templateId)
  const node = {
    id: uid(),
    templateId: item.templateId,
    libraryItemId: item.id,
    label: item.name,
    x: snapToGrid(x),
    y: snapToGrid(y),
    parameters: cloneParams({ ...(t?.defaultParameters || {}), ...(item.parameters || {}) })
  }
  project.nodes.push(node)
  selectedNodeId.value = node.id
  selectedEdgeId.value = null
  clearValidation()
}

function onSelectNode(id) {
  selectedNodeId.value = id
  selectedEdgeId.value = null
}

function onSelectEdge(id) {
  selectedEdgeId.value = id
  selectedNodeId.value = null
}

function onMoveNode({ id, x, y }) {
  const n = project.nodes.find(i => i.id === id)
  if (!n) return
  const nx = snapToGrid(x)
  const ny = snapToGrid(y)
  if (n.x === nx && n.y === ny) return
  pushHistory()
  n.x = nx
  n.y = ny
}

async function onPortClick({ nodeId, portId }) {
  if (connecting.value) return

  if (!linking.value) {
    linking.value = { nodeId, portId }
    return
  }
  if (linking.value.nodeId === nodeId && linking.value.portId === portId) {
    linking.value = null
    pointerWorld.value = null
    return
  }

  const source = { ...linking.value }
  const edge = {
    id: uid(),
    fromNodeId: source.nodeId,
    fromPortId: source.portId,
    toNodeId: nodeId,
    toPortId: portId
  }

  const localReject = quickReject(edge)
  if (localReject) {
    showConnectFeedback('error', '连接被拒绝', `[${localReject.code}] ${localReject.message}`)
    linking.value = source
    pointerWorld.value = null
    problemNodeIds.value = [edge.fromNodeId, edge.toNodeId]
    return
  }

  const seq = ++connectSeq
  connecting.value = true
  try {
    const res = await postTopologyConnect({
      project: projectPayload(),
      edge,
      expandBundle: true
    })
    if (seq !== connectSeq) return

    const validation = res?.validation || res?.Validation || {}
    const ok = validation.ok ?? validation.Ok
    const message = validation.message || validation.Message || ''
    const code = validation.code || validation.Code || ''

    if (!ok) {
      showConnectFeedback(
        'error',
        '连接被拒绝',
        code ? `[${code}] ${message || '校验未通过'}` : (message || '校验未通过')
      )
      linking.value = source
      pointerWorld.value = null
      const nodes = validation.problemNodeIds || validation.ProblemNodeIds || [edge.fromNodeId, edge.toNodeId]
      problemNodeIds.value = [...nodes]
      return
    }
    pushHistory()
    applyProject(res.project)
    linking.value = null
    pointerWorld.value = null
    clearValidation()
  } catch (e) {
    if (seq !== connectSeq) return
    showConnectFeedback('error', '连线失败', e.message || '请求异常')
    linking.value = source
    pointerWorld.value = null
  } finally {
    if (seq === connectSeq) connecting.value = false
  }
}

async function deleteSelected() {
  if (selectedEdgeId.value) {
    try {
      const updated = await postTopologyDisconnect({
        project: projectPayload(),
        edgeId: selectedEdgeId.value
      })
      pushHistory()
      applyProject(updated)
      selectedEdgeId.value = null
      clearValidation()
    } catch (e) {
      ElMessage.error(e.message || '断开失败')
    }
    return
  }
  if (selectedNodeId.value) {
    pushHistory()
    const id = selectedNodeId.value
    const removed = project.nodes.find(n => n.id === id)
    project.edges = project.edges.filter(e => e.fromNodeId !== id && e.toNodeId !== id)
    project.nodes = project.nodes.filter(n => n.id !== id)
    // 删除 EMU 分组虚拟节点时解除设备的 groupId 引用
    if (removed?.templateId === 'emu_group') {
      for (const n of project.nodes)
        if (n.parameters?.groupId === id) n.parameters.groupId = ''
    }
    // 删除 EMU 虚拟节点时同步解除其下设备（PCS/断路器/电表）归属
    if (removed?.templateId === 'emu') {
      const bound = devicesOfEmu(id)
      const pcsOrphans = bound.filter(n => n.templateId === 'pcs').length
      unassignDevicesFromEmu(id)
      if (bound.length > 0)
        ElMessage.warning(`${pcsOrphans} 台 PCS、${bound.length - pcsOrphans} 台断路器/电表已解除归属，请重新选择所属 EMU 后再保存`)
    }
    selectedNodeId.value = null
    clearValidation()
  }
}

function openSaveLibrary() {
  if (!selectedNode.value) return
  libName.value = selectedNode.value.label || '未命名设备'
  libDialog.value = true
}

async function saveLibrary() {
  if (!selectedNode.value) return
  savingLib.value = true
  try {
    const item = await putTopologyLibrary({
      name: libName.value || selectedNode.value.label,
      templateId: selectedNode.value.templateId,
      parameters: cloneParams(selectedNode.value.parameters)
    })
    library.value = await getTopologyLibrary()
    selectedNode.value.libraryItemId = item.id
    libDialog.value = false
    ElMessage.success('已写入设备库')
  } catch (e) {
    ElMessage.error(e.message || '保存设备库失败')
  } finally {
    savingLib.value = false
  }
}

async function removeLibrary(id) {
  try {
    await ElMessageBox.confirm('确定删除该设备库条目？', '确认', { type: 'warning' })
    await deleteTopologyLibrary(id)
    library.value = await getTopologyLibrary()
  } catch { /* cancel */ }
}

async function applyWizard() {
  const emuCount = Number(wizardEmuCount.value) || 0
  const pvCount = Number(wizardPvCount.value) || 0
  if (emuCount + pvCount < 1) {
    ElMessage.warning('EMU 与光伏单元至少需要 1 个')
    return
  }
  if (!(await confirmDiscardIfDirty('生成骨架'))) return
  wizardLoading.value = true
  try {
    const scaffolded = await postTopologyScaffold({
      emuCount,
      pvCount,
      name: wizardName.value || undefined,
      includeLoad: wizardIncludeLoad.value
    })
    applyProject(scaffolded, { resetHistory: true })
    dirty.value = true
    selectedNodeId.value = null
    selectedEdgeId.value = null
    linking.value = null
    clearValidation()
    wizardOpen.value = false
    editHint.value = '向导已生成'
    const parts = []
    if (emuCount > 0) parts.push(`EMU×${emuCount}`)
    if (pvCount > 0) parts.push(`光伏×${pvCount}`)
    ElMessage.success(`已生成标准径向拓扑（${parts.join('、')}），请检查后保存`)
  } catch (e) {
    ElMessage.error(e.message || '生成失败')
  } finally {
    wizardLoading.value = false
  }
}

function onKey(ev) {
  const mod = ev.metaKey || ev.ctrlKey
  if (mod && ev.key.toLowerCase() === 'z') {
    const tag = (ev.target?.tagName || '').toLowerCase()
    if (tag === 'input' || tag === 'textarea') return
    ev.preventDefault()
    if (ev.shiftKey) redo()
    else undo()
    return
  }
  if (mod && ev.key.toLowerCase() === 'y') {
    const tag = (ev.target?.tagName || '').toLowerCase()
    if (tag === 'input' || tag === 'textarea') return
    ev.preventDefault()
    redo()
    return
  }
  if (ev.key === 'Escape') {
    linking.value = null
    pointerWorld.value = null
    return
  }
  if (ev.key === 'Delete' || ev.key === 'Backspace') {
    const tag = (ev.target?.tagName || '').toLowerCase()
    if (tag === 'input' || tag === 'textarea') return
    if (canDelete.value) {
      ev.preventDefault()
      deleteSelected()
    }
  }
}

function onBeforeUnload(ev) {
  if (!dirty.value) return
  ev.preventDefault()
  ev.returnValue = ''
}

watch(
  () => `${route.query.mode || ''}:${route.query.id || ''}`,
  () => { syncEditHintFromRoute() }
)

onMounted(async () => {
  try {
    await reload()
  } catch (e) {
    ElMessage.error(e.message || '加载组态失败')
  }
  window.addEventListener('keydown', onKey)
  window.addEventListener('beforeunload', onBeforeUnload)
})

onBeforeUnmount(() => {
  window.removeEventListener('keydown', onKey)
  window.removeEventListener('beforeunload', onBeforeUnload)
  linking.value = null
  pointerWorld.value = null
  connecting.value = false
  ElMessage.closeAll()
})
</script>

<style scoped>
.topo-page {
  height: 100%;
  max-height: calc(100vh - 80px);
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 0;
  min-height: 0;
  overflow: hidden;
}
.toolbar { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 10px 12px; margin-bottom: 0; flex-shrink: 0; }
.toolbar .left { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.toolbar .right { display: flex; align-items: center; justify-content: flex-end; min-width: 240px; flex-shrink: 0; font-variant-numeric: tabular-nums; }
.workspace {
  flex: 1;
  display: grid;
  grid-template-columns: 220px minmax(0, 1fr) 280px;
  gap: 8px;
  min-width: 0;
  min-height: 0;
  overflow: hidden;
}
.palette, .props { margin-bottom: 0; min-width: 0; min-height: 0; overflow: auto; }
.canvas-wrap { margin-bottom: 0; padding: 0; overflow: hidden; display: flex; min-width: 0; min-height: 0; position: relative; }
/* 连线提示浮层：不占工具栏空间，避免右侧状态组宽度变化引起工具栏换行、画布上下抖动 */
.linking-tip {
  position: absolute; top: 10px; left: 50%; transform: translateX(-50%); z-index: 10;
  padding: 4px 12px; font-size: 12px; color: #e6a23c;
  background: #fdf6ec; border: 1px solid #faecd8; border-radius: 4px;
  pointer-events: none; box-shadow: 0 2px 8px rgba(0, 0, 0, .12);
}
.palette-item {
  display: flex; align-items: center; gap: 8px;
  padding: 8px; border: 1px solid #ebeef5; border-radius: 6px; margin-bottom: 6px;
  cursor: grab; background: #fafbfc;
}
.palette-item:hover { border-color: #c0c4cc; background: #fff; }
.palette-item.active { border-color: #409eff; background: #ecf5ff; }
.palette-item .dot { width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0; }
.palette-item .meta { flex: 1; min-width: 0; }
.palette-item .name { font-size: 13px; font-weight: 600; color: #303133; }
.palette-item .desc { font-size: 11px; color: #909399; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.emu-group-row { cursor: pointer; padding-left: 10px; border-radius: 3px; }
.emu-group-row:hover { color: #0f8a9d; }
.emu-group-row.active { color: #0f8a9d; font-weight: 600; }
.empty { font-size: 12px; color: #909399; line-height: 1.5; }
.param-hint { font-size: 11px; color: #909399; margin-top: 2px; line-height: 1.3; }
.validation-box { margin-bottom: 12px; }
.issue-list { margin: 6px 0 0; padding-left: 18px; }
.issue-item { font-size: 12px; line-height: 1.5; margin-bottom: 4px; }
.issue-item.clickable { cursor: pointer; color: #c45656; text-decoration: underline; }
.wizard-desc { font-size: 13px; color: #606266; line-height: 1.5; margin: 0 0 12px; }
@media (max-width: 1100px) {
  .workspace { grid-template-columns: 180px minmax(0, 1fr); }
  .props { display: none; }
}
</style>
