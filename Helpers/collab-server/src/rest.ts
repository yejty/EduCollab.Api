import express, {
  type NextFunction,
  type Request,
  type Response,
  type Router,
} from 'express'
import { authenticateBearer, extractBearer } from './auth'
import type { CollabServerConfig } from './config'
import {
  alternatePrincipalsForIdentity,
  createSession,
  deleteGrant,
  deleteSession,
  getSessionById,
  listGrantsForSession,
  listSessionsForUser,
  resolveRoleForUser,
  updateSession,
  upsertGrant,
} from './sessions'
import {
  isParticipantRole,
  isSessionAssetKind,
  type AuthIdentity,
  type ParticipantRole,
  type SessionAssetKind,
  type SessionRecord,
  type SessionStatus,
} from './types'

interface AuthedRequest extends Request {
  auth?: AuthIdentity
}

function requireAuth(config: CollabServerConfig) {
  return async (
    req: AuthedRequest,
    res: Response,
    next: NextFunction,
  ): Promise<void> => {
    const token = extractBearer({
      headers: { authorization: req.headers.authorization ?? null },
    })
    if (!token) {
      res.status(401).json({ error: 'Missing bearer token' })
      return
    }
    const identity = await authenticateBearer(token, config)
    if (!identity) {
      res.status(401).json({ error: 'Invalid or expired bearer token' })
      return
    }
    req.auth = identity
    next()
  }
}

function asString(value: unknown): string | null {
  if (typeof value === 'string') return value
  if (typeof value === 'number') return String(value)
  return null
}

/** Express 5 typings widen `req.params.x` to `string | string[]`; we always want the first scalar. */
function paramValue(raw: string | string[] | undefined): string | null {
  if (typeof raw === 'string') {
    const t = raw.trim()
    return t.length > 0 ? t : null
  }
  if (Array.isArray(raw) && raw.length > 0 && typeof raw[0] === 'string') {
    const t = raw[0].trim()
    return t.length > 0 ? t : null
  }
  return null
}

function asNonEmptyString(value: unknown, fallback?: string): string {
  if (typeof value === 'string') {
    const t = value.trim()
    if (t.length > 0) return t
  }
  if (typeof value === 'number' && Number.isFinite(value)) {
    return String(value)
  }
  if (fallback !== undefined) return fallback
  throw new Error('Expected non-empty string')
}

function asOptionalString(value: unknown): string | null | undefined {
  if (value === undefined) return undefined
  if (value === null) return null
  return asString(value)
}

function serializeSession(record: SessionRecord) {
  return {
    id: record.id,
    name: record.name,
    description: record.description,
    hostUserId: record.hostUserId,
    hostUserName: record.hostUserName,
    assetId: record.assetId,
    assetKind: record.assetKind,
    assetName: record.assetName,
    defaultRole: record.defaultRole,
    status: record.status,
    createdAtMs: record.createdAtMs,
    updatedAtMs: record.updatedAtMs,
    settings: record.settings,
  }
}

export function createRestRouter(config: CollabServerConfig): Router {
  const router = express.Router()
  const auth = requireAuth(config)

  router.get('/healthz', (_req, res) => {
    res.json({ ok: true, name: 'alphacollab-collab-server' })
  })

  router.use(auth)

  // ---- sessions ----
  router.get('/sessions', (req: AuthedRequest, res: Response) => {
    const identity = req.auth
    if (!identity) {
      res.status(401).end()
      return
    }
    const alternates = alternatePrincipalsForIdentity(identity)
    const sessions = listSessionsForUser(identity.userId, alternates).map(serializeSession)
    res.json({ sessions })
  })

  router.post('/sessions', (req: AuthedRequest, res: Response) => {
    const identity = req.auth
    if (!identity) {
      res.status(401).end()
      return
    }
    const body = (req.body ?? {}) as Record<string, unknown>
    let name: string
    try {
      name = asNonEmptyString(body.name)
    } catch {
      res.status(400).json({ error: 'Field "name" is required' })
      return
    }

    const description = typeof body.description === 'string' ? body.description : ''
    const assetIdRaw = body.assetId
    const assetKindRaw = body.assetKind
    /**
     * `assetKind` is independent of `assetId` so that an "Empty scene"
     * session can be created (kind='scene', id=null). Validation only runs
     * when the client actually supplies a kind; absence keeps the session
     * unbound (kind=null), which is the legacy "(none)" path.
     */
    let assetKind: SessionAssetKind | null = null
    if (assetKindRaw != null) {
      if (!isSessionAssetKind(assetKindRaw)) {
        res.status(400).json({ error: 'Field "assetKind" must be "scene" or "flow"' })
        return
      }
      assetKind = assetKindRaw
    }

    const defaultRoleRaw = body.defaultRole ?? 'viewer'
    if (!isParticipantRole(defaultRoleRaw)) {
      res.status(400).json({ error: 'Invalid "defaultRole"' })
      return
    }

    const session = createSession({
      name,
      description,
      hostUserId: identity.userId,
      hostUserName: identity.userName,
      assetId: assetIdRaw == null ? null : asString(assetIdRaw),
      assetKind,
      assetName:
        typeof body.assetName === 'string' ? body.assetName : null,
      defaultRole: defaultRoleRaw,
      settings:
        body.settings && typeof body.settings === 'object' && !Array.isArray(body.settings)
          ? (body.settings as Record<string, unknown>)
          : undefined,
    })
    res.status(201).json(serializeSession(session))
  })

  router.get('/sessions/:id', (req: AuthedRequest, res: Response) => {
    const identity = req.auth
    if (!identity) {
      res.status(401).end()
      return
    }
    const id = paramValue(req.params.id)
    if (!id) {
      res.status(400).json({ error: 'Missing session id' })
      return
    }
    const session = getSessionById(id)
    if (!session) {
      res.status(404).json({ error: 'Session not found' })
      return
    }
    const alternates = alternatePrincipalsForIdentity(identity)
    const role = resolveRoleForUser(session, identity.userId, alternates)
    const grants = listGrantsForSession(id)
    res.json({
      session: serializeSession(session),
      grants,
      effectiveRole: role,
    })
  })

  router.patch('/sessions/:id', (req: AuthedRequest, res: Response) => {
    const identity = req.auth
    if (!identity) {
      res.status(401).end()
      return
    }
    const id = paramValue(req.params.id)
    if (!id) {
      res.status(400).json({ error: 'Missing session id' })
      return
    }
    const session = getSessionById(id)
    if (!session) {
      res.status(404).json({ error: 'Session not found' })
      return
    }
    const alternates = alternatePrincipalsForIdentity(identity)
    const role = resolveRoleForUser(session, identity.userId, alternates)
    if (role !== 'host') {
      res.status(403).json({ error: 'Only the host can update this session' })
      return
    }
    const body = (req.body ?? {}) as Record<string, unknown>
    const patch: Parameters<typeof updateSession>[1] = {}
    if (typeof body.name === 'string' && body.name.trim().length > 0) {
      patch.name = body.name.trim()
    }
    if (typeof body.description === 'string') {
      patch.description = body.description
    }

    const assetIdMaybe = asOptionalString(body.assetId)
    if (assetIdMaybe !== undefined) {
      patch.assetId = assetIdMaybe
    }
    if (body.assetKind !== undefined) {
      if (body.assetKind === null) {
        patch.assetKind = null
      } else if (isSessionAssetKind(body.assetKind)) {
        patch.assetKind = body.assetKind
      } else {
        res.status(400).json({ error: 'Invalid "assetKind"' })
        return
      }
    }
    if (body.assetName !== undefined) {
      patch.assetName =
        body.assetName === null
          ? null
          : typeof body.assetName === 'string'
            ? body.assetName
            : null
    }
    if (body.defaultRole !== undefined) {
      if (!isParticipantRole(body.defaultRole)) {
        res.status(400).json({ error: 'Invalid "defaultRole"' })
        return
      }
      patch.defaultRole = body.defaultRole
    }
    if (body.status !== undefined) {
      const allowed: SessionStatus[] = ['idle', 'live', 'closed']
      if (
        typeof body.status === 'string' &&
        (allowed as readonly string[]).includes(body.status)
      ) {
        patch.status = body.status as SessionStatus
      } else {
        res.status(400).json({ error: 'Invalid "status"' })
        return
      }
    }
    if (body.settings && typeof body.settings === 'object' && !Array.isArray(body.settings)) {
      patch.settings = body.settings as Record<string, unknown>
    }

    const updated = updateSession(id, patch)
    res.json(serializeSession(updated!))
  })

  router.delete('/sessions/:id', (req: AuthedRequest, res: Response) => {
    const identity = req.auth
    if (!identity) {
      res.status(401).end()
      return
    }
    const id = paramValue(req.params.id)
    if (!id) {
      res.status(400).json({ error: 'Missing session id' })
      return
    }
    const session = getSessionById(id)
    if (!session) {
      res.status(204).end()
      return
    }
    if (session.hostUserId !== identity.userId) {
      res.status(403).json({ error: 'Only the host can delete this session' })
      return
    }
    deleteSession(id)
    res.status(204).end()
  })

  // ---- grants (per-user role) ----
  router.post('/sessions/:id/grants', (req: AuthedRequest, res: Response) => {
    const identity = req.auth
    if (!identity) {
      res.status(401).end()
      return
    }
    const id = paramValue(req.params.id)
    if (!id) {
      res.status(400).json({ error: 'Missing session id' })
      return
    }
    const session = getSessionById(id)
    if (!session) {
      res.status(404).json({ error: 'Session not found' })
      return
    }
    if (session.hostUserId !== identity.userId) {
      res.status(403).json({ error: 'Only the host can manage grants' })
      return
    }
    const body = (req.body ?? {}) as Record<string, unknown>
    let userId: string
    try {
      userId = asNonEmptyString(body.userId)
    } catch {
      res.status(400).json({ error: 'Field "userId" is required' })
      return
    }
    if (!isParticipantRole(body.role)) {
      res.status(400).json({ error: 'Invalid "role"' })
      return
    }
    const userName =
      typeof body.userName === 'string' ? body.userName : null
    const role: ParticipantRole = body.role
    const grant = upsertGrant({
      sessionId: id,
      userId,
      userName,
      role,
    })
    res.json(grant)
  })

  router.delete(
    '/sessions/:id/grants/:userId',
    (req: AuthedRequest, res: Response) => {
      const identity = req.auth
      if (!identity) {
        res.status(401).end()
        return
      }
      const id = paramValue(req.params.id)
      const userId = paramValue(req.params.userId)
      if (!id || !userId) {
        res.status(400).json({ error: 'Missing parameters' })
        return
      }
      const session = getSessionById(id)
      if (!session) {
        res.status(404).json({ error: 'Session not found' })
        return
      }
      if (session.hostUserId !== identity.userId) {
        res.status(403).json({ error: 'Only the host can manage grants' })
        return
      }
      if (userId === session.hostUserId) {
        res
          .status(400)
          .json({ error: 'Cannot revoke the host grant; delete the session instead' })
        return
      }
      deleteGrant(id, userId)
      res.status(204).end()
    },
  )

  return router
}
