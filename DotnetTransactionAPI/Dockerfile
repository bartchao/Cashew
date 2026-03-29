# =============================================================================
# Multi-stage Dockerfile for CashewAPI
# Builds and runs the .NET 10 CashewAPI web application in a minimal container.
# =============================================================================

# ---------------------------------------------------------------------------
# Stage 1: Build
# Uses the full .NET SDK image to restore dependencies and compile the project.
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy the solution file and the project file first so that dependency
# restoration is cached independently of source-code changes.
COPY CashewAPI.slnx ./
COPY CashewAPI/CashewAPI.csproj CashewAPI/

# Restore NuGet packages as a separate layer for better Docker cache usage.
RUN dotnet restore CashewAPI/CashewAPI.csproj

# Copy the remaining application source code (tests are excluded via
# .dockerignore and are not needed in the final image).
COPY CashewAPI/ CashewAPI/

# Publish a self-contained Release build to the /app/publish directory.
# The --no-restore flag skips restore since we already ran it above.
RUN dotnet publish CashewAPI/CashewAPI.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---------------------------------------------------------------------------
# Stage 2: Runtime
# Uses the lightweight ASP.NET runtime image to keep the final image small.
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Create directories for runtime data:
#   /app/data        - SQLite database files (mount as a volume for persistence)
#   /app/credentials - Google Drive service-account credentials
RUN mkdir -p /app/data /app/credentials

# Copy the published application from the build stage.
COPY --from=build /app/publish .

# ASP.NET Core listens on port 8080 by default inside containers.
EXPOSE 8080

# Run in Production mode so that development-only features are disabled.
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application.
ENTRYPOINT ["dotnet", "CashewAPI.dll"]
