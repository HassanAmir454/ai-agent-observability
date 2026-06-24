import { useState } from 'react'
import { useSummaryData } from './hooks/useSummaryData.js'
import { TotalEventsCard } from './components/TotalEventsCard.js'
import { TimeRangeSelector } from './components/TimeRangeSelector.js'
import type { TimeRange } from './components/TimeRangeSelector.js'
import { EventsByAgentChart } from './components/EventsByAgentChart.js'
import { EventTimelineChart } from './components/EventTimelineChart.js'
import { RecentEventsFeed } from './components/RecentEventsFeed.js'
import './app.css'

export default function App() {
  const [timeRange, setTimeRange] = useState<TimeRange>('24h')
  const { data, loading, error, lastUpdated } = useSummaryData(timeRange)

  const isEmpty = !loading && data !== null && data.totalEvents === 0
  const firstLoad = loading && data === null

  return (
    <div className="app-shell">
      <div className="app-inner">

        {/* ── Header ─────────────────────────────────────────────── */}
        <header className="app-header">
          <h1 className="app-title">AI Agent Observability</h1>
          <div className="app-header-right">
            <TimeRangeSelector selected={timeRange} onChange={setTimeRange} />
            {lastUpdated && (
              <span className="last-updated">
                Updated {lastUpdated.toLocaleTimeString()}
              </span>
            )}
          </div>
        </header>

        {/* ── Error banner (non-blocking, C24) ───────────────────── */}
        {error && (
          <div className="error-banner" role="alert">
            ⚠ API unavailable: {error}
            {data !== null && ' — showing last known data.'}
          </div>
        )}

        {/* ── First-load skeleton ─────────────────────────────────── */}
        {firstLoad && (
          <TotalEventsCard totalEvents={0} totalCostUsd={0} totalTokens={0} loading />
        )}

        {/* ── Empty state (C25) ───────────────────────────────────── */}
        {isEmpty && (
          <div className="page-empty-state">
            <p className="page-empty-heading">No data yet</p>
            <p className="page-empty-hint">
              Run <code>npm run collect:once</code> to collect your first events, then refresh.
            </p>
          </div>
        )}

        {/* ── Data layout ─────────────────────────────────────────── */}
        {data !== null && data.totalEvents > 0 && (
          <>
            <TotalEventsCard
              totalEvents={data.totalEvents}
              totalCostUsd={data.totalCostUsd}
              totalTokens={data.totalTokens}
              loading={false}
            />

            <div className="chart-grid">
              <EventsByAgentChart data={data.byAgent} />
              <EventTimelineChart data={data.timeline} />
            </div>

            <RecentEventsFeed events={data.recentEvents} />
          </>
        )}

      </div>
    </div>
  )
}

