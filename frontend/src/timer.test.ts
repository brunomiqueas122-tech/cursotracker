import { MemoryStorage } from './test-utils'
import { elapsedHours, formatElapsed, loadTimer, saveTimer } from './timer'

describe('timer', () => {
  it('survives closing the app', () => {
    const storage = new MemoryStorage()
    saveTimer({ courseId: 3, startedAt: '2026-10-27T10:00:00.000Z' }, storage)

    expect(loadTimer(storage)).toEqual({ courseId: 3, startedAt: '2026-10-27T10:00:00.000Z' })

    saveTimer(null, storage)
    expect(loadTimer(storage)).toBeNull()
  })

  it('ignores corrupted data', () => {
    const storage = new MemoryStorage()
    storage.setItem('cursotracker.timer', '{"courseId":"x"}')

    expect(loadTimer(storage)).toBeNull()
  })

  it('converts elapsed time to hours with two decimals', () => {
    expect(elapsedHours('2026-10-27T10:00:00Z', new Date('2026-10-27T11:20:00Z'))).toBe(1.33)
  })

  it('formats the running clock', () => {
    expect(formatElapsed(3_909_000)).toBe('1:05:09')
  })
})
