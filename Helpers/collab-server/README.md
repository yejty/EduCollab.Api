# EduCollab collab-server

Self-hosted [Colyseus](https://docs.colyseus.io/) realtime server for EduCollab
**live sessions**. Session ownership, sharing, and content live in EduCollab API;
this service is realtime-only (presence, sceneSync, voice).

---

## Architecture

```
Browser
  │  HTTPS  create session / join-ticket / bootstrap  →  EduCollab API
  │  WSS    joinOrCreate('collab', { sessionId, joinTicket })
  ▼
┌────────────────────────────────────────────┐
│  educollab-collab-server                   │
│   • Node.js 22 + @colyseus/core 0.17       │
│   • Join-ticket HMAC auth (no SQLite)      │
│   • room-started / room-ended webhooks     │
└────────────────────────────────────────────┘
```

---

## HTTP surface

| Method | Path        | Description        |
| ------ | ----------- | ------------------ |
| GET    | `/healthz`  | Liveness probe     |
| GET    | `/`         | Same as healthz    |
| GET    | `/colyseus` | Optional monitor   |

There is **no** session REST on this server. Use EduCollab:

- `POST /api/workspace/sessions`
- `POST /api/workspace/sessions/{id}/join-ticket`
- `GET /api/workspace/sessions/{id}/bootstrap`

---

## Client join

```ts
const session = await educollabApi.post('/api/workspace/sessions', {
  name: 'Physics lab',
  sceneId: 42,
  includeAssets: true,
})
const { joinTicket } = await educollabApi.post(
  `/api/workspace/sessions/${session.id}/join-ticket`,
)
await educollabApi.get(`/api/workspace/sessions/${session.id}/bootstrap`)

const client = new Client('ws://localhost:2567')
const room = await client.joinOrCreate('collab', {
  sessionId: String(session.id),
  joinTicket,
  wantsSceneSync: true,
})
```

---

## Guest join

1. Host creates a session with `allowGuestLink: true` → `guestLinkUrl` on the session.
2. Guest opens the link, enters a display name, then:

```ts
const { joinTicket, sessionId, colyseusEndpoint } = await educollabApi.post(
  '/api/public/sessions/join',
  { guestToken, displayName: 'Anna' },
)
const bootstrap = await educollabApi.get(
  `/api/public/sessions/bootstrap?guestToken=${encodeURIComponent(guestToken)}`,
)
const room = await client.joinOrCreate('collab', {
  sessionId: String(sessionId),
  joinTicket,
  wantsSceneSync: true,
})
```

Guest content downloads use `/api/public/session-scenes/content` and `/api/public/session-assets/content` with the same `guestToken`.

---

## Local development

Requires Node.js 20+.

```sh
cd Helpers/collab-server
cp .env.example .env
# Set EDUCOLLAB_API_BASE, SESSION_JOIN_SECRET, EDUCOLLAB_INTERNAL_API_KEY
# to match EduCollab appsettings SessionJoin + InternalApi
npm install
npm run dev
```

Smoke-test:

```sh
curl http://localhost:2567/healthz
# {"ok":true,"name":"educollab-collab-server"}
```

---

## Environment

| Variable | Required | Purpose |
| -------- | -------- | ------- |
| `EDUCOLLAB_API_BASE` | yes | API base for webhooks |
| `SESSION_JOIN_SECRET` | yes | Same as `SessionJoin:SecretKey` |
| `EDUCOLLAB_INTERNAL_API_KEY` | yes | Same as `InternalApi:ApiKey` |
| `SESSION_JOIN_ISSUER` | no | Default `EduCollab.Api` |
| `SESSION_JOIN_AUDIENCE` | no | Default `EduCollab.CollabServer` |
| `COLLAB_ALLOWED_ORIGINS` | no | CORS allowlist |
| `COLLAB_PORT` | no | Default `2567` |

---

## Docker

```sh
docker compose up --build
```

The server is **stateless** (no SQLite volume). Room state is in-memory only.
