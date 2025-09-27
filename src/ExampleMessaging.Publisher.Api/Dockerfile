# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set working directory
WORKDIR /app

# Copy solution file and project files
COPY *.sln .
COPY src/ExampleMessaging.Shared/*.csproj ./src/ExampleMessaging.Shared/
COPY src/ExampleMessaging.Publisher.Api/*.csproj ./src/ExampleMessaging.Publisher.Api/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ExampleMessaging.Shared/ ./src/ExampleMessaging.Shared/
COPY src/ExampleMessaging.Publisher.Api/ ./src/ExampleMessaging.Publisher.Api/

# Build the application
WORKDIR /app/src/ExampleMessaging.Publisher.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Use the official .NET 8 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set working directory
WORKDIR /app

# Create a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Create logs directory and set permissions
RUN mkdir -p /app/logs && chown -R appuser:appuser /app

# Copy the published application
COPY --from=build /app/publish .

# Change ownership of the application files
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "ExampleMessaging.Publisher.Api.dll"]