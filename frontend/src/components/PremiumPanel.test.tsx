import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { PremiumCode, PremiumStatus } from '../types'
import { PremiumPanel } from './PremiumPanel'

const free: PremiumStatus = { isPremium: false, premiumUntil: null, forever: false, contact: 'WhatsApp (11) 90000-0000' }

function setup(p: Partial<Parameters<typeof PremiumPanel>[0]> = {}) {
  const props = {
    status: vi.fn<() => Promise<PremiumStatus>>().mockResolvedValue(free),
    redeem: vi.fn<(code: string) => Promise<PremiumStatus>>().mockResolvedValue({ ...free, isPremium: true, forever: true }),
    onActivated: vi.fn(),
    onBack: vi.fn(),
    ...p,
  }
  render(<PremiumPanel {...props} />)
  return props
}

describe('PremiumPanel', () => {
  it('shows how to subscribe and activates with a code', async () => {
    const { redeem, onActivated } = setup()

    expect(await screen.findByText('WhatsApp (11) 90000-0000')).toBeInTheDocument()
    await userEvent.type(screen.getByLabelText('Código de ativação'), 'abcd-efgh-jkmn')
    await userEvent.click(screen.getByRole('button', { name: 'Ativar Premium' }))

    expect(redeem).toHaveBeenCalledWith('abcd-efgh-jkmn')
    expect(await screen.findByText('Premium ativado! Todos os recursos já estão liberados.')).toBeInTheDocument()
    expect(screen.getByText('Premium para sempre.')).toBeInTheDocument()
    expect(onActivated).toHaveBeenCalledOnce()
  })

  it('shows the error for an invalid code', async () => {
    setup({ redeem: vi.fn().mockRejectedValue(new Error('Código inválido ou já usado.')) })

    await userEvent.type(screen.getByLabelText('Código de ativação'), 'errado')
    await userEvent.click(screen.getByRole('button', { name: 'Ativar Premium' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Código inválido ou já usado.')
  })

  it('lets the admin generate codes and see who used them', async () => {
    const used: PremiumCode = {
      id: 1, hint: 'ABCD', days: 30, createdAt: '2026-10-30T10:00:00Z', redeemedByEmail: 'ana@example.com', redeemedAt: '2026-10-30T12:00:00Z',
    }
    const create = vi.fn().mockResolvedValue({ code: 'WXYZ-2345-6789', days: null })
    setup({ admin: { list: vi.fn().mockResolvedValue([used]), create } })

    expect(await screen.findByText(/usado por ana@example.com/)).toBeInTheDocument()
    await userEvent.selectOptions(screen.getByLabelText('Duração'), 'Para sempre')
    await userEvent.click(screen.getByRole('button', { name: 'Gerar código' }))

    expect(create).toHaveBeenCalledWith(null)
    expect(await screen.findByText('WXYZ-2345-6789')).toBeInTheDocument()
  })

  it('hides the admin area from regular users', () => {
    setup()
    expect(screen.queryByRole('button', { name: 'Gerar código' })).not.toBeInTheDocument()
  })
})
