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

/** Identity derived from an EduCollab join ticket. */
export interface JoinIdentity {
  principal: string
  displayName: string
  isGuest: boolean
}
