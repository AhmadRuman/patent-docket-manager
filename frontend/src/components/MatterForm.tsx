import { useState, type FormEvent } from 'react'
import { matterStatuses, type Attorney, type MatterInput } from '../api'

interface Props {
  initial?: Partial<MatterInput>
  attorneys: Attorney[]
  submitLabel: string
  onSubmit: (input: MatterInput) => Promise<void>
  onCancel?: () => void
}

const empty: MatterInput = {
  docketNumber: '',
  title: '',
  clientName: '',
  jurisdiction: 'US',
  applicationNumber: null,
  filingDate: null,
  priorityDate: null,
  status: 'Pending',
  responsibleAttorneyId: null,
}

export function MatterForm({ initial, attorneys, submitLabel, onSubmit, onCancel }: Props) {
  const [form, setForm] = useState<MatterInput>({ ...empty, ...initial })
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const set = <K extends keyof MatterInput>(key: K, value: MatterInput[K]) => setForm((f) => ({ ...f, [key]: value }))
  const orNull = (v: string) => (v.trim() === '' ? null : v)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    try {
      await onSubmit(form)
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="form" onSubmit={handleSubmit}>
      {error && (
        <div className="alert" role="alert">
          {error}
        </div>
      )}
      <div className="grid-2">
        <label>
          Docket number
          <input required minLength={3} maxLength={40} value={form.docketNumber} onChange={(e) => set('docketNumber', e.target.value)} placeholder="FAKE-2026-010-US" />
        </label>
        <label>
          Client
          <input required minLength={2} maxLength={200} value={form.clientName} onChange={(e) => set('clientName', e.target.value)} placeholder="Example Corp (fictional)" />
        </label>
      </div>
      <label>
        Title
        <input required minLength={3} maxLength={300} value={form.title} onChange={(e) => set('title', e.target.value)} />
      </label>
      <div className="grid-3">
        <label>
          Jurisdiction
          <input required minLength={2} maxLength={10} value={form.jurisdiction} onChange={(e) => set('jurisdiction', e.target.value)} />
        </label>
        <label>
          Application number
          <input maxLength={40} value={form.applicationNumber ?? ''} onChange={(e) => set('applicationNumber', orNull(e.target.value))} />
        </label>
        <label>
          Status
          <select value={form.status} onChange={(e) => set('status', e.target.value as MatterInput['status'])}>
            {matterStatuses.map((s) => (
              <option key={s}>{s}</option>
            ))}
          </select>
        </label>
      </div>
      <div className="grid-3">
        <label>
          Filing date
          <input type="date" value={form.filingDate ?? ''} onChange={(e) => set('filingDate', orNull(e.target.value))} />
        </label>
        <label>
          Priority date
          <input type="date" value={form.priorityDate ?? ''} onChange={(e) => set('priorityDate', orNull(e.target.value))} />
        </label>
        <label>
          Responsible attorney
          <select
            value={form.responsibleAttorneyId ?? ''}
            onChange={(e) => set('responsibleAttorneyId', e.target.value === '' ? null : Number(e.target.value))}
          >
            <option value="">Unassigned</option>
            {attorneys.map((a) => (
              <option key={a.id} value={a.id}>
                {a.name} ({a.role})
              </option>
            ))}
          </select>
        </label>
      </div>
      <div className="form-actions">
        <button className="btn btn-primary" disabled={saving}>
          {saving ? 'Saving…' : submitLabel}
        </button>
        {onCancel && (
          <button type="button" className="btn btn-ghost" onClick={onCancel}>
            Cancel
          </button>
        )}
      </div>
    </form>
  )
}
