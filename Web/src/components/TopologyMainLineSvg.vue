<template>
  <div class="mainline-zoom-root">
    <div class="mainline-toolbar">
      <el-button-group size="small">
        <el-button :disabled="zoom <= MIN_ZOOM" @click="zoomOut" title="缩小">−</el-button>
        <el-button disabled class="zoom-label">{{ zoomPercent }}%</el-button>
        <el-button :disabled="zoom >= MAX_ZOOM" @click="zoomIn" title="放大">+</el-button>
      </el-button-group>
      <el-slider
        v-model="zoom"
        class="zoom-slider"
        :min="MIN_ZOOM"
        :max="MAX_ZOOM"
        :step="0.1"
        :show-tooltip="true"
        :format-tooltip="v => `${Math.round(v * 100)}%`"
      />
      <el-button size="small" link type="primary" @click="resetZoom">重置 100%</el-button>
      <span v-if="snap.activeProjectName" class="project-tag">工程：{{ snap.activeProjectName }}</span>
      <span class="toolbar-hint">经典单线图 · 右键平移 · Ctrl/⌘ + 滚轮缩放</span>
    </div>

    <div
      ref="viewportRef"
      class="mainline-viewport"
      :class="{ 'is-panning': isPanning }"
      @wheel="onWheel"
      @contextmenu.prevent
      @mousedown="onPanMouseDown"
    >
      <div class="mainline-pan-layer" :style="panLayerStyle">
        <svg
          class="mainline-svg"
          :width="renderWidth"
          :height="renderHeight"
          :viewBox="`0 0 ${layout.width} ${layout.height}`"
          preserveAspectRatio="xMinYMin meet"
        >
          <!-- 站侧由组态连通递归生成：电网 / 串联断路器 / 母线 / 变压器 / 电表 / 负载 -->
          <g class="station">
            <line
              v-for="(w, wi) in layout.wires"
              :key="`wire-${wi}`"
              class="bus-line"
              :x1="w.x1"
              :y1="w.y1"
              :x2="w.x2"
              :y2="w.y2"
            />

            <template v-if="layout.grid">
              <text :x="layout.gridX + 14" :y="layout.yGrid + 12" class="label-text">{{ layout.grid.label || '电网' }}</text>
              <text :x="layout.gridX + 14" :y="layout.yGrid + 28" class="value-text">
                PCC {{ fmtVolt(snap.pccLineVoltageV) }} / {{ fmtHz(snap.systemFrequencyHz) }}
              </text>
              <text :x="layout.gridX + 14" :y="layout.yGrid + 42" class="value-text muted-text">
                设定 {{ fmtVolt(snap.gridNominalLineVoltageV) }} / {{ fmtHz(snap.gridNominalFrequencyHz) }}
              </text>
            </template>

            <g
              v-for="br in layout.stemBreakers"
              :key="`stem-brk-${br.id}`"
              :class="{ 'breaker-hit': br.isMain }"
              @click="br.isMain && $emit('toggle-main-breaker')"
            >
              <rect
                :x="br.x - 18"
                :y="br.yTop"
                width="36"
                :height="br.yBottom - br.yTop"
                rx="3"
                fill="transparent"
              />
              <BreakerSymbol
                :x="br.x"
                :y="br.y"
                :closed="br.isMain ? snap.mainBreakerClosed : breakerClosed(br.node)"
                :tripped="br.isMain ? snap.mainBreakerTripped : false"
              />
              <text :x="br.x + 22" :y="br.y + 4" class="label-text breaker-label">
                {{ br.isMain ? `主断 ${snap.mainBreakerLabel || fmtBreaker(snap.mainBreakerClosed, snap.mainBreakerTripped)}` : br.label }}
              </text>
            </g>

            <!-- 母线联络分段断路器：状态取组态静态参数 -->
            <g v-for="br in layout.tieBreakers" :key="`tie-brk-${br.id}`">
              <rect :x="br.x - 18" :y="br.yTop" width="36" :height="br.yBottom - br.yTop" rx="3" fill="transparent" />
              <BreakerSymbol :x="br.x" :y="br.y" :closed="br.closed" :tripped="br.tripped" />
              <text :x="br.x + 22" :y="br.y + 4" class="label-text breaker-label">
                {{ br.label }} {{ fmtBreaker(br.closed, br.tripped) }}
              </text>
            </g>

            <g v-for="bus in layout.buses" :key="`bus-${bus.id}`">
              <template v-if="!bus.omit">
                <line class="bus-line bus-thick" :x1="bus.x1" :y1="bus.y" :x2="bus.x2" :y2="bus.y" />
                <text :x="bus.x1" :y="bus.y - 8" class="label-text">
                  {{ bus.label }}{{ busTelemetry(bus) }}
                </text>
              </template>
            </g>

            <g v-for="(xf, xfIndex) in layout.transformers" :key="`xfmr-${xf.id}`">
              <TransformerSymbol :x="xf.x" :y="xf.y + xf.span / 2" />
              <text
                :x="xf.labelSide === 'left' ? xf.x - 22 : xf.x + 22"
                :y="xf.y + 14"
                :text-anchor="xf.labelSide === 'left' ? 'end' : 'start'"
                class="label-text"
              >
                {{ xf.label }} {{ xf.ratioLabel }}
              </text>
              <text
                :x="xf.labelSide === 'left' ? xf.x - 22 : xf.x + 22"
                :y="xf.y + 30"
                :text-anchor="xf.labelSide === 'left' ? 'end' : 'start'"
                class="value-text"
              >
                {{ xf.kvaLabel || (xfIndex === 0 ? fmtVolt(snap.mainTransformerSecondary?.lineVoltageV) : '') }}
              </text>
            </g>

            <g v-for="m in layout.meters" :key="`meter-${m.id}`">
              <rect :x="m.x - 32" :y="m.y" width="64" :height="m.h" rx="4" class="meter-box" />
              <text :x="m.x" :y="m.y + 18" text-anchor="middle" class="label-text">{{ m.label }}</text>
              <text :x="m.x" :y="m.y + 34" text-anchor="middle" class="value-text">PT/CT</text>
              <text :x="m.x" :y="m.y + 48" text-anchor="middle" class="value-text">
                P {{ fmtKw(m.isPcc ? snap.meterPrimary?.activePowerKw : null) }}
              </text>
              <text :x="m.x" :y="m.y + 62" text-anchor="middle" class="value-text">
                Q {{ fmtKvar(m.isPcc ? snap.meterPrimary?.reactivePowerKvar : null) }}
              </text>
            </g>

            <g v-for="(load, li) in layout.loads" :key="`load-${load.id}`">
              <LoadSymbol :x="load.x" :y="load.busY + load.stub + load.symbolH / 2" />
              <text :x="load.x + 16" :y="load.busY + load.stub + 12" class="label-text">{{ load.label }}</text>
              <text :x="load.x + 16" :y="load.busY + load.stub + 28" class="value-text">
                P {{ fmtKw(li === 0 ? snap.loadActivePowerKw : null) }}
              </text>
              <text :x="load.x + 16" :y="load.busY + load.stub + 42" class="value-text">
                Q {{ fmtKvar(li === 0 ? snap.loadReactivePowerKvar : null) }}
              </text>
            </g>
          </g>

          <!-- EMU 虚线框（容器）：先绘制，单元内容叠加在框内 -->
          <g class="emu-overlay" pointer-events="none">
            <rect
              v-for="g in layout.groups"
              :key="g.id"
              :x="g.x"
              :y="g.y"
              :width="g.w"
              :height="g.h"
              class="group-box"
            />
          </g>

          <!-- 各发电单元：EMU / 光伏；支路原点在 LV 母线上 -->
          <g
            v-for="u in layout.units"
            :key="u.index"
            :transform="`translate(${u.cx}, ${u.originY ?? layout.yBusLv})`"
          >
            <!-- LV 母线 —引线— 单元断 —引线— 单元变 —引线— 690 母线（连线一律黑色） -->
            <line class="bus-line" x1="0" y1="0" x2="0" :y2="u.unitBrkTop" />

            <g v-if="u.kind === 'pv'">
              <rect x="-22" :y="u.unitBrkTop" width="44" :height="u.unitBrkBottom - u.unitBrkTop" rx="3" fill="transparent" />
              <BreakerSymbol :x="0" :y="u.unitBrkMid" :closed="true" :tripped="false" />
              <text x="26" :y="u.unitBrkMid + 4" class="label-text breaker-label">单元断 —</text>
            </g>
            <g
              v-else
              class="breaker-hit"
              @click="$emit('toggle-unit-breaker', u.unitSnap?.unitIndex ?? u.index)"
            >
              <rect x="-22" :y="u.unitBrkTop" width="44" :height="u.unitBrkBottom - u.unitBrkTop" rx="3" fill="transparent" />
              <BreakerSymbol :x="0" :y="u.unitBrkMid" :closed="!!u.unitSnap?.unitBreakerClosed" :tripped="!!u.unitSnap?.unitBreakerTripped" />
              <text x="26" :y="u.unitBrkMid + 4" class="label-text breaker-label">
                单元断 {{ u.unitBreakerNode?.label || u.unitSnap?.unitBreakerLabel || fmtBreaker(u.unitSnap?.unitBreakerClosed, u.unitSnap?.unitBreakerTripped) }}
              </text>
            </g>

            <line class="bus-line" x1="0" :y1="u.unitBrkBottom" x2="0" :y2="u.kind === 'pv' ? u.xfmrCardTop : u.unitXfmrTop" />
            <PvXfmrCard
              v-if="u.kind === 'pv'"
              :unit="u"
              :live="pvLive(u)"
              :x="0"
              :y="u.xfmrCardTop"
              :h="u.xfmrCardH"
              @pv-start="n => $emit('pv-start', n)"
              @pv-stop="n => $emit('pv-stop', n)"
              @pv-set-power="p => $emit('pv-set-power', p)"
              @pv-set-reactive="p => $emit('pv-set-reactive', p)"
            />
            <TransformerSymbol
              v-else
              :x="0"
              :y="u.unitXfmrTop + u.unitXfmrSpan / 2"
              :scale="0.85"
            />
            <template v-if="u.kind !== 'pv'">
              <text x="22" :y="u.unitXfmrTop + 14" class="label-text">
                单元变 {{ u.unitXfLabel }}
              </text>
            </template>

            <!-- 光伏：箱变模块 → 光伏方阵A/B -->
            <template v-if="u.kind === 'pv'">
              <line class="bus-line" x1="0" :y1="u.xfmrCardTop + u.xfmrCardH" x2="0" :y2="u.arraySplitY" />
              <polyline
                class="bus-line"
                :points="`0,${u.arraySplitY} ${-u.channelX},${u.arraySplitY} ${-u.channelX},${u.bmsTop}`"
              />
              <polyline
                class="bus-line"
                :points="`0,${u.arraySplitY} ${u.channelX},${u.arraySplitY} ${u.channelX},${u.bmsTop}`"
              />
              <PvArrayCard
                :group="u.groupA"
                :live="pvArrayLive(u, 'A')"
                :pv-number="(pvLive(u)?.pvNumber) || ((u.pvIndex ?? 0) + 1)"
                :x="-u.channelX"
                :y="u.bmsTop"
                :h="u.bmsH"
                @pv-set-temp="p => $emit('pv-set-temp', p)"
                @pv-set-angle="p => $emit('pv-set-angle', p)"
              />
              <PvArrayCard
                :group="u.groupB"
                :live="pvArrayLive(u, 'B')"
                :pv-number="(pvLive(u)?.pvNumber) || ((u.pvIndex ?? 0) + 1)"
                :x="u.channelX"
                :y="u.bmsTop"
                :h="u.bmsH"
                @pv-set-temp="p => $emit('pv-set-temp', p)"
                @pv-set-angle="p => $emit('pv-set-angle', p)"
              />
            </template>

            <!-- EMU：单元变 —引线— [690母线|PCS]：仅 1 路 PCS 时省略 690 母线 -->
            <template v-else>
            <template v-if="u.omitBus690">
              <line
                class="bus-line"
                x1="0"
                :y1="u.unitXfmrTop + u.unitXfmrSpan"
                :x2="u.pcsA ? -u.channelX : u.channelX"
                :y2="u.pcsTop"
              />
            </template>
            <template v-else>
              <line class="bus-line" x1="0" :y1="u.unitXfmrTop + u.unitXfmrSpan" x2="0" :y2="u.unitBus690Y" />
              <line class="bus-line" :x1="-u.channelX" :y1="u.unitBus690Y" :x2="u.channelX" :y2="u.unitBus690Y" />
            </template>

            <PcsCard
              v-if="u.pcsA"
              :channel="u.pcsA"
              side="A"
              :x="-u.channelX"
              :bus-y="u.omitBus690 ? u.pcsTop : u.unitBus690Y"
              :y="u.pcsTop"
              :h="u.pcsH"
              @pcs-start="n => $emit('pcs-start', n)"
              @pcs-stop="n => $emit('pcs-stop', n)"
              @pcs-set-power="p => $emit('pcs-set-power', p)"
              @pcs-set-reactive="p => $emit('pcs-set-reactive', p)"
            />
            <DevicePlaceholder
              v-else-if="u.runtimeMissing && u.drawPcsSlots >= 1"
              title="PCS-A"
              hint="运行时未加载"
              :x="-u.channelX"
              :y="u.pcsTop"
              :h="u.pcsH"
            />
            <PcsCard
              v-if="u.pcsB"
              :channel="u.pcsB"
              side="B"
              :x="u.channelX"
              :bus-y="u.omitBus690 ? u.pcsTop : u.unitBus690Y"
              :y="u.pcsTop"
              :h="u.pcsH"
              @pcs-start="n => $emit('pcs-start', n)"
              @pcs-stop="n => $emit('pcs-stop', n)"
              @pcs-set-power="p => $emit('pcs-set-power', p)"
              @pcs-set-reactive="p => $emit('pcs-set-reactive', p)"
            />
            <DevicePlaceholder
              v-else-if="u.runtimeMissing && u.drawPcsSlots >= 2"
              title="PCS-B"
              hint="运行时未加载"
              :x="u.channelX"
              :y="u.pcsTop"
              :h="u.pcsH"
            />

            <!-- 单元电表（EMU 绑定）：自 690 母线右侧取电；未绑定不画 -->
            <template v-if="u.unitMeterNode">
              <line class="bus-line" :x1="u.channelX" :y1="u.unitBus690Y" :x2="u.unitMeterX" :y2="u.unitBus690Y" />
              <line class="bus-line" :x1="u.unitMeterX" :y1="u.unitBus690Y" :x2="u.unitMeterX" :y2="u.unitMeterTopY" />
              <rect
                :x="u.unitMeterX - u.unitMeterHalfW"
                :y="u.unitMeterTopY"
                :width="u.unitMeterHalfW * 2"
                :height="u.unitMeterH"
                rx="4"
                class="meter-box"
              />
              <text :x="u.unitMeterX" :y="u.unitMeterTopY + 18" text-anchor="middle" class="label-text">{{ u.unitMeterNode.label || '电表' }}</text>
              <text :x="u.unitMeterX" :y="u.unitMeterTopY + 34" text-anchor="middle" class="value-text">PT/CT</text>
            </template>

            <!-- DC：并联共母线；下方仅 1 路 BMS 时省略直流母线直连 -->
            <template v-if="u.dcParallel && !u.omitDcBus">
              <line class="bus-line" :x1="-u.channelX" :y1="u.pcsTop + u.pcsH" :x2="-u.channelX" :y2="u.dcBusY" />
              <line class="bus-line" :x1="u.channelX" :y1="u.pcsTop + u.pcsH" :x2="u.channelX" :y2="u.dcBusY" />
              <line class="bus-line bus-thick" :x1="-u.channelX - 20" :y1="u.dcBusY" :x2="u.channelX + 20" :y2="u.dcBusY" />
              <text :x="-u.channelX - 18" :y="u.dcBusY - 6" class="label-text">
                {{ u.dcBus?.label || '直流母线' }} {{ u.dcVoltageLabel }}
              </text>
              <line class="bus-line" :x1="-u.channelX" :y1="u.dcBusY" :x2="-u.channelX" :y2="u.bmsTop" />
              <line class="bus-line" :x1="u.channelX" :y1="u.dcBusY" :x2="u.channelX" :y2="u.bmsTop" />
            </template>
            <template v-else>
              <line class="bus-line" :x1="-u.channelX" :y1="u.pcsTop + u.pcsH" :x2="-u.channelX" :y2="u.bmsTop" />
              <line class="bus-line" :x1="u.channelX" :y1="u.pcsTop + u.pcsH" :x2="u.channelX" :y2="u.bmsTop" />
            </template>

            <BmsCard
              v-if="u.pcsA"
              :channel="u.pcsA"
              :label="bmsLabel(u, 0)"
              :x="-u.channelX"
              :y="u.bmsTop"
              :h="u.bmsH"
              @bms-power-on="n => $emit('bms-power-on', n)"
              @bms-power-off="n => $emit('bms-power-off', n)"
              @bms-fault-clear="n => $emit('bms-fault-clear', n)"
              @bms-set-soc="p => $emit('bms-set-soc', p)"
            />
            <DevicePlaceholder
              v-else-if="u.runtimeMissing && u.drawPcsSlots >= 1"
              :title="bmsLabel(u, 0)"
              hint="运行时未加载"
              :x="-u.channelX"
              :y="u.bmsTop"
              :h="u.bmsH"
              tone="bms"
            />
            <BmsCard
              v-if="u.pcsB"
              :channel="u.pcsB"
              :label="bmsLabel(u, 1)"
              :x="u.channelX"
              :y="u.bmsTop"
              :h="u.bmsH"
              @bms-power-on="n => $emit('bms-power-on', n)"
              @bms-power-off="n => $emit('bms-power-off', n)"
              @bms-fault-clear="n => $emit('bms-fault-clear', n)"
              @bms-set-soc="p => $emit('bms-set-soc', p)"
            />
            <DevicePlaceholder
              v-else-if="u.runtimeMissing && u.drawPcsSlots >= 2"
              :title="bmsLabel(u, 1)"
              hint="运行时未加载"
              :x="u.channelX"
              :y="u.bmsTop"
              :h="u.bmsH"
              tone="bms"
            />

            <text
              v-if="u.runtimeMissing"
              :x="0"
              :y="u.pcsTop + u.pcsH / 2"
              text-anchor="middle"
              class="runtime-missing-hint"
            >请在「系统配置」应用组态并重启</text>
            </template>
          </g>
        </svg>
      </div>
    </div>

    <div v-if="blackStartChips.length" class="mainline-blackstart">
      <span class="footer-label">黑启动</span>
      <span
        v-for="chip in blackStartChips"
        :key="chip.pcs"
        class="bs-chip"
        :class="chipStatusClass(chip.status)"
      >PCS{{ chip.pcs }} {{ chip.status }}</span>
    </div>
    <div class="mainline-legend">
      <span><i class="legend-line legend-closed" />合闸</span>
      <span><i class="legend-line legend-open" />分闸/跳闸</span>
      <span>经典单线图图例</span>
      <span>结构随组态工程自动适配</span>
      <span>光伏/储能单元按设备类型展开</span>
    </div>
  </div>
</template>

<script setup>
import { computed, defineComponent, h, onBeforeUnmount, reactive, ref } from 'vue'
import { buildTopologyMainLineLayout } from './topology/topologyMainLineLayout.js'

const MIN_ZOOM = 0.5
const MAX_ZOOM = 10
const DEFAULT_ZOOM = 1
const ZOOM_STEP = 0.1
const BOX_W = 132
const XFMR_BOX_W = 168

function pvConfigLayoutText(modulesPerString, stringCount, inverterCount) {
  const rows = Number(modulesPerString)
  const strings = Number(stringCount)
  const inv = Number(inverterCount)
  if (!(rows > 0 && strings > 0 && inv > 0)) return ''
  return `${rows}×${strings}×${inv}`
}

const props = defineProps({
  snap: { type: Object, required: true }
})
defineEmits([
  'toggle-main-breaker',
  'toggle-unit-breaker',
  'pcs-start',
  'pcs-stop',
  'pcs-set-power',
  'pcs-set-reactive',
  'bms-power-on',
  'bms-power-off',
  'bms-fault-clear',
  'bms-set-soc',
  'pv-start',
  'pv-stop',
  'pv-set-power',
  'pv-set-reactive',
  'pv-set-temp',
  'pv-set-angle'
])

const zoom = ref(DEFAULT_ZOOM)
const viewportRef = ref(null)
const panX = ref(0)
const panY = ref(0)
const isPanning = ref(false)
let panDragStart = { x: 0, y: 0, panX: 0, panY: 0 }

const layout = computed(() => {
  const l = buildTopologyMainLineLayout(props.snap.topology, props.snap.units || [])
  return {
    ...l,
    wires: l.wires || [],
    buses: l.buses || [],
    transformers: l.transformers || l.stationXfmrs || [],
    meters: l.meters || [],
    loads: l.loads || [],
    stemBreakers: l.stemBreakers || [],
    tieBreakers: l.tieBreakers || []
  }
})

const zoomPercent = computed(() => Math.round(zoom.value * 100))
const panLayerStyle = computed(() => ({
  transform: `translate(${panX.value}px, ${panY.value}px)`
}))
const renderWidth = computed(() => Math.round(layout.value.width * zoom.value))
const renderHeight = computed(() => Math.round(layout.value.height * zoom.value))

function clampZoom(v) {
  return Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, +v.toFixed(2)))
}
function zoomIn() { zoom.value = clampZoom(zoom.value + ZOOM_STEP) }
function zoomOut() { zoom.value = clampZoom(zoom.value - ZOOM_STEP) }
function resetZoom() {
  zoom.value = DEFAULT_ZOOM
  panX.value = 0
  panY.value = 0
}
/** 普通滚轮交给页面滚动；按住 Ctrl/⌘ 才缩放接线图 */
function onWheel(e) {
  if (!e.ctrlKey && !e.metaKey) return
  e.preventDefault()
  zoom.value = clampZoom(zoom.value + (e.deltaY > 0 ? -ZOOM_STEP : ZOOM_STEP))
}
function onPanMouseMove(e) {
  if (!isPanning.value) return
  panX.value = panDragStart.panX + (e.clientX - panDragStart.x)
  panY.value = panDragStart.panY + (e.clientY - panDragStart.y)
}
function stopPanning() {
  if (!isPanning.value) return
  isPanning.value = false
  window.removeEventListener('mousemove', onPanMouseMove)
  window.removeEventListener('mouseup', onPanMouseUp)
}
function onPanMouseUp() { stopPanning() }
function onPanMouseDown(e) {
  if (e.button !== 2) return
  e.preventDefault()
  isPanning.value = true
  panDragStart = { x: e.clientX, y: e.clientY, panX: panX.value, panY: panY.value }
  window.addEventListener('mousemove', onPanMouseMove)
  window.addEventListener('mouseup', onPanMouseUp)
}
onBeforeUnmount(() => stopPanning())

const blackStartChips = computed(() => {
  const summary = props.snap.blackStartSummary
  if (!summary) return []
  return summary.split(/\s{2,}/).filter(Boolean).map(part => {
    const m = part.match(/^PCS(\d+):(.+)$/)
    return m ? { pcs: m[1], status: m[2] } : { pcs: part, status: '' }
  })
})
function chipStatusClass(status) {
  if (!status || status === '关') return 'bs-off'
  if (status.includes('运行') || status === '开' || status === '运') return 'bs-on'
  return 'bs-partial'
}

function bmsLabel(u, slot) {
  const node = u.bmsNodes?.[slot]
  if (node?.label) return node.label
  const name = node?.parameters?.name
  if (name) return String(name)
  const ch = slot === 0 ? u.pcsA : u.pcsB
  return `BMS 舱${ch?.compartmentNumber ?? slot + 1}`
}

function fmtVolt(v) {
  if (v == null) return '—'
  return v >= 1000 ? `${(v / 1000).toFixed(1)} kV` : `${(v || 0).toFixed(1)} V`
}
function fmtHz(v) { return v == null ? '—' : `${Number(v).toFixed(2)} Hz` }
function fmtKw(v) { return v == null ? '—' : `${Number(v).toFixed(1)} kW` }
function fmtKvar(v) { return v == null ? '—' : `${Number(v).toFixed(1)} kvar` }
function pvLive(u) {
  const list = props.snap.pvUnits || []
  const idx = u?.pvIndex
  if (Number.isInteger(idx) && idx >= 0 && list[idx]) return list[idx]
  return list.find(p => p.pvNumber === (idx + 1)) || null
}
function pvArrayLive(u, side) {
  const live = pvLive(u) || {}
  return side === 'B' ? (live.arrayB || null) : (live.arrayA || null)
}
function fmtBreaker(closed, tripped) { return tripped ? '跳闸' : closed ? '合' : '分' }
function breakerClosed(node) {
  const v = node?.parameters?.closed
  return v === true || v === 'true' || v === 1
}
function busTelemetry(bus) {
  const v = Number(bus?.voltage || 0)
  if (v >= 100000) {
    const t = fmtVolt(props.snap.pccLineVoltageV)
    return t === '—' ? '' : ` · ${t}`
  }
  if (v >= 1000 && v <= 40000) {
    const t = fmtVolt(props.snap.stationBus35LineVoltageV)
    return t === '—' ? '' : ` · ${t}`
  }
  return ''
}
const TransformerSymbol = defineComponent({
  props: { x: Number, y: Number, scale: { type: Number, default: 1 } },
  setup(p) {
    return () => {
      const r = 9 * (p.scale || 1)
      const gap = 7 * (p.scale || 1)
      return h('g', { transform: `translate(${p.x}, ${p.y})` }, [
        h('circle', { cx: 0, cy: -gap, r, fill: 'none', stroke: '#000', 'stroke-width': 2 }),
        h('circle', { cx: 0, cy: gap, r, fill: 'none', stroke: '#000', 'stroke-width': 2 })
      ])
    }
  }
})

const LoadSymbol = defineComponent({
  props: { x: Number, y: Number },
  setup(p) {
    return () => h('g', { transform: `translate(${p.x}, ${p.y})` }, [
      h('polyline', {
        points: '0,-16 6,-10 0,-4 6,2 0,8 6,14 0,20',
        fill: 'none',
        stroke: '#000',
        'stroke-width': 2,
        'stroke-linejoin': 'round'
      }),
      h('line', { x1: 0, y1: 20, x2: 0, y2: 28, stroke: '#000', 'stroke-width': 2 }),
      h('polyline', {
        points: '-7,28 0,36 7,28',
        fill: 'none',
        stroke: '#000',
        'stroke-width': 2,
        'stroke-linejoin': 'round'
      })
    ])
  }
})

const BreakerSymbol = defineComponent({
  props: { x: Number, y: Number, closed: Boolean, tripped: Boolean },
  setup(p) {
    return () => {
      const color = p.tripped ? '#f56c6c' : p.closed ? '#67c23a' : '#f56c6c'
      const children = [
        h('line', { x1: p.x - 10, y1: p.y, x2: p.x - 2, y2: p.y, stroke: color, 'stroke-width': 2.5 }),
        h('line', { x1: p.x + 2, y1: p.y, x2: p.x + 10, y2: p.y, stroke: color, 'stroke-width': 2.5 })
      ]
      if (!p.closed || p.tripped) {
        children.push(h('line', { x1: p.x - 4, y1: p.y - 5, x2: p.x + 4, y2: p.y + 5, stroke: color, 'stroke-width': 2 }))
      }
      return h('g', children)
    }
  }
})

const powerDrafts = reactive({})
function draftKey(ch, kind) {
  return `${kind}-${ch?.pvNumber ?? ch?.pcsNumber ?? ch?.compartmentNumber ?? ''}-${ch?.side ?? ''}`
}
function getDraft(ch, kind, fallback) {
  const key = draftKey(ch, kind)
  if (powerDrafts[key] === undefined) powerDrafts[key] = String(Number(fallback ?? 0).toFixed(1))
  return powerDrafts[key]
}
function setDraft(ch, kind, value) { powerDrafts[draftKey(ch, kind)] = value }

const DevicePlaceholder = defineComponent({
  props: {
    title: String,
    hint: String,
    x: Number,
    y: Number,
    h: Number,
    tone: { type: String, default: 'pcs' }
  },
  setup(p) {
    return () => {
      const halfW = BOX_W / 2
      return h('g', { transform: `translate(${p.x}, 0)` }, [
        h('foreignObject', { x: -halfW, y: p.y, width: BOX_W, height: p.h }, [
          h('div', {
            xmlns: 'http://www.w3.org/1999/xhtml',
            class: ['svg-device-box', 'placeholder-box', p.tone === 'bms' ? 'bms-box' : 'pcs-box']
          }, [
            h('div', { class: 'box-title' }, p.title || '设备'),
            h('div', { class: 'box-line muted' }, p.hint || '运行时未加载'),
            h('div', { class: 'box-line muted' }, '应用组态后显示')
          ])
        ])
      ])
    }
  }
})

const PvXfmrCard = defineComponent({
  props: { unit: Object, live: Object, x: Number, y: Number, h: Number },
  emits: ['pv-start', 'pv-stop', 'pv-set-power', 'pv-set-reactive'],
  setup(p, { emit }) {
    return () => {
      const u = p.unit || {}
      const live = p.live || {}
      const pvNumber = live.pvNumber || ((u.pvIndex ?? 0) + 1)
      const draftHost = { pvNumber }
      const halfW = XFMR_BOX_W / 2
      const running = live.running === true || Number(live.onOff) === 1
      const pKw = live.activePowerKw
      const qKvar = live.reactivePowerKvar
      const invCount = Number(u.inverterCount)
      const gridCount = invCount > 0
        ? invCount
        : (live.gridConnectedDeviceCount != null ? live.gridConnectedDeviceCount : null)
      const lines = [
        `${u.unitXfLabel || ''} · ${Number(u.xfRatedKva || 0).toFixed(0)} kVA`,
        `逆变器 ${u.inverterCount}×${Number(u.inverterRatedKw || 0).toFixed(0)} kW`,
        `实时 P ${pKw == null ? '—' : `${Number(pKw).toFixed(1)} kW`}`,
        `实时 Q ${qKvar == null ? '—' : `${Number(qKvar).toFixed(1)} kvar`}`,
        `可发 ${live.maximumDischargePowerKw == null ? '—' : `${Number(live.maximumDischargePowerKw).toFixed(1)} kW`}`,
        `状态 ${running ? '运行' : '停机'}${gridCount != null ? ` · 并网 ${gridCount}` : ''}`
      ]
      return h('g', { transform: `translate(${p.x}, 0)` }, [
        h('foreignObject', { x: -halfW, y: p.y, width: XFMR_BOX_W, height: p.h }, [
          h('div', { xmlns: 'http://www.w3.org/1999/xhtml', class: 'svg-device-box pv-xfmr-box' }, [
            h('div', { class: 'box-title' }, '箱变'),
            ...lines.map(t => h('div', { class: 'box-line' }, t)),
            h('div', { class: 'box-controls' }, [
              h('div', { class: 'power-row' }, [
                h('label', { class: 'power-label' }, 'P设(kW)'),
                h('input', {
                  type: 'text', inputMode: 'decimal', class: 'power-input',
                  value: getDraft(draftHost, 'p', live.targetActivePowerKw ?? u.totalRatedKw),
                  onInput: (e) => setDraft(draftHost, 'p', e.target.value)
                }),
                h('button', {
                  type: 'button', class: 'act-btn act-set',
                  onClick: (e) => {
                    e.stopPropagation()
                    const kw = Number(getDraft(draftHost, 'p', live.targetActivePowerKw))
                    if (!Number.isFinite(kw)) return
                    emit('pv-set-power', { pvNumber, powerKw: kw })
                  }
                }, '设定')
              ]),
              h('div', { class: 'power-row' }, [
                h('label', { class: 'power-label' }, 'Q设(kvar)'),
                h('input', {
                  type: 'text', inputMode: 'decimal', class: 'power-input',
                  value: getDraft(draftHost, 'q', live.targetReactivePowerKvar ?? 0),
                  onInput: (e) => setDraft(draftHost, 'q', e.target.value)
                }),
                h('button', {
                  type: 'button', class: 'act-btn act-set',
                  onClick: (e) => {
                    e.stopPropagation()
                    const kvar = Number(getDraft(draftHost, 'q', live.targetReactivePowerKvar))
                    if (!Number.isFinite(kvar)) return
                    emit('pv-set-reactive', { pvNumber, reactiveKvar: kvar })
                  }
                }, '设定')
              ]),
              h('div', { class: 'box-actions' }, [
                h('button', {
                  type: 'button', class: 'act-btn act-on',
                  onClick: (e) => { e.stopPropagation(); emit('pv-start', pvNumber) }
                }, '启动'),
                h('button', {
                  type: 'button', class: 'act-btn act-off',
                  onClick: (e) => { e.stopPropagation(); emit('pv-stop', pvNumber) }
                }, '停机')
              ])
            ])
          ])
        ])
      ])
    }
  }
})

const PvArrayCard = defineComponent({
  props: { group: Object, live: Object, pvNumber: Number, x: Number, y: Number, h: Number },
  emits: ['pv-set-temp', 'pv-set-angle'],
  setup(p, { emit }) {
    return () => {
      const g = p.group || {}
      const live = p.live || {}
      const side = g.side || live.side || 'A'
      const pvNumber = p.pvNumber || 0
      const draftHost = { pvNumber, side }
      const halfW = BOX_W / 2
      const gPoa = live.planeOfArrayWm2
      const pNow = live.activePowerKw
      const pAvail = live.availableAcPowerKw
      const vdc = live.dcVoltageV
      const idc = live.dcCurrentA
      const layout = pvConfigLayoutText(g.modulesPerString, g.stringCount, g.inverterCount)
      const lines = [
        `有功 ${pNow == null ? '—' : `${Number(pNow).toFixed(1)} kW`}`,
        `可发 ${pAvail == null ? '—' : `${Number(pAvail).toFixed(1)} kW`}`,
        `直流 ${vdc == null ? '—' : `${Number(vdc).toFixed(0)} V`} / ${idc == null ? '—' : `${Number(idc).toFixed(1)} A`}`,
        `辐照 ${gPoa == null ? '—' : `${Number(gPoa).toFixed(0)} W/㎡`}`
      ]
      return h('g', { transform: `translate(${p.x}, 0)` }, [
        h('foreignObject', { x: -halfW, y: p.y, width: BOX_W, height: p.h }, [
          h('div', { xmlns: 'http://www.w3.org/1999/xhtml', class: 'svg-device-box pv-array-box' }, [
            h('div', { class: 'box-title' }, [
              `光伏方阵${side}`,
              layout ? h('span', { class: 'box-title-meta' }, ` ${layout}`) : null
            ]),
            ...lines.map(t => h('div', { class: 'box-line' }, t)),
            h('div', { class: 'box-controls' }, [
              h('div', { class: 'power-row' }, [
                h('label', { class: 'power-label' }, '温度℃'),
                h('input', {
                  type: 'text', inputMode: 'decimal', class: 'power-input',
                  value: getDraft(draftHost, 't', live.ambientTemperatureC ?? 25),
                  onInput: (e) => setDraft(draftHost, 't', e.target.value)
                }),
                h('button', {
                  type: 'button', class: 'act-btn act-set',
                  onClick: (e) => {
                    e.stopPropagation()
                    const temperatureC = Number(getDraft(draftHost, 't', live.ambientTemperatureC))
                    if (!Number.isFinite(temperatureC)) return
                    emit('pv-set-temp', { pvNumber, side, temperatureC })
                  }
                }, '设定')
              ]),
              h('div', { class: 'power-row' }, [
                h('label', { class: 'power-label' }, '入射角°'),
                h('input', {
                  type: 'text', inputMode: 'decimal', class: 'power-input',
                  value: getDraft(draftHost, 'ang', live.incidenceAngleDeg ?? 90),
                  onInput: (e) => setDraft(draftHost, 'ang', e.target.value)
                }),
                h('button', {
                  type: 'button', class: 'act-btn act-set',
                  onClick: (e) => {
                    e.stopPropagation()
                    const angleDeg = Number(getDraft(draftHost, 'ang', live.incidenceAngleDeg))
                    if (!Number.isFinite(angleDeg)) return
                    emit('pv-set-angle', { pvNumber, side, angleDeg })
                  }
                }, '设定')
              ])
            ])
          ])
        ])
      ])
    }
  }
})

const PcsCard = defineComponent({
  props: { channel: Object, side: String, x: Number, busY: Number, y: Number, h: Number },
  emits: ['pcs-start', 'pcs-stop', 'pcs-set-power', 'pcs-set-reactive'],
  setup(p, { emit }) {
    return () => {
      const ch = p.channel
      const halfW = BOX_W / 2
      const lines = [
        ch.pcsDeviceState, ch.pcsStartStop, ch.pcsTargetP, ch.pcsActualP,
        ch.pcsTargetQ, ch.pcsActualQ, ch.pcsBlackStart,
        ch.pcsGridMode !== '—' ? `模式:${ch.pcsGridMode}` : null
      ].filter(Boolean)
      return h('g', { transform: `translate(${p.x}, 0)` }, [
        // 母线 —引线— PCS（禁止设备贴连母线）
        h('line', { class: 'bus-line', x1: 0, y1: p.busY ?? 96, x2: 0, y2: p.y }),
        h('foreignObject', { x: -halfW, y: p.y, width: BOX_W, height: p.h }, [
          h('div', { xmlns: 'http://www.w3.org/1999/xhtml', class: 'svg-device-box pcs-box' }, [
            h('div', { class: 'box-title' }, `PCS-${p.side} (PCS${ch.pcsNumber})`),
            ...lines.map(t => h('div', { class: 'box-line' }, t)),
            h('div', { class: 'box-controls' }, [
              h('div', { class: 'power-row' }, [
                h('label', { class: 'power-label' }, 'P设(kW)'),
                h('input', {
                  type: 'text', inputMode: 'decimal', class: 'power-input',
                  value: getDraft(ch, 'p', ch.targetActivePowerKw),
                  onInput: (e) => setDraft(ch, 'p', e.target.value)
                }),
                h('button', {
                  type: 'button', class: 'act-btn act-set',
                  onClick: (e) => {
                    e.stopPropagation()
                    const kw = Number(getDraft(ch, 'p', ch.targetActivePowerKw))
                    if (!Number.isFinite(kw)) return
                    emit('pcs-set-power', { pcsNumber: ch.pcsNumber, emuUnit: ch.emuUnitNumber, ytPoint: ch.activePowerYtPoint, powerKw: kw })
                  }
                }, '设定')
              ]),
              h('div', { class: 'power-row' }, [
                h('label', { class: 'power-label' }, 'Q设(kvar)'),
                h('input', {
                  type: 'text', inputMode: 'decimal', class: 'power-input',
                  value: getDraft(ch, 'q', ch.targetReactivePowerKvar),
                  onInput: (e) => setDraft(ch, 'q', e.target.value)
                }),
                h('button', {
                  type: 'button', class: 'act-btn act-set',
                  onClick: (e) => {
                    e.stopPropagation()
                    const kvar = Number(getDraft(ch, 'q', ch.targetReactivePowerKvar))
                    if (!Number.isFinite(kvar)) return
                    emit('pcs-set-reactive', { pcsNumber: ch.pcsNumber, emuUnit: ch.emuUnitNumber, ytPoint: ch.reactivePowerYtPoint, reactiveKvar: kvar })
                  }
                }, '设定')
              ]),
              h('div', { class: 'box-actions' }, [
                h('button', { type: 'button', class: 'act-btn act-on', onClick: (e) => { e.stopPropagation(); emit('pcs-start', ch.pcsNumber) } }, '启动'),
                h('button', { type: 'button', class: 'act-btn act-off', onClick: (e) => { e.stopPropagation(); emit('pcs-stop', ch.pcsNumber) } }, '停机')
              ])
            ])
          ])
        ])
      ])
    }
  }
})

const BmsCard = defineComponent({
  props: { channel: Object, label: String, x: Number, y: Number, h: Number },
  emits: ['bms-power-on', 'bms-power-off', 'bms-fault-clear', 'bms-set-soc'],
  setup(p, { emit }) {
    return () => {
      const ch = p.channel
      const halfW = BOX_W / 2
      const lines = [
        ch.bmsCompact,
        ch.bmsRunStatus || '运行:—',
        ch.bmsAirConditioner,
        ch.bmsEnergy || `累计充 ${(ch.cumulativeChargeEnergyKwh ?? 0).toFixed(1)} / 放 ${(ch.cumulativeDischargeEnergyKwh ?? 0).toFixed(1)} kWh`,
        `并网:${ch.gridConnect}`,
        ch.bmsBlackStart
      ].filter(Boolean)
      return h('g', { transform: `translate(${p.x}, 0)` }, [
        h('foreignObject', { x: -halfW, y: p.y, width: BOX_W, height: p.h }, [
          h('div', { xmlns: 'http://www.w3.org/1999/xhtml', class: 'svg-device-box bms-box' }, [
            h('div', { class: 'box-title' }, p.label || `BMS 舱${ch.compartmentNumber}`),
            ...lines.map(t => h('div', { class: 'box-line' }, t)),
            h('div', { class: 'box-controls' }, [
              h('div', { class: 'power-row' }, [
                h('label', { class: 'power-label' }, 'SOC(%)'),
                h('input', {
                  type: 'text', inputMode: 'decimal', class: 'power-input',
                  value: getDraft(ch, 'soc', ch.socPercent),
                  onInput: (e) => setDraft(ch, 'soc', e.target.value),
                  onKeydown: (e) => {
                    if (e.key === 'Enter') {
                      e.preventDefault()
                      const pct = Number(getDraft(ch, 'soc', ch.socPercent))
                      if (!Number.isFinite(pct)) return
                      emit('bms-set-soc', { bmsNumber: ch.compartmentNumber, socPercent: pct })
                    }
                  }
                }),
                h('button', {
                  type: 'button', class: 'act-btn act-set',
                  onClick: (e) => {
                    e.stopPropagation()
                    const pct = Number(getDraft(ch, 'soc', ch.socPercent))
                    if (!Number.isFinite(pct)) return
                    emit('bms-set-soc', { bmsNumber: ch.compartmentNumber, socPercent: pct })
                  }
                }, '设定')
              ]),
              h('div', { class: 'box-actions' }, [
                h('button', { type: 'button', class: 'act-btn act-on', onClick: (e) => { e.stopPropagation(); emit('bms-power-on', ch.compartmentNumber) } }, '上电'),
                h('button', { type: 'button', class: 'act-btn act-off', onClick: (e) => { e.stopPropagation(); emit('bms-power-off', ch.compartmentNumber) } }, '下电')
              ]),
              h('div', { class: 'box-actions' }, [
                h('button', { type: 'button', class: 'act-btn act-clear', onClick: (e) => { e.stopPropagation(); emit('bms-fault-clear', ch.compartmentNumber) } }, '故障清除')
              ])
            ])
          ])
        ])
      ])
    }
  }
})
</script>

<style scoped>
.mainline-zoom-root { width: 100%; }
.mainline-toolbar {
  display: flex; align-items: center; gap: 12px; flex-wrap: wrap;
  margin-bottom: 8px; padding: 4px 0;
}
.zoom-slider { width: 140px; margin: 0 4px; }
.zoom-label { min-width: 52px; font-variant-numeric: tabular-nums; }
.project-tag {
  font-size: 12px; color: #409eff; background: #ecf5ff;
  border: 1px solid #d9ecff; border-radius: 4px; padding: 2px 8px;
}
.toolbar-hint { font-size: 12px; color: #909399; margin-left: auto; }
.mainline-viewport {
  overflow: hidden; width: 100%; height: min(78vh, 860px);
  border: 1px solid #ebeef5; border-radius: 6px; background: #fafbfc;
  position: relative; touch-action: none;
}
.mainline-viewport.is-panning { cursor: grabbing; user-select: none; }
.mainline-pan-layer { position: absolute; left: 0; top: 0; will-change: transform; }
.mainline-svg { display: block; flex-shrink: 0; }
.breaker-hit { cursor: pointer; }
.breaker-hit:hover .breaker-label { fill: #1e6abc; font-weight: 600; }
.breaker-label { font-size: 11px; }
.label-text { font-size: 11px; fill: #303133; }
.value-text { font-size: 10px; fill: #606266; }
.muted-text { fill: #909399; }
.bus-line { stroke: #000; stroke-width: 2; fill: none; }
.bus-thick { stroke-width: 3.5; }
.meter-box { fill: #f5eef8; stroke: #8e44ad; stroke-width: 1.5; }
.emu-overlay { pointer-events: none; }
.group-box {
  fill: rgba(64, 158, 255, 0.04);
  stroke: #909399;
  stroke-width: 1.5;
  stroke-dasharray: 6 4;
  rx: 8;
}
.runtime-missing-hint {
  font-size: 11px;
  font-weight: 600;
  fill: #e6a23c;
  pointer-events: none;
}
.mainline-blackstart {
  display: flex; flex-wrap: wrap; align-items: center; gap: 6px 8px;
  margin-top: 8px; padding: 8px 10px; border: 1px solid #ebeef5;
  border-radius: 6px; background: #fafbfc;
}
.footer-label { font-size: 12px; font-weight: 600; color: #606266; margin-right: 2px; }
.bs-chip {
  font-size: 11px; line-height: 1.2; padding: 2px 8px; border-radius: 10px;
  border: 1px solid #dcdfe6; background: #fff; color: #606266; white-space: nowrap;
}
.bs-chip.bs-on { border-color: #b3e19d; background: #f0f9eb; color: #529b2e; }
.bs-chip.bs-partial { border-color: #f3d19e; background: #fdf6ec; color: #b88230; }
.bs-chip.bs-off { border-color: #e4e7ed; background: #f4f4f5; color: #909399; }
.mainline-legend {
  display: flex; flex-wrap: wrap; align-items: center; gap: 6px 14px;
  margin-top: 6px; font-size: 11px; color: #909399;
}
.mainline-legend .legend-line {
  display: inline-block; width: 18px; height: 0; margin-right: 4px;
  vertical-align: middle; border-top-width: 2px; border-top-style: solid;
}
.mainline-legend .legend-closed { border-top-color: #67c23a; }
.mainline-legend .legend-open { border-top-color: #f56c6c; border-top-style: dashed; }
</style>

<style>
.svg-device-box {
  font-size: 10px; line-height: 1.4; padding: 5px 6px; border-radius: 4px;
  box-sizing: border-box; word-break: break-word; height: 100%;
  display: flex; flex-direction: column;
}
.svg-device-box.pcs-box { background: #eef5ff; border: 1px solid #1e6abc; color: #303133; }
.svg-device-box.bms-box { background: #fff7e6; border: 1px solid #e6a23c; color: #303133; }
.svg-device-box.pv-array-box {
  background: #fffbeb; border: 1px solid #e6a817; color: #303133;
  padding: 3px 5px; font-size: 10px; line-height: 1.2;
  height: auto;
}
.svg-device-box.pv-array-box .box-title { margin-bottom: 1px; white-space: nowrap; }
.svg-device-box.pv-array-box .box-title-meta { font-weight: 500; color: #606266; }
.svg-device-box.pv-array-box .box-line { margin-bottom: 0; line-height: 1.2; }
.svg-device-box.pv-array-box .box-controls { margin-top: 3px; padding-top: 0; }
.svg-device-box.pv-array-box .power-row { margin-top: 3px; height: 18px; }
.svg-device-box.pv-array-box .power-input { height: 18px; padding: 0 3px; }
.svg-device-box.pv-array-box .act-btn { height: 18px; padding: 0 4px; line-height: 16px; }
.svg-device-box.pv-xfmr-box {
  background: #f3faf3; border: 1px solid #529b2e; color: #303133;
  padding: 3px 5px; font-size: 10px; line-height: 1.2;
  height: auto;
}
.svg-device-box.pv-xfmr-box .box-title { margin-bottom: 1px; }
.svg-device-box.pv-xfmr-box .box-line { margin-bottom: 0; line-height: 1.2; }
.svg-device-box.pv-xfmr-box .box-controls { margin-top: 3px; padding-top: 0; }
.svg-device-box.pv-xfmr-box .power-row { margin-top: 3px; height: 18px; }
.svg-device-box.pv-xfmr-box .power-input { height: 18px; padding: 0 3px; }
.svg-device-box.pv-xfmr-box .act-btn { height: 18px; padding: 0 4px; line-height: 16px; }
.svg-device-box.pv-xfmr-box .box-actions { margin-top: 3px; }
.svg-device-box.placeholder-box {
  opacity: 0.85;
  border-style: dashed;
  justify-content: center;
}
.svg-device-box .box-title { font-weight: 700; margin-bottom: 3px; color: #1e3a5f; }
.svg-device-box .box-line { white-space: normal; overflow: visible; margin-bottom: 1px; }
.svg-device-box .box-line.muted { color: #909399; }
.svg-device-box .box-actions { display: flex; gap: 4px; margin-top: 4px; }
.svg-device-box .box-controls { margin-top: auto; padding-top: 4px; }
.svg-device-box .act-btn {
  flex: 1; font-size: 10px; line-height: 1.2; padding: 2px 0; border-radius: 3px;
  border: 1px solid #c0c4cc; background: #fff; cursor: pointer; color: #303133;
}
.svg-device-box .act-btn:hover { border-color: #409eff; color: #409eff; }
.svg-device-box .act-btn.act-on:hover { border-color: #67c23a; color: #67c23a; }
.svg-device-box .act-btn.act-off:hover { border-color: #e6a23c; color: #e6a23c; }
.svg-device-box .act-btn.act-clear { flex: 1; }
.svg-device-box .act-btn.act-clear:hover { border-color: #f56c6c; color: #f56c6c; }
.svg-device-box .power-row { display: flex; align-items: center; gap: 3px; margin-top: 4px; }
.svg-device-box .power-label { font-size: 9px; color: #606266; white-space: nowrap; }
.svg-device-box .power-input {
  flex: 1; min-width: 0; font-size: 10px; padding: 1px 3px;
  border: 1px solid #c0c4cc; border-radius: 3px; box-sizing: border-box;
}
.svg-device-box .act-btn.act-set { flex: 0 0 auto; min-width: 32px; padding: 2px 4px; }
.svg-device-box .act-btn.act-set:hover { border-color: #409eff; color: #409eff; }
</style>
