FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
# curl is needed for the Docker healthcheck below — not in the base runtime image.
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Shared/AtlasBank.Shared/AtlasBank.Shared.csproj", "src/Shared/AtlasBank.Shared/"]
COPY ["src/Shared/AtlasBank.Grpc/AtlasBank.Grpc.csproj", "src/Shared/AtlasBank.Grpc/"]
COPY ["src/Services/CustomerService/AtlasBank.CustomerService/AtlasBank.CustomerService.csproj", "src/Services/CustomerService/AtlasBank.CustomerService/"]
RUN dotnet restore "src/Services/CustomerService/AtlasBank.CustomerService/AtlasBank.CustomerService.csproj"
COPY . .
RUN dotnet build "src/Services/CustomerService/AtlasBank.CustomerService/AtlasBank.CustomerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/Services/CustomerService/AtlasBank.CustomerService/AtlasBank.CustomerService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Microsoft's aspnet base image ships a ready-made non-root "app" user (owns /app
# already) specifically for this — the image just doesn't switch to it by default.
USER $APP_UID
ENTRYPOINT ["dotnet", "AtlasBank.CustomerService.dll"]
