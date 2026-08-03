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
          :class="{ selected: node.id === selectedNodeId }"
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
            @mousedown.stop
            @click.stop="emit('port-click', { nodeId: node.id, portId: port.id })"
          >
            <circle
              :cx="port.localX"
              :cy="port.localY"
              r="5"
              :fill="portFill(port)"
              stroke="#fff"
              stroke-width="1.5"
            />
            <title>{{ port.label }} ({{ port.kind }}{{ port.phase ? ' ' + port.phase : '' }})</title>
          </g>
        </g>
      </g>
    </svg>
    <div class="hint">滚轮缩放 · 右键拖动画布 · 点击拐角连线 · Delete 删除选中</div>
  </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { formatVoltage, nodeSize, portPosition, templateColor } from './nodeLayout.js'

const props = defineProps({
  nodes: { type: Array, default: () => [] },
  edges: { type: Array, default: () => [] },
  templates: { type: Array, default: () => [] },
  selectedNodeId: { type: String, default: null },
  selectedEdgeId: { type: String, default: null },
  linking: { type: Object, default: null },
  pointerWorld: { type: Object, default: null }
})

const emit = defineEmits(['select-node', 'select-edge', 'port-click', 'move-node', 'pointer-world'])

const root = ref(null)
const width = ref(800)
const height = ref(560)
const panX = ref(40)
const panY = ref(40)
const scale = ref(1)

const tplMap = computed(() => {
  const m = {}
  for (const t of props.templates) m[t.id] = t
  return m
})

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
  if (node.templateId === 'dc_bus') return formatVoltage(p.nominalVoltage)
  if (node.templateId === 'bms') {
    const series = Number(p.cellSeriesCount || 0) * Number(p.packCount || 0)
    const v = series * Number(p.cellNominalVoltage || 0)
    return v > 0 ? `≈${formatVoltage(v)}` : ''
  }
  if (node.templateId === 'ac_meter') return `PT ${formatVoltage(p.ptPrimaryVoltage)}`
  return ''
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
  const rect = root.value.getBoundingClientRect()
  return {
    x: (clientX - rect.left - panX.value) / scale.value,
    y: (clientY - rect.top - panY.value) / scale.value
  }
}

function onWheel(ev) {
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

function onBackgroundDown(ev) {
  if (ev.button === 2 || ev.button === 1 || (ev.button === 0 && ev.altKey)) {
    panning = true
    panStart = { x: ev.clientX, y: ev.clientY, panX: panX.value, panY: panY.value }
  }
}

function onNodeDown(ev, node) {
  if (ev.button !== 0) return
  draggingNode = node
  const w = clientToWorld(ev.clientX, ev.clientY)
  dragOffset = { x: w.x - node.x, y: w.y - node.y }
}

function onMoveTrack(ev) {
  if (panning && panStart) {
    panX.value = panStart.panX + (ev.clientX - panStart.x)
    panY.value = panStart.panY + (ev.clientY - panStart.y)
  } else if (draggingNode && dragOffset) {
    const w = clientToWorld(ev.clientX, ev.clientY)
    draggingNode.x = Math.round(w.x - dragOffset.x)
    draggingNode.y = Math.round(w.y - dragOffset.y)
  }
  if (props.linking && root.value) {
    emit('pointer-world', clientToWorld(ev.clientX, ev.clientY))
  }
}

function onUpTrack() {
  if (draggingNode) {
    emit('move-node', { id: draggingNode.id, x: draggingNode.x, y: draggingNode.y })
  }
  panning = false
  panStart = null
  draggingNode = null
  dragOffset = null
}

function resize() {
  if (!root.value) return
  width.value = root.value.clientWidth
  height.value = root.value.clientHeight
}

onMounted(() => {
  resize()
  window.addEventListener('resize', resize)
  window.addEventListener('mousemove', onMoveTrack)
  window.addEventListener('mouseup', onUpTrack)
})

onUnmounted(() => {
  window.removeEventListener('resize', resize)
  window.removeEventListener('mousemove', onMoveTrack)
  window.removeEventListener('mouseup', onUpTrack)
})

defineExpose({ clientToWorld })
</script>

<style scoped>
.topo-canvas {
  position: relative;
  width: 100%;
  height: 100%;
  min-height: 480px;
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
.topo-svg { display: block; width: 100%; height: 100%; }
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
.node-label { font-size: 12px; font-weight: 600; pointer-events: none; }
.node-sub { font-size: 10px; pointer-events: none; }
.port { cursor: crosshair; }
.port.active circle { stroke: #e6a23c; stroke-width: 2.5; }
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
