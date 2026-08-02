function readNumber(name: string, fallback: number): number {
  const raw = process.env[name]
  if (raw == null || raw === '') return fallback
  const parsed = Number(raw)
  return Number.isFinite(parsed) ? parsed : fallback
}

function readList(name: string, fallback: string[]): string[] {
  const raw = process.env[name]
  if (raw == null) return fallback
  return raw
    .split(',')
    .map((s) => s.trim())
    .filter((s) => s.length > 0)
}

function requireEnv(name: string): string {
  const value = process.env[name]?.trim() ?? ''
  if (!value) {
    throw new Error(`Missing required environment variable: ${name}`)
  }
  return value
}

export interface CollabServerConfig {
  port: number
  bindAddr: string
  allowedOrigins: string[]
  educollabApiBase: string
  sessionJoinSecret: string
  sessionJoinIssuer: string
  sessionJoinAudience: string
  internalApiKey: string
  monitorUser: string
  monitorPassword: string
  logLevel: 'debug' | 'info' | 'warn' | 'error'
}

export function loadConfig(): CollabServerConfig {
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
      'http://localhost:5173',
      'http://localhost:3000',
      // Same-origin monitor UI at /colyseus (module scripts send this Origin)
      'http://localhost:2567',
    ]),
    educollabApiBase: requireEnv('EDUCOLLAB_API_BASE').replace(/\/+$/, ''),
    sessionJoinSecret: requireEnv('SESSION_JOIN_SECRET'),
    sessionJoinIssuer:
      process.env.SESSION_JOIN_ISSUER?.trim() || 'EduCollab.Api',
    sessionJoinAudience:
      process.env.SESSION_JOIN_AUDIENCE?.trim() || 'EduCollab.CollabServer',
    internalApiKey: requireEnv('EDUCOLLAB_INTERNAL_API_KEY'),
    monitorUser: process.env.COLLAB_MONITOR_USER?.trim() || '',
    monitorPassword: process.env.COLLAB_MONITOR_PASSWORD ?? '',
    logLevel,
  }
}
