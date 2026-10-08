import { useState } from 'react'
import { api, type Deadline } from '../api'
import { DeadlineTable } from '../components/DeadlineTable'
import { ErrorMessage, Loading } from '../components/Status'
import { useApi } from '../useApi'

type StatusFilter = 'Open' | 'Completed' | 'All'

/** The full docket: every deadline, filterable by date range, attorney and status. */
export function DocketPage() {
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [status, setStatus] = useState<StatusFilter>('Open')
  const [attorneyId, setAttorneyId] = useState<number | ''>('')

  const attorneys = useApi(api.attorneys, [])
  const { data, error, loading, reload } = useApi(() => api.deadlines({ from, to, status, attorneyId }), [from, to, status, attorneyId])

  async function act(fn: (id: number) => Promise<unknown>, d: Deadline) {
    await fn(d.id)
    reload()
  }

  return (
    <>
      <div className="page-header">
        <h1>Docket</h1>
        <p className="muted">All deadlines, soonest first.</p>
      </div>

      <div className="filters">
        <label>
          From
          <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
        </label>
        <label>
          To
          <input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
        </label>
        <label>
          Attorney
          <select value={attorneyId} onChange={(e) => setAttorneyId(e.target.value === '' ? '' : Number(e.target.value))}>
            <option value="">Everyone</option>
            {attorneys.data?.map((a) => (
              <option key={a.id} value={a.id}>
                {a.name}
              </option>
            ))}
          </select>
        </label>
        <label>
          Status
          <select value={status} onChange={(e) => setStatus(e.target.value as StatusFilter)}>
            <option value="Open">Open</option>
            <option value="Completed">Completed</option>
            <option value="All">All</option>
          </select>
        </label>
      </div>

      {error && <ErrorMessage message={error} />}
      {loading && !data && <Loading />}
      {data && (
        <section className="card">
          <DeadlineTable
            deadlines={data}
            emptyText="No deadlines match these filters."
            onComplete={(d) => act(api.completeDeadline, d)}
            onReopen={(d) => act(api.reopenDeadline, d)}
          />
        </section>
      )}
    </>
  )
}
