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
      <el-tag style="margin-left:12px" size="small">单体最高 {{ data?.maxCellVoltage?.toFixed(3) }} V / 最低 {{ data?.minCellVoltage?.toFixed(3) }} V</el-tag>
    </div>

    <div class="card" v-if="data">
      <div class="cell-grid">
        <div class="cell-pack" v-for="(pack, pi) in data.packs" :key="pi">
          <div class="pack-title">包 {{ pi }}（{{ data.cellsPerPack }} 节）</div>
          <div class="cell-grid-inner">
            <div v-for="(v, ci) in pack" :key="ci" class="cell-box" :style="{ background: colorFor(v) }" :title="`${ci}# ${v.toFixed(3)} V`">
              {{ v > 0 ? v.toFixed(2) : '—' }}
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

function colorFor(v) {
  if (!v || v <= 0) return '#c0c4cc'
  // 单体电压范围 2.8 ~ 3.65 V，映射颜色
  const min = 2.8, max = 3.65
  const ratio = Math.min(1, Math.max(0, (v - min) / (max - min)))
  // 低=红，高=绿
  const hue = ratio * 120 // 0=红,120=绿
  return `hsl(${hue}, 70%, 45%)`
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
