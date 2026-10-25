# CursoTracker

Acompanhamento de cursos gratuitos com certificado.

**App no celular:** https://brunomiqueas122-tech.github.io/cursotracker/ — abra no navegador e use "Adicionar à tela inicial". Os dados ficam no aparelho (use Backup para guardar uma cópia).

## Modos

| Modo | Dados | Como gerar |
|---|---|---|
| Celular (GitHub Pages) | `localStorage` do aparelho, offline | `npm run build:pages` — publicado pelo GitHub Actions a cada push na `main` |
| PC / servidor | API .NET + SQLite, com login | `dotnet run` + `npm run dev`, ou a imagem Docker (`Auth__Password` obrigatório) |

O "Dia N" vem da data: a data mais antiga do plano é o dia 1.

## Rodar (modo servidor)

```bash
# terminal 1 — API em http://localhost:5080 (cria cursotracker.db e aplica o seed na 1ª vez; senha local: dev)
cd backend/CursoTracker.Api
dotnet run

# terminal 2 — app em http://localhost:5173
cd frontend
npm install
npm run dev
```

## Testes

```bash
cd backend && dotnet test
cd frontend && npm test
```

## API

| Método | Rota | |
|---|---|---|
| GET | `/api/courses?status=&area=` | Lista ordenada por dia |
| GET | `/api/courses/{id}` | Um curso |
| POST | `/api/courses` | Novo curso (data, nome, plataforma, área, link, horas estimadas) |
| PUT | `/api/courses/{id}` | Edita ou reagenda; os dias do plano são recalculados |
| DELETE | `/api/courses/{id}` | Remove o curso |
| PUT | `/api/courses/{id}/progress` | Horas, status, certificado, LinkedIn, anotações |
| GET | `/api/dashboard` | % concluído, certificados, horas (total e por dia) |
