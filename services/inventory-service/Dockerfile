FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY services/inventory-service/Inventory.Api.csproj services/inventory-service/
RUN dotnet restore services/inventory-service/Inventory.Api.csproj
COPY services/inventory-service/ services/inventory-service/
RUN dotnet publish services/inventory-service/Inventory.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Inventory.Api.dll"]
