import type { Course } from '../types'
import { formatHours } from '../format'

// iCalendar (RFC 5545): eventos de dia inteiro com alarme às 9h, um por curso pendente

const pending = (c: Course) => c.status === 'NotStarted' || c.status === 'InProgress'

const escapeText = (text: string) =>
  text.replace(/\\/g, '\\\\').replace(/;/g, '\\;').replace(/,/g, '\\,').replace(/\r?\n/g, '\\n')

const compactDate = (isoDate: string) => isoDate.replaceAll('-', '')

function nextDay(isoDate: string): string {
  const d = new Date(`${isoDate}T00:00:00Z`)
  d.setUTCDate(d.getUTCDate() + 1)
  return d.toISOString().slice(0, 10)
}

const utcStamp = (date: Date) => date.toISOString().replace(/[-:]/g, '').replace(/\.\d{3}/, '')

/** Linhas com mais de 75 bytes continuam na linha seguinte começando com espaço. */
export function foldLine(line: string): string {
  const encoder = new TextEncoder()
  const parts: string[] = []
  let current = ''
  for (const char of line) {
    const limit = parts.length === 0 ? 75 : 74 // a continuação já gasta 1 byte com o espaço
    if (encoder.encode(current + char).length > limit) {
      parts.push(current)
      current = char
    } else {
      current += char
    }
  }
  parts.push(current)
  return parts.join('\r\n ')
}

export function buildCalendar(courses: Course[], now: Date): string {
  const events = courses.filter(pending).flatMap((c) => [
    'BEGIN:VEVENT',
    `UID:cursotracker-${c.id}@brunomiqueas122-tech.github.io`,
    `DTSTAMP:${utcStamp(now)}`,
    `DTSTART;VALUE=DATE:${compactDate(c.date)}`,
    `DTEND;VALUE=DATE:${compactDate(nextDay(c.date))}`,
    `SUMMARY:${escapeText(`Estudar: ${c.name}`)}`,
    `DESCRIPTION:${escapeText(`${c.platform} · ${formatHours(c.estimatedHours)} estimadas\n${c.url}`)}`,
    `URL:${c.url}`,
    'BEGIN:VALARM',
    'ACTION:DISPLAY',
    `DESCRIPTION:${escapeText(c.name)}`,
    'TRIGGER:PT9H',
    'END:VALARM',
    'END:VEVENT',
  ])

  return [
    'BEGIN:VCALENDAR',
    'VERSION:2.0',
    'PRODID:-//CursoTracker//PT-BR',
    'CALSCALE:GREGORIAN',
    'METHOD:PUBLISH',
    'X-WR-CALNAME:CursoTracker',
    ...events,
    'END:VCALENDAR',
  ]
    .map(foldLine)
    .join('\r\n')
    .concat('\r\n')
}
