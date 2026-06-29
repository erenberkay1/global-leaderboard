# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Önce sadece csproj'u kopyalayıp restore et (katman önbelleği için)
COPY server/LeaderboardApi/LeaderboardApi.csproj ./LeaderboardApi/
RUN dotnet restore ./LeaderboardApi/LeaderboardApi.csproj

# Geri kalan kaynak kodu kopyala ve yayınla
COPY server/LeaderboardApi/ ./LeaderboardApi/
RUN dotnet publish ./LeaderboardApi/LeaderboardApi.csproj -c Release -o /app/publish

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "LeaderboardApi.dll"]
