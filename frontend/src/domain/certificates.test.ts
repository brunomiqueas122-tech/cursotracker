import type { Course } from '../types'
import { portfolioCertificates } from './certificates'

const course = (p: Partial<Course>): Course => ({
  id: 1, day: 1, date: '2026-10-25', platform: 'Linux Foundation', name: 'Curso', area: 'Segurança',
  url: 'https://example.com', estimatedHours: 2, hoursDone: 2, status: 'Completed', completedAt: '2026-10-26T20:00:00Z',
  certificateReceived: true, certificateUrl: 'https://cert.example.com/1', onLinkedIn: false, notes: 'privado', ...p,
})

describe('portfolioCertificates', () => {
  it('exports only completed courses with certificate, newest first, without private notes', () => {
    const result = portfolioCertificates([
      course({ name: 'Antigo', completedAt: '2026-10-26T20:00:00Z' }),
      course({ name: 'Sem certificado', certificateReceived: false }),
      course({ name: 'Em andamento', status: 'InProgress', completedAt: null }),
      course({ name: 'Novo', completedAt: '2026-10-30T09:00:00Z', certificateUrl: null }),
    ])

    expect(result).toEqual([
      { name: 'Novo', platform: 'Linux Foundation', area: 'Segurança', completedAt: '2026-10-30', certificateUrl: null },
      { name: 'Antigo', platform: 'Linux Foundation', area: 'Segurança', completedAt: '2026-10-26', certificateUrl: 'https://cert.example.com/1' },
    ])
  })
})
