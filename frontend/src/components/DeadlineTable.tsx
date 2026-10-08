import { Link } from 'react-router-dom'
import type { Deadline } from '../api'
import { deadlineTypeLabels, formatDate } from '../format'
import { UrgencyBadge } from './UrgencyBadge'

interface Props {
  deadlines: Deadline[]
  emptyText: string
  /** Hide the matter columns when the table is already shown inside a matter. */
  showMatter?: boolean
  onComplete?: (d: Deadline) => void
  onReopen?: (d: Deadline) => void
  onDelete?: (d: Deadline) => void
}

export function DeadlineTable({ deadlines, emptyText, showMatter = true, onComplete, onReopen, onDelete }: Props) {
  if (deadlines.length === 0) {
    return <p className="empty">{emptyText}</p>
  }

  return (
    <div className="table-wrap">
      <table className="table">
        <thead>
          <tr>
            <th>Due</th>
            <th>Status</th>
            {showMatter && <th>Matter</th>}
            <th>Deadline</th>
            {showMatter && <th title="Responsible attorney">Atty</th>}
            <th aria-label="Actions" />
          </tr>
        </thead>
        <tbody>
          {deadlines.map((d) => (
            <tr key={d.id} className={d.isCompleted ? 'row-done' : undefined}>
              <td className="nowrap" data-label="Due">
                {formatDate(d.dueDate)}
                {d.finalDueDate && <div className="muted small">Final: {formatDate(d.finalDueDate)}</div>}
              </td>
              <td data-label="Status">
                <UrgencyBadge urgency={d.urgency} days={d.daysUntilDue} />
              </td>
              {showMatter && (
                <td data-label="Matter">
                  <Link to={`/matters/${d.matterId}`} className="mono">
                    {d.docketNumber}
                  </Link>
                  <div className="muted small">{d.matterTitle}</div>
                </td>
              )}
              <td data-label="Deadline">
                {d.description}
                <div className="muted small">
                  {deadlineTypeLabels[d.type]}
                  {d.notes && <> · {d.notes}</>}
                </div>
              </td>
              {showMatter && <td data-label="Atty">{d.responsibleAttorneyInitials ?? '—'}</td>}
              <td className="actions">
                {!d.isCompleted && onComplete && (
                  <button className="btn btn-small" onClick={() => onComplete(d)}>
                    Mark done
                  </button>
                )}
                {d.isCompleted && onReopen && (
                  <button className="btn btn-small btn-ghost" onClick={() => onReopen(d)}>
                    Reopen
                  </button>
                )}
                {onDelete && (
                  <button className="btn btn-small btn-ghost btn-danger" onClick={() => onDelete(d)} aria-label={`Delete ${d.description}`}>
                    Delete
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
