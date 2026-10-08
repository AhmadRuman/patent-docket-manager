// Typed client for the Patent Docket API. Dates are ISO "YYYY-MM-DD" strings.

export type MatterStatus = 'Drafting' | 'Pending' | 'Allowed' | 'Issued' | 'Abandoned'
export const matterStatuses: MatterStatus[] = ['Drafting', 'Pending', 'Allowed', 'Issued', 'Abandoned']

export type DeadlineType =
  | 'NonFinalOfficeActionResponse'
  | 'FinalOfficeActionResponse'
  | 'RestrictionRequirementResponse'
  | 'NoticeOfMissingParts'
  | 'IssueFeePayment'
  | 'PctNationalPhaseEntry'
  | 'InformationDisclosureStatement'
  | 'Custom'

export type Urgency = 'overdue' | 'today' | 'thisWeek' | 'soon' | 'later' | 'completed'

export interface Attorney {
  id: number
  name: string
  initials: string
  email: string
  role: string
}

export interface Deadline {
  id: number
  matterId: number
  docketNumber: string
  matterTitle: string
  clientName: string
  responsibleAttorneyInitials: string | null
  type: DeadlineType
  description: string
  triggerDate: string | null
  dueDate: string
  finalDueDate: string | null
  notes: string | null
  isCompleted: boolean
  completedAtUtc: string | null
  daysUntilDue: number
  urgency: Urgency
}

export interface MatterSummary {
  id: number
  docketNumber: string
  title: string
  clientName: string
  jurisdiction: string
  applicationNumber: string | null
  status: MatterStatus
  responsibleAttorney: Attorney | null
  openDeadlineCount: number
  nextDueDate: string | null
}

export interface MatterDetail {
  id: number
  docketNumber: string
  title: string
  clientName: string
  jurisdiction: string
  applicationNumber: string | null
  filingDate: string | null
  priorityDate: string | null
  status: MatterStatus
  responsibleAttorney: Attorney | null
  deadlines: Deadline[]
  createdAtUtc: string
  updatedAtUtc: string
}

export interface MatterInput {
  docketNumber: string
  title: string
  clientName: string
  jurisdiction: string
  applicationNumber: string | null
  filingDate: string | null
  priorityDate: string | null
  status: MatterStatus
  responsibleAttorneyId: number | null
}

export interface DeadlineInput {
  type: DeadlineType
  description: string | null
  triggerDate: string | null
  dueDate: string | null
  finalDueDate: string | null
  notes: string | null
}

export interface DeadlineRule {
  type: DeadlineType
  label: string
  monthsFromTrigger: number
  maxMonthsWithExtensions: number | null
  triggerDescription: string
}

export interface CalculatedDeadline {
  type: DeadlineType
  description: string
  triggerDate: string
  dueDate: string
  finalDueDate: string | null
}

export interface Dashboard {
  today: string
  thisWeekEnd: string
  overdueCount: number
  dueThisWeekCount: number
  dueNext30DaysCount: number
  activeMatterCount: number
  overdue: Deadline[]
  thisWeek: Deadline[]
  upcoming: Deadline[]
}

export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: Record<string, string[]>

  constructor(status: number, message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message)
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  })

  if (!response.ok) {
    // The API returns RFC 9457 problem details for errors.
    const problem = await response.json().catch(() => null)
    const errors = (problem?.errors ?? {}) as Record<string, string[]>
    const message = problem?.detail ?? Object.values(errors).flat()[0] ?? problem?.title ?? `Request failed (${response.status})`
    throw new ApiError(response.status, message, errors)
  }
  return response.status === 204 ? (undefined as T) : ((await response.json()) as T)
}

function query(params: Record<string, string | number | undefined | null>): string {
  const entries = Object.entries(params).filter(([, v]) => v !== undefined && v !== null && v !== '')
  return entries.length ? '?' + new URLSearchParams(entries.map(([k, v]) => [k, String(v)])).toString() : ''
}

export const api = {
  dashboard: () => request<Dashboard>('/api/dashboard'),
  attorneys: () => request<Attorney[]>('/api/attorneys'),

  matters: (filter: { search?: string; status?: MatterStatus | ''; attorneyId?: number | '' } = {}) =>
    request<MatterSummary[]>('/api/matters' + query(filter)),
  matter: (id: number) => request<MatterDetail>(`/api/matters/${id}`),
  createMatter: (input: MatterInput) => request<MatterDetail>('/api/matters', { method: 'POST', body: JSON.stringify(input) }),
  updateMatter: (id: number, input: MatterInput) =>
    request<MatterDetail>(`/api/matters/${id}`, { method: 'PUT', body: JSON.stringify(input) }),
  deleteMatter: (id: number) => request<void>(`/api/matters/${id}`, { method: 'DELETE' }),

  deadlines: (filter: { from?: string; to?: string; status?: 'Open' | 'Completed' | 'All'; attorneyId?: number | '' } = {}) =>
    request<Deadline[]>('/api/deadlines' + query(filter)),
  addDeadline: (matterId: number, input: DeadlineInput) =>
    request<Deadline>(`/api/matters/${matterId}/deadlines`, { method: 'POST', body: JSON.stringify(input) }),
  completeDeadline: (id: number) => request<Deadline>(`/api/deadlines/${id}/complete`, { method: 'POST' }),
  reopenDeadline: (id: number) => request<Deadline>(`/api/deadlines/${id}/reopen`, { method: 'POST' }),
  deleteDeadline: (id: number) => request<void>(`/api/deadlines/${id}`, { method: 'DELETE' }),

  rules: () => request<DeadlineRule[]>('/api/deadline-rules'),
  calculate: (type: DeadlineType, triggerDate: string) =>
    request<CalculatedDeadline>('/api/deadline-rules/calculate' + query({ type, triggerDate })),
}
