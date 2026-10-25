import { formatDate, formatHours } from '../format'
import type { DashboardSummary } from '../types'

export function DashboardPanel({ summary }: { summary: DashboardSummary }) {
  const planned = summary.totalCourses - summary.skippedCourses
  const maxDayHours = Math.max(1, ...summary.days.map((d) => Math.max(d.estimatedHours, d.hoursDone)))

  return (
    <section className="dashboard" aria-label="Resumo">
      <div className="stats">
        <div className="stat">
          <span className="stat-label">Concluído</span>
          <strong className="stat-value">{summary.completedPercent.toLocaleString('pt-BR')}%</strong>
          <div className="meter" role="progressbar" aria-valuenow={summary.completedPercent} aria-valuemin={0} aria-valuemax={100}>
            <div style={{ width: `${summary.completedPercent}%` }} />
          </div>
          <span className="stat-sub">
            {summary.completedCourses} de {planned} cursos
            {summary.skippedCourses > 0 && ` · ${summary.skippedCourses} pulado(s)`}
          </span>
        </div>
        <div className="stat">
          <span className="stat-label">Certificados</span>
          <strong className="stat-value">{summary.certificatesReceived}</strong>
          <span className="stat-sub">
            {summary.certificatesPending > 0
              ? `${summary.certificatesPending} concluído(s) aguardando certificado`
              : 'nenhum pendente'}
            {' · '}
            {summary.onLinkedIn} no LinkedIn
          </span>
        </div>
        <div className="stat">
          <span className="stat-label">Horas</span>
          <strong className="stat-value">{formatHours(summary.hoursDone)}</strong>
          <span className="stat-sub">de {formatHours(summary.estimatedHours)} estimadas</span>
        </div>
      </div>

      <div className="days" aria-label="Horas por dia">
        {summary.days.map((d) => (
          <div className="day-bar" key={d.day} title={`${formatHours(d.hoursDone)} de ${formatHours(d.estimatedHours)}`}>
            <div className="bars">
              <div className="bar estimated" style={{ height: `${(d.estimatedHours / maxDayHours) * 100}%` }} />
              <div className="bar done" style={{ height: `${(d.hoursDone / maxDayHours) * 100}%` }} />
            </div>
            <span className="day-label">Dia {d.day}</span>
            <span className="day-date">{formatDate(d.date)}</span>
          </div>
        ))}
      </div>
      <div className="legend">
        <span><i className="estimated" /> estimadas</span>
        <span><i className="done" /> feitas</span>
      </div>
    </section>
  )
}
