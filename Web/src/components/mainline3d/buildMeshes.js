import * as THREE from 'three'
import { Z, Y, channelX } from './layout.js'
import { createPowerCable } from './powerFlow.js'

/**
 * 材质库：参考公共数字孪生/工业可视化中储能站设备配色
 * （油浸灰、PCS 蓝灰柜、集装箱黄/白、绝缘子釉色等）
 */
const MAT = {
  steel: () => new THREE.MeshStandardMaterial({ color: 0xa8b0bc, metalness: 0.62, roughness: 0.38 }),
  darkSteel: () => new THREE.MeshStandardMaterial({ color: 0x3e4654, metalness: 0.55, roughness: 0.42 }),
  galvanized: () => new THREE.MeshStandardMaterial({ color: 0x9aa3ad, metalness: 0.7, roughness: 0.32 }),
  concrete: () => new THREE.MeshStandardMaterial({ color: 0x7a8494, metalness: 0.05, roughness: 0.9 }),
  insulator: () => new THREE.MeshStandardMaterial({ color: 0xd4c4a0, metalness: 0.05, roughness: 0.55 }),
  insulatorBrown: () => new THREE.MeshStandardMaterial({ color: 0x8b5a2b, metalness: 0.08, roughness: 0.5 }),
  pcsBody: () => new THREE.MeshStandardMaterial({ color: 0xf5f7fa, metalness: 0.2, roughness: 0.55 }),
  pcsDoor: () => new THREE.MeshStandardMaterial({ color: 0xffffff, metalness: 0.18, roughness: 0.5 }),
  pcsAccent: () => new THREE.MeshStandardMaterial({ color: 0xe4e7ed, metalness: 0.25, roughness: 0.48 }),
  bmsWall: () => new THREE.MeshStandardMaterial({ color: 0xf5f7fa, metalness: 0.15, roughness: 0.6 }),
  bmsCorrugation: () => new THREE.MeshStandardMaterial({ color: 0xffffff, metalness: 0.18, roughness: 0.55 }),
  bmsDoor: () => new THREE.MeshStandardMaterial({ color: 0xf0f2f5, metalness: 0.2, roughness: 0.52 }),
  bmsRoof: () => new THREE.MeshStandardMaterial({ color: 0xe4e7ed, metalness: 0.3, roughness: 0.45 }),
  xfTank: () => new THREE.MeshStandardMaterial({ color: 0x6b7a88, metalness: 0.5, roughness: 0.4 }),
  xfRadiator: () => new THREE.MeshStandardMaterial({ color: 0x5a6878, metalness: 0.55, roughness: 0.38 }),
  boxXf: () => new THREE.MeshStandardMaterial({ color: 0xd8dde4, metalness: 0.25, roughness: 0.55 }),
  boxXfAccent: () => new THREE.MeshStandardMaterial({ color: 0x4a90c8, metalness: 0.3, roughness: 0.5 }),
  open: () => new THREE.MeshStandardMaterial({
    color: 0xf56c6c, metalness: 0.28, roughness: 0.4, emissive: 0x5c1a1a, emissiveIntensity: 0.3
  }),
  accent220: () => new THREE.MeshStandardMaterial({ color: 0xe0b83a, metalness: 0.45, roughness: 0.38 }),
  accent35: () => new THREE.MeshStandardMaterial({ color: 0x4fb5ba, metalness: 0.4, roughness: 0.4 }),
  ledGreen: () => new THREE.MeshStandardMaterial({
    color: 0x67c23a, emissive: 0x3a9a20, emissiveIntensity: 0.85, metalness: 0.1, roughness: 0.4
  }),
  ledAmber: () => new THREE.MeshStandardMaterial({
    color: 0xe6a23c, emissive: 0xb87a20, emissiveIntensity: 0.7, metalness: 0.1, roughness: 0.4
  }),
  glass: () => new THREE.MeshStandardMaterial({
    color: 0x88aacc, metalness: 0.1, roughness: 0.15, transparent: true, opacity: 0.45
  }),
  blackRubber: () => new THREE.MeshStandardMaterial({ color: 0x2a2a2a, metalness: 0.1, roughness: 0.85 })
}

function box(w, h, d, mat, y = h / 2) {
  const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), mat)
  m.position.y = y
  m.castShadow = true
  m.receiveShadow = true
  return m
}

function cyl(rTop, rBot, h, mat, y = h / 2, seg = 16) {
  const m = new THREE.Mesh(new THREE.CylinderGeometry(rTop, rBot, h, seg), mat)
  m.position.y = y
  m.castShadow = true
  m.receiveShadow = true
  return m
}

/** 盘形绝缘子串（公共变电站模型常见） */
function createInsulatorString(height = 1.4, scale = 1, brown = false) {
  const g = new THREE.Group()
  const mat = brown ? MAT.insulatorBrown() : MAT.insulator()
  const n = Math.max(3, Math.round(height / 0.28))
  for (let i = 0; i < n; i++) {
    const disc = cyl(0.22 * scale, 0.14 * scale, 0.1 * scale, mat, (i + 0.5) * (height / n), 12)
    g.add(disc)
  }
  const rod = cyl(0.04 * scale, 0.04 * scale, height, MAT.galvanized(), height / 2, 8)
  g.add(rod)
  return g
}

/** 220kV 门型进线构架 + 三相绝缘子串 */
export function createGridGantry(x) {
  const g = new THREE.Group()
  g.position.set(x, 0, Z.grid)

  // 双柱门架
  for (const sx of [-2.8, 2.8]) {
    const leg = box(0.45, 11, 0.45, MAT.galvanized(), 5.5)
    leg.position.x = sx
    g.add(leg)
    // 斜撑
    const brace = box(0.12, 4.5, 0.12, MAT.steel(), 3.2)
    brace.position.set(sx * 0.55, 0, 0)
    brace.rotation.z = sx > 0 ? 0.35 : -0.35
    g.add(brace)
  }
  // 横梁 + 上弦
  g.add(box(6.2, 0.4, 0.4, MAT.galvanized(), 11))
  g.add(box(6.2, 0.18, 0.18, MAT.steel(), 10.35))
  // 挂点横担
  const crossArm = box(5.5, 0.2, 0.55, MAT.accent220(), 9.6)
  g.add(crossArm)

  // 三相绝缘子 + 线夹示意
  for (let i = -1; i <= 1; i++) {
    const ins = createInsulatorString(1.5, 1.05, true)
    ins.position.set(i * 1.55, 8.0, 0)
    g.add(ins)
    const clamp = box(0.35, 0.18, 0.35, MAT.darkSteel(), 9.7)
    clamp.position.x = i * 1.55
    g.add(clamp)
  }

  // 基础墩
  for (const sx of [-2.8, 2.8]) {
    const pier = box(1.1, 0.55, 1.1, MAT.concrete(), 0.28)
    pier.position.x = sx
    g.add(pier)
  }

  g.userData.kind = 'grid'
  return g
}

/**
 * 户外断路器（三相柱式）：参考公共变电站模型
 * — 三相灭弧室柱 + 支撑绝缘子 + 机构箱
 */
export function createBreakerMesh(id, unitIndex = null) {
  const g = new THREE.Group()
  const bodyMat = MAT.open()

  const frame = box(2.8, 0.18, 1.6, MAT.galvanized(), 0.35)
  g.add(frame)
  const pad = box(3.2, 0.28, 2.0, MAT.concrete(), 0.14)
  g.add(pad)

  // 三相柱
  for (let i = -1; i <= 1; i++) {
    const pole = new THREE.Group()
    pole.position.x = i * 0.85

    const support = createInsulatorString(1.1, 0.85)
    support.position.y = 0.45
    pole.add(support)

    // 灭弧室筒
    const chamber = cyl(0.22, 0.26, 1.15, bodyMat, 2.15, 14)
    chamber.userData.isBreakerBody = true
    pole.add(chamber)

    // 上下出线绝缘子
    const topBush = createInsulatorString(0.7, 0.7)
    topBush.position.y = 2.7
    pole.add(topBush)
    const topCap = cyl(0.08, 0.1, 0.2, MAT.accent220(), 3.45, 8)
    pole.add(topCap)

    g.add(pole)
  }

  // 操作机构箱
  const mech = box(0.9, 1.1, 0.7, MAT.darkSteel(), 0.9)
  mech.position.set(0, 0, 1.05)
  g.add(mech)
  const mechDoor = box(0.75, 0.85, 0.05, MAT.steel(), 0.95)
  mechDoor.position.set(0, 0, 1.42)
  g.add(mechDoor)

  // 状态指示灯条（随合分变色的主材质取三相中柱）
  const indicator = box(0.5, 0.12, 0.08, MAT.ledAmber(), 1.55)
  indicator.position.set(0, 0, 1.45)
  g.add(indicator)

  g.userData.kind = 'breaker'
  g.userData.pickId = id
  g.userData.unitIndex = unitIndex
  g.userData.bodyMat = bodyMat
  g.userData.indicatorMat = indicator.material

  g.traverse(o => {
    if (o.isMesh) {
      o.userData.pickId = id
      o.userData.unitIndex = unitIndex
      if (o.userData.isBreakerBody) o.material = bodyMat
    }
  })
  return g
}

export function setBreakerVisual(group, { closed, tripped }) {
  const mat = group?.userData?.bodyMat
  const ind = group?.userData?.indicatorMat
  if (!mat) return
  if (tripped) {
    mat.color.setHex(0xf56c6c)
    mat.emissive.setHex(0x7a1f1f)
    mat.emissiveIntensity = 0.55
    if (ind) {
      ind.color.setHex(0xf56c6c)
      ind.emissive.setHex(0xaa2020)
      ind.emissiveIntensity = 1.0
    }
  } else if (closed) {
    mat.color.setHex(0x67c23a)
    mat.emissive.setHex(0x1a5c2a)
    mat.emissiveIntensity = 0.4
    if (ind) {
      ind.color.setHex(0x67c23a)
      ind.emissive.setHex(0x3a9a20)
      ind.emissiveIntensity = 0.9
    }
  } else {
    mat.color.setHex(0xf56c6c)
    mat.emissive.setHex(0x5c1a1a)
    mat.emissiveIntensity = 0.3
    if (ind) {
      ind.color.setHex(0xe6a23c)
      ind.emissive.setHex(0xb87a20)
      ind.emissiveIntensity = 0.7
    }
  }
  group.userData.tripped = !!tripped
}

/**
 * 油浸主变：油箱 + 片式散热器 + 油枕 + 三相套管
 * （公共变电站/储能升压变模型常见结构）
 */
export function createTransformer(scale = 1, { boxType = false } = {}) {
  if (boxType) return createBoxTransformer(scale)
  const g = new THREE.Group()
  const s = scale

  // 油箱（矩形更接近真实油浸变外观）
  const tank = box(2.6 * s, 2.8 * s, 2.2 * s, MAT.xfTank(), 1.55 * s)
  g.add(tank)

  // 两侧散热器翅片
  for (const side of [-1, 1]) {
    for (let i = 0; i < 7; i++) {
      const fin = box(0.08 * s, 2.2 * s, 1.6 * s, MAT.xfRadiator(), 1.4 * s)
      fin.position.set(side * (1.4 * s + 0.06), 0, (i - 3) * 0.22 * s)
      g.add(fin)
    }
    const header = box(0.22 * s, 2.4 * s, 1.7 * s, MAT.darkSteel(), 1.45 * s)
    header.position.x = side * 1.55 * s
    g.add(header)
  }

  // 油枕
  const conservator = cyl(0.42 * s, 0.42 * s, 2.0 * s, MAT.darkSteel(), 3.55 * s, 16)
  conservator.rotation.z = Math.PI / 2
  conservator.position.set(0, 0.15 * s, 0)
  g.add(conservator)
  const pipe = cyl(0.08 * s, 0.08 * s, 0.7 * s, MAT.steel(), 3.15 * s, 8)
  pipe.position.x = 0.9 * s
  g.add(pipe)

  // HV 三相套管（顶部）
  for (let i = -1; i <= 1; i++) {
    const bush = createInsulatorString(1.35 * s, 0.95 * s, true)
    bush.position.set(i * 0.55 * s, 2.85 * s, -0.55 * s)
    g.add(bush)
    const tip = cyl(0.06 * s, 0.08 * s, 0.18 * s, MAT.accent220(), 4.3 * s, 8)
    tip.position.set(i * 0.55 * s, 0, -0.55 * s)
    g.add(tip)
  }
  // LV 套管（侧向较低）
  for (let i = -1; i <= 1; i++) {
    const bush = createInsulatorString(0.75 * s, 0.7 * s)
    bush.position.set(i * 0.4 * s, 2.6 * s, 0.85 * s)
    g.add(bush)
  }

  // 基础 + 滚轮示意
  const pad = box(3.6 * s, 0.28 * s, 3.0 * s, MAT.concrete(), 0.14 * s)
  g.add(pad)
  for (const wx of [-1.1, 1.1]) {
    for (const wz of [-0.9, 0.9]) {
      const wheel = cyl(0.18 * s, 0.18 * s, 0.12 * s, MAT.blackRubber(), 0.22 * s, 12)
      wheel.rotation.z = Math.PI / 2
      wheel.position.set(wx * s, 0, wz * s)
      g.add(wheel)
    }
  }

  // 铭牌区
  const plate = box(0.7 * s, 0.4 * s, 0.04 * s, MAT.steel(), 1.6 * s)
  plate.position.z = 1.14 * s
  g.add(plate)

  g.userData.kind = 'transformer'
  return g
}

/** 单元箱变（预制舱/美式箱变外观） */
function createBoxTransformer(scale = 1) {
  const g = new THREE.Group()
  const s = scale
  const body = box(3.2 * s, 2.4 * s, 2.0 * s, MAT.boxXf(), 1.35 * s)
  g.add(body)
  // 高压 / 低压仓分隔线
  const divider = box(0.06 * s, 2.2 * s, 2.02 * s, MAT.boxXfAccent(), 1.35 * s)
  g.add(divider)
  // 百叶通风
  for (let i = 0; i < 5; i++) {
    const louver = box(1.1 * s, 0.08 * s, 0.05 * s, MAT.darkSteel(), 0.9 * s + i * 0.28 * s)
    louver.position.set(-0.85 * s, 0, 1.02 * s)
    g.add(louver)
  }
  // 仓门把手
  for (const sx of [-0.9, 0.9]) {
    const handle = box(0.08 * s, 0.35 * s, 0.08 * s, MAT.galvanized(), 1.3 * s)
    handle.position.set(sx * s, 0, 1.05 * s)
    g.add(handle)
  }
  // 顶部散热罩
  const hood = box(3.0 * s, 0.25 * s, 1.8 * s, MAT.darkSteel(), 2.65 * s)
  g.add(hood)
  const pad = box(3.5 * s, 0.25 * s, 2.3 * s, MAT.concrete(), 0.12 * s)
  g.add(pad)
  // 出线套管小柱
  for (let i = -1; i <= 1; i++) {
    const bush = createInsulatorString(0.55 * s, 0.55 * s)
    bush.position.set(i * 0.45 * s, 2.55 * s, 0)
    g.add(bush)
  }
  g.userData.kind = 'box-transformer'
  return g
}

/**
 * PCS 柜：工业变流器柜常见造型
 * — 双开门、多道百叶、状态指示灯、底部进线、底座
 */
export function createPcsCabinet(panelKey) {
  const g = new THREE.Group()

  const body = box(2.5, 3.8, 1.7, MAT.pcsBody(), 2.05)
  g.add(body)
  const skirting = box(2.55, 0.25, 1.75, MAT.pcsAccent(), 0.2)
  g.add(skirting)

  // 双开门
  for (const sx of [-0.58, 0.58]) {
    const door = box(1.05, 3.2, 0.07, MAT.pcsDoor(), 2.0)
    door.position.set(sx, 0, 0.9)
    g.add(door)
    const handle = box(0.08, 0.35, 0.1, MAT.galvanized(), 2.0)
    handle.position.set(sx + (sx > 0 ? -0.35 : 0.35), 0, 0.98)
    g.add(handle)
    // 观察窗
    const win = box(0.45, 0.55, 0.04, MAT.glass(), 2.7)
    win.position.set(sx, 0, 0.94)
    g.add(win)
  }

  // 顶部与侧向百叶
  for (let i = 0; i < 6; i++) {
    const topLouver = box(2.1, 0.06, 0.05, MAT.darkSteel(), 3.55 - i * 0.08)
    topLouver.position.z = 0.88
    g.add(topLouver)
  }
  for (let i = 0; i < 8; i++) {
    const sideLouver = box(0.05, 0.12, 1.2, MAT.darkSteel(), 1.0 + i * 0.28)
    sideLouver.position.set(1.28, 0, 0)
    g.add(sideLouver)
  }

  // 状态灯带
  const ledBar = box(1.6, 0.1, 0.06, MAT.ledGreen(), 3.7)
  ledBar.position.z = 0.9
  g.add(ledBar)
  for (let i = 0; i < 3; i++) {
    const led = cyl(0.06, 0.06, 0.05, i === 0 ? MAT.ledGreen() : MAT.ledAmber(), 3.55, 10)
    led.rotation.x = Math.PI / 2
    led.position.set(-0.5 + i * 0.5, 0, 0.92)
    g.add(led)
  }

  // 铭牌 / 品牌条
  const brand = box(1.4, 0.28, 0.04, MAT.pcsAccent(), 3.25)
  brand.position.z = 0.9
  g.add(brand)

  // 底部电缆沟盖板
  const cableEntry = box(1.8, 0.12, 0.5, MAT.darkSteel(), 0.35)
  cableEntry.position.z = 1.05
  g.add(cableEntry)

  const base = box(2.7, 0.18, 1.9, MAT.concrete(), 0.09)
  g.add(base)

  g.userData.kind = 'pcs'
  tagPanelPick(g, panelKey)
  return g
}

/**
 * 储能集装箱：ISO 箱体 + 波纹侧板 + 端门锁杆 + 顶部空调
 * （公共储能数字孪生资源中最常见外观）
 */
export function createBmsContainer(panelKey) {
  const g = new THREE.Group()
  const L = 5.2
  const H = 2.7
  const W = 2.5

  // 主箱体
  const shell = box(L, H, W, MAT.bmsWall(), H / 2 + 0.15)
  g.add(shell)

  // 波纹侧板
  for (const side of [-1, 1]) {
    for (let i = 0; i < 14; i++) {
      const rib = box(0.12, H * 0.92, 0.08, MAT.bmsCorrugation(), H / 2 + 0.15)
      rib.position.set(-L / 2 + 0.35 + i * 0.35, 0, side * (W / 2 + 0.02))
      g.add(rib)
    }
  }

  // 角件
  for (const sx of [-1, 1]) {
    for (const sz of [-1, 1]) {
      for (const sy of [0.35, H + 0.05]) {
        const corner = box(0.22, 0.22, 0.22, MAT.darkSteel(), sy)
        corner.position.set(sx * (L / 2 - 0.05), 0, sz * (W / 2 - 0.05))
        g.add(corner)
      }
    }
  }

  // 端门（双开）+ 锁杆
  for (const sx of [-0.65, 0.65]) {
    const door = box(1.15, H * 0.88, 0.08, MAT.bmsDoor(), H / 2 + 0.15)
    door.position.set(sx, 0, W / 2 + 0.05)
    g.add(door)
    const bar = cyl(0.04, 0.04, H * 0.75, MAT.galvanized(), H / 2 + 0.15, 8)
    bar.position.set(sx, 0, W / 2 + 0.12)
    g.add(bar)
    for (const hy of [0.7, 1.9]) {
      const latch = box(0.25, 0.1, 0.1, MAT.darkSteel(), hy)
      latch.position.set(sx, 0, W / 2 + 0.14)
      g.add(latch)
    }
  }

  // 屋顶 + HVAC
  const roof = box(L + 0.1, 0.12, W + 0.1, MAT.bmsRoof(), H + 0.2)
  g.add(roof)
  const hvac = box(1.6, 0.55, 1.1, MAT.darkSteel(), H + 0.55)
  hvac.position.set(-1.2, 0, 0)
  g.add(hvac)
  for (let i = 0; i < 4; i++) {
    const grill = box(1.3, 0.06, 0.04, MAT.steel(), H + 0.45 + i * 0.1)
    grill.position.set(-1.2, 0, 0.58)
    g.add(grill)
  }

  // 侧向检修门 / 铭牌
  const sideDoor = box(0.9, 1.6, 0.06, MAT.bmsDoor(), 1.2)
  sideDoor.position.set(-L / 2 - 0.02, 0, 0)
  g.add(sideDoor)
  const nameplate = box(1.0, 0.35, 0.04, MAT.steel(), 2.2)
  nameplate.position.set(0, 0, W / 2 + 0.06)
  g.add(nameplate)

  // 底座梁
  const base = box(L + 0.2, 0.28, W + 0.15, MAT.concrete(), 0.14)
  g.add(base)
  for (const sx of [-1.6, 0, 1.6]) {
    const beam = box(0.25, 0.22, W + 0.1, MAT.darkSteel(), 0.2)
    beam.position.x = sx
    g.add(beam)
  }

  g.userData.kind = 'bms'
  tagPanelPick(g, panelKey)
  return g
}

function tagPanelPick(group, panelKey) {
  if (!panelKey) return
  group.userData.panelKey = panelKey
  group.traverse(o => {
    if (o.isMesh) o.userData.panelKey = panelKey
  })
}

/** 35kV / 690V 母线管（保留导出兼容） */
export function createBusBar(x0, x1, y, z, radius, matFactory) {
  const len = Math.abs(x1 - x0)
  const mesh = new THREE.Mesh(
    new THREE.CylinderGeometry(radius, radius, len || 0.1, 12),
    matFactory()
  )
  mesh.rotation.z = Math.PI / 2
  mesh.position.set((x0 + x1) / 2, y, z)
  mesh.userData.kind = 'bus'
  return mesh
}

/**
 * 根据 layout 与 snap 单元数构建整站场景内容
 * @returns {{ root: THREE.Group, refs: object }}
 */
export function buildStation(layout, units) {
  const root = new THREE.Group()
  const refs = {
    mainBreaker: null,
    unitBreakers: [],
    cables: [],
    panelAnchors: [],
    labelAnchors: []
  }

  const { mainX, busStartX, busEndX, unitXs } = layout

  root.add(createGridGantry(mainX))
  refs.labelAnchors.push({
    key: 'grid',
    kind: 'grid',
    position: new THREE.Vector3(mainX + 3.8, Y.label + 0.5, Z.grid)
  })

  const mainBr = createBreakerMesh('main')
  mainBr.position.set(mainX, 0, Z.mainBreaker)
  root.add(mainBr)
  refs.mainBreaker = mainBr
  refs.labelAnchors.push({
    key: 'main-breaker',
    kind: 'breaker-label',
    position: new THREE.Vector3(mainX + 2.4, 3.6, Z.mainBreaker)
  })

  const mainXf = createTransformer(1.2, { boxType: false })
  mainXf.position.set(mainX, 0, Z.mainXf)
  root.add(mainXf)
  refs.labelAnchors.push({
    key: 'main-xf',
    kind: 'main-xf',
    position: new THREE.Vector3(mainX + 3.6, 5.0, Z.mainXf)
  })

  const c1 = createPowerCable([
    new THREE.Vector3(mainX, 9.5, Z.grid + 0.5),
    new THREE.Vector3(mainX, 6.0, (Z.grid + Z.mainBreaker) / 2),
    new THREE.Vector3(mainX, 3.5, Z.mainBreaker)
  ], { radius: 0.1 })
  c1.userData.cableRole = 'grid-main'
  root.add(c1)
  refs.cables.push(c1)

  const c2 = createPowerCable([
    new THREE.Vector3(mainX, 3.5, Z.mainBreaker),
    new THREE.Vector3(mainX, 4.8, (Z.mainBreaker + Z.mainXf) / 2),
    new THREE.Vector3(mainX, 4.4, Z.mainXf)
  ], { radius: 0.1 })
  c2.userData.cableRole = 'main-xf'
  root.add(c2)
  refs.cables.push(c2)

  const c3 = createPowerCable([
    new THREE.Vector3(mainX, 3.6, Z.mainXf + 1.4),
    new THREE.Vector3(mainX, Y.bus35, (Z.mainXf + Z.bus35) / 2),
    new THREE.Vector3(mainX, Y.bus35, Z.bus35)
  ], { radius: 0.12 })
  c3.userData.cableRole = 'xf-bus35'
  root.add(c3)
  refs.cables.push(c3)

  const bus35 = createPowerCable([
    new THREE.Vector3(busStartX, Y.bus35, Z.bus35),
    new THREE.Vector3((busStartX + busEndX) / 2, Y.bus35, Z.bus35),
    new THREE.Vector3(busEndX, Y.bus35, Z.bus35)
  ], { radius: 0.2 })
  bus35.userData.cableRole = 'bus35'
  root.add(bus35)
  refs.cables.push(bus35)
  refs.labelAnchors.push({
    key: 'bus35',
    kind: 'bus35',
    position: new THREE.Vector3(mainX + 1.5, Y.bus35 + 1.2, Z.bus35 - 1.2)
  })

  ;(units || []).forEach((u, i) => {
    const ux = unitXs[i] ?? i * 22
    const unitIndex = u.unitIndex ?? i

    refs.labelAnchors.push({
      key: `unit-${unitIndex}`,
      kind: 'unit-title',
      unitIndex,
      position: new THREE.Vector3(ux - 3, 5.5, Z.bus35 + 0.5)
    })

    const drop = createPowerCable([
      new THREE.Vector3(ux, Y.bus35, Z.bus35),
      new THREE.Vector3(ux, 3.6, Z.unitBreaker - 1),
      new THREE.Vector3(ux, 3.4, Z.unitBreaker)
    ], { radius: 0.09 })
    drop.userData.cableRole = 'unit-drop'
    drop.userData.unitIndex = unitIndex
    root.add(drop)
    refs.cables.push(drop)

    const ub = createBreakerMesh(`unit-${unitIndex}`, unitIndex)
    ub.position.set(ux, 0, Z.unitBreaker)
    root.add(ub)
    refs.unitBreakers[unitIndex] = ub
    refs.labelAnchors.push({
      key: `unit-br-${unitIndex}`,
      kind: 'unit-breaker-label',
      unitIndex,
      position: new THREE.Vector3(ux + 2.4, 3.4, Z.unitBreaker)
    })

    // 单元侧用箱变造型，更贴近储能升压一体机常见外观
    const uxf = createTransformer(0.9, { boxType: true })
    uxf.position.set(ux, 0, Z.unitXf)
    root.add(uxf)
    refs.labelAnchors.push({
      key: `unit-xf-${unitIndex}`,
      kind: 'unit-xf',
      unitIndex,
      position: new THREE.Vector3(ux + 2.8, 3.8, Z.unitXf)
    })

    const toXf = createPowerCable([
      new THREE.Vector3(ux, 3.4, Z.unitBreaker),
      new THREE.Vector3(ux, 3.6, (Z.unitBreaker + Z.unitXf) / 2),
      new THREE.Vector3(ux, 3.0, Z.unitXf)
    ], { radius: 0.08 })
    toXf.userData.cableRole = 'unit-xf'
    toXf.userData.unitIndex = unitIndex
    root.add(toXf)
    refs.cables.push(toXf)

    const ax = channelX(ux, 'A')
    const bx = channelX(ux, 'B')
    const bus690 = createPowerCable([
      new THREE.Vector3(ax - 0.5, Y.bus690, Z.bus690),
      new THREE.Vector3(ux, Y.bus690, Z.bus690),
      new THREE.Vector3(bx + 0.5, Y.bus690, Z.bus690)
    ], { radius: 0.1 })
    bus690.userData.cableRole = 'unit-690-bus'
    bus690.userData.unitIndex = unitIndex
    root.add(bus690)
    refs.cables.push(bus690)

    const to690 = createPowerCable([
      new THREE.Vector3(ux, 2.6, Z.unitXf + 1.1),
      new THREE.Vector3(ux, Y.bus690, (Z.unitXf + Z.bus690) / 2),
      new THREE.Vector3(ux, Y.bus690, Z.bus690)
    ], { radius: 0.07 })
    to690.userData.cableRole = 'unit-690'
    to690.userData.unitIndex = unitIndex
    root.add(to690)
    refs.cables.push(to690)

    for (const side of ['A', 'B']) {
      const ch = side === 'A' ? u.channelA : u.channelB
      if (!ch) continue
      const cx = channelX(ux, side)

      const feed = createPowerCable([
        new THREE.Vector3(ux, Y.bus690, Z.bus690),
        new THREE.Vector3(cx, Y.bus690, Z.bus690),
        new THREE.Vector3(cx, 2.5, Z.pcs - 1.5),
        new THREE.Vector3(cx, 2.2, Z.pcs)
      ], { radius: 0.06 })
      feed.userData.cableRole = 'pcs-feed'
      feed.userData.unitIndex = unitIndex
      feed.userData.side = side
      root.add(feed)
      refs.cables.push(feed)

      const pcsKey = `pcs-${unitIndex}-${side}`
      const bmsKey = `bms-${unitIndex}-${side}`

      const pcs = createPcsCabinet(pcsKey)
      pcs.position.set(cx, 0, Z.pcs)
      root.add(pcs)

      const dc = createPowerCable([
        new THREE.Vector3(cx, 1.5, Z.pcs + 1.1),
        new THREE.Vector3(cx, 1.2, (Z.pcs + Z.bms) / 2),
        new THREE.Vector3(cx, 1.5, Z.bms - 1.2)
      ], { radius: 0.05 })
      dc.userData.cableRole = 'dc-link'
      dc.userData.unitIndex = unitIndex
      dc.userData.side = side
      root.add(dc)
      refs.cables.push(dc)

      const bms = createBmsContainer(bmsKey)
      bms.position.set(cx, 0, Z.bms)
      root.add(bms)

      refs.panelAnchors.push({
        key: pcsKey,
        type: 'pcs',
        unitIndex,
        side,
        position: new THREE.Vector3(cx, 4.4, Z.pcs + 0.3)
      })
      refs.panelAnchors.push({
        key: bmsKey,
        type: 'bms',
        unitIndex,
        side,
        position: new THREE.Vector3(cx, 3.6, Z.bms + 0.3)
      })
    }
  })

  return { root, refs }
}
