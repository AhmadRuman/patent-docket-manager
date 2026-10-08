import type { Urgency } from '../api'
import { relativeDays, urgencyLabels } from '../format'

export function UrgencyBadge({ urgency, days }: { urgency: Urgency; days: number }) {
  const text = urgency === 'completed' ? urgencyLabels.completed : relativeDays(days)
  return <span className={`badge badge-${urgency}`}>{text}</span>
}
