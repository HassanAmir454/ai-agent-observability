import type { AgentEvent } from './types.js'
import type { CollectorConfig } from './config.js'
import { getToken, clearTokenCache } from './auth.js'

const COLLECTOR_VERSION = '1.0.0'

interface IngestResponse {
  stored?: number
  message?: string
}

async function postEvents(
  url: string,
  body: string,
  token: string,
): Promise<Response | null> {
  try {
    return await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
      },
      body,
    })
  } catch (err) {
    console.error(`Network error posting to ${url}:`, err)
    return null
  }
}

export async function sendEvents(
  events: AgentEvent[],
  config: CollectorConfig,
): Promise<void> {
  if (events.length === 0) {
    console.log('no events to send')
    return
  }

  const url = `${config.ingestEndpoint}/api/events`
  const body = JSON.stringify({ events, collectorVersion: COLLECTOR_VERSION })

  const token = await getToken(config)
  if (!token) {
    console.error('Could not obtain auth token; skipping send.')
    return
  }

  let response = await postEvents(url, body, token)
  if (!response) return

  // On 401 the token may have been revoked or rotated: clear cache, fetch once,
  // and retry. Never loop further — log and return on second failure.
  if (response.status === 401) {
    console.warn('Received 401; refreshing token and retrying once.')
    clearTokenCache()
    const freshToken = await getToken(config)
    if (!freshToken) {
      console.error('Token refresh failed; giving up on this batch.')
      return
    }
    response = await postEvents(url, body, freshToken)
    if (!response) return
  }

  if (!response.ok) {
    const text = await response.text().catch(() => '(no body)')
    console.error(
      `POST ${url} failed — HTTP ${response.status} ${response.statusText}: ${text}`,
    )
    return
  }

  try {
    const data = (await response.json()) as IngestResponse
    console.log(`Stored ${data.stored ?? '?'} event(s). ${data.message ?? ''}`.trim())
  } catch {
    console.log(`POST ${url} succeeded (HTTP ${response.status}), but response was not JSON.`)
  }
}
