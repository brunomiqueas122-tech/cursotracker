export const courseStatuses = ['NotStarted', 'InProgress', 'Completed', 'Skipped'] as const
export type CourseStatus = (typeof courseStatuses)[number]

export interface Course {
  id: number
  day: number
  date: string
  platform: string
  name: string
  area: string
  url: string
  estimatedHours: number
  hoursDone: number
  status: CourseStatus
  completedAt: string | null
  certificateReceived: boolean
  certificateUrl: string | null
  onLinkedIn: boolean
  notes: string | null
}

export type UpdateProgressRequest = Pick<
  Course,
  'hoursDone' | 'status' | 'certificateReceived' | 'certificateUrl' | 'onLinkedIn' | 'notes'
>

export interface DaySummary {
  day: number
  date: string
  courses: number
  completed: number
  estimatedHours: number
  hoursDone: number
}

export interface DashboardSummary {
  totalCourses: number
  completedCourses: number
  skippedCourses: number
  completedPercent: number
  certificatesReceived: number
  certificatesPending: number
  onLinkedIn: number
  estimatedHours: number
  hoursDone: number
  days: DaySummary[]
}

export type CourseDetails = Pick<Course, 'date' | 'platform' | 'name' | 'area' | 'url' | 'estimatedHours'>
