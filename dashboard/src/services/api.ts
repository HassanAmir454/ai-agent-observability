import type { SummaryData } from '../types.js'

export async function fetchSummary(timeRange: string): Promise<SummaryData> {
  const url = `/api/events/summary?timeRange=${encodeURIComponent(timeRange)}`
  const response = await fetch(url, { cache: 'no-store' })

  if (!response.ok) {
    throw new Error(`GET /api/events/summary returned HTTP ${response.status}`)
  }

  return response.json() as Promise<SummaryData>
}
