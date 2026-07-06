import { Schema, MapSchema, type } from '@colyseus/schema'

/**
 * Per-participant state synced over the wire. We start lean: identity + role +
 * a "presenter" camera slot. Future iterations can layer scene-edit deltas on top.
 */
export class ParticipantState extends Schema {
  @type('string') userId = ''
  @type('string') userName = ''
  @type('string') role = 'viewer'
  @type('boolean') connected = false
  /** Last-seen camera position (x, y, z); broadcast for camera-share / presenter. */
  @type('number') camX = 0
  @type('number') camY = 0
  @type('number') camZ = 0
  /** Camera target / lookAt point (x, y, z). */
  @type('number') tgtX = 0
  @type('number') tgtY = 0
  @type('number') tgtZ = 0
  /**
   * Navigation presentation: `orbit` | `walk` | `ar` | `vr` (free string valid
   * on the wire; clients normalize). Sent with `camera` messages.
   */
  @type('string') camNav = 'orbit'
  /** Pointer ray end (world), e.g. surface hit or projected point. */
  @type('number') curHitX = 0
  @type('number') curHitY = 0
  @type('number') curHitZ = 0
  @type('boolean') curHasHit = false
  /** Pointer is over the shared viewport (false when idle / outside canvas). */
  @type('boolean') curOn = false
  /** Voice chat: user has mic enabled (WebRTC experiment). */
  @type('boolean') voiceOn = false
  /** Voice chat: simple mic activity flag from the speaker's client (for presence UI). */
  @type('boolean') voiceTalking = false
}

export class SessionState extends Schema {
  @type('string') sessionId = ''
  @type('string') name = ''
  /** "scene" | "flow" | "" when nothing bound. */
  @type('string') assetKind = ''
  @type('string') assetId = ''
  @type('string') assetName = ''
  /** Identity of the current presenter (empty when free-cam for everyone). */
  @type('string') presenterUserId = ''
  @type({ map: ParticipantState }) participants = new MapSchema<ParticipantState>()
}
