/** 3D 主接线布局常量与坐标计算（Y 向上） */

export const UNIT_SPACING = 22
export const CHANNEL_OFFSET_X = 5.5

/** 主干沿 -Z → +Z：电网 → 主断 → 主变 → 35kV 母线 */
export const Z = {
  grid: -22,
  mainBreaker: -14,
  mainXf: -7,
  bus35: 0,
  unitBreaker: 4,
  unitXf: 9,
  bus690: 13,
  pcs: 18,
  bms: 26
}

export const Y = {
  ground: 0,
  /**
   * 设备间电缆贴地走线高度（管心）
   * 须高于水泥垫/基座顶面 + 最大管径半径，避免刚体段陷入地面
   */
  cable: 0.55,
  bus35: 4.2,
  bus690: 2.4,
  equipment: 1.2,
  label: 6.5
}

/**
 * @param {number} unitCount
 * @returns {{ mainX: number, busStartX: number, busEndX: number, unitXs: number[], spacing: number }}
 */
export function computeLayout(unitCount) {
  const n = Math.max(0, unitCount | 0)
  // 单元多时略压缩间距，避免场景过宽难适配
  const spacing = n >= 16 ? 16 : n >= 10 ? 18 : UNIT_SPACING
  const unitXs = []
  for (let i = 0; i < n; i++) {
    unitXs.push(i * spacing)
  }
  const busStartX = n > 0 ? unitXs[0] - 2 : 0
  const busEndX = n > 0 ? unitXs[n - 1] + 2 : 8
  const mainX = n > 0 ? unitXs[0] : 0
  return { mainX, busStartX, busEndX, unitXs, spacing }
}

/**
 * @param {number} unitX
 * @param {'A'|'B'} side
 */
export function channelX(unitX, side) {
  return unitX + (side === 'A' ? -CHANNEL_OFFSET_X : CHANNEL_OFFSET_X)
}
