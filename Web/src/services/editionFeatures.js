import { getConfig } from './api.js'

/** @type {{ allowDroopSlices: boolean, allowMainline3d: boolean, allowTopologyEditor: boolean }} */
let features = {
  allowDroopSlices: false,
  allowMainline3d: false,
  allowTopologyEditor: false
}

let loadPromise = null

export function getEditionFeatures() {
  return features
}

/** 从 /api/config 拉取档位能力（可重复调用，只请求一次）。 */
export function loadEditionFeatures() {
  if (!loadPromise) {
    loadPromise = getConfig()
      .then(cfg => {
        features = {
          allowDroopSlices: cfg?.edition?.allowDroopSlices === true,
          allowMainline3d: cfg?.edition?.allowMainline3d === true,
          allowTopologyEditor: cfg?.edition?.allowTopologyEditor === true
        }
        return features
      })
      .catch(() => features)
  }
  return loadPromise
}

const TOPOLOGY_PATHS = ['/topology', '/projects', '/system']

/** 当前档位是否允许访问该路由。 */
export function isEditionRouteAllowed(path) {
  if (!path) return true
  if (path === '/mainline-3d' || path.startsWith('/mainline-3d/')) {
    return features.allowMainline3d
  }
  if (path === '/droop-slices' || path.startsWith('/droop-slices/')) {
    return features.allowDroopSlices
  }
  if (TOPOLOGY_PATHS.some(p => path === p || path.startsWith(`${p}/`))) {
    return features.allowTopologyEditor
  }
  return true
}
