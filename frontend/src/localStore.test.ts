import { createLocalStore } from './localStore'
import { MemoryStorage } from './test-utils'
import type { CourseDetails, UpdateProgressRequest } from './types'

const complete: UpdateProgressRequest = {
  hoursDone: 5, status: 'Completed', certificateReceived: true, certificateUrl: 'https://cert.example.com',
  onLinkedIn: false, notes: null,
}

describe('createLocalStore', () => {
  it('seeds the real course plan on first use', async () => {
    const store = createLocalStore({ storage: new MemoryStorage() })

    const courses = await store.courses()

    expect(courses).toHaveLength(11)
    expect(courses[0]).toMatchObject({ id: 1, day: 1, name: 'Developing Secure Software (LFD121) – módulos 1 a 4' })
    expect((await store.dashboard()).estimatedHours).toBe(35)
  })

  it('persists progress between sessions', async () => {
    const storage = new MemoryStorage()
    await createLocalStore({ storage }).updateProgress(1, complete)

    const reopened = createLocalStore({ storage })

    expect((await reopened.courses())[0].status).toBe('Completed')
    expect((await reopened.dashboard()).certificatesReceived).toBe(1)
  })

  it('rejects invalid progress without saving', async () => {
    const store = createLocalStore({ storage: new MemoryStorage() })

    await expect(store.updateProgress(1, { ...complete, hoursDone: -1 })).rejects.toThrow('negativas')
    expect((await store.courses())[0].hoursDone).toBe(0)
  })

  it('restores a backup exported from another device', async () => {
    const phone = createLocalStore({ storage: new MemoryStorage() })
    await phone.updateProgress(2, complete)
    const file = phone.backup!.exportJson()

    const newPhone = createLocalStore({ storage: new MemoryStorage() })
    newPhone.backup!.importJson(file)

    expect((await newPhone.courses())[1].status).toBe('Completed')
  })

  it('refuses an invalid backup file', () => {
    const store = createLocalStore({ storage: new MemoryStorage() })

    expect(() => store.backup!.importJson('{"courses":[{"oops":true}]}')).toThrow('Arquivo de backup inválido.')
  })

  const docker: CourseDetails = {
    date: '2026-11-02', platform: 'Udemy', name: 'Docker para iniciantes', area: 'DevOps',
    url: 'https://www.udemy.com/course/docker', estimatedHours: 3,
  }

  it('adds a course on the matching plan day', async () => {
    const store = createLocalStore({ storage: new MemoryStorage() })

    const created = await store.createCourse(docker)

    expect(created).toMatchObject({ id: 12, day: 9, status: 'NotStarted', hoursDone: 0 })
    expect(await store.courses()).toHaveLength(12)
  })

  it('reschedules a course and renumbers the plan', async () => {
    const store = createLocalStore({ storage: new MemoryStorage() })
    const course4 = (await store.courses()).find((c) => c.id === 4)!

    await store.updateCourse(4, { ...course4, date: '2026-10-24' })

    const courses = await store.courses()
    expect(courses[0]).toMatchObject({ id: 4, day: 1 })
    expect(courses.find((c) => c.id === 1)!.day).toBe(2)
  })

  it('deletes a course', async () => {
    const store = createLocalStore({ storage: new MemoryStorage() })

    await store.deleteCourse(11)

    expect((await store.courses()).map((c) => c.id)).not.toContain(11)
    await expect(store.deleteCourse(11)).rejects.toThrow('Curso não encontrado.')
  })

  it('rejects invalid course details', async () => {
    const store = createLocalStore({ storage: new MemoryStorage() })

    await expect(store.createCourse({ ...docker, name: ' ', estimatedHours: 0 })).rejects.toThrow(
      'Informe o nome do curso. As horas estimadas devem ser maiores que zero.',
    )
  })
})
