import { linkedInCertificationUrl, linkedInPostText, linkedInShareUrl } from './linkedin'
import type { Course } from './types'

const course: Course = {
  id: 1, day: 1, date: '2026-10-25', platform: 'Linux Foundation', name: 'Developing Secure Software (LFD121)',
  area: 'Segurança', url: 'https://example.com', estimatedHours: 5, hoursDone: 5, status: 'Completed',
  completedAt: '2026-11-03T12:00:00Z', certificateReceived: true, certificateUrl: 'https://cert.example.com/123',
  onLinkedIn: false, notes: null,
}

describe('linkedInCertificationUrl', () => {
  it('prefills the certification form with name, issuer, date and certificate link', () => {
    const url = new URL(linkedInCertificationUrl(course))

    expect(url.origin + url.pathname).toBe('https://www.linkedin.com/profile/add')
    expect(Object.fromEntries(url.searchParams)).toEqual({
      startTask: 'CERTIFICATION_NAME',
      name: 'Developing Secure Software (LFD121)',
      organizationName: 'Linux Foundation',
      issueYear: '2026',
      issueMonth: '11',
      certUrl: 'https://cert.example.com/123',
    })
  })

  it('uses today when there is no completion date and omits a missing certificate link', () => {
    const url = new URL(linkedInCertificationUrl({ ...course, completedAt: null, certificateUrl: null }, new Date(2026, 0, 15)))

    expect(url.searchParams.get('issueMonth')).toBe('1')
    expect(url.searchParams.has('certUrl')).toBe(false)
  })
})

describe('linkedInPostText', () => {
  it('announces the course with certificate link and hashtags from the area', () => {
    expect(linkedInPostText({ ...course, area: 'Cloud / .NET' })).toBe(
      'Concluí o curso "Developing Secure Software (LFD121)", pela Linux Foundation! 🎓\n\n' +
        'Certificado: https://cert.example.com/123\n\n' +
        '#Cloud #NET #certificação #aprendizadoContínuo',
    )
  })

  it('skips the certificate line when there is no link', () => {
    expect(linkedInPostText({ ...course, certificateUrl: null })).not.toContain('Certificado')
  })

  it('opens the LinkedIn composer with the text', () => {
    const url = new URL(linkedInShareUrl(course))

    expect(url.searchParams.get('shareActive')).toBe('true')
    expect(url.searchParams.get('text')).toBe(linkedInPostText(course))
  })
})
