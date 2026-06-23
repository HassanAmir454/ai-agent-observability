import type { RecentEvent } from '../types.js'
import './components.css'

const AGENT_BADGE: Record<string, { bg: string; color: string }> = {
  Cursor:           { bg: 'rgba(59,130,246,0.15)',  color: '#3B82F6' },
  'Claude Code':    { bg: 'rgba(139,92,246,0.15)',  color: '#8B5CF6' },
  'GitHub Copilot': { bg: 'rgba(16,185,129,0.15)',  color: '#10B981' },
}

function badgeStyle(agent: string): React.CSSProperties {
  const s = AGENT_BADGE[agent] ?? { bg: 'rgba(123,127,158,0.15)', color: '#7B7F9E' }
  return { backgroundColor: s.bg, color: s.color }
}

function formatTime(iso: string): string {
  const d = new Date(iso)
  return d.toLocaleString(undefined, {
    month: 'short', day: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })
}

function totalTokens(e: RecentEvent): number {
  return e.inputTokens + e.outputTokens + e.cacheReadTokens + e.cacheWriteTokens + e.reasoningTokens
}

interface Props {
  events: RecentEvent[]
}

export function RecentEventsFeed({ events }: Props) {
  return (
    <div className="card">
      <p className="chart-title">Recent Events</p>
      {events.length === 0 ? (
        <div className="empty-state">
          <strong>No data yet</strong>
          <span>Run <code>npm run collect:once</code> to see recent events.</span>
        </div>
      ) : (
        <div style={{ overflowX: 'auto' }}>
          <table className="events-table">
            <thead>
              <tr>
                <th>Time</th>
                <th>Agent</th>
                <th>Model</th>
                <th>Activity</th>
                <th style={{ textAlign: 'right' }}>Tokens</th>
                <th style={{ textAlign: 'right' }}>Cost</th>
              </tr>
            </thead>
            <tbody>
              {events.map(e => (
                <tr key={`${e.sessionId}-${e.eventTimestamp}`}>
                  <td style={{ color: '#6b7280' }}>{formatTime(e.eventTimestamp)}</td>
                  <td>
                    <span className="agent-badge" style={badgeStyle(e.agentName)}>
                      {e.agentName}
                    </span>
                  </td>
                  <td style={{ color: '#6b7280' }}>{e.model ?? '—'}</td>
                  <td>{e.activityType ?? '—'}</td>
                  <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                    {totalTokens(e).toLocaleString()}
                  </td>
                  <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                    ${e.totalCostUsd.toFixed(4)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
