FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Shared/AtlasBank.Shared/AtlasBank.Shared.csproj", "src/Shared/AtlasBank.Shared/"]
COPY ["src/Services/LedgerService/AtlasBank.LedgerService/AtlasBank.LedgerService.csproj", "src/Services/LedgerService/AtlasBank.LedgerService/"]
RUN dotnet restore "src/Services/LedgerService/AtlasBank.LedgerService/AtlasBank.LedgerService.csproj"
COPY . .
RUN dotnet build "src/Services/LedgerService/AtlasBank.LedgerService/AtlasBank.LedgerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/Services/LedgerService/AtlasBank.LedgerService/AtlasBank.LedgerService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AtlasBank.LedgerService.dll"]
