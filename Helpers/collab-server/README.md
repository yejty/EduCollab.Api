# AlphaCollab collab-server

A self-hosted [Colyseus](https://docs.colyseus.io/) realtime server that powers
the **Sessions** tab of the AlphaCollab admin. Each session corresponds to a
single Colyseus room hosting a scene or flow, with role-based participant
permissions (`host` / `editor` / `presenter` / `viewer`).

This server is intended to run **on the same Hetzner VM as `cad-occt-service`**,
behind Caddy at `https://collab.skyform.space`.

It does **not** replace the AlphaCollab API — sessions live in this service's
own SQLite database; the bound scene/flow content is loaded by clients directly
from the AlphaCollab API as usual.

---

## Architecture

```
Browser (admin / participants)
    │
    │  HTTPS REST  (admin: list / create / kill rooms)
    │  WSS         (participants: real-time room state)
    ▼
┌────────────────────────────────────────────┐
│  collab-server                             │
│   • Node.js 22 + @colyseus/core 0.17       │
│   • SQLite (better-sqlite3) persistence    │
│   • Bearer-token auth (AlphaCollab JWT)    │
│   • One Colyseus room per session          │
└────────────────────────────────────────────┘
        │   loads scene/flow JSON (read-only) on the client
        ▼
   AlphaCollab API
```

---

## REST endpoints

All endpoints (except `/healthz`) require an `Authorization: Bearer <token>`
header. The token is the same JWT issued by the AlphaCollab API on login.

| Method | Path                                  | Description                                   |
| ------ | ------------------------------------- | --------------------------------------------- |
| GET    | `/healthz`                            | liveness probe                                |
| GET    | `/sessions`                           | list sessions visible to the caller           |
| POST   | `/sessions`                           | create a new session (caller becomes host)    |
| GET    | `/sessions/:id`                       | session detail + grants + caller's role       |
| PATCH  | `/sessions/:id`                       | update a session (host only)                  |
| DELETE | `/sessions/:id`                       | delete a session (host only)                  |
| POST   | `/sessions/:id/grants`                | upsert `{ userId, userName?, role }` (host)   |
| DELETE | `/sessions/:id/grants/:userId`        | revoke a user's grant (host)                  |

Participants connect to the Colyseus room with:

```ts
// Browser SDK (colyseus.js):
const client = new Client('wss://collab.skyform.space')
const room = await client.joinOrCreate('collab', {
  sessionId,
  token: bearerToken, // optional fallback if Authorization header is unavailable
})
```

Each room exposes the schema in `src/rooms/schema.ts`: `sessionId`, `assetKind`,
`assetId`, `assetName`, `presenterUserId`, and a `participants` map keyed by the
Colyseus `client.sessionId`.

---

## Local development

Requires Node.js 20+ (we test on 22).

```sh
cd collab-server
cp .env.example .env       # then edit values for your dev machine
npm install
npm run dev                # tsx watch — restarts on changes
```

`npm run dev` listens on `http://localhost:2567`. Smoke-test it:

```sh
curl http://localhost:2567/healthz
# {"ok":true,"name":"alphacollab-collab-server"}

curl -H "Authorization: Bearer <admin-jwt>" http://localhost:2567/sessions
# {"sessions":[]}
```

In dev `COLLAB_VALIDATE_VIA_API=0` is fine — the bearer is decoded locally and
treated as authenticated as long as the JWT `exp` is in the future. **Always**
turn this on (`=1`) in production.

---

## Run with Docker locally (matches Hetzner)

```sh
cd collab-server
docker compose up --build
```

This:

- builds the Node image (multi-stage, ~80 MB),
- exposes `:2567` on the host,
- mounts a `collab-data` volume at `/var/collab-data` for the SQLite database.

---

## Deploy to Hetzner — step by step

This assumes the same VM that already runs `cad-occt-service` and Caddy.

### 1. Add a DNS A-record

In your domain registrar for `skyform.space`, add:

| Type | Host    | Value          | TTL  |
| ---- | ------- | -------------- | ---- |
| A    | collab  | <your VM IPv4> | auto |

Wait a minute, then `dig +short collab.skyform.space` on your laptop should
return the VM's IP.

### 2. Copy the folder to the server

From your laptop, in the project root:

```sh
rsync -avz --delete \
  --exclude node_modules --exclude dist --exclude data --exclude .git \
  collab-server/ \
  <user>@<vm-ip>:/srv/collab-server/
```

(Substitute your usual SSH user, e.g. `root`. The folder doesn't have to be in
`/srv/` — just keep it next to `cad-occt-service` for sanity.)

### 3. Set production env on the VM

SSH in:

```sh
ssh <user>@<vm-ip>
cd /srv/collab-server
cp .env.example .env
```

Open `.env` and set at minimum:

```sh
COLLAB_ALLOWED_ORIGINS=https://alphacollab-admin.vercel.app
COLLAB_ALPHACOLLAB_API_BASE=https://assetstore.alphacollab.online
COLLAB_VALIDATE_VIA_API=1
COLLAB_MONITOR_USER=admin
COLLAB_MONITOR_PASSWORD=<long random string>
```

### 4. Build & start the container

Still on the VM:

```sh
docker compose up -d --build
docker compose logs -f
```

You should see:

```
[collab-server] listening on 0.0.0.0:2567 (CORS: https://alphacollab-admin.vercel.app)
```

Verify locally on the VM:

```sh
curl http://127.0.0.1:2567/healthz
```

### 5. Add the Caddy reverse-proxy block

Your existing Caddy config (`/etc/caddy/Caddyfile`) presumably already has
the converter block (`converter.skyform.space`). Add a new site:

```caddy
collab.skyform.space {
  encode zstd gzip
  reverse_proxy 127.0.0.1:2567
}
```

Caddy automatically obtains a Let's Encrypt cert for the new hostname.

Reload Caddy:

```sh
sudo systemctl reload caddy
```

Test from your laptop:

```sh
curl https://collab.skyform.space/healthz
# {"ok":true,"name":"alphacollab-collab-server"}
```

### 6. Tell the admin where to find it

In the admin Vercel project, set the env var:

```
VITE_COLLAB_BASE_URL=/__alphacollab_collab
```

And in `vercel.json` we ship a rewrite:

```json
{
  "source": "/__alphacollab_collab/:path*",
  "destination": "https://collab.skyform.space/:path*"
}
```

so the browser calls the admin's own origin (avoids CORS), and Vercel proxies
on to the collab server. Same pattern as `__alphacollab_api` and
`__alphacollab_direct`.

For local dev, point Vite at the same dev server via `VITE_COLLAB_PROXY_TARGET`
in `alphacollab-admin/.env` (defaults to `http://127.0.0.1:2567`).

---

## Updating after code changes

```sh
ssh <user>@<vm-ip>
cd /srv/collab-server
# pull or rsync the new code, then:
docker compose up -d --build
```

---

## Optional: Colyseus monitor

If `COLLAB_MONITOR_USER` and `COLLAB_MONITOR_PASSWORD` are set, an admin
dashboard is exposed at `https://collab.skyform.space/colyseus` (HTTP basic
auth). Useful for inspecting active rooms during a session.

---

## Why not Colyseus Cloud?

Colyseus Cloud is a managed service. We chose a self-hosted setup because:

- our auth is the AlphaCollab JWT (no Cloud SSO integration needed),
- traffic stays on the same VM as `cad-occt-service`,
- pricing is predictable (a single $5/month VM serves both services),
- session persistence (SQLite) is local and easy to back up.

If we ever need horizontal scale (sessions across multiple VMs), the Colyseus
docs explain how to swap the local presence/driver for Redis with no code
changes — the architecture here is compatible.
