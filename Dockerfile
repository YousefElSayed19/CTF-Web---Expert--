# ── Stage 1: Build ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY VaultCorp.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ───────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Create persistent data directory (SQLite lives here)
RUN mkdir -p /app/data && chmod 777 /app/data

# wwwroot must be writable — CommandExecutorJob writes output files there
RUN chmod -R 777 /app/wwwroot

# Flag — injected at deploy time via env var or directly
ARG FLAG=VCT{placeholder_set_me_in_docker_compose}
RUN echo "$FLAG" > /app/flag.txt && chmod 444 /app/flag.txt

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "VaultCorp.dll"]
