# ==========================================================
#  Auth Service — multi-stage build
#
#  ONEMLI: build context REPO KOKUDUR, bu klasor degil:
#     docker build -f src/services/AuthService/AuthService.Api/Dockerfile .
#  Cunku servis src/BuildingBlocks/* projelerine referans veriyor ve
#  Docker, context disindaki dosyayi kopyalayamaz.
# ==========================================================

# ---------- 1) build: SDK imaji, sadece derleme icin ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Once SADECE proje dosyalari + merkezi ayarlar kopyalanir ve restore edilir.
# Neden? Docker her satiri katman olarak cache'ler. Kaynak kod degistiginde
# bu katman BOZULMAZ -> restore tekrar calismaz, build saniyeler surer.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/BuildingBlocks/BuildingBlocks.Domain/BuildingBlocks.Domain.csproj                 src/BuildingBlocks/BuildingBlocks.Domain/
COPY src/BuildingBlocks/BuildingBlocks.Application/BuildingBlocks.Application.csproj       src/BuildingBlocks/BuildingBlocks.Application/
COPY src/BuildingBlocks/BuildingBlocks.Infrastructure/BuildingBlocks.Infrastructure.csproj src/BuildingBlocks/BuildingBlocks.Infrastructure/
COPY src/services/AuthService/AuthService.Domain/AuthService.Domain.csproj                 src/services/AuthService/AuthService.Domain/
COPY src/services/AuthService/AuthService.Application/AuthService.Application.csproj       src/services/AuthService/AuthService.Application/
COPY src/services/AuthService/AuthService.Infrastructure/AuthService.Infrastructure.csproj src/services/AuthService/AuthService.Infrastructure/
COPY src/services/AuthService/AuthService.Api/AuthService.Api.csproj                       src/services/AuthService/AuthService.Api/

RUN dotnet restore src/services/AuthService/AuthService.Api/AuthService.Api.csproj

# Simdi kaynak kod. Bu satirdan sonrasi her kod degisikliginde yeniden calisir.
COPY src/ src/

RUN dotnet publish src/services/AuthService/AuthService.Api/AuthService.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ---------- 2) runtime: SDK yok, sadece calistirma ----------
# ~200 MB'lik ASP.NET runtime; SDK (~800 MB) imaja hic girmiyor.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# compose healthcheck'i HTTP istegi atacak; aspnet imajinda curl YOK.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# Imaj root olarak calismaz. APP_UID, Microsoft'un imajinda tanimli
# ayricaliksiz kullanici. Container kacisi olsa bile root degil.
USER $APP_UID

# ASP.NET 8+ imajlari varsayilan olarak 8080 dinler (eskiden 80'di).
EXPOSE 8080

ENTRYPOINT ["dotnet", "SmartLogistics.AuthService.Api.dll"]
