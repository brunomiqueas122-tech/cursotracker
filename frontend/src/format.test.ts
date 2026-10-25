import { formatDate, formatHours, groupByDay } from './format'
import type { Course } from './types'

const course = (id: number, day: number): Course => ({
  id,
  day,
  date: `2026-10-${24 + day}`,
  platform: 'Plataforma',
  name: `Curso ${id}`,
  area: 'Cloud',
  url: 'https://example.com',
  estimatedHours: 2,
  hoursDone: 0,
  status: 'NotStarted',
  completedAt: null,
  certificateReceived: false,
  certificateUrl: null,
  onLinkedIn: false,
  notes: null,
})

describe('groupByDay', () => {
  it('groups courses by day in ascending order keeping course order', () => {
    const groups = groupByDay([course(3, 2), course(1, 1), course(4, 2)])

    expect(groups.map((g) => g.day)).toEqual([1, 2])
    expect(groups[1].courses.map((c) => c.id)).toEqual([3, 4])
  })
})

describe('formatHours', () => {
  it('uses Brazilian decimal separator', () => {
    expect(formatHours(2.5)).toBe('2,5 h')
    expect(formatHours(5)).toBe('5 h')
  })
})

describe('formatDate', () => {
  it('keeps the calendar day regardless of timezone', () => {
    expect(formatDate('2026-10-25')).toContain('25/10')
  })
})
