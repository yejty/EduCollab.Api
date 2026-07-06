// Load `.env` for native dev (Docker injects env vars via compose, so the file
// is missing in production — `dotenv` silently no-ops in that case).
import 'dotenv/config'

import { createServer } from 'node:http'
import express from 'express'
import cors from 'cors'
import { Server, matchMaker } from '@colyseus/core'
import { WebSocketTransport } from '@colyseus/ws-transport'
import { monitor } from '@colyseus/monitor'
import { loadConfig } from './config'
import { openDatabase } from './db'
import { createRestRouter } from './rest'
import { CollabRoom } from './rooms/CollabRoom'

async function main(): Promise<void> {
  const config = loadConfig()
  openDatabase(config.dbPath)

  const app = express()
  app.disable('x-powered-by')
  app.use(express.json({ limit: '1mb' }))

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
        callback(new Error(`Origin ${origin} not allowed`))
      },
      credentials: false,
    })
  })()
  app.use(corsConfig)

  app.get('/', (_req, res) => {
    res.json({
      name: 'alphacollab-collab-server',
      ok: true,
      buildTime: new Date().toISOString(),
    })
  })

  const restRouter = createRestRouter(config)
  app.use(restRouter)

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
  CollabRoom.configure({ config })

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

  // Surface unhandled errors with a prefix so we don't see bare
  // `Max payload size exceeded` lines without context.
  process.on('uncaughtException', (err) => {
    console.error('[collab-server] uncaughtException:', err)
  })
  process.on('unhandledRejection', (reason) => {
    console.error('[collab-server] unhandledRejection:', reason)
  })

  // The room name is "collab"; clients pass {sessionId} so each session gets its own room.
  // We use filterBy + matchmaker.joinOrCreate from the REST layer to ensure unique rooms per session.
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
