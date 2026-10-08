import type { DeadlineType, Urgency } from './api'

/** Parses "YYYY-MM-DD" as a local calendar date (no time-zone shift). */
export function parseDate(iso: string): Date {
  const [y, m, d] = iso.split('-').map(Number)
  return new Date(y, m - 1, d)
}

const dateFormat = new Intl.DateTimeFormat('en-US', { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric' })

export function formatDate(iso: string | null | undefined): string {
  return iso ? dateFormat.format(parseDate(iso)) : '—'
}

export function relativeDays(days: number): string {
  if (days === 0) return 'Due today'
  if (days === 1) return 'Tomorrow'
  if (days === -1) return '1 day overdue'
  return days < 0 ? `${-days} days overdue` : `In ${days} days`
}

export const urgencyLabels: Record<Urgency, string> = {
  overdue: 'Overdue',
  today: 'Today',
  thisWeek: 'This week',
  soon: 'Next 30 days',
  later: 'Later',
  completed: 'Done',
}

export const deadlineTypeLabels: Record<DeadlineType, string> = {
  NonFinalOfficeActionResponse: 'Non-final OA response',
  FinalOfficeActionResponse: 'Final OA response',
  RestrictionRequirementResponse: 'Restriction response',
  NoticeOfMissingParts: 'Missing parts',
  IssueFeePayment: 'Issue fee',
  PctNationalPhaseEntry: 'PCT national phase',
  InformationDisclosureStatement: 'IDS',
  Custom: 'Custom',
}

/** Today's date as "YYYY-MM-DD" in the browser's local time zone. */
export function todayIso(): string {
  const now = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`
}
