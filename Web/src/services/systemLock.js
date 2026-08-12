import { reactive, readonly } from 'vue'

/**
 * 系统重新初始化全局锁：阻止侧栏切换、路由跳转及其他交互，并展示进度。
 */
const state = reactive({
  locked: false,
  message: '',
  /** 0–100，估算进度 */
  progress: 0,
  /** 阶段文案，如「提交配置」「等待重启」 */
  stage: ''
})

export function lockSystem(message = '正在重新初始化仿真，请稍候…', progress = 5, stage = '') {
  state.locked = true
  state.message = message
  state.progress = clampProgress(progress)
  state.stage = stage || message
}

export function updateSystemProgress(progress, message, stage) {
  if (!state.locked) return
  if (progress != null) state.progress = clampProgress(progress)
  if (message != null) state.message = message
  if (stage != null) state.stage = stage
}

export function unlockSystem() {
  state.locked = false
  state.message = ''
  state.progress = 0
  state.stage = ''
}

export function isSystemLocked() {
  return state.locked
}

function clampProgress(v) {
  const n = Number(v)
  if (!Number.isFinite(n)) return 0
  return Math.max(0, Math.min(100, Math.round(n)))
}

export const systemLock = readonly(state)
