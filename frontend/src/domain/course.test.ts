import type { Course, UpdateProgressRequest } from '../types'
import { applyProgress, validateProgress } from './course'

const now = new Date('2026-10-25T20:00:00Z')
const later = new Date('2026-10-26T20:00:00Z')

const course: Course = {
  id: 1, day: 1, date: '2026-10-25', platform: 'Linux Foundation', name: 'LFD121', area: 'Segurança',
  url: 'https://example.com', estimatedHours: 5, hoursDone: 0, status: 'NotStarted', completedAt: null,
  certificateReceived: false, certificateUrl: null, onLinkedIn: false, notes: null,
}

const progress = (p: Partial<UpdateProgressRequest>): UpdateProgressRequest => ({
  hoursDone: 0, status: 'NotStarted', certificateReceived: false, certificateUrl: null, onLinkedIn: false, notes: null, ...p,
})

describe('applyProgress', () => {
  it('sets completedAt only on the first completion', () => {
    const done = applyProgress(course, progress({ hoursDone: 5, status: 'Completed' }), now)
    const again = applyProgress(done, progress({ hoursDone: 5, status: 'Completed' }), later)

    expect(again.completedAt).toBe(now.toISOString())
  })

  it('clears completedAt when leaving completed', () => {
    const done = applyProgress(course, progress({ hoursDone: 5, status: 'Completed' }), now)

    expect(applyProgress(done, progress({ hoursDone: 4, status: 'InProgress' }), now).completedAt).toBeNull()
  })

  it('moves a not started course with hours to in progress', () => {
    expect(applyProgress(course, progress({ hoursDone: 1.5 }), now).status).toBe('InProgress')
  })

  it('drops the certificate url when the certificate was not received', () => {
    const updated = applyProgress(course, progress({ status: 'Completed', certificateUrl: 'https://cert' }), now)

    expect(updated.certificateUrl).toBeNull()
  })
})

describe('validateProgress', () => {
  it('accepts valid progress', () => {
    expect(validateProgress(progress({ hoursDone: 2, certificateUrl: 'https://cert.example.com' }))).toEqual([])
  })

  it('rejects negative hours and non http links', () => {
    const errors = validateProgress(progress({ hoursDone: -1, certificateUrl: 'javascript:alert(1)' }))

    expect(errors).toEqual([
      'As horas feitas não podem ser negativas.',
      'Informe um link começando com http:// ou https://.',
    ])
  })
})
