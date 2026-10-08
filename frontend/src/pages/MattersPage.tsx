import { useState } from 'react'
import { Link } from 'react-router-dom'
import { api, matterStatuses, type MatterStatus } from '../api'
import { ErrorMessage, Loading } from '../components/Status'
import { formatDate } from '../format'
import { useApi } from '../useApi'

export function MattersPage() {
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<MatterStatus | ''>('')
  const { data, error, loading } = useApi(() => api.matters({ search, status }), [search, status])

  return (
    <>
      <div className="page-header row">
        <div>
          <h1>Matters</h1>
          <p className="muted">Every patent matter on the docket.</p>
        </div>
        <Link to="/matters/new" className="btn btn-primary">
          New matter
        </Link>
      </div>

      <div className="filters">
        <label className="grow">
          Search
          <input type="search" placeholder="Docket number, title, client or application number" value={search} onChange={(e) => setSearch(e.target.value)} />
        </label>
        <label>
          Status
          <select value={status} onChange={(e) => setStatus(e.target.value as MatterStatus | '')}>
            <option value="">Any</option>
            {matterStatuses.map((s) => (
              <option key={s}>{s}</option>
            ))}
          </select>
        </label>
      </div>

      {error && <ErrorMessage message={error} />}
      {loading && !data && <Loading />}
      {data && (
        <section className="card">
          {data.length === 0 ? (
            <p className="empty">No matters found.</p>
          ) : (
            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr>
                    <th>Docket #</th>
                    <th>Title / client</th>
                    <th>Juris.</th>
                    <th>Status</th>
                    <th>Atty</th>
                    <th>Next due</th>
                  </tr>
                </thead>
                <tbody>
                  {data.map((m) => (
                    <tr key={m.id}>
                      <td data-label="Docket #">
                        <Link to={`/matters/${m.id}`} className="mono">
                          {m.docketNumber}
                        </Link>
                      </td>
                      <td data-label="Title / client">
                        {m.title}
                        <div className="muted small">{m.clientName}</div>
                      </td>
                      <td data-label="Juris.">{m.jurisdiction}</td>
                      <td data-label="Status">
                        <span className={`pill pill-${m.status.toLowerCase()}`}>{m.status}</span>
                      </td>
                      <td data-label="Atty">{m.responsibleAttorney?.initials ?? '—'}</td>
                      <td data-label="Next due" className="nowrap">
                        {formatDate(m.nextDueDate)}
                        {m.openDeadlineCount > 1 && <div className="muted small">{m.openDeadlineCount} open</div>}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}
    </>
  )
}
