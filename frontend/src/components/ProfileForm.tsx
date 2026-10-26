import { useState, type FormEvent } from 'react'
import { goalLabels, languageLabels, levelLabels } from '../format'
import {
  courseLanguages,
  learningGoals,
  skillLevels,
  type CourseLanguage,
  type LearningGoal,
  type ProfileAnswers,
  type SkillLevel,
} from '../types'

interface Props {
  initial: ProfileAnswers | null
  onSubmit: (answers: ProfileAnswers) => Promise<void>
  onCancel?: () => void
}

const empty: ProfileAnswers = { lookingFor: '', goal: 'GetJob', level: 'Beginner', language: 'Portuguese', hoursPerWeek: 5 }

export function ProfileForm({ initial, onSubmit, onCancel }: Props) {
  const [form, setForm] = useState(initial ?? empty)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const firstTime = initial === null

  const update = (patch: Partial<ProfileAnswers>) => setForm((f) => ({ ...f, ...patch }))

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await onSubmit(form)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível salvar.')
      setBusy(false)
    }
  }

  return (
    <main className="page">
      <header className="top">
        <h1>{firstTime ? 'Vamos montar seu plano' : 'Seu perfil'}</h1>
        <p>
          {firstTime
            ? 'Responda rapidinho: a IA usa isso para pesquisar cursos gratuitos que dão certificado gratuito.'
            : 'A IA usa essas respostas em todas as buscas.'}
        </p>
      </header>
      <form className="profile-form" onSubmit={submit}>
        <label htmlFor="lookingFor">
          Que tipo de curso você está procurando?
          <textarea
            id="lookingFor"
            rows={3}
            required
            maxLength={500}
            placeholder="Ex.: quero aprender cloud e DevOps para conseguir meu primeiro emprego na área"
            value={form.lookingFor}
            onChange={(e) => update({ lookingFor: e.target.value })}
          />
        </label>
        <div className="fields">
          <label htmlFor="goal">
            Objetivo
            <select id="goal" value={form.goal} onChange={(e) => update({ goal: e.target.value as LearningGoal })}>
              {learningGoals.map((g) => (
                <option key={g} value={g}>
                  {goalLabels[g]}
                </option>
              ))}
            </select>
          </label>
          <label htmlFor="level">
            Seu nível
            <select id="level" value={form.level} onChange={(e) => update({ level: e.target.value as SkillLevel })}>
              {skillLevels.map((l) => (
                <option key={l} value={l}>
                  {levelLabels[l]}
                </option>
              ))}
            </select>
          </label>
          <label htmlFor="language">
            Idioma dos cursos
            <select id="language" value={form.language} onChange={(e) => update({ language: e.target.value as CourseLanguage })}>
              {courseLanguages.map((l) => (
                <option key={l} value={l}>
                  {languageLabels[l]}
                </option>
              ))}
            </select>
          </label>
          <label htmlFor="hours">
            Horas por semana
            <input
              id="hours"
              type="number"
              min={1}
              max={80}
              required
              value={form.hoursPerWeek}
              onChange={(e) => update({ hoursPerWeek: e.target.valueAsNumber || 0 })}
            />
          </label>
        </div>
        {error && <p className="error" role="alert">{error}</p>}
        <div className="actions">
          {onCancel && (
            <button type="button" className="secondary" onClick={onCancel} disabled={busy}>
              Cancelar
            </button>
          )}
          <button type="submit" disabled={busy}>
            {busy ? 'Salvando…' : firstTime ? 'Buscar meus cursos' : 'Salvar perfil'}
          </button>
        </div>
      </form>
    </main>
  )
}
