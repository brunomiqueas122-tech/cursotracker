import type { Course, CourseStatus } from '../types'
import { summarize } from './dashboard'

let nextId = 1
const course = (day: number, estimated: number, done: number, status: CourseStatus, cert = false): Course => ({
  id: nextId++, day, date: `2026-10-${24 + day}`, platform: 'P', name: `Curso ${day}`, area: 'Cloud',
  url: 'https://example.com', estimatedHours: estimated, hoursDone: done, status, completedAt: null,
  certificateReceived: cert, certificateUrl: null, onLinkedIn: false, notes: null,
})

describe('summarize', () => {
  it('leaves skipped courses out of the goal', () => {
    const s = summarize([
      course(1, 5, 5, 'Completed', true),
      course(1, 3, 1, 'InProgress'),
      course(2, 2, 0, 'Skipped'),
    ])

    expect(s.completedPercent).toBe(50)
    expect(s.estimatedHours).toBe(8)
    expect(s.hoursDone).toBe(6)
    expect(s.skippedCourses).toBe(1)
  })

  it('counts completed courses without certificate as pending', () => {
    const s = summarize([course(1, 5, 5, 'Completed', true), course(2, 3, 3, 'Completed')])

    expect(s.certificatesReceived).toBe(1)
    expect(s.certificatesPending).toBe(1)
  })

  it('groups hours by day in order', () => {
    const days = summarize([
      course(2, 2, 1, 'InProgress'),
      course(1, 5, 5, 'Completed'),
      course(2, 3, 0, 'NotStarted'),
    ]).days

    expect(days.map((d) => d.day)).toEqual([1, 2])
    expect(days[1]).toEqual({ day: 2, date: '2026-10-26', courses: 2, completed: 0, estimatedHours: 5, hoursDone: 1 })
  })

  it('rounds the percentage to one decimal like the api', () => {
    const courses = Array.from({ length: 11 }, (_, i) => course(1, 1, 0, i === 0 ? 'Completed' : 'NotStarted'))

    expect(summarize(courses).completedPercent).toBe(9.1)
  })
})
