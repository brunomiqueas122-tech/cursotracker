import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { Course, UpdateProgressRequest } from '../types'
import { CourseCard } from './CourseCard'

const course: Course = {
  id: 7,
  day: 1,
  date: '2026-10-25',
  platform: 'Linux Foundation',
  name: 'Developing Secure Software (LFD121)',
  area: 'Segurança',
  url: 'https://example.com',
  estimatedHours: 5,
  hoursDone: 0,
  status: 'NotStarted',
  completedAt: null,
  certificateReceived: false,
  certificateUrl: null,
  onLinkedIn: false,
  notes: null,
}

describe('CourseCard', () => {
  it('saves edited hours', async () => {
    const onSave = vi.fn<(id: number, p: UpdateProgressRequest) => Promise<void>>().mockResolvedValue()
    render(<CourseCard course={course} onSave={onSave} />)

    const save = screen.getByRole('button', { name: 'Salvar' })
    expect(save).toBeDisabled()

    const hours = screen.getByLabelText('Horas feitas')
    await userEvent.clear(hours)
    await userEvent.type(hours, '2')
    await userEvent.click(save)

    expect(onSave).toHaveBeenCalledWith(7, expect.objectContaining({ hoursDone: 2, status: 'NotStarted' }))
  })

  it('shows certificate link field only after marking certificate as received', async () => {
    render(<CourseCard course={course} onSave={vi.fn()} />)

    expect(screen.queryByLabelText('Link do certificado')).not.toBeInTheDocument()
    await userEvent.click(screen.getByLabelText('Certificado recebido'))

    expect(screen.getByLabelText('Link do certificado')).toBeInTheDocument()
  })

  it('completes a course filling at least the estimated hours', async () => {
    const onSave = vi.fn<(id: number, p: UpdateProgressRequest) => Promise<void>>().mockResolvedValue()
    render(<CourseCard course={course} onSave={onSave} />)

    await userEvent.click(screen.getByRole('button', { name: 'Marcar como concluído' }))

    expect(onSave).toHaveBeenCalledWith(7, expect.objectContaining({ status: 'Completed', hoursDone: 5 }))
  })

  it('shows the server error message', async () => {
    const onSave = vi.fn().mockRejectedValue(new Error('As horas feitas não podem ser negativas.'))
    render(<CourseCard course={course} onSave={onSave} />)

    await userEvent.click(screen.getByRole('button', { name: 'Marcar como concluído' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('As horas feitas não podem ser negativas.')
  })

  it('offers to add a completed course to LinkedIn and marks it after confirmation', async () => {
    const onSave = vi.fn<(id: number, p: UpdateProgressRequest) => Promise<void>>().mockResolvedValue()
    const completed: Course = { ...course, status: 'Completed', hoursDone: 5, completedAt: '2026-10-25T20:00:00Z' }
    render(<CourseCard course={completed} onSave={onSave} />)

    const link = screen.getByRole('link', { name: 'Adicionar ao LinkedIn' })
    expect(link).toHaveAttribute('href', expect.stringContaining('linkedin.com/profile/add'))
    expect(screen.queryByRole('button', { name: 'Já adicionei' })).not.toBeInTheDocument()

    link.addEventListener('click', (e) => e.preventDefault())
    await userEvent.click(link)
    await userEvent.click(screen.getByRole('button', { name: 'Já adicionei' }))

    expect(onSave).toHaveBeenCalledWith(7, expect.objectContaining({ onLinkedIn: true, status: 'Completed' }))
  })

  it('hides the LinkedIn prompt when the course is not completed or already there', () => {
    const { rerender } = render(<CourseCard course={course} onSave={vi.fn()} />)
    expect(screen.queryByText('Adicionar ao LinkedIn')).not.toBeInTheDocument()

    rerender(<CourseCard course={{ ...course, status: 'Completed', onLinkedIn: true }} onSave={vi.fn()} />)
    expect(screen.queryByText('Adicionar ao LinkedIn')).not.toBeInTheDocument()
  })
})
