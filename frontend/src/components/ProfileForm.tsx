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
import { PremiumLock } from './PremiumLock'

interface Props {
  initial: ProfileAnswers | null
  /** Máximo de assuntos; null = ilimitado (Premium) */
  topicLimit: number | null
  onSubmit: (answers: ProfileAnswers) => Promise<void>
  onCancel?: () => void
  onUpgrade: () => void
}

const empty: ProfileAnswers = { topics: [''], goal: 'GetJob', level: 'Beginner', language: 'Portuguese', hoursPerWeek: 5 }

export function ProfileForm({ initial, topicLimit, onSubmit, onCancel, onUpgrade }: Props) {
  // Só os campos do formulário: o perfil salvo traz também contadores do servidor
  const [form, setForm] = useState<ProfileAnswers>(() =>
    initial
      ? { topics: initial.topics, goal: initial.goal, level: initial.level, language: initial.language, hoursPerWeek: initial.hoursPerWeek }
      : empty,
  )
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const firstTime = initial === null

  const update = (patch: Partial<ProfileAnswers>) => setForm((f) => ({ ...f, ...patch }))
  const setTopic = (index: number, value: string) => update({ topics: form.topics.map((t, i) => (i === index ? value : t)) })
  const removeTopic = (index: number) => update({ topics: form.topics.filter((_, i) => i !== index) })
  const atLimit = topicLimit !== null && form.topics.length >= topicLimit

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await onSubmit({ ...form, topics: form.topics.map((t) => t.trim()).filter((t) => t !== '') })
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
        <fieldset className="topics">
          <legend>Que tipo de curso você está procurando?</legend>
          <small className="hint">
            Um assunto por linha.{topicLimit !== null && ` No plano grátis, até ${topicLimit} assuntos.`}
          </small>
          {form.topics.map((topic, i) => (
            <div key={i} className="topic-row">
              <input
                aria-label={`Assunto ${i + 1}`}
                required={i === 0}
                maxLength={200}
                placeholder={i === 0 ? 'Ex.: cloud e DevOps para conseguir meu primeiro emprego' : 'Ex.: SQL para análise de dados'}
                value={topic}
                onChange={(e) => setTopic(i, e.target.value)}
              />
              {form.topics.length > 1 && (
                <button type="button" className="link danger" onClick={() => removeTopic(i)} aria-label={`Remover assunto ${i + 1}`}>
                  Remover
                </button>
              )}
            </div>
          ))}
          {atLimit ? (
            <PremiumLock onUpgrade={onUpgrade}>Adicionar mais assuntos</PremiumLock>
          ) : (
            <button type="button" className="secondary" onClick={() => update({ topics: [...form.topics, ''] })}>
              + Adicionar assunto
            </button>
          )}
        </fieldset>
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
