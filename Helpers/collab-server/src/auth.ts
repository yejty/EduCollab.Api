import type { CollabServerConfig } from './config'
import type { AuthIdentity } from './types'

interface JwtPayloadShape {
  sub?: string | number
  uid?: string | number
  userId?: string | number
  id?: string | number
  /** AlphaCollab login JWT uses this for the login name. */
  user?: string | number
  username?: string | number
  email?: string
  name?: string
  given_name?: string
  preferred_username?: string
  exp?: number
  iat?: number
  [k: string]: unknown
}

/** Decode a JWT payload without verifying the signature. Returns null on shape error. */
export function decodeJwtPayload(token: string): JwtPayloadShape | null {
  const segments = token.split('.')
  if (segments.length < 2) return null
  const payloadSegment = segments[1]
  if (!payloadSegment) return null
  try {
    const padded = payloadSegment
      .replace(/-/g, '+')
      .replace(/_/g, '/')
      .padEnd(payloadSegment.length + ((4 - (payloadSegment.length % 4)) % 4), '=')
    const json = Buffer.from(padded, 'base64').toString('utf8')
    const parsed = JSON.parse(json) as unknown
    if (parsed && typeof parsed === 'object') {
      return parsed as JwtPayloadShape
    }
    return null
  } catch {
    return null
  }
}

function pickUserId(payload: JwtPayloadShape): string | null {
  const candidates = [
    payload.sub,
    payload.userId,
    payload.uid,
    payload.id,
    payload.user,
    payload.username,
  ]
  for (const c of candidates) {
    if (typeof c === 'string' && c.trim().length > 0) return c.trim()
    if (typeof c === 'number' && Number.isFinite(c)) return String(c)
  }
  return null
}

function pickUserName(payload: JwtPayloadShape): string | null {
  const candidates = [
    payload.username,
    payload.name,
    payload.preferred_username,
    payload.given_name,
    typeof payload.user === 'string' ? payload.user : undefined,
    payload.email,
  ]
  for (const c of candidates) {
    if (typeof c === 'string' && c.trim().length > 0) return c.trim()
  }
  return null
}

interface CacheEntry {
  identity: AuthIdentity
  expiresAtMs: number
}

const tokenCache = new Map<string, CacheEntry>()

function cacheKey(token: string): string {
  return token.length <= 32
    ? token
    : `${token.slice(0, 16)}…${token.slice(-16)}:${token.length}`
}

export function clearAuthCache(): void {
  tokenCache.clear()
}

/**
 * Validate the bearer token. When `config.validateViaApi` is true, calls the
 * AlphaCollab API to confirm the bearer is still accepted server-side.
 * Returns null when validation fails.
 */
export async function authenticateBearer(
  token: string,
  config: CollabServerConfig,
): Promise<AuthIdentity | null> {
  const trimmed = token.trim()
  if (trimmed.length === 0) return null

  const nowMs = Date.now()
  const cached = tokenCache.get(trimmed)
  if (cached && cached.expiresAtMs > nowMs) {
    return cached.identity
  }

  const payload = decodeJwtPayload(trimmed)
  if (!payload) return null

  // Reject expired tokens immediately.
  const exp = typeof payload.exp === 'number' ? payload.exp : null
  if (exp != null && exp * 1000 < nowMs) return null

  const userId = pickUserId(payload)
  if (!userId) return null

  let valid = true
  if (config.validateViaApi && config.alphaCollabApiBase) {
    valid = await remoteValidate(trimmed, config)
  }
  if (!valid) return null

  const identity: AuthIdentity = {
    userId,
    userName: pickUserName(payload),
    email: typeof payload.email === 'string' ? payload.email : null,
    rawToken: trimmed,
    expiresAtSec: exp,
  }

  const ttlMs = Math.max(1, config.authCacheSeconds) * 1000
  const cacheUntil = exp != null
    ? Math.min(nowMs + ttlMs, exp * 1000)
    : nowMs + ttlMs
  tokenCache.set(trimmed, { identity, expiresAtMs: cacheUntil })
  return identity
}

async function remoteValidate(
  token: string,
  config: CollabServerConfig,
): Promise<boolean> {
  if (!config.alphaCollabApiBase) return true
  const url = `${config.alphaCollabApiBase}${
    config.validatePath.startsWith('/') ? '' : '/'
  }${config.validatePath}`
  const ctrl = new AbortController()
  const timer = setTimeout(() => ctrl.abort(), 5000)
  try {
    const res = await fetch(url, {
      method: 'GET',
      headers: {
        Authorization: `Bearer ${token}`,
        Accept: 'application/json',
      },
      signal: ctrl.signal,
    })
    return res.ok
  } catch (err) {
    /**
     * When the AlphaCollab API is briefly unreachable, don't reject otherwise
     * valid JWTs — that was causing immediate WS disconnects (4002) on join
     * while REST session fetch still worked via cache/proxy.
     */
    console.warn(
      '[collab-auth] remote token validation unreachable; falling back to local JWT decode:',
      err instanceof Error ? err.message : String(err),
    )
    return true
  } finally {
    clearTimeout(timer)
  }
}

/**
 * Extract the bearer token from `Authorization: Bearer …` header,
 * or from a `?token=…` query string (used by Colyseus WS handshake).
 */
export function extractBearer(input: {
  headers?: { authorization?: string | null } | undefined
  queryToken?: string | null
}): string | null {
  const header = input.headers?.authorization
  if (typeof header === 'string') {
    const match = header.match(/^bearer\s+(.+)$/i)
    if (match && match[1]) return match[1].trim()
  }
  if (typeof input.queryToken === 'string' && input.queryToken.length > 0) {
    return input.queryToken.trim()
  }
  return null
}
