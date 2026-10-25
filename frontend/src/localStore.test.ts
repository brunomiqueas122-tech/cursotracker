import { createLocalStore } from './localStore'
import type { UpdateProgressRequest } from './types'

class MemoryStorage implements Storage {
  private data = new Map<string, string>()
  get length() { return this.data.size }
  clear() { this.data.clear() }
  getItem(key: string) { return this.data.get(key) ?? null }
  key(index: number) { return [...this.data.keys()][index] ?? null }
  removeItem(key: string) { this.data.delete(key) }
  setItem(key: string, value: string) { this.data.set(key, value) }
}

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
})
