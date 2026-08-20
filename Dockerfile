# syntax=docker/dockerfile:1.10
FROM mcr.microsoft.com/dotnet/sdk:10.0.302-alpine3.24@sha256:979da27fc87dc255f4675b7642556cdcba9307459f8891f85f3cc26edcd7e766 AS build

WORKDIR /src
COPY .editorconfig global.json Directory.Build.props ItEquipmentCheckout.slnx ./
COPY src/ItEquipmentCheckout.Core/ItEquipmentCheckout.Core.csproj src/ItEquipmentCheckout.Core/packages.lock.json src/ItEquipmentCheckout.Core/
COPY src/ItEquipmentCheckout.Web/ItEquipmentCheckout.Web.csproj src/ItEquipmentCheckout.Web/packages.lock.json src/ItEquipmentCheckout.Web/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore src/ItEquipmentCheckout.Web/ItEquipmentCheckout.Web.csproj --locked-mode

COPY src/ src/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish src/ItEquipmentCheckout.Web/ItEquipmentCheckout.Web.csproj \
      --configuration Release \
      --no-restore \
      --output /app/publish \
      /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.11-alpine3.24@sha256:c4b29bf368004ad9076c1ab9bc91fb373561e3905b4345637e14e8b8c57e3be8

WORKDIR /app
RUN mkdir -p /app/data && chown -R "$APP_UID:0" /app
COPY --from=build --chown=$APP_UID:0 /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    ConnectionStrings__CheckoutDatabase="Data Source=/app/data/checkout.db" \
    Database__Seed=true \
    DOTNET_EnableDiagnostics=0

USER $APP_UID
EXPOSE 8080
VOLUME ["/app/data"]
ENTRYPOINT ["dotnet", "ItEquipmentCheckout.Web.dll"]

HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=3 \
  CMD wget --spider --quiet http://127.0.0.1:8080/health || exit 1
