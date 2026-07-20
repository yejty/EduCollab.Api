import { Client, Room } from '@colyseus/core'
import type { EduCollabClient } from '../api/educollabClient'
import { extractBearer } from '../auth'
import type { CollabServerConfig } from '../config'
import { verifyJoinTicket } from '../joinTicket'
import { claimsToSessionMeta, type RoomSessionMeta } from '../sessionMeta'
import {
  isParticipantRole,
  type JoinIdentity,
  type ParticipantRole,
} from '../types'
import { ParticipantState, SessionState } from './schema'

interface CollabRoomOptions {
  sessionId?: string
  /** Short-lived join ticket minted by EduCollab API. */
  joinTicket?: string
  /**
   * If true, this client opts in to receive `sceneSync` broadcasts (it's
   * actually rendering the scene — i.e. the editor). Join-page clients should
   * leave this falsy so we don't flood them with multi-MB scene JSON, which
   * was triggering WebSocket close code 1009 ("message too big") on the
   * smaller-sized join-page connections.
   */
  wantsSceneSync?: boolean
}

interface ClientUserData {
  identity: JoinIdentity
  role: ParticipantRole
  session: RoomSessionMeta
  /** Mirrors `CollabRoomOptions.wantsSceneSync` after onAuth. */
  wantsSceneSync: boolean
}

interface CameraUpdatePayload {
  camX?: number
  camY?: number
  camZ?: number
  tgtX?: number
  tgtY?: number
  tgtZ?: number
  /** Client navigation mode: orbit, walk (FPS), ar, vr */
  nav?: string
}

interface RoleUpdatePayload {
  userId?: string
  role?: string
}

interface PresenterPayload {
  userId?: string
}

interface CursorPayload {
  hitX?: number
  hitY?: number
  hitZ?: number
  hasHit?: boolean
  active?: boolean
}

interface SceneSyncPayload {
  json?: string
}

interface GlbPartSelectPayload {
  placementKey?: string
  outlinerPath?: string | null
}

interface GlbPartTransformPayload {
  placementKey?: string
  outlinerPath?: string
  transform?: {
    position?: unknown
    rotation?: unknown
    scale?: unknown
  }
}

function asNumericTriple(value: unknown): [number, number, number] | null {
  if (Array.isArray(value) && value.length >= 3) {
    const a = Number(value[0])
    const b = Number(value[1])
    const c = Number(value[2])
    if (Number.isFinite(a) && Number.isFinite(b) && Number.isFinite(c)) {
      return [a, b, c]
    }
  }
  if (value && typeof value === 'object') {
    const o = value as Record<string, unknown>
    const a = Number(o[0] ?? o.x)
    const b = Number(o[1] ?? o.y)
    const c = Number(o[2] ?? o.z)
    if (Number.isFinite(a) && Number.isFinite(b) && Number.isFinite(c)) {
      return [a, b, c]
    }
  }
  return null
}

type GlbPartWireTransform = {
  position: [number, number, number]
  rotation: [number, number, number]
  scale: [number, number, number]
}

/** Patch one sub-part TRS into the cached scene document (late-join replay). */
function patchGlbPartIntoSceneJson(
  json: string,
  placementKey: string,
  outlinerPath: string,
  transform: GlbPartWireTransform,
): string {
  try {
    const doc = JSON.parse(json) as {
      nodes?: Array<Record<string, unknown>>
    }
    if (!Array.isArray(doc.nodes)) return json
    let changed = false
    doc.nodes = doc.nodes.map((node) => {
      if (node.key !== placementKey) return node
      const prev =
        node.glbPartTransforms && typeof node.glbPartTransforms === 'object'
          ? (node.glbPartTransforms as Record<string, GlbPartWireTransform>)
          : {}
      changed = true
      return {
        ...node,
        glbPartTransforms: {
          ...prev,
          [outlinerPath]: transform,
        },
      }
    })
    return changed ? JSON.stringify(doc) : json
  } catch {
    return json
  }
}

/**
 * Edit `sceneSync` omits live `glbPartTransforms`. When updating the room
 * cache, carry forward part overrides already accumulated so late joiners
 * and replay do not lose sub-object moves.
 */
function mergeSceneSyncCachePreservingParts(
  cachedJson: string | null,
  incomingJson: string,
): string {
  if (!cachedJson) return incomingJson
  try {
    const incoming = JSON.parse(incomingJson) as {
      nodes?: Array<Record<string, unknown>>
    }
    const cached = JSON.parse(cachedJson) as {
      nodes?: Array<Record<string, unknown>>
    }
    if (!Array.isArray(incoming.nodes) || !Array.isArray(cached.nodes)) {
      return incomingJson
    }
    const cachedPartsByKey = new Map<string, Record<string, GlbPartWireTransform>>()
    for (const node of cached.nodes) {
      const key = typeof node.key === 'string' ? node.key : ''
      const parts = node.glbPartTransforms
      if (
        key &&
        parts &&
        typeof parts === 'object' &&
        Object.keys(parts as object).length > 0
      ) {
        cachedPartsByKey.set(
          key,
          parts as Record<string, GlbPartWireTransform>,
        )
      }
    }
    if (cachedPartsByKey.size === 0) return incomingJson
    incoming.nodes = incoming.nodes.map((node) => {
      const key = typeof node.key === 'string' ? node.key : ''
      const cachedParts = key ? cachedPartsByKey.get(key) : undefined
      if (!cachedParts) return node
      const incomingParts = node.glbPartTransforms
      if (
        incomingParts &&
        typeof incomingParts === 'object' &&
        Object.keys(incomingParts as object).length > 0
      ) {
        return {
          ...node,
          glbPartTransforms: {
            ...cachedParts,
            ...(incomingParts as Record<string, GlbPartWireTransform>),
          },
        }
      }
      return { ...node, glbPartTransforms: cachedParts }
    })
    return JSON.stringify(incoming)
  } catch {
    return incomingJson
  }
}

/** Compact relay cache + strip outliner-only bulk from legacy / pretty-printed clients. */
function normalizeSceneSyncJson(raw: string): string | null {
  try {
    const parsed = JSON.parse(raw) as {
      nodes?: Array<Record<string, unknown>>
      [key: string]: unknown
    }
    if (Array.isArray(parsed.nodes)) {
      parsed.nodes = parsed.nodes.map((node) => {
        const { outlinerTree: _ot, editorOutlinerComponents: _eoc, ...rest } =
          node
        return rest
      })
    }
    return JSON.stringify(parsed)
  } catch {
    return raw
  }
}

interface VoiceStatePayload {
  voiceOn?: boolean
  voiceTalking?: boolean
}

interface VoiceSignalPayload {
  /** Target Colyseus `sessionId` (recipient only). */
  to?: string
  kind?: string
  sdp?: string
  /** `RTCIceCandidateInit`-shaped JSON; relayed verbatim. */
  ice?: Record<string, unknown> | null
}

const NUMERIC = (value: unknown): number | null => {
  if (typeof value === 'number' && Number.isFinite(value)) return value
  return null
}

export interface CollabRoomDeps {
  config: CollabServerConfig
  educollabClient: EduCollabClient
}

interface CollabRoomShape {
  state: SessionState
  metadata: {
    sessionId: string
    name: string
    assetKind: string | null
    assetId: string | null
  }
}

export class CollabRoom extends Room<CollabRoomShape> {
  private deps!: CollabRoomDeps
  private sessionMeta!: RoomSessionMeta
  private static deps: CollabRoomDeps | null = null
  /** Last scene JSON from an author; replayed to new joins (same cap as relay). */
  private lastSceneSyncJson: string | null = null

  static configure(deps: CollabRoomDeps): void {
    CollabRoom.deps = deps
  }

  override onCreate(options: CollabRoomOptions): void {
    const deps = CollabRoom.deps
    if (!deps) {
      throw new Error('CollabRoom not configured. Call CollabRoom.configure() first.')
    }
    this.deps = deps

    const sessionIdRaw = options.sessionId
    if (!sessionIdRaw) {
      throw new Error('Missing sessionId on room create')
    }

    const joinTicket =
      extractBearer({ queryToken: options.joinTicket ?? null }) ??
      options.joinTicket ??
      null
    if (!joinTicket) {
      throw new Error('Missing joinTicket on room create')
    }

    const claims = verifyJoinTicket(
      joinTicket,
      this.deps.config.sessionJoinSecret,
      this.deps.config.sessionJoinIssuer,
      this.deps.config.sessionJoinAudience,
    )
    if (!claims) {
      throw new Error('Invalid join ticket on room create')
    }
    if (String(claims.sessionId) !== String(sessionIdRaw)) {
      throw new Error('Join ticket session mismatch on room create')
    }

    this.sessionMeta = claimsToSessionMeta(claims)
    this.roomId = String(this.sessionMeta.sessionId)
    this.maxClients = 64

    const state = new SessionState()
    state.sessionId = String(this.sessionMeta.sessionId)
    state.name = this.sessionMeta.name
    state.assetKind = this.sessionMeta.assetKind ?? ''
    state.assetId = this.sessionMeta.assetId ?? ''
    state.assetName = this.sessionMeta.assetName ?? ''
    this.state = state

    void this.setMetadata({
      sessionId: String(this.sessionMeta.sessionId),
      name: this.sessionMeta.name,
      assetKind: this.sessionMeta.assetKind,
      assetId: this.sessionMeta.assetId,
    })

    void this.deps.educollabClient.notifyRoomStarted(this.sessionMeta.sessionId)

    // Wrap every message handler: errors thrown here propagate up to the
    // ws-transport message frame and look like a generic 4002 close from
    // the client's perspective. Logging the type + sessionId makes the
    // root cause obvious.
    const safe = <T>(type: string, fn: (client: Client, payload: T) => void) =>
      (client: Client, payload: T) => {
        try {
          fn(client, payload)
        } catch (err) {
          console.error(
            `[collab-room ${this.roomId}] message '${type}' handler threw (sessionId=${client.sessionId}):`,
            err,
          )
        }
      }

    this.onMessage<CameraUpdatePayload>('camera', safe('camera', (client, payload) => {
      this.handleCameraUpdate(client, payload)
    }))
    this.onMessage<RoleUpdatePayload>('roleUpdate', safe('roleUpdate', (client, payload) => {
      this.handleRoleUpdate(client, payload)
    }))
    this.onMessage<PresenterPayload>('setPresenter', safe('setPresenter', (client, payload) => {
      this.handleSetPresenter(client, payload)
    }))
    this.onMessage<CursorPayload>('cursor', safe('cursor', (client, payload) => {
      this.handleCursorUpdate(client, payload)
    }))
    this.onMessage<SceneSyncPayload>('sceneSync', safe('sceneSync', (client, payload) => {
      this.handleSceneSync(client, payload)
    }))
    this.onMessage<GlbPartSelectPayload>(
      'glbPartSelect',
      safe('glbPartSelect', (client, payload) => {
        this.relayGlbPartSelect(client, payload)
      }),
    )
    this.onMessage<GlbPartTransformPayload>(
      'glbPartTransform',
      safe('glbPartTransform', (client, payload) => {
        this.relayGlbPartTransform(client, payload)
      }),
    )
    this.onMessage<VoiceStatePayload>('voiceState', safe('voiceState', (client, payload) => {
      this.handleVoiceState(client, payload)
    }))
    this.onMessage<VoiceSignalPayload>('voiceSignal', safe('voiceSignal', (client, payload) => {
      this.handleVoiceSignal(client, payload)
    }))
  }

  /**
   * Authorize a connection attempt. Colyseus calls this BEFORE onJoin.
   * Returning truthy here allows the join; we stash auth+role in the userData.
   *
   * Throwing here causes Colyseus to close the WS with `CloseCode.WITH_ERROR`
   * (4002). We log the precise reason so the operator can tell auth/role
   * problems apart from later runtime errors that share the same close code.
   */
  override async onAuth(client: Client, options: CollabRoomOptions): Promise<ClientUserData> {
    try {
      const headerToken = (() => {
        const headers = ((client as unknown) as { auth?: { headers?: Record<string, string | string[]> } }).auth?.headers
        const raw = headers?.['authorization']
        if (typeof raw === 'string') return raw
        if (Array.isArray(raw) && typeof raw[0] === 'string') return raw[0]
        return null
      })()

      const joinTicket =
        extractBearer({
          headers: { authorization: headerToken },
          queryToken: options.joinTicket ?? null,
        }) ?? options.joinTicket ?? null
      if (!joinTicket) {
        console.warn(
          `[collab-room ${this.roomId}] onAuth rejected: missing join ticket (sessionId=${client.sessionId})`,
        )
        throw new Error('Missing join ticket')
      }

      const claims = verifyJoinTicket(
        joinTicket,
        this.deps.config.sessionJoinSecret,
        this.deps.config.sessionJoinIssuer,
        this.deps.config.sessionJoinAudience,
      )
      if (!claims) {
        console.warn(
          `[collab-room ${this.roomId}] onAuth rejected: invalid/expired join ticket (sessionId=${client.sessionId})`,
        )
        throw new Error('Invalid or expired join ticket')
      }
      if (String(claims.sessionId) !== String(this.sessionMeta.sessionId)) {
        console.warn(
          `[collab-room ${this.roomId}] onAuth rejected: session mismatch (sessionId=${client.sessionId})`,
        )
        throw new Error('Join ticket session mismatch')
      }

      const identity: JoinIdentity = {
        principal: claims.sub,
        displayName: claims.displayName,
        isGuest: claims.sub.startsWith('guest:'),
      }
      const role = claims.colyseusRole
      const wantsSceneSync = options.wantsSceneSync === true
      console.info(
        `[collab-room ${this.roomId}] onAuth ok user=${identity.principal} role=${role} wantsSceneSync=${wantsSceneSync} sessionId=${client.sessionId}`,
      )
      return { identity, role, session: this.sessionMeta, wantsSceneSync }
    } catch (err) {
      console.error(
        `[collab-room ${this.roomId}] onAuth threw (sessionId=${client.sessionId}):`,
        err,
      )
      throw err
    }
  }

  override onJoin(client: Client, _options: CollabRoomOptions, userData?: ClientUserData): void {
    try {
      const data = userData ?? (client.userData as ClientUserData | undefined)
      if (!data) {
        console.warn(
          `[collab-room ${this.roomId}] onJoin without userData (sessionId=${client.sessionId})`,
        )
        client.leave(4000, 'Missing auth context')
        return
      }
      client.userData = data

      const participant = new ParticipantState()
      participant.userId = data.identity.principal
      participant.userName = data.identity.displayName || 'guest'
      participant.role = data.role
      participant.connected = true
      participant.camNav = 'orbit'
      participant.voiceOn = false
      participant.voiceTalking = false
      this.state.participants.set(client.sessionId, participant)

      console.info(
        `[collab-room ${this.roomId}] onJoin ok user=${data.identity.principal} role=${data.role} sessionId=${client.sessionId} clients=${this.clients.length}`,
      )

      // Send the cached scene to the late joiner *after* JOIN_ROOM has been
      // acknowledged, and only if they asked for it. Join-page clients
      // (`wantsSceneSync=false`) don't render the scene — pushing multi-MB
      // JSON at them was the cause of WS close 1009 ("message too big").
      if (
        data.wantsSceneSync &&
        this.lastSceneSyncJson &&
        this.lastSceneSyncJson.length > 0
      ) {
        const snapshot = this.lastSceneSyncJson
        const replayCap = 1 * 1024 * 1024
        if (snapshot.length > replayCap) {
          console.warn(
            `[collab-room ${this.roomId}] skipping replay to late joiner sessionId=${client.sessionId}: cached scene ${snapshot.length} bytes > cap ${replayCap}`,
          )
        } else {
          setImmediate(() => {
            try {
              console.info(
                `[collab-room ${this.roomId}] replay sceneSync to late joiner sessionId=${client.sessionId} bytes=${snapshot.length}`,
              )
              client.send('sceneSync', { json: snapshot })
            } catch (err) {
              console.error(
                `[collab-room ${this.roomId}] failed to replay sceneSync to late joiner (sessionId=${client.sessionId}):`,
                err,
              )
            }
          })
        }
      }
    } catch (err) {
      console.error(
        `[collab-room ${this.roomId}] onJoin threw (sessionId=${client.sessionId}):`,
        err,
      )
      throw err
    }
  }

  override onLeave(client: Client, code?: number): void {
    const participant = this.state.participants.get(client.sessionId)
    if (participant) {
      participant.connected = false
    }
    this.state.participants.delete(client.sessionId)
    // Colyseus removes the client from `this.clients` before invoking onLeave,
    // so its current length already excludes the leaver — no need to subtract.
    const remaining = Math.max(0, this.clients.length)
    console.info(
      `[collab-room ${this.roomId}] onLeave sessionId=${client.sessionId} code=${code ?? 'unknown'} remaining=${remaining}`,
    )
  }

  override onDispose(): void {
    if (this.sessionMeta) {
      void this.deps.educollabClient.notifyRoomEnded(this.sessionMeta.sessionId)
    }
  }

  // ---- message handlers ----

  private handleVoiceState(client: Client, payload: VoiceStatePayload): void {
    const participant = this.state.participants.get(client.sessionId)
    if (!participant) return
    if (typeof payload.voiceOn === 'boolean') {
      participant.voiceOn = payload.voiceOn
      if (!payload.voiceOn) participant.voiceTalking = false
    }
    if (typeof payload.voiceTalking === 'boolean') {
      participant.voiceTalking = participant.voiceOn ? payload.voiceTalking : false
    }
  }

  private handleVoiceSignal(client: Client, payload: VoiceSignalPayload): void {
    const to =
      typeof payload.to === 'string' && payload.to.length > 0 ? payload.to : null
    if (!to) return
    const peer = this.clients.find((c) => c.sessionId === to)
    if (!peer || peer === client) return
    try {
      peer.send('voiceSignal', {
        from: client.sessionId,
        kind: typeof payload.kind === 'string' ? payload.kind : '',
        sdp: typeof payload.sdp === 'string' ? payload.sdp : undefined,
        ice: payload.ice && typeof payload.ice === 'object' ? payload.ice : undefined,
      })
    } catch (err) {
      console.error(
        `[collab-room ${this.roomId}] voiceSignal relay failed (to=${to}):`,
        err,
      )
    }
  }

  private handleCameraUpdate(client: Client, payload: CameraUpdatePayload): void {
    const participant = this.state.participants.get(client.sessionId)
    if (!participant) return
    const data = client.userData as ClientUserData | undefined
    if (!data) return
    const camX = NUMERIC(payload.camX)
    const camY = NUMERIC(payload.camY)
    const camZ = NUMERIC(payload.camZ)
    const tgtX = NUMERIC(payload.tgtX)
    const tgtY = NUMERIC(payload.tgtY)
    const tgtZ = NUMERIC(payload.tgtZ)
    if (camX !== null) participant.camX = camX
    if (camY !== null) participant.camY = camY
    if (camZ !== null) participant.camZ = camZ
    if (tgtX !== null) participant.tgtX = tgtX
    if (tgtY !== null) participant.tgtY = tgtY
    if (tgtZ !== null) participant.tgtZ = tgtZ
    if (typeof payload.nav === 'string') {
      const n = payload.nav.trim().toLowerCase()
      if (n === 'fps' || n === 'walk') participant.camNav = 'walk'
      else if (n === 'ar') participant.camNav = 'ar'
      else if (n === 'vr') participant.camNav = 'vr'
      else if (n === 'orbit') participant.camNav = 'orbit'
    }
  }

  private handleCursorUpdate(client: Client, payload: CursorPayload): void {
    const participant = this.state.participants.get(client.sessionId)
    if (!participant) return
    const hx = NUMERIC(payload.hitX)
    const hy = NUMERIC(payload.hitY)
    const hz = NUMERIC(payload.hitZ)
    if (hx !== null) participant.curHitX = hx
    if (hy !== null) participant.curHitY = hy
    if (hz !== null) participant.curHitZ = hz
    if (typeof payload.hasHit === 'boolean') participant.curHasHit = payload.hasHit
    if (typeof payload.active === 'boolean') participant.curOn = payload.active
  }

  /**
   * Authoritative scene document broadcast. Host, editor, and presenter may publish;
   * server relays to everyone else (late joiners miss history until the next publish — see client initial push on connect).
   *
   * Only relay to clients with `wantsSceneSync=true` (i.e. editors). Join-page
   * clients don't render the scene; sending them several MB of JSON each time
   * the scene changes used to crash their WebSocket with close code 1009.
   */
  private relayGlbPartSelect(client: Client, payload: GlbPartSelectPayload): void {
    const placementKey =
      typeof payload.placementKey === 'string' ? payload.placementKey.trim() : ''
    if (!placementKey) return
    const outlinerPath =
      payload.outlinerPath == null
        ? null
        : typeof payload.outlinerPath === 'string' &&
            payload.outlinerPath.length > 0
          ? payload.outlinerPath
          : null
    for (const peer of this.clients) {
      if (peer === client) continue
      const peerData = peer.userData as ClientUserData | undefined
      if (!peerData?.wantsSceneSync) continue
      try {
        peer.send('glbPartSelect', {
          from: client.sessionId,
          placementKey,
          outlinerPath,
        })
      } catch (err) {
        console.error(
          `[collab-room ${this.roomId}] glbPartSelect relay failed (to=${peer.sessionId}):`,
          err,
        )
      }
    }
  }

  private relayGlbPartTransform(
    client: Client,
    payload: GlbPartTransformPayload,
  ): void {
    const placementKey =
      typeof payload.placementKey === 'string' ? payload.placementKey.trim() : ''
    const outlinerPath =
      typeof payload.outlinerPath === 'string' ? payload.outlinerPath.trim() : ''
    const transform = payload.transform
    if (!placementKey || !outlinerPath || !transform) {
      return
    }
    const position = asNumericTriple(transform.position)
    const rotation = asNumericTriple(transform.rotation)
    const scale = asNumericTriple(transform.scale)
    if (!position || !rotation || !scale) {
      console.warn(
        `[collab-room ${this.roomId}] glbPartTransform dropped (invalid transform) from=${client.sessionId}`,
      )
      return
    }
    const normalizedTransform = { position, rotation, scale }
    if (this.lastSceneSyncJson) {
      this.lastSceneSyncJson = patchGlbPartIntoSceneJson(
        this.lastSceneSyncJson,
        placementKey,
        outlinerPath,
        normalizedTransform,
      )
    }
    for (const peer of this.clients) {
      if (peer === client) continue
      const peerData = peer.userData as ClientUserData | undefined
      if (!peerData?.wantsSceneSync) continue
      try {
        peer.send('glbPartTransform', {
          from: client.sessionId,
          placementKey,
          outlinerPath,
          transform: normalizedTransform,
        })
      } catch (err) {
        console.error(
          `[collab-room ${this.roomId}] glbPartTransform relay failed (to=${peer.sessionId}):`,
          err,
        )
      }
    }
  }

  private handleSceneSync(client: Client, payload: SceneSyncPayload): void {
    const data = client.userData as ClientUserData | undefined
    if (!data) return
    /**
     * Authoring is currently open to every authenticated participant so
     * bidirectional editing works in dev. The role-based gate is deferred
     * until the per-role permission matrix for the collab editor is defined
     * (see project notes / pending todo #9).
     */
    let json = typeof payload.json === 'string' ? payload.json : null
    if (!json || json.length === 0) return
    json = normalizeSceneSyncJson(json)
    if (!json || json.length === 0) return
    // Match the editor-side `MAX_SCENE_SYNC_BYTES` cap. WS frames over ~1 MB
    // are unreliable across browsers / proxies and can trigger close code
    // 1009 ("message too big") on the receiving peer (Safari/WebKit is the
    // most aggressive about closing).
    const maxBytes = 1 * 1024 * 1024
    if (json.length > maxBytes) {
      console.warn(
        `[collab-room ${this.roomId}] dropping sceneSync from sessionId=${client.sessionId}: ${json.length} bytes > cap ${maxBytes}`,
      )
      return
    }
    const mergedJson = mergeSceneSyncCachePreservingParts(
      this.lastSceneSyncJson,
      json,
    )
    this.lastSceneSyncJson = mergedJson
    const relayJson = mergedJson.length <= maxBytes ? mergedJson : json

    let relayed = 0
    for (const peer of this.clients) {
      if (peer === client) continue
      const peerData = peer.userData as ClientUserData | undefined
      if (!peerData?.wantsSceneSync) continue
      try {
        peer.send('sceneSync', { json: relayJson })
        relayed++
      } catch (err) {
        console.error(
          `[collab-room ${this.roomId}] failed to relay sceneSync to sessionId=${peer.sessionId}:`,
          err,
        )
      }
    }
    console.info(
      `[collab-room ${this.roomId}] sceneSync from sessionId=${client.sessionId} bytes=${json.length} relayedTo=${relayed}`,
    )
  }

  private handleRoleUpdate(client: Client, payload: RoleUpdatePayload): void {
    const data = client.userData as ClientUserData | undefined
    if (!data || data.role !== 'host') return
    const targetUserId = typeof payload.userId === 'string' ? payload.userId : null
    if (!targetUserId) return
    if (!isParticipantRole(payload.role)) return
    if (targetUserId === this.sessionMeta.hostUserId) return
    // Update everyone in the room with that user id.
    for (const [sessionId, participant] of this.state.participants.entries()) {
      if (participant.userId === targetUserId) {
        participant.role = payload.role
        const peer = this.clients.find((c) => c.sessionId === sessionId)
        if (peer && peer.userData) {
          ;(peer.userData as ClientUserData).role = payload.role
        }
      }
    }
  }

  private handleSetPresenter(client: Client, payload: PresenterPayload): void {
    const data = client.userData as ClientUserData | undefined
    if (!data || data.role !== 'host') return
    const target =
      typeof payload.userId === 'string' && payload.userId.length > 0
        ? payload.userId
        : ''
    this.state.presenterUserId = target
  }
}
