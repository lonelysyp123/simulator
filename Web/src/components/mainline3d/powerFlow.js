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
 * 沿折线路径的潮流电缆：管线 + 分级流光
 */
export function createPowerCable(points, { radius = 0.08 } = {}) {
  const curve = new THREE.CatmullRomCurve3(points.map(p => p.clone()))
  const geo = new THREE.TubeGeometry(curve, Math.max(12, points.length * 8), radius, 8, false)
  const mat = new THREE.MeshStandardMaterial({
    color: COLOR.off,
    metalness: 0.35,
    roughness: 0.5,
    emissive: 0x000000,
    emissiveIntensity: 0
  })
  const mesh = new THREE.Mesh(geo, mat)
  mesh.userData.isCable = true

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

  // 次级拖尾粒子（更密、更小）
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

  // 速度随功率增大；充电/放电都明显流动
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
  // 待机：呼吸发光，粒子缓慢往复
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

  // 管线脉动增强流向感
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
    const p = s.curve.getPointAt(t)
    s.positions[i * 3] = p.x
    s.positions[i * 3 + 1] = p.y
    s.positions[i * 3 + 2] = p.z
  }
  s.particles.geometry.attributes.position.needsUpdate = true

  const tn = s.trailCount
  for (let i = 0; i < tn; i++) {
    let t = (phase + i / tn * 0.85 + 1) % 1
    if (idle) t = 0.15 + 0.7 * (i / tn)
    const p = s.curve.getPointAt(t)
    s.tPos[i * 3] = p.x
    s.tPos[i * 3 + 1] = p.y
    s.tPos[i * 3 + 2] = p.z
  }
  s.trail.geometry.attributes.position.needsUpdate = true
}
