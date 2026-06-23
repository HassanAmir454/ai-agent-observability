import { useEffect, useRef, useState } from 'react'
import type { SummaryData } from '../types.js'
import { fetchSummary } from '../services/api.js'

const POLL_INTERVAL_MS = 60_000

interface UseSummaryDataResult {
  data: SummaryData | null
  loading: boolean
  error: string | null
  lastUpdated: Date | null
}

export function useSummaryData(timeRange: string): UseSummaryDataResult {
  const [data, setData] = useState<SummaryData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null)

  // Tracks whether the effect that set up the current interval is still alive.
  const cancelledRef = useRef(false)

  useEffect(() => {
    cancelledRef.current = false
    setLoading(true)

    const run = async (): Promise<void> => {
      try {
        const result = await fetchSummary(timeRange)
        if (cancelledRef.current) return
        setData(result)
        setError(null)
        setLastUpdated(new Date())
      } catch (err) {
        if (cancelledRef.current) return
        setError(err instanceof Error ? err.message : 'Failed to fetch summary')
      } finally {
        if (!cancelledRef.current) setLoading(false)
      }
    }

    void run()
    const timer = setInterval(() => { void run() }, POLL_INTERVAL_MS)

    return () => {
      cancelledRef.current = true
      clearInterval(timer)
    }
  }, [timeRange])

  return { data, loading, error, lastUpdated }
}
