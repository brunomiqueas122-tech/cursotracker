import type { ReactNode } from 'react'

interface Props {
  onUpgrade: () => void
  children: ReactNode
  className?: string
}

/** Recurso do Premium: mostra o que existe e leva para a assinatura ao clicar */
export function PremiumLock({ onUpgrade, children, className = '' }: Props) {
  return (
    <button type="button" className={`premium-lock ${className}`.trim()} onClick={onUpgrade} title="Disponível no Premium">
      <span aria-hidden="true">🔒</span> {children}
      <span className="premium-tag">Premium</span>
    </button>
  )
}
