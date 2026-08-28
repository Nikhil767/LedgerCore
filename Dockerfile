FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy solution and all .csproj files first
COPY LedgerCore.slnx ./
COPY LedgerCore.Domain/LedgerCore.Domain.csproj LedgerCore.Domain/
COPY LedgerCore.Engine/LedgerCore.Engine.csproj LedgerCore.Engine/
COPY LedgerCore/LedgerCore.csproj LedgerCore/
COPY LedgerCore.Tests/LedgerCore.Tests.csproj LedgerCore.Tests/

# Restore both projects
RUN dotnet restore LedgerCore.slnx

# Copy remaining source code
COPY . ./

# Build everything (including tests)
RUN dotnet build LedgerCore.slnx -c Release

# Run tests
RUN dotnet test LedgerCore.Tests/LedgerCore.Tests.csproj \
    -c Release \
    --no-build \
    --no-restore

RUN dotnet publish LedgerCore/LedgerCore.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false	

FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS final
WORKDIR /app

# Run as non-root user for security
RUN addgroup -S appgroup && adduser -S appuser -G appgroup

# Copy published files from publish stage
COPY --from=build /app/publish ./

USER appuser

ENTRYPOINT ["dotnet", "LedgerCore.dll"]