import { render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import type { Dashboard, Deadline } from '../api'
import { DashboardPage } from './DashboardPage'

function deadline(id: number, description: string, daysUntilDue: number, urgency: Deadline['urgency']): Deadline {
  return {
    id,
    matterId: 1,
    docketNumber: `FAKE-${id}`,
    matterTitle: 'Fictional matter',
    clientName: 'Example (fictional)',
    responsibleAttorneyInitials: 'RS',
    type: 'Custom',
    description,
    triggerDate: null,
    dueDate: '2026-10-10',
    finalDueDate: null,
    notes: null,
    isCompleted: false,
    completedAtUtc: null,
    daysUntilDue,
    urgency,
  }
}

const dashboard: Dashboard = {
  today: '2026-10-07',
  thisWeekEnd: '2026-10-13',
  overdueCount: 1,
  dueThisWeekCount: 1,
  dueNext30DaysCount: 2,
  activeMatterCount: 5,
  overdue: [deadline(1, 'Late response', -2, 'overdue')],
  thisWeek: [deadline(2, 'Call inventor', 3, 'thisWeek')],
  upcoming: [deadline(3, 'File IDS', 20, 'soon')],
}

describe('DashboardPage', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify(dashboard), { status: 200 })))
  })
  afterEach(() => vi.unstubAllGlobals())

  it('shows summary counts and groups deadlines into sections', async () => {
    render(
      <MemoryRouter>
        <DashboardPage />
      </MemoryRouter>,
    )

    const overdueSection = (await screen.findByRole('heading', { name: 'Overdue' })).closest('section')!
    expect(within(overdueSection).getByText('Late response')).toBeInTheDocument()

    const weekSection = screen.getByRole('heading', { name: 'Due this week' }).closest('section')!
    expect(within(weekSection).getByText('Call inventor')).toBeInTheDocument()

    const summary = screen.getByRole('region', { name: 'Summary' })
    expect(within(summary).getByText('Active matters').previousSibling).toHaveTextContent('5')
    expect(fetch).toHaveBeenCalledWith('/api/dashboard', expect.anything())
  })

  it('shows an error when the API is unreachable', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response('{"title":"Service unavailable"}', { status: 503 })))
    render(
      <MemoryRouter>
        <DashboardPage />
      </MemoryRouter>,
    )

    expect(await screen.findByRole('alert')).toHaveTextContent('Service unavailable')
  })
})
