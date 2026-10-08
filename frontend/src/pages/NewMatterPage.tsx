import { useNavigate } from 'react-router-dom'
import { api } from '../api'
import { MatterForm } from '../components/MatterForm'
import { ErrorMessage, Loading } from '../components/Status'
import { useApi } from '../useApi'

export function NewMatterPage() {
  const navigate = useNavigate()
  const attorneys = useApi(api.attorneys, [])

  return (
    <>
      <div className="page-header">
        <h1>New matter</h1>
        <p className="muted">Please use fictional data only. This is a demo system.</p>
      </div>
      {attorneys.error && <ErrorMessage message={attorneys.error} />}
      {attorneys.data ? (
        <section className="card">
          <MatterForm
            attorneys={attorneys.data}
            submitLabel="Create matter"
            onSubmit={async (input) => {
              const created = await api.createMatter(input)
              navigate(`/matters/${created.id}`)
            }}
            onCancel={() => navigate('/matters')}
          />
        </section>
      ) : (
        !attorneys.error && <Loading />
      )}
    </>
  )
}
