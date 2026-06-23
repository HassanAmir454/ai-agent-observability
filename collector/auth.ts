import type { CollectorConfig } from './config.js'

interface TokenCache {
  token: string
  expiresAt: Date
}

let cache: TokenCache | null = null

function isCacheValid(): boolean {
  if (!cache) return false
  // Treat the token as expired 60 seconds before its actual expiry to avoid
  // race conditions between collection runs.
  return cache.expiresAt.getTime() - 60_000 > Date.now()
}

interface TokenResponse {
  token: string
  expiresAt: string
}

export async function getToken(config: CollectorConfig): Promise<string | null> {
  if (isCacheValid()) {
    return cache!.token
  }

  const url = `${config.ingestEndpoint}/api/auth/token`
  let response: Response
  try {
    response = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ apiKey: config.apiKey }),
    })
  } catch (err) {
    console.error(`Network error fetching token from ${url}:`, err)
    return null
  }

  if (!response.ok) {
    const text = await response.text().catch(() => '(no body)')
    console.error(`Failed to obtain token — HTTP ${response.status}: ${text}`)
    return null
  }

  let data: TokenResponse
  try {
    data = (await response.json()) as TokenResponse
  } catch {
    console.error('Token response was not valid JSON.')
    return null
  }

  cache = { token: data.token, expiresAt: new Date(data.expiresAt) }
  return cache.token
}

export function clearTokenCache(): void {
  cache = null
}
