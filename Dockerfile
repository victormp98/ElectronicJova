FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# Cache buster to force a full rebuild
ENV CACHE_BUSTER=2024-05-20-01
WORKDIR /src
COPY ["ElectronicJova/ElectronicJova.csproj", "ElectronicJova/"]
RUN dotnet restore "ElectronicJova/ElectronicJova.csproj"
COPY . .
WORKDIR "/src/ElectronicJova"
RUN dotnet publish "ElectronicJova.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ElectronicJova.dll"]
