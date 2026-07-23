# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/Fixar.Domain/Fixar.Domain.csproj src/Fixar.Domain/
COPY src/Fixar.Application/Fixar.Application.csproj src/Fixar.Application/
COPY src/Fixar.Infrastructure/Fixar.Infrastructure.csproj src/Fixar.Infrastructure/
COPY src/Fixar.API/Fixar.API.csproj src/Fixar.API/
RUN dotnet restore src/Fixar.API/Fixar.API.csproj

COPY src/ src/
RUN dotnet publish src/Fixar.API/Fixar.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && addgroup --system --gid 1000 fixar \
    && adduser --system --uid 1000 --ingroup fixar --shell /bin/false fixar \
    && mkdir -p /var/log/fixar-os \
    && chown -R fixar:fixar /var/log/fixar-os

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

USER fixar

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "Fixar.API.dll"]
