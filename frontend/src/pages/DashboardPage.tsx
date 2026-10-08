import { api, type Deadline } from '../api'
import { DeadlineTable } from '../components/DeadlineTable'
import { StatTile } from '../components/StatTile'
import { ErrorMessage, Loading } from '../components/Status'
import { formatDate } from '../format'
import { useApi } from '../useApi'

export function DashboardPage() {
  const { data, error, loading, reload } = useApi(api.dashboard, [])

  async function complete(d: Deadline) {
    await api.completeDeadline(d.id)
    reload()
  }

  if (error) return <ErrorMessage message={error} />
  if (!data) return loading ? <Loading /> : null

  return (
    <>
      <div className="page-header">
        <h1>Dashboard</h1>
        <p className="muted">
          Today is {formatDate(data.today)}. "This week" runs through {formatDate(data.thisWeekEnd)}.
        </p>
      </div>

      <section className="stats" aria-label="Summary">
        <StatTile label="Overdue" value={data.overdueCount} tone={data.overdueCount ? 'danger' : 'neutral'} />
        <StatTile label="Due this week" value={data.dueThisWeekCount} tone={data.dueThisWeekCount ? 'warning' : 'neutral'} />
        <StatTile label="Due in 30 days" value={data.dueNext30DaysCount} />
        <StatTile label="Active matters" value={data.activeMatterCount} />
      </section>

      {data.overdue.length > 0 && (
        <section className="card card-danger">
          <h2>Overdue</h2>
          <DeadlineTable deadlines={data.overdue} emptyText="" onComplete={complete} />
        </section>
      )}

      <section className="card">
        <h2>Due this week</h2>
        <DeadlineTable deadlines={data.thisWeek} emptyText="Nothing due in the next 7 days." onComplete={complete} />
      </section>

      <section className="card">
        <h2>Coming up (next 30 days)</h2>
        <DeadlineTable deadlines={data.upcoming} emptyText="Nothing else due in the next 30 days." onComplete={complete} />
      </section>
    </>
  )
}
