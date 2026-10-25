/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** "local" guarda os dados no aparelho; ausente usa a API .NET */
  readonly VITE_STORAGE?: 'local'
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
