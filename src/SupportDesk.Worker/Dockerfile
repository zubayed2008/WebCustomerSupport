FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json ./
COPY src/SupportDesk.Domain/SupportDesk.Domain.csproj src/SupportDesk.Domain/
COPY src/SupportDesk.Application/SupportDesk.Application.csproj src/SupportDesk.Application/
COPY src/SupportDesk.Infrastructure/SupportDesk.Infrastructure.csproj src/SupportDesk.Infrastructure/
COPY src/SupportDesk.Worker/SupportDesk.Worker.csproj src/SupportDesk.Worker/
RUN dotnet restore src/SupportDesk.Worker/SupportDesk.Worker.csproj

COPY src/ src/
RUN dotnet publish src/SupportDesk.Worker/SupportDesk.Worker.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /var/lib/supportdesk/files \
    && chown -R app:app /var/lib/supportdesk
COPY --from=build --chown=app:app /app/publish ./
USER app
ENTRYPOINT ["dotnet", "SupportDesk.Worker.dll"]
