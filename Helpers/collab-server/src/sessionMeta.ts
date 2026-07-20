import type { JoinTicketClaims } from './joinTicket'
import type { SessionAssetKind } from './types'

export interface RoomSessionMeta {
  sessionId: number
  name: string
  hostUserId: string
  assetKind: SessionAssetKind | null
  assetId: string | null
  assetName: string | null
}

export function claimsToSessionMeta(claims: JoinTicketClaims): RoomSessionMeta {
  return {
    sessionId: claims.sessionId,
    name: claims.sessionName || `Session ${claims.sessionId}`,
    hostUserId: claims.hostUserId
      ? `user:${claims.hostUserId}`
      : claims.role === 'host'
        ? claims.sub
        : '',
    assetKind: claims.assetKind || null,
    assetId: claims.assetId || null,
    assetName: claims.assetName || null,
  }
}
