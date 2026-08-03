import axios from 'axios'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'

const api = axios.create({ baseURL: '/api', timeout: 10000 })

api.interceptors.response.use(
  r => r,
  err => {
    const msg = err?.response?.data?.message || err.message
    return Promise.reject(new Error(msg))
  }
)

export async function getHealth() { return (await api.get('/health')).data }
export async function getMainLine() { return (await api.get('/mainline')).data }
export async function getBattery(unit) { return (await api.get(`/battery/${unit}`)).data }
export async function getCells(unit, cluster) { return (await api.get(`/cells/${unit}/${cluster}`)).data }
export async function getRackThresholds(unit, rack = 0) {
  return (await api.get(`/bms/${unit}/rack-thresholds`, { params: { rack } })).data
}
export async function getAlarms() { return (await api.get('/alarms')).data }
export async function getBmsAlarms(unit, rack) {
  return (await api.get(`/alarms/bms/${unit}`, { params: rack == null ? {} : { rack } })).data
}
export async function getConnections() { return (await api.get('/connections')).data }
export async function getAlert() { return (await api.get('/alert')).data }
export async function getConfig() { return (await api.get('/config')).data }
export async function getProtocol() { return (await api.get('/protocol')).data }
export async function getAutoTest() { return (await api.get('/autotest')).data }
export async function getPointMaps() { return (await api.get('/pointmaps')).data }
export async function postCommand(input) { return (await api.post('/command', { input })).data }
export async function postLink(target, state) { return (await api.post(`/link/${target}/${state}`)).data }
export async function postMainBreaker(closed) { return (await api.post(`/breaker/main/${closed}`)).data }
export async function postUnitBreaker(unit, closed) { return (await api.post(`/breaker/unit/${unit}/${closed}`)).data }
export async function postDpcTest(name) { return (await api.post(`/dpctest/${name}`)).data }

export async function getDroopSliceStatus() { return (await api.get('/droop-slices/status')).data }
export async function getDroopSlices(limit = 100, offset = 0) {
  return (await api.get('/droop-slices', { params: { limit, offset } })).data
}
export async function getDroopSlice(id) { return (await api.get(`/droop-slices/${id}`)).data }
export async function clearDroopSlices() { return (await api.post('/droop-slices/clear')).data }
export async function setDroopSliceConfig(body) {
  return (await api.post('/droop-slices/config', body)).data
}

// 组态编辑
export async function getTopologyTemplates() { return (await api.get('/topology/templates')).data }
export async function getTopologyProject() { return (await api.get('/topology/project')).data }
export async function putTopologyProject(project) { return (await api.put('/topology/project', project)).data }
export async function postTopologyConnect(body) { return (await api.post('/topology/connect', body)).data }
export async function postTopologyDisconnect(body) { return (await api.post('/topology/disconnect', body)).data }
export async function getTopologyLibrary() { return (await api.get('/topology/library')).data }
export async function putTopologyLibrary(item) { return (await api.put('/topology/library', item)).data }
export async function deleteTopologyLibrary(id) { return (await api.delete(`/topology/library/${id}`)).data }
export async function getTopologyPaths() { return (await api.get('/topology/paths')).data }

let hubPromise = null
export function getHub() {
  if (!hubPromise) {
    const conn = new HubConnectionBuilder()
      .withUrl('/hub/realtime')
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()
    hubPromise = conn.start().then(() => conn).catch(err => {
      hubPromise = null
      throw err
    })
  }
  return hubPromise
}

export { api }
