import type { Course } from '../types'

/** Formato lido pela seção "Certificações" do portfólio (certificados.json). */
export interface PortfolioCertificate {
  name: string
  platform: string
  area: string
  completedAt: string | null
  certificateUrl: string | null
}

export function portfolioCertificates(courses: Course[]): PortfolioCertificate[] {
  return courses
    .filter((c) => c.status === 'Completed' && c.certificateReceived)
    .map((c) => ({
      name: c.name,
      platform: c.platform,
      area: c.area,
      completedAt: c.completedAt?.slice(0, 10) ?? null,
      certificateUrl: c.certificateUrl,
    }))
    .sort((a, b) => (b.completedAt ?? '').localeCompare(a.completedAt ?? ''))
}
