# CursoTracker

Acompanhe cursos gratuitos com certificado. Crie sua conta, conte o que você procura e a IA pesquisa na web cursos **gratuitos com certificado gratuito** para o seu perfil.

## Funcionalidades

- Contas (e-mail e senha) com código de convite
- Onboarding: perfil com os assuntos que você procura, objetivo, nível, idioma e horas por semana
- Plano grátis e **Premium** (ativado por código de uso único gerado pelo administrador)
- Descobrir cursos com IA (Gemini + busca do Google, cota grátis), 1 busca por dia salva no banco e reaproveitada
- Plano por dia com "Para hoje" e "Atrasados"; criar, editar, reagendar e excluir cursos
- Cronômetro de estudo, dashboard de progresso, lembretes no calendário (.ics)
- LinkedIn: formulário de certificação pré-preenchido e post pronto ao concluir
- Exporta `certificados.json` para o portfólio
- Importa o backup da versão antiga (só no celular)

## Rodar localmente

```bash
docker compose up -d                 # PostgreSQL local

cd backend/CursoTracker.Api          # API em http://localhost:5080 (código de convite: dev)
Gemini__ApiKey=... dotnet run

cd frontend && npm install && npm run dev   # app em http://localhost:5173
```

Sem `Gemini__ApiKey` tudo funciona, menos a busca com IA (que responde um erro amigável).

### Banco

As migrations são aplicadas sozinhas ao iniciar a API. Para criar uma nova depois de mudar o modelo:

```bash
dotnet tool restore
cd backend/CursoTracker.Api
ConnectionStrings__Default="Host=localhost;Database=design" Auth__InviteCode=x \
  dotnet ef migrations add NomeDaMudanca --output-dir Data/Migrations
```

## Testes

```bash
cd backend && dotnet test      # precisa do Docker (sobe um PostgreSQL de teste)
cd frontend && npm test
```

## Deploy (Render + Neon, plano grátis)

1. Crie um banco no [Neon](https://neon.tech) e copie a URL de conexão (`postgresql://...`).
2. Crie uma chave grátis no [Google AI Studio](https://aistudio.google.com/apikey).
3. No [Render](https://render.com): **New → Blueprint** e escolha este repositório. Ele lê o `render.yaml` e pede:
   - `ConnectionStrings__Default`: a URL do Neon
   - `Gemini__ApiKey`: a chave do Google AI Studio
   - `Auth__InviteCode`: o código que você passa para quem pode criar conta

Cada push na `main` publica de novo. No plano grátis o servidor "dorme" após 15 min sem uso; a primeira abertura depois disso leva cerca de 1 minuto.

## Configuração

| Variável | Para que serve | Padrão |
|---|---|---|
| `ConnectionStrings__Default` | PostgreSQL (formato Npgsql ou URL `postgresql://`) | obrigatória |
| `Auth__InviteCode` | Código exigido no cadastro | obrigatória |
| `Gemini__ApiKey` | Busca de cursos com IA (Gemini) | — |
| `Ai__Provider` | `Gemini` (grátis) ou `Claude` (pago, usa `ANTHROPIC_API_KEY`) | `Gemini` |
| `Ai__GeminiModel` | Modelo do Gemini | `gemini-flash-latest` |
| `Ai__ClaudeModel` | Modelo do Claude | `claude-opus-5-5` |
| `Ai__DailySearchLimit` | Buscas com IA por usuário por dia | `1` |
| `Ai__MaxWebSearches` | Pesquisas na web por busca (Claude) | `6` |
| `Premium__AdminEmails` | E-mails (separados por vírgula) que geram códigos premium | — |
| `Premium__Contact` | Contato mostrado na tela do Premium | — |
| `Premium__FreeCoursesPerMonth` | Cursos liberados por mês no plano grátis | `3` |
| `Premium__FreeTopics` | Assuntos no perfil no plano grátis | `2` |

## Premium

| | Grátis | Premium |
|---|---|---|
| Cursos encontrados pela IA | 3 liberados por mês, os outros com cadeado | Todos |
| Assuntos no perfil | Até 2 | Ilimitados |
| Cronograma (por dia, "Para hoje", atrasados, reagendar) | — | Sim |
| Lembretes no calendário, LinkedIn, exportar certificados | — | Sim |

Para ativar: entre com o e-mail de `Premium__AdminEmails`, abra **Seja Premium** e gere um código (30 dias, 1 ano ou para sempre). A pessoa digita o código na mesma tela. O limite de cursos e de assuntos é conferido no servidor; os outros recursos são da tela.

## API

| Método | Rota | |
|---|---|---|
| POST | `/api/auth/register` · `/login` · `/logout` | Conta (cookie) |
| GET | `/api/auth/me` | Usuário logado, se já tem perfil, se é Premium/admin |
| GET/PUT | `/api/profile` | Perfil usado pela IA |
| POST | `/api/recommendations` | Busca cursos com IA (`{ refinement? }`) e salva o resultado |
| GET | `/api/recommendations/latest` | Última busca salva, sem os cursos já no plano |
| GET/POST | `/api/courses` | Lista / cria curso |
| PUT/DELETE | `/api/courses/{id}` | Edita ou reagenda / remove |
| PUT | `/api/courses/{id}/progress` | Horas, status, certificado, LinkedIn, anotações |
| POST | `/api/courses/import` | Importa o backup da versão antiga |
| GET | `/api/dashboard` | % concluído, certificados, horas |
| GET | `/api/premium` | Situação do Premium e contato |
| POST | `/api/premium/redeem` | Ativa o Premium com um código |
| GET/POST | `/api/premium/codes` | Lista / gera códigos (só administrador) |
