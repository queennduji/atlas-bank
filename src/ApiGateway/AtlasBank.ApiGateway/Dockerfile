FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
# curl is needed for the Docker healthcheck below — not in the base runtime image.
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Shared/AtlasBank.Shared/AtlasBank.Shared.csproj", "src/Shared/AtlasBank.Shared/"]
COPY ["src/ApiGateway/AtlasBank.ApiGateway/AtlasBank.ApiGateway.csproj", "src/ApiGateway/AtlasBank.ApiGateway/"]
RUN dotnet restore "src/ApiGateway/AtlasBank.ApiGateway/AtlasBank.ApiGateway.csproj"
COPY . .
RUN dotnet build "src/ApiGateway/AtlasBank.ApiGateway/AtlasBank.ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/ApiGateway/AtlasBank.ApiGateway/AtlasBank.ApiGateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AtlasBank.ApiGateway.dll"]
