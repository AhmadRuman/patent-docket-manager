import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, type Deadline, type MatterInput } from '../api'
import { DeadlineForm } from '../components/DeadlineForm'
import { DeadlineTable } from '../components/DeadlineTable'
import { MatterForm } from '../components/MatterForm'
import { ErrorMessage, Loading } from '../components/Status'
import { formatDate } from '../format'
import { useApi } from '../useApi'

export function MatterDetailPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const [editing, setEditing] = useState(false)

  const matter = useApi(() => api.matter(id), [id])
  const attorneys = useApi(api.attorneys, [])
  const rules = useApi(api.rules, [])

  async function act(fn: (id: number) => Promise<unknown>, d: Deadline) {
    await fn(d.id)
    matter.reload()
  }

  async function remove(d: Deadline) {
    if (window.confirm(`Delete "${d.description}"? This cannot be undone.`)) {
      await act(api.deleteDeadline, d)
    }
  }

  async function deleteMatter() {
    if (window.confirm('Delete this matter and all of its deadlines? This cannot be undone.')) {
      await api.deleteMatter(id)
      navigate('/matters')
    }
  }

  if (matter.error) return <ErrorMessage message={matter.error} />
  const m = matter.data
  if (!m) return <Loading />

  const initial: MatterInput = {
    docketNumber: m.docketNumber,
    title: m.title,
    clientName: m.clientName,
    jurisdiction: m.jurisdiction,
    applicationNumber: m.applicationNumber,
    filingDate: m.filingDate,
    priorityDate: m.priorityDate,
    status: m.status,
    responsibleAttorneyId: m.responsibleAttorney?.id ?? null,
  }

  return (
    <>
      <p className="crumbs">
        <Link to="/matters">← Matters</Link>
      </p>
      <div className="page-header row">
        <div>
          <h1>
            <span className="mono">{m.docketNumber}</span> <span className={`pill pill-${m.status.toLowerCase()}`}>{m.status}</span>
          </h1>
          <p className="lead">{m.title}</p>
        </div>
        {!editing && (
          <div className="row-actions">
            <button className="btn" onClick={() => setEditing(true)}>
              Edit
            </button>
            <button className="btn btn-ghost btn-danger" onClick={deleteMatter}>
              Delete
            </button>
          </div>
        )}
      </div>

      <section className="card">
        {editing && attorneys.data ? (
          <MatterForm
            initial={initial}
            attorneys={attorneys.data}
            submitLabel="Save changes"
            onSubmit={async (input) => {
              await api.updateMatter(id, input)
              setEditing(false)
              matter.reload()
            }}
            onCancel={() => setEditing(false)}
          />
        ) : (
          <dl className="facts">
            <div>
              <dt>Client</dt>
              <dd>{m.clientName}</dd>
            </div>
            <div>
              <dt>Jurisdiction</dt>
              <dd>{m.jurisdiction}</dd>
            </div>
            <div>
              <dt>Application no.</dt>
              <dd>{m.applicationNumber ?? '—'}</dd>
            </div>
            <div>
              <dt>Filing date</dt>
              <dd>{formatDate(m.filingDate)}</dd>
            </div>
            <div>
              <dt>Priority date</dt>
              <dd>{formatDate(m.priorityDate)}</dd>
            </div>
            <div>
              <dt>Responsible</dt>
              <dd>{m.responsibleAttorney ? `${m.responsibleAttorney.name} (${m.responsibleAttorney.role})` : 'Unassigned'}</dd>
            </div>
          </dl>
        )}
      </section>

      <section className="card">
        <h2>Deadlines</h2>
        <DeadlineTable
          deadlines={m.deadlines}
          showMatter={false}
          emptyText="No deadlines docketed for this matter yet."
          onComplete={(d) => act(api.completeDeadline, d)}
          onReopen={(d) => act(api.reopenDeadline, d)}
          onDelete={remove}
        />
      </section>

      <section className="card">
        <h2>Add deadline</h2>
        {rules.data && <DeadlineForm rules={rules.data} onSubmit={async (input) => {
          await api.addDeadline(id, input)
          matter.reload()
        }} />}
        <p className="hint">
          Calculated dates use simplified USPTO-style rules and roll forward past weekends and federal holidays. They are illustrative only and
          must be checked against the actual office communication.
        </p>
      </section>
    </>
  )
}
