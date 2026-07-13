<template>
  <div>
    <div class="card">
      <el-select v-model="unitNumber" style="width:140px" @change="reload">
        <el-option v-for="i in unitCount" :key="i" :label="`舱 ${i}`" :value="i" />
      </el-select>
      <el-select v-model="clusterNumber" style="width:140px;margin-left:12px" @change="reload">
        <el-option v-for="i in clusterCount" :key="i" :label="`簇 ${i}`" :value="i" />
      </el-select>
      <el-button style="margin-left:12px" @click="reload">刷新</el-button>
      <el-tag v-if="data" style="margin-left:12px" size="small">
        单体最高 簇{{ clusterNumber }} 包{{ data.maxCellVoltagePackId }} 单体{{ data.maxCellVoltageCellId }}
        {{ data.maxCellVoltage.toFixed(3) }} V
        /
        最低 簇{{ clusterNumber }} 包{{ data.minCellVoltagePackId }} 单体{{ data.minCellVoltageCellId }}
        {{ data.minCellVoltage.toFixed(3) }} V
      </el-tag>
    </div>

    <div class="card" v-if="data">
      <div class="cell-grid">
        <div class="cell-pack" v-for="(pack, pi) in data.packs" :key="pi">
          <div class="pack-title">包 {{ pi }}（{{ data.cellsPerPack }} 节）</div>
          <div class="cell-grid-inner">
            <div
              v-for="(v, ci) in pack"
              :key="ci"
              class="cell-box"
              :class="cellClass(pi, ci)"
              :title="`簇${clusterNumber} 包${pi} 单体${ci} ${formatVoltage(v)}`"
            >
              {{ formatVoltage(v) }}
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { getCells, getConfig } from '@/services/api.js'

const unitNumber = ref(1)
const clusterNumber = ref(1)
const unitCount = ref(1)
const clusterCount = ref(12)
const data = ref(null)

function formatVoltage(v) {
  return v > 0 ? v.toFixed(3) : '—'
}

function cellClass(packIndex, cellIndex) {
  const d = data.value
  if (!d) return 'cell-empty'
  const v = d.packs?.[packIndex]?.[cellIndex]
  if (!v || v <= 0) return 'cell-empty'
  if (packIndex === d.maxCellVoltagePackId && cellIndex === d.maxCellVoltageCellId) return 'cell-max'
  if (packIndex === d.minCellVoltagePackId && cellIndex === d.minCellVoltageCellId) return 'cell-min'
  return 'cell-normal'
}

async function reload() {
  try { data.value = await getCells(unitNumber.value, clusterNumber.value) } catch (e) { console.warn(e) }
}

onMounted(async () => {
  try {
    const cfg = await getConfig()
    unitCount.value = cfg.simulator.channelCount || (cfg.simulator.unitCount * 2) || 1
    if (unitNumber.value > unitCount.value) unitNumber.value = unitCount.value
  } catch { /* ignore */ }
  await reload()
})
</script>

<style scoped>
.cell-box.cell-normal {
  background: #8bc34a;
  color: #fff;
}

.cell-box.cell-max {
  background: #409eff;
  color: #fff;
}

.cell-box.cell-min {
  background: #f56c6c;
  color: #fff;
}

.cell-box.cell-empty {
  background: #c0c4cc;
  color: #fff;
}
</style>
