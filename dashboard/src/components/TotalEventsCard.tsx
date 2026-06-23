import './components.css'

interface Props {
  totalEvents: number
  totalCostUsd: number
  totalTokens: number
  loading: boolean
}

function Tile({ label, value, loading }: { label: string; value: string; loading: boolean }) {
  return (
    <div className="stat-tile">
      <div className="stat-label">{label}</div>
      {loading
        ? <div className="skeleton skeleton-value" />
        : <div className="stat-value">{value}</div>}
    </div>
  )
}

export function TotalEventsCard({ totalEvents, totalCostUsd, totalTokens, loading }: Props) {
  return (
    <div className="stat-grid">
      <Tile label="Total Events"   value={totalEvents.toLocaleString()}        loading={loading} />
      <Tile label="Total Cost"     value={`$${totalCostUsd.toFixed(4)}`}       loading={loading} />
      <Tile label="Total Tokens"   value={totalTokens.toLocaleString()}        loading={loading} />
    </div>
  )
}
