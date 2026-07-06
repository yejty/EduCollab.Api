function readNumber(name: string, fallback: number): number {
  const raw = process.env[name]
  if (raw == null || raw === '') return fallback
  const parsed = Number(raw)
  return Number.isFinite(parsed) ? parsed : fallback
}

function readBool(name: string, fallback: boolean): boolean {
  const raw = process.env[name]
  if (raw == null || raw === '') return fallback
  const trimmed = raw.trim().toLowerCase()
  return ['1', 'true', 'yes', 'on'].includes(trimmed)
}

function readList(name: string, fallback: string[]): string[] {
  const raw = process.env[name]
  if (raw == null) return fallback
  return raw
    .split(',')
    .map((s) => s.trim())
    .filter((s) => s.length > 0)
}

export interface CollabServerConfig {
  port: number
  bindAddr: string
  allowedOrigins: string[]
  alphaCollabApiBase: string | null
  validatePath: string
  validateViaApi: boolean
  authCacheSeconds: number
  dbPath: string
  monitorUser: string
  monitorPassword: string
  logLevel: 'debug' | 'info' | 'warn' | 'error'
}

export function loadConfig(): CollabServerConfig {
  const apiBase =
    (process.env.COLLAB_ALPHACOLLAB_API_BASE ?? '').trim().replace(/\/+$/, '') ||
    null

  const logLevelRaw = (process.env.COLLAB_LOG_LEVEL ?? 'info').toLowerCase()
  const logLevel: CollabServerConfig['logLevel'] =
    logLevelRaw === 'debug' ||
    logLevelRaw === 'info' ||
    logLevelRaw === 'warn' ||
    logLevelRaw === 'error'
      ? logLevelRaw
      : 'info'

  return {
    port: readNumber('COLLAB_PORT', 2567),
    bindAddr: process.env.COLLAB_BIND_ADDR?.trim() || '0.0.0.0',
    allowedOrigins: readList('COLLAB_ALLOWED_ORIGINS', [
      'https://alphacollab-admin.vercel.app',
      'http://localhost:5173',
    ]),
    alphaCollabApiBase: apiBase,
    validatePath: (process.env.COLLAB_VALIDATE_PATH ?? '/me').trim() || '/me',
    validateViaApi: readBool('COLLAB_VALIDATE_VIA_API', false),
    authCacheSeconds: readNumber('COLLAB_AUTH_CACHE_SECONDS', 60),
    dbPath:
      process.env.COLLAB_DB_PATH?.trim() || '/var/collab-data/collab.sqlite',
    monitorUser: process.env.COLLAB_MONITOR_USER?.trim() || '',
    monitorPassword: process.env.COLLAB_MONITOR_PASSWORD ?? '',
    logLevel,
  }
}
