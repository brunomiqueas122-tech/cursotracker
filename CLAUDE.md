# CursoTracker

App para acompanhar cursos gratuitos com certificado. Dados iniciais: `seed-courses.json` (usar como seed do banco na primeira execução).

## Stack
- Backend: ASP.NET Core (.NET LTS atual), Minimal APIs, EF Core + SQLite
- Frontend: React + TypeScript + Vite
- Testes: xUnit (backend), Vitest (frontend)

## Arquitetura
- Vertical slices por feature (`Features/Cursos`, `Features/Dashboard`), sem camadas genéricas vazias
- Domínio sem dependência de infraestrutura; validação na borda (FluentValidation)
- Sem repositório sobre EF Core; usar DbContext direto
- Abstração só quando houver segundo uso real (YAGNI)

## Domínio
Course (campos iguais ao seed): day, date, platform, name, area, url, estimatedHours, hoursDone, status (NotStarted|InProgress|Completed|Skipped), completedAt, certificateReceived, certificateUrl, onLinkedIn, notes.
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
