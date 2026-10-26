import type { Course } from '../types'
import { nextPlanDates } from './schedule'

const on = (date: string) => ({ date }) as Course

describe('nextPlanDates', () => {
  it('starts today when the plan is empty or already over', () => {
    expect(nextPlanDates([], '2026-10-27', 2)).toEqual(['2026-10-27', '2026-10-28'])
    expect(nextPlanDates([on('2026-10-20')], '2026-10-27', 1)).toEqual(['2026-10-27'])
  })

  it('continues after the last planned day, one course per day', () => {
    expect(nextPlanDates([on('2026-10-31'), on('2026-10-25')], '2026-10-27', 3)).toEqual(['2026-11-01', '2026-11-02', '2026-11-03'])
  })
})
