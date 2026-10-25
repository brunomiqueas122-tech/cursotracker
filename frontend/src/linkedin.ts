import type { Course } from './types'

/**
 * Link "Adicionar ao perfil" do LinkedIn: abre o formulário de certificação já preenchido.
 * O LinkedIn não permite gravar no perfil via API para apps comuns, então a pessoa só confirma lá.
 */
export function linkedInCertificationUrl(course: Course, today = new Date()): string {
  const issued = course.completedAt ? new Date(course.completedAt) : today
  const params = new URLSearchParams({
    startTask: 'CERTIFICATION_NAME',
    name: course.name,
    organizationName: course.platform,
    issueYear: String(issued.getFullYear()),
    issueMonth: String(issued.getMonth() + 1),
  })
  if (course.certificateUrl) params.set('certUrl', course.certificateUrl)
  return `https://www.linkedin.com/profile/add?${params}`
}
