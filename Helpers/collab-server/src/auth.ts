/**
 * Extract a bearer token from `Authorization: Bearer …` or a query/options token.
 */
export function extractBearer(input: {
  headers?: { authorization?: string | null } | undefined
  queryToken?: string | null
}): string | null {
  const header = input.headers?.authorization
  if (typeof header === 'string') {
    const match = header.match(/^bearer\s+(.+)$/i)
    if (match && match[1]) return match[1].trim()
  }
  if (typeof input.queryToken === 'string' && input.queryToken.length > 0) {
    return input.queryToken.trim()
  }
  return null
}
