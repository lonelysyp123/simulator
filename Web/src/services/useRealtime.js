import { onMounted, onBeforeUnmount, ref } from 'vue'
import { getHub } from './api.js'
import { RealtimeMethods, RealtimeChannels } from './constants.js'

/// 订阅 SignalR 频道并绑定回调，组件卸载时自动退订。
/// channel: 频道名（RealtimeChannels）；extra: 额外 group 后缀（如电池单元号）
/// handlers: { methodName: callback }
export function useRealtime(channel, handlers, extra = '') {
  const connected = ref(false)
  let conn = null
  const groupName = extra ? `${channel}.${extra}` : channel

  onMounted(async () => {
    try {
      conn = await getHub()
      await conn.invoke('JoinChannel', groupName)
      connected.value = true
      for (const [method, cb] of Object.entries(handlers)) {
        conn.on(method, cb)
      }
    } catch (e) {
      console.warn('SignalR 连接失败', e)
    }
  })

  onBeforeUnmount(async () => {
    try {
      if (conn) {
        for (const method of Object.keys(handlers)) conn.off(method)
        await conn.invoke('LeaveChannel', groupName)
      }
    } catch { /* ignore */ }
  })

  return { connected }
}

export { RealtimeMethods, RealtimeChannels }
