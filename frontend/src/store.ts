import { api } from './api'
import type { Course, CourseDetails, DashboardSummary, UpdateProgressRequest } from './types'

export interface Backup {
  /** Gera o arquivo de backup e registra a data dele */
  exportJson(): string
  importJson(json: string): void
  lastBackupAt(): string | null
}

export interface CourseStore {
  courses(signal?: AbortSignal): Promise<Course[]>
  dashboard(signal?: AbortSignal): Promise<DashboardSummary>
  updateProgress(id: number, progress: UpdateProgressRequest): Promise<Course>
  createCourse(details: CourseDetails): Promise<Course>
  updateCourse(id: number, details: CourseDetails): Promise<Course>
  deleteCourse(id: number): Promise<void>
  /** Só existe quando os dados ficam no aparelho */
  backup?: Backup
}

export const httpStore: CourseStore = {
  courses: api.courses,
  dashboard: api.dashboard,
  updateProgress: api.updateProgress,
  createCourse: api.createCourse,
  updateCourse: api.updateCourse,
  deleteCourse: api.deleteCourse,
}
