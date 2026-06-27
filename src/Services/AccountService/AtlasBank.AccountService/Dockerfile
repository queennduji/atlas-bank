FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Shared/AtlasBank.Shared/AtlasBank.Shared.csproj", "src/Shared/AtlasBank.Shared/"]
COPY ["src/Services/AccountService/AtlasBank.AccountService/AtlasBank.AccountService.csproj", "src/Services/AccountService/AtlasBank.AccountService/"]
RUN dotnet restore "src/Services/AccountService/AtlasBank.AccountService/AtlasBank.AccountService.csproj"
COPY . .
RUN dotnet build "src/Services/AccountService/AtlasBank.AccountService/AtlasBank.AccountService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/Services/AccountService/AtlasBank.AccountService/AtlasBank.AccountService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AtlasBank.AccountService.dll"]
