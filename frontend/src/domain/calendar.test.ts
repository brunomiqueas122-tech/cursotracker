import type { Course, CourseStatus } from '../types'
import { buildCalendar, foldLine } from './calendar'

const course = (id: number, date: string, status: CourseStatus = 'NotStarted', name = `Curso ${id}`): Course => ({
  id, day: 1, date, platform: 'Linux Foundation', name, area: 'DevOps', url: 'https://example.com/curso',
  estimatedHours: 2.5, hoursDone: 0, status, completedAt: null, certificateReceived: false, certificateUrl: null,
  onLinkedIn: false, notes: null,
})

const now = new Date('2026-10-26T12:34:56.789Z')
const unfold = (ics: string) => ics.replace(/\r\n /g, '')

describe('buildCalendar', () => {
  it('creates one all-day event with a 9am alarm per pending course', () => {
    const ics = unfold(
      buildCalendar([course(1, '2026-10-31'), course(2, '2026-11-01', 'Completed'), course(3, '2026-11-02', 'Skipped')], now),
    )

    expect(ics.match(/BEGIN:VEVENT/g)).toHaveLength(1)
    expect(ics).toContain('UID:cursotracker-1@brunomiqueas122-tech.github.io')
    expect(ics).toContain('DTSTAMP:20261026T123456Z')
    expect(ics).toContain('DTSTART;VALUE=DATE:20261031')
    expect(ics).toContain('DTEND;VALUE=DATE:20261101')
    expect(ics).toContain('TRIGGER:PT9H')
    expect(ics).toContain('DESCRIPTION:Linux Foundation · 2\\,5 h estimadas\\nhttps://example.com/curso')
  })

  it('escapes special characters and uses CRLF line endings', () => {
    const ics = buildCalendar([course(1, '2026-10-31', 'InProgress', 'Docker; Kubernetes, e mais')], now)

    expect(unfold(ics)).toContain('SUMMARY:Estudar: Docker\\; Kubernetes\\, e mais')
    expect(ics.startsWith('BEGIN:VCALENDAR\r\n')).toBe(true)
    expect(ics.endsWith('END:VCALENDAR\r\n')).toBe(true)
  })
})

describe('foldLine', () => {
  it('keeps every physical line within 75 bytes without splitting characters', () => {
    const line = `SUMMARY:${'Segurança – módulo '.repeat(10)}`
    const folded = foldLine(line)

    for (const physical of folded.split('\r\n')) expect(new TextEncoder().encode(physical).length).toBeLessThanOrEqual(75)
    expect(folded.replace(/\r\n /g, '')).toBe(line)
  })
})
