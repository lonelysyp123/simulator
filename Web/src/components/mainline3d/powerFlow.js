import * as THREE from 'three'

/** PCS ActivePower>0 放电，<0 充电（与 PcsDevice 约定一致） */
export const FLOW = {
  OFF: 'off',
  IDLE: 'idle',
  CHARGE: 'charge',
  DISCHARGE: 'discharge',
  TRIP: 'trip'
}

const COLOR = {
  off: 0x6b7280,
  idle: 0x67c23a,        // 待机通电：绿色，易与充/放区分
  charge: 0x38bdf8,      // 充电：蓝青，流向电池
  discharge: 0xfbbf24,   // 放电：琥珀，流向电网
  trip: 0xf56c6c
}

const IDLE_KW = 0.8

/**
 * @param {number} powerKw  有功：>0 放电，<0 充电
 * @param {{ energized?: boolean, tripped?: boolean }} opts
 */
export function resolveFlow(powerKw, { energized = false, tripped = false } = {}) {
  if (tripped) {
    return { mode: FLOW.TRIP, live: false, direction: 0, powerKw: 0, magnitude: 0 }
  }
  if (!energized) {
    return { mode: FLOW.OFF, live: false, direction: 0, powerKw: 0, magnitude: 0 }
  }
  const p = Number(powerKw) || 0
  const mag = Math.abs(p)
  if (mag < IDLE_KW) {
    return { mode: FLOW.IDLE, live: true, direction: 0, powerKw: p, magnitude: 0 }
  }
  // 电缆路径默认“朝电池/负荷”为正方向；充电同向，放电反向
  if (p < 0) {
    return { mode: FLOW.CHARGE, live: true, direction: 1, powerKw: p, magnitude: mag }
  }
  return { mode: FLOW.DISCHARGE, live: true, direction: -1, powerKw: p, magnitude: mag }
}

/**
 * 正交折线 → 刚体直线段 + 拐角二次贝塞尔圆角（不使用 CatmullRom，避免下陷/斜切）
 * @param {THREE.Vector3[]} rawPoints
 * @param {number} [cornerR=0.35]
 * @returns {THREE.CurvePath}
 */
export function buildRigidCableCurve(rawPoints, cornerR = 0.35) {
  const pts = []
  for (const p of rawPoints || []) {
    const v = p.clone()
    if (!pts.length || pts[pts.length - 1].distanceToSquared(v) > 1e-8) pts.push(v)
  }

  const path = new THREE.CurvePath()
  if (pts.length < 2) {
    const a = pts[0] || new THREE.Vector3()
    path.add(new THREE.LineCurve3(a, a.clone().add(new THREE.Vector3(0, 0.01, 0))))
    return path
  }
  if (pts.length === 2) {
    path.add(new THREE.LineCurve3(pts[0], pts[1]))
    return path
  }

  const n = pts.length
  const segStart = new Array(n - 1)
  const segEnd = new Array(n - 1)
  for (let i = 0; i < n - 1; i++) {
    segStart[i] = pts[i].clone()
    segEnd[i] = pts[i + 1].clone()
  }

  // 拐角处缩短相邻直线段，插入圆角
  for (let i = 1; i < n - 1; i++) {
    const prev = pts[i - 1]
    const curr = pts[i]
    const next = pts[i + 1]
    const dirIn = curr.clone().sub(prev)
    const dirOut = next.clone().sub(curr)
    const lenIn = dirIn.length()
    const lenOut = dirOut.length()
    if (lenIn < 1e-6 || lenOut < 1e-6) continue
    dirIn.multiplyScalar(1 / lenIn)
    dirOut.multiplyScalar(1 / lenOut)
    // 共线则无需圆角
    if (dirIn.dot(dirOut) > 0.999) continue
    const r = Math.min(cornerR, lenIn * 0.4, lenOut * 0.4)
    if (r < 0.05) continue
    segEnd[i - 1] = curr.clone().addScaledVector(dirIn, -r)
    segStart[i] = curr.clone().addScaledVector(dirOut, r)
  }

  for (let i = 0; i < n - 1; i++) {
    if (segStart[i].distanceToSquared(segEnd[i]) > 1e-8) {
      path.add(new THREE.LineCurve3(segStart[i], segEnd[i]))
    }
    if (i < n - 2) {
      const corner = pts[i + 1]
      const a = segEnd[i]
      const b = segStart[i + 1]
      // 圆角控制点取原折点，形成平滑转弯
      if (a.distanceToSquared(b) > 1e-8) {
        path.add(new THREE.QuadraticBezierCurve3(a.clone(), corner.clone(), b.clone()))
      }
    }
  }

  return path
}

/**
 * 沿正交刚体路径的潮流电缆：直线段 + 拐角软化 + 流光
 */
export function createPowerCable(points, { radius = 0.08, cornerRadius = 0.35 } = {}) {
  const curve = buildRigidCableCurve(points, cornerRadius)
  const tubularSegments = Math.max(24, (points?.length || 2) * 12)
  const geo = new THREE.TubeGeometry(curve, tubularSegments, radius, 8, false)
  const mat = new THREE.MeshStandardMaterial({
    color: COLOR.off,
    metalness: 0.35,
    roughness: 0.5,
    emissive: 0x000000,
    emissiveIntensity: 0,
    polygonOffset: true,
    polygonOffsetFactor: -2,
    polygonOffsetUnits: -2
  })
  const mesh = new THREE.Mesh(geo, mat)
  mesh.userData.isCable = true
  mesh.renderOrder = 2
  // 贴地电缆不投射阴影，避免贴地阴影加深“被埋”感
  mesh.castShadow = false
  mesh.receiveShadow = false

  const particleCount = 28
  const pGeo = new THREE.BufferGeometry()
  const positions = new Float32Array(particleCount * 3)
  pGeo.setAttribute('position', new THREE.BufferAttribute(positions, 3))

  const pMat = new THREE.PointsMaterial({
    color: COLOR.idle,
    size: 0.35,
    transparent: true,
    opacity: 0,
    depthWrite: false,
    sizeAttenuation: true,
    blending: THREE.AdditiveBlending
  })
  const particles = new THREE.Points(pGeo, pMat)
  particles.visible = false
  particles.renderOrder = 3

  const trailCount = 40
  const tGeo = new THREE.BufferGeometry()
  const tPos = new Float32Array(trailCount * 3)
  tGeo.setAttribute('position', new THREE.BufferAttribute(tPos, 3))
  const tMat = new THREE.PointsMaterial({
    color: COLOR.idle,
    size: 0.14,
    transparent: true,
    opacity: 0,
    depthWrite: false,
    sizeAttenuation: true,
    blending: THREE.AdditiveBlending
  })
  const trail = new THREE.Points(tGeo, tMat)
  trail.visible = false
  trail.renderOrder = 3

  const group = new THREE.Group()
  group.add(mesh)
  group.add(particles)
  group.add(trail)

  group.userData.cableState = {
    mode: FLOW.OFF,
    live: false,
    direction: 0,
    speed: 0,
    phase: Math.random(),
    pulse: 0,
    magnitude: 0,
    curve,
    particleCount,
    trailCount,
    positions,
    tPos,
    pMat,
    tMat,
    mat,
    particles,
    trail,
    mesh
  }
  return group
}

/**
 * @param {THREE.Group} cable
 * @param {{ energized?: boolean, tripped?: boolean, powerKw?: number }} opts
 */
export function updateCableState(cable, { energized = false, tripped = false, powerKw = 0 } = {}) {
  const s = cable?.userData?.cableState
  if (!s) return

  const flow = resolveFlow(powerKw, { energized, tripped })
  s.mode = flow.mode
  s.live = flow.live
  s.direction = flow.direction
  s.magnitude = flow.magnitude

  const norm = Math.min(1, flow.magnitude / 800)
  if (flow.mode === FLOW.CHARGE || flow.mode === FLOW.DISCHARGE) {
    s.speed = 0.45 + norm * 1.8
  } else if (flow.mode === FLOW.IDLE) {
    s.speed = 0.12
  } else {
    s.speed = 0
  }

  const col = COLOR[flow.mode] ?? COLOR.off
  s.mat.color.setHex(col)
  s.pMat.color.setHex(col)
  s.tMat.color.setHex(col)

  if (flow.mode === FLOW.TRIP) {
    s.mat.emissive.setHex(COLOR.trip)
    s.mat.emissiveIntensity = 0.45
    s.pMat.opacity = 0
    s.tMat.opacity = 0
    s.particles.visible = false
    s.trail.visible = false
  } else if (flow.mode === FLOW.CHARGE || flow.mode === FLOW.DISCHARGE) {
    s.mat.emissive.setHex(col)
    s.mat.emissiveIntensity = 0.25 + norm * 0.55
    s.pMat.opacity = 0.95
    s.tMat.opacity = 0.55
    s.pMat.size = 0.28 + norm * 0.35
    s.tMat.size = 0.1 + norm * 0.12
    s.particles.visible = true
    s.trail.visible = true
  } else if (flow.mode === FLOW.IDLE) {
    s.mat.emissive.setHex(COLOR.idle)
    s.mat.emissiveIntensity = 0.12
    s.pMat.opacity = 0.35
    s.tMat.opacity = 0.15
    s.pMat.size = 0.18
    s.particles.visible = true
    s.trail.visible = true
  } else {
    s.mat.emissive.setHex(0x000000)
    s.mat.emissiveIntensity = 0
    s.pMat.opacity = 0
    s.tMat.opacity = 0
    s.particles.visible = false
    s.trail.visible = false
  }
}

/**
 * @param {THREE.Group} cable
 * @param {number} dt
 */
export function tickCable(cable, dt) {
  const s = cable?.userData?.cableState
  if (!s || !s.live) return

  s.pulse += dt
  if (s.mode === FLOW.IDLE) {
    const breath = 0.1 + 0.08 * (0.5 + 0.5 * Math.sin(s.pulse * 2.2))
    s.mat.emissiveIntensity = breath
    s.phase = (s.phase + dt * s.speed) % 1
    placeParticles(s, s.phase, 1, true)
    return
  }

  const dir = s.direction || 1
  s.phase = (s.phase + dt * s.speed * dir + 1) % 1
  placeParticles(s, s.phase, dir, false)

  const magN = Math.min(1, s.magnitude / 800)
  s.mat.emissiveIntensity = 0.22 + magN * 0.45 + 0.12 * Math.sin(s.pulse * (4 + magN * 6))
}

function placeParticles(s, phase, dir, idle) {
  const n = s.particleCount
  for (let i = 0; i < n; i++) {
    let t = (phase + i / n + 1) % 1
    if (idle) {
      t = 0.2 + 0.6 * ((Math.sin(phase * Math.PI * 2 + i) + 1) / 2)
    }
    const p = s.curve.getPointAt(Math.min(0.9999, Math.max(0, t)))
    s.positions[i * 3] = p.x
    s.positions[i * 3 + 1] = p.y
    s.positions[i * 3 + 2] = p.z
  }
  s.particles.geometry.attributes.position.needsUpdate = true

  const tn = s.trailCount
  for (let i = 0; i < tn; i++) {
    let t = (phase + i / tn * 0.85 + 1) % 1
    if (idle) t = 0.15 + 0.7 * (i / tn)
    const p = s.curve.getPointAt(Math.min(0.9999, Math.max(0, t)))
    s.tPos[i * 3] = p.x
    s.tPos[i * 3 + 1] = p.y
    s.tPos[i * 3 + 2] = p.z
  }
  s.trail.geometry.attributes.position.needsUpdate = true
}
