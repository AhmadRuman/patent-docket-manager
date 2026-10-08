export function Loading() {
  return <p className="muted">Loading…</p>
}

export function ErrorMessage({ message }: { message: string }) {
  return (
    <div className="alert" role="alert">
      {message}
    </div>
  )
}
