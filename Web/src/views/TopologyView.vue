<template>
  <div class="topo-page">
    <div class="card toolbar">
      <div class="left">
        <el-input v-model="project.name" size="small" style="width:200px" placeholder="工程名称" />
        <el-button type="primary" size="small" :loading="saving" @click="saveProject">保存工程</el-button>
        <el-button size="small" @click="reload">重新加载</el-button>
        <el-button size="small" type="danger" plain :disabled="!canDelete" @click="deleteSelected">删除选中</el-button>
        <el-button size="small" :disabled="!selectedNode" @click="openSaveLibrary">存入设备库</el-button>
      </div>
      <div class="right">
        <el-tag size="small" type="info">节点 {{ project.nodes.length }}</el-tag>
        <el-tag size="small" type="info" style="margin-left:6px">连线 {{ project.edges.length }}</el-tag>
        <el-tag v-if="linking" size="small" type="warning" style="margin-left:6px">连线中…再点目标拐角（Esc 取消）</el-tag>
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
          @select-node="onSelectNode"
          @select-edge="onSelectEdge"
          @port-click="onPortClick"
          @move-node="onMoveNode"
          @pointer-world="w => pointerWorld = w"
        />
      </div>

      <aside class="props card">
        <div class="card-title">属性</div>
        <template v-if="selectedNode && selectedTemplate">
          <el-form label-position="top" size="small">
            <el-form-item label="显示名称">
              <el-input v-model="selectedNode.label" />
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
                :min="def.min"
                :max="def.max"
                :step="numberStep(def)"
                controls-position="right"
                style="width:100%"
              />
              <el-input v-else v-model="selectedNode.parameters[def.key]" />
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
        <p v-else class="empty">从左侧拖入设备，或选中画布中的设备编辑参数。</p>
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
  </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import TopologyCanvas from '@/components/topology/TopologyCanvas.vue'
import { nodeSize, templateColor } from '@/components/topology/nodeLayout.js'
import {
  getTopologyTemplates,
  getTopologyProject,
  putTopologyProject,
  postTopologyConnect,
  postTopologyDisconnect,
  getTopologyLibrary,
  putTopologyLibrary,
  deleteTopologyLibrary
} from '@/services/api.js'

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

/** 清空队列后只展示当前一条，避免连线过快时提示堆积/延后爆发 */
function showConnectFeedback(type, title, detail = '') {
  ElMessage.closeAll()
  // 成功只靠画布出线反馈，避免三相连线刷屏
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
      x: 80 + project.nodes.length * 24,
      y: 80 + (project.nodes.length % 5) * 40
    }
  }
  return {
    x: Math.round(world.x - size.w / 2),
    y: Math.round(world.y - size.h / 2)
  }
}

function isDcKind(kind) {
  return kind === 'dc' || kind === 'dc_pos' || kind === 'dc_neg'
}

/** 常见错误本地即时校验，避免等接口才弹提示 */
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

  const aAc = fromPort.kind === 'ac_phase'
  const bAc = toPort.kind === 'ac_phase'
  const aDc = isDcKind(fromPort.kind)
  const bDc = isDcKind(toPort.kind)
  if ((aAc && bDc) || (aDc && bAc)) {
    return { code: 'DOMAIN_MISMATCH', message: `端口类型不兼容：${fromPort.label} ↔ ${toPort.label}。交流不能接直流。` }
  }
  if (aAc && bAc && fromPort.phase && toPort.phase && fromPort.phase !== toPort.phase) {
    return {
      code: 'PHASE_MISMATCH',
      message: `相位不匹配：${fromPort.label}(${fromPort.phase}) ↔ ${toPort.label}(${toPort.phase})`
    }
  }
  if (aDc && bDc) {
    const pa = fromPort.kind === 'dc_neg' ? 'neg' : 'pos'
    const pb = toPort.kind === 'dc_neg' ? 'neg' : 'pos'
    if (pa !== pb) {
      return {
        code: 'DC_POLARITY',
        message: `直流极性不匹配：${fromPort.label} ↔ ${toPort.label}。正极只能接正极，负极只能接负极。`
      }
    }
  }
  return null
}

const selectedNode = computed(() => project.nodes.find(n => n.id === selectedNodeId.value) || null)
const selectedTemplate = computed(() => templates.value.find(t => t.id === selectedNode.value?.templateId) || null)
const canDelete = computed(() => !!(selectedNodeId.value || selectedEdgeId.value))

function colorOf(id) { return templateColor(id) }
function templateName(id) { return templates.value.find(t => t.id === id)?.name || id }
function numberStep(def) {
  if (def.key?.toLowerCase().includes('efficiency') || def.key?.toLowerCase().includes('soc')) return 0.01
  if ((def.max ?? 0) <= 2) return 0.01
  return 1
}

function uid() {
  return Math.random().toString(16).slice(2) + Date.now().toString(16)
}

function cloneParams(obj) {
  return JSON.parse(JSON.stringify(obj || {}))
}

function applyProject(p) {
  project.schemaVersion = p.schemaVersion || '1.0'
  project.id = p.id || 'current'
  project.name = p.name || '未命名组态'
  project.nodes = (p.nodes || []).map(n => ({
    ...n,
    parameters: n.parameters || {}
  }))
  project.edges = p.edges || []
}

async function reload() {
  const [tpl, proj, lib] = await Promise.all([
    getTopologyTemplates(),
    getTopologyProject(),
    getTopologyLibrary()
  ])
  templates.value = tpl
  library.value = lib
  applyProject(proj)
  selectedNodeId.value = null
  selectedEdgeId.value = null
  linking.value = null
}

async function saveProject() {
  saving.value = true
  try {
    const saved = await putTopologyProject({
      schemaVersion: project.schemaVersion,
      id: project.id,
      name: project.name,
      nodes: project.nodes,
      edges: project.edges
    })
    applyProject(saved)
    ElMessage.success(`工程已保存到 configs/topology/project.json`)
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
  const node = {
    id: uid(),
    templateId: t.id,
    libraryItemId: null,
    label: t.name,
    x,
    y,
    parameters: cloneParams(t.defaultParameters)
  }
  project.nodes.push(node)
  selectedNodeId.value = node.id
  selectedEdgeId.value = null
}

function addFromLibrary(item, x = 140, y = 120) {
  const t = templates.value.find(i => i.id === item.templateId)
  const node = {
    id: uid(),
    templateId: item.templateId,
    libraryItemId: item.id,
    label: item.name,
    x,
    y,
    parameters: cloneParams({ ...(t?.defaultParameters || {}), ...(item.parameters || {}) })
  }
  project.nodes.push(node)
  selectedNodeId.value = node.id
  selectedEdgeId.value = null
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
  if (n) { n.x = x; n.y = y }
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
    return
  }

  const seq = ++connectSeq
  connecting.value = true
  try {
    const res = await postTopologyConnect({
      project: {
        schemaVersion: project.schemaVersion,
        id: project.id,
        name: project.name,
        nodes: project.nodes,
        edges: project.edges
      },
      edge
    })
    // 被更新的连线请求覆盖时，丢弃过期响应，避免延后刷一堆旧提示
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
      return
    }
    applyProject(res.project)
    // 成功不弹 toast，画布出现连线即反馈
    linking.value = null
    pointerWorld.value = null
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
        project: {
          schemaVersion: project.schemaVersion,
          id: project.id,
          name: project.name,
          nodes: project.nodes,
          edges: project.edges
        },
        edgeId: selectedEdgeId.value
      })
      applyProject(updated)
      selectedEdgeId.value = null
    } catch (e) {
      ElMessage.error(e.message || '断开失败')
    }
    return
  }
  if (selectedNodeId.value) {
    const id = selectedNodeId.value
    project.edges = project.edges.filter(e => e.fromNodeId !== id && e.toNodeId !== id)
    project.nodes = project.nodes.filter(n => n.id !== id)
    selectedNodeId.value = null
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

function onKey(ev) {
  if (ev.key === 'Escape') {
    linking.value = null
    pointerWorld.value = null
  }
  if (ev.key === 'Delete' || ev.key === 'Backspace') {
    const tag = (ev.target?.tagName || '').toLowerCase()
    if (tag === 'input' || tag === 'textarea') return
    if (canDelete.value) deleteSelected()
  }
}

onMounted(async () => {
  try {
    await reload()
  } catch (e) {
    ElMessage.error(e.message || '加载组态失败')
  }
  window.addEventListener('keydown', onKey)
})

onUnmounted(() => window.removeEventListener('keydown', onKey))
</script>

<style scoped>
.topo-page { height: calc(100vh - 80px); display: flex; flex-direction: column; gap: 8px; min-height: 0; }
.toolbar { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 10px 12px; margin-bottom: 0; }
.toolbar .left { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.toolbar .right { display: flex; align-items: center; }
.workspace { flex: 1; display: grid; grid-template-columns: 220px 1fr 280px; gap: 8px; min-height: 0; }
.palette, .props, .canvas-wrap { margin-bottom: 0; min-height: 0; overflow: auto; }
.canvas-wrap { padding: 0; overflow: hidden; display: flex; }
.palette-item {
  display: flex; align-items: center; gap: 8px;
  padding: 8px; border: 1px solid #ebeef5; border-radius: 6px; margin-bottom: 6px;
  cursor: grab; background: #fafbfc;
}
.palette-item:hover { border-color: #c0c4cc; background: #fff; }
.palette-item .dot { width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0; }
.palette-item .meta { flex: 1; min-width: 0; }
.palette-item .name { font-size: 13px; font-weight: 600; color: #303133; }
.palette-item .desc { font-size: 11px; color: #909399; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.empty { font-size: 12px; color: #909399; line-height: 1.5; }
.param-hint { font-size: 11px; color: #909399; margin-top: 2px; line-height: 1.3; }
@media (max-width: 1100px) {
  .workspace { grid-template-columns: 180px 1fr; }
  .props { display: none; }
}
</style>
