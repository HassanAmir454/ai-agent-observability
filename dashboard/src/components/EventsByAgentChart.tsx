import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  Cell,
} from 'recharts'
import type { AgentSummary } from '../types.js'
import './components.css'

const AGENT_COLORS: Record<string, string> = {
  Cursor: '#2563eb',
  'Claude Code': '#7c3aed',
  'GitHub Copilot': '#16a34a',
}

function agentColor(name: string): string {
  return AGENT_COLORS[name] ?? '#9ca3af'
}

interface Props {
  data: AgentSummary[]
}

export function EventsByAgentChart({ data }: Props) {
  return (
    <div className="card">
      <p className="chart-title">Events by Agent</p>
      {data.length === 0 ? (
        <div className="empty-state">
          <strong>No data yet</strong>
          <span>Run <code>npm run collect:once</code> to populate this chart.</span>
        </div>
      ) : (
        <ResponsiveContainer width="100%" height={220}>
          <BarChart data={data} margin={{ top: 4, right: 8, left: 0, bottom: 4 }}>
            <XAxis dataKey="agentName" tick={{ fontSize: 12 }} />
            <YAxis allowDecimals={false} tick={{ fontSize: 12 }} />
            <Tooltip
              formatter={(value: number) => [value.toLocaleString(), 'Events']}
            />
            <Bar dataKey="eventCount" radius={[4, 4, 0, 0]}>
              {data.map(entry => (
                <Cell key={entry.agentName} fill={agentColor(entry.agentName)} />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      )}
    </div>
  )
}
