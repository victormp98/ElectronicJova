Set-Content -Path Dockerfile -Value 'FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ElectronicJova/ElectronicJova.csproj", "ElectronicJova/"]
RUN dotnet restore "ElectronicJova/ElectronicJova.csproj"
COPY . .
WORKDIR "/src/ElectronicJova"
RUN dotnet publish "ElectronicJova.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ElectronicJova.dll"]' -Encoding Ascii