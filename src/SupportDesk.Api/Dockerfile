FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json ./
COPY src/SupportDesk.Domain/SupportDesk.Domain.csproj src/SupportDesk.Domain/
COPY src/SupportDesk.Application/SupportDesk.Application.csproj src/SupportDesk.Application/
COPY src/SupportDesk.Infrastructure/SupportDesk.Infrastructure.csproj src/SupportDesk.Infrastructure/
COPY src/SupportDesk.Api/SupportDesk.Api.csproj src/SupportDesk.Api/
RUN dotnet restore src/SupportDesk.Api/SupportDesk.Api.csproj

COPY src/ src/
RUN dotnet publish src/SupportDesk.Api/SupportDesk.Api.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /var/lib/supportdesk/files /home/app/.aspnet/DataProtection-Keys \
    && chown -R app:app /var/lib/supportdesk /home/app/.aspnet
COPY --from=build --chown=app:app /app/publish ./
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "SupportDesk.Api.dll"]
