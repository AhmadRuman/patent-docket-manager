import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import type { Deadline } from '../api'
import { DeadlineTable } from './DeadlineTable'

const deadline: Deadline = {
  id: 7,
  matterId: 3,
  docketNumber: 'FAKE-2024-001-US',
  matterTitle: 'Self-Calibrating Widget Torque Sensor',
  clientName: 'Acme Widgets Ltd. (fictional)',
  responsibleAttorneyInitials: 'JE',
  type: 'NonFinalOfficeActionResponse',
  description: 'Response to non-final office action',
  triggerDate: '2026-07-12',
  dueDate: '2026-10-13',
  finalDueDate: '2027-01-12',
  notes: null,
  isCompleted: false,
  completedAtUtc: null,
  daysUntilDue: -2,
  urgency: 'overdue',
}

describe('DeadlineTable', () => {
  it('shows the empty message when there are no deadlines', () => {
    render(<DeadlineTable deadlines={[]} emptyText="Nothing due." />)
    expect(screen.getByText('Nothing due.')).toBeInTheDocument()
  })

  it('renders deadline details with an urgency badge and a link to the matter', () => {
    render(
      <MemoryRouter>
        <DeadlineTable deadlines={[deadline]} emptyText="" />
      </MemoryRouter>,
    )

    expect(screen.getByText('2 days overdue')).toHaveClass('badge-overdue')
    expect(screen.getByRole('link', { name: 'FAKE-2024-001-US' })).toHaveAttribute('href', '/matters/3')
    expect(screen.getByText('Final: Tue, Jan 12, 2027')).toBeInTheDocument()
  })

  it('calls onComplete when "Mark done" is clicked', async () => {
    const onComplete = vi.fn()
    render(
      <MemoryRouter>
        <DeadlineTable deadlines={[deadline]} emptyText="" onComplete={onComplete} />
      </MemoryRouter>,
    )

    await userEvent.click(screen.getByRole('button', { name: 'Mark done' }))
    expect(onComplete).toHaveBeenCalledWith(deadline)
  })
})
