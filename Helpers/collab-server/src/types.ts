/** Roles assigned to participants of a session. */
export const PARTICIPANT_ROLES = [
  'host',
  'editor',
  'presenter',
  'viewer',
] as const

export type ParticipantRole = (typeof PARTICIPANT_ROLES)[number]

export function isParticipantRole(value: unknown): value is ParticipantRole {
  return (
    typeof value === 'string' &&
    (PARTICIPANT_ROLES as readonly string[]).includes(value)
  )
}

/** Kind of asset bound to a session. */
export const ASSET_KINDS = ['scene', 'flow'] as const
export type SessionAssetKind = (typeof ASSET_KINDS)[number]

export function isSessionAssetKind(value: unknown): value is SessionAssetKind {
  return (
    typeof value === 'string' &&
    (ASSET_KINDS as readonly string[]).includes(value)
  )
}

export const SESSION_STATUSES = ['idle', 'live', 'closed'] as const
export type SessionStatus = (typeof SESSION_STATUSES)[number]

export interface SessionRecord {
  id: string
  name: string
  description: string
  hostUserId: string
  hostUserName: string | null
  /** Bound asset id (string form). null until the user drops a scene/flow on it. */
  assetId: string | null
  assetKind: SessionAssetKind | null
  assetName: string | null
  /** Role given to a user who joins without an explicit grant. */
  defaultRole: ParticipantRole
  status: SessionStatus
  createdAtMs: number
  updatedAtMs: number
  /** JSON blob for room-level options (e.g. allow guest joins, max participants). */
  settings: Record<string, unknown>
}

export interface SessionGrantRecord {
  sessionId: string
  userId: string
  userName: string | null
  role: ParticipantRole
}

/** Authenticated identity attached to every authorized HTTP/WS request. */
export interface AuthIdentity {
  userId: string
  userName: string | null
  email: string | null
  /** Original JWT, forwarded back to AlphaCollab when we need to act on the user's behalf. */
  rawToken: string
  /** Unix epoch seconds when the token expires (decoded from JWT `exp`). */
  expiresAtSec: number | null
}
