FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY services/billing-service/Billing.Api.csproj services/billing-service/
RUN dotnet restore services/billing-service/Billing.Api.csproj
COPY services/billing-service/ services/billing-service/
RUN dotnet publish services/billing-service/Billing.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Billing.Api.dll"]
