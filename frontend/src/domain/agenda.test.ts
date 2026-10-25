import type { Course, CourseStatus } from '../types'
import { agenda, toIsoDate } from './agenda'

const course = (id: number, date: string, status: CourseStatus = 'NotStarted'): Course => ({
  id, day: 1, date, platform: 'P', name: `Curso ${id}`, area: 'Cloud', url: 'https://example.com', estimatedHours: 2,
  hoursDone: 0, status, completedAt: null, certificateReceived: false, certificateUrl: null, onLinkedIn: false, notes: null,
})

describe('agenda', () => {
  const courses = [
    course(1, '2026-10-25'),
    course(2, '2026-10-26', 'Completed'),
    course(3, '2026-10-26', 'Skipped'),
    course(4, '2026-10-26', 'InProgress'),
    course(5, '2026-10-27'),
    course(6, '2026-10-27', 'Completed'),
    course(7, '2026-10-28'),
  ]

  it('lists pending courses for today and pending courses from past days', () => {
    const { today, overdue } = agenda(courses, '2026-10-27')

    expect(today.map((c) => c.id)).toEqual([5])
    expect(overdue.map((c) => c.id)).toEqual([1, 4])
  })
})

describe('toIsoDate', () => {
  it('uses the local calendar date', () => {
    expect(toIsoDate(new Date(2026, 9, 5, 23, 30))).toBe('2026-10-05')
  })
})
