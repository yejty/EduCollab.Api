import { v4 as uuidv4 } from 'uuid'
import { getDatabase } from './db'
import {
  isParticipantRole,
  isSessionAssetKind,
  type ParticipantRole,
  type SessionAssetKind,
  type SessionGrantRecord,
  type SessionRecord,
  type SessionStatus,
} from './types'

interface SessionRow {
  id: string
  name: string
  description: string
  host_user_id: string
  host_user_name: string | null
  asset_id: string | null
  asset_kind: string | null
  asset_name: string | null
  default_role: string
  status: string
  created_at_ms: number
  updated_at_ms: number
  settings_json: string
}

interface GrantRow {
  session_id: string
  user_id: string
  user_name: string | null
  role: string
}

function rowToSession(row: SessionRow): SessionRecord {
  let settings: Record<string, unknown> = {}
  try {
    const parsed = JSON.parse(row.settings_json) as unknown
    if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
      settings = parsed as Record<string, unknown>
    }
  } catch {
    settings = {}
  }
  const status: SessionStatus =
    row.status === 'idle' || row.status === 'live' || row.status === 'closed'
      ? row.status
      : 'idle'
  const defaultRole: ParticipantRole = isParticipantRole(row.default_role)
    ? row.default_role
    : 'viewer'
  const assetKind: SessionAssetKind | null = isSessionAssetKind(row.asset_kind)
    ? row.asset_kind
    : null
  return {
    id: row.id,
    name: row.name,
    description: row.description,
    hostUserId: row.host_user_id,
    hostUserName: row.host_user_name,
    assetId: row.asset_id,
    assetKind,
    assetName: row.asset_name,
    defaultRole,
    status,
    createdAtMs: row.created_at_ms,
    updatedAtMs: row.updated_at_ms,
    settings,
  }
}

function rowToGrant(row: GrantRow): SessionGrantRecord {
  return {
    sessionId: row.session_id,
    userId: row.user_id,
    userName: row.user_name,
    role: isParticipantRole(row.role) ? row.role : 'viewer',
  }
}

export interface CreateSessionInput {
  name: string
  description?: string
  hostUserId: string
  hostUserName: string | null
  assetId?: string | null
  assetKind?: SessionAssetKind | null
  assetName?: string | null
  defaultRole?: ParticipantRole
  settings?: Record<string, unknown>
}

export function createSession(input: CreateSessionInput): SessionRecord {
  const db = getDatabase()
  const now = Date.now()
  const id = uuidv4()
  const defaultRole: ParticipantRole = input.defaultRole ?? 'viewer'
  const settingsJson = JSON.stringify(input.settings ?? {})

  db.prepare(
    `INSERT INTO sessions (
      id, name, description, host_user_id, host_user_name,
      asset_id, asset_kind, asset_name, default_role, status,
      created_at_ms, updated_at_ms, settings_json
    ) VALUES (
      @id, @name, @description, @host_user_id, @host_user_name,
      @asset_id, @asset_kind, @asset_name, @default_role, 'idle',
      @created_at_ms, @updated_at_ms, @settings_json
    )`,
  ).run({
    id,
    name: input.name,
    description: input.description ?? '',
    host_user_id: input.hostUserId,
    host_user_name: input.hostUserName,
    asset_id: input.assetId ?? null,
    asset_kind: input.assetKind ?? null,
    asset_name: input.assetName ?? null,
    default_role: defaultRole,
    created_at_ms: now,
    updated_at_ms: now,
    settings_json: settingsJson,
  })

  // Always grant the creator the host role explicitly so role lookups are uniform.
  db.prepare(
    `INSERT OR REPLACE INTO session_grants (session_id, user_id, user_name, role)
     VALUES (@session_id, @user_id, @user_name, 'host')`,
  ).run({
    session_id: id,
    user_id: input.hostUserId,
    user_name: input.hostUserName,
  })

  const created = getSessionById(id)
  if (!created) throw new Error('Failed to read back created session')
  return created
}

export function getSessionById(id: string): SessionRecord | null {
  const row = getDatabase()
    .prepare('SELECT * FROM sessions WHERE id = ?')
    .get(id) as SessionRow | undefined
  return row ? rowToSession(row) : null
}

/** Alternate JWT claims that may have been used as grant keys in older sessions. */
export function alternatePrincipalsForIdentity(identity: {
  userId: string
  userName: string | null
  email: string | null
}): string[] {
  const alternates: string[] = []
  if (identity.userName && identity.userName !== identity.userId) {
    alternates.push(identity.userName)
  }
  if (identity.email && identity.email !== identity.userId) {
    alternates.push(identity.email)
  }
  return alternates
}

function principalMatches(
  userId: string,
  alternates: readonly string[],
  stored: string,
): boolean {
  if (stored === userId) return true
  return alternates.includes(stored)
}

/** Lists every session visible to a user: hosted by them OR they have an explicit grant. */
export function listSessionsForUser(
  userId: string,
  alternates: readonly string[] = [],
): SessionRecord[] {
  const principals = Array.from(new Set([userId, ...alternates]))
  const placeholders = principals.map(() => '?').join(', ')
  const rows = getDatabase()
    .prepare(
      `SELECT DISTINCT s.* FROM sessions s
       LEFT JOIN session_grants g ON g.session_id = s.id AND g.user_id IN (${placeholders})
       WHERE s.host_user_id IN (${placeholders}) OR g.user_id IN (${placeholders})
       ORDER BY s.updated_at_ms DESC`,
    )
    .all(...principals, ...principals, ...principals) as SessionRow[]
  return rows.map(rowToSession)
}

export interface UpdateSessionInput {
  name?: string
  description?: string
  assetId?: string | null
  assetKind?: SessionAssetKind | null
  assetName?: string | null
  defaultRole?: ParticipantRole
  status?: SessionStatus
  settings?: Record<string, unknown>
}

export function updateSession(
  id: string,
  patch: UpdateSessionInput,
): SessionRecord | null {
  const existing = getSessionById(id)
  if (!existing) return null
  const next = { ...existing }
  if (patch.name !== undefined) next.name = patch.name
  if (patch.description !== undefined) next.description = patch.description
  if (patch.assetId !== undefined) next.assetId = patch.assetId
  if (patch.assetKind !== undefined) next.assetKind = patch.assetKind
  if (patch.assetName !== undefined) next.assetName = patch.assetName
  if (patch.defaultRole !== undefined) next.defaultRole = patch.defaultRole
  if (patch.status !== undefined) next.status = patch.status
  if (patch.settings !== undefined) next.settings = patch.settings
  const now = Date.now()
  getDatabase()
    .prepare(
      `UPDATE sessions SET
         name = @name,
         description = @description,
         asset_id = @asset_id,
         asset_kind = @asset_kind,
         asset_name = @asset_name,
         default_role = @default_role,
         status = @status,
         settings_json = @settings_json,
         updated_at_ms = @updated_at_ms
       WHERE id = @id`,
    )
    .run({
      id,
      name: next.name,
      description: next.description,
      asset_id: next.assetId,
      asset_kind: next.assetKind,
      asset_name: next.assetName,
      default_role: next.defaultRole,
      status: next.status,
      settings_json: JSON.stringify(next.settings),
      updated_at_ms: now,
    })
  return getSessionById(id)
}

export function deleteSession(id: string): boolean {
  const result = getDatabase()
    .prepare('DELETE FROM sessions WHERE id = ?')
    .run(id)
  return result.changes > 0
}

export function listGrantsForSession(
  sessionId: string,
): SessionGrantRecord[] {
  const rows = getDatabase()
    .prepare(
      'SELECT * FROM session_grants WHERE session_id = ? ORDER BY user_name COLLATE NOCASE',
    )
    .all(sessionId) as GrantRow[]
  return rows.map(rowToGrant)
}

export function getGrantForUser(
  sessionId: string,
  userId: string,
  alternates: readonly string[] = [],
): SessionGrantRecord | null {
  const rows = getDatabase()
    .prepare('SELECT * FROM session_grants WHERE session_id = ?')
    .all(sessionId) as GrantRow[]
  for (const row of rows) {
    if (principalMatches(userId, alternates, row.user_id)) {
      return rowToGrant(row)
    }
  }
  return null
}

export interface UpsertGrantInput {
  sessionId: string
  userId: string
  userName: string | null
  role: ParticipantRole
}

export function upsertGrant(input: UpsertGrantInput): SessionGrantRecord {
  getDatabase()
    .prepare(
      `INSERT INTO session_grants (session_id, user_id, user_name, role)
       VALUES (@session_id, @user_id, @user_name, @role)
       ON CONFLICT(session_id, user_id) DO UPDATE SET
         user_name = excluded.user_name,
         role = excluded.role`,
    )
    .run({
      session_id: input.sessionId,
      user_id: input.userId,
      user_name: input.userName,
      role: input.role,
    })
  const grant = getGrantForUser(input.sessionId, input.userId)
  if (!grant) throw new Error('Failed to read back upserted grant')
  return grant
}

export function deleteGrant(sessionId: string, userId: string): boolean {
  const result = getDatabase()
    .prepare(
      'DELETE FROM session_grants WHERE session_id = ? AND user_id = ?',
    )
    .run(sessionId, userId)
  return result.changes > 0
}

/**
 * Resolve the effective role of `userId` in `session`.
 * Falls back to the session's `defaultRole` when no grant exists.
 */
export function resolveRoleForUser(
  session: SessionRecord,
  userId: string,
  alternates: readonly string[] = [],
): ParticipantRole {
  if (principalMatches(userId, alternates, session.hostUserId)) return 'host'
  const grant = getGrantForUser(session.id, userId, alternates)
  if (grant) return grant.role
  return session.defaultRole
}
