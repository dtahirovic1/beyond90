FROM node:22-bookworm-slim AS frontend
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY backend/Beyond90.Api/Beyond90.Api.csproj backend/Beyond90.Api/
COPY backend/Beyond90.Domain/Beyond90.Domain.csproj backend/Beyond90.Domain/
COPY backend/Beyond90.Analytics/Beyond90.Analytics.csproj backend/Beyond90.Analytics/
COPY backend/Beyond90.Ai/Beyond90.Ai.csproj backend/Beyond90.Ai/
RUN dotnet restore backend/Beyond90.Api/Beyond90.Api.csproj
COPY backend/ backend/
COPY data/synthetic/ data/synthetic/
RUN dotnet publish backend/Beyond90.Api/Beyond90.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=backend /app/publish ./
COPY --from=frontend /src/frontend/dist ./wwwroot
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=10000
EXPOSE 10000
USER $APP_UID
ENTRYPOINT ["sh", "-c", "exec dotnet Beyond90.Api.dll --urls \"http://0.0.0.0:${PORT:-10000}\""]
