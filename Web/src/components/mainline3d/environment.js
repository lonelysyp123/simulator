import * as THREE from 'three'
import { Z } from './layout.js'

const MAT = {
  grass: () => new THREE.MeshStandardMaterial({ color: 0x4a6b48, metalness: 0.02, roughness: 0.92 }),
  grassDark: () => new THREE.MeshStandardMaterial({ color: 0x3a5638, metalness: 0.02, roughness: 0.94 }),
  asphalt: () => new THREE.MeshStandardMaterial({ color: 0x3d424a, metalness: 0.12, roughness: 0.88 }),
  asphaltLine: () => new THREE.MeshStandardMaterial({ color: 0xc9c4a8, metalness: 0.05, roughness: 0.7 }),
  curb: () => new THREE.MeshStandardMaterial({ color: 0x8a909a, metalness: 0.08, roughness: 0.85 }),
  trunk: () => new THREE.MeshStandardMaterial({ color: 0x5c4030, metalness: 0.05, roughness: 0.9 }),
  foliage: () => new THREE.MeshStandardMaterial({ color: 0x3f7a45, metalness: 0.02, roughness: 0.85 }),
  foliage2: () => new THREE.MeshStandardMaterial({ color: 0x2f6a38, metalness: 0.02, roughness: 0.88 }),
  hedge: () => new THREE.MeshStandardMaterial({ color: 0x457a42, metalness: 0.02, roughness: 0.9 }),
  pole: () => new THREE.MeshStandardMaterial({ color: 0x4a505a, metalness: 0.55, roughness: 0.4 }),
  lamp: () => new THREE.MeshStandardMaterial({
    color: 0xffe6a8,
    emissive: 0xffcc66,
    emissiveIntensity: 1.2,
    metalness: 0.1,
    roughness: 0.35
  }),
  building: () => new THREE.MeshStandardMaterial({
    color: 0x6a7382,
    metalness: 0.15,
    roughness: 0.85,
    transparent: true,
    opacity: 0.55
  }),
  buildingFar: () => new THREE.MeshStandardMaterial({
    color: 0x5a6575,
    metalness: 0.1,
    roughness: 0.9,
    transparent: true,
    opacity: 0.28
  }),
  fence: () => new THREE.MeshStandardMaterial({ color: 0x7a8494, metalness: 0.4, roughness: 0.5 })
}

function box(w, h, d, mat, y = h / 2) {
  const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), mat)
  m.position.y = y
  m.castShadow = true
  m.receiveShadow = true
  return m
}

function cyl(rTop, rBot, h, mat, y = h / 2, segments = 10) {
  const m = new THREE.Mesh(new THREE.CylinderGeometry(rTop, rBot, h, segments), mat)
  m.position.y = y
  m.castShadow = true
  m.receiveShadow = true
  return m
}

/** 简单乔木 */
function createTree(scale = 1) {
  const g = new THREE.Group()
  const trunk = cyl(0.18 * scale, 0.28 * scale, 2.2 * scale, MAT.trunk(), 1.1 * scale, 8)
  const crown1 = cyl(0.05, 1.6 * scale, 2.4 * scale, MAT.foliage(), 3.0 * scale, 8)
  const crown2 = cyl(0.05, 1.1 * scale, 1.6 * scale, MAT.foliage2(), 4.2 * scale, 8)
  g.add(trunk, crown1, crown2)
  return g
}

/** 灌木球 */
function createBush(scale = 1) {
  const g = new THREE.Group()
  const b = new THREE.Mesh(
    new THREE.SphereGeometry(0.7 * scale, 10, 8),
    MAT.hedge()
  )
  b.position.y = 0.55 * scale
  b.castShadow = true
  b.receiveShadow = true
  g.add(b)
  return g
}

/** 路灯（每隔一盏带点光，控制性能） */
function createStreetLamp(withLight = true) {
  const g = new THREE.Group()
  const pole = cyl(0.08, 0.12, 5.2, MAT.pole(), 2.6, 8)
  const arm = box(1.4, 0.08, 0.08, MAT.pole(), 5.25)
  arm.position.x = 0.55
  const head = box(0.55, 0.12, 0.35, MAT.pole(), 5.15)
  head.position.x = 1.15
  const bulb = new THREE.Mesh(
    new THREE.SphereGeometry(0.14, 10, 8),
    MAT.lamp()
  )
  bulb.position.set(1.15, 5.0, 0)
  g.add(pole, arm, head, bulb)

  if (withLight) {
    const light = new THREE.PointLight(0xffd89a, 0.45, 20, 2)
    light.position.set(1.15, 4.9, 0)
    g.add(light)
  }
  return g
}

/**
 * 在设备区周围铺路面、绿化、路灯，并加远景虚化建筑
 * @param {{ busStartX: number, busEndX: number, unitXs: number[] }} layout
 */
export function buildEnvironment(layout) {
  const root = new THREE.Group()
  root.name = 'station-environment'

  const x0 = (layout.busStartX ?? 0) - 18
  const x1 = (layout.busEndX ?? 20) + 18
  const cx = (x0 + x1) / 2
  const width = Math.max(40, x1 - x0)

  // —— 设备区混凝土垫层（略高于草地）——
  const pad = box(width + 8, 0.12, 58, MAT.curb(), 0.04)
  pad.position.set(cx, 0, 6)
  pad.receiveShadow = true
  pad.castShadow = false
  root.add(pad)

  // —— 前方巡视道路（BMS 外侧）——
  const roadZ = Z.bms + 10
  const road = box(width + 24, 0.08, 7.5, MAT.asphalt(), 0.03)
  road.position.set(cx, 0, roadZ)
  road.receiveShadow = true
  root.add(road)
  // 中线虚线
  const dashCount = Math.max(6, Math.round(width / 8))
  for (let i = 0; i < dashCount; i++) {
    const t = dashCount === 1 ? 0.5 : i / (dashCount - 1)
    const x = x0 - 6 + t * (width + 12)
    const dash = box(2.2, 0.02, 0.18, MAT.asphaltLine(), 0.08)
    dash.position.set(x, 0, roadZ)
    root.add(dash)
  }
  // 路缘
  const curbF = box(width + 24, 0.22, 0.35, MAT.curb(), 0.12)
  curbF.position.set(cx, 0, roadZ - 3.9)
  root.add(curbF)
  const curbB = box(width + 24, 0.22, 0.35, MAT.curb(), 0.12)
  curbB.position.set(cx, 0, roadZ + 3.9)
  root.add(curbB)

  // —— 侧向联络道路 ——
  const sideRoad = box(6.5, 0.08, 70, MAT.asphalt(), 0.03)
  sideRoad.position.set(x0 - 10, 0, 4)
  root.add(sideRoad)
  const sideRoad2 = box(6.5, 0.08, 70, MAT.asphalt(), 0.03)
  sideRoad2.position.set(x1 + 10, 0, 4)
  root.add(sideRoad2)

  // —— 绿化带（道路外侧）——
  const strip = box(width + 28, 0.06, 5.5, MAT.grass(), 0.02)
  strip.position.set(cx, 0, roadZ + 7.2)
  strip.receiveShadow = true
  root.add(strip)
  const stripBack = box(width + 20, 0.06, 8, MAT.grassDark(), 0.02)
  stripBack.position.set(cx, 0, Z.grid - 14)
  root.add(stripBack)

  // 绿篱
  const hedgeLen = width + 16
  const hedge = box(hedgeLen, 1.1, 0.7, MAT.hedge(), 0.55)
  hedge.position.set(cx, 0, roadZ + 5.2)
  root.add(hedge)

  // 树木沿道路外侧
  const treeStep = 14
  for (let x = x0 - 4; x <= x1 + 4; x += treeStep) {
    const tree = createTree(0.85 + ((Math.abs(x) * 0.01) % 0.35))
    tree.position.set(x, 0, roadZ + 9.5)
    root.add(tree)
    if ((x / treeStep) % 2 === 0) {
      const bush = createBush(0.9)
      bush.position.set(x + 3.5, 0, roadZ + 6.8)
      root.add(bush)
    }
  }
  // 电网侧零星树木
  for (let x = x0; x <= x1; x += 18) {
    const tree = createTree(1.05)
    tree.position.set(x + 2, 0, Z.grid - 18)
    root.add(tree)
  }

  // —— 路灯（道路两侧，点数控制）——
  const lampStep = Math.max(18, Math.round(width / 4))
  let lampIdx = 0
  for (let x = x0 - 2; x <= x1 + 2; x += lampStep) {
    const lampA = createStreetLamp(lampIdx % 2 === 0)
    lampA.position.set(x, 0, roadZ - 4.2)
    lampA.rotation.y = Math.PI
    root.add(lampA)

    const lampB = createStreetLamp(lampIdx % 2 === 1)
    lampB.position.set(x + lampStep * 0.45, 0, roadZ + 4.2)
    root.add(lampB)
    lampIdx++
  }

  // —— 围栏示意 ——
  const fenceZ = roadZ + 12
  const fence = box(width + 30, 2.2, 0.12, MAT.fence(), 1.1)
  fence.position.set(cx, 0, fenceZ)
  root.add(fence)
  for (let x = x0 - 10; x <= x1 + 10; x += 8) {
    const post = box(0.18, 2.4, 0.18, MAT.pole(), 1.2)
    post.position.set(x, 0, fenceZ)
    root.add(post)
  }

  // —— 远景虚化建筑（电站周边厂房感）——
  const farBuildings = [
    { x: cx - width * 0.55, z: -55, w: 28, h: 14, d: 12, far: true },
    { x: cx + width * 0.4, z: -62, w: 36, h: 18, d: 14, far: true },
    { x: cx - 8, z: 58, w: 42, h: 12, d: 16, far: false },
    { x: cx + width * 0.6, z: 52, w: 24, h: 16, d: 10, far: false },
    { x: cx - width * 0.7, z: 48, w: 30, h: 10, d: 12, far: true },
    { x: x0 - 40, z: 10, w: 18, h: 22, d: 18, far: true },
    { x: x1 + 42, z: -5, w: 20, h: 20, d: 16, far: true }
  ]
  for (const b of farBuildings) {
    const mat = b.far ? MAT.buildingFar() : MAT.building()
    const mesh = box(b.w, b.h, b.d, mat, b.h / 2)
    mesh.position.set(b.x, 0, b.z)
    mesh.castShadow = false
    root.add(mesh)
  }

  // —— 天空穹顶（淡色渐变感，配合雾效虚化远景）——
  const sky = new THREE.Mesh(
    new THREE.SphereGeometry(420, 24, 16),
    new THREE.MeshBasicMaterial({
      color: 0x7a8fa8,
      side: THREE.BackSide,
      depthWrite: false,
      fog: false
    })
  )
  sky.position.set(cx, 0, 4)
  root.add(sky)

  // 地平线雾环（半透明带，增强“周围模糊”）
  const haze = new THREE.Mesh(
    new THREE.CylinderGeometry(180, 200, 28, 32, 1, true),
    new THREE.MeshBasicMaterial({
      color: 0x9aabbc,
      transparent: true,
      opacity: 0.22,
      side: THREE.DoubleSide,
      depthWrite: false
    })
  )
  haze.position.set(cx, 10, 4)
  root.add(haze)

  root.userData.center = { x: cx, z: 4 }
  root.userData.width = width
  return root
}
