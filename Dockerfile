FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# 1. Copy solution and project files for restore caching
COPY LedgerCore.slnx ./
COPY LedgerCore.Domain/LedgerCore.Domain.csproj LedgerCore.Domain/
COPY LedgerCore.Engine/LedgerCore.Engine.csproj LedgerCore.Engine/
COPY LedgerCore/LedgerCore.csproj LedgerCore/
COPY LedgerCore.Tests/LedgerCore.Tests.csproj LedgerCore.Tests/

# 2. Restore solution
RUN dotnet restore LedgerCore.slnx

# 3. Copy full repository source code
COPY . ./

# 4. Build solution
RUN dotnet build LedgerCore.slnx -c Release --no-restore

# 5. Run tests
FROM build AS test
RUN dotnet test LedgerCore.Tests/LedgerCore.Tests.csproj \
    -c Release \
    --no-build \
    --no-restore

# 6. Publish application
FROM build AS publish
RUN dotnet publish LedgerCore/LedgerCore.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# 7. Runtime Stage
FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS final
WORKDIR /app

RUN addgroup -S appgroup && adduser -S appuser -G appgroup \
    && chown -R appuser:appgroup /app

COPY --chown=appuser:appgroup --from=publish /app/publish ./

USER appuser

ENTRYPOINT ["dotnet", "LedgerCore.dll"]