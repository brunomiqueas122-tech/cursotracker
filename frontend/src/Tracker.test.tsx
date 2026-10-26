import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createLocalStore } from './localStore'
import { MemoryStorage } from './test-utils'
import { Tracker } from './Tracker'

vi.mock('./download', () => ({ downloadFile: vi.fn() }))

const setup = () => render(<Tracker store={createLocalStore({ storage: new MemoryStorage() })} today="2026-10-27" />)

afterEach(() => vi.restoreAllMocks())

// Colar em vez de digitar: cada tecla re-renderiza a lista inteira e deixa o teste lento
async function fill(input: HTMLElement, text: string) {
  await userEvent.clear(input)
  await userEvent.click(input)
  await userEvent.paste(text)
}

describe('Tracker', () => {
  it('shows today and overdue courses and moves an overdue course to today', async () => {
    setup()
    const agenda = await screen.findByRole('region', { name: 'Agenda' })
    expect(within(agenda).getByText('AWS Cloud Practitioner Essentials')).toBeInTheDocument()
    expect(within(agenda).getAllByRole('button', { name: 'Mover para hoje' })).toHaveLength(3)

    await userEvent.click(within(agenda).getAllByRole('button', { name: 'Mover para hoje' })[0])

    await vi.waitFor(() => expect(within(agenda).getAllByRole('button', { name: 'Mover para hoje' })).toHaveLength(2))
    expect(screen.getByText('Hoje')).toBeInTheDocument()
  })

  it('adds a new course to the plan', async () => {
    setup()
    await userEvent.click(await screen.findByRole('button', { name: '+ Novo curso' }))
    const form = screen.getByRole('region', { name: 'Novo curso' })

    await fill(within(form).getByLabelText('Nome do curso'), 'Docker para iniciantes')
    await fill(within(form).getByLabelText('Plataforma'), 'Udemy')
    await fill(within(form).getByLabelText('Área'), 'DevOps')
    await fill(within(form).getByLabelText('Link do curso'), 'https://www.udemy.com/course/docker')
    await userEvent.click(within(form).getByRole('button', { name: 'Adicionar curso' }))

    // Entra com a data de hoje: aparece no card e em "Para hoje"
    const links = await screen.findAllByRole('link', { name: 'Docker para iniciantes' })
    expect(links.map((a) => a.getAttribute('href'))).toEqual(['#course-12', 'https://www.udemy.com/course/docker'])
    expect(screen.queryByRole('region', { name: 'Novo curso' })).not.toBeInTheDocument()
  })

  it('deletes a course after confirmation', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    setup()
    const name = 'Curso curto de Cloud ou IA (badge Credly)'
    const card = (await screen.findByRole('link', { name })).closest('article')!

    await userEvent.click(within(card).getByRole('button', { name: 'Excluir' }))

    await vi.waitFor(() => expect(screen.queryByRole('link', { name })).not.toBeInTheDocument())
  })

  it('edits a course name', async () => {
    setup()
    const card = (await screen.findByRole('link', { name: 'Intro to Machine Learning' })).closest('article')!

    await userEvent.click(within(card).getByRole('button', { name: 'Editar' }))
    const name = screen.getByDisplayValue('Intro to Machine Learning')
    await fill(name, 'Intermediate Machine Learning')
    await userEvent.click(screen.getByRole('button', { name: 'Salvar alterações' }))

    expect(await screen.findByRole('link', { name: 'Intermediate Machine Learning' })).toBeInTheDocument()
  })

  it('reminds to back up once there is progress and hides after downloading', async () => {
    const store = createLocalStore({ storage: new MemoryStorage() })
    await store.updateProgress(1, { hoursDone: 1, status: 'InProgress', certificateReceived: false, certificateUrl: null, onLinkedIn: false, notes: null })
    render(<Tracker store={store} today="2026-10-27" />)

    expect(await screen.findByText('Você ainda não fez nenhum backup do seu progresso.')).toBeInTheDocument()
    await userEvent.click(within(screen.getByRole('status')).getByRole('button', { name: 'Baixar backup' }))

    expect(screen.queryByText('Você ainda não fez nenhum backup do seu progresso.')).not.toBeInTheDocument()
  })
})
