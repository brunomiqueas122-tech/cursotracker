import { useEffect, useState } from 'react'
import { elapsedMs, formatElapsed } from '../timer'

interface Props {
  startedAt: string | null
  busyElsewhere: boolean
  disabled: boolean
  onStart: () => void
  onStop: () => void
}

export function StudyTimer({ startedAt, busyElsewhere, disabled, onStart, onStop }: Props) {
  const [now, setNow] = useState(() => new Date())

  useEffect(() => {
    if (!startedAt) return
    const tick = () => setNow(new Date())
    tick()
    const interval = window.setInterval(tick, 1000)
    return () => window.clearInterval(interval)
  }, [startedAt])

  if (startedAt) {
    return (
      <div className="timer running">
        <span className="timer-clock" aria-live="off">
          {formatElapsed(elapsedMs(startedAt, now))}
        </span>
        <button type="button" onClick={onStop} disabled={disabled}>
          Parar e registrar
        </button>
      </div>
    )
  }

  return (
    <div className="timer">
      <button
        type="button"
        className="secondary"
        onClick={onStart}
        disabled={disabled || busyElsewhere}
        title={busyElsewhere ? 'Pare o cronômetro do outro curso primeiro' : undefined}
      >
        Iniciar estudo
      </button>
      {busyElsewhere && <span className="timer-hint">Cronômetro rodando em outro curso</span>}
    </div>
  )
}
