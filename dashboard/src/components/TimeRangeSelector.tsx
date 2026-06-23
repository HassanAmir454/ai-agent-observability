import './components.css'

const RANGES = ['1h', '6h', '24h', '7d'] as const
export type TimeRange = (typeof RANGES)[number]

interface Props {
  selected: TimeRange
  onChange: (range: TimeRange) => void
}

export function TimeRangeSelector({ selected, onChange }: Props) {
  return (
    <div className="time-range-selector">
      {RANGES.map(r => (
        <button
          key={r}
          className={`time-range-btn${selected === r ? ' active' : ''}`}
          onClick={() => onChange(r)}
        >
          {r}
        </button>
      ))}
    </div>
  )
}
