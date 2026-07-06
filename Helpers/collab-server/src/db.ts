import { mkdirSync } from 'node:fs'
import { dirname } from 'node:path'
import Database from 'better-sqlite3'
import type { Database as Db } from 'better-sqlite3'

let database: Db | null = null

export function openDatabase(filePath: string): Db {
  if (database) return database
  mkdirSync(dirname(filePath), { recursive: true })
  const db = new Database(filePath)
  db.pragma('journal_mode = WAL')
  db.pragma('synchronous = NORMAL')
  db.pragma('foreign_keys = ON')
  applyMigrations(db)
  database = db
  return db
}

export function getDatabase(): Db {
  if (!database) {
    throw new Error('Database not initialized. Call openDatabase() first.')
  }
  return database
}

function applyMigrations(db: Db): void {
  db.exec(`
    CREATE TABLE IF NOT EXISTS sessions (
      id              TEXT PRIMARY KEY,
      name            TEXT NOT NULL,
      description     TEXT NOT NULL DEFAULT '',
      host_user_id    TEXT NOT NULL,
      host_user_name  TEXT,
      asset_id        TEXT,
      asset_kind      TEXT CHECK (asset_kind IN ('scene','flow')),
      asset_name      TEXT,
      default_role    TEXT NOT NULL DEFAULT 'viewer'
                       CHECK (default_role IN ('host','editor','presenter','viewer')),
      status          TEXT NOT NULL DEFAULT 'idle'
                       CHECK (status IN ('idle','live','closed')),
      created_at_ms   INTEGER NOT NULL,
      updated_at_ms   INTEGER NOT NULL,
      settings_json   TEXT NOT NULL DEFAULT '{}'
    );

    CREATE INDEX IF NOT EXISTS sessions_host_idx
      ON sessions(host_user_id);

    CREATE TABLE IF NOT EXISTS session_grants (
      session_id   TEXT NOT NULL,
      user_id      TEXT NOT NULL,
      user_name    TEXT,
      role         TEXT NOT NULL
                   CHECK (role IN ('host','editor','presenter','viewer')),
      PRIMARY KEY (session_id, user_id),
      FOREIGN KEY (session_id) REFERENCES sessions(id) ON DELETE CASCADE
    );

    CREATE INDEX IF NOT EXISTS session_grants_user_idx
      ON session_grants(user_id);
  `)
}
