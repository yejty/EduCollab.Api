import { createHmac, timingSafeEqual } from 'node:crypto'
import { isParticipantRole, type ParticipantRole, type SessionAssetKind } from './types'

export interface JoinTicketClaims {
  sub: string
  sessionId: number
  workspaceId: number
  role: 'host' | 'participant' | 'guest'
  displayName: string
  assetKind: SessionAssetKind | ''
  assetId: string
  assetName: string
  sessionName: string
  includeAssets: boolean
  colyseusRole: ParticipantRole
  hostUserId: string
  exp: number
  iss?: string
  aud?: string
}

function base64UrlDecode(input: string): Buffer {
  const padded = input.replace(/-/g, '+').replace(/_/g, '/')
  const padLength = (4 - (padded.length % 4)) % 4
  return Buffer.from(padded + '='.repeat(padLength), 'base64')
}

function safeEqual(a: string, b: string): boolean {
  const aBuf = Buffer.from(a)
  const bBuf = Buffer.from(b)
  if (aBuf.length !== bBuf.length) return false
  return timingSafeEqual(aBuf, bBuf)
}

function readString(value: unknown): string | null {
  return typeof value === 'string' && value.trim().length > 0 ? value.trim() : null
}

function readNumber(value: unknown): number | null {
  if (typeof value === 'number' && Number.isFinite(value)) return value
  if (typeof value === 'string' && value.trim().length > 0) {
    const parsed = Number(value)
    return Number.isFinite(parsed) ? parsed : null
  }
  return null
}

function readBool(value: unknown): boolean {
  if (typeof value === 'boolean') return value
  if (typeof value === 'string') {
    const normalized = value.trim().toLowerCase()
    return normalized === 'true' || normalized === '1'
  }
  return false
}

/**
 * Verify an EduCollab session join ticket (HMAC-SHA256 JWT).
 * Returns null when the signature, expiry, or claims are invalid.
 */
export function verifyJoinTicket(
  token: string,
  secret: string,
  expectedIssuer: string,
  expectedAudience: string,
): JoinTicketClaims | null {
  const trimmed = token.trim()
  if (!trimmed || !secret) return null

  const segments = trimmed.split('.')
  if (segments.length !== 3) return null
  const [headerSegment, payloadSegment, signatureSegment] = segments
  if (!headerSegment || !payloadSegment || !signatureSegment) return null

  const signingInput = `${headerSegment}.${payloadSegment}`
  const expectedSignature = createHmac('sha256', secret)
    .update(signingInput)
    .digest('base64url')

  if (!safeEqual(signatureSegment, expectedSignature)) return null

  let payload: Record<string, unknown>
  try {
    payload = JSON.parse(base64UrlDecode(payloadSegment).toString('utf8')) as Record<
      string,
      unknown
    >
  } catch {
    return null
  }

  const exp = readNumber(payload.exp)
  if (exp == null || exp * 1000 < Date.now()) return null

  const iss = readString(payload.iss)
  if (iss && expectedIssuer && iss !== expectedIssuer) return null

  const aud = readString(payload.aud)
  if (aud && expectedAudience && aud !== expectedAudience) return null

  const sub = readString(payload.sub)
  const sessionId = readNumber(payload.sessionId)
  const workspaceId = readNumber(payload.workspaceId)
  const roleRaw = readString(payload.role)
  const displayName = readString(payload.displayName) ?? 'Participant'
  const colyseusRoleRaw = readString(payload.colyseusRole)
  const hostUserId = readString(payload.hostUserId) ?? ''

  if (!sub || sessionId == null || sessionId <= 0 || workspaceId == null || workspaceId <= 0) {
    return null
  }

  if (roleRaw !== 'host' && roleRaw !== 'participant' && roleRaw !== 'guest') {
    return null
  }

  if (!colyseusRoleRaw || !isParticipantRole(colyseusRoleRaw)) {
    return null
  }

  const assetKindRaw = readString(payload.assetKind) ?? ''
  const assetKind: SessionAssetKind | '' =
    assetKindRaw === 'scene' || assetKindRaw === 'flow' ? assetKindRaw : ''

  return {
    sub,
    sessionId,
    workspaceId,
    role: roleRaw,
    displayName,
    assetKind,
    assetId: readString(payload.assetId) ?? '',
    assetName: readString(payload.assetName) ?? '',
    sessionName: readString(payload.sessionName) ?? '',
    includeAssets: readBool(payload.includeAssets),
    colyseusRole: colyseusRoleRaw,
    hostUserId,
    exp,
    iss: iss ?? undefined,
    aud: aud ?? undefined,
  }
}
