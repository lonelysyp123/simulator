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
          <!-- 站侧：电网 → 主断(组态角色) → HV母线 → 主变 / 并网点电表 → LV母线 -->
          <g class="station">
            <!-- 电网本体 -->
            <text :x="layout.stationCenterX + 14" :y="layout.yGrid + 12" class="label-text">{{ layout.grid?.label || '电网' }}</text>
            <text :x="layout.stationCenterX + 14" :y="layout.yGrid + 28" class="value-text">
              PCC {{ fmtVolt(snap.pccLineVoltageV) }} / {{ fmtHz(snap.systemFrequencyHz) }}
            </text>
            <text :x="layout.stationCenterX + 14" :y="layout.yGrid + 42" class="value-text muted-text">
              设定 {{ fmtVolt(snap.gridNominalLineVoltageV) }} / {{ fmtHz(snap.gridNominalFrequencyHz) }}
            </text>

            <!-- 规则：设备间须有黑线引线；母线下方仅 1 挂时可省略母线直连 -->
            <template v-if="layout.hasMainBreaker">
              <line
                class="bus-line"
                :x1="layout.stationCenterX"
                :y1="layout.yGrid + 8"
                :x2="layout.stationCenterX"
                :y2="layout.yBrkTop"
              />
              <g class="breaker-hit" @click="$emit('toggle-main-breaker')">
                <rect
                  :x="layout.stationCenterX - 18"
                  :y="layout.yBrkTop"
                  width="36"
                  :height="layout.yBrkBottom - layout.yBrkTop"
                  rx="3"
                  fill="transparent"
                />
                <BreakerSymbol
                  :x="layout.stationCenterX"
                  :y="layout.yMainBreaker"
                  :closed="snap.mainBreakerClosed"
                  :tripped="snap.mainBreakerTripped"
                />
                <text :x="layout.stationCenterX + 22" :y="layout.yMainBreaker + 4" class="label-text breaker-label">
                  主断 {{ snap.mainBreakerLabel || fmtBreaker(snap.mainBreakerClosed, snap.mainBreakerTripped) }}
                </text>
              </g>
              <!-- 主断 —引线— [HV母线|主变] -->
              <line
                class="bus-line"
                :x1="layout.stationCenterX"
                :y1="layout.yBrkBottom"
                :x2="layout.omitBusHv ? layout.xfmrX : layout.stationCenterX"
                :y2="layout.omitBusHv ? layout.yXfmr : layout.yBusHv"
              />
            </template>
            <template v-else>
              <line
                class="bus-line"
                :x1="layout.stationCenterX"
                :y1="layout.yGrid + 8"
                :x2="layout.omitBusHv ? layout.xfmrX : layout.stationCenterX"
                :y2="layout.omitBusHv ? layout.yXfmr : layout.yBusHv"
              />
            </template>

            <!-- HV 母线：下方挂接 >1 时绘制（主变/电表/负载） -->
            <template v-if="!layout.omitBusHv">
              <line class="bus-line bus-thick" :x1="layout.busLeft" :y1="layout.yBusHv" :x2="layout.busRight" :y2="layout.yBusHv" />
              <text :x="layout.busLeft" :y="layout.yBusHv - 8" class="label-text">
                {{ layout.busHvLabel }} · {{ fmtVolt(snap.pccLineVoltageV) }}
              </text>
              <line
                class="bus-line"
                :x1="layout.xfmrX"
                :y1="layout.yBusHv"
                :x2="layout.xfmrX"
                :y2="layout.yXfmr"
              />
            </template>

            <!-- 负载：挂点由组态连通决定（HV 或 LV 母线），绑定概览遥测 -->
            <g v-if="layout.hasLoad && layout.loadAttachSide === 'hv'">
              <line
                class="bus-line"
                :x1="layout.loadX"
                :y1="layout.yBusHv"
                :x2="layout.loadX"
                :y2="layout.yBusHv + layout.loadStub"
              />
              <LoadSymbol :x="layout.loadX" :y="layout.yBusHv + layout.loadStub + layout.loadSymbolH / 2" />
              <text :x="layout.loadX + 16" :y="layout.yBusHv + layout.loadStub + 12" class="label-text">
                {{ layout.loadLabel }}
              </text>
              <text :x="layout.loadX + 16" :y="layout.yBusHv + layout.loadStub + 28" class="value-text">
                P {{ fmtKw(snap.loadActivePowerKw) }}
              </text>
              <text :x="layout.loadX + 16" :y="layout.yBusHv + layout.loadStub + 42" class="value-text">
                Q {{ fmtKvar(snap.loadReactivePowerKvar) }}
              </text>
            </g>

            <TransformerSymbol :x="layout.xfmrX" :y="layout.yXfmr + layout.xfmrSpan / 2" />
            <text
              :x="layout.xfmrLabelSide === 'left' ? layout.xfmrX - 22 : layout.xfmrX + 22"
              :y="layout.yXfmr + 14"
              :text-anchor="layout.xfmrLabelSide === 'left' ? 'end' : 'start'"
              class="label-text"
            >
              {{ layout.mainXfmr?.label || '主变' }} {{ layout.xfmrLabel }}
            </text>
            <text
              :x="layout.xfmrLabelSide === 'left' ? layout.xfmrX - 22 : layout.xfmrX + 22"
              :y="layout.yXfmr + 30"
              :text-anchor="layout.xfmrLabelSide === 'left' ? 'end' : 'start'"
              class="value-text"
            >
              {{ fmtVolt(snap.mainTransformerSecondary?.lineVoltageV) }}
            </text>

            <!-- 主变 —引线— [LV母线|单元断] -->
            <line
              class="bus-line"
              :x1="layout.xfmrX"
              :y1="layout.yXfmr + layout.xfmrSpan"
              :x2="layout.omitBusLv ? layout.stationCenterX : layout.xfmrX"
              :y2="layout.yBusLv"
            />

            <!-- LV 母线：下方挂接 >1（单元/LV 负载）时绘制 -->
            <template v-if="!layout.omitBusLv">
              <line class="bus-line bus-thick" :x1="layout.busLeft" :y1="layout.yBusLv" :x2="layout.busRight" :y2="layout.yBusLv" />
              <text :x="layout.busLeft" :y="layout.yBusLv - 8" class="label-text">
                {{ layout.busLvLabel }} · {{ fmtVolt(snap.stationBus35LineVoltageV) }}
              </text>
            </template>

            <g v-if="layout.hasLoad && layout.loadAttachSide === 'lv'">
              <line
                class="bus-line"
                :x1="layout.loadX"
                :y1="layout.yBusLv"
                :x2="layout.loadX"
                :y2="layout.yBusLv + layout.loadStub"
              />
              <LoadSymbol :x="layout.loadX" :y="layout.yBusLv + layout.loadStub + layout.loadSymbolH / 2" />
              <text :x="layout.loadX + 16" :y="layout.yBusLv + layout.loadStub + 12" class="label-text">
                {{ layout.loadLabel }}
              </text>
              <text :x="layout.loadX + 16" :y="layout.yBusLv + layout.loadStub + 28" class="value-text">
                P {{ fmtKw(snap.loadActivePowerKw) }}
              </text>
              <text :x="layout.loadX + 16" :y="layout.yBusLv + layout.loadStub + 42" class="value-text">
                Q {{ fmtKvar(snap.loadReactivePowerKvar) }}
              </text>
            </g>

            <!-- 并网点电表（挂在 HV 母线/交接点上） -->
            <g v-if="layout.hasPccMeter">
              <line
                class="bus-line"
                :x1="layout.meterX"
                :y1="layout.yBusHv"
                :x2="layout.meterX"
                :y2="layout.yXfmr"
              />
              <rect
                :x="layout.meterX - 32"
                :y="layout.yXfmr"
                width="64"
                :height="layout.meterH"
                rx="4"
                class="meter-box"
              />
              <text :x="layout.meterX" :y="layout.yXfmr + 18" text-anchor="middle" class="label-text">{{ layout.meterLabel }}</text>
              <text :x="layout.meterX" :y="layout.yXfmr + 34" text-anchor="middle" class="value-text">PT/CT</text>
              <text :x="layout.meterX" :y="layout.yXfmr + 48" text-anchor="middle" class="value-text">
                P {{ fmtKw(snap.meterPrimary?.activePowerKw) }}
              </text>
              <text :x="layout.meterX" :y="layout.yXfmr + 62" text-anchor="middle" class="value-text">
                Q {{ fmtKvar(snap.meterPrimary?.reactivePowerKvar) }}
              </text>
            </g>
          </g>

          <!-- 各 EMU：经典图例 + 组态 DC 并联（支路原点在 LV 母线上） -->
          <g
            v-for="u in layout.units"
            :key="u.index"
            :transform="`translate(${u.cx}, ${layout.yBusLv})`"
          >
            <!-- LV 母线 —引线— 单元断 —引线— 单元变 —引线— 690 母线（连线一律黑色） -->
            <line class="bus-line" x1="0" y1="0" x2="0" :y2="u.unitBrkTop" />

            <g class="breaker-hit" @click="$emit('toggle-unit-breaker', u.unitSnap?.unitIndex ?? u.index)">
              <rect x="-22" :y="u.unitBrkTop" width="44" :height="u.unitBrkBottom - u.unitBrkTop" rx="3" fill="transparent" />
              <BreakerSymbol :x="0" :y="u.unitBrkMid" :closed="!!u.unitSnap?.unitBreakerClosed" :tripped="!!u.unitSnap?.unitBreakerTripped" />
              <text x="26" :y="u.unitBrkMid + 4" class="label-text breaker-label">
                单元断 {{ u.unitSnap?.unitBreakerLabel || fmtBreaker(u.unitSnap?.unitBreakerClosed, u.unitSnap?.unitBreakerTripped) }}
              </text>
            </g>

            <line class="bus-line" x1="0" :y1="u.unitBrkBottom" x2="0" :y2="u.unitXfmrTop" />
            <TransformerSymbol :x="0" :y="u.unitXfmrTop + u.unitXfmrSpan / 2" :scale="0.85" />
            <text x="18" :y="u.unitXfmrTop + 14" class="label-text">单元变 35/690</text>
            <text x="18" :y="u.unitXfmrTop + 28" class="value-text">
              {{ u.unitSnap?.unitTransformerLine || fmtPhasorVi(u.unitSnap?.unitTransformerSecondary) }}
            </text>

            <!-- 单元变 —引线— [690母线|PCS]：仅 1 路 PCS 时省略 690 母线 -->
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
              <text :x="-u.channelX + 3" :y="u.unitBus690Y + 14" class="label-text">
                690V {{ fmtVolt(u.unitSnap?.bus690?.lineVoltageV ?? u.unitSnap?.unitTransformerSecondary?.lineVoltageV) }}
              </text>
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
          </g>

          <!-- EMU 虚线遮罩：仅框线，无文字；不拦截点击 -->
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
      <span>结构由组态连通/角色推导</span>
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
  'bms-set-soc'
])

const zoom = ref(DEFAULT_ZOOM)
const viewportRef = ref(null)
const panX = ref(0)
const panY = ref(0)
const isPanning = ref(false)
let panDragStart = { x: 0, y: 0, panX: 0, panY: 0 }

const layout = computed(() =>
  buildTopologyMainLineLayout(props.snap.topology, props.snap.units || [])
)

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
function fmtBreaker(closed, tripped) { return tripped ? '跳闸' : closed ? '合' : '分' }
function fmtPhasorVi(p) {
  if (!p) return '—'
  return `${fmtVolt(p.lineVoltageV)} / ${Number(p.lineCurrentA || 0).toFixed(1)}A / φ${Number(p.phaseAngleDeg || 0).toFixed(1)}°`
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

/** 经典单线图负载符号：折线阻抗 + 接地箭头 */
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
function draftKey(ch, kind) { return `${kind}-${ch?.pcsNumber ?? ch?.compartmentNumber ?? ''}` }
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
