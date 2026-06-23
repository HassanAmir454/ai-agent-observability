import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
} from 'recharts'
import type { TimelineBucket } from '../types.js'
import './components.css'

interface Props {
  data: TimelineBucket[]
}

function formatHour(iso: string): string {
  const d = new Date(iso)
  return `${d.getMonth() + 1}/${d.getDate()} ${String(d.getHours()).padStart(2, '0')}:00`
}

export function EventTimelineChart({ data }: Props) {
  const chartData = data.map(b => ({
    hour: formatHour(b.hour),
    count: b.count,
    totalCost: b.totalCost,
  }))

  return (
    <div className="card">
      <p className="chart-title">Event Timeline</p>
      {data.length === 0 ? (
        <div className="empty-state">
          <strong>No data yet</strong>
          <span>Events will appear here once collected.</span>
        </div>
      ) : (
        <ResponsiveContainer width="100%" height={220}>
          <AreaChart data={chartData} margin={{ top: 4, right: 8, left: 0, bottom: 4 }}>
            <defs>
              <linearGradient id="areaGrad" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%"  stopColor="#2563eb" stopOpacity={0.25} />
                <stop offset="95%" stopColor="#2563eb" stopOpacity={0} />
              </linearGradient>
            </defs>
            <XAxis dataKey="hour" tick={{ fontSize: 11 }} interval="preserveStartEnd" />
            <YAxis allowDecimals={false} tick={{ fontSize: 12 }} />
            <Tooltip
              formatter={(value: number, name: string) =>
                name === 'count'
                  ? [value.toLocaleString(), 'Events']
                  : [`$${value.toFixed(4)}`, 'Cost']
              }
            />
            <Area
              type="monotone"
              dataKey="count"
              stroke="#2563eb"
              strokeWidth={2}
              fill="url(#areaGrad)"
              dot={false}
            />
          </AreaChart>
        </ResponsiveContainer>
      )}
    </div>
  )
}
