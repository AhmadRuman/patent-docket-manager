export function StatTile({ label, value, tone }: { label: string; value: number; tone?: 'danger' | 'warning' | 'neutral' }) {
  return (
    <div className={`stat stat-${tone ?? 'neutral'}`}>
      <div className="stat-value">{value}</div>
      <div className="stat-label">{label}</div>
    </div>
  )
}
