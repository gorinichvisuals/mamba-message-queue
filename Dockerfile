FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY . .
RUN dotnet publish sln/MambaMQ.Server/MambaMQ.Server.csproj \
    --configuration $BUILD_CONFIGURATION \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final

WORKDIR /app
RUN mkdir -p /app/data && chown app:app /app /app/data
COPY --from=build --chown=app:app /app/publish .
COPY --chown=app:app docker-entrypoint.sh /app/docker-entrypoint.sh

USER app
EXPOSE 24/tcp

ENTRYPOINT ["/bin/sh", "/app/docker-entrypoint.sh"]