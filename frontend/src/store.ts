import { api } from './api'
import type { Course, CourseDetails, DashboardSummary, UpdateProgressRequest } from './types'

/** Operações do plano usadas pela tela; nos testes entra uma versão em memória. */
export interface CourseStore {
  courses(signal?: AbortSignal): Promise<Course[]>
  dashboard(signal?: AbortSignal): Promise<DashboardSummary>
  updateProgress(id: number, progress: UpdateProgressRequest): Promise<Course>
  createCourse(details: CourseDetails): Promise<Course>
  updateCourse(id: number, details: CourseDetails): Promise<Course>
  deleteCourse(id: number): Promise<void>
}

export const httpStore: CourseStore = {
  courses: api.courses,
  dashboard: api.dashboard,
  updateProgress: api.updateProgress,
  createCourse: api.createCourse,
  updateCourse: api.updateCourse,
  deleteCourse: api.deleteCourse,
}
