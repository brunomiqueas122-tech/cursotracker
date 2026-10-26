import type { Course } from '../types'
import { backupReminder } from './backupReminder'

const course = (p: Partial<Course> = {}): Course => ({
  id: 1, day: 1, date: '2026-10-25', platform: 'P', name: 'Curso', area: 'Cloud', url: 'https://example.com',
  estimatedHours: 2, hoursDone: 0, status: 'NotStarted', completedAt: null, certificateReceived: false,
  certificateUrl: null, onLinkedIn: false, notes: null, ...p,
})

describe('backupReminder', () => {
  it('stays quiet while there is no progress to lose', () => {
    expect(backupReminder([course()], null, '2026-11-20')).toBeNull()
  })

  it('asks for a first backup once there is progress', () => {
    expect(backupReminder([course({ hoursDone: 1 })], null, '2026-10-26')).toEqual({ kind: 'never' })
  })

  it('reminds after seven days', () => {
    const courses = [course({ status: 'Completed' })]

    expect(backupReminder(courses, '2026-10-26T22:00:00Z', '2026-11-01')).toBeNull()
    expect(backupReminder(courses, '2026-10-26T22:00:00Z', '2026-11-02')).toEqual({ kind: 'stale', days: 7 })
  })
})
