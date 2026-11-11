import { encodeLegacyImport, readLegacyImport } from './legacyImport'

const backup = JSON.stringify({ version: 1, courses: [{ name: 'Lógica de programação', url: 'https://example.com/ç' }, { name: 'SQL' }] })

describe('legacy import link', () => {
  it('reads the courses sent by the old app, accents included', () => {
    const found = readLegacyImport(`#importar=${encodeLegacyImport(backup)}`)

    expect(found).toEqual({ json: backup, count: 2 })
  })

  it('ignores links without courses or with broken data', () => {
    expect(readLegacyImport('')).toBeNull()
    expect(readLegacyImport('#outra=1')).toBeNull()
    expect(readLegacyImport('#importar=%%%')).toBeNull()
    expect(readLegacyImport(`#importar=${encodeLegacyImport('{"courses":[]}')}`)).toBeNull()
  })
})
