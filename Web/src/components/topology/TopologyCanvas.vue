<template>
  <div
    ref="root"
    class="topo-canvas"
    @wheel.prevent="onWheel"
    @mousedown="onBackgroundDown"
    @contextmenu.prevent
  >
    <svg :width="width" :height="height" class="topo-svg">
      <g :transform="`translate(${panX},${panY}) scale(${scale})`">
        <g class="edges">
          <path
            v-for="e in edgePaths"
            :key="e.id"
            :d="e.d"
            class="edge"
            :class="{ selected: e.id === selectedEdgeId }"
            @mousedown.stop
            @click.stop="emit('select-edge', e.id)"
          />
          <line
            v-if="draftLine"
            :x1="draftLine.x1"
            :y1="draftLine.y1"
            :x2="draftLine.x2"
            :y2="draftLine.y2"
            class="edge draft"
          />
        </g>

        <g
          v-for="node in nodes"
          :key="node.id"
          class="node"
          :class="{
            selected: node.id === selectedNodeId,
            problem: problemSet.has(node.id)
          }"
          :transform="`translate(${node.x},${node.y})`"
          @mousedown.stop="onNodeDown($event, node)"
          @click.stop="emit('select-node', node.id)"
        >
          <rect
            v-if="node.templateId !== 'ac_bus' && node.templateId !== 'dc_bus'"
            :width="sizeOf(node).w"
            :height="sizeOf(node).h"
            rx="6"
            :fill="fillOf(node)"
            stroke="#303133"
            stroke-width="1.5"
          />
          <g v-if="node.templateId === 'ac_bus'">
            <rect :width="sizeOf(node).w" :height="sizeOf(node).h" rx="2" fill="#fafafa" stroke="#909399" stroke-width="1" />
            <line x1="8" :y1="sizeOf(node).h * 0.28" :x2="sizeOf(node).w - 8" :y2="sizeOf(node).h * 0.28" stroke="#e74c3c" stroke-width="2.5" />
            <line x1="8" :y1="sizeOf(node).h * 0.5" :x2="sizeOf(node).w - 8" :y2="sizeOf(node).h * 0.5" stroke="#27ae60" stroke-width="2.5" />
            <line x1="8" :y1="sizeOf(node).h * 0.72" :x2="sizeOf(node).w - 8" :y2="sizeOf(node).h * 0.72" stroke="#3498db" stroke-width="2.5" />
          </g>
          <g v-if="node.templateId === 'dc_bus'">
            <rect :width="sizeOf(node).w" :height="sizeOf(node).h" rx="4" fill="#fff7e6" stroke="#d35400" stroke-width="2" />
            <line x1="12" :y1="sizeOf(node).h * 0.38" :x2="sizeOf(node).w - 12" :y2="sizeOf(node).h * 0.38" stroke="#e67e22" stroke-width="3" />
            <line x1="12" :y1="sizeOf(node).h * 0.62" :x2="sizeOf(node).w - 12" :y2="sizeOf(node).h * 0.62" stroke="#2c3e50" stroke-width="3" />
          </g>
          <g v-if="node.templateId === 'transformer'" fill="none" stroke="#fff" stroke-width="2">
            <circle :cx="sizeOf(node).w / 2" cy="42" r="14" />
            <circle :cx="sizeOf(node).w / 2" cy="78" r="14" />
          </g>
          <g v-if="node.templateId === 'pv_unit'" fill="none" stroke="#fff" stroke-width="1.6">
            <rect :x="sizeOf(node).w * 0.22" y="28" :width="sizeOf(node).w * 0.56" height="36" rx="2" />
            <line :x1="sizeOf(node).w * 0.22" y1="40" :x2="sizeOf(node).w * 0.78" y2="40" />
            <line :x1="sizeOf(node).w * 0.22" y1="52" :x2="sizeOf(node).w * 0.78" y2="52" />
            <line :x1="sizeOf(node).w * 0.4" y1="28" :x2="sizeOf(node).w * 0.4" y2="64" />
            <line :x1="sizeOf(node).w * 0.6" y1="28" :x2="sizeOf(node).w * 0.6" y2="64" />
          </g>
          <g v-if="node.templateId === 'ac_breaker'" fill="none" stroke="#fff" stroke-width="2">
            <!-- 三相竖线 + 中间联动开关 -->
            <line :x1="sizeOf(node).w * 0.2" y1="18" :x2="sizeOf(node).w * 0.2" y2="42" />
            <line :x1="sizeOf(node).w * 0.5" y1="18" :x2="sizeOf(node).w * 0.5" y2="42" />
            <line :x1="sizeOf(node).w * 0.8" y1="18" :x2="sizeOf(node).w * 0.8" y2="42" />
            <line
              :x1="sizeOf(node).w * 0.2"
              :y1="breakerClosed(node) ? 55 : 48"
              :x2="sizeOf(node).w * 0.8"
              :y2="breakerClosed(node) ? 55 : 48"
              stroke-width="2.5"
            />
            <line :x1="sizeOf(node).w * 0.2" :y1="breakerClosed(node) ? 55 : 48" :x2="sizeOf(node).w * 0.2" y2="68" />
            <line :x1="sizeOf(node).w * 0.5" :y1="breakerClosed(node) ? 55 : 48" :x2="sizeOf(node).w * 0.5" y2="68" />
            <line :x1="sizeOf(node).w * 0.8" :y1="breakerClosed(node) ? 55 : 48" :x2="sizeOf(node).w * 0.8" y2="68" />
            <line
              v-if="!breakerClosed(node)"
              :x1="sizeOf(node).w * 0.35"
              y1="42"
              :x2="sizeOf(node).w * 0.65"
              y2="68"
              stroke="#ffdddd"
              stroke-width="2"
            />
            <line :x1="sizeOf(node).w * 0.2" y1="68" :x2="sizeOf(node).w * 0.2" :y2="sizeOf(node).h - 18" />
            <line :x1="sizeOf(node).w * 0.5" y1="68" :x2="sizeOf(node).w * 0.5" :y2="sizeOf(node).h - 18" />
            <line :x1="sizeOf(node).w * 0.8" y1="68" :x2="sizeOf(node).w * 0.8" :y2="sizeOf(node).h - 18" />
          </g>

          <text
            :x="sizeOf(node).w / 2"
            :y="node.templateId === 'ac_bus' || node.templateId === 'dc_bus' ? -8 : 16"
            text-anchor="middle"
            class="node-label"
            :fill="node.templateId === 'ac_bus' || node.templateId === 'dc_bus' ? '#303133' : '#fff'"
          >{{ node.label }}</text>
          <text
            v-if="voltageHint(node)"
            :x="sizeOf(node).w / 2"
            :y="node.templateId === 'ac_bus' || node.templateId === 'dc_bus' ? sizeOf(node).h + 14 : sizeOf(node).h - 10"
            text-anchor="middle"
            class="node-sub"
            :fill="node.templateId === 'ac_bus' || node.templateId === 'dc_bus' ? '#606266' : 'rgba(255,255,255,.85)'"
          >{{ voltageHint(node) }}</text>

          <g
            v-for="port in portsOf(node)"
            :key="port.id"
            class="port"
            :class="{ active: linking && linking.nodeId === node.id && linking.portId === port.id }"
            @mousedown.stop.prevent
            @click.stop="emit('port-click', { nodeId: node.id, portId: port.id })"
          >
            <!-- 透明扩大命中区 -->
            <circle
              :cx="port.localX"
              :cy="port.localY"
              r="14"
              fill="transparent"
              stroke="none"
            />
            <circle
              :cx="port.localX"
              :cy="port.localY"
              r="7"
              :fill="portFill(port)"
              stroke="#fff"
              stroke-width="1.5"
            />
            <title>{{ port.label }} ({{ port.kind }}{{ port.phase ? ' ' + port.phase : '' }})</title>
          </g>
        </g>
      </g>
    </svg>
    <div class="hint">滚轮缩放 · 右键拖动画布 · 点拐角连线（三相/直流自动成组）· 网格吸附 · Ctrl+Z 撤销 · Delete 删除</div>
  </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { formatVoltage, nodeSize, portPosition, snapToGrid, templateColor } from './nodeLayout.js'

const props = defineProps({
  nodes: { type: Array, default: () => [] },
  edges: { type: Array, default: () => [] },
  templates: { type: Array, default: () => [] },
  selectedNodeId: { type: String, default: null },
  selectedEdgeId: { type: String, default: null },
  linking: { type: Object, default: null },
  pointerWorld: { type: Object, default: null },
  problemNodeIds: { type: Array, default: () => [] },
  snap: { type: Boolean, default: true }
})

const emit = defineEmits(['select-node', 'select-edge', 'port-click', 'move-node', 'pointer-world'])

const root = ref(null)
const width = ref(0)
const height = ref(0)
const panX = ref(40)
const panY = ref(40)
const scale = ref(1)

const tplMap = computed(() => {
  const m = {}
  for (const t of props.templates) m[t.id] = t
  return m
})

const problemSet = computed(() => new Set(props.problemNodeIds || []))

function sizeOf(node) {
  return nodeSize(node.templateId)
}

function fillOf(node) {
  return templateColor(node.templateId)
}

function portsOf(node) {
  const tpl = tplMap.value[node.templateId]
  if (!tpl) return []
  return tpl.ports.map(p => {
    const pos = portPosition(node, p)
    return { ...p, localX: pos.localX, localY: pos.localY }
  })
}

function portFill(port) {
  if (port.kind === 'dc' || port.kind === 'dc_pos') return '#e67e22'
  if (port.kind === 'dc_neg') return '#2c3e50'
  if (port.phase === 'A') return '#e74c3c'
  if (port.phase === 'B') return '#27ae60'
  if (port.phase === 'C') return '#3498db'
  return '#909399'
}

function voltageHint(node) {
  const p = node.parameters || {}
  if (node.templateId === 'grid') return formatVoltage(p.outputVoltage)
  if (node.templateId === 'ac_bus') {
    const v = formatVoltage(p.nominalVoltage)
    return p.energized ? `${v} · 带电` : (Number(p.nominalVoltage) > 0 ? `${v} · 未带电` : '未带电')
  }
  if (node.templateId === 'transformer') {
    return `${formatVoltage(p.primaryVoltage)}/${formatVoltage(p.secondaryVoltage)}`
  }
  if (node.templateId === 'emu') return formatVoltage(p.acVoltage)
  if (node.templateId === 'pv_unit') {
    const n = Number(p.inverterCount || 1)
    const kw = Number(p.inverterRatedPowerKw || 0)
    if (kw <= 0) return formatVoltage(p.acVoltage)
    const inv = n > 1 ? `${n.toFixed(0)}×${kw.toFixed(0)}kW` : `${kw.toFixed(0)}kW`
    return `${formatVoltage(p.acVoltage)} · ${inv}`
  }
  if (node.templateId === 'dc_bus') return formatVoltage(p.nominalVoltage)
  if (node.templateId === 'bms') {
    const series = Number(p.cellSeriesCount || 0) * Number(p.packCount || 0)
    const v = series * Number(p.cellNominalVoltage || 0)
    return v > 0 ? `≈${formatVoltage(v)}` : ''
  }
  if (node.templateId === 'ac_breaker') {
    const closed = breakerClosed(node)
    const role = p.isMainBreaker === true || p.isMainBreaker === 'true' ? '主断 · ' : ''
    return `${role}${closed ? '合' : '分'} · ${formatVoltage(p.ratedVoltage)}`
  }
  if (node.templateId === 'ac_meter') {
    const role = p.isPccMeter === true || p.isPccMeter === 'true' ? '并网点 · ' : ''
    return `${role}PT/CT ${formatVoltage(p.ptPrimaryVoltage)} / ${Number(p.ctPrimaryCurrent || 0).toFixed(0)}A`
  }
  if (node.templateId === 'load') {
    const pKw = Number(p.activePowerKw || 0)
    const qKvar = Number(p.reactivePowerKvar || 0)
    return `P ${pKw.toFixed(1)}kW · Q ${qKvar.toFixed(1)}kvar`
  }
  return ''
}

function breakerClosed(node) {
  const v = node?.parameters?.closed
  if (v === false || v === 'false' || v === 0) return false
  return true
}

const edgePaths = computed(() => {
  return props.edges.map(e => {
    const fromNode = props.nodes.find(n => n.id === e.fromNodeId)
    const toNode = props.nodes.find(n => n.id === e.toNodeId)
    const fromTpl = tplMap.value[fromNode?.templateId]
    const toTpl = tplMap.value[toNode?.templateId]
    const fromPort = fromTpl?.ports.find(p => p.id === e.fromPortId)
    const toPort = toTpl?.ports.find(p => p.id === e.toPortId)
    if (!fromNode || !toNode || !fromPort || !toPort) return { id: e.id, d: '' }
    const a = portPosition(fromNode, fromPort)
    const b = portPosition(toNode, toPort)
    const mx = (a.x + b.x) / 2
    const my = (a.y + b.y) / 2
    return { id: e.id, d: `M ${a.x} ${a.y} Q ${mx} ${my - 20} ${b.x} ${b.y}` }
  })
})

const draftLine = computed(() => {
  if (!props.linking || !props.pointerWorld) return null
  const node = props.nodes.find(n => n.id === props.linking.nodeId)
  const tpl = tplMap.value[node?.templateId]
  const port = tpl?.ports.find(p => p.id === props.linking.portId)
  if (!node || !port) return null
  const a = portPosition(node, port)
  return { x1: a.x, y1: a.y, x2: props.pointerWorld.x, y2: props.pointerWorld.y }
})

function clientToWorld(clientX, clientY) {
  if (!root.value) return { x: 0, y: 0 }
  const rect = root.value.getBoundingClientRect()
  return {
    x: (clientX - rect.left - panX.value) / scale.value,
    y: (clientY - rect.top - panY.value) / scale.value
  }
}

function onWheel(ev) {
  if (!root.value) return
  const factor = ev.deltaY > 0 ? 0.9 : 1.1
  const next = Math.min(2.5, Math.max(0.35, scale.value * factor))
  const rect = root.value.getBoundingClientRect()
  const cx = ev.clientX - rect.left
  const cy = ev.clientY - rect.top
  const wx = (cx - panX.value) / scale.value
  const wy = (cy - panY.value) / scale.value
  scale.value = next
  panX.value = cx - wx * next
  panY.value = cy - wy * next
}

let panning = false
let panStart = null
let draggingNode = null
let dragOffset = null
let tracking = false
let alive = true

function bindWindowTracking() {
  if (!alive || tracking) return
  tracking = true
  window.addEventListener('mousemove', onMoveTrack, true)
  window.addEventListener('mouseup', onUpTrack, true)
  window.addEventListener('blur', onUpTrack)
}

function unbindWindowTracking() {
  if (!tracking) return
  tracking = false
  window.removeEventListener('mousemove', onMoveTrack, true)
  window.removeEventListener('mouseup', onUpTrack, true)
  window.removeEventListener('blur', onUpTrack)
}

function onBackgroundDown(ev) {
  if (ev.button === 2 || ev.button === 1 || (ev.button === 0 && ev.altKey)) {
    panning = true
    panStart = { x: ev.clientX, y: ev.clientY, panX: panX.value, panY: panY.value }
    bindWindowTracking()
  }
}

function onNodeDown(ev, node) {
  if (ev.button !== 0) return
  draggingNode = node
  const w = clientToWorld(ev.clientX, ev.clientY)
  dragOffset = { x: w.x - node.x, y: w.y - node.y }
  bindWindowTracking()
}

function onMoveTrack(ev) {
  if (!alive || !root.value) return
  if (panning && panStart) {
    panX.value = panStart.panX + (ev.clientX - panStart.x)
    panY.value = panStart.panY + (ev.clientY - panStart.y)
  } else if (draggingNode && dragOffset) {
    const w = clientToWorld(ev.clientX, ev.clientY)
    let x = w.x - dragOffset.x
    let y = w.y - dragOffset.y
    if (props.snap) {
      x = snapToGrid(x)
      y = snapToGrid(y)
    } else {
      x = Math.round(x)
      y = Math.round(y)
    }
    draggingNode.x = x
    draggingNode.y = y
  }
  if (props.linking) {
    emit('pointer-world', clientToWorld(ev.clientX, ev.clientY))
  }
}

function onUpTrack() {
  if (!alive) {
    unbindWindowTracking()
    return
  }
  if (draggingNode) {
    emit('move-node', { id: draggingNode.id, x: draggingNode.x, y: draggingNode.y })
  }
  panning = false
  panStart = null
  draggingNode = null
  dragOffset = null
  // 连线预览仍需要跟踪时保持监听；否则释放
  if (!props.linking) unbindWindowTracking()
}

function resize() {
  if (!root.value) return
  const w = root.value.clientWidth
  const h = root.value.clientHeight
  if (w > 0) width.value = w
  if (h > 0) height.value = h
}

let ro = null

onMounted(() => {
  resize()
  window.addEventListener('resize', resize)
  if (typeof ResizeObserver !== 'undefined' && root.value) {
    ro = new ResizeObserver(() => resize())
    ro.observe(root.value)
  }
})

onUnmounted(() => {
  alive = false
  window.removeEventListener('resize', resize)
  unbindWindowTracking()
  if (ro) {
    ro.disconnect()
    ro = null
  }
  panning = false
  panStart = null
  draggingNode = null
  dragOffset = null
})

// 进入连线态时开始跟踪鼠标画预览线
watch(() => props.linking, (v) => {
  if (v) bindWindowTracking()
  else if (!panning && !draggingNode) unbindWindowTracking()
})

defineExpose({ clientToWorld })
</script>

<style scoped>
.topo-canvas {
  position: relative;
  width: 100%;
  height: 100%;
  min-width: 0;
  min-height: 0;
  background:
    linear-gradient(90deg, rgba(0,0,0,.03) 1px, transparent 1px) 0 0 / 20px 20px,
    linear-gradient(rgba(0,0,0,.03) 1px, transparent 1px) 0 0 / 20px 20px,
    #f7f9fc;
  border: 1px solid #e4e7ed;
  border-radius: 6px;
  overflow: hidden;
  cursor: default;
  user-select: none;
}
.topo-svg { display: block; width: 100%; height: 100%; max-width: 100%; }
.edge {
  fill: none;
  stroke: #606266;
  stroke-width: 2;
  cursor: pointer;
}
.edge.selected { stroke: #e6a23c; stroke-width: 3; }
.edge.draft { stroke: #409eff; stroke-dasharray: 6 4; }
.node { cursor: grab; }
.node.selected rect { stroke: #e6a23c; stroke-width: 2.5; }
.node.problem rect,
.node.problem > rect { stroke: #f56c6c; stroke-width: 2.5; }
.node-label { font-size: 12px; font-weight: 600; pointer-events: none; }
.node-sub { font-size: 10px; pointer-events: none; }
.port { cursor: crosshair; }
.port.active circle:last-of-type { stroke: #e6a23c; stroke-width: 2.5; }
.hint {
  position: absolute;
  left: 10px;
  bottom: 8px;
  font-size: 11px;
  color: #909399;
  background: rgba(255,255,255,.8);
  padding: 2px 8px;
  border-radius: 4px;
  pointer-events: none;
}
</style>
