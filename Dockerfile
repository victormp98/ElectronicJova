FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar proyecto y restaurar
COPY ["ElectronicJova/ElectronicJova.csproj", "ElectronicJova/"]
RUN dotnet restore "ElectronicJova/ElectronicJova.csproj"

# Publicar
COPY . .
WORKDIR "/src/ElectronicJova"
RUN dotnet publish "ElectronicJova.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ElectronicJova.dll"]