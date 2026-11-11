# Space collab-server

Self-hosted [Colyseus](https://docs.colyseus.io/) realtime server for Space
**live sessions**. Session ownership, sharing, and content live in Space API;
this service is realtime-only (presence, sceneSync, voice).

---

## Architecture

```
Browser
  │  HTTPS  create session / join-ticket / bootstrap  →  Space API
  │  WSS    joinOrCreate('collab', { sessionId, joinTicket })
  ▼
┌────────────────────────────────────────────┐
│  space-collab-server                   │
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

There is **no** session REST on this server. Use Space:

- `POST /api/workspace/sessions`
- `POST /api/workspace/sessions/{id}/join-ticket`
- `GET /api/workspace/sessions/{id}/bootstrap`

---

## Client join

```ts
const session = await spaceApi.post('/api/workspace/sessions', {
  name: 'Physics lab',
  sceneId: 42,
  includeAssets: true,
})
const { joinTicket } = await spaceApi.post(
  `/api/workspace/sessions/${session.id}/join-ticket`,
)
await spaceApi.get(`/api/workspace/sessions/${session.id}/bootstrap`)

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
const { joinTicket, sessionId, colyseusEndpoint } = await spaceApi.post(
  '/api/public/sessions/join',
  { guestToken, displayName: 'Anna' },
)
const bootstrap = await spaceApi.get(
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
# Set SPACE_API_BASE, SESSION_JOIN_SECRET, SPACE_INTERNAL_API_KEY
# to match Space appsettings SessionJoin + InternalApi
npm install
npm run dev
```

Smoke-test:

```sh
curl http://localhost:2567/healthz
# {"ok":true,"name":"space-collab-server"}
```

---

## Environment

| Variable | Required | Purpose |
| -------- | -------- | ------- |
| `SPACE_API_BASE` | yes | API base for webhooks |
| `SESSION_JOIN_SECRET` | yes | Same as `SessionJoin:SecretKey` |
| `SPACE_INTERNAL_API_KEY` | yes | Same as `InternalApi:ApiKey` |
| `SESSION_JOIN_ISSUER` | no | Default `Hub.Api` |
| `SESSION_JOIN_AUDIENCE` | no | Default `Space.CollabServer` |
| `COLLAB_ALLOWED_ORIGINS` | no | CORS allowlist |
| `COLLAB_PORT` | no | Default `2567` |

---

## Docker

### Local development

Joins the existing Space API Docker network (`space-collab-server` on port `2567`):

```sh
docker compose up --build
```

### Full split deploy (API host + collab host)

Hetzner files use the `*.hetzner.*` naming. Root `docker-compose.hetzner.yml` runs **postgres + api only**. Collab is this folder’s Hetzner compose.

**1. API host** (repo root):

```sh
cp .env.hetzner.example .env.hetzner
# Edit: Jwt__SecretKey, SessionJoin__*, InternalApi__ApiKey,
#       SessionJoin__ColyseusEndpoint=wss://<collab-host-or-proxy>
docker compose -f docker-compose.hetzner.yml --env-file .env.hetzner up -d --build
```

**2. Collab host** (this directory):

```sh
cp .env.hetzner.example .env.hetzner
# Edit: SPACE_API_BASE, COLLAB_ALLOWED_ORIGINS,
#       SESSION_JOIN_SECRET + SPACE_INTERNAL_API_KEY (same as API .env.hetzner)
docker compose -p space-collab-v2 -f docker-compose.hetzner.yml --env-file .env.hetzner up -d --build
```

Publishes `space-collab-server-v2` on host port `2568` by default (does not replace an existing `:2567` collab). Prefer a reverse proxy so clients use `wss://collab…` without the port.

Stop only the v2 instance:

```sh
docker compose -p space-collab-v2 -f docker-compose.hetzner.yml down
```

The server is **stateless** (no SQLite volume). Room state is in-memory only.
