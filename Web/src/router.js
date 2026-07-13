import { createRouter, createWebHistory } from 'vue-router'

const routes = [
  { path: '/', redirect: '/mainline' },
  { path: '/mainline', name: 'mainline', component: () => import('./views/MainLineView.vue'), meta: { title: '主电气接线' } },
  { path: '/battery', name: 'battery', component: () => import('./views/BatteryView.vue'), meta: { title: '电池堆簇信息' } },
  { path: '/cells', name: 'cells', component: () => import('./views/CellsView.vue'), meta: { title: '电池单体信息' } },
  { path: '/command', name: 'command', component: () => import('./views/CommandView.vue'), meta: { title: '命令输入' } },
  { path: '/connections', name: 'connections', component: () => import('./views/ConnectionsView.vue'), meta: { title: '连接信息' } }
]

export default createRouter({
  history: createWebHistory(),
  routes
})
