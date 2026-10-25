# CursoTracker

Acompanhamento de cursos gratuitos com certificado.

## Rodar

```bash
# terminal 1 — API em http://localhost:5080 (cria cursotracker.db e aplica o seed na 1ª vez)
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
| PUT | `/api/courses/{id}/progress` | Horas, status, certificado, LinkedIn, anotações |
| GET | `/api/dashboard` | % concluído, certificados, horas (total e por dia) |
