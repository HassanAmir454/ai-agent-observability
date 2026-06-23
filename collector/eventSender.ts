import type { AgentEvent } from './types.js'
import type { CollectorConfig } from './config.js'

const COLLECTOR_VERSION = '1.0.0'

interface IngestResponse {
  stored?: number
  message?: string
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

  let response: Response
  try {
    response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Api-Key': config.apiKey,
      },
      body,
    })
  } catch (err) {
    console.error(`Network error posting to ${url}:`, err)
    return
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
