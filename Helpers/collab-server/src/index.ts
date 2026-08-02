// Load `.env` for native dev (Docker injects env vars via compose, so the file
// is missing in production — `dotenv` silently no-ops in that case).
import 'dotenv/config'

import { createServer } from 'node:http'
import path from 'node:path'
import express from 'express'
import cors from 'cors'
import { Server, matchMaker } from '@colyseus/core'
import { WebSocketTransport } from '@colyseus/ws-transport'
import { monitor } from '@colyseus/monitor'
import { createEduCollabClient } from './api/educollabClient'
import { loadConfig } from './config'
import { CollabRoom } from './rooms/CollabRoom'

async function main(): Promise<void> {
  const config = loadConfig()
  const educollabClient = createEduCollabClient(config)

  const app = express()
  app.disable('x-powered-by')
  app.use(express.json({ limit: '1mb' }))

  // Deny unknown origins with `callback(null, false)` — never throw.
  // Throwing makes Express return 500 HTML, which breaks the Colyseus monitor
  // (module scripts send Origin: http://localhost:2567 for /colyseus assets).
  const corsConfig = (() => {
    if (config.allowedOrigins.includes('*')) {
      return cors({ origin: true, credentials: false })
    }
    return cors({
      origin: (origin, callback) => {
        if (!origin) return callback(null, true)
        if (config.allowedOrigins.includes(origin)) {
          return callback(null, true)
        }
        callback(null, false)
      },
      credentials: false,
    })
  })()
  app.use(corsConfig)

  app.get('/healthz', (_req, res) => {
    res.json({
      name: 'educollab-collab-server',
      ok: true,
      buildTime: new Date().toISOString(),
    })
  })

  app.get('/', (_req, res) => {
    res.json({
      name: 'educollab-collab-server',
      ok: true,
      buildTime: new Date().toISOString(),
    })
  })

  // Local visual smoke page (join ticket → Colyseus room)
  const smokeTestPath = path.join(__dirname, '..', 'smoke-test.html')
  app.get('/smoke-test', (_req, res) => {
    res.sendFile(smokeTestPath)
  })
  app.get('/smoke-test.html', (_req, res) => {
    res.sendFile(smokeTestPath)
  })

  // Optional Colyseus monitor at /colyseus (HTTP basic auth)
  if (config.monitorUser && config.monitorPassword) {
    const expected = `Basic ${Buffer.from(
      `${config.monitorUser}:${config.monitorPassword}`,
    ).toString('base64')}`
    app.use('/colyseus', (req, res, next) => {
      const provided = req.headers.authorization ?? ''
      if (provided === expected) {
        next()
        return
      }
      res
        .status(401)
        .set('WWW-Authenticate', 'Basic realm="collab-monitor"')
        .send('Authentication required')
    })
    app.use('/colyseus', monitor())
  }

  const httpServer = createServer(app)
  CollabRoom.configure({ config, educollabClient })

  // `ws` defaults `maxPayload` to 100 MB. Any inbound WebSocket frame larger
  // than this triggers an unprefixed "Max payload size exceeded" error and
  // closes the connection with code 1009 ("message too big"). 4 MB is well
  // above what the editor needs (sceneSync is hard-capped at 1 MB on the
  // client) but small enough that a runaway client cannot DOS the room.
  const WS_MAX_PAYLOAD = 4 * 1024 * 1024
  const gameServer = new Server({
    transport: new WebSocketTransport({
      server: httpServer,
      maxPayload: WS_MAX_PAYLOAD,
    }),
  })

  process.on('uncaughtException', (err) => {
    console.error('[collab-server] uncaughtException:', err)
  })
  process.on('unhandledRejection', (reason) => {
    console.error('[collab-server] unhandledRejection:', reason)
  })

  // The room name is "collab"; clients pass {sessionId} so each session gets its own room.
  gameServer.define('collab', CollabRoom).filterBy(['sessionId'])

  await gameServer.listen(config.port, config.bindAddr)
  console.log(
    `[collab-server] listening on ${config.bindAddr}:${config.port} (CORS: ${config.allowedOrigins.join(', ') || '(none)'})`,
  )

  const shutdown = async (signal: string) => {
    console.log(`[collab-server] received ${signal}, shutting down`)
    try {
      await matchMaker.gracefullyShutdown()
    } catch (err) {
      console.error('[collab-server] error during shutdown', err)
    }
    httpServer.close(() => process.exit(0))
    setTimeout(() => process.exit(0), 5000).unref()
  }

  process.on('SIGTERM', () => void shutdown('SIGTERM'))
  process.on('SIGINT', () => void shutdown('SIGINT'))
}

main().catch((err) => {
  console.error('[collab-server] fatal startup error', err)
  process.exit(1)
})
