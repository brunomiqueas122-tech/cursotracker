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

export const learningGoals = ['GetJob', 'GrowInCurrentJob', 'ChangeCareer', 'Curiosity'] as const
export type LearningGoal = (typeof learningGoals)[number]
export const skillLevels = ['Beginner', 'Intermediate', 'Advanced'] as const
export type SkillLevel = (typeof skillLevels)[number]
export const courseLanguages = ['Portuguese', 'English', 'Any'] as const
export type CourseLanguage = (typeof courseLanguages)[number]

export interface ProfileAnswers {
  lookingFor: string
  goal: LearningGoal
  level: SkillLevel
  language: CourseLanguage
  hoursPerWeek: number
}

export interface Profile extends ProfileAnswers {
  remainingSearchesToday: number
}

export interface Me {
  email: string
  hasProfile: boolean
}

export interface CourseSuggestion {
  name: string
  platform: string
  url: string
  area: string
  estimatedHours: number
  language: string
  certificate: string
  reason: string
}

export interface Recommendations {
  courses: CourseSuggestion[]
  note: string | null
  remainingSearchesToday: number
}
