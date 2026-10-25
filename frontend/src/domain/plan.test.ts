import type { Course, CourseDetails } from '../types'
import { renumber, validateDetails } from './plan'

const on = (id: number, date: string): Course => ({
  id, day: 0, date, platform: 'P', name: `Curso ${id}`, area: 'Cloud', url: 'https://example.com', estimatedHours: 2,
  hoursDone: 0, status: 'NotStarted', completedAt: null, certificateReceived: false, certificateUrl: null,
  onLinkedIn: false, notes: null,
})

describe('renumber', () => {
  it('makes the earliest date day one and keeps gaps', () => {
    const days = renumber([on(1, '2026-10-27'), on(2, '2026-10-25'), on(3, '2026-10-25'), on(4, '2026-11-02')]).map((c) => c.day)

    expect(days).toEqual([3, 1, 1, 9])
  })

  it('handles month boundaries', () => {
    expect(renumber([on(1, '2026-10-31'), on(2, '2026-11-01'), on(3, '2027-02-15')]).map((c) => c.day)).toEqual([1, 2, 108])
  })
})

describe('validateDetails', () => {
  const valid: CourseDetails = { date: '2026-11-02', platform: 'Udemy', name: 'Docker', area: 'DevOps', url: 'https://u.dev', estimatedHours: 3 }

  it('accepts valid details', () => {
    expect(validateDetails(valid)).toEqual([])
  })

  it('reports every missing field in Portuguese', () => {
    expect(validateDetails({ date: '', platform: '', name: '', area: '', url: 'ftp://x', estimatedHours: 0 })).toEqual([
      'Informe a data.',
      'Informe o nome do curso.',
      'Informe a plataforma.',
      'Informe a área.',
      'Informe um link começando com http:// ou https://.',
      'As horas estimadas devem ser maiores que zero.',
    ])
  })
})
