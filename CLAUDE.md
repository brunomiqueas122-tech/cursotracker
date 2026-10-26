# CursoTracker

App para acompanhar cursos gratuitos com certificado. Cada pessoa tem conta; no primeiro login responde um perfil e a IA pesquisa cursos gratuitos com certificado gratuito para ela.

## Stack
- Backend: ASP.NET Core (.NET LTS atual), Minimal APIs, EF Core + PostgreSQL (Neon em produção, Docker local)
- Contas: ASP.NET Core Identity com cookie; cadastro exige código de convite (`Auth:InviteCode`)
- IA: SDK oficial da Anthropic para C#, `claude-opus-5-5` + web search, atrás de `ICourseFinder`
- Frontend: React + TypeScript + Vite, servido pela própria API (`wwwroot`)
- Testes: xUnit + Testcontainers/PostgreSQL (backend), Vitest (frontend)
- Deploy: Render (Docker, `render.yaml`) + Neon

## Arquitetura
- Vertical slices por feature (`Features/Auth`, `Features/Cursos`, `Features/Dashboard`, `Features/Profile`, `Features/Recommendations`), sem camadas genéricas vazias
- Domínio sem dependência de infraestrutura; validação na borda (FluentValidation)
- Sem repositório sobre EF Core; usar DbContext direto
- Abstração só quando houver segundo uso real (YAGNI)
- Todo dado de curso/perfil é filtrado pelo usuário logado

## Domínio
Course (por usuário): day, date, platform, name, area, url, estimatedHours, hoursDone, status (NotStarted|InProgress|Completed|Skipped), completedAt, certificateReceived, certificateUrl, onLinkedIn, notes. "Dia N" é derivado da data (data mais antiga = dia 1).
UserProfile: lookingFor, goal, level, language, hoursPerWeek + limite diário de buscas com IA.
Dashboard: % concluído, certificados, horas estimadas vs. feitas (total e por dia).

## Padrões
- C#: nullable on, records para DTOs, async ponta a ponta, CancellationToken
- TS: strict, sem `any`
- Nomes de código em inglês; textos de UI em português
- Commits pequenos, Conventional Commits

## Como responder
- Seja direto: sem introdução, sem resumo do que fez, sem repetir o pedido
- Mostre só o código alterado (diff ou trecho), nunca o arquivo inteiro sem necessidade
- Não explique o óbvio; explique só decisões não triviais, em 1 linha
- Uma pergunta por vez, apenas se bloquear o trabalho
- Antes de features grandes: plano curto em tópicos, depois implemente
- Rode build e testes antes de dizer que terminou
