# 1) Frontend
FROM node:24-alpine AS web
WORKDIR /web
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# 2) API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY seed-courses.json ./
COPY backend/ backend/
RUN dotnet publish backend/CursoTracker.Api -c Release -o /app

# 3) Runtime: API servindo o frontend
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api /app ./
COPY --from=web /web/dist ./wwwroot
# Volumes da Railway são montados como root; o usuário "app" da imagem não conseguiria gravar o SQLite
USER root
RUN mkdir -p /data
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    ConnectionStrings__Default="Data Source=/data/cursotracker.db"
EXPOSE 8080
ENTRYPOINT ["dotnet", "CursoTracker.Api.dll"]
