# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Kopiert die Projektdatei, die direkt im Build-Kontext liegt (Ordner: MyWeatherApi)
COPY ["MyWeatherApi/MyWeatherApi.csproj", "./"]
RUN dotnet restore "MyWeatherApi.csproj"

# Kopiere alle restlichen Dateien
COPY . .
RUN dotnet build "MyWeatherApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 2: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "MyWeatherApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyWeatherApi.dll"]
