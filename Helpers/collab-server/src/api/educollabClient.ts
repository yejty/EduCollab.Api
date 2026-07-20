import type { CollabServerConfig } from '../config'

export interface EduCollabClient {
  notifyRoomStarted(sessionId: number): Promise<void>
  notifyRoomEnded(sessionId: number): Promise<void>
}

async function postWebhook(
  config: CollabServerConfig,
  sessionId: number,
  action: 'room-started' | 'room-ended',
): Promise<void> {
  const url = `${config.educollabApiBase}/api/internal/sessions/${sessionId}/${action}`
  const ctrl = new AbortController()
  const timer = setTimeout(() => ctrl.abort(), 5000)
  try {
    const res = await fetch(url, {
      method: 'POST',
      headers: {
        'X-Api-Key': config.internalApiKey,
        Accept: 'application/json',
      },
      signal: ctrl.signal,
    })
    if (!res.ok) {
      console.warn(
        `[educollab-client] ${action} webhook failed for session ${sessionId}: HTTP ${res.status}`,
      )
    }
  } catch (err) {
    console.warn(
      `[educollab-client] ${action} webhook unreachable for session ${sessionId}:`,
      err instanceof Error ? err.message : String(err),
    )
  } finally {
    clearTimeout(timer)
  }
}

export function createEduCollabClient(config: CollabServerConfig): EduCollabClient {
  return {
    notifyRoomStarted(sessionId: number): Promise<void> {
      return postWebhook(config, sessionId, 'room-started')
    },
    notifyRoomEnded(sessionId: number): Promise<void> {
      return postWebhook(config, sessionId, 'room-ended')
    },
  }
}
