/**
 * Cursos vindos do app antigo do celular (GitHub Pages). Lá eles ficam no navegador, em outro endereço,
 * então a página de mudança os envia no fragmento do link (#importar=...), que não vai para nenhum servidor.
 */
export interface LegacyImport {
  json: string
  count: number
}

const PARAM = 'importar'
const SESSION_KEY = 'cursotracker.legacyImport'

function decode(value: string): string {
  const bytes = Uint8Array.from(atob(value.replace(/-/g, '+').replace(/_/g, '/')), (c) => c.charCodeAt(0))
  return new TextDecoder().decode(bytes)
}

export function encodeLegacyImport(json: string): string {
  let binary = ''
  for (const byte of new TextEncoder().encode(json)) binary += String.fromCharCode(byte)
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
}

function parse(json: string): LegacyImport | null {
  try {
    const courses = (JSON.parse(json) as { courses?: unknown } | null)?.courses
    return Array.isArray(courses) && courses.length > 0 ? { json, count: courses.length } : null
  } catch {
    return null
  }
}

export function readLegacyImport(hash: string): LegacyImport | null {
  const value = new URLSearchParams(hash.replace(/^#/, '')).get(PARAM)
  if (!value) return null
  try {
    return parse(decode(value))
  } catch {
    return null
  }
}

/**
 * Pega os cursos do link (ou os guardados na aba, se a pessoa recarregou antes de entrar) e limpa o link,
 * para não ficarem no histórico nem serem importados de novo ao recarregar.
 */
export function takeLegacyImport(): LegacyImport | null {
  const fromLink = readLegacyImport(window.location.hash)
  if (fromLink) {
    window.history.replaceState(null, '', window.location.pathname + window.location.search)
    try {
      sessionStorage.setItem(SESSION_KEY, fromLink.json)
    } catch {
      // sem sessionStorage a importação ainda funciona nesta abertura
    }
    return fromLink
  }
  try {
    const saved = sessionStorage.getItem(SESSION_KEY)
    return saved ? parse(saved) : null
  } catch {
    return null
  }
}

export function forgetLegacyImport() {
  try {
    sessionStorage.removeItem(SESSION_KEY)
  } catch {
    // nada a limpar
  }
}
