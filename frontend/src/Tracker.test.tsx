import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { course, createFakeStore } from './test-utils'
import { Tracker } from './Tracker'

vi.mock('./download', () => ({ downloadFile: vi.fn() }))

const plan = () => [
  course(1, '2026-10-25', { day: 1, name: 'LFD121 – módulos 1 a 4' }),
  course(2, '2026-10-26', { day: 2, name: 'LFD121 – final', status: 'InProgress', hoursDone: 1 }),
  course(3, '2026-10-26', { day: 2, name: 'Applied Skills', status: 'Completed', hoursDone: 2 }),
  course(4, '2026-10-27', { day: 3, name: 'AWS Cloud Practitioner Essentials' }),
  course(5, '2026-10-30', { day: 6, name: 'Intro to Machine Learning' }),
]

const setup = (props: Partial<Parameters<typeof Tracker>[0]> = {}) =>
  render(<Tracker store={createFakeStore(plan())} today="2026-10-27" {...props} />)

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
    expect(within(agenda).getAllByRole('button', { name: 'Mover para hoje' })).toHaveLength(2)

    await userEvent.click(within(agenda).getAllByRole('button', { name: 'Mover para hoje' })[0])

    await vi.waitFor(() => expect(within(agenda).getAllByRole('button', { name: 'Mover para hoje' })).toHaveLength(1))
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
    expect(links.map((a) => a.getAttribute('href'))).toEqual(['#course-6', 'https://www.udemy.com/course/docker'])
    expect(screen.queryByRole('region', { name: 'Novo curso' })).not.toBeInTheDocument()
  })

  it('deletes a course after confirmation', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    setup()
    const card = (await screen.findByRole('link', { name: 'Intro to Machine Learning' })).closest('article')!

    await userEvent.click(within(card).getByRole('button', { name: 'Excluir' }))

    await vi.waitFor(() => expect(screen.queryByRole('link', { name: 'Intro to Machine Learning' })).not.toBeInTheDocument())
  })

  it('edits a course name', async () => {
    setup()
    const card = (await screen.findByRole('link', { name: 'Intro to Machine Learning' })).closest('article')!

    await userEvent.click(within(card).getByRole('button', { name: 'Editar' }))
    await fill(screen.getByDisplayValue('Intro to Machine Learning'), 'Intermediate Machine Learning')
    await userEvent.click(screen.getByRole('button', { name: 'Salvar alterações' }))

    expect(await screen.findByRole('link', { name: 'Intermediate Machine Learning' })).toBeInTheDocument()
  })

  it('invites to discover courses when the plan is empty', async () => {
    const onDiscover = vi.fn()
    render(<Tracker store={createFakeStore([])} today="2026-10-27" onDiscover={onDiscover} />)

    expect(await screen.findByText('Seu plano ainda está vazio')).toBeInTheDocument()
    await userEvent.click(screen.getAllByRole('button', { name: 'Descobrir cursos com IA' })[1])

    expect(onDiscover).toHaveBeenCalled()
  })

  it('imports the old phone backup', async () => {
    const importCourses = vi.fn().mockResolvedValue({ imported: 2 })
    setup({ importCourses })

    const file = new File(['{"courses":[]}'], 'backup.json', { type: 'application/json' })
    await userEvent.upload(await screen.findByLabelText('Escolher arquivo de backup'), file)

    expect(importCourses).toHaveBeenCalledWith('{"courses":[]}')
    expect(await screen.findByText('2 curso(s) importado(s) para sua conta.')).toBeInTheDocument()
  })

  it('locks the schedule, calendar and certificates on the free plan', async () => {
    const onUpgrade = vi.fn()
    setup({ free: true, onUpgrade, premiumLabel: 'Seja Premium' })

    expect(await screen.findByText('AWS Cloud Practitioner Essentials')).toBeInTheDocument()
    expect(screen.queryByRole('region', { name: 'Agenda' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Mover para hoje' })).not.toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: /Cronograma/ }))
    await userEvent.click(screen.getByRole('button', { name: /Adicionar ao calendário/ }))
    await userEvent.click(screen.getByRole('button', { name: /Exportar certificados/ }))
    await userEvent.click(screen.getByRole('button', { name: 'Seja Premium' }))
    expect(onUpgrade).toHaveBeenCalledTimes(4)

    await userEvent.click(screen.getByRole('button', { name: '+ Novo curso' }))
    expect(screen.queryByLabelText('Data')).not.toBeInTheDocument()
  })
})
