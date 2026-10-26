import { useCallback, useEffect, useState } from 'react'
import { api } from './api'
import { AuthScreen } from './components/AuthScreen'
import { DiscoverPanel } from './components/DiscoverPanel'
import { ProfileForm } from './components/ProfileForm'
import { toIsoDate } from './domain/agenda'
import { nextPlanDates } from './domain/schedule'
import { httpStore } from './store'
import { Tracker } from './Tracker'
import type { CourseSuggestion, Profile, ProfileAnswers } from './types'

type Session = 'checking' | 'offline' | 'loggedOut' | 'loggedIn'
type View = 'tracker' | 'discover' | 'profile'

export default function App() {
  const [session, setSession] = useState<Session>('checking')
  const [profile, setProfile] = useState<Profile | null>(null)
  const [view, setView] = useState<View>('tracker')
  const [returnTo, setReturnTo] = useState<View>('tracker')
  const [searchOnOpen, setSearchOnOpen] = useState(false)

  const enter = useCallback(
    (signal?: AbortSignal) =>
      api.me(signal).then(async (me) => {
        const current = me?.hasProfile ? await api.profile(signal) : null
        setProfile(current)
        // Primeiro acesso: começa pelas perguntas do perfil
        setView(current ? 'tracker' : 'profile')
        setSession(me ? 'loggedIn' : 'loggedOut')
      }),
    [],
  )

  useEffect(() => {
    const controller = new AbortController()
    enter(controller.signal).catch(() => {
      if (!controller.signal.aborted) setSession('offline')
    })
    return () => controller.abort()
  }, [enter])

  const login = async (email: string, password: string) => {
    await api.login(email, password)
    await enter()
  }
  const register = async (email: string, password: string, inviteCode: string) => {
    await api.register(email, password, inviteCode)
    await enter()
  }
  const logout = async () => {
    await api.logout().catch(() => undefined)
    setProfile(null)
    setSession('loggedOut')
  }
  const expire = useCallback(() => setSession('loggedOut'), [])

  const editProfile = (from: View) => {
    setReturnTo(from)
    setView('profile')
  }

  const saveProfile = async (answers: ProfileAnswers) => {
    const firstTime = profile === null
    setProfile(await api.saveProfile(answers))
    setSearchOnOpen(firstTime)
    setView(firstTime ? 'discover' : returnTo)
  }

  const openDiscover = async () => {
    setProfile(await api.profile())
    setSearchOnOpen(false)
    setView('discover')
  }

  // Um curso por dia, a partir do fim do plano atual
  const addSuggestions = async (list: CourseSuggestion[]) => {
    const dates = nextPlanDates(await api.courses(), toIsoDate(new Date()), list.length)
    for (const [i, c] of list.entries())
      await api.createCourse({ date: dates[i], platform: c.platform, name: c.name, area: c.area, url: c.url, estimatedHours: c.estimatedHours })
  }

  switch (session) {
    case 'checking':
      return <p className="empty">Carregando…</p>
    case 'offline':
      return <p className="error page">Não foi possível falar com o servidor. Confira sua internet e recarregue a página.</p>
    case 'loggedOut':
      return <AuthScreen onLogin={login} onRegister={register} />
  }

  if (view === 'profile' || !profile)
    return <ProfileForm initial={profile} onSubmit={saveProfile} onCancel={profile ? () => setView(returnTo) : undefined} />

  if (view === 'discover')
    return (
      <DiscoverPanel
        profile={profile}
        search={api.recommendations}
        onAdd={addSuggestions}
        onEditProfile={() => editProfile('discover')}
        onBack={() => setView('tracker')}
        searchOnOpen={searchOnOpen}
      />
    )

  return (
    <Tracker
      store={httpStore}
      onUnauthorized={expire}
      onLogout={logout}
      onDiscover={openDiscover}
      onEditProfile={() => editProfile('tracker')}
      importCourses={api.importCourses}
    />
  )
}
