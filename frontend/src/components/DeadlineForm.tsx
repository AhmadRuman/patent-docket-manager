import { useEffect, useState, type FormEvent } from 'react'
import { api, type CalculatedDeadline, type DeadlineInput, type DeadlineRule, type DeadlineType } from '../api'
import { formatDate } from '../format'

interface Props {
  rules: DeadlineRule[]
  onSubmit: (input: DeadlineInput) => Promise<void>
}

/**
 * Adds a deadline either from a docketing rule (pick the communication type and its
 * mailing/trigger date; the API calculates the due dates) or as a custom dated task.
 */
export function DeadlineForm({ rules, onSubmit }: Props) {
  const [type, setType] = useState<DeadlineType>(rules[0]?.type ?? 'Custom')
  const [triggerDate, setTriggerDate] = useState('')
  const [dueDate, setDueDate] = useState('')
  const [description, setDescription] = useState('')
  const [notes, setNotes] = useState('')
  const [preview, setPreview] = useState<CalculatedDeadline | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const rule = rules.find((r) => r.type === type)

  useEffect(() => {
    setPreview(null)
    if (!rule || !triggerDate) return
    let cancelled = false
    api
      .calculate(type, triggerDate)
      .then((p) => !cancelled && setPreview(p))
      .catch(() => !cancelled && setPreview(null))
    return () => {
      cancelled = true
    }
  }, [rule, type, triggerDate])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    try {
      await onSubmit({
        type,
        description: description.trim() || null,
        triggerDate: rule ? triggerDate || null : null,
        dueDate: rule ? null : dueDate || null,
        finalDueDate: null,
        notes: notes.trim() || null,
      })
      setTriggerDate('')
      setDueDate('')
      setDescription('')
      setNotes('')
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="form" onSubmit={handleSubmit} aria-label="Add deadline">
      {error && (
        <div className="alert" role="alert">
          {error}
        </div>
      )}
      <div className="grid-2">
        <label>
          Deadline type
          <select value={type} onChange={(e) => setType(e.target.value as DeadlineType)}>
            {rules.map((r) => (
              <option key={r.type} value={r.type}>
                {r.label}
              </option>
            ))}
            <option value="Custom">Custom (enter due date)</option>
          </select>
        </label>
        {rule ? (
          <label>
            {rule.triggerDescription}
            <input type="date" required value={triggerDate} onChange={(e) => setTriggerDate(e.target.value)} />
          </label>
        ) : (
          <label>
            Due date
            <input type="date" required value={dueDate} onChange={(e) => setDueDate(e.target.value)} />
          </label>
        )}
      </div>
      {rule && (
        <p className="hint" aria-live="polite">
          {rule.monthsFromTrigger} months from the trigger date
          {rule.maxMonthsWithExtensions ? `, extendable to ${rule.maxMonthsWithExtensions} months` : ', not extendable'}.
          {preview && (
            <>
              {' '}
              Due <strong>{formatDate(preview.dueDate)}</strong>
              {preview.finalDueDate && (
                <>
                  ; final date with extensions <strong>{formatDate(preview.finalDueDate)}</strong>
                </>
              )}
              .
            </>
          )}
        </p>
      )}
      <div className="grid-2">
        <label>
          <span>Description {rule && <span className="muted">(optional)</span>}</span>
          <input required={!rule} maxLength={300} value={description} onChange={(e) => setDescription(e.target.value)} placeholder={rule?.label} />
        </label>
        <label>
          <span>Notes <span className="muted">(optional)</span></span>
          <input maxLength={2000} value={notes} onChange={(e) => setNotes(e.target.value)} />
        </label>
      </div>
      <div className="form-actions">
        <button className="btn btn-primary" disabled={saving}>
          {saving ? 'Adding…' : 'Add deadline'}
        </button>
      </div>
    </form>
  )
}
