# ─────────────────────────────────────────────────────────────
# Stage 1: Build
# ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Shared/Shared.csproj src/Shared/
COPY src/AzureFunctions/AzureFunctions.csproj src/AzureFunctions/

RUN dotnet restore src/AzureFunctions/AzureFunctions.csproj

COPY src/Shared/ src/Shared/
COPY src/AzureFunctions/ src/AzureFunctions/

WORKDIR /src/src/AzureFunctions
RUN dotnet publish -c Release -o /app/publish --no-restore

# ─────────────────────────────────────────────────────────────
# Stage 2: Azure Functions Runtime
# ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS runtime
WORKDIR /home/site/wwwroot

COPY --from=build /app/publish .

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated

EXPOSE 80
