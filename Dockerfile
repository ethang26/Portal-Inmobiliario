# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copia el csproj para aprovechar la cache
COPY ["Portal-Inmobiliario.csproj", "./"]
RUN dotnet restore "Portal-Inmobiliario.csproj"

# copia el resto y publica
COPY . .
RUN dotnet publish "Portal-Inmobiliario.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# copia lo publicado
COPY --from=build /app/publish .

# Render te inyecta PORT y (si quieres) ASPNETCORE_URLS desde variables
ENTRYPOINT ["dotnet", "Portal-Inmobiliario.dll"]
